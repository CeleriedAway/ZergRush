using System;
using ZergRush.Alive;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Alive
{
    [GenTaskCustomImpl(GenTaskFlags.LivableNodePack), GenZergRushFolder()]
    public sealed partial class LivableSlot<TLivable> : Livable, ICell<TLivable> where TLivable : Livable
    {
        [CanBeNull] TLivable _value;
        EventStream<TLivable> update;

        // need to distinct normal runtime set and set during updatefrom and deserialization;
        public bool __update_mod;
        
        public void ClearValue()
        {
            value = null;
        }
                
        public override void Enlive()
        {
            EnliveSelf();
            EnliveValue();
        }

        public override void Mortify()
        {
            MortifySelf();
            _value?.Mortify();
        }

        void EnliveValue()
        {
            if (_value == null) return;
            _value.Enlive();
        }

        public IDisposable ListenUpdates(Action<TLivable> reaction)
        {
            if (update == null) update = new EventStream<TLivable>();
            return update.Subscribe(reaction);
        }

        public TLivable value
        {
            get { return _value; }
            set
            {
                if (object.ReferenceEquals(value, _value)) return;
                
                if (_value != null)
                {
                    if (isAlive) _value.Mortify();
                    if (!__update_mod)
                    {
                        _value.Destroy();
                    }
                }

                this._value = value;
                update?.Send(_value);

                if (_value != null)
                {
                    if (_value.isAlive)
                    {
                        throw new ZergRushException("alive value came into livable slot");
                    }

                    if (root != null)
                    {
                        _value.SetRootAndCarrier(root, carrier);
                        _value.__PropagateHierarchy();
                    }
                    
                    if (isAlive)
                    {
                        EnliveValue();
                    }

                    if (!__update_mod)
                    {
                        _value.OnInsertedIntoHierarchy();
                    }
                }
            }
        }

        public override void __PropagateHierarchy()
        {
            if (_value != null && _value.root != root)
            {
                _value.SetRootAndCarrier(root, carrier);
                _value.__PropagateHierarchy();
            }
        }

        public void TransplantTo(LivableSlot<TLivable> otherSlotOfSameParent)
        {
            var temp = _value;
            _value = null;
            otherSlotOfSameParent._value = temp;
        }

        public override void VisitNode(Action<object> action)
        {
            if (_value != null)
            {
                _value.VisitNode(action);
            }
        }
    }
}
