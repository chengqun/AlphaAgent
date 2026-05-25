using System;
using System.Collections.Concurrent;

namespace AlphaAgent.Maui.Services;

public interface IEventBusService
{
    void Subscribe<T>(Action<T> handler);
    void Unsubscribe<T>(Action<T> handler);
    void Publish<T>(T @event);
}

public class EventBusService : IEventBusService
{
    private readonly ConcurrentDictionary<Type, Delegate> _handlers = new();

    public void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        _handlers.AddOrUpdate(type, handler, (_, existing) => 
        {
            var existingHandler = (Action<T>)existing;
            return existingHandler + handler;
        });
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var existing))
        {
            var existingHandler = (Action<T>)existing;
            existingHandler -= handler;
            if (existingHandler is null)
            {
                _handlers.TryRemove(type, out _);
            }
            else
            {
                _handlers[type] = existingHandler;
            }
        }
    }

    public void Publish<T>(T @event)
    {
        if (_handlers.TryGetValue(typeof(T), out var handler))
        {
            ((Action<T>)handler)?.Invoke(@event);
        }
    }
}