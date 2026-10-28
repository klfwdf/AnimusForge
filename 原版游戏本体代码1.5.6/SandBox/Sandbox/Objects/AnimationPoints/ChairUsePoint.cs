using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.AnimationPoints;

public class ChairUsePoint : AnimationPoint
{
	private enum ChairAction
	{
		None,
		LeanOnTable,
		Drink,
		Eat
	}

	public bool NearTable;

	public string NearTableLoopAction = "";

	public string NearTablePairLoopAction = "";

	public bool Drink;

	public string DrinkLoopAction = "";

	public string DrinkPairLoopAction = "";

	public string DrinkRightHandItem = "";

	public string DrinkLeftHandItem = "";

	public bool Eat;

	public string EatLoopAction = "";

	public string EatPairLoopAction = "";

	public string EatRightHandItem = "";

	public string EatLeftHandItem = "";

	private ActionIndexCache _loopAction;

	private ActionIndexCache _pairLoopAction;

	private ActionIndexCache _nearTableLoopAction;

	private ActionIndexCache _nearTablePairLoopAction;

	private ActionIndexCache _drinkLoopAction;

	private ActionIndexCache _drinkPairLoopAction;

	private ActionIndexCache _eatLoopAction;

	private ActionIndexCache _eatPairLoopAction;

	protected override void SetActionCodes()
	{
		base.SetActionCodes();
		_loopAction = ActionIndexCache.Create(LoopStartAction);
		_pairLoopAction = ActionIndexCache.Create(PairLoopStartAction);
		_nearTableLoopAction = ActionIndexCache.Create(NearTableLoopAction);
		_nearTablePairLoopAction = ActionIndexCache.Create(NearTablePairLoopAction);
		_drinkLoopAction = ActionIndexCache.Create(DrinkLoopAction);
		_drinkPairLoopAction = ActionIndexCache.Create(DrinkPairLoopAction);
		_eatLoopAction = ActionIndexCache.Create(EatLoopAction);
		_eatPairLoopAction = ActionIndexCache.Create(EatPairLoopAction);
		SetChairAction(GetRandomChairAction());
	}

	protected override bool ShouldUpdateOnEditorVariableChanged(string variableName)
	{
		if (!base.ShouldUpdateOnEditorVariableChanged(variableName))
		{
			switch (variableName)
			{
			default:
				return variableName == "EatLoopAction";
			case "NearTable":
			case "Drink":
			case "Eat":
			case "NearTableLoopAction":
			case "DrinkLoopAction":
				break;
			}
		}
		return true;
	}

	public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
	{
		ChairAction chairAction = (CanAgentUseItem(userAgent) ? GetRandomChairAction() : ChairAction.None);
		SetChairAction(chairAction);
		base.OnUse(userAgent, agentBoneIndex);
	}

	private ChairAction GetRandomChairAction()
	{
		List<ChairAction> list = new List<ChairAction> { ChairAction.None };
		if (NearTable && _nearTableLoopAction != ActionIndexCache.act_none)
		{
			list.Add(ChairAction.LeanOnTable);
		}
		if (Drink && _drinkLoopAction != ActionIndexCache.act_none)
		{
			list.Add(ChairAction.Drink);
		}
		if (Eat && _eatLoopAction != ActionIndexCache.act_none)
		{
			list.Add(ChairAction.Eat);
		}
		return list[new Random().Next(list.Count)];
	}

	private void SetChairAction(ChairAction chairAction)
	{
		switch (chairAction)
		{
		case ChairAction.None:
			LoopStartActionCode = _loopAction;
			PairLoopStartActionCode = _pairLoopAction;
			base.SelectedRightHandItem = RightHandItem;
			base.SelectedLeftHandItem = LeftHandItem;
			break;
		case ChairAction.LeanOnTable:
			LoopStartActionCode = _nearTableLoopAction;
			PairLoopStartActionCode = _nearTablePairLoopAction;
			base.SelectedRightHandItem = string.Empty;
			base.SelectedLeftHandItem = string.Empty;
			break;
		case ChairAction.Drink:
			LoopStartActionCode = _drinkLoopAction;
			PairLoopStartActionCode = _drinkPairLoopAction;
			base.SelectedRightHandItem = DrinkRightHandItem;
			base.SelectedLeftHandItem = DrinkLeftHandItem;
			break;
		case ChairAction.Eat:
			LoopStartActionCode = _eatLoopAction;
			PairLoopStartActionCode = _eatPairLoopAction;
			base.SelectedRightHandItem = EatRightHandItem;
			base.SelectedLeftHandItem = EatLeftHandItem;
			break;
		}
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (base.UserAgent != null && !base.UserAgent.IsAIControlled && base.UserAgent.EventControlFlags.HasAnyFlag(Agent.EventControlFlag.Crouch | Agent.EventControlFlag.Stand))
		{
			base.UserAgent.StopUsingGameObject();
		}
	}
}
