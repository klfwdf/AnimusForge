using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace TaleWorlds.MountAndBlade.Objects.Usables;

public class SiegeMachineStonePile : UsableMachine, ISpawnable
{
	private bool _spawnedFromSpawner;

	protected internal override void OnInit()
	{
		base.OnInit();
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		if (usableGameObject.GameEntity.HasTag(AmmoPickUpTag))
		{
			TextObject textObject = new TextObject("{=jfcceEoE}{PILE_TYPE} Pile");
			textObject.SetTextVariable("PILE_TYPE", new TextObject("{=1CPdu9K0}Stone"));
			return textObject;
		}
		return null;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		if (gameEntity.HasTag(AmmoPickUpTag))
		{
			TextObject textObject = new TextObject("{=bNYm3K6b}{KEY} Pick Up");
			textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
			return textObject;
		}
		return null;
	}

	public void SetSpawnedFromSpawner()
	{
		_spawnedFromSpawner = true;
	}

	public override OrderType GetOrder(BattleSideEnum side)
	{
		return OrderType.None;
	}
}
