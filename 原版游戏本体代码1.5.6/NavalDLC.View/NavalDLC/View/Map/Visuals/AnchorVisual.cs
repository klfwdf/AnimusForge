using System.Collections.Generic;
using NavalDLC.Missions.Objects;
using SandBox;
using SandBox.View.Map;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.View.Map.Visuals;

public class AnchorVisual : MapEntityVisual<AnchorPoint>
{
	private const string BodyMeshTag = "body_mesh";

	private ShipHull _flagshipHull;

	private uint _cachedVersion;

	private List<SailVisual> _sailVisuals = new List<SailVisual>();

	private WeakGameEntity _bodyMeshEntity;

	private Scene _mapScene;

	public override CampaignVec2 InteractionPositionForPlayer => base.MapEntity.GetInteractionPosition(MobileParty.MainParty);

	public override MapEntityVisual AttachedTo => null;

	public GameEntity Entity { get; private set; }

	private Scene MapScene
	{
		get
		{
			if (_mapScene == null && Campaign.Current != null && Campaign.Current.MapSceneWrapper != null)
			{
				_mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene;
			}
			return _mapScene;
		}
	}

	public AnchorVisual(AnchorPoint mapEntity)
		: base(mapEntity)
	{
	}

	public override Vec3 GetVisualPosition()
	{
		return base.MapEntity.Position.AsVec3();
	}

	public override bool IsVisibleOrFadingOut()
	{
		return !base.MapEntity.Owner.IsTransitionInProgress;
	}

	public override void OnHover()
	{
		InformationManager.ShowTooltip(typeof(AnchorPoint), base.MapEntity);
	}

	public override bool OnMapClick(bool followModifierUsed)
	{
		if (IsInteractable())
		{
			MobileParty.MainParty.SetMoveGoToInteractablePoint(base.MapEntity, MobileParty.NavigationType.All);
			return true;
		}
		return false;
	}

	public override void ReleaseResources()
	{
	}

	public override void OnOpenEncyclopedia()
	{
	}

	public override bool IsInteractable()
	{
		return base.MapEntity.IsInteractable();
	}

	public void OnStartup()
	{
		if (Entity != null)
		{
			OnVisualUpdate();
		}
		else
		{
			RefreshGameEntity();
		}
	}

	public void OnRemoved()
	{
		if (PartyBase.MainParty.Ships.Count > 0)
		{
			Entity.SetVisibilityExcludeParents(visible: false);
			return;
		}
		base.MapEntity.ResetPosition();
		Entity?.Remove(111);
		Entity = null;
		ResetVersionCache();
		_sailVisuals.Clear();
	}

	public void OnVisualUpdate()
	{
		Ship flagShip = PartyBase.MainParty.FlagShip;
		if (_flagshipHull == null || _flagshipHull != flagShip.ShipHull)
		{
			if (Entity != null)
			{
				MapScreen.VisualsOfEntities.Remove(Entity.Pointer);
				Entity?.Remove(111);
			}
			RefreshGameEntity();
		}
		else if (flagShip.VersionNo != _cachedVersion)
		{
			UpdateVersionCache();
			NavalDLCViewHelpers.ShipVisualHelper.RefreshShipVisuals(Entity.WeakEntity, flagShip, _sailVisuals);
		}
	}

	private void UpdateVersionCache()
	{
		Ship flagShip = PartyBase.MainParty.FlagShip;
		_cachedVersion = flagShip.VersionNo;
		_flagshipHull = flagShip.ShipHull;
	}

	private void ResetVersionCache()
	{
		_flagshipHull = null;
		_cachedVersion = 0u;
	}

	private void RefreshGameEntity()
	{
		UpdateVersionCache();
		Entity = NavalDLCViewHelpers.ShipVisualHelper.GetFlagshipEntity(PartyBase.MainParty, MapScene);
		_bodyMeshEntity = Entity.WeakEntity.GetFirstChildEntityWithTagRecursive("body_mesh");
		NavalDLCViewHelpers.ShipVisualHelper.CollectSailVisuals(Entity.WeakEntity, _sailVisuals);
		Entity.SetVisibilityExcludeParents(visible: false);
		UpdateAnchorVisualPosition();
		InitializeAnchorCollider();
	}

	private void InitializeAnchorCollider()
	{
		if (Entity != null)
		{
			if (_bodyMeshEntity.IsValid)
			{
				Vec3 eulerAngles = Entity.GetGlobalFrame().rotation.GetEulerAngles();
				Vec3 eulerAngles2 = _bodyMeshEntity.GetGlobalFrame().rotation.GetEulerAngles();
				BoundingBox localPhysicsBoundingBox = _bodyMeshEntity.GetLocalPhysicsBoundingBox(includeChildren: false);
				localPhysicsBoundingBox.max.RotateAboutZ(eulerAngles.RotationZ - eulerAngles2.RotationZ);
				localPhysicsBoundingBox.min.RotateAboutZ(eulerAngles.RotationZ - eulerAngles2.RotationZ);
				float num = MathF.Abs(localPhysicsBoundingBox.max.x - localPhysicsBoundingBox.min.x) / 2f;
				float num2 = num / 2f;
				float num3 = MathF.Max(localPhysicsBoundingBox.max.y, localPhysicsBoundingBox.min.y);
				float num4 = MathF.Min(localPhysicsBoundingBox.max.y, localPhysicsBoundingBox.min.y);
				GameEntityPhysicsExtensions.AddCapsuleAsBody(p1: new Vec3(0f, num3 - num2, num2 + 0.4f), p2: new Vec3(0f, num4 + num2, num2 + 0.4f), gameEntity: Entity, radius: num, bodyFlags: BodyFlags.Moveable | BodyFlags.OnlyCollideWithRaycast);
			}
			else
			{
				Entity.AddSphereAsBody(new Vec3(0f, 0f, 0f, -1f), 5f, BodyFlags.Moveable | BodyFlags.OnlyCollideWithRaycast);
			}
		}
	}

	internal void UpdateAnchorVisualPosition()
	{
		MatrixFrame frame = CalculateAnchorFrame(base.MapEntity);
		Entity.SetFrame(ref frame);
		Entity.SetVisibilityExcludeParents(visible: true);
	}

	private MatrixFrame CalculateAnchorFrame(AnchorPoint anchor)
	{
		Vec2 vec = (anchor.GetInteractionPosition(anchor.Owner).ToVec2() - anchor.Position.ToVec2()).Normalized();
		Vec3 scaleAmountXYZ = Entity.GetLocalScale();
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = GetVisualPosition();
		identity.rotation.f.AsVec2 = vec.RightVec();
		identity.rotation.f.NormalizeWithoutChangingZ();
		identity.rotation.Orthonormalize();
		identity.rotation.ApplyScaleLocal(in scaleAmountXYZ);
		return identity;
	}
}
