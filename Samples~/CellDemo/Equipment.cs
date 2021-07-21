using System.Linq;
using UnityEngine;
using ZergRush;
using ZergRush.ReactiveCore;

namespace Demo.CellDemo
{
    public class Equipment
    {
        public UnitBuffType type;
        public Cell<int> buff = new Cell<int>(3);

        public void Upgrade()
        {
            buff.value += 1;
        }

        public static Equipment RandomOne()
        {
            var allValues = Utils.GetEnumValues<UnitBuffType>().ToList();
            var randomVal = allValues[Random.Range(0, allValues.Count)];
            return new Equipment
            {
                type = randomVal,
                buff = {value = Random.Range(1, 5)}
            };
        }
    }
}