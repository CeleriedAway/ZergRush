﻿namespace ZergRush.Alive
{
    using System;
    using System.IO;
    using CodeGen;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a top level container for configs.
    /// Inherit that class and pass inherited type as T if u need to create global config container.
    /// See examples below.
    /// </summary>
    /// <typeparam name="T">Inherited type</typeparam>
    [GenInLocalFolder]
    public abstract partial class GameConfigBase<T> : __GameConfigBase where T : GameConfigBase<T>, new()
    {
        /// <summary>
        /// Your config container.
        /// </summary>
        public static T Instance { get; private set; }
        
        /// <summary>
        /// Storage of all config members.
        /// </summary>
        [GenIgnore] public ConfigRegister allConfigs;
        
        /// <summary>
        /// Registers config member in config storage.
        /// Registered config can be retrieved from "allConfigs" dictionary by id.
        /// You should manually call that method for each config member if you need to create config members manually.
        /// </summary>
        public void RegisterConfig(LoadableConfig config)
        {
            if (config.UId() == 0)
                throw new ZergRushException($"Config entity {config.GetType()} should mark fields with {nameof(UIDComponent)} tag." +
                    "Usually it`s an \"id\" field.");
            
            if (allConfigs.ContainsKey(config.UId()))
                throw new ZergRushException($"Two config entities of type {config.GetType()} have a similar uid {config.UId()}. " +
                    $"{nameof(UIDComponent)} should mark only an unique identifier fields.");

            allConfigs[config.UId()] = config;
        }
        
        /// <summary>
        /// Retrieves config member from ConfigsRegister by id.
        /// Used to deserialize references to config member instances.
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static IUniquelyIdentifiable GetConfig(ulong uid)
        {
            return Instance.allConfigs[uid];
        }
        
        /// <summary>
        /// Executes actions to fulfill game config with members.
        /// </summary>
        public void SetGameConfig(Action<T> fillConfig)
        {
            Instance = new T();
            fillConfig(Instance);
        }
        
        /// <summary>
        /// Executes async actions to fulfill game config with members.
        /// </summary>
        public async Task SetGameConfig(Func<T, Task> fillInstance)
        {
            Instance = new T();
            await fillInstance(Instance);
        }
        
        public void LoadFrom(BinaryReader reader)
        {
            Instance = new T();
            Instance.Deserialize(reader);
        }

        public void LoadFrom(JsonTextReader reader)
        {
            Instance = new T();
            Instance.ReadFromJson(reader);
        }
    }

    #region Example
    
    [GenInLocalFolder]
    public partial class GameConfigExample : GameConfigBase<GameConfigExample>
    {
        public ConfigStorageList<SomeItemFromConfig> items;
    }
    
    #endregion
    
    #region Stub
    /// <summary>
    /// DO NOT USE THAT CLASS.
    /// YOU DONT NEED IT, but
    /// it`s just a stub to gen polymorphic constructions hierarchy for generic GameConfigBase<T> class
    /// </summary>
    [GenConfigData, GenTask(GenTaskFlags.ConfigData), GenInLocalFolder]
    public partial class __GameConfigBase { }
    #endregion
}