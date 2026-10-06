using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Information;

public class GameNotificationItemVM : ViewModel
{
	public readonly int Priority;

	public readonly bool IsDialog;

	public readonly MBInformationManager.DialogNotificationHandle Handle;

	private string _gameNotificationText;

	private string _characterNameText;

	private string _notificationSoundId;

	private string _dialogSoundPath;

	private int _extraTimeInMs;

	private CharacterImageIdentifierVM _announcer;

	[DataSourceProperty]
	public int ExtraTimeInMs
	{
		get
		{
			return _extraTimeInMs;
		}
		set
		{
			if (value != _extraTimeInMs)
			{
				_extraTimeInMs = value;
				OnPropertyChangedWithValue(value, "ExtraTimeInMs");
			}
		}
	}

	[DataSourceProperty]
	public string GameNotificationText
	{
		get
		{
			return _gameNotificationText;
		}
		set
		{
			if (value != _gameNotificationText)
			{
				_gameNotificationText = value;
				OnPropertyChangedWithValue(value, "GameNotificationText");
			}
		}
	}

	[DataSourceProperty]
	public string CharacterNameText
	{
		get
		{
			return _characterNameText;
		}
		set
		{
			if (value != _characterNameText)
			{
				_characterNameText = value;
				OnPropertyChangedWithValue(value, "CharacterNameText");
			}
		}
	}

	[DataSourceProperty]
	public string NotificationSoundId
	{
		get
		{
			return _notificationSoundId;
		}
		set
		{
			if (value != _notificationSoundId)
			{
				_notificationSoundId = value;
				OnPropertyChangedWithValue(value, "NotificationSoundId");
			}
		}
	}

	[DataSourceProperty]
	public string DialogSoundPath
	{
		get
		{
			return _dialogSoundPath;
		}
		set
		{
			if (value != _dialogSoundPath)
			{
				_dialogSoundPath = value;
				OnPropertyChangedWithValue(value, "DialogSoundPath");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM Announcer
	{
		get
		{
			return _announcer;
		}
		set
		{
			if (value != _announcer)
			{
				_announcer = value;
				OnPropertyChangedWithValue(value, "Announcer");
			}
		}
	}

	public GameNotificationItemVM(string notificationText, int extraTimeInMs, BasicCharacterObject announcerCharacter, Equipment characterEquipment, string soundId, int priority, bool isDialog, string dialogSoundPath)
	{
		GameNotificationText = notificationText;
		NotificationSoundId = soundId;
		Announcer = ((announcerCharacter != null) ? new CharacterImageIdentifierVM(CharacterCode.CreateFrom(announcerCharacter, characterEquipment)) : new CharacterImageIdentifierVM(null));
		CharacterNameText = ((announcerCharacter != null) ? announcerCharacter.Name.ToString() : "");
		ExtraTimeInMs = extraTimeInMs;
		Priority = priority;
		IsDialog = isDialog;
		DialogSoundPath = dialogSoundPath;
		if (IsDialog)
		{
			Handle = new MBInformationManager.DialogNotificationHandle();
		}
	}
}
