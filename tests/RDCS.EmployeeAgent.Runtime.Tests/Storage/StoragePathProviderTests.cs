using RDCS.EmployeeAgent.Runtime.Storage;
using Xunit;

namespace RDCS.EmployeeAgent.Runtime.Tests.Storage;

public class StoragePathProviderTests
{
    private readonly StorageSettings _settings;
    private readonly StoragePathProvider _pathProvider;

    public StoragePathProviderTests()
    {
        _settings = new StorageSettings { RootPath = @"C:\Test\RDCS Agent" };
        _pathProvider = new StoragePathProvider(Microsoft.Extensions.Options.Options.Create(_settings));
    }

    [Fact]
    public void GetRootPath_ShouldReturnConfiguredPath()
    {
        // Act
        var result = _pathProvider.GetRootPath();

        // Assert
        Assert.Equal(@"C:\Test\RDCS Agent", result);
    }

    [Fact]
    public void GetScreenshotFolder_ShouldReturnCorrectPath()
    {
        // Act
        var result = _pathProvider.GetScreenshotFolder();

        // Assert
        Assert.Equal(@"C:\Test\RDCS Agent\Screenshots", result);
    }

    [Fact]
    public void GetEmployeeScreenshotFolder_ShouldGenerateDateHierarchy()
    {
        // Arrange
        var employeeId = "EMP001";
        var date = new DateTime(2026, 8, 10);

        // Act
        var result = _pathProvider.GetEmployeeScreenshotFolder(employeeId, date);

        // Assert
        Assert.Equal(@"C:\Test\RDCS Agent\Screenshots\2026\08\10\EMP001", result);
    }

    [Fact]
    public void GetQueueFolder_ShouldReturnCorrectPath()
    {
        // Act
        var result = _pathProvider.GetQueueFolder();

        // Assert
        Assert.Equal(@"C:\Test\RDCS Agent\Queue", result);
    }

    [Fact]
    public void GetQueuePendingFolder_ShouldReturnCorrectPath()
    {
        // Act
        var result = _pathProvider.GetQueuePendingFolder();

        // Assert
        Assert.Equal(@"C:\Test\RDCS Agent\Queue\Pending", result);
    }

    [Fact]
    public void GetDatabasePath_ShouldReturnCorrectPath()
    {
        // Act
        var result = _pathProvider.GetDatabasePath();

        // Assert
        Assert.Equal(@"C:\Test\RDCS Agent\Database\rdcs_agent.db", result);
    }

    [Fact]
    public void GetLogFolder_ShouldReturnCorrectPath()
    {
        // Act
        var result = _pathProvider.GetLogFolder();

        // Assert
        Assert.Equal(@"C:\Test\RDCS Agent\Logs", result);
    }

    [Fact]
    public void GetTempFolder_ShouldReturnCorrectPath()
    {
        // Act
        var result = _pathProvider.GetTempFolder();

        // Assert
        Assert.Equal(@"C:\Test\RDCS Agent\Temp", result);
    }

    [Fact]
    public void GetCacheFolder_ShouldReturnCorrectPath()
    {
        // Act
        var result = _pathProvider.GetCacheFolder();

        // Assert
        Assert.Equal(@"C:\Test\RDCS Agent\Cache", result);
    }

    [Fact]
    public void GetConfigFolder_ShouldReturnCorrectPath()
    {
        // Act
        var result = _pathProvider.GetConfigFolder();

        // Assert
        Assert.Equal(@"C:\Test\RDCS Agent\Config", result);
    }

    [Fact]
    public void GetDiagnosticsFolder_ShouldReturnCorrectPath()
    {
        // Act
        var result = _pathProvider.GetDiagnosticsFolder();

        // Assert
        Assert.Equal(@"C:\Test\RDCS Agent\Diagnostics", result);
    }

    [Fact]
    public void GetBackupFolder_ShouldReturnCorrectPath()
    {
        // Act
        var result = _pathProvider.GetBackupFolder();

        // Assert
        Assert.Equal(@"C:\Test\RDCS Agent\Backups", result);
    }

    [Fact]
    public void CombinePath_ShouldSafelyCombineSegments()
    {
        // Act
        var result = _pathProvider.CombinePath(@"C:\Test", "Folder", "Subfolder");

        // Assert
        Assert.Equal(@"C:\Test\Folder\Subfolder", result);
    }
}
