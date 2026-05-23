using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.MissionLogics;
using SandBox.Objects;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.AreaMarkers;
using SandBox.Objects.Usables;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Objects;

namespace SandBox.View.Missions.SandBox
{
	// Token: 0x0200002C RID: 44
	public class SpawnPointDebugView : ScriptComponentBehavior
	{
		// Token: 0x06000130 RID: 304 RVA: 0x0000D6B4 File Offset: 0x0000B8B4
		protected override void OnEditorInit()
		{
			base.OnEditorInit();
			this.DetermineSceneType();
			this.AddSpawnPointsToList(false);
		}

		// Token: 0x06000131 RID: 305 RVA: 0x0000D6C9 File Offset: 0x0000B8C9
		protected override void OnInit()
		{
			base.OnInit();
			this.DetermineSceneType();
			this.AddSpawnPointsToList(false);
			base.SetScriptComponentToTick(this.GetTickRequirement());
		}

		// Token: 0x06000132 RID: 306 RVA: 0x0000D6EA File Offset: 0x0000B8EA
		public override ScriptComponentBehavior.TickRequirement GetTickRequirement()
		{
			if (SpawnPointDebugView.ActivateDebugUI || (MBEditor.IsEditModeOn && this.ActivateDebugUIEditor))
			{
				return ScriptComponentBehavior.TickRequirement.Tick | base.GetTickRequirement();
			}
			return base.GetTickRequirement();
		}

		// Token: 0x06000133 RID: 307 RVA: 0x0000D711 File Offset: 0x0000B911
		protected override void OnTick(float dt)
		{
			this.ToolMainFunction();
		}

		// Token: 0x06000134 RID: 308 RVA: 0x0000D719 File Offset: 0x0000B919
		protected override void OnEditorTick(float dt)
		{
			base.OnEditorTick(dt);
			this.ToolMainFunction();
		}

		// Token: 0x06000135 RID: 309 RVA: 0x0000D728 File Offset: 0x0000B928
		private void ToolMainFunction()
		{
			if (SpawnPointDebugView.ActivateDebugUI || (MBEditor.IsEditModeOn && this.ActivateDebugUIEditor))
			{
				this.StartImGUIWindow("Debug Window");
				if (Mission.Current != null)
				{
					this.ImGUITextArea("- Do Not Hide The Mouse Cursor When Debug Window Is Intersecting With The Center Of The Screen!! -", this._separatorNeeded, !this._onSameLineNeeded);
				}
				if (this.ImGUIButton("Scene Basic Information Tab", this._normalButton))
				{
					this.ChangeTab(true, false, false, false);
				}
				this.LeaveSpaceBetweenTabs();
				if (this.ImGUIButton("Scene Entity Check Tab", this._normalButton))
				{
					this.ChangeTab(false, true, false, false);
				}
				this.LeaveSpaceBetweenTabs();
				if (this.ImGUIButton("Navigation Mesh Check Tab", this._normalButton))
				{
					this.ChangeTab(false, false, true, false);
				}
				if (this.ImGUIButton("Navigation Mesh Can Walkable Check Tab", this._normalButton))
				{
					this.ChangeTab(false, false, false, true);
				}
				if (this._entityInformationTab)
				{
					this.ShowEntityInformationTab();
				}
				if (this._basicInformationTab)
				{
					this.ShowBasicInformationTab();
				}
				if (this._navigationMeshCheckTab)
				{
					this.ShowNavigationCheckTab();
				}
				if (this._inaccessiblePositionCheckTab)
				{
					this.CheckInaccessiblePoint();
				}
				if (this._relatedEntityWindow)
				{
					this.ShowRelatedEntity();
				}
				if (this._workshopAndAlleyConflictWindow)
				{
					this.ShowWorkshopAndAlleyConflictWindow();
				}
				this.ImGUITextArea("If there are more than one 'SpawnPointDebugView' in the scene, please remove them.", this._separatorNeeded, !this._onSameLineNeeded);
				this.ImGUITextArea("If you have any questions about this tool feel free to ask Campaign team.", this._separatorNeeded, !this._onSameLineNeeded);
				this.EndImGUIWindow();
			}
		}

		// Token: 0x06000136 RID: 310 RVA: 0x0000D88C File Offset: 0x0000BA8C
		private void ShowWorkshopAndAlleyConflictWindow()
		{
			this.StartImGUIWindow("Warning Window");
			this.ImGUITextArea(this._problematicAreaMarkerWarningText, !this._separatorNeeded, !this._onSameLineNeeded);
			if (this.ImGUIButton("Close Tab", this._normalButton))
			{
				this._workshopAndAlleyConflictWindow = false;
				this._problematicAreaMarkerWarningText = "";
			}
			this.EndImGUIWindow();
		}

