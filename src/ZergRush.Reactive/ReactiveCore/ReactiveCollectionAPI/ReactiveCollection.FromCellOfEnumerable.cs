using System;
using System.Collections.Generic;
using System.Linq;

namespace ZergRush.ReactiveCore
{
    public static partial class ReactiveCollectionAPI
    {
        public static IReactiveCollection<T> ToReactiveCollection<T>(this ICell<IEnumerable<T>> cell)
        {
            return new ReactiveCollectionFromCellOfArray<T> { cell = cell };
        }

        class ReactiveCollectionFromCellOfArray<T> : AbstractCollectionTransform<T>
        {
            public ICell<IEnumerable<T>> cell;

            protected override IDisposable StartListenAndRefill()
            {
                return cell.Bind(coll =>
                {
                    if (coll == null)
                    {
                        buffer.Reset();
                        return;
                    }

                    // TODO make smarter algorithm later
                    buffer.Reset((IEnumerable<T>)coll);
                    // This algorithm does not work on simple types and same items in collection
                    //                    var newItems = coll as T[] ?? coll.ToArray();
                    //                    for (var index = 0; index < newItems.Length; index++)
                    //                    {
                    //                        var item = newItems[index];
                    //                        if (buffer.Contains(item)) continue;
                    //                        buffer.Add(item);
                    //                    }
                    //                    for (var index = buffer.Count - 1; index >= 0; index--)
                    //                    {
                    //                        var oldItem = buffer[index];
                    //                        if (newItems.Contains(oldItem)) continue;
                    //                        buffer.RemoveAt(index);
                    //                    }
                });
            }

            protected override void RefillRaw()
            {
                buffer.Reset(cell.value);
            }
        }

    }
}