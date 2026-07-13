using System.Collections.Concurrent;

namespace RDCS.EmployeeAgent.Runtime.EventBus;

public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<EventSubscription>> _subscriptions = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        await PublishAsync(@event, EventPriority.Normal, cancellationToken);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, EventPriority priority, CancellationToken cancellationToken = default) where TEvent : class
    {
        var eventType = typeof(TEvent);

        if (!_subscriptions.TryGetValue(eventType, out var subscriptions))
        {
            return;
        }

        // Dispatch to all subscribers ordered by their registered priority (highest first).
        // The publish-side priority parameter is reserved for future priority-based filtering.
        List<EventSubscription> snapshot;
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            snapshot = subscriptions.OrderByDescending(s => (int)s.Priority).ToList();
        }
        finally
        {
            _semaphore.Release();
        }

        var tasks = snapshot.Select(async subscription =>
        {
            try
            {
                await subscription.Handler(@event);
            }
            catch (Exception)
            {
                // Swallow handler exceptions so one bad subscriber never breaks others.
            }
        });

        await Task.WhenAll(tasks);
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        return Subscribe(handler, EventPriority.Normal);
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler, EventPriority priority) where TEvent : class
    {
        var eventType = typeof(TEvent);
        
        _semaphore.Wait();
        try
        {
            if (!_subscriptions.ContainsKey(eventType))
            {
                _subscriptions[eventType] = new List<EventSubscription>();
            }

            var subscription = new EventSubscription(
                typeof(TEvent),
                @event => handler((TEvent)@event),
                priority
            );

            _subscriptions[eventType].Add(subscription);

            return new EventSubscriptionDisposable(this, eventType, subscription);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        var eventType = typeof(TEvent);
        
        _semaphore.Wait();
        try
        {
            if (_subscriptions.TryGetValue(eventType, out var subscriptions))
            {
                subscriptions.RemoveAll(s => s.Handler == handler);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void RemoveSubscription(Type eventType, EventSubscription subscription)
    {
        _semaphore.Wait();
        try
        {
            if (_subscriptions.TryGetValue(eventType, out var subscriptions))
            {
                subscriptions.Remove(subscription);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private class EventSubscriptionDisposable : IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly Type _eventType;
        private readonly EventSubscription _subscription;
        private bool _disposed;

        public EventSubscriptionDisposable(EventBus eventBus, Type eventType, EventSubscription subscription)
        {
            _eventBus = eventBus;
            _eventType = eventType;
            _subscription = subscription;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _eventBus.RemoveSubscription(_eventType, _subscription);
                _disposed = true;
            }
        }
    }
}
