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
* Add unit tests
* Add paging to the transactions and wallets list endpoints
* More error checking
* Fill out the remainder of the REST endpoints (like DELETE)