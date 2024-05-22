using System.Collections.Generic;
using ZergRush;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.ReactiveUI
{
    public class ReactiveCollectionImitator<T, T2> where T : IUpdatableFrom<T2>, IHashable, new()
        where T2 : IHashable
    {
        public ReactiveCollection<T> data;

        private ZRHashHelper hashHelper = new ZRHashHelper();

        public ReactiveCollectionImitator(IList<T2> initialData)
        {
            data = new ReactiveCollection<T>();
            data.UpdateFromCollection(initialData);
        }

        public void UpdateFrom(IList<T2> newData)
        {
            hashHelper.Reuse();
            var dataHash = ListHash(data, hashHelper);
            hashHelper.Reuse();
            var newHash = ListHash(newData, hashHelper);
            
            if (dataHash != newHash)
                data.UpdateFromCollection(newData);
        }

        private static ulong ListHash<TList>(IList<TList> list, ZRHashHelper __helper) where TList : IHashable
        {
            System.UInt64 hash = 345093625;
            hash ^= (ulong)1261931807;
            hash += hash << 11;
            hash ^= hash >> 7;
            var size = list.Count;
            for (int i = 0; i < size; i++)
            {
                hash += list[i] != null ? list[i].CalculateHash(__helper) : 345093625;
                hash += hash << 11;
                hash ^= hash >> 7;
            }

            return hash;
        }
    }
}