using System;
using System.Collections.Generic;
using UnityEngine;

namespace CortexPlugin
{
    public class EventBufferInstance : MonoBehaviour
    {
        List<EventBufferBase> buffers = new List<EventBufferBase>();

        private void Update()
        {
            foreach (EventBufferBase buffer in buffers)
                buffer.Process();
        }

        public void AddBuffer(EventBufferBase newBuffer) => buffers.Add(newBuffer);
        public void AddBuffers(IEnumerable<EventBufferBase> buffers)
        {
            foreach (EventBufferBase buffer in buffers)
                AddBuffer(buffer);
        }
    }

    public abstract class EventBufferBase
    {
        public abstract void Process();
    }

    public class EventBuffer<T> : EventBufferBase
    {
        private List<Action<T>> callbacks = new List<Action<T>>();

        T data;
        bool shouldFire;
        object locker = new object();

        public override void Process()
        {
            try
            {
                lock (locker)
                {
                    if (shouldFire)
                    {
                        shouldFire = false;

                        List<Action<T>> deadActions = new List<Action<T>>();

                        foreach (Action<T> action in callbacks)
                        {
                            try
                            {
                                action(data);
                            }
                            catch (Exception e)
                            {
                                if (action.Target == null)
                                {
                                    deadActions.Add(action);
                                    Debug.LogWarning("Exception caused by null method or delegate in event buffer." +
                                        "It was automatically cleaned up, but best practice is to manually remove" +
                                        "event subcriptions in OnDisable() or OnDestroy() when they are no longer needed." + e.Message);
                                }
                                Debug.LogException(e);
                            }
                        }

                        foreach (Action<T> action in deadActions)
                        {
                            callbacks.Remove(action);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Exception in event Buffer process: " + e);
                Debug.LogException(e);
            }
        }

        public void Subscribe(Action<T> action)
        {
            lock (locker)
                callbacks.Add(action);
        }

        public void Unsubscribe(Action<T> action)
        {
            lock (locker)
                callbacks.Remove(action);
        }

        public static EventBuffer<T> operator +(EventBuffer<T> lhs, Action<T> rhs)
        {
            if (lhs == null)
            {
                Debug.LogError("Attempted to subscribe to a null EventBuffer," +
                    "make sure you are starting Cortex before trying to use it.");
            }
            else
                lhs.Subscribe(rhs);

            return lhs;
        }
        public static EventBuffer<T> operator -(EventBuffer<T> lhs, Action<T> rhs)
        {
            lhs.Unsubscribe(rhs);
            return lhs;
        }

        public void OnParentEvent(object sender, T args)
        {
            try
            {
                lock (locker)
                {
                    shouldFire = true;
                    data = args;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Exception in event Buffer parent event: " + e);
            }
        }
    }
}
