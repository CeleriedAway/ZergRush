using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ZergRush;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.Samples
{
    public class ZergRushBasics : ConnectableMonoBehaviour
    {
        IEnumerator Start()
        {
            EventBasicsAndConnections();
            EventTransformations();

            yield return EventsInCoroutine();
            EventsInAsyncContext();
            
            Cells();
        }

        public void EventBasicsAndConnections()
        {
            // The first part of ZergRush.Reactive is EventStream
            // It represents simple stream of events that you can subscribe to get its updates
            // The main implementation EventStream allows to "Send()" events.
            EventStream<int> stream = new EventStream<int>();
            var connection = stream.Subscribe(value => Debug.Log($"value received {value}"));
            stream.Send(1);

            // Subscribe method return IDisposable "Connection" object.
            // If disposed then this connections is not valid anymore and initial callback wont be called after that.
            connection.Dispose();
            stream.Send(666);

            // One of the best usecase for this is a collecting of connection objects for automate disposing,
            // so you won't forget to "unsubscribe" for event
            // ConnectableMonoBehaviour class has connections array that auto disposed in OnDestroy() callback
            connections += stream.Subscribe(ValueReceiver);
            // There are some syntax variations for this
            stream.Subscribe(connections, ValueReceiver);
            stream.Subscribe(this, ValueReceiver);

            // but you can manage connections lifetime manually by collecting connections by hand
            var connectionsWithCustomLivetime = new Connections();
            stream.Subscribe(connectionsWithCustomLivetime, ValueReceiver);
            connectionsWithCustomLivetime.DisconnectAll();

            // So more real world example will look like this..
            var dogModel = new DogModel();
            dogModel.injured.Subscribe(connections, ShowBloodSpashes);
            dogModel.spokeSomething.Subscribe(connections, ShowTextBubble);

            // and if somewhere in your program somebody decided to kick a dog, this view will notify player
            dogModel.Kick();
        }
        
        void ValueReceiver(int value)
        {
            Debug.Log($"Value receiver called with value {value}");
        }
        
        class DogModel
        {
            public EventStream injured = new EventStream();

            // if your care about incapsulation too much you can dispose event readonly interface
            EventStream<string> spokeSomething_ = new EventStream<string>();
            public IEventStream<string> spokeSomething => spokeSomething_;

            public void Kick()
            {
                injured.Send();
                spokeSomething_.Send("wtf you are doing man im your dog!");
            }
        }

        void ShowBloodSpashes()
        {
            /*...*/
        }

        void ShowTextBubble(string text)
        {
            /*...*/
        }

        void EventTransformations()
        {
            // EventStreams can be Filtered, Transformed, and Merged. Like this..
            // The only real stream here 
            EventStream<int> streamOfNumbers = new EventStream<int>();
            // Subscribe to this stream and you will receive only event number events sent with streamOfNumbers.
            IEventStream<int> streamOfEvenNumbers = streamOfNumbers.Filter(IsEven);
            // Same with odd numbers. You can use Where, its the same method, just more Linq-like
            IEventStream<int> streamOfOddNumbers = streamOfNumbers.Where(IsOdd);
            // Here you will receive strings created from numbers
            IEventStream<string> streamOfSomeStrings = streamOfEvenNumbers.Map(i => "event is converted to string " + i);
            // Here you will receive events from both streams
            IEventStream<int> mergedStreamOfNumbers = streamOfEvenNumbers.MergeWith(streamOfOddNumbers);
            // you can make event that receive only first value send with Once() function
            IEventStream<int> onlyFirstNumber = streamOfNumbers.Once();

            streamOfEvenNumbers.Subscribe(v => Debug.Log($"streamOfEvenNumbers event {v}"));
            streamOfOddNumbers.Subscribe(v => Debug.Log($"streamOfOddNumbers event {v}"));
            streamOfSomeStrings.Subscribe(Debug.Log);
            mergedStreamOfNumbers.Subscribe(v => Debug.Log($"merged stream event {v}"));

            for (int i = 0; i < 10; i++) streamOfNumbers.Send(i);
        }

        IEnumerator EventsInCoroutine()
        {
            // you can use events to control coroutine flow
            var buttonPressed = new EventStream();
            ReactiveTimeInteractions.ExecuteAfterDelay(0.4f, buttonPressed.Send);
            yield return new WaitForEvent(buttonPressed);
            
            // and also retrieve result if event had interesting information
            var someValueEvents = new EventStream<int>();
            var result = new WaitResult<int>();
            
            ReactiveTimeInteractions.ExecuteAfterDelay(0.6f, () => someValueEvents.Send(7));
            yield return new WaitForEvent<int>(someValueEvents, result);
            Debug.Log($"coroutine result {result.value} received");
        }

        async void EventsInAsyncContext()
        {
            var buttonPressed = new EventStream();
            ReactiveTimeInteractions.ExecuteAfterDelay(0.4f, buttonPressed.Send);
            await buttonPressed.SingleMessageAsync();
            
            var someValueEvents = new EventStream<int>();
            ReactiveTimeInteractions.ExecuteAfterDelay(0.6f, () => someValueEvents.Send(7));
            var result = await someValueEvents.SingleMessageAsync();
            Debug.Log($"async result {result} received");
        }
        
        static bool IsEven(int i) => i % 2 == 0;
        static bool IsOdd(int i) => i % 2 == 1;

        void Cells()
        {
            // But the most useful part of the library in game development is Cell<T>
            // It represents a value that is changing in time. 
            var moneyCount = new Cell<int>();
            moneyCount.ListenUpdates(connections, v => Debug.Log($"Money changed to {v}"));
            moneyCount.value = 10;
            
            // It is important that all cell api guarantee that setting same value won't trigger update callback
            moneyCount.value = 10; // Log won't be triggered second time
            
            // Map and Select (Linq-ish) are same functions that transforms inner value of a cell
            ICell<bool> imRich; 
            imRich = moneyCount.Map(m => m > 100);
            imRich = moneyCount.Select(m => m > 100);
            
            // Bind is the most usable subscription function 
            // It calls provided action once Bind called and then during all value updates
            // It allows to form kind of strong connection between data and consequence
            connections += imRich.Bind(v => Debug.Log(v ? "I'm rich!!!" : "not rich enough"));

            moneyCount.value = 50;
            moneyCount.value = 101;
            
            // this time imRich callback wont be called, because despite base value changed transformed result of that value did not
            // this behaviour is consistent though all cell API.
            // ICell<T> gaurantee not to send update events if value was not changed.
            moneyCount.value = 200;

            // You can turn cells into events with When functions
            var levelEnds = moneyCount.WhenMoreOrEqual(10);
            // or in more general way
            levelEnds = moneyCount.When(m => m > 10);
            
            // Then look at PlayerData class for further examples
            // Then look through other samples in the package
        }

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

        class HpBar
        {
            public void SetRelativeValue(float relativeValue) {}
        }

        class PlayerView : ConnectableMonoBehaviour
        {
            public Image bloodSprite;
            public Image borderSprite;
            public HpBar hpBar;
            
            public void Show(PlayerData player)
            {
                player.isWounded.Bind(connections, wounded => bloodSprite.SetActiveSafe(wounded));
                // or same behaviour with shortcut api
                connections += bloodSprite.SetActive(player.isWounded);
                // another example of shortcut api
                connections += bloodSprite.SetColor(player.isWounded.Select(wounded => wounded ? Color.red : Color.white));
                // or with method group like this
                player.relativeHp.Bind(connections, hpBar.SetRelativeValue);
            }
        }
        
        /*
         *    Some real project examples
         *
         *
            public partial class SkillGroupInvestments : TWLEntity
            {
                public SkillGroup group;
                public readonly DataList<SkillInvestment> investments;
                public Unit owner => (Unit) carrier;

                public int investedSkillPoints => investments.Sum(i => i.points.value);

                // Get total number of invested skill points
                public ICell<int> investedSkillPointsCell => investments.AsCell()
                    .Map(l => l.Select(s => s.points).ToCellOfCollection().Map(c => c.Sum()))
                    .Join();
                ...
            }
         * 
         */
    }
}