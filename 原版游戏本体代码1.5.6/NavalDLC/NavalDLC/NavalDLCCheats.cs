using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;

namespace NavalDLC;

public static class NavalDLCCheats
{
	public const string DLCNotActive = "DLC is not active.";

	public static bool CheckCheatUsage(ref string message)
	{
		if (!CampaignCheats.CheckCheatUsage(ref message))
		{
			return false;
		}
		ModuleInfo moduleInfo = ModuleHelper.GetModuleInfo("NavalDLC");
		if (moduleInfo == null || !moduleInfo.IsActive)
		{
			message = "DLC is not active.";
			return false;
		}
		return true;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("damage_player_ships", "naval")]
	public static string DamagePlayerShips(List<string> strings)
	{
		string message = string.Empty;
		if (!CheckCheatUsage(ref message))
		{
			return message;
		}
		MBReadOnlyList<Ship> mBReadOnlyList = MobileParty.MainParty?.Ships;
		if (mBReadOnlyList == null || mBReadOnlyList.Count == 0)
		{
			return "Player does not have any ships";
		}
		float result = 0.5f;
		if (strings.Count == 1 && float.TryParse(strings[0], out result) && (result.ApproximatelyEqualsTo(0f) || result < 0f))
		{
			result = 0.5f;
		}
		for (int num = mBReadOnlyList.Count - 1; num >= 0; num--)
		{
			Ship ship = mBReadOnlyList[num];
			ship.OnShipDamaged(ship.MaxHitPoints * result, null, out var _);
		}
		return "All ship hit points are reduced";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_ship_to_player", "naval")]
	public static string AddShipToPlayer(List<string> strings)
	{
		string message = string.Empty;
		if (!CheckCheatUsage(ref message))
		{
			return message;
		}
		string text = "Format is \"naval.add_ship_to_player [ShipName] | [Count] - (Empty = Random 1 Ship)\".";
		if (CampaignCheats.CheckHelp(strings))
		{
			return text;
		}
		if (!CampaignCheats.IsPartySuitableToUseCheat(PartyBase.MainParty))
		{
			return "Main party not suitable to take ship right now";
		}
		List<string> separatedNames = CampaignCheats.GetSeparatedNames(strings, removeEmptySpaces: true);
		MBList<ShipHull> shipHulls = Kingdom.All.SelectMany((Kingdom x) => x.Culture.AvailableShipHulls).ToMBList();
		string requestedId = string.Empty;
		int result = 1;
		ShipHull obj = null;
		if (separatedNames.Count == 0 || string.IsNullOrEmpty(separatedNames[0]))
		{
			obj = shipHulls.GetRandomElement();
		}
		else if (separatedNames.Count == 1)
		{
			requestedId = separatedNames[0];
		}
		else
		{
			if (separatedNames.Count != 2)
			{
				return text;
			}
			requestedId = separatedNames[0];
			int.TryParse(separatedNames[1], out result);
		}
		if (result <= 0 || result > 33)
		{
			return $"Ship count must between 0-{33}";
		}
		if (obj != null || CampaignCheats.TryGetObject(requestedId, out obj, out var errorMessage, (ShipHull x) => shipHulls.Contains(x)))
		{
			for (int num = 0; num < result; num++)
			{
				Ship ship = new Ship(obj);
				ChangeShipOwnerAction.ApplyByLooting(PartyBase.MainParty, ship);
			}
			if (!MobileParty.MainParty.IsCurrentlyAtSea && !MobileParty.MainParty.Anchor.IsValid)
			{
				MobileParty.MainParty.Anchor.Settlement = FindAnchorSettlementForParty(MobileParty.MainParty);
			}
			return $"{result} {obj.Name} were added to main party.";
		}
		return errorMessage + "    " + text;
	}

	public static Settlement FindAnchorSettlementForParty(MobileParty party)
	{
		IEnumerable<Town> source = Town.AllTowns.Where((Town x) => x.Settlement.HasPort && !party.MapFaction.IsAtWarWith(x.MapFaction));
		if (source.IsEmpty())
		{
			source = Town.AllTowns.Where((Town x) => x.Settlement.HasPort);
		}
		return source.MinBy((Town x) => x.Settlement.PortPosition.Distance(party.Position)).Settlement;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("unlock_figurehead", "naval")]
	public static string UnlockFigurehead(List<string> strings)
	{
		if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
		{
			return CampaignCheats.ErrorType;
		}
		string result = "Format is \"naval.unlock_figurehead [figurehead_id or all]\".";
		if (!CampaignCheats.CheckParameters(strings, 1) || CampaignCheats.CheckHelp(strings))
		{
			return result;
		}
		if (!CampaignCheats.IsPartySuitableToUseCheat(PartyBase.MainParty))
		{
			return "Main party not suitable to take figurehead right now";
		}
		string text = strings[0];
		if (string.Equals(text, "all", StringComparison.OrdinalIgnoreCase))
		{
			foreach (Figurehead objectType in MBObjectManager.Instance.GetObjectTypeList<Figurehead>())
			{
				if (!Campaign.Current.UnlockedFigureheadsByMainHero.Contains(objectType))
				{
					Campaign.Current.UnlockFigurehead(objectType);
				}
			}
			return "All figureheads unlocked for the player";
		}
		if (CampaignCheats.TryGetObject(text, out Figurehead obj, out string _, (Func<Figurehead, bool>)null))
		{
			if (!Campaign.Current.UnlockedFigureheadsByMainHero.Contains(obj))
			{
				Campaign.Current.UnlockFigurehead(obj);
				return $"Figurehead {obj.Name} is unlocked";
			}
			return "This figurehead already unlocked by the player";
		}
		return "Figurehead with id " + text + " does not exist.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("list_all_ship_names", "naval")]
	public static string ListAllShipNames(List<string> strings)
	{
		string message = string.Empty;
		if (!CheckCheatUsage(ref message))
		{
			return message;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ShipHull @object in MBObjectManager.Instance.GetObjects((ShipHull x) => x != null))
		{
			stringBuilder.AppendLine(@object.Name.ToString() + "   -   " + @object.StringId);
		}
		return stringBuilder.ToString();
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("list_all_figurehead_names", "naval")]
	public static string ListAllFigureheads(List<string> strings)
	{
		string message = string.Empty;
		if (!CheckCheatUsage(ref message))
		{
			return message;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Figurehead @object in MBObjectManager.Instance.GetObjects((Figurehead x) => x != null))
		{
			stringBuilder.AppendLine(@object.Name.ToString() + "   -   " + @object.StringId);
		}
		return stringBuilder.ToString();
	}
}
