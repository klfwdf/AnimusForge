using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class DailyBehaviorGroup : AgentBehaviorGroup
{
	public DailyBehaviorGroup(AgentNavigator navigator, Mission mission)
		: base(navigator, mission)
	{
	}

	public override void Tick(float dt, bool isSimulation)
	{
		if (!base.IsActive)
		{
			return;
		}
		if (base.ScriptedBehavior != null)
		{
			if (!base.ScriptedBehavior.IsActive)
			{
				DisableAllBehaviors();
				base.ScriptedBehavior.IsActive = true;
			}
		}
		else if (CheckBehaviorTimer == null || CheckBehaviorTimer.Check(base.Mission.CurrentTime))
		{
			Think(isSimulation);
		}
		TickActiveBehaviors(dt, isSimulation);
	}

	public override void ConversationTick()
	{
		foreach (AgentBehavior behavior in Behaviors)
		{
			if (behavior.IsActive)
			{
				behavior.ConversationTick();
			}
		}
	}

	private void Think(bool isSimulation)
	{
		float num = 0f;
		float[] array = new float[Behaviors.Count];
		for (int i = 0; i < Behaviors.Count; i++)
		{
			array[i] = Behaviors[i].GetAvailability(isSimulation);
			num += array[i];
		}
		if (!(num > 0f))
		{
			return;
		}
		float num2 = MBRandom.RandomFloat * num;
		for (int j = 0; j < array.Length; j++)
		{
			float num3 = array[j];
			num2 -= num3;
			if (num2 < 0f)
			{
				if (!Behaviors[j].IsActive)
				{
					DisableAllBehaviors();
					Behaviors[j].IsActive = true;
					CheckBehaviorTime = Behaviors[j].CheckTime;
					SetCheckBehaviorTimer(CheckBehaviorTime);
				}
				break;
			}
		}
	}

	private void TickActiveBehaviors(float dt, bool isSimulation)
	{
		foreach (AgentBehavior behavior in Behaviors)
		{
			if (behavior.IsActive)
			{
				behavior.Tick(dt, isSimulation);
			}
		}
	}

	private void SetCheckBehaviorTimer(float time)
	{
		if (CheckBehaviorTimer == null)
		{
			CheckBehaviorTimer = new Timer(base.Mission.CurrentTime, time);
		}
		else
		{
			CheckBehaviorTimer.Reset(base.Mission.CurrentTime, time);
		}
	}

	public override float GetScore(bool isSimulation)
	{
		return 0.5f;
	}

	public override void OnAgentRemoved(Agent agent)
	{
		if (!base.IsActive)
		{
			return;
		}
		foreach (AgentBehavior behavior in Behaviors)
		{
			if (behavior.IsActive)
			{
				behavior.OnAgentRemoved(agent);
			}
		}
	}

	protected override void OnActivate()
	{
		if (CampaignMission.Current.Location != null)
		{
			LocationCharacter locationCharacter = CampaignMission.Current.Location.GetLocationCharacter(base.OwnerAgent.Origin);
			if (locationCharacter != null && locationCharacter.ActionSetCode != locationCharacter.AlarmedActionSetCode)
			{
				AnimationSystemData animationSystemData = locationCharacter.GetAgentBuildData().AgentMonster.FillAnimationSystemData(MBGlobals.GetActionSet(locationCharacter.ActionSetCode), locationCharacter.Character.GetStepSize(), hasClippingPlane: false);
				base.OwnerAgent.SetActionSet(ref animationSystemData);
			}
		}
		Navigator.SetItemsVisibility(isVisible: true);
		Navigator.SetSpecialItem();
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		CheckBehaviorTimer = null;
	}

	public override void ForceThink(float inSeconds)
	{
		if (MathF.Abs(inSeconds) < float.Epsilon)
		{
			Think(isSimulation: false);
		}
		else
		{
			SetCheckBehaviorTimer(inSeconds);
		}
	}
}
