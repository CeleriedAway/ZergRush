# ZergRush
Unity package that represents C# reactive library and set of tools for Unity development.

This toolset consists of several parts:

1. ZergRush.Reactive is a reactive library designed to be better alternative to UniRX. Main goal is to manage complex logic dependancies of game/UI state and all sorts of "state-observers". Consists of following parts:
	* EventStream<T> - Concept that represents some event stream in time that can be observed, kind of replacement for "event" and actions chaining.
	* Cell<T> aka "reactive property" is a concept of value that changes over time and can be observed
	* ReactiveCollection<T> is kind of Cell<T> for collections, with detailed information of changes inside of it.
	* Dozens of tools for transformation and manipulating those primitives.
	* ReactiveUI - set of tools allowing to present various forms of reactive data withing unity environment

2. ZergRush.CodeGen is a code generation tools that allows to add huge range of various functionality for your data models, for example it can add:
	* Fast Binary and Json serialization options with support to abstract classes, generics ect...
	* Optimized deep copy functionality
	* Fast hash calculations
	* Finding differences between different data models
	* Default constructor generation
	* Advanced tools that allow to create complex game models with ids, config integration, hierarchy propagation ect..

3. ZergRush.Utils collections of variuos tools for common gamedev tasks and extensions for above libraries.

# Installation

For now installation process is the following:

Use Package Manager > + (button) > Add package from git URL...

If you do not have Newtonsoft.Json-for-Unity in your project first install this:
```
https://github.com/jilleJr/Newtonsoft.Json-for-Unity.git#upm
```

Then install this package with url:
```
https://github.com/CeleriedAway/ZergRush.git
```


# ZergRush.Reactive

The first part of ZergRush.Reactive is EventStream
```c#
public interface IEventStream<out T> : IEventStream
{
    IDisposable Subscribe(Action<T> callback);
}
```
As you see it represents simple event that you can subscribe to get its updates. Usually your action would not be called during subsribtions process. The main implementation EventStream allows to "Send()" events.

Subscribe method return IDisposable "Connection" object. If disposed then this connections is not valied anymore and initial callback wont be called after that.
One of the best usecase for this is a collecting of connection objects for automatically disposion, so you won't forget to "unsubscribe"

```c#
class DogModel
{
	EventStream injured;
	EventStream<string> spokeSomething;
}

class MyView : ConnectableMonoBehaviour
{
	void Start()
	{
		// those connections is just a list of IDisposables that will be auto-disposed in OnDestroy callback
		connections += dogModel.injured.Subscribe(ShowBloodSpashes);

		// Subscribe extension allowing pass connections as first argument
		dogModel.spokeSomething.Subscribe(connections, ShowTextBubble);
	}
	void ShowBloodSpashes() {...}
	void ShowTetBubble(string text) {...}
}
```

EventStreams can be Filtered, Transformed, and Merged. Like this..
```c#
// The only real stream allowing to send values
EventStream<int> streamOfNumbers = new EventStream<int>();
// Subscribe to this stream and you will receive only event number events sent with streamOfNumbers.
IEventStream<int> streamOfEvenNumbers = streamOfNumbers.Filter(i => IsEven(i));
// Same with odd numbers.
IEventStream<int> streamOfOddNumbers = streamOfNumbers.Filter(i => IsOdd(i));
// Here you will receive strings created from numbers
IEventStream<string> streamOfSomeStrings = streamOfEvenNumbers.Map(i => i.ToString());
// Here you will receive events from both streams
IEventStream<int> mergedStreamOfNumbers = streamOfEvenNumbers.MergeWith(streamOfOddNumbers);
```

But the most usefull part of the library in game development is Cell<T>
```c#
public interface ICell<out T> 
{
    IDisposable ListenUpdates(Action<T> reaction);
    T value { get; }
}	
```

Basic api for cells is:
```c#
var moneyCount = new Cell<int>();
moneyCount.ListenUpdates(v => Debug.Log($"Money changed to {v}"));
moneyCount.value = 10;
// It is important that all cell api guerantee that setting same value won't trigger update callback
moneyCount.value = 10; // Log won't be trigered

var imRich = moneyCount.Select(m => m > 100);
```


It represents a value that is changing in time. And a perfect fit for representing game data.
```c#
        class Item
        {
            public Cell<int> hpBonus;
            public void Upgrade() => hpBonus.value++;
        }

        class Weapon
        {
            public Cell<int> damage;
        }

        partial class PlayerData
        {
            public Cell<int> money;
            public Cell<int> hp;
            public Cell<int> baseMaxHp;

            public void LevelUp()
            {
                baseMaxHp.value++;
                hp.value = maxHpTotal.value;
            }

            public ReactiveCollection<Item> items;
            public Cell<Weapon> selectedWeapon;
            public void EquipWeapon(Weapon w) => selectedWeapon.value = w; 
            
            // Join is the most important operator that transforms ICell<ICell<T>> to just ICell<T>
            // That is the core mechanic of cell transformation and
            // the one that allows to collapse all dependency layers into one
            public ICell<int> damage => selectedWeapon.Map(w => w.damage).Join();

            // you can use transform api to compose new properties without loosing of its dependancies
            // Look how total maxHp is calculated 
            public ICell<int> maxHpFromItems => items.Map(i => i.hpBonus).ToCellOfCollection().Map(itemBuffs => itemBuffs.Sum());
            public ICell<int> maxHpTotal => baseMaxHp.Merge(maxHpFromItems, (hp1, hp2) => hp1 + hp2);
            public ICell<float> relativeHp => hp.Merge(maxHpTotal, (hp, maxHp) => hp / (float) maxHp);
            public ICell<bool> isWounded => relativeHp.Select(value => value < 0.5f);
            
        }

```

