using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ZergRush.CodeGen;

namespace ZergRush.Alive
{
    public sealed partial class __OldRefList<T> : IList<T> where T : class, IDataNode, IReferencableFromDataRoot
    {
        List<__RefListRecord<T>> vals = new List<__RefListRecord<T>>();
        [GenIgnore] public DataRoot root;
        [GenIgnore] public DataNode carrier;

        public void ClearDead()
        {
            for (var i = vals.Count - 1; i >= 0; i--)
            {
                if(IsValid(i) == false) vals.RemoveAt(i);
            }
        }

        public int AliveCount()
        {
            var c = 0;
            for (var i = vals.Count - 1; i >= 0; i--)
            {
                if (IsValid(i)) c++;
            }

            return c;
        }
        public bool HasDead()
        {
            for (var i = vals.Count - 1; i >= 0; i--)
            {
                if (IsValid(i) == false) return true;
            }
            return false;
        }

        public void FillEmptySlot(T val)
        {
            for (var i = vals.Count - 1; i >= 0; i--)
            {
                if (IsValid(i) == false)
                {
                    this[i] = val;
                    return;
                }
            }
            throw new ZergRushException("no free slots");
        }
        
        public void RefreshValid()
        {
            for (var i = vals.Count - 1; i >= 0; i--)
            {
                IsValid(i);
            }
        }
        
        public int IdAtIndex(int index)
        {
            //IsValid(index);
            return vals[index].id;
        }

        public bool IsValid(int index)
        {
            var val = vals[index];
            if (val.id <= 0)
            {
                return false;
            }
            var cached = val.val;
            if (cached == null || val.id != cached.Id)
            {
                if (root == null)
                {
                    throw new ZergRushException("there in no cached ref in non alive ref list");
                }
                var recolled = root.RecallMayBe(val.id);
                if (recolled == null)
                {
                    vals[index] = new __RefListRecord<T>(0);
                    return false;
                }
                cached = recolled as T;
                if (cached == null)
                {
                    throw new ZergRushException("invalid object stored with id: " + val.id);
                }
                // save cached value and return
                vals[index] = new __RefListRecord<T>(cached);
                return true;
            }
            return true;
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < Count; i++)
            {
                IsValid(i);
                if (vals[i].val == item)
                    return i;
            }

            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            if (index < Count)
            {
                vals.RemoveAt(index);
            }
        }

        public T this[int index]
        {
            get
            {
                if (IsValid(index)) return vals[index].val;
                return null;
            }
            set
            {
                if (value == null)
                {
                    throw new Exception($"null arg set as ref into ref list");
                }
                vals[index] = new __RefListRecord<T>(value);
            }
        }

        public void UpdateFrom(__OldRefList<T> other, ObjectPool pool)
        {
            UpdateFrom(other);
        }
        public void UpdateFrom(__OldRefList<T> other)
        {
            vals.Clear();
            for (int i = 0; i < other.Count; i++)
            {
                vals.Add(new __RefListRecord<T>(other.vals[i].id));
            }
        }

        public void CompareCheck(__OldRefList<T> other, Stack<string> path)
        {
            if (Count != other.Count) SerializationTools.LogCompError(path, "Count", other.Count, Count);
            var count = Math.Min(Count, other.Count);
            for (int i = 0; i < count; i++)
            {
                if (vals[i].id != other.vals[i].id) SerializationTools.LogCompError(path, $"id at index: {i.ToString()}", vals[i].id, other.vals[i].id);
            }
        }
        
        public long CalculateHash()
        {
            long hash = 0xffffff;
            for (int i = 0; i < Count; i++)
            {
                hash += vals[i].id;
                hash += hash << 11; hash ^= hash >> 7;
            }
            return hash;
        }

        public void AddEmpty()
        {
            vals.Add(new __RefListRecord<T>(0));
        }

        public void Add(T item)
        {
            vals.Add(new __RefListRecord<T>(item));
        }

        public void Clear()
        {
            vals.Clear();
        }

        public bool Contains(T item)
        {
            for (var i = 0; i < vals.Count; i++)
            {
                IsValid(i);
                if (vals[i].id == (item == null ? 0 : item.Id))
                {
                    vals[i] = new __RefListRecord<T>(item);
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            var itemId = (item == null ? 0 : item.Id);
            return vals.RemoveAll(i => i.id == itemId) > 0;
        }

        public int Count => vals.Count;

        public int CountLinq(Func<T, bool> p)
        {
            return vals.Count(r => p(r.val));
        }
        
        public bool IsReadOnly => false;

        public __OldRefList(ObjectPool pool) { }
        public __OldRefList() { }

        public IEnumerator<T> GetEnumerator()
        {
            RefreshValid();
            return vals.Select(v => v.val).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [GenTask(GenTaskFlags.CompareChech), GenInLocalFolder]
    partial struct __RefListRecord<T> where T : class, IReferencableFromDataRoot
    {
        public int id;
        [GenIgnore] public T val;

        public __RefListRecord(int id)
        {
            this.id = id;
            val = null;
        }
        public __RefListRecord(T val)
        {
            this.id = val.Id;
            this.val = val;
        }
    }
}