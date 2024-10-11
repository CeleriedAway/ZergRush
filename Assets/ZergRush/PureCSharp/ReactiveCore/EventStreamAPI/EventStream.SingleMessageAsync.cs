using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ZergRush.ReactiveCore
{
    public static partial class StreamApi
    {
        static YieldAwaitable frame => Task.Yield();

        public static Task<T> SingleMessageAsync<T>(this IEventStream<T> stream)
        {
            var result = new TaskCompletionSource<T>();
            IDisposable waiting = null;
            waiting = stream.Subscribe(res =>
            {
                result.SetResult(res);
                waiting.Dispose();
            });
            return result.Task;
        }

        public static async Task SingleMessageAsync(this IEventStream stream)
        {
            var result = new TaskCompletionSource<int>();
            IDisposable waiting = null;
            waiting = stream.Subscribe(() =>
            {
                result.SetResult(0);
                waiting.Dispose();
            });
            await result.Task;
        }
    }
}