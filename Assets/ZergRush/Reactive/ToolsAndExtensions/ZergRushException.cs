using System;

namespace ZergRush
{
    public class ZergRushException : Exception
    {
        public ZergRushException(string message) : base(message)
        {
        }
        public ZergRushException() 
        {
        }
    }
}