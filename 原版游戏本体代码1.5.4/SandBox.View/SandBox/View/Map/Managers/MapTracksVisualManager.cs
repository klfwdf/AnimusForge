using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.View.Map.Managers;

public class MapTracksVisualManager : EntityVisualManagerBase<Track>
{
	private const string TrackPrefabName = "map_track_arrow";

	private const int DefaultObjectPoolCount = 256;

	private Dictionary<Track, (TrackVisual, GameEntity)> _trackVisuals;

	private SphereData _trackSphere;

	private bool _tracksDirty = true;

	private readonly TWParallel.ParallelForAuxPredicate _parallelUpdateTrackColorsPredicate;

	private readonly TWParallel.ParallelForAuxPredicate _parallelUpdateVisibleTracksPredicate;

	private Stack<GameEntity> _entityPool;

	public static MapTracksVisualManager Current => SandBoxViewSubModule.SandBoxViewVisualManager.GetEntityComponent<MapTracksVisualManager>();

	public override int Priority => 50;

	public MapTracksVisualManager()
	{
		_trackVisuals = new Dictionary<Track, (TrackVisual, GameEntity)>();
		_entityPool = new Stack<GameEntity>();
		PopulateEntityPool();
		_parallelUpdateTrackColorsPredicate = ParallelUpdateTrackColors;
		_parallelUpdateVisibleTracksPredicate = ParallelUpdateVisibleTracks;
	}

	public override void OnVisualTick(MapScreen screen, float realDt, float dt)
	{
		if (_tracksDirty)
		{
			UpdateTrackMesh();
			_tracksDirty = false;
		}
		TWParallel.For(0, MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks.Count, _parallelUpdateTrackColorsPredicate);
	}

	public override bool OnVisualIntersected(Ray mouseRay, UIntPtr[] intersectedEntityIDs, Intersection[] intersectionInfos, int entityCount, Vec3 worldMouseNear, Vec3 worldMouseFar, Vec3 terrainIntersectionPoint, ref MapEntityVisual hoveredVisual, ref MapEntityVisual selectedVisual)
	{
		if (hoveredVisual == null)
		{
			hoveredVisual = GetVisualOfEntity(GetTrackOnMouse(mouseRay, terrainIntersectionPoint));
		}
		return hoveredVisual != null;
	}

	public override void OnGameLoadFinished()
	{
		base.OnGameLoadFinished();
		foreach (Track detectedTrack in MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks)
		{
			OnTrackDetected(detectedTrack);
		}
	}

	public override MapEntityVisual<Track> GetVisualOfEntity(Track entity)
	{
		if (entity == null)
		{
			return null;
		}
		return _trackVisuals[entity].Item1;
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		foreach (GameEntity item in _entityPool.ToList())
		{
			item.Remove(111);
		}
		_entityPool.Clear();
		_trackVisuals.Clear();
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		CampaignEvents.TrackDetectedEvent.AddNonSerializedListener(this, OnTrackDetected);
		CampaignEvents.TrackLostEvent.AddNonSerializedListener(this, OnTrackLost);
	}

	internal void ReleaseResources(Track track)
	{
		if (_trackVisuals.TryGetValue(track, out var value))
		{
			value.Item2.Remove(111);
		}
	}

	private void OnTrackDetected(Track track)
	{
		_tracksDirty = true;
		GameEntity gameEntity = GetGameEntity();
		gameEntity.SetVisibilityExcludeParents(visible: true);
		_trackVisuals.Add(track, (new TrackVisual(track), gameEntity));
		SandBoxViewSubModule.VisualsOfEntities.Add(_trackVisuals[track].Item2.Pointer, _trackVisuals[track].Item1);
	}

	private void OnTrackLost(Track track)
	{
		_tracksDirty = true;
		(TrackVisual, GameEntity) tuple = _trackVisuals[track];
		_trackVisuals.Remove(track);
		SandBoxViewSubModule.VisualsOfEntities.Remove(tuple.Item2.Pointer);
		ReleaseEntity(tuple.Item2);
	}

	private void ParallelUpdateTrackColors(Track track)
	{
		(_trackVisuals[track].Item2.GetComponentAtIndex(0, GameEntity.ComponentType.Decal) as Decal).SetFactor1(Campaign.Current.Models.MapTrackModel.GetTrackColor(track));
	}

