﻿using System;

namespace ZergRush
{
    public class CycleBuffer<T>
    {
        public int currentIndex = -1;
        public int total;
        public int max;
        public T[] values;


        public CycleBuffer(int sampleCount)
        {
            max = sampleCount;
            values = new T[sampleCount];
        }

        public CycleBuffer(int sampleCount, T prefillValue) : this(sampleCount)
        {
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = prefillValue;
            }
            total = max;
        }
        
        public CycleBuffer(int sampleCount, Func<T> prefillFactory) : this(sampleCount)
        {
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = prefillFactory();
            }
            total = max;
        }

        public void Clear()
        {
            total = 0;
        }

        public int Count => total < max ? total : max;

        // index sampling goes from newest value to latest at the end
        public T Sample(int index)
        {
            return values[(currentIndex - index + max) % max];
        }

        public bool Filled => total == max;

        // index == 0 is the last one, 1 is the previous and so on
        public void ForEach(Action<int, T> action)
        {
            //Debug.Log(values.PrintCollection());
            if (currentIndex < 0) return;
            int limit = Count;
            for (int i = 0; i < limit; i++)
            {
                action(i, Sample(i));
            }
        }

        public void Fill(T value)
        {
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = value;
            }
        }

        public void Push(T value)
        {
            currentIndex = (currentIndex + 1) % max;
            if (total != max) total = currentIndex + 1;
            values[currentIndex] = value;
        }
            
        public T PushAndReturnPrev(T value)
        {
            currentIndex = (currentIndex + 1) % max;
            if (total != max) total = currentIndex + 1;
            var prev = values[currentIndex];
            values[currentIndex] = value;
            return prev;
        }

        // Pushes same value that was in buffer before
        public T PushCached()
        {
            currentIndex = (currentIndex + 1) % max;
            if (total != max) total = currentIndex + 1;
            return values[currentIndex];
        }
    }
}