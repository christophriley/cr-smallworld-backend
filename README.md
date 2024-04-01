### Installation
* You will need the [DotNet SDK](https://dotnet.microsoft.com/en-us/download) version 8
* From the main repository folder, run `dotnet restore CRSmallWorldBackend.csproj` to download dependencies
* Then run `dotnet run CRSmallWorldBackend.csproj` to run the API service in development mode, on the default port 5078
* In dev mode, there is Swagger API Documentation you can find by pointing your browser to `localhost:5078/swagger`

### Endpoints
#### General
At the moment, only PUT and GET requests are mapped. GET requests require no parameters, and PUT commands require data to be passed in the body of the request, not as URL parameters.

#### GET /wallets
Returns a list of wallet IDs together with their current respective balance.

#### PUT /wallets
Takes a string as the body of the request, and creates a new wallet with that string as the wallet ID. New wallets have 0 balance.

#### GET /transactions
Returns a list of all transactions, ordered by transaction timestamp with the most recent transaction first

#### PUT /transactions
Takes: A JSON formatted request body with the following parameters
* TimeStamp (string) - a timestamp in the format "2021-10-01T11:00:00Z"
* Points (integer) - the number of points to transact
* CreditWalletId - the id of the wallet from which the points should be deducted
* DebitWalletId - the id of the wallet to which the points should be added

Returns: A list of "Point Deductions" that consist of a wallet id and a point value. A point deduction represents a previous transaction containing points that were "consumed" to cover this new transacction. Wallet Ids in point deductions may be duplicated to indicate that more than one past transaction from a given wallet needed to be consumed in order to cover the new transaction. A point deduction does not necessarily indicate that a transaction was fully consumed...it can be partially consumed and the balance used for future transactions.

Errors:
* 404 - Either the Credit or Debit wallet ids were not found in the database. The error message will indicate which is the case.
* 400 - Either malformed input (missing wallet Ids, nonpositive point value), attemp to transfer from a wallet into the same wallet, or insufficient funds in the source wallet to cover the transaction.
* 500 - Wallet balance indicates there should be enough points to cover the transaction, but the server could not find enough unspent points in individual transactions to cover the point value.

#### PUT /gifts
Create a one-sided transaction representing new points entering the economy, presumably as rewards. 

Takes: A JSON formatted request body with the following parameters
* Points (integer) - the number of points to transact
* ToWalletId (string) the ID of the wallet to award points to

#### PUT /spends
Create a one-sided transaction representing spending points on rewards, removing them from the economy.

Takes: A JSON formatted request body with the following parameters
* Points (integer) - the number of points to transact
* FromWalletId (string) the ID of the wallet to remove points from

#### GET /test
Not an endpoint I would ever deploy to production. This is purely a development convenience to test the system's compliance with the exercise specification, and left it in for your convenience as well.

This will initialize wallets with gift values, then run the transactions listed in the spec. End balances will  match the spec values iff this endpoint is used, then a separate spend endpoint is used to spend the 5000 points.

### Code Organization
The entry point is `Program.cs`, but most of this code is framework boilerplate or mapping endpoints to code in more interesting places.

The Models folder contains the code-first definitions of database objects and DTOs. In a larger project I would typically distinguish between actual database models and non-database models with a file naming convention or a separater folder. The database models also have a *Db.css file that defines the database context object used by Entity Framework.

All of the really interesting code is in TransactionHandler.cs. The transaction handler gets injected into the endpoint functions where it's needed, and contains nearly all of the complexity in this project.

### Notes and Oddities
* Since transactions are not allowed to result in negative balances, this can't be a zero-sum system - the points have to come from somewhere. I assume that "Points can be earned by taking certain actions or meeting certain objectives within the system..." means that points enter the economy from some other mechanism. I represent this with Transactions whose `CreditWalletId` is null.
* Similarly, I represent "spend" transactions that remove points from the economy with Transactions whos `DebitWalletId` is null.
* It's possible to insert a transaction at a previous point in time where the wallet balance would not have had enough at that time, but "future" transactions cover it. In this case we're kind of borrowing from "future" transactions, but I assume this is okay. Otherwise we have a tangled web of race conditions and retroactive point adjustments to deal with and it's unclear how untangling it would benefit the user in any meaningful way.
* My use of `TransactionScope.Complete()` to commit changes to the transaction and wallet tables in a single database transaction doesn't really work - i.e. the changes are committed even without calling Complete(), which should not be the case.  I'm not sure if this is a limitation of the in-memory database or if I'm simply using the TransactionScope incorrectly. Hopefully I get points (ha!) for trying and my solemn promise that in a real world scenario I would take the time to resolve this issue.
* `Program.cs` is kind of messy. In the real world, I would refactor services and initialization etc. into separate files and functions to keep it more organized. But this isn't really the crux of the exercise.
* The `/test` endpoint is the only automated "testing" I implemented, and it is not idempotent.
* I think the specification is wrong. It expects only 100 points from the Spend to come from wallet "8c18b5bf-0171-4918-a611-bde754382f7a", but there are in fact 500 points of transactions from that wallet in two different transactions that occur before the remainder of the spend would consume the 10k transfer from wallet "d5af01f0-515a-4834-ab4e-a2f54aeaedbf".
* I initialized the wallet balances such that the ending balances match what is expected in the spec, taking into account the above discrepancy.


### Things I'd do with More Time
* Add automated testing
* Add more automated testing
* Add paging to the transactions and wallets list endpoints
* More error checking
* Fill out the remainder of the REST endpoints (like DELETE)