# ZergRush.CodeGen

I hope this code is clear enough

```c#
    /*
     * To generate code press Shift + Alt + C in unity, or "Code Gen" > "Run CodeGen" from menu
     * Some time it is difficult to refactor code because of other generated code
     * And new code can be generated only if program is fully compiled, that is IMPORTANT!!!
     * Use "Code Gen" > "Generate Stubs" or Shift + Alt + S to generate stub code before or during your refactor
     * And when you program is compilable generate code normal way again
     *
     * Versioning is not supported for BinarySerialization by now
     * Json serialization is not very sensitive for versions 
     *
     * Code generation starts with defining tag with task enum value describing which functionality we want to generate
     * The simplest one is the following...
     */
    [GenTask(
        GenTaskFlags.Serialization |         // Fast binary serialize/deserialize methods
        GenTaskFlags.JsonSerialization |     // Json serialize/deserialize methods
        GenTaskFlags.Hash |                  // Fast hash code calculation
        GenTaskFlags.UpdateFrom |            // Deep copy optimized for copying into other created similar model 
        GenTaskFlags.CompareChech |          // Function that prints all differences between two models into error log 
        GenTaskFlags.DefaultConstructor |    // Generate Constructor that constructs all class type fields with defaults
        GenTaskFlags.PolymorphicConstruction // Allows to save ancestor as base class values as fields or in containers
    )]
    // All generated code will be placed into "x_generated" folder
    [GenInLocalFolder]
    public partial class CodeGenSamples : ISerializable
    {
        // All fields are automatically included
        int intField;
        // All properties are not included by default
        string stringPropWithoutTagNotIncluded { get; set; }
        // You need to specify which properties to include with GenInclude tag
        [GenInclude] string stringProp { get; set; }
        // You can ignore some fields with GenIgnoreTag
        [GenIgnore] int someTempIgnoredField;
        
        // all ref type fields considered not null by default, if null expect exception during generated function calls
        string stringFieldMustNotBeNull;
        
        // Use CanBeNull tag for fields that can be null so code for this case will be generated
        [CanBeNull] string stringFieldThatCanBeNull;
        
        // Extension methods for external classes used in generated classes will be generated.
        // But extension methods can't access private members, so be careful with that
        ExternalClass externalClass;
        Vector3 vector;

        // Other generated objects can be included
        [CanBeNull] OtherData otherData;
        
        // You can ignore specific parts of code generation, for example if you do not want default construction of this field
        [GenIgnore(GenTaskFlags.DefaultConstructor)]
        OtherData otherData2;
        
        List<int> listsOfPrimitivesAreOk;
        List<OtherData> listsOfDataAreOk;
        int[] arraysAreOk;
        
        // Dictionaries are supported but not for deep copy (UpdateFrom) for now...
        [GenIgnore(GenTaskFlags.UpdateFrom)] Dictionary<int, OtherData> dictsAreOk;
        [GenIgnore(GenTaskFlags.UpdateFrom)] Dictionary<int, List<List<string>>> complexStructuresAreAlsoOk;
        
        // NOT SUPPORTED
        [GenIgnore] int[,] multyDimArraysAreNotSupported;

        // ZergRush.Reactive primitives are supported
        Cell<OtherData> reactiveValue;
        ReactiveCollection<int> reactiveCollections;

        [GenIgnore(GenTaskFlags.DefaultConstructor)]
        public List<CodeGenSamples> ancestorArray = new List<CodeGenSamples>
        {
            // because of PolymorphicConstruction, Ancestor class will be serialized in right way
            new Ancestor()
        };

        static void HowToUse()
        {
            var data = new CodeGenSamples();
            // json serialize
            string jsonData = data.SaveToJsonString();
            // binary serialize
            byte[] binaryData = data.SaveToBinary();
            // json deserialize
            data = jsonData.LoadFromJsonString<CodeGenSamples>();
            // binary deserialize
            var data2 = binaryData.LoadFromBinary<CodeGenSamples>();
            
            // deep copy data2 into data
            data.UpdateFrom(data2);

            // compare data hashes
            if (data.CalculateHash() != data2.CalculateHash())
            {
                // and check for differences if hashes are not equal
                data.CompareCheck(data2, new Stack<string>());
            }
            
            // polymorphic construction example
            var ancestor = CreatePolymorphic((ushort)Types.Ancestor);
        }
    }

    // All class tags are inhereted, so its handy to create one base class for you model classes with all tags you want 
    [GenInLocalFolder]
    public partial class Ancestor : CodeGenSamples
    {
        public int fields;
    }

    [GenTask(GenTaskFlags.SimpleDataPack)]
    [GenInLocalFolder]
    public partial class OtherData
    {
        public int someData;
    }

    [GenInLocalFolder]
    public class ExternalClass
    {
        public int somePublicField;
        // private fields are not included in extension methods generation
        int somePrivateField;
    }

```

## Look package samples for more code examples






