using System.Collections.Generic;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class LightCycle : ScriptComponentBehavior
{
	public bool alwaysBurn;

	private bool visibility;

	private void SetVisibility()
	{
		Light light = base.GameEntity.GetLight();
		float timeOfDay = base.Scene.TimeOfDay;
		visibility = timeOfDay < 6f || timeOfDay > 20f || base.Scene.IsAtmosphereIndoor || alwaysBurn;
		if (light != null)
		{
			light.SetVisibility(visibility);
		}
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			child.SetVisibilityExcludeParents(visibility);
		}
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		SetVisibility();
		if (!visibility)
		{
			List<WeakGameEntity> children = new List<WeakGameEntity>();
			base.GameEntity.GetChildrenRecursive(ref children);
			for (int num = children.Count - 1; num >= 0; num--)
			{
				base.Scene.RemoveEntity(children[num], 0);
			}
			base.GameEntity.RemoveScriptComponent(base.ScriptComponent.Pointer, 0);
		}
	}

	protected internal override void OnEditorTick(float dt)
	{
		SetVisibility();
	}

	protected internal override bool MovesEntity()
	{
		return false;
	}
}
