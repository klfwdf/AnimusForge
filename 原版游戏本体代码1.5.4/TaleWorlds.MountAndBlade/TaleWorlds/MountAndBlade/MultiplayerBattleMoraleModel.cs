using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerBattleMoraleModel : BattleMoraleModel
{
	public override (float affectedSideMaxMoraleLoss, float affectorSideMaxMoraleGain) CalculateMaxMoraleChangeDueToAgentIncapacitated(Agent affectedAgent, AgentState affectedAgentState, Agent affectorAgent, in KillingBlow killingBlow)
	{
		return (affectedSideMaxMoraleLoss: 0f, affectorSideMaxMoraleGain: 0f);
	}

	public override (float affectedSideMaxMoraleLoss, float affectorSideMaxMoraleGain) CalculateMaxMoraleChangeDueToAgentPanicked(Agent agent)
	{
		return (affectedSideMaxMoraleLoss: 0f, affectorSideMaxMoraleGain: 0f);
	}

	public override float CalculateMoraleChangeToCharacter(Agent agent, float maxMoraleChange)
	{
		return 0f;
	}

	public override float GetEffectiveInitialMorale(Agent agent, float baseMorale)
	{
		return baseMorale;
	}

	public override bool CanPanicDueToMorale(Agent agent)
	{
		return true;
	}

	public override float CalculateCasualtiesFactor(BattleSideEnum battleSide)
	{
		return 1f;
	}

	public override float GetAverageMorale(Formation formation)
	{
		return 0f;
	}

	public override float CalculateMoraleChangeOnShipSunk(IShipOrigin shipOrigin)
	{
		return 0f;
	}

	public override float CalculateMoraleOnRamming(Agent agent, IShipOrigin rammingShip, IShipOrigin rammedShip)
	{
		return agent.GetMorale();
	}

	public override float CalculateMoraleOnShipsConnected(Agent agent, IShipOrigin ownerShip, IShipOrigin targetShip)
	{
		return agent.GetMorale();
	}
}
