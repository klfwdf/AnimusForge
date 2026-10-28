using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.View.Map.Visuals;
using SandBox.View;
using SandBox.View.Map;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.View.Map.Managers;

public class NavalMobilePartyVisualManager : EntityVisualManagerBase<PartyBase>
{
	private const float DamageSoundCooldown = 2f;

	private static int _shipDamageSoundEventId = SoundManager.GetEventGlobalIndex("event:/ui/campaign/ship_damage");

	private readonly Dictionary<PartyBase, NavalMobilePartyVisual> _partiesAndVisuals = new Dictionary<PartyBase, NavalMobilePartyVisual>();

	private readonly List<NavalMobilePartyVisual> _visualsFlattened = new List<NavalMobilePartyVisual>();

	private int _dirtyPartyVisualCount;

	private NavalMobilePartyVisual[] _dirtyPartiesList = new NavalMobilePartyVisual[2500];

	private float _timeElapsedSinceLastShipDamageSoundPlayed;

	private float _mainPartyPreviousShipDamageTriggerHealthPercent = 1f;

	private readonly List<NavalMobilePartyVisual> _fadingPartiesFlatten = new List<NavalMobilePartyVisual>();

	private readonly HashSet<NavalMobilePartyVisual> _fadingPartiesSet = new HashSet<NavalMobilePartyVisual>();

	private readonly List<GameEntity> _bridgeEntityCache = new List<GameEntity>();

	public static NavalMobilePartyVisualManager Current => SandBoxViewSubModule.SandBoxViewVisualManager.GetEntityComponent<NavalMobilePartyVisualManager>();

	public override int Priority => 20;

