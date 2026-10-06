using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.CampaignBehaviors;

public class NavalIncidentsCampaignBehaviour : CampaignBehaviorBase, INonReadyObjectHandler
{
	public static class IncidentType
	{
		public const string NauticalHazard = "NauticalHazard";

		public const string LifeAtSea = "LifeAtSea";

		public const string HarborWaterfront = "HarborWaterfront";

		public const string River = "River";

		public const string PostNavalBattleScene = "PostNavalBattleScene";
	}

	public static class IncidentTrigger
	{
		public const string LeavingPort = "LeavingPort";

		public const string LeavingEncounterAtSea = "LeavingEncounterAtSea";

		public const string LeavingNavalBattle = "LeavingNavalBattle";
	}

	private const string PortMenuId = "port_menu";

	private const string PortSetSailOptionId = "sail_option";

	private const string WhaleOilItemId = "whale_oil";

	private const string WineItemId = "wine";

	private const string FishItemId = "fish";

	private const float NorthernWatersY = 600f;

	private const float SouthernWatersY = 400f;

	public override void RegisterEvents()
	{
		CampaignEvents.GameMenuOptionSelectedEvent.AddNonSerializedListener(this, OnGameMenuOptionSelected);
		CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnConversationEnded);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	void INonReadyObjectHandler.OnBeforeNonReadyObjectsDeleted()
	{
		InitializeIncidents();
	}

	private void OnGameMenuOptionSelected(GameMenu gameMenu, GameMenuOption option)
	{
		if (gameMenu.StringId == "port_menu" && option.IdString == "sail_option" && MobileParty.MainParty.RandomFloatWithSeed((uint)CampaignTime.Now.ToMinutes) < Campaign.Current.Models.IncidentModel.GetIncidentTriggerGlobalProbability())
		{
			TryInvokeIncident(new Incident.IncidentTrigger("LeavingPort"));
		}
	}

	private void OnConversationEnded(IEnumerable<CharacterObject> conversationCharacters)
	{
		if (PlayerEncounter.Current != null && PlayerEncounter.LeaveEncounter && MobileParty.MainParty.CurrentSettlement == null && MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.RandomFloatWithSeed((uint)CampaignTime.Now.ToMinutes) < Campaign.Current.Models.IncidentModel.GetIncidentTriggerGlobalProbability())
		{
			TryInvokeIncident(new Incident.IncidentTrigger("LeavingEncounterAtSea"));
		}
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		if (mapEvent.IsPlayerMapEvent && mapEvent.IsNavalMapEvent && mapEvent.HasWinner && mapEvent.DefeatedSide != mapEvent.PlayerSide && mapEvent.IsFieldBattle && MobileParty.MainParty.RandomFloatWithSeed((uint)CampaignTime.Now.ToMinutes) < Campaign.Current.Models.IncidentModel.GetIncidentTriggerGlobalProbability())
		{
			TryInvokeIncident(new Incident.IncidentTrigger("LeavingNavalBattle"));
		}
	}

	private void TryInvokeIncident(Incident.IncidentTrigger trigger)
	{
		if ((!trigger.HasTrigger("LeavingPort") || (MobileParty.MainParty.LastVisitedSettlement != null && !MobileParty.MainParty.LastVisitedSettlement.IsSettlementBusy(this))) && (trigger.HasTrigger("LeavingEncounterAtSea") || !Campaign.Current.ConversationManager.IsConversationFlowActive))
		{
			Campaign.Current.IncidentManager.TryInvokeIncident(trigger);
		}
	}

	private Incident RegisterIncident(string id, string title, string description, string triggerId, string type, CampaignTime cooldown, Func<TextObject, bool> condition)
	{
		return RegisterIncident(id, title, description, new string[1] { triggerId }, type, cooldown, condition);
	}

	private Incident RegisterIncident(string id, string title, string description, string[] triggerIds, string type, CampaignTime cooldown, Func<TextObject, bool> condition)
	{
		Incident incident = Game.Current.ObjectManager.RegisterPresumedObject(new Incident(id));
		incident.Initialize(title, description, new Incident.IncidentTrigger(triggerIds), type, cooldown, condition);
		return incident;
	}

