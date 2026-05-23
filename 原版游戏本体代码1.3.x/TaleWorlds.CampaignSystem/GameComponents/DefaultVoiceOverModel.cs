using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000164 RID: 356
	public class DefaultVoiceOverModel : VoiceOverModel
	{
		// Token: 0x06001AD3 RID: 6867 RVA: 0x00089E60 File Offset: 0x00088060
		public override string GetSoundPathForCharacter(CharacterObject character, VoiceObject voiceObject)
		{
			if (voiceObject == null)
			{
				return "";
			}
			string text = "";
			string value = character.StringId + "_" + (CharacterObject.PlayerCharacter.IsFemale ? "female" : "male");
			foreach (string text2 in voiceObject.VoicePaths)
			{
				if (text2.Contains(value))
				{
					text = text2;
					break;
				}
				if (text2.Contains(character.StringId + "_"))
				{
					text = text2;
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				string accentClass = Campaign.Current.Models.VoiceOverModel.GetAccentClass(character.Culture, ConversationTagHelper.UsesHighRegister(character));
				Debug.Print("accentClass: " + accentClass, 0, Debug.DebugColor.White, 17592186044416UL);
				string text3 = (character.IsFemale ? "female" : "male");
				string stringId = character.GetPersona().StringId;
				List<string> list = new List<string>();
				List<string> list2 = new List<string>();
				list2.Add(string.Concat(new string[] { ".+\\\\", accentClass, "_", text3, "_", stringId, "_.+" }));
				list2.Add(string.Concat(new string[] { ".+\\\\", accentClass, "_", text3, "_generic_.+" }));
				this.CheckPossibleMatches(voiceObject, list2, ref list, false, false);
				if (list.IsEmpty<string>())
				{
					list2.Clear();
					list2.Add(string.Concat(new string[] { ".+\\\\", accentClass, "_", stringId, "_.+" }));
					list2.Add(".+\\\\" + accentClass + "_generic_.+");
					list2.Add(string.Concat(new string[] { ".+\\\\", text3, "_", stringId, "_.+" }));
					list2.Add(".+\\\\" + text3 + "_generic_.+");
					this.CheckPossibleMatches(voiceObject, list2, ref list, false, false);
					if (list.IsEmpty<string>())
					{
						list2.Clear();
						list2.Add(".+\\\\" + stringId + "_.+");
						list2.Add(".+\\\\generic_.+");
						list2.Add(".+" + accentClass + "_.+");
						this.CheckPossibleMatches(voiceObject, list2, ref list, true, character.IsFemale);
					}
				}
				if (!list.IsEmpty<string>())
				{
					if (character.IsHero)
					{
						text = list[character.HeroObject.RandomInt(list.Count)];
					}
					else if (MobileParty.ConversationParty != null)
					{
						text = list[MobileParty.ConversationParty.RandomInt(list.Count)];
					}
					else
					{
						text = list.GetRandomElement<string>();
					}
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			Debug.Print("[VOICEOVER]Sound path found: " + BasePath.Name + text, 0, Debug.DebugColor.White, 17592186044416UL);
			text = text.Replace("$PLATFORM", "PC");
			return text + ".ogg";
		}

		// Token: 0x06001AD4 RID: 6868 RVA: 0x0008A1C0 File Offset: 0x000883C0
		private void CheckPossibleMatches(VoiceObject voiceObject, List<string> possibleMatches, ref List<string> possibleVoicePaths, bool doubleCheckForGender = false, bool isFemale = false)
		{
			foreach (string pattern in possibleMatches)
			{
				Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
				foreach (string text in voiceObject.VoicePaths)
				{
					if (regex.Match(text).Success && !possibleVoicePaths.Contains(text))
					{
						if (doubleCheckForGender)
						{
							if (text.Contains("_male") || text.Contains("_female"))
							{
								string value = (isFemale ? "_female" : "_male");
								if (text.Contains(value))
								{
									possibleVoicePaths.Add(text);
								}
							}
						}
						else
						{
							possibleVoicePaths.Add(text);
						}
					}
				}
			}
		}

		// Token: 0x06001AD5 RID: 6869 RVA: 0x0008A2B4 File Offset: 0x000884B4
		public override string GetAccentClass(CultureObject culture, bool isHighClass)
		{
			if (culture.StringId == "empire")
			{
				if (isHighClass)
				{
					return "imperial_high";
				}
				return "imperial_low";
			}
			else
			{
				if (culture.StringId == "vlandia")
				{
					return "vlandian";
				}
				if (culture.StringId == "sturgia")
				{
					return "sturgian";
				}
				if (culture.StringId == "khuzait")
				{
					return "khuzait";
				}
				if (culture.StringId == "aserai")
				{
					return "aserai";
				}
				if (culture.StringId == "battania")
				{
					return "battanian";
				}
				if (culture.StringId == "forest_bandits")
				{
					return "forest_bandits";
				}
				if (culture.StringId == "sea_raiders")
				{
					return "sea_raiders";
				}
				if (culture.StringId == "mountain_bandits")
				{
					return "mountain_bandits";
				}
				if (culture.StringId == "desert_bandits")
				{
					return "desert_bandits";
				}
				if (culture.StringId == "steppe_bandits")
				{
					return "steppe_bandits";
				}
				if (culture.StringId == "looters")
				{
					return "looters";
				}
				return "";
			}
		}

		// Token: 0x040008F4 RID: 2292
		private const string ImperialHighClass = "imperial_high";

		// Token: 0x040008F5 RID: 2293
		private const string ImperialLowClass = "imperial_low";

		// Token: 0x040008F6 RID: 2294
		private const string VlandianClass = "vlandian";

		// Token: 0x040008F7 RID: 2295
		private const string SturgianClass = "sturgian";

		// Token: 0x040008F8 RID: 2296
		private const string KhuzaitClass = "khuzait";

		// Token: 0x040008F9 RID: 2297
		private const string AseraiClass = "aserai";

		// Token: 0x040008FA RID: 2298
		private const string BattanianClass = "battanian";

		// Token: 0x040008FB RID: 2299
		private const string ForestBanditClass = "forest_bandits";

		// Token: 0x040008FC RID: 2300
		private const string SeaBanditClass = "sea_raiders";

		// Token: 0x040008FD RID: 2301
		private const string MountainBanditClass = "mountain_bandits";

		// Token: 0x040008FE RID: 2302
		private const string DesertBanditClass = "desert_bandits";

		// Token: 0x040008FF RID: 2303
		private const string SteppeBanditClass = "steppe_bandits";

		// Token: 0x04000900 RID: 2304
		private const string LootersClass = "looters";

		// Token: 0x04000901 RID: 2305
		private const string Male = "male";

		// Token: 0x04000902 RID: 2306
		private const string Female = "female";

		// Token: 0x04000903 RID: 2307
		private const string GenericPersonaId = "generic";
	}
}
