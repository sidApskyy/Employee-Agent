namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record ConfigurationChanged(
    string ConfigVersion,
    DateTime Timestamp
);
