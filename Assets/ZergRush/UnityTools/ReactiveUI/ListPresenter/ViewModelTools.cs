﻿using System;
using System.Collections.Generic;
using System.Linq;
using ZergRush;

namespace ZergRush.CodeGen
{
    public static class ViewModelTools
    {
        public static void UpdateFromCollection<T, T2>(this IList<T> buffer, IEnumerable<T2> collection)
            where T : IUpdatableFrom<T2>, new()
        {
            if (collection == null)
            {
                buffer.Clear();
                return;
            }

            var coll = collection as List<T2> ?? collection.ToList();
            int oldCnt = buffer.Count;
            while (buffer.Count > coll.Count)
            {
                buffer.TakeLast();
            }
            for (int i = 0; i < coll.Count; i++)
            {
                var inst = new T();
                inst.UpdateFrom(coll[i], new ZRUpdateFromHelper());
                if (i < oldCnt)
                {
                    buffer[i] = inst;
                }
                else
                {
                    buffer.Add(inst);
                }
            }
        }
        
        public static void UpdateFromCollection<T, T2>(this IList<T> buffer, IEnumerable<T2> collection, Func<T2, T> createFactory, Action<T2, T> destroyFactory)
            where T : IUpdatableFrom<T2>
        {
            if (collection == null)
            {
                buffer.Clear();
                return;
            }

            var coll = collection as List<T2> ?? collection.ToList();
            
            while (buffer.Count > coll.Count)
            {
                buffer.TakeLast();
            }
            for (int i = buffer.Count; i < coll.Count; i++)
            {
                buffer.Add(createFactory(coll[i]));
            }
            for (int i = 0; i < coll.Count; i++)
            {
                buffer[i].UpdateFrom(coll[i], new ZRUpdateFromHelper());
            }
        }


    }
}
