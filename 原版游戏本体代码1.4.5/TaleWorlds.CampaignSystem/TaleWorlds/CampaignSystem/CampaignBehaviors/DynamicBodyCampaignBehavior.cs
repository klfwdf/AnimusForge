using Helpers;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class DynamicBodyCampaignBehavior : CampaignBehaviorBase
{
	private const float DailyBuildDecrease = -0.015f;

	private const float DailyBuildIncrease = 0.025f;

	private const float DailyWeightDecreaseWhenStarving = -0.1f;

	private const float DailyWeightDecreaseWhenNotStarving = -0.025f;

	private const float DailyWeightIncrease = 0.025f;

	private CampaignTime _lastSettlementVisitTime;

	private CampaignTime _lastEncounterTime;

	private float _unmodifiedWeight = -1f;

	private float _unmodifiedBuild = -1f;

	private CampaignTime LastSettlementVisitTime
	{
		get
		{
			if (Hero.MainHero.CurrentSettlement != null)
			{
				_lastSettlementVisitTime = CampaignTime.Now;
			}
			return _lastSettlementVisitTime;
		}
		set
		{
			_lastSettlementVisitTime = value;
		}
	}

	private float MaxPlayerWeight => MathF.Min(1f, _unmodifiedWeight * 1.3f);

	private float MinPlayerWeight => MathF.Max(0f, _unmodifiedWeight * 0.7f);

	private float MaxPlayerBuild => MathF.Min(1f, _unmodifiedBuild * 1.3f);

	private float MinPlayerBuild => MathF.Max(0f, _unmodifiedBuild * 0.7f);

	private void DailyTick()
	{
		bool flag = LastSettlementVisitTime.ElapsedDaysUntilNow < 1f;
		bool flag2 = Hero.MainHero.PartyBelongedTo != null && Hero.MainHero.PartyBelongedTo.Party.IsStarving;
		float num = ((Hero.MainHero.CurrentSettlement == null && flag2) ? (-0.1f) : (flag ? 0.025f : (-0.025f)));
		Hero.MainHero.Weight = MBMath.ClampFloat(Hero.MainHero.Weight + num, MinPlayerWeight, MaxPlayerWeight);
		float num2 = ((MapEvent.PlayerMapEvent != null || PlayerSiege.PlayerSiegeEvent != null || _lastEncounterTime.ElapsedDaysUntilNow < 2f) ? 0.025f : (-0.015f));
		Hero.MainHero.Build = MBMath.ClampFloat(Hero.MainHero.Build + num2, MinPlayerBuild, MaxPlayerBuild);
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (party != null && party.IsMainParty)
		{
			LastSettlementVisitTime = CampaignTime.Now;
		}
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		if (mapEvent.IsPlayerMapEvent)
		{
			_lastEncounterTime = CampaignTime.Now;
		}
	}

	private void OnPlayerBodyPropertiesChanged()
	{
		_unmodifiedBuild = Hero.MainHero.Build;
		_unmodifiedWeight = Hero.MainHero.Weight;
	}

	private void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newMainParty, bool isMainPartyChanged)
	{
		_unmodifiedBuild = newPlayer.Build;
		_unmodifiedWeight = newPlayer.Weight;
	}

	private void OnHeroCreated(Hero hero, bool bornNaturally)
	{
		if (!bornNaturally)
		{
			DynamicBodyProperties dynamicBodyPropertiesBetweenMinMaxRange = CharacterHelper.GetDynamicBodyPropertiesBetweenMinMaxRange(hero.CharacterObject);
			hero.Weight = dynamicBodyPropertiesBetweenMinMaxRange.Weight;
			hero.Build = dynamicBodyPropertiesBetweenMinMaxRange.Build;
		}
	}

	private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
	{
		_lastSettlementVisitTime = CampaignTime.Now;
		_lastEncounterTime = CampaignTime.Now;
		OnPlayerBodyPropertiesChanged();
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
		CampaignEvents.OnPlayerBodyPropertiesChangedEvent.AddNonSerializedListener(this, OnPlayerBodyPropertiesChanged);
		CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, OnPlayerBodyPropertiesChanged);
		CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, OnPlayerCharacterChanged);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEnd);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_lastSettlementVisitTime", ref _lastSettlementVisitTime);
		dataStore.SyncData("_lastEncounterTime", ref _lastEncounterTime);
		dataStore.SyncData("_unmodifiedWeight", ref _unmodifiedWeight);
		dataStore.SyncData("_unmodifiedBuild", ref _unmodifiedBuild);
	}
}
