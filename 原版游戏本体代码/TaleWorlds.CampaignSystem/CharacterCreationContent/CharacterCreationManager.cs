using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent
{
	// Token: 0x02000205 RID: 517
	public class CharacterCreationManager
	{
		// Token: 0x170007D1 RID: 2001
		// (get) Token: 0x06001F87 RID: 8071 RVA: 0x0008DADC File Offset: 0x0008BCDC
		public MBReadOnlyList<NarrativeMenu> NarrativeMenus
		{
			get
			{
				return this._narrativeMenus;
			}
		}

		// Token: 0x170007D2 RID: 2002
		// (get) Token: 0x06001F88 RID: 8072 RVA: 0x0008DAE4 File Offset: 0x0008BCE4
		// (set) Token: 0x06001F89 RID: 8073 RVA: 0x0008DAEC File Offset: 0x0008BCEC
		public CharacterCreationContent CharacterCreationContent { get; private set; }

		// Token: 0x170007D3 RID: 2003
		// (get) Token: 0x06001F8A RID: 8074 RVA: 0x0008DAF5 File Offset: 0x0008BCF5
		// (set) Token: 0x06001F8B RID: 8075 RVA: 0x0008DAFD File Offset: 0x0008BCFD
		public NarrativeMenu CurrentMenu { get; private set; }

		// Token: 0x170007D4 RID: 2004
		// (get) Token: 0x06001F8C RID: 8076 RVA: 0x0008DB06 File Offset: 0x0008BD06
		public int CharacterCreationMenuCount
		{
			get
			{
				return this.NarrativeMenus.Count;
			}
		}

		// Token: 0x170007D5 RID: 2005
		// (get) Token: 0x06001F8D RID: 8077 RVA: 0x0008DB13 File Offset: 0x0008BD13
		// (set) Token: 0x06001F8E RID: 8078 RVA: 0x0008DB1B File Offset: 0x0008BD1B
		public CharacterCreationStageBase CurrentStage { get; private set; }

		// Token: 0x06001F8F RID: 8079 RVA: 0x0008DB24 File Offset: 0x0008BD24
		public CharacterCreationManager(CharacterCreationState state)
		{
			this._state = state;
			this._stages = new MBList<CharacterCreationStageBase>();
			this.FaceGenHistory = new FaceGenHistory(new List<UndoRedoKey>(100), new List<UndoRedoKey>(100), new Dictionary<string, float>());
			this._narrativeMenus = new MBList<NarrativeMenu>();
			this.SelectedOptions = new Dictionary<NarrativeMenu, NarrativeMenuOption>();
			this.CharacterCreationContent = new CharacterCreationContent();
			CampaignEventDispatcher.Instance.OnCharacterCreationInitialized(this);
			foreach (KeyValuePair<int, ICharacterCreationContentHandler> keyValuePair in this._handlers)
			{
				keyValuePair.Value.InitializeContent(this);
			}
			foreach (KeyValuePair<int, ICharacterCreationContentHandler> keyValuePair2 in this._handlers)
			{
				keyValuePair2.Value.AfterInitializeContent(this);
			}
		}

		// Token: 0x06001F90 RID: 8080 RVA: 0x0008DC30 File Offset: 0x0008BE30
		public void RegisterCharacterCreationContentHandler(ICharacterCreationContentHandler characterCreationContentHandler, int priority)
		{
			this._handlers.Add(priority, characterCreationContentHandler);
		}

		// Token: 0x06001F91 RID: 8081 RVA: 0x0008DC3F File Offset: 0x0008BE3F
		public void AddStage(CharacterCreationStageBase stage)
		{
			this._stages.Add(stage);
		}

		// Token: 0x06001F92 RID: 8082 RVA: 0x0008DC50 File Offset: 0x0008BE50
		public bool RemoveStage<T>() where T : CharacterCreationStageBase
		{
			for (int i = 0; i < this._stages.Count; i++)
			{
				if (this._stages[i] is T)
				{
					this._stages.RemoveAt(i);
					return true;
				}
			}
			return false;
		}

		// Token: 0x06001F93 RID: 8083 RVA: 0x0008DC98 File Offset: 0x0008BE98
		public T GetStage<T>() where T : CharacterCreationStageBase
		{
			for (int i = 0; i < this._stages.Count; i++)
			{
				T result;
				if ((result = this._stages[i] as T) != null)
				{
					return result;
				}
			}
			return default(T);
		}

		// Token: 0x06001F94 RID: 8084 RVA: 0x0008DCE8 File Offset: 0x0008BEE8
		public void NextStage()
		{
			this._stageIndex++;
			if (this.CurrentStage != null)
			{
				CharacterCreationStageBase currentStage = this.CurrentStage;
				if (currentStage != null)
				{
					currentStage.OnFinalize();
				}
				foreach (KeyValuePair<int, ICharacterCreationContentHandler> keyValuePair in this._handlers)
				{
					keyValuePair.Value.OnStageCompleted(this.CurrentStage);
				}
			}
			this._furthestStageIndex = MathF.Max(this._furthestStageIndex, this._stageIndex);
			if (this._stageIndex == this._stages.Count)
			{
				this.ApplyFinalEffects();
				this._state.FinalizeCharacterCreationState();
				return;
			}
			this.ActivateStage(this._stages[this._stageIndex]);
			this._state.Refresh();
		}

		// Token: 0x06001F95 RID: 8085 RVA: 0x0008DDC8 File Offset: 0x0008BFC8
		public void PreviousStage()
		{
			CharacterCreationStageBase currentStage = this.CurrentStage;
			if (currentStage != null)
			{
				currentStage.OnFinalize();
			}
			this._stageIndex--;
			this.ActivateStage(this._stages[this._stageIndex]);
			this._state.Refresh();
		}

		// Token: 0x06001F96 RID: 8086 RVA: 0x0008DE18 File Offset: 0x0008C018
		public void GoToStage(int stageIndex)
		{
			if (stageIndex >= 0 && stageIndex < this._stages.Count && stageIndex != this._stageIndex && stageIndex <= this._furthestStageIndex)
			{
				CharacterCreationStageBase currentStage = this.CurrentStage;
				if (currentStage != null)
				{
					currentStage.OnFinalize();
				}
				this._stageIndex = stageIndex;
				this.ActivateStage(this._stages[this._stageIndex]);
				this._state.Refresh();
			}
		}

		// Token: 0x06001F97 RID: 8087 RVA: 0x0008DE83 File Offset: 0x0008C083
		private void ActivateStage(CharacterCreationStageBase stage)
		{
			this.CurrentStage = stage;
			if (this._stageIndex == 0)
			{
				this.FaceGenHistory.ClearHistory();
			}
			this._state.OnStageActivated(this.CurrentStage);
		}

		// Token: 0x06001F98 RID: 8088 RVA: 0x0008DEB0 File Offset: 0x0008C0B0
		internal void OnStateActivated()
		{
			if (this._stageIndex == -1)
			{
				this.NextStage();
			}
		}

		// Token: 0x06001F99 RID: 8089 RVA: 0x0008DEC1 File Offset: 0x0008C0C1
		public int GetIndexOfCurrentStage()
		{
			return this._stageIndex;
		}

		// Token: 0x06001F9A RID: 8090 RVA: 0x0008DEC9 File Offset: 0x0008C0C9
		public int GetTotalStagesCount()
		{
			return this._stages.Count;
		}

		// Token: 0x06001F9B RID: 8091 RVA: 0x0008DED6 File Offset: 0x0008C0D6
		public int GetFurthestIndex()
		{
			return this._furthestStageIndex;
		}

		// Token: 0x06001F9C RID: 8092 RVA: 0x0008DEDE File Offset: 0x0008C0DE
		public void AddNewMenu(NarrativeMenu menu)
		{
			this._narrativeMenus.Add(menu);
		}

		// Token: 0x06001F9D RID: 8093 RVA: 0x0008DEEC File Offset: 0x0008C0EC
		public NarrativeMenu GetCurrentMenu(int index)
		{
			if (index >= 0 && index < this.NarrativeMenus.Count)
			{
				return this.NarrativeMenus[index];
			}
			return null;
		}

		// Token: 0x06001F9E RID: 8094 RVA: 0x0008DF0E File Offset: 0x0008C10E
		public IEnumerable<NarrativeMenuOption> GetCurrentMenuOptions(int index)
		{
			NarrativeMenu currentMenu = this.GetCurrentMenu(index);
			if (currentMenu == null)
			{
				return null;
			}
			return currentMenu.CharacterCreationMenuOptions;
		}

		// Token: 0x06001F9F RID: 8095 RVA: 0x0008DF24 File Offset: 0x0008C124
		public NarrativeMenu GetNarrativeMenuWithId(string stringId)
		{
			return this.NarrativeMenus.FirstOrDefault((NarrativeMenu m) => m.StringId.Equals(stringId));
		}

		// Token: 0x06001FA0 RID: 8096 RVA: 0x0008DF58 File Offset: 0x0008C158
		public void DeleteNarrativeMenuWithId(string stringId)
		{
			NarrativeMenu narrativeMenu = null;
			foreach (NarrativeMenu narrativeMenu2 in this.NarrativeMenus)
			{
				if (narrativeMenu2.StringId.Equals(stringId))
				{
					narrativeMenu = narrativeMenu2;
					break;
				}
			}
			if (narrativeMenu != null)
			{
				this._narrativeMenus.Remove(narrativeMenu);
			}
		}

		// Token: 0x06001FA1 RID: 8097 RVA: 0x0008DFC8 File Offset: 0x0008C1C8
		public void ResetNarrativeMenus()
		{
			this._narrativeMenus.Clear();
			this.ResetMenuOptions();
		}

		// Token: 0x06001FA2 RID: 8098 RVA: 0x0008DFDB File Offset: 0x0008C1DB
		public void ResetMenuOptions()
		{
			this.SelectedOptions.Clear();
		}

		// Token: 0x06001FA3 RID: 8099 RVA: 0x0008DFE8 File Offset: 0x0008C1E8
		public void StartNarrativeStage()
		{
			NarrativeMenu currentMenu = this.NarrativeMenus.FirstOrDefault((NarrativeMenu m) => m.InputMenuId == "start");
			this.CurrentMenu = currentMenu;
			this.ModifyMenuCharacters();
		}

		// Token: 0x06001FA4 RID: 8100 RVA: 0x0008E030 File Offset: 0x0008C230
		public bool TrySwitchToNextMenu()
		{
			string stringId = this.CurrentMenu.StringId;
			this.SelectedOptions[this.CurrentMenu].OnConsequence(this);
			foreach (NarrativeMenu narrativeMenu in this.NarrativeMenus)
			{
				if (narrativeMenu.InputMenuId.Equals(stringId))
				{
					this.CurrentMenu = narrativeMenu;
					this.ModifyMenuCharacters();
					return true;
				}
			}
			return false;
		}

		// Token: 0x06001FA5 RID: 8101 RVA: 0x0008E0C4 File Offset: 0x0008C2C4
		private void ModifyMenuCharacters()
		{
			List<NarrativeMenuCharacter> characters = this.CurrentMenu.Characters;
			foreach (NarrativeMenuCharacterArgs narrativeMenuCharacterArgs in this.CurrentMenu.GetNarrativeMenuCharacterArgs(this.CharacterCreationContent.SelectedCulture, this.CharacterCreationContent.SelectedTitleType, this))
			{
				foreach (NarrativeMenuCharacter narrativeMenuCharacter in characters)
				{
					if (narrativeMenuCharacter.StringId == narrativeMenuCharacterArgs.CharacterId)
					{
						if (narrativeMenuCharacterArgs.IsHuman)
						{
							MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(narrativeMenuCharacterArgs.EquipmentId);
							if (@object == null)
							{
								Debug.FailedAssert("character creation menu character equipment should not be null! Equipment id: " + narrativeMenuCharacterArgs.EquipmentId, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CharacterCreationContent\\CharacterCreationManager.cs", "ModifyMenuCharacters", 305);
								@object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
							}
							narrativeMenuCharacter.SetEquipment(@object);
							narrativeMenuCharacter.SetLeftHandItem(narrativeMenuCharacterArgs.LeftHandItemId);
							narrativeMenuCharacter.SetRightHandItem(narrativeMenuCharacterArgs.RightHandItemId);
							narrativeMenuCharacter.ChangeAge((float)narrativeMenuCharacterArgs.Age);
							narrativeMenuCharacter.IsFemale = narrativeMenuCharacterArgs.IsFemale;
						}
						else
						{
							narrativeMenuCharacter.SetMountCreationKey(narrativeMenuCharacterArgs.MountCreationKey);
							narrativeMenuCharacter.SetHorseItemId(narrativeMenuCharacterArgs.LeftHandItemId);
							narrativeMenuCharacter.SetHarnessItemId(narrativeMenuCharacterArgs.RightHandItemId);
						}
						narrativeMenuCharacter.SetAnimationId(narrativeMenuCharacterArgs.AnimationId);
						narrativeMenuCharacter.SetSpawnPointEntityId(narrativeMenuCharacterArgs.SpawnPointEntityId);
						break;
					}
				}
			}
		}

		// Token: 0x06001FA6 RID: 8102 RVA: 0x0008E294 File Offset: 0x0008C494
		public bool TrySwitchToPreviousMenu()
		{
			string inputMenuId = this.CurrentMenu.InputMenuId;
			foreach (NarrativeMenu narrativeMenu in this.NarrativeMenus)
			{
				if (narrativeMenu.StringId.Equals(inputMenuId))
				{
					this.CurrentMenu = narrativeMenu;
					this.ModifyMenuCharacters();
					return true;
				}
			}
			return false;
		}

		// Token: 0x06001FA7 RID: 8103 RVA: 0x0008E310 File Offset: 0x0008C510
		public void OnNarrativeMenuOptionSelected(NarrativeMenuOption option)
		{
			this.SelectedOptions[this.CurrentMenu] = option;
			option.OnSelect(this);
		}

		// Token: 0x06001FA8 RID: 8104 RVA: 0x0008E32B File Offset: 0x0008C52B
		public IEnumerable<NarrativeMenuOption> GetSuitableNarrativeMenuOptions()
		{
			return from o in this.CurrentMenu.CharacterCreationMenuOptions
				where o.OnCondition(this)
				select o;
		}

		// Token: 0x06001FA9 RID: 8105 RVA: 0x0008E34C File Offset: 0x0008C54C
		public void ApplyFinalEffects()
		{
			Clan.PlayerClan.Renown = 0f;
			this.CharacterCreationContent.ApplyCulture(this);
			foreach (KeyValuePair<NarrativeMenu, NarrativeMenuOption> keyValuePair in this.SelectedOptions)
			{
				keyValuePair.Value.ApplyFinalEffects(this.CharacterCreationContent);
			}
			TraitLevelingHelper.UpdateTraitXPAccordingToTraitLevels();
			CultureObject culture = CharacterObject.PlayerCharacter.Culture;
			if (culture.StartingPoint.IsNonZero())
			{
				if (NavigationHelper.IsPositionValidForNavigationType(culture.StartingPoint, MobileParty.MainParty.NavigationCapability))
				{
					MobileParty.MainParty.Position = culture.StartingPoint;
				}
				else
				{
					Debug.FailedAssert("Selected culture start pos is invalid!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CharacterCreationContent\\CharacterCreationManager.cs", "ApplyFinalEffects", 382);
					CampaignVec2 closestNavMeshFaceCenterPositionForPosition = NavigationHelper.GetClosestNavMeshFaceCenterPositionForPosition(culture.StartingPoint, Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.MainParty.NavigationCapability));
					MobileParty.MainParty.Position = closestNavMeshFaceCenterPositionForPosition;
				}
			}
			MapState mapState;
			if ((mapState = GameStateManager.Current.ActiveState as MapState) != null)
			{
				mapState.Handler.ResetCamera(true, true);
				mapState.Handler.TeleportCameraToMainParty();
			}
			foreach (KeyValuePair<int, ICharacterCreationContentHandler> keyValuePair2 in this._handlers)
			{
				keyValuePair2.Value.OnCharacterCreationFinalize(this);
			}
		}

		// Token: 0x04000931 RID: 2353
		private readonly MBList<CharacterCreationStageBase> _stages;

		// Token: 0x04000932 RID: 2354
		private readonly MBList<NarrativeMenu> _narrativeMenus;

		// Token: 0x04000933 RID: 2355
		public readonly Dictionary<NarrativeMenu, NarrativeMenuOption> SelectedOptions;

		// Token: 0x04000936 RID: 2358
		private SortedList<int, ICharacterCreationContentHandler> _handlers = new SortedList<int, ICharacterCreationContentHandler>();

		// Token: 0x04000937 RID: 2359
		private readonly CharacterCreationState _state;

		// Token: 0x04000938 RID: 2360
		private int _stageIndex = -1;

		// Token: 0x0400093A RID: 2362
		public readonly FaceGenHistory FaceGenHistory;

		// Token: 0x0400093B RID: 2363
		private int _furthestStageIndex;
	}
}
