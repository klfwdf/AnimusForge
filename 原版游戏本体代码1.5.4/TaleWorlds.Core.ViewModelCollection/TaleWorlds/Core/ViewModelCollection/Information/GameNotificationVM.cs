using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Information;

public class GameNotificationVM : ViewModel
{
	private readonly List<GameNotificationItemVM> _items;

	private const float MinimumDisplayTimeInSeconds = 0.6f;

	private const float ExtraDisplayTimeInSeconds = 1f;

	private GameNotificationItemVM _currentNotification;

	private bool _gotNotification;

	private int _notificationId;

	private float _currentNotificationDurationInSeconds;

	private bool _isPaused;

	private bool _mustFadeOutCurrentNotification;

	private float _currentNotificationFadeOutDelayInSeconds;

	private float CurrentNotificationOnScreenTime
	{
		get
		{
			float num = 0.6f;
			num += (float)CurrentNotification.ExtraTimeInMs / 1000f;
			int numberOfWords = GetNumberOfWords(CurrentNotification.GameNotificationText);
			if (numberOfWords > 4)
			{
				num += (float)(numberOfWords - 4) / 5f;
			}
			if (CurrentNotification.IsDialog)
			{
				num += 10000f;
			}
			return num + 1f / (float)(_items.Count + 1);
		}
	}

	[DataSourceProperty]
	public GameNotificationItemVM CurrentNotification
	{
		get
		{
			return _currentNotification;
		}
		set
		{
			if (_currentNotification != value)
			{
				_currentNotification = value;
				NotificationId++;
				OnPropertyChangedWithValue(value, "CurrentNotification");
				this.CurrentNotificationChanged?.Invoke(value);
			}
		}
	}

	[DataSourceProperty]
	public bool GotNotification
	{
		get
		{
			return _gotNotification;
		}
		set
		{
			if (value != _gotNotification)
			{
				_gotNotification = value;
				OnPropertyChangedWithValue(value, "GotNotification");
			}
		}
	}

