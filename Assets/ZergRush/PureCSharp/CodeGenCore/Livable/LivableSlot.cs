﻿using System;
using ZergRush.Alive;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Alive
{
    [GenTaskCustomImpl(GenTaskFlags.NodePack), GenZergRushFolder()]
    public sealed partial class DataSlot<TLivable> : DataNode, ICell<TLivable>, IConnectable  where TLivable : DataNode
    {
        [CanBeNull] TLivable _value;

        public TLivable value
        {
            get { return _value; }
            set
            {
                if (_value == value) return;

                this._value = value;

                if (_value != null)
                {
                    if (_value.root != root)
                    {
                        _value.root = root;
                        _value.__PropagateHierarchyAndRememberIds();
                    }
                    _value.SetRootAndCarrier(root, carrier);
                }

                if (up != null)
                    up.Send(_value);
            }
        }

        [GenIgnore] private EventStream<TLivable> up;
        public IDisposable ListenUpdates(Action<TLivable> reaction)
        {
            if (up == null) up = new EventStream<TLivable>();
            return up.Subscribe(reaction);
        }

        public override void __PropagateHierarchyAndRememberIds()
        {
            if (_value != null && _value.root != root)
            {
                _value.SetRootAndCarrier(root, carrier);
                _value.__PropagateHierarchyAndRememberIds();
            }
        }

        public int getConnectionCount => up != null ? up.getConnectionCount : 0;
    }

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
                    _value.__ForgetIds();
                    if (isAlive) _value.Mortify();
                    if (root != null)
                    {
                        _value.ReturnToPool(root.pool);
                    }
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
                        _value.__PropagateHierarchyAndRememberIds();
                    }
                    
                    if (isAlive)
                    {
                        EnliveValue();
                    }

                    if (!__update_mod)
                    {
                        _value.OnInsertedIntoHierarchy(_value.staticConnections);
                    }
                }
            }
        }

        public override void __PropagateHierarchyAndRememberIds()
        {
            if (_value != null && _value.root != root)
            {
                _value.SetRootAndCarrier(root, carrier);
                _value.__PropagateHierarchyAndRememberIds();
            }
        }

        public override void __ForgetIds()
        {
            if (_value != null)
            {
                _value.__ForgetIds();
            }
        }

        public void OnReturnToPool(ObjectPool pool)
        {
            _value?.ReturnToPool(pool);
            _value = null;
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