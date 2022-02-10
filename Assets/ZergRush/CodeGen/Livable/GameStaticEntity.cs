using System;
using ZergRush.CodeGen;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZergRush.Alive
{
    [GenTask(GenTaskFlags.ConfigData), ConfigRootType(typeof(GameConfigExample)), GenInLocalFolder]
    public partial class GameLoadableConfigExample : LoadableConfig
    {
        [UIDComponent]
        public string uid;
    }

    [GenInLocalFolder]
    public partial class GameConfigExample : GameConfigBase<GameConfigExample>
    {
        public ConfigStorageList<GameLoadableConfigExample> examples;
    }

    [GenInLocalFolder]
    public abstract partial class GameConfigBase<T> : GameConfigBaseBase where T : GameConfigBase<T>, new()
    {
        public static T Instance { get; private set; }

        [GenIgnore] public ConfigRegister allConfigs;

        public void RegisterConfig(LoadableConfig config)
        {
            if (config.UId() == 0)
                throw new ZergRushException($"Config entity {config.GetType()} should mark fields with {nameof(UIDComponent)} tag." +
                                            "Usually it`s an \"id\" field.");
            
            if (allConfigs.ContainsKey(config.UId()))
                throw new ZergRushException($"Two config entities of type {config.GetType()} have a similar uid {config.UId()}. "
                                          + $"{nameof(UIDComponent)} should mark only an unique identifier fields.");

            allConfigs[config.UId()] = config;
        }
        
        public static IUniquelyIdentifiable GetConfig(ulong uid)
        {
            return Instance.allConfigs[uid];
        }

        public void SetGameConfig(Action<T> fillConfig, Action<string> writeLog)
        {
            Instance = new T();
            fillConfig(Instance);
            writeLog("Game config loaded");
        }

        public async Task SetGameConfig(Func<T, Task> fillInstance, Action<string> writeLog)
        {
            Instance = new T();
            await fillInstance(Instance);
            writeLog("Game config loaded");
        }
    }
    
    [GenConfigData, GenTask(GenTaskFlags.ConfigData), GenInLocalFolder]
    public partial class GameConfigBaseBase { }
    
    [GenTask((GenTaskFlags.ConfigData | GenTaskFlags.UIDGen) & ~GenTaskFlags.PolymorphicConstruction), GenInLocalFolder, Immutable, GenTaskCustomImpl(GenTaskFlags.UIDGen)]
    public partial class LoadableConfig : IUniquelyIdentifiable
    {
        public ulong id => UId();
        public virtual ulong UId() => 0;
    }

    [GenTask((GenTaskFlags.ConfigData | GenTaskFlags.UIDGen) & ~GenTaskFlags.PolymorphicConstruction), GenInLocalFolder]
    public partial class StubTypeBasedDataFromConfig : LoadableConfig
    {
    }
    
    public class ConfigRegister : Dictionary<ulong, IUniquelyIdentifiable> {}
    
    // list that force direct serialization of its members instead of reading from config
    public class ConfigStorageList<T> : List<T> {}
    public class ConfigStorageDict<TKey, T> : Dictionary<TKey, T> {}
    
    //[GenHashing]
}
