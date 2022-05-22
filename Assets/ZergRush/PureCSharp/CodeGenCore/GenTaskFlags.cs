using System;

namespace ZergRush.CodeGen
{
    [Flags]
    public enum GenTaskFlags
    {
        None = 0,
        Hash = 2 << 0,
        UpdateFrom = 2 << 1,
        LifeSupport = 2 << 2,
        DefaultConstructor = 2 << 3,
        CompareChech = 2 << 4,
        OwnershipHierarchy = 2 << 5,
        PooledUpdateFrom = 2 << 6,
        Deserialize = 2 << 8,
        Serialize = 2 << 9,
        Pooled = 2 << 10,
        PooledDeserialize = 2 << 11,

        PolymorphicConstruction = 2 << 12,
        PooledPolymorphicConstruction = 2 << 13,
        JsonSerialization = 2 << 14,
        RPC = 2 << 15,

        UIDGen = 2 << 16,
        CollectConfigs = 2 << 17,
        LocalCommands = 2 << 18,

        Serialization = Deserialize | Serialize,
        SimpleDataPack = DefaultConstructor | UpdateFrom | Hash | CompareChech | JsonSerialization | Serialization,

        PolymorphicDataPack = SimpleDataPack | PolymorphicConstruction,
        NodePack = PolymorphicDataPack | OwnershipHierarchy,
        LivableNodePack = NodePack | LifeSupport,

        PooledDataPack = DefaultConstructor | Hash | CompareChech | JsonSerialization | Pooled | PooledUpdateFrom |
                         PooledPolymorphicConstruction | PooledDeserialize,
        PooledNodePack = PooledDataPack | OwnershipHierarchy,
        PooledLivableNodePack = PooledNodePack | LifeSupport,

        ConfigData = Hash | Serialization | JsonSerialization | DefaultConstructor | PolymorphicConstruction |
                     CollectConfigs,

        All = 0xfffffff
    }
}