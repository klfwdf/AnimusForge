using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Home;

public class MPAnnouncementsVM : ViewModel
{
	private readonly float? _announcementUpdateIntervalInSeconds;

	private float? _updateTimer;

	private bool _isRefreshingAnnouncements;

	private bool _hasValidAnnouncements;

	private string _titleText;

	private MBBindingList<MPAnnouncementItemVM> _announcementList;

	[DataSourceProperty]
	public bool HasValidAnnouncements
	{
		get
		{
			return _hasValidAnnouncements;
		}
		set
		{
			if (value != _hasValidAnnouncements)
			{
				_hasValidAnnouncements = value;
				OnPropertyChangedWithValue(value, "HasValidAnnouncements");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPAnnouncementItemVM> AnnouncementList
	{
		get
		{
			return _announcementList;
		}
		set
		{
			if (value != _announcementList)
			{
				_announcementList = value;
				OnPropertyChangedWithValue(value, "AnnouncementList");
			}
		}
	}

	public MPAnnouncementsVM(float? announcementUpdateIntervalInSeconds)
	{
		_updateTimer = null;
		_announcementUpdateIntervalInSeconds = announcementUpdateIntervalInSeconds;
		AnnouncementList = new MBBindingList<MPAnnouncementItemVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=lQ0T2pbY}Events & Announcements").ToString();
	}

	public void OnTick(float dt)
	{
		if (!NetworkMain.GameClient.AtLobby)
		{
			_updateTimer = null;
		}
		else if (_announcementUpdateIntervalInSeconds.HasValue && _announcementUpdateIntervalInSeconds > 0f && !_isRefreshingAnnouncements)
		{
			if (!_updateTimer.HasValue || _updateTimer > _announcementUpdateIntervalInSeconds)
			{
				RefreshAnnouncements();
				_updateTimer = 0f;
			}
			else
			{
				_updateTimer += dt;
			}
		}
	}

	private void RefreshAnnouncements()
	{
		_isRefreshingAnnouncements = true;
		UpdateAnnouncements();
	}

	public async void UpdateAnnouncements()
	{
		PublishedLobbyNewsArticle[] array = await NetworkMain.GameClient.GetLobbyNews();
		AnnouncementList.Clear();
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				MPAnnouncementItemVM item = new MPAnnouncementItemVM(array[i]);
				AnnouncementList.Add(item);
			}
		}
		HasValidAnnouncements = AnnouncementList.Count > 0 && ApplicationPlatform.IsPlatformWindows() && ApplicationPlatform.CurrentPlatform != Platform.GDKDesktop;
		_isRefreshingAnnouncements = false;
	}
}
