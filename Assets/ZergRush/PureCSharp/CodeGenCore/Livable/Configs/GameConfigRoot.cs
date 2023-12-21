﻿namespace ZergRush.Alive
{
    using System;
    using CodeGen;
    using System.IO;
    using Newtonsoft.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a top level container for configs.
    /// Inherit that class and pass inherited type as T if u need to create global config container.
    /// See examples below.
    /// </summary>
    /// <typeparam name="T">Inherited type</typeparam>
    [GenTask(GenTaskFlags.ConfigData & ~GenTaskFlags.PolymorphicConstruction), GenZergRushFolder()]
    public abstract partial class GameConfigRoot<T> : IBinarySerializable, IBinaryDeserializable where T : GameConfigRoot<T>, new()
    {
        /// <summary>
        /// Your config container.
        /// </summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// Storage of all config members.
        /// </summary>
        [GenIgnore] public ConfigRegister allConfigs = new ConfigRegister();

        /// <summary>
        /// Registers config member in config storage.
        /// Registered config can be retrieved from "allConfigs" dictionary by id.
        /// You should manually call that method for each config member you need to create manually.
        /// </summary>
        public void RegisterConfig(LoadableConfig config)
        {
            if (config.UId() == 0)
                throw new ZergRushException(
                    $"Config member {config.GetType()} should mark fields with {nameof(UIDComponent)} tag." +
                    "Usually it`s something like \"string id\" field.");

            if (allConfigs.ContainsKey(config.UId()))
                throw new ZergRushException(
                    $"Two config entities of type {config} and {allConfigs[config.UId()]} have a similar uid {config.UId()}. " +
                    $"{nameof(UIDComponent)} should mark only unique identifier fields.");

            allConfigs[config.UId()] = config;
        }


        public static TConf GetConfig<TConf>(ulong uid) where TConf : IUniquelyIdentifiable => (TConf) GetConfig(uid);
        public static TConf GetConfigIfAny<TConf>(ulong uid, TConf ifNotFound) where TConf : class, IUniquelyIdentifiable => TryGetConfig(uid) as TConf ?? ifNotFound;

        /// <summary>
        /// Retrieves config member from ConfigsRegister by id.
        /// Used to deserialize references to config member instances.
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static IUniquelyIdentifiable GetConfig(ulong uid)
        {
            if (Instance.allConfigs.TryGetValue(uid, out var c))
            {
                return c;
            }
            throw new ZergRushException(
                $"uid:{uid} is not registerred in game config, check that this configs are contained in" +
                $" ConfigStorageList or Dict in somewhere in game config and is loaded earlier then required id");
        }

        public static IUniquelyIdentifiable TryGetConfig(ulong uid)
        {
            if (Instance.allConfigs.TryGetValue(uid, out var c))
            {
                return c;
            }
            return null;
        }

        /// <summary>
        /// Executes actions to fulfill game config with members.
        /// </summary>
        public static void SetGameConfig(Action<T> fillConfig)
        {
            Instance = new T();
            fillConfig(Instance);
        }
        /// <summary>
        /// Executes actions to fulfill game config with members.
        /// </summary>
        public static void SetGameConfig<TConfig>(Action<T> fillConfig) where TConfig : T , new()
        {
            Instance = new TConfig();
            fillConfig(Instance);
        }

        /// <summary>
        /// Executes async actions to fulfill game config with members.
        /// </summary>
        public static async Task SetGameConfig(Func<T, Task> fillInstance)
        {
            Instance = new T();
            await fillInstance(Instance);
        }

        public static void LoadFrom(ZRBinaryReader reader)
        {
            Instance = new T();
            Instance.Deserialize(reader);
        }

        public static void LoadFrom(ZRJsonTextReader reader)
        {
            Instance = new T();
            Instance.ReadFromJson(reader);
        }
    }
}