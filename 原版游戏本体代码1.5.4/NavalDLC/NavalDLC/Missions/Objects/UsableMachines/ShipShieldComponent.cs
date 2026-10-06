using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class ShipShieldComponent : DestructableComponent
{
	private List<GameEntity> _disablingConnectionEntities = new List<GameEntity>();

	public override bool IsFocusable => false;

	private ShipShieldComponent()
	{
	}

	protected override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
	}

	public void RegisterRampEntityDisablingShield(GameEntity connectionEntity)
	{
		if (_disablingConnectionEntities.Count == 0)
		{
			base.GameEntity.SetVisibilityExcludeParents(visible: false);
		}
		_disablingConnectionEntities.Add(connectionEntity);
	}

	public void DeregisterRampEntityDisablingShield(GameEntity connectionEntity)
	{
		if (_disablingConnectionEntities.Remove(connectionEntity) && _disablingConnectionEntities.Count == 0)
		{
			base.GameEntity.SetVisibilityExcludeParents(visible: true);
		}
	}
}
