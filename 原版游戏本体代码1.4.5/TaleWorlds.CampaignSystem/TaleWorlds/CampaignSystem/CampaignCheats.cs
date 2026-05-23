using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem;

public static class CampaignCheats
{
	[Flags]
	private enum CheatTextControl
	{
		None = 0,
		IgnoreCase = 1,
		ContainId = 2,
		RemoveEmptySpace = 4,
		All = 7
	}

	public const string Help = "help";

	public const string EnterNumber = "Please enter a number";

	public const string EnterPositiveNumber = "Please enter a positive number";

	public const string CampaignNotStarted = "Campaign was not started.";

	public const string CheatModeDisabled = "Cheat mode is disabled!";

	private const string AmbiguityFoundErrorMessage = "There is ambiguity with requested id, check parameters";

	private const string NotFoundErrorMessage = "Requested object could not found, check parameters";

	private const string EmptyIdRequestedMessage = "Requested Id can't be empty";

	public const int MaxSkillValue = 300;

	public const string OK = "Success";

	public const string CheatNameSeparator = "|";

	public static string ErrorType = "";

	private const float _maxAmountPlayerCanGive = 10000f;

	private const string _maxAmountWarning = "The value is too much";

	public static Settlement GetDefaultSettlement
	{
		get
		{
			if (Hero.MainHero.HomeSettlement == null)
			{
				return Town.AllTowns.GetRandomElement().Settlement;
			}
			return Hero.MainHero.HomeSettlement;
		}
	}

	public static bool CheckCheatUsage(ref string ErrorType)
	{
		if (Campaign.Current == null)
		{
			ErrorType = "Campaign was not started.";
			return false;
		}
		if (!Game.Current.CheatMode)
		{
			ErrorType = "Cheat mode is disabled!";
			return false;
		}
		ErrorType = "";
		return true;
	}

	public static bool CheckParameters(List<string> strings, int ParameterCount)
	{
		if (strings.Count == 0)
		{
			return ParameterCount == 0;
		}
		return strings.Count == ParameterCount;
	}

	public static bool CheckHelp(List<string> strings)
	{
		if (strings.Count == 0)
		{
			return false;
		}
		return strings[0].ToLower() == "help";
	}

	private static bool IsValueAcceptable(float value)
	{
		return value <= 10000f;
	}

	public static List<string> GetSeparatedNames(List<string> strings, bool removeEmptySpaces = false)
	{
		string text = "|";
		List<string> list = new List<string>();
		List<int> list2 = new List<int>(strings.Count);
		for (int i = 0; i < strings.Count; i++)
		{
			if (strings[i] == text)
			{
				list2.Add(i);
			}
		}
		list2.Add(strings.Count);
		int num = 0;
		for (int j = 0; j < list2.Count; j++)
		{
			int num2 = list2[j];
			string text2 = ConcatenateString(strings.GetRange(num, num2 - num));
			num = num2 + 1;
			list.Add(removeEmptySpaces ? text2.Replace(" ", "") : text2);
		}
		return list;
	}

