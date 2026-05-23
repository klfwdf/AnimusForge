using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.View.Map.Managers
{
	// Token: 0x02000073 RID: 115
	public class MapTracksVisualManager : EntityVisualManagerBase<Track>
	{
		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x060004E9 RID: 1257 RVA: 0x00025DDA File Offset: 0x00023FDA
		public static MapTracksVisualManager Current
		{
			get
			{
				return SandBoxViewSubModule.SandBoxViewVisualManager.GetEntityComponent<MapTracksVisualManager>();
			}
		}

		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x060004EA RID: 1258 RVA: 0x00025DE6 File Offset: 0x00023FE6
		public override int Priority
		{
			get
			{
				return 50;
			}
		}

		// Token: 0x060004EB RID: 1259 RVA: 0x00025DEC File Offset: 0x00023FEC
		public MapTracksVisualManager()
		{
			this._trackVisuals = new Dictionary<Track, ValueTuple<TrackVisual, GameEntity>>();
			this._entityPool = new Stack<GameEntity>();
			this.PopulateEntityPool();
			this._parallelUpdateTrackColorsPredicate = new TWParallel.ParallelForAuxPredicate(this.ParallelUpdateTrackColors);
			this._parallelUpdateVisibleTracksPredicate = new TWParallel.ParallelForAuxPredicate(this.ParallelUpdateVisibleTracks);
		}

		// Token: 0x060004EC RID: 1260 RVA: 0x00025E46 File Offset: 0x00024046
		public override void OnVisualTick(MapScreen screen, float realDt, float dt)
		{
			if (this._tracksDirty)
			{
				this.UpdateTrackMesh();
				this._tracksDirty = false;
			}
			TWParallel.For(0, MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks.Count, this._parallelUpdateTrackColorsPredicate, 16);
		}

		// Token: 0x060004ED RID: 1261 RVA: 0x00025E7F File Offset: 0x0002407F
		public override bool OnVisualIntersected(Ray mouseRay, UIntPtr[] intersectedEntityIDs, Intersection[] intersectionInfos, int entityCount, Vec3 worldMouseNear, Vec3 worldMouseFar, Vec3 terrainIntersectionPoint, ref MapEntityVisual hoveredVisual, ref MapEntityVisual selectedVisual)
		{
			if (hoveredVisual == null)
			{
				hoveredVisual = this.GetVisualOfEntity(this.GetTrackOnMouse(mouseRay, terrainIntersectionPoint));
			}
			return hoveredVisual != null;
		}

		// Token: 0x060004EE RID: 1262 RVA: 0x00025EA0 File Offset: 0x000240A0
		public override void OnGameLoadFinished()
		{
			base.OnGameLoadFinished();
			foreach (Track track in MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks)
			{
				this.OnTrackDetected(track);
			}
		}

		// Token: 0x060004EF RID: 1263 RVA: 0x00025F04 File Offset: 0x00024104
		public override MapEntityVisual<Track> GetVisualOfEntity(Track entity)
		{
			if (entity == null)
			{
				return null;
			}
			return this._trackVisuals[entity].Item1;
		}

		// Token: 0x060004F0 RID: 1264 RVA: 0x00025F1C File Offset: 0x0002411C
		protected override void OnFinalize()
		{
			base.OnFinalize();
			foreach (GameEntity gameEntity in this._entityPool.ToList<GameEntity>())
			{
				gameEntity.Remove(111);
			}
			this._entityPool.Clear();
			this._trackVisuals.Clear();
			CampaignEventDispatcher.Instance.RemoveListeners(this);
		}

		// Token: 0x060004F1 RID: 1265 RVA: 0x00025F9C File Offset: 0x0002419C
		protected override void OnInitialize()
		{
			base.OnInitialize();
			CampaignEvents.TrackDetectedEvent.AddNonSerializedListener(this, new Action<Track>(this.OnTrackDetected));
			CampaignEvents.TrackLostEvent.AddNonSerializedListener(this, new Action<Track>(this.OnTrackLost));
		}

		// Token: 0x060004F2 RID: 1266 RVA: 0x00025FD4 File Offset: 0x000241D4
		internal void ReleaseResources(Track track)
		{
			ValueTuple<TrackVisual, GameEntity> valueTuple;
			if (this._trackVisuals.TryGetValue(track, out valueTuple))
			{
				valueTuple.Item2.Remove(111);
			}
		}

		// Token: 0x060004F3 RID: 1267 RVA: 0x00026000 File Offset: 0x00024200
		private void OnTrackDetected(Track track)
		{
			this._tracksDirty = true;
			GameEntity gameEntity = this.GetGameEntity();
			gameEntity.SetVisibilityExcludeParents(true);
			this._trackVisuals.Add(track, new ValueTuple<TrackVisual, GameEntity>(new TrackVisual(track), gameEntity));
			SandBoxViewSubModule.VisualsOfEntities.Add(this._trackVisuals[track].Item2.Pointer, this._trackVisuals[track].Item1);
		}

		// Token: 0x060004F4 RID: 1268 RVA: 0x0002606C File Offset: 0x0002426C
		private void OnTrackLost(Track track)
		{
			this._tracksDirty = true;
			ValueTuple<TrackVisual, GameEntity> valueTuple = this._trackVisuals[track];
			this._trackVisuals.Remove(track);
			SandBoxViewSubModule.VisualsOfEntities.Remove(valueTuple.Item2.Pointer);
			this.ReleaseEntity(valueTuple.Item2);
		}

		// Token: 0x060004F5 RID: 1269 RVA: 0x000260BC File Offset: 0x000242BC
		private void ParallelUpdateTrackColors(Track track)
		{
			(this._trackVisuals[track].Item2.GetComponentAtIndex(0, GameEntity.ComponentType.Decal) as Decal).SetFactor1(Campaign.Current.Models.MapTrackModel.GetTrackColor(track));
		}

		// Token: 0x060004F6 RID: 1270 RVA: 0x000260F8 File Offset: 0x000242F8
		private void ParallelUpdateTrackColors(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				this.ParallelUpdateTrackColors(MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks[i]);
			}
		}

		// Token: 0x060004F7 RID: 1271 RVA: 0x0002612C File Offset: 0x0002432C
		private void UpdateTrackMesh()
		{
			TWParallel.For(0, MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks.Count, this._parallelUpdateVisibleTracksPredicate, 16);
		}

		// Token: 0x060004F8 RID: 1272 RVA: 0x00026150 File Offset: 0x00024350
		private void UpdateTrackPoolPosition(Track track)
		{
			MatrixFrame matrixFrame = this.CalculateTrackFrame(track);
			this._trackVisuals[track].Item2.SetFrame(ref matrixFrame, true);
		}

		// Token: 0x060004F9 RID: 1273 RVA: 0x0002617E File Offset: 0x0002437E
		private void ParallelUpdateVisibleTracks(Track track)
		{
			this._trackVisuals[track].Item2.SetVisibilityExcludeParents(true);
			this.UpdateTrackPoolPosition(track);
		}

		// Token: 0x060004FA RID: 1274 RVA: 0x000261A0 File Offset: 0x000243A0
		private void ParallelUpdateVisibleTracks(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				this.ParallelUpdateVisibleTracks(MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks[i]);
			}
		}

		// Token: 0x060004FB RID: 1275 RVA: 0x000261D4 File Offset: 0x000243D4
		private bool RaySphereIntersection(Ray ray, SphereData sphere, ref Vec3 intersectionPoint)
		{
			Vec3 origin = sphere.Origin;
			float radius = sphere.Radius;
			Vec3 v = origin - ray.Origin;
			float num = Vec3.DotProduct(ray.Direction, v);
			if (num > 0f)
			{
				Vec3 vec = ray.Origin + ray.Direction * num - origin;
				float num2 = radius * radius - vec.LengthSquared;
				if (num2 >= 0f)
				{
					float num3 = MathF.Sqrt(num2);
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

		// Token: 0x060004FC RID: 1276 RVA: 0x000262D8 File Offset: 0x000244D8
		private Track GetTrackOnMouse(Ray mouseRay, Vec3 mouseIntersectionPoint)
		{
			Track result = null;
			for (int i = 0; i < MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks.Count; i++)
			{
				Track track = MapScreen.Instance.MapTracksCampaignBehavior.DetectedTracks[i];
				float trackScale = Campaign.Current.Models.MapTrackModel.GetTrackScale(track);
				MatrixFrame matrixFrame = this.CalculateTrackFrame(track);
				float lengthSquared = (matrixFrame.origin - mouseIntersectionPoint).LengthSquared;
				if (lengthSquared < 0.1f)
				{
					float num = MathF.Sqrt(lengthSquared);
					this._trackSphere.Origin = matrixFrame.origin;
					this._trackSphere.Radius = 0.05f + num * 0.01f + trackScale;
					Vec3 vec = default(Vec3);
					if (this.RaySphereIntersection(mouseRay, this._trackSphere, ref vec))
					{
						result = track;
						break;
					}
				}
			}
			return result;
		}

		// Token: 0x060004FD RID: 1277 RVA: 0x000263B8 File Offset: 0x000245B8
		private MatrixFrame CalculateTrackFrame(Track track)
		{
			Vec3 origin = track.Position.AsVec3();
			float scale = track.Scale;
			MatrixFrame identity = MatrixFrame.Identity;
			identity.origin = origin;
			float num;
			Vec3 u;
			Campaign.Current.MapSceneWrapper.GetTerrainHeightAndNormal(identity.origin.AsVec2, out num, out u);
			identity.rotation.u = u;
			Vec2 asVec = identity.rotation.f.AsVec2;
			asVec.RotateCCW(track.Direction);
			identity.rotation.f = new Vec3(asVec.x, asVec.y, identity.rotation.f.z, -1f);
			identity.rotation.s = Vec3.CrossProduct(identity.rotation.f, identity.rotation.u);
			identity.rotation.s.Normalize();
			identity.rotation.f = Vec3.CrossProduct(identity.rotation.u, identity.rotation.s);
			identity.rotation.f.Normalize();
			float f = scale;
			identity.rotation.s = identity.rotation.s * f;
			identity.rotation.f = identity.rotation.f * f;
			identity.rotation.u = identity.rotation.u * f;
			return identity;
		}

		// Token: 0x060004FE RID: 1278 RVA: 0x00026534 File Offset: 0x00024734
		private GameEntity GetGameEntity()
		{
			Stack<GameEntity> entityPool = this._entityPool;
			if (entityPool.Count != 0)
			{
				return entityPool.Pop();
			}
			GameEntity gameEntity = GameEntity.Instantiate(base.MapScene, "map_track_arrow", MatrixFrame.Identity, true, "");
			gameEntity.SetVisibilityExcludeParents(false);
			return gameEntity;
		}

		// Token: 0x060004FF RID: 1279 RVA: 0x0002657C File Offset: 0x0002477C
		private void PopulateEntityPool()
		{
			for (int i = 0; i < 256; i++)
			{
				GameEntity gameEntity = GameEntity.Instantiate(base.MapScene, "map_track_arrow", MatrixFrame.Identity, true, "");
				gameEntity.SetVisibilityExcludeParents(false);
				this._entityPool.Push(gameEntity);
			}
		}

		// Token: 0x06000500 RID: 1280 RVA: 0x000265C8 File Offset: 0x000247C8
		private void ReleaseEntity(GameEntity e)
		{
			e.SetVisibilityExcludeParents(false);
			if (this._entityPool == null)
			{
				this._entityPool = new Stack<GameEntity>();
			}
			this._entityPool.Push(e);
		}

		// Token: 0x04000230 RID: 560
		private const string TrackPrefabName = "map_track_arrow";

		// Token: 0x04000231 RID: 561
		private const int DefaultObjectPoolCount = 256;

		// Token: 0x04000232 RID: 562
		private Dictionary<Track, ValueTuple<TrackVisual, GameEntity>> _trackVisuals;

		// Token: 0x04000233 RID: 563
		private SphereData _trackSphere;

		// Token: 0x04000234 RID: 564
		private bool _tracksDirty = true;

		// Token: 0x04000235 RID: 565
		private readonly TWParallel.ParallelForAuxPredicate _parallelUpdateTrackColorsPredicate;

		// Token: 0x04000236 RID: 566
		private readonly TWParallel.ParallelForAuxPredicate _parallelUpdateVisibleTracksPredicate;

		// Token: 0x04000237 RID: 567
		private Stack<GameEntity> _entityPool;
	}
}
