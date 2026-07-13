using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;

namespace RDCS.EmployeeAgent.Services.Modules;

public class ModuleRegistry
{
    private readonly List<IAgentModule> _modules = new();

    public void RegisterModule(IAgentModule module)
    {
        _modules.Add(module);
    }

    public IReadOnlyList<IAgentModule> GetAllModules()
    {
        return _modules.AsReadOnly();
    }

    public IAgentModule? GetModule(string name)
    {
        return _modules.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
