using System.Collections.Generic;
using ZergRush.CodeGen;

namespace ZergRush.Alive
{
    [GenZergRushFolder(), GenTask(GenTaskFlags.LivableNodePack & ~GenTaskFlags.PolymorphicConstruction), GenTaskCustomImpl(GenTaskFlags.LifeSupport)]
    public abstract partial class LivableRoot : Livable
    {
        [GenIgnore] bool alive;
        // Ignore hierarchy side effects during updatefrom.
        [GenIgnore] public bool __updating;
        [GenIgnore] public string __debugTag;

        // You should call this only when otherData is loaded from somewhere.
        public virtual void RootUpdateFromPartial<T>(T selfData, T otherData) where T : Livable
        {
            otherData.root = this;
            __updating = true;
            selfData.UpdateFrom(otherData);
            __updating = false;
        }

        public virtual void RootUpdateFrom(LivableRoot other, ZRUpdateFromHelper __helper)
        {
            var self = this;
            __helper.TryLoadAlreadyUpdated(other, ref self);
            __updating = true;
            UpdateFrom(other, __helper);
            __updating = false;
        }

        public virtual void EnliveWorld()
        {
            if (!alive)
                Enlive();
        }

        public new bool isAlive => alive;

        public virtual void MortifyWorld()
        {
            // Can be uncommented to test performance gain in multiplayer tests.
            //Mortify();
        }

        protected override void EnliveSelf()
        {
            alive = true;
        }

        protected override void MortifySelf()
        {
            alive = false;
        }

        public override void Enlive() 
        {
            EnliveSelf();
            EnliveChildren();
        }

        public override void Mortify() 
        {
            MortifySelf();
            MortifyChildren();
        }
    }
}
