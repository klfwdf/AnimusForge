using System;
using SandBox.View.Map.Visuals;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.View.Map;

public class CampaignEntityVisualComponent : IEntityComponent
{
	public virtual int Priority => 0;

	public virtual void OnVisualTick(MapScreen screen, float realDt, float dt)
	{
	}

	public virtual bool OnMouseClick(MapEntityVisual visualOfSelectedEntity, Vec3 intersectionPoint, PathFaceRecord mouseOverFaceIndex, bool isDoubleClick)
	{
		return false;
	}

	public virtual bool OnVisualIntersected(Ray mouseRay, UIntPtr[] intersectedEntityIDs, Intersection[] intersectionInfos, int entityCount, Vec3 worldMouseNear, Vec3 worldMouseFar, Vec3 terrainIntersectionPoint, ref MapEntityVisual hoveredVisual, ref MapEntityVisual selectedVisual)
	{
		return false;
	}

	public virtual void OnFrameTick(float dt)
	{
	}

	public virtual void OnGameLoadFinished()
	{
	}

	public virtual void OnTick(float realDt, float dt)
	{
	}

	public virtual void ClearVisualMemory()
	{
	}

	void IEntityComponent.OnInitialize()
	{
		OnInitialize();
	}

	void IEntityComponent.OnFinalize()
	{
		OnFinalize();
	}

	protected virtual void OnInitialize()
	{
	}

	protected virtual void OnFinalize()
	{
	}
}
