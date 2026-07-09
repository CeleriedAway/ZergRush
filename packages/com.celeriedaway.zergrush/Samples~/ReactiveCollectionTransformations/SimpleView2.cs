using UnityEngine;
using UnityEngine.UI;
using ZergRush.ReactiveUI;

namespace Demo.ReactiveCollectionTransformations
{
    public class SimpleView2 : ReusableView
    {
        public new Text name;
        
        public override void OnRecycle()
        {
            tr.localScale = Vector3.one;
        }
    }
}