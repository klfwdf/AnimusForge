using System;
using System.Collections.Generic;
using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace NavalDLC.CampaignBehaviors;

public class PortCharactersCampaignBehavior : CampaignBehaviorBase
{
	private const float PortTownsmanCarryingStuffSpawnPercentage = 0.6f;

	private const float PortTownsmanSpawnPercentageMale = 0.2f;

	private const float PortTownsmanSpawnPercentageFemale = 0.1f;

	private const float ShipyardWorkerSpawnPercentage = 1f;

	private const float MarketWorkerSpawnPercentage = 0.75f;

	private const float CarpenterSpawnPercentage = 0.35f;

	private static List<(string, bool)> _itemToCarryAndIsMainHandData = new List<(string, bool)>
	{
		("wood_load", true),
		("bucket_filled", false),
		("carry_fish_stick", false)
	};

	public override void RegisterEvents()
	{
		CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnAfterSessionLaunched(CampaignGameStarter campaignGameSystemStarter)
	{
		AddDialogs(campaignGameSystemStarter);
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		Location location = Settlement.CurrentSettlement?.LocationComplex.GetLocationWithId("port");
		if (location != null && !NavalStorylineData.IsNavalStoryLineActive())
		{
			if (unusedUsablePointCount.TryGetValue("sp_shipwright", out var value))
			{
				location.AddLocationCharacters(CreateShipWright, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
			}
			if (unusedUsablePointCount.TryGetValue("merchant_carpenter", out value))
			{
				int count = 1 + (int)((float)value * 0.35f);
				location.AddLocationCharacters(CreatePortMerchant, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, count);
			}
			if (unusedUsablePointCount.TryGetValue("npc_common", out value))
			{
				float num = (float)value * 0.2f;
				location.AddLocationCharacters(CreateTownsPeopleMale, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, (int)num);
				float num2 = (float)value * 0.1f;
				location.AddLocationCharacters(CreateTownsPeopleFemale, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, (int)num2);
			}
			if (unusedUsablePointCount.TryGetValue("npc_common_limited", out value))
			{
				float num3 = (float)value * 0.6f;
				location.AddLocationCharacters(CreateTownsManCarryingStuff, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, (int)num3);
			}
			if (unusedUsablePointCount.TryGetValue("shipyard_worker", out value))
			{
				float num4 = (float)value * 1f;
				location.AddLocationCharacters(CreateShipyardWorker, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, (int)num4);
			}
			if (unusedUsablePointCount.TryGetValue("market_worker", out value))
			{
				float num5 = (float)value * 0.75f;
				location.AddLocationCharacters(CreatePortMarketWorker, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, (int)num5);
			}
			if (unusedUsablePointCount.TryGetValue("static_npc", out value))
			{
				location.AddLocationCharacters(CreateStaticTownsPeopleMale, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, value);
			}
			if (unusedUsablePointCount.TryGetValue("musician", out value) && value > 0)
			{
				location.AddLocationCharacters(CreateMusician, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, value);
			}
		}
	}

	private static LocationCharacter CreateTownsManCarryingStuff(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townsman = culture.Townsman;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(townsman.Race, "_settlement_slow");
		var (suffix, text, flag) = GetRandomActionSetSuffixAndItem();
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out var minimumAge, out var maximumAge, "TownsfolkCarryingStuff");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(townsman)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(text);
		LocationCharacter locationCharacter = new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common_limited", fixedLocation: false, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, townsman.IsFemale, suffix), useCivilianEquipment: true, isFixedCharacter: false, itemObject);
		if (itemObject == null)
		{
			locationCharacter.PrefabNamesForBones.Add(flag ? agentData.AgentMonster.MainHandItemBoneIndex : agentData.AgentMonster.OffHandItemBoneIndex, text);
		}
		return locationCharacter;
	}

	private static LocationCharacter CreateTownsPeopleMale(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townsman = culture.Townsman;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(townsman.Race, "_settlement_slow");
		Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, isFemale: false, "_villager_2"), monsterWithSuffix);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townsman)).Monster(tuple.Item2).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddIndoorWandererBehaviors, "npc_common", fixedLocation: false, relation, tuple.Item1, useCivilianEquipment: true);
	}

	private static LocationCharacter CreateStaticTownsPeopleMale(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townsman = culture.Townsman;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(townsman.Race, "_settlement_slow");
		Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, isFemale: false, "_villager_2"), monsterWithSuffix);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townsman)).Monster(tuple.Item2).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddIndoorWandererBehaviors, "static_npc", fixedLocation: false, relation, tuple.Item1, useCivilianEquipment: true);
	}

	private static LocationCharacter CreateTownsPeopleFemale(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject townswoman = culture.Townswoman;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(townswoman.Race, "_settlement_slow");
		Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, isFemale: true, "_villager_2"), monsterWithSuffix);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(townswoman)).Monster(tuple.Item2).Age(MBRandom.RandomInt(minimumAge, maximumAge)).IsFemale(isFemale: true), SandBoxManager.Instance.AgentBehaviorManager.AddIndoorWandererBehaviors, "npc_common", fixedLocation: false, relation, tuple.Item1, useCivilianEquipment: true);
	}

	private static LocationCharacter CreateShipyardWorker(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject shipyardWorker = culture.ShipyardWorker;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(shipyardWorker.Race, "_settlement_slow");
		Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, isFemale: false, "_villager_2"), monsterWithSuffix);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(shipyardWorker, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(shipyardWorker)).Monster(tuple.Item2).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddIndoorWandererBehaviors, "shipyard_worker", fixedLocation: true, relation, tuple.Item1, useCivilianEquipment: true);
	}

	private static LocationCharacter CreatePortMarketWorker(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject shipyardWorker = culture.ShipyardWorker;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(shipyardWorker.Race, "_settlement_slow");
		Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, isFemale: false, "_villager_2"), monsterWithSuffix);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(shipyardWorker, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(shipyardWorker)).Monster(tuple.Item2).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddIndoorWandererBehaviors, "market_worker", fixedLocation: true, relation, tuple.Item1, useCivilianEquipment: true);
	}

	private static LocationCharacter CreatePortMerchant(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject merchant = culture.Merchant;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(merchant.Race, "_settlement_slow");
		Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, isFemale: false, "_villager_2"), monsterWithSuffix);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(merchant, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(merchant)).Monster(tuple.Item2).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors, "shipyard_shop_worker", fixedLocation: false, relation, tuple.Item1, useCivilianEquipment: true);
	}

	private static LocationCharacter CreateMusician(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject musician = culture.Musician;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(musician.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(musician, out var minimumAge, out var maximumAge);
		AgentData agentData = new AgentData(new SimpleAgentOrigin(musician)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "musician", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_musician"), useCivilianEquipment: true);
	}

	private static LocationCharacter CreateShipWright(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject shipwright = culture.Shipwright;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(shipwright.Race, "_settlement_slow");
		Tuple<string, Monster> tuple = new Tuple<string, Monster>(ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, isFemale: false, "_villager_2"), monsterWithSuffix);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(shipwright, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(shipwright)).Monster(tuple.Item2).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors, "sp_shipwright", fixedLocation: true, relation, tuple.Item1, useCivilianEquipment: true);
	}

	public static (string, string, bool) GetRandomActionSetSuffixAndItem()
	{
		string item = _itemToCarryAndIsMainHandData.GetRandomElement().Item1;
		return item switch
		{
			"wood_load" => ("_worker_carry_wood_on_shoulder", item, true), 
			"bucket_filled" => ("_villager_carry_bucket_on_lefthand", item, false), 
			"carry_fish_stick" => ("_villager_carry_fish_buckets", item, false), 
			_ => ("_worker_carry_wood_on_shoulder", item, true), 
		};
	}

	private static void AddDialogs(CampaignGameStarter campaignGameSystemStarter)
	{
		campaignGameSystemStarter.AddDialogLine("shipwright_dialog_start", "start", "close_window", "{=PZk5f99h}Greetings, {?PLAYER.GENDER}madam{?}sir{\\?}. This is where we lay the keels, fit the planks, and nail them all together.", shipwright_default_dialog_start, null);
		campaignGameSystemStarter.AddDialogLine("shipyard_market_worker", "start", "close_window", "{=!}Greetings, {?PLAYER.GENDER}madam{?}sir{\\?}. This is where we pack the stores for all those sailors and travelers about to put to sea.", shipyard_marker_worker_default_dialog_start, null);
	}

	private static (Occupation ConversationCharacterOccupation, string ConversationCharacterSpecialTag) GetConversationCharacterInfo()
	{
		if (Campaign.Current.ConversationManager.OneToOneConversationCharacter != null && Campaign.Current.ConversationManager.OneToOneConversationAgent != null && Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.LocationComplex != null)
		{
			CharacterObject oneToOneConversationCharacter = Campaign.Current.ConversationManager.OneToOneConversationCharacter;
			IAgent oneToOneConversationAgent = Campaign.Current.ConversationManager.OneToOneConversationAgent;
			return new ValueTuple<Occupation, string>(item2: Settlement.CurrentSettlement.LocationComplex.FindCharacter(oneToOneConversationAgent)?.SpecialTargetTag, item1: oneToOneConversationCharacter.Occupation);
		}
		return (ConversationCharacterOccupation: Occupation.NotAssigned, ConversationCharacterSpecialTag: string.Empty);
	}

	private static bool shipwright_default_dialog_start()
	{
		(Occupation, string) conversationCharacterInfo = GetConversationCharacterInfo();
		if (conversationCharacterInfo.Item1 == Occupation.ShipWright)
		{
			if (!(conversationCharacterInfo.Item2 == "shipyard_worker"))
			{
				return conversationCharacterInfo.Item2 == "sp_shipwright";
			}
			return true;
		}
		return false;
	}

	private static bool shipyard_marker_worker_default_dialog_start()
	{
		(Occupation, string) conversationCharacterInfo = GetConversationCharacterInfo();
		if (conversationCharacterInfo.Item1 == Occupation.ShipWright)
		{
			return conversationCharacterInfo.Item2 == "market_worker";
		}
		return false;
	}
}
