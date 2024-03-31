using Microsoft.EntityFrameworkCore;
using CRSmallWorldBackend.Models;

namespace CRSmallWorldBackend.Handlers;

public interface ITransactionHandler
{
    Task<IResult> ProcessTransaction(Transaction transaction);
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

        var previousDebitTransactions = _transactionDb.Transactions
            .Where(t => t.DebitWalletId == transaction.DebitWalletId)
            .OrderBy(t => t.TimeStamp);

        // Filter the previous transactions to get the ones that will be consumed
        // by the current transaction
        var debitTransactionsToConsume = await previousDebitTransactions
            .TakeWhile(t => (t.Points - t.SpentPoints) < transaction.Points)
            .ToListAsync();

        // Consume points from each of these previous transactions until the
        // current transaction amount is reached
        long totalConsumedPoints = 0;
        foreach (var debitTransaction in debitTransactionsToConsume)
        {
            var remainingPoints = debitTransaction.Points - debitTransaction.SpentPoints;
            var pointsToConsume = Math.Min(remainingPoints, transaction.Points - totalConsumedPoints);

            debitTransaction.SpentPoints += pointsToConsume;
            totalConsumedPoints += pointsToConsume;
        }

        // Update the wallet balances
        fromWallet!.Balance -= transaction.Points;
        toWallet!.Balance += transaction.Points;

        // Insert the new transaction
        _transactionDb.Transactions.Add(transaction);

        // Commit all of the above in an atomic database transaction
        await _transactionDb.SaveChangesAsync();

        return Results.Ok();
    }
}