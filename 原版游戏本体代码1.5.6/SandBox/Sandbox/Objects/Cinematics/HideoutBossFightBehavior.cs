using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.Objects.Cinematics;

public class HideoutBossFightBehavior : ScriptComponentBehavior
{
	private readonly struct HideoutBossFightPreviewEntityInfo
	{
		public readonly GameEntity BaseEntity;

		public readonly GameEntity InitialEntity;

		public readonly GameEntity TargetEntity;

		public static HideoutBossFightPreviewEntityInfo Invalid => new HideoutBossFightPreviewEntityInfo(null, null, null);

		public bool IsValid => BaseEntity == null;

		public HideoutBossFightPreviewEntityInfo(GameEntity baseEntity, GameEntity initialEntity, GameEntity targetEntity)
		{
			BaseEntity = baseEntity;
			InitialEntity = initialEntity;
			TargetEntity = targetEntity;
		}
	}

	private enum HideoutSeedPerturbOffset
	{
		Player,
		Boss,
		Ally,
		Bandit
	}

	private const int PreviewPerturbSeed = 0;

	private const float PreviewPerturbAmount = 0.25f;

	private const int PreviewTroopCount = 10;

	private const float PreviewPlacementAngle = System.MathF.PI / 20f;

	private const string InitialFrameTag = "initial_frame";

	private const string TargetFrameTag = "target_frame";

	private const string BossPreviewPrefab = "hideout_boss_fight_preview_boss";

	private const string PlayerPreviewPrefab = "hideout_boss_fight_preview_player";

	private const string AllyPreviewPrefab = "hideout_boss_fight_preview_ally";

	private const string BanditPreviewPrefab = "hideout_boss_fight_preview_bandit";

	private const string PreviewCameraPrefab = "hideout_boss_fight_camera_preview";

	public const float MaxCameraHeight = 5f;

	public const float MaxCameraWidth = 10f;

	public float InnerRadius = 2.5f;

	public float OuterRadius = 6f;

	public float WalkDistance = 3f;

	public bool ShowPreview;

	private int _perturbSeed;

	private Random _perturbRng = new Random(0);

	private MatrixFrame _previousEntityFrame = MatrixFrame.Identity;

	private GameEntity _previewEntities;

	private List<HideoutBossFightPreviewEntityInfo> _previewAllies = new List<HideoutBossFightPreviewEntityInfo>();

	private List<HideoutBossFightPreviewEntityInfo> _previewBandits = new List<HideoutBossFightPreviewEntityInfo>();

	private HideoutBossFightPreviewEntityInfo _previewBoss = HideoutBossFightPreviewEntityInfo.Invalid;

	private HideoutBossFightPreviewEntityInfo _previewPlayer = HideoutBossFightPreviewEntityInfo.Invalid;

	private GameEntity _previewCamera;

	public int PerturbSeed
	{
		get
		{
			return _perturbSeed;
		}
		private set
		{
			_perturbSeed = value;
			ReSeedPerturbRng();
		}
	}

	public void GetPlayerFrames(out MatrixFrame initialFrame, out MatrixFrame targetFrame, float perturbAmount = 0f)
	{
		ReSeedPerturbRng();
		ComputePerturbedSpawnOffset(perturbAmount, out var perturbVector);
		ComputeSpawnWorldFrame(System.MathF.PI, InnerRadius, perturbVector - WalkDistance * Vec3.Forward, out initialFrame);
		ComputeSpawnWorldFrame(System.MathF.PI, InnerRadius, in perturbVector, out targetFrame);
	}

	public void GetBossFrames(out MatrixFrame initialFrame, out MatrixFrame targetFrame, float perturbAmount = 0f)
	{
		ReSeedPerturbRng(1);
		ComputePerturbedSpawnOffset(perturbAmount, out var perturbVector);
		ComputeSpawnWorldFrame(0f, InnerRadius, perturbVector + WalkDistance * Vec3.Forward, out initialFrame);
		ComputeSpawnWorldFrame(0f, InnerRadius, in perturbVector, out targetFrame);
	}

