using ZergRush.CodeGen;

namespace ZergRush.Alive
{
    [GenInLocalFolder, GenTask(GenTaskFlags.LivableNodePack & ~GenTaskFlags.PolymorphicConstruction), GenTaskCustomImpl(GenTaskFlags.LifeSupport)]
    public abstract partial class LivableRoot : DataRoot, ILivable
    {
        [GenIgnore] bool alive;
        public virtual void EnliveWorld()
        {
            if (!alive)
                Enlive();
        }

        public bool isAlive => alive;

        public virtual void MortifyWorld()
        {
            // Can be uncommented to test performance gain in multiplayer tests
            //Mortify();
        }

        public virtual void EnliveSelf()
        {
            alive = true;
        }

        public virtual void MortifySelf()
        {
            alive = false;
        }

        public virtual void Enlive() 
        {
            EnliveSelf();
            EnliveChildren();
        }
        public virtual void Mortify() 
        {
            MortifySelf();
            MortifyChildren();
        }
        protected virtual void EnliveChildren() 
        {

        }
        protected virtual void MortifyChildren() 
        {

        }
    }
    
    public interface IDataRootWithStep
    {
        int step { get; }
    }
}

