using UnityEngine.UI;
using ZergRush.ReactiveUI;

namespace Demo.CellDemo
{
    public class UnitView : ReusableView
    {
        public Text attack;
        public Text defence;
        public Text hp;

        public Button upgradeButton;
        public Button viewClickButton;

        public Image selectedCheckbox;
    }
}