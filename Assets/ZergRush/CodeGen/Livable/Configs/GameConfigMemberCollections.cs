namespace ZergRush.Alive
{
    using System.Collections.Generic;

    /// <summary>
    /// Global storage for retrieving all config members by id.   
    /// </summary>
    public class ConfigRegister : Dictionary<ulong, IUniquelyIdentifiable> {}
    
    /// <summary>
    /// List to force direct serialization of its members instead of reading from config.
    /// By default, all config members are deserialized by reading it from ConfigRegistry in GameConfigBase.
    /// Only config itself serialize members as data.
    /// </summary>
    public class ConfigStorageList<T> : List<T> where T : LoadableConfig {}
    
    /// <summary>
    /// Dictionary to force direct serialization of its members instead of reading from config.
    /// By default, all config members are deserialized by reading it from ConfigRegistry in GameConfigBase.
    /// Only config itself serialize members as data.
    /// </summary>
    public class ConfigStorageDict<TKey, T> : Dictionary<TKey, T> where T : LoadableConfig {}
}