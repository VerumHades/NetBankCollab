using System.Data;
using Microsoft.Data.Sqlite;

namespace NetBank.Infrastructure;

public class SqliteStorageStrategy : IStorageStrategy
{
    private readonly string _connectionString;
    
    private const int MinId = 10000;
    private const int MaxId = 99999;
    
    private int _lastIdCursor = MinId - 1;

    public SqliteStorageStrategy(string dbPath = "bank_data.db")
    {
        _connectionString = $"Data Source={dbPath};Pooling=True;";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS Accounts (
                Id INTEGER PRIMARY KEY,
                Balance INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT range_check CHECK (Id >= {MinId} AND Id <= {MaxId})
            );
            CREATE INDEX IF NOT EXISTS idx_accounts_id ON Accounts(Id);";
        
        command.ExecuteNonQuery();
    }

    public async Task<IReadOnlyList<AccountIdentifier>> CreateAccounts(int count)
    {
        if (count <= 0) return Array.Empty<AccountIdentifier>();

        var created = new List<AccountIdentifier>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            var availableIds = await FindAvailableGaps(connection, transaction, count);
            
            using var insertCmd = connection.CreateCommand();
            insertCmd.Transaction = transaction;
            insertCmd.CommandText = "INSERT INTO Accounts (Id, Balance) VALUES ($id, 0)";
            var idParam = insertCmd.Parameters.Add("$id", SqliteType.Integer);

            foreach (var id in availableIds)
            {
                idParam.Value = id;
                await insertCmd.ExecuteNonQueryAsync();
                created.Add(new AccountIdentifier(id));
                
                _lastIdCursor = id;
            }

            await transaction.CommitAsync();
            return created;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<List<int>> FindAvailableGaps(SqliteConnection conn, SqliteTransaction tx, int count)
    {
        var found = new List<int>();

        // Query: Find up to 'count' IDs that don't exist in the Accounts table,
        // starting from the cursor and wrapping around to the start if necessary.
        string query = $@"
            WITH RECURSIVE GapSearch(n) AS (
                VALUES($cursor + 1)
                UNION ALL
                SELECT n + 1 FROM GapSearch WHERE n < {MaxId}
            )
            SELECT n FROM GapSearch
            LEFT JOIN Accounts ON Accounts.Id = n
            WHERE Accounts.Id IS NULL
            LIMIT $limit;";

        async Task Search(int cursor, int limit)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = query;
            cmd.Parameters.AddWithValue("$cursor", cursor);
            cmd.Parameters.AddWithValue("$limit", limit);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                found.Add(reader.GetInt32(0));
            }
        }

        // Phase 1: Search from current cursor to MaxId
        await Search(_lastIdCursor, count);

        // Phase 2: If we didn't find enough, wrap around and search from MinId-1
        if (found.Count < count)
        {
            int remaining = count - found.Count;
            await Search(MinId - 1, remaining);
        }

        return found;
    }

    public async Task<IReadOnlyList<AccountIdentifier>> RemoveAccounts(IEnumerable<AccountIdentifier> accounts)
    {
        var ids = accounts.Select(a => a.Number).ToList();
        if (ids.Count == 0) return Array.Empty<AccountIdentifier>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        
        // We delete directly and return the list of IDs we were asked to delete
        // (The interface suggests returning confirmed deletions)
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = $"DELETE FROM Accounts WHERE Id IN ({string.Join(",", ids)})";
        
        await cmd.ExecuteNonQueryAsync();
        await transaction.CommitAsync();

        return accounts.ToList();
    }

    public async Task<IReadOnlyList<AccountIdentifier>> UpdateAll(IEnumerable<Account> accounts)
    {
        var accountList = accounts.ToList();
        if (accountList.Count == 0) return Array.Empty<AccountIdentifier>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "UPDATE Accounts SET Balance = $bal WHERE Id = $id";
        var balParam = cmd.Parameters.Add("$bal", SqliteType.Integer);
        var idParam = cmd.Parameters.Add("$id", SqliteType.Integer);

        foreach (var acc in accountList)
        {
            balParam.Value = acc.Amount.Value;
            idParam.Value = acc.Identifier.Number;
            await cmd.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return accountList.Select(a => a.Identifier).ToList();
    }

    public async Task<IReadOnlyList<Account>> GetAll(IEnumerable<AccountIdentifier> accounts)
    {
        var ids = accounts.Select(a => a.Number).ToList();
        if (ids.Count == 0) return Array.Empty<Account>();

        var results = new List<Account>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT Id, Balance FROM Accounts WHERE Id IN ({string.Join(",", ids)})";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new Account(
                new AccountIdentifier(reader.GetInt32(0)), 
                new Amount(reader.GetInt64(1))));
        }
        return results;
    }

    public async Task<Amount> BankTotal()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT SUM(Balance) FROM Accounts";
        var res = await cmd.ExecuteScalarAsync();
        return res == DBNull.Value ? new Amount(0) : new Amount((long)res);
    }

    public async Task<int> BankNumberOfClients()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Accounts";
        return (int)(long)await cmd.ExecuteScalarAsync();
    }
}