using System;
using ZergRush.CodeGen;

namespace ZergRush
{
    // Copy paste from system random diassembly
    [GenTask(GenTaskFlags.SimpleDataPack), GenTaskCustomImpl(GenTaskFlags.DefaultConstructor), GenInLocalFolder]
    public partial class ZergRandom
    {
        public static ZergRandom global = new ZergRandom();
        
        int[] SeedArray = new int[56];
        int inext;
        int inextp;
        
        const int MBIG = 2147483647;
        const int MSEED = 161803398;
        const int MZ = 0;

        public ZergRandom()
            : this(Environment.TickCount)
        {
        }

        public ZergRandom(int Seed)
        {
            int num1 = 161803398 - (Seed == int.MinValue ? int.MaxValue : Math.Abs(Seed));
            this.SeedArray[55] = num1;
            int num2 = 1;
            for (int index1 = 1; index1 < 55; ++index1)
            {
                int index2 = 21 * index1 % 55;
                this.SeedArray[index2] = num2;
                num2 = num1 - num2;
                if (num2 < 0)
                    num2 += int.MaxValue;
                num1 = this.SeedArray[index2];
            }
            for (int index1 = 1; index1 < 5; ++index1)
            {
                for (int index2 = 1; index2 < 56; ++index2)
                {
                    this.SeedArray[index2] -= this.SeedArray[1 + (index2 + 30) % 55];
                    if (this.SeedArray[index2] < 0)
                        this.SeedArray[index2] += int.MaxValue;
                }
            }
            this.inext = 0;
            this.inextp = 21;
            Seed = 1;
        }

        protected double Sample()
        {
            return (double) this.InternalSample() * 4.6566128752458E-10;
        }

        private int InternalSample()
        {
            int inext = this.inext;
            int inextp = this.inextp;
            int index1;
            if ((index1 = inext + 1) >= 56)
                index1 = 1;
            int index2;
            if ((index2 = inextp + 1) >= 56)
                index2 = 1;
            int num = this.SeedArray[index1] - this.SeedArray[index2];
            if (num == int.MaxValue)
                --num;
            if (num < 0)
                num += int.MaxValue;
            this.SeedArray[index1] = num;
            this.inext = index1;
            this.inextp = index2;
            return num;
        }

        public int Next()
        {
            return this.InternalSample();
        }

//        private double GetSampleForLargeRange()
//        {
//            int num = this.InternalSample();
//            if (this.InternalSample() % 2 == 0)
//                num = -num;
//            return ((double) num + 2147483646.0) / 4294967293.0;
//        }

        /// <summary>Returns a non-negative random integer that is less than the specified maximum.</summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue" /> must be greater than or equal to 0. </param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue" />; that is, the range of return values ordinarily includes 0 but not <paramref name="maxValue" />. However, if <paramref name="maxValue" /> equals 0, <paramref name="maxValue" /> is returned.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="maxValue" /> is less than 0. </exception>

        /// <summary>Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.</summary>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        public double NextDouble()
        {
            return this.Sample();
        }
        public float NextFloat()
        {
            return (float)this.Sample();
        }
        public float MinusOneToOne()
        {
            return (float)this.Sample() * 2 - 1;
        }

        public void NextBytes(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof (buffer));
            for (int index = 0; index < buffer.Length; ++index)
                buffer[index] = (byte) (this.InternalSample() % 256);
        }

    }
}