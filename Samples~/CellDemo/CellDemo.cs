using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;
using UnityEngine.UI;
using ZergRush;
using ZergRush.ReactiveCore;
using ZergRush.ReactiveUI;

namespace Demo.CellDemo
{
	public class CellDemo : ConnectableMonoBehaviour
	{
		GameData gameData;
		UiState uiState;

		public UnitView finalUnitView;
		public EquipmentView selectedEquipmentView;

		public ReactiveScrollRect unitTable;
		public ReactiveScrollRect equipmentTable;

		void Start()
		{
            gameData = new GameData();
            uiState = new UiState();
			
			// Unit table
			connections += gameData.unitsAvailable.PresentInScrollWithLayout(unitTable, PrefabRef<UnitView>.Auto(), layout: Rui.LinearLayout(
				LayoutDirection.Horizontal, margin: 30, topShift: 30, bottomShift: 30),
				show: (unit, view) =>
				{
					// Show dynamic data
					view.connections += view.attack.SetTextContent(unit.attack);
					view.connections += view.defence.SetTextContent(unit.defence);
					view.connections += view.hp.SetTextContent(unit.hp);
					
					// Show checkbox when current unit is selected
					view.connections += view.selectedCheckbox.SetVisibility(uiState.selectedUnit.Is(unit));
					
					// Command to upgrade unit
					view.upgradeButton.ClickStream().Subscribe(view.connections, unit.Upgrade);
					// Command to change ui state
					view.connections += view.viewClickButton.ClickStream().Subscribe(() => uiState.SetUnitSelection(unit));
				});

			// Equipment table
			connections += gameData.equipmentAvailable.PresentInScrollWithLayout(equipmentTable, PrefabRef<EquipmentView>.Auto(), layout: Rui.LinearLayout(
				LayoutDirection.Horizontal, margin: 30, topShift: 30, bottomShift: 30),
				show: (equipment, view) =>
				{
					// Show dynamic data
					view.connections += view.buff.SetTextContent(equipment.buff.Select(value =>
						string.Format("{0}:+{1}", equipment.type, value)));
					
					// Show checkbox when current equipment is selected
					view.connections += view.selectedCheckbox.SetVisibility(uiState.selectedEquipment.Is(equipment));
					
					// Command to upgrade equipment 
					view.connections += view.upgradeButton.ClickStream().Subscribe(equipment.Upgrade);
					// Command to change ui equipment selected state
					view.connections += view.viewClickButton.ClickStream().Subscribe(() => uiState.SetEquipmentSelection(equipment));
				});
		
			// Selected equipment view
			uiState.selectedEquipment.Bind(connections, equipment =>
			{
				var view = selectedEquipmentView;
				// Remove connections from previous selected item.
				view.DisconnectAll();

				if (equipment == null)
				{
					view.buff.text = "X";
					return;
				}
			
				view.connections += view.buff.SetTextContent(equipment.buff.Select(value =>
					string.Format("{0}:+{1}", equipment.type, value)));
				view.connections += view.upgradeButton.ClickStream().Subscribe(equipment.Upgrade);
			});
			connections += selectedEquipmentView.upgradeButton.SetVisibility(uiState.selectedEquipment.IsNot(null));
		
			// Final selected unit + equpment buff
			foreach (var buffTypeIt in Utils.GetEnumValues<UnitBuffType>())
			{
				// Copy var to prevent c# capturing iterator
				var buffType = buffTypeIt;
				
				// Compose final stat value from dynamic data
				// This is an example of how use linq api to manage complex data dependancies.
				var finalStat =
					from su in uiState.selectedUnit
					from se in uiState.selectedEquipment
					from stat in FinalStat(su, se, buffType)
					select stat;
				
				// Alternative (and slightly more ellegant and performant) to achieve same result.
				finalStat = uiState.selectedUnit
					.Merge(uiState.selectedEquipment, (su, se) => FinalStat(su, se, buffType))
					.Join();

				connections += TextForBuffType(finalUnitView, buffType).SetTextContent(finalStat);
			}
			connections += finalUnitView.upgradeButton.SetVisibility(uiState.selectedUnit.IsNot(null));
            // Command to upgrade unit from final unit view
            connections += finalUnitView.upgradeButton.ClickStream().Subscribe(() => uiState.selectedUnit.value.Upgrade());
		}

		static ICell<int> FinalStat(Unit unit, Equipment eq, UnitBuffType buffType)
		{
			// selected unit can be null.
			if (unit == null) return StaticCell<int>.Default();
		
			var stat = StatForBuffType(unit, buffType);
			// If equipment does not buff this stat return just stat.
			if (eq == null || eq.type != buffType) return stat;
			// If equipment stat match this kind of stat merge those.
			return stat.Merge(eq.buff, (s, buff) => s + buff);
		}

		static Cell<int> StatForBuffType(Unit unit, UnitBuffType type)
		{
			switch (type)
			{
				case UnitBuffType.Attack: return unit.attack;
				case UnitBuffType.Defence: return unit.defence;
				case UnitBuffType.Hp: return unit.hp;
				default:
					throw new ArgumentOutOfRangeException("type", type, null);
			}
		}
		static Text TextForBuffType(UnitView view, UnitBuffType type)
		{
			switch (type)
			{
				case UnitBuffType.Attack: return view.attack;
				case UnitBuffType.Defence: return view.defence;
				case UnitBuffType.Hp: return view.hp;
				default:
					throw new ArgumentOutOfRangeException("type", type, null);
			}
		}
	}
}