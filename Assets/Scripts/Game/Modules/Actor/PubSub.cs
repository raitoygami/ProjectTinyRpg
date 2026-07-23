using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PubSub
{
    private readonly Dictionary<string, IEnumerable> events = new();
    private readonly List<UniTask> _publishTaskList = new List<UniTask>(32);

    public async UniTask<bool> Publish<TEventArgs>(TEventArgs args, bool sequential = false) where TEventArgs : EventArgs
    {
        var key = typeof(TEventArgs).ToString();
        if (!events.TryGetValue(key, out var @event))
            return false;

        var handlers = (List<Func<TEventArgs, UniTask>>)@event;
        if (sequential)
        {
            for (var i = handlers.Count - 1; i >= 0 ; i--)
                await handlers[i](args);
        }
        else
        {
            _publishTaskList.Clear();
            for (var i = handlers.Count - 1; i >= 0 ; i--)
            {
                _publishTaskList.Add(handlers[i](args));
            }
            /*foreach (var t in handlers)
                _publishTaskList.Add(t(args));*/

            await UniTask.WhenAll(_publishTaskList);
        }
        return true;
    }

    public Action Subscribe<TEventArgs>(Func<TEventArgs, UniTask> handler) where TEventArgs : EventArgs
    {
        var key = typeof(TEventArgs).ToString();
        if (!events.ContainsKey(key))
        {
            events.Add(key, new List<Func<TEventArgs, UniTask>>());
        }

        (events[key] as List<Func<TEventArgs, UniTask>>)?.Add(handler);
        return delegate
        {
            Unsubscribe(handler);
        };
    }

    public void Unsubscribe<TEventArgs>(Func<TEventArgs, UniTask> handler) where TEventArgs : EventArgs
    {
        var key = typeof(TEventArgs).ToString();
        if (events.TryGetValue(key, out var @event))
        {
            (@event as List<Func<TEventArgs, UniTask>>)?.Remove(handler);
        }
    }

    public bool HasSubscription<TEventArgs>() where TEventArgs : EventArgs
    {
        var key = typeof(TEventArgs).ToString();
        if (!events.TryGetValue(key, out var @event))
            return false;
        return @event is List<Func<TEventArgs, UniTask>> {Count: > 0};
    }
}