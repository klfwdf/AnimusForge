using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.Overlay;

public class OverlayPopupWidget : Widget
{
	private bool _isOpen;

	private ImageIdentifierWidget _currentCharacterImageWidget;

	private TextWidget _locationTextWidget;

	private TextWidget _descriptionTextWidget;

	private TextWidget _powerTextWidget;

	private TextWidget _nameTextWidget;

	private Widget _relationBackgroundWidget;

	private ButtonWidget _closeButton;

	private ListPanel _actionButtonsList;

	[Editor(false)]
	public ImageIdentifierWidget CurrentCharacterImageWidget
	{
		get
		{
			return _currentCharacterImageWidget;
		}
		set
		{
			if (_currentCharacterImageWidget != value)
			{
				_currentCharacterImageWidget = value;
				OnPropertyChanged(value, "CurrentCharacterImageWidget");
			}
		}
	}

	[Editor(false)]
	public TextWidget LocationTextWidget
	{
		get
		{
			return _locationTextWidget;
		}
		set
		{
			if (_locationTextWidget != value)
			{
				_locationTextWidget = value;
				OnPropertyChanged(value, "LocationTextWidget");
			}
		}
	}

	[Editor(false)]
	public TextWidget NameTextWidget
	{
		get
		{
			return _nameTextWidget;
		}
		set
		{
			if (_nameTextWidget != value)
			{
				_nameTextWidget = value;
				OnPropertyChanged(value, "NameTextWidget");
			}
		}
	}

	[Editor(false)]
	public TextWidget PowerTextWidget
	{
		get
		{
			return _powerTextWidget;
		}
		set
		{
			if (_powerTextWidget != value)
			{
				_powerTextWidget = value;
				OnPropertyChanged(value, "PowerTextWidget");
			}
		}
	}

	[Editor(false)]
	public TextWidget DescriptionTextWidget
	{
		get
		{
			return _descriptionTextWidget;
		}
		set
		{
			if (_descriptionTextWidget != value)
			{
				_descriptionTextWidget = value;
				OnPropertyChanged(value, "DescriptionTextWidget");
			}
		}
	}

	[Editor(false)]
	public Widget RelationBackgroundWidget
	{
		get
		{
			return _relationBackgroundWidget;
		}
		set
		{
			if (_relationBackgroundWidget != value)
			{
				_relationBackgroundWidget = value;
				OnPropertyChanged(value, "RelationBackgroundWidget");
			}
		}
	}

	[Editor(false)]
	public ListPanel ActionButtonsList
	{
		get
		{
			return _actionButtonsList;
		}
		set
		{
			if (_actionButtonsList != value)
			{
				_actionButtonsList = value;
				OnPropertyChanged(value, "ActionButtonsList");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget CloseButton
	{
		get
		{
			return _closeButton;
		}
		set
		{
			if (_closeButton != value)
			{
				_closeButton?.ClickEventHandlers.Remove(OnCloseButtonClick);
				_closeButton = value;
				OnPropertyChanged(value, "CloseButton");
				_closeButton?.ClickEventHandlers.Add(OnCloseButtonClick);
			}
		}
	}

	public OverlayPopupWidget(UIContext context)
		: base(context)
	{
	}

	public void SetCurrentCharacter(GameMenuPartyItemButtonWidget item)
	{
		NameTextWidget.Text = item.Name;
		DescriptionTextWidget.Text = item.Description;
		LocationTextWidget.Text = item.Location;
		PowerTextWidget.Text = item.Power;
		if (item.CurrentCharacterImageWidget != null)
		{
			CurrentCharacterImageWidget.ImageId = item.CurrentCharacterImageWidget.ImageId;
			CurrentCharacterImageWidget.TextureProviderName = item.CurrentCharacterImageWidget.TextureProviderName;
			CurrentCharacterImageWidget.AdditionalArgs = item.CurrentCharacterImageWidget.AdditionalArgs;
		}
		if (!base.ParentWidget.IsVisible)
		{
			OpenPopup();
		}
	}

	private void OpenPopup()
	{
		base.ParentWidget.IsVisible = true;
		EventFired("OnOpen");
		_isOpen = true;
	}

	private void ClosePopup()
	{
		base.ParentWidget.IsVisible = false;
		EventFired("OnClose");
		_isOpen = false;
	}

	public void OnCloseButtonClick(Widget widget)
	{
		ClosePopup();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isOpen && !IsRecursivelyVisible())
		{
			ClosePopup();
		}
		else if (!_isOpen && IsRecursivelyVisible())
		{
			OpenPopup();
		}
		if (!(base.EventManager.LatestMouseDownWidget is GameMenuPartyItemButtonWidget) && base.EventManager.LatestMouseDownWidget != this && base.EventManager.LatestMouseDownWidget != _closeButton && base.ParentWidget.IsVisible && (!CheckIsMyChildRecursive(base.EventManager.LatestMouseDownWidget) || ActionButtonsList.CheckIsMyChildRecursive(base.EventManager.LatestMouseUpWidget)))
		{
			ClosePopup();
		}
	}
}
