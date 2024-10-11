using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IMessage
{

}

public class EventManager
{
    static readonly Dictionary<Type, Action<IMessage>> events = new Dictionary<Type, Action<IMessage>>();

    static readonly Dictionary<Delegate, Action<IMessage>> eventLookups = new Dictionary<Delegate, Action<IMessage>>();
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        events.Clear();
        eventLookups.Clear();
    }

    public static void Subscribe<T>(Action<T> evt) where T : IMessage
    {
        if (!eventLookups.ContainsKey(evt))
        {
            Action<IMessage> newAction = (e) => evt((T)e);
            eventLookups[evt] = newAction;
            
            if (events.ContainsKey(typeof(T)))
                events[typeof(T)] += newAction;
            else
                events[typeof(T)] = newAction;
        }
    }

    public static void Unsubscribe<T>(Action<T> evt) where T : IMessage
    {
        if (eventLookups.TryGetValue(evt, out var action))
        {
            if (events.TryGetValue(typeof(T), out var tempAction))
            {
                tempAction -= action;
                if (tempAction == null)
                    events.Remove(typeof(T));
                else
                    events[typeof(T)] = tempAction;
            }

            eventLookups.Remove(evt);
        }
    }

    public static void TriggerEvent<T>(T message) where T : IMessage
    {
        if (events.TryGetValue(typeof(T), out var action))
            action.Invoke(message);
    }

}
