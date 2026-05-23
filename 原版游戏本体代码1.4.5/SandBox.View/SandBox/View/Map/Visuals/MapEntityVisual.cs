using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace SandBox.View.Map.Visuals;

public abstract class MapEntityVisual
{
	public MapScreen MapScreen => MapScreen.Instance;

	public abstract CampaignVec2 InteractionPositionForPlayer { get; }

	public abstract MapEntityVisual AttachedTo { get; }

	public virtual bool IsMobileEntity => false;

	public virtual MatrixFrame CircleLocalFrame { get; protected set; }

	public virtual bool IsMainEntity => false;

	public virtual float BearingRotation { get; }

	public abstract bool OnMapClick(bool followModifierUsed);

	public abstract void OnHover();

	public abstract void OnOpenEncyclopedia();

	public abstract bool IsVisibleOrFadingOut();

	public abstract Vec3 GetVisualPosition();

	public virtual void ReleaseResources()
	{
	}

	public virtual void OnHoverEnd()
	{
	}

	public virtual void OnTrackAction()
	{
	}

	public virtual bool IsEnemyOf(IFaction faction)
	{
		return false;
	}

	public virtual bool IsAllyOf(IFaction faction)
	{
		return false;
	}

	public virtual bool IsInSameFaction(IFaction faction)
	{
		return false;
	}
}
public abstract class MapEntityVisual<T> : MapEntityVisual
{
	public T MapEntity { get; private set; }

	public MapEntityVisual(T entity)
	{
		MapEntity = entity;
	}
}
