using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using Microsoft.Extensions.Hosting;

namespace RDCS.EmployeeAgent.Services.Modules;

public class ModuleHost : IHostedService, IModuleHost
{
    private readonly ModuleRegistry _registry;
    private readonly IAgentLogger _logger;
    private readonly CancellationTokenSource _shutdownCts = new();

    public ModuleHost(ModuleRegistry registry, IAgentLogger logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public async Task StartAllModulesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(Core.Enums.LogCategory.Application, "Starting all modules");

        var modules = _registry.GetAllModules();
        foreach (var module in modules)
        {
            try
            {
                _logger.LogInformation(Core.Enums.LogCategory.Application, "Starting module {ModuleName}", module.Name);
                await module.StartAsync(cancellationToken);
                _logger.LogInformation(Core.Enums.LogCategory.Application, "Module {ModuleName} started successfully", module.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(Core.Enums.LogCategory.Application, "Failed to start module {ModuleName}", ex, module.Name);
            }
        }
    }

    public async Task StopAllModulesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(Core.Enums.LogCategory.Application, "Stopping all modules");

        var modules = _registry.GetAllModules().Reverse().ToList();
        foreach (var module in modules)
        {
            try
            {
                _logger.LogInformation(Core.Enums.LogCategory.Application, "Stopping module {ModuleName}", module.Name);
                await module.StopAsync(cancellationToken);
                _logger.LogInformation(Core.Enums.LogCategory.Application, "Module {ModuleName} stopped successfully", module.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(Core.Enums.LogCategory.Application, "Failed to stop module {ModuleName}", ex, module.Name);
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await StartAllModulesAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopAllModulesAsync(cancellationToken);
    }
}
