using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultExecutionRelationModel : ExecutionRelationModel
{
	public override int HeroKillingHeroClanRelationPenalty => -40;

	public override int HeroKillingHeroFriendRelationPenalty => -10;

	public override int PlayerExecutingHeroFactionRelationPenaltyDishonorable => -5;

	public override int PlayerExecutingHeroClanRelationPenaltyDishonorable => -30;

	public override int PlayerExecutingHeroFriendRelationPenaltyDishonorable => -15;

	public override int PlayerExecutingHeroHonorPenalty => -1000;

	public override int PlayerExecutingHeroFactionRelationPenalty => -10;

	public override int PlayerExecutingHeroHonorableNobleRelationPenalty => -10;

	public override int PlayerExecutingHeroClanRelationPenalty => -60;

	public override int PlayerExecutingHeroFriendRelationPenalty => -30;

	public override int GetRelationChangeForExecutingHero(Hero victim, Hero hero, out bool showQuickNotification)
	{
		int result = 0;
		showQuickNotification = false;
		if (victim.GetTraitLevel(DefaultTraits.Honor) < 0)
		{
			if (!hero.IsHumanPlayerCharacter && hero != victim && hero.Clan != null && hero.Clan.Leader == hero)
			{
				if (hero.Clan == victim.Clan)
				{
					result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroClanRelationPenaltyDishonorable;
					showQuickNotification = true;
				}
				else if (victim.IsFriend(hero))
				{
					result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroFriendRelationPenaltyDishonorable;
					showQuickNotification = true;
				}
				else if (hero.MapFaction == victim.MapFaction && hero.CharacterObject.Occupation == Occupation.Lord)
				{
					result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroFactionRelationPenaltyDishonorable;
					showQuickNotification = true;
				}
			}
		}
		else if (!hero.IsHumanPlayerCharacter && hero != victim && hero.Clan != null && hero.Clan.Leader == hero)
		{
			if (hero.Clan == victim.Clan)
			{
				result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroClanRelationPenalty;
				showQuickNotification = true;
			}
			else if (victim.IsFriend(hero))
			{
				result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroFriendRelationPenalty;
				showQuickNotification = true;
			}
			else if (hero.MapFaction == victim.MapFaction && hero.CharacterObject.Occupation == Occupation.Lord)
			{
				result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroFactionRelationPenalty;
				showQuickNotification = false;
			}
			else if (hero.GetTraitLevel(DefaultTraits.Honor) > 0 && !victim.Clan.IsRebelClan)
			{
				result = Campaign.Current.Models.ExecutionRelationModel.PlayerExecutingHeroHonorableNobleRelationPenalty;
				showQuickNotification = true;
			}
		}
		return result;
	}
}
