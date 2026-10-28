using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

[ScriptComponentParams("ship_visual_only", "")]
internal class ShipBurningSystem : ScriptComponentBehavior
{
	private const string RailingParentTag = "railing_parent";

	private bool _fireStarted;

	private BurningSystem _railingFire;

	private BurningSystem _shipDeckFire;

	private BurningSystem _deckUpgradeFire;

	private BurningSystem _mastFire;

	private List<BurningNode> _railingNodes = new List<BurningNode>();

	private List<BurningNode> _shipDeckNodes = new List<BurningNode>();

	private List<BurningNode> _deckUpgradeNodes = new List<BurningNode>();

	private List<BurningNode> _mastNodes = new List<BurningNode>();

	private List<BurningSoundNode> _soundNodes = new List<BurningSoundNode>();

	private List<Light> _burningLights = new List<Light>();

	private MBFastRandom _randomGenerator;

	private List<BurningNode> _temporaryBurningNodes = new List<BurningNode>();

	[EditableScriptComponentVariable(true, "Start Fire")]
	private SimpleButton _startFire = new SimpleButton();

	[EditableScriptComponentVariable(true, "Stop Fire")]
	private SimpleButton _stopFire = new SimpleButton();

	[EditableScriptComponentVariable(true, "Spread Rate")]
	private float _spreadRate = 1f;

	[EditableScriptComponentVariable(true, "Fire Start Random Count")]
	private int _fireStartRandomCount = 2;

	[EditableScriptComponentVariable(true, "All Fire Mode")]
	private bool _allFireMode;

	[EditableScriptComponentVariable(true, "Small Hit Debug")]
	private bool _hitDebug;

	[EditableScriptComponentVariable(true, "Min Fire Progress For Light")]
	private float _minFireProgressLight = 0.5f;

	[EditableScriptComponentVariable(true, "Max Fire Progress For Light")]
	private float _maxFireProgressLight = 1f;

	[EditableScriptComponentVariable(true, "Max Light Intensity")]
	private float _maxLightIntensity = 5000f;

	public void DummyFunc()
	{
		Debug.Print(_stopFire.ToString());
		Debug.Print(_stopFire.ToString());
		Debug.Print(_startFire.ToString());
		Debug.Print(_allFireMode.ToString());
		Debug.Print(_hitDebug.ToString());
	}

	protected override void OnInit()
	{
		FetchEntities();
		_randomGenerator = new MBFastRandom((uint)((ulong)base.GameEntity.Pointer & 0xFFFFFFFFu));
	}

	protected override void OnTickParallel(float dt)
	{
		if (_fireStarted)
		{
			TickFire(dt);
		}
		HandleTemporaryBurningNodes(dt);
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.TickParallel;
	}