	private void ParallelUpdateTrackColors(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			ParallelUpdateTrackColors(MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks[i]);
		}
	}

	private void UpdateTrackMesh()
	{
		TWParallel.For(0, MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks.Count, _parallelUpdateVisibleTracksPredicate);
	}

	private void UpdateTrackPoolPosition(Track track)
	{
		MatrixFrame frame = CalculateTrackFrame(track);
		_trackVisuals[track].Item2.SetFrame(ref frame);
	}

	private void ParallelUpdateVisibleTracks(Track track)
	{
		_trackVisuals[track].Item2.SetVisibilityExcludeParents(visible: true);
		UpdateTrackPoolPosition(track);
	}

	private void ParallelUpdateVisibleTracks(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			ParallelUpdateVisibleTracks(MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks[i]);
		}
	}

	private bool RaySphereIntersection(Ray ray, SphereData sphere, ref Vec3 intersectionPoint)
	{
		Vec3 origin = sphere.Origin;
		float radius = sphere.Radius;
		Vec3 v = origin - ray.Origin;
		float num = Vec3.DotProduct(ray.Direction, v);
		if (num > 0f)
		{
			float num2 = radius * radius - (ray.Origin + ray.Direction * num - origin).LengthSquared;
			if (num2 >= 0f)
			{
				float num3 = TaleWorlds.Library.MathF.Sqrt(num2);
				float num4 = num - num3;
				if (num4 >= 0f && num4 <= ray.MaxDistance)
				{
					intersectionPoint = ray.Origin + ray.Direction * num4;
					return true;
				}
				if (num4 < 0f)
				{
					intersectionPoint = ray.Origin;
					return true;
				}
			}
		}
		else if ((ray.Origin - origin).LengthSquared < radius * radius)
		{
			intersectionPoint = ray.Origin;
			return true;
		}
		return false;
	}

	private Track GetTrackOnMouse(Ray mouseRay, Vec3 mouseIntersectionPoint)
	{
		Track result = null;
		for (int i = 0; i < MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks.Count; i++)
		{
			Track track = MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks[i];
			float trackScale = Campaign.Current.Models.MapTrackModel.GetTrackScale(track);
			MatrixFrame matrixFrame = CalculateTrackFrame(track);
			float lengthSquared = (matrixFrame.origin - mouseIntersectionPoint).LengthSquared;
			if (lengthSquared < 0.1f)
			{
				float num = TaleWorlds.Library.MathF.Sqrt(lengthSquared);
				_trackSphere.Origin = matrixFrame.origin;
				_trackSphere.Radius = 0.05f + num * 0.01f + trackScale;
				Vec3 intersectionPoint = default(Vec3);
				if (RaySphereIntersection(mouseRay, _trackSphere, ref intersectionPoint))
				{
					result = track;
					break;
				}
			}
		}
		return result;
	}

	private MatrixFrame CalculateTrackFrame(Track track)
	{
		Vec3 origin = track.Position.AsVec3();
		float scale = track.Scale;
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = origin;
		Campaign.Current.MapSceneWrapper.GetTerrainHeightAndNormal(identity.origin.AsVec2, out var _, out var normal);
		identity.rotation.u = normal;
		Vec2 asVec = identity.rotation.f.AsVec2;
		asVec.RotateCCW(track.Direction);
		identity.rotation.f = new Vec3(asVec.x, asVec.y, identity.rotation.f.z);
		identity.rotation.s = Vec3.CrossProduct(identity.rotation.f, identity.rotation.u);
		identity.rotation.s.Normalize();
		identity.rotation.f = Vec3.CrossProduct(identity.rotation.u, identity.rotation.s);
		identity.rotation.f.Normalize();
		float num = scale;
		identity.rotation.s *= num;
		identity.rotation.f *= num;
		identity.rotation.u *= num;
		return identity;
	}

	private GameEntity GetGameEntity()
	{
		Stack<GameEntity> entityPool = _entityPool;
		if (entityPool.Count != 0)
		{
			return entityPool.Pop();
		}
		GameEntity gameEntity = GameEntity.Instantiate(base.MapScene, "map_track_arrow", MatrixFrame.Identity);
		gameEntity.SetVisibilityExcludeParents(visible: false);
		return gameEntity;
	}

	private void PopulateEntityPool()
	{
		for (int i = 0; i < 256; i++)
		{
			GameEntity gameEntity = GameEntity.Instantiate(base.MapScene, "map_track_arrow", MatrixFrame.Identity);
			gameEntity.SetVisibilityExcludeParents(visible: false);
			_entityPool.Push(gameEntity);
		}
	}

	private void ReleaseEntity(GameEntity e)
	{
		e.SetVisibilityExcludeParents(visible: false);
		if (_entityPool == null)
		{
			_entityPool = new Stack<GameEntity>();
		}
		_entityPool.Push(e);
	}
}
