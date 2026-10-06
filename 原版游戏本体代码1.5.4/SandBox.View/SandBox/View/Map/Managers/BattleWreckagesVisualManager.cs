using System;
using System.Collections.Generic;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BattleWreckages;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.View.Map.Managers;

public class BattleWreckagesVisualManager : EntityVisualManagerBase<BattleWreckage>
{
	private readonly Dictionary<BattleWreckage, BattleWreckageVisual> _visuals = new Dictionary<BattleWreckage, BattleWreckageVisual>();

	private readonly List<BattleWreckageVisual> _visualsFlattened = new List<BattleWreckageVisual>();

	private readonly List<BattleWreckageVisual> _fadingVisuals = new List<BattleWreckageVisual>();

	private MapScreen.DecalEntity _circleDecalHover;

	private MapScreen.DecalEntity _circleDecalTarget;

	private const string CircleDecalMaterialName = "map_circle_decal";

	private const string CircleDecalEntityName = "OuterPointTarget";

	public static BattleWreckagesVisualManager Current => SandBoxViewSubModule.SandBoxViewVisualManager.GetEntityComponent<BattleWreckagesVisualManager>();

	public override int Priority => 75;

	public override MapEntityVisual<BattleWreckage> GetVisualOfEntity(BattleWreckage entity)
	{
		if (_visuals.TryGetValue(entity, out var value))
		{
			return value;
		}
		return null;
	}

	protected override void OnInitialize()
	{
		RegisterEvents();
		foreach (BattleWreckage wreckage in Campaign.Current.Wreckages)
		{
			OnBattleWreckageCreated(wreckage);
		}
		_circleDecalHover = MapScreen.DecalEntity.Create(base.MapScene, "map_circle_decal", "OuterPointTarget");
		_circleDecalTarget = MapScreen.DecalEntity.Create(base.MapScene, "map_circle_decal", "OuterPointTarget");
	}

	private void RegisterEvents()
	{
		CampaignEvents.MapInteractableCreated.AddNonSerializedListener(this, OnInteractableCreated);
		CampaignEvents.MapInteractableDestroyed.AddNonSerializedListener(this, OnInteractableDestroyed);
	}

	public override void OnTick(float realDt, float dt)
	{
		TWParallel.For(0, _visualsFlattened.Count, delegate(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				_visualsFlattened[i].Tick(dt, realDt);
			}
		});
		foreach (KeyValuePair<BattleWreckage, BattleWreckageVisual> visual in _visuals)
		{
			if (visual.Value.HasVisibilityChanged())
			{
				visual.Value.OnVisibilityChanged();
				if (!_fadingVisuals.Contains(visual.Value))
				{
					_fadingVisuals.Add(visual.Value);
				}
			}
		}
		for (int num = _fadingVisuals.Count - 1; num >= 0; num--)
		{
			_fadingVisuals[num].TickFadingState(realDt);
			if (!_fadingVisuals[num].IsFading)
			{
				_fadingVisuals.RemoveAt(num);
			}
		}
	}

	public override void OnVisualTick(MapScreen screen, float realDt, float dt)
	{
		BattleWreckageVisual battleWreckageVisual = null;
		BattleWreckageVisual battleWreckageVisual2 = null;
		if (MobileParty.MainParty.Ai.AiBehaviorInteractable is BattleWreckage entity)
		{
			battleWreckageVisual2 = (BattleWreckageVisual)GetVisualOfEntity(entity);
		}
		else if (screen.CurrentVisualOfTooltip is BattleWreckageVisual battleWreckageVisual3)
		{
			battleWreckageVisual = battleWreckageVisual3;
		}
		if (battleWreckageVisual2 != null)
		{
			MatrixFrame frame = MatrixFrame.Identity;
			frame.origin = battleWreckageVisual2.GetVisualPosition();
			frame.Scale(Vec3.One * battleWreckageVisual2.WreckageTypeCoefficient);
			_circleDecalTarget.GameEntity.SetVisibilityExcludeParents(visible: true);
			_circleDecalTarget.Decal.SetVectorArgument(0.166f, 1f, 0.83f, 0f);
			_circleDecalTarget.Decal.SetFactor1Linear(4291596077u);
			_circleDecalTarget.GameEntity.SetGlobalFrame(in frame);
		}
		else
		{
			_circleDecalTarget.GameEntity.SetVisibilityExcludeParents(visible: false);
		}
		if (battleWreckageVisual != null && battleWreckageVisual != battleWreckageVisual2)
		{
			MatrixFrame frame2 = MatrixFrame.Identity;
			frame2.origin = battleWreckageVisual.GetVisualPosition();
			frame2.Scale(Vec3.One * battleWreckageVisual.WreckageTypeCoefficient);
			_circleDecalHover.GameEntity.SetVisibilityExcludeParents(visible: true);
			_circleDecalHover.Decal.SetVectorArgument(0.166f, 1f, 0.83f, 0f);
			_circleDecalHover.Decal.SetFactor1Linear(4291596077u);
			_circleDecalHover.GameEntity.SetGlobalFrame(in frame2);
		}
		else
		{
			_circleDecalHover.GameEntity.SetVisibilityExcludeParents(visible: false);
		}
	}

	public override bool OnVisualIntersected(Ray mouseRay, UIntPtr[] intersectedEntityIDs, Intersection[] intersectionInfos, int entityCount, Vec3 worldMouseNear, Vec3 worldMouseFar, Vec3 terrainIntersectionPoint, ref MapEntityVisual hoveredVisual, ref MapEntityVisual selectedVisual)
	{
		for (int num = entityCount - 1; num >= 0; num--)
		{
			UIntPtr uIntPtr = intersectedEntityIDs[num];
			if (uIntPtr != UIntPtr.Zero && MapScreen.VisualsOfEntities.TryGetValue(uIntPtr, out var value) && value is BattleWreckageVisual && value.IsVisibleOrFadingOut())
			{
				hoveredVisual = value;
				selectedVisual = value;
			}
		}
		return selectedVisual != null;
	}

	private void OnInteractableCreated(IInteractablePoint point)
	{
		if (point is BattleWreckage battleWreckage)
		{
			OnBattleWreckageCreated(battleWreckage);
		}
	}

	private void OnInteractableDestroyed(IInteractablePoint point)
	{
		if (point is BattleWreckage battleWreckage)
		{
			OnBattleWreckageDestroyed(battleWreckage);
		}
	}

	private void OnBattleWreckageCreated(BattleWreckage battleWreckage)
	{
		BattleWreckageVisual battleWreckageVisual = new BattleWreckageVisual(battleWreckage);
		battleWreckageVisual.OnStartup();
		if (!_visuals.ContainsKey(battleWreckageVisual.MapEntity))
		{
			_visuals.Add(battleWreckageVisual.MapEntity, battleWreckageVisual);
			_visualsFlattened.Add(battleWreckageVisual);
		}
	}

	private void OnBattleWreckageDestroyed(BattleWreckage battleWreckage)
	{
		BattleWreckageVisual battleWreckageVisual = (BattleWreckageVisual)GetVisualOfEntity(battleWreckage);
		if (battleWreckageVisual != null)
		{
			_fadingVisuals.Remove(battleWreckageVisual);
			_visualsFlattened.Remove(battleWreckageVisual);
			battleWreckageVisual.OnRemoved();
		}
		_visuals.Remove(battleWreckage);
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		foreach (KeyValuePair<BattleWreckage, BattleWreckageVisual> visual in _visuals)
		{
			visual.Value.OnRemoved();
		}
		_visuals.Clear();
		_fadingVisuals.Clear();
		_visualsFlattened.Clear();
	}
}