	[DataSourceProperty]
	public int NotificationId
	{
		get
		{
			return _notificationId;
		}
		set
		{
			if (value != _notificationId)
			{
				_notificationId = value;
				OnPropertyChangedWithValue(value, "NotificationId");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentNotificationDurationInSeconds
	{
		get
		{
			return _currentNotificationDurationInSeconds;
		}
		set
		{
			if (value != _currentNotificationDurationInSeconds)
			{
				_currentNotificationDurationInSeconds = value;
				OnPropertyChangedWithValue(value, "CurrentNotificationDurationInSeconds");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPaused
	{
		get
		{
			return _isPaused;
		}
		set
		{
			if (value != _isPaused)
			{
				_isPaused = value;
				OnPropertyChangedWithValue(value, "IsPaused");
			}
		}
	}

	[DataSourceProperty]
	public bool MustFadeOutCurrentNotification
	{
		get
		{
			return _mustFadeOutCurrentNotification;
		}
		set
		{
			if (value != _mustFadeOutCurrentNotification)
			{
				_mustFadeOutCurrentNotification = value;
				OnPropertyChangedWithValue(value, "MustFadeOutCurrentNotification");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentNotificationFadeOutDelayInSeconds
	{
		get
		{
			return _currentNotificationFadeOutDelayInSeconds;
		}
		set
		{
			if (value != _currentNotificationFadeOutDelayInSeconds)
			{
				_currentNotificationFadeOutDelayInSeconds = value;
				OnPropertyChangedWithValue(value, "CurrentNotificationFadeOutDelayInSeconds");
			}
		}
	}

	public event Action<GameNotificationItemVM> CurrentNotificationChanged;

	public void FadeOutCurrentNotification(bool useExtraDisplayTime = false)
	{
		if (GotNotification)
		{
			MustFadeOutCurrentNotification = true;
			if (useExtraDisplayTime)
			{
				CurrentNotificationFadeOutDelayInSeconds = (float)CurrentNotification.ExtraTimeInMs / 1000f;
			}
		}
	}

	public void SkipCurrentNotification()
	{
		if (_items.Count > 0)
		{
			CurrentNotification = _items[0];
			_items.RemoveAt(0);
			CurrentNotificationDurationInSeconds = CurrentNotificationOnScreenTime;
		}
		else
		{
			GotNotification = false;
		}
	}

	public GameNotificationVM()
	{
		_items = new List<GameNotificationItemVM>();
		CurrentNotification = new GameNotificationItemVM("NULL", 0, null, null, "NULL", 0, isDialog: false, null);
		GotNotification = false;
	}

	public void ClearNotifications()
	{
		_items.Clear();
		GotNotification = false;
	}

	public MBInformationManager.DialogNotificationHandle AddDialogNotification(TextObject text, int extraTimeInMs, BasicCharacterObject announcerCharacter, Equipment equipment, MBInformationManager.NotificationPriority priority, string dialogSoundPath)
	{
		GameNotificationItemVM gameNotificationItemVM = new GameNotificationItemVM(text.ToString(), extraTimeInMs, announcerCharacter, equipment, null, (int)priority, isDialog: true, dialogSoundPath);
		if (GotNotification && CurrentNotification.GameNotificationText == text.ToString())
		{
			return CurrentNotification.Handle;
		}
		if (_items.Any((GameNotificationItemVM i) => i.GameNotificationText == text.ToString()))
		{
			return _items.First((GameNotificationItemVM i) => i.GameNotificationText == text.ToString()).Handle;
		}
		if (GotNotification && CurrentNotification.Priority >= (int)priority)
		{
			int index = _items.FindLastIndex((GameNotificationItemVM i) => i.Priority >= (int)priority) + 1;
			_items.Insert(index, gameNotificationItemVM);
		}
		else
		{
			CurrentNotification = gameNotificationItemVM;
			CurrentNotificationDurationInSeconds = CurrentNotificationOnScreenTime;
			GotNotification = true;
		}
		return gameNotificationItemVM.Handle;
	}

	public MBInformationManager.NotificationStatus GetStatusOfDialogNotification(MBInformationManager.DialogNotificationHandle handle)
	{
		if (handle == null)
		{
			return MBInformationManager.NotificationStatus.Inactive;
		}
		if (GotNotification && CurrentNotification.Handle == handle)
		{
			return MBInformationManager.NotificationStatus.CurrentlyActive;
		}
		if (_items.Any((GameNotificationItemVM i) => i.Handle == handle))
		{
			return MBInformationManager.NotificationStatus.InQueue;
		}
		return MBInformationManager.NotificationStatus.Inactive;
	}

	public void ClearDialogNotification(MBInformationManager.DialogNotificationHandle handle, bool fadeOut)
	{
		if (handle == null)
		{
			return;
		}
		_items.RemoveAll((GameNotificationItemVM x) => x.Handle == handle);
		if (GotNotification && CurrentNotification.Handle == handle)
		{
			if (fadeOut)
			{
				FadeOutCurrentNotification();
			}
			else
			{
				SkipCurrentNotification();
			}
		}
	}

	public bool GetIsAnyDialogNotificationActiveOrQueued()
	{
		if (!GotNotification || !CurrentNotification.IsDialog)
		{
			return _items.Any((GameNotificationItemVM x) => x.IsDialog);
		}
		return true;
	}

	public void ClearAllDialogNotifications(bool fadeOut)
	{
		_items.RemoveAll((GameNotificationItemVM x) => x.IsDialog);
		if (GotNotification && CurrentNotification.IsDialog)
		{
			if (fadeOut)
			{
				FadeOutCurrentNotification();
			}
			else
			{
				SkipCurrentNotification();
			}
		}
	}

	public void AddGameNotification(string notificationText, int extraTimeInMs, BasicCharacterObject announcerCharacter, Equipment equipment, string soundId)
	{
		if (string.IsNullOrEmpty(notificationText))
		{
			Debug.FailedAssert("Quick information message is empty", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core.ViewModelCollection\\Information\\GameNotificationVM.cs", "AddGameNotification", 183);
		}
		GameNotificationItemVM gameNotificationItemVM = new GameNotificationItemVM(notificationText, extraTimeInMs, announcerCharacter, equipment, soundId, 0, isDialog: false, null);
		if ((!GotNotification || CurrentNotification.GameNotificationText != notificationText) && !_items.Any((GameNotificationItemVM i) => i.GameNotificationText == notificationText))
		{
			if (GotNotification)
			{
				_items.Add(gameNotificationItemVM);
				return;
			}
			CurrentNotification = gameNotificationItemVM;
			CurrentNotificationDurationInSeconds = CurrentNotificationOnScreenTime;
			GotNotification = true;
		}
	}

	private int GetNumberOfWords(string text)
	{
		string text2 = text.Trim();
		int num = 0;
		int i = 0;
		while (i < text2.Length)
		{
			for (; i < text2.Length && !char.IsWhiteSpace(text2[i]); i++)
			{
			}
			num++;
			for (; i < text2.Length && char.IsWhiteSpace(text2[i]); i++)
			{
			}
		}
		return num;
	}
}
