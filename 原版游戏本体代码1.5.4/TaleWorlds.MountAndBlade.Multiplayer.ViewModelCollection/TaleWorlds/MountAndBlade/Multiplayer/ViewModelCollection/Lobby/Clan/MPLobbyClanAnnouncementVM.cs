using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Friends;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanAnnouncementVM : ViewModel
{
	private DateTime _announcedDate;

	private PlayerId _senderId;

	private int _id;

	private bool _canBeDeleted;

	private string _messageText;

	private string _details;

	private MPLobbyPlayerBaseVM _senderPlayer;

	[DataSourceProperty]
	public bool CanBeDeleted
	{
		get
		{
			return _canBeDeleted;
		}
		set
		{
			if (value != _canBeDeleted)
			{
				_canBeDeleted = value;
				OnPropertyChanged("CanBeDeleted");
			}
		}
	}

	[DataSourceProperty]
	public string MessageText
	{
		get
		{
			return _messageText;
		}
		set
		{
			if (value != _messageText)
			{
				_messageText = value;
				OnPropertyChanged("MessageText");
			}
		}
	}

	[DataSourceProperty]
	public string Details
	{
		get
		{
			return _details;
		}
		set
		{
			if (value != _details)
			{
				_details = value;
				OnPropertyChanged("Details");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyPlayerBaseVM SenderPlayer
	{
		get
		{
			return _senderPlayer;
		}
		set
		{
			if (value != _senderPlayer)
			{
				_senderPlayer = value;
				OnPropertyChanged("SenderPlayer");
			}
		}
	}

	public MPLobbyClanAnnouncementVM(PlayerId senderId, string message, DateTime date, int id, bool canBeDeleted)
	{
		_id = id;
		_senderId = senderId;
		_announcedDate = date;
		SenderPlayer = new MPLobbyPlayerBaseVM(senderId);
		MessageText = message;
		CanBeDeleted = canBeDeleted;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		string content = new TextObject("{=oMiNaY1E}Posted By").ToString();
		GameTexts.SetVariable("STR1", content);
		GameTexts.SetVariable("STR2", SenderPlayer.Name);
		string content2 = GameTexts.FindText("str_STR1_space_STR2").ToString();
		string dateFormattedByLanguage = LocalizedTextManager.GetDateFormattedByLanguage(BannerlordConfig.Language, _announcedDate);
		GameTexts.SetVariable("STR1", content2);
		GameTexts.SetVariable("STR2", dateFormattedByLanguage);
		Details = new TextObject("{=QvDxB57o}{STR1} | {STR2}").ToString();
	}

	private void ExecuteDeleteAnnouncement()
	{
		string titleText = new TextObject("{=P1MybNr7}Delete Announcement").ToString();
		string text = new TextObject("{=CW2JkWzC}Are you sure want to delete this announcement?").ToString();
		InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), DeleteAnnouncement, null));
	}

	private void DeleteAnnouncement()
	{
		NetworkMain.GameClient.RemoveClanAnnouncement(_id);
	}
}
