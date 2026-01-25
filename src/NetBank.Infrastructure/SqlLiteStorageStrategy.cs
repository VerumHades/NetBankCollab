using NetBank.Persistence;

namespace NetBank.Infrastructure;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

public class SqliteStorageStrategy : IStorageStrategy
{
    private readonly string _connectionString;

    public SqliteStorageStrategy(string dbPath = "bank_data.db")
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Accounts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Balance INTEGER NOT NULL DEFAULT 0
            );";
        command.ExecuteNonQuery();
    }

    public async Task<IReadOnlyList<AccountIdentifier>> CreateAccounts(int count)
    {
        var created = new List<AccountIdentifier>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        for (int i = 0; i < count; i++)
        {
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Accounts (Balance) VALUES (0); SELECT last_insert_rowid();";
            var id = (long)await command.ExecuteScalarAsync();
            created.Add(new AccountIdentifier((int)id));
        }
        await transaction.CommitAsync();
        return created;
    }

    public async Task<IReadOnlyList<AccountIdentifier>> RemoveAccounts(IEnumerable<AccountIdentifier> accounts)
    {
        var idsToRemove = accounts.Select(a => a.Number).ToList();
        if (!idsToRemove.Any()) return new List<AccountIdentifier>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = new List<AccountIdentifier>();
        var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = $"SELECT Id FROM Accounts WHERE Id IN ({string.Join(",", idsToRemove)})";
        using (var reader = await checkCmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                existing.Add(new AccountIdentifier(reader.GetInt32(0)));
        }

        var deleteCmd = connection.CreateCommand();
        deleteCmd.CommandText = $"DELETE FROM Accounts WHERE Id IN ({string.Join(",", idsToRemove)})";
        await deleteCmd.ExecuteNonQueryAsync();

        return existing;
    }

    public async Task<IReadOnlyList<AccountIdentifier>> UpdateAll(IEnumerable<Account> accounts)
    {
        var updated = new List<AccountIdentifier>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        foreach (var account in accounts)
        {
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Accounts SET Balance = $val WHERE Id = $id";
            command.Parameters.AddWithValue("$val", account.Amount.Value); // Value is long
            command.Parameters.AddWithValue("$id", account.Identifier.Number);
            
            if (await command.ExecuteNonQueryAsync() > 0)
                updated.Add(account.Identifier);
        }
        await transaction.CommitAsync();
        return updated;
    }

    public async Task<IReadOnlyList<Account>> GetAll(IEnumerable<AccountIdentifier> accounts)
    {
        var ids = accounts.Select(a => a.Number).ToList();
        if (!ids.Any()) return new List<Account>();

        var results = new List<Account>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = $"SELECT Id, Balance FROM Accounts WHERE Id IN ({string.Join(",", ids)})";
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = new AccountIdentifier(reader.GetInt32(0));
            var balance = new Amount(reader.GetInt64(1)); // Read as long
            results.Add(new Account(id, balance));
        }
        return results;
    }

    public async Task<Amount> BankTotal()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT SUM(Balance) FROM Accounts";
        var result = await command.ExecuteScalarAsync();
        
        return result == System.DBNull.Value ? new Amount(0) : new Amount((long)result);
    }

    public async Task<int> BankNumberOfClients()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Accounts";
        return (int)(long)await command.ExecuteScalarAsync();
    }
}