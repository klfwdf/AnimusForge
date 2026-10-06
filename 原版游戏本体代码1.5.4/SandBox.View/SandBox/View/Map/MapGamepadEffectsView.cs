using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Map;

public class MapGamepadEffectsView : MapView
{
	private readonly float[] _lowFrequencyLevels = new float[5];

	private readonly float[] _lowFrequencyDurations = new float[5];

	private readonly float[] _highFrequencyLevels = new float[5];

	private readonly float[] _highFrequencyDurations = new float[5];

	protected internal override void CreateLayout()
	{
		base.CreateLayout();
		RegisterEvents();
	}

	protected internal override void OnFinalize()
	{
		base.OnFinalize();
		UnregisterEvents();
	}

	private void RegisterEvents()
	{
		CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageRaid);
		CampaignEvents.OnSiegeBombardmentWallHitEvent.AddNonSerializedListener(this, OnSiegeBombardmentWallHit);
		CampaignEvents.OnSiegeEngineDestroyedEvent.AddNonSerializedListener(this, OnSiegeEngineDestroyed);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		CampaignEvents.OnPeaceOfferedToPlayerEvent.AddNonSerializedListener(this, OnPeaceOfferedToPlayer);
		CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
		CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, OnHeroLevelUp);
		CampaignEvents.KingdomDecisionAdded.AddNonSerializedListener(this, OnKingdomDecisionAdded);
		CampaignEvents.OnMainPartyStarvingEvent.AddNonSerializedListener(this, OnMainPartyStarving);
		CampaignEvents.RebellionFinished.AddNonSerializedListener(this, OnRebellionFinished);
		CampaignEvents.OnHideoutSpottedEvent.AddNonSerializedListener(this, OnHideoutSpotted);
		CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
		CampaignEvents.HeroOrPartyTradedGold.AddNonSerializedListener(this, OnHeroOrPartyTradedGold);
		CampaignEvents.PartyAttachedAnotherParty.AddNonSerializedListener(this, OnPartyAttachedAnotherParty);
	}

	private void UnregisterEvents()
	{
		CampaignEvents.VillageBeingRaided.ClearListeners(this);
		CampaignEvents.OnSiegeBombardmentWallHitEvent.ClearListeners(this);
		CampaignEvents.OnSiegeEngineDestroyedEvent.ClearListeners(this);
		CampaignEvents.WarDeclared.ClearListeners(this);
		CampaignEvents.OnPeaceOfferedToPlayerEvent.ClearListeners(this);
		CampaignEvents.ArmyDispersed.ClearListeners(this);
		CampaignEvents.HeroLevelledUp.ClearListeners(this);
		CampaignEvents.KingdomDecisionAdded.ClearListeners(this);
		CampaignEvents.OnMainPartyStarvingEvent.ClearListeners(this);
		CampaignEvents.RebellionFinished.ClearListeners(this);
		CampaignEvents.OnHideoutSpottedEvent.ClearListeners(this);
		CampaignEvents.HeroCreated.ClearListeners(this);
		CampaignEvents.MakePeace.ClearListeners(this);
		CampaignEvents.HeroOrPartyTradedGold.ClearListeners(this);
		CampaignEvents.PartyAttachedAnotherParty.ClearListeners(this);
	}

	private void OnVillageRaid(Village village)
	{
		if (MobileParty.MainParty.CurrentSettlement == village.Settlement)
		{
			SetRumbleWithRandomValues(0.2f, 0.4f);
		}
	}

	private void OnSiegeBombardmentWallHit(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType weapon, bool isWallCracked)
	{
		if (isWallCracked && (besiegerParty == MobileParty.MainParty || besiegedSettlement == MobileParty.MainParty.CurrentSettlement))
		{
			SetRumbleWithRandomValues(0.3f, 0.8f);
		}
	}

	private void OnSiegeEngineDestroyed(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType destroyedEngine)
	{
		if (besiegerParty == MobileParty.MainParty || besiegedSettlement == MobileParty.MainParty.CurrentSettlement)
		{
			SetRumbleWithRandomValues(0.05f, 0.3f, 4);
		}
	}

	private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
	{
		if (faction1 == Clan.PlayerClan.MapFaction || faction2 == Clan.PlayerClan.MapFaction)
		{
			SetRumbleWithRandomValues(0.3f, 0.5f, 3);
		}
	}

	private void OnPeaceOfferedToPlayer(IFaction opponentFaction, int tributeAmount, int tributeDurationInDays)
	{
		SetRumbleWithRandomValues(0.2f, 0.4f, 3);
	}

	private void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
	{
		if (isPlayersArmy)
		{
			SetRumbleWithRandomValues((float)army.TotalManCount / 2000f, (float)army.TotalManCount / 1000f);
		}
	}

	private void OnHeroLevelUp(Hero hero, bool shouldNotify)
	{
		if (hero == Hero.MainHero && !(GameStateManager.Current.ActiveState is GameLoadingState))
		{
			SetRumbleWithRandomValues(0.1f, 0.3f, 3);
		}
	}

	private void OnKingdomDecisionAdded(KingdomDecision decision, bool isPlayerInvolved)
	{
		if (isPlayerInvolved)
		{
			SetRumbleWithRandomValues(0.1f, 0.3f, 2);
		}
	}

	private void OnMainPartyStarving()
	{
		SetRumbleWithRandomValues(0.2f, 0.4f);
	}

	private void OnRebellionFinished(Settlement settlement, Clan oldOwnerClan)
	{
		if (oldOwnerClan == Clan.PlayerClan)
		{
			SetRumbleWithRandomValues(0.2f, 0.4f);
		}
	}

	private void OnHideoutSpotted(PartyBase party, PartyBase hideoutParty)
	{
		SetRumbleWithRandomValues(0.1f, 0.3f, 3);
	}

	private void OnHeroCreated(Hero hero, bool isBornNaturally = false)
	{
		if (hero.Father == Hero.MainHero || hero.Mother == Hero.MainHero)
		{
			SetRumbleWithRandomValues(0.2f, 0.4f, 3);
		}
	}

	private void OnMakePeace(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
	{
		if (side1Faction == Clan.PlayerClan.MapFaction || side2Faction == Clan.PlayerClan.MapFaction)
		{
			SetRumbleWithRandomValues(0.2f, 0.4f, 3);
		}
	}

	private void OnHeroOrPartyTradedGold((Hero, PartyBase) giver, (Hero, PartyBase) recipient, (int, string) goldAmount, bool showNotification)
	{
		if (giver.Item1 == Hero.MainHero && Hero.MainHero.Gold == 0)
		{
			SetRumbleWithRandomValues(0.1f, 0.3f, 3);
		}
	}

	private void OnPartyAttachedAnotherParty(MobileParty party)
	{
		if (party.Army != null && party.Army.LeaderParty == MobileParty.MainParty)
		{
			SetRumbleWithRandomValues(0.1f, 0.3f, 3);
		}
	}

	protected internal override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (Input.IsKeyDown(InputKey.BackSpace))
		{
			SetRumbleWithRandomValues(0.5f, 0f);
		}
	}

	private void SetRumbleWithRandomValues(float baseValue = 0f, float offsetRange = 1f, int frequencyCount = 5)
	{
		SetRandomRumbleValues(baseValue, offsetRange, frequencyCount);
	}

	private void SetRandomRumbleValues(float baseValue, float offsetRange, int frequencyCount)
	{
		baseValue = MBMath.ClampFloat(baseValue, 0f, 1f);
		offsetRange = MBMath.ClampFloat(offsetRange, 0f, 1f - baseValue);
		frequencyCount = MBMath.ClampInt(frequencyCount, 2, 5);
		for (int i = 0; i < frequencyCount; i++)
		{
			_lowFrequencyLevels[i] = baseValue + MBRandom.RandomFloatRanged(offsetRange);
			_lowFrequencyDurations[i] = baseValue + MBRandom.RandomFloatRanged(offsetRange);
			_highFrequencyLevels[i] = baseValue + MBRandom.RandomFloatRanged(offsetRange);
			_highFrequencyDurations[i] = baseValue + MBRandom.RandomFloatRanged(offsetRange);
		}
	}
}
