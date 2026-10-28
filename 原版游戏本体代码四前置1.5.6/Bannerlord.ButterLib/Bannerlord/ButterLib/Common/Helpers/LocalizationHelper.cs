using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib.Common.Helpers;

public static class LocalizationHelper
{
	public enum RecursiveCaller : byte
	{
		None,
		Hero,
		Settlement,
		Clan,
		Kingdom
	}

	public enum PluralForm : byte
	{
		Singular,
		SpecificSingular,
		SpecificPlural,
		Plural
	}

	public const string PLURAL_FORM_TAG = "PLURAL_FORM";

	public const string SPECIFIC_SINGULAR_FORM_TAG = "SPECIFIC_SINGULAR_FORM";

	public const string SPECIFIC_PLURAL_FORM_TAG = "SPECIFIC_PLURAL_FORM";

	public const string LIST_HAS_ITEMS_TAG = "ANY";

	public const string LIST_HAS_MULTIPLE_ITEMS_TAG = "IS_PLURAL";

	public const string LIST_START_TAG = "START";

	public const string LIST_END_TAG = "END";

	private const string BUILT_IN_AGGREGATION_STRING = "{=JLkq0ZkOSI}{START}{?IS_PLURAL} and {?}{\\?}{END}";

	private static readonly ReadOnlyCollection<int> EasternSlavicPluralExceptions = new ReadOnlyCollection<int>(new List<int> { 11, 12, 13, 14 });

	private static readonly ReadOnlyCollection<int> EasternSlavicSingularNumerics = new ReadOnlyCollection<int>(new List<int> { 1, 2, 3, 4 });

	private static readonly ReadOnlyCollection<int> WesternSlavicPluralExceptions = new ReadOnlyCollection<int>(new List<int> { 12, 13, 14 });

	private static readonly ReadOnlyCollection<int> WesternSlavicSingularNumerics = new ReadOnlyCollection<int>(new List<int> { 2, 3, 4 });

	private static readonly ReadOnlyCollection<string> SingleZeroLanguageIDs = new ReadOnlyCollection<string>(new List<string> { "French", "Français" });

	private static readonly ReadOnlyCollection<string> EasternSlavicGroupLanguageIDs = new ReadOnlyCollection<string>(new List<string> { "Russian", "Русский", "Ukrainian", "Українська", "Belarusian", "Беларускі" });

	private static readonly ReadOnlyCollection<string> WesternSlavicGroupLanguageIDs = new ReadOnlyCollection<string>(new List<string> { "Polish", "Polski" });

	private static RecursiveCaller GetRecursiveCaller(RecursiveCaller currentCaller, RecursiveCaller receivedCaller)
	{
		return (RecursiveCaller)Math.Max((byte)currentCaller, (byte)receivedCaller);
	}

	private static RecursiveCaller GetCurrentCaller<T>(T entity) where T : class
	{
		if (!(entity is Hero))
		{
			if (!(entity is Settlement))
			{
				if (!(entity is Clan))
				{
					if (entity is Kingdom)
					{
						return RecursiveCaller.Kingdom;
					}
					throw new ArgumentException(entity.GetType().FullName + " is not supported type", "entity");
				}
				return RecursiveCaller.Clan;
			}
			return RecursiveCaller.Settlement;
		}
		return RecursiveCaller.Hero;
	}

	private static TextObject GetEntityTextObject<T>(T entity) where T : class
	{
		if (!(entity is Hero hero))
		{
			if (!(entity is Settlement settlement))
			{
				if (!(entity is Clan clan))
				{
					if (entity is Kingdom kingdom)
					{
						TextObject textObject = new TextObject(string.Empty);
						textObject.SetTextVariable("NAME", kingdom.Name);
						textObject.SetTextVariable("LINK", kingdom.EncyclopediaLinkWithName);
						return textObject;
					}
					throw new ArgumentException(entity.GetType().FullName + " is not supported type", "entity");
				}
				TextObject textObject2 = new TextObject(string.Empty);
				textObject2.SetTextVariable("NAME", clan.Name);
				textObject2.SetTextVariable("MINOR_FACTION", clan.IsMinorFaction ? 1 : 0);
				textObject2.SetTextVariable("UNDER_CONTRACT", clan.IsUnderMercenaryService ? 1 : 0);
				textObject2.SetTextVariable("LINK", clan.EncyclopediaLinkWithName);
				return textObject2;
			}
			TextObject textObject3 = new TextObject(string.Empty);
			textObject3.SetTextVariable("NAME", settlement.Name);
			textObject3.SetTextVariable("IS_TOWN", settlement.IsTown ? 1 : 0);
			textObject3.SetTextVariable("IS_CASTLE", settlement.IsCastle ? 1 : 0);
			textObject3.SetTextVariable("IS_VILLAGE", settlement.IsVillage ? 1 : 0);
			textObject3.SetTextVariable("LINK", settlement.EncyclopediaLinkWithName);
			return textObject3;
		}
		TextObject textObject4 = new TextObject(string.Empty);
		textObject4.SetTextVariable("NAME", hero.Name);
		textObject4.SetTextVariable("AGE", (int)hero.Age);
		textObject4.SetTextVariable("GENDER", hero.IsFemale ? 1 : 0);
		textObject4.SetTextVariable("LINK", hero.EncyclopediaLinkWithName);
		textObject4.SetTextVariable("FIRSTNAME", hero.FirstName ?? hero.Name);
		return textObject4;
	}

