namespace CRSmallWorldBackend.Handlers;
public class TransactionHandler(TransactionDb pointsBalanceDb, WalletDb walletDb)
{
    private readonly TransactionDb _transactionDb = pointsBalanceDb;
    private readonly WalletDb _walletDb = walletDb;

    public IResult? ValidateTransaction(Transaction transaction)
    {
        if (transaction.Amount <= 0)
        {
            return Results.BadRequest("Amount must be greater than 0");
        }
        if (transaction.CreditWallet == null)
        {
            return Results.BadRequest("Credit wallet ID is required");
        }
        if (transaction.CreditWallet.Id == transaction.DebitWallet.Id)
        {
            return Results.BadRequest("Cannot transfer to the same wallet");
        }

        return null;
    }

    public IResult? ValidateWallets(Wallet? fromWallet, Wallet? toWallet, long amount)
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

    public async Task<IResult> ProcessTransaction(Transaction transaction)
    {
        var transactionValidationError = ValidateTransaction(transaction);
        if (transactionValidationError != null) return transactionValidationError;

        var fromWallet = await walletDb.Wallets.FindAsync(transaction.CreditWallet?.Id);
        var toWallet = await walletDb.Wallets.FindAsync(transaction.DebitWallet.Id);

        var walletValidationError = ValidateWallets(fromWallet, toWallet, transaction.Amount);
        if (walletValidationError != null) return walletValidationError;

        var previousPointIncreases = await _transactionDb.Transactions
            .Where(t => t.DebitWalletId == transaction.DebitWallet.Id)
            .SumAsync(t => t.Amount);
    }
}