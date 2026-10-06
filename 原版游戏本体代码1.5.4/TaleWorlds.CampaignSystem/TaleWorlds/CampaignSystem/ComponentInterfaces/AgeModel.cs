using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class AgeModel : MBGameModel<AgeModel>
{
	public abstract int BecomeInfantAge { get; }

	public abstract int BecomeChildAge { get; }

	public abstract int BecomeTeenagerAge { get; }

	public abstract int HeroComesOfAge { get; }

	public abstract int BecomeOldAge { get; }

	public abstract int MiddleAdultHoodAge { get; }

	public abstract int MaxAge { get; }

	public abstract void GetAgeLimitForLocation(CharacterObject character, out int minimumAge, out int maximumAge, string additionalTags = "");
}
