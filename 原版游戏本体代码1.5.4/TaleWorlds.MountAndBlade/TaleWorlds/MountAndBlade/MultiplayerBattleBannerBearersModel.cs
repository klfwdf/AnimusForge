using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerBattleBannerBearersModel : BattleBannerBearersModel
{
	public override int GetMinimumFormationTroopCountToBearBanners()
	{
		return int.MaxValue;
	}

	public override float GetBannerInteractionDistance(Agent interactingAgent)
	{
		return float.MaxValue;
	}

	public override bool CanAgentPickUpAnyBanner(Agent agent)
	{
		return false;
	}

	public override bool CanBannerBearerProvideEffectToFormation(Agent agent, Formation formation)
	{
		return false;
	}

	public override bool CanAgentBecomeBannerBearer(Agent agent)
	{
		return false;
	}

	public override int GetAgentBannerBearingPriority(Agent agent)
	{
		return 0;
	}

	public override bool CanFormationDeployBannerBearers(Formation formation)
	{
		return false;
	}

	public override int GetDesiredNumberOfBannerBearersForFormation(Formation formation)
	{
		return 0;
	}

	public override ItemObject GetBannerBearerReplacementWeapon(BasicCharacterObject agentCharacter)
	{
		return null;
	}
}
