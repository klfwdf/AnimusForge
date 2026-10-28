using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.ComponentInterfaces;

public abstract class BattleBannerBearersModel : MBGameModel<BattleBannerBearersModel>
{
	public const float DefaultDetachmentCostMultiplier = 10f;

	private BannerBearerLogic _bannerBearerLogic;

	protected BannerBearerLogic BannerBearerLogic => _bannerBearerLogic;

	public void InitializeModel(BannerBearerLogic bannerBearerLogic)
	{
		_bannerBearerLogic = bannerBearerLogic;
	}

	public void FinalizeModel()
	{
		_bannerBearerLogic = null;
	}

	public bool IsFormationBanner(Formation formation, SpawnedItemEntity item)
	{
		if (formation == null)
		{
			return false;
		}
		return BannerBearerLogic?.IsFormationBanner(formation, item) ?? false;
	}

	public bool IsBannerSearchingAgent(Agent agent)
	{
		return BannerBearerLogic?.IsBannerSearchingAgent(agent) ?? false;
	}

	public bool IsInteractableFormationBanner(SpawnedItemEntity item, Agent interactingAgent)
	{
		Formation formation = BannerBearerLogic?.GetFormationFromBanner(item);
		if (formation != null)
		{
			if (formation.Captain != interactingAgent && interactingAgent.Formation != formation)
			{
				if (interactingAgent.IsPlayerControlled)
				{
					return interactingAgent.Team == formation.Team;
				}
				return false;
			}
			return true;
		}
		return true;
	}

	public bool HasFormationBanner(Formation formation)
	{
		if (formation == null)
		{
			return false;
		}
		return BannerBearerLogic?.GetFormationBanner(formation) != null;
	}

	public bool HasBannerOnGround(Formation formation)
	{
		if (formation == null)
		{
			return false;
		}
		return BannerBearerLogic?.HasBannerOnGround(formation) ?? false;
	}

	public ItemObject GetFormationBanner(Formation formation)
	{
		if (formation == null)
		{
			return null;
		}
		return BannerBearerLogic?.GetFormationBanner(formation);
	}

	public List<Agent> GetFormationBannerBearers(Formation formation)
	{
		if (formation == null)
		{
			return new List<Agent>();
		}
		BannerBearerLogic bannerBearerLogic = BannerBearerLogic;
		if (bannerBearerLogic != null)
		{
			return bannerBearerLogic.GetFormationBannerBearers(formation);
		}
		return new List<Agent>();
	}

	public BannerComponent GetActiveBanner(Formation formation)
	{
		if (formation == null)
		{
			return null;
		}
		return BannerBearerLogic?.GetActiveBanner(formation);
	}

	public abstract int GetMinimumFormationTroopCountToBearBanners();

	public abstract float GetBannerInteractionDistance(Agent interactingAgent);

	public abstract bool CanBannerBearerProvideEffectToFormation(Agent agent, Formation formation);

	public abstract bool CanAgentPickUpAnyBanner(Agent agent);

	public abstract bool CanAgentBecomeBannerBearer(Agent agent);

	public abstract int GetAgentBannerBearingPriority(Agent agent);

	public abstract bool CanFormationDeployBannerBearers(Formation formation);

	public abstract int GetDesiredNumberOfBannerBearersForFormation(Formation formation);

	public abstract ItemObject GetBannerBearerReplacementWeapon(BasicCharacterObject agentCharacter);
}
