using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

public class SiegeDeploymentVisualizationMissionView : MissionView
{
	public enum DeploymentVisualizationPreference
	{
		ShowUndeployed = 1,
		Line = 2,
		Arc = 4,
		Banner = 8,
		Path = 16,
		Ghost = 32,
		Contour = 64,
		LiftLadders = 128,
		Light = 256,
		AllEnabled = 1023
	}

	private static int deploymentVisualizerSelector;

	private List<DeploymentPoint> _deploymentPoints;

	private bool _deploymentPointsVisible;

	private Dictionary<DeploymentPoint, List<Vec3>> _deploymentArcs = new Dictionary<DeploymentPoint, List<Vec3>>();

	private Dictionary<DeploymentPoint, (GameEntity, GameEntity)> _deploymentBanners = new Dictionary<DeploymentPoint, (GameEntity, GameEntity)>();

	private Dictionary<DeploymentPoint, GameEntity> _deploymentLights = new Dictionary<DeploymentPoint, GameEntity>();

	private const uint EntityHighlightColor = 4289622555u;

	public override void AfterStart()
	{
		base.AfterStart();
		_deploymentPoints = (from dp in Mission.Current.ActiveMissionObjects.FindAllWithType<DeploymentPoint>()
			where !dp.IsDisabled
			select dp).ToList();
		foreach (DeploymentPoint deploymentPoint in _deploymentPoints)
		{
			deploymentPoint.OnDeploymentPointTypeDetermined += OnDeploymentPointStateSet;
			deploymentPoint.OnDeploymentStateChanged += OnDeploymentStateChanged;
		}
		_deploymentPointsVisible = true;
		Mission.Current.GetMissionBehavior<SiegeDeploymentMissionController>();
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		TryRemoveDeploymentVisibilities();
		Mission.Current.RemoveMissionBehavior(this);
	}

	protected override void OnEndMission()
	{
		base.OnEndMission();
		TryRemoveDeploymentVisibilities();
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
	}

	private void TryRemoveDeploymentVisibilities()
	{
		if (!_deploymentPointsVisible)
		{
			return;
		}
		foreach (DeploymentPoint deploymentPoint in _deploymentPoints)
		{
			RemoveDeploymentVisibility(deploymentPoint);
			deploymentPoint.OnDeploymentStateChanged -= OnDeploymentStateChanged;
		}
		_deploymentPointsVisible = false;
	}

	private void RemoveDeploymentVisibility(DeploymentPoint deploymentPoint)
	{
		switch (deploymentPoint.GetDeploymentPointType())
		{
		case DeploymentPoint.DeploymentPointType.BatteringRam:
			HideDeploymentBanners(deploymentPoint, isRemoving: true);
			SetGhostVisibility(deploymentPoint, isVisible: false);
			break;
		case DeploymentPoint.DeploymentPointType.TowerLadder:
			HideDeploymentBanners(deploymentPoint, isRemoving: true);
			SetGhostVisibility(deploymentPoint, isVisible: false);
			SetDeploymentTargetContourState(deploymentPoint, isHighlighted: false);
			SetLaddersUpState(deploymentPoint, isRaised: false);
			SetLightState(deploymentPoint, isVisible: false);
			break;
		case DeploymentPoint.DeploymentPointType.Breach:
			HideDeploymentBanners(deploymentPoint, isRemoving: true);
			SetDeploymentTargetContourState(deploymentPoint, isHighlighted: false);
			SetLightState(deploymentPoint, isVisible: false);
			break;
		}
	}

