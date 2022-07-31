using System;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Core
{
    public static class SynchronizationContextExtensions
    {
        public static void Post<T>(this SynchronizationContext context, T caller, Action<T> callback)
            where T : class
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (caller is null)
            {
                throw new ArgumentNullException(nameof(caller));
            }

            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            context.Post(o => callback.Invoke((T)o!), caller);
        }

        public static void Send<T>(this SynchronizationContext context, T caller, Action<T> callback)
            where T : class
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (caller is null)
            {
                throw new ArgumentNullException(nameof(caller));
            }

            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            context.Send(o => callback.Invoke((T)o!), caller);
        }
    }
}
