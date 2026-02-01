# Book Repository Integration Tests

## Setup

These integration tests require that the PostgreSQL test database schema is up-to-date with all migrations.

If you encounter errors like "relation 'Books' does not exist", you need to apply the latest migrations to your test database.

### Applying Migrations to Test Database

Run the following command from the solution root to apply migrations to your test database:

```bash
dotnet ef database update --project Persistence --startup-project Web --connection "YOUR_TEST_CONNECTION_STRING"
```

Replace `YOUR_TEST_CONNECTION_STRING` with the connection string from your test configuration (NeonConnectionUnitTests).

Alternatively, you can modify `IntegrationTestWebAppFactory.cs` to use `db.Database.Migrate()` instead of `db.Database.EnsureCreated()` for automatic migration application during test runs.

## Test Coverage

The `BookRepositoryTests` class provides comprehensive integration tests for the `BookRepository`:

### GetByIdAsync Tests
- ✅ Returns book when it exists
- ✅ Returns null when book doesn't exist  
- ✅ Throws exception when ID is default (Guid.Empty)
- ✅ Includes reading dates when book has them

### Add Tests
- ✅ Adds book successfully
- ✅ Throws exception when adding book with same ID twice

### Update Tests
- ✅ Updates book properties successfully

### Remove Tests
- ✅ Removes book successfully

### Count Tests
- ✅ Returns zero when no books exist
- ✅ Returns correct count when books exist

### ListAsync Tests
- ✅ Returns all books
- ✅ Returns empty list when no books exist
- ✅ Includes reading dates in results

### GetByIsbnAsync Tests
- ✅ Returns book by ISBN
- ✅ Normalizes ISBN (removes dashes and spaces)
- ✅ Returns null when ISBN doesn't exist
- ✅ Throws ArgumentException for null, empty, or whitespace ISBN
- ✅ Handles ISBN-10 format
- ✅ Handles ISBN with spaces
- ✅ Case-insensitive for ISBN-10 with 'X' check digit
