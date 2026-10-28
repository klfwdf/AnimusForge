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

namespace SandBox.View.Missions;

public class MissionCampaignView : MissionView
{
	private MapScreen _mapScreen;

	private MissionMainAgentController _missionMainAgentController;

	public override void OnMissionScreenPreLoad()
	{
		_mapScreen = MapScreen.Instance;
		if (_mapScreen != null && base.Mission.NeedsMemoryCleanup && ScreenManager.ScreenTypeExistsAtList(_mapScreen))
		{
			_mapScreen.ClearGPUMemory();
			Utilities.ClearShaderMemory();
		}
	}

	public override void OnMissionScreenFinalize()
	{
		if (_mapScreen?.BannerTexturedMaterialCache != null)
		{
			_mapScreen.BannerTexturedMaterialCache.Clear();
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("get_face_and_helmet_info_of_followed_agent", "mission")]
	public static string GetFaceAndHelmetInfoOfFollowedAgent(List<string> strings)
	{
		if (!(ScreenManager.TopScreen is MissionScreen { LastFollowedAgent: var lastFollowedAgent }))
		{
			return "Only works at missions";
		}
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
		if (lastFollowedAgent.Character != null && lastFollowedAgent.Character is CharacterObject characterObject)
		{
			text = text + "\n Troop Id: " + characterObject.StringId;
		}
		TaleWorlds.InputSystem.Input.SetClipboardText(text);
		return "Copied to clipboard:\n" + text;
	}

	public override void EarlyStart()
	{
		base.EarlyStart();
		_missionMainAgentController = Mission.Current.GetMissionBehavior<MissionMainAgentController>();
		MissionBoardGameLogic missionBehavior = Mission.Current.GetMissionBehavior<MissionBoardGameLogic>();
		if (missionBehavior != null)
		{
			missionBehavior.GameStarted += _missionMainAgentController.Disable;
			missionBehavior.GameEnded += _missionMainAgentController.Enable;
		}
	}
}
