using ZergRush.ReactiveCore;

namespace Demo.CellDemo
{
    public class UiState
    {
        public Cell<Unit> selectedUnit = new Cell<Unit>();
        public Cell<Equipment> selectedEquipment = new Cell<Equipment>();

        public void SetUnitSelection(Unit unit)
        {
            if (selectedUnit.value == unit) selectedUnit.value = null;
            else selectedUnit.value = unit;
        }
        public void SetEquipmentSelection(Equipment eq)
        {
            if (selectedEquipment.value == eq) selectedEquipment.value = null;
            else selectedEquipment.value = eq;
        }
    }
}