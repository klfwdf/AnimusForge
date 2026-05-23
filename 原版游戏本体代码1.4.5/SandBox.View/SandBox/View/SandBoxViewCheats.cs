using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.View;

public static class SandBoxViewCheats
{
	[CommandLineFunctionality.CommandLineArgumentFunction("kill_hero", "campaign")]
	public static string KillHero(List<string> strings)
	{
		if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
		{
			return CampaignCheats.ErrorType;
		}
		string text = "Format is \"campaign.kill_hero [HeroName]\".";
		if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckHelp(strings))
		{
			return text;
		}
		string text2 = CampaignCheats.ConcatenateString(strings);
		if (CampaignCheats.TryGetObject(text2, out var obj, out var errorMessage, (Hero x) => x.IsActive && (x.IsLord || x.IsWanderer)))
		{
			if (!obj.IsAlive)
			{
				return "Hero " + text2 + " is already dead.";
			}
			if (obj.DeathMark != KillCharacterAction.KillCharacterActionDetail.None)
			{
				return "Hero already has a death mark.";
			}
			if (obj.CurrentSettlement != null && !obj.IsNotable)
			{
				return "Hero cannot be killed while staying in a settlement.";
			}
			if (MapScreen.Instance.IsHeirSelectionPopupActive)
			{
				return "Hero cannot be killed during the heir selection.";
			}
			if (Campaign.Current.ConversationManager.OneToOneConversationHero != null)
			{
				return "Hero cannot be killed during a conversation.";
			}
			if (obj.PartyBelongedTo?.MapEvent != null || obj.PartyBelongedTo?.SiegeEvent != null)
			{
				if (!obj.CanDie(KillCharacterAction.KillCharacterActionDetail.DiedInBattle))
				{
					return "Hero can't die!";
				}
				obj.AddDeathMark(null, KillCharacterAction.KillCharacterActionDetail.DiedInBattle);
			}
			else
			{
				if (!obj.CanDie(KillCharacterAction.KillCharacterActionDetail.Murdered))
				{
					return "Hero can't die!";
				}
				KillCharacterAction.ApplyByMurder(obj);
			}
			return "Hero " + text2.ToLower() + " is killed.";
		}
		return errorMessage + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("focus_tournament", "campaign")]
	public static string FocusTournament(List<string> strings)
	{
		if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
		{
			return CampaignCheats.ErrorType;
		}
		if (CampaignCheats.CheckHelp(strings))
		{
			return "Format is \"campaign.focus_tournament\".";
		}
		Settlement settlement = Settlement.FindFirst((Settlement x) => x.IsTown && Campaign.Current.TournamentManager.GetTournamentGame(x.Town) != null);
		if (settlement == null)
		{
			return "There isn't any tournament right now.";
		}
		((MapCameraView)typeof(MapCameraView).GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
		settlement.Party.SetAsCameraFollowParty();
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("make_clan_mercenary_of_kingdom", "campaign")]
	public static string MakeClanMercenaryOfKingdom(List<string> strings)
	{
		if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
		{
			return CampaignCheats.ErrorType;
		}
		if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckParameters(strings, 1) || CampaignCheats.CheckHelp(strings))
		{
			return "Format is \"campaign.MakeClanMercenaryOfKingdom [clan] | [kingdom] | [days]\".";
		}
		List<string> separatedNames = CampaignCheats.GetSeparatedNames(strings, removeEmptySpaces: true);
		if (separatedNames.Count < 2)
		{
			return "Format is \"campaign.MakeClanMercenaryOfKingdom [clan] | [kingdom] | [days]\".";
		}
		CampaignCheats.TryGetObject(separatedNames[0], out Clan obj, out string errorMessage, (Func<Clan, bool>)null);
		if (obj == null)
		{
			return "Cant find the clan\n" + errorMessage;
		}
		CampaignCheats.TryGetObject(separatedNames[1], out Kingdom obj2, out string errorMessage2, (Func<Kingdom, bool>)null);
		if (obj2 == null)
		{
			return errorMessage2;
		}
		if (!obj.IsMinorFaction)
		{
			return "Clan is not suitable to be mercenary";
		}
		if (obj == Clan.PlayerClan)
		{
			return "Use join_kingdom or join_kingdom_as_mercenary";
		}
		if (obj.IsUnderMercenaryService)
		{
			ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(obj);
		}
		CampaignTime shouldStayInKingdomUntil = CampaignTime.Zero;
		if (separatedNames.Count == 3 && int.TryParse(separatedNames[2], out var result))
		{
			shouldStayInKingdomUntil = CampaignTime.DaysFromNow(result);
		}
		ChangeKingdomAction.ApplyByJoinFactionAsMercenary(obj, obj2, shouldStayInKingdomUntil);
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("focus_hostile_army", "campaign")]
	public static string FocusHostileArmy(List<string> strings)
	{
		if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
		{
			return CampaignCheats.ErrorType;
		}
		if (CampaignCheats.CheckHelp(strings))
		{
			return "Format is \"campaign.focus_hostile_army\".";
		}
		Army army = null;
		foreach (Kingdom item in Kingdom.All)
		{
			if (item != Clan.PlayerClan.MapFaction && !item.Armies.IsEmpty() && item.IsAtWarWith(Clan.PlayerClan.MapFaction))
			{
				army = item.Armies.GetRandomElement();
			}
			if (army != null)
			{
				break;
			}
		}
		if (army == null)
		{
			return "There isn't any hostile army right now.";
		}
		((MapCameraView)typeof(MapCameraView).GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
		army.LeaderParty.Party.SetAsCameraFollowParty();
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("focus_mobile_party", "campaign")]
	public static string FocusMobileParty(List<string> strings)
	{
		if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
		{
			return CampaignCheats.ErrorType;
		}
		string text = "Format is \"campaign.focus_mobile_party [PartyName]\".";
		if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckHelp(strings))
		{
			return text;
		}
		if (CampaignCheats.TryGetObject(CampaignCheats.ConcatenateString(strings), out MobileParty obj, out string errorMessage, (Func<MobileParty, bool>)null))
		{
			MapCameraView obj2 = (MapCameraView)typeof(MapCameraView).GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
			if (!obj.IsVisible && obj.CurrentSettlement == null)
			{
				obj.IsVisible = true;
			}
			obj2.SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
			obj.Party.SetAsCameraFollowParty();
			return $"Focused party {obj.Name}";
		}
		return errorMessage + " : \n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("focus_hero", "campaign")]
	public static string FocusHero(List<string> strings)
	{
		if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
		{
			return CampaignCheats.ErrorType;
		}
		string text = "Format is \"campaign.focus_hero [HeroName]\".";
		if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckHelp(strings))
		{
			return text;
		}
		string text2 = CampaignCheats.ConcatenateString(strings);
		if (CampaignCheats.TryGetObject(text2, out var obj, out var errorMessage, (Hero x) => x != Hero.MainHero && x.IsActive && (x.IsLord || x.IsWanderer)))
		{
			MapCameraView mapCameraView = (MapCameraView)typeof(MapCameraView).GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
			if (obj.CurrentSettlement != null)
			{
				mapCameraView.SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
				obj.CurrentSettlement.Party.SetAsCameraFollowParty();
				return "Success";
			}
			if (obj.PartyBelongedTo != null)
			{
				mapCameraView.SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
				obj.PartyBelongedTo.Party.SetAsCameraFollowParty();
				if (obj.PartyBelongedTo.CurrentSettlement == null && !obj.PartyBelongedTo.IsVisible)
				{
					obj.PartyBelongedTo.IsVisible = true;
				}
				return "Success";
			}
			if (obj.PartyBelongedToAsPrisoner != null)
			{
				mapCameraView.SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
				obj.PartyBelongedToAsPrisoner.SetAsCameraFollowParty();
				if (obj.PartyBelongedToAsPrisoner.MobileParty.CurrentSettlement == null && !obj.PartyBelongedToAsPrisoner.MobileParty.IsVisible)
				{
					obj.PartyBelongedToAsPrisoner.MobileParty.IsVisible = true;
				}
				return "Success";
			}
			return "Party is not found : " + text2 + "\n" + text;
		}
		return errorMessage + ": " + text2 + "\n" + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("focus_infested_hideout", "campaign")]
	public static string FocusInfestedHideout(List<string> strings)
	{
		if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
		{
			return CampaignCheats.ErrorType;
		}
		string text = "Format is \"campaign.focus_infested_hideout [Optional: Number of troops]\".";
		if (CampaignCheats.CheckHelp(strings))
		{
			return text;
		}
		MBList<Settlement> mBList = Settlement.All.Where((Settlement t) => t.IsHideout && t.Parties.Count > 0).ToMBList();
		Settlement settlement = null;
		if (mBList.IsEmpty())
		{
			return "All hideouts are empty!";
		}
		if (strings.Count > 0)
		{
			int troopCount = -1;
			int.TryParse(strings[0], out troopCount);
			if (troopCount == -1)
			{
				return "Incorrect input.\n" + text;
			}
			MBList<Settlement> mBList2 = mBList.Where((Settlement t) => t.Parties.Sum((MobileParty p) => p.MemberRoster.TotalManCount) >= troopCount).ToMBList();
			if (mBList2.IsEmpty())
			{
				return "Can't find suitable hideout.";
			}
			settlement = mBList2.GetRandomElement();
		}
		else
		{
			settlement = mBList.GetRandomElement();
		}
		if (settlement != null)
		{
			((MapCameraView)typeof(MapCameraView).GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
			settlement.Party.SetAsCameraFollowParty();
			return "Success";
		}
		return "Unable to find such a hideout.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("focus_issue", "campaign")]
	public static string FocusIssues(List<string> strings)
	{
		if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
		{
			return CampaignCheats.ErrorType;
		}
		string text = "Format is \"campaign.focus_issue [IssueName]\".";
		if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckHelp(strings))
		{
			return text;
		}
		MapCameraView mapCameraView = (MapCameraView)typeof(MapCameraView).GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		CampaignCheats.TryGetObject(CampaignCheats.ConcatenateString(strings), out IssueBase obj, out string errorMessage, (Func<IssueBase, bool>)null);
		if (obj == null)
		{
			return errorMessage + " " + text;
		}
		if (obj.IssueSettlement != null)
		{
			mapCameraView.SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
			obj.IssueSettlement.Party.SetAsCameraFollowParty();
		}
		else if (obj.IssueOwner.PartyBelongedTo != null)
		{
			mapCameraView.SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
			obj.IssueOwner.PartyBelongedTo?.Party.SetAsCameraFollowParty();
		}
		else if (obj.IssueOwner.CurrentSettlement != null)
		{
			mapCameraView.SetCameraMode(MapCameraView.CameraFollowMode.FollowParty);
			obj.IssueOwner.CurrentSettlement.Party.SetAsCameraFollowParty();
		}
		return "Found issue: " + obj.Title.ToString() + ". Issue Owner: " + obj.IssueOwner.Name.ToString();
	}
}
