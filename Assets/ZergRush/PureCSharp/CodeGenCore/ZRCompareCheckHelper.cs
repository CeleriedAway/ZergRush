using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ZergRush
{
    public class ZRCompareCheckHelper
    {
        public Stack<string> path = new();
        public static implicit operator Stack<string> (ZRCompareCheckHelper helper)
        {
            return helper.path;
        }
        
        ObjectIDGenerator generatorSelf = new();
        ObjectIDGenerator generatorOther = new();
        Dictionary<long, long> projection = new();

        public void Push(string name)
        {
            path.Push(name);
        }

        public void Pop()
        {
            path.Pop();
        }
        
        public bool NeedCompareCheck<T>(string name, Action<string> print, T self, T other)
        {
            var selfId = generatorSelf.GetId(self, out var firstTime);
            var otherId = generatorOther.GetId(other, out var firstTimeOther);

            if (firstTime && !firstTimeOther)
            {
                print(
                    $"{path.Reverse().PrintCollection("/")} {name} in self object {self} was ecnountered first time " +
                    $"while other model already encountered {other} instance encountered second time ");
            }
            
            if (!firstTime && firstTimeOther)
            {
                print(
                    $"{path.Reverse().PrintCollection("/")} {name} in self object {self} was encountered not first time " +
                    $"while other model instance of {other} encountered first time");
            }
            
            if (firstTime)
            {
                projection[selfId] = otherId;
                return true;
            }
            else 
            {
                if (projection[selfId] != otherId)
                {
                    print($"{path.Reverse().PrintCollection("/")} {name} {self} {selfId} instance encountered second time " +
                          $"and is not corespond to {other} {otherId} {firstTimeOther} encountered previous time");
                }
                return false;
            }
        }
    }
}