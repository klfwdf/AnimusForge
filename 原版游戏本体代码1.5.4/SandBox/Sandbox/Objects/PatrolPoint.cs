using System;
using System.Collections.Generic;
using SandBox.Objects.AnimationPoints;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects;

public class PatrolPoint : StandingPoint
{
	public readonly int WaitDuration;

	public readonly int WaitDeviation;

	public readonly int Index;

	public readonly string SpawnGroupTag;

	public readonly bool IsInfiniteWaitPoint;

	public readonly float PatrollingSpeed = -1f;

	public string LoopAction = "";

	private ActionIndexCache _loopAction;

	public string RightHandItem = "";

	public HumanBone RightHandItemBone = HumanBone.ItemR;

	public string LeftHandItem = "";

	public HumanBone LeftHandItemBone = HumanBone.ItemL;

	private List<AnimationPoint.ItemForBone> _itemsForBones;

	private string _selectedRightHandItem;

	private string _selectedLeftHandItem;

	protected string SelectedRightHandItem
	{
		get
		{
			return _selectedRightHandItem;
		}
		set
		{
			if (value != _selectedRightHandItem)
			{
				AnimationPoint.ItemForBone newItem = new AnimationPoint.ItemForBone(RightHandItemBone, value, isVisible: false);
				AssignItemToBone(newItem);
				_selectedRightHandItem = value;
			}
		}
	}

	protected string SelectedLeftHandItem
	{
		get
		{
			return _selectedLeftHandItem;
		}
		set
		{
			if (value != _selectedLeftHandItem)
			{
				AnimationPoint.ItemForBone newItem = new AnimationPoint.ItemForBone(LeftHandItemBone, value, isVisible: false);
				AssignItemToBone(newItem);
				_selectedLeftHandItem = value;
			}
		}
	}

	protected void AssignItemToBone(AnimationPoint.ItemForBone newItem)
	{
		if (!string.IsNullOrEmpty(newItem.ItemPrefabName) && !_itemsForBones.Contains(newItem))
		{
			_itemsForBones.Add(newItem);
		}
	}

	public void SetAgentItemsVisibility(bool isVisible)
	{
		if (base.UserAgent.IsMainAgent)
		{
			return;
		}
		foreach (AnimationPoint.ItemForBone itemsForBone in _itemsForBones)
		{
			sbyte realBoneIndex = base.UserAgent.AgentVisuals.GetRealBoneIndex(itemsForBone.HumanBone);
			base.UserAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SetPrefabVisibility(realBoneIndex, itemsForBone.ItemPrefabName, isVisible);
			itemsForBone.IsVisible = isVisible;
		}
	}

	public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
	{
		base.OnUse(userAgent, agentBoneIndex);
		base.UserAgent.SetActionChannel(0, in _loopAction, ignorePriority: false, (AnimFlags)0uL);
		SetAgentItemsVisibility(isVisible: true);
	}

	public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
	{
		base.UserAgent.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: false, (AnimFlags)Math.Min(base.UserAgent.GetCurrentActionPriority(0), 73));
		SetAgentItemsVisibility(isVisible: false);
		base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);
	}

	protected override void OnInit()
	{
		base.OnInit();
		_itemsForBones = new List<AnimationPoint.ItemForBone>();
		_loopAction = ActionIndexCache.Create(LoopAction);
		SelectedRightHandItem = RightHandItem;
		SelectedLeftHandItem = LeftHandItem;
	}

	protected override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		_itemsForBones = new List<AnimationPoint.ItemForBone>();
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return null;
	}
}
