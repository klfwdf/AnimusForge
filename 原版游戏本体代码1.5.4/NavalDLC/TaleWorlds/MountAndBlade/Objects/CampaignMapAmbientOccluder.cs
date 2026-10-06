using System.Collections.Generic;
using NavalDLC;
using NavalDLC.Map;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Objects;

internal class CampaignMapAmbientOccluder : ScriptComponentBehavior
{
	private const int MaximumSpecialStormNumber = 2;

	private readonly List<GameEntity> _questStorms = new List<GameEntity>();

	private int MaximumNumberOfStorms => NavalDLCManager.Instance.GameModels.MapStormModel.MaximumNumberOfStorms + 2;

	protected override void OnInit()
	{
		Mesh firstMesh = base.GameEntity.GetFirstMesh();
		int num = MathF.Max(MaximumNumberOfStorms, 16);
		firstMesh.SetupAdditionalBoneBuffer(num);
		for (int i = 0; i < num; i++)
		{
			firstMesh.SetAdditionalBoneFrame(i, MatrixFrame.Zero);
		}
	}

	protected override void OnTick(float dt)
	{
		int i = 0;
		foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
		{
			SetBoneFrame(spawnedStorm.CurrentPosition.ToVec3(), base.GameEntity, i++);
		}
		foreach (GameEntity questStorm in _questStorms)
		{
			SetBoneFrame(questStorm.GlobalPosition, base.GameEntity, i++);
		}
		for (int num = MathF.Max(MaximumNumberOfStorms, 16); i < num; i++)
		{
			MatrixFrame frame = MatrixFrame.Zero;
			base.GameEntity.SetBoneFrameToAllMeshes(i, in frame);
		}
	}

	protected override void OnEditorInit()
	{
		Mesh firstMesh = base.GameEntity.GetFirstMesh();
		int num = 16;
		firstMesh.SetupAdditionalBoneBuffer(num);
		for (int i = 0; i < num; i++)
		{
			firstMesh.SetAdditionalBoneFrame(i, MatrixFrame.Zero);
		}
	}

	protected override void OnEditorTick(float dt)
	{
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	public void RegisterQuestStorm(GameEntity stormEntity)
	{
		_questStorms.Add(stormEntity);
	}

	public void UnregisterQuestStorm(GameEntity stormEntity)
	{
		_questStorms.Remove(stormEntity);
	}

	private void SetBoneFrame(Vec3 origin, WeakGameEntity gameEntity, int boneIndex)
	{
		MatrixFrame frame = MatrixFrame.Identity;
		frame.Scale(new Vec3(60f));
		frame.origin = origin;
		base.GameEntity.SetBoneFrameToAllMeshes(boneIndex, in frame);
	}
}
