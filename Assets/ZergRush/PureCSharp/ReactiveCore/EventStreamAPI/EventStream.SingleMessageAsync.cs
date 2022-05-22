using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ZergRush.ReactiveCore
{
    public static partial class StreamApi
    {
        static YieldAwaitable frame => Task.Yield();

        public static async Task<T> SingleMessageAsync<T>(this IEventStream<T> stream)
        {
            T result = default(T);
            bool finished = false;
            var waiting = stream.Subscribe(res =>
            {
                result = res;
                finished = true;
            });
            while (!finished)
                await frame;
            return result;
        }

        public static async Task SingleMessageAsync(this IEventStream stream)
        {
            bool finished = false;
            var waiting = stream.Subscribe(() => { finished = true; });
            while (!finished)
                await frame;
        }
    }
}