	private static void SetRelatedProperties<T>(TextObject? parentTextObject, string tag, T entity, bool addLeaderInfo, RecursiveCaller recursiveCaller) where T : class
	{
		if (!(entity is Hero hero))
		{
			if (!(entity is Settlement settlement))
			{
				if (!(entity is Clan clan))
				{
					if (!(entity is Kingdom kingdom))
					{
						throw new ArgumentException(entity.GetType().FullName + " is not supported type", "entity");
					}
					if (addLeaderInfo)
					{
						SetEntityProperties(parentTextObject, tag + "_LEADER", kingdom.Leader, addLeaderInfo: false, RecursiveCaller.Kingdom);
					}
				}
				else
				{
					if (addLeaderInfo)
					{
						SetEntityProperties(parentTextObject, tag + "_LEADER", clan.Leader, addLeaderInfo: false, RecursiveCaller.Clan);
					}
					if (clan.Kingdom != null)
					{
						SetEntityProperties(parentTextObject, tag + "_KINGDOM", clan.Kingdom, addLeaderInfo, GetRecursiveCaller(RecursiveCaller.Clan, recursiveCaller));
					}
				}
			}
			else
			{
				SetEntityProperties(parentTextObject, tag + "_CLAN", settlement.OwnerClan, addLeaderInfo, GetRecursiveCaller(RecursiveCaller.Settlement, recursiveCaller));
			}
		}
		else
		{
			SetEntityProperties(parentTextObject, tag + "_CLAN", hero.Clan, addLeaderInfo, GetRecursiveCaller(RecursiveCaller.Hero, recursiveCaller));
		}
	}

	public static void SetEntityProperties<T>(TextObject? parentTextObject, string tag, T? entity, bool addLeaderInfo = false, RecursiveCaller recursiveCaller = RecursiveCaller.None) where T : class
	{
		if (!string.IsNullOrEmpty(tag) && entity != null && recursiveCaller != GetCurrentCaller(entity))
		{
			if ((object)parentTextObject == null)
			{
				MBTextManager.SetTextVariable(tag, GetEntityTextObject(entity));
			}
			else
			{
				parentTextObject.SetTextVariable(tag, GetEntityTextObject(entity));
			}
			SetRelatedProperties(parentTextObject, tag, entity, addLeaderInfo, recursiveCaller);
		}
	}

	private static PluralForm GetEasternSlavicPluralFormInternal(int number)
	{
		int num = Math.Abs(number);
		int num2 = num % 10;
		if (!EasternSlavicPluralExceptions.Contains(num % 100) && EasternSlavicSingularNumerics.Contains(num2))
		{
			if (EasternSlavicPluralExceptions.Contains(num) || !EasternSlavicSingularNumerics.Contains(num2) || num2 == 1)
			{
				return PluralForm.Singular;
			}
			return PluralForm.SpecificSingular;
		}
		return PluralForm.Plural;
	}

	private static PluralForm GetWesternSlavicPluralFormInternal(int number)
	{
		int num = Math.Abs(number);
		int value = num % 10;
		if (num <= 1 || (!WesternSlavicPluralExceptions.Contains(num % 100) && WesternSlavicSingularNumerics.Contains(value)))
		{
			if (WesternSlavicPluralExceptions.Contains(num) || !WesternSlavicSingularNumerics.Contains(value))
			{
				return PluralForm.Singular;
			}
			return PluralForm.SpecificPlural;
		}
		return PluralForm.Plural;
	}

