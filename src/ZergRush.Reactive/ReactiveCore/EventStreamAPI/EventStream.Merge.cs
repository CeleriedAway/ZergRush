using System;
using System.Collections.Generic;
using System.Linq;

namespace ZergRush.ReactiveCore
{
    public static partial class StreamApi
    {
        /// Merge an array of streams info one stream.
        public static IEventStream<T> Merge<T>(params IEventStream<T>[] others)
        {
            if (others == null || others.Any(s => s == null)) throw new ArgumentException("Null streams in merge");
            return new AnonymousEventStream<T>((reaction) =>
            {
                var disp = new Connections(others.Length);

                foreach (var other in others)
                {
                    disp.Add(other.Subscribe((Action<T>)reaction));
                }

                return disp;
            });
        }

        public static IEventStream Merge(this IEnumerable<IEventStream> others)
        {
            return MergeSome(others.ToArray());
        }

        public static IEventStream<T> Merge<T>(this IEnumerable<IEventStream<T>> events)
        {
            return new AnonymousEventStream<T>(reaction =>
            {
                var disp = new Connections();
                foreach (var other in events)
                {
                    disp.Add(other.Subscribe((Action<T>)reaction));
                }

                return disp;
            });
        }

        public static IEventStream MergeSome(params IEventStream[] others)
        {
            return new AnonymousEventStream((reaction) =>
            {
                var disp = new Connections(others.Length);

                for (var i = 0; i < others.Length; i++)
                {
                    var other = others[i];
                    disp.Add(other.Subscribe(reaction));
                }

                return disp;
            });
        }

        public static IEventStream<T> MergeWith<T>(this IEventStream<T> stream, params IEventStream<T>[] others)
        {
            if (stream == null || others == null || others.Any(s => s == null))
                throw new ArgumentException("Null streams in merge");
            return new AnonymousEventStream<T>((Action<T> reaction) =>
            {
                var disp = new Connections(others.Length + 1);
                disp.Add(stream.Subscribe(reaction));

                foreach (var other in others)
                {
                    disp.Add(other.Subscribe(reaction));
                }

                return disp;
            });
        }

        public static IEventStream MergeWith(this IEventStream stream, params IEventStream[] others)
        {
            if (stream == null || others == null || others.Any(s => s == null))
                throw new ArgumentException("Null streams in merge");
            return new AnonymousEventStream(reaction =>
            {
                var disp = new Connections(others.Length + 1);
                disp.Add(stream.Subscribe(reaction));
                foreach (var other in others)
                {
                    disp.Add(other.Subscribe(reaction));
                }

                return disp;
            });
        }
    }
}