namespace ZergRush
{
    public class GaussFilteredFloat : GaussFilterBufferBase<float>
    {
        public float PushValue(float value)
        {
            valueBuffer.Push(value);
            var c = valueBuffer.Count;
            var weights = GaussFilterWeightsCache.GetWeights(c);
            float result = 0;
            for (int i = 0; i < c; i++)
            {
                var gauss = weights[i];
                result += gauss * valueBuffer.Sample(i);
            }
            this.value = result;
            return result;
        }

        public GaussFilteredFloat(int samples) : base(samples)
        {
        }

        public GaussFilteredFloat(int samples, float prefillValue) : base(samples, prefillValue)
        {
        }
    }
}