	private void InitializeIncidents()
	{
		Incident incident = RegisterIncident("naval_incident_infamous_rocks", "{=Tm1w4bgO}Infamous rocks to your lee", "{=PlZZxksT}The entry to {TOWN_NAME} harbor is marked by a pair of jagged rocks, but the predictable sea-breezes usually make them easy to avoid. Shortly after you cut loose from the docks, however, a cold land wind starts blowing from your stern. It will move you swiftly along but push you leeward, towards the rocks, and should it suddenly shift it could drive {PLAYER_SHIP_NAME} onto them.", "LeavingPort", "NauticalHazard", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.Ships.Count < 3 || !MobileParty.MainParty.LastVisitedSettlement.IsTown)
			{
				return false;
			}
			description.SetTextVariable("TOWN_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			description.SetTextVariable("PLAYER_SHIP_NAME", GetPlayerShipName());
			return true;
		});
		incident.AddOption("{=JrMtxGFU}Such a fine wind is a gift from Heaven. Fill the sails! It will not betray you.", new List<IncidentEffect> { CampaignIncidentEffects.Select((CampaignIncidentEffects.Group(CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, 100), CampaignIncidentEffects.MoraleChange(10f)), 0.95f), (CampaignIncidentEffects.Group(NavalIncidentEffects.FleetShipHitPointsChangeFactor(-0.2f), CampaignIncidentEffects.MoraleChange(-10f)), 0.05f)) });
		incident.AddOption("{=8xpaTQuT}Cut your sails and man your oars, giving your men the tedious task of getting downwind of the rocks, so you are out of danger.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, -100),
			CampaignIncidentEffects.MoraleChange(-10f)
		});
		incident.AddOption("{=4TEt8fWP}Turn about and row back to the docks, and treat your men to a round of drinks while you wait for the winds to change.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 200),
			CampaignIncidentEffects.GoldChange(() => -200)
		});
		Incident incident2 = RegisterIncident("naval_incident_drift_ice", "{=Avb3aP1J}Drift ice", "{=Lt5mE6yf}The northern seas this year are thick with drift ice. Some of your sailors say that beyond the horizon lie huge sheets of ice, and speculate that warm weather may have caused them to break up. Others say that the spirits of the deep send jagged ice shards to the surface to sink ships, so that they may eat the sailors. Either way, you need to decide what to do about them.", "LeavingEncounterAtSea", "NauticalHazard", CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.IsActive && MobileParty.MainParty.Ships.Count > 0 && MobileParty.MainParty.GetPosition2D.y > 600f);
		incident2.AddOption("{=rRDw9P1A}Move slowly, with oarsmen sleeping at their stations ready to backwater and men in the prow with long poles to push you off from any ice that you do not spot in time.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, -100),
			CampaignIncidentEffects.DisorganizeParty()
		});
		incident2.AddOption("{=kRICtQ3c}Press on, trusting in your helmsmen's skill, your lookouts' eyes, and the strength of your hulls.", new List<IncidentEffect> { CampaignIncidentEffects.Select((CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, 100), 0.9f), (CampaignIncidentEffects.Group(NavalIncidentEffects.FleetShipHitPointsChangeFactor(-0.2f), CampaignIncidentEffects.MoraleChange(-10f)), 0.1f)) });
		Incident incident3 = RegisterIncident("naval_incident_bad_luck_sailor", "{=6GB99thn}Sailor brings bad luck", "{=JUx133ap}You've noticed that one of your crew, a {TROOP.NAME}, has been receiving a wide berth from {?TROOP.GENDER}her{?}his{\\?} mates. Through overheard conversations, you work out that the {TROOP.NAME} sleeps with {?TROOP.GENDER}her{?}his{\\?} boots lying on their sides rather than upright - an insult to whichever of the winds the bootsoles are facing, and which, if not corrected, is sure to condemn {PLAYER_SHIP_NAME} to destruction in a storm.", "LeavingEncounterAtSea", "LifeAtSea", CampaignTime.Days(60f), delegate(TextObject description)
		{
			CharacterObject characterObject = GetCrewMember();
			if (characterObject == null || MobileParty.MainParty.MemberRoster.TotalManCount < 40)
			{
				return false;
			}
			description.SetCharacterProperties("TROOP", characterObject);
			description.SetTextVariable("PLAYER_SHIP_NAME", GetPlayerShipName());
			return true;
		});
		incident3.AddOption("{=QKwsYBC8}Have the offender cast into the water for {?TROOP.GENDER}her{?}his{\\?} disrespect to the powers of nature and the custom of the sea.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, -200),
			CampaignIncidentEffects.MoraleChange(5f),
			CampaignIncidentEffects.KillTroop(GetCrewMember, 1)
		}, delegate(TextObject text)
		{
			CharacterObject characterObject = GetCrewMember();
			if (characterObject != null)
			{
				text.SetCharacterProperties("TROOP", characterObject);
				return true;
			}
			return false;
		});
		incident3.AddOption("{=0zt3CGfg}Tell the {TROOP.NAME} to respect {?TROOP.GENDER}her{?}his{\\?} crewmates' customs, or {?TROOP.GENDER}she{?}he{\\?} shall be put ashore at the next port.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, 100),
			CampaignIncidentEffects.MoraleChange(-5f)
		}, delegate(TextObject text)
		{
			CharacterObject characterObject = GetCrewMember();
			if (characterObject != null)
			{
				text.SetCharacterProperties("TROOP", characterObject);
				return true;
			}
			return false;
		});
		incident3.AddOption("{=r0h04KT1}Do nothing. The men who signed on with you swore an oath to obey their captain, not cater to every fanciful superstition that pops into a mariner's head.", new List<IncidentEffect> { CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, 100) });
		incident3.AddOption("{=9vv1JRlN}Flog the offender's mates for spreading discord, telling them that the one they should fear is not the wind-spirits but their captain.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.RenownChange(25f),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, -100)
		});
		Incident incident4 = RegisterIncident("naval_incident_seasick_landlubbers", "{=P8akxuDv}Seasick landlubbers", "{=EtKdNtZ2}You set sail on an unusually choppy sea. One of your {TROOP_TYPE} is hit hard by lubber's sickness. The normally high-spirited youth already resembles a corpse, and even your veterans, who normally consider watching landsmen puke their guts out to be one of the perks of the trade, utter compassionate-sounding grunts as they climb over him.", "LeavingPort", "LifeAtSea", CampaignTime.Days(60f), delegate(TextObject description)
		{
			CharacterObject characterObject = GetSeasickLandlubber();
			if (characterObject == null)
			{
				return false;
			}
			description.SetTextVariable("TROOP_TYPE", characterObject.Name);
			return true;
		});
		incident4.AddOption("{=k47xdmae}No one ever died of sea-sickness, however much they might want their misery to end. It's right and fit that green crewmembers learn that the sea is a harsh mistress with no tolerance for weakness.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, -100)
		});
		incident4.AddOption("{=8U5LUHUd}Steer into the waves a bit to reduce the roll, even though you'll need to tack back a bit to return to your course.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, 100),
			CampaignIncidentEffects.DisorganizeParty()
		});
		Incident incident5 = RegisterIncident("naval_incident_drunken_sailor", "{=Z9fkuDFl}Drunken sailor", "{=UunIe68v}Your crew members sometimes set sail still reeling from their last night in port, and usually their watch-mates cover for their mistakes. When it's the rowing master who's had too much to sleep off properly, however, there's no concealing it. An hour's worth of offbeat time-calling, interspersed by retching, results in {PLAYER_SHIP_NAME} breaking three oars before he passes out. What shall you do with him?", "LeavingPort", "HarborWaterfront", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (!HasLargeGalley())
			{
				return false;
			}
			description.SetTextVariable("PLAYER_SHIP_NAME", GetPlayerShipName());
			return true;
		});
		incident5.AddOption("{=ha2y9PAa}Encourage his mates to inflict one of the traditional humiliations for drunken sailors: shaving his belly with a rusty razor, or leaving him to soak in the bilge.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 100),
			CampaignIncidentEffects.MoraleChange(10f)
		});
		incident5.AddOption("{=v4xCPwq5}Give him a punishment that carries a real risk of death - dragging him beneath the keel from starboard to port -- knowing that this will motivate your crew to be less tolerant of negligence.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, -100),
			CampaignIncidentEffects.PartyExperienceChance(200),
			CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 1).WithChance(0.4f)
		});
		Incident incident6 = RegisterIncident("naval_incident_scurvy", "{=SzI42NtS}Scurvy", "{=xsVSCrfA}Your surgeon {SURGEON.NAME} informs you that some wounded crewmembers are not healing properly, and their gums are an unhealthy color. This is a common sailor's malady, especially on longer voyages. {?SURGEON.GENDER}She{?}He{\\?} believes that {?SURGEON.GENDER}she{?}he{\\?} can treat this with some rhubarb and kale growing along the shore, though it might be hard to get your crew to eat the strong-tasting plants, which some suspect to be poison.", "LeavingEncounterAtSea", "LifeAtSea", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace) != TerrainType.CoastalSea)
			{
				return false;
			}
			if (MobileParty.MainParty.Ships.Count < 2)
			{
				return false;
			}
			if (MobileParty.MainParty.EffectiveSurgeon == null || MobileParty.MainParty.EffectiveSurgeon == Hero.MainHero)
			{
				return false;
			}
			description.SetCharacterProperties("SURGEON", MobileParty.MainParty.EffectiveSurgeon.CharacterObject);
			return true;
		});
		incident6.AddOption("{=ykIWxJUp}Harvest the herb and tell your crew the plain truth: you think this treatment will work, but aren't sure, and each can make the choice and bear the consequences.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, 100),
			CampaignIncidentEffects.WoundTroopsRandomlyWithChanceOfDeath(0.1f, 0.05f)
		});
		incident6.AddOption("{=svC4Hdz6}Tell the crew that these herbs are well-known with many fine virility-restoring properties, and that the ladies of their next port of call will thank them if they consume it.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, -100),
			CampaignIncidentEffects.WoundTroopsRandomlyWithChanceOfDeath(0.1f, 0.05f)
		});
		incident6.AddOption("{=QjiA9l6a}Mix the herbs with wine, as your men will never turn down a drink. {SURGEON.NAME} doesn't know if the alcohol will weaken the herbs' properties, but as a physician and scholar {?SURGEON.GENDER}she{?}he{\\?} is always ready to experiment.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.WoundTroopsRandomlyWithChanceOfDeath(0.15f, 0.05f)
		}, delegate(TextObject text)
		{
			text.SetCharacterProperties("SURGEON", MobileParty.MainParty.EffectiveSurgeon.CharacterObject);
			return true;
		});
		incident6.AddOption("{=abSjmv9Q}Press on without stopping to harvest any herbs. Men can always be replaced. Time cannot.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.WoundTroopsRandomlyWithChanceOfDeath(0.25f, 0.1f),
			CampaignIncidentEffects.SkillChange(DefaultSkills.Medicine, 100f)
		});
		Incident incident7 = RegisterIncident("naval_incident_tricksters_harbor", "{=m8zA3odf}Tricksters' Harbor", "{=XwvloXL5}When you sailed into {PORT_NAME} you noted small flags sticking out of the water. These marked the harbor's infamous shoals, which shift with every storm. Sailing out, however, the flags are missing. Some fishermen pull up alongside you. It would be a great pity if {PLAYER_SHIP_NAME} were to run aground and damage its timbers, they say. But, they know these waters well and could pilot you to safety, for a fee.", "LeavingPort", "HarborWaterfront", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 2)
			{
				return false;
			}
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			description.SetTextVariable("PLAYER_SHIP_NAME", GetPlayerShipName());
			return true;
		});
		incident7.AddOption("{=jNa6b2WU}Villainy must be punished, not rewarded. You seize one of the fishermen and swordpoint and force him to guide you past the reefs, even though his colleagues will no doubt spin a story of your wickedness afterwards.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, 100),
			CampaignIncidentEffects.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10)
		});
		incident7.AddOption("{=FaY2WPOo}You see this not as a danger but an opportunity for your crew to practice their skills. You command {PLAYER_SHIP_NAME} to creep ahead, watching where the locals go, casting the lead at every opportunity to judge depth and find a channel.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, -100),
			CampaignIncidentEffects.PartyExperienceChance(100),
			CampaignIncidentEffects.DisorganizeParty()
		}, delegate(TextObject text)
		{
			text.SetTextVariable("PLAYER_SHIP_NAME", GetPlayerShipName());
			return true;
		});
		incident7.AddOption("{=d86MCwwq}Hire a fisherman to pilot you. Tell him that you respect a man who knows his business, even if that man be a rogue, but he must share his knowledge of the local shoals with you.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, -50),
			CampaignIncidentEffects.SkillChange(NavalSkills.Shipmaster, 200f),
			CampaignIncidentEffects.GoldChange(() => -500)
		});
		incident7.AddOption("{=Ai1dcisD}Wait for the high tide, when the current runs against you but the shoals will be well-covered.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, -100),
			CampaignIncidentEffects.MoraleChange(-10f),
			CampaignIncidentEffects.DisorganizeParty()
		});
		Incident incident8 = RegisterIncident("naval_incident_collision_in_fog", "{=MoavGaXR}Collision in the fog", "{=uGGM6v3A}A thick white fog blankets the harbor as you set sail. You proceed with regular blasts of the horn, as custom requires, but this doesn't stop a small coastal trader from careening out of the gloom and crashing into {PLAYER_SHIP_NAME}. A falling spar hits one of your {TROOP.NAME} on the head. {?TROOP.GENDER}She'll{?}He'll{\\?} live, but might not recover all his wits.", "LeavingPort", "HarborWaterfront", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 2 || GetInjuredCrew() == null)
			{
				return false;
			}
			description.SetTextVariable("PLAYER_SHIP_NAME", GetPlayerShipName());
			description.SetCharacterProperties("TROOP", GetInjuredCrew());
			return true;
		});
		incident8.AddOption("{=iro8QU2p}{TROOP.NAME} don't need a full complement of wits in their line of work, and the fog has cost you enough time already. Curse the trader's crew for the motherless lubbers that they are, and push on to the open sea.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, -100),
			CampaignIncidentEffects.WoundTroop(GetInjuredCrew, 1)
		}, delegate(TextObject text)
		{
			text.SetCharacterProperties("TROOP", GetInjuredCrew());
			return true;
		});
		incident8.AddOption("{=noYCt8f5}File a complaint with the harbormaster as the law demands, even though you'll lose your favorable tide and have to sail with the flood.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, 100),
			CampaignIncidentEffects.WoundTroop(GetInjuredCrew, 1),
			CampaignIncidentEffects.DisorganizeParty()
		});
		incident8.AddOption("{=AWkNKYTt}No one can stop you from taking fair compensation right here, right now. Have your men lift everything from the trader that isn't nailed down.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.ChangeItemAmount(() => DefaultItems.Grain, () => 2),
			CampaignIncidentEffects.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10, (Hero notable) => notable.IsMerchant),
			CampaignIncidentEffects.WoundTroop(GetInjuredCrew, 1),
			CampaignIncidentEffects.MoraleChange(10f)
		});
		incident8.AddOption("{=0LpYc6ci}Take the trader's cargo, and give its captain a knock on the head to match that received by your {TROOP.NAME}.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.WoundTroop(GetInjuredCrew, 1),
			CampaignIncidentEffects.MoraleChange(20f),
			CampaignIncidentEffects.ChangeItemAmount(() => DefaultItems.Grain, () => 2),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, -200),
			CampaignIncidentEffects.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10)
		}, delegate(TextObject text)
		{
			text.SetCharacterProperties("TROOP", GetInjuredCrew());
			return true;
		});
		Incident incident9 = RegisterIncident("naval_incident_sea_snakes", "{=9bk2m4GD}Risky catch - Sea-snakes", "{=pg8J0btk}It's a calm, warm day, when shouts of excitement draw you to the prow. The sea is alive with snakes wriggling in the water. Many of your hands have seen these creatures, but never in such numbers. Some say that they make a fine stew and their bright turquoise and orange skin can be made into pouches. Despite a deadly bite, they are said to be docile enough for swimmers to harvest. Others have their doubts.", "LeavingEncounterAtSea", "NauticalHazard", CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.IsActive && MobileParty.MainParty.Ships.Count > 0 && MobileParty.MainParty.GetPosition2D.y < 400f);
		incident9.AddOption("{=5I5s3K6x}Allow your more headstrong men to dive into the waters to try to grab the serpents by the neck, though caution them that you can do nothing to save them if they are bitten.", new List<IncidentEffect> { CampaignIncidentEffects.Select(CampaignIncidentEffects.MoraleChange(10f), CampaignIncidentEffects.Group(CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 1), CampaignIncidentEffects.MoraleChange(-10f)), 0.8f) });
		incident9.AddOption("{=7kqfnXEQ}Cast out nets to try to trap the serpents, though your ships are not really designed for this purpose and it will be tedious work.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.MoraleChange(5f),
			CampaignIncidentEffects.DisorganizeParty()
		});
		incident9.AddOption("{=olNebjub}Admire the beasts, but leave them alone.", new List<IncidentEffect>());
		Incident incident10 = RegisterIncident("naval_incident_gale_from_desert", "{=naNbjFal}Gale from the desert", "{=GdrgUpGF}The mariners of the Kannic coast call it the Janaiz, the funeral wind, and it is said to carry the souls of sailors who died inland in the Nahhasa to their home far out at sea. It fills the sky with desert dust, giving everything an unearthly bronze hue. Its power can also be harnessed by a captain who is willing to risk falling spars, or men blown off the rigging.", "LeavingEncounterAtSea", "NauticalHazard", CampaignTime.Days(60f), delegate
		{
			if (!MobileParty.MainParty.IsActive || MobileParty.MainParty.Ships.Count == 0)
			{
				return false;
			}
			Vec2 windForPosition = Campaign.Current.Models.MapWeatherModel.GetWindForPosition(MobileParty.MainParty.Position);
			return windForPosition.y > 0f && TaleWorlds.Library.MathF.Abs(windForPosition.y) > TaleWorlds.Library.MathF.Abs(windForPosition.x);
		});
		incident10.AddOption("{=EF8UAZs6}Cut sails, lest a powerful gust damage your masts and spars, or sweep a sailor into the sea.", new List<IncidentEffect> { CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, -100) });
		incident10.AddOption("{=2L2MVdSR}Catch the wind, run out the knotted log-line, turn the sand-glass, and see what kind of speed you can reach.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, 100),
			CampaignIncidentEffects.SkillChange(NavalSkills.Shipmaster, 200f),
			CampaignIncidentEffects.MoraleChange(5f),
			CampaignIncidentEffects.Group(CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 2), NavalIncidentEffects.FlagShipHitPointsChange(-0.1f)).WithChance(0.2f)
		});
		Incident incident11 = RegisterIncident("naval_incident_whaling_opportunity", "{=e0gI0bvS}Whaling opportunity", "{=a7bU26sR}Your lookout cries out from the mast: there's a whale, three bowshots off your bow, and it's just sitting in the water. Usually you'd never get close to a whale in anything larger than a small boat, but this beast looks tired or sick, and {PLAYER_SHIP_NAME} might be just small enough to approach without alarming it. The fresh meat would be welcomed by your crew, andthe oil should fetch a fine price.", "LeavingEncounterAtSea", "NauticalHazard", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (!MobileParty.MainParty.Ships.Any((Ship x) => x.ShipHull.Type == ShipHull.ShipType.Light))
			{
				return false;
			}
			if (GetShipWithBallista() == null)
			{
				return false;
			}
			description.SetTextVariable("PLAYER_SHIP_NAME", GetPlayerShipName());
			return true;
		});
		incident11.AddOption("{=mZCBMaKK}Have your crew fashion makeshift harpoons and creep close under oar. Most likely the beast is not too strong enough to pull you under if it dives, and if it does, you can cut the ropes.", new List<IncidentEffect> { CampaignIncidentEffects.Select((CampaignIncidentEffects.Group(CampaignIncidentEffects.MoraleChange(10f), CampaignIncidentEffects.ChangeItemAmount(GetMeat, () => 3), CampaignIncidentEffects.ChangeItemAmount(GetWhaleOil, () => 3)), 0.6f), (CampaignIncidentEffects.DisorganizeParty(), 0.35f), (CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 2), 0.05f)) });
		incident11.AddOption("{=Vner50Ww}{BALLISTA_SHIP_NAME} has a ballista. None of your crew has fired the weapon with a line attached, and you can think of a half-dozen things that might go wrong, but your crew is eager to try.", new List<IncidentEffect> { CampaignIncidentEffects.Select((CampaignIncidentEffects.Group(CampaignIncidentEffects.MoraleChange(10f), CampaignIncidentEffects.ChangeItemAmount(GetMeat, () => 10), CampaignIncidentEffects.ChangeItemAmount(GetWhaleOil, () => 3)), 0.7f), (CampaignIncidentEffects.DisorganizeParty(), 0.25f), (NavalIncidentEffects.DestroyShipSiegeEngine(GetShipWithBallista), 0.05f)) }, delegate(TextObject text)
		{
			text.SetTextVariable("BALLISTA_SHIP_NAME", GetShipWithBallista().Name);
			return true;
		});
		incident11.AddOption("{=V1Yswzga}Let the whale be.", new List<IncidentEffect>());
		Incident incident12 = RegisterIncident("naval_incident_smugglers_deal", "{=3aPt1MuS}Make a deal with smugglers", "{=zZnWF44d}The night is unusually dark as you set sail from {PORT_NAME}, with clouds covering the stars. As you glide along, you come up suddenly on a coastal trader anchored in a small cove. You hear a cry, then some splashes, and you surmise that its crew are swimming ashore and scattering. They must be smugglers, and presumably they thought your vessels belonged to the lord of the town.", "LeavingPort", "HarborWaterfront", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 2 || GetWine() == null)
			{
				return false;
			}
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident12.AddOption("{=vvz2b2ZB}You stop, and find a number of casks of wine already unloaded on the beach. You bring them aboard, broaching one to drink a toast to the smugglers' carelessness and your good fortune.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.ChangeItemAmount(GetWine, () => 3),
			CampaignIncidentEffects.MoraleChange(10f),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, -300)
		});
		incident12.AddOption("{=qJiOihya}Smugglers are lampreys on the body of honest commerce, who let other men pay to maintain the ports and markets they enjoy. You sail back to harbor and inform the authorities.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, 100),
			CampaignIncidentEffects.DisorganizeParty()
		});
		incident12.AddOption("{=7HnZsfwm}Smugglers strike a blow against the grasping lords and guilds who demand shares of other men's work. You call them back so you can have a quick drink and a chat about the best ways to get in and out of harbors undetected.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, -50),
			CampaignIncidentEffects.SkillChange(NavalSkills.Shipmaster, 100f)
		});
		incident12.AddOption("{=G5wInbGx}This is none of your business, and you at any rate are pressed for time.", new List<IncidentEffect>());
		Incident incident13 = RegisterIncident("naval_incident_discordancy", "{=Wsi2xXKq}Discordancy", "{=JfQ06dT0}Usually, a bit of music on ship makes the work go faster, but lately you've found it's brought more quarrels than harmony. The {CULTURE1} say the {CULTURE2} songs are gloomy and undanceable, while {CULTURE2} says the pipes of the {CULTURE1} sound like goats bleating. The night's watch complains about the thumping and stomping when they try to sleep.", "LeavingEncounterAtSea", "LifeAtSea", CampaignTime.Days(60f), delegate(TextObject description)
		{
			(CultureObject, CultureObject) tuple = GetFirstTwoCrewCultures();
			if (MobileParty.MainParty.MemberRoster.TotalManCount < 40 || tuple.Item1 == null || tuple.Item2 == null)
			{
				return false;
			}
			description.SetTextVariable("CULTURE1", tuple.Item1.Name);
			description.SetTextVariable("CULTURE2", tuple.Item2.Name);
			return true;
		});
		incident13.AddOption("{=hiCvTpXJ}This is a chance to reinforce your authority. You are the captain, and you shall call the tune.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.SkillChange(DefaultSkills.Leadership, 50f),
			CampaignIncidentEffects.MoraleChange(10f),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 50)
		});
		incident13.AddOption("{=aMl2TFXC}If the crew cannot agree, let them work in silence until they do.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 100)
		});
		incident13.AddOption("{=TgH3avaY}The crew shall draw lots choosing which songs shall be played, if any. This won't stop the arguments, but at least you won't have to think about it.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 50),
			CampaignIncidentEffects.MoraleChange(-5f),
			CampaignIncidentEffects.WoundTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 1, specifyUnitTypeOnHint: false).WithChance(0.5f)
		});
		Incident incident14 = RegisterIncident("naval_incident_tuna_corraling", "{=T3uko0K2}Tuna corraling", "{=AAINh7bC}Cruising the Biscan coast, your lookout sights a cluster of boats in the open water. You are upon them before they can escape. They must be subjects of your enemy {KINGDOM}. The water is red with blood and their boats are piled high with tuna the size of cattle. They had been fishing in the traditional Biscan way, corraling the migrating fish into nets, then diving into their midst, knife in hand, to finish them off.", "LeavingEncounterAtSea", "HarborWaterfront", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 4)
			{
				return false;
			}
			Settlement settlement = SettlementHelper.FindNearestSettlementToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement x) => x.IsFortification && x.HasPort);
			if (settlement == null || settlement.Culture == null || settlement.Culture.StringId != "vlandia")
			{
				return false;
			}
			Kingdom kingdom = FactionHelper.GetEnemyKingdoms(Clan.PlayerClan.MapFaction).FirstOrDefault();
			if (kingdom == null)
			{
				return false;
			}
			description.SetTextVariable("KINGDOM", kingdom.Name);
			return true;
		});
		incident14.AddOption("{=FZlblVnr}As enemies their property is a lawful prize. Let them know that they must hand over their catch, or their blood with mingle with that of the fishes.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.ChangeItemAmount(GetFish, () => 3),
			CampaignIncidentEffects.GoldChange(() => 100)
		});
		incident14.AddOption("{=OU5qBygt}It takes skill to hunt tuna this way, and there is a bond between veteran sailors that eclipses the petty quarrels of kingdoms. You buy a portion of their catch, while your crew discusses with them the finer points of lashing boats and laying nets in a tossing sea.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, 100),
			CampaignIncidentEffects.GoldChange(() => -100),
			CampaignIncidentEffects.ChangeItemAmount(GetFish, () => 2),
			CampaignIncidentEffects.PartyExperienceChance(50)
		});
		Incident incident15 = RegisterIncident("naval_incident_on_deck_entertainments", "{=s8oZYz1b}On-deck entertainments", "{=n1GP9bpj}While at sea, a ship's crew has relatively few amusements. Gambling is one of them. Recently, though, you have noticed that it has become your men's obsession. In every corner of the ship you can see men who are not on duty (and sometimes a few who are) rattling dice, sometimes with a month's pay hanging on a single throw.", "LeavingEncounterAtSea", "LifeAtSea", CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.Ships.Count >= 2);
		incident15.AddOption("{=YwBp1GWx}You decree that gambling shall be banned, as it can spark fights and undermine the crew's sense of fellowship.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, -200),
			CampaignIncidentEffects.MoraleChange(-5f)
		});
		incident15.AddOption("{=aE039haV}Let the crew have their fun. A quarrel or two will keep them in good trim for the next battle.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, 100),
			CampaignIncidentEffects.MoraleChange(5f),
			CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 2).WithChance(0.2f)
		});
		incident15.AddOption("{=9Mf6ruSu}Allow your crew as little idle time as possible, having them drill at arms when not eating, sleeping or working.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.PartyExperienceChance(100),
			CampaignIncidentEffects.MoraleChange(-10f)
		});
		Incident incident16 = RegisterIncident("naval_incident_pearl_divers", "{=pauKxZ8s}Pearl divers", "{=1WxVF22O}As you sail from {PORT_NAME} into the warm coastal waters of the south, you sight a handful of boats gathered around a light-blue streak in the water that seems to be a reef. These appear to be pearl-divers, renowned in these parts for the skill, daring and lung-power that allows them to go down 10 fathoms to the riches of the oyster-beds.", "LeavingPort", "LifeAtSea", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 2)
			{
				return false;
			}
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident16.AddOption("{=BLnzsUNg}Stop and purchase some pearls as gifts for your companions and kin.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.GoldChange(() => -500),
			CampaignIncidentEffects.SkillChange(DefaultSkills.Trade, 200f)
		});
		incident16.AddOption("{=lrwwmOhW}Men such as these will make fine sailors. Offer any volunteers a bounty to join up, and encourage them to share their expertise with your crew.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.GoldChange(() => -300),
			CampaignIncidentEffects.ChangeTroopAmount(GetSeamanTroop, 2),
			CampaignIncidentEffects.PartyExperienceChance(100)
		});
		incident16.AddOption("{=ebu9flIW}You may not be at war with {KINGDOM}, but looting opportunities such as this do not come around every day. Take the pearls by force.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, -100),
			CampaignIncidentEffects.GoldChange(() => 1000),
			CampaignIncidentEffects.SkillChange(DefaultSkills.Roguery, 100f)
		}, delegate(TextObject text)
		{
			Kingdom kingdom = MobileParty.MainParty.LastVisitedSettlement.OwnerClan.Kingdom;
			if (Clan.PlayerClan.MapFaction.IsAtWarWith(kingdom))
			{
				return false;
			}
			text.SetTextVariable("KINGDOM", kingdom.Name);
			return true;
		});
		incident16.AddOption("{=3tmybEdR}Hoist sails and press on.", new List<IncidentEffect>());
		Incident incident17 = RegisterIncident("naval_incident_stowaways", "{=PKK9xviQ}Stowaways", "{=zsuPjihQ}Soon after you sail from {PORT_NAME}, you hear a commotion in your hold. You go belowdecks to find that your crew has caught a stowaway. The hapless-looking young man was found huddled beneath some extra sailcloth. He claims that he was a carpenter's apprentice, and he boarded your ship to escape an abusive master back in port.", "LeavingPort", "LifeAtSea", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 2)
			{
				return false;
			}
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident17.AddOption("{=7Z5hYBCb}A ship survives on its captain's authority, and any tolerance of wrongdoing corrodes discipline. You throw him overboard, assuming that he can probably swim to shore but, whether he can or not, he is no responsibility of yours.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, -100),
			CampaignIncidentEffects.SkillChange(DefaultSkills.Leadership, 50f)
		});
		incident17.AddOption("{=eU0HkXn1}You have mercy on the poor lad, and let him come with you providing he disembarks at the next port.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, 100),
			CampaignIncidentEffects.MoraleChange(-5f)
		});
		incident17.AddOption("{=95RMU0a9}Enlist him. He's an awkward landsman, and your crew won't like covering for his mistakes as he learns the ropes of the profession, but you're quite impressed that he managed to get on deck and into your hold without being detected.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.ChangeTroopAmount(GetTownRecruit, 1),
			CampaignIncidentEffects.MoraleChange(-5f),
			CampaignIncidentEffects.SkillChange(DefaultSkills.Roguery, 100f)
		});
		Incident incident18 = RegisterIncident("naval_incident_boatswain_bonus", "{=incident_title_captain}Captain", "{=64UotGRp}Some captains mix readily with their crew, taking an interest in the men's work and the skills needed to do it. Others maintain a lofty reserve, protecting their authority. By now you've spent a great deal of time at sea. What style of command did you take?", "LeavingEncounterAtSea", "LifeAtSea", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 4)
			{
				return false;
			}
			description.SetTextVariable("PLAYER_SHIP_NAME", GetPlayerShipName());
			return true;
		});
		incident18.AddOption("{=Pt01rNXU}You caulk and scrape planks wih the carpenters, stich canvas with the sailing master, and go aloft with the lookouts. In this way, you learn the ways of {PLAYER_SHIP_NAME} and how to fight with her.", new List<IncidentEffect> { CampaignIncidentEffects.SkillChange(NavalSkills.Boatswain, 100f) }, delegate(TextObject text)
		{
			text.SetTextVariable("PLAYER_SHIP_NAME", GetPlayerShipName());
			return true;
		});
		incident18.AddOption("{=4mbLxCmM}A captain who fraternizes undercuts a ship's discipline, becomes suspected of favoritism, and may be mocked for incompetence. You spend your days at sea in the quarterdeck with a dour expression on your face.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.RenownChange(50f),
			CampaignIncidentEffects.MoraleChange(10f)
		});
		Incident incident19 = RegisterIncident("naval_incident_brackish_water", "{=wGEyuCWJ}Brackish water", "{=gQDQoU1c}Rowers on galleys such as the {PLAYER_SHIP_NAME} drink huge quantities of water during the hotter months of the year. Unfortunately, the harbor of {PORT_NAME} failed to maintain adequate stocks for all the ships coming and going. It seems that your supplier adulterated the fresh water with seawater, as many of the barrels are brackish and undrinkable.", "LeavingPort", "LifeAtSea", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 2)
			{
				return false;
			}
			if (Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace) != TerrainType.CoastalSea)
			{
				return false;
			}
			description.SetTextVariable("PLAYER_SHIP_NAME", GetPlayerShipName());
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident19.AddOption("{=s6thJ4gQ}Dump the most brackish barrels and ration out the rest, might have men collapse of heatstroke.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, -100),
			CampaignIncidentEffects.WoundTroopsRandomlyByChance(0.05f)
		});
		incident19.AddOption("{=iayvLykH}Go slowly, making as little use of your oars as possible, until you spot a stream coming to the sea where you can refill your barrels.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, -100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 100),
			CampaignIncidentEffects.DisorganizeParty()
		});
		incident19.AddOption("{=g7KzT84w}Return to {PORT_NAME} and demand that your barrels be refilled with whatever fresh water is available. This will take some time and annoy other shipowners, who also need the limited stocks for their voyages.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, -200),
			CampaignIncidentEffects.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10, (Hero notable) => notable.IsMerchant),
			CampaignIncidentEffects.DisorganizeParty()
		}, delegate(TextObject text)
		{
			text.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		Incident incident20 = RegisterIncident("naval_incident_sailors_tales", "{=1RUiedW4}Sailors' Tales", "{=JPbmDtKw}As you prepare to sail from {PORT_NAME}, an elder sailor warns you to keep a wide berth of the headland. Beneath it, he says, there lies a huge lodestone that will suck all the nails out of the ship. It's true that most local shipping does avoid the headland, but you suspect that has more to do with the local winds than any deadly stone.", "LeavingPort", "NauticalHazard", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 2)
			{
				return false;
			}
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident20.AddOption("{=jgYDPSyd}The sailor knows his duties well, if not the science of navigation. He has been a mentor to the younger crew, and you have no wish to undermine his authority. Nod sagely at the advice, and take the route he suggests.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, -100),
			CampaignIncidentEffects.MoraleChange(5f)
		});
		incident20.AddOption("{=QC6nHbZk}Now is a chance to demonstrate your superior knowledge of the seas. Steer close to the headland, having your men ready to stand by the oars if the wind fails.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, -200),
			CampaignIncidentEffects.RenownChange(50f)
		});
		Incident incident21 = RegisterIncident("naval_incident_careless_fire", "{=F1Q5iy8t}Crew members careless with fire", "{=EDJXWwuJ}Your crew is taught to be extremely wary of fire on board the ship, and to warm themselves with clay boxes of embers and coals rather than open flames. On this voyage, however, the cold winds of the north have chilled your crew to their bones. You've seen men carrying coals wrapped in cloth, sometimes even taking them aloft into the rigging.", "LeavingEncounterAtSea", "LifeAtSea", CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.IsActive && MobileParty.MainParty.Ships.Count > 0 && MobileParty.MainParty.GetPosition2D.y > 600f);
		incident21.AddOption("{=JTOtGN4M}In a wooden ship, rigged with hemp, where everything is caulked with pine pitch, it's only a matter of time before a fire starts. Flog the next man you see violating the ship's rules.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, -100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100)
		});
		incident21.AddOption("{=bNNcdgyO}Cold can kill too, and you trust that the veteran members of your crew will impress upon the rest how cautious they must be when carrying coals. Turn a blind eye to the violations.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 100),
			CampaignIncidentEffects.Group(CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 2), CampaignIncidentEffects.WoundTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 5, specifyUnitTypeOnHint: false), NavalIncidentEffects.FlagShipHitPointsChange(-0.05f)).WithChance(0.15f)
		});
		incident21.AddOption("{=bRJUJjlB}Explain to your crew that you understand their plight, but that they must stick to regulations. However, you will reduce the number of men who need to be on deck at any given time.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, 100),
			CampaignIncidentEffects.DisorganizeParty(),
			CampaignIncidentEffects.Group(CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 2), CampaignIncidentEffects.WoundTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 5, specifyUnitTypeOnHint: false), NavalIncidentEffects.FlagShipHitPointsChange(-0.05f)).WithChance(0.05f)
		});
		Incident incident22 = RegisterIncident("naval_incident_navigational_debate", "{=OpVBl1ZH}Navigational debate", "{=acJzcsD0}Among your other loot from your recent battle, you find a curious brass instrument about the size of a man's hand, with interlocking dials, marked with what appear to be ancient Kannic runes. You've heard of these devices, which match the night sky, the time of day and the time of year to help sailors know where they are.", "LeavingNavalBattle", "HarborWaterfront", CampaignTime.Days(60f), (TextObject description) => Clan.PlayerClan.Tier >= 4);
		incident22.AddOption("{=ZJfJYchv}You decide to work out how to use this device to track how far north and south you travel, even though it will take some time and distract you from your other duties as captain.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.SkillChange(NavalSkills.Shipmaster, 100f),
			CampaignIncidentEffects.MoraleChange(-10f),
			CampaignIncidentEffects.DisorganizeParty()
		});
		incident22.AddOption("{=WocxoQ4b}It's already easy enough for you, as an experienced mariner, to use coastal and offshore landmarks, dead reckoning and the sun to know approximately where you are. Sell the device the next time you put into a port.", new List<IncidentEffect> { CampaignIncidentEffects.GoldChange(() => 300) });
		incident22.AddOption("{=z9DetbOl}Pass the device off to a credulous merchant as an enchanted wayfinder that always points a ship home, and name a price to match the tale.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, -200),
			CampaignIncidentEffects.GoldChange(() => 500)
		});
		Incident incident23 = RegisterIncident("naval_incident_sea_caves", "{=3xR08ros}Sea-caves", "{=JcybaxmA}Ships sailing from {PORT_NAME} typically pass by several sea-caves in the cliffs. During high tide they are covered, but at low tide the waters recede, and often valuable items that have fallen overboard from passing vessels can be found. The caves are typically picked clean by fishermen as soon as the tides recede, but today they seem not to have come.", "LeavingPort", "HarborWaterfront", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 2)
			{
				return false;
			}
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident23.AddOption("{=FJZkM74b}Send swimmers over to look for the choicest items, but leave before any locals arrive to avoid quarrels.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.GoldChange(() => 300),
			CampaignIncidentEffects.MoraleChange(10f),
			CampaignIncidentEffects.DisorganizeParty()
		});
		incident23.AddOption("{=d639UgkY}Anchor near the caves and take the time to pick them clean, keeping away local fishermen as they arrive.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.GoldChange(() => 500),
			CampaignIncidentEffects.MoraleChange(15f),
			CampaignIncidentEffects.DisorganizeParty(),
			CampaignIncidentEffects.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10)
		});
		incident23.AddOption("{=gfQ6IrAe}You aren't going to waste time, or risk violating anyone's lawful claims, over a bit of soggy detritus.", new List<IncidentEffect> { CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, 100) });
		Incident incident24 = RegisterIncident("naval_incident_sudden_squall", "{=s3MlDGIQ}Sudden Squall", "{=4y1HdsPG}The skies are clear, and you are attending to matters belowdecks when you hear frantic shouts above, and the ship suddenly heels. The {PLAYER_SHIP_NAME} was hit by a brief but violent white squall, so called because it was unaccompanied by black warning clouds. These are rare but dangerous, as they catch vessels unprepared. The ship took little damage, but two men who were in the rigging are missing.", "LeavingEncounterAtSea", "NauticalHazard", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (!MobileParty.MainParty.IsActive || MobileParty.MainParty.Ships.Count == 0 || MobileParty.MainParty.GetPosition2D.y > 600f)
			{
				return false;
			}
			description.SetTextVariable("PLAYER_SHIP_NAME", GetPlayerShipName());
			return true;
		});
		incident24.AddOption("{=bydfK3OD}The men may be alive in the water but you will not risk another such squall while searching for them. Even white squalls can be spotted in time if lookouts are sufficiently alert, and these two clearly were not.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, -200),
			CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 2)
		});
		incident24.AddOption("{=7T6Gb3rX}You will search, but incur no additional risks. You tie everything down and cut sail before doubling back to look for your men.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.DisorganizeParty(),
			CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 2).WithChance(0.5f)
		});
		incident24.AddOption("{=LV8euAUz}Every minute that slips away reduces your men's chance of rescue. You turn about immediately.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, 200),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, -200),
			CampaignIncidentEffects.Group(CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 4), CampaignIncidentEffects.MoraleChange(-10f), NavalIncidentEffects.FlagShipHitPointsChange(-0.05f)).WithChance(0.25f)
		});
		Incident incident25 = RegisterIncident("naval_incident_rival_fishermen", "{=080lP3lg}Rival fishermen", "{=bCP2By6G}As you sail from {PORT_NAME} you are approached by a delegation of fishermen. Recently, boats visiting the rich banks a few leagues offshore have been harassed by villagers from {VILLAGE_NAME}, who say these are their ancestral waters. The men from {PORT_NAME} concede that {VILLAGE_NAME} was there first, but say it gives them no special claim. They offer you some denars for your help.", "LeavingPort", "HarborWaterfront", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 4)
			{
				return false;
			}
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			description.SetTextVariable("VILLAGE_NAME", GetRivalVillage().Name);
			return true;
		});
		incident25.AddOption("{=oXujosgB}There are plenty of fish in the sea, for those willing to work. Let each take what they can. You accept the money, and escort the town's fishermen to their destination to let the village know they have powerful friends.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.GoldChange(() => 500),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, -200),
			CampaignIncidentEffects.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 5),
			CampaignIncidentEffects.SettlementRelationChange(GetRivalVillage, -10)
		});
		incident25.AddOption("{=ZP8mTOxN}Fishing villages such as {VILLAGE_NAME} live and die by the size of the catch, while urban mariners have many options. The villagers were the first to fish the banks, and you ask that the townspeople respect that.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, 100),
			CampaignIncidentEffects.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5),
			CampaignIncidentEffects.SettlementRelationChange(GetRivalVillage, 10)
		}, delegate(TextObject text)
		{
			text.SetTextVariable("VILLAGE_NAME", GetRivalVillage().Name);
			return true;
		});
		incident25.AddOption("{=kpJlqbE8}This is a matter for the local magistrates. No doubt there are a half a dozen court cases and decrees mouldering in local archives that have already resolved this.", new List<IncidentEffect>());
		Incident incident26 = RegisterIncident("naval_incident_gift_of_the_sea", "{=a7F2Ypyn}Gift of the sea", "{=m8WtOj6n}When your lookouts first spotted it, they thought it was a great sea-serpent or whale. A close approach however revealed it to be a majestic cedar log from some coastal forest, perhaps uprooted in a landslide brought by a storm. Such logs are quite valuable, though hauling this one onto your ship will be a challenge.", "LeavingEncounterAtSea", "NauticalHazard", CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.IsActive && MobileParty.MainParty.GetPosition2D.y > 500f && MobileParty.MainParty.Ships.Any((Ship x) => x.ShipHull.Type == ShipHull.ShipType.Heavy));
		incident26.AddOption("{=n38WRgmp}One of your northerners is a fine carver. Try to get the log onto your ship, so he can craft a figurehead in the form of a deer, inspiring your crew to row more quickly.", new List<IncidentEffect> { CampaignIncidentEffects.Select((NavalIncidentEffects.UnlockFigurehead(DefaultFigureheads.Deer), 0.8f), (NavalIncidentEffects.FlagShipHitPointsChange(-0.05f), 0.2f)) });
		incident26.AddOption("{=P9zsaX5V}A carpenter from one of the southern ports offers to shape the log into a figurehead of a hawk, which he says will guide your arrows in flight. Try to haul up the log.", new List<IncidentEffect> { CampaignIncidentEffects.Select((NavalIncidentEffects.UnlockFigurehead(DefaultFigureheads.Hawk), 0.8f), (NavalIncidentEffects.FlagShipHitPointsChange(-0.05f), 0.2f)) });
		incident26.AddOption("{=dkIL68kF}Haul the log on deck, but cut it up for timber to sell in a nearby port.", new List<IncidentEffect> { CampaignIncidentEffects.Select((CampaignIncidentEffects.ChangeItemAmount(() => DefaultItems.HardWood, () => 5), 0.8f), (NavalIncidentEffects.FlagShipHitPointsChange(-0.05f), 0.2f)) });
		incident26.AddOption("{=pB6h40Oi}It will be hard to get such a heavy log on board in a tossing sea, and if it slips it could easily injury or damage.", new List<IncidentEffect>());
		Incident incident27 = RegisterIncident("naval_incident_medical_stores", "{=CQRxiugb}Medical stores", "{=knpbLM3v}Your surgeon {SURGEON.NAME} gravely informs you that {?SURGEON.GENDER}she{?}he{\\?} suspects a crew member on the {PLAYER_SHIP_NAME} of raiding the medical stores and stealing some of the fortified wine used to clean wounds and the opium to dull pain. You suspect you could find the culprit, although the crew would resent an intrusive investigation.", "LeavingEncounterAtSea", "LifeAtSea", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.EffectiveSurgeon == null || MobileParty.MainParty.EffectiveSurgeon == Hero.MainHero)
			{
				return false;
			}
			description.SetCharacterProperties("SURGEON", MobileParty.MainParty.EffectiveSurgeon.CharacterObject);
			description.SetTextVariable("PLAYER_SHIP_NAME", GetPlayerShipName());
			return true;
		});
		incident27.AddOption("{=mf1zkkXz}Stop at nothing to find and punish the thief. Order the crew to assemble while you go through their belongings, then give the culprit 50 lashes.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, 100),
			CampaignIncidentEffects.MoraleChange(-10f),
			CampaignIncidentEffects.WoundTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 1, specifyUnitTypeOnHint: false)
		});
		incident27.AddOption("{=X1OTCE2D}Watch carefully for any crew members who appear particularly drunk or listless, then interrogate their mates. Dock their pay and give them a week of extra duties.", new List<IncidentEffect> { CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 100) });
		incident27.AddOption("{=806dkabJ}Announce that there has been a theft and that it will be investigated, giving the thief ample time to throw the incriminating goods overboard.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, -50)
		});
		Incident incident28 = RegisterIncident("naval_incident_skilled_prisoners", "{=ATBmBGku}Skilled prisoners", "{=oiX7sKjB}Among your prisoners from the recent battle are several from {PORT_NAME}, known for its nautical tradition. They seem skilled at stitching sails and splicing rope, and are willing to enlist. To make the most out of their skills, you would parcel them out to the different watches and designate them as master seamen, with authority to supervise and teach the others.", "LeavingNavalBattle", "LifeAtSea", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 4)
			{
				return false;
			}
			Settlement settlement = SettlementHelper.FindNearestSettlementToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement x) => x.IsFortification && x.HasPort);
			description.SetTextVariable("PORT_NAME", settlement.Name);
			return true;
		});
		incident28.AddOption("{=f7z2QdCq}Enlist the prisoners as master seamen. Many in your crew will resent taking directions and criticisms from men who they defeated in battle, but they will have to swallow their pride.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.ChangeTroopAmount(GetMasterSeamanTroop, 2),
			CampaignIncidentEffects.PartyExperienceChance(200),
			CampaignIncidentEffects.MoraleChange(-10f),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, -100)
		});
		incident28.AddOption("{=WbbZRJAf}Enlist the {PORT_NAME} sailors, but make it clear that they start from the bottom of the {PLAYER_SHIP_NAME}'s hierarchy and will need to work their way.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.ChangeTroopAmount(GetMasterSeamanTroop, 2),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 100)
		}, delegate(TextObject text)
		{
			Settlement settlement = SettlementHelper.FindNearestSettlementToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement x) => x.IsFortification && x.HasPort);
			text.SetTextVariable("PORT_NAME", settlement.Name);
			text.SetTextVariable("PLAYER_SHIP_NAME", GetPlayerShipName());
			return true;
		});
		incident28.AddOption("{=B8Q4K6Gb}Keep the prisoners for the ransom broker.", new List<IncidentEffect> { CampaignIncidentEffects.ChangePrisonerAmount(GetMasterSeamanTroop, 2) });
		Incident incident29 = RegisterIncident("naval_incident_shipwreck", "{=rabajOaL}Shipwreck", "{=E5QVubY7}Sailing out of {PORT_NAME}, you pass a beach that is strewn with wreckage. While in port you had heard of a merchant ship that foundered in a storm several days ago and capsized: this must be its remains. No doubt the local fishermen have already salvaged most of the cargo, but the ship's frame itself, though broken up, may still be of value.", "LeavingPort", "HarborWaterfront", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 2)
			{
				return false;
			}
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident29.AddOption("{=Wb86xDaN}Have your carpenters go ashore to pick out the best timbers from the wreck..", new List<IncidentEffect>
		{
			CampaignIncidentEffects.ChangeItemAmount(() => DefaultItems.HardWood, () => 5),
			CampaignIncidentEffects.DisorganizeParty()
		});
		incident29.AddOption("{=GGexIfNP}Post your men around the wreck and charge the local fishermen a 'salvage fee' to pick through what remains, claiming the wreck's owner named you its warden.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, -200),
			CampaignIncidentEffects.GoldChange(() => 200),
			CampaignIncidentEffects.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5)
		});
		incident29.AddOption("{=bbYpAcfR}A handful of planks and broken masts is not worth the trouble of landing men in the surf. Sail on.", new List<IncidentEffect>());
		Incident incident30 = RegisterIncident("naval_incident_rotted_timbers", "{=pSCCRk52}Rotted timbers", "{=W6oe5bKK}Marine life thrives in the warm waters of the south, some of it unhealthy for wooden vessels. Your carpenters inform you that some of the planks of {PLAYER_SHIP_NAME} have been eaten by shipworm. Your carpenters think they can fashion a patch while at sea, but they caution that, should the timbers give out suddenly, {PLAYER_SHIP_NAME} could founder and sink.", "LeavingEncounterAtSea", "NauticalHazard", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (!MobileParty.MainParty.IsActive || MobileParty.MainParty.Ships.Count < 3)
			{
				return false;
			}
			description.SetTextVariable("PLAYER_SHIP_NAME", GetSmallestShip().Name);
			return true;
		});
		incident30.AddOption("{=IXqPnUiU}Help your carpenters scour the inner hull for any sign of rot, patching any threatened areas with planks and tar-soaked sailcloth until you have time for a more thorough overhaul in port.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, 100),
			CampaignIncidentEffects.SkillChange(NavalSkills.Boatswain, 100f),
			NavalIncidentEffects.DestroyShip(GetSmallestShip).WithChance(0.05f)
		});
		incident30.AddOption("{=8CNadak3}Turn back immediately, and pay the shipwrights of {PORT_NAME} to replace the damaged timbers.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.GoldChange(() => -2000),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, -100)
		}, delegate(TextObject text)
		{
			Settlement lastVisitedSettlement = MobileParty.MainParty.LastVisitedSettlement;
			if (lastVisitedSettlement != null)
			{
				text.SetTextVariable("PORT_NAME", lastVisitedSettlement.Name);
			}
			return true;
		});
		Incident incident31 = RegisterIncident("naval_incident_off_key_shanties", "{=0NCSxWq9}Off-key shanties", "{=SPElm4bz}Your men chant songs to keep time when rowing or hauling sails. Their most recent favorite features the girls from {RANDOM_PORT_2}, who (according to the song) all smell like fish, comb their hair with kipper backbones, and fail to properly guard their virtue when tempted by lusty sailors from {RANDOM_PORT_1}. You have men from both ports in your crew.", "LeavingEncounterAtSea", "LifeAtSea", CampaignTime.Days(60f), delegate(TextObject description)
		{
			List<CultureObject> list = GetTwoCrewCultures();
			if (MobileParty.MainParty.Ships.Count < 2 || list.Count < 2)
			{
				return false;
			}
			description.SetTextVariable("RANDOM_PORT_2", GetNearestPortNameOfCulture(list[1]));
			description.SetTextVariable("RANDOM_PORT_1", GetNearestPortNameOfCulture(list[0]));
			return true;
		});
		incident31.AddOption("{=y3wA5Xvd}Ribald sea shanties should be beneath a captain's notice, if {?PLAYER.GENDER}she{?}he{\\?} wishes to maintain {?PLAYER.GENDER}her{?}his{\\?} dignity and authority. Let the men of {PORT_NAME_1} singly loudly to drown the others out, if it bothers them.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, 100),
			CampaignIncidentEffects.WoundTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 4, specifyUnitTypeOnHint: false).WithChance(0.5f)
		}, delegate(TextObject text)
		{
			List<CultureObject> list = GetTwoCrewCultures();
			text.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
			text.SetTextVariable("PORT_NAME_1", GetNearestPortNameOfCulture(list[0]));
			return true;
		});
		incident31.AddOption("{=oZ5IfQrQ}You've seen crew members from {RANDOM_PORT_1} looking at those from {RANDOM_PORT_2} with hate their eyes, and you don't want a fight. Silence the men if they start singing that song.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, -100),
			CampaignIncidentEffects.MoraleChange(-5f)
		}, delegate(TextObject text)
		{
			List<CultureObject> list = GetTwoCrewCultures();
			text.SetTextVariable("RANDOM_PORT_1", GetNearestPortNameOfCulture(list[0]));
			text.SetTextVariable("RANDOM_PORT_2", GetNearestPortNameOfCulture(list[1]));
			return true;
		});
		incident31.AddOption("{=1bEpEosd}Have one of your more barrel-chested crew members lead the rest in a pious hymn to the North Star, who guides ships and keeps watch over sailors.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, 50),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 50),
			CampaignIncidentEffects.MoraleChange(-10f)
		});
		Incident incident32 = RegisterIncident("naval_incident_hellship_fugitives", "{=7VGo31vR}Hellship fugitives", "{=lnnGoAAl}You are about to set sail when two of your crew approach, along with three dishevelled {TROOP.NAME}. They had met in a tavern, shortly after the three had signed on to sail with the {OTHER_SHIP_NAME} and were busy drinking away their bonus. Your men told the {TROOP.NAME} that the {OTHER_SHIP_NAME} was hell afloat, and the captain flogged his crew to break them to his will. They ask you to rescue their new friends by enlisting them.", "LeavingPort", "HarborWaterfront", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.MemberRoster.TotalManCount < 40 || GetMarinerInfantry() == null)
			{
				return false;
			}
			description.SetCharacterProperties("TROOP", GetMarinerInfantry());
			description.SetTextVariable("OTHER_SHIP_NAME", MobileParty.MainParty.Party.Ships.GetRandomElement().Name);
			return true;
		});
		incident32.AddOption("{=BrcJ3g8C}If a sailor takes a captain's coin, he is bound to sail with him for at least one voyage. Refuse, and scold your men for violating the custom of the sea.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, 200),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 100)
		});
		incident32.AddOption("{=X85Q6g7n}You are touched by your men's faith in you. Bring the {TROOP.NAME} aboard, but tell them to keep a low profile to aboid being spotted.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, -100),
			CampaignIncidentEffects.ChangeTroopAmount(GetMarinerInfantry, 3)
		}, delegate(TextObject text)
		{
			text.SetCharacterProperties("TROOP", GetMarinerInfantry());
			return true;
		});
		incident32.AddOption("{=tb52xAuD}Tell your men that they'll need to take up a collection to repay the signing bonus, but assist them with a contribution of your own.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, 100),
			CampaignIncidentEffects.GoldChange(() => -300),
			CampaignIncidentEffects.ChangeTroopAmount(GetMarinerInfantry, 3)
		});
		Incident incident33 = RegisterIncident("naval_incident_glimmer_in_the_deep", "{=W4nn08TR}A Glimmer in the Deep", "{=RTjChyUA}One of your {TROOP.NAME} claims {?TROOP.GENDER}she{?}he{\\?} has spotted the glint of gold in the unusually clear, cold waters of this part of the river. {?TROOP.GENDER}She{?}He{\\?} begs permission to dive in and try to find when {?TROOP.GENDER}she{?}he{\\?} believes to be a scattering of coins. You know {?TROOP.GENDER}she{?}he{\\?} is a strong swimmer, but even the best may be caught in a swift and deadly undercurrent.", "LeavingEncounterAtSea", "River", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 2 || !IsOnRiver())
			{
				return false;
			}
			description.SetCharacterProperties("TROOP", GetSwimmerTroop());
			return true;
		});
		incident33.AddOption("{=ZAFNy4b7}You've heard that the people of these lands frequently make offerings to the waters. Give {?TROOP.GENDER}her{?}him{\\?} permission to dive.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 100),
			CampaignIncidentEffects.Select((CampaignIncidentEffects.Group(CampaignIncidentEffects.MoraleChange(10f), CampaignIncidentEffects.GoldChange(() => 100)), 0.5f), (CampaignIncidentEffects.Group(CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 1), CampaignIncidentEffects.MoraleChange(-10f), CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, -200)), 0.2f), (CampaignIncidentEffects.Custom(null, () => new List<TextObject>(), (IncidentEffect effect) => new IncidentHint(new TextObject("{=lobJVVWT}Nothing happens"))), 0.3f))
		}, delegate(TextObject text)
		{
			text.SetCharacterProperties("TROOP", GetSwimmerTroop());
			return true;
		});
		incident33.AddOption("{=HuaZhoya}Praise {?TROOP.GENDER}her{?}his{\\?} alertness but refuse permission. You won't allow such a promising young {?TROOP.GENDER}woman{?}man{\\?} to risk {?TROOP.GENDER}her{?}his{\\?} life like this.", new List<IncidentEffect> { CampaignIncidentEffects.TraitChange(DefaultTraits.Mercy, 100) }, delegate(TextObject text)
		{
			text.SetCharacterProperties("TROOP", GetSwimmerTroop());
			return true;
		});
		incident33.AddOption("{=lj34XEab}Tell him not to be a fool: everyone knows this is how the water-goblins lure in their prey.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, -200)
		});
		Incident incident34 = RegisterIncident("naval_incident_river_sirens", "{=5mCeOw3Y}River sirens", "{=F8bkJVrB}As you round a bend in the river, you see several young fisherwomen sunning themselves on a rock. They call out to your men to come ashore, to sample the catch and a bit of home-brewed wine. One of your older hands looks at the crew's eager faces and scoffs, \"There is no easier prey than a mariner long at sea, with a heart full of yearning and pockets full of silver.\"", "LeavingEncounterAtSea", "River", CampaignTime.Days(60f), (TextObject description) => Clan.PlayerClan.Tier >= 2 && IsOnRiver());
		incident34.AddOption("{=VCjT88qp}Tell your men to keep their wits about them, but allow them to go ashore to barter for wine and anything else that may be on offer.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, -100),
			CampaignIncidentEffects.MoraleChange(10f),
			CampaignIncidentEffects.DisorganizeParty(),
			CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 1).WithChance(0.05f)
		});
		incident34.AddOption("{=0mQZGw2C}Refuse. At best you'll lose time. At worst some of your men might be lured off into the nearby woods to be robbed or murdered.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.MoraleChange(-10f)
		});
		Incident incident35 = RegisterIncident("naval_incident_dangerous_marshes", "{=qOrAnIpR}Dangerous marshes", "{=U8aK2KfI}Shortly after putting out from {PORT_NAME}, you pass a stretch of marshes. Sometimes the river here is so quiet that you can hear the wind rustling the reeds, but today there is a great cacaphony of bird calls. Every so often a huge mass of storks takes flight. It must be nesting season, which means that your crew could easily gather eggs if you stopped. Marshes, however, can be dangerous places.", "LeavingPort", "River", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.GetPosition2D.y > 400f || !IsLastVisitedPortOnRiver())
			{
				return false;
			}
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident35.AddOption("{=2a3laFpM}You allow your men to wade into maze of reedbeds, directing them to stick together lest anyone be bit by a serpent, or stalked by a leopard, or stuck fast in the ooze, his cries drowned out by the sqawking birds.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.ChangeItemAmount(() => DefaultItems.Meat, () => 2),
			CampaignIncidentEffects.MoraleChange(5f),
			CampaignIncidentEffects.SkillChange(DefaultSkills.Scouting, 50f),
			CampaignIncidentEffects.Group(CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 2), CampaignIncidentEffects.MoraleChange(-10f)).WithChance(0.1f)
		});
		incident35.AddOption("{=bnMUdgXD}Eggs are all very well but the real wealth of the marshes are their leeches. You wait for the men to return, then harvest the squiggling beasts from their legs, selling them to a doctor in town who will use them for bloodletting.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.GoldChange(() => 200),
			CampaignIncidentEffects.SkillChange(DefaultSkills.Medicine, 50f),
			CampaignIncidentEffects.MoraleChange(-15f),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, -200)
		});
		incident35.AddOption("{=98bk2LT3}You are pressed for time, and sail on.", new List<IncidentEffect>());
		Incident incident36 = RegisterIncident("naval_incident_deadly_shallows", "{=UVy7IO08}Deadly shallows", "{=3n55uJs9}Not far from {PORT_NAME} a huge black basalt outcrop looms over the river. It has an ominous reputation. Legend has it that beneath dwells a colony of wicked gnomes, who tear the bottom out of passing ships. Less fanciful sailors warn of of underwater rocks that can be quite dangerous when water levels are low. Today, you are in the midst of a dry spell, and tips of stone poke above the surface while whirlpools form between them.", "LeavingPort", "River", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (Clan.PlayerClan.Tier < 2 || !IsLastVisitedPortOnRiver())
			{
				return false;
			}
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident36.AddOption("{=4w3m2AFG}You steer towards the opposite bank but proceed at normal speed, as it is good for your men to learn to put their faith in your judgment in dangerous situations.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, 200),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, -100),
			CampaignIncidentEffects.MoraleChange(10f),
			CampaignIncidentEffects.Group(NavalIncidentEffects.FlagShipHitPointsChange(-0.05f), CampaignIncidentEffects.MoraleChange(-20f)).WithChance(0.15f)
		});
		incident36.AddOption("{=Qpo5cIfN}You give the great rock a wide berth, creeping forward slowly and repeatedly casting the lead to gauge depth.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, -100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100)
		});
		Incident incident37 = RegisterIncident("naval_incident_treacherous_passages", "{=Gb5UGBO8}Treacherous Passages", "{=f9jqJ5lA}The river near {PORT_NAME} is usually safe going, but a recent storm has caused landslides and sent boulders cascading into the waters. A barge laden with {TRADE_GOOD} apparently blundered into them and is now stuck fast. Its crew abandoned ship but a good portion of its cargo is still secured and dry, at least until the barge breaks up.", "LeavingPort", "River", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (!IsLastVisitedPortOnRiver())
			{
				return false;
			}
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			description.SetTextVariable("TRADE_GOOD", GetTradeGood().Name);
			return true;
		});
		incident37.AddOption("{=C0cNERLY}You send swimmers to salvage what they can, warning them not to get dashed against a rock by the current, or get their foot wedged between two boulders and drown.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, 50),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, -100),
			CampaignIncidentEffects.Select(CampaignIncidentEffects.ChangeItemAmount(GetTradeGood, () => 5), CampaignIncidentEffects.Group(CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 1), CampaignIncidentEffects.MoraleChange(-10f)), 0.75f)
		});
		incident37.AddOption("{=AXsjXc1d}Get as close as you can, then throw out grappling hooks to drag the barge towards you before starting to salvage the cargo. If the barge starts to break up, or you sense unseen hazards beneath the surface, then abandon the attempt.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, -100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.DisorganizeParty(),
			CampaignIncidentEffects.ChangeItemAmount(GetTradeGood, () => 5).WithChance(0.6f)
		});
		incident37.AddOption("{=VBba1TBE}Continue your voyage on the river.", new List<IncidentEffect>());
		Incident incident38 = RegisterIncident("naval_incident_river_of_plenty", "{=414cLaaj}River of Plenty", "{=OxX2dxjD}At this time of year, the river outside {PORT_NAME} is thick with salmon and eel. Wherever a small stream enters the main flow, villagers are setting up nets or perching on rocks with two-pronged fishing spears. Your men would appreciate the chance to stop and spear themselves some fresh fish, though it would no doubt trespass upon some local claims.", "LeavingPort", "River", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (MobileParty.MainParty.LastVisitedSettlement.Town == null || MobileParty.MainParty.LastVisitedSettlement.Town.Villages.Count < 2)
			{
				return false;
			}
			if (Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.LastVisitedSettlement.PortPosition.Face) != TerrainType.River)
			{
				return false;
			}
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident38.AddOption("{=FbVPCncO}Land a party to spear fish, even if it leads to quarrels with the locals.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.ChangeItemAmount(GetFish2, () => 5),
			CampaignIncidentEffects.MoraleChange(10f),
			CampaignIncidentEffects.DisorganizeParty(),
			CampaignIncidentEffects.SettlementRelationChange(() => GetVillages().Item1.Settlement, -10),
			CampaignIncidentEffects.SettlementRelationChange(() => GetVillages().Item2.Settlement, -10)
		});
		incident38.AddOption("{=VBba1TBE}Continue your voyage on the river.", new List<IncidentEffect>());
		Incident incident39 = RegisterIncident("naval_incident_pirates_life", "{=F91GOZbI}A Pirate's Life for Us", "{=zQnj1U4a}Several of your men crewed pirate vessels. You did not know this when you signed them on, though you had your suspicions. However, they are quite proud of it, and often boast of their criminal exploits. Apparently, when ashore in {PORT_NAME}, they taunted the local tavern-goers about the fine sport they had, hunting the port's merchant vessels.", "LeavingPort", "HarborWaterfront", CampaignTime.Days(60f), delegate(TextObject description)
		{
			if (!MobileParty.MainParty.Ships.Any((Ship x) => x.ShipHull.Type == ShipHull.ShipType.Heavy))
			{
				return false;
			}
			if (MobileParty.MainParty.MemberRoster.TotalManCount < 10)
			{
				return false;
			}
			description.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident39.AddOption("{=Qw9a6GyW}Saints rarely serve on warships, and bit of swagger and bombast puts the rest of your men in a fine spirit for battle.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, 100),
			CampaignIncidentEffects.MoraleChange(10f),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, -200),
			CampaignIncidentEffects.CrimeRatingChange(() => MobileParty.MainParty.LastVisitedSettlement.MapFaction, 5f)
		}, delegate(TextObject text)
		{
			text.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		incident39.AddOption("{=eeiZAKc4}Instruct the men to be more discrete the next time they go ashore.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Calculating, 100),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Valor, -100)
		});
		incident39.AddOption("{=Ypp9b2bQ}Clap the men in irons, and hand them over to the authorites in {PORT_NAME} for judgement.", new List<IncidentEffect>
		{
			CampaignIncidentEffects.TraitChange(DefaultTraits.Honor, 200),
			CampaignIncidentEffects.TraitChange(DefaultTraits.Generosity, -100),
			CampaignIncidentEffects.MoraleChange(-5f),
			CampaignIncidentEffects.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 5),
			CampaignIncidentEffects.KillTroopsRandomly((TroopRosterElement element) => !element.Character.IsHero, () => 3, useLostText: true)
		}, delegate(TextObject text)
		{
			text.SetTextVariable("PORT_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
			return true;
		});
		static CharacterObject GetCrewMember()
		{
			return IncidentHelper.GetSeededRandomElement(MobileParty.MainParty.MemberRoster.GetTroopRoster(), Campaign.Current.IncidentManager.ActiveIncidentSeed).Character;
		}
		static (CultureObject, CultureObject) GetFirstTwoCrewCultures()
		{
			CultureObject cultureObject = null;
			CultureObject item = null;
			foreach (TroopRosterElement item2 in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (!item2.Character.IsHero)
				{
					if (cultureObject == null)
					{
						cultureObject = item2.Character.Culture;
					}
					else if (item2.Character.Culture != cultureObject)
					{
						item = item2.Character.Culture;
						break;
					}
				}
			}
			return (cultureObject, item);
		}
		static ItemObject GetFish()
		{
			return Game.Current.ObjectManager.GetObject<ItemObject>("fish");
		}
		static ItemObject GetFish2()
		{
			return Game.Current.ObjectManager.GetObject<ItemObject>("fish");
		}
		static CharacterObject GetInjuredCrew()
		{
			List<TroopRosterElement> list = new List<TroopRosterElement>();
			foreach (TroopRosterElement item3 in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (!item3.Character.IsHero)
				{
					list.Add(item3);
				}
			}
			if (list.Count == 0)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement(list, Campaign.Current.IncidentManager.ActiveIncidentSeed).Character;
		}
		static CharacterObject GetMarinerInfantry()
		{
			List<CharacterObject> list = (from x in CharacterHelper.GetTroopTree(MobileParty.MainParty.LastVisitedSettlement.Culture.BasicTroop, 1f, 5f)
				where x.IsMariner
				select x).ToList();
			if (list.Count == 0)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement(list, Campaign.Current.IncidentManager.ActiveIncidentSeed);
		}
		static CharacterObject GetMasterSeamanTroop()
		{
			CultureObject culture = SettlementHelper.FindNearestSettlementToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement x) => x.IsFortification && x.HasPort).Culture;
			return CharacterHelper.GetTroopTree(culture.BasicTroop, 4f, 4f).FirstOrDefault((CharacterObject x) => x.IsMariner) ?? culture.EliteBasicTroop;
		}
		static ItemObject GetMeat()
		{
			return DefaultItems.Meat;
		}
		static TextObject GetNearestPortNameOfCulture(CultureObject culture)
		{
			return SettlementHelper.FindNearestSettlementToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement x) => x.IsTown && x.HasPort && x.Culture == culture)?.Name ?? culture.Name;
		}
		static Settlement GetRivalVillage()
		{
			Town town = MobileParty.MainParty.LastVisitedSettlement.Town;
			return town.Villages[MobileParty.MainParty.LastVisitedSettlement.RandomIntWithSeed((uint)CampaignTime.Now.ToDays, 0, town.Villages.Count)].Settlement;
		}
		static CharacterObject GetSeamanTroop()
		{
			CultureObject culture = MobileParty.MainParty.LastVisitedSettlement.Culture;
			return CharacterHelper.GetTroopTree(culture.BasicTroop, 3f, 3f).FirstOrDefault((CharacterObject x) => x.IsMariner) ?? culture.BasicTroop;
		}
		static CharacterObject GetSeasickLandlubber()
		{
			List<TroopRosterElement> list = new List<TroopRosterElement>();
			foreach (TroopRosterElement item4 in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (!item4.Character.IsHero && !item4.Character.IsMariner)
				{
					list.Add(item4);
				}
			}
			if (list.Count == 0)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement(list, Campaign.Current.IncidentManager.ActiveIncidentSeed).Character;
		}
		static Ship GetShipWithBallista()
		{
			foreach (Ship ship in MobileParty.MainParty.Ships)
			{
				if (ship.GetSiegeEngines().Any((SiegeEngineType x) => x.IsRanged))
				{
					return ship;
				}
			}
			return null;
		}
		static Ship GetSmallestShip()
		{
			ShipHull.ShipType[] array = new ShipHull.ShipType[3]
			{
				ShipHull.ShipType.Light,
				ShipHull.ShipType.Medium,
				ShipHull.ShipType.Heavy
			};
			foreach (ShipHull.ShipType type in array)
			{
				List<Ship> list = MobileParty.MainParty.Ships.Where((Ship x) => x.ShipHull.Type == type).ToList();
				if (list.Count > 0)
				{
					return list.MinBy((Ship x) => x.SeaWorthiness);
				}
			}
			return MobileParty.MainParty.Ships.FirstOrDefault();
		}
		static CharacterObject GetSwimmerTroop()
		{
			List<TroopRosterElement> list = new List<TroopRosterElement>();
			foreach (TroopRosterElement item5 in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (!item5.Character.IsHero)
				{
					list.Add(item5);
				}
			}
			return IncidentHelper.GetSeededRandomElement(list, Campaign.Current.IncidentManager.ActiveIncidentSeed).Character;
		}
		static CharacterObject GetTownRecruit()
		{
			return MobileParty.MainParty.LastVisitedSettlement.Culture.BasicTroop;
		}
		static ItemObject GetTradeGood()
		{
			Town town = MobileParty.MainParty.LastVisitedSettlement.Town;
			new List<ItemObject>();
			Village village = town.Villages[town.Settlement.RandomIntWithSeed((uint)CampaignTime.Now.ToWeeks, 0, town.Villages.Count)];
			return village.VillageType.Productions[town.Settlement.RandomIntWithSeed((uint)CampaignTime.Now.ToDays, 0, village.VillageType.Productions.Count)].Item1;
		}
		static List<CultureObject> GetTwoCrewCultures()
		{
			List<CultureObject> list = new List<CultureObject>();
			foreach (TroopRosterElement item6 in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (!item6.Character.IsHero && !list.Contains(item6.Character.Culture))
				{
					list.Add(item6.Character.Culture);
					if (list.Count == 2)
					{
						break;
					}
				}
			}
			return list;
		}
		static (Village, Village) GetVillages()
		{
			Town town = MobileParty.MainParty.LastVisitedSettlement.Town;
			return (town.Villages[0], town.Villages[1]);
		}
		static ItemObject GetWhaleOil()
		{
			return Game.Current.ObjectManager.GetObject<ItemObject>("whale_oil");
		}
		static ItemObject GetWine()
		{
			return Game.Current.ObjectManager.GetObject<ItemObject>("wine");
		}
		static bool HasLargeGalley()
		{
			return MobileParty.MainParty.Ships.Any((Ship x) => x.ShipHull.Type == ShipHull.ShipType.Heavy);
		}
	}

	private static TextObject GetPlayerShipName()
	{
		return MobileParty.MainParty.Party.FlagShip.Name;
	}

	private static bool IsOnRiver()
	{
		return Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace) == TerrainType.River;
	}

	private static bool IsLastVisitedPortOnRiver()
	{
		Settlement lastVisitedSettlement = MobileParty.MainParty.LastVisitedSettlement;
		if (lastVisitedSettlement != null)
		{
			return Campaign.Current.MapSceneWrapper.GetFaceTerrainType(lastVisitedSettlement.PortPosition.Face) == TerrainType.River;
		}
		return false;
	}
}
