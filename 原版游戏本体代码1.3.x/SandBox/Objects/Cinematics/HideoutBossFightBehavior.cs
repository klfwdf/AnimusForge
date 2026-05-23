using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.Objects.Cinematics
{
	// Token: 0x02000041 RID: 65
	public class HideoutBossFightBehavior : ScriptComponentBehavior
	{
		// Token: 0x17000042 RID: 66
		// (get) Token: 0x06000269 RID: 617 RVA: 0x0000E441 File Offset: 0x0000C641
		// (set) Token: 0x06000268 RID: 616 RVA: 0x0000E431 File Offset: 0x0000C631
		public int PerturbSeed
		{
			get
			{
				return this._perturbSeed;
			}
			private set
			{
				this._perturbSeed = value;
				this.ReSeedPerturbRng(0);
			}
		}

		// Token: 0x0600026A RID: 618 RVA: 0x0000E44C File Offset: 0x0000C64C
		public void GetPlayerFrames(out MatrixFrame initialFrame, out MatrixFrame targetFrame, float perturbAmount = 0f)
		{
			this.ReSeedPerturbRng(0);
			Vec3 v;
			this.ComputePerturbedSpawnOffset(perturbAmount, out v);
			float localAngle = 3.1415927f;
			float innerRadius = this.InnerRadius;
			Vec3 vec = v - this.WalkDistance * Vec3.Forward;
			this.ComputeSpawnWorldFrame(localAngle, innerRadius, vec, out initialFrame);
			this.ComputeSpawnWorldFrame(3.1415927f, this.InnerRadius, v, out targetFrame);
		}

		// Token: 0x0600026B RID: 619 RVA: 0x0000E4A8 File Offset: 0x0000C6A8
		public void GetBossFrames(out MatrixFrame initialFrame, out MatrixFrame targetFrame, float perturbAmount = 0f)
		{
			this.ReSeedPerturbRng(1);
			Vec3 v;
			this.ComputePerturbedSpawnOffset(perturbAmount, out v);
			float localAngle = 0f;
			float innerRadius = this.InnerRadius;
			Vec3 vec = v + this.WalkDistance * Vec3.Forward;
			this.ComputeSpawnWorldFrame(localAngle, innerRadius, vec, out initialFrame);
			this.ComputeSpawnWorldFrame(0f, this.InnerRadius, v, out targetFrame);
		}

		// Token: 0x0600026C RID: 620 RVA: 0x0000E504 File Offset: 0x0000C704
		public void GetAllyFrames(out List<MatrixFrame> initialFrames, out List<MatrixFrame> targetFrames, int agentCount = 10, float agentOffsetAngle = 0.15707964f, float perturbAmount = 0f)
		{
			this.ReSeedPerturbRng(2);
			initialFrames = this.ComputeSpawnWorldFrames(agentCount, this.OuterRadius, -this.WalkDistance * Vec3.Forward, 3.1415927f, agentOffsetAngle, perturbAmount).ToList<MatrixFrame>();
			this.ReSeedPerturbRng(2);
			targetFrames = this.ComputeSpawnWorldFrames(agentCount, this.OuterRadius, Vec3.Zero, 3.1415927f, agentOffsetAngle, perturbAmount).ToList<MatrixFrame>();
		}

		// Token: 0x0600026D RID: 621 RVA: 0x0000E570 File Offset: 0x0000C770
		public void GetBanditFrames(out List<MatrixFrame> initialFrames, out List<MatrixFrame> targetFrames, int agentCount = 10, float agentOffsetAngle = 0.15707964f, float perturbAmount = 0f)
		{
			this.ReSeedPerturbRng(3);
			initialFrames = this.ComputeSpawnWorldFrames(agentCount, this.OuterRadius, this.WalkDistance * Vec3.Forward, 0f, agentOffsetAngle, perturbAmount).ToList<MatrixFrame>();
			this.ReSeedPerturbRng(3);
			targetFrames = this.ComputeSpawnWorldFrames(agentCount, this.OuterRadius, Vec3.Zero, 0f, agentOffsetAngle, perturbAmount).ToList<MatrixFrame>();
		}

		// Token: 0x0600026E RID: 622 RVA: 0x0000E5DC File Offset: 0x0000C7DC
		public void GetAlliesInitialFrame(out MatrixFrame frame)
		{
			float localAngle = 3.1415927f;
			float outerRadius = this.OuterRadius;
			Vec3 vec = -this.WalkDistance * Vec3.Forward;
			this.ComputeSpawnWorldFrame(localAngle, outerRadius, vec, out frame);
		}

		// Token: 0x0600026F RID: 623 RVA: 0x0000E610 File Offset: 0x0000C810
		public void GetBanditsInitialFrame(out MatrixFrame frame)
		{
			float localAngle = 0f;
			float outerRadius = this.OuterRadius;
			Vec3 vec = this.WalkDistance * Vec3.Forward;
			this.ComputeSpawnWorldFrame(localAngle, outerRadius, vec, out frame);
		}

		// Token: 0x06000270 RID: 624 RVA: 0x0000E644 File Offset: 0x0000C844
		public bool IsWorldPointInsideCameraVolume(in Vec3 worldPoint)
		{
			Vec3 vec = base.GameEntity.GetGlobalFrame().TransformToLocal(worldPoint);
			return this.IsLocalPointInsideCameraVolume(vec);
		}

		// Token: 0x06000271 RID: 625 RVA: 0x0000E674 File Offset: 0x0000C874
		public bool ClampWorldPointToCameraVolume(in Vec3 worldPoint, out Vec3 clampedPoint)
		{
			MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
			Vec3 vec = globalFrame.TransformToLocal(worldPoint);
			bool flag = this.IsLocalPointInsideCameraVolume(vec);
			if (flag)
			{
				clampedPoint = worldPoint;
				return flag;
			}
			float num = 5f;
			float num2 = this.OuterRadius + this.WalkDistance;
			vec.x = MathF.Clamp(vec.x, -num, num);
			vec.y = MathF.Clamp(vec.y, -num2, num2);
			vec.z = MathF.Clamp(vec.z, 0f, 5f);
			clampedPoint = globalFrame.TransformToParent(vec);
			return flag;
		}

		// Token: 0x06000272 RID: 626 RVA: 0x0000E720 File Offset: 0x0000C920
		protected override void OnEditorVariableChanged(string variableName)
		{
			base.OnEditorVariableChanged(variableName);
			if (variableName == "ShowPreview")
			{
				this.UpdatePreview();
				this.TogglePreviewVisibility(this.ShowPreview);
				return;
			}
			if (this.ShowPreview && (variableName == "InnerRadius" || variableName == "OuterRadius" || variableName == "WalkDistance"))
			{
				this.UpdatePreview();
			}
		}

		// Token: 0x06000273 RID: 627 RVA: 0x0000E78C File Offset: 0x0000C98C
		protected override void OnEditorTick(float dt)
		{
			base.OnEditorTick(dt);
			if (this.ShowPreview)
			{
				MatrixFrame frame = base.GameEntity.GetFrame();
				if (!this._previousEntityFrame.origin.NearlyEquals(frame.origin, 1E-05f) || !this._previousEntityFrame.rotation.NearlyEquals(frame.rotation, 1E-05f))
				{
					this._previousEntityFrame = frame;
					this.UpdatePreview();
				}
			}
		}

		// Token: 0x06000274 RID: 628 RVA: 0x0000E806 File Offset: 0x0000CA06
		protected override void OnRemoved(int removeReason)
		{
			base.OnRemoved(removeReason);
			this.RemovePreview();
		}

		// Token: 0x06000275 RID: 629 RVA: 0x0000E818 File Offset: 0x0000CA18
		private void UpdatePreview()
		{
			if (this._previewEntities == null)
			{
				this.GeneratePreview();
			}
			GameEntity previewEntities = this._previewEntities;
			MatrixFrame matrixFrame = base.GameEntity.GetGlobalFrame();
			previewEntities.SetGlobalFrame(matrixFrame, true);
			MatrixFrame identity = MatrixFrame.Identity;
			MatrixFrame identity2 = MatrixFrame.Identity;
			this.GetPlayerFrames(out identity, out identity2, 0.25f);
			this._previewPlayer.InitialEntity.SetGlobalFrame(identity, true);
			this._previewPlayer.TargetEntity.SetGlobalFrame(identity2, true);
			List<MatrixFrame> list;
			List<MatrixFrame> list2;
			this.GetAllyFrames(out list, out list2, 10, 0.15707964f, 0.25f);
			int num = 0;
			foreach (HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo hideoutBossFightPreviewEntityInfo in this._previewAllies)
			{
				GameEntity initialEntity = hideoutBossFightPreviewEntityInfo.InitialEntity;
				matrixFrame = list[num];
				initialEntity.SetGlobalFrame(matrixFrame, true);
				GameEntity targetEntity = hideoutBossFightPreviewEntityInfo.TargetEntity;
				matrixFrame = list2[num];
				targetEntity.SetGlobalFrame(matrixFrame, true);
				num++;
			}
			this.GetBossFrames(out identity, out identity2, 0.25f);
			this._previewBoss.InitialEntity.SetGlobalFrame(identity, true);
			this._previewBoss.TargetEntity.SetGlobalFrame(identity2, true);
			List<MatrixFrame> list3;
			List<MatrixFrame> list4;
			this.GetBanditFrames(out list3, out list4, 10, 0.15707964f, 0.25f);
			int num2 = 0;
			foreach (HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo hideoutBossFightPreviewEntityInfo2 in this._previewBandits)
			{
				GameEntity initialEntity2 = hideoutBossFightPreviewEntityInfo2.InitialEntity;
				matrixFrame = list3[num2];
				initialEntity2.SetGlobalFrame(matrixFrame, true);
				GameEntity targetEntity2 = hideoutBossFightPreviewEntityInfo2.TargetEntity;
				matrixFrame = list4[num2];
				targetEntity2.SetGlobalFrame(matrixFrame, true);
				num2++;
			}
			MatrixFrame frame = this._previewCamera.GetFrame();
			Vec3 scaleVector = frame.rotation.GetScaleVector();
			Vec3 vec = Vec3.Forward * (this.OuterRadius + this.WalkDistance) + Vec3.Side * 5f + Vec3.Up * 5f;
			Vec3 vec2 = new Vec3(vec.x / scaleVector.x, vec.y / scaleVector.y, vec.z / scaleVector.z, -1f);
			frame.rotation.ApplyScaleLocal(vec2);
			this._previewCamera.SetFrame(ref frame, true);
		}

		// Token: 0x06000276 RID: 630 RVA: 0x0000EA94 File Offset: 0x0000CC94
		private void GeneratePreview()
		{
			Scene scene = base.GameEntity.Scene;
			this._previewEntities = TaleWorlds.Engine.GameEntity.CreateEmpty(scene, false, true, true);
			this._previewEntities.EntityFlags |= EntityFlags.DontSaveToScene;
			MatrixFrame identity = MatrixFrame.Identity;
			this._previewEntities.SetFrame(ref identity, true);
			MatrixFrame globalFrame = this._previewEntities.GetGlobalFrame();
			GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(scene, "hideout_boss_fight_preview_boss", globalFrame, true, "");
			this._previewEntities.AddChild(gameEntity, false);
			GameEntity initialEntity;
			GameEntity targetEntity;
			this.ReadPrefabEntity(gameEntity, out initialEntity, out targetEntity);
			this._previewBoss = new HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo(gameEntity, initialEntity, targetEntity);
			GameEntity gameEntity2 = TaleWorlds.Engine.GameEntity.Instantiate(scene, "hideout_boss_fight_preview_player", globalFrame, true, "");
			this._previewEntities.AddChild(gameEntity2, false);
			GameEntity initialEntity2;
			GameEntity targetEntity2;
			this.ReadPrefabEntity(gameEntity2, out initialEntity2, out targetEntity2);
			this._previewPlayer = new HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo(gameEntity2, initialEntity2, targetEntity2);
			for (int i = 0; i < 10; i++)
			{
				GameEntity gameEntity3 = TaleWorlds.Engine.GameEntity.Instantiate(scene, "hideout_boss_fight_preview_ally", globalFrame, true, "");
				this._previewEntities.AddChild(gameEntity3, false);
				GameEntity initialEntity3;
				GameEntity targetEntity3;
				this.ReadPrefabEntity(gameEntity3, out initialEntity3, out targetEntity3);
				this._previewAllies.Add(new HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo(gameEntity3, initialEntity3, targetEntity3));
			}
			for (int j = 0; j < 10; j++)
			{
				GameEntity gameEntity4 = TaleWorlds.Engine.GameEntity.Instantiate(scene, "hideout_boss_fight_preview_bandit", globalFrame, true, "");
				this._previewEntities.AddChild(gameEntity4, false);
				GameEntity initialEntity4;
				GameEntity targetEntity4;
				this.ReadPrefabEntity(gameEntity4, out initialEntity4, out targetEntity4);
				this._previewBandits.Add(new HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo(gameEntity4, initialEntity4, targetEntity4));
			}
			this._previewCamera = TaleWorlds.Engine.GameEntity.Instantiate(scene, "hideout_boss_fight_camera_preview", globalFrame, true, "");
			this._previewEntities.AddChild(this._previewCamera, false);
		}

		// Token: 0x06000277 RID: 631 RVA: 0x0000EC48 File Offset: 0x0000CE48
		private void RemovePreview()
		{
			if (this._previewEntities != null)
			{
				this._previewEntities.Remove(90);
			}
		}

		// Token: 0x06000278 RID: 632 RVA: 0x0000EC65 File Offset: 0x0000CE65
		private void TogglePreviewVisibility(bool value)
		{
			if (this._previewEntities != null)
			{
				this._previewEntities.SetVisibilityExcludeParents(value);
			}
		}

		// Token: 0x06000279 RID: 633 RVA: 0x0000EC84 File Offset: 0x0000CE84
		private void ReadPrefabEntity(GameEntity entity, out GameEntity initialEntity, out GameEntity targetEntity)
		{
			GameEntity firstChildEntityWithTag = entity.GetFirstChildEntityWithTag("initial_frame");
			if (firstChildEntityWithTag == null)
			{
				Debug.FailedAssert("Prefab entity " + entity.Name + " is not a spawn prefab with an initial frame entity", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Objects\\Cinematics\\HideoutBossFightBehavior.cs", "ReadPrefabEntity", 389);
			}
			GameEntity firstChildEntityWithTag2 = entity.GetFirstChildEntityWithTag("target_frame");
			if (firstChildEntityWithTag2 == null)
			{
				Debug.FailedAssert("Prefab entity " + entity.Name + " is not a spawn prefab with an target frame entity", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Objects\\Cinematics\\HideoutBossFightBehavior.cs", "ReadPrefabEntity", 395);
			}
			initialEntity = firstChildEntityWithTag;
			targetEntity = firstChildEntityWithTag2;
		}

		// Token: 0x0600027A RID: 634 RVA: 0x0000ED14 File Offset: 0x0000CF14
		private void FindRadialPlacementFrame(float angle, float radius, out MatrixFrame frame)
		{
			float f;
			float num;
			MathF.SinCos(angle, out f, out num);
			Vec3 v = num * Vec3.Forward + f * Vec3.Side;
			Vec3 vec = radius * v;
			Vec3 vec2 = ((num > 0f) ? (-1f) : 1f) * Vec3.Forward;
			Mat3 mat = Mat3.CreateMat3WithForward(vec2);
			frame = new MatrixFrame(ref mat, ref vec);
		}

		// Token: 0x0600027B RID: 635 RVA: 0x0000ED88 File Offset: 0x0000CF88
		private void SnapOnClosestCollider(ref MatrixFrame frameWs)
		{
			Scene scene = base.GameEntity.Scene;
			Vec3 origin = frameWs.origin;
			origin.z += 5f;
			Vec3 targetPoint = origin;
			float num = 500f;
			targetPoint.z -= num;
			float num2;
			if (scene.RayCastForClosestEntityOrTerrain(origin, targetPoint, out num2, 0.01f, BodyFlags.CommonFocusRayCastExcludeFlags))
			{
				frameWs.origin.z = origin.z - num2;
			}
		}

		// Token: 0x0600027C RID: 636 RVA: 0x0000EDF7 File Offset: 0x0000CFF7
		private void ReSeedPerturbRng(int seedOffset = 0)
		{
			this._perturbRng = new Random(this._perturbSeed + seedOffset);
		}

		// Token: 0x0600027D RID: 637 RVA: 0x0000EE0C File Offset: 0x0000D00C
		private void ComputeSpawnWorldFrame(float localAngle, float localRadius, in Vec3 localOffset, out MatrixFrame worldFrame)
		{
			MatrixFrame matrixFrame;
			this.FindRadialPlacementFrame(localAngle, localRadius, out matrixFrame);
			matrixFrame.origin += localOffset;
			worldFrame = base.GameEntity.GetGlobalFrame().TransformToParent(matrixFrame);
			this.SnapOnClosestCollider(ref worldFrame);
		}

		// Token: 0x0600027E RID: 638 RVA: 0x0000EE67 File Offset: 0x0000D067
		private IEnumerable<MatrixFrame> ComputeSpawnWorldFrames(int spawnCount, float localRadius, Vec3 localOffset, float localBaseAngle, float localOffsetAngle, float localPerturbAmount = 0f)
		{
			float[] localPlacementAngles = new float[]
			{
				localBaseAngle + localOffsetAngle / 2f,
				localBaseAngle - localOffsetAngle / 2f
			};
			int angleIndex = 0;
			MatrixFrame identity = MatrixFrame.Identity;
			Vec3 zero = Vec3.Zero;
			int num;
			for (int i = 0; i < spawnCount; i = num + 1)
			{
				this.ComputePerturbedSpawnOffset(localPerturbAmount, out zero);
				float localAngle = localPlacementAngles[angleIndex];
				Vec3 vec = zero + localOffset;
				this.ComputeSpawnWorldFrame(localAngle, localRadius, vec, out identity);
				yield return identity;
				localPlacementAngles[angleIndex] += (float)((angleIndex == 0) ? 1 : (-1)) * localOffsetAngle;
				angleIndex = (angleIndex + 1) % 2;
				num = i;
			}
			yield break;
		}

		// Token: 0x0600027F RID: 639 RVA: 0x0000EEA4 File Offset: 0x0000D0A4
		private void ComputePerturbedSpawnOffset(float perturbAmount, out Vec3 perturbVector)
		{
			perturbVector = Vec3.Zero;
			perturbAmount = MathF.Abs(perturbAmount);
			if (perturbAmount > 1E-05f)
			{
				float num;
				float num2;
				MathF.SinCos(6.2831855f * this._perturbRng.NextFloat(), out num, out num2);
				perturbVector.x = perturbAmount * num2;
				perturbVector.y = perturbAmount * num;
			}
		}

		// Token: 0x06000280 RID: 640 RVA: 0x0000EEF8 File Offset: 0x0000D0F8
		private bool IsLocalPointInsideCameraVolume(in Vec3 localPoint)
		{
			float num = 5f;
			float num2 = this.OuterRadius + this.WalkDistance;
			return localPoint.x >= -num && localPoint.x <= num && localPoint.y >= -num2 && localPoint.y <= num2 && localPoint.z >= 0f && localPoint.z <= 5f;
		}

		// Token: 0x040000FD RID: 253
		private const int PreviewPerturbSeed = 0;

		// Token: 0x040000FE RID: 254
		private const float PreviewPerturbAmount = 0.25f;

		// Token: 0x040000FF RID: 255
		private const int PreviewTroopCount = 10;

		// Token: 0x04000100 RID: 256
		private const float PreviewPlacementAngle = 0.15707964f;

		// Token: 0x04000101 RID: 257
		private const string InitialFrameTag = "initial_frame";

		// Token: 0x04000102 RID: 258
		private const string TargetFrameTag = "target_frame";

		// Token: 0x04000103 RID: 259
		private const string BossPreviewPrefab = "hideout_boss_fight_preview_boss";

		// Token: 0x04000104 RID: 260
		private const string PlayerPreviewPrefab = "hideout_boss_fight_preview_player";

		// Token: 0x04000105 RID: 261
		private const string AllyPreviewPrefab = "hideout_boss_fight_preview_ally";

		// Token: 0x04000106 RID: 262
		private const string BanditPreviewPrefab = "hideout_boss_fight_preview_bandit";

		// Token: 0x04000107 RID: 263
		private const string PreviewCameraPrefab = "hideout_boss_fight_camera_preview";

		// Token: 0x04000108 RID: 264
		public const float MaxCameraHeight = 5f;

		// Token: 0x04000109 RID: 265
		public const float MaxCameraWidth = 10f;

		// Token: 0x0400010A RID: 266
		public float InnerRadius = 2.5f;

		// Token: 0x0400010B RID: 267
		public float OuterRadius = 6f;

		// Token: 0x0400010C RID: 268
		public float WalkDistance = 3f;

		// Token: 0x0400010D RID: 269
		public bool ShowPreview;

		// Token: 0x0400010E RID: 270
		private int _perturbSeed;

		// Token: 0x0400010F RID: 271
		private Random _perturbRng = new Random(0);

		// Token: 0x04000110 RID: 272
		private MatrixFrame _previousEntityFrame = MatrixFrame.Identity;

		// Token: 0x04000111 RID: 273
		private GameEntity _previewEntities;

		// Token: 0x04000112 RID: 274
		private List<HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo> _previewAllies = new List<HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo>();

		// Token: 0x04000113 RID: 275
		private List<HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo> _previewBandits = new List<HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo>();

		// Token: 0x04000114 RID: 276
		private HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo _previewBoss = HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo.Invalid;

		// Token: 0x04000115 RID: 277
		private HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo _previewPlayer = HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo.Invalid;

		// Token: 0x04000116 RID: 278
		private GameEntity _previewCamera;

		// Token: 0x02000147 RID: 327
		private readonly struct HideoutBossFightPreviewEntityInfo
		{
			// Token: 0x1700011D RID: 285
			// (get) Token: 0x06000DF3 RID: 3571 RVA: 0x00063F61 File Offset: 0x00062161
			public static HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo Invalid
			{
				get
				{
					return new HideoutBossFightBehavior.HideoutBossFightPreviewEntityInfo(null, null, null);
				}
			}

			// Token: 0x1700011E RID: 286
			// (get) Token: 0x06000DF4 RID: 3572 RVA: 0x00063F6B File Offset: 0x0006216B
			public bool IsValid
			{
				get
				{
					return this.BaseEntity == null;
				}
			}

			// Token: 0x06000DF5 RID: 3573 RVA: 0x00063F79 File Offset: 0x00062179
			public HideoutBossFightPreviewEntityInfo(GameEntity baseEntity, GameEntity initialEntity, GameEntity targetEntity)
			{
				this.BaseEntity = baseEntity;
				this.InitialEntity = initialEntity;
				this.TargetEntity = targetEntity;
			}

			// Token: 0x0400066C RID: 1644
			public readonly GameEntity BaseEntity;

			// Token: 0x0400066D RID: 1645
			public readonly GameEntity InitialEntity;

			// Token: 0x0400066E RID: 1646
			public readonly GameEntity TargetEntity;
		}

		// Token: 0x02000148 RID: 328
		private enum HideoutSeedPerturbOffset
		{
			// Token: 0x04000670 RID: 1648
			Player,
			// Token: 0x04000671 RID: 1649
			Boss,
			// Token: 0x04000672 RID: 1650
			Ally,
			// Token: 0x04000673 RID: 1651
			Bandit
		}
	}
}
