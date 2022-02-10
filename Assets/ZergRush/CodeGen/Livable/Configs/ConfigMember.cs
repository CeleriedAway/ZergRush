namespace ZergRush.Alive
{
    using System;
    using CodeGen;
    
    /// <summary>
    /// Base class for all config members.
    /// Inheritors must defile one or more fields with [UIDComponent] to check equality of config members.
    /// Usually, it`s something unique like "string id" field.
    /// See example below.
    /// </summary>
    [GenInLocalFolder, Immutable, GenTaskCustomImpl(GenTaskFlags.UIDGen)]
    [GenTask((GenTaskFlags.ConfigData | GenTaskFlags.UIDGen) & ~GenTaskFlags.PolymorphicConstruction)]
    public partial class LoadableConfig : IUniquelyIdentifiable
    {
        public ulong id => UId();
        public virtual ulong UId() => 0;
    }
    
    /// <summary>
    /// An attribute used to define which config is responsible for storing config members for this hierarchy.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigRootType : Attribute
    {
        public Type type;
        public ConfigRootType(Type type) => this.type = type;
    }
    
    #region Example
    
    [ConfigRootType(typeof(GameConfigExample)), GenInLocalFolder]
    public partial class SomeItemFromConfig : LoadableConfig
    {
        [UIDComponent]
        public string id;

        public int price;
        public string name;
    }
    
    #endregion
}