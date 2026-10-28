using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Conversation;

public static class ConversationHelper
{
	public static CharacterObject AskedLord;

	public static bool ConversationTroopCommentShown;

	public static string HeroRefersToHero(Hero talkTroop, Hero referringTo, bool uppercaseFirst)
	{
		if (talkTroop == null)
		{
			return referringTo.Name.ToString();
		}
		StringHelpers.SetCharacterProperties("REFERING_HERO", referringTo.CharacterObject);
		string text = "";
		if (talkTroop.Father == referringTo)
		{
			text = "str_relation_my_father";
		}
		else if (talkTroop.Mother == referringTo)
		{
			text = "str_relation_my_mother";
		}
		else if ((referringTo.Mother == talkTroop || referringTo.Father == talkTroop) && !referringTo.IsFemale)
		{
			text = "str_relation_my_son";
		}
		else if ((referringTo.Mother == talkTroop || referringTo.Father == talkTroop) && referringTo.IsFemale)
		{
			text = "str_relation_my_daughter";
		}
		else if (talkTroop.Father != null && talkTroop.Father == referringTo.Father && !referringTo.IsFemale)
		{
			text = "str_relation_my_brother";
		}
		else if (talkTroop.Father != null && talkTroop.Father == referringTo.Father && referringTo.IsFemale)
		{
			text = "str_relation_my_sister";
		}
		else if (talkTroop.Spouse == referringTo && referringTo.IsFemale)
		{
			text = "str_relation_my_wife";
		}
		else if (talkTroop.Spouse == referringTo && !referringTo.IsFemale)
		{
			text = "str_relation_my_husband";
		}
		else if (talkTroop.IsPlayerCompanion && Hero.MainHero.IsLord && Hero.MainHero.IsFemale)
		{
			text = "str_player_salutation_my_lady";
		}
		else if (talkTroop.IsPlayerCompanion && Hero.MainHero.IsLord)
		{
			text = "str_player_salutation_my_lord";
		}
		else if (talkTroop.IsPlayerCompanion)
		{
			text = "str_player_salutation_captain";
		}
		else if (talkTroop.Clan != null && !talkTroop.Clan.IsMapFaction && referringTo.Clan != null && !referringTo.Clan.IsMapFaction && talkTroop.Clan == referringTo.Clan && !referringTo.IsFemale)
		{
			text = "str_relation_my_kinsman";
		}
		else if (talkTroop.Clan != null && !talkTroop.Clan.IsMapFaction && referringTo.Clan != null && !referringTo.Clan.IsMapFaction && talkTroop.Clan == referringTo.Clan)
		{
			text = "str_relation_my_kinswoman";
		}
		else if (Hero.OneToOneConversationHero != null && talkTroop.MapFaction == Hero.OneToOneConversationHero.MapFaction && talkTroop.MapFaction.Leader == referringTo)
		{
			text = "str_relation_our_liege";
		}
		else if (referringTo.IsLord && referringTo.IsFemale)
		{
			text = "str_relation_the_lady";
		}
		else if (talkTroop.MapFaction != null && talkTroop.MapFaction == referringTo.MapFaction && talkTroop.IsLord && referringTo.IsLord && talkTroop.IsCommander && referringTo.IsCommander)
		{
			text = "str_relation_my_ally";
		}
		else if (talkTroop.Clan == null || referringTo.Clan == null)
		{
			text = "str_relation_my_friend";
		}
		if (text == "")
		{
			return referringTo.Name.ToString();
		}
		string text2 = GameTexts.FindText(text).ToString();
		if (uppercaseFirst)
		{
			char[] array = text2.ToCharArray();
			text2 = array[0].ToString().ToUpper();
			for (int i = 1; i < array.Length; i++)
			{
				text2 += array[i];
			}
		}
		return text2;
	}