	public static string ConcatenateString(List<string> strings)
	{
		if (strings == null || strings.IsEmpty())
		{
			return string.Empty;
		}
		string text = strings[0];
		if (strings.Count > 1)
		{
			for (int i = 1; i < strings.Count; i++)
			{
				text = text + " " + strings[i];
			}
		}
		return text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("export_hero", "campaign")]
	private static string ExportHero(List<string> strings)
	{
		string ErrorType = string.Empty;
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.export_hero [filenamewithoutextension] | [nameofhero]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckParameters(strings, 2))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		string text2 = separatedNames[0];
		string requestedId = separatedNames[1];
		PlatformFilePath saveFilePath = FileDriver.GetSaveFilePath(text2 + ".char");
		if (FileHelper.FileExists(saveFilePath))
		{
			return "File " + text2 + " already exists";
		}
		if (TryGetObject(requestedId, out Hero obj, out string errorMessage, (Func<Hero, bool>)null))
		{
			CharacterData.ExportCharacter(obj, saveFilePath.FileFullPath);
			return "Success";
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("import_main_hero", "campaign")]
	public static string ImportMainHero(List<string> strings)
	{
		string ErrorType = string.Empty;
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string result = "Format is \"campaign.import_main_hero [filenamewithoutextension]\".";
		if (CheckParameters(strings, 0))
		{
			return result;
		}
		try
		{
			string text = ConcatenateString(strings);
			PlatformFilePath saveFilePath = FileDriver.GetSaveFilePath(text + ".char");
			if (!FileHelper.FileExists(saveFilePath))
			{
				return "File " + text + " doesn't exists";
			}
			if (FileHelper.GetFileContent(saveFilePath) == null)
			{
				return "Can't read file: " + text + ". It's possible that it's corrupted.";
			}
			CharacterData.ImportCharacter(Hero.MainHero, saveFilePath.FileFullPath);
			return "Main hero was imported successfully.";
		}
		catch
		{
			return "An error occurred";
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("export_main_hero", "campaign")]
	public static string ExportMainHero(List<string> strings)
	{
		string ErrorType = string.Empty;
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string result = "Format is \"campaign.export_main_hero [filenamewithoutextension]\".";
		if (CheckParameters(strings, 0))
		{
			return result;
		}
		try
		{
			PlatformFilePath saveFilePath = FileDriver.GetSaveFilePath(ConcatenateString(strings) + ".char");
			if (FileHelper.FileExists(saveFilePath))
			{
				return "File already exists";
			}
			CharacterData.ExportCharacter(Hero.MainHero, saveFilePath.FileFullPath);
			return "Main hero is exported to " + saveFilePath.FileFullPath;
		}
		catch
		{
			return "An error occurred";
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_hero_crafting_stamina", "campaign")]
	public static string SetCraftingStamina(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_hero_crafting_stamina [HeroName] | [Stamina]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings, removeEmptySpaces: true);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		ICraftingCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
		if (campaignBehavior == null)
		{
			return "Can not found ICrafting Campaign Behavior!\n" + text;
		}
		int result = 0;
		if (!int.TryParse(separatedNames[1], out result) || result < 0 || result > 100)
		{
			return "Please enter a valid number between 0-100 number is: " + result + "\n" + text;
		}
		if (!IsValueAcceptable(result))
		{
			return "The value is too much";
		}
		if (TryGetObject(separatedNames[0], out var obj, out var errorMessage, (Hero x) => x.IsAlive && (x.Occupation == Occupation.Lord || x.Occupation == Occupation.Wanderer)))
		{
			int value = (int)((float)(campaignBehavior.GetMaxHeroCraftingStamina(obj) * result) / 100f);
			campaignBehavior.SetHeroCraftingStamina(obj, value);
			return "Success";
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_hero_culture", "campaign")]
	public static string SetHeroCulture(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_hero_culture [HeroName] | [CultureName]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		if (TryGetObject(separatedNames[1], out CultureObject obj, out string errorMessage, (Func<CultureObject, bool>)null))
		{
			if (TryGetObject(separatedNames[0], out var obj2, out var errorMessage2, (Hero x) => x.Occupation == Occupation.Lord || x.Occupation == Occupation.Wanderer))
			{
				if (obj2.Culture == obj)
				{
					return $"Hero culture is already {obj.Name}";
				}
				obj2.Culture = obj;
				return "Success";
			}
			return errorMessage2 + "\n" + text;
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_clan_culture", "campaign")]
	public static string SetClanCulture(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_clan_culture [ClanName] | [CultureName]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		if (TryGetObject(separatedNames[1], out CultureObject obj, out string errorMessage, (Func<CultureObject, bool>)null))
		{
			if (TryGetObject(separatedNames[0], out Clan obj2, out errorMessage, (Func<Clan, bool>)null))
			{
				if (obj2.Culture == obj)
				{
					return $"Clan culture is already {obj.Name}";
				}
				obj2.Culture = obj;
				return "Success";
			}
			return errorMessage + "\n" + text;
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_skill_xp_to_hero", "campaign")]
	public static string AddSkillXpToHero(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		Hero obj = Hero.MainHero;
		int result = 100;
		string text = "Format is \"campaign.add_skill_xp_to_hero [HeroName] | [SkillName] | [PositiveNumber]\".";
		if (CheckHelp(strings))
		{
			return text;
		}
		if (CheckParameters(strings, 0))
		{
			if (obj != null)
			{
				string text2 = "";
				{
					foreach (SkillObject item in Skills.All)
					{
						obj.HeroDeveloper.AddSkillXp(item, result);
						int num = (int)(obj.HeroDeveloper.GetFocusFactor(item) * (float)result);
						text2 += $"{result} xp is modified to {num} xp due to focus point factor \nand added to the {obj.Name}'s {item.Name} skill.\n";
					}
					return text2;
				}
			}
			return "Wrong Input.\n" + text;
		}
		List<string> separatedNames = GetSeparatedNames(strings, removeEmptySpaces: true);
		if (separatedNames.Count == 1)
		{
			string text3 = "";
			if (int.TryParse(separatedNames[0], out result))
			{
				if (result <= 0)
				{
					return "Please enter a positive number\n" + text;
				}
				{
					foreach (SkillObject item2 in Skills.All)
					{
						obj.HeroDeveloper.AddSkillXp(item2, result);
						int num2 = (int)(obj.HeroDeveloper.GetFocusFactor(item2) * (float)result);
						text3 += $"{result} xp is modified to {num2} xp due to focus point factor \nand added to the {obj.Name}'s {item2.Name} skill.\n";
					}
					return text3;
				}
			}
			TryGetObject(separatedNames[0], out obj, out string _, (Func<Hero, bool>)null);
			result = 100;
			if (obj == null)
			{
				obj = Hero.MainHero;
				string text4 = separatedNames[0].ToLower();
				{
					foreach (SkillObject item3 in Skills.All)
					{
						if (item3.Name.ToString().Replace(" ", "").ToLower()
							.Equals(text4, StringComparison.InvariantCultureIgnoreCase) || item3.StringId.Replace(" ", "").ToLower() == text4)
						{
							if (obj.GetSkillValue(item3) < 300)
							{
								obj.HeroDeveloper.AddSkillXp(item3, result);
								int num2 = (int)(obj.HeroDeveloper.GetFocusFactor(item3) * (float)result);
								return $"Input {result} xp is modified to {num2} xp due to focus point factor \nand added to the {obj.Name}'s {item3.Name} skill. ";
							}
							return $"{item3} value for {obj} is already at max.. ";
						}
					}
					return text;
				}
			}
			{
				foreach (SkillObject item4 in Skills.All)
				{
					obj.HeroDeveloper.AddSkillXp(item4, result);
					int num2 = (int)(obj.HeroDeveloper.GetFocusFactor(item4) * (float)result);
					text3 += $"{result} xp is modified to {num2} xp due to focus point factor \nand added to the {obj.Name}'s {item4.Name} skill.\n";
				}
				return text3;
			}
		}
		if (separatedNames.Count == 2)
		{
			TryGetObject(separatedNames[0], out obj, out string _, (Func<Hero, bool>)null);
			if (obj == null)
			{
				obj = Hero.MainHero;
				if (!int.TryParse(separatedNames[1], out result))
				{
					return text;
				}
				if (result <= 0)
				{
					return "Please enter a positive number\n" + text;
				}
				string text5 = separatedNames[0];
				foreach (SkillObject item5 in Skills.All)
				{
					if (item5.Name.ToString().Replace(" ", "").Equals(text5.Replace(" ", ""), StringComparison.InvariantCultureIgnoreCase) || item5.StringId == text5.Replace(" ", ""))
					{
						if (obj.GetSkillValue(item5) < 300)
						{
							obj.HeroDeveloper.AddSkillXp(item5, result);
							int num3 = (int)(obj.HeroDeveloper.GetFocusFactor(item5) * (float)result);
							return $"Input {result} xp is modified to {num3} xp due to focus point factor \nand added to the {obj.Name}'s {item5.Name} skill. ";
						}
						return $"{item5} value for {obj} is already at max.. ";
					}
				}
				return "Skill not found.\n" + text;
			}
			if (!int.TryParse(separatedNames[1], out result))
			{
				result = 100;
				string text6 = separatedNames[1];
				foreach (SkillObject item6 in Skills.All)
				{
					if (item6.Name.ToString().Replace(" ", "").Equals(text6.Replace(" ", ""), StringComparison.InvariantCultureIgnoreCase) || item6.StringId == text6.Replace(" ", ""))
					{
						if (obj.GetSkillValue(item6) < 300)
						{
							obj.HeroDeveloper.AddSkillXp(item6, result);
							int num4 = (int)(obj.HeroDeveloper.GetFocusFactor(item6) * (float)result);
							return $"Input {result} xp is modified to {num4} xp due to focus point factor \nand added to the {obj.Name}'s {item6.Name} skill. ";
						}
						return $"{item6} value for {obj} is already at max.. ";
					}
				}
				return "Skill not found.\n" + text;
			}
			if (result <= 0)
			{
				return "Please enter a positive number\n" + text;
			}
			using List<SkillObject>.Enumerator enumerator = Skills.All.GetEnumerator();
			if (enumerator.MoveNext())
			{
				SkillObject current7 = enumerator.Current;
				if (obj.GetSkillValue(current7) < 300)
				{
					obj.HeroDeveloper.AddSkillXp(current7, result);
					int num5 = (int)(obj.HeroDeveloper.GetFocusFactor(current7) * (float)result);
					return $"Input {result} xp is modified to {num5} xp due to focus point factor \nand added to the {obj.Name}'s {current7.Name} skill. ";
				}
				return $"{current7} value for {obj} is already at max.. ";
			}
		}
		if (separatedNames.Count == 3)
		{
			if (!int.TryParse(separatedNames[2], out result) || result < 0)
			{
				return "Please enter a positive number\n" + text;
			}
			TryGetObject(separatedNames[0], out obj, out string errorMessage3, (Func<Hero, bool>)null);
			if (obj == null)
			{
				return errorMessage3 + "\n" + text;
			}
			string text7 = separatedNames[1];
			foreach (SkillObject item7 in Skills.All)
			{
				if (item7.Name.ToString().Replace(" ", "").Equals(text7.Replace(" ", ""), StringComparison.InvariantCultureIgnoreCase) || item7.StringId == text7.Replace(" ", ""))
				{
					if (obj.GetSkillValue(item7) < 300)
					{
						obj.HeroDeveloper.AddSkillXp(item7, result);
						int num6 = (int)(obj.HeroDeveloper.GetFocusFactor(item7) * (float)result);
						return $"Input {result} xp is modified to {num6} xp due to focus point factor \nand added to the {obj.Name}'s {item7.Name} skill. ";
					}
					return $"{item7} value for {obj} is already at max.. ";
				}
			}
			return "Skill not found.\n" + text;
		}
		return text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("print_player_traits", "campaign")]
	public static string PrintPlayerTrait(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (CheckHelp(strings))
		{
			return "Format is \"campaign.print_player_traits\".";
		}
		string text = "";
		foreach (TraitObject item in TraitObject.All)
		{
			text = text + item.Name.ToString() + " Trait Level:  " + Hero.MainHero.GetTraitLevel(item) + " Trait Xp: " + Campaign.Current.PlayerTraitDeveloper.GetPropertyValue(item) + "\n";
		}
		return text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("show_settlements", "campaign")]
	public static string ShowSettlements(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		foreach (Settlement item in Settlement.All)
		{
			if (!item.IsHideout)
			{
				item.IsVisible = true;
				item.IsInspected = true;
			}
		}
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_skills_of_hero", "campaign")]
	public static string SetSkillsOfGivenHero(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_skills_of_hero [HeroName] | [Level]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		int result = -1;
		if (!int.TryParse(separatedNames[1], out result))
		{
			return "Level must be a number\n" + text;
		}
		if (!IsValueAcceptable(result))
		{
			return "The value is too much";
		}
		if (TryGetObject(separatedNames[0], out var obj, out var errorMessage, (Hero x) => x.IsAlive && (x.Occupation == Occupation.Lord || x.Occupation == Occupation.Wanderer)))
		{
			if (result > 0 && result <= 300)
			{
				obj.CharacterObject.Level = 0;
				obj.HeroDeveloper.ClearHero();
				int num = TaleWorlds.Library.MathF.Min(result / 25 + 1, 10);
				int maxFocusPerSkill = Campaign.Current.Models.CharacterDevelopmentModel.MaxFocusPerSkill;
				foreach (SkillObject item2 in Skills.All)
				{
					if (obj.HeroDeveloper.GetFocus(item2) + num > maxFocusPerSkill)
					{
						num = maxFocusPerSkill;
					}
					obj.HeroDeveloper.AddFocus(item2, num, checkUnspentFocusPoints: false);
					obj.HeroDeveloper.SetInitialSkillLevel(item2, result);
				}
				if (obj.Clan == Clan.PlayerClan)
				{
					for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
					{
						ItemObject item = obj.BattleEquipment[equipmentIndex].Item;
						if (item != null && item.Difficulty > result)
						{
							obj.PartyBelongedTo?.ItemRoster.AddToCounts(obj.BattleEquipment[equipmentIndex], 1);
							obj.BattleEquipment[equipmentIndex].Clear();
						}
						item = obj.CivilianEquipment[equipmentIndex].Item;
						if (item != null && item.Difficulty > result)
						{
							obj.PartyBelongedTo?.ItemRoster.AddToCounts(obj.CivilianEquipment[equipmentIndex], 1);
							obj.CivilianEquipment[equipmentIndex].Clear();
						}
					}
				}
				obj.HeroDeveloper.UnspentFocusPoints = 0;
				return $"{obj.Name}'s skills are set to level {result}.";
			}
			return $"Level must be between 0 - {300}.";
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("hide_settlements", "campaign")]
	public static string HideSettlements(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		foreach (Settlement item in Settlement.All)
		{
			if (!item.IsHideout && !(item.SettlementComponent is RetirementSettlementComponent))
			{
				item.IsVisible = false;
				item.IsInspected = false;
			}
		}
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_skill_player", "campaign")]
	public static string SetSkillMainHero(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_skill_player [SkillName] | [LevelValue]\".";
		List<string> separatedNames = GetSeparatedNames(strings, removeEmptySpaces: true);
		if (separatedNames.Count != 2 || CheckHelp(strings))
		{
			return text;
		}
		if (TryGetObject(separatedNames[0], out SkillObject obj, out string errorMessage, (Func<SkillObject, bool>)null))
		{
			if (!int.TryParse(separatedNames[1], out var result))
			{
				return "Please enter a number\n" + text;
			}
			if (result <= 0 || result > 300)
			{
				return $"Level must be between 0 - {300}.";
			}
			if (!IsValueAcceptable(result))
			{
				return "The value is too much";
			}
			Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(obj, result);
			Hero.MainHero.HeroDeveloper.InitializeSkillXp(obj);
			return "Success";
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_skill_of_all_companions", "campaign")]
	public static string SetSkillCompanion(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_skill_of_all_companions [SkillName] | [LevelValue]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings, removeEmptySpaces: true);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		if (Clan.PlayerClan.Companions.Count == 0)
		{
			return "There is no companion in player clan";
		}
		if (TryGetObject(separatedNames[0], out SkillObject obj, out string errorMessage, (Func<SkillObject, bool>)null))
		{
			int result = 1;
			if (!int.TryParse(separatedNames[1], out result))
			{
				return "Please enter a number\n" + text;
			}
			if (result <= 0 || result > 300)
			{
				return $"Level must be between 0 - {300}.";
			}
			if (!IsValueAcceptable(result))
			{
				return "The value is too much";
			}
			foreach (HeroDeveloper item in Clan.PlayerClan.Companions.Select((Hero x) => x.HeroDeveloper))
			{
				item.SetInitialSkillLevel(obj, result);
				item.InitializeSkillXp(obj);
			}
			return "Success";
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_all_companion_skills", "campaign")]
	public static string SetAllSkillsOfAllCompanions(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_all_companion_skills [LevelValue]\".";
		if (!CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		foreach (SkillObject item in Skills.All)
		{
			int result = 1;
			if (strings.Count == 0 || !int.TryParse(strings[0], out result))
			{
				return "Please enter a number\n" + text;
			}
			if (result <= 0 || result > 300)
			{
				return $"Level must be between 0 - {300}.";
			}
			if (!IsValueAcceptable(result))
			{
				return "The value is too much";
			}
			foreach (Hero companion in Clan.PlayerClan.Companions)
			{
				companion.HeroDeveloper.SetInitialSkillLevel(item, result);
				companion.HeroDeveloper.InitializeSkillXp(item);
			}
		}
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_all_heroes_skills", "campaign")]
	public static string SetAllHeroSkills(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_all_heroes_skills [LevelValue]\".";
		if (CheckParameters(strings, 0) || CheckHelp(strings))
		{
			return text;
		}
		if (strings.Count == 0 || !int.TryParse(strings[0], out var result))
		{
			return "Please enter a positive number\n" + text;
		}
		foreach (Hero item in Hero.AllAliveHeroes.Where((Hero x) => x.IsActive && x.PartyBelongedTo != null).ToList())
		{
			foreach (SkillObject item2 in Skills.All)
			{
				if (result <= 0 || result > 300)
				{
					return $"Level must be between 0 - {300}.";
				}
				if (!IsValueAcceptable(result))
				{
					return "The value is too much";
				}
				item.HeroDeveloper.SetInitialSkillLevel(item2, result);
				item.HeroDeveloper.InitializeSkillXp(item2);
			}
		}
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_loyalty_of_settlement", "campaign")]
	public static string SetLoyaltyOfSettlement(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_loyalty_of_settlement [SettlementName] | [loyalty]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		if (!int.TryParse(separatedNames[1], out var result))
		{
			return "Please enter a positive number\n" + text;
		}
		if (result > 100 || result < 0)
		{
			return "Loyalty has to be in the range of 0 to 100";
		}
		if (!IsValueAcceptable(result))
		{
			return "The value is too much";
		}
		string text2 = separatedNames[0];
		if (TryGetObject(text2, out Settlement obj, out string errorMessage, (Func<Settlement, bool>)null))
		{
			if (obj.IsVillage)
			{
				return "Settlement must be castle or town";
			}
			obj.Town.Loyalty = result;
			return "Success";
		}
		return errorMessage + ": " + text2 + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_prosperity_of_settlement", "campaign")]
	public static string SetProsperityOfSettlement(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_prosperity_of_settlement [SettlementName/SettlementID] | [Value]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckParameters(strings, 2) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		string text2 = separatedNames[0];
		if (TryGetObject(text2, out Settlement obj, out string errorMessage, (Func<Settlement, bool>)null))
		{
			if (obj.IsVillage)
			{
				return "Settlement must be castle or town";
			}
			if (!float.TryParse(separatedNames[1], out var result) || result < 0f)
			{
				return "Please enter a positive number\n" + text;
			}
			if (!IsValueAcceptable(result))
			{
				return "The value is too much";
			}
			obj.Town.Prosperity = result;
			return "Success";
		}
		return errorMessage + ": " + text2 + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_militia_of_settlement", "campaign")]
	public static string SetMilitiaOfSettlement(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_militia_of_settlement [SettlementName/SettlementID] | [Value]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckParameters(strings, 2) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		string text2 = separatedNames[0];
		if (TryGetObject(text2, out Settlement obj, out string errorMessage, (Func<Settlement, bool>)null))
		{
			if (!float.TryParse(separatedNames[1], out var result))
			{
				return "Please enter a number\n" + text;
			}
			if (!IsValueAcceptable(result))
			{
				return "The value is too much";
			}
			obj.Militia = result;
			return "Success";
		}
		return errorMessage + ": " + text2 + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_security_of_settlement", "campaign")]
	public static string SetSecurityOfSettlement(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_security_of_settlement [SettlementName/SettlementID] | [Value]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckParameters(strings, 2) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		string text2 = separatedNames[0];
		if (TryGetObject(text2, out Settlement obj, out string errorMessage, (Func<Settlement, bool>)null))
		{
			if (obj.IsVillage)
			{
				return "Settlement must be castle or town";
			}
			if (!float.TryParse(separatedNames[1], out var result))
			{
				return "Please enter a number\n" + text;
			}
			if (!IsValueAcceptable(result))
			{
				return "The value is too much";
			}
			obj.Town.Security = result;
			return "Success";
		}
		return errorMessage + ": " + text2 + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_food_of_settlement", "campaign")]
	public static string SetFoodOfSettlement(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_food_of_settlement [SettlementName/SettlementID] | [Value]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckParameters(strings, 2) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		string text2 = separatedNames[0];
		if (TryGetObject(text2, out Settlement obj, out string errorMessage, (Func<Settlement, bool>)null))
		{
			if (obj.IsVillage)
			{
				return "Settlement must be castle or town";
			}
			if (!float.TryParse(separatedNames[1], out var result))
			{
				return "Please enter a number\n" + text;
			}
			if (!IsValueAcceptable(result))
			{
				return "The value is too much";
			}
			obj.Town.FoodStocks = result;
			return "Success";
		}
		return errorMessage + ": " + text2 + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_hearth_of_settlement", "campaign")]
	public static string SetHearthOfSettlement(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_hearth_of_settlement [SettlementName/SettlementID] | [Value]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckParameters(strings, 2) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		string text2 = separatedNames[0];
		if (TryGetObject(text2, out var obj, out var errorMessage, (Settlement x) => x.IsVillage))
		{
			if (obj.Village == null)
			{
				return "Settlement doesn't have hearth variable.";
			}
			if (!float.TryParse(separatedNames[1], out var result))
			{
				return "Please enter a number\n" + text;
			}
			if (!IsValueAcceptable(result))
			{
				return "The value is too much";
			}
			obj.Village.Hearth = result;
			return "Success";
		}
		return errorMessage + ": " + text2 + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("show_relation", "campaign")]
	public static string ShowHeroRelation(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string result = (Game.Current.IsDevelopmentMode ? "Format is \"campaign.show_relation [HeroName] | [HeroName] \".\n" : "Format is \"campaign.show_relation [HeroName] | [HeroName] \".\n");
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return result;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return result;
		}
		string requestedId = separatedNames[0];
		string requestedId2 = separatedNames[1];
		if (!TryGetObject(requestedId, out Hero obj, out string errorMessage, (Func<Hero, bool>)null))
		{
			return errorMessage;
		}
		if (!TryGetObject(requestedId2, out Hero obj2, out string errorMessage2, (Func<Hero, bool>)null))
		{
			return errorMessage2;
		}
		return $"{obj.Name} relation to {obj2.Name}: {obj.GetRelation(obj2)}";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_hero_relation", "campaign")]
	public static string AddHeroRelation(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		bool isDevelopmentMode = Game.Current.IsDevelopmentMode;
		string text = (isDevelopmentMode ? "Format is \"campaign.add_hero_relation [HeroName]/All | [OtherHeroName(optional)] | [Value] \".\n" : "Format is \"campaign.add_hero_relation [HeroName] | [OtherHeroName(optional)] | [Value] \".\n");
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		Hero obj = Hero.MainHero;
		string empty = string.Empty;
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count == 3)
		{
			if (!TryGetObject(separatedNames[1], out obj, out string errorMessage, (Func<Hero, bool>)null))
			{
				return errorMessage;
			}
			empty = separatedNames[2];
		}
		else
		{
			if (separatedNames.Count != 2)
			{
				return text;
			}
			empty = separatedNames[1];
		}
		if (int.TryParse(empty, out var result))
		{
			if (!IsValueAcceptable(result))
			{
				return "The value is too much";
			}
			string text2 = separatedNames[0];
			TryGetObject(text2, out Hero obj2, out string errorMessage2, (Func<Hero, bool>)null);
			if (obj2 == obj)
			{
				return "Can not add relation to same heroes.";
			}
			if (obj2 != null)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(obj2, obj, result);
				return "Success";
			}
			if (string.Equals(text2, "all", StringComparison.OrdinalIgnoreCase) && isDevelopmentMode)
			{
				foreach (Hero allAliveHero in Hero.AllAliveHeroes)
				{
					if (!allAliveHero.IsHumanPlayerCharacter && allAliveHero != obj)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(allAliveHero, obj, result);
					}
				}
				return "Success";
			}
			return errorMessage2 + "\n" + text;
		}
		return "Please enter a number\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("print_player_party_position", "campaign")]
	public static string PrintMainPartyPosition(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (!CheckParameters(strings, 0) || CheckHelp(strings))
		{
			return "Format is \"campaign.print_player_party_position\".";
		}
		return MobileParty.MainParty.Position.X + " " + MobileParty.MainParty.Position.Y;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_crafting_materials", "campaign")]
	public static string AddCraftingMaterials(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (!CheckParameters(strings, 0) || CheckHelp(strings))
		{
			return "Format is \"campaign.add_crafting_materials\".";
		}
		for (int i = 0; i < 9; i++)
		{
			PartyBase.MainParty.ItemRoster.AddToCounts(Campaign.Current.Models.SmithingModel.GetCraftingMaterialItem((CraftingMaterials)i), 100);
		}
		return "100 pieces for each crafting material is added to the player inventory.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("heal_player_party", "campaign")]
	public static string HealMainParty(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (!CheckParameters(strings, 0) || CheckHelp(strings))
		{
			return "Format is \"campaign.heal_player_party\".";
		}
		if (MobileParty.MainParty.MapEvent == null)
		{
			for (int i = 0; i < PartyBase.MainParty.MemberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = PartyBase.MainParty.MemberRoster.GetElementCopyAtIndex(i);
				if (elementCopyAtIndex.Character.IsHero)
				{
					elementCopyAtIndex.Character.HeroObject.Heal(elementCopyAtIndex.Character.HeroObject.MaxHitPoints);
				}
				else
				{
					MobileParty.MainParty.Party.AddToMemberRosterElementAtIndex(i, 0, -PartyBase.MainParty.MemberRoster.GetElementWoundedNumber(i));
				}
			}
			return "Success";
		}
		return "Party shouldn't be in a map event.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("declare_war", "campaign")]
	public static string DeclareWar(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is campaign.declare_war [Faction1] | [Faction2]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings, removeEmptySpaces: true);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		string text2 = separatedNames[0];
		string text3 = separatedNames[1];
		TryGetObject(separatedNames[0], out Kingdom obj, out string errorMessage, (Func<Kingdom, bool>)null);
		TryGetObject(separatedNames[1], out Kingdom obj2, out errorMessage, (Func<Kingdom, bool>)null);
		IFaction faction;
		if (obj != null)
		{
			faction = obj;
		}
		else
		{
			if (!TryGetObject(separatedNames[0], out Clan obj3, out string errorMessage2, (Func<Clan, bool>)null))
			{
				return errorMessage2 + ": " + text2 + "\n" + text;
			}
			faction = obj3;
		}
		IFaction faction2;
		if (obj2 != null)
		{
			faction2 = obj2;
		}
		else
		{
			if (!TryGetObject(separatedNames[1], out Clan obj4, out string errorMessage3, (Func<Clan, bool>)null))
			{
				return errorMessage3 + ": " + text3 + "\n" + text;
			}
			faction2 = obj4;
		}
		if (faction == faction2 || faction.MapFaction == faction2.MapFaction)
		{
			return "Can't declare between same factions";
		}
		if (!faction.IsMapFaction)
		{
			return string.Concat(faction.Name, " is bound to a kingdom.");
		}
		if (!faction2.IsMapFaction)
		{
			return string.Concat(faction.Name, " is bound to a kingdom.");
		}
		if (faction.IsAtWarWith(faction2))
		{
			return "The factions already at war";
		}
		if (faction.IsEliminated)
		{
			return "Faction 1 is eliminated";
		}
		if (faction2.IsEliminated)
		{
			return "Faction 2 is eliminated";
		}
		DeclareWarAction.ApplyByDefault(faction, faction2);
		return string.Concat("War declared between ", faction.Name, " and ", faction2.Name);
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_item_to_player_party", "campaign")]
	public static string AddItemToPlayerParty(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.add_item_to_player_party [ItemId] | [ModifierId] | [Amount]\"\n If amount is not entered only 1 item will be given.\n Modifier name is optional.";
		if (CheckParameters(strings, 0) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(separatedNames[0]);
		if (itemObject != null)
		{
			int result = 1;
			if (separatedNames.Count == 1)
			{
				PartyBase.MainParty.ItemRoster.AddToCounts(itemObject, result);
				return string.Concat(itemObject.Name, " has been given to the main party.");
			}
			ItemModifier itemModifier = Game.Current.ObjectManager.GetObject<ItemModifier>(separatedNames[1]);
			if (itemModifier != null)
			{
				EquipmentElement rosterElement = new EquipmentElement(itemObject, itemModifier);
				if (separatedNames.Count > 2 && int.TryParse(separatedNames[2], out result) && result >= 1)
				{
					if (!IsValueAcceptable(result))
					{
						return "The value is too much";
					}
					MobileParty.MainParty.ItemRoster.AddToCounts(rosterElement, result);
				}
				else
				{
					MobileParty.MainParty.ItemRoster.AddToCounts(rosterElement, 1);
				}
				return string.Concat(rosterElement.GetModifiedItemName(), " has been given to the main party.");
			}
			if (int.TryParse(separatedNames[1], out result) && result >= 1)
			{
				if (!IsValueAcceptable(result))
				{
					return "The value is too much";
				}
				MobileParty.MainParty.ItemRoster.AddToCounts(itemObject, result);
				return string.Concat(itemObject.Name, " has been given to the main party.");
			}
			return "Second parameter is invalid.\n" + text;
		}
		return "Item is not found\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("declare_peace", "campaign")]
	public static string DeclarePeace(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "campaign.declare_peace [Faction1] | [Faction2]";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings, removeEmptySpaces: true);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		string text2 = separatedNames[0];
		string text3 = separatedNames[1];
		TryGetObject(separatedNames[0], out Kingdom obj, out string errorMessage, (Func<Kingdom, bool>)null);
		TryGetObject(separatedNames[1], out Kingdom obj2, out errorMessage, (Func<Kingdom, bool>)null);
		IFaction faction;
		if (obj != null)
		{
			faction = obj;
		}
		else
		{
			if (!TryGetObject(separatedNames[0], out Clan obj3, out string errorMessage2, (Func<Clan, bool>)null))
			{
				return errorMessage2 + ": " + text2 + "\n" + text;
			}
			faction = obj3;
		}
		IFaction faction2;
		if (obj2 != null)
		{
			faction2 = obj2;
		}
		else
		{
			if (!TryGetObject(separatedNames[1], out Clan obj4, out string errorMessage3, (Func<Clan, bool>)null))
			{
				return errorMessage3 + ": " + text3 + "\n" + text;
			}
			faction2 = obj4;
		}
		if (faction == faction2 || faction.MapFaction == faction2.MapFaction)
		{
			return "Can't declare between same factions";
		}
		if (!faction.IsMapFaction)
		{
			return string.Concat(faction.Name, " is bound to a kingdom.");
		}
		if (!faction2.IsMapFaction)
		{
			return string.Concat(faction.Name, " is bound to a kingdom.");
		}
		if (FactionManager.IsAtConstantWarAgainstFaction(faction, faction2))
		{
			return "There is constant war between factions, peace can't be declared";
		}
		if (!faction.IsAtWarWith(faction2))
		{
			return "The factions not at war";
		}
		MakePeaceAction.Apply(faction, faction2);
		return string.Concat("Peace declared between ", faction.Name, " and ", faction2.Name);
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_influence", "campaign")]
	public static string AddInfluence(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (CheckHelp(strings))
		{
			return "Format is \"campaign.add_influence [Number]\". If Number is not entered, 100 influence will be added.";
		}
		int result = 100;
		bool flag = false;
		if (!CheckParameters(strings, 0))
		{
			flag = int.TryParse(strings[0], out result);
		}
		if (!IsValueAcceptable(result))
		{
			return "The value is too much";
		}
		if (flag || CheckParameters(strings, 0))
		{
			float num = MBMath.ClampFloat(result, -200f, float.MaxValue);
			ChangeClanInfluenceAction.Apply(Clan.PlayerClan, num);
			return $"The influence of player is changed by {num} to {Clan.PlayerClan.Influence} ";
		}
		return "Please enter a positive number\nFormat is \"campaign.add_influence [Number]\".";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_renown_to_clan", "campaign")]
	public static string AddRenown(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.add_renown [ClanName] | [PositiveNumber]\". \n If number is not specified, 100 will be added. \n If clan name is not specified, player clan will get the renown.";
		if (CheckHelp(strings))
		{
			return text;
		}
		int result = 100;
		string text2 = "";
		Hero hero = Hero.MainHero;
		bool flag = false;
		if (CheckParameters(strings, 1))
		{
			if (!int.TryParse(strings[0], out result))
			{
				result = 100;
				text2 = ConcatenateString(strings);
				hero = GetClanLeader(text2);
				flag = true;
			}
		}
		else if (!CheckParameters(strings, 0))
		{
			List<string> separatedNames = GetSeparatedNames(strings, removeEmptySpaces: true);
			if (separatedNames.Count == 2 && !int.TryParse(separatedNames[1], out result))
			{
				return "Please enter a positive number\n" + text;
			}
			text2 = separatedNames[0];
			hero = GetClanLeader(text2);
			flag = true;
		}
		if (hero != null)
		{
			if (!IsValueAcceptable(result))
			{
				return "The value is too much";
			}
			if (result > 0)
			{
				GainRenownAction.Apply(hero, result);
				return $"Added {result} renown to " + hero.Clan.Name;
			}
			return "Please enter a positive number\n" + text;
		}
		if (flag)
		{
			return "Clan: " + text2 + " not found.\n" + text;
		}
		return "Wrong Input.\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_gold_to_hero", "campaign")]
	public static string AddGoldToHero(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.add_gold_to_hero [HeroName] | [PositiveNumber]\".\n If number is not specified, 1000 will be added. \n If hero name is not specified, player's gold will change.";
		if (CheckHelp(strings))
		{
			return text;
		}
		int result = 1000;
		Hero obj = Hero.MainHero;
		string errorMessage = string.Empty;
		if (CheckParameters(strings, 0))
		{
			GiveGoldAction.ApplyBetweenCharacters(null, obj, result, disableNotification: true);
			return "Success";
		}
		if (CheckParameters(strings, 1) && !int.TryParse(strings[0], out result))
		{
			result = 1000;
			TryGetObject(ConcatenateString(strings), out obj, out errorMessage, (Func<Hero, bool>)null);
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count == 2)
		{
			if (!int.TryParse(separatedNames[1], out result))
			{
				return "Please enter a number\n" + text;
			}
			TryGetObject(separatedNames[0], out obj, out errorMessage, (Func<Hero, bool>)null);
		}
		if (separatedNames.Count == 1 && !int.TryParse(separatedNames[0], out result))
		{
			TryGetObject(separatedNames[0], out obj, out errorMessage, (Func<Hero, bool>)null);
		}
		if (obj == null)
		{
			return errorMessage + "\n" + text;
		}
		if (obj.Gold + result < 0 || obj.Gold + result > 100000000)
		{
			return "Hero's gold must be between 0-100000000.";
		}
		GiveGoldAction.ApplyBetweenCharacters(null, obj, result, disableNotification: true);
		return $"{obj.Name}'s denars changed by {result}.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_building_level", "campaign")]
	public static string AddDevelopment(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.add_building_level [SettlementName] | [Building]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		if (TryGetObject(separatedNames[0], out Settlement obj, out string errorMessage, (Func<Settlement, bool>)null) && obj.IsFortification)
		{
			if (!obj.IsUnderSiege && !obj.IsUnderRaid && obj.Party.MapEvent == null)
			{
				string requestedId = separatedNames[1];
				List<Building> settlementBuildings = obj.Town.Buildings.ToList();
				if (TryGetObject(requestedId, out var buildingType, out errorMessage, (BuildingType x) => settlementBuildings.ContainsQ((Building y) => y.BuildingType == x)))
				{
					Building building = settlementBuildings.First((Building x) => x.BuildingType == buildingType);
					if (building.CurrentLevel < 3)
					{
						building.CurrentLevel++;
						CampaignEventDispatcher.Instance.OnBuildingLevelChanged(obj.Town, building, 1);
						return string.Concat(building.BuildingType.Name, " level increased to ", building.CurrentLevel, " at ", obj.Name);
					}
					return string.Concat(building.BuildingType.Name, " is already at max level!");
				}
				return errorMessage + "\n" + text;
			}
			return "Requested settlement is not suitable right now to take this action";
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("activate_all_policies_for_player_kingdom", "campaign")]
	public static string ActivateAllPolicies(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (!CheckParameters(strings, 0) || CheckHelp(strings))
		{
			return "Format is \"campaign.activate_all_policies_for_player_kingdom";
		}
		if (Clan.PlayerClan.Kingdom != null)
		{
			Kingdom kingdom = Clan.PlayerClan.Kingdom;
			foreach (PolicyObject item in PolicyObject.All)
			{
				if (!kingdom.ActivePolicies.Contains(item))
				{
					kingdom.AddPolicy(item);
				}
			}
			return "All policies are now active for player kingdom.";
		}
		return "Player is not in a kingdom.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_player_trait", "campaign")]
	public static string SetPlayerReputationTrait(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_player_trait [Trait] | [Number]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings, removeEmptySpaces: true);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		if (int.TryParse(separatedNames[1], out var result))
		{
			if (TryGetObject(separatedNames[0], out TraitObject obj, out string errorMessage, (Func<TraitObject, bool>)null))
			{
				if (result >= obj.MinValue && result <= obj.MaxValue)
				{
					Hero.MainHero.SetTraitLevel(obj, result);
					TraitLevelingHelper.UpdateTraitXPAccordingToTraitLevels();
					return $"Set {obj.Name} to {result}.";
				}
				return $"Number must be between {obj.MinValue} and {obj.MaxValue}.";
			}
			return errorMessage + "\n" + text;
		}
		return "Please enter a number\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("give_settlement_to_player", "campaign")]
	public static string GiveSettlementToPlayer(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.give_settlement_to_player [SettlementName/SettlementId]\nWrite \"campaign.give_settlement_to_player help\" to list available settlements.\nWrite \"campaign.give_settlement_to_player Calradia\" to give all settlements to player.";
		if (CheckParameters(strings, 0))
		{
			return text;
		}
		string text2 = ConcatenateString(strings);
		if (text2.ToLower() == "help")
		{
			string text3 = "";
			text3 += "\n";
			text3 += "Available settlements";
			text3 += "\n";
			text3 += "==============================";
			text3 += "\n";
			{
				foreach (Settlement objectType in MBObjectManager.Instance.GetObjectTypeList<Settlement>())
				{
					text3 = string.Concat(text3, "Id: ", objectType.StringId, " Name: ", objectType.Name, "\n");
				}
				return text3;
			}
		}
		if (Clan.PlayerClan.IsUnderMercenaryService)
		{
			return "Player cannot take ownership of a settlement during mercenary service.";
		}
		MBReadOnlyList<Settlement> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<Settlement>();
		if (text2.ToLower().Replace(" ", "") == "calradia")
		{
			foreach (Settlement item in objectTypeList)
			{
				if (item.IsCastle || item.IsTown)
				{
					ChangeOwnerOfSettlementAction.ApplyByDefault(Hero.MainHero, item);
				}
			}
			return "You own all of Calradia now!";
		}
		if (TryGetObject(text2, out Settlement obj, out string errorMessage, (Func<Settlement, bool>)null))
		{
			if (obj.IsVillage)
			{
				return "Settlement must be castle or town.";
			}
			ChangeOwnerOfSettlementAction.ApplyByDefault(Hero.MainHero, obj);
			return string.Concat(obj.Name, " has been given to the player.");
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("give_settlement_to_kingdom", "campaign")]
	public static string GiveSettlementToKingdom(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.give_settlement_to_kingdom [SettlementName] | [KingdomName]";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		if (TryGetObject(separatedNames[0], out Settlement obj, out string errorMessage, (Func<Settlement, bool>)null))
		{
			if (obj.IsVillage)
			{
				obj = obj.Village.Bound;
			}
			if (TryGetObject(separatedNames[1], out Kingdom obj2, out errorMessage, (Func<Kingdom, bool>)null))
			{
				if (obj.MapFaction == obj2)
				{
					return "Kingdom already owns the settlement.";
				}
				if (obj.IsVillage)
				{
					return "Settlement must be castle or town.";
				}
				ChangeOwnerOfSettlementAction.ApplyByDefault(obj2.Leader, obj);
				return string.Concat(obj.Name, $" has been given to {obj2.Leader.Name}.");
			}
			return errorMessage + "\n" + text;
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_power_to_notable", "campaign")]
	public static string AddPowerToNotable(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is campaign.add_power_to_notable [HeroName] | [Number]";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		if (!int.TryParse(separatedNames[1], out var result))
		{
			return "Please enter a positive number\n" + text;
		}
		if (result <= 0)
		{
			return "Please enter a positive number\n" + text;
		}
		if (TryGetObject(separatedNames[0], out Hero obj, out string errorMessage, (Func<Hero, bool>)null))
		{
			if (!obj.IsNotable)
			{
				return "Hero is not a notable.";
			}
			if (!IsValueAcceptable(result))
			{
				return "The value is too much";
			}
			obj.AddPower(result);
			return $"{obj.Name} power is {obj.Power}";
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("rule_your_faction", "campaign")]
	public static string LeadYourFaction(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (CheckHelp(strings))
		{
			return "Format is \"campaign.rule_your_faction\".";
		}
		if (Hero.MainHero.MapFaction.Leader != Hero.MainHero)
		{
			if (Hero.MainHero.MapFaction.IsKingdomFaction)
			{
				ChangeRulingClanAction.Apply(Hero.MainHero.MapFaction as Kingdom, Clan.PlayerClan);
			}
			else
			{
				(Hero.MainHero.MapFaction as Clan).SetLeader(Hero.MainHero);
			}
			return "Success";
		}
		return "Function execution failed.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("print_heroes_suitable_for_marriage", "campaign")]
	public static string PrintHeroesSuitableForMarriage(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (CheckHelp(strings))
		{
			return "Format is \"print_heroes_suitable_for_marriage\".";
		}
		List<Hero> list = new List<Hero>();
		List<Hero> list2 = new List<Hero>();
		foreach (Kingdom item in Kingdom.All)
		{
			foreach (Hero aliveLord in item.AliveLords)
			{
				if (aliveLord.CanMarry())
				{
					if (aliveLord.IsFemale)
					{
						list.Add(aliveLord);
					}
					else
					{
						list2.Add(aliveLord);
					}
				}
			}
		}
		string text = "Maidens:\n";
		string text2 = "Suitors:\n";
		foreach (Hero item2 in list)
		{
			TextObject textObject = ((item2.PartyBelongedTo == null) ? TextObject.GetEmpty() : item2.PartyBelongedTo.Name);
			text = string.Concat(text, "Name: ", item2.Name, " --- Clan: ", item2.Clan, " --- Party:", textObject, "\n");
		}
		foreach (Hero item3 in list2)
		{
			TextObject textObject2 = ((item3.PartyBelongedTo == null) ? TextObject.GetEmpty() : item3.PartyBelongedTo.Name);
			text2 = string.Concat(text2, "Name: ", item3.Name, " --- Clan: ", item3.Clan, " --- Party:", textObject2, "\n");
		}
		return text + "\n" + text2;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("marry_player_with_hero", "campaign")]
	public static string MarryPlayerWithHero(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.marry_player_with_hero [HeroName]\".";
		if (!CheckParameters(strings, 0) && !CheckHelp(strings))
		{
			if (!Campaign.Current.Models.MarriageModel.IsSuitableForMarriage(Hero.MainHero))
			{
				return "Main hero is not suitable for marriage";
			}
			string text2 = ConcatenateString(strings);
			if (TryGetObject(text2, out Hero obj, out string errorMessage, (Func<Hero, bool>)null))
			{
				MarriageModel marriageModel = Campaign.Current.Models.MarriageModel;
				if (marriageModel.IsCoupleSuitableForMarriage(Hero.MainHero, obj))
				{
					MarriageAction.Apply(Hero.MainHero, obj);
					return "Success";
				}
				if (!marriageModel.IsSuitableForMarriage(obj))
				{
					return $"Hero: {obj.Name} is not suitable for marriage.";
				}
				if (!marriageModel.IsClanSuitableForMarriage(obj.Clan))
				{
					return $"{obj.Name}'s clan is not suitable for marriage.";
				}
				if (!marriageModel.IsClanSuitableForMarriage(Hero.MainHero.Clan))
				{
					return "Main hero's clan is not suitable for marriage.";
				}
				if (Hero.MainHero.Clan?.Leader == Hero.MainHero && obj.Clan?.Leader == obj)
				{
					return "Clan leaders are not suitable for marriage.";
				}
				if (!obj.IsFemale)
				{
					return "Hero is not female.";
				}
				DefaultMarriageModel obj2 = new DefaultMarriageModel();
				if ((bool)typeof(DefaultMarriageModel).GetMethod("AreHeroesRelated", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj2, new object[3]
				{
					Hero.MainHero,
					obj,
					1
				}))
				{
					return "Heroes are related.";
				}
				Hero courtedHeroInOtherClan = Romance.GetCourtedHeroInOtherClan(Hero.MainHero, obj);
				if (courtedHeroInOtherClan != null && courtedHeroInOtherClan != obj)
				{
					return $"{courtedHeroInOtherClan.Name} has courted {Hero.MainHero.Name}.";
				}
				Hero courtedHeroInOtherClan2 = Romance.GetCourtedHeroInOtherClan(obj, Hero.MainHero);
				if (courtedHeroInOtherClan2 != null && courtedHeroInOtherClan2 != Hero.MainHero)
				{
					return $"{courtedHeroInOtherClan2.Name} has courted {obj.Name}.";
				}
				return string.Concat("Marriage is not suitable between ", Hero.MainHero.Name, " and ", obj.Name, "\n");
			}
			return errorMessage + ": " + text2.ToLower() + "\n" + text;
		}
		return text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("marry_hero_to_hero", "campaign")]
	public static string MarryHeroWithHero(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string result = "Format is \"campaign.marry_hero_to_hero [HeroName] | [HeroName]\".";
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count == 2 && !CheckHelp(strings))
		{
			TryGetObject(separatedNames[0], out Hero obj, out string errorMessage, (Func<Hero, bool>)null);
			TryGetObject(separatedNames[1], out Hero obj2, out string errorMessage2, (Func<Hero, bool>)null);
			if (obj != null && obj2 != null)
			{
				if (Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(obj, obj2))
				{
					MarriageAction.Apply(obj, obj2);
					return "Success";
				}
				return "They are not suitable for marriage";
			}
			if (obj == null)
			{
				return errorMessage + "\nCan't find a hero with name: " + separatedNames[0];
			}
			return errorMessage2 + "\nCan't find a hero with name: " + separatedNames[1];
		}
		return result;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("is_hero_suitable_for_marriage_with_player", "campaign")]
	public static string IsHeroSuitableForMarriageWithPlayer(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.is_hero_suitable_for_marriage_with_player [HeroName]\".";
		if (!CheckParameters(strings, 0) && !CheckHelp(strings))
		{
			string text2 = ConcatenateString(strings);
			if (TryGetObject(text2, out Hero obj, out string errorMessage, (Func<Hero, bool>)null))
			{
				if (Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(Hero.MainHero, obj))
				{
					return string.Concat("Marriage is suitable between ", Hero.MainHero.Name, " and ", obj.Name, "\n");
				}
				return string.Concat("Marriage is not suitable between ", Hero.MainHero.Name, " and ", obj.Name, "\n");
			}
			return errorMessage + ": " + text2.ToLower() + "\n" + text;
		}
		return text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("create_player_kingdom", "campaign")]
	public static string CreatePlayerKingdom(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (CheckHelp(strings) || !CheckParameters(strings, 0))
		{
			return "Format is \"campaign.create_player_kingdom\".";
		}
		Campaign.Current.KingdomManager.CreateKingdom(Clan.PlayerClan.Name, Clan.PlayerClan.InformalName, Clan.PlayerClan.Culture, Clan.PlayerClan);
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("create_clan", "campaign")]
	public static string CreateRandomClan(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.create_clan [KingdomName]\".";
		if (CheckHelp(strings) || !CheckParameters(strings, 1))
		{
			return text;
		}
		TryGetObject(GetSeparatedNames(strings)[0], out Kingdom obj, out string errorMessage, (Func<Kingdom, bool>)null);
		if (obj == null)
		{
			return errorMessage + "\n" + text;
		}
		CultureObject culture = obj.Culture;
		Settlement settlement = obj.Settlements.FirstOrDefault((Settlement x) => x.IsTown) ?? obj.Settlements.GetRandomElement() ?? Settlement.All.FirstOrDefault((Settlement x) => x.IsTown && x.Culture == culture);
		TextObject textObject = NameGenerator.Current.GenerateClanName(culture, settlement);
		Clan clan = Clan.CreateClan("test_clan_" + Clan.All.Count);
		clan.ChangeClanName(textObject, textObject);
		clan.Culture = Kingdom.All.GetRandomElement().Culture;
		clan.Banner = Banner.CreateRandomClanBanner();
		clan.SetInitialHomeSettlement(settlement);
		CharacterObject characterObject = culture.LordTemplates.FirstOrDefault((CharacterObject x) => x.Occupation == Occupation.Lord);
		if (characterObject == null)
		{
			return "Can't find a proper lord template.\n" + text;
		}
		Settlement bornSettlement = obj.Settlements.GetRandomElement() ?? Settlement.All.FirstOrDefault((Settlement x) => x.IsTown && x.Culture == culture);
		Hero hero = HeroCreator.CreateSpecialHero(characterObject, bornSettlement, clan, null, MBRandom.RandomInt(18, 36));
		hero.HeroDeveloper.InitializeHeroDeveloper();
		hero.ChangeState(Hero.CharacterStates.Active);
		clan.SetLeader(hero);
		ChangeKingdomAction.ApplyByJoinToKingdom(clan, obj, default(CampaignTime), showNotification: false);
		EnterSettlementAction.ApplyForCharacterOnly(hero, settlement);
		GiveGoldAction.ApplyBetweenCharacters(null, hero, 15000);
		CampaignEventDispatcher.Instance.OnClanCreated(clan, isCompanion: false);
		return $"{clan.Name} is added to {obj.Name}. Its leader is: {hero.Name}";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("lead_kingdom", "campaign")]
	public static string LeadKingdom(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (Hero.MainHero.IsKingdomLeader)
		{
			return "You are already leading your faction";
		}
		if (Clan.PlayerClan.Kingdom == null)
		{
			return "You are not in a kingdom";
		}
		ChangeRulingClanAction.Apply(Clan.PlayerClan.Kingdom, Clan.PlayerClan);
		return "OK";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("join_kingdom", "campaign")]
	public static string JoinKingdom(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string result = "Format is \"campaign.join_kingdom[KingdomName / FirstTwoCharactersOfKingdomName]\".\nWrite \"campaign.join_kingdom help\" to list available Kingdoms.";
		if (CheckParameters(strings, 0))
		{
			return result;
		}
		string text = ConcatenateString(strings).Replace(" ", "");
		if (text.ToLower() == "help")
		{
			string text2 = "";
			text2 += "\n";
			text2 += "Format is \"campaign.join_kingdom [KingdomName/FirstTwoCharacterOfKingdomName]\".";
			text2 += "\n";
			text2 += "Available Kingdoms";
			text2 += "\n";
			{
				foreach (Kingdom item in Kingdom.All)
				{
					text2 = text2 + "Kingdom name " + item.Name.ToString() + "\n";
				}
				return text2;
			}
		}
		if (Hero.MainHero.IsKingdomLeader)
		{
			return "Cannot join a kingdom while leading a kingdom!";
		}
		if (TryGetObject(text, out Kingdom obj, out string errorMessage, (Func<Kingdom, bool>)null))
		{
			ChangeKingdomAction.ApplyByJoinToKingdom(Hero.MainHero.Clan, obj);
			return "Success";
		}
		return errorMessage;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("join_kingdom_as_mercenary", "campaign")]
	public static string JoinKingdomAsMercenary(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.join_kingdom_as_mercenary[KingdomName / FirstTwoCharactersOfKingdomName]\".\nWrite \"campaign.join_kingdom_as_mercenary help\" to list available Kingdoms.";
		if (CheckParameters(strings, 0))
		{
			return text;
		}
		string text2 = ConcatenateString(strings).Replace(" ", "");
		if (text2.ToLower() == "help")
		{
			string text3 = "";
			text3 += "\n";
			text3 += "Format is \"campaign.join_kingdom_as_mercenary [KingdomName/FirstTwoCharacterOfKingdomName]\".";
			text3 += "\n";
			text3 += "Available Kingdoms";
			text3 += "\n";
			{
				foreach (Kingdom item in Kingdom.All)
				{
					text3 = text3 + "Kingdom name " + item.Name.ToString() + "\n";
				}
				return text3;
			}
		}
		if (TryGetObject(text2, out Kingdom obj, out string errorMessage, (Func<Kingdom, bool>)null))
		{
			ChangeKingdomAction.ApplyByJoinFactionAsMercenary(Hero.MainHero.Clan, obj);
			return "Success";
		}
		return errorMessage + ": " + text2 + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("make_trade_agreement", "campaign")]
	public static string MakeTradeAgreement(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string result = "Format is campaign.make_trade_agreement [Kingdom1] | [Kingdom2]\".";
		if (CheckParameters(strings, 0) || CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return result;
		}
		List<string> separatedNames = GetSeparatedNames(strings, removeEmptySpaces: true);
		if (separatedNames.Count != 2)
		{
			return result;
		}
		_ = separatedNames[0];
		_ = separatedNames[1];
		TryGetObject(separatedNames[0], out Kingdom obj, out string errorMessage, (Func<Kingdom, bool>)null);
		TryGetObject(separatedNames[1], out Kingdom obj2, out errorMessage, (Func<Kingdom, bool>)null);
		if (obj == null || obj2 == null)
		{
			return "Cant find one of the kingdoms";
		}
		if (obj == obj2)
		{
			return "Can't declare between same factions";
		}
		if (!Campaign.Current.Models.TradeAgreementModel.CanMakeTradeAgreement(obj, obj2, checkOtherSideTradeSupport: true, out var reason, includeReason: true))
		{
			return reason.ToString();
		}
		if (obj.IsEliminated)
		{
			return "kingdom1 is eliminated";
		}
		if (obj2.IsEliminated)
		{
			return "kingdom2 is eliminated";
		}
		Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>()?.MakeTradeAgreement(obj, obj2, Campaign.Current.Models.TradeAgreementModel.GetTradeAgreementDurationInYears(obj, obj2));
		return string.Concat("Trade agreement signed between ", obj.Name, " and ", obj2.Name);
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("print_criminal_ratings", "campaign")]
	public static string PrintCriminalRatings(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (!CheckParameters(strings, 0) || CheckHelp(strings))
		{
			return "Format is \"campaign.print_criminal_ratings";
		}
		string text = "";
		bool flag = true;
		foreach (Kingdom item in Kingdom.All)
		{
			if (item.MainHeroCrimeRating > 0f)
			{
				text = string.Concat(text, item.Name, "   criminal rating: ", item.MainHeroCrimeRating, "\n");
				flag = false;
			}
		}
		text += "-----------\n";
		foreach (Clan nonBanditFaction in Clan.NonBanditFactions)
		{
			if (nonBanditFaction.MainHeroCrimeRating > 0f)
			{
				text = string.Concat(text, nonBanditFaction.Name, "   criminal rating: ", nonBanditFaction.MainHeroCrimeRating, "\n");
				flag = false;
			}
		}
		if (flag)
		{
			return "You don't have any criminal rating.";
		}
		return text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_player_age", "campaign")]
	public static string SetMainHeroAge(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.add_player_age [PositiveNumber]\".";
		if (CheckParameters(strings, 0) || CheckHelp(strings))
		{
			return text;
		}
		float age = Hero.MainHero.Age;
		int result = -1;
		if (!int.TryParse(strings[0], out result) || result < 0)
		{
			return "Please enter a positive number\n" + text;
		}
		age += (float)result;
		if (age > (float)Campaign.Current.Models.AgeModel.MaxAge)
		{
			return $"Age must be between {Campaign.Current.Models.AgeModel.HeroComesOfAge} - {Campaign.Current.Models.AgeModel.MaxAge}";
		}
		Hero.MainHero.SetBirthDay(HeroHelper.GetRandomBirthDayForAge(age));
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_main_party_attackable", "campaign")]
	public static string SetMainPartyAttackable(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is campaign.set_main_party_attackable [1/0]\".";
		if (CheckHelp(strings) || !CheckParameters(strings, 1))
		{
			return text;
		}
		if (strings[0] == "0" || strings[0] == "1")
		{
			bool flag = strings[0] == "1";
			if (flag)
			{
				MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Now);
			}
			else
			{
				MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
				foreach (MobileParty item in MobileParty.All)
				{
					if (item.TargetParty == MobileParty.MainParty && item.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
					{
						item.SetMoveModeHold();
					}
				}
			}
			return "Main party is" + (flag ? " " : " NOT ") + "attackable.";
		}
		return "Wrong input.\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_morale_to_party", "campaign")]
	public static string AddMoraleToParty(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.add_morale_to_party [HeroName] | [Number]\".";
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2 || CheckHelp(strings))
		{
			return text;
		}
		int result = 0;
		if (!int.TryParse(separatedNames[1], out result))
		{
			result = 100;
		}
		string text2 = separatedNames[0];
		if (text2.ToLower().Equals("all"))
		{
			foreach (MobileParty allLordParty in MobileParty.AllLordParties)
			{
				if (!allLordParty.IsMainParty)
				{
					float num = MBMath.ClampFloat(result, -10000f, 10000f);
					allLordParty.RecentEventsMorale += num;
				}
			}
			return "All lords parties morale is changed";
		}
		if (TryGetObject(text2, out var obj, out var errorMessage, (Hero x) => x.PartyBelongedTo != null))
		{
			MobileParty partyBelongedTo = obj.PartyBelongedTo;
			if (partyBelongedTo != null)
			{
				float num2 = MBMath.ClampFloat(result, -10000f, 10000f);
				partyBelongedTo.RecentEventsMorale += num2;
				return $"The base morale of {obj.Name}'s party changed by {num2}.";
			}
			return "Hero: " + text2 + " does not belonged to any party.\n" + text;
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("boost_cohesion_of_army", "campaign")]
	public static string BoostCohesionOfArmy(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"boost_cohesion_of_army [ArmyLeaderName]\".";
		if (CheckHelp(strings))
		{
			return text;
		}
		Hero obj = Hero.MainHero;
		string text2 = "";
		Army army = obj.PartyBelongedTo.Army;
		if (!CheckParameters(strings, 0))
		{
			text2 = ConcatenateString(strings.GetRange(0, strings.Count));
			TryGetObject(text2, out obj, out string errorMessage, (Func<Hero, bool>)null);
			if (obj == null)
			{
				return errorMessage + ".\n" + text;
			}
			if (obj.PartyBelongedTo == null)
			{
				return "Hero: " + text2 + " does not belong to any army.";
			}
			army = obj.PartyBelongedTo.Army;
			if (army == null)
			{
				return "Hero: " + text2 + " does not belong to any army.";
			}
		}
		if (army != null)
		{
			army.Cohesion = 100f;
			return $"{obj.Name}'s army cohesion is boosted.";
		}
		return "Wrong input.\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_focus_points_to_hero", "campaign")]
	public static string AddFocusPointCheat(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.add_focus_points_to_hero [HeroName] | [PositiveNumber]\".";
		if (CheckHelp(strings))
		{
			return text;
		}
		bool flag = false;
		int num = 1;
		Hero mainHero;
		if (CheckParameters(strings, 0))
		{
			Hero.MainHero.HeroDeveloper.UnspentFocusPoints = MBMath.ClampInt(Hero.MainHero.HeroDeveloper.UnspentFocusPoints + 1, 0, int.MaxValue);
			mainHero = Hero.MainHero;
			return $"{num} focus points added to the {mainHero.Name}. ";
		}
		int result = 0;
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count == 1)
		{
			bool flag2 = int.TryParse(separatedNames[0], out result);
			if (result <= 0 && flag2)
			{
				return "Please enter a positive number\n" + text;
			}
			Hero.MainHero.HeroDeveloper.UnspentFocusPoints = MBMath.ClampInt(Hero.MainHero.HeroDeveloper.UnspentFocusPoints + result, 0, 10000);
			mainHero = Hero.MainHero;
			flag = true;
			num = result;
		}
		else
		{
			if (separatedNames.Count != 2)
			{
				return text;
			}
			if (int.TryParse(separatedNames[1], out result))
			{
				if (!TryGetObject(separatedNames[0], out mainHero, out string errorMessage, (Func<Hero, bool>)null))
				{
					return errorMessage;
				}
				mainHero.HeroDeveloper.UnspentFocusPoints = MBMath.ClampInt(mainHero.HeroDeveloper.UnspentFocusPoints + result, 0, 10000);
				flag = true;
				num = result;
			}
			else
			{
				if (!TryGetObject(separatedNames[0], out mainHero, out string errorMessage2, (Func<Hero, bool>)null))
				{
					return errorMessage2;
				}
				mainHero.HeroDeveloper.UnspentFocusPoints = MBMath.ClampInt(mainHero.HeroDeveloper.UnspentFocusPoints + 1, 0, 10000);
				flag = true;
			}
		}
		if (flag)
		{
			return $"{num} focus points added to the {mainHero.Name}. ";
		}
		return "Check parameters \n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_attribute_points_to_hero", "campaign")]
	public static string AddAttributePointsCheat(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.add_attribute_points_to_hero [HeroName] | [PositiveNumber]\".";
		if (CheckHelp(strings))
		{
			return text;
		}
		bool flag = false;
		int num = 1;
		Hero mainHero;
		if (CheckParameters(strings, 0))
		{
			Hero.MainHero.HeroDeveloper.UnspentAttributePoints = MBMath.ClampInt(Hero.MainHero.HeroDeveloper.UnspentAttributePoints + 1, 0, 10000);
			mainHero = Hero.MainHero;
			return $"{num} attribute points added to the {mainHero.Name}. ";
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count == 1)
		{
			int result;
			bool flag2 = int.TryParse(separatedNames[0], out result);
			if (result <= 0 || !flag2)
			{
				return "Please enter a positive number\n" + text;
			}
			Hero.MainHero.HeroDeveloper.UnspentAttributePoints = MBMath.ClampInt(Hero.MainHero.HeroDeveloper.UnspentAttributePoints + result, 0, 10000);
			mainHero = Hero.MainHero;
			flag = true;
			num = result;
		}
		else
		{
			if (separatedNames.Count != 2)
			{
				return text;
			}
			if (int.TryParse(separatedNames[1], out var result2))
			{
				if (!TryGetObject(separatedNames[0], out mainHero, out string errorMessage, (Func<Hero, bool>)null))
				{
					return errorMessage;
				}
				mainHero.HeroDeveloper.UnspentAttributePoints = MBMath.ClampInt(mainHero.HeroDeveloper.UnspentAttributePoints + result2, 0, 10000);
				flag = true;
				num = result2;
			}
			else
			{
				if (!TryGetObject(separatedNames[0], out mainHero, out string errorMessage2, (Func<Hero, bool>)null))
				{
					return errorMessage2;
				}
				mainHero.HeroDeveloper.UnspentAttributePoints = MBMath.ClampInt(mainHero.HeroDeveloper.UnspentAttributePoints + 1, 0, 10000);
				flag = true;
			}
		}
		if (flag)
		{
			return $"{num} attribute points added to the {mainHero.Name}. ";
		}
		return "Check parameters \n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("print_tournaments", "campaign")]
	public static string PrintSettlementsWithTournament(List<string> strings)
	{
		string ErrorType = "";
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (!Campaign.Current.IsDay)
		{
			return "Cant print tournaments. Wait day light.";
		}
		string text = "";
		foreach (Town allTown in Town.AllTowns)
		{
			if (Campaign.Current.TournamentManager.GetTournamentGame(allTown) != null)
			{
				text = string.Concat(text, allTown.Name, "\n");
			}
		}
		return text;
	}

	public static string ConvertListToMultiLine(List<string> strings)
	{
		string text = "";
		foreach (string @string in strings)
		{
			text = text + @string + "\n";
		}
		return text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("print_all_issues", "campaign")]
	public static string PrintAllIssues(List<string> strings)
	{
		string ErrorType = "";
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Total issue count : " + Campaign.Current.IssueManager.Issues.Count + "\n";
		int num = 0;
		foreach (KeyValuePair<Hero, IssueBase> issue in Campaign.Current.IssueManager.Issues)
		{
			text = string.Concat(text, ++num, ") ", issue.Value.Title, ", ", issue.Key, ": ", issue.Value.IssueSettlement, "\n");
		}
		return text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("give_workshop_to_player", "campaign")]
	public static string GiveWorkshopToPlayer(List<string> strings)
	{
		string result = "Format is \"campaign.give_workshop_to_player [SettlementName] | [workshop_name]\".";
		List<string> separatedNames = GetSeparatedNames(strings);
		if (!CheckCheatUsage(ref ErrorType) || CheckHelp(strings) || !CheckParameters(separatedNames, 2))
		{
			return result;
		}
		if (TryGetObject(separatedNames[0], out Settlement obj, out string errorMessage, (Func<Settlement, bool>)null))
		{
			if (obj.IsTown)
			{
				if (TryGetObject(separatedNames[1], out WorkshopType obj2, out errorMessage, (Func<WorkshopType, bool>)null))
				{
					Workshop[] workshops = obj.Town.Workshops;
					foreach (Workshop workshop in workshops)
					{
						if (workshop.WorkshopType == obj2 && workshop.Owner != Hero.MainHero)
						{
							int costForPlayer = Campaign.Current.Models.WorkshopModel.GetCostForPlayer(workshop);
							GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, costForPlayer);
							ChangeOwnerOfWorkshopAction.ApplyByPlayerBuying(workshop);
							return $"Gave {workshop.WorkshopType.Name} to {Hero.MainHero.Name}";
						}
					}
					return "There is no suitable workshop to give player in requested settlement";
				}
				return errorMessage;
			}
			return "Settlement should be town\n";
		}
		return errorMessage;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("conceive_child", "campaign")]
	public static string MakePregnant(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (Hero.MainHero.Spouse == null)
		{
			if (Game.Current.IsDevelopmentMode)
			{
				Hero hero = Hero.AllAliveHeroes.FirstOrDefault((Hero t) => t != Hero.MainHero && Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(Hero.MainHero, t));
				if (hero != null)
				{
					MarriageAction.Apply(Hero.MainHero, hero);
					if (Hero.MainHero.IsFemale ? (!Hero.MainHero.IsPregnant) : (!Hero.MainHero.Spouse.IsPregnant))
					{
						MakePregnantAction.Apply(Hero.MainHero.IsFemale ? Hero.MainHero : Hero.MainHero.Spouse);
						return "Success";
					}
					return "You are expecting a child already.";
				}
				return "error";
			}
			return "You need to be married to have a child, use \"campaign.marry_player_with_hero [HeroName]\" cheat to marry.";
		}
		if (Hero.MainHero.IsFemale ? (!Hero.MainHero.IsPregnant) : (!Hero.MainHero.Spouse.IsPregnant))
		{
			MakePregnantAction.Apply(Hero.MainHero.IsFemale ? Hero.MainHero : Hero.MainHero.Spouse);
			return "Success";
		}
		return "You are expecting a child already.";
	}

	public static Hero GenerateChild(Hero hero, bool isFemale, CultureObject culture)
	{
		if (hero.Spouse == null)
		{
			List<Hero> list = Hero.AllAliveHeroes.ToList();
			list.Shuffle();
			Hero hero2 = list.FirstOrDefault((Hero t) => t != hero && Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(hero, t));
			if (hero2 != null)
			{
				MarriageAction.Apply(hero, hero2);
				if (hero.IsFemale ? (!hero.IsPregnant) : (!hero.Spouse.IsPregnant))
				{
					MakePregnantAction.Apply(hero.IsFemale ? hero : hero.Spouse);
				}
			}
		}
		Hero hero3 = (hero.IsFemale ? hero : hero.Spouse);
		Hero spouse = hero3.Spouse;
		Hero hero4 = HeroCreator.DeliverOffSpring(hero3, spouse, isFemale);
		hero4.Culture = culture;
		hero3.IsPregnant = false;
		return hero4;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_prisoner_to_party", "campaign")]
	public static string AddPrisonerToParty(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.add_prisoner_to_party [PrisonerName] | [CapturerName]\".";
		if (CheckHelp(strings) || CheckParameters(strings, 0) || CheckParameters(strings, 1))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings);
		if (separatedNames.Count != 2)
		{
			return text;
		}
		string requestedId = separatedNames[0].Trim();
		string requestedId2 = separatedNames[1].Trim();
		TryGetObject(requestedId, out Hero obj, out string errorMessage, (Func<Hero, bool>)null);
		TryGetObject(requestedId2, out Hero obj2, out string errorMessage2, (Func<Hero, bool>)null);
		if (obj == null)
		{
			return errorMessage + "\n" + text;
		}
		if (obj2 == null)
		{
			return errorMessage2 + "\n" + text;
		}
		if (!obj2.IsActive || obj2.PartyBelongedTo == null)
		{
			return "Capturer hero is not active!";
		}
		if (!obj.IsActive || obj.IsHumanPlayerCharacter || (obj.Occupation != Occupation.Lord && obj.Occupation != Occupation.Wanderer))
		{
			return "Hero can't be taken as a prisoner!";
		}
		if (!FactionManager.IsAtWarAgainstFaction(obj.MapFaction, obj2.MapFaction))
		{
			return "Factions are not at war!";
		}
		if (obj.PartyBelongedTo != null)
		{
			if (obj.PartyBelongedTo.MapEvent != null)
			{
				return "prisoners party shouldn't be in a map event.";
			}
			if (obj.PartyBelongedTo.LeaderHero == obj)
			{
				DestroyPartyAction.Apply(null, obj.PartyBelongedTo);
			}
			else
			{
				obj.PartyBelongedTo.MemberRoster.RemoveTroop(obj.CharacterObject);
			}
		}
		if (obj.IsPrisoner)
		{
			EndCaptivityAction.ApplyByEscape(obj);
		}
		if (obj.CurrentSettlement != null)
		{
			LeaveSettlementAction.ApplyForCharacterOnly(obj);
		}
		if (obj2.IsHumanPlayerCharacter)
		{
			obj.SetHasMet();
		}
		TakePrisonerAction.Apply(obj2.PartyBelongedTo.Party, obj);
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("clear_settlement_defense", "campaign")]
	public static string ClearSettlementDefense(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.clear_settlement_defense [SettlementName]\".";
		if (CheckParameters(strings, 0) || CheckHelp(strings))
		{
			return text;
		}
		if (TryGetObject(ConcatenateString(strings.GetRange(0, strings.Count)), out Settlement obj, out string errorMessage, (Func<Settlement, bool>)null))
		{
			obj.Militia = 0f;
			MobileParty mobileParty = (obj.IsFortification ? obj.Town.GarrisonParty : null);
			if (mobileParty != null)
			{
				DestroyPartyAction.Apply(null, mobileParty);
			}
			return "Success";
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_xp_to_player_party_prisoners", "campaign")]
	public static string AddPrisonersXp(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.add_xp_to_player_party_prisoners [Amount]\".";
		if (!CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		int result = 1;
		if (!int.TryParse(strings[0], out result) || result < 1)
		{
			return "Please enter a positive number\n" + text;
		}
		for (int i = 0; i < MobileParty.MainParty.PrisonRoster.Count; i++)
		{
			MobileParty.MainParty.PrisonRoster.SetElementXp(i, MobileParty.MainParty.PrisonRoster.GetElementXp(i) + result);
			InformationManager.DisplayMessage(new InformationMessage("[DEBUG] " + result + " xp given to " + MobileParty.MainParty.PrisonRoster.GetElementCopyAtIndex(i).Character.Name));
		}
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_hero_trait", "campaign")]
	public static string SetHeroTrait(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.set_hero_trait [HeroName] | [Trait]  | [Value]\".";
		if (CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings, removeEmptySpaces: true);
		if (separatedNames.Count < 3)
		{
			return text;
		}
		if (!int.TryParse(separatedNames[2], out var _))
		{
			return "Please enter a number\n" + text;
		}
		if (TryGetObject(separatedNames[0], out Hero obj, out string errorMessage, (Func<Hero, bool>)null))
		{
			if (int.TryParse(separatedNames[2], out var result2))
			{
				if (TryGetObject(separatedNames[1], out TraitObject obj2, out string errorMessage2, (Func<TraitObject, bool>)null))
				{
					int traitLevel = obj.GetTraitLevel(obj2);
					if (result2 >= obj2.MinValue && result2 <= obj2.MaxValue)
					{
						obj.SetTraitLevel(obj2, result2);
						if (obj == Hero.MainHero)
						{
							TraitLevelingHelper.UpdateTraitXPAccordingToTraitLevels();
							CampaignEventDispatcher.Instance.OnPlayerTraitChanged(obj2, traitLevel);
						}
						TraitLevelingHelper.UpdateTraitXPAccordingToTraitLevels();
						CampaignEventDispatcher.Instance.OnPlayerTraitChanged(obj2, traitLevel);
						return $"{separatedNames[0]} 's {obj2.Name} trait has been set to {result2}.";
					}
					return $"Number must be between {obj2.MinValue} and {obj2.MaxValue}.";
				}
				return errorMessage2 + "\n" + text;
			}
			return "Trait not found.\n" + text;
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("remove_militias_from_settlement", "campaign")]
	public static string RemoveMilitiasFromSettlement(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (CheckParameters(strings, 0) || CheckHelp(strings))
		{
			return "Format is \"campaign.remove_militias_from_settlement [SettlementName]\".";
		}
		string text = ConcatenateString(strings);
		if (TryGetObject(text, out Settlement settlement, out string errorMessage, (Func<Settlement, bool>)null))
		{
			if (settlement.Party.MapEvent != null)
			{
				return "Settlement, " + text + " is in a MapEvent, try later to remove them";
			}
			List<MobileParty> list = new List<MobileParty>();
			foreach (MobileParty item in MobileParty.All.Where((MobileParty x) => x.IsMilitia && x.CurrentSettlement == settlement))
			{
				if (item.MapEvent != null)
				{
					return "Militia in " + text + " are in a MapEvent, try later to remove them";
				}
				list.Add(item);
			}
			foreach (MobileParty item2 in list)
			{
				item2.RemoveParty();
			}
			return "Success";
		}
		return errorMessage + ": " + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("cancel_quest", "campaign")]
	public static string CancelQuestCheat(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.cancel_quest [quest name]\".";
		if (CheckHelp(strings))
		{
			return text;
		}
		List<string> separatedNames = GetSeparatedNames(strings, removeEmptySpaces: true);
		if (separatedNames.Count == 1)
		{
			string text2 = separatedNames[0].ToLower();
			if (text2.IsEmpty())
			{
				return text;
			}
			QuestBase questBase = null;
			int num = 0;
			foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
			{
				if (text2.Equals(quest.Title.ToString().Replace(" ", "").ToLower(), StringComparison.OrdinalIgnoreCase))
				{
					num++;
					if (num == 1)
					{
						questBase = quest;
					}
				}
			}
			if (questBase == null)
			{
				return "Quest not found.\n" + text;
			}
			if (num > 1)
			{
				return "There are more than one quest with the name: " + text2;
			}
			if (questBase.IsSpecialQuest)
			{
				return "Quest can not be special quest";
			}
			questBase.CompleteQuestWithCancel(new TextObject("{=!}Quest is canceled by cheat."));
			return "Success";
		}
		return "Given parameters is not correct";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("kick_companion", "campaign")]
	public static string KickCompanionFromParty(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		bool isDevelopmentMode = Game.Current.IsDevelopmentMode;
		string text = (isDevelopmentMode ? "Format is \"campaign.kick_companion [CompanionName] or [all](kicks all companions) or [noargument](kicks first companion if any) \"." : "Format is \"campaign.kick_companion [CompanionName] or [noargument](kicks first companion if any) \".");
		if (CheckHelp(strings))
		{
			return text;
		}
		IEnumerable<TroopRosterElement> enumerable = from h in MobileParty.MainParty.MemberRoster.GetTroopRoster()
			where h.Character != null && h.Character.IsHero && h.Character.HeroObject.IsWanderer
			select h;
		if (enumerable.IsEmpty())
		{
			return "There are no companions in your party.";
		}
		string text2 = ConcatenateString(strings).Replace(" ", "");
		if (strings.IsEmpty())
		{
			RemoveCompanionAction.ApplyByFire(Clan.PlayerClan, enumerable.First().Character.HeroObject);
			return "Success";
		}
		if (isDevelopmentMode && string.Equals(text2, "all", StringComparison.OrdinalIgnoreCase))
		{
			foreach (TroopRosterElement item in enumerable)
			{
				RemoveCompanionAction.ApplyByFire(Clan.PlayerClan, item.Character.HeroObject);
			}
			return "Success";
		}
		if (TryGetObject(text2, out Hero obj, out string errorMessage, (Func<Hero, bool>)null))
		{
			RemoveCompanionAction.ApplyByFire(Clan.PlayerClan, obj);
			return "Success";
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_xp_to_player_party_troops", "campaign")]
	public static string AddTroopsXp(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is \"campaign.add_xp_to_player_party_troops [Amount]\".";
		if (!CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return text;
		}
		int result = 1;
		if (!int.TryParse(strings[0], out result) || result < 1)
		{
			return "Please enter a positive number\n" + text;
		}
		if (!IsValueAcceptable(result))
		{
			return "The value is too much";
		}
		for (int i = 0; i < MobileParty.MainParty.MemberRoster.Count; i++)
		{
			MobileParty.MainParty.MemberRoster.SetElementXp(i, MobileParty.MainParty.MemberRoster.GetElementXp(i) + result);
			InformationManager.DisplayMessage(new InformationMessage("[DEBUG] " + result + " xp given to " + MobileParty.MainParty.MemberRoster.GetElementCopyAtIndex(i).Character.Name));
		}
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("print_gameplay_statistics", "campaign")]
	public static string PrintGameplayStatistics(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (CheckHelp(strings))
		{
			return "Format is \"statistics.print_gameplay_statistics\".";
		}
		IStatisticsCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IStatisticsCampaignBehavior>();
		if (campaignBehavior == null)
		{
			return "Can not find IStatistics Campaign Behavior!";
		}
		string text = "";
		text += "---------------------------GENERAL---------------------------\n";
		text = text + "Played Time in Campaign Time(Days): " + campaignBehavior.GetTotalTimePlayed().ToDays + "\n";
		text = text + "Played Time in Real Time: " + campaignBehavior.GetTotalTimePlayedInSeconds() + "\n";
		text = text + "Number of children born: " + campaignBehavior.GetNumberOfChildrenBorn() + "\n";
		text = text + "Total influence earned: " + campaignBehavior.GetTotalInfluenceEarned() + "\n";
		text = text + "Number of issues solved: " + campaignBehavior.GetNumberOfIssuesSolved() + "\n";
		text = text + "Number of tournaments won: " + campaignBehavior.GetNumberOfTournamentWins() + "\n";
		text = text + "Best tournament rank: " + campaignBehavior.GetHighestTournamentRank() + "\n";
		text = text + "Number of prisoners recruited: " + campaignBehavior.GetNumberOfPrisonersRecruited() + "\n";
		text = text + "Number of troops recruited: " + campaignBehavior.GetNumberOfTroopsRecruited() + "\n";
		text = text + "Number of enemy clans defected: " + campaignBehavior.GetNumberOfClansDefected() + "\n";
		text = text + "Total crime rating gained: " + campaignBehavior.GetTotalCrimeRatingGained() + "\n";
		text += "---------------------------BATTLE---------------------------\n";
		text = text + "Battles Won / Lost: " + campaignBehavior.GetNumberOfBattlesWon() + " / " + campaignBehavior.GetNumberOfBattlesLost() + "\n";
		text = text + "Largest battle won as the leader: " + campaignBehavior.GetLargestBattleWonAsLeader() + "\n";
		text = text + "Largest army formed by the player: " + campaignBehavior.GetLargestArmyFormedByPlayer() + "\n";
		text = text + "Number of enemy clans destroyed: " + campaignBehavior.GetNumberOfEnemyClansDestroyed() + "\n";
		text = text + "Heroes killed in battle: " + campaignBehavior.GetNumberOfHeroesKilledInBattle() + "\n";
		text = text + "Troops killed or knocked out in person: " + campaignBehavior.GetNumberOfTroopsKnockedOrKilledByPlayer() + "\n";
		text = text + "Troops killed or knocked out by player party: " + campaignBehavior.GetNumberOfTroopsKnockedOrKilledAsParty() + "\n";
		text = text + "Number of hero prisoners taken: " + campaignBehavior.GetNumberOfHeroPrisonersTaken() + "\n";
		text = text + "Number of troop prisoners taken: " + campaignBehavior.GetNumberOfTroopPrisonersTaken() + "\n";
		text = text + "Number of captured towns: " + campaignBehavior.GetNumberOfTownsCaptured() + "\n";
		text = text + "Number of captured castles: " + campaignBehavior.GetNumberOfCastlesCaptured() + "\n";
		text = text + "Number of cleared hideouts: " + campaignBehavior.GetNumberOfHideoutsCleared() + "\n";
		text = text + "Number of raided villages: " + campaignBehavior.GetNumberOfVillagesRaided() + "\n";
		text = text + "Number of days spent as prisoner: " + campaignBehavior.GetTimeSpentAsPrisoner().ToDays + "\n";
		text += "---------------------------FINANCES---------------------------\n";
		text = text + "Total denars earned: " + campaignBehavior.GetTotalDenarsEarned() + "\n";
		text = text + "Total denars earned from caravans: " + campaignBehavior.GetDenarsEarnedFromCaravans() + "\n";
		text = text + "Total denars earned from workshops: " + campaignBehavior.GetDenarsEarnedFromWorkshops() + "\n";
		text = text + "Total denars earned from ransoms: " + campaignBehavior.GetDenarsEarnedFromRansoms() + "\n";
		text = text + "Total denars earned from taxes: " + campaignBehavior.GetDenarsEarnedFromTaxes() + "\n";
		text = text + "Total denars earned from tributes: " + campaignBehavior.GetDenarsEarnedFromTributes() + "\n";
		text = text + "Total denars paid in tributes: " + campaignBehavior.GetDenarsPaidAsTributes() + "\n";
		text += "---------------------------CRAFTING---------------------------\n";
		text = text + "Number of weapons crafted: " + campaignBehavior.GetNumberOfWeaponsCrafted() + "\n";
		text = text + "Most expensive weapon crafted: " + campaignBehavior.GetMostExpensiveItemCrafted().Item1 + " - " + campaignBehavior.GetMostExpensiveItemCrafted().Item2 + "\n";
		text = text + "Numbere of crafting parts unlocked: " + campaignBehavior.GetNumberOfCraftingPartsUnlocked() + "\n";
		text = text + "Number of crafting orders completed: " + campaignBehavior.GetNumberOfCraftingOrdersCompleted() + "\n";
		text += "---------------------------COMPANIONS---------------------------\n";
		text = text + "Number of hired companions: " + campaignBehavior.GetNumberOfCompanionsHired() + "\n";
		text = text + "Companion with most issues solved: " + campaignBehavior.GetCompanionWithMostIssuesSolved().name + " - " + campaignBehavior.GetCompanionWithMostIssuesSolved().value + "\n";
		return text + "Companion with most kills: " + campaignBehavior.GetCompanionWithMostKills().name + " - " + campaignBehavior.GetCompanionWithMostKills().value + "\n";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_parties_visible", "campaign")]
	public static string SetAllArmiesAndPartiesVisible(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (!CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return "Format is \"campaign.set_parties_visible [1/0]\".";
		}
		Campaign.Current.TrueSight = strings[0] == "1";
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("print_strength_of_lord_parties", "campaign")]
	public static string PrintStrengthOfLordParties(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (MobileParty allLordParty in MobileParty.AllLordParties)
		{
			stringBuilder.AppendLine(string.Concat(allLordParty.Name, " strength: ", allLordParty.Party.CalculateCurrentStrength()));
		}
		stringBuilder.AppendLine("Success");
		return stringBuilder.ToString();
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("toggle_information_restrictions", "campaign")]
	public static string ToggleInformationRestrictions(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (CheckHelp(strings))
		{
			return "Format is \"campaign.toggle_information_restrictions\".";
		}
		if (!(Campaign.Current.Models.InformationRestrictionModel is DefaultInformationRestrictionModel defaultInformationRestrictionModel))
		{
			return "DefaultInformationRestrictionModel is missing.";
		}
		defaultInformationRestrictionModel.IsDisabledByCheat = !defaultInformationRestrictionModel.IsDisabledByCheat;
		return "Information restrictions are " + (defaultInformationRestrictionModel.IsDisabledByCheat ? "disabled" : "enabled") + ".";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("print_strength_of_factions", "campaign")]
	public static string PrintStrengthOfFactions(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Clan item in Clan.All)
		{
			stringBuilder.AppendLine(string.Concat(item.Name, " strength: ", item.CurrentTotalStrength));
		}
		stringBuilder.AppendLine("Success");
		return stringBuilder.ToString();
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_supporters_for_main_hero", "campaign")]
	public static string AddSupportersForMainHero(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Usage : campaign.add_supporters_for_main_hero [Number]";
		if (CheckHelp(strings) || !CheckParameters(strings, 1))
		{
			return string.Concat("" + "Usage : campaign.add_supporters_for_main_hero [Number]", "\n");
		}
		string text2 = "";
		if (int.TryParse(strings[0], out var result) && result > 0)
		{
			for (int i = 0; i < result; i++)
			{
				Hero randomElementWithPredicate = Hero.AllAliveHeroes.GetRandomElementWithPredicate((Hero x) => !x.IsChild && x.SupporterOf != Clan.PlayerClan && x.IsNotable);
				if (randomElementWithPredicate != null)
				{
					randomElementWithPredicate.SupporterOf = Clan.PlayerClan;
					text2 = text2 + "supporter added: " + randomElementWithPredicate.Name.ToString() + "\n";
				}
			}
			return text2 + "\nSuccess";
		}
		return "Please enter a positive number\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_campaign_speed_multiplier", "campaign")]
	public static string SetCampaignSpeed(List<string> strings)
	{
		string result = "Format is \"campaign.set_campaign_speed_multiplier  [positive speedUp multiplier]\".";
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (!CheckParameters(strings, 1) || CheckHelp(strings))
		{
			return result;
		}
		float num = (Game.Current.IsDevelopmentMode ? 30f : 15f);
		if (float.TryParse(strings[0], out var result2) && result2 > 0f)
		{
			if (result2 <= num)
			{
				Campaign.Current.SpeedUpMultiplier = result2;
				return "Success";
			}
			Campaign.Current.SpeedUpMultiplier = num;
			return "Campaign speed is set to " + num + ". which is the maximum value for speed up multiplier!";
		}
		return result;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("show_hideouts", "campaign")]
	public static string ShowHideouts(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (Game.Current.IsDevelopmentMode)
		{
			if (!CheckParameters(strings, 1) || CheckHelp(strings) || !int.TryParse(strings[0], out var result) || (result != 1 && result != 2))
			{
				return "Format is \"campaign.show_hideouts [1/2]\n 1: Show infested hideouts\n2: Show all hideouts\".";
			}
			foreach (Settlement item in Settlement.All)
			{
				if (item.IsHideout && (result != 1 || item.Hideout.IsInfested))
				{
					Hideout hideout = item.Hideout;
					hideout.IsSpotted = true;
					hideout.Owner.Settlement.IsVisible = true;
				}
			}
			return ((result == 1) ? "Infested" : "All") + " hideouts is visible now.";
		}
		if (!CheckParameters(strings, 0) || CheckHelp(strings))
		{
			return "Format is \"campaign.show_hideouts";
		}
		foreach (Settlement item2 in Settlement.All)
		{
			if (item2.IsHideout && !item2.Hideout.IsInfested)
			{
				Hideout hideout2 = item2.Hideout;
				hideout2.IsSpotted = true;
				hideout2.Owner.Settlement.IsVisible = true;
			}
		}
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("hide_hideouts", "campaign")]
	public static string HideHideouts(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		foreach (Settlement item in Settlement.All)
		{
			if (item.IsHideout)
			{
				Hideout hideout = item.Hideout;
				hideout.IsSpotted = false;
				hideout.Owner.Settlement.IsVisible = false;
			}
		}
		return "All hideouts should be invisible now.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("unlock_all_crafting_pieces", "campaign")]
	public static string UnlockCraftingPieces(List<string> strings)
	{
		string ErrorType = "";
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (!CheckParameters(strings, 0) || CheckHelp(strings))
		{
			return "Format is \"campaign.unlock_all_crafting_pieces\".";
		}
		CraftingCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<CraftingCampaignBehavior>();
		if (campaignBehavior == null)
		{
			return "Can not find Crafting Campaign Behavior!";
		}
		Type typeFromHandle = typeof(CraftingCampaignBehavior);
		FieldInfo field = typeFromHandle.GetField("_openedPartsDictionary", BindingFlags.Instance | BindingFlags.NonPublic);
		FieldInfo field2 = typeFromHandle.GetField("_openNewPartXpDictionary", BindingFlags.Instance | BindingFlags.NonPublic);
		Dictionary<CraftingTemplate, List<CraftingPiece>> dictionary = (Dictionary<CraftingTemplate, List<CraftingPiece>>)field.GetValue(campaignBehavior);
		Dictionary<CraftingTemplate, float> dictionary2 = (Dictionary<CraftingTemplate, float>)field2.GetValue(campaignBehavior);
		MethodInfo method = typeFromHandle.GetMethod("OpenPart", BindingFlags.Instance | BindingFlags.NonPublic);
		foreach (CraftingTemplate item in CraftingTemplate.All)
		{
			if (!dictionary.ContainsKey(item))
			{
				dictionary.Add(item, new List<CraftingPiece>());
			}
			if (!dictionary2.ContainsKey(item))
			{
				dictionary2.Add(item, 0f);
			}
			foreach (CraftingPiece piece in item.Pieces)
			{
				object[] parameters = new object[3] { piece, item, false };
				method.Invoke(campaignBehavior, parameters);
			}
		}
		field.SetValue(campaignBehavior, dictionary);
		field2.SetValue(campaignBehavior, dictionary2);
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("rebellion_enabled", "campaign")]
	public static string SetRebellionEnabled(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		string text = "Format is campaign.rebellion_enabled [1/0]\".";
		if (CheckHelp(strings) || !CheckParameters(strings, 1))
		{
			return text;
		}
		if (strings[0] == "0" || strings[0] == "1")
		{
			RebellionsCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<RebellionsCampaignBehavior>();
			if (campaignBehavior != null)
			{
				FieldInfo field = typeof(RebellionsCampaignBehavior).GetField("_rebellionEnabled", BindingFlags.Instance | BindingFlags.NonPublic);
				field.SetValue(campaignBehavior, strings[0] == "1");
				return "Rebellion is" + (((bool)field.GetValue(campaignBehavior)) ? " enabled" : " disabled");
			}
			return "Rebellions Campaign behavior not found.";
		}
		return "Wrong input.\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_troops", "campaign")]
	public static string AddTroopsToParty(List<string> strings)
	{
		if (!CheckCheatUsage(ref ErrorType))
		{
			return ErrorType;
		}
		if (CheckParameters(strings, 0))
		{
			return "Write \"campaign.add_troops help\" for help";
		}
		string text = "Usage : \"campaign.add_troops [TroopId] | [Number] | [PartyName]\". Party name is optional.";
		List<string> separatedNames = GetSeparatedNames(strings);
		if (CheckHelp(strings) || separatedNames.Count < 2)
		{
			string text2 = "";
			text2 += text;
			text2 += "\n";
			text2 += "\n";
			text2 += "Available troops";
			text2 += "\n";
			text2 += "==============================";
			text2 += "\n";
			{
				foreach (CharacterObject objectType in MBObjectManager.Instance.GetObjectTypeList<CharacterObject>())
				{
					if (objectType.Occupation == Occupation.Soldier || objectType.Occupation == Occupation.Gangster)
					{
						text2 = string.Concat(text2, "Id: ", objectType.StringId, " Name: ", objectType.Name, "\n");
					}
				}
				return text2;
			}
		}
		TryGetObject(separatedNames[0], out CharacterObject obj, out string errorMessage, (Func<CharacterObject, bool>)null);
		if (obj == null)
		{
			return errorMessage + "\n" + text;
		}
		if (obj.Occupation != Occupation.Soldier && obj.Occupation != Occupation.Gangster)
		{
			return "Troop occupation should be Soldier or Gangster to add party";
		}
		if (!int.TryParse(separatedNames[1], out var result) || result < 1)
		{
			return "Please enter a positive number\n" + text;
		}
		MobileParty obj2 = PartyBase.MainParty.MobileParty;
		if (separatedNames.Count == 3)
		{
			TryGetObject(separatedNames[2], out obj2, out string _, (Func<MobileParty, bool>)null);
			if (obj2 == null)
			{
				return "Given party with the parameter: " + separatedNames[2] + "  not found";
			}
		}
		if (obj2.MapEvent != null)
		{
			return "Party shouldn't be in a map event.";
		}
		if (!IsValueAcceptable(result))
		{
			return "The value is too much";
		}
		typeof(DefaultPartySizeLimitModel).GetField("_addAdditionalPartySizeAsCheat", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, true);
		obj2.AddElementToMemberRoster(obj, result);
		return string.Concat(obj2.Name.ToString(), " gained ", result, " of ", obj.Name, ".");
	}

	public static bool TryGetObject<T>(string requestedId, out T obj, out string errorMessage, Func<T, bool> predicate = null) where T : MBObjectBase
	{
		Func<string, string, CheatTextControl, bool> func = delegate(string requestedIdLocal, string idToCompare, CheatTextControl cheatTextFlag)
		{
			if (cheatTextFlag.HasAnyFlag(CheatTextControl.RemoveEmptySpace))
			{
				requestedIdLocal = requestedIdLocal.Replace(" ", "");
				idToCompare = idToCompare.Replace(" ", "");
			}
			if (cheatTextFlag.HasAnyFlag(CheatTextControl.IgnoreCase))
			{
				requestedIdLocal = requestedIdLocal.ToLower();
				idToCompare = idToCompare.ToLower();
			}
			return idToCompare.Equals(requestedIdLocal) || (cheatTextFlag.HasAnyFlag(CheatTextControl.ContainId) && idToCompare.Contains(requestedIdLocal));
		};
		obj = null;
		errorMessage = string.Empty;
		if (!string.IsNullOrEmpty(requestedId))
		{
			MBReadOnlyList<T> mBReadOnlyList = Campaign.Current.CampaignObjectManager.FindAll((T x) => predicate == null || predicate(x));
			MBList<T> mBList = new MBList<T>();
			if (mBReadOnlyList == null || mBReadOnlyList.Count == 0)
			{
				mBReadOnlyList = MBObjectManager.Instance.GetObjects((T x) => predicate == null || predicate(x));
			}
			if (mBReadOnlyList == null || mBReadOnlyList.Count == 0)
			{
				if (typeof(T) == typeof(PerkObject))
				{
					mBReadOnlyList = PerkObject.All as MBReadOnlyList<T>;
				}
				else if (typeof(T) == typeof(IssueBase))
				{
					mBReadOnlyList = TaleWorlds.Core.Extensions.DistinctBy(Campaign.Current.IssueManager.Issues.Select((KeyValuePair<Hero, IssueBase> x) => x.Value), (IssueBase x) => x.GetType().ToString()).ToMBList() as MBReadOnlyList<T>;
				}
			}
			if (mBReadOnlyList != null && mBReadOnlyList.Any())
			{
				for (int num = 0; num <= 7; num++)
				{
					for (int num2 = 0; num2 < mBReadOnlyList.Count; num2++)
					{
						if (func(requestedId, mBReadOnlyList[num2].StringId, (CheatTextControl)num))
						{
							obj = mBReadOnlyList[num2];
							return true;
						}
					}
					for (int num3 = 0; num3 < mBReadOnlyList.Count; num3++)
					{
						if (func(requestedId, mBReadOnlyList[num3].GetName().ToString(), (CheatTextControl)num) && !mBList.ContainsQ(mBReadOnlyList[num3]))
						{
							mBList.Add(mBReadOnlyList[num3]);
						}
					}
				}
			}
			if (mBList.Count == 1)
			{
				obj = mBList[0];
				return true;
			}
			if (mBList.Count == 0)
			{
				errorMessage = "Requested object could not found, check parameters";
			}
			else
			{
				errorMessage = "There is ambiguity with requested id, check parameters";
				errorMessage += "\nEnter id to select the object you want";
				for (int num4 = 0; num4 < mBList.Count; num4++)
				{
					T val = mBList[num4];
					errorMessage += $"\nObject {num4 + 1}: {val.GetName()}  Id:  {val.StringId}";
				}
			}
			return false;
		}
		errorMessage = "Requested Id can't be empty";
		return false;
	}

	private static Hero GetClanLeader(string clanName)
	{
		if (TryGetObject(clanName, out Clan obj, out string _, (Func<Clan, bool>)null))
		{
			return obj?.Leader;
		}
		return null;
	}

	private static ItemModifier GetItemModifier(string itemModifierName)
	{
		if (TryGetObject(itemModifierName, out ItemModifier obj, out string _, (Func<ItemModifier, bool>)null))
		{
			return obj;
		}
		return null;
	}

	public static bool IsPartySuitableToUseCheat(PartyBase party, bool ignoreMapEvents = false)
	{
		if ((party.MapEvent == null || ignoreMapEvents) && party.SiegeEvent == null && party.IsActive)
		{
			if (party.LeaderHero != null)
			{
				return party.LeaderHero.IsActive;
			}
			return true;
		}
		return false;
	}
}
