using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects;

public class TeleportUsePoint : StandingPoint
{
	public enum TeleportType
	{
		Lair,
		Door,
		Gate
	}

	public TeleportType TypeOfTeleport;

	public string TargetPointTag;

	public bool IsLeave;

	private const float LairInteractionDistance = 0.5f;

	private const float GateInteractionDistance = 2.5f;

	public override bool HasAIMovingTo => false;

	public TeleportUsePoint()
	{
		base.IsInstantUse = true;
		LockUserFrames = false;
		LockUserFrames = false;
	}

	public override bool IsAIMovingTo(Agent agent)
	{
		return false;
	}

	protected override void OnInit()
	{
		DescriptionMessage = TextObject.GetEmpty();
		if (IsLeave)
		{
			ActionMessage = GameTexts.FindText("str_mission_exit");
			return;
		}
		switch (TypeOfTeleport)
		{
		case TeleportType.Lair:
			ActionMessage = GameTexts.FindText("str_ui_lair");
			break;
		case TeleportType.Door:
			ActionMessage = GameTexts.FindText("str_ui_door");
			break;
		case TeleportType.Gate:
			ActionMessage = new TextObject("{=6wZUG0ev}Gate");
			break;
		}
	}

	public override bool IsUsableByAgent(Agent userAgent)
	{
		if (userAgent.IsPlayerControlled && !base.IsDeactivated)
		{
			float num = InteractionEntity.GetGlobalFrame().origin.AsVec2.DistanceSquared(userAgent.Position.AsVec2);
			float interactionDistance = GetInteractionDistance();
			return num <= interactionDistance * interactionDistance;
		}
		return false;
	}

	public override bool IsDisabledForAgent(Agent agent)
	{
		if (agent.IsPlayerControlled && !base.IsDisabledForPlayers)
		{
			return base.IsDeactivated;
		}
		return true;
	}

	protected override void OnTick(float dt)
	{
	}

	public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
	{
		if (!base.IsDeactivated && (Campaign.Current.GameMode == CampaignGameMode.Campaign || userAgent.IsPlayerControlled))
		{
			base.OnUse(userAgent, agentBoneIndex);
			GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag(TargetPointTag);
			userAgent.TeleportToPosition(gameEntity.GetGlobalFrame().origin.ToWorldPosition().GetGroundVec3());
			userAgent.FadeIn();
		}
	}

	public void Deactivate()
	{
		base.IsDeactivated = true;
		ActionMessage = TextObject.GetEmpty();
	}

	public void Activate()
	{
		base.IsDeactivated = false;
		OnInit();
	}

	public override void OnFocusGain(Agent userAgent)
	{
		if (!base.IsDeactivated)
		{
			base.OnFocusGain(userAgent);
		}
	}

	private float GetInteractionDistance()
	{
		if (TypeOfTeleport == TeleportType.Lair)
		{
			return 0.5f;
		}
		return 2.5f;
	}
}
