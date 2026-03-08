using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.Objects.Usables;
using TaleWorlds.ObjectSystem;

namespace SandBox.Missions.MissionEvents
{
	// Token: 0x0200009A RID: 154
	public class OpenInventoryWithGivenItemsEventListenerLogic : MissionLogic
	{
		// Token: 0x0600066C RID: 1644 RVA: 0x0002C0BF File Offset: 0x0002A2BF
		public OpenInventoryWithGivenItemsEventListenerLogic()
		{
			Game.Current.EventManager.RegisterEvent<GenericMissionEvent>(new Action<GenericMissionEvent>(this.OnGenericMissionEventTriggered));
		}

		// Token: 0x0600066D RID: 1645 RVA: 0x0002C0ED File Offset: 0x0002A2ED
		protected override void OnEndMission()
		{
			Game.Current.EventManager.UnregisterEvent<GenericMissionEvent>(new Action<GenericMissionEvent>(this.OnGenericMissionEventTriggered));
		}

		// Token: 0x0600066E RID: 1646 RVA: 0x0002C10A File Offset: 0x0002A30A
		private void OnGenericMissionEventTriggered(GenericMissionEvent missionEvent)
		{
			if (missionEvent.EventId == "open_inventory_with_given_items")
			{
				this.OpenInventoryWithGivenEquipment(missionEvent.Parameter);
			}
		}

		// Token: 0x0600066F RID: 1647 RVA: 0x0002C12C File Offset: 0x0002A32C
		private void OpenInventoryWithGivenEquipment(string parameters)
		{
			string[] array = parameters.Split(new char[] { ' ' });
			string text = array[0];
			if (!this._openedInventoryItemRosters.ContainsKey(text))
			{
				this._openedInventoryItemRosters.Add(text, new ItemRoster());
				string[] array2 = new string[array.Length - 2];
				Array.Copy(array, 2, array2, 0, array2.Length);
				this.InitializeEventItemRoster(array2, this._openedInventoryItemRosters[text]);
			}
			EventTriggeringUsableMachine firstScriptOfType = Mission.Current.Scene.FindEntityWithTag(text).GetFirstScriptOfType<EventTriggeringUsableMachine>();
			for (int i = 0; i < firstScriptOfType.StandingPoints.Count; i++)
			{
				if (firstScriptOfType.StandingPoints[i].HasUser)
				{
					firstScriptOfType.StandingPoints[i].UserAgent.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
				}
			}
			TextObject descriptionText = firstScriptOfType.DescriptionText;
			string text2 = array[1];
			if (text2.Equals("battle"))
			{
				InventoryScreenHelper.OpenScreenAsReceiveItems(this._openedInventoryItemRosters[text], descriptionText, new Action(this.DoneLogicForBattleEquipmentUpdate));
				return;
			}
			if (text2.Equals("civilian"))
			{
				InventoryScreenHelper.OpenScreenAsReceiveItems(this._openedInventoryItemRosters[text], descriptionText, new Action(this.DoneLogicForCivilianEquipmentUpdate));
				return;
			}
			if (text2.Equals("stealth"))
			{
				InventoryScreenHelper.OpenScreenAsReceiveItems(this._openedInventoryItemRosters[text], descriptionText, new Action(this.DoneLogicForStealthEquipmentUpdate));
				return;
			}
			if (text2.Equals("none"))
			{
				InventoryScreenHelper.OpenScreenAsReceiveItems(this._openedInventoryItemRosters[text], descriptionText, null);
			}
		}

		// Token: 0x06000670 RID: 1648 RVA: 0x0002C2AD File Offset: 0x0002A4AD
		private void DoneLogicForBattleEquipmentUpdate()
		{
			Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(Hero.MainHero.BattleEquipment);
		}

		// Token: 0x06000671 RID: 1649 RVA: 0x0002C2C3 File Offset: 0x0002A4C3
		private void DoneLogicForCivilianEquipmentUpdate()
		{
			Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(Hero.MainHero.CivilianEquipment);
		}

		// Token: 0x06000672 RID: 1650 RVA: 0x0002C2D9 File Offset: 0x0002A4D9
		private void DoneLogicForStealthEquipmentUpdate()
		{
			Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(Hero.MainHero.StealthEquipment);
		}

		// Token: 0x06000673 RID: 1651 RVA: 0x0002C2F0 File Offset: 0x0002A4F0
		private void InitializeEventItemRoster(string[] itemsWithModifiers, ItemRoster eventItemRoster)
		{
			for (int i = 0; i < itemsWithModifiers.Length; i++)
			{
				string[] array = itemsWithModifiers[i].Split(new char[] { ',' });
				string objectName = array[0];
				string s = array[1];
				string text = ((array.Length > 2) ? array[2] : "");
				ItemRosterElement itemRosterElement = new ItemRosterElement(MBObjectManager.Instance.GetObject<ItemObject>(objectName), int.Parse(s), string.IsNullOrEmpty(text) ? null : MBObjectManager.Instance.GetObject<ItemModifier>(text));
				eventItemRoster.Add(itemRosterElement);
			}
		}

		// Token: 0x04000379 RID: 889
		private const string OpenInventoryWithGivenItemsEventId = "open_inventory_with_given_items";

		// Token: 0x0400037A RID: 890
		private readonly Dictionary<string, ItemRoster> _openedInventoryItemRosters = new Dictionary<string, ItemRoster>();
	}
}
