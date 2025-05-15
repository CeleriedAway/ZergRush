using UnityEngine;

namespace ZergRush
{
    public class GaussFilteredVector : GaussFilterBufferBase<Vector3>
    {
        public Vector3 PushValue(Vector3 value)
        {
            valueBuffer.Push(value);
            var c = valueBuffer.Count;
            var weights = GaussFilterWeightsCache.GetWeights(c);
            Vector3 result = Vector3.zero;
            for (int i = 0; i < c; i++)
            {
                var gauss = weights[i];
                result += gauss * valueBuffer.Sample(i);
            }
            this.value = result;
            return result;
        }

        public GaussFilteredVector(int samples) : base(samples)
        {
        }

        public GaussFilteredVector(int samples, Vector3 prefillValue) : base(samples, prefillValue)
        {
        }
    }
}