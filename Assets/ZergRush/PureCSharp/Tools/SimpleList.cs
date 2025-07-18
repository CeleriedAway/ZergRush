using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ZergRush;

public class SimpleList<T> : IList<T>, IReadOnlyList<T>
{
    public SimpleList()
    {
        
    }
    
    public SimpleList(IEnumerable<T> list)
    {
        foreach (var x1 in list)
        {
            Add(x1);
        }
    }
    
    public SimpleList(IReadOnlyList<T> list)
    {
        if (list == null) return;
        var listCount = list.Count;
        Capacity = listCount;
        currentCount = listCount;
        var i = 0;
        foreach (var x1 in list)
        {
            data[i] = x1;
            i++;
        }
    }
    
    public SimpleList(int capacity)
    {
        Capacity = capacity;
    }
    
    public T[] data = Array.Empty<T>();
    int currentCount;

    public int Capacity
    {
        get { return data.Length; }
        set
        {
            if (currentCount > value)
            {
                throw new ZergRushException(
                    $"currentCount:{currentCount} is more then desired capacity:{value}");
            }

            Array.Resize(ref data, value);
        }
    }


    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return data.GetEnumerator();
    }

    public void Add(T item)
    {
        EnsureCapacity(currentCount + 1);
        data[currentCount] = item;
        currentCount++;
    }
    
    public void AddRange(ICollection<T> item)
    {
        EnsureCapacity(currentCount + item.Count);
        item.CopyTo(data, currentCount);
        currentCount += item.Count;
    }
    
    public void AddRange(IEnumerable<T> item)
    {
        if (item is ICollection<T> coll)
        {
            AddRange(coll);
            return;
        }
        foreach (var x1 in item)
        {
            Add(x1);           
        }
    }

    public void RemoveTail(int newLastIndex)
    {
        if (newLastIndex < 0 || newLastIndex >= currentCount)
            throw new ZergRushException("remove tail last index out of range index:" + newLastIndex + " count:" + currentCount);
        currentCount = newLastIndex + 1;
    }

    public void Clear()
    {
        currentCount = 0;
    }

    public bool Contains(T item)
    {
        if (item == null)
        {
            for (int index = 0; index < currentCount; ++index)
            {
                if (data[index] == null)
                    return true;
            }

            return false;
        }

        EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
        for (int index = 0; index < currentCount; ++index)
        {
            if (equalityComparer.Equals(data[index], item))
                return true;
        }

        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(data, 0, array, 0, currentCount);
    }

    public void RemoveAt(int index)
    {
        if ((uint) index >= (uint) currentCount)
            throw new ZergRushException("remove index out of range index:" + index + " count:" + currentCount);
        --currentCount;
        if (index < currentCount)
            Array.Copy((Array) data, index + 1, (Array) data, index, currentCount - index);
        data[currentCount] = default(T);
    }

    public bool Remove(T item)
    {
        int index = this.IndexOf(item);
        if (index < 0)
            return false;
        this.RemoveAt(index);
        return true;
    }

    public int Count => currentCount;

    public bool IsReadOnly => data.IsReadOnly;

    public int IndexOf(T item)
    {
        return Array.IndexOf<T>(data, item, 0, currentCount);
    }

    public int IndexOf(Func<T, bool> predicate) {
        for (int i = 0; i < currentCount; i++) {
            if (predicate(data[i]))
                return i;
        }
        return -1;
    }

    public int IndexOf(T item, int index)
    {
        if (index > currentCount)
            throw new ZergRushException("index out of range index:" + index + " count:" + currentCount);
        return Array.IndexOf<T>(data, item, index, currentCount - index);
    }

    public int IndexOf(T item, int index, int count)
    {
        if (index > currentCount)
            throw new ZergRushException("index out of range index:" + index + " count:" + currentCount);
        if (count < 0 || index > currentCount - count)
            throw new ZergRushException("count out of range index:" + index + " count:" + currentCount);
        return Array.IndexOf<T>(data, item, index, count);
    }

    public void EnsureCapacity(int capacity)
    {
        var num = 0;
        if (capacity > data.Length)
        {
            if (data.Length == 0)
                num = 4;
            else
                num = data.Length * 2;
        }
        else
        {
            return;
        }
        if (num < capacity) num = capacity;
        Capacity = num;
    }

    public void Insert(int index, T item)
    {
        if ((uint) index > (uint) currentCount)
            throw new ZergRushException("insert index out of range index:" + index + " count:" + currentCount);
        if (currentCount == data.Length)
            this.EnsureCapacity(currentCount + 1);
        if (index < currentCount)
            Array.Copy((Array) data, index, (Array) data, index + 1, currentCount - index);
        data[index] = item;
        ++currentCount;
    }

    public ref T AtRef(int index)
    {
        if ((uint) index >= (uint) currentCount)
            throw new ZergRushException("get index out of range index:" + index + " count:" + currentCount);
        return ref data[index];
    }

    public T this[int index]
    {
        get
        {
            if ((uint) index >= (uint) currentCount)
                throw new ZergRushException("get index out of range index:" + index + " count:" + currentCount);
            return data[index];
        }
        set
        {
            if ((uint) index >= (uint) currentCount)
                throw new ZergRushException("set index out of range index:" + index + " count:" + currentCount);
            data[index] = value;
        }
    }
    
    public SimpleList<T> Copy()
    {
        var list = new SimpleList<T>();
        list.data = new T[data.Length];
        list.currentCount = currentCount;
        Array.Copy(data, list.data, data.Length);
        return list;
    }
    
    public SimpleList<T> ToList() => Copy();

    public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
    {
        private SimpleList<T> list;
        private int index;
        private T current;

        internal Enumerator(SimpleList<T> list)
        {
            this.list = list;
            this.index = 0;
            this.current = default(T);
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (list.currentCount <= index) return false;
            this.current = list[index];
            this.index++;
            return true;
        }

        public T Current => this.current;
        object IEnumerator.Current => (object) this.Current;

        void IEnumerator.Reset()
        {
            this.index = 0;
            this.current = default(T);
        }

    }
}