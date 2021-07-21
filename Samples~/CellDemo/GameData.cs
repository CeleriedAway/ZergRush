using System.Linq;
using ZergRush.ReactiveCore;

namespace Demo.CellDemo
{
    public class GameData
    {
        public GameData()
        {
            unitsAvailable.Reset(Enumerable.Range(0, 10).Select(_ => Unit.RandomOne()));
            equipmentAvailable.Reset(Enumerable.Range(0, 10).Select(_ => Equipment.RandomOne()));
        }
	
        public ReactiveCollection<Unit> unitsAvailable = new ReactiveCollection<Unit>();
        public ReactiveCollection<Equipment> equipmentAvailable = new ReactiveCollection<Equipment>();
    }
}