using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Missions.Deployment;

public class NavalFormationDeploymentPlan : IFormationDeploymentPlan
{
	private MatrixFrame _spawnFrame;

	private readonly FormationClass _class;

	private bool _hasFrame;

	private Mission _mission;

	public FormationClass Class => _class;

	public FormationClass SpawnClass => _class;

	public float PlannedWidth
	{
		get
		{
			if (ShipObject == null)
			{
				return 0f;
			}
			return ShipObject.DeploymentArea.X;
		}
	}

	public float PlannedDepth
	{
		get
		{
			if (ShipObject == null)
			{
				return 0f;
			}
			return ShipObject.DeploymentArea.Y;
		}
	}

	public int PlannedTroopCount
	{
		get
		{
			if (ShipObject == null)
			{
				return 0;
			}
			return ShipOrigin.TotalCrewCapacity;
		}
	}

	public bool HasDimensions
	{
		get
		{
			if (PlannedWidth >= 1E-05f)
			{
				return PlannedDepth >= 1E-05f;
			}
			return false;
		}
	}

	public bool HasShipObject => ShipObject != null;

	public IShipOrigin ShipOrigin { get; private set; }

	public MissionShipObject ShipObject { get; private set; }

	public NavalFormationDeploymentPlan(FormationClass fClass, Mission mission)
	{
		_class = fClass;
		Clear();
		_hasFrame = false;
		ShipOrigin = null;
		ShipObject = null;
		_mission = mission;
	}

	public bool HasFrame()
	{
		return _hasFrame;
	}

	public FormationDeploymentFlank GetDefaultFlank()
	{
		FormationDeploymentFlank formationDeploymentFlank = FormationDeploymentFlank.Count;
		if (!HasShipObject)
		{
			formationDeploymentFlank = FormationDeploymentFlank.Rear;
		}
		switch (_class)
		{
		case FormationClass.Cavalry:
		case FormationClass.HeavyCavalry:
			return FormationDeploymentFlank.Left;
		case FormationClass.HorseArcher:
		case FormationClass.LightCavalry:
			return FormationDeploymentFlank.Right;
		case FormationClass.Ranged:
		case FormationClass.NumberOfRegularFormations:
		case FormationClass.Bodyguard:
		case FormationClass.NumberOfAllFormations:
			return FormationDeploymentFlank.Rear;
		default:
			return FormationDeploymentFlank.Front;
		}
	}

	public MatrixFrame GetFrame()
	{
		UpdateFrameZ();
		return _spawnFrame;
	}

	public Vec3 GetPosition()
	{
		UpdateFrameZ();
		return _spawnFrame.origin;
	}

	public Vec2 GetDirection()
	{
		return _spawnFrame.rotation.f.AsVec2;
	}

	public WorldPosition CreateNewDeploymentWorldPosition(WorldPosition.WorldPositionEnforcedCache worldPositionEnforcedCache)
	{
		return WorldPosition.Invalid;
	}

	public void Clear()
	{
		_spawnFrame = MatrixFrame.Identity;
		_hasFrame = false;
		ShipOrigin = null;
		ShipObject = null;
	}

	public void SetShipOrigin(IShipOrigin shipOrigin)
	{
		if (shipOrigin != null)
		{
			ShipOrigin = shipOrigin;
		}
		else
		{
			ShipOrigin = null;
		}
		if (ShipOrigin != null)
		{
			ShipObject = MBObjectManager.Instance.GetObject<MissionShipObject>(ShipOrigin.OriginShipId);
		}
		else
		{
			ShipObject = null;
		}
	}

	public void SetFrame(in Vec2 deployPosition, in Vec2 deployDirection)
	{
		_spawnFrame = new MatrixFrame(Mat3.CreateMat3WithForward(deployDirection.ToVec3()), deployPosition.ToVec3());
		UpdateFrameZ();
		_hasFrame = true;
	}

	private void UpdateFrameZ()
	{
		_spawnFrame.origin.z = _mission.Scene.GetWaterLevelAtPosition(_spawnFrame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
	}
}