	private static string GetSelectorStateDescription()
	{
		string text = "";
		for (int num = 1; num < 1023; num *= 2)
		{
			if ((deploymentVisualizerSelector & num) > 0)
			{
				string text2 = text;
				DeploymentVisualizationPreference deploymentVisualizationPreference = (DeploymentVisualizationPreference)num;
				text = text2 + " " + deploymentVisualizationPreference;
			}
		}
		return text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_deployment_visualization_selector", "mission")]
	public static string SetDeploymentVisualizationSelector(List<string> strings)
	{
		if (strings.Count == 1 && int.TryParse(strings[0], out deploymentVisualizerSelector))
		{
			return "Enabled deployment visualization options are:" + GetSelectorStateDescription();
		}
		return "Format is \"mission.set_deployment_visualization_selector [integer > 0]\".";
	}

	private void OnDeploymentStateChanged(DeploymentPoint deploymentPoint, SynchedMissionObject targetObject)
	{
		OnDeploymentPointStateSet(deploymentPoint);
	}

	private void OnDeploymentPointStateSet(DeploymentPoint deploymentPoint)
	{
		switch (deploymentPoint.GetDeploymentPointState())
		{
		case DeploymentPoint.DeploymentPointState.BatteringRam:
		case DeploymentPoint.DeploymentPointState.SiegeTower:
			if ((deploymentVisualizerSelector & 2) > 0)
			{
				ShowLineFromDeploymentPointToTarget(deploymentPoint);
			}
			if ((deploymentVisualizerSelector & 4) > 0)
			{
				CreateArcPoints(deploymentPoint);
			}
			if ((deploymentVisualizerSelector & 8) > 0)
			{
				ShowDeploymentBanners(deploymentPoint);
			}
			if ((deploymentVisualizerSelector & 0x10) > 0)
			{
				ShowPath(deploymentPoint);
			}
			if ((deploymentVisualizerSelector & 0x20) > 0)
			{
				SetGhostVisibility(deploymentPoint, isVisible: true);
			}
			SetLaddersUpState(deploymentPoint, isRaised: false);
			SetLightState(deploymentPoint, isVisible: false);
			break;
		case DeploymentPoint.DeploymentPointState.SiegeLadder:
			if ((deploymentVisualizerSelector & 2) > 0)
			{
				ShowLineFromDeploymentPointToTarget(deploymentPoint);
			}
			if ((deploymentVisualizerSelector & 4) > 0)
			{
				CreateArcPoints(deploymentPoint);
			}
			if ((deploymentVisualizerSelector & 8) > 0)
			{
				ShowDeploymentBanners(deploymentPoint);
			}
			if ((deploymentVisualizerSelector & 0x40) > 0)
			{
				SetDeploymentTargetContourState(deploymentPoint, isHighlighted: true);
			}
			if ((deploymentVisualizerSelector & 0x80) > 0)
			{
				SetLaddersUpState(deploymentPoint, isRaised: true);
			}
			if ((deploymentVisualizerSelector & 0x100) > 0)
			{
				SetLightState(deploymentPoint, isVisible: true);
			}
			break;
		case DeploymentPoint.DeploymentPointState.Breach:
			if ((deploymentVisualizerSelector & 2) > 0)
			{
				ShowLineFromDeploymentPointToTarget(deploymentPoint);
			}
			if ((deploymentVisualizerSelector & 4) > 0)
			{
				CreateArcPoints(deploymentPoint);
			}
			if ((deploymentVisualizerSelector & 8) > 0)
			{
				ShowDeploymentBanners(deploymentPoint);
			}
			if ((deploymentVisualizerSelector & 0x40) > 0)
			{
				SetDeploymentTargetContourState(deploymentPoint, isHighlighted: true);
			}
			if ((deploymentVisualizerSelector & 0x100) > 0)
			{
				SetLightState(deploymentPoint, isVisible: true);
			}
			break;
		case DeploymentPoint.DeploymentPointState.NotDeployed:
			if ((deploymentVisualizerSelector & 1) > 0)
			{
				if (deploymentPoint.GetDeploymentPointType() == DeploymentPoint.DeploymentPointType.BatteringRam)
				{
					if ((deploymentVisualizerSelector & 2) > 0)
					{
						ShowLineFromDeploymentPointToTarget(deploymentPoint);
					}
					if ((deploymentVisualizerSelector & 4) > 0)
					{
						CreateArcPoints(deploymentPoint);
					}
					if ((deploymentVisualizerSelector & 8) > 0)
					{
						ShowDeploymentBanners(deploymentPoint);
					}
					if ((deploymentVisualizerSelector & 0x10) > 0)
					{
						ShowPath(deploymentPoint);
					}
					if ((deploymentVisualizerSelector & 0x20) > 0)
					{
						SetGhostVisibility(deploymentPoint, isVisible: true);
					}
				}
			}
			else if (deploymentPoint.GetDeploymentPointType() == DeploymentPoint.DeploymentPointType.BatteringRam)
			{
				HideDeploymentBanners(deploymentPoint);
			}
			break;
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		foreach (DeploymentPoint deploymentPoint in _deploymentPoints)
		{
			switch (deploymentPoint.GetDeploymentPointState())
			{
			case DeploymentPoint.DeploymentPointState.NotDeployed:
				if ((deploymentVisualizerSelector & 1) > 0 && deploymentPoint.GetDeploymentPointType() == DeploymentPoint.DeploymentPointType.BatteringRam)
				{
					if ((deploymentVisualizerSelector & 2) > 0)
					{
						ShowLineFromDeploymentPointToTarget(deploymentPoint);
					}
					if ((deploymentVisualizerSelector & 4) > 0)
					{
						ShowArcFromDeploymentPointToTarget(deploymentPoint);
					}
					if ((deploymentVisualizerSelector & 0x10) > 0)
					{
						ShowPath(deploymentPoint);
					}
				}
				break;
			case DeploymentPoint.DeploymentPointState.SiegeLadder:
				if ((deploymentVisualizerSelector & 2) > 0)
				{
					ShowLineFromDeploymentPointToTarget(deploymentPoint);
				}
				if ((deploymentVisualizerSelector & 4) > 0)
				{
					ShowArcFromDeploymentPointToTarget(deploymentPoint);
				}
				break;
			case DeploymentPoint.DeploymentPointState.BatteringRam:
			case DeploymentPoint.DeploymentPointState.SiegeTower:
				if ((deploymentVisualizerSelector & 2) > 0)
				{
					ShowLineFromDeploymentPointToTarget(deploymentPoint);
				}
				if ((deploymentVisualizerSelector & 4) > 0)
				{
					ShowArcFromDeploymentPointToTarget(deploymentPoint);
				}
				if ((deploymentVisualizerSelector & 0x10) > 0)
				{
					ShowPath(deploymentPoint);
				}
				break;
			case DeploymentPoint.DeploymentPointState.Breach:
				if ((deploymentVisualizerSelector & 2) > 0)
				{
					ShowLineFromDeploymentPointToTarget(deploymentPoint);
				}
				if ((deploymentVisualizerSelector & 4) > 0)
				{
					ShowArcFromDeploymentPointToTarget(deploymentPoint);
				}
				break;
			}
		}
	}

	private void ShowLineFromDeploymentPointToTarget(DeploymentPoint deploymentPoint)
	{
		deploymentPoint.GetDeploymentOrigin();
		_ = deploymentPoint.DeploymentTargetPosition;
	}

	private List<Vec3> CreateArcPoints(DeploymentPoint deploymentPoint)
	{
		Vec3 deploymentOrigin = deploymentPoint.GetDeploymentOrigin();
		Vec3 deploymentTargetPosition = deploymentPoint.DeploymentTargetPosition;
		float num = (deploymentTargetPosition - deploymentOrigin).Length / 3f;
		List<Vec3> list = new List<Vec3>();
		for (int i = 0; (float)i < num; i++)
		{
			Vec3 item = MBMath.Lerp(deploymentOrigin, deploymentTargetPosition, (float)i / num, 0f);
			float num2 = 8f - TaleWorlds.Library.MathF.Pow(TaleWorlds.Library.MathF.Abs((float)i - num * 0.5f) / (num * 0.5f), 1.2f) * 8f;
			item.z += num2;
			list.Add(item);
		}
		list.Add(deploymentTargetPosition);
		return list;
	}

	private void ShowArcFromDeploymentPointToTarget(DeploymentPoint deploymentPoint)
	{
		Vec3 deploymentTargetPosition = deploymentPoint.DeploymentTargetPosition;
		_deploymentArcs.TryGetValue(deploymentPoint, out var value);
		if (value == null || value[value.Count - 1] != deploymentTargetPosition)
		{
			value = CreateArcPoints(deploymentPoint);
		}
		Vec3 vec = Vec3.Invalid;
		foreach (Vec3 item in value)
		{
			_ = vec.IsValid;
			vec = item;
		}
	}

	private void ShowDeploymentBanners(DeploymentPoint deploymentPoint)
	{
		Vec3 deploymentOrigin = deploymentPoint.GetDeploymentOrigin();
		Vec3 deploymentTargetPosition = deploymentPoint.DeploymentTargetPosition;
		_deploymentBanners.TryGetValue(deploymentPoint, out var value);
		if (value.Item1 == null || value.Item2 == null)
		{
			value = CreateBanners(deploymentPoint);
		}
		GameEntity item = _deploymentBanners[deploymentPoint].Item1;
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = deploymentOrigin;
		identity.origin.z += 7.5f;
		identity.rotation.ApplyScaleLocal(10f);
		MatrixFrame frame = identity;
		item.SetFrame(ref frame);
		item.SetVisibilityExcludeParents(visible: true);
		item.SetAlpha(1f);
		GameEntity item2 = _deploymentBanners[deploymentPoint].Item2;
		identity = MatrixFrame.Identity;
		identity.origin = deploymentTargetPosition;
		identity.origin.z += 7.5f;
		identity.rotation.ApplyScaleLocal(10f);
		MatrixFrame frame2 = identity;
		item2.SetFrame(ref frame2);
		item2.SetVisibilityExcludeParents(visible: true);
		item2.SetAlpha(1f);
	}

	private void HideDeploymentBanners(DeploymentPoint deploymentPoint, bool isRemoving = false)
	{
		_deploymentBanners.TryGetValue(deploymentPoint, out var value);
		if (value.Item1 != null && value.Item2 != null)
		{
			if (isRemoving)
			{
				value.Item1.Remove(104);
				value.Item2.Remove(105);
			}
			else
			{
				value.Item1.SetVisibilityExcludeParents(visible: false);
				value.Item2.SetVisibilityExcludeParents(visible: false);
			}
		}
	}

	private (GameEntity, GameEntity) CreateBanners(DeploymentPoint deploymentPoint)
	{
		GameEntity gameEntity = CreateBannerEntity(isTargetEntity: false);
		gameEntity.SetVisibilityExcludeParents(visible: false);
		GameEntity gameEntity2 = CreateBannerEntity(isTargetEntity: true);
		gameEntity2.SetVisibilityExcludeParents(visible: false);
		(GameEntity, GameEntity) tuple = (gameEntity, gameEntity2);
		_deploymentBanners.Add(deploymentPoint, tuple);
		return tuple;
	}

	private GameEntity CreateBannerEntity(bool isTargetEntity)
	{
		GameEntity gameEntity = GameEntity.CreateEmpty(Mission.Current.Scene);
		gameEntity.EntityFlags |= EntityFlags.NoOcclusionCulling;
		uint color = 4278190080u;
		uint color2 = (isTargetEntity ? 2131100887u : 2141323264u);
		gameEntity.AddMultiMesh(MetaMesh.GetCopy("billboard_unit_mesh"));
		gameEntity.GetFirstMesh().Color = uint.MaxValue;
		Material material = Material.GetFromResource("formation_icon").CreateCopy();
		if (isTargetEntity)
		{
			Texture fromResource = Texture.GetFromResource("plain_yellow");
			material.SetTexture(Material.MBTextureType.DiffuseMap2, fromResource);
		}
		else
		{
			Texture fromResource2 = Texture.GetFromResource("plain_blue");
			material.SetTexture(Material.MBTextureType.DiffuseMap2, fromResource2);
		}
		gameEntity.GetFirstMesh().SetMaterial(material);
		gameEntity.GetFirstMesh().Color = color2;
		gameEntity.GetFirstMesh().Color2 = color;
		gameEntity.GetFirstMesh().SetVectorArgument(0f, 1f, 0f, 1f);
		return gameEntity;
	}

	private void ShowPath(DeploymentPoint deploymentPoint)
	{
		(deploymentPoint.GetWeaponsUnder().FirstOrDefault((SynchedMissionObject wu) => wu is IMoveableSiegeWeapon) as IMoveableSiegeWeapon).HighlightPath();
	}

	private void SetGhostVisibility(DeploymentPoint deploymentPoint, bool isVisible)
	{
	}

	private void SetDeploymentTargetContourState(DeploymentPoint deploymentPoint, bool isHighlighted)
	{
		switch (deploymentPoint.GetDeploymentPointState())
		{
		case DeploymentPoint.DeploymentPointState.SiegeLadder:
		{
			foreach (SiegeLadder associatedSiegeLadder in deploymentPoint.GetAssociatedSiegeLadders())
			{
				if (isHighlighted)
				{
					associatedSiegeLadder.GameEntity.SetContourColor(4289622555u);
				}
				else
				{
					associatedSiegeLadder.GameEntity.SetContourColor(null);
				}
			}
			break;
		}
		case DeploymentPoint.DeploymentPointState.Breach:
			if (isHighlighted)
			{
				deploymentPoint.AssociatedWallSegment.GameEntity.SetContourColor(4289622555u);
			}
			else
			{
				deploymentPoint.AssociatedWallSegment.GameEntity.SetContourColor(null);
			}
			break;
		}
	}

	private void SetLaddersUpState(DeploymentPoint deploymentPoint, bool isRaised)
	{
		foreach (SiegeLadder associatedSiegeLadder in deploymentPoint.GetAssociatedSiegeLadders())
		{
			associatedSiegeLadder.SetUpStateVisibility(isRaised);
		}
	}

	private void SetLightState(DeploymentPoint deploymentPoint, bool isVisible)
	{
		_deploymentLights.TryGetValue(deploymentPoint, out var value);
		if (value != null)
		{
			value.SetVisibilityExcludeParents(isVisible);
		}
		else if (isVisible)
		{
			CreateLight(deploymentPoint);
		}
	}

	private void CreateLight(DeploymentPoint deploymentPoint)
	{
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = deploymentPoint.DeploymentTargetPosition + new Vec3(0f, 0f, (deploymentPoint.GetDeploymentPointType() == DeploymentPoint.DeploymentPointType.TowerLadder) ? 10f : 3f);
		identity.rotation.RotateAboutSide(System.MathF.PI / 2f);
		identity.Scale(new Vec3(5f, 5f, 5f));
		GameEntity value = GameEntity.Instantiate(Mission.Current.Scene, "aserai_keep_interior_a_light_shaft_a", identity);
		_deploymentLights.Add(deploymentPoint, value);
	}
}
