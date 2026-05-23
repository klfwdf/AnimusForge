using System;
using System.Collections.Generic;
using SandBox.BoardGames.MissionLogics;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Missions
{
	// Token: 0x02000014 RID: 20
	public class MissionCampaignView : MissionView
	{
		// Token: 0x06000083 RID: 131 RVA: 0x0000516C File Offset: 0x0000336C
		public override void OnMissionScreenPreLoad()
		{
			this._mapScreen = MapScreen.Instance;
			if (this._mapScreen != null && base.Mission.NeedsMemoryCleanup && ScreenManager.ScreenTypeExistsAtList(this._mapScreen))
			{
				this._mapScreen.ClearGPUMemory();
				Utilities.ClearShaderMemory();
			}
		}

		// Token: 0x06000084 RID: 132 RVA: 0x000051AB File Offset: 0x000033AB
		public override void OnMissionScreenFinalize()
		{
			MapScreen mapScreen = this._mapScreen;
			if (((mapScreen != null) ? mapScreen.BannerTexturedMaterialCache : null) != null)
			{
				this._mapScreen.BannerTexturedMaterialCache.Clear();
			}
		}

		// Token: 0x06000085 RID: 133 RVA: 0x000051D4 File Offset: 0x000033D4
		[CommandLineFunctionality.CommandLineArgumentFunction("get_face_and_helmet_info_of_followed_agent", "mission")]
		public static string GetFaceAndHelmetInfoOfFollowedAgent(List<string> strings)
		{
			MissionScreen missionScreen = ScreenManager.TopScreen as MissionScreen;
			if (missionScreen == null)
			{
				return "Only works at missions";
			}
			Agent lastFollowedAgent = missionScreen.LastFollowedAgent;
			if (lastFollowedAgent == null)
			{
				return "An agent needs to be focussed.";
			}
			string text = "";
			text += lastFollowedAgent.BodyPropertiesValue.ToString();
			EquipmentElement equipmentFromSlot = lastFollowedAgent.SpawnEquipment.GetEquipmentFromSlot(EquipmentIndex.NumAllWeaponSlots);
			if (!equipmentFromSlot.IsEmpty)
			{
				text = text + "\n Armor Name: " + equipmentFromSlot.Item.Name.ToString();
				text = text + "\n Mesh Name: " + equipmentFromSlot.Item.MultiMeshName;
			}
			if (lastFollowedAgent.Character != null)
			{
				CharacterObject characterObject = lastFollowedAgent.Character as CharacterObject;
				if (characterObject != null)
				{
					text = text + "\n Troop Id: " + characterObject.StringId;
				}
			}
			TaleWorlds.InputSystem.Input.SetClipboardText(text);
			return "Copied to clipboard:\n" + text;
		}

		// Token: 0x06000086 RID: 134 RVA: 0x000052B0 File Offset: 0x000034B0
		public override void EarlyStart()
		{
			base.EarlyStart();
			this._missionMainAgentController = Mission.Current.GetMissionBehavior<MissionMainAgentController>();
			MissionBoardGameLogic missionBehavior = Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
			if (missionBehavior != null)
			{
				missionBehavior.GameStarted += this._missionMainAgentController.Disable;
				missionBehavior.GameEnded += this._missionMainAgentController.Enable;
			}
		}

		// Token: 0x04000026 RID: 38
		private MapScreen _mapScreen;

		// Token: 0x04000027 RID: 39
		private MissionMainAgentController _missionMainAgentController;
	}
}
