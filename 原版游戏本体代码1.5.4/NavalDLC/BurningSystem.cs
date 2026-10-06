using System.Collections.Generic;
using NavalDLC.Missions.Objects;
using TaleWorlds.Engine;
using TaleWorlds.Library;

internal class BurningSystem
{
	private class AdvancedSpreadNode
	{
		internal BurningNode Node;

		internal BurningNode NextNode;

		internal BurningNode PrevNode;

		internal float[] CurrentFlame;
	}

	private GameEntity _fireRoot;

	private MBList<BurningNode> _burningNodes = new MBList<BurningNode>();

	private int _lastBurningIndex = -1;

	private int _currentAdvancedSpreadFlameIndex;

	private Dictionary<BurningNode, AdvancedSpreadNode> _advancedNodes = new Dictionary<BurningNode, AdvancedSpreadNode>();

	private float _averageFireProgress;

	public bool AdvancedSpread { get; private set; }

	public float AverageFireProgress => _averageFireProgress;

	public float SpreadRate { get; set; }

	public RopeSegment BurnedRope { get; private set; }

	public PulleySystem BurnedPulley { get; private set; }

	public MBReadOnlyList<BurningNode> BurningNodes => _burningNodes;

	public BurningSystem(GameEntity fireRoot, float spreadRate)
	{
		_fireRoot = fireRoot;
		SpreadRate = spreadRate;
		BurnedRope = null;
		BurnedPulley = null;
		AdvancedSpread = false;
		_lastBurningIndex = 0;
	}

	public BurningSystem(GameEntity fireRoot, float spreadRate, PulleySystem pulley)
	{
		_fireRoot = fireRoot;
		SpreadRate = spreadRate;
		BurnedRope = null;
		BurnedPulley = pulley;
		_lastBurningIndex = 0;
	}

	public BurningSystem(GameEntity fireRoot, float spreadRate, RopeSegment rope)
	{
		_fireRoot = fireRoot;
		SpreadRate = spreadRate;
		BurnedRope = rope;
		BurnedPulley = null;
		_lastBurningIndex = 0;
	}

	public void Tick(float dt)
	{
		if (AdvancedSpread)
		{
			DoAdvancedSpread(dt);
		}
		else
		{
			DoSimpleSpread(dt);
		}
	}

	private void DoAdvancedSpread(float dt)
	{
		int num = (_currentAdvancedSpreadFlameIndex + 1) % 2;
		_averageFireProgress = 0f;
		foreach (AdvancedSpreadNode value in _advancedNodes.Values)
		{
			float num2 = value.CurrentFlame[_currentAdvancedSpreadFlameIndex];
			_averageFireProgress += num2;
			if (!(num2 < 1f))
			{
				continue;
			}
			if (value.Node.BurningTimer > 0f)
			{
				value.Node.BurningTimer -= dt;
				continue;
			}
			if (num2 > 0f)
			{
				num2 += SpreadRate * dt;
			}
			if (num2 < 0.01f && value.NextNode != null && _advancedNodes[value.NextNode].CurrentFlame[_currentAdvancedSpreadFlameIndex] > 0.5f)
			{
				num2 = 0.01f;
			}
			if (num2 < 0.01f && value.PrevNode != null && _advancedNodes[value.PrevNode].CurrentFlame[_currentAdvancedSpreadFlameIndex] > 0.5f)
			{
				num2 = 0.01f;
			}
			num2 = MathF.Min(num2, 1f);
			value.CurrentFlame[num] = num2;
			value.Node.CurrentFireProgress = num2;
		}
		if (_advancedNodes.Count > 0)
		{
			_averageFireProgress /= _advancedNodes.Count;
		}
		_currentAdvancedSpreadFlameIndex = num;
	}

	private void DoSimpleSpread(float dt)
	{
		if (_lastBurningIndex != -1 && _lastBurningIndex != _burningNodes.Count)
		{
			BurningNode burningNode = _burningNodes[_lastBurningIndex];
			burningNode.CurrentFireProgress += SpreadRate * dt;
			if (burningNode.CurrentFireProgress >= 1f)
			{
				_lastBurningIndex++;
			}
		}
	}

	public void SetSpreadRate(float value)
	{
		SpreadRate = value;
	}

	public void AddNewNode(BurningNode node)
	{
		_burningNodes.Add(node);
	}

	public void AddAdvancedNode(BurningNode node, BurningNode prevNode, BurningNode nextNode)
	{
		AdvancedSpread = true;
		AdvancedSpreadNode advancedSpreadNode = new AdvancedSpreadNode();
		advancedSpreadNode.Node = node;
		advancedSpreadNode.NextNode = nextNode;
		advancedSpreadNode.PrevNode = prevNode;
		advancedSpreadNode.CurrentFlame = new float[2];
		node.CurrentFireProgress = 0f;
		advancedSpreadNode.CurrentFlame[0] = node.CurrentFireProgress;
		advancedSpreadNode.CurrentFlame[1] = advancedSpreadNode.CurrentFlame[0];
		_advancedNodes.Add(node, advancedSpreadNode);
	}

	public void SetFlameProgressOfAdvancedNode(BurningNode node, float progress)
	{
		if (_advancedNodes.TryGetValue(node, out var value))
		{
			value.CurrentFlame[_currentAdvancedSpreadFlameIndex] = progress;
		}
	}

	public float GetFlameProgress()
	{
		if (_lastBurningIndex >= _burningNodes.Count)
		{
			return 1f;
		}
		if (_lastBurningIndex >= 0)
		{
			return ((float)_lastBurningIndex + _burningNodes[_lastBurningIndex].CurrentFireProgress) / (float)_burningNodes.Count;
		}
		return 0f;
	}

	public bool FireStarted()
	{
		return _lastBurningIndex != -1;
	}

	public bool FlamesReachedEnd()
	{
		return _lastBurningIndex == _burningNodes.Count;
	}

	public void Remove()
	{
		foreach (BurningNode burningNode in _burningNodes)
		{
			RopeSegmentCosmetics firstScriptOfType = burningNode.GameEntity.GetFirstScriptOfType<RopeSegmentCosmetics>();
			if (firstScriptOfType != null)
			{
				if (BurnedRope != null)
				{
					BurnedRope.DeregisterRopeSegmentCosmetics(firstScriptOfType);
				}
				if (BurnedPulley != null)
				{
					BurnedPulley.DeregisterRopeSegmentCosmetics(firstScriptOfType);
				}
			}
			burningNode.GameEntity.Remove(33);
		}
		if (_fireRoot != null)
		{
			_fireRoot.Remove(33);
		}
	}

	public float GetBurningAnimationDuration()
	{
		return (float)_burningNodes.Count / SpreadRate;
	}

	public void SetExternalFlameMultiplier(float value)
	{
		foreach (BurningNode burningNode in _burningNodes)
		{
			burningNode.SetExternalFlameMultiplier(value);
		}
	}

	public void CheckWater()
	{
		foreach (BurningNode burningNode in _burningNodes)
		{
			burningNode.CheckWater();
		}
	}
}
