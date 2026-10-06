using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.BarterSystem.Barterables;

public class MarriageBarterable : Barterable
{
	[SaveableField(600)]
	public readonly Hero ProposingHero;

	[SaveableField(601)]
	public readonly Hero HeroBeingProposedTo;

	public override string StringID => "marriage_barterable";

	public override TextObject Name
	{
		get
		{
			StringHelpers.SetCharacterProperties("HERO_BEING_PROPOSED_TO", HeroBeingProposedTo.CharacterObject);
			StringHelpers.SetCharacterProperties("HERO_TO_MARRY", ProposingHero.CharacterObject);
			return new TextObject("{=rv6hk8X2}{HERO_BEING_PROPOSED_TO.NAME} to marry {HERO_TO_MARRY.NAME}");
		}
	}

	public MarriageBarterable(Hero owner, PartyBase ownerParty, Hero heroBeingProposedTo, Hero proposingHero)
		: base(owner, ownerParty)
	{
		HeroBeingProposedTo = heroBeingProposedTo;
		ProposingHero = proposingHero;
	}

	public override int GetUnitValueForFaction(IFaction faction)
	{
		if (faction == ProposingHero.Clan)
		{
			float num = ProposingHero.RandomInt(10000);
			float num2 = ProposingHero.RandomInt(-25000, 25000) + HeroBeingProposedTo.RandomInt(-25000, 25000);
			if (ProposingHero == Hero.MainHero)
			{
				num2 = 0f;
				num = 0f;
			}
			float num3 = ProposingHero.GetRelation(HeroBeingProposedTo) * 1000;
			Campaign.Current.Models.DiplomacyModel.GetHeroCommandingStrengthForClan(ProposingHero);
			Campaign.Current.Models.DiplomacyModel.GetHeroCommandingStrengthForClan(HeroBeingProposedTo);
			float num4 = ((ProposingHero.Clan == null) ? 0f : ((float)ProposingHero.Clan.Tier + ((ProposingHero.Clan.Leader == ProposingHero.MapFaction.Leader) ? (MathF.Min(3f, (float)ProposingHero.MapFaction.Fiefs.Count / 10f) + 0.5f) : 0f)));
			float num5 = ((HeroBeingProposedTo.Clan == null) ? 0f : ((float)HeroBeingProposedTo.Clan.Tier + ((HeroBeingProposedTo.Clan.Leader == HeroBeingProposedTo.MapFaction.Leader) ? (MathF.Min(3f, (float)HeroBeingProposedTo.MapFaction.Fiefs.Count / 10f) + 0.5f) : 0f)));
			float num6 = ((faction == ProposingHero.Clan) ? ((num5 - num4) * MathF.Abs(num5 - num4) * 1000f) : ((num4 - num5) * MathF.Abs(num4 - num5) * 1000f));
			int relationBetweenClans = FactionManager.GetRelationBetweenClans(HeroBeingProposedTo.Clan, ProposingHero.Clan);
			int num7 = 1000 * relationBetweenClans;
			Clan clanAfterMarriage = Campaign.Current.Models.MarriageModel.GetClanAfterMarriage(HeroBeingProposedTo, ProposingHero);
			float num8 = 0f;
			float num9 = 0f;
			if (clanAfterMarriage != HeroBeingProposedTo.Clan)
			{
				if (faction == clanAfterMarriage)
				{
					num8 = Campaign.Current.Models.DiplomacyModel.GetValueOfHeroForFaction(HeroBeingProposedTo, clanAfterMarriage, forMarriage: true);
				}
				else if (faction == HeroBeingProposedTo.Clan)
				{
					num8 = Campaign.Current.Models.DiplomacyModel.GetValueOfHeroForFaction(HeroBeingProposedTo, HeroBeingProposedTo.Clan, forMarriage: true);
				}
				if (clanAfterMarriage.Kingdom != null && clanAfterMarriage.Kingdom != HeroBeingProposedTo.Clan.Kingdom)
				{
					num9 = Campaign.Current.Models.DiplomacyModel.GetValueOfHeroForFaction(HeroBeingProposedTo, clanAfterMarriage.Kingdom, forMarriage: true);
				}
			}
			float num10 = Campaign.Current.Models.AgeModel.HeroComesOfAge;
			float num11 = 2f * MathF.Min(0f, 20f - MathF.Max(HeroBeingProposedTo.Age - num10, 0f)) * MathF.Min(0f, 20f - MathF.Max(HeroBeingProposedTo.Age - num10, 0f)) * MathF.Min(0f, 20f - MathF.Max(HeroBeingProposedTo.Age - num10, 0f));
			return (int)(-50000f + num + num2 + num3 + num8 + (float)num7 + num9 + num6 + num11);
		}
		float num12 = 0f - HeroBeingProposedTo.Clan.Renown;
		float num13 = Campaign.Current.Models.AgeModel.HeroComesOfAge;
		float num14 = 0f - 2f * MathF.Min(0f, 20f - MathF.Max(HeroBeingProposedTo.Age - num13, 0f)) * MathF.Min(0f, 20f - MathF.Max(HeroBeingProposedTo.Age - num13, 0f)) * MathF.Min(0f, 20f - MathF.Max(HeroBeingProposedTo.Age - num13, 0f));
		return (int)(num12 + num14);
	}

	public override void CheckBarterLink(Barterable linkedBarterable)
	{
		if (linkedBarterable.GetType() == typeof(MarriageBarterable) && linkedBarterable.OriginalOwner == base.OriginalOwner && ((MarriageBarterable)linkedBarterable).HeroBeingProposedTo == HeroBeingProposedTo && ((MarriageBarterable)linkedBarterable).ProposingHero == ProposingHero)
		{
			AddBarterLink(linkedBarterable);
		}
	}

	public override bool IsCompatible(Barterable barterable)
	{
		if (barterable is MarriageBarterable marriageBarterable)
		{
			if (marriageBarterable.HeroBeingProposedTo != HeroBeingProposedTo && marriageBarterable.HeroBeingProposedTo != ProposingHero && marriageBarterable.ProposingHero != HeroBeingProposedTo)
			{
				return marriageBarterable.ProposingHero != ProposingHero;
			}
			return false;
		}
		return true;
	}

	public override ImageIdentifier GetVisualIdentifier()
	{
		return new CharacterImageIdentifier(CharacterCode.CreateFrom(HeroBeingProposedTo.CharacterObject));
	}

	public override string GetEncyclopediaLink()
	{
		return HeroBeingProposedTo.EncyclopediaLink;
	}

	public override void Apply()
	{
		MarriageAction.Apply(HeroBeingProposedTo, ProposingHero, HeroBeingProposedTo.Clan == Clan.PlayerClan || ProposingHero.Clan == Clan.PlayerClan);
	}

	internal static void AutoGeneratedStaticCollectObjectsMarriageBarterable(object o, List<object> collectedObjects)
	{
		((MarriageBarterable)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(ProposingHero);
		collectedObjects.Add(HeroBeingProposedTo);
	}

	internal static object AutoGeneratedGetMemberValueProposingHero(object o)
	{
		return ((MarriageBarterable)o).ProposingHero;
	}

	internal static object AutoGeneratedGetMemberValueHeroBeingProposedTo(object o)
	{
		return ((MarriageBarterable)o).HeroBeingProposedTo;
	}
}
