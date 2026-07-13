using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;

namespace RDCS.EmployeeAgent.Services.Modules;

public class ModuleManager
{
    private readonly ModuleRegistry _registry;

    public ModuleManager(ModuleRegistry registry)
    {
        _registry = registry;
    }

    public Dictionary<string, ModuleState> GetModuleStates()
    {
        return _registry.GetAllModules()
            .ToDictionary(m => m.Name, m => m.State);
    }

    public ModuleState GetModuleState(string moduleName)
    {
        var module = _registry.GetModule(moduleName);
        return module?.State ?? ModuleState.Stopped;
    }
}
