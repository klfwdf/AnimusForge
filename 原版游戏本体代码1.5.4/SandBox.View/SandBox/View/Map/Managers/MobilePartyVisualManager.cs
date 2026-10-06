using System;
using System.Collections.Generic;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.View.Map.Managers;

public class MobilePartyVisualManager : EntityVisualManagerBase<PartyBase>
{
	private readonly Dictionary<PartyBase, MobilePartyVisual> _partiesAndVisuals = new Dictionary<PartyBase, MobilePartyVisual>();

	private readonly List<MobilePartyVisual> _visualsFlattened = new List<MobilePartyVisual>();

	private int _dirtyPartyVisualCount;

	private MobilePartyVisual[] _dirtyPartiesList = new MobilePartyVisual[2500];

	private readonly List<MobilePartyVisual> _fadingPartiesFlatten = new List<MobilePartyVisual>();

	private readonly HashSet<MobilePartyVisual> _fadingPartiesSet = new HashSet<MobilePartyVisual>();

	public override int Priority => 10;

	public static MobilePartyVisualManager Current => SandBoxViewSubModule.SandBoxViewVisualManager.GetEntityComponent<MobilePartyVisualManager>();

	public override void OnTick(float realDt, float dt)
	{
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
			_fadingPartiesFlatten[num2].TickFadingState(realDt, dt);
		}
	}

	public override void ClearVisualMemory()
	{
		foreach (MobilePartyVisual item in _visualsFlattened)
		{
			item.ClearVisualMemory();
		}
	}

	public override void OnVisualTick(MapScreen screen, float realDt, float dt)
	{
		base.OnVisualTick(screen, realDt, dt);
	}

	public override bool OnVisualIntersected(Ray mouseRay, UIntPtr[] intersectedEntityIDs, Intersection[] intersectionInfos, int entityCount, Vec3 worldMouseNear, Vec3 worldMouseFar, Vec3 terrainIntersectionPoint, ref MapEntityVisual hoveredVisual, ref MapEntityVisual selectedVisual)
	{
		for (int num = entityCount - 1; num >= 0; num--)
		{
			UIntPtr uIntPtr = intersectedEntityIDs[num];
			if (uIntPtr != UIntPtr.Zero && MapScreen.VisualsOfEntities.TryGetValue(uIntPtr, out var value) && value is MobilePartyVisual mobilePartyVisual && value.IsVisibleOrFadingOut() && (!mobilePartyVisual.MapEntity.IsMobile || mobilePartyVisual.MapEntity.MobileParty.IsMainParty || !mobilePartyVisual.MapEntity.MobileParty.IsInRaftState))
			{
				if (!mobilePartyVisual.IsMainEntity || !Hero.MainHero.IsPrisoner)
				{
					hoveredVisual = value.AttachedTo ?? value;
				}
				if (!value.IsMainEntity && (value.AttachedTo == null || !value.AttachedTo.IsMainEntity))
				{
					selectedVisual = value.AttachedTo ?? value;
				}
			}
		}
		return selectedVisual != null;
	}

	public override MapEntityVisual<PartyBase> GetVisualOfEntity(PartyBase partyBase)
	{
		MobileParty mobileParty = partyBase.MobileParty;
		if (mobileParty != null && !mobileParty.IsCurrentlyAtSea)
		{
			_partiesAndVisuals.TryGetValue(partyBase, out var value);
			return value;
		}
		return null;
	}

	protected override void OnFinalize()
	{
		foreach (MobilePartyVisual value in _partiesAndVisuals.Values)
		{
			value.ReleaseResources();
		}
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		foreach (MobileParty item in MobileParty.All)
		{
			AddNewPartyVisualForParty(item, shouldTick: true);
		}
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
	}

	private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
	{
		RemovePartyVisualForParty(mobileParty);
	}

	private void OnMobilePartyCreated(MobileParty mobileParty)
	{
		AddNewPartyVisualForParty(mobileParty);
	}

	public MobilePartyVisual GetPartyVisual(PartyBase partyBase)
	{
		return _partiesAndVisuals[partyBase];
	}

	internal void RegisterFadingVisual(MobilePartyVisual visual)
	{
		if (!_fadingPartiesSet.Contains(visual))
		{
			_fadingPartiesFlatten.Add(visual);
			_fadingPartiesSet.Add(visual);
		}
	}

	internal void UnRegisterFadingVisual(MobilePartyVisual visual)
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
		if (!mobileParty.IsGarrison && !mobileParty.IsMilitia && !_partiesAndVisuals.ContainsKey(mobileParty.Party))
		{
			MobilePartyVisual mobilePartyVisual = new MobilePartyVisual(mobileParty.Party);
			mobilePartyVisual.OnStartup();
			_partiesAndVisuals.Add(mobileParty.Party, mobilePartyVisual);
			_visualsFlattened.Add(mobilePartyVisual);
			if (shouldTick)
			{
				mobilePartyVisual.Tick(0.1f, 0.1f, ref _dirtyPartyVisualCount, ref _dirtyPartiesList);
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
