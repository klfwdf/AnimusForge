using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class TrainingIcon : UsableMachine
{
	private const string HighlightBeamTag = "highlight_beam";

	private const float MarkerAlphaChangeAmount = 110f;

	private bool _activated;

	private float _markerAlpha;

	private float _targetMarkerAlpha;

	private List<GameEntity> _weaponIcons = new List<GameEntity>();

	private GameEntity _markerBeam;

	[EditableScriptComponentVariable(true, "")]
	private string _descriptionTextOfIcon = "";

	[EditableScriptComponentVariable(true, "")]
	private string _trainingSubTypeTag = "";

	public bool Focused { get; private set; }

	protected internal override void OnInit()
	{
		base.OnInit();
		_markerBeam = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("highlight_beam"));
		_weaponIcons = (from x in TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity).GetChildren()
			where !x.GetScriptComponents().Any() && x != _markerBeam
			select x).ToList();
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_markerBeam != null)
		{
			if (MathF.Abs(_markerAlpha - _targetMarkerAlpha) > dt * 110f)
			{
				_markerAlpha += dt * 110f * (float)MathF.Sign(_targetMarkerAlpha - _markerAlpha);
				_markerBeam.GetChild(0).GetFirstMesh().SetVectorArgument(_markerAlpha, 1f, 0.49f, 11.65f);
			}
			else
			{
				_markerAlpha = _targetMarkerAlpha;
				if (_targetMarkerAlpha == 0f)
				{
					_markerBeam?.SetVisibilityExcludeParents(visible: false);
				}
			}
		}
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			if (!standingPoint.HasUser)
			{
				continue;
			}
			Agent userAgent = standingPoint.UserAgent;
			ActionIndexCache currentAction = userAgent.GetCurrentAction(0);
			ActionIndexCache currentAction2 = userAgent.GetCurrentAction(1);
			if (!(currentAction2 == ActionIndexCache.act_none) || (!(currentAction == ActionIndexCache.act_pickup_middle_begin) && !(currentAction == ActionIndexCache.act_pickup_middle_begin_left_stance)))
			{
				if (currentAction2 == ActionIndexCache.act_none && (currentAction == ActionIndexCache.act_pickup_middle_end || currentAction == ActionIndexCache.act_pickup_middle_end_left_stance))
				{
					_activated = true;
					userAgent.StopUsingGameObject();
				}
				else if (currentAction2 != ActionIndexCache.act_none || !userAgent.SetActionChannel(0, userAgent.GetIsLeftStance() ? ActionIndexCache.act_pickup_middle_begin_left_stance : ActionIndexCache.act_pickup_middle_begin, ignorePriority: false, (AnimFlags)0uL))
				{
					userAgent.StopUsingGameObject();
				}
			}
		}
	}

	public void SetMarked(bool highlight)
	{
		if (highlight)
		{
			_targetMarkerAlpha = 75f;
			_markerBeam.GetChild(0).GetFirstMesh().SetVectorArgument(_markerAlpha, 1f, 0.49f, 11.65f);
			_markerBeam?.SetVisibilityExcludeParents(visible: true);
		}
		else
		{
			_targetMarkerAlpha = 0f;
		}
	}

	public bool GetIsActivated()
	{
		bool activated = _activated;
		_activated = false;
		return activated;
	}

	public string GetTrainingSubTypeTag()
	{
		return _trainingSubTypeTag;
	}

	public void DisableIcon()
	{
		foreach (GameEntity weaponIcon in _weaponIcons)
		{
			weaponIcon.SetVisibilityExcludeParents(visible: false);
		}
	}

	public void EnableIcon()
	{
		foreach (GameEntity weaponIcon in _weaponIcons)
		{
			weaponIcon.SetVisibilityExcludeParents(visible: true);
		}
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		TextObject textObject = new TextObject("{=!}{TRAINING_TYPE}");
		textObject.SetTextVariable("TRAINING_TYPE", GameTexts.FindText("str_tutorial_" + _descriptionTextOfIcon));
		return textObject;
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject = null)
	{
		TextObject textObject = new TextObject("{=wY1qP2qj}{KEY} Select");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override void OnFocusGain(Agent userAgent)
	{
		base.OnFocusGain(userAgent);
		Focused = true;
	}

	public override void OnFocusLose(Agent userAgent)
	{
		base.OnFocusLose(userAgent);
		Focused = false;
	}
}
