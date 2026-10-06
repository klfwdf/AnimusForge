using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.MissionObjects;

public class ShipDoorUsePoint : UsableMissionObject
{
	private const string ShipDoorHighlightTag = "ship_door_highlight";

	private GameEntity _highlight;

	private bool _isEnabled;

	[EditableScriptComponentVariable(true, "ActionStringId")]
	private string _actionStringId;

	[EditableScriptComponentVariable(true, "DescriptionStringId")]
	private string _descriptionStringId;

	public ShipDoorUsePoint()
	{
		_actionStringId = string.Empty;
		_descriptionStringId = string.Empty;
	}

	protected override void OnInit()
	{
		base.OnInit();
		_isEnabled = false;
		ActionMessage = GameTexts.FindText(string.IsNullOrEmpty(_actionStringId) ? "str_open_ship_door" : _actionStringId);
		ActionMessage.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		DescriptionMessage = GameTexts.FindText(string.IsNullOrEmpty(_descriptionStringId) ? "str_ui_door" : _descriptionStringId);
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return DescriptionMessage;
	}

	public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
	{
		base.OnUse(userAgent, agentBoneIndex);
		if (userAgent.IsMainAgent)
		{
			SoundManager.StartOneShotEvent("event:/mission/movement/foley/door_open", userAgent.Position);
			userAgent.StopUsingGameObject();
		}
	}

	public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
	{
		base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);
		if (LockUserFrames || LockUserPositions)
		{
			userAgent.ClearTargetFrame();
		}
	}

	public override bool IsDisabledForAgent(Agent agent)
	{
		if (!_isEnabled)
		{
			return !agent.IsMainAgent;
		}
		return false;
	}

	public override bool IsUsableByAgent(Agent userAgent)
	{
		if (_isEnabled && userAgent.IsMainAgent)
		{
			return base.GameEntity.GlobalPosition.Distance(Agent.Main.Position) <= 2f;
		}
		return false;
	}

	public void SetShipDoorUsePointEnabled(bool isEnabled)
	{
		if (_isEnabled == isEnabled && !(_highlight == null))
		{
			return;
		}
		_isEnabled = isEnabled;
		if (_highlight == null)
		{
			foreach (WeakGameEntity child in base.GameEntity.GetChildren())
			{
				if (child.HasTag("ship_door_highlight"))
				{
					_highlight = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child);
				}
			}
		}
		_highlight?.SetVisibilityExcludeParents(visible: false);
	}
}