	public void GetAllyFrames(out List<MatrixFrame> initialFrames, out List<MatrixFrame> targetFrames, int agentCount = 10, float agentOffsetAngle = System.MathF.PI / 20f, float perturbAmount = 0f)
	{
		ReSeedPerturbRng(2);
		initialFrames = ComputeSpawnWorldFrames(agentCount, OuterRadius, (0f - WalkDistance) * Vec3.Forward, System.MathF.PI, agentOffsetAngle, perturbAmount).ToList();
		ReSeedPerturbRng(2);
		targetFrames = ComputeSpawnWorldFrames(agentCount, OuterRadius, Vec3.Zero, System.MathF.PI, agentOffsetAngle, perturbAmount).ToList();
	}

	public void GetBanditFrames(out List<MatrixFrame> initialFrames, out List<MatrixFrame> targetFrames, int agentCount = 10, float agentOffsetAngle = System.MathF.PI / 20f, float perturbAmount = 0f)
	{
		ReSeedPerturbRng(3);
		initialFrames = ComputeSpawnWorldFrames(agentCount, OuterRadius, WalkDistance * Vec3.Forward, 0f, agentOffsetAngle, perturbAmount).ToList();
		ReSeedPerturbRng(3);
		targetFrames = ComputeSpawnWorldFrames(agentCount, OuterRadius, Vec3.Zero, 0f, agentOffsetAngle, perturbAmount).ToList();
	}

	public void GetAlliesInitialFrame(out MatrixFrame frame)
	{
		ComputeSpawnWorldFrame(System.MathF.PI, OuterRadius, (0f - WalkDistance) * Vec3.Forward, out frame);
	}

	public void GetBanditsInitialFrame(out MatrixFrame frame)
	{
		ComputeSpawnWorldFrame(0f, OuterRadius, WalkDistance * Vec3.Forward, out frame);
	}

	public bool IsWorldPointInsideCameraVolume(in Vec3 worldPoint)
	{
		return IsLocalPointInsideCameraVolume(base.GameEntity.GetGlobalFrame().TransformToLocal(in worldPoint));
	}

	public bool ClampWorldPointToCameraVolume(in Vec3 worldPoint, out Vec3 clampedPoint)
	{
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		Vec3 localPoint = globalFrame.TransformToLocal(in worldPoint);
		bool num = IsLocalPointInsideCameraVolume(in localPoint);
		if (num)
		{
			clampedPoint = worldPoint;
			return num;
		}
		float num2 = 5f;
		float num3 = OuterRadius + WalkDistance;
		localPoint.x = TaleWorlds.Library.MathF.Clamp(localPoint.x, 0f - num2, num2);
		localPoint.y = TaleWorlds.Library.MathF.Clamp(localPoint.y, 0f - num3, num3);
		localPoint.z = TaleWorlds.Library.MathF.Clamp(localPoint.z, 0f, 5f);
		clampedPoint = globalFrame.TransformToParent(in localPoint);
		return num;
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (variableName == "ShowPreview")
		{
			UpdatePreview();
			TogglePreviewVisibility(ShowPreview);
		}
		else if (ShowPreview && (variableName == "InnerRadius" || variableName == "OuterRadius" || variableName == "WalkDistance"))
		{
			UpdatePreview();
		}
	}

