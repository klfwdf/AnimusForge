using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Conversation
{
	// Token: 0x02000231 RID: 561
	public static class ConversationHelper
	{
		// Token: 0x060021EB RID: 8683 RVA: 0x00095070 File Offset: 0x00093270
		public static string HeroRefersToHero(Hero talkTroop, Hero referringTo, bool uppercaseFirst)
		{
			if (talkTroop == null)
			{
				return referringTo.Name.ToString();
			}
			StringHelpers.SetCharacterProperties("REFERING_HERO", referringTo.CharacterObject, null, false);
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
			string text2 = GameTexts.FindText(text, null).ToString();
			if (uppercaseFirst)
			{
				char[] array = text2.ToCharArray();
				text2 = array[0].ToString().ToUpper();
				for (int i = 1; i < array.Length; i++)
				{
					text2 += array[i].ToString();
				}
			}
			return text2;
		}

		// Token: 0x060021EC RID: 8684 RVA: 0x0009538C File Offset: 0x0009358C
		public static string GetHeroRelationToHeroTextShort(Hero queriedHero, Hero baseHero, bool uppercaseFirst)
		{
			TextObject textObject = null;
			if (baseHero.Father == queriedHero)
			{
				textObject = GameTexts.FindText("str_father", null);
			}
			else if (baseHero.Mother == queriedHero)
			{
				textObject = GameTexts.FindText("str_mother", null);
			}
			else if (baseHero.Siblings.Contains(queriedHero))
			{
				if (queriedHero.Age == baseHero.Age)
				{
					if (queriedHero.IsFemale)
					{
						textObject = GameTexts.FindText("str_twin_female", null);
					}
					else
					{
						textObject = GameTexts.FindText("str_twin_male", null);
					}
				}
				else if (queriedHero.IsFemale)
				{
					textObject = ((queriedHero.Age > baseHero.Age) ? GameTexts.FindText("str_bigsister", null) : GameTexts.FindText("str_littlesister", null));
				}
				else
				{
					textObject = ((queriedHero.Age > baseHero.Age) ? GameTexts.FindText("str_bigbrother", null) : GameTexts.FindText("str_littlebrother", null));
				}
			}
			else if (baseHero.Spouse == queriedHero)
			{
				textObject = GameTexts.FindText("str_spouse", null);
			}
			else if (baseHero.Children.Contains(queriedHero))
			{
				if (queriedHero.Age > (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
				{
					textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_daughter", null) : GameTexts.FindText("str_son", null));
				}
				else
				{
					textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_female_child", null) : GameTexts.FindText("str_male_child", null));
				}
			}
			else
			{
				Hero spouse = baseHero.Spouse;
				if (((spouse != null) ? spouse.Father : null) == queriedHero)
				{
					textObject = (baseHero.IsFemale ? GameTexts.FindText("str_husband_fatherinlaw", null) : GameTexts.FindText("str_wife_fatherinlaw", null));
				}
				else
				{
					Hero spouse2 = baseHero.Spouse;
					if (((spouse2 != null) ? spouse2.Mother : null) == queriedHero)
					{
						textObject = (baseHero.IsFemale ? GameTexts.FindText("str_husband_motherinlaw", null) : GameTexts.FindText("str_wife_motherinlaw", null));
					}
					else if (queriedHero.ExSpouses.Contains(baseHero))
					{
						textObject = GameTexts.FindText("str_exspouse", null);
					}
					else if (queriedHero.CompanionOf == baseHero.Clan)
					{
						textObject = GameTexts.FindText("str_companion", null);
					}
					else if (baseHero.ExSpouses.Any((Hero exSpouse) => exSpouse.Mother == queriedHero))
					{
						textObject = (baseHero.IsFemale ? GameTexts.FindText("str_ex_husband_motherinlaw", null) : GameTexts.FindText("str_ex_wife_motherinlaw", null));
					}
					else if (baseHero.ExSpouses.Any((Hero exSpouse) => exSpouse.Father == queriedHero))
					{
						textObject = (baseHero.IsFemale ? GameTexts.FindText("str_ex_husband_fatherinlaw", null) : GameTexts.FindText("str_ex_wife_fatherinlaw", null));
					}
					else if (baseHero.ExSpouses.Any((Hero exSpouse) => exSpouse.Siblings.Contains(queriedHero)))
					{
						textObject = (baseHero.IsFemale ? GameTexts.FindText(queriedHero.IsFemale ? "str_ex_husband_sisterinlaw" : "str_ex_husband_brotherinlaw", null) : GameTexts.FindText(queriedHero.IsFemale ? "str_ex_wife_sisterinlaw" : "str_ex_wife_brotherinlaw", null));
					}
					else if (baseHero.Spouse != null && baseHero.Spouse.Siblings.Contains(queriedHero))
					{
						textObject = (baseHero.IsFemale ? GameTexts.FindText(queriedHero.IsFemale ? "str_husband_sisterinlaw" : "str_husband_brotherinlaw", null) : GameTexts.FindText(queriedHero.IsFemale ? "str_wife_sisterinlaw" : "str_wife_brotherinlaw", null));
					}
					else if (baseHero.Children.Any((Hero children) => children.Spouse == queriedHero))
					{
						textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_child_spouse_female", null) : GameTexts.FindText("str_child_spouse_male", null));
					}
					else if (baseHero.Siblings.Any((Hero sibling) => sibling.Spouse == queriedHero))
					{
						textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_sibling_spouse_female", null) : GameTexts.FindText("str_sibling_spouse_male", null));
					}
					else
					{
						Hero father = baseHero.Father;
						if (((father != null) ? father.Siblings.FirstOrDefault((Hero x) => x == queriedHero) : null) == null)
						{
							Hero mother = baseHero.Mother;
							if (((mother != null) ? mother.Siblings.FirstOrDefault((Hero x) => x == queriedHero) : null) == null)
							{
								if (baseHero.Siblings.Any((Hero x) => x.Children.Contains(queriedHero)))
								{
									textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_niece", null) : GameTexts.FindText("str_nephew", null));
									goto IL_688;
								}
								if (baseHero.Children.Any((Hero x) => x.Children.Contains(queriedHero)))
								{
									textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_granddaughter", null) : GameTexts.FindText("str_grandson", null));
									goto IL_688;
								}
								foreach (Hero hero in baseHero.Siblings)
								{
									if (hero.Children.Contains(queriedHero))
									{
										textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_niece", null) : GameTexts.FindText("str_nephew", null));
										break;
									}
									if (hero.Spouse == queriedHero)
									{
										textObject = (hero.IsFemale ? GameTexts.FindText(queriedHero.IsFemale ? "str_husband_sisterinlaw" : "str_husband_brotherinlaw", null) : GameTexts.FindText(queriedHero.IsFemale ? "str_wife_sisterinlaw" : "str_wife_brotherinlaw", null));
										break;
									}
									if (hero.ExSpouses.Contains(queriedHero))
									{
										textObject = (hero.IsFemale ? GameTexts.FindText(queriedHero.IsFemale ? "str_ex_husband_sisterinlaw" : "str_ex_husband_brotherinlaw", null) : GameTexts.FindText(queriedHero.IsFemale ? "str_ex_wife_sisterinlaw" : "str_ex_wife_brotherinlaw", null));
										break;
									}
								}
								goto IL_688;
							}
						}
						textObject = (queriedHero.IsFemale ? GameTexts.FindText("str_aunt", null) : GameTexts.FindText("str_uncle", null));
					}
				}
			}
			IL_688:
			if (textObject == null)
			{
				Debug.FailedAssert("GENERIC - UNSPECIFIED RELATION in clan", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Conversation\\ConversationHelper.cs", "GetHeroRelationToHeroTextShort", 275);
				textObject = GameTexts.FindText("str_relative_of_player", null);
			}
			else if (queriedHero != null)
			{
				textObject.SetCharacterProperties("NPC", queriedHero.CharacterObject, false);
			}
			string text = textObject.ToString();
			if (!char.IsLower(text[0]) != uppercaseFirst)
			{
				char[] array = text.ToCharArray();
				text = (uppercaseFirst ? array[0].ToString().ToUpper() : array[0].ToString().ToLower());
				for (int i = 1; i < array.Length; i++)
				{
					text += array[i].ToString();
				}
			}
			return text;
		}

		// Token: 0x060021ED RID: 8685 RVA: 0x00095B00 File Offset: 0x00093D00
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
				foreach (TroopRosterElement troopRosterElement in party.MemberRoster.GetTroopRoster())
				{
					if (characterObject == null || troopRosterElement.Character.Tier > characterObject.Tier)
					{
						characterObject = troopRosterElement.Character;
						if (characterObject.Tier == Campaign.Current.Models.CharacterStatsModel.MaxCharacterTier)
						{
							break;
						}
					}
				}
			}
			return characterObject;
		}

		// Token: 0x040009E4 RID: 2532
		public static CharacterObject AskedLord;

		// Token: 0x040009E5 RID: 2533
		public static bool ConversationTroopCommentShown;
	}
}
