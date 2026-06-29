#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading;

namespace Alday.UnityAiGameMaker.Editor
{
    public static class UnityAiMainThread
    {
        static readonly Queue<Action> Queue = new();
        static readonly object Mutex = new();

        public static T Run<T>(Func<T> action)
        {
            T result = default;
            Exception exception = null;
            using var wait = new ManualResetEventSlim(false);

            lock (Mutex)
            {
                Queue.Enqueue(() =>
                {
                    try
                    {
                        result = action();
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    finally
                    {
                        wait.Set();
                    }
                });
            }

            wait.Wait();

            if (exception != null)
                throw exception;

            return result;
        }

        public static void Pump()
        {
            while (true)
            {
                Action action = null;
                lock (Mutex)
                {
                    if (Queue.Count > 0)
                        action = Queue.Dequeue();
                }

                if (action == null)
                    break;

                action();
            }
        }
    }
}
#endif