	protected override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		if (ShowPreview)
		{
			MatrixFrame frame = base.GameEntity.GetFrame();
			if (!_previousEntityFrame.origin.NearlyEquals(in frame.origin) || !_previousEntityFrame.rotation.NearlyEquals(in frame.rotation))
			{
				_previousEntityFrame = frame;
				UpdatePreview();
			}
		}
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		RemovePreview();
	}

	private void UpdatePreview()
	{
		if (_previewEntities == null)
		{
			GeneratePreview();
		}
		_previewEntities.SetGlobalFrame(base.GameEntity.GetGlobalFrame());
		MatrixFrame initialFrame = MatrixFrame.Identity;
		MatrixFrame targetFrame = MatrixFrame.Identity;
		GetPlayerFrames(out initialFrame, out targetFrame, 0.25f);
		_previewPlayer.InitialEntity.SetGlobalFrame(in initialFrame);
		_previewPlayer.TargetEntity.SetGlobalFrame(in targetFrame);
		GetAllyFrames(out var initialFrames, out var targetFrames, 10, System.MathF.PI / 20f, 0.25f);
		int num = 0;
		foreach (HideoutBossFightPreviewEntityInfo previewAlly in _previewAllies)
		{
			previewAlly.InitialEntity.SetGlobalFrame(initialFrames[num]);
			previewAlly.TargetEntity.SetGlobalFrame(targetFrames[num]);
			num++;
		}
		GetBossFrames(out initialFrame, out targetFrame, 0.25f);
		_previewBoss.InitialEntity.SetGlobalFrame(in initialFrame);
		_previewBoss.TargetEntity.SetGlobalFrame(in targetFrame);
		GetBanditFrames(out var initialFrames2, out var targetFrames2, 10, System.MathF.PI / 20f, 0.25f);
		int num2 = 0;
		foreach (HideoutBossFightPreviewEntityInfo previewBandit in _previewBandits)
		{
			previewBandit.InitialEntity.SetGlobalFrame(initialFrames2[num2]);
			previewBandit.TargetEntity.SetGlobalFrame(targetFrames2[num2]);
			num2++;
		}
		MatrixFrame frame = _previewCamera.GetFrame();
		Vec3 scaleVector = frame.rotation.GetScaleVector();
		Vec3 vec = Vec3.Forward * (OuterRadius + WalkDistance) + Vec3.Side * 5f + Vec3.Up * 5f;
		Vec3 scaleAmountXYZ = new Vec3(vec.x / scaleVector.x, vec.y / scaleVector.y, vec.z / scaleVector.z);
		frame.rotation.ApplyScaleLocal(in scaleAmountXYZ);
		_previewCamera.SetFrame(ref frame);
	}

	private void GeneratePreview()
	{
		Scene scene = base.GameEntity.Scene;
		_previewEntities = TaleWorlds.Engine.GameEntity.CreateEmpty(scene, isModifiableFromEditor: false);
		_previewEntities.EntityFlags |= EntityFlags.DontSaveToScene;
		MatrixFrame frame = MatrixFrame.Identity;
		_previewEntities.SetFrame(ref frame);
		MatrixFrame globalFrame = _previewEntities.GetGlobalFrame();
		GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(scene, "hideout_boss_fight_preview_boss", globalFrame);
		_previewEntities.AddChild(gameEntity);
		ReadPrefabEntity(gameEntity, out var initialEntity, out var targetEntity);
		_previewBoss = new HideoutBossFightPreviewEntityInfo(gameEntity, initialEntity, targetEntity);
		GameEntity gameEntity2 = TaleWorlds.Engine.GameEntity.Instantiate(scene, "hideout_boss_fight_preview_player", globalFrame);
		_previewEntities.AddChild(gameEntity2);
		ReadPrefabEntity(gameEntity2, out var initialEntity2, out var targetEntity2);
		_previewPlayer = new HideoutBossFightPreviewEntityInfo(gameEntity2, initialEntity2, targetEntity2);
		for (int i = 0; i < 10; i++)
		{
			GameEntity gameEntity3 = TaleWorlds.Engine.GameEntity.Instantiate(scene, "hideout_boss_fight_preview_ally", globalFrame);
			_previewEntities.AddChild(gameEntity3);
			ReadPrefabEntity(gameEntity3, out var initialEntity3, out var targetEntity3);
			_previewAllies.Add(new HideoutBossFightPreviewEntityInfo(gameEntity3, initialEntity3, targetEntity3));
		}
		for (int j = 0; j < 10; j++)
		{
			GameEntity gameEntity4 = TaleWorlds.Engine.GameEntity.Instantiate(scene, "hideout_boss_fight_preview_bandit", globalFrame);
			_previewEntities.AddChild(gameEntity4);
			ReadPrefabEntity(gameEntity4, out var initialEntity4, out var targetEntity4);
			_previewBandits.Add(new HideoutBossFightPreviewEntityInfo(gameEntity4, initialEntity4, targetEntity4));
		}
		_previewCamera = TaleWorlds.Engine.GameEntity.Instantiate(scene, "hideout_boss_fight_camera_preview", globalFrame);
		_previewEntities.AddChild(_previewCamera);
	}

	private void RemovePreview()
	{
		if (_previewEntities != null)
		{
			_previewEntities.Remove(90);
		}
	}

	private void TogglePreviewVisibility(bool value)
	{
		if (_previewEntities != null)
		{
			_previewEntities.SetVisibilityExcludeParents(value);
		}
	}

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

	private void FindRadialPlacementFrame(float angle, float radius, out MatrixFrame frame)
	{
		TaleWorlds.Library.MathF.SinCos(angle, out var sa, out var ca);
		Vec3 vec = ca * Vec3.Forward + sa * Vec3.Side;
		Vec3 o = radius * vec;
		frame = new MatrixFrame(Mat3.CreateMat3WithForward(((ca > 0f) ? (-1f) : 1f) * Vec3.Forward), in o);
	}

	private void SnapOnClosestCollider(ref MatrixFrame frameWs)
	{
		Scene scene = base.GameEntity.Scene;
		Vec3 origin = frameWs.origin;
		origin.z += 5f;
		Vec3 targetPoint = origin;
		float num = 500f;
		targetPoint.z -= num;
		if (scene.RayCastForClosestEntityOrTerrain(origin, targetPoint, out var collisionDistance))
		{
			frameWs.origin.z = origin.z - collisionDistance;
		}
	}

	private void ReSeedPerturbRng(int seedOffset = 0)
	{
		_perturbRng = new Random(_perturbSeed + seedOffset);
	}

	private void ComputeSpawnWorldFrame(float localAngle, float localRadius, in Vec3 localOffset, out MatrixFrame worldFrame)
	{
		FindRadialPlacementFrame(localAngle, localRadius, out var frame);
		frame.origin += localOffset;
		worldFrame = base.GameEntity.GetGlobalFrame().TransformToParent(in frame);
		SnapOnClosestCollider(ref worldFrame);
	}

	private IEnumerable<MatrixFrame> ComputeSpawnWorldFrames(int spawnCount, float localRadius, Vec3 localOffset, float localBaseAngle, float localOffsetAngle, float localPerturbAmount = 0f)
	{
		float[] localPlacementAngles = new float[2]
		{
			localBaseAngle + localOffsetAngle / 2f,
			localBaseAngle - localOffsetAngle / 2f
		};
		int angleIndex = 0;
		MatrixFrame worldFrame = MatrixFrame.Identity;
		Vec3 perturbVector = Vec3.Zero;
		for (int i = 0; i < spawnCount; i++)
		{
			ComputePerturbedSpawnOffset(localPerturbAmount, out perturbVector);
			ComputeSpawnWorldFrame(localPlacementAngles[angleIndex], localRadius, perturbVector + localOffset, out worldFrame);
			yield return worldFrame;
			localPlacementAngles[angleIndex] += (float)((angleIndex == 0) ? 1 : (-1)) * localOffsetAngle;
			angleIndex = (angleIndex + 1) % 2;
		}
	}

	private void ComputePerturbedSpawnOffset(float perturbAmount, out Vec3 perturbVector)
	{
		perturbVector = Vec3.Zero;
		perturbAmount = TaleWorlds.Library.MathF.Abs(perturbAmount);
		if (perturbAmount > 1E-05f)
		{
			TaleWorlds.Library.MathF.SinCos(System.MathF.PI * 2f * _perturbRng.NextFloat(), out var sa, out var ca);
			perturbVector.x = perturbAmount * ca;
			perturbVector.y = perturbAmount * sa;
		}
	}

	private bool IsLocalPointInsideCameraVolume(in Vec3 localPoint)
	{
		float num = 5f;
		float num2 = OuterRadius + WalkDistance;
		if (localPoint.x >= 0f - num && localPoint.x <= num && localPoint.y >= 0f - num2 && localPoint.y <= num2 && localPoint.z >= 0f)
		{
			return localPoint.z <= 5f;
		}
		return false;
	}
}
