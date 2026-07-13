using Microsoft.Data.Sqlite;
using Dapper;

namespace RDCS.EmployeeAgent.Persistence.SQLite;

public class SQLiteConnectionFactory
{
    private readonly string _databasePath;

    public SQLiteConnectionFactory(string databasePath)
    {
        _databasePath = databasePath;
    }

    public SqliteConnection CreateConnection()
    {
        // Ensure directory exists before opening database
        var databaseDirectory = System.IO.Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(databaseDirectory) && !System.IO.Directory.Exists(databaseDirectory))
        {
            System.IO.Directory.CreateDirectory(databaseDirectory);
        }

        var connectionString = $"Data Source={_databasePath};Mode=ReadWriteCreate;";
        var connection = new SqliteConnection(connectionString);
        connection.Open();

        // Enable WAL mode for better concurrency
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        command.ExecuteNonQuery();

        // Enable foreign keys
        using var fkCommand = connection.CreateCommand();
        fkCommand.CommandText = "PRAGMA foreign_keys = ON;";
        fkCommand.ExecuteNonQuery();

        return connection;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<SqliteConnection, SqliteTransaction, Task<T>> action, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var result = await action(connection, transaction);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<SqliteConnection, SqliteTransaction, Task> action, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            await action(connection, transaction);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
