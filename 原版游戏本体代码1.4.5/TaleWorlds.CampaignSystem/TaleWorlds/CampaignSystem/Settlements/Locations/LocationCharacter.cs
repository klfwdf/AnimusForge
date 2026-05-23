using System.Collections.Generic;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Settlements.Locations;

public class LocationCharacter
{
	public delegate void AddBehaviorsDelegate(IAgent agent);

	public delegate void AfterAgentCreatedDelegate(IAgent agent);

	public enum CharacterRelations
	{
		Neutral,
		Friendly,
		Enemy
	}

	public bool IsVisualTracked;

	public Dictionary<sbyte, string> PrefabNamesForBones;

	public CharacterRelations CharacterRelation;

	public CharacterObject Character => (CharacterObject)AgentData.AgentCharacter;

	public IAgentOriginBase AgentOrigin => AgentData.AgentOrigin;

	public AgentData AgentData { get; }

	public bool UseCivilianEquipment { get; }

	public string ActionSetCode { get; }

	public string AlarmedActionSetCode { get; }

	public string SpecialTargetTag { get; set; }

	public bool ForceSpawnInSpecialTargetTag { get; set; }

	public AddBehaviorsDelegate AddBehaviors { get; }

	public AfterAgentCreatedDelegate AfterAgentCreated { get; }

	public bool FixedLocation { get; }

	public Alley MemberOfAlley { get; private set; }

	public ItemObject SpecialItem { get; }

	public LocationCharacter(AgentData agentData, AddBehaviorsDelegate addBehaviorsDelegate, string spawnTag, bool fixedLocation, CharacterRelations characterRelation, string actionSetCode, bool useCivilianEquipment, bool isFixedCharacter = false, ItemObject specialItem = null, bool isHidden = false, bool isVisualTracked = false, bool overrideBodyProperties = true, AfterAgentCreatedDelegate afterAgentCreated = null, bool forceSpawnOnSpecialTargetTag = false)
	{
		AgentData = agentData;
		if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
		{
			int seed = -2;
			if (overrideBodyProperties)
			{
				seed = (isFixedCharacter ? (Settlement.CurrentSettlement.StringId + "_" + Character.StringId).GetDeterministicHashCode() : agentData.AgentEquipmentSeed);
			}
			AgentData.BodyProperties(Character.GetBodyProperties(Character.Equipment, seed));
		}
		AddBehaviors = addBehaviorsDelegate;
		SpecialTargetTag = spawnTag;
		FixedLocation = fixedLocation;
		ActionSetCode = actionSetCode ?? TaleWorlds.Core.ActionSetCode.GenerateActionSetNameWithSuffix(AgentData.AgentMonster, AgentData.AgentCharacter.IsFemale, "_villager");
		AlarmedActionSetCode = TaleWorlds.Core.ActionSetCode.GenerateActionSetNameWithSuffix(AgentData.AgentMonster, AgentData.AgentIsFemale, "_villager");
		PrefabNamesForBones = new Dictionary<sbyte, string>();
		CharacterRelation = characterRelation;
		SpecialItem = specialItem;
		UseCivilianEquipment = useCivilianEquipment;
		AfterAgentCreated = afterAgentCreated;
		IsVisualTracked = isVisualTracked;
		if (forceSpawnOnSpecialTargetTag)
		{
			ForceSpawnInSpecialTargetTag = true;
		}
	}

	public void SetAlleyOfCharacter(Alley alley)
	{
		MemberOfAlley = alley;
	}

	public static LocationCharacter CreateBodyguardHero(Hero hero, MobileParty party, AddBehaviorsDelegate addBehaviorsDelegate)
	{
		UniqueTroopDescriptor uniqueNo = new UniqueTroopDescriptor(FlattenedTroopRoster.GenerateUniqueNoFromParty(party, 0));
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(hero.CharacterObject.Race, "_settlement");
		return new LocationCharacter(new AgentData(new PartyAgentOrigin(PartyBase.MainParty, hero.CharacterObject, -1, uniqueNo)).Monster(monsterWithSuffix).NoHorses(noHorses: true), addBehaviorsDelegate, null, fixedLocation: false, CharacterRelations.Friendly, null, !PlayerEncounter.LocationEncounter.Settlement.IsVillage);
	}
}
