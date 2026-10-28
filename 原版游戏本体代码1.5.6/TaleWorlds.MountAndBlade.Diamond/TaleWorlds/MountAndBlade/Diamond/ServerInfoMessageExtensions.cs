using Messages.FromLobbyServer.ToClient;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Diamond;

public static class ServerInfoMessageExtensions
{
	public static TextObject GetDescription(this SystemMessage message)
	{
		for (int i = 0; i < message.Parameters.Count; i++)
		{
			GameTexts.SetVariable("ARG" + (i + 1), message.Parameters[i]);
		}
		switch (message.Message)
		{
		case ServerInfoMessage.Success:
			return new TextObject("{=NaTaGiB1}Success.");
		case ServerInfoMessage.LoginMuted:
			return new TextObject("{=h84jSMrT}You are muted until {ARG1}. Reason: {ARG2}");
		case ServerInfoMessage.DestroySessionPremadeGameCancellation:
			return new TextObject("{=T5gR6XCJ}Premade game no longer exists.");
		case ServerInfoMessage.DestroySessionPartyInvitationCancellation:
			return new TextObject("{=a1RXhy6A}Your party invitation is no longer valid.");
		case ServerInfoMessage.DestroySessionPartyAutoDisband:
			return new TextObject("{=BwBR7TJB}No one left in your party and it has been disbanded!");
		case ServerInfoMessage.PlayerNotFound:
			return new TextObject("{=ysfEQO6c}Player not found");
		case ServerInfoMessage.PlayerNotInLobby:
			if (message.Parameters.Count <= 0 || !(message.Parameters[0] == string.Empty))
			{
				return new TextObject("{=SWZaqPx4}{ARG1} is not in lobby.");
			}
			return new TextObject("{=uDbGN6fp}Player is not in lobby.");
		case ServerInfoMessage.MustBeInLobby:
			return new TextObject("{=DJFaYeWm}You must be in lobby to perform this action.");
		case ServerInfoMessage.NoTextGiven:
			return new TextObject("{=hav7yZyB}No text entered.");
		case ServerInfoMessage.TextTooLong:
			return new TextObject("{=aId8kA8r}Given text is longer than the limit.");
		case ServerInfoMessage.FindGameBlockedFromMatchmaking:
			return new TextObject("{=P0aRStg4}Cannot queue to battle because you are blocked from matchmaking.");
		case ServerInfoMessage.FindGamePartyMemberBlockedFromMatchmaking:
			return new TextObject("{=AIgw3aFG}Cannot queue to battle because some of the players are blocked from matchmaking.");
		case ServerInfoMessage.FindGameNoGameTypeSelected:
			return new TextObject("{=caMylVF7}Cannot queue to battle because no enabled game types are selected.");
		case ServerInfoMessage.FindGameDisabledGameTypesSelected:
			return new TextObject("{=4YuWC1Jg}Disabled game types selected.");
		case ServerInfoMessage.FindGamePlayerCountNotAllowed:
			return new TextObject("{=vi3lFSBW}Parties of {ARG1} are not allowed in party queue.");
		case ServerInfoMessage.FindGameNotPartyLeader:
			return new TextObject("{=wNubP2BF}Only party leader can queue for games.");
		case ServerInfoMessage.FindGameNotAllPlayersReady:
			return new TextObject("{=OByu0fjQ}Cannot queue to battle because not all players are ready in your party.");
		case ServerInfoMessage.FindGameRegionNotAvailable:
			return new TextObject("{=E2ILp86t}Cannot queue to battle because region is not available.");
		case ServerInfoMessage.FindGamePunished:
			return new TextObject("{=ND2abg9k}You are blocked from matchmaking for {ARG1} seconds.");
		case ServerInfoMessage.RejoinGame:
			return new TextObject("{=lcq2bKRk}Rejoining battle.");
		case ServerInfoMessage.RejoinGameNotFound:
			return new TextObject("{=MowSn4ch}Game not found.");
		case ServerInfoMessage.RejoinGameNotAllowed:
			return new TextObject("{=Ebp5T7vh}Can't rejoin game you've abandoned.");
		case ServerInfoMessage.AddFriendCantAddSelf:
			return new TextObject("{=SIT1tREw}You cannot add yourself as friend");
		case ServerInfoMessage.AddFriendRequestSent:
			if (message.Parameters.Count <= 0 || !(message.Parameters[0] == string.Empty))
			{
				return new TextObject("{=CyeaniWe}Friend request is sent to {ARG1}.");
			}
			return new TextObject("{=yvT1gK1g}Friend request is sent.");
		case ServerInfoMessage.AddFriendRequestReceived:
			return new TextObject("{=shIV99LZ}Friend request received from {ARG1}.");
		case ServerInfoMessage.AddFriendAlreadyFriends:
			return new TextObject("{=dabogDDj}You are already friends");
		case ServerInfoMessage.AddFriendRequestPending:
			if (message.Parameters.Count <= 0 || !(message.Parameters[0] == string.Empty))
			{
				return new TextObject("{=bzcI3ei2}You already have a pending request for {ARG1}.");
			}
			return new TextObject("{=rwcA5RMK}You already have a pending request for this player.");
		case ServerInfoMessage.AddFriendRequestAccepted:
			return new TextObject("{=TaLNjfao}You are now friends with {ARG1}.");
		case ServerInfoMessage.AddFriendRequestDeclined:
			if (message.Parameters.Count <= 0 || !(message.Parameters[0] == string.Empty))
			{
				return new TextObject("{=Ds01FoSn}{ARG1} declined your friend request.");
			}
			return new TextObject("{=ASgVFGs1}Your friend request is declined.");
		case ServerInfoMessage.AddFriendRequestBlocked:
			return new TextObject("{=GIiGZOxP}Cannot add friend. Privacy settings failed.");
		case ServerInfoMessage.RemoveFriendSuccess:
			return new TextObject("{=M0Cgjgb0}{ARG1} has been removed as a friend.");
		case ServerInfoMessage.FriendRequestAccepted:
			return new TextObject("{=3NwGpJwe}Friend request accepted.");
		case ServerInfoMessage.FriendRequestDeclined:
			return new TextObject("{=ruC5qF1H}Friend request declined.");
		case ServerInfoMessage.FriendRequestNotFound:
			return new TextObject("{=oFfMEy1S}Friend request cannot be found");
		case ServerInfoMessage.MustBeInParty:
			return new TextObject("{=2BtJYizu}You must be in a party to perform this action.");
		case ServerInfoMessage.MustBePartyLeader:
			return new TextObject("{=IRWReNWu}You must be the leader of your party to perform this action.");
		case ServerInfoMessage.InvitePartyHasModules:
			return new TextObject("{=vd8dEfvZ}You can't create a party while having mods!");
		case ServerInfoMessage.InvitePartyOtherPlayerHasModules:
			return new TextObject("{=l1YfVXF3}You can't invite a player that has mods!");
		case ServerInfoMessage.InvitePartyCantInviteSelf:
			return new TextObject("{=fD2SyeA7}You cannot invite yourself to party.");
		case ServerInfoMessage.InvitePartyOtherPlayerAlreadyInParty:
		case ServerInfoMessage.SuggestPartyOtherPlayerAlreadyInParty:
			return new TextObject("{=hcJumPEg}{ARG1} is already in party.");
		case ServerInfoMessage.InvitePartyPartyIsFull:
			return new TextObject("{=mvQKpHXH}Your party is full.");
		case ServerInfoMessage.InvitePartyOnlyLeaderCanInvite:
			return new TextObject("{=l6a9vI9z}Only party leader can invite other players.");
		case ServerInfoMessage.InvitePartySuccess:
			if (message.Parameters.Count <= 0 || !(message.Parameters[0] == string.Empty))
			{
				return new TextObject("{=vfsaTZKg}{ARG1} invited to your party.");
			}
			return new TextObject("{=x5vn4FPi}Player invited to your party.");
		case ServerInfoMessage.SuggestPartyMustBeInParty:
			return new TextObject("{=mdH4S8Kx}You must be in a party to suggest someone to your party.");
		case ServerInfoMessage.SuggestPartyMustBeMember:
			return new TextObject("{=4lNl8zS2}You cannot suggest someone to party as party leader.");
		case ServerInfoMessage.SuggestPartyCantSuggestSelf:
			return new TextObject("{=S7Y5Suho}You cannot suggest yourself to party.");
		case ServerInfoMessage.SuggestPartySuccess:
			if (message.Parameters.Count <= 0 || !(message.Parameters[0] == string.Empty))
			{
				return new TextObject("{=9kzik0vP}{ARG1} suggested to your party.");
			}
			return new TextObject("{=DOkblbrz}Player suggested to your party.");
		case ServerInfoMessage.DisbandPartySuccess:
			return new TextObject("{=Pn13ZKwI}Your party has been disbanded!");
		case ServerInfoMessage.KickPlayerOtherPlayerMustBeInParty:
			return new TextObject("{=VdyJtjSG}{ARG1} is not in your party!");
		case ServerInfoMessage.KickPartyPlayerMustBeLeader:
			return new TextObject("{=4XDu4Qcd}Only party leader can kick players from the party.");
		case ServerInfoMessage.PromotePartyLeaderOngoingClanCreation:
			return new TextObject("{=yrCBeRMf}You can not change the party leader when in the process of creating a clan.");
		case ServerInfoMessage.PromotePartyLeaderCantPromoteSelf:
			return new TextObject("{=bivF3RXY}You cannot promote yourself");
		case ServerInfoMessage.PromotePartyLeaderCantPromoteNonMember:
			return new TextObject("{=wJsR1aGI}{ARG1} is not a party member.");
		case ServerInfoMessage.PromotePartyLeaderMustBeLeader:
			return new TextObject("{=emx7txM9}Member promotion action can only be performed by the party leader.");
		case ServerInfoMessage.PromotePartyLeaderSuccess:
			return new TextObject("{=IWr2ZmWX}{ARG1} was promoted to party leader.");
		case ServerInfoMessage.PromotePartyLeaderAuto:
			return new TextObject("{=zPvuvrBe}{ARG1} was assigned as party leader.");
		case ServerInfoMessage.MustBeInClan:
			return new TextObject("{=bMLqfPRv}You are not in a clan.");
		case ServerInfoMessage.MustBeClanLeader:
			return new TextObject("{=PzUrwWnO}You are not the leader of your clan.");
		case ServerInfoMessage.MustBePrivilegedClanMember:
			return new TextObject("{=Y7wWhSsO}You are not a privileged member of your clan.");
		case ServerInfoMessage.ClanCreationNameIsInvalid:
			return new TextObject("{=nlYK3i5a}Clan name is invalid.");
		case ServerInfoMessage.ClanCreationTagIsInvalid:
			return new TextObject("{=APQ0kNVj}Clan tag is invalid.");
		case ServerInfoMessage.ClanCreationSigilIsInvalid:
			return new TextObject("{=bB5p8KpK}Clan sigil is invalid.");
		case ServerInfoMessage.ClanCreationCultureIsInvalid:
			return new TextObject("{=OfCk5NMA}Clan faction is invalid.");
		case ServerInfoMessage.ClanCreationNotAllPlayersReady:
			return new TextObject("{=SvHP9TXr}Not all players are ready.");
		case ServerInfoMessage.ClanCreationNotEnoughPlayers:
			return new TextObject("{=K0mSh8Hw}Your party does not have enough players to create a clan.");
		case ServerInfoMessage.ClanCreationAlreadyInAClan:
			return new TextObject("{=dHlxVbMw}You are already in a clan.");
		case ServerInfoMessage.ClanCreationHaveToBeInAParty:
			return new TextObject("{=b72mIrOl}You have to be in a party to create a clan.");
		case ServerInfoMessage.SetClanInformationSuccess:
			return new TextObject("{=iG72AMy8}Information text changed successfully.");
		case ServerInfoMessage.AddClanAnnouncementSuccess:
			return new TextObject("{=baHOuMTb}Announcement added successfully.");
		case ServerInfoMessage.EditClanAnnouncementNotFound:
			return new TextObject("{=e9LrrgAH}Announcement doesn't exist.");
		case ServerInfoMessage.EditClanAnnouncementSuccess:
			return new TextObject("{=y0dKQ8pL}Announcement changed successfully.");
		case ServerInfoMessage.DeleteClanAnnouncementNotFound:
			return new TextObject("{=e9LrrgAH}Announcement doesn't exist.");
		case ServerInfoMessage.DeleteClanAnnouncementSuccess:
			return new TextObject("{=rI8gE2Qh}Announcement deleted successfully.");
		case ServerInfoMessage.ChangeClanSigilInvalid:
			return new TextObject("{=qHGkxNWJ}Invalid clan sigil.");
		case ServerInfoMessage.ChangeClanSigilSuccess:
			return new TextObject("{=rfaIXasP}Clan sigil changed successfully.");
		case ServerInfoMessage.ChangeClanCultureSuccess:
			return new TextObject("{=KPMxDIgb}Clan culture changed successfully.");
		case ServerInfoMessage.InviteClanPlayerAlreadyInvited:
			if (message.Parameters.Count <= 0 || !(message.Parameters[0] == string.Empty))
			{
				return new TextObject("{=uJ7527FB}{ARG1} is already invited to another clan.");
			}
			return new TextObject("{=Np8V0jcX}Player is already invited to another clan.");
		case ServerInfoMessage.InviteClanPlayerAlreadyInClan:
			if (message.Parameters.Count <= 0 || !(message.Parameters[0] == string.Empty))
			{
				return new TextObject("{=DcbVwkit}{ARG1} is already in a clan.");
			}
			return new TextObject("{=ba4CJpja}Player is already in a clan.");
		case ServerInfoMessage.InviteClanPlayerIsNotOnline:
			if (message.Parameters.Count <= 0 || !(message.Parameters[0] == string.Empty))
			{
				return new TextObject("{=gQ8deZ17}{ARG1} is not online.");
			}
			return new TextObject("{=rkxTEFGp}Player is not online.");
		case ServerInfoMessage.InviteClanPlayerFeatureNotSupported:
			return new TextObject("{=8UmHXSnO}Player does not have the Clan feature available.");
		case ServerInfoMessage.InviteClanCantInviteSelf:
			return new TextObject("{=XgQQPZsn}You can't invite yourself.");
		case ServerInfoMessage.InviteClanSuccess:
			if (message.Parameters.Count <= 0 || !(message.Parameters[0] == string.Empty))
			{
				return new TextObject("{=ZB6cxznt}{ARG1} invited to clan.");
			}
			return new TextObject("{=Dmz3JLyp}Player invited to clan.");
		case ServerInfoMessage.AcceptClanInvitationSuccess:
			return new TextObject("{=T2Q35w3d}{ARG1} accepted clan invitation.");
		case ServerInfoMessage.DeclineClanInvitationSuccess:
			return new TextObject("{=7cEXe02I}{ARG1} declined clan invitation.");
		case ServerInfoMessage.PromoteClanRolePlayerNotInClan:
			if (message.Parameters.Count <= 0 || !(message.Parameters[0] == string.Empty))
			{
				return new TextObject("{=tam7tCdb}{ARG1} is not in your clan.");
			}
			return new TextObject("{=M8x38ZZZ}Player is not in your clan.");
		case ServerInfoMessage.PromoteClanLeaderCantPromoteSelf:
			return new TextObject("{=P3JTNI9P}You can not promote yourself to clan leader.");
		case ServerInfoMessage.PromoteClanLeaderSuccess:
			return new TextObject("{=jbaOH6qf}{ARG1} successfully promoted to clan leader.");
		case ServerInfoMessage.PromoteClanOfficerRoleLimitReached:
			return new TextObject("{=bvWo9N6k}Officer limit of the clan is reached.");
		case ServerInfoMessage.PromoteClanOfficerCantPromoteSelf:
			return new TextObject("{=eozBcVaB}You can not promote yourself as clan officer.");
		case ServerInfoMessage.PromoteClanOfficerSuccess:
			return new TextObject("{=FFPyGnZg}{ARG1} successfully promoted to clan officer.");
		case ServerInfoMessage.RemoveClanOfficerMustBeOfficerToMember:
			return new TextObject("{=3NxaSDgg}You are not an officer.");
		case ServerInfoMessage.RemoveClanOfficerMustBeOfficerToLeader:
			return new TextObject("{=yR7avbUg}{ARG1} is not an officer.");
		case ServerInfoMessage.RemoveClanOfficerSuccessFromLeader:
			return new TextObject("{=3lNHtNWa}Officer role of {ARG1} is removed.");
		case ServerInfoMessage.RemoveClanOfficerSuccessFromMember:
			return new TextObject("{=TuORvMMq}Left officer role.");
		case ServerInfoMessage.RemoveClanMemberToMember:
			return new TextObject("{=nZlu3Moe}Left clan.");
		case ServerInfoMessage.RemoveClanMemberToLeader:
			return new TextObject("{=tKlQbZaJ}{ARG1} removed from clan.");
		case ServerInfoMessage.RemoveClanMemberLeaderCantLeave:
			return new TextObject("{=FEeVGSO6}Clan leader can't leave the clan.");
		case ServerInfoMessage.PremadeGameCreationCanceled:
			return new TextObject("{=zKK3LDbF}Premade game creation canceled");
		case ServerInfoMessage.PremadeGameCreationMustBeCreating:
			return new TextObject("{=sa138Btz}You have to be creating a clan game to cancel one.");
		case ServerInfoMessage.PremadeGameCreationMapNotAvailable:
			return new TextObject("{=MgGvnobq}Selected map is not available.");
		case ServerInfoMessage.PremadeGameCreationPartyNotEligible:
			return new TextObject("{=yQyozBBX}Your party is not eligible to create a clan game.");
		case ServerInfoMessage.PremadeGameCreationInvalidGameType:
			return new TextObject("{=hqK9565f}Invalid game type.");
		case ServerInfoMessage.PremadeGameJoinIncorrectPassword:
			return new TextObject("{=Ajw0d4dW}Incorrect password.");
		case ServerInfoMessage.PremadeGameJoinGameNotFound:
			return new TextObject("{=MowSn4ch}Game not found.");
		case ServerInfoMessage.PremadeGameJoinPartyNotEligible:
			return new TextObject("{=uqqqWl6f}Party not eligible for clan game.");
		case ServerInfoMessage.GetPremadeGameListNotEligible:
			return new TextObject("{=0XY1VKVB}Your party is not eligible for clan games.");
		case ServerInfoMessage.ReportPlayerGameNotFound:
			return new TextObject("{=xHqjCamt}Could not report player. Game cannot be found.");
		case ServerInfoMessage.ReportPlayerPlayerNotFound:
			return new TextObject("{=6cMSAe2Z}Could not report player. Player cannot be found.");
		case ServerInfoMessage.ReportPlayerServerIsUnofficial:
			return new TextObject("{=t8K21Vmc}Could not report player. Server is unofficial.");
		case ServerInfoMessage.ReportPlayerSuccess:
			return new TextObject("{=kuM16dg2}{ARG1} has been reported.");
		case ServerInfoMessage.ChangeBannerlordIDFailure:
			return new TextObject("{=R7z6X8bT}Could not update Bannerlord ID.");
		case ServerInfoMessage.ChangeBannerlordIDSuccess:
			return new TextObject("{=bFSVAiel}Your Bannerlord ID is updated successfully.");
		case ServerInfoMessage.ChangeBannerlordIDEmpty:
			return new TextObject("{=RKioOKi4}Bannerlord ID cannot be empty.");
		case ServerInfoMessage.ChangeBannerlordIDTooShort:
			return new TextObject("{=1CO3GGEk}Given ID is too short.");
		case ServerInfoMessage.ChangeBannerlordIDTooLong:
			return new TextObject("{=RuKlb0Li}Given ID is too long.");
		case ServerInfoMessage.ChangeBannerlordIDInvalidCharacters:
			return new TextObject("{=fQCaY3da}Given ID contains invalid characters.");
		case ServerInfoMessage.ChangeBannerlordIDProfanity:
			return new TextObject("{=BpwNljJF}Bannerlord Id cannot contain profanity.");
		case ServerInfoMessage.GameInvitationCantInviteSelf:
			return new TextObject("{=0MDEP0jy}You cannot invite yourself");
		case ServerInfoMessage.GameInvitationPlayerAlreadyInGame:
			return new TextObject("{=UcUqJcr9}You cannot invite an online player to the game");
		case ServerInfoMessage.GameInvitationSuccess:
			return new TextObject("{=J3dazru7}Invited {ARG1} to the game");
		case ServerInfoMessage.ChangeRegionFailed:
			return new TextObject("{=YGfHWmNS}Could not change region");
		case ServerInfoMessage.ChangeGameModeFailed:
			return new TextObject("{=AAnoAhox}Could not change selected game mode");
		case ServerInfoMessage.BattleServerKickFriendlyFire:
			return new TextObject("{=InUAmnX4}You are kicked due to friendly damage");
		case ServerInfoMessage.ChatServerDisconnectedFromRoom:
			return new TextObject("{=DMPzhEK0}Disconnected from chat room: /{ARG1}");
		case ServerInfoMessage.CustomizationServiceIsUnavailable:
			return new TextObject("{=o76rZw0U}Service is not available at the moment");
		case ServerInfoMessage.CustomizationNotEnoughLoot:
			return new TextObject("{=CyrOnYI2}You do not have enough loot.");
		case ServerInfoMessage.CustomizationItemIsUnavailable:
			return new TextObject("{=SsCPw38T}Selected cosmetic does not exist");
		case ServerInfoMessage.CustomizationItemAlreadyOwned:
			return new TextObject("{=hhUKkNR6}You already own selected cosmetic item");
		case ServerInfoMessage.CustomizationItemIsFree:
			return new TextObject("{=cwF03dOR}You cannot buy a free cosmetic item");
		case ServerInfoMessage.CustomizationItemIsNotOwned:
			return new TextObject("{=4HQNKMUZ}You do not own selected cosmetic item.");
		case ServerInfoMessage.CustomizationChangeSigilSuccess:
			return new TextObject("{=2KsbeJHa}You have successfully updated your sigil preference");
		case ServerInfoMessage.CustomizationTroopIsNotValid:
			return new TextObject("{=kVcDXjUx}Invalid troop");
		case ServerInfoMessage.CustomizationCantUseMoreThanOneForSingleSlot:
			return new TextObject("{=96e2tZea}You cannot use multiple cosmetic items changing same item");
		case ServerInfoMessage.CustomizationCantUpdateBadge:
			return new TextObject("{=9TxM5UQS}Could not update shown badge.");
		case ServerInfoMessage.CustomizationInvalidBadge:
			return new TextObject("{=bl2dNFmT}Invalid badge.");
		case ServerInfoMessage.CustomizationCantDowngradeBadge:
			return new TextObject("{=w0FSTU2h}You cannot pick a lower tier badge.");
		case ServerInfoMessage.CustomizationBadgeNotAvailable:
			return new TextObject("{=HTFNfODt}You cannot pick a badge you have not earned.");
		default:
			return TextObject.GetEmpty();
		}
	}
}
