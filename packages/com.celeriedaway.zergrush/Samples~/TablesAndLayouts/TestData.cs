using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ZergRush;
using ZergRush.ReactiveCore;

namespace Demo.TablesAndLayouts
{
    public class TestData : MonoBehaviour {

        public ReactiveCollection<Cell<int>> data = new ReactiveCollection<Cell<int>>();

        public static TestData instance;
        public Text printCollection;
        public bool chaoticChange;

        void Awake()
        {
            instance = this;
        }

        void Start()
        {
            for (int i = 0; i < 50; i++)
            {
                data.Add(new Cell<int>(i));
            }
        
            var _ = data.AsCell().Bind(_ => PrintCollection());
        
            // Random change
            if (chaoticChange) StartCoroutine(TestRemove());
        }
    
        void PrintCollection()
        {
            if (printCollection != null)
                printCollection.text = string.Concat(data.Select(v => " " + v + " ").ToArray());
        }
    
        IEnumerator TestRemove()
        {
            Func<int> random = () => UnityEngine.Random.Range(0, data.Count);
            Action removeRandom = () => data.RemoveAt(random());
            Action insertRandom = () => {
                var i = random();
                data.Insert(i, new Cell<int>(i));
            };
            Action correct = () =>
            {
                data.ForeachWithIndices((item, i) => item.value = i);
                PrintCollection();
            };

            const float mult = 4f;
            while (true)
            {
                removeRandom();
                correct();
                yield return new WaitForSeconds(0.5f * mult);
                insertRandom();
                correct();
                yield return new WaitForSeconds(0.5f * mult);
                insertRandom();
                insertRandom();
                insertRandom();
                correct();
                yield return new WaitForSeconds(0.5f * mult);
                removeRandom();
                removeRandom();
                removeRandom();
                correct();
            }
        }
    }
}
