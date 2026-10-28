using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent;

public class CharacterCreationManager
{
	private readonly MBList<CharacterCreationStageBase> _stages;

	private readonly MBList<NarrativeMenu> _narrativeMenus;

	public readonly Dictionary<NarrativeMenu, NarrativeMenuOption> SelectedOptions;

	private SortedList<int, ICharacterCreationContentHandler> _handlers = new SortedList<int, ICharacterCreationContentHandler>();

	private readonly CharacterCreationState _state;

	private int _stageIndex = -1;

	public readonly FaceGenHistory FaceGenHistory;

	private int _furthestStageIndex;

	public MBReadOnlyList<NarrativeMenu> NarrativeMenus => _narrativeMenus;

	public CharacterCreationContent CharacterCreationContent { get; private set; }

	public NarrativeMenu CurrentMenu { get; private set; }

	public int CharacterCreationMenuCount => NarrativeMenus.Count;

	public CharacterCreationStageBase CurrentStage { get; private set; }

	public CharacterCreationManager(CharacterCreationState state)
	{
		_state = state;
		_stages = new MBList<CharacterCreationStageBase>();
		FaceGenHistory = new FaceGenHistory(new List<UndoRedoKey>(100), new List<UndoRedoKey>(100), new Dictionary<string, float>());
		_narrativeMenus = new MBList<NarrativeMenu>();
		SelectedOptions = new Dictionary<NarrativeMenu, NarrativeMenuOption>();
		CharacterCreationContent = new CharacterCreationContent();
		CampaignEventDispatcher.Instance.OnCharacterCreationInitialized(this);
		foreach (KeyValuePair<int, ICharacterCreationContentHandler> handler in _handlers)
		{
			handler.Value.InitializeContent(this);
		}
		foreach (KeyValuePair<int, ICharacterCreationContentHandler> handler2 in _handlers)
		{
			handler2.Value.AfterInitializeContent(this);
		}
	}

	public void RegisterCharacterCreationContentHandler(ICharacterCreationContentHandler characterCreationContentHandler, int priority)
	{
		_handlers.Add(priority, characterCreationContentHandler);
	}

	public void AddStage(CharacterCreationStageBase stage)
	{
		_stages.Add(stage);
	}