	public static PluralForm GetPluralForm(int number)
	{
		if (EasternSlavicGroupLanguageIDs.Contains(BannerlordConfig.Language))
		{
			return GetEasternSlavicPluralFormInternal(number);
		}
		if (WesternSlavicGroupLanguageIDs.Contains(BannerlordConfig.Language))
		{
			return GetWesternSlavicPluralFormInternal(number);
		}
		if (SingleZeroLanguageIDs.Contains(BannerlordConfig.Language) && number == 0)
		{
			return PluralForm.Singular;
		}
		if (number == 1)
		{
			return PluralForm.Singular;
		}
		return PluralForm.Plural;
	}

	public static PluralForm GetPluralForm(float number)
	{
		if (EasternSlavicGroupLanguageIDs.Contains(BannerlordConfig.Language))
		{
			return GetEasternSlavicPluralFormInternal((int)Math.Floor(number));
		}
		if (!(Math.Abs(number) > 1f))
		{
			return PluralForm.Singular;
		}
		return PluralForm.Plural;
	}

	private static Dictionary<string, object> GetPluralFormAttributes(PluralForm pluralForm)
	{
		return new Dictionary<string, object>
		{
			["PLURAL_FORM"] = new TextObject((pluralForm == PluralForm.Plural) ? 1 : 0),
			["SPECIFIC_PLURAL_FORM"] = new TextObject((pluralForm == PluralForm.SpecificPlural) ? 1 : 0),
			["SPECIFIC_SINGULAR_FORM"] = new TextObject((pluralForm == PluralForm.SpecificSingular) ? 1 : 0)
		};
	}

	public static void SetNumericVariable(TextObject? textObject, string tag, int variableValue, string? format = null)
	{
		if (!string.IsNullOrEmpty(tag))
		{
			Dictionary<string, object> pluralFormAttributes = GetPluralFormAttributes(GetPluralForm(variableValue));
			TextObject textObject2 = (string.IsNullOrEmpty(format) ? new TextObject(variableValue.ToString(), pluralFormAttributes) : new TextObject(variableValue.ToString(format), pluralFormAttributes));
			if ((object)textObject == null)
			{
				MBTextManager.SetTextVariable(tag, textObject2);
			}
			else
			{
				textObject.SetTextVariable(tag, textObject2);
			}
		}
	}

	public static void SetNumericVariable(TextObject? textObject, string tag, float variableValue, string? format = null)
	{
		if (!string.IsNullOrEmpty(tag))
		{
			Dictionary<string, object> pluralFormAttributes = GetPluralFormAttributes(GetPluralForm(variableValue));
			TextObject textObject2 = (string.IsNullOrEmpty(format) ? new TextObject(variableValue.ToString("R"), pluralFormAttributes) : new TextObject(variableValue.ToString(format), pluralFormAttributes));
			if ((object)textObject == null)
			{
				MBTextManager.SetTextVariable(tag, textObject2);
			}
			else
			{
				textObject.SetTextVariable(tag, textObject2);
			}
		}
	}

	private static Dictionary<string, object?> GetListAttributes(IEnumerable<string> valuesList, string separator = ", ", bool useDistinctValues = true)
	{
		IEnumerable<string> source;
		if (!useDistinctValues)
		{
			source = valuesList;
		}
		else
		{
			source = valuesList.Distinct();
		}
		List<string> list = source.ToList();
		if (!list.Any())
		{
			return new Dictionary<string, object>
			{
				["ANY"] = new TextObject(0),
				["IS_PLURAL"] = new TextObject(0),
				["START"] = new TextObject(string.Empty),
				["END"] = new TextObject(string.Empty)
			};
		}
		return new Dictionary<string, object>
		{
			["ANY"] = new TextObject(1),
			["IS_PLURAL"] = new TextObject((list.Count != 1) ? 1 : 0),
			["START"] = new TextObject(string.Join(separator, list.Take(list.Count - 1))),
			["END"] = new TextObject(list.Last())
		};
	}

	public static void SetListVariable(TextObject? textObject, string tag, List<string> valuesList, string separator = ", ", bool useDistinctValues = true)
	{
		if (!string.IsNullOrEmpty(tag))
		{
			Dictionary<string, object> listAttributes = GetListAttributes(valuesList, separator, useDistinctValues);
			TextObject textObject2 = new TextObject("{=JLkq0ZkOSI}{START}{?IS_PLURAL} and {?}{\\?}{END}", listAttributes);
			if ((object)textObject == null)
			{
				MBTextManager.SetTextVariable(tag, textObject2);
			}
			else
			{
				textObject.SetTextVariable(tag, textObject2);
			}
		}
	}
}
