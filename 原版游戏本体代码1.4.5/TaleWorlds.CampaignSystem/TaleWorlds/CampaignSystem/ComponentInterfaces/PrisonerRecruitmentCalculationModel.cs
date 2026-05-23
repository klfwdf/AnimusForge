using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PrisonerRecruitmentCalculationModel : MBGameModel<PrisonerRecruitmentCalculationModel>
{
	public abstract int GetConformityNeededToRecruitPrisoner(CharacterObject character);

	public abstract ExplainedNumber GetConformityChangePerHour(PartyBase party, CharacterObject character);

	public abstract int GetPrisonerRecruitmentMoraleEffect(PartyBase party, CharacterObject character, int num);

	public abstract bool IsPrisonerRecruitable(PartyBase party, CharacterObject character, out int conformityNeeded);

	public abstract bool ShouldPartyRecruitPrisoners(PartyBase party);

	public abstract int CalculateRecruitableNumber(PartyBase party, CharacterObject character);
}
