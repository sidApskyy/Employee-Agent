namespace RDCS.EmployeeAgent.Runtime.EventBus;

public class EventSubscription
{
    public Type EventType { get; }
    public Func<object, Task> Handler { get; }
    public EventPriority Priority { get; }
    public DateTime SubscribedAtUtc { get; }

    public EventSubscription(Type eventType, Func<object, Task> handler, EventPriority priority)
    {
        EventType = eventType;
        Handler = handler;
        Priority = priority;
        SubscribedAtUtc = DateTime.UtcNow;
    }
}
