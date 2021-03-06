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
IEventStream<string> mergedStreamOfNumbers = streamOfEvenNumbers.MergeWith(streamOfOddNumbers);
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
class PlayerData
{
	public Cell<int> money;
	public Cell<int> hp;
	public Cell<int> maxHp;
	// you can use transform api to make additional
	public ICell<float> relativeHp => hp.Merge(maxHp, (hp, maxHp) => hp / (float)maxHp);
	public ICell<bool> isWounded => relativeHp.Select(value => value < 0.5f);
}
```






