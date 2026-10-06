namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class AttractedToPlayerTag : ConversationTag
{
	public const string Id = "AttractedToPlayerTag";

	private const int MinimumFlirtPercentageForComment = 70;

	public override string StringId => "AttractedToPlayerTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		Hero heroObject = character.HeroObject;
		if (heroObject != null && Hero.MainHero.IsFemale != heroObject.IsFemale && !FactionManager.IsAtWarAgainstFaction(heroObject.MapFaction, Hero.MainHero.MapFaction) && Campaign.Current.Models.RomanceModel.GetAttractionValuePercentage(heroObject, Hero.MainHero) > 70 && heroObject.Spouse == null)
		{
			return Hero.MainHero.Spouse == null;
		}
		return false;
	}
}
