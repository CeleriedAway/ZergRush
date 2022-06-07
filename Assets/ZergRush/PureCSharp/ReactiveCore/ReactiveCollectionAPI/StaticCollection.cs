using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ZergRush.ReactiveCore
{
    public class StaticCollection<T> : IReactiveCollection<T>
    {
        public List<T> list;

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEventStream<IReactiveCollectionEvent<T>> update
        {
            get { return AbandonedStream<ReactiveCollectionEvent<T>>.value; }
        }

        public List<T> current
        {
            get { return list; }
        }

        static readonly StaticCollection<T> def = new StaticCollection<T>{list = new List<T>()};
        public static IReactiveCollection<T> Empty()
        {
            return def;
        }

        public int Count => list.Count;

        public T this[int index] => list[index];
    }

    public static partial class ReactiveCollectionAPI
    {
        public static IReactiveCollection<T> ToStaticReactiveCollection<T>(this List<T> coll)
        {
            return new StaticCollection<T> { list = coll };
        }

        public static IReactiveCollection<T> ToStaticReactiveCollection<T>(this IEnumerable<T> coll)
        {
            return new StaticCollection<T> { list = coll.ToList() };
        }

        public static List<T> ToList<T>(this ReactiveCollection<T> coll)
        {
            return coll.AsEnumerable().ToList();
        }
    }

}