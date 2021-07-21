using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ZergRush.ReactiveCore;
using ZergRush.ReactiveUI;
using Random = UnityEngine.Random;

namespace Demo.ReactiveCollectionTransformations
{
    public class ReactiveCollectionTransformations : ConnectableMonoBehaviour
    {
        ReactiveCollection<int> data = new ReactiveCollection<int>();

        public SimpleView2 prefab;

        public ReactiveScrollRect initial;
        public ReactiveScrollRect mapped;
        public ReactiveScrollRect filtered;

        public Button insertButton;
        public Button removeButton;

        void Start()
        {
            data.Reset(Enumerable.Range(0, 5).Select(_ => Random.Range(0, 20)));
            insertButton.ClickStream().Subscribe(() => data.Insert(Random.Range(0, data.Count), Random.Range(0, 20)));
            removeButton.ClickStream().Subscribe(() =>
            {
                if (data.Count > 0)
                    data.RemoveAt(Random.Range(0, data.Count));
            });

            var settings = new LinearLayoutSettings()
            {
                direction = LayoutDirection.Horizontal,
                marginVec = new Vector2(30, 30),
                topShift = 30,
                bottomShift = 30,
            };

            Action<int, SimpleView2> factory = (i, view) => { view.name.text = i.ToString(); };

            connections += data.PresentInScrollWithLayout(initial, prefab, factory, layout: Rui.LinearLayout(settings),
                delegates: Animations.Default<SimpleView2>());

            var mappedData = data.Map(val => (val * 3 + 5) % 10);

            connections += mappedData.PresentInScrollWithLayout(mapped, prefab, factory,
                layout: Rui.LinearLayout(settings), delegates: Animations.Default<SimpleView2>());

            var filteredData = mappedData.Filter(val => val % 2 == 0);

            connections += filteredData.PresentInScrollWithLayout(filtered, prefab, factory,
                layout: Rui.LinearLayout(settings), delegates: Animations.Default<SimpleView2>());
        }
    }
}