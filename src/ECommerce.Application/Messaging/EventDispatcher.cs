using ECommerce.Domain.Common;

namespace ECommerce.Application.Messaging;

public interface IEventHandler
{
    string EventType { get; }
    Task HandleAsync(string payloadJson, CancellationToken cancellationToken);
}

public interface IEventDispatcher
{
    Task DispatchAsync(string eventType, string payloadJson, CancellationToken cancellationToken);
}

public class EventDispatcher : IEventDispatcher
{
    private readonly IEnumerable<IEventHandler> _handlers;

    public EventDispatcher(IEnumerable<IEventHandler> handlers)
    {
        _handlers = handlers;
    }

    public Task DispatchAsync(string eventType, string payloadJson, CancellationToken cancellationToken)
    {
        var handler = _handlers.FirstOrDefault(h =>
            string.Equals(h.EventType, eventType, StringComparison.Ordinal));

        return handler?.HandleAsync(payloadJson, cancellationToken)
               ?? Task.CompletedTask;
    }
}