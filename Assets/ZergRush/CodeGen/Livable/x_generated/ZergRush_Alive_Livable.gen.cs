using System;
using System.Collections.Generic;
using System.Text;
using ZergRush.Alive;
using System.IO;
using Newtonsoft.Json;
#if !INCLUDE_ONLY_CODE_GENERATION
namespace ZergRush.Alive {

    public partial class Livable : IUpdatableFrom<ZergRush.Alive.Livable>, IUpdatableFrom<ZergRush.Alive.DataNode>, IHashable, ICompareChechable<ZergRush.Alive.DataNode>, IJsonSerializable
    {
        public void UpdateFrom(ZergRush.Alive.Livable other) 
        {
            throw new NotImplementedException();
        }
        public virtual void Enlive() 
        {
            throw new NotImplementedException();
        }
        public virtual void Mortify() 
        {
            throw new NotImplementedException();
        }
        protected virtual void EnliveChildren() 
        {
            throw new NotImplementedException();
        }
        protected virtual void MortifyChildren() 
        {
            throw new NotImplementedException();
        }
        public  Livable() 
        {
            throw new NotImplementedException();
        }
    }
}
#endif
