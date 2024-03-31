using CRSmallWorldBackend;
using CRSmallWorldBackend.Handlers;
using CRSmallWorldBackend.Models;
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
await transactionHandler.GiftPoints("8c18b5bf-0171-4918-a611-bde754382f7a", 2500);
await transactionHandler.GiftPoints("21f51c05-e556-41f1-9cc9-0a314bb2ebcc", 200);
await transactionHandler.GiftPoints("d5af01f0-515a-4834-ab4e-a2f54aeaedbf", 15300);
await transactionHandler.GiftPoints("363a3f19-7fa9-4e34-851d-6e42ef92a285", 0);

app.MapGet("/balances", (WalletDb db) =>
{
    return db.Wallets.ToDictionary(wallet => wallet.Id, wallet => wallet.Balance);
});

app.MapPut("/transactions", async (Transaction transaction, TransactionDb transactionDb, WalletDb walletDb, ITransactionHandler transactionHandler) =>
{
    return await transactionHandler.ProcessTransaction(transaction);
});

app.MapGet("/transactions", (TransactionDb db) =>
{
    return db.Transactions.ToList();
});

app.MapPut("/gifts", async (Gift gift, ITransactionHandler transactionHandler) =>
{
    return await transactionHandler.GiftPoints(gift.ToWalletId, gift.Points);
});

app.Run();
