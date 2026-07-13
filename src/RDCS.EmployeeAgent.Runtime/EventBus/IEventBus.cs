namespace RDCS.EmployeeAgent.Runtime.EventBus;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
    Task PublishAsync<TEvent>(TEvent @event, EventPriority priority, CancellationToken cancellationToken = default) where TEvent : class;
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler, EventPriority priority) where TEvent : class;
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
}
