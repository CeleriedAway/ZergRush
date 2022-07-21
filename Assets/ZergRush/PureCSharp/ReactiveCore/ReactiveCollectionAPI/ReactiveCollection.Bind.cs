using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using ZergRush.CodeGen;

namespace ZergRush.ReactiveCore
{
    public static partial class ReactiveCollectionAPI
    {
        public static IDisposable BindEach<T>(this IReactiveCollection<T> collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }

            return collection.update.Subscribe(rce =>
            {
                switch (rce.type)
                {
                    case ReactiveCollectionEventType.Insert:
                    case ReactiveCollectionEventType.Set:
                        action(rce.newItem);
                        break;
                    case ReactiveCollectionEventType.Reset:
                        foreach (var item in collection)
                        {
                            action(item);
                        }

                        break;
                }
            });
        }

        /// Wont work well if collection has same elements multiple times
        [MustUseReturnValue]
        public static IDisposable AffectEach<T>(this IReactiveCollection<T> collection,
            Action<IConnectionSink, T> affect) where T : class
        {
            var itemConnectionsDict = new Dictionary<T, Connections>();

            collection.BindEach(item =>
            {
                var itemConnections = new Connections();
                if (itemConnectionsDict.ContainsKey(item))
                {
                    LogSink.errLog?.Invoke(
                        "it seems item is already loaded, this function wont work if elements repeated in the collection");
                    return;
                }

                affect(itemConnections, item);
                itemConnectionsDict[item] = itemConnections;
            }, item => { itemConnectionsDict.TakeKey(item).DisconnectAll(); });

            return new AnonymousDisposable(() =>
            {
                foreach (var connections in itemConnectionsDict.Values)
                {
                    connections.DisconnectAll();
                }
            });
        }

        public static IDisposable BindEach<T>(this IReactiveCollection<T> collection, Action<T> onInsert,
            Action<T> onRemove)
        {
            foreach (var item in collection)
            {
                onInsert(item);
            }

            return collection.update.Subscribe(rce =>
            {
                switch (rce.type)
                {
                    case ReactiveCollectionEventType.Insert:
                        onInsert(rce.newItem);
                        break;
                    case ReactiveCollectionEventType.Set:
                        onInsert(rce.newItem);
                        onRemove(rce.oldItem);
                        break;
                    case ReactiveCollectionEventType.Remove:
                        onRemove(rce.oldItem);
                        break;
                    case ReactiveCollectionEventType.Reset:
                        foreach (var item in rce.oldData)
                        {
                            onRemove(item);
                        }

                        foreach (var item in rce.newData)
                        {
                            onInsert(item);
                        }

                        break;
                }
            });
        }

        /// Calls actionConfig with current value of and subscribes to its updates with that actionConfig.
        public static IDisposable Bind<T>(this IReactiveCollection<T> list, Action<IReadOnlyList<T>> action)
        {
            action(list);
            return list.update.Subscribe(_ => action(list));
        }

        public static IDisposable BindCollection<T>(this IReactiveCollection<T> list,
            Action<IReactiveCollectionEvent<T>> action)
        {
            var disp = list.update.Subscribe(action);
            action(new ReactiveCollectionEvent<T> { type = ReactiveCollectionEventType.Reset, newData = list });
            return disp;
        }
    }
}