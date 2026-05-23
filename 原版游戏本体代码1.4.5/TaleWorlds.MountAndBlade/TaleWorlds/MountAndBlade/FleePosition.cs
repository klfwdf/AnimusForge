using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class FleePosition : ScriptComponentBehavior
{
	private List<Vec3> _nodes = new List<Vec3>();

	private BattleSideEnum _side;

	public string Side = "both";

	protected internal override void OnInit()
	{
		CollectNodes();
		bool flag = false;
		if (Side == "both")
		{
			_side = BattleSideEnum.None;
		}
		else if (Side == "attacker")
		{
			_side = ((!flag) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
		}
		else if (Side == "defender")
		{
			_side = (flag ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
		}
		else
		{
			_side = BattleSideEnum.None;
		}
		if (base.GameEntity.HasTag("sally_out"))
		{
			if (Mission.Current.IsSallyOutBattle)
			{
				Mission.Current.AddFleePosition(this);
			}
		}
		else
		{
			Mission.Current.AddFleePosition(this);
		}
	}

	public BattleSideEnum GetSide()
	{
		return _side;
	}

	protected internal override void OnEditorInit()
	{
		CollectNodes();
	}

	private void CollectNodes()
	{
		_nodes.Clear();
		int childCount = base.GameEntity.ChildCount;
		for (int i = 0; i < childCount; i++)
		{
			_nodes.Add(base.GameEntity.GetChild(i).GlobalPosition);
		}
		if (_nodes.IsEmpty())
		{
			_nodes.Add(base.GameEntity.GlobalPosition);
		}
	}

	protected internal override void OnEditorTick(float dt)
	{
		CollectNodes();
		bool flag = base.GameEntity.IsSelectedOnEditor();
		int childCount = base.GameEntity.ChildCount;
		int num = 0;
		while (!flag && num < childCount)
		{
			flag = base.GameEntity.GetChild(num).IsSelectedOnEditor();
			num++;
		}
		if (flag)
		{
			for (int i = 0; i < _nodes.Count; i++)
			{
				_ = _nodes.Count - 1;
			}
		}
	}

	public Vec3 GetClosestPointToEscape(Vec2 position)
	{
		if (_nodes.Count == 1)
		{
			return _nodes[0];
		}
		float num = float.MaxValue;
		Vec3 result = _nodes[0];
		for (int i = 0; i < _nodes.Count - 1; i++)
		{
			Vec3 vec = _nodes[i];
			Vec3 vec2 = _nodes[i + 1];
			float num2 = vec.DistanceSquared(vec2);
			if (num2 > 0f)
			{
				float num3 = MathF.Max(0f, MathF.Min(1f, Vec2.DotProduct(position - vec.AsVec2, vec2.AsVec2 - vec.AsVec2) / num2));
				Vec3 vec3 = vec + num3 * (vec2 - vec);
				float num4 = vec3.AsVec2.DistanceSquared(position);
				if (num > num4)
				{
					num = num4;
					result = vec3;
				}
			}
			else
			{
				num = 0f;
				result = vec;
			}
		}
		return result;
	}

	protected internal override bool MovesEntity()
	{
		return true;
	}
}
