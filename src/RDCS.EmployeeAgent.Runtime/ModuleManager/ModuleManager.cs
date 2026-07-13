using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.EventBus.Events;
using RDCS.EmployeeAgent.Runtime.Workers;
using System.Collections.Concurrent;

namespace RDCS.EmployeeAgent.Runtime.ModuleManager;

public class ModuleManager : IModuleManager
{
    private readonly IAgentLogger _logger;
    private readonly IEventBus _eventBus;
    private readonly ConcurrentDictionary<string, ModuleInfo> _modules = new();

    public ModuleManager(IAgentLogger logger, IEventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task LoadModuleAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(LogCategory.Application, "Loading module {ModuleName}", moduleName);

        // Check if already loaded
        if (_modules.TryGetValue(moduleName, out var existingModule))
        {
            if (existingModule.State == ModuleState.Loaded || existingModule.State == ModuleState.Running)
            {
                _logger.LogWarning(LogCategory.Application, "Module {ModuleName} is already loaded", moduleName);
                return;
            }
        }

        // Check dependencies
        var dependencies = await GetModuleDependenciesAsync(moduleName, cancellationToken);
        if (!await CheckDependenciesAsync(moduleName, cancellationToken))
        {
            throw new InvalidOperationException($"Module {moduleName} dependencies are not satisfied");
        }

        // Create module info
        var moduleInfo = new ModuleInfo
        {
            Name = moduleName,
            Version = "1.0.0",
            State = ModuleState.Loaded,
            Dependencies = dependencies,
            Permissions = await GetModulePermissionsAsync(moduleName, cancellationToken),
            LoadedAtUtc = DateTime.UtcNow
        };

        _modules[moduleName] = moduleInfo;

        await _eventBus.PublishAsync(new ModuleLoaded(moduleName, DateTime.UtcNow), cancellationToken);

        _logger.LogInformation(LogCategory.Application, "Module {ModuleName} loaded successfully", moduleName);
    }

    public async Task UnloadModuleAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(LogCategory.Application, "Unloading module {ModuleName}", moduleName);

        if (!_modules.TryGetValue(moduleName, out var moduleInfo))
        {
            throw new ArgumentException($"Module {moduleName} not found");
        }

        if (moduleInfo.State == ModuleState.Running)
        {
            await DisableModuleAsync(moduleName, cancellationToken);
        }

        _modules.Remove(moduleName, out _);

        await _eventBus.PublishAsync(new ModuleUnloaded(moduleName, DateTime.UtcNow), cancellationToken);

        _logger.LogInformation(LogCategory.Application, "Module {ModuleName} unloaded successfully", moduleName);
    }

    public async Task EnableModuleAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(LogCategory.Application, "Enabling module {ModuleName}", moduleName);

        if (!_modules.TryGetValue(moduleName, out var moduleInfo))
        {
            throw new ArgumentException($"Module {moduleName} not found");
        }

        if (moduleInfo.State == ModuleState.Running)
        {
            _logger.LogWarning(LogCategory.Application, "Module {ModuleName} is already running", moduleName);
            return;
        }

        moduleInfo.State = ModuleState.Starting;

        // Simulate module startup
        await Task.Delay(100, cancellationToken);

        moduleInfo.State = ModuleState.Running;
        moduleInfo.LastStartedUtc = DateTime.UtcNow;

        await _eventBus.PublishAsync(new ModuleStarted(moduleName, DateTime.UtcNow), cancellationToken);

        _logger.LogInformation(LogCategory.Application, "Module {ModuleName} enabled successfully", moduleName);
    }

    public async Task DisableModuleAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(LogCategory.Application, "Disabling module {ModuleName}", moduleName);

        if (!_modules.TryGetValue(moduleName, out var moduleInfo))
        {
            throw new ArgumentException($"Module {moduleName} not found");
        }

        if (moduleInfo.State == ModuleState.Disabled)
        {
            _logger.LogWarning(LogCategory.Application, "Module {ModuleName} is already disabled", moduleName);
            return;
        }

        moduleInfo.State = ModuleState.Stopping;

        // Simulate module shutdown
        await Task.Delay(100, cancellationToken);

        moduleInfo.State = ModuleState.Disabled;

        await _eventBus.PublishAsync(new ModuleStopped(moduleName, DateTime.UtcNow), cancellationToken);

        _logger.LogInformation(LogCategory.Application, "Module {ModuleName} disabled successfully", moduleName);
    }

    public async Task RestartModuleAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(LogCategory.Application, "Restarting module {ModuleName}", moduleName);

        await DisableModuleAsync(moduleName, cancellationToken);
        await EnableModuleAsync(moduleName, cancellationToken);

        _logger.LogInformation(LogCategory.Application, "Module {ModuleName} restarted successfully", moduleName);
    }

    public async Task<ModuleState> GetModuleStateAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        if (_modules.TryGetValue(moduleName, out var moduleInfo))
        {
            return moduleInfo.State;
        }

        return ModuleState.Unloaded;
    }

    public async Task<WorkerHealth> GetModuleHealthAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        if (_modules.TryGetValue(moduleName, out var moduleInfo))
        {
            return moduleInfo.Health;
        }

        return new WorkerHealth { Status = HealthStatus.Unknown, Message = "Module not loaded" };
    }

    public async Task<string> GetModuleVersionAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        if (_modules.TryGetValue(moduleName, out var moduleInfo))
        {
            return moduleInfo.Version;
        }

        return "Unknown";
    }

    public async Task<List<string>> GetModuleDependenciesAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        // In a full implementation, this would load from module metadata
        // For now, return empty list
        return new List<string>();
    }

    public async Task<bool> CheckDependenciesAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        var dependencies = await GetModuleDependenciesAsync(moduleName, cancellationToken);

        foreach (var dependency in dependencies)
        {
            if (!_modules.TryGetValue(dependency, out var depModule) || 
                depModule.State != ModuleState.Running)
            {
                _logger.LogWarning(LogCategory.Application, "Module {ModuleName} dependency {Dependency} is not satisfied", moduleName, dependency);
                return false;
            }
        }

        return true;
    }

    public async Task<List<string>> GetModulePermissionsAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        // In a full implementation, this would load from module metadata
        // For now, return empty list
        return new List<string>();
    }

    public async Task<bool> HasPermissionAsync(string moduleName, string permission, CancellationToken cancellationToken = default)
    {
        if (_modules.TryGetValue(moduleName, out var moduleInfo))
        {
            return moduleInfo.Permissions.Contains(permission);
        }

        return false;
    }

    public async Task<List<ModuleInfo>> GetAllModulesAsync(CancellationToken cancellationToken = default)
    {
        return _modules.Values.ToList();
    }

    public async Task<ModuleInfo?> GetModuleInfoAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        if (_modules.TryGetValue(moduleName, out var moduleInfo))
        {
            return moduleInfo;
        }

        return null;
    }
}
