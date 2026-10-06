using System.Diagnostics;
using System.IO;
using System.Xml;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ObjectSystem;

namespace SandBox.View.Missions;

public class MissionSingleplayerViewHandler : MissionView
{
	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		if (!base.MissionScreen.SceneLayer.Input.IsCategoryRegistered(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory")))
		{
			base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (base.Mission == null || base.MissionScreen.IsPhotoModeEnabled || base.Mission.MissionEnded)
		{
			return;
		}
		if (base.Input.IsGameKeyPressed(38))
		{
			if (base.Mission.IsInventoryAccessAllowed)
			{
				InventoryScreenHelper.OpenScreenAsInventory(OnInventoryScreenDone);
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText((base.Mission.Mode == MissionMode.Battle || base.Mission.Mode == MissionMode.Duel) ? "str_cannot_reach_inventory_during_battle" : "str_cannot_reach_inventory").ToString()));
			}
		}
		else if (base.Input.IsGameKeyPressed(42))
		{
			if (base.Mission.IsQuestScreenAccessAllowed)
			{
				Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<QuestsState>());
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_quests").ToString()));
			}
		}
		else if (!base.Input.IsControlDown() && base.Input.IsGameKeyPressed(43))
		{
			if (base.Mission.IsPartyWindowAccessAllowed)
			{
				PartyScreenHelper.OpenScreenAsNormal();
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_party").ToString()));
			}
		}
		else if (base.Input.IsGameKeyPressed(39))
		{
			if (base.Mission.IsEncyclopediaWindowAccessAllowed)
			{
				Campaign.Current.EncyclopediaManager.GoToLink("LastPage", "");
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_encyclopedia").ToString()));
			}
		}
		else if (base.Input.IsGameKeyPressed(40))
		{
			if (base.Mission.IsKingdomWindowAccessAllowed && Hero.MainHero.MapFaction.IsKingdomFaction)
			{
				Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<KingdomState>());
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_kingdom").ToString()));
			}
		}
		else if (base.Input.IsGameKeyPressed(41))
		{
			if (base.Mission.IsClanWindowAccessAllowed)
			{
				Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<ClanState>());
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_clan").ToString()));
			}
		}
		else if (base.Input.IsGameKeyPressed(45))
		{
			InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_fleet").ToString()));
		}
		else if (base.Input.IsGameKeyPressed(37))
		{
			if (base.Mission.IsCharacterWindowAccessAllowed)
			{
				Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<CharacterDeveloperState>());
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_character").ToString()));
			}
		}
		else if (base.Input.IsGameKeyPressed(36))
		{
			if (base.Mission.IsBannerWindowAccessAllowed && Campaign.Current.IsBannerEditorEnabled)
			{
				Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<BannerEditorState>());
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_banner").ToString()));
			}
		}
		else
		{
			if (((Campaign.Current == null || Campaign.Current.GameMode != CampaignGameMode.Tutorial) && EditorGame.Current == null) || !base.DebugInput.IsHotKeyDown("MissionSingleplayerUiHandlerHotkeyUpdateItems"))
			{
				return;
			}
			MBDebug.ShowWarning("spitems.xml and mpitems.xml will be reloaded!");
			foreach (XmlNode childNode in Game.Current.ObjectManager.LoadXMLFromFileSkipValidation(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/mpitems.xml", ModuleHelper.GetModuleFullPath("Sandbox") + "ModuleData/spitems.xsd").ChildNodes[1].ChildNodes)
			{
				XmlAttributeCollection attributes = childNode.Attributes;
				if (attributes != null)
				{
					string innerText = attributes["id"].InnerText;
					ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(innerText);
					MBObjectManager.Instance.UnregisterObject(itemObject);
					itemObject?.Deserialize(Game.Current.ObjectManager, childNode);
				}
			}
			string text = BasePath.Name + "/Modules/SandBoxCore/ModuleData/spitems";
			FileInfo[] files = new DirectoryInfo(text).GetFiles("*.xml");
			foreach (FileInfo fileInfo in files)
			{
				foreach (XmlNode childNode2 in Game.Current.ObjectManager.LoadXMLFromFileSkipValidation(text + "/" + fileInfo.Name, ModuleHelper.GetModuleFullPath("Sandbox") + "ModuleData/spitems.xsd").ChildNodes[1].ChildNodes)
				{
					XmlAttributeCollection attributes2 = childNode2.Attributes;
					if (attributes2 != null)
					{
						string innerText2 = attributes2["id"].InnerText;
						ItemObject itemObject2 = Game.Current.ObjectManager.GetObject<ItemObject>(innerText2);
						MBObjectManager.Instance.UnregisterObject(itemObject2);
						itemObject2?.Deserialize(Game.Current.ObjectManager, childNode2);
					}
				}
			}
		}
	}

	private void OnInventoryScreenDone()
	{
		if (Mission.Current?.Agents == null)
		{
			return;
		}
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent == null)
			{
				continue;
			}
			CharacterObject characterObject = (CharacterObject)agent.Character;
			Campaign current2 = Campaign.Current;
			bool num;
			if (current2 == null || current2.GameMode != CampaignGameMode.Tutorial)
			{
				if (!agent.IsHuman || characterObject == null || !characterObject.IsHero)
				{
					continue;
				}
				num = characterObject.HeroObject?.PartyBelongedTo == MobileParty.MainParty;
			}
			else
			{
				if (!agent.IsMainAgent)
				{
					continue;
				}
				num = characterObject != null;
			}
			if (num)
			{
				agent.UpdateSpawnEquipmentAndRefreshVisuals(Mission.Current.DoesMissionRequireCivilianEquipment ? characterObject.FirstCivilianEquipment : characterObject.FirstBattleEquipment);
			}
		}
	}

	[Conditional("DEBUG")]
	private void OnDebugTick()
	{
		if (base.DebugInput.IsHotKeyDown("MissionSingleplayerUiHandlerHotkeyJoinEnemyTeam"))
		{
			base.Mission.JoinEnemyTeam();
		}
	}
}
