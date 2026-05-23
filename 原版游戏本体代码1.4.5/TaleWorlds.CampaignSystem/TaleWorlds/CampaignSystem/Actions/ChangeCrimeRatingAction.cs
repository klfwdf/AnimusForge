using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Actions;

public static class ChangeCrimeRatingAction
{
	private static void ApplyInternal(IFaction faction, float deltaCrimeRating, bool showNotification)
	{
		float num = MBMath.ClampFloat(faction.MainHeroCrimeRating + deltaCrimeRating, 0f, Campaign.Current.Models.CrimeModel.GetMaxCrimeRating());
		deltaCrimeRating = num - faction.MainHeroCrimeRating;
		if (showNotification && !deltaCrimeRating.ApproximatelyEqualsTo(0f))
		{
			TextObject textObject = new TextObject("{=hwq0RMRN}Your criminal rating with {FACTION_NAME} has {?IS_INCREASED}increased{?}decreased{\\?} by {CHANGE} to {NEW_RATING}");
			textObject.SetTextVariable("CHANGE", MathF.Round(MathF.Abs(deltaCrimeRating)));
			textObject.SetTextVariable("IS_INCREASED", (deltaCrimeRating > 0f) ? 1 : 0);
			textObject.SetTextVariable("FACTION_NAME", faction.Name);
			textObject.SetTextVariable("NEW_RATING", MathF.Round(num));
			MBInformationManager.AddQuickInformation(textObject);
		}
		faction.MainHeroCrimeRating = num;
		if (num > Campaign.Current.Models.CrimeModel.DeclareWarCrimeRatingThreshold && Hero.MainHero.MapFaction.Leader == Hero.MainHero && !faction.IsAtWarWith(Hero.MainHero.MapFaction) && Hero.MainHero.MapFaction != faction)
		{
			ChangeRelationAction.ApplyPlayerRelation(faction.Leader, -10);
			DeclareWarAction.ApplyByCrimeRatingChange(faction, Hero.MainHero.MapFaction);
		}
		CampaignEventDispatcher.Instance.OnCrimeRatingChanged(faction, deltaCrimeRating);
	}

	public static void Apply(IFaction faction, float deltaCrimeRating, bool showNotification = true)
	{
		ApplyInternal(faction, deltaCrimeRating, showNotification);
	}
}
