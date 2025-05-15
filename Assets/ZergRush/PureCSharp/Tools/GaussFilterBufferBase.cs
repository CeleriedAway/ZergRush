using System.Collections.Generic;
using UnityEngine;
using ZergRush.ReactiveCore;

namespace ZergRush
{
    public class GaussFilterBufferBase<T> : Cell<T>
    {
        protected CycleBuffer<T> valueBuffer;

        public GaussFilterBufferBase(int samples)
        {
            valueBuffer = new CycleBuffer<T>(samples);
        }

        public GaussFilterBufferBase(int samples, T prefillValue)
        {
            valueBuffer = new CycleBuffer<T>(samples, prefillValue);
        }

        public void Clear()
        {
            valueBuffer.Clear();
        }
            
        public void Fill(T value)
        {
            valueBuffer.Fill(value);
        }
    }
    
    public static class GaussFilterWeightsCache
    {
        static Dictionary<int, float[]> cache = new Dictionary<int, float[]>();

        public static float [] GetWeights(int samples)
        {
            if (cache.TryGetValue(samples, out var cachedWeights))
            {
                return cachedWeights;
            }

            var weights = new float[samples];
            float weightAccum = 0;
            for (int i = 0; i < samples; i++)
            {
                float sigma = samples * samples * 2;
                var newWeight = Mathf.Exp(-i * i / sigma);
                weights[i] = newWeight;
                weightAccum += newWeight;
            }

            for (int i = 0; i < samples; i++)
            {
                weights[i] /= weightAccum;
            }

            cache[samples] = weights;
            return weights;
        }
    }
}