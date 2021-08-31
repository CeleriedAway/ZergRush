using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace ZergRush.ReactiveCore
{
    public abstract class Modifiable<TVal, TModification> : ICell<TVal>
    {
        public Modifiable(TVal baseVal)
        {
            this.baseVal = baseVal;
            this.currVal = baseVal;
        }

        protected TVal baseVal;
        protected TVal currVal;
        protected List<TModification> modifications = new List<TModification>();
        EventStream<TVal> changed = new EventStream<TVal>();

        public TVal baseValue
        {
            get { return baseVal; }
            set
            {
                baseVal = value;
                Update();
            }
        }

        private void AddModification(TModification mod)
        {
            modifications.Add(mod);
            Update();
        }

        private void RemoveModification(TModification mod)
        {
            modifications.Remove(mod);
            Update();
        }

        private void ReplaceModification(TModification modOld, TModification modNew)
        {
            var index = modifications.FindIndex(m => EqualityComparer<TModification>.Default.Equals(m, modOld));
            modifications[index] = modNew;
            Update();
        }

        private Action ModifyRaw(TModification mod)
        {
            AddModification(mod);
            return () =>
            {
                RemoveModification(mod);
            };
        }

        public IDisposable Modify(TModification mod)
        {
            return ModifyRaw(mod).ToDisposable();
        }

        public void Modify(IConnectionSink connections, TModification mod)
        {
            connections.AddConnection(Modify(mod));
        }

        public IDisposable Modify(ICell<TModification> cellMod)
        {
            //Debug.Log($"cell({cellMod.GetHashCode()}) value:{cellMod.value} Connected to {GetHashCode()}}}");
            AddModification(cellMod.value);
            var disp = new DoubleDisposable();
            disp.First = new AnonymousDisposable(() => RemoveModification(cellMod.value));
            disp.Second = cellMod.BufferListenUpdates((newVal, oldVal) =>
            {
                //Debug.Log($"cell({cellMod.GetHashCode()}) value replaced from:{oldVal} to:{newVal} Connected to {GetHashCode()}}}");
                ReplaceModification(oldVal, newVal);
            });
            return disp;
        }

        public void Modify(IConnectionSink connections, ICell<TModification> cellMod)
        {
            connections.AddConnection(Modify(cellMod));
        }

        void Update()
        {
            var newVal = Calculate();
            //Debug.Log($"{GetHashCode()} updated to {newVal}");
            if (EqualityComparer<TVal>.Default.Equals(newVal, currVal) == false)
            {
                currVal = newVal;
                changed.Send(currVal);
            }
        }

        protected abstract TVal Calculate();

        public IDisposable ListenUpdates(Action<TVal> reaction)
        {
            return changed.Subscribe(reaction);
        }

        public TVal value
        {
            get { return currVal; }
        }
    }

    public static class ModifiableTools
    {
        public static IDisposable ToDisposable(this Action action)
        {
            return new AnonymousDisposable(action);
        }
    }

    public class LastValueModification<T> : Modifiable<T, T>
    {
        public LastValueModification(T baseVal) : base(baseVal)
        {
        }

        protected override T Calculate()
        {
            if (modifications.Count > 0)
                return modifications[modifications.Count - 1];
            else
                return baseVal;
        }
    }

    public class MultiplicativeModification : Modifiable<float, float>
    {
        public MultiplicativeModification() : base(1f)
        {
        }

        protected override float Calculate()
        {
            var result = baseVal;
            for (var i = 0; i < modifications.Count; i++)
            {
                result *= modifications[i];
            }

            return result;
        }
    }

    public class OrModification : Modifiable<bool, bool>
    {
        protected override bool Calculate()
        {
            for (var i = 0; i < modifications.Count; i++)
            {
                if (modifications[i]) return true;
            }

            return false;
        }

        public OrModification() : base(false)
        {
        }
    }

    public class AdditiveModification : Modifiable<float, float>
    {
        public AdditiveModification() : base(0)
        {
        }

        public AdditiveModification(float val) : base(val)
        {
        }

        protected override float Calculate()
        {
            var result = baseVal;
            for (var i = 0; i < modifications.Count; i++)
            {
                result += modifications[i];
            }

            return result;
        }
    }

    public class AdditiveIntegerModification : Modifiable<int, int>
    {
        public AdditiveIntegerModification() : base(0)
        {
        }

        public AdditiveIntegerModification(int val) : base(val)
        {
        }

        protected override int Calculate()
        {
            var result = baseVal;
            for (var i = 0; i < modifications.Count; i++)
            {
                result += modifications[i];
            }

            return result;
        }
    }

    public class ModifiableList<T> : IReactiveCollection<T>, IReadOnlyList<T>
    {
        ReactiveCollection<T> collection = new ReactiveCollection<T>();

        public IDisposable ModifyAdd(T elem)
        {
            collection.Add(elem);
            return new AnonymousDisposable(() => collection.Remove(elem));
        }

        public void ModifyAdd(IConnectionSink sink, T elem)
        {
            collection.Add(elem);
            sink.AddConnection(new AnonymousDisposable(() =>
            {
                //Debug.Log("removing...");
                collection.Remove(elem);
            }));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) collection).GetEnumerator();
        }

        public IEventStream<IReactiveCollectionEvent<T>> update => collection.update;
        public int Count => collection.Count;
        public T this[int index] => collection[index];
    }
}