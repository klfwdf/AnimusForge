using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class MapNotificationItemBaseVM : ViewModel
{
	internal Action<MapNotificationItemBaseVM> OnRemove;

	internal Action<MapNotificationItemBaseVM> OnFocus;

	protected Action _onInspect;

	private readonly TextObject _removeHintTextObject = new TextObject("{=Bcs9s2tC}Right Click to Remove");

	private string _removeHintText;

	private InputKeyItemVM _removeInputKey;

	private bool _isFocused;

	private string _titleText;

	private string _descriptionText;

	private string _soundId;

	private bool _forceInspection;

	private string _notificationIdentifier = "Default";

	public INavigationHandler NavigationHandler { get; private set; }

	protected Action<CampaignVec2> FastMoveCameraToPosition { get; private set; }

	public InformationData Data { get; private set; }

	[DataSourceProperty]
	public InputKeyItemVM RemoveInputKey
	{
		get
		{
			return _removeInputKey;
		}
		set
		{
			if (value != _removeInputKey)
			{
				_removeInputKey = value;
				OnPropertyChangedWithValue(value, "RemoveInputKey");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFocused
	{
		get
		{
			return _isFocused;
		}
		set
		{
			if (value != _isFocused)
			{
				_isFocused = value;
				OnPropertyChangedWithValue(value, "IsFocused");
				OnFocus?.Invoke(this);
			}
		}
	}

	[DataSourceProperty]
	public string NotificationIdentifier
	{
		get
		{
			return _notificationIdentifier;
		}
		set
		{
			if (value != _notificationIdentifier)
			{
				_notificationIdentifier = value;
				OnPropertyChangedWithValue(value, "NotificationIdentifier");
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
	public bool ForceInspection
	{
		get
		{
			return _forceInspection;
		}
		set
		{
			if (value != _forceInspection)
			{
				Game current = Game.Current;
				if (current != null && !current.IsDevelopmentMode)
				{
					_forceInspection = value;
					OnPropertyChangedWithValue(value, "ForceInspection");
				}
			}
		}
	}

	[DataSourceProperty]
	public string DescriptionText
	{
		get
		{
			return _descriptionText;
		}
		set
		{
			if (value != _descriptionText)
			{
				_descriptionText = value;
				OnPropertyChangedWithValue(value, "DescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public string SoundId
	{
		get
		{
			return _soundId;
		}
		set
		{
			if (value != _soundId)
			{
				_soundId = value;
				OnPropertyChangedWithValue(value, "SoundId");
			}
		}
	}

	public MapNotificationItemBaseVM(InformationData data)
	{
		Data = data;
		ForceInspection = false;
		SoundId = data.SoundEventPath;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = Data.TitleText?.ToString();
		DescriptionText = Data.DescriptionText?.ToString();
		_removeHintText = _removeHintTextObject.ToString();
	}

	public void SetNavigationHandler(INavigationHandler navigationHandler)
	{
		NavigationHandler = navigationHandler;
	}

	public void SetFastMoveCameraToPosition(Action<CampaignVec2> fastMoveCameraToPosition)
	{
		FastMoveCameraToPosition = fastMoveCameraToPosition;
	}

	public void ExecuteAction()
	{
		_onInspect?.Invoke();
	}

	public void ExecuteRemove()
	{
		OnRemove?.Invoke(this);
		OnFocus?.Invoke(null);
	}

	public void ExecuteSetFocused()
	{
		IsFocused = true;
		OnFocus?.Invoke(this);
	}

	public void ExecuteSetUnfocused()
	{
		IsFocused = false;
		OnFocus?.Invoke(null);
	}

	public virtual void ManualRefreshRelevantStatus()
	{
	}

	internal void GoToMapPosition(CampaignVec2 position)
	{
		FastMoveCameraToPosition?.Invoke(position);
	}

	public void SetRemoveInputKey(HotKey hotKey)
	{
		RemoveInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