	public override void OnTick(float realDt, float dt)
	{
		if (!base.MapScene.HasTerrainHeightmap || !base.MapScene.ContainsTerrain)
		{
			return;
		}
		_dirtyPartyVisualCount = -1;
		TWParallel.For(0, _visualsFlattened.Count, delegate(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				_visualsFlattened[i].Tick(dt, realDt, ref _dirtyPartyVisualCount, ref _dirtyPartiesList);
			}
		});
		for (int num = 0; num < _dirtyPartyVisualCount + 1; num++)
		{
			_dirtyPartiesList[num].ValidateIsDirty();
		}
		for (int num2 = _fadingPartiesFlatten.Count - 1; num2 >= 0; num2--)
		{
			_fadingPartiesFlatten[num2].TickFadingState(realDt);
		}
		if (dt > 0f && _timeElapsedSinceLastShipDamageSoundPlayed < 0f)
		{
			_timeElapsedSinceLastShipDamageSoundPlayed += realDt;
		}
		if (_timeElapsedSinceLastShipDamageSoundPlayed >= 0f && MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.Ships.Any())
		{
			TriggerShipDamageSound();
		}
	}

	public override void ClearVisualMemory()
	{
		foreach (NavalMobilePartyVisual item in _visualsFlattened)
		{
			item.ClearVisualMemory();
		}
	}

	public override MapEntityVisual<PartyBase> GetVisualOfEntity(PartyBase partyBase)
	{
		MobileParty mobileParty = partyBase.MobileParty;
		if (mobileParty != null && mobileParty.IsCurrentlyAtSea)
		{
			_partiesAndVisuals.TryGetValue(partyBase, out var value);
			return value;
		}
		return null;
	}

	public override bool OnVisualIntersected(Ray mouseRay, UIntPtr[] intersectedEntityIDs, Intersection[] intersectionInfos, int entityCount, Vec3 worldMouseNear, Vec3 worldMouseFar, Vec3 terrainIntersectionPoint, ref MapEntityVisual hoveredVisual, ref MapEntityVisual selectedVisual)
	{
		for (int num = entityCount - 1; num >= 0; num--)
		{
			UIntPtr uIntPtr = intersectedEntityIDs[num];
			if (uIntPtr != UIntPtr.Zero && MapScreen.VisualsOfEntities.TryGetValue(uIntPtr, out var value) && value is NavalMobilePartyVisual navalMobilePartyVisual && value.IsVisibleOrFadingOut() && (!navalMobilePartyVisual.MapEntity.IsMobile || navalMobilePartyVisual.MapEntity.MobileParty.IsMainParty || !navalMobilePartyVisual.MapEntity.MobileParty.IsInRaftState))
			{
				Intersection intersection = intersectionInfos[num];
				_ = (worldMouseNear - intersection.IntersectionPoint).Length;
				if (value.AttachedTo == null)
				{
					hoveredVisual = value;
				}
				else
				{
					hoveredVisual = value.AttachedTo;
				}
				if (!value.IsMainEntity && (value.AttachedTo == null || !value.AttachedTo.IsMainEntity))
				{
					if (value.AttachedTo != null)
					{
						selectedVisual = value.AttachedTo;
					}
					else
					{
						selectedVisual = value;
					}
				}
			}
		}
		return selectedVisual != null;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		foreach (MobileParty item in MobileParty.All)
		{
			AddNewPartyVisualForParty(item, shouldTick: true);
		}
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
		CampaignEvents.OnMobilePartyNavigationStateChangedEvent.AddNonSerializedListener(this, OnMobilePartyNavigationStateChanged);
		CampaignEvents.OnMobilePartyJoinedToSiegeEventEvent.AddNonSerializedListener(this, OnMobilePartyJoinedToSiegeEvent);
		CampaignEvents.OnMobilePartyLeftSiegeEventEvent.AddNonSerializedListener(this, OnMobilePartyLeftSiegeEvent);
		if (MobileParty.MainParty.Ships.Any())
		{
			_mainPartyPreviousShipDamageTriggerHealthPercent = MobileParty.MainParty.Ships.Average((Ship s) => s.HitPoints / s.MaxHitPoints);
		}
		_bridgeEntityCache.AddRange(base.MapScene.FindEntitiesWithTag("bridge"));
	}

	protected override void OnFinalize()
	{
		foreach (NavalMobilePartyVisual value in _partiesAndVisuals.Values)
		{
			value.ReleaseResources();
		}
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	public NavalMobilePartyVisual GetPartyVisual(PartyBase partyBase)
	{
		return _partiesAndVisuals[partyBase];
	}

	internal void RegisterFadingVisual(NavalMobilePartyVisual visual)
	{
		if (!_fadingPartiesSet.Contains(visual))
		{
			_fadingPartiesFlatten.Add(visual);
			_fadingPartiesSet.Add(visual);
		}
	}

	internal GameEntity GetNearbyBridgeToParty(PartyBase partyBase)
	{
		if (_partiesAndVisuals.TryGetValue(partyBase, out var visual))
		{
			return _bridgeEntityCache.FirstOrDefault((GameEntity x) => x.GlobalPosition.Distance(visual.StrategicEntity.GlobalPosition) < 3f);
		}
		return null;
	}

	private void OnMobilePartyNavigationStateChanged(MobileParty mobileParty)
	{
		if (mobileParty.IsCurrentlyAtSea && mobileParty.Ships.Count > 0)
		{
			if (mobileParty.IsMainParty)
			{
				SoundEvent.PlaySound2D("event:/ui/ship_disembark");
			}
		}
		else if (mobileParty.IsMainParty)
		{
			SoundEvent.PlaySound2D("event:/ui/ship_embark");
		}
	}

	private void TriggerShipDamageSound()
	{
		float num = MobileParty.MainParty.Ships.Average((Ship s) => s.HitPoints / s.MaxHitPoints);
		float num2 = _mainPartyPreviousShipDamageTriggerHealthPercent - num;
		if (num2 > 0.01f)
		{
			_mainPartyPreviousShipDamageTriggerHealthPercent = num;
			_timeElapsedSinceLastShipDamageSoundPlayed = -2f;
			SoundEventParameter parameter = new SoundEventParameter("Campaign Ship Damage", num2 * 10f);
			MBSoundEvent.PlaySound(_shipDamageSoundEventId, ref parameter, Vec3.Zero);
		}
	}

	private void OnMobilePartyLeftSiegeEvent(MobileParty mobileParty)
	{
		if (mobileParty.SiegeEvent == null || !mobileParty.SiegeEvent.BesiegedSettlement.HasPort || mobileParty.SiegeEvent.BlockadeShouldBeActivated || !mobileParty.Ships.Any())
		{
			return;
		}
		mobileParty.SetNavalVisualAsDirty();
		foreach (PartyBase item in mobileParty.BesiegerCamp.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.BlockadeBattle))
		{
			item.MobileParty.SetNavalVisualAsDirty();
		}
	}

	private void OnMobilePartyJoinedToSiegeEvent(MobileParty mobileParty)
	{
		SiegeEvent siegeEvent = mobileParty.SiegeEvent;
		if (siegeEvent == null || !siegeEvent.IsBlockadeActive || !mobileParty.Ships.Any())
		{
			return;
		}
		foreach (PartyBase item in mobileParty.BesiegerCamp.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.BlockadeBattle))
		{
			item.MobileParty.SetNavalVisualAsDirty();
		}
	}

	private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase _)
	{
		RemovePartyVisualForParty(mobileParty);
	}

	private void OnMobilePartyCreated(MobileParty mobileParty)
	{
		AddNewPartyVisualForParty(mobileParty);
	}

	internal void UnRegisterFadingVisual(NavalMobilePartyVisual visual)
	{
		if (_fadingPartiesSet.Contains(visual))
		{
			int index = _fadingPartiesFlatten.IndexOf(visual);
			_fadingPartiesFlatten[index] = _fadingPartiesFlatten[_fadingPartiesFlatten.Count - 1];
			_fadingPartiesFlatten.Remove(_fadingPartiesFlatten[_fadingPartiesFlatten.Count - 1]);
			_fadingPartiesSet.Remove(visual);
		}
	}

	private void AddNewPartyVisualForParty(MobileParty mobileParty, bool shouldTick = false)
	{
		if (mobileParty.IsGarrison || mobileParty.IsMilitia || _partiesAndVisuals.ContainsKey(mobileParty.Party))
		{
			return;
		}
		NavalMobilePartyVisual navalMobilePartyVisual = new NavalMobilePartyVisual(mobileParty.Party);
		navalMobilePartyVisual.OnStartup();
		_partiesAndVisuals.Add(mobileParty.Party, navalMobilePartyVisual);
		_visualsFlattened.Add(navalMobilePartyVisual);
		if (shouldTick)
		{
			navalMobilePartyVisual.Tick(0.1f, 0.1f, ref _dirtyPartyVisualCount, ref _dirtyPartiesList);
			if (mobileParty.IsTransitionInProgress)
			{
				mobileParty.SetNavalVisualAsDirty();
				navalMobilePartyVisual.UpdateEntityPosition(0.1f, 0.1f);
			}
		}
	}

	private void RemovePartyVisualForParty(MobileParty mobileParty)
	{
		if (_partiesAndVisuals.TryGetValue(mobileParty.Party, out var value))
		{
			value.OnPartyRemoved();
			_visualsFlattened.Remove(value);
			_partiesAndVisuals.Remove(mobileParty.Party);
		}
	}
}
