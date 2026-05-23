using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class CaptureTheFlagCapturePoint
{
	public float Progress { get; set; }

	public CaptureTheFlagFlagDirection Direction { get; set; }

	public float Speed { get; set; }

	public MatrixFrame InitialFlagFrame { get; private set; }

	public GameEntity FlagEntity { get; private set; }

	public SynchedMissionObject FlagHolder { get; private set; }

	public GameEntity FlagBottomBoundary { get; private set; }

	public GameEntity FlagTopBoundary { get; private set; }

	public BattleSideEnum BattleSide { get; }

	public int Index { get; }

	public bool UpdateFlag { get; set; }

	public CaptureTheFlagCapturePoint(GameEntity flagPole, BattleSideEnum battleSide, int index)
	{
		Reset();
		BattleSide = battleSide;
		Index = index;
		FlagHolder = flagPole.CollectChildrenEntitiesWithTag("score_stand").SingleOrDefault().GetFirstScriptOfType<SynchedMissionObject>();
		FlagEntity = GameEntity.CreateFromWeakEntity(FlagHolder.GameEntity.GetChildren().Single((WeakGameEntity q) => q.HasTag("flag")));
		FlagHolder.GameEntity.SetEntityFlags(FlagHolder.GameEntity.EntityFlags | EntityFlags.NoOcclusionCulling);
		FlagEntity.EntityFlags |= EntityFlags.NoOcclusionCulling;
		FlagBottomBoundary = flagPole.GetChildren().Single((GameEntity q) => q.HasTag("flag_raising_bottom"));
		FlagTopBoundary = flagPole.GetChildren().Single((GameEntity q) => q.HasTag("flag_raising_top"));
		MatrixFrame globalFrame = FlagHolder.GameEntity.GetGlobalFrame();
		globalFrame.origin.z = FlagBottomBoundary.GetGlobalFrame().origin.z;
		InitialFlagFrame = globalFrame;
	}

	public void Reset()
	{
		Progress = 0f;
		Direction = CaptureTheFlagFlagDirection.None;
		Speed = 0f;
		UpdateFlag = false;
	}
}