	private void TickFire(float dt)
	{
		float num = 0f;
		int num2 = 0;
		if (_railingFire != null)
		{
			_railingFire.Tick(dt);
			num += _railingFire.AverageFireProgress;
			num2++;
		}
		if (_shipDeckFire != null)
		{
			_shipDeckFire.Tick(dt);
			num += _shipDeckFire.AverageFireProgress;
			num2++;
		}
		if (_deckUpgradeFire != null)
		{
			_deckUpgradeFire.Tick(dt);
			num += _deckUpgradeFire.AverageFireProgress;
			num2++;
		}
		if (_mastFire != null)
		{
			_mastFire.Tick(dt);
			num += _mastFire.AverageFireProgress;
			num2++;
		}
		if (num2 > 0)
		{
			num /= (float)num2;
			if (num < _minFireProgressLight)
			{
				foreach (Light burningLight in _burningLights)
				{
					burningLight.GetEntity().SetVisibilityExcludeParents(visible: false);
				}
			}
			else
			{
				float value = (num - _minFireProgressLight) / (_maxFireProgressLight - _minFireProgressLight);
				value = MathF.Clamp(value, 0f, 1f) * _maxLightIntensity;
				foreach (Light burningLight2 in _burningLights)
				{
					burningLight2.GetEntity().SetVisibilityExcludeParents(visible: true);
					burningLight2.Intensity = value;
				}
			}
		}
		foreach (BurningNode railingNode in _railingNodes)
		{
			MatrixFrame globalFrame = railingNode.GameEntity.GetGlobalFrame();
			float waterLevelAtPosition = base.GameEntity.GetWaterLevelAtPosition(globalFrame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
			if (globalFrame.origin.z < waterLevelAtPosition)
			{
				_railingFire.SetFlameProgressOfAdvancedNode(railingNode, 0f);
				railingNode.CurrentFireProgress = 0f;
				railingNode.BurningTimer = 3f;
			}
		}
		foreach (BurningNode shipDeckNode in _shipDeckNodes)
		{
			MatrixFrame globalFrame2 = shipDeckNode.GameEntity.GetGlobalFrame();
			float waterLevelAtPosition2 = base.GameEntity.GetWaterLevelAtPosition(globalFrame2.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
			if (globalFrame2.origin.z < waterLevelAtPosition2)
			{
				_shipDeckFire.SetFlameProgressOfAdvancedNode(shipDeckNode, 0f);
				shipDeckNode.CurrentFireProgress = 0f;
				shipDeckNode.BurningTimer = 3f;
			}
		}
		foreach (BurningNode deckUpgradeNode in _deckUpgradeNodes)
		{
			MatrixFrame globalFrame3 = deckUpgradeNode.GameEntity.GetGlobalFrame();
			float waterLevelAtPosition3 = base.GameEntity.GetWaterLevelAtPosition(globalFrame3.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
			if (globalFrame3.origin.z < waterLevelAtPosition3)
			{
				_deckUpgradeFire.SetFlameProgressOfAdvancedNode(deckUpgradeNode, 0f);
				deckUpgradeNode.CurrentFireProgress = 0f;
				deckUpgradeNode.BurningTimer = 3f;
			}
		}
		foreach (BurningNode mastNode in _mastNodes)
		{
			MatrixFrame globalFrame4 = mastNode.GameEntity.GetGlobalFrame();
			float waterLevelAtPosition4 = base.GameEntity.GetWaterLevelAtPosition(globalFrame4.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
			if (globalFrame4.origin.z < waterLevelAtPosition4)
			{
				_mastFire.SetFlameProgressOfAdvancedNode(mastNode, 0f);
				mastNode.CurrentFireProgress = 0f;
				mastNode.BurningTimer = 3f;
			}
		}
	}

	private void FillFireSystemWithNodes(ref List<BurningNode> nodes, ref BurningSystem fire)
	{
		nodes.Sort((BurningNode x, BurningNode y) => x.NodeIndex.CompareTo(x.NodeIndex));
		fire = new BurningSystem(null, 1f / _spreadRate);
		fire.AddAdvancedNode(nodes[0], nodes[nodes.Count - 1], nodes[1]);
		for (int num = 1; num < nodes.Count - 1; num++)
		{
			fire.AddAdvancedNode(nodes[num], nodes[num - 1], nodes[num + 1]);
			foreach (BurningSoundNode soundNode in _soundNodes)
			{
				soundNode.AddBurningNode(nodes[num]);
			}
		}
		fire.AddAdvancedNode(nodes[nodes.Count - 1], nodes[nodes.Count - 2], nodes[0]);
		for (int num2 = 0; num2 < _fireStartRandomCount; num2++)
		{
			int index = MBRandom.RandomInt(nodes.Count);
			fire.SetFlameProgressOfAdvancedNode(nodes[index], 0.05f + MBRandom.RandomFloat * 0.1f);
		}
	}

	private void FetchEntities()
	{
		_railingNodes.Clear();
		WeakGameEntity firstChildEntityWithTag = base.GameEntity.GetFirstChildEntityWithTag("railing_parent");
		if (firstChildEntityWithTag != null)
		{
			foreach (WeakGameEntity child in firstChildEntityWithTag.GetChildren())
			{
				BurningNode firstScriptOfType = child.GetFirstScriptOfType<BurningNode>();
				if (firstScriptOfType != null)
				{
					_railingNodes.Add(firstScriptOfType);
				}
			}
		}
		_shipDeckNodes.Clear();
		WeakGameEntity firstChildEntityWithTag2 = base.GameEntity.GetFirstChildEntityWithTag("ship_deck_parent");
		if (firstChildEntityWithTag2 != null)
		{
			foreach (WeakGameEntity child2 in firstChildEntityWithTag2.GetChildren())
			{
				BurningNode firstScriptOfType2 = child2.GetFirstScriptOfType<BurningNode>();
				if (firstScriptOfType2 != null)
				{
					_shipDeckNodes.Add(firstScriptOfType2);
				}
			}
		}
		_deckUpgradeNodes.Clear();
		WeakGameEntity firstChildEntityWithTag3 = base.GameEntity.GetFirstChildEntityWithTag("deck_upgrade_parent");
		if (firstChildEntityWithTag3 != null)
		{
			foreach (WeakGameEntity child3 in firstChildEntityWithTag3.GetChildren())
			{
				BurningNode firstScriptOfType3 = child3.GetFirstScriptOfType<BurningNode>();
				if (firstScriptOfType3 != null)
				{
					_deckUpgradeNodes.Add(firstScriptOfType3);
				}
			}
		}
		_mastNodes.Clear();
		WeakGameEntity firstChildEntityWithTag4 = base.GameEntity.GetFirstChildEntityWithTag("mast_parent");
		if (firstChildEntityWithTag4 != null)
		{
			foreach (WeakGameEntity child4 in firstChildEntityWithTag4.GetChildren())
			{
				BurningNode firstScriptOfType4 = child4.GetFirstScriptOfType<BurningNode>();
				if (firstScriptOfType4 != null)
				{
					_mastNodes.Add(firstScriptOfType4);
				}
			}
		}
		_burningLights.Clear();
		WeakGameEntity firstChildEntityWithTag5 = base.GameEntity.GetFirstChildEntityWithTag("light_parent");
		if (firstChildEntityWithTag5 != null)
		{
			foreach (WeakGameEntity child5 in firstChildEntityWithTag5.GetChildren())
			{
				Light light = child5.GetComponentAtIndex(0, TaleWorlds.Engine.GameEntity.ComponentType.Light) as Light;
				if (light != null)
				{
					_burningLights.Add(light);
					if (!_allFireMode)
					{
						child5.SetVisibilityExcludeParents(visible: false);
					}
				}
			}
		}
		_soundNodes.Clear();
		WeakGameEntity firstChildEntityWithTag6 = base.GameEntity.GetFirstChildEntityWithTag("sound_parent");
		if (!(firstChildEntityWithTag6 != null))
		{
			return;
		}
		foreach (WeakGameEntity child6 in firstChildEntityWithTag6.GetChildren())
		{
			BurningSoundNode firstScriptOfType5 = child6.GetFirstScriptOfType<BurningSoundNode>();
			if (firstScriptOfType5 != null)
			{
				_soundNodes.Add(firstScriptOfType5);
			}
		}
	}

	private void HandleTemporaryBurningNodes(float dt)
	{
		float num = 0.05f;
		for (int i = 0; i < _temporaryBurningNodes.Count; i++)
		{
			BurningNode burningNode = _temporaryBurningNodes[i];
			burningNode.CurrentFireProgress -= dt * num;
			if (burningNode.CurrentFireProgress == 0f)
			{
				_temporaryBurningNodes[i] = _temporaryBurningNodes[_temporaryBurningNodes.Count - 1];
				_temporaryBurningNodes.Remove(_temporaryBurningNodes[_temporaryBurningNodes.Count - 1]);
				i--;
			}
		}
	}

	private void RegisterBlowAux(Vec3 collisionPosition, List<BurningNode> nodes, BurningSystem fire)
	{
		float num = 6f;
		float num2 = num * num;
		float num3 = 2f;
		float maxVal = 0.75f;
		float minVal = 0.35f;
		foreach (BurningNode node in nodes)
		{
			if (!(node.CurrentFireProgress < 1f))
			{
				continue;
			}
			float num4 = node.GameEntity.GetGlobalFrame().origin.DistanceSquared(collisionPosition);
			if (num4 < num2)
			{
				float num5 = MathF.Sqrt(num4);
				float num6 = 1f - MathF.Clamp((num5 - num3) / num, 0f, 1f);
				float num7 = _randomGenerator.NextFloatRanged(minVal, maxVal) * num6;
				if (fire != null)
				{
					fire.SetFlameProgressOfAdvancedNode(node, node.CurrentFireProgress);
				}
				else if (node.CurrentFireProgress == 0f)
				{
					_temporaryBurningNodes.Add(node);
				}
				node.CurrentFireProgress += num7;
			}
		}
	}

	public void RegisterBlow(Vec3 collisionPosition)
	{
		RegisterBlowAux(collisionPosition, _railingNodes, _railingFire);
		RegisterBlowAux(collisionPosition, _shipDeckNodes, _shipDeckFire);
		RegisterBlowAux(collisionPosition, _deckUpgradeNodes, _deckUpgradeFire);
		RegisterBlowAux(collisionPosition, _mastNodes, _mastFire);
	}

	public void StartFire()
	{
		_fireStarted = true;
		if (_railingNodes.Count > 2)
		{
			FillFireSystemWithNodes(ref _railingNodes, ref _railingFire);
		}
		if (_shipDeckNodes.Count > 2)
		{
			FillFireSystemWithNodes(ref _shipDeckNodes, ref _shipDeckFire);
		}
		if (_deckUpgradeNodes.Count > 2)
		{
			FillFireSystemWithNodes(ref _deckUpgradeNodes, ref _deckUpgradeFire);
		}
		if (_mastNodes.Count > 2)
		{
			FillFireSystemWithNodes(ref _mastNodes, ref _mastFire);
		}
		foreach (BurningSoundNode soundNode in _soundNodes)
		{
			soundNode.StartFire();
		}
	}
}
