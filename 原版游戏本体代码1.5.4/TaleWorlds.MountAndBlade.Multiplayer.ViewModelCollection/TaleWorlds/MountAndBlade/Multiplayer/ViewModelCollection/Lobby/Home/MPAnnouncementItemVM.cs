using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Home;

public class MPAnnouncementItemVM : ViewModel
{
	private readonly PublishedLobbyNewsArticle _announcement;

	private bool _isSingleDate;

	private bool _isPinned;

	private int _type;

	private string _typeName;

	private string _title;

	private string _description;

	private string _dateText;

	[DataSourceProperty]
	public bool IsSingleDate
	{
		get
		{
			return _isSingleDate;
		}
		set
		{
			if (value != _isSingleDate)
			{
				_isSingleDate = value;
				OnPropertyChangedWithValue(value, "IsSingleDate");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPinned
	{
		get
		{
			return _isPinned;
		}
		set
		{
			if (value != _isPinned)
			{
				_isPinned = value;
				OnPropertyChangedWithValue(value, "IsPinned");
			}
		}
	}

	[DataSourceProperty]
	public int Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (value != _type)
			{
				_type = value;
				OnPropertyChangedWithValue(value, "Type");
			}
		}
	}

	[DataSourceProperty]
	public string TypeName
	{
		get
		{
			return _typeName;
		}
		set
		{
			if (value != _typeName)
			{
				_typeName = value;
				OnPropertyChangedWithValue(value, "TypeName");
			}
		}
	}

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	[DataSourceProperty]
	public string DateText
	{
		get
		{
			return _dateText;
		}
		set
		{
			if (value != _dateText)
			{
				_dateText = value;
				OnPropertyChangedWithValue(value, "DateText");
			}
		}
	}

	public static event Action<MPAnnouncementItemVM> OnInspect;

	public MPAnnouncementItemVM(PublishedLobbyNewsArticle announcement)
	{
		_announcement = announcement;
		IsPinned = announcement.Pinned;
		Type = announcement.Type;
		TypeName = GetAnnouncementTypeName(Type);
		Title = ParseMarkup(announcement.Title);
		Description = ParseMarkup(announcement.Description);
		UpdateDateText();
	}

	public void ExecuteInspect()
	{
		MPAnnouncementItemVM.OnInspect?.Invoke(this);
	}

	private string ParseMarkup(string markupText)
	{
		return markupText;
	}

	private void UpdateDateText()
	{
		IsSingleDate = string.IsNullOrEmpty(_announcement.DateStart) || string.IsNullOrEmpty(_announcement.DateEnd) || _announcement.DateStart == _announcement.DateEnd;
		string empty = string.Empty;
		if (IsSingleDate)
		{
			string text = _announcement.DateStart;
			if (string.IsNullOrEmpty(text))
			{
				text = _announcement.DateEnd;
			}
			empty = text;
		}
		else
		{
			TextObject textObject = GameTexts.FindText("str_LEFT_dash_RIGHT");
			textObject.SetTextVariable("LEFT", _announcement.DateStart);
			textObject.SetTextVariable("RIGHT", _announcement.DateEnd);
			empty = textObject.ToString();
		}
		if (!string.IsNullOrEmpty(empty))
		{
			empty = empty.Replace('\\', '.');
			empty = empty.Replace('/', '.');
		}
		DateText = empty;
	}

	private string GetAnnouncementTypeName(int announcementType)
	{
		return announcementType switch
		{
			1 => "Event", 
			2 => "Announcement", 
			_ => string.Empty, 
		};
	}
}
