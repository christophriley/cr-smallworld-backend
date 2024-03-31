using CRSmallWorldBackend.Handlers;
using CRSmallWorldBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<WalletDb>(options => options.UseInMemoryDatabase("Wallets"));
builder.Services.AddDbContext<TransactionDb>(options => options.UseInMemoryDatabase("Transactions"));
builder.Services.AddScoped<ITransactionHandler, TransactionHandler>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "Chris Riley Small World API";
    config.Title = "CRSmallWorldAPI";
    config.Version = "v1";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "TodoAPI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

var scope = app.Services.CreateScope();
var transactionHandler = scope.ServiceProvider.GetRequiredService<ITransactionHandler>();
await transactionHandler.GiftPoints("8c18b5bf-0171-4918-a611-bde754382f7a", 2500, DateTime.Parse("2021-10-01T11:00:00Z"));
await transactionHandler.GiftPoints("21f51c05-e556-41f1-9cc9-0a314bb2ebcc", 200, DateTime.Parse("2021-10-01T11:00:00Z"));
await transactionHandler.GiftPoints("d5af01f0-515a-4834-ab4e-a2f54aeaedbf", 15300, DateTime.Parse("2021-10-01T11:00:00Z"));
await transactionHandler.GiftPoints("363a3f19-7fa9-4e34-851d-6e42ef92a285", 0, DateTime.Parse("2021-10-01T11:00:00Z"));

app.MapGet("/wallets", (WalletDb db) =>
{
    return db.Wallets.ToDictionary(wallet => wallet.Id, wallet => wallet.Balance);
});

app.MapPut("/wallets", async ([FromBody] string WalletId, WalletDb db) =>
{
    var newWallet = new Wallet { Id = WalletId, Balance = 0 };
    db.Wallets.Add(newWallet);
    await db.SaveChangesAsync();
    return Results.Created($"/wallets/{newWallet.Id}", newWallet);
});

app.MapPut("/transactions", async (Transaction transaction, TransactionDb transactionDb, WalletDb walletDb, ITransactionHandler transactionHandler) =>
{
    return await transactionHandler.ProcessTransaction(transaction);
});

app.MapGet("/transactions", (TransactionDb db) =>
{
    return db.Transactions
        .OrderByDescending(transaction => transaction.TimeStamp)
        .ToList();
});

app.MapPut("/gifts", async (Gift gift, ITransactionHandler transactionHandler) =>
{
    return await transactionHandler.GiftPoints(gift.ToWalletId, gift.Points);
});

app.MapPut("/spends", async (Spend spend, ITransactionHandler transactionHandler) =>
{
    return await transactionHandler.SpendPoints(spend.FromWalletId, spend.Points);
});

app.MapGet("/test", async (WalletDb db) =>
{
    // replicate transactions from exercise spec
    List<Transaction> testTransactions = [
        new() {
            CreditWalletId = "8c18b5bf-0171-4918-a611-bde754382f7a",
            DebitWalletId = "363a3f19-7fa9-4e34-851d-6e42ef92a285",
            Points = 1000,
            TimeStamp = DateTime.Parse("2021-11-02T14:00:00Z"),
        },
        new() {
            CreditWalletId = "21f51c05-e556-41f1-9cc9-0a314bb2ebcc",
            DebitWalletId = "363a3f19-7fa9-4e34-851d-6e42ef92a285",
            Points = 200,
            TimeStamp = DateTime.Parse("2021-10-31T11:00:00Z"),
        },
        new() {
            CreditWalletId = "8c18b5bf-0171-4918-a611-bde754382f7a",
            DebitWalletId = "363a3f19-7fa9-4e34-851d-6e42ef92a285",
            Points = 200,
            TimeStamp = DateTime.Parse("2021-10-31T15:00:00Z"),
        },
        new() {
            CreditWalletId = "d5af01f0-515a-4834-ab4e-a2f54aeaedbf",
            DebitWalletId = "363a3f19-7fa9-4e34-851d-6e42ef92a285",
            Points = 10000,
            TimeStamp = DateTime.Parse("2021-11-01T14:00:00Z"),
        },
        new() {
            CreditWalletId = "8c18b5bf-0171-4918-a611-bde754382f7a",
            DebitWalletId = "363a3f19-7fa9-4e34-851d-6e42ef92a285",
            Points = 300,
            TimeStamp = DateTime.Parse("2021-10-31T10:00:00Z"),
        },
    ];

    foreach (var transaction in testTransactions)
    {
        await transactionHandler.ProcessTransaction(transaction);
    }

    return db.Wallets.ToDictionary(wallet => wallet.Id, wallet => wallet.Balance);
});


app.Run();
