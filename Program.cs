using CRSmallWorldBackend;
using Microsoft.EntityFrameworkCore;
using NSwag.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<WalletDb>(options => options.UseInMemoryDatabase("Wallets"));
builder.Services.AddDbContext<PointsBalanceDb>(options => options.UseInMemoryDatabase("PointsBalances"));
builder.Services.AddDbContext<TransactionDb>(options => options.UseInMemoryDatabase("Transactions"));
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

void InitializeWallet(string walletId, long points)
{
    var wallet = new Wallet
    {
        Id = walletId,
        Balance = points
    };
    var transaction = new Transaction
    {
        TimeStamp = DateTime.Now,
        Amount = points,
        DebitWalletId = walletId
    };
    var scope = app.Services.CreateScope();
    var walletDb = scope.ServiceProvider.GetRequiredService<WalletDb>();
    var transactionDb = scope.ServiceProvider.GetRequiredService<TransactionDb>();
    walletDb.Wallets.Add(wallet);
    transactionDb.Transactions.Add(transaction);
    walletDb.SaveChanges();
}

InitializeWallet("8c18b5bf-0171-4918-a611-bde754382f7a", 2500);
InitializeWallet("21f51c05-e556-41f1-9cc9-0a314bb2ebcc", 200);
InitializeWallet("d5af01f0-515a-4834-ab4e-a2f54aeaedbf", 15300);
InitializeWallet("363a3f19-7fa9-4e34-851d-6e42ef92a285", 0);

app.MapGet("/balances", (WalletDb db) =>
{
    return db.Wallets.ToDictionary(wallet => wallet.Id, wallet => wallet.Balance);
});

app.Run();
