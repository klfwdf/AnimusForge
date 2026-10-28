using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace StoryMode;

public static class StoryModeData
{
	private static HashSet<string> _conspiracyTroops = new HashSet<string>
	{
		"conspiracy_commander_antiempire", "conspiracy_wilder", "conspiracy_warmonger", "conspiracy_berserker", "conspiracy_hellion", "conspiracy_guardsman", "conspiracy_guardian", "conspiracy_raider", "conspiracy_battlerider", "conspiracy_trained_bowman",
		"conspiracy_longbowman", "conspiracy_kern", "conspiracy_horse_archer", "conspiracy_mounted_master_archer", "conspiracy_trained_spearman", "conspiracy_spearmaster", "conspiracy_knight_trainee", "conspiracy_knight", "conspiracy_fighter", "conspiracy_veteran_fighter",
		"conspiracy_noble_horseman", "conspiracy_mounted_fighter", "conspiracy_trained_crossbowman", "conspiracy_warworn_crossbowman", "conspiracy_trained_huntsman", "conspiracy_hunt_leader", "conspiracy_mounted_huntsman", "conspiracy_packmaster", "anti_imperial_conspiracy_boss", "imperial_conspiracy_boss"
	};

	public static CampaignTime StorylineQuestHideoutHiddenDuration = CampaignTime.Hours(12f);

	private static Kingdom _northernEmpireKingdom;

	private static Kingdom _westernEmpireKingdom;

	private static Kingdom _southernEmpireKingdom;

	private static Kingdom _sturgiaKingdom;

	private static Kingdom _aseraiKingdom;

	private static Kingdom _vlandiaKingdom;

	private static Kingdom _battaniaKingdom;

	private static Kingdom _khuzaitKingdom;

	public static CultureObject ImperialCulture => NorthernEmpireKingdom.Culture;

	public static Kingdom NorthernEmpireKingdom
	{
		get
		{
			if (_northernEmpireKingdom != null)
			{
				return _northernEmpireKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "empire")
				{
					_northernEmpireKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "NorthernEmpireKingdom", 74);
			return null;
		}
	}

	public static Kingdom WesternEmpireKingdom
	{
		get
		{
			if (_westernEmpireKingdom != null)
			{
				return _westernEmpireKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "empire_w")
				{
					_westernEmpireKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "WesternEmpireKingdom", 99);
			return null;
		}
	}

	public static Kingdom SouthernEmpireKingdom
	{
		get
		{
			if (_southernEmpireKingdom != null)
			{
				return _southernEmpireKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "empire_s")
				{
					_southernEmpireKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "SouthernEmpireKingdom", 124);
			return null;
		}
	}

	public static Kingdom SturgiaKingdom
	{
		get
		{
			if (_sturgiaKingdom != null)
			{
				return _sturgiaKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "sturgia")
				{
					_sturgiaKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "SturgiaKingdom", 149);
			return null;
		}
	}

	public static Kingdom AseraiKingdom
	{
		get
		{
			if (_aseraiKingdom != null)
			{
				return _aseraiKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "aserai")
				{
					_aseraiKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "AseraiKingdom", 174);
			return null;
		}
	}

	public static Kingdom VlandiaKingdom
	{
		get
		{
			if (_vlandiaKingdom != null)
			{
				return _vlandiaKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "vlandia")
				{
					_vlandiaKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "VlandiaKingdom", 200);
			return null;
		}
	}

	public static Kingdom BattaniaKingdom
	{
		get
		{
			if (_battaniaKingdom != null)
			{
				return _battaniaKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "battania")
				{
					_battaniaKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "BattaniaKingdom", 227);
			return null;
		}
	}

	public static Kingdom KhuzaitKingdom
	{
		get
		{
			if (_khuzaitKingdom != null)
			{
				return _khuzaitKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "khuzait")
				{
					_khuzaitKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "KhuzaitKingdom", 253);
			return null;
		}
	}

	public static bool IsKingdomImperial(Kingdom kingdomToCheck)
	{
		if (kingdomToCheck != null)
		{
			return kingdomToCheck.Culture == ImperialCulture;
		}
		return false;
	}

	public static bool IsConspiracyTroop(CharacterObject troop)
	{
		return _conspiracyTroops.Contains(troop.StringId);
	}

	public static void OnGameEnd()
	{
		_northernEmpireKingdom = null;
		_westernEmpireKingdom = null;
		_southernEmpireKingdom = null;
		_sturgiaKingdom = null;
		_aseraiKingdom = null;
		_vlandiaKingdom = null;
		_battaniaKingdom = null;
		_khuzaitKingdom = null;
	}
}
