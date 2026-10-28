using System.Collections.Generic;
using NavalDLC.CharacterDevelopment;
using NavalDLC.Map;
using NavalDLC.Settlements;
using NavalDLC.Settlements.Building;
using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace NavalDLC;

public class NavalDLCManager : GameHandler
{
	public static NavalDLCManager Instance;

	public GameModels GameModels { get; private set; }

	public NavalCulturalFeats NavalCulturalFeats { get; private set; }

	public NavalBuildingTypes NavalBuildingTypes { get; private set; }

	public NavalVillageTypes NavalVillageTypes { get; private set; }

	public NavalSkills NavalSkills { get; private set; }

	public NavalSkillEffects NavalSkillEffects { get; private set; }

	public NavalPerks NavalPerks { get; private set; }

	public NavalPolicies NavalPolicies { get; private set; }

	public NavalStorylineData NavalStorylineData { get; private set; }

	public NavalDLCEvents NavalDLCEvents { get; private set; }

	public NavalItemCategories NavalItemCategories { get; private set; }

	public INavalMapSceneWrapper NavalMapSceneWrapper { get; set; }

	public Dictionary<Village, List<FishingPartyComponent>> FishingParties { get; private set; }

	public StormManager StormManager { get; internal set; }

	public override void OnAfterSave()
	{
	}

	public override void OnBeforeSave()
	{
	}

	protected override void OnInitialize()
	{
	}

	protected override void OnTick(float dt)
	{
		NavalMapSceneWrapper?.Tick(dt);
	}

	public void OnGameStart(Game game, IGameStarter gameStarter)
	{
		GameModels = game.AddGameModelsManager<GameModels>(gameStarter.Models);
		if (game.GameType is Campaign)
		{
			NavalDLCEvents = new NavalDLCEvents();
			Campaign.Current.AddCampaignEventReceiver(NavalDLCEvents);
			if (Campaign.Current.CampaignGameLoadingType == Campaign.GameLoadingType.NewCampaign || Campaign.Current.CampaignGameLoadingType == Campaign.GameLoadingType.Tutorial)
			{
				Campaign.Current.AddCustomManager<StormManager>();
				StormManager = Campaign.Current.GetCustomManager<StormManager>();
			}
			else if (Campaign.Current.CampaignGameLoadingType == Campaign.GameLoadingType.SavedCampaign)
			{
				StormManager = Campaign.Current.GetCustomManager<StormManager>();
				StormManager.OnAfterLoad();
			}
		}
	}

	public void OnGameEnd(Game game)
	{
		NavalMapSceneWrapper = null;
		Instance = null;
	}

	public void InitializeNavalGameObjects(Game game)
	{
		Campaign campaign = game.GameType as Campaign;
		if (campaign != null)
		{
			NavalBuildingTypes = new NavalBuildingTypes();
			NavalCulturalFeats = new NavalCulturalFeats();
			NavalItemCategories = new NavalItemCategories();
			NavalVillageTypes = new NavalVillageTypes();
			NavalStorylineData = new NavalStorylineData();
		}
		NavalSkills = new NavalSkills();
		if (campaign != null)
		{
			NavalSkillEffects = new NavalSkillEffects();
			NavalPerks = new NavalPerks();
			NavalPolicies = new NavalPolicies();
			campaign.SkillLevelingManager = new NavalSkillLevellingManager();
			FishingParties = new Dictionary<Village, List<FishingPartyComponent>>();
		}
	}
}