	public bool RemoveStage<T>() where T : CharacterCreationStageBase
	{
		for (int i = 0; i < _stages.Count; i++)
		{
			if (_stages[i] is T)
			{
				_stages.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	public T GetStage<T>() where T : CharacterCreationStageBase
	{
		for (int i = 0; i < _stages.Count; i++)
		{
			if (_stages[i] is T result)
			{
				return result;
			}
		}
		return null;
	}

	public void NextStage()
	{
		_stageIndex++;
		if (CurrentStage != null)
		{
			CurrentStage?.OnFinalize();
			foreach (KeyValuePair<int, ICharacterCreationContentHandler> handler in _handlers)
			{
				handler.Value.OnStageCompleted(CurrentStage);
			}
		}
		_furthestStageIndex = MathF.Max(_furthestStageIndex, _stageIndex);
		if (_stageIndex == _stages.Count)
		{
			ApplyFinalEffects();
			_state.FinalizeCharacterCreationState();
		}
		else
		{
			ActivateStage(_stages[_stageIndex]);
			_state.Refresh();
		}
	}

	public void PreviousStage()
	{
		CurrentStage?.OnFinalize();
		_stageIndex--;
		ActivateStage(_stages[_stageIndex]);
		_state.Refresh();
	}

	public void GoToStage(int stageIndex)
	{
		if (stageIndex >= 0 && stageIndex < _stages.Count && stageIndex != _stageIndex && stageIndex <= _furthestStageIndex)
		{
			CurrentStage?.OnFinalize();
			_stageIndex = stageIndex;
			ActivateStage(_stages[_stageIndex]);
			_state.Refresh();
		}
	}

	private void ActivateStage(CharacterCreationStageBase stage)
	{
		CurrentStage = stage;
		if (_stageIndex == 0)
		{
			FaceGenHistory.ClearHistory();
		}
		_state.OnStageActivated(CurrentStage);
	}

	internal void OnStateActivated()
	{
		if (_stageIndex == -1)
		{
			NextStage();
		}
	}

	public int GetIndexOfCurrentStage()
	{
		return _stageIndex;
	}

	public int GetTotalStagesCount()
	{
		return _stages.Count;
	}

	public int GetFurthestIndex()
	{
		return _furthestStageIndex;
	}

	public void AddNewMenu(NarrativeMenu menu)
	{
		_narrativeMenus.Add(menu);
	}

	public NarrativeMenu GetCurrentMenu(int index)
	{
		if (index >= 0 && index < NarrativeMenus.Count)
		{
			return NarrativeMenus[index];
		}
		return null;
	}

	public IEnumerable<NarrativeMenuOption> GetCurrentMenuOptions(int index)
	{
		return GetCurrentMenu(index)?.CharacterCreationMenuOptions;
	}

	public NarrativeMenu GetNarrativeMenuWithId(string stringId)
	{
		return NarrativeMenus.FirstOrDefault((NarrativeMenu m) => m.StringId.Equals(stringId));
	}

	public void DeleteNarrativeMenuWithId(string stringId)
	{
		NarrativeMenu narrativeMenu = null;
		foreach (NarrativeMenu narrativeMenu2 in NarrativeMenus)
		{
			if (narrativeMenu2.StringId.Equals(stringId))
			{
				narrativeMenu = narrativeMenu2;
				break;
			}
		}
		if (narrativeMenu != null)
		{
			_narrativeMenus.Remove(narrativeMenu);
		}
	}

	public void ResetNarrativeMenus()
	{
		_narrativeMenus.Clear();
		ResetMenuOptions();
	}

	public void ResetMenuOptions()
	{
		SelectedOptions.Clear();
	}

	public void StartNarrativeStage()
	{
		NarrativeMenu currentMenu = NarrativeMenus.FirstOrDefault((NarrativeMenu m) => m.InputMenuId == "start");
		CurrentMenu = currentMenu;
		ModifyMenuCharacters();
	}

	public bool TrySwitchToNextMenu()
	{
		string stringId = CurrentMenu.StringId;
		SelectedOptions[CurrentMenu].OnConsequence(this);
		foreach (NarrativeMenu narrativeMenu in NarrativeMenus)
		{
			if (narrativeMenu.InputMenuId.Equals(stringId))
			{
				CurrentMenu = narrativeMenu;
				ModifyMenuCharacters();
				return true;
			}
		}
		return false;
	}

	private void ModifyMenuCharacters()
	{
		List<NarrativeMenuCharacter> characters = CurrentMenu.Characters;
		foreach (NarrativeMenuCharacterArgs item in CurrentMenu.GetNarrativeMenuCharacterArgs(CharacterCreationContent.SelectedCulture, CharacterCreationContent.SelectedTitleType, this))
		{
			foreach (NarrativeMenuCharacter item2 in characters)
			{
				if (!(item2.StringId == item.CharacterId))
				{
					continue;
				}
				if (item.IsHuman)
				{
					MBEquipmentRoster mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(item.EquipmentId);
					if (mBEquipmentRoster == null)
					{
						Debug.FailedAssert("character creation menu character equipment should not be null! Equipment id: " + item.EquipmentId, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CharacterCreationContent\\CharacterCreationManager.cs", "ModifyMenuCharacters", 305);
						mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
					}
					item2.SetEquipment(mBEquipmentRoster);
					item2.SetLeftHandItem(item.LeftHandItemId);
					item2.SetRightHandItem(item.RightHandItemId);
					item2.ChangeAge(item.Age);
					item2.IsFemale = item.IsFemale;
				}
				else
				{
					item2.SetMountCreationKey(item.MountCreationKey);
					item2.SetHorseItemId(item.LeftHandItemId);
					item2.SetHarnessItemId(item.RightHandItemId);
				}
				item2.SetAnimationId(item.AnimationId);
				item2.SetSpawnPointEntityId(item.SpawnPointEntityId);
				break;
			}
		}
	}

	public bool TrySwitchToPreviousMenu()
	{
		string inputMenuId = CurrentMenu.InputMenuId;
		foreach (NarrativeMenu narrativeMenu in NarrativeMenus)
		{
			if (narrativeMenu.StringId.Equals(inputMenuId))
			{
				CurrentMenu = narrativeMenu;
				ModifyMenuCharacters();
				return true;
			}
		}
		return false;
	}

	public void OnNarrativeMenuOptionSelected(NarrativeMenuOption option)
	{
		SelectedOptions[CurrentMenu] = option;
		option.OnSelect(this);
	}

	public IEnumerable<NarrativeMenuOption> GetSuitableNarrativeMenuOptions()
	{
		return CurrentMenu.CharacterCreationMenuOptions.Where((NarrativeMenuOption o) => o.OnCondition(this));
	}

	public void ApplyFinalEffects()
	{
		Clan.PlayerClan.Renown = 0f;
		CharacterCreationContent.ApplyCulture(this);
		foreach (KeyValuePair<NarrativeMenu, NarrativeMenuOption> selectedOption in SelectedOptions)
		{
			selectedOption.Value.ApplyFinalEffects(CharacterCreationContent);
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
		if (GameStateManager.Current.ActiveState is MapState mapState)
		{
			mapState.Handler.ResetCamera(resetDistance: true, teleportToMainParty: true);
			mapState.Handler.TeleportCameraToMainParty();
		}
		foreach (KeyValuePair<int, ICharacterCreationContentHandler> handler in _handlers)
		{
			handler.Value.OnCharacterCreationFinalize(this);
		}
	}
}
