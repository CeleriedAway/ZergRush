using UnityEngine;
using ZergRush.ReactiveCore;

namespace Demo.CellDemo
{
    public class Unit
    {
        public Cell<int> attack = new Cell<int>(10);
        public Cell<int> defence = new Cell<int>(10);
        public Cell<int> hp = new Cell<int>(10);

        public void Upgrade()
        {
            attack.value += Random.Range(1, 4);
            defence.value += Random.Range(1, 4);
            hp.value += Random.Range(1, 4);
        }
	
        public static Unit RandomOne()
        {
            return new Unit
            {
                attack = {value = Random.Range(10, 20)},
                defence = {value = Random.Range(10, 20)},
                hp = {value = Random.Range(10, 20)}
            };
        }
    }
}