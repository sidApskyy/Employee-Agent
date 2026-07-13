using RDCS.EmployeeAgent.Runtime.Workers;

namespace RDCS.EmployeeAgent.Runtime.ModuleManager;

public interface IModuleManager
{
    // Lifecycle
    Task LoadModuleAsync(string moduleName, CancellationToken cancellationToken = default);
    Task UnloadModuleAsync(string moduleName, CancellationToken cancellationToken = default);
    Task EnableModuleAsync(string moduleName, CancellationToken cancellationToken = default);
    Task DisableModuleAsync(string moduleName, CancellationToken cancellationToken = default);
    Task RestartModuleAsync(string moduleName, CancellationToken cancellationToken = default);
    
    // State
    Task<ModuleState> GetModuleStateAsync(string moduleName, CancellationToken cancellationToken = default);
    Task<WorkerHealth> GetModuleHealthAsync(string moduleName, CancellationToken cancellationToken = default);
    Task<string> GetModuleVersionAsync(string moduleName, CancellationToken cancellationToken = default);
    
    // Dependencies
    Task<List<string>> GetModuleDependenciesAsync(string moduleName, CancellationToken cancellationToken = default);
    Task<bool> CheckDependenciesAsync(string moduleName, CancellationToken cancellationToken = default);
    
    // Permissions
    Task<List<string>> GetModulePermissionsAsync(string moduleName, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(string moduleName, string permission, CancellationToken cancellationToken = default);
    
    // Discovery
    Task<List<ModuleInfo>> GetAllModulesAsync(CancellationToken cancellationToken = default);
    Task<ModuleInfo?> GetModuleInfoAsync(string moduleName, CancellationToken cancellationToken = default);
}

public class ModuleInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public ModuleState State { get; set; }
    public WorkerHealth Health { get; set; } = new WorkerHealth();
    public List<string> Dependencies { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public DateTime? LoadedAtUtc { get; set; }
    public DateTime? LastStartedUtc { get; set; }
}