		// Token: 0x06000137 RID: 311 RVA: 0x0000D8F0 File Offset: 0x0000BAF0
		private void ShowRelatedEntity()
		{
			this.StartImGUIWindow("Entity Window");
			if (this.ImGUIButton("Close Tab", this._normalButton))
			{
				this._relatedEntityWindow = false;
			}
			this.ImGUITextArea("Please expand the window!", !this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("Prefabs with '" + this._relatedPrefabTag + "' tags are listed.", this._separatorNeeded, !this._onSameLineNeeded);
			this.FindAllPrefabsWithSelectedTag();
			this.EndImGUIWindow();
		}

		// Token: 0x06000138 RID: 312 RVA: 0x0000D978 File Offset: 0x0000BB78
		private void ShowBasicInformationTab()
		{
			this.ImGUITextArea("Tool tried to detect the scene type. If scene type is not correct or not determined", !this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("please select the scene type from toggle buttons below.", this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("Scene Type: " + this._sceneType + " ", !this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("Scene Name: " + this._sceneName + " ", !this._separatorNeeded, !this._onSameLineNeeded);
			this.HandleRadioButtons();
		}

		// Token: 0x06000139 RID: 313 RVA: 0x0000DA24 File Offset: 0x0000BC24
		private void HandleRadioButtons()
		{
			if (this.ImGUIButton("Town Center", this._townCenterRadioButton))
			{
				this._sceneType = SpawnPointUnits.SceneType.Center;
				this._townCenterRadioButton = false;
				this._tavernRadioButton = false;
				this._villageRadioButton = false;
				this._arenaRadioButton = false;
				this._lordshallRadioButton = false;
				this._castleRadioButton = false;
				this.AddSpawnPointsToList(true);
			}
			if (this.ImGUIButton("Tavern", this._tavernRadioButton))
			{
				this._sceneType = SpawnPointUnits.SceneType.Tavern;
				this._tavernRadioButton = false;
				this._townCenterRadioButton = false;
				this._villageRadioButton = false;
				this._arenaRadioButton = false;
				this._lordshallRadioButton = false;
				this._castleRadioButton = false;
				this.AddSpawnPointsToList(true);
			}
			if (this.ImGUIButton("Village", this._villageRadioButton))
			{
				this._sceneType = SpawnPointUnits.SceneType.VillageCenter;
				this._villageRadioButton = false;
				this._townCenterRadioButton = false;
				this._tavernRadioButton = false;
				this._arenaRadioButton = false;
				this._lordshallRadioButton = false;
				this.AddSpawnPointsToList(true);
			}
			if (this.ImGUIButton("Arena", this._arenaRadioButton))
			{
				this._sceneType = SpawnPointUnits.SceneType.Arena;
				this._arenaRadioButton = false;
				this._townCenterRadioButton = false;
				this._tavernRadioButton = false;
				this._villageRadioButton = false;
				this._lordshallRadioButton = false;
				this._castleRadioButton = false;
				this.AddSpawnPointsToList(true);
			}
			if (this.ImGUIButton("Lords Hall", this._lordshallRadioButton))
			{
				this._sceneType = SpawnPointUnits.SceneType.LordsHall;
				this._lordshallRadioButton = false;
				this._townCenterRadioButton = false;
				this._tavernRadioButton = false;
				this._villageRadioButton = false;
				this._arenaRadioButton = false;
				this._castleRadioButton = false;
				this.AddSpawnPointsToList(true);
			}
			if (this.ImGUIButton("Castle", this._castleRadioButton))
			{
				this._sceneType = SpawnPointUnits.SceneType.Castle;
				this._castleRadioButton = false;
				this._lordshallRadioButton = false;
				this._townCenterRadioButton = false;
				this._tavernRadioButton = false;
				this._villageRadioButton = false;
				this._arenaRadioButton = false;
				this.AddSpawnPointsToList(true);
			}
		}

		// Token: 0x0600013A RID: 314 RVA: 0x0000DBEC File Offset: 0x0000BDEC
		private void ChangeTab(bool basicInformationTab, bool entityInformationTab, bool navigationMeshCheckTab, bool navigationMeshCanWalkCheckTab)
		{
			this._basicInformationTab = basicInformationTab;
			this._entityInformationTab = entityInformationTab;
			this._navigationMeshCheckTab = navigationMeshCheckTab;
			this._inaccessiblePositionCheckTab = navigationMeshCanWalkCheckTab;
		}

		// Token: 0x0600013B RID: 315 RVA: 0x0000DC0C File Offset: 0x0000BE0C
		private void DetermineSceneType()
		{
			this._sceneName = base.Scene.GetName();
			if (this._sceneName.Contains("tavern"))
			{
				this._sceneType = SpawnPointUnits.SceneType.Tavern;
				return;
			}
			if (this._sceneName.Contains("lords_hall") || (this._sceneName.Contains("interior") && (this._sceneName.Contains("lords_hall") || this._sceneName.Contains("castle") || this._sceneName.Contains("keep"))))
			{
				this._sceneType = SpawnPointUnits.SceneType.LordsHall;
				return;
			}
			if (this._sceneName.Contains("village"))
			{
				this._sceneType = SpawnPointUnits.SceneType.VillageCenter;
				return;
			}
			if (this._sceneName.Contains("town") || this._sceneName.Contains("city"))
			{
				this._sceneType = SpawnPointUnits.SceneType.Center;
				return;
			}
			if (this._sceneName.Contains("shipyard"))
			{
				this._sceneType = SpawnPointUnits.SceneType.Shipyard;
				return;
			}
			if (this._sceneName.Contains("dungeon"))
			{
				this._sceneType = SpawnPointUnits.SceneType.Dungeon;
				return;
			}
			if (this._sceneName.Contains("hippodrome") || this._sceneName.Contains("arena"))
			{
				this._sceneType = SpawnPointUnits.SceneType.Arena;
				return;
			}
			if (this._sceneName.Contains("castle") || this._sceneName.Contains("siege"))
			{
				this._sceneType = SpawnPointUnits.SceneType.Castle;
				return;
			}
			if (this._sceneName.Contains("interior"))
			{
				this._sceneType = SpawnPointUnits.SceneType.EmptyShop;
				return;
			}
			this._sceneType = SpawnPointUnits.SceneType.NotDetermined;
		}

		// Token: 0x0600013C RID: 316 RVA: 0x0000DD9C File Offset: 0x0000BF9C
		private void AddSpawnPointsToList(bool alreadyInitialized)
		{
			this._spUnitsList.Clear();
			if (this._sceneType == SpawnPointUnits.SceneType.Center)
			{
				this._spUnitsList.Add(new SpawnPointUnits("spawnpoint_player_outside", SpawnPointUnits.SceneType.Center, "npc", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("alley_1_population", SpawnPointUnits.SceneType.Center, 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("alley_2_population", SpawnPointUnits.SceneType.Center, 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("alley_3_population", SpawnPointUnits.SceneType.Center, 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("center_conversation_point", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_alley_1", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_alley_2", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_alley_3", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("workshop_area_1_population", SpawnPointUnits.SceneType.Center, 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("workshop_area_2_population", SpawnPointUnits.SceneType.Center, 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("workshop_area_3_population", SpawnPointUnits.SceneType.Center, 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_workshop_1", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_workshop_2", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_workshop_3", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("workshop_1_notable_parent", SpawnPointUnits.SceneType.Center, 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("workshop_2_notable_parent", SpawnPointUnits.SceneType.Center, 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("workshop_3_notable_parent", SpawnPointUnits.SceneType.Center, 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("navigation_mesh_deactivator", SpawnPointUnits.SceneType.Center, 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("alley_marker", SpawnPointUnits.SceneType.Center, 3, 3));
				this._spUnitsList.Add(new SpawnPointUnits("workshop_area_marker", SpawnPointUnits.SceneType.Center, 3, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_outside_near_town_main_gate", SpawnPointUnits.SceneType.Center, "npc", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("npc_dancer", SpawnPointUnits.SceneType.Center, "npc", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("spawnpoint_cleaner", SpawnPointUnits.SceneType.Center, "npc", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("npc_beggar", SpawnPointUnits.SceneType.Center, "npc", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_notable_artisan", SpawnPointUnits.SceneType.Center, "npc", 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_notable_gangleader", SpawnPointUnits.SceneType.Center, "npc", 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_notable_merchant", SpawnPointUnits.SceneType.Center, "npc", 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_notable_preacher", SpawnPointUnits.SceneType.Center, "npc", 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_merchant", SpawnPointUnits.SceneType.Center, "npc", 1, 25));
				this._spUnitsList.Add(new SpawnPointUnits("sp_armorer", SpawnPointUnits.SceneType.Center, "npc", 1, 25));
				this._spUnitsList.Add(new SpawnPointUnits("sp_blacksmith", SpawnPointUnits.SceneType.Center, "npc", 1, 25));
				this._spUnitsList.Add(new SpawnPointUnits("sp_weaponsmith", SpawnPointUnits.SceneType.Center, "npc", 1, 25));
				this._spUnitsList.Add(new SpawnPointUnits("sp_horse_merchant", SpawnPointUnits.SceneType.Center, "npc", 1, 25));
				this._spUnitsList.Add(new SpawnPointUnits("sp_guard", SpawnPointUnits.SceneType.Center, "npc", 2, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_guard_castle", SpawnPointUnits.SceneType.Center, "npc", 1, 2));
				this._spUnitsList.Add(new SpawnPointUnits("sp_prison_guard", SpawnPointUnits.SceneType.Center, "npc", 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_guard_patrol", SpawnPointUnits.SceneType.Center, "npc", 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_guard_unarmed", SpawnPointUnits.SceneType.Center, "npc", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_tavern_wench", SpawnPointUnits.SceneType.Center, "npc", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("spawnpoint_tavernkeeper", SpawnPointUnits.SceneType.Center, "npc", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_barber", SpawnPointUnits.SceneType.Center, "npc", 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_player_conversation", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("npc_passage_tavern", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("npc_passage_arena", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("npc_passage_lordshall", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("npc_passage_prison", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("npc_passage_house_1", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("npc_passage_house_2", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("npc_passage_house_3", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("desert_war_horse", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("steppe_charger", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("war_horse", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("charger", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("desert_horse", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("hunter", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_sheep", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_cow", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_hog", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_goose", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
			}
			else if (this._sceneType == SpawnPointUnits.SceneType.Shipyard)
			{
				this._spUnitsList.Add(new SpawnPointUnits("navigation_mesh_deactivator", SpawnPointUnits.SceneType.Shipyard, 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("sp_shipwright", SpawnPointUnits.SceneType.Shipyard, "npc", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("shipyard_worker", SpawnPointUnits.SceneType.Shipyard, "npc", 1, 1));
			}
			else if (this._sceneType == SpawnPointUnits.SceneType.Tavern)
			{
				this._spUnitsList.Add(new SpawnPointUnits("musician", SpawnPointUnits.SceneType.Tavern, "npc", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_tavern_wench", SpawnPointUnits.SceneType.Tavern, "npc", 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("spawnpoint_tavernkeeper", SpawnPointUnits.SceneType.Tavern, "npc", 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("spawnpoint_mercenary", SpawnPointUnits.SceneType.Tavern, "npc", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("gambler_npc", SpawnPointUnits.SceneType.Tavern, "npc", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("npc_passage_center", SpawnPointUnits.SceneType.Tavern, "passage", 1, 1));
			}
			else if (this._sceneType == SpawnPointUnits.SceneType.VillageCenter)
			{
				this._spUnitsList.Add(new SpawnPointUnits("sp_notable", SpawnPointUnits.SceneType.VillageCenter, "notable", 6, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_notable_rural_notable", SpawnPointUnits.SceneType.VillageCenter, "npc", 6, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_player_conversation", SpawnPointUnits.SceneType.VillageCenter, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("alley_1_population", SpawnPointUnits.SceneType.VillageCenter, 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("alley_2_population", SpawnPointUnits.SceneType.VillageCenter, 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("alley_3_population", SpawnPointUnits.SceneType.VillageCenter, 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("center_conversation_point", SpawnPointUnits.SceneType.VillageCenter, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_alley_1", SpawnPointUnits.SceneType.VillageCenter, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_alley_2", SpawnPointUnits.SceneType.VillageCenter, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_alley_3", SpawnPointUnits.SceneType.VillageCenter, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("alley_marker", SpawnPointUnits.SceneType.VillageCenter, 3, 3));
				this._spUnitsList.Add(new SpawnPointUnits("battle_set", SpawnPointUnits.SceneType.VillageCenter, 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("navigation_mesh_deactivator", SpawnPointUnits.SceneType.VillageCenter, 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("desert_war_horse", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("steppe_charger", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("war_horse", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("charger", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("desert_horse", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("hunter", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_sheep", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_cow", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_hog", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_goose", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
			}
			else if (this._sceneType == SpawnPointUnits.SceneType.Arena)
			{
				this._spUnitsList.Add(new SpawnPointUnits("spawnpoint_tournamentmaster", SpawnPointUnits.SceneType.Arena, "npc", 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_player_near_arena_master", SpawnPointUnits.SceneType.Arena, "npc", 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("tournament_archery", SpawnPointUnits.SceneType.Arena, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("tournament_fight", SpawnPointUnits.SceneType.Arena, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("tournament_jousting", SpawnPointUnits.SceneType.Arena, 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("npc_passage_center", SpawnPointUnits.SceneType.Arena, "passage", 1, 1));
			}
			else if (this._sceneType == SpawnPointUnits.SceneType.LordsHall)
			{
				this._spUnitsList.Add(new SpawnPointUnits("battle_set", SpawnPointUnits.SceneType.LordsHall, 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("sp_guard", SpawnPointUnits.SceneType.LordsHall, "npc", 2, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_notable", SpawnPointUnits.SceneType.LordsHall, "npc", 10, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_king", SpawnPointUnits.SceneType.LordsHall, "npc", 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_throne", SpawnPointUnits.SceneType.LordsHall, "npc", 1, 2));
				this._spUnitsList.Add(new SpawnPointUnits("npc_passage_center", SpawnPointUnits.SceneType.LordsHall, "passage", 1, 1));
			}
			else if (this._sceneType == SpawnPointUnits.SceneType.Castle)
			{
				this._spUnitsList.Add(new SpawnPointUnits("sp_prison_guard", SpawnPointUnits.SceneType.Castle, "npc", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("sp_guard", SpawnPointUnits.SceneType.Castle, "npc", 2, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_guard_castle", SpawnPointUnits.SceneType.Castle, "npc", 1, 2));
				this._spUnitsList.Add(new SpawnPointUnits("sp_guard_patrol", SpawnPointUnits.SceneType.Castle, "npc", 1, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_guard_unarmed", SpawnPointUnits.SceneType.Castle, "npc", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("npc_passage_lordshall", SpawnPointUnits.SceneType.Castle, "passage", 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("sp_player_conversation", SpawnPointUnits.SceneType.Castle, 1, int.MaxValue));
			}
			else if (this._sceneType == SpawnPointUnits.SceneType.Dungeon)
			{
				this._spUnitsList.Add(new SpawnPointUnits("sp_guard", SpawnPointUnits.SceneType.Dungeon, "npc", 2, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_guard_patrol", SpawnPointUnits.SceneType.Dungeon, "npc", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_guard_unarmed", SpawnPointUnits.SceneType.Dungeon, "npc", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("npc_passage_center", SpawnPointUnits.SceneType.Castle, "passage", 1, 1));
			}
			if (!alreadyInitialized)
			{
				this._spUnitsList.Add(new SpawnPointUnits("spawnpoint_player", SpawnPointUnits.SceneType.All, 1, 1));
				this._spUnitsList.Add(new SpawnPointUnits("npc_common", SpawnPointUnits.SceneType.All, "npc", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("npc_common_limited", SpawnPointUnits.SceneType.All, "npc", 0, int.MaxValue));
				this._spUnitsList.Add(new SpawnPointUnits("sp_npc", SpawnPointUnits.SceneType.All, "DONTUSE", 0, 0));
				this._spUnitsList.Add(new SpawnPointUnits("spawnpoint_elder", SpawnPointUnits.SceneType.VillageCenter, "DONTUSE", 0, 0));
			}
			this._invalidSpawnPointsDictionary.Clear();
			this._invalidSpawnPointsDictionary.Add(SpawnPointDebugView.CategoryId.NPC, new List<SpawnPointDebugView.InvalidPosition>());
			this._invalidSpawnPointsDictionary.Add(SpawnPointDebugView.CategoryId.Animal, new List<SpawnPointDebugView.InvalidPosition>());
			this._invalidSpawnPointsDictionary.Add(SpawnPointDebugView.CategoryId.Chair, new List<SpawnPointDebugView.InvalidPosition>());
			this._invalidSpawnPointsDictionary.Add(SpawnPointDebugView.CategoryId.Passage, new List<SpawnPointDebugView.InvalidPosition>());
			this._invalidSpawnPointsDictionary.Add(SpawnPointDebugView.CategoryId.SemivalidChair, new List<SpawnPointDebugView.InvalidPosition>());
		}

		// Token: 0x0600013D RID: 317 RVA: 0x0000ECAC File Offset: 0x0000CEAC
		private List<List<string>> GetLevelCombinationsToCheck()
		{
			base.GameEntity.Scene.GetName();
			bool flag = base.GameEntity.Scene.GetUpgradeLevelMaskOfLevelName("siege") > 0U;
			List<List<string>> list = new List<List<string>>();
			if (flag)
			{
				list.Add(new List<string>());
				list[0].Add("level_1");
				list[0].Add("civilian");
				list.Add(new List<string>());
				list[1].Add("level_2");
				list[1].Add("civilian");
				list.Add(new List<string>());
				list[2].Add("level_3");
				list[2].Add("civilian");
			}
			else
			{
				list.Add(new List<string>());
				list[0].Add("base");
			}
			return list;
		}

		// Token: 0x0600013E RID: 318 RVA: 0x0000ED99 File Offset: 0x0000CF99
		protected override void OnSceneSave(string saveFolder)
		{
			base.OnSceneSave(saveFolder);
			this.OnCheckForProblems();
		}

		// Token: 0x0600013F RID: 319 RVA: 0x0000EDAC File Offset: 0x0000CFAC
		protected override bool OnCheckForProblems()
		{
			base.OnCheckForProblems();
			this.GetDisableFaceID();
			bool flag = false;
			if (this._sceneType == SpawnPointUnits.SceneType.NotDetermined)
			{
				flag = true;
				MBEditor.AddEntityWarning(base.GameEntity, "Scene type could not be determined");
			}
			uint upgradeLevelMask = base.GameEntity.Scene.GetUpgradeLevelMask();
			foreach (List<string> list in this.GetLevelCombinationsToCheck())
			{
				string text = "";
				for (int i = 0; i < list.Count - 1; i++)
				{
					text = text + list[i] + "|";
				}
				text += list[list.Count - 1];
				base.GameEntity.Scene.SetUpgradeLevelVisibility(list);
				this.CountEntities();
				foreach (SpawnPointUnits spawnPointUnits in this._spUnitsList)
				{
					if (spawnPointUnits.Place == SpawnPointUnits.SceneType.All || this._sceneType == spawnPointUnits.Place)
					{
						bool flag2 = spawnPointUnits.CurrentCount <= spawnPointUnits.MaxCount && spawnPointUnits.CurrentCount >= spawnPointUnits.MinCount;
						flag |= !flag2;
						if (!flag2)
						{
							string text2 = "Spawnpoint (" + spawnPointUnits.SpName + ") has some issues. ";
							if (spawnPointUnits.MaxCount < spawnPointUnits.CurrentCount)
							{
								text2 = string.Concat(new object[] { text2, "It is placed too much. Placed count(", spawnPointUnits.CurrentCount, "). Max count(", spawnPointUnits.MaxCount, "). Level: ", text });
							}
							else
							{
								text2 = string.Concat(new object[] { text2, "It is placed too less. Placed count(", spawnPointUnits.CurrentCount, "). Min count(", spawnPointUnits.MinCount, "). Level: ", text });
							}
							MBEditor.AddEntityWarning(base.GameEntity, text2);
						}
					}
				}
				if (!string.IsNullOrEmpty(this._problematicAreaMarkerWarningText))
				{
					MBEditor.AddEntityWarning(base.GameEntity, this._problematicAreaMarkerWarningText);
				}
			}
			base.GameEntity.Scene.SetUpgradeLevelVisibility(upgradeLevelMask);
			this.CheckForNavigationMesh();
			foreach (List<SpawnPointDebugView.InvalidPosition> list2 in this._invalidSpawnPointsDictionary.Values)
			{
				foreach (SpawnPointDebugView.InvalidPosition invalidPosition in list2)
				{
					if (!invalidPosition.doNotShowWarning)
					{
						string msg;
						if (invalidPosition.isDisabledNavMesh)
						{
							msg = string.Concat(new object[]
							{
								"Special entity with name (",
								invalidPosition.entity.Name,
								") has a navigation mesh below which is deactivated by the deactivater script. Position ",
								invalidPosition.position.x,
								" , ",
								invalidPosition.position.y,
								" , ",
								invalidPosition.position.z,
								"."
							});
						}
						else
						{
							msg = string.Concat(new object[]
							{
								"Special entity with name (",
								invalidPosition.entity.Name,
								") has no navigation mesh below. Position ",
								invalidPosition.position.x,
								" , ",
								invalidPosition.position.y,
								" , ",
								invalidPosition.position.z,
								"."
							});
						}
						MBEditor.AddEntityWarning(invalidPosition.entity.WeakEntity, msg);
						flag = true;
					}
				}
			}
			return flag;
		}

		// Token: 0x06000140 RID: 320 RVA: 0x0000F238 File Offset: 0x0000D438
		private void ShowEntityInformationTab()
		{
			this.ImGUITextArea("This tab calculates the spawnpoint counts and warns you if", !this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("counts are not in the given criteria.", this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("Click 'Count Entities' button to calculate and toggle categories.", this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("You can use the list button to list all the prefabs with tag.", this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("Current Townsfolk count: " + this._currentTownsfolkCount, this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUICheckBox("NPCs ", ref this._showNPCsList, !this._separatorNeeded, this._onSameLineNeeded);
			this.ImGUICheckBox("Animals ", ref this._showAnimalsList, !this._separatorNeeded, this._onSameLineNeeded);
			this.ImGUICheckBox("Passages ", ref this._showPassagesList, !this._separatorNeeded, this._onSameLineNeeded);
			this.ImGUICheckBox("Others ", ref this._showOthersList, !this._separatorNeeded, this._onSameLineNeeded);
			this.ImGUICheckBox("DONT USE ", ref this._showDontUseList, this._separatorNeeded, !this._onSameLineNeeded);
			this.WriteTableHeaders();
			foreach (SpawnPointUnits spawnPointUnits in this._spUnitsList)
			{
				if (spawnPointUnits.Place == SpawnPointUnits.SceneType.All)
				{
					if (spawnPointUnits.CurrentCount > spawnPointUnits.MaxCount || spawnPointUnits.CurrentCount < spawnPointUnits.MinCount)
					{
						this.WriteLineOfTableDebug(spawnPointUnits, this._redColor, spawnPointUnits.Type);
					}
					else
					{
						this.WriteLineOfTableDebug(spawnPointUnits, this._greenColor, spawnPointUnits.Type);
					}
				}
				else if (this._sceneType == spawnPointUnits.Place)
				{
					if (spawnPointUnits.CurrentCount > spawnPointUnits.MaxCount || spawnPointUnits.CurrentCount < spawnPointUnits.MinCount)
					{
						this.WriteLineOfTableDebug(spawnPointUnits, this._redColor, spawnPointUnits.Type);
					}
					else
					{
						this.WriteLineOfTableDebug(spawnPointUnits, this._greenColor, spawnPointUnits.Type);
					}
				}
			}
			if (this.ImGUIButton("COUNT ENTITIES", this._normalButton))
			{
				this.CountEntities();
			}
		}

		// Token: 0x06000141 RID: 321 RVA: 0x0000F47C File Offset: 0x0000D67C
		private void CalculateSpawnedAgentCount(SpawnPointUnits spUnit)
		{
			if (spUnit.SpName == "npc_common")
			{
				spUnit.SpawnedAgentCount = (int)((float)spUnit.CurrentCount * 0.2f + 0.15f);
			}
			else if (spUnit.SpName == "npc_common_limited")
			{
				spUnit.SpawnedAgentCount = (int)((float)spUnit.CurrentCount * 0.15f + 0.1f);
			}
			else if (spUnit.SpName == "npc_beggar")
			{
				spUnit.SpawnedAgentCount = (int)((float)spUnit.CurrentCount * 0.33f);
			}
			else if (spUnit.SpName == "spawnpoint_cleaner" || spUnit.SpName == "npc_dancer" || spUnit.SpName == "sp_guard_patrol" || spUnit.SpName == "sp_guard")
			{
				spUnit.SpawnedAgentCount = spUnit.CurrentCount;
			}
			else if (spUnit.CurrentCount != 0)
			{
				spUnit.SpawnedAgentCount = 1;
			}
			this._currentTownsfolkCount += spUnit.SpawnedAgentCount;
		}

		// Token: 0x06000142 RID: 322 RVA: 0x0000F58C File Offset: 0x0000D78C
		private void CountEntities()
		{
			this.GetDisableFaceID();
			this._currentTownsfolkCount = 0;
			foreach (SpawnPointUnits spawnPointUnits in this._spUnitsList.ToList<SpawnPointUnits>())
			{
				List<GameEntity> list = base.Scene.FindEntitiesWithTag(spawnPointUnits.SpName).ToList<GameEntity>();
				int num = 0;
				using (List<GameEntity>.Enumerator enumerator2 = list.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						if (enumerator2.Current.GetEditModeLevelVisibility())
						{
							num++;
						}
					}
				}
				spawnPointUnits.CurrentCount = num;
				this.CalculateSpawnedAgentCount(spawnPointUnits);
				this.CountPassages(spawnPointUnits);
				using (List<GameEntity>.Enumerator enumerator2 = list.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						if (enumerator2.Current.IsGhostObject())
						{
							spawnPointUnits.CurrentCount--;
						}
					}
				}
				if (spawnPointUnits.SpName == "alley_marker")
				{
					this.CheckForCommonAreas(list, spawnPointUnits);
				}
				else if (spawnPointUnits.SpName == "workshop_area_marker")
				{
					this.CheckForWorkshops(list, spawnPointUnits);
				}
				else if (spawnPointUnits.SpName == "center_conversation_point")
				{
					List<GameEntity> list2 = base.Scene.FindEntitiesWithTag("sp_player_conversation").ToList<GameEntity>();
					List<GameEntity> list3 = base.Scene.FindEntitiesWithTag("alley_marker").ToList<GameEntity>();
					foreach (GameEntity gameEntity in list2)
					{
						bool flag = false;
						using (List<GameEntity>.Enumerator enumerator3 = list3.GetEnumerator())
						{
							while (enumerator3.MoveNext())
							{
								if (enumerator3.Current.GetFirstScriptOfType<CommonAreaMarker>().IsPositionInRange(gameEntity.GlobalPosition))
								{
									flag = true;
									break;
								}
							}
						}
						if (!flag)
						{
							SpawnPointUnits spawnPointUnits2 = this._spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "center_conversation_point" && x.Place == this._sceneType);
							if (spawnPointUnits2 != null)
							{
								spawnPointUnits2.CurrentCount++;
							}
						}
					}
				}
			}
			IEnumerable<CommonAreaMarker> enumerable = from x in base.Scene.FindEntitiesWithTag("alley_marker")
				select x.GetFirstScriptOfType<CommonAreaMarker>();
			IEnumerable<WorkshopAreaMarker> enumerable2 = from x in base.Scene.FindEntitiesWithTag("workshop_area_marker")
				select x.GetFirstScriptOfType<WorkshopAreaMarker>();
			foreach (CommonAreaMarker commonAreaMarker in enumerable)
			{
				foreach (WorkshopAreaMarker workshopAreaMarker in enumerable2)
				{
					if (commonAreaMarker.GetPosition().Distance(workshopAreaMarker.GetPosition()) < commonAreaMarker.AreaRadius + workshopAreaMarker.AreaRadius)
					{
						this._workshopAndAlleyConflictWindow = true;
						this._problematicAreaMarkerWarningText = string.Concat(new object[]
						{
							"The areas of Alley Marker at position:\n",
							commonAreaMarker.GetPosition(),
							"\nand Workshop Marker at position:\n",
							workshopAreaMarker.GetPosition(),
							"\nintersects! \nPlease move one of them or\ndecrease their radius accordingly!"
						});
						break;
					}
				}
			}
		}

		// Token: 0x06000143 RID: 323 RVA: 0x0000F998 File Offset: 0x0000DB98
		private void CheckForCommonAreas(IEnumerable<GameEntity> allGameEntitiesWithGivenTag, SpawnPointUnits spUnit)
		{
			foreach (GameEntity gameEntity in allGameEntitiesWithGivenTag)
			{
				CommonAreaMarker alleyMarker = gameEntity.GetFirstScriptOfType<CommonAreaMarker>();
				if (alleyMarker != null && !gameEntity.IsGhostObject())
				{
					float areaRadius = alleyMarker.AreaRadius;
					List<GameEntity> list = base.Scene.FindEntitiesWithTag("npc_common").ToList<GameEntity>();
					foreach (GameEntity gameEntity2 in list.ToList<GameEntity>())
					{
						float num = areaRadius * areaRadius;
						if (gameEntity2.HasScriptOfType<Passage>() || !gameEntity2.IsVisibleIncludeParents() || gameEntity2.GlobalPosition.DistanceSquared(gameEntity.GlobalPosition) > num)
						{
							list.Remove(gameEntity2);
						}
					}
					List<GameEntity> list2 = base.Scene.FindEntitiesWithTag("sp_player_conversation").ToList<GameEntity>();
					int num2 = 0;
					Func<SpawnPointUnits, bool> <>9__0;
					foreach (GameEntity gameEntity3 in list2)
					{
						if (alleyMarker.IsPositionInRange(gameEntity3.GlobalPosition))
						{
							IEnumerable<SpawnPointUnits> spUnitsList = this._spUnitsList;
							Func<SpawnPointUnits, bool> predicate;
							if ((predicate = <>9__0) == null)
							{
								predicate = (<>9__0 = (SpawnPointUnits x) => x.SpName == "sp_player_conversation_alley_" + alleyMarker.AreaIndex && x.Place == this._sceneType);
							}
							SpawnPointUnits spawnPointUnits = spUnitsList.FirstOrDefault(predicate);
							if (spawnPointUnits != null)
							{
								num2 = (spawnPointUnits.CurrentCount = num2 + 1);
							}
						}
					}
					if (alleyMarker.AreaIndex == 1)
					{
						SpawnPointUnits spawnPointUnits2 = this._spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "alley_1_population" && x.Place == this._sceneType);
						if (spawnPointUnits2 != null)
						{
							spawnPointUnits2.CurrentCount = this.FindValidSpawnPointCountOfUsableMachine(list);
						}
					}
					else if (alleyMarker.AreaIndex == 2)
					{
						SpawnPointUnits spawnPointUnits3 = this._spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "alley_2_population" && x.Place == this._sceneType);
						if (spawnPointUnits3 != null)
						{
							spawnPointUnits3.CurrentCount = this.FindValidSpawnPointCountOfUsableMachine(list);
						}
					}
					else if (alleyMarker.AreaIndex == 3)
					{
						SpawnPointUnits spawnPointUnits4 = this._spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "alley_3_population" && x.Place == this._sceneType);
						if (spawnPointUnits4 != null)
						{
							spawnPointUnits4.CurrentCount = this.FindValidSpawnPointCountOfUsableMachine(list);
						}
					}
				}
			}
		}

		// Token: 0x06000144 RID: 324 RVA: 0x0000FC28 File Offset: 0x0000DE28
		private void CheckForWorkshops(IEnumerable<GameEntity> allGameEntitiesWithGivenTag, SpawnPointUnits spUnit)
		{
			foreach (GameEntity gameEntity in allGameEntitiesWithGivenTag)
			{
				WorkshopAreaMarker workshopAreaMarker = gameEntity.GetFirstScriptOfType<WorkshopAreaMarker>();
				if (workshopAreaMarker != null && !gameEntity.IsGhostObject())
				{
					float areaRadius = workshopAreaMarker.AreaRadius;
					List<GameEntity> list = new List<GameEntity>();
					base.Scene.GetEntities(ref list);
					float num = areaRadius * areaRadius;
					foreach (GameEntity gameEntity2 in list.ToList<GameEntity>())
					{
						if (!gameEntity2.HasScriptOfType<UsableMachine>() || gameEntity2.HasScriptOfType<Passage>() || gameEntity2.GlobalPosition.DistanceSquared(gameEntity.GlobalPosition) > num)
						{
							list.Remove(gameEntity2);
						}
					}
					using (List<GameEntity>.Enumerator enumerator2 = base.Scene.FindEntitiesWithTag("sp_notables_parent").ToList<GameEntity>().GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							if (enumerator2.Current.GlobalPosition.DistanceSquared(gameEntity.GlobalPosition) < num)
							{
								if (workshopAreaMarker.AreaIndex == 1)
								{
									SpawnPointUnits spawnPointUnits = this._spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "workshop_1_notable_parent" && x.Place == this._sceneType);
									if (spawnPointUnits != null)
									{
										spawnPointUnits.CurrentCount = 1;
									}
								}
								else if (workshopAreaMarker.AreaIndex == 2)
								{
									SpawnPointUnits spawnPointUnits2 = this._spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "workshop_2_notable_parent" && x.Place == this._sceneType);
									if (spawnPointUnits2 != null)
									{
										spawnPointUnits2.CurrentCount = 1;
									}
								}
								else if (workshopAreaMarker.AreaIndex == 3)
								{
									SpawnPointUnits spawnPointUnits3 = this._spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "workshop_3_notable_parent" && x.Place == this._sceneType);
									if (spawnPointUnits3 != null)
									{
										spawnPointUnits3.CurrentCount = 1;
									}
								}
							}
						}
					}
					List<GameEntity> list2 = base.Scene.FindEntitiesWithTag("sp_player_conversation").ToList<GameEntity>();
					int num2 = 0;
					Func<SpawnPointUnits, bool> <>9__3;
					foreach (GameEntity gameEntity3 in list2)
					{
						if (workshopAreaMarker.IsPositionInRange(gameEntity3.GlobalPosition))
						{
							IEnumerable<SpawnPointUnits> spUnitsList = this._spUnitsList;
							Func<SpawnPointUnits, bool> predicate;
							if ((predicate = <>9__3) == null)
							{
								predicate = (<>9__3 = (SpawnPointUnits x) => x.SpName == "sp_player_conversation_workshop_" + workshopAreaMarker.AreaIndex && x.Place == this._sceneType);
							}
							SpawnPointUnits spawnPointUnits4 = spUnitsList.FirstOrDefault(predicate);
							if (spawnPointUnits4 != null)
							{
								num2 = (spawnPointUnits4.CurrentCount = num2 + 1);
							}
						}
					}
					if (workshopAreaMarker.AreaIndex == 1)
					{
						SpawnPointUnits spawnPointUnits5 = this._spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "workshop_area_1_population" && x.Place == this._sceneType);
						if (spawnPointUnits5 != null)
						{
							spawnPointUnits5.CurrentCount += this.FindValidSpawnPointCountOfUsableMachine(list);
						}
					}
					else if (workshopAreaMarker.AreaIndex == 2)
					{
						SpawnPointUnits spawnPointUnits6 = this._spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "workshop_area_2_population" && x.Place == this._sceneType);
						if (spawnPointUnits6 != null)
						{
							spawnPointUnits6.CurrentCount += this.FindValidSpawnPointCountOfUsableMachine(list);
						}
					}
					else if (workshopAreaMarker.AreaIndex == 3)
					{
						SpawnPointUnits spawnPointUnits7 = this._spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "workshop_area_3_population" && x.Place == this._sceneType);
						if (spawnPointUnits7 != null)
						{
							spawnPointUnits7.CurrentCount += this.FindValidSpawnPointCountOfUsableMachine(list);
						}
					}
				}
			}
		}

		// Token: 0x06000145 RID: 325 RVA: 0x0000FFD8 File Offset: 0x0000E1D8
		private int FindValidSpawnPointCountOfUsableMachine(List<GameEntity> gameEntities)
		{
			int num = 0;
			foreach (GameEntity gameEntity in gameEntities)
			{
				UsableMachine firstScriptOfType = gameEntity.GetFirstScriptOfType<UsableMachine>();
				if (firstScriptOfType != null)
				{
					num += MissionAgentHandler.GetPointCountOfUsableMachine(firstScriptOfType, false);
				}
			}
			return num;
		}

		// Token: 0x06000146 RID: 326 RVA: 0x00010034 File Offset: 0x0000E234
		private void CountPassages(SpawnPointUnits spUnit)
		{
			if (spUnit.SpName.Contains("npc_passage"))
			{
				foreach (GameEntity gameEntity in base.Scene.FindEntitiesWithTag("npc_passage"))
				{
					foreach (GameEntity gameEntity2 in gameEntity.GetChildren())
					{
						PassageUsePoint firstScriptOfType = gameEntity2.GetFirstScriptOfType<PassageUsePoint>();
						if (firstScriptOfType != null && !gameEntity2.IsGhostObject() && gameEntity2.GetEditModeLevelVisibility() && (this.DetectWhichPassage(firstScriptOfType, spUnit.SpName, "tavern") || this.DetectWhichPassage(firstScriptOfType, spUnit.SpName, "arena") || this.DetectWhichPassage(firstScriptOfType, spUnit.SpName, "prison") || this.DetectWhichPassage(firstScriptOfType, spUnit.SpName, "lordshall") || this.DetectWhichPassage(firstScriptOfType, spUnit.SpName, "house_1") || this.DetectWhichPassage(firstScriptOfType, spUnit.SpName, "house_2") || this.DetectWhichPassage(firstScriptOfType, spUnit.SpName, "house_3")))
						{
							spUnit.CurrentCount++;
						}
					}
				}
			}
		}

		// Token: 0x06000147 RID: 327 RVA: 0x000101B0 File Offset: 0x0000E3B0
		private void CalculateCurrentInvalidPointsCount()
		{
			this._currentInvalidPoints = 0;
			if (this._showAnimals)
			{
				this._currentInvalidPoints += this.GetCategoryCount(SpawnPointDebugView.CategoryId.Animal);
			}
			if (this._showChairs)
			{
				this._currentInvalidPoints += this.GetCategoryCount(SpawnPointDebugView.CategoryId.Chair);
			}
			if (this._showNPCs)
			{
				this._currentInvalidPoints += this.GetCategoryCount(SpawnPointDebugView.CategoryId.NPC);
			}
			if (this._showSemiValidPoints)
			{
				this._currentInvalidPoints += this.GetCategoryCount(SpawnPointDebugView.CategoryId.SemivalidChair);
			}
			if (this._showPassagePoints)
			{
				this._currentInvalidPoints += this.GetCategoryCount(SpawnPointDebugView.CategoryId.Passage);
			}
			if (this._showOutOfBoundPoints)
			{
				this._currentInvalidPoints += this.GetCategoryCount(SpawnPointDebugView.CategoryId.OutOfMissionBound);
			}
		}

		// Token: 0x06000148 RID: 328 RVA: 0x0001026C File Offset: 0x0000E46C
		private bool DetectWhichPassage(PassageUsePoint passageUsePoint, string spName, string locationName)
		{
			string toLocationId = passageUsePoint.ToLocationId;
			if (this._sceneType != SpawnPointUnits.SceneType.Center && this._sceneType != SpawnPointUnits.SceneType.Castle)
			{
				locationName = "center";
			}
			return toLocationId == locationName && spName == "npc_passage_" + locationName;
		}

		// Token: 0x06000149 RID: 329 RVA: 0x000102A7 File Offset: 0x0000E4A7
		private void ShowNavigationCheckTab()
		{
			this.WriteNavigationMeshTabTexts();
			this.ToggleButtons();
			this.CalculateCurrentInvalidPointsCount();
			if (this.ImGUIButton("CHECK", this._normalButton))
			{
				this.CheckForNavigationMesh();
			}
		}

		// Token: 0x0600014A RID: 330 RVA: 0x000102D4 File Offset: 0x0000E4D4
		private void CheckForNavigationMesh()
		{
			this.ClearAllLists();
			this.GetDisableFaceID();
			this.CountEntities();
			foreach (SpawnPointUnits spawnPointUnits in this._spUnitsList)
			{
				if (!(spawnPointUnits.SpName == "alley_marker") && !(spawnPointUnits.SpName == "navigation_mesh_deactivator"))
				{
					this.CheckIfPassage(spawnPointUnits);
					this.CheckIfChairOrAnimal(spawnPointUnits);
				}
			}
			this.RemoveDuplicateValuesInLists();
		}

		// Token: 0x0600014B RID: 331 RVA: 0x00010364 File Offset: 0x0000E564
		private void CheckNavigationMeshForParticularEntity(GameEntity gameEntity, SpawnPointDebugView.CategoryId categoryId)
		{
			if (gameEntity.Name == "workshop_1" || gameEntity.Name == "workshop_2" || gameEntity.Name == "workshop_3")
			{
				return;
			}
			Vec3 origin = gameEntity.GetGlobalFrame().origin;
			if (!gameEntity.HasScriptOfType<NavigationMeshDeactivator>() && MBEditor.IsEditModeOn && gameEntity.GetEditModeLevelVisibility() && gameEntity.HasScriptOfType<StandingPoint>())
			{
				if (Mission.Current != null && !Mission.Current.IsPositionInsideBoundaries(origin.AsVec2))
				{
					this.AddPositionToInvalidList(categoryId, origin, gameEntity, false, false);
				}
				this._particularfaceID = -1;
				if (base.Scene.GetNavigationMeshForPosition(origin, out this._particularfaceID, 1.5f, false) != UIntPtr.Zero)
				{
					if (!gameEntity.Name.Contains("player") && this._particularfaceID == this._disabledFaceId && (this._sceneType == SpawnPointUnits.SceneType.Center || this._sceneType == SpawnPointUnits.SceneType.VillageCenter) && categoryId != SpawnPointDebugView.CategoryId.Chair && categoryId != SpawnPointDebugView.CategoryId.Animal)
					{
						if (!(gameEntity.Parent != null) || !(gameEntity.Parent.Name == "sp_battle_set"))
						{
							this.AddPositionToInvalidList(categoryId, origin, gameEntity, true, false);
							return;
						}
					}
					else if (gameEntity.Parent != null)
					{
						this.CheckSemiValidsOfChair(gameEntity);
						return;
					}
				}
				else
				{
					if (categoryId == SpawnPointDebugView.CategoryId.Chair && gameEntity.Parent != null)
					{
						this.CheckSemiValidsOfChair(gameEntity);
						return;
					}
					this.AddPositionToInvalidList(categoryId, origin, gameEntity, false, false);
				}
			}
		}

		// Token: 0x0600014C RID: 332 RVA: 0x000104D8 File Offset: 0x0000E6D8
		private void CheckSemiValidsOfChair(GameEntity gameEntity)
		{
			AnimationPoint firstScriptOfType = gameEntity.GetFirstScriptOfType<AnimationPoint>();
			if (firstScriptOfType != null)
			{
				bool flag = false;
				bool flag2 = false;
				List<AnimationPoint> alternatives = firstScriptOfType.GetAlternatives();
				if (alternatives != null && !alternatives.IsEmpty<AnimationPoint>())
				{
					foreach (AnimationPoint animationPoint in alternatives)
					{
						Vec3 origin = animationPoint.GameEntity.GetGlobalFrame().origin;
						if ((!(base.Scene.GetNavigationMeshForPosition(origin, out this._particularfaceID, 1.5f, false) == UIntPtr.Zero) || animationPoint.GameEntity.IsGhostObject()) && this._particularfaceID != this._disabledFaceId)
						{
							flag = true;
							if (animationPoint == firstScriptOfType)
							{
								flag2 = true;
							}
						}
					}
					if (!flag2)
					{
						if (flag)
						{
							Vec3 origin2 = firstScriptOfType.GameEntity.GetGlobalFrame().origin;
							this.AddPositionToInvalidList(SpawnPointDebugView.CategoryId.SemivalidChair, origin2, gameEntity, false, true);
							return;
						}
						Vec3 origin3 = firstScriptOfType.GameEntity.GetGlobalFrame().origin;
						this.AddPositionToInvalidList(SpawnPointDebugView.CategoryId.Chair, origin3, gameEntity, false, false);
						return;
					}
				}
				else
				{
					Vec3 origin4 = firstScriptOfType.GameEntity.GetGlobalFrame().origin;
					if (base.Scene.GetNavigationMeshForPosition(origin4) == UIntPtr.Zero && !firstScriptOfType.GameEntity.IsGhostObject())
					{
						this.AddPositionToInvalidList(SpawnPointDebugView.CategoryId.Chair, origin4, gameEntity, false, false);
					}
				}
			}
		}

		// Token: 0x0600014D RID: 333 RVA: 0x0001064C File Offset: 0x0000E84C
		private void CheckIfChairOrAnimal(SpawnPointUnits spUnit)
		{
			foreach (GameEntity gameEntity in base.Scene.FindEntitiesWithTag(spUnit.SpName))
			{
				IEnumerable<GameEntity> children = gameEntity.GetChildren();
				if (children.Count<GameEntity>() != 0)
				{
					using (IEnumerator<GameEntity> enumerator2 = children.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							GameEntity gameEntity2 = enumerator2.Current;
							if (gameEntity2.Name.Contains("chair") && !gameEntity2.IsGhostObject())
							{
								this.CheckNavigationMeshForParticularEntity(gameEntity2, SpawnPointDebugView.CategoryId.Chair);
							}
							else if (!gameEntity2.IsGhostObject() && !gameEntity2.IsGhostObject())
							{
								this.CheckNavigationMeshForParticularEntity(gameEntity2, SpawnPointDebugView.CategoryId.NPC);
							}
						}
						continue;
					}
				}
				if (spUnit.Type == "animal" && !gameEntity.IsGhostObject())
				{
					this.CheckNavigationMeshForParticularEntity(gameEntity, SpawnPointDebugView.CategoryId.Animal);
				}
				else if (!gameEntity.IsGhostObject())
				{
					this.CheckNavigationMeshForParticularEntity(gameEntity, SpawnPointDebugView.CategoryId.NPC);
				}
			}
		}

		// Token: 0x0600014E RID: 334 RVA: 0x0001075C File Offset: 0x0000E95C
		private void CheckIfPassage(SpawnPointUnits spUnit)
		{
			if (spUnit.SpName.Contains("passage"))
			{
				foreach (GameEntity gameEntity in base.Scene.FindEntitiesWithTag("npc_passage"))
				{
					foreach (GameEntity gameEntity2 in gameEntity.GetChildren())
					{
						if (gameEntity2.Name.Contains("passage") && !gameEntity2.IsGhostObject())
						{
							this.CheckNavigationMeshForParticularEntity(gameEntity2, SpawnPointDebugView.CategoryId.Passage);
							break;
						}
					}
				}
			}
		}

		// Token: 0x0600014F RID: 335 RVA: 0x00010818 File Offset: 0x0000EA18
		private void RemoveDuplicateValuesInLists()
		{
			this._invalidSpawnPointsDictionary = this._invalidSpawnPointsDictionary.ToDictionary((KeyValuePair<SpawnPointDebugView.CategoryId, List<SpawnPointDebugView.InvalidPosition>> c) => c.Key, (KeyValuePair<SpawnPointDebugView.CategoryId, List<SpawnPointDebugView.InvalidPosition>> c) => c.Value.Distinct<SpawnPointDebugView.InvalidPosition>().ToList<SpawnPointDebugView.InvalidPosition>());
			if (this._invalidSpawnPointsDictionary.ContainsKey(SpawnPointDebugView.CategoryId.SemivalidChair))
			{
				foreach (SpawnPointDebugView.InvalidPosition item in this._invalidSpawnPointsDictionary[SpawnPointDebugView.CategoryId.SemivalidChair])
				{
					if (this._invalidSpawnPointsDictionary[SpawnPointDebugView.CategoryId.Chair].Contains(item))
					{
						this._invalidSpawnPointsDictionary[SpawnPointDebugView.CategoryId.Chair].Remove(item);
					}
				}
			}
		}

		// Token: 0x06000150 RID: 336 RVA: 0x000108F0 File Offset: 0x0000EAF0
		private void AddPositionToInvalidList(SpawnPointDebugView.CategoryId categoryId, Vec3 globalPosition, GameEntity entity, bool isDisabledNavMesh, bool doNotShowWarning = false)
		{
			if (!entity.IsGhostObject() && entity.IsVisibleIncludeParents() && this._invalidSpawnPointsDictionary.ContainsKey(categoryId))
			{
				SpawnPointDebugView.InvalidPosition item;
				item.position = globalPosition;
				item.entity = entity;
				item.isDisabledNavMesh = isDisabledNavMesh;
				item.doNotShowWarning = doNotShowWarning;
				if (this._invalidSpawnPointsDictionary[categoryId].All((SpawnPointDebugView.InvalidPosition x) => x.position != globalPosition))
				{
					this._invalidSpawnPointsDictionary[categoryId].Add(item);
				}
			}
		}

		// Token: 0x06000151 RID: 337 RVA: 0x00010980 File Offset: 0x0000EB80
		private void ToggleButtons()
		{
			if (this._showNPCs)
			{
				this.DrawDebugLinesForInvalidSpawnPoints(SpawnPointDebugView.CategoryId.NPC, this._npcDebugLineColor);
			}
			if (this._showChairs)
			{
				this.DrawDebugLinesForInvalidSpawnPoints(SpawnPointDebugView.CategoryId.Chair, this._chairDebugLineColor);
			}
			if (this._showAnimals)
			{
				this.DrawDebugLinesForInvalidSpawnPoints(SpawnPointDebugView.CategoryId.Animal, this._animalDebugLineColor);
			}
			if (this._showSemiValidPoints)
			{
				this.DrawDebugLinesForInvalidSpawnPoints(SpawnPointDebugView.CategoryId.SemivalidChair, this._semivalidChairDebugLineColor);
			}
			if (this._showPassagePoints)
			{
				this.DrawDebugLinesForInvalidSpawnPoints(SpawnPointDebugView.CategoryId.Passage, this._passageDebugLineColor);
			}
			if (this._showOutOfBoundPoints)
			{
				this.DrawDebugLinesForInvalidSpawnPoints(SpawnPointDebugView.CategoryId.OutOfMissionBound, this._missionBoundDebugLineColor);
			}
		}

		// Token: 0x06000152 RID: 338 RVA: 0x00010A0B File Offset: 0x0000EC0B
		private void CheckInaccessiblePoint()
		{
			this.WriteInaccessiblePointTexts();
			if (this.ImGUIButton("Check Inaccessible Points", this._normalButton))
			{
				this.CheckInaccesiblePositions();
			}
		}

		// Token: 0x06000153 RID: 339 RVA: 0x00010A2C File Offset: 0x0000EC2C
		private void CheckInaccesiblePositions()
		{
			this._allGameEntitiesWithAnimationScript.Clear();
			this._allPathForPosition.Clear();
			this._inaccessibleEntitiesList.Clear();
			this._closeEntitiesToInaccessible.Clear();
			this.GetDisableFaceID();
			int[] excludedFaceIds = new int[] { this._disabledFaceId };
			base.Scene.GetAllEntitiesWithScriptComponent<AnimationPoint>(ref this._allGameEntitiesWithAnimationScript);
			for (int i = this._allGameEntitiesWithAnimationScript.Count - 1; i >= 0; i--)
			{
				GameEntity gameEntity = this._allGameEntitiesWithAnimationScript[i];
				bool flag = false;
				while (gameEntity != null)
				{
					if (gameEntity.HasTag("static_npc"))
					{
						flag = true;
						break;
					}
					gameEntity = gameEntity.Parent;
				}
				if (flag)
				{
					this._allGameEntitiesWithAnimationScript.RemoveAt(i);
				}
			}
			for (int j = 0; j < this._allGameEntitiesWithAnimationScript.Count; j++)
			{
				base.Scene.GetNavMeshFaceIndex(ref this._startPositionNavMesh, this._allGameEntitiesWithAnimationScript[j].GlobalPosition, true);
				if (this._startPositionNavMesh.FaceGroupIndex != this._disabledFaceId)
				{
					this._selectedEntity = this._allGameEntitiesWithAnimationScript[j];
					break;
				}
			}
			for (int k = 0; k < this._allGameEntitiesWithAnimationScript.Count; k++)
			{
				GameEntity gameEntity2 = this._allGameEntitiesWithAnimationScript[k];
				base.Scene.GetNavMeshFaceIndex(ref this._targetPositionNavMesh, gameEntity2.GlobalPosition, true);
				if (this._startPositionNavMesh.FaceGroupIndex == this._targetPositionNavMesh.FaceGroupIndex)
				{
					NavigationPath navigationPath = new NavigationPath();
					if (!base.Scene.GetPathBetweenAIFaces(this._startPositionNavMesh.FaceIndex, this._targetPositionNavMesh.FaceIndex, this._selectedEntity.GlobalPosition.AsVec2, gameEntity2.GlobalPosition.AsVec2, 0.3f, navigationPath, excludedFaceIds, 1f))
					{
						this._inaccessibleEntitiesList.Add(this._allGameEntitiesWithAnimationScript[k]);
					}
					else
					{
						this._allPathForPosition.Add(navigationPath);
					}
				}
			}
			if (this._inaccessibleEntitiesList.Any<GameEntity>())
			{
				MBEditor.AddEditorWarning("Scene has inaccessible point!");
				foreach (GameEntity gameEntity3 in this._inaccessibleEntitiesList)
				{
					float num = float.MaxValue;
					for (int l = 0; l < this._allGameEntitiesWithAnimationScript.Count; l++)
					{
						GameEntity gameEntity4 = this._allGameEntitiesWithAnimationScript[l];
						base.Scene.GetNavMeshFaceIndex(ref this._startPositionNavMesh, gameEntity4.GlobalPosition, true);
						base.Scene.GetNavMeshFaceIndex(ref this._targetPositionNavMesh, gameEntity3.GlobalPosition, true);
						if (this._startPositionNavMesh.FaceGroupIndex == this._targetPositionNavMesh.FaceGroupIndex)
						{
							float num2 = gameEntity3.GlobalPosition.DistanceSquared(gameEntity4.GlobalPosition);
							if (!this._inaccessibleEntitiesList.Contains(gameEntity4) && num2 < num)
							{
								num = num2;
								this._closeEntity = gameEntity4;
							}
						}
					}
					if (!this._closeEntitiesToInaccessible.Contains(this._closeEntity))
					{
						this._closeEntitiesToInaccessible.Add(this._closeEntity);
					}
				}
			}
		}

		// Token: 0x06000154 RID: 340 RVA: 0x00010D70 File Offset: 0x0000EF70
		private void FindAllPrefabsWithSelectedTag()
		{
			if (this.allPrefabsWithParticularTag != null)
			{
				string[] array = this.allPrefabsWithParticularTag.Split(new char[] { '/' });
				for (int i = 0; i < array.Length; i++)
				{
					this.ImGUITextArea(array[i], !this._separatorNeeded, !this._onSameLineNeeded);
				}
			}
		}

		// Token: 0x06000155 RID: 341 RVA: 0x00010DC8 File Offset: 0x0000EFC8
		private void FocusCameraToMisplacedObjects(SpawnPointDebugView.CategoryId CategoryId)
		{
			List<SpawnPointDebugView.InvalidPosition> list;
			this._invalidSpawnPointsDictionary.TryGetValue(CategoryId, out list);
			if (list.Count == 0 || this._cameraFocusIndex < 0 || this._cameraFocusIndex >= list.Count)
			{
				this._cameraFocusIndex = 0;
				return;
			}
			MBEditor.ZoomToPosition(list[this._cameraFocusIndex].position);
			int cameraFocusIndex;
			if (this._cameraFocusIndex < list.Count - 1)
			{
				int num = this._cameraFocusIndex + 1;
				this._cameraFocusIndex = num;
				cameraFocusIndex = num;
			}
			else
			{
				cameraFocusIndex = (this._cameraFocusIndex = 0);
			}
			this._cameraFocusIndex = cameraFocusIndex;
		}

		// Token: 0x06000156 RID: 342 RVA: 0x00010E54 File Offset: 0x0000F054
		private void FocusCameraToInaccessiblePosition()
		{
			if (this._inaccessibleEntitiesList.Count == 0 || this._cameraFocusIndex < 0 || this._cameraFocusIndex >= this._inaccessibleEntitiesList.Count)
			{
				this._cameraFocusIndex = 0;
				MBEditor.ZoomToPosition(this._inaccessibleEntitiesList[this._cameraFocusIndex].GlobalPosition);
				return;
			}
			int cameraFocusIndex;
			if (this._cameraFocusIndex < this._inaccessibleEntitiesList.Count - 1)
			{
				int num = this._cameraFocusIndex + 1;
				this._cameraFocusIndex = num;
				cameraFocusIndex = num;
			}
			else
			{
				cameraFocusIndex = (this._cameraFocusIndex = 0);
			}
			this._cameraFocusIndex = cameraFocusIndex;
			MBEditor.ZoomToPosition(this._inaccessibleEntitiesList[this._cameraFocusIndex].GlobalPosition);
		}

		// Token: 0x06000157 RID: 343 RVA: 0x00010F00 File Offset: 0x0000F100
		private int GetCategoryCount(SpawnPointDebugView.CategoryId CategoryId)
		{
			int result = 0;
			if (this._invalidSpawnPointsDictionary.ContainsKey(CategoryId))
			{
				result = this._invalidSpawnPointsDictionary[CategoryId].Count;
			}
			return result;
		}

		// Token: 0x06000158 RID: 344 RVA: 0x00010F30 File Offset: 0x0000F130
		private void GetDisableFaceID()
		{
			foreach (SpawnPointUnits spawnPointUnits in this._spUnitsList.ToList<SpawnPointUnits>())
			{
				List<GameEntity> list = base.Scene.FindEntitiesWithTag(spawnPointUnits.SpName).ToList<GameEntity>();
				if (spawnPointUnits.SpName == "navigation_mesh_deactivator")
				{
					foreach (GameEntity gameEntity in list)
					{
						NavigationMeshDeactivator firstScriptOfType = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>();
						if (firstScriptOfType != null && firstScriptOfType.GameEntity.GetEditModeLevelVisibility())
						{
							this._disabledFaceId = firstScriptOfType.DisableFaceWithId;
							break;
						}
					}
				}
			}
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00011014 File Offset: 0x0000F214
		private void ClearAllLists()
		{
			foreach (KeyValuePair<SpawnPointDebugView.CategoryId, List<SpawnPointDebugView.InvalidPosition>> keyValuePair in this._invalidSpawnPointsDictionary)
			{
				keyValuePair.Value.Clear();
			}
		}

		// Token: 0x0600015A RID: 346 RVA: 0x0001106C File Offset: 0x0000F26C
		private bool ImGUIButton(string buttonText, bool smallButton)
		{
			if (smallButton)
			{
				return Imgui.SmallButton(buttonText);
			}
			return Imgui.Button(buttonText);
		}

		// Token: 0x0600015B RID: 347 RVA: 0x0001107E File Offset: 0x0000F27E
		private void LeaveSpaceBetweenTabs()
		{
			this.OnSameLine();
			this.ImGUITextArea(" ", !this._separatorNeeded, this._onSameLineNeeded);
		}

		// Token: 0x0600015C RID: 348 RVA: 0x000110A0 File Offset: 0x0000F2A0
		private void EndImGUIWindow()
		{
			Imgui.End();
			Imgui.EndMainThreadScope();
		}

		// Token: 0x0600015D RID: 349 RVA: 0x000110AC File Offset: 0x0000F2AC
		private void StartImGUIWindow(string str)
		{
			Imgui.BeginMainThreadScope();
			Imgui.Begin(str);
		}

		// Token: 0x0600015E RID: 350 RVA: 0x000110B9 File Offset: 0x0000F2B9
		private void ImGUITextArea(string text, bool separatorNeeded, bool onSameLine)
		{
			Imgui.Text(text);
			this.ImGUISeparatorSameLineHandler(separatorNeeded, onSameLine);
		}

		// Token: 0x0600015F RID: 351 RVA: 0x000110C9 File Offset: 0x0000F2C9
		private void ImGUICheckBox(string text, ref bool is_checked, bool separatorNeeded, bool onSameLine)
		{
			Imgui.Checkbox(text, ref is_checked);
			this.ImGUISeparatorSameLineHandler(separatorNeeded, onSameLine);
		}

		// Token: 0x06000160 RID: 352 RVA: 0x000110DC File Offset: 0x0000F2DC
		private void ImguiSameLine(float positionX, float spacingWidth)
		{
			Imgui.SameLine(positionX, spacingWidth);
		}

		// Token: 0x06000161 RID: 353 RVA: 0x000110E5 File Offset: 0x0000F2E5
		private void ImGUISeparatorSameLineHandler(bool separatorNeeded, bool onSameLine)
		{
			if (separatorNeeded)
			{
				this.Separator();
			}
			if (onSameLine)
			{
				this.OnSameLine();
			}
		}

		// Token: 0x06000162 RID: 354 RVA: 0x000110F9 File Offset: 0x0000F2F9
		private void OnSameLine()
		{
			Imgui.SameLine(0f, 0f);
		}

		// Token: 0x06000163 RID: 355 RVA: 0x0001110A File Offset: 0x0000F30A
		private void Separator()
		{
			Imgui.Separator();
		}

		// Token: 0x06000164 RID: 356 RVA: 0x00011114 File Offset: 0x0000F314
		private void WriteLineOfTableDebug(SpawnPointUnits spUnit, Vec3 Color, string type)
		{
			if ((type == "animal" && this._showAnimalsList) || (type == "npc" && this._showNPCsList) || (type == "passage" && this._showPassagesList) || (type == "DONTUSE" && this._showDontUseList) || (type == "other" && this._showOthersList))
			{
				Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref Color);
				this.ImguiSameLine(0f, 0f);
				this.ImGUITextArea(spUnit.SpName, !this._separatorNeeded, this._onSameLineNeeded);
				this.ImguiSameLine(305f, 10f);
				this.ImGUITextArea(spUnit.MinCount.ToString(), !this._separatorNeeded, this._onSameLineNeeded);
				this.ImguiSameLine(345f, 10f);
				this.ImGUITextArea((spUnit.MaxCount == int.MaxValue) ? "-" : spUnit.MaxCount.ToString(), !this._separatorNeeded, this._onSameLineNeeded);
				this.ImguiSameLine(405f, 10f);
				this.ImGUITextArea(spUnit.CurrentCount.ToString(), !this._separatorNeeded, this._onSameLineNeeded);
				this.ImguiSameLine(500f, 10f);
				this.ImGUITextArea(spUnit.SpawnedAgentCount.ToString(), !this._separatorNeeded, this._onSameLineNeeded);
				Imgui.PopStyleColor();
				this.ImguiSameLine(575f, 10f);
				if (this.ImGUIButton(spUnit.SpName, this._normalButton))
				{
					this._relatedEntityWindow = true;
					this._relatedPrefabTag = spUnit.SpName;
					this.allPrefabsWithParticularTag = MBEditor.GetAllPrefabsAndChildWithTag(this._relatedPrefabTag);
				}
				this.ImGUITextArea(" ", !this._separatorNeeded, !this._onSameLineNeeded);
			}
		}

		// Token: 0x06000165 RID: 357 RVA: 0x00011308 File Offset: 0x0000F508
		private void WriteNavigationMeshTabTexts()
		{
			this.ImGUITextArea("This tool will mark the spawn points which are not on the navigation mesh", !this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("or on the navigation mesh that will be deactivated by 'Navigation Mesh Deactivator'", !this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("Deactivation Face Id: " + this._disabledFaceId, !this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("Click 'CHECK' button to calculate.", this._separatorNeeded, !this._onSameLineNeeded);
			Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref this._redColor);
			this.ImGUICheckBox("Show NPCs ", ref this._showNPCs, !this._separatorNeeded, this._onSameLineNeeded);
			this.ImGUITextArea("(" + this.GetCategoryCount(SpawnPointDebugView.CategoryId.NPC) + ")", !this._separatorNeeded, !this._onSameLineNeeded);
			if (this._showNPCs)
			{
				if (this.ImGUIButton("<Previous NPC", this._normalButton))
				{
					this._cameraFocusIndex -= 2;
					this.FocusCameraToMisplacedObjects(SpawnPointDebugView.CategoryId.NPC);
				}
				this.ImguiSameLine(120f, 20f);
				if (this.ImGUIButton("Next NPC>", this._normalButton))
				{
					this.FocusCameraToMisplacedObjects(SpawnPointDebugView.CategoryId.NPC);
				}
				this.ImGUITextArea(string.Concat(new object[]
				{
					this._cameraFocusIndex + 1,
					" (",
					this.GetCategoryCount(SpawnPointDebugView.CategoryId.NPC),
					")"
				}), this._separatorNeeded, !this._onSameLineNeeded);
			}
			Imgui.PopStyleColor();
			Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref this._blueColor);
			this.ImGUICheckBox("Show Animals ", ref this._showAnimals, !this._separatorNeeded, this._onSameLineNeeded);
			this.ImGUITextArea("(" + this.GetCategoryCount(SpawnPointDebugView.CategoryId.Animal) + ")", !this._separatorNeeded, !this._onSameLineNeeded);
			if (this._showAnimals)
			{
				if (this.ImGUIButton("<Previous Animal", this._normalButton))
				{
					this._cameraFocusIndex -= 2;
					this.FocusCameraToMisplacedObjects(SpawnPointDebugView.CategoryId.Animal);
				}
				this.ImguiSameLine(120f, 20f);
				if (this.ImGUIButton("Next Animal>", this._normalButton))
				{
					this.FocusCameraToMisplacedObjects(SpawnPointDebugView.CategoryId.Animal);
				}
				this.ImGUITextArea(string.Concat(new object[]
				{
					this._cameraFocusIndex + 1,
					" (",
					this.GetCategoryCount(SpawnPointDebugView.CategoryId.Animal),
					")"
				}), !this._separatorNeeded, !this._onSameLineNeeded);
			}
			Imgui.PopStyleColor();
			Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref this._purbleColor);
			this.ImGUICheckBox("Show Passages ", ref this._showPassagePoints, !this._separatorNeeded, this._onSameLineNeeded);
			this.ImGUITextArea("(" + this.GetCategoryCount(SpawnPointDebugView.CategoryId.Passage) + ")", !this._separatorNeeded, !this._onSameLineNeeded);
			if (this._showPassagePoints)
			{
				if (this.ImGUIButton("<Previous Passage", this._normalButton))
				{
					this._cameraFocusIndex -= 2;
					this.FocusCameraToMisplacedObjects(SpawnPointDebugView.CategoryId.Passage);
				}
				this.ImguiSameLine(120f, 20f);
				if (this.ImGUIButton("Next Passage>", this._normalButton))
				{
					this.FocusCameraToMisplacedObjects(SpawnPointDebugView.CategoryId.Passage);
				}
				this.ImGUITextArea(string.Concat(new object[]
				{
					this._cameraFocusIndex + 1,
					" (",
					this.GetCategoryCount(SpawnPointDebugView.CategoryId.Passage),
					")"
				}), !this._separatorNeeded, !this._onSameLineNeeded);
			}
			Imgui.PopStyleColor();
			Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref this._greenColor);
			this.ImGUICheckBox("Show Chairs ", ref this._showChairs, !this._separatorNeeded, this._onSameLineNeeded);
			this.ImGUITextArea("(" + this.GetCategoryCount(SpawnPointDebugView.CategoryId.Chair) + ")", !this._separatorNeeded, !this._onSameLineNeeded);
			if (this._showChairs)
			{
				if (this.ImGUIButton("<Previous Chair", this._normalButton))
				{
					this._cameraFocusIndex -= 2;
					this.FocusCameraToMisplacedObjects(SpawnPointDebugView.CategoryId.Chair);
				}
				this.ImguiSameLine(120f, 20f);
				if (this.ImGUIButton("Next Chair>", this._normalButton))
				{
					this.FocusCameraToMisplacedObjects(SpawnPointDebugView.CategoryId.Chair);
				}
				this.ImGUITextArea(string.Concat(new object[]
				{
					this._cameraFocusIndex + 1,
					" (",
					this.GetCategoryCount(SpawnPointDebugView.CategoryId.Chair),
					")"
				}), !this._separatorNeeded, !this._onSameLineNeeded);
			}
			Imgui.PopStyleColor();
			Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref this._yellowColor);
			this.ImGUICheckBox("Show semi-valid Chairs* ", ref this._showSemiValidPoints, !this._separatorNeeded, this._onSameLineNeeded);
			this.ImGUITextArea("(" + this.GetCategoryCount(SpawnPointDebugView.CategoryId.SemivalidChair) + ")", !this._separatorNeeded, !this._onSameLineNeeded);
			if (this._showSemiValidPoints)
			{
				if (this.ImGUIButton("<Previous S-Chair", this._normalButton))
				{
					this._cameraFocusIndex -= 2;
					this.FocusCameraToMisplacedObjects(SpawnPointDebugView.CategoryId.SemivalidChair);
				}
				this.ImguiSameLine(120f, 20f);
				if (this.ImGUIButton("Next S-Chair>", this._normalButton))
				{
					this.FocusCameraToMisplacedObjects(SpawnPointDebugView.CategoryId.SemivalidChair);
				}
				this.ImGUITextArea(string.Concat(new object[]
				{
					this._cameraFocusIndex + 1,
					" (",
					this.GetCategoryCount(SpawnPointDebugView.CategoryId.SemivalidChair),
					")"
				}), !this._separatorNeeded, !this._onSameLineNeeded);
			}
			Imgui.PopStyleColor();
			this.ImGUICheckBox("Show out of Mission Bound Points**", ref this._showOutOfBoundPoints, !this._separatorNeeded, this._onSameLineNeeded);
			this.ImGUITextArea(" (" + this.GetCategoryCount(SpawnPointDebugView.CategoryId.OutOfMissionBound) + ")", !this._separatorNeeded, !this._onSameLineNeeded);
			this._totalInvalidPoints = this.GetCategoryCount(SpawnPointDebugView.CategoryId.NPC) + this.GetCategoryCount(SpawnPointDebugView.CategoryId.Chair) + this.GetCategoryCount(SpawnPointDebugView.CategoryId.Animal) + this.GetCategoryCount(SpawnPointDebugView.CategoryId.SemivalidChair) + this.GetCategoryCount(SpawnPointDebugView.CategoryId.Passage) + this.GetCategoryCount(SpawnPointDebugView.CategoryId.OutOfMissionBound);
			this.ImGUITextArea(string.Concat(new object[] { "(", this._currentInvalidPoints, " / ", this._totalInvalidPoints, " ) are being shown." }), !this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("Found " + this._totalInvalidPoints + " invalid spawnpoints.", this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("* Points that have at least one valid point as alternative", this._separatorNeeded, !this._onSameLineNeeded);
			if (Mission.Current == null)
			{
				this.ImGUITextArea("** Mission bound checking feature is not working in editor. Open mission to check it.", this._separatorNeeded, !this._onSameLineNeeded);
			}
		}

		// Token: 0x06000166 RID: 358 RVA: 0x00011A3C File Offset: 0x0000FC3C
		private void WriteInaccessiblePointTexts()
		{
			this.ImGUITextArea("This tool will mark the spawn points as inaccessible because ", !this._separatorNeeded, !this._onSameLineNeeded);
			this.ImGUITextArea("the navmeshes leading to these points have incorrect IDs.", !this._separatorNeeded, !this._onSameLineNeeded);
			if (this._inaccessibleEntitiesList.Any<GameEntity>())
			{
				Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref this._redColor);
				this.ImGUITextArea("Inaccessible Point Count:", !this._separatorNeeded, this._onSameLineNeeded);
				this.ImGUITextArea(string.Concat(new object[]
				{
					this._cameraFocusIndex + 1,
					" (",
					this._inaccessibleEntitiesList.Count,
					")"
				}), !this._separatorNeeded, !this._onSameLineNeeded);
				this.ImGUITextArea("Red lines mark the inaccessible point.", !this._separatorNeeded, !this._onSameLineNeeded);
				Imgui.PopStyleColor();
				Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref this._yellowColor);
				this.ImGUITextArea("Yellow lines mark the closest point to the inaccessible point.", !this._separatorNeeded, !this._onSameLineNeeded);
				Imgui.PopStyleColor();
				Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref this._redColor);
				if (this.ImGUIButton("<Previous Position", this._normalButton))
				{
					this._cameraFocusIndex -= 2;
					this.FocusCameraToInaccessiblePosition();
				}
				this.ImguiSameLine(120f, 40f);
				if (this.ImGUIButton("Next Position>", this._normalButton))
				{
					this.FocusCameraToInaccessiblePosition();
				}
				Imgui.PopStyleColor();
				this.DrawDebugLineForInaccesiblePositions();
				return;
			}
			Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref this._greenColor);
			this.ImGUITextArea("Inaccessible Point Count: ", !this._separatorNeeded, this._onSameLineNeeded);
			this.ImGUITextArea(this._inaccessibleEntitiesList.Count.ToString(), !this._separatorNeeded, !this._onSameLineNeeded);
			Imgui.PopStyleColor();
		}

		// Token: 0x06000167 RID: 359 RVA: 0x00011C20 File Offset: 0x0000FE20
		private void DrawDebugLineForInaccesiblePositions()
		{
			for (int i = 0; i < this._closeEntitiesToInaccessible.Count; i++)
			{
			}
			for (int j = 0; j < this._inaccessibleEntitiesList.Count; j++)
			{
			}
		}

		// Token: 0x06000168 RID: 360 RVA: 0x00011C5C File Offset: 0x0000FE5C
		private void DrawDebugLinesForInvalidSpawnPoints(SpawnPointDebugView.CategoryId CategoryId, uint color)
		{
			if (this._invalidSpawnPointsDictionary.ContainsKey(CategoryId))
			{
				foreach (SpawnPointDebugView.InvalidPosition invalidPosition in this._invalidSpawnPointsDictionary[CategoryId])
				{
				}
			}
		}

		// Token: 0x06000169 RID: 361 RVA: 0x00011CBC File Offset: 0x0000FEBC
		private void WriteTableHeaders()
		{
			this.ImguiSameLine(0f, 0f);
			this.ImGUITextArea("Tag Name", !this._separatorNeeded, this._onSameLineNeeded);
			this.ImguiSameLine(295f, 10f);
			this.ImGUITextArea("Min", !this._separatorNeeded, this._onSameLineNeeded);
			this.ImguiSameLine(340f, 10f);
			this.ImGUITextArea("Max", !this._separatorNeeded, this._onSameLineNeeded);
			this.ImguiSameLine(390f, 10f);
			this.ImGUITextArea("Current", !this._separatorNeeded, this._onSameLineNeeded);
			this.ImguiSameLine(465f, 10f);
			this.ImGUITextArea("Agent Count", !this._separatorNeeded, this._onSameLineNeeded);
			this.ImguiSameLine(575f, 10f);
			this.ImGUITextArea("List all prefabs with tag:", this._separatorNeeded, !this._onSameLineNeeded);
		}

		// Token: 0x04000090 RID: 144
		private const string BattleSetName = "sp_battle_set";

		// Token: 0x04000091 RID: 145
		private const string CenterConversationPoint = "center_conversation_point";

		// Token: 0x04000092 RID: 146
		private const float AgentRadius = 0.3f;

		// Token: 0x04000093 RID: 147
		public static bool ActivateDebugUI;

		// Token: 0x04000094 RID: 148
		public bool ActivateDebugUIEditor;

		// Token: 0x04000095 RID: 149
		private readonly bool _separatorNeeded = true;

		// Token: 0x04000096 RID: 150
		private readonly bool _onSameLineNeeded = true;

		// Token: 0x04000097 RID: 151
		private bool _townCenterRadioButton;

		// Token: 0x04000098 RID: 152
		private bool _tavernRadioButton;

		// Token: 0x04000099 RID: 153
		private bool _arenaRadioButton;

		// Token: 0x0400009A RID: 154
		private bool _villageRadioButton;

		// Token: 0x0400009B RID: 155
		private bool _lordshallRadioButton;

		// Token: 0x0400009C RID: 156
		private bool _castleRadioButton;

		// Token: 0x0400009D RID: 157
		private bool _basicInformationTab;

		// Token: 0x0400009E RID: 158
		private bool _entityInformationTab;

		// Token: 0x0400009F RID: 159
		private bool _navigationMeshCheckTab;

		// Token: 0x040000A0 RID: 160
		private bool _inaccessiblePositionCheckTab;

		// Token: 0x040000A1 RID: 161
		private bool _relatedEntityWindow;

		// Token: 0x040000A2 RID: 162
		private string _relatedPrefabTag;

		// Token: 0x040000A3 RID: 163
		private bool _workshopAndAlleyConflictWindow;

		// Token: 0x040000A4 RID: 164
		private string _problematicAreaMarkerWarningText;

		// Token: 0x040000A5 RID: 165
		private int _cameraFocusIndex;

		// Token: 0x040000A6 RID: 166
		private bool _showNPCs;

		// Token: 0x040000A7 RID: 167
		private bool _showChairs;

		// Token: 0x040000A8 RID: 168
		private bool _showAnimals;

		// Token: 0x040000A9 RID: 169
		private bool _showSemiValidPoints;

		// Token: 0x040000AA RID: 170
		private bool _showPassagePoints;

		// Token: 0x040000AB RID: 171
		private bool _showOutOfBoundPoints;

		// Token: 0x040000AC RID: 172
		private bool _showPassagesList;

		// Token: 0x040000AD RID: 173
		private bool _showAnimalsList;

		// Token: 0x040000AE RID: 174
		private bool _showNPCsList;

		// Token: 0x040000AF RID: 175
		private bool _showDontUseList;

		// Token: 0x040000B0 RID: 176
		private bool _showOthersList;

		// Token: 0x040000B1 RID: 177
		private string _sceneName;

		// Token: 0x040000B2 RID: 178
		private SpawnPointUnits.SceneType _sceneType;

		// Token: 0x040000B3 RID: 179
		private readonly bool _normalButton;

		// Token: 0x040000B4 RID: 180
		private int _currentTownsfolkCount;

		// Token: 0x040000B5 RID: 181
		private Vec3 _redColor = new Vec3(200f, 0f, 0f, 255f);

		// Token: 0x040000B6 RID: 182
		private Vec3 _greenColor = new Vec3(0f, 200f, 0f, 255f);

		// Token: 0x040000B7 RID: 183
		private Vec3 _blueColor = new Vec3(0f, 180f, 180f, 255f);

		// Token: 0x040000B8 RID: 184
		private Vec3 _yellowColor = new Vec3(200f, 200f, 0f, 255f);

		// Token: 0x040000B9 RID: 185
		private Vec3 _purbleColor = new Vec3(255f, 0f, 255f, 255f);

		// Token: 0x040000BA RID: 186
		private uint _npcDebugLineColor = 4294901760U;

		// Token: 0x040000BB RID: 187
		private uint _chairDebugLineColor = 4278255360U;

		// Token: 0x040000BC RID: 188
		private uint _animalDebugLineColor = 4279356620U;

		// Token: 0x040000BD RID: 189
		private uint _semivalidChairDebugLineColor = 4294963200U;

		// Token: 0x040000BE RID: 190
		private uint _passageDebugLineColor = 4288217241U;

		// Token: 0x040000BF RID: 191
		private uint _missionBoundDebugLineColor = uint.MaxValue;

		// Token: 0x040000C0 RID: 192
		private int _totalInvalidPoints;

		// Token: 0x040000C1 RID: 193
		private int _currentInvalidPoints;

		// Token: 0x040000C2 RID: 194
		private int _disabledFaceId;

		// Token: 0x040000C3 RID: 195
		private int _particularfaceID;

		// Token: 0x040000C4 RID: 196
		private Dictionary<SpawnPointDebugView.CategoryId, List<SpawnPointDebugView.InvalidPosition>> _invalidSpawnPointsDictionary = new Dictionary<SpawnPointDebugView.CategoryId, List<SpawnPointDebugView.InvalidPosition>>();

		// Token: 0x040000C5 RID: 197
		private string allPrefabsWithParticularTag;

		// Token: 0x040000C6 RID: 198
		private IList<SpawnPointUnits> _spUnitsList = new List<SpawnPointUnits>();

		// Token: 0x040000C7 RID: 199
		private List<NavigationPath> _allPathForPosition = new List<NavigationPath>();

		// Token: 0x040000C8 RID: 200
		private List<GameEntity> _allGameEntitiesWithAnimationScript = new List<GameEntity>();

		// Token: 0x040000C9 RID: 201
		private List<GameEntity> _inaccessibleEntitiesList = new List<GameEntity>();

		// Token: 0x040000CA RID: 202
		private List<GameEntity> _closeEntitiesToInaccessible = new List<GameEntity>();

		// Token: 0x040000CB RID: 203
		private GameEntity _selectedEntity;

		// Token: 0x040000CC RID: 204
		private GameEntity _closeEntity;

		// Token: 0x040000CD RID: 205
		private PathFaceRecord _startPositionNavMesh;

		// Token: 0x040000CE RID: 206
		private PathFaceRecord _targetPositionNavMesh;

		// Token: 0x02000091 RID: 145
		private enum CategoryId
		{
			// Token: 0x040002EA RID: 746
			NPC,
			// Token: 0x040002EB RID: 747
			Animal,
			// Token: 0x040002EC RID: 748
			Chair,
			// Token: 0x040002ED RID: 749
			Passage,
			// Token: 0x040002EE RID: 750
			OutOfMissionBound,
			// Token: 0x040002EF RID: 751
			SemivalidChair
		}

		// Token: 0x02000092 RID: 146
		private struct InvalidPosition
		{
			// Token: 0x040002F0 RID: 752
			public Vec3 position;

			// Token: 0x040002F1 RID: 753
			public GameEntity entity;

			// Token: 0x040002F2 RID: 754
			public bool isDisabledNavMesh;

			// Token: 0x040002F3 RID: 755
			public bool doNotShowWarning;
		}
	}
}
