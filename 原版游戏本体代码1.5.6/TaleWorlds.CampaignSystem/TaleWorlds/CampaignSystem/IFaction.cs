using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem;

[SaveableInterface(22001)]
public interface IFaction
{
	TextObject Name { get; }

	string StringId { get; }

	MBGUID Id { get; }

	TextObject InformalName { get; }

	string EncyclopediaLink { get; }

	TextObject EncyclopediaLinkWithName { get; }

	TextObject EncyclopediaText { get; }

	CultureObject Culture { get; }

	Settlement InitialHomeSettlement { get; }

	uint Color { get; }

	uint Color2 { get; }

	CharacterObject BasicTroop { get; }

	Hero Leader { get; }

	Banner Banner { get; }

	MBReadOnlyList<Settlement> Settlements { get; }

	MBReadOnlyList<Town> Fiefs { get; }

	MBReadOnlyList<Hero> AliveLords { get; }

	MBReadOnlyList<Hero> DeadLords { get; }

	MBReadOnlyList<Hero> Heroes { get; }

	MBReadOnlyList<WarPartyComponent> WarPartyComponents { get; }

	bool IsBanditFaction { get; }

	bool IsMinorFaction { get; }

	bool IsKingdomFaction { get; }

	bool IsRebelClan { get; }

	bool IsClan { get; }

	bool IsOutlaw { get; }

	bool IsMapFaction { get; }

	bool HasNavalNavigationCapability { get; }

	IFaction MapFaction { get; }

	float CurrentTotalStrength { get; }

	Settlement FactionMidSettlement { get; }

	float DistanceToClosestNonAllyFortification { get; }

	MBReadOnlyList<IFaction> FactionsAtWarWith { get; }

	int TributeWallet { get; set; }

	float MainHeroCrimeRating { get; set; }

	float DailyCrimeRatingChange { get; }

	float Aggressiveness { get; }

	bool IsEliminated { get; }

	ExplainedNumber DailyCrimeRatingChangeExplained { get; }

	CampaignTime NotAttackableByPlayerUntilTime { get; set; }

	bool IsAtWarWith(IFaction other);

	StanceLink GetStanceWith(IFaction other);

	void UpdateFactionsAtWarWith();
}
