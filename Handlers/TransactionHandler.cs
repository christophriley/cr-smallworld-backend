using Microsoft.EntityFrameworkCore;
using CRSmallWorldBackend.Models;

namespace CRSmallWorldBackend.Handlers;

public interface ITransactionHandler
{
    Task<IResult> GiftPoints(string toWalletId, long points, DateTime? timeStamp = null);
    Task<IResult> ProcessTransaction(Transaction transaction);
    Task<IResult> SpendPoints(string walletId, long points);
}

public class TransactionHandler(TransactionDb pointsBalanceDb, WalletDb walletDb) : ITransactionHandler
{
    private readonly TransactionDb _transactionDb = pointsBalanceDb;
    private readonly WalletDb _walletDb = walletDb;

    protected IResult? ValidateTransaction(Transaction transaction)
    {
        if (transaction.Points <= 0)
        {
            return Results.BadRequest("Amount must be greater than 0");
        }
        if (transaction.CreditWalletId == null)
        {
            return Results.BadRequest("Credit wallet ID is required");
        }
        if (transaction.CreditWalletId == transaction.DebitWalletId)
        {
            return Results.BadRequest("Cannot transfer to the same wallet");
        }

        return null;
    }

    protected IResult? ValidateWallets(Wallet? fromWallet, Wallet? toWallet, long amount)
    {
        if (fromWallet == null)
        {
            return Results.NotFound("Credit wallet not found");
        }

        if (toWallet == null)
        {
            return Results.NotFound("Debit wallet not found");
        }

        if (fromWallet.Balance < amount)
        {
            return Results.BadRequest("Insufficient funds");
        }

        return null;
    }

    protected async Task<List<PointDeduction>> ConsumePointsFromPreviousTransactions(Transaction newTransaction)
    {
        List<PointDeduction> pointDeductions = [];

        var debitTransactionsToConsume = await _transactionDb.Transactions
            .Where(t => t.DebitWalletId == newTransaction.CreditWalletId)
            .OrderBy(t => t.TimeStamp)
            .Select(t => new
            {
                Transaction = t,
                CumulativePoints = _transactionDb.Transactions
                    .Where(t2 => t2.DebitWalletId == newTransaction.CreditWalletId && t2.TimeStamp <= t.TimeStamp)
                    .Sum(t2 => t2.Points - t2.SpentPoints)
            })
            .Where(x => x.CumulativePoints - (x.Transaction.Points - x.Transaction.SpentPoints) <= newTransaction.Points)
            .Select(x => x.Transaction)
            .ToListAsync();

        // Consume points from each of these previous transactions until the
        // current transaction amount is reached
        long totalConsumedPoints = 0;
        foreach (var debitTransaction in debitTransactionsToConsume)
        {
            var remainingPoints = debitTransaction.Points - debitTransaction.SpentPoints;
            var pointsToConsume = Math.Min(remainingPoints, newTransaction.Points - totalConsumedPoints);

            debitTransaction.SpentPoints += pointsToConsume;
            totalConsumedPoints += pointsToConsume;

            pointDeductions.Add(new PointDeduction
            {
                WalletId = debitTransaction.CreditWalletId!,
                Points = pointsToConsume
            });
        }

        return pointDeductions;
    }

    /* This is how points enter the economy. Care needs to be taken that it is
        only called by trusted sources, as it can be used to inflate the economy
    */
    public async Task<IResult> GiftPoints(string toWalletId, long points, DateTime? timeStamp)
    {
        var toWallet = await _walletDb.Wallets.FindAsync(toWalletId);
        if (toWallet == null)
        {
            toWallet = new Wallet
            {
                Id = toWalletId,
                Balance = 0
            };
            _walletDb.Wallets.Add(toWallet);
        }

        Transaction transaction = new()
        {
            TimeStamp = timeStamp ?? DateTime.Now,
            Points = points,
            DebitWalletId = toWalletId
        };

        toWallet.Balance += points;
        _transactionDb.Transactions.Add(transaction);

        using var scope = new System.Transactions.TransactionScope(
            System.Transactions.TransactionScopeOption.Required,
            new System.Transactions.TransactionOptions
            {
                IsolationLevel = System.Transactions.IsolationLevel.Serializable
            });
        await _walletDb.SaveChangesAsync();
        await _transactionDb.SaveChangesAsync();
        scope.Complete(); // Commit the database transaction

        return Results.Ok(toWallet);
    }


    /*
        Process a transaction
        - Validate the transaction
        - Validate the wallet balances
        - Consume points from previous transactions
        - Update the wallet balances
        - Insert the new transaction
        - Commit all of the above in an atomic database transaction
    */
    public async Task<IResult> ProcessTransaction(Transaction transaction)
    {
        var transactionValidationError = ValidateTransaction(transaction);
        if (transactionValidationError != null) return transactionValidationError;

        var fromWallet = await _walletDb.Wallets.FindAsync(transaction.CreditWalletId);
        var toWallet = await _walletDb.Wallets.FindAsync(transaction.DebitWalletId);

        var walletValidationError = ValidateWallets(fromWallet, toWallet, transaction.Points);
        if (walletValidationError != null) return walletValidationError;

        var pointDeductions = await ConsumePointsFromPreviousTransactions(transaction);

        if (pointDeductions.Sum(pd => pd.Points) < transaction.Points)
        {
            return Results.Problem("Could not find enough points to deduct");
        }

        // Update the wallet balances
        fromWallet!.Balance -= transaction.Points;
        toWallet!.Balance += transaction.Points;

        // Insert the new transaction
        _transactionDb.Transactions.Add(transaction);

        // Commit all of the above in an atomic database transaction
        using var scope = new System.Transactions.TransactionScope(
            System.Transactions.TransactionScopeOption.Required,
            new System.Transactions.TransactionOptions
            {
                IsolationLevel = System.Transactions.IsolationLevel.Serializable
            });
        await _walletDb.SaveChangesAsync();
        await _transactionDb.SaveChangesAsync();
        scope.Complete(); // Commit the database transaction

        return Results.Ok(pointDeductions);
    }

    public async Task<IResult> SpendPoints(string walletId, long points)
    {
        var wallet = await _walletDb.Wallets.FindAsync(walletId);
        if (wallet == null)
        {
            return Results.NotFound("Wallet not found");
        }

        if (wallet.Balance < points)
        {
            return Results.BadRequest("Insufficient funds");
        }

        var spendTransaction = new Transaction
        {
            TimeStamp = DateTime.Now,
            Points = points,
            CreditWalletId = walletId
        };

        var pointDeductions = await ConsumePointsFromPreviousTransactions(spendTransaction);
        if (pointDeductions.Sum(pd => pd.Points) < points)
        {
            return Results.Problem("Could not find enough points to deduct");
        }

        wallet.Balance -= points;
        _transactionDb.Transactions.Add(spendTransaction);

        // Commit all of the above in an atomic database transaction
        using var scope = new System.Transactions.TransactionScope(
            System.Transactions.TransactionScopeOption.Required,
            new System.Transactions.TransactionOptions
            {
                IsolationLevel = System.Transactions.IsolationLevel.Serializable
            });
        await _walletDb.SaveChangesAsync();
        await _transactionDb.SaveChangesAsync();
        scope.Complete(); // Commit the database transaction

        return Results.Ok(pointDeductions);
    }
}