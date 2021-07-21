using System;
using UnityEngine;
using ZergRush.ReactiveCore;
using ZergRush.ReactiveUI;

namespace Demo.TablesAndLayouts
{
    class TablesAndLayouts : ConnectableMonoBehaviour
    {
        public SimpleView prefab;
    
        public ReactiveScrollRect vertical;
        public ReactiveScrollRect horizontal;
        public ReactiveScrollRect gridVertical;
        public ReactiveScrollRect gridHorizontal;
        public ReactiveScrollRect variableVertical;
        public ReactiveScrollRect variableHorizontal;

        void Start()
        {
            var data = TestData.instance.data;
        
            Action<Cell<int>, SimpleView> factory = (i, view) => {
                view.connections += i.Bind(val => view.name.text = val.ToString());
            };

            // Linear layout
            // Default layout is vertical with some small margins, often good enough
            connections += data.PresentInScrollWithLayout(vertical, prefab, factory,
                delegates: Animations.Default<SimpleView>());
            
            // Layout settings can be specified with explicit layout construction
            connections += data.PresentInScrollWithLayout(horizontal, prefab, factory,
                layout: Rui.LinearLayout(LayoutDirection.Horizontal),
                delegates: Animations.Default<SimpleView>());

            // Grid layout
            connections += data.PresentInScrollWithLayout(gridHorizontal, prefab, factory,
                layout: Rui.GridLayout(3, LayoutDirection.Horizontal), 
                delegates: Animations.Default<SimpleView>()
            );

            connections += data.PresentInScrollWithLayout(gridVertical, prefab, factory,
                layout: Rui.GridLayout(7), 
                delegates: Animations.Default<SimpleView>()
            );

            // Variable view size layout
            Func<Cell<int>, float> sizeFactory = item => 100 + item.value * 10;
            Action<Cell<int>, SimpleView> variableSizeViewFactory = (i, view) =>
            {
                var size = sizeFactory(i);
                view.rectTransform.sizeDelta = new Vector2(size, size);
                factory(i, view);
            };

            connections += data.PresentInScrollWithLayout(variableHorizontal, prefab, variableSizeViewFactory,
                layout: Rui.VariableViewSizeLayout(data, sizeFactory, new LinearLayoutSettings{direction = LayoutDirection.Horizontal}),
                delegates: Animations.Default<SimpleView>()
            );

            connections += data.PresentInScrollWithLayout(variableVertical, prefab, variableSizeViewFactory,
                layout: Rui.VariableViewSizeLayout(data, sizeFactory, new LinearLayoutSettings{}),
                delegates: Animations.Default<SimpleView>()
            );
        }
    }
}
