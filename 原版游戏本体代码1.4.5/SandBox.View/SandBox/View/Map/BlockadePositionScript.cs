using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.View.Map;

public class BlockadePositionScript : ScriptComponentBehavior
{
	public int MaximumNumberOfShips = 12;

	public int NumberOfArcs = 4;

	public float DistanceBetweenShips = System.MathF.PI / 4f;

	public float DistanceRandomizationOnArcs = 0.1f;

	public float DistanceRandomizationBetweenArcs = 0.1f;

	public float Angle = System.MathF.PI / 2f;

	public string MissionShipId = "dromon_ship_nested";

	public float ShipScaleFactor = 0.052f;

	public bool IsVisualizationEnabled;

	public bool IsRandomizationEnabled;

	public bool IsShipVisualizationEnabled;

	public SimpleButton RefreshVisualization;

	private List<List<Vec3>> _pointsOfArcs;

	private Vec3 _center;

	private List<GameEntity> _shipEntities = new List<GameEntity>();

	protected override void OnEditorTick(float dt)
	{
		if (IsVisualizationEnabled)
		{
			VisualizeArcs();
		}
	}

	private void VisualizeArcs()
	{
		if (_pointsOfArcs == null || !IsRandomizationEnabled)
		{
			_pointsOfArcs = GetBlockadeArc(MaximumNumberOfShips, out _center);
		}
		if (_pointsOfArcs == null)
		{
			return;
		}
		foreach (List<Vec3> pointsOfArc in _pointsOfArcs)
		{
			foreach (Vec3 item in pointsOfArc)
			{
				_ = item;
			}
		}
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (!(variableName == "RefreshVisualization"))
		{
			return;
		}
		_pointsOfArcs = GetBlockadeArc(MaximumNumberOfShips, out _center);
		if (!_shipEntities.IsEmpty())
		{
			Utilities.DeleteEntitiesInEditorScene(_shipEntities);
		}
		_shipEntities.Clear();
		if (!IsShipVisualizationEnabled)
		{
			return;
		}
		foreach (List<Vec3> pointsOfArc in _pointsOfArcs)
		{
			foreach (Vec3 item in pointsOfArc)
			{
				Vec2 vec = item.AsVec2 - _center.AsVec2;
				MatrixFrame frame = MatrixFrame.Identity;
				frame.origin = item;
				float num = vec.AngleBetween(frame.rotation.f.AsVec2);
				frame.Rotate(System.MathF.PI / 2f - num, in Vec3.Up);
				frame.rotation.ApplyScaleLocal(ShipScaleFactor);
				GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, MissionShipId, callScriptCallbacks: false);
				if (gameEntity == null)
				{
					break;
				}
				gameEntity.SetFrame(ref frame);
				_shipEntities.Add(gameEntity);
			}
		}
	}

	public List<List<Vec3>> GetBlockadeArc(int totalNumberOfShips, out Vec3 center)
	{
		int num = MaximumNumberOfShips;
		if (totalNumberOfShips < num)
		{
			num = totalNumberOfShips;
		}
		List<List<Vec3>> list = new List<List<Vec3>>();
		WeakGameEntity firstChildEntityWithTag = base.GameEntity.GetFirstChildEntityWithTag("Blockade_Arc_Start");
		WeakGameEntity firstChildEntityWithTag2 = base.GameEntity.GetFirstChildEntityWithTag("Blockade_Arc_End");
		center = Vec3.Invalid;
		if (firstChildEntityWithTag == null || firstChildEntityWithTag2 == null)
		{
			return list;
		}
		center = FindCenterOfCircle(firstChildEntityWithTag2.GlobalPosition, firstChildEntityWithTag.GlobalPosition);
		Vec3 vec = firstChildEntityWithTag2.GlobalPosition - center;
		vec.Normalize();
		Vec3 vec2 = vec;
		int num2 = NumberOfArcs;
		float num3 = firstChildEntityWithTag2.GlobalPosition.Distance(center) / (float)NumberOfArcs;
		int num4 = 0;
		float num5 = DistanceBetweenShips;
		while (num2 > 0)
		{
			int num6 = TaleWorlds.Library.MathF.Round(Angle * (float)num2 / DistanceBetweenShips);
			if (num - num4 < num6)
			{
				num5 *= (float)num6 / (float)(num - num4);
				num6 = num - num4;
			}
			vec2.RotateAboutZ(num5 / (float)(num2 * 2));
			List<Vec3> list2 = new List<Vec3>();
			for (int i = 0; i < num6; i++)
			{
				float a = MBRandom.RandomFloatRanged(0f - DistanceRandomizationOnArcs, DistanceRandomizationOnArcs);
				float num7 = MBRandom.RandomFloatRanged(0f, DistanceRandomizationBetweenArcs);
				Vec3 item = center + vec2 * num2 * num3;
				vec2.RotateAboutZ(num5 / (float)num2);
				if (IsRandomizationEnabled)
				{
					item += vec2 * num7;
					vec2.RotateAboutZ(a);
				}
				list2.Add(item);
				num4++;
				if (num4 >= num)
				{
					break;
				}
			}
			list.Add(list2);
			if (num4 >= num)
			{
				return list;
			}
			vec2 = vec;
			num2--;
		}
		return list;
	}

	private Vec3 FindCenterOfCircle(Vec3 arcPointStart, Vec3 arcPointEnd)
	{
		Vec3 v = arcPointEnd + (arcPointStart - arcPointEnd) / 2f;
		Vec3 vec = (arcPointStart - arcPointEnd) / 2f;
		float num = arcPointEnd.Distance(v);
		float num2 = num / TaleWorlds.Library.MathF.Tan(Angle / 2f);
		return new Vec3(v.X + num2 * vec.Y / num, v.Y - num2 * vec.X / num, arcPointStart.Z);
	}
}
