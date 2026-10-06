using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class AgentBindsMachine : UsableMachine
{
	private readonly ActionIndexCache _breakChainsShortAction = ActionIndexCache.Create("act_cutscene_break_chains_short");

	public ShipOarMachine ShipOarMachine { get; private set; }

	public bool HasCaptive => ShipOarMachine.PilotStandingPoint.HasUser;

	public void SetOarMachine(ShipOarMachine shipOarMachine)
	{
		ShipOarMachine = shipOarMachine;
	}

	protected override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
		base.PilotStandingPoint.AddComponent(new ResetAnimationOnStopUsageComponent(ActionIndexCache.act_none, alwaysResetWithAction: false));
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	protected override void OnTick(float dt)
	{
		Agent agent = ShipOarMachine?.PilotAgent;
		base.PilotStandingPoint.SetIsDeactivatedSynched(agent == null);
		if (base.PilotAgent == null)
		{
			return;
		}
		if (base.PilotAgent.SetActionChannel(0, in _breakChainsShortAction, ignorePriority: false, (AnimFlags)0uL))
		{
			if (base.PilotAgent.GetCurrentActionProgress(0) > 0.99f)
			{
				base.PilotAgent.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL);
				base.PilotAgent.StopUsingGameObject();
				if (agent != null)
				{
					agent.StopUsingGameObject();
					agent.ClearHandInverseKinematics();
				}
			}
		}
		else
		{
			base.PilotAgent.StopUsingGameObject();
			agent.MakeVoice(SkinVoiceManager.VoiceType.MpThanks, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
		}
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = new TextObject("{=fEQAPJ2e}{KEY} Use");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=ut9C8hA9}Chains");
	}
}
