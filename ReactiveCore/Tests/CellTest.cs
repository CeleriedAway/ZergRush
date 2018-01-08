#if UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace ZergRush.ReactiveCore.Tests
{
    [TestFixture]
    public class CellTest
    {
        const string Bind = "BIND";
        const string ListenUpdates = "LISTEN_UPDATES";
        const string Map = "MAP";
        const string Merge = "MERGE";
        const string When = "WHEN";
        const string Join = "JOIN";
        const string FlatMap = "FLATMAP";
        const string ToCellOfCollection = "TO_CELL_OF_COLLECTION";
        const string Connection = "CONNECTION";

        [Test]
        public void Cell_CreatesWithValue_12()
        {
            var value = 12;

            var cell = new Cell<int>(value);

            Assert.AreEqual(value, cell.value);
        }

        [Test]
        public void Value_WasChangedTo_12()
        {
            var value = 12;
            var cell = new Cell<int>(0);

            cell.value = value;

            Assert.AreEqual(value, cell.value);
        }

        [Test]
        public void SelectMany_SelectedValue_SecondCell()
        {
            var firstCell = new Cell<int>(3);
            var secondCell = new Cell<int>(6);

            var selectedCell = firstCell.SelectMany(secondCell);

            Assert.AreEqual(6, selectedCell.value);
        }

        [Test]
        [Category(Bind)]
        public void Bind_ValueWasNotChanged_CallbackWasCalled()
        {
            var cell = new Cell<int>(0);
            var isUpdated = false;

            cell.Bind(value => isUpdated = true);

            Assert.True(isUpdated);
        }

        [Test]
        [Category(Bind)]
        public void Bind_ValueWasChanged_CallbackWasCalledAndValueWasChanged()
        {
            var bindedValue = 12;
            var isUpdated = false;
            var cell = new Cell<int>(0);

            cell.Bind(value => isUpdated = true);
            cell.value = bindedValue;

            Assert.True(isUpdated);
            Assert.AreEqual(bindedValue, cell.value);
        }

        [Test]
        [Category(Bind)]
        public void Bind_ValueWasChangedToSame_CallbackWasCalledOnlyOnInit()
        {
            var initValue = 12;
            var callCounter = 0;
            var cell = new Cell<int>(initValue);

            cell.Bind(value => callCounter++);
            cell.value = initValue;

            Assert.AreEqual(1, callCounter);
        }

        [Test]
        [Category(FlatMap)]
        public void FlatMap_ValueIsUpdated_CallbackCalled()
        {
            Cell<string>[] cells = Utils.Some("zero", "one", "two", "three")
                .Select(val => new Cell<string>(val))
                .ToArray();
            Cell<int> indexCell = new Cell<int>(2);

            ICell<string> currentWord = indexCell.FlatMap(i => cells[i]);

            Assert.AreEqual(currentWord.value, "two");
            
            indexCell.value = 3;
            Assert.AreEqual(currentWord.value, "three");

            int callCount = 0;
            currentWord.OnChanged(() => callCount++);

            string valUpdate = null;
            currentWord.ListenUpdates(word => valUpdate = word);
            
            indexCell.value = 1;
            Assert.AreEqual(valUpdate, "one");
            
            indexCell.value = 3;
            Assert.AreEqual(valUpdate, "three");
            
            cells[3].value = "three+";
            Assert.AreEqual(valUpdate, "three+");
            
            cells[2].value = "two+";
            indexCell.value = 2;
            Assert.AreEqual(valUpdate, "two+");
            
            Assert.AreEqual(callCount, 4);
        }

        [Test]
        [Category(Join)]
        public void Join_CellOfIntegerCell_CellOfInteger()
        {
            var initValue = 12;
            var cellToJoin = MakeCellToJoin(initValue);

            var joinedCell = cellToJoin.Join();

            Assert.AreEqual(initValue, joinedCell.value);
        }

        [Test]
        [Category(Join)]
        public void Join_AfterJoin_ValueUpdates()
        {
            var initValue = 12;
            var nextValue = 15;
            var cellToJoin = MakeCellToJoin(initValue);

            var joinedCell = cellToJoin.Join();
            cellToJoin.value = new Cell<int>(nextValue);

            Assert.AreEqual(nextValue, joinedCell.value);
        }

        [Test]
        [Category(Join)]
        public void Join_WhenCellToJoinIsNull_ThrowException()
        {
            var cellToJoin = new Cell<Cell<object>>(null);


            Assert.Throws<CellReactiveApi.JoinNullCellException>(() =>
            {
                var joinedCell = cellToJoin.Join();
                var value = joinedCell.value;
            });
        }

        [Test]
        [Category(Join)]
        public void Join_WhenCellToJoinIsNullAndListenToUpdates_ThrowException()
        {
            var cellToJoin = new Cell<Cell<object>>(null);

            Assert.Throws<CellReactiveApi.JoinNullCellException>(() =>
            {
                var joinedCell = cellToJoin.Join();
                joinedCell.Bind(value => { });
            });
        }

        [Test]
        [Category(Join)]
        [Category(Connection)]
        public void Join_JoinedCell_ListenUpdates()
        {
            var initValue = 12;
            var nextValue = 15;
            var updatedValue = 0;
            var cellToJoin = MakeCellToJoin(initValue);
            var cell = cellToJoin.value;

            var joinedCell = cellToJoin.Join();
            joinedCell.ListenUpdates(value => updatedValue = value);
            cell.value = nextValue;

            Assert.AreEqual(nextValue, updatedValue);
        }

        [Test]
        [Category(Join)]
        [Category(Connection)]
        public void Join_JoinedCellWhenDispose_DontListenUpdates()
        {
            var initValue = 12;
            var valueBeforeDispose = 15;
            var valueAfterDispose = 20;
            var updatedValue = 0;
            var cellToJoin = MakeCellToJoin(initValue);
            var cell = cellToJoin.value;

            var joinedCell = cellToJoin.Join();
            var connection = joinedCell.ListenUpdates(value => updatedValue = value);
            cell.value = valueBeforeDispose;
            connection.Dispose();
            cell.value = valueAfterDispose;

            Assert.AreEqual(valueBeforeDispose, updatedValue);
        }

        [Test]
        [Category(ListenUpdates)]
        public void ListenUpdates_ValueWasNotChanged_CallbackWasNotCalled()
        {
            var cell = new Cell<int>(0);
            var isUpdated = false;

            cell.ListenUpdates(value => isUpdated = true);
            cell.value = 0;

            Assert.False(isUpdated);
        }

        [Test]
        [Category(ListenUpdates)]
        public void ListenUpdates_ValueWasChangedToSame_CallbackWasNotCalled()
        {
            var initValue = 3;
            var callCounter = 0;

            var cell = new Cell<int>(initValue);
            cell.ListenUpdates(value =>
            {
                callCounter++;
            });
            cell.value = initValue;

            Assert.Zero(callCounter);
        }

        [Test]
        [Category(ListenUpdates)]
        [Category(Connection)]
        public void ListenUpdates_ValueWasChanged_CallbackWasCalledAndValueWasChanged()
        {
            var bindedValue = 15;
            var cell = new Cell<int>(0);
            var isUpdated = false;

            cell.ListenUpdates(value => isUpdated = true);
            cell.value = bindedValue;

            Assert.True(isUpdated);
            Assert.AreEqual(bindedValue, cell.value);
        }

        [Test]
        [Category(ListenUpdates)]
        [Category(Connection)]
        public void ListenUpdates_ListenToValueUpdates_CloseWhenDispose()
        {
            var valueBeforeDispose = 15;
            var valueAfterDispose = 17;
            var listenedValue = 0;

            var cell = new Cell<int>(0);
            var connection = cell.ListenUpdates(value => listenedValue = value);

            cell.value = valueBeforeDispose;
            connection.Dispose();
            cell.value = valueAfterDispose;

            Assert.AreEqual(valueBeforeDispose, listenedValue);
        }

        [Test]
        [Category(Map)]
        public void Map_ValueMapsTo_SeedMultiplyByInitValue()
        {
            var initValue = 3;
            var seed = 2;

            var cell = new Cell<int>(initValue);
            var mappedCell = cell.Map(value => seed * value);

            Assert.AreEqual((initValue * seed), mappedCell.value);
        }

        [Test]
        [Category(Map)]
        public void Map_ValueMapsWhenValueChangedTo_SeedMultiplyByChangedValue()
        {
            var initValue = 3;
            var seed = 2;
            var nextValue = 10;
            var cell = new Cell<int>(initValue);

            var mappedCell = cell.Map(value => seed * value);
            cell.value = nextValue;

            Assert.AreEqual((nextValue * seed), mappedCell.value);
        }

        [Test]
        [Category(Map)]
        [Category(Connection)]
        public void Map_MappedCell_ListenToUpdates()
        {
            var initValue = 3;
            var nextValue = 10;
            var updatedValue = 0;
            var cell = new Cell<int>(initValue);

            var mappedCell = cell.Map(value => value);
            mappedCell.ListenUpdates(value => updatedValue = value);
            cell.value = nextValue;

            Assert.AreEqual(nextValue, updatedValue);
        }

        [Test]
        [Category(Map)]
        [Category(Connection)]
        public void Map_MappedCellWhenDispose_DontListenToUpdates()
        {
            var initValue = 3;
            var valueBeforeDispose = 10;
            var valueAfterDispose = 15;
            var updatedValue = 0;
            var cell = new Cell<int>(initValue);

            var mappedCell = cell.Map(value => value);
            var connection = mappedCell.ListenUpdates(value => updatedValue = value);
            cell.value = valueBeforeDispose;
            connection.Dispose();
            cell.value = valueAfterDispose;

            Assert.AreEqual(valueBeforeDispose, updatedValue);
        }

        [Test]
        [Category(Merge)]
        public void Merge_AddTwoCells_SumOfTwoCells()
        {
            var firstValue = 12;
            var secondValue = 15;

            var firstCell = new Cell<int>(firstValue);
            var secondCell = new Cell<int>(secondValue);
            var mergedCell = firstCell.Merge(secondCell, (x, y) => x + y);

            Assert.AreEqual((firstValue + secondValue), mergedCell.value);
        }

        [Test]
        [Category(Merge)]
        public void Merge_AddTwoCellsAndChangeValues_SumOfTwoChangedValues()
        {
            var firstNextValue = 20;
            var secondNextValue = 30;

            var firstCell = new Cell<int>(0);
            var secondCell = new Cell<int>(0);
            var mergedCell = firstCell.Merge(secondCell, (x, y) => x + y);
            firstCell.value = firstNextValue;
            secondCell.value = secondNextValue;

            Assert.AreEqual((firstNextValue + secondNextValue), mergedCell.value);
        }

        [Test]
        [Category(Merge)]
        [Category(Connection)]
        public void Merge_MergedCell_ListenToUpdates()
        {
            var updatedValue = 0;
            var nextValue = 15;
            var firstCell = new Cell<int>(0);
            var secondCell = new Cell<int>(0);

            var mergedCell = firstCell.Merge(secondCell, (x, y) => x);
            mergedCell.ListenUpdates(value => updatedValue = value);
            firstCell.value = nextValue;

            Assert.AreEqual(nextValue, updatedValue);
        }

        [Test]
        [Category(Merge)]
        [Category(Connection)]
        public void Merge_MergedCellWhenDispose_DontListenToUpdates()
        {
            var updatedValue = 0;
            var valueBeforeDispose = 12;
            var valueAfterDispose = 15;
            var firstCell = new Cell<int>(0);
            var secondCell = new Cell<int>(0);

            var mergedCell = firstCell.Merge(secondCell, (x, y) => x);
            var connection = mergedCell.ListenUpdates(value => updatedValue = value);
            firstCell.value = valueBeforeDispose;
            connection.Dispose();
            firstCell.value = valueAfterDispose;

            Assert.AreEqual(valueBeforeDispose, updatedValue);
        }

        [Test]
        [Category(ToCellOfCollection)]
        public void ToCellOfCollection_ListOfCellsTo_CellOfLists()
        {
            var firstValue = 12;
            var secondValue = 15;
            var listOfCells = MakeListOfCells(firstValue, secondValue);

            var cellOfLists = listOfCells.ToCellOfCollection();

            var targetArray = cellOfLists.value.ToArray();
            Assert.AreEqual(firstValue, targetArray[0]);
        }

        [Test]
        [Category(ToCellOfCollection)]
        public void ToCellOfCollection_CellOfLists_UpdatesWhenValueInListOfCellsUpdates()
        {
            var firstValue = 12;
            var secondValue = 15;
            var changedValue = 999;
            var listOfCells = MakeListOfCells(firstValue, secondValue);

            var cellOfLists = listOfCells.ToCellOfCollection();
            listOfCells[0].value = changedValue;

            var targetArray = cellOfLists.value.ToArray();
            Assert.AreEqual(changedValue, targetArray[0]);
        }

        [Test]
        [Category(ToCellOfCollection)]
        [Category(Connection)]
        public void ToCellOfCollection_CellOfLists_ListenToUpdates()
        {
            var firstValue = 12;
            var secondValue = 15;
            var firstNextValue = 20;
            var updatedValue = 0;
            var listOfCells = MakeListOfCells(firstValue, secondValue);
            var firstCell = listOfCells[0];

            var cellOfLists = listOfCells.ToCellOfCollection();
            cellOfLists.ListenUpdates(list => updatedValue = list.ToArray()[0]);
            firstCell.value = firstNextValue;

            Assert.AreEqual(firstNextValue, updatedValue);
        }

        [Test]
        [Category(ToCellOfCollection)]
        [Category(Connection)]
        public void ToCellOfCollection_CellOfListsWhenDispose_DontListenToUpdates()
        {
            var firstValue = 12;
            var secondValue = 15;
            var firstValueBeforeDispose = 20;
            var firstValueAfterDispose = 30;
            var updatedValue = 0;
            var listOfCells = MakeListOfCells(firstValue, secondValue);
            var firstCell = listOfCells[0];

            var cellOfLists = listOfCells.ToCellOfCollection();
            var connection = cellOfLists.ListenUpdates(list => updatedValue = list.ToArray()[0]);
            firstCell.value = firstValueBeforeDispose;
            connection.Dispose();
            firstCell.value = firstValueAfterDispose;

            Assert.AreEqual(firstValueBeforeDispose, updatedValue);
        }

        [Test]
        [TestCase(12, false)]
        [TestCase(18, true)]
        [Category(When)]
        public void When_FilterCell_UpdateIfInitValueBiggerThan15(int initValue, bool expectedResult)
        {
            var borderValue = 15;
            var isUpdated = false;

            var cell = new Cell<int>(initValue);
            var stream = cell.When(value => value >= borderValue);

            stream.Listen(() =>
            {
                isUpdated = true;
            });

            Assert.AreEqual(expectedResult, isUpdated);
        }

        [Test]
        [Category(When)]
        public void When_CallbackCalls_OnceIfPredicateStillTrue()
        {
            const int valueChanges = 3;
            var callCounter = 0;
            var cell = new Cell<int>(0);
            var stream = cell.When(value => true);

            stream.Listen(() =>
            {
                callCounter++;
            });

            for (var i = 0; i < valueChanges; i++)
                cell.value = i;

            Assert.AreEqual(1, callCounter);
        }

        [Test]
        [Category(When)]
        [Category(Connection)]
        public void When_FilterCell_CallbackWasNotCalledWhenConnectionDispose()
        {
            var callCounter = 0;
            var cell = new Cell<int>(0);
            var stream = cell.When(value => true);

            var connection = stream.Listen(() =>
            {
                callCounter++;
            });
            connection.Dispose();

            cell.value = 10;
            cell.value = 20;

            Assert.AreEqual(1, callCounter);
        }

        [Test]
        [Category(When)]
        public void WhenOnce_FilterCell_CallbackWasCalledOnlyOnce()
        {
            var initValue = 17;
            var borderValue = 15;
            var callCounter = 0;

            var cell = new Cell<int>(initValue);
            var stream = cell.WhenOnce(value => value >= borderValue);

            stream.Listen(() =>
            {
                callCounter++;
            });
            cell.value = 20;
            cell.value = 25;

            Assert.AreEqual(1, callCounter);
        }

        [Test]
        [Category(When)]
        public void WhenUpdatedToSatisfy_FilterCell_CallbackWasNotCalledWhenNoUpdates()
        {
            var initValue = 17;
            var borderValue = 15;
            var callCounter = 0;
            var cell = new Cell<int>(initValue);
            var stream = cell.WhenUpdatedToSatisfy(value => value >= borderValue);

            stream.Listen(() => callCounter++);

            Assert.Zero(callCounter);
        }

        private Cell<Cell<int>> MakeCellToJoin(int value)
        {
            return new Cell<Cell<int>>(new Cell<int>(value));
        }

        private List<Cell<int>> MakeListOfCells(params int[] values)
        {
            var list = new List<Cell<int>>(5);

            foreach (var val in values)
                list.Add(new Cell<int>(val));

            return list;
        }
    }
}

#endif