	public static string GetHeroRelationToHeroTextShort(Hero queriedHero, Hero baseHero, bool uppercaseFirst)
	{
		TextObject textObject = null;
		if (baseHero.Father == queriedHero)
		{
			textObject = GameTexts.FindText("str_father");
		}
		else if (baseHero.Mother == queriedHero)
		{
			textObject = GameTexts.FindText("str_mother");
		}
		else if (baseHero.Siblings.Contains(queriedHero))
		{
			textObject = ((queriedHero.Age == baseHero.Age) ? ((!queriedHero.IsFemale) ? GameTexts.FindText("str_twin_male") : GameTexts.FindText("str_twin_female")) : ((!queriedHero.IsFemale) ? ((queriedHero.Age > baseHero.Age) ? GameTexts.FindText("str_bigbrother") : GameTexts.FindText("str_littlebrother")) : ((queriedHero.Age > baseHero.Age) ? GameTexts.FindText("str_bigsister") : GameTexts.FindText("str_littlesister"))));
		}
		else if (baseHero.Spouse == queriedHero)
		{
			textObject = GameTexts.FindText("str_spouse");
		}
		else if (baseHero.Children.Contains(queriedHero))
		{
			textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_daughter") : GameTexts.FindText("str_son"));
		}
		else if (baseHero.Spouse?.Father == queriedHero)
		{
			textObject = (baseHero.IsFemale ? GameTexts.FindText("str_husband_fatherinlaw") : GameTexts.FindText("str_wife_fatherinlaw"));
		}
		else if (baseHero.Spouse?.Mother == queriedHero)
		{
			textObject = (baseHero.IsFemale ? GameTexts.FindText("str_husband_motherinlaw") : GameTexts.FindText("str_wife_motherinlaw"));
		}
		else if (queriedHero.ExSpouses.Contains(baseHero))
		{
			textObject = GameTexts.FindText("str_exspouse");
		}
		else if (queriedHero.CompanionOf == baseHero.Clan)
		{
			textObject = GameTexts.FindText("str_companion");
		}
		else if (baseHero.ExSpouses.Any((Hero exSpouse) => exSpouse.Mother == queriedHero))
		{
			textObject = (baseHero.IsFemale ? GameTexts.FindText("str_ex_husband_motherinlaw") : GameTexts.FindText("str_ex_wife_motherinlaw"));
		}
		else if (baseHero.ExSpouses.Any((Hero exSpouse) => exSpouse.Father == queriedHero))
		{
			textObject = (baseHero.IsFemale ? GameTexts.FindText("str_ex_husband_fatherinlaw") : GameTexts.FindText("str_ex_wife_fatherinlaw"));
		}
		else if (baseHero.ExSpouses.Any((Hero exSpouse) => exSpouse.Siblings.Contains(queriedHero)))
		{
			textObject = (baseHero.IsFemale ? GameTexts.FindText(queriedHero.IsFemale ? "str_ex_husband_sisterinlaw" : "str_ex_husband_brotherinlaw") : GameTexts.FindText(queriedHero.IsFemale ? "str_ex_wife_sisterinlaw" : "str_ex_wife_brotherinlaw"));
		}
		else if (baseHero.Spouse != null && baseHero.Spouse.Siblings.Contains(queriedHero))
		{
			textObject = (baseHero.IsFemale ? GameTexts.FindText(queriedHero.IsFemale ? "str_husband_sisterinlaw" : "str_husband_brotherinlaw") : GameTexts.FindText(queriedHero.IsFemale ? "str_wife_sisterinlaw" : "str_wife_brotherinlaw"));
		}
		else if (baseHero.Children.Any((Hero children) => children.Spouse == queriedHero))
		{
			textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_child_spouse_female") : GameTexts.FindText("str_child_spouse_male"));
		}
		else if (baseHero.Siblings.Any((Hero sibling) => sibling.Spouse == queriedHero))
		{
			textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_sibling_spouse_female") : GameTexts.FindText("str_sibling_spouse_male"));
		}
		else if (baseHero.Father?.Siblings.FirstOrDefault((Hero x) => x == queriedHero) != null || baseHero.Mother?.Siblings.FirstOrDefault((Hero x) => x == queriedHero) != null)
		{
			textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_aunt") : GameTexts.FindText("str_uncle"));
		}
		else if (baseHero.Siblings.Any((Hero x) => x.Children.Contains(queriedHero)))
		{
			textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_niece") : GameTexts.FindText("str_nephew"));
		}
		else if (baseHero.Children.Any((Hero x) => x.Children.Contains(queriedHero)))
		{
			textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_granddaughter") : GameTexts.FindText("str_grandson"));
		}
		else
		{
			foreach (Hero sibling in baseHero.Siblings)
			{
				if (sibling.Children.Contains(queriedHero))
				{
					textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_niece") : GameTexts.FindText("str_nephew"));
					break;
				}
				if (sibling.Spouse == queriedHero)
				{
					textObject = (sibling.IsFemale ? GameTexts.FindText(queriedHero.IsFemale ? "str_husband_sisterinlaw" : "str_husband_brotherinlaw") : GameTexts.FindText(queriedHero.IsFemale ? "str_wife_sisterinlaw" : "str_wife_brotherinlaw"));
					break;
				}
				if (sibling.ExSpouses.Contains(queriedHero))
				{
					textObject = (sibling.IsFemale ? GameTexts.FindText(queriedHero.IsFemale ? "str_ex_husband_sisterinlaw" : "str_ex_husband_brotherinlaw") : GameTexts.FindText(queriedHero.IsFemale ? "str_ex_wife_sisterinlaw" : "str_ex_wife_brotherinlaw"));
					break;
				}
			}
		}
		if (textObject == null)
		{
			Debug.FailedAssert("GENERIC - UNSPECIFIED RELATION in clan", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Conversation\\ConversationHelper.cs", "GetHeroRelationToHeroTextShort", 268);
			textObject = GameTexts.FindText("str_relative_of_player");
		}
		else if (queriedHero != null)
		{
			textObject.SetCharacterProperties("NPC", queriedHero.CharacterObject);
		}
		string text = textObject.ToString();
		if (!char.IsLower(text[0]) != uppercaseFirst)
		{
			char[] array = text.ToCharArray();
			text = (uppercaseFirst ? array[0].ToString().ToUpper() : array[0].ToString().ToLower());
			for (int num = 1; num < array.Length; num++)
			{
				text += array[num];
			}
		}
		return text;
	}

	public static CharacterObject GetConversationCharacterPartyLeader(PartyBase party)
	{
		if (party == null)
		{
			return null;
		}
		if (party.LeaderHero != null)
		{
			return party.LeaderHero.CharacterObject;
		}
		CharacterObject characterObject = null;
		if (party.MemberRoster.TotalManCount > 0)
		{
			foreach (TroopRosterElement item in party.MemberRoster.GetTroopRoster())
			{
				if (characterObject == null || item.Character.Tier > characterObject.Tier)
				{
					characterObject = item.Character;
					if (characterObject.Tier == Campaign.Current.Models.CharacterStatsModel.MaxCharacterTier)
					{
						break;
					}
				}
			}
		}
		return characterObject;
	}
}
