using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.View;

public class NavalDLCViewCheats
{
	[CommandLineFunctionality.CommandLineArgumentFunction("focus_player_anchor", "naval")]
	public static string FocusPlayerAnchor(List<string> strings)
	{
		string message = string.Empty;
		if (!NavalDLCCheats.CheckCheatUsage(ref message))
		{
			return message;
		}
		if (CampaignCheats.CheckHelp(strings))
		{
			return "Format is \"naval.focus_player_anchor\".";
		}
		if (!MobileParty.MainParty.Anchor.IsValid)
		{
			return "Anchor is not valid";
		}
		MapScreen.Instance.FastMoveCameraToPosition(MobileParty.MainParty.Anchor.Position);
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("focus_ship", "naval")]
	public static string FocusShip(List<string> strings)
	{
		if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
		{
			return CampaignCheats.ErrorType;
		}
		string text = "Format is \"naval.focus_ship [ShipHullStringId/ShipHullName]\".";
		if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckHelp(strings))
		{
			return text;
		}
		string text2 = CampaignCheats.ConcatenateString(strings);
		ShipHull shipHull = MBObjectManager.Instance.GetObject<ShipHull>(text2);
		if (shipHull == null)
		{
			foreach (ShipHull objectType in MBObjectManager.Instance.GetObjectTypeList<ShipHull>())
			{
				if (string.Equals(objectType.Name.ToString().ToLower(), text2.ToLower(), StringComparison.OrdinalIgnoreCase))
				{
					shipHull = objectType;
					break;
				}
			}
		}
		if (shipHull != null)
		{
			string shipHullStringId = shipHull.StringId;
			Town town = Town.AllTowns.FirstOrDefault((Town x) => x.AvailableShips.Exists((Ship y) => y.ShipHull.StringId == shipHullStringId));
			if (town != null)
			{
				town.AvailableShips.First((Ship x) => x.ShipHull.StringId == shipHullStringId);
				MapScreen.Instance.MapCameraView.SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
				town.Settlement.Party.SetAsCameraFollowParty();
				return "Success! Found in " + town.Name.ToString();
			}
		}
		return "Ship is not found : " + text2 + "\n" + text;
	}
}
