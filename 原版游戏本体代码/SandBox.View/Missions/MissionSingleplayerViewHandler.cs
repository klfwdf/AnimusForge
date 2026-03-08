using System;
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

namespace SandBox.View.Missions
{
	// Token: 0x02000021 RID: 33
	public class MissionSingleplayerViewHandler : MissionView
	{
		// Token: 0x060000D2 RID: 210 RVA: 0x0000A034 File Offset: 0x00008234
		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			if (!base.MissionScreen.SceneLayer.Input.IsCategoryRegistered(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory")))
			{
				base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			}
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x0000A088 File Offset: 0x00008288
		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			if (base.Mission != null && !base.MissionScreen.IsPhotoModeEnabled && !base.Mission.MissionEnded)
			{
				if (base.Input.IsGameKeyPressed(38))
				{
					if (base.Mission.IsInventoryAccessAllowed)
					{
						InventoryScreenHelper.OpenScreenAsInventory(new Action(this.OnInventoryScreenDone));
						return;
					}
					InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText((base.Mission.Mode == MissionMode.Battle || base.Mission.Mode == MissionMode.Duel) ? "str_cannot_reach_inventory_during_battle" : "str_cannot_reach_inventory", null).ToString()));
					return;
				}
				else if (base.Input.IsGameKeyPressed(42))
				{
					if (base.Mission.IsQuestScreenAccessAllowed)
					{
						Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<QuestsState>(), 0);
						return;
					}
					InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_quests", null).ToString()));
					return;
				}
				else if (!base.Input.IsControlDown() && base.Input.IsGameKeyPressed(43))
				{
					if (base.Mission.IsPartyWindowAccessAllowed)
					{
						PartyScreenHelper.OpenScreenAsNormal();
						return;
					}
					InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_party", null).ToString()));
					return;
				}
				else if (base.Input.IsGameKeyPressed(39))
				{
					if (base.Mission.IsEncyclopediaWindowAccessAllowed)
					{
						Campaign.Current.EncyclopediaManager.GoToLink("LastPage", "");
						return;
					}
					InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_encyclopedia", null).ToString()));
					return;
				}
				else if (base.Input.IsGameKeyPressed(40))
				{
					if (base.Mission.IsKingdomWindowAccessAllowed && Hero.MainHero.MapFaction.IsKingdomFaction)
					{
						Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<KingdomState>(), 0);
						return;
					}
					InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_kingdom", null).ToString()));
					return;
				}
				else if (base.Input.IsGameKeyPressed(41))
				{
					if (base.Mission.IsClanWindowAccessAllowed)
					{
						Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<ClanState>(), 0);
						return;
					}
					InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_clan", null).ToString()));
					return;
				}
				else
				{
					if (base.Input.IsGameKeyPressed(45))
					{
						InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_fleet", null).ToString()));
						return;
					}
					if (base.Input.IsGameKeyPressed(37))
					{
						if (base.Mission.IsCharacterWindowAccessAllowed)
						{
							Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<CharacterDeveloperState>(), 0);
							return;
						}
						InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_character", null).ToString()));
						return;
					}
					else if (base.Input.IsGameKeyPressed(36))
					{
						if (base.Mission.IsBannerWindowAccessAllowed && Campaign.Current.IsBannerEditorEnabled)
						{
							Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<BannerEditorState>(), 0);
							return;
						}
						InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_cannot_open_banner", null).ToString()));
						return;
					}
					else if (((Campaign.Current != null && Campaign.Current.GameMode == CampaignGameMode.Tutorial) || EditorGame.Current != null) && base.DebugInput.IsHotKeyDown("MissionSingleplayerUiHandlerHotkeyUpdateItems"))
					{
						MBDebug.ShowWarning("spitems.xml and mpitems.xml will be reloaded!");
						foreach (object obj in Game.Current.ObjectManager.LoadXMLFromFileSkipValidation(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/mpitems.xml", ModuleHelper.GetModuleFullPath("Sandbox") + "ModuleData/spitems.xsd").ChildNodes[1].ChildNodes)
						{
							XmlNode xmlNode = (XmlNode)obj;
							XmlAttributeCollection attributes = xmlNode.Attributes;
							if (attributes != null)
							{
								string innerText = attributes["id"].InnerText;
								ItemObject @object = Game.Current.ObjectManager.GetObject<ItemObject>(innerText);
								MBObjectManager.Instance.UnregisterObject(@object);
								if (@object != null)
								{
									@object.Deserialize(Game.Current.ObjectManager, xmlNode);
								}
							}
						}
						string text = BasePath.Name + "/Modules/SandBoxCore/ModuleData/spitems";
						foreach (FileInfo fileInfo in new DirectoryInfo(text).GetFiles("*.xml"))
						{
							foreach (object obj2 in Game.Current.ObjectManager.LoadXMLFromFileSkipValidation(text + "/" + fileInfo.Name, ModuleHelper.GetModuleFullPath("Sandbox") + "ModuleData/spitems.xsd").ChildNodes[1].ChildNodes)
							{
								XmlNode xmlNode2 = (XmlNode)obj2;
								XmlAttributeCollection attributes2 = xmlNode2.Attributes;
								if (attributes2 != null)
								{
									string innerText2 = attributes2["id"].InnerText;
									ItemObject object2 = Game.Current.ObjectManager.GetObject<ItemObject>(innerText2);
									MBObjectManager.Instance.UnregisterObject(object2);
									if (object2 != null)
									{
										object2.Deserialize(Game.Current.ObjectManager, xmlNode2);
									}
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x0000A5FC File Offset: 0x000087FC
		private void OnInventoryScreenDone()
		{
			Mission mission = Mission.Current;
			if (((mission != null) ? mission.Agents : null) != null)
			{
				foreach (Agent agent in Mission.Current.Agents)
				{
					if (agent != null)
					{
						CharacterObject characterObject = (CharacterObject)agent.Character;
						Campaign campaign = Campaign.Current;
						bool flag;
						if (campaign == null || campaign.GameMode != CampaignGameMode.Tutorial)
						{
							if (agent.IsHuman && characterObject != null && characterObject.IsHero)
							{
								Hero heroObject = characterObject.HeroObject;
								flag = ((heroObject != null) ? heroObject.PartyBelongedTo : null) == MobileParty.MainParty;
							}
							else
							{
								flag = false;
							}
						}
						else
						{
							flag = agent.IsMainAgent && characterObject != null;
						}
						if (flag)
						{
							agent.UpdateSpawnEquipmentAndRefreshVisuals(Mission.Current.DoesMissionRequireCivilianEquipment ? characterObject.FirstCivilianEquipment : characterObject.FirstBattleEquipment);
						}
					}
				}
			}
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x0000A6F4 File Offset: 0x000088F4
		[Conditional("DEBUG")]
		private void OnDebugTick()
		{
			if (base.DebugInput.IsHotKeyDown("MissionSingleplayerUiHandlerHotkeyJoinEnemyTeam"))
			{
				base.Mission.JoinEnemyTeam();
			}
		}
	}
}
