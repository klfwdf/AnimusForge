using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.Missions.Objects;

internal class ShipClothFixer : ScriptComponentBehavior
{
	private struct ClothData
	{
		internal ClothSimulatorComponent ClothComponent;

		internal MatrixFrame ShipLocalFrame;
	}

	private List<ClothData> _shipCloths = new List<ClothData>();

	private MatrixFrame _prevPrevShipFrame = MatrixFrame.Identity;

	private MatrixFrame _prevShipFrame = MatrixFrame.Identity;

	private float _fixedDt;

	private int _frameCounter;

	private ShipClothFixer()
	{
	}

	protected override void OnEditorInit()
	{
		FetchClothComponents();
	}

	protected override void OnInit()
	{
		FetchClothComponents();
	}

	protected override void OnEditorTick(float dt)
	{
		foreach (ClothData shipCloth in _shipCloths)
		{
			SetPrevFrameToCloth(shipCloth);
		}
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.TickParallel | TickRequirement.FixedParallelTick;
	}

	protected override void OnParallelFixedTick(float fixedDt)
	{
		_prevPrevShipFrame = _prevShipFrame;
		_prevShipFrame = base.GameEntity.Root.GetBodyWorldTransform();
		_fixedDt = fixedDt;
		_frameCounter++;
	}

	protected override void OnTickParallel(float dt)
	{
		foreach (ClothData shipCloth in _shipCloths)
		{
			SetPrevFrameToCloth(shipCloth);
		}
	}

	private void FetchClothComponents()
	{
		_shipCloths.Clear();
		MatrixFrame globalFrame = base.GameEntity.Root.GetGlobalFrame();
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		base.GameEntity.Root.GetChildrenRecursive(ref children);
		foreach (WeakGameEntity item3 in children)
		{
			int componentCount = item3.GetComponentCount(TaleWorlds.Engine.GameEntity.ComponentType.ClothSimulator);
			for (int i = 0; i < componentCount; i++)
			{
				ClothData item = new ClothData
				{
					ClothComponent = (item3.GetComponentAtIndex(i, TaleWorlds.Engine.GameEntity.ComponentType.ClothSimulator) as ClothSimulatorComponent),
					ShipLocalFrame = globalFrame.TransformToLocal(item3.GetGlobalFrame())
				};
				_shipCloths.Add(item);
			}
			if (item3.Skeleton != null)
			{
				int componentCount2 = item3.Skeleton.GetComponentCount(TaleWorlds.Engine.GameEntity.ComponentType.ClothSimulator);
				for (int j = 0; j < componentCount2; j++)
				{
					ClothData item2 = new ClothData
					{
						ClothComponent = (item3.Skeleton.GetComponentAtIndex(TaleWorlds.Engine.GameEntity.ComponentType.ClothSimulator, j) as ClothSimulatorComponent),
						ShipLocalFrame = globalFrame.TransformToLocal(item3.GetGlobalFrame())
					};
					_shipCloths.Add(item2);
				}
			}
		}
	}

	private void SetPrevFrameToCloth(ClothData clothData)
	{
		Vec3 forcedVelocity = Vec3.Zero;
		if (_frameCounter > 2)
		{
			forcedVelocity = (_prevShipFrame.TransformToParent(in clothData.ShipLocalFrame.origin) - _prevPrevShipFrame.TransformToParent(in clothData.ShipLocalFrame.origin)) / _fixedDt;
		}
		clothData.ClothComponent.SetForcedVelocity(in forcedVelocity);
	}
}
