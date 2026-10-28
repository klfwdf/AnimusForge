using System;
using NavalDLC.View.Map.Visuals;
using SandBox.View;
using SandBox.View.Map;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.View.Map.Managers;

public class AnchorVisualManager : EntityVisualManagerBase<AnchorPoint>
{
	private const float DecalEntityHeight = 1f;

	private const uint DecalColor = 4291596077u;

	private AnchorVisual _anchorVisual;

	private MapScreen.DecalEntity _anchorCircleDecal;

	private CampaignVec2 _cachedPosition;

	private (bool, bool) _cachedDisabledValue;

	public static AnchorVisualManager Current => SandBoxViewSubModule.SandBoxViewVisualManager.GetEntityComponent<AnchorVisualManager>();

	public override int Priority => 30;

	public override MapEntityVisual<AnchorPoint> GetVisualOfEntity(AnchorPoint entity)
	{
		return _anchorVisual;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		if (CanPlayerHaveAnchor())
		{
			if (_anchorVisual == null)
			{
				CreateNewVisual();
			}
			else
			{
				_anchorVisual.OnVisualUpdate();
			}
		}
		_anchorCircleDecal = MapScreen.DecalEntity.Create(base.MapScene, "decal_city_circle_a", "TownCircle");
	}

	public override bool OnVisualIntersected(Ray mouseRay, UIntPtr[] intersectedEntityIDs, Intersection[] intersectionInfos, int entityCount, Vec3 worldMouseNear, Vec3 worldMouseFar, Vec3 terrainIntersectionPoint, ref MapEntityVisual hoveredVisual, ref MapEntityVisual selectedVisual)
	{
		for (int num = entityCount - 1; num >= 0; num--)
		{
			UIntPtr uIntPtr = intersectedEntityIDs[num];
			if (uIntPtr != UIntPtr.Zero && MapScreen.VisualsOfEntities.TryGetValue(uIntPtr, out var value) && value is AnchorVisual && value.IsVisibleOrFadingOut())
			{
				hoveredVisual = value;
				selectedVisual = value;
			}
		}
		return selectedVisual != null;
	}

	public override void OnVisualTick(MapScreen screen, float realDt, float dt)
	{
		bool flag = false;
		MatrixFrame frame = MatrixFrame.Identity;
		if (_anchorVisual != null && ((MobileParty.MainParty.Ai.AiBehaviorInteractable != null && MobileParty.MainParty.Ai.AiBehaviorInteractable is AnchorPoint) || (MapScreen.Instance.CurrentVisualOfTooltip != null && MapScreen.Instance.CurrentVisualOfTooltip is AnchorVisual)))
		{
			flag = true;
			frame.origin = _anchorVisual.GetVisualPosition();
		}
		_anchorCircleDecal.GameEntity.SetVisibilityExcludeParents(flag);
		if (flag)
		{
			_anchorCircleDecal.Decal.SetVectorArgument(1f, 1f, 0f, 0f);
			_anchorCircleDecal.Decal.SetFactor1Linear(4291596077u);
			_anchorCircleDecal.GameEntity.SetGlobalFrame(in frame);
		}
	}

	public override void OnTick(float realDt, float dt)
	{
		base.OnTick(realDt, dt);
		bool flag = _anchorVisual?.Entity != null && _anchorVisual.Entity.IsVisibleIncludeParents() && (_anchorVisual.MapEntity != MobileParty.MainParty.Anchor || !MobileParty.MainParty.IsActive || MobileParty.MainParty.Anchor.IsDisabled);
		if (_anchorVisual != null && (flag || PartyBase.MainParty.Ships.Count == 0))
		{
			RemoveAnchorVisual();
		}
		if (CanPlayerHaveAnchor())
		{
			if (_anchorVisual != null)
			{
				UpdateAnchorVisual();
			}
			else
			{
				CreateNewVisual();
			}
		}
		if (_cachedPosition != MobileParty.MainParty.Anchor.Position && (_cachedPosition.IsValid() || MobileParty.MainParty.Anchor.IsValid) && !MobileParty.MainParty.Anchor.IsDisabled)
		{
			OnAnchorPositionUpdated();
			_cachedPosition = MobileParty.MainParty.Anchor.Position;
		}
		if (_cachedDisabledValue.Item1 != MobileParty.MainParty.Anchor.IsDisabled || _cachedDisabledValue.Item2 != MobileParty.MainParty.IsActive)
		{
			OnAnchorPositionUpdated();
			_cachedDisabledValue = (MobileParty.MainParty.Anchor.IsDisabled, MobileParty.MainParty.IsActive);
		}
	}

	internal void OnAnchorPositionUpdated()
	{
		if (_anchorVisual != null)
		{
			if (CanPlayerHaveAnchor())
			{
				_anchorVisual.UpdateAnchorVisualPosition();
			}
			else
			{
				RemoveAnchorVisual();
			}
		}
		else if (CanPlayerHaveAnchor())
		{
			CreateNewVisual();
		}
	}

	private void CreateNewVisual()
	{
		_anchorVisual = new AnchorVisual(MobileParty.MainParty.Anchor);
		_anchorVisual.OnStartup();
		_cachedPosition = _anchorVisual.MapEntity.Position;
		MapScreen.VisualsOfEntities.Add(_anchorVisual.Entity.Pointer, _anchorVisual);
	}

	private void RemoveAnchorVisual()
	{
		MapScreen.VisualsOfEntities.Remove(_anchorVisual.Entity.Pointer);
		_anchorVisual.OnRemoved();
		_cachedPosition = CampaignVec2.Invalid;
		_anchorVisual = null;
	}

	private void UpdateAnchorVisual()
	{
		_anchorVisual.OnVisualUpdate();
		if (_anchorVisual?.Entity != null && !MapScreen.VisualsOfEntities.ContainsKey(_anchorVisual.Entity.Pointer))
		{
			MapScreen.VisualsOfEntities.Add(_anchorVisual.Entity.Pointer, _anchorVisual);
		}
	}

	public static bool CanPlayerHaveAnchor()
	{
		if (!MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.IsActive && MobileParty.MainParty.Anchor.IsValid && MobileParty.MainParty.HasNavalNavigationCapability)
		{
			return !MobileParty.MainParty.Anchor.IsDisabled;
		}
		return false;
	}
}
