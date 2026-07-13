using RDCS.EmployeeAgent.Persistence.SQLite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace RDCS.EmployeeAgent.Tests.Persistence;

public class SQLiteConnectionFactoryTests
{
    private readonly string _testDatabasePath;

    public SQLiteConnectionFactoryTests()
    {
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"RDCS_Test_{Guid.NewGuid()}.db");
    }

    [Fact]
    public void CreateConnection_CreatesDatabase_WhenNotExists()
    {
        // Arrange
        var factory = new SQLiteConnectionFactory(_testDatabasePath);

        // Act
        using var connection = factory.CreateConnection();

        // Assert
        Assert.True(File.Exists(_testDatabasePath));
    }

    [Fact]
    public void CreateConnection_ReturnsOpenConnection()
    {
        // Arrange
        var factory = new SQLiteConnectionFactory(_testDatabasePath);

        // Act
        using var connection = factory.CreateConnection();

        // Assert
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    [Fact]
    public void CreateConnection_EnablesWALMode()
    {
        // Arrange
        var factory = new SQLiteConnectionFactory(_testDatabasePath);

        // Act
        using var connection = factory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";
        var journalMode = command.ExecuteScalar();

        // Assert
        Assert.Equal("wal", journalMode?.ToString());
    }

    [Fact]
    public void CreateConnection_EnablesForeignKeys()
    {
        // Arrange
        var factory = new SQLiteConnectionFactory(_testDatabasePath);

        // Act
        using var connection = factory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys;";
        var foreignKeys = command.ExecuteScalar();

        // Assert
        Assert.Equal(1, Convert.ToInt32(foreignKeys));
    }

    [Fact]
    public void CreateConnection_CreatesDirectory_WhenNotExists()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), $"RDCS_Test_Dir_{Guid.NewGuid()}");
        var testDbPath = Path.Combine(testDir, "test.db");
        var factory = new SQLiteConnectionFactory(testDbPath);

        // Act
        using var connection = factory.CreateConnection();

        // Assert
        Assert.True(Directory.Exists(testDir));
        Assert.True(File.Exists(testDbPath));

        // Cleanup - close connection first
        connection.Close();
        connection.Dispose();
        Thread.Sleep(100); // Give SQLite time to release file lock
        if (Directory.Exists(testDir))
        {
            try
            {
                Directory.Delete(testDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_CommitsTransaction_WhenActionSucceeds()
    {
        // Arrange
        var factory = new SQLiteConnectionFactory(_testDatabasePath);
        using var connection = factory.CreateConnection();
        
        // Create test table
        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = "CREATE TABLE IF NOT EXISTS Test (Id INTEGER PRIMARY KEY, Value TEXT);";
        createCommand.ExecuteNonQuery();

        // Act
        var result = await factory.ExecuteInTransactionAsync(async (conn, transaction) =>
        {
            using var command = conn.CreateCommand();
            command.CommandText = "INSERT INTO Test (Value) VALUES ('TestValue');";
            command.Transaction = transaction;
            await command.ExecuteNonQueryAsync();
            return 42;
        });

        // Assert
        Assert.Equal(42, result);

        // Verify data was committed
        using var verifyCommand = connection.CreateCommand();
        verifyCommand.CommandText = "SELECT COUNT(*) FROM Test;";
        var count = Convert.ToInt32(verifyCommand.ExecuteScalar());
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_RollsBackTransaction_WhenActionFails()
    {
        // Arrange
        var factory = new SQLiteConnectionFactory(_testDatabasePath);
        using var connection = factory.CreateConnection();
        
        // Create test table
        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = "CREATE TABLE IF NOT EXISTS Test (Id INTEGER PRIMARY KEY, Value TEXT);";
        createCommand.ExecuteNonQuery();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await factory.ExecuteInTransactionAsync(async (conn, transaction) =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "INSERT INTO Test (Value) VALUES ('TestValue');";
                command.Transaction = transaction;
                await command.ExecuteNonQueryAsync();
                throw new Exception("Test exception");
            });
        });

        // Verify data was rolled back
        using var verifyCommand = connection.CreateCommand();
        verifyCommand.CommandText = "SELECT COUNT(*) FROM Test;";
        var count = Convert.ToInt32(verifyCommand.ExecuteScalar());
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_CommitsTransaction_WhenVoidActionSucceeds()
    {
        // Arrange
        var factory = new SQLiteConnectionFactory(_testDatabasePath);
        using var connection = factory.CreateConnection();
        
        // Create test table
        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = "CREATE TABLE IF NOT EXISTS Test (Id INTEGER PRIMARY KEY, Value TEXT);";
        createCommand.ExecuteNonQuery();

        // Act
        await factory.ExecuteInTransactionAsync(async (conn, transaction) =>
        {
            using var command = conn.CreateCommand();
            command.CommandText = "INSERT INTO Test (Value) VALUES ('TestValue');";
            command.Transaction = transaction;
            await command.ExecuteNonQueryAsync();
        });

        // Assert
        using var verifyCommand = connection.CreateCommand();
        verifyCommand.CommandText = "SELECT COUNT(*) FROM Test;";
        var count = Convert.ToInt32(verifyCommand.ExecuteScalar());
        Assert.Equal(1, count);
    }

    private void Dispose()
    {
        // Cleanup test database
        if (File.Exists(_testDatabasePath))
        {
            try
            {
                File.Delete(_testDatabasePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
