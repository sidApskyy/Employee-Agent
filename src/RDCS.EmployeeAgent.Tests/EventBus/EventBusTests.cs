using RDCS.EmployeeAgent.Runtime.EventBus;
using Xunit;
using EventBusClass = RDCS.EmployeeAgent.Runtime.EventBus.EventBus;

namespace RDCS.EmployeeAgent.Tests.EventBus;

public class EventBusTests
{
    private readonly EventBusClass _eventBus;

    public EventBusTests()
    {
        _eventBus = new EventBusClass();
    }

    [Fact]
    public async Task PublishAsync_CallsSubscribedHandler_WhenEventPublished()
    {
        // Arrange
        var testEvent = new TestEvent { Message = "Test" };
        var handlerCalled = false;
        string? receivedMessage = null;

        _eventBus.Subscribe<TestEvent>(async e =>
        {
            handlerCalled = true;
            receivedMessage = e.Message;
            await Task.CompletedTask;
        });

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        Assert.True(handlerCalled);
        Assert.Equal("Test", receivedMessage);
    }

    [Fact]
    public async Task PublishAsync_CallsMultipleSubscribers_WhenMultipleHandlersSubscribed()
    {
        // Arrange
        var testEvent = new TestEvent { Message = "Test" };
        var callCount = 0;

        _eventBus.Subscribe<TestEvent>(async e =>
        {
            callCount++;
            await Task.CompletedTask;
        });

        _eventBus.Subscribe<TestEvent>(async e =>
        {
            callCount++;
            await Task.CompletedTask;
        });

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task PublishAsync_DoesNotCallHandler_WhenUnsubscribed()
    {
        // Arrange
        var testEvent = new TestEvent { Message = "Test" };
        var handlerCalled = false;

        var subscription = _eventBus.Subscribe<TestEvent>(async e =>
        {
            handlerCalled = true;
            await Task.CompletedTask;
        });

        subscription.Dispose();

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task PublishAsync_WithPriority_CallsHandlersInPriorityOrder()
    {
        // Arrange
        var testEvent = new TestEvent { Message = "Test" };
        var executionOrder = new List<string>();

        _eventBus.Subscribe<TestEvent>(async e =>
        {
            executionOrder.Add("Low");
            await Task.CompletedTask;
        }, EventPriority.Low);

        _eventBus.Subscribe<TestEvent>(async e =>
        {
            executionOrder.Add("High");
            await Task.CompletedTask;
        }, EventPriority.High);

        _eventBus.Subscribe<TestEvent>(async e =>
        {
            executionOrder.Add("Normal");
            await Task.CompletedTask;
        }, EventPriority.Normal);

        // Act
        await _eventBus.PublishAsync(testEvent, EventPriority.High);

        // Assert
        Assert.Single(executionOrder);
        Assert.Equal("High", executionOrder[0]);
    }

    [Fact]
    public async Task PublishAsync_DoesNotThrow_WhenNoSubscribers()
    {
        // Arrange
        var testEvent = new TestEvent { Message = "Test" };

        // Act & Assert
        await _eventBus.PublishAsync(testEvent);
    }

    [Fact]
    public async Task PublishAsync_ContinuesExecution_WhenHandlerThrows()
    {
        // Arrange
        var testEvent = new TestEvent { Message = "Test" };
        var secondHandlerCalled = false;

        _eventBus.Subscribe<TestEvent>(async e =>
        {
            throw new Exception("Test exception");
        });

        _eventBus.Subscribe<TestEvent>(async e =>
        {
            secondHandlerCalled = true;
            await Task.CompletedTask;
        });

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        Assert.True(secondHandlerCalled);
    }

    [Fact]
    public void Subscribe_ReturnsDisposable_ThatUnsubscribes()
    {
        // Arrange
        var handlerCalled = false;

        // Act
        var subscription = _eventBus.Subscribe<TestEvent>(async e =>
        {
            handlerCalled = true;
            await Task.CompletedTask;
        });

        subscription.Dispose();

        // Assert
        Assert.NotNull(subscription);
    }

    [Fact]
    public async Task Unsubscribe_RemovesHandler_WhenCalled()
    {
        // Arrange
        var testEvent = new TestEvent { Message = "Test" };
        var handlerCalled = false;

        Func<TestEvent, Task> handler = async e =>
        {
            handlerCalled = true;
            await Task.CompletedTask;
        };

        var subscription = _eventBus.Subscribe(handler);
        subscription.Dispose();

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        Assert.False(handlerCalled);
    }

    private class TestEvent
    {
        public string Message { get; set; } = string.Empty;
    }
}
