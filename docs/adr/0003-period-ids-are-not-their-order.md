# A period's id is not its place in the curriculum order

Six literary periods already had numbered addresses and stored content before the full
curriculum list was added. In that numbering Realizam is 4 and Romantizam is 5, although
Romantizam comes first in the curriculum. Renumbering would change existing addresses and
move the meaning of stored `PeriodId` values.

The twelve periods are reference data inserted by the `Periods` migration. Each keeps a
stable `Id`; `SortOrder` states its place in the curriculum. The six existing ids keep their
meaning, and the six new periods receive ids 7 through 12. Period Content has a required
foreign key to this list and a unique index on `PeriodId`.

## Considered options

- **Renumber the stored ids.** This would make id and order agree, but would require a data
  migration and change existing links solely to make the numbers look tidy.
- **Keep the list in the client.** It would preserve the existing cards but leave the database
  unable to enforce that content belongs to a real period.
- **Let the startup seeder own the list.** The list would then depend on an optional runtime
  setting, while the foreign key and the client need it wherever migrations have run.

## Consequences

- The server returns the periods, and search results across periods, ordered by `SortOrder`.
  Nothing orders them by `Id`.
- A change to the period list requires a migration.
- A database that holds content for a period outside the list fails the migration instead of
  silently deleting or reassigning that content.
