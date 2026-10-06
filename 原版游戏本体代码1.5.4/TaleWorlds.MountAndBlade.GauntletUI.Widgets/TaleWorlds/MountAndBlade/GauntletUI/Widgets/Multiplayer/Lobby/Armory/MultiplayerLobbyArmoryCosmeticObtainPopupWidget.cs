using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Armory;

public class MultiplayerLobbyArmoryCosmeticObtainPopupWidget : Widget
{
	private int _obtainState = -1;

	private ButtonWidget _cancelButtonWidget;

	private ListPanel _itemPreviewListPanel;

	private ButtonWidget _actionButtonWidget;

	private Widget _resultSuccessfulIconWidget;

	private Widget _resultFailedIconWidget;

	private TextWidget _resultTextWidget;

	private Widget _loadingAnimationWidget;

	[Editor(false)]
	public int ObtainState
	{
		get
		{
			return _obtainState;
		}
		set
		{
			if (value != _obtainState)
			{
				_obtainState = value;
				OnPropertyChanged(value, "ObtainState");
				OnObtainStateChanged(value);
			}
		}
	}

	[Editor(false)]
	public ButtonWidget CancelButtonWidget
	{
		get
		{
			return _cancelButtonWidget;
		}
		set
		{
			if (value != _cancelButtonWidget)
			{
				_cancelButtonWidget = value;
				OnPropertyChanged(value, "CancelButtonWidget");
			}
		}
	}

	[Editor(false)]
	public ListPanel ItemPreviewListPanel
	{
		get
		{
			return _itemPreviewListPanel;
		}
		set
		{
			if (value != _itemPreviewListPanel)
			{
				_itemPreviewListPanel = value;
				OnPropertyChanged(value, "ItemPreviewListPanel");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget ActionButtonWidget
	{
		get
		{
			return _actionButtonWidget;
		}
		set
		{
			if (value != _actionButtonWidget)
			{
				_actionButtonWidget = value;
				OnPropertyChanged(value, "ActionButtonWidget");
			}
		}
	}

	[Editor(false)]
	public Widget ResultSuccessfulIconWidget
	{
		get
		{
			return _resultSuccessfulIconWidget;
		}
		set
		{
			if (value != _resultSuccessfulIconWidget)
			{
				_resultSuccessfulIconWidget = value;
				OnPropertyChanged(value, "ResultSuccessfulIconWidget");
			}
		}
	}

	[Editor(false)]
	public Widget ResultFailedIconWidget
	{
		get
		{
			return _resultFailedIconWidget;
		}
		set
		{
			if (value != _resultFailedIconWidget)
			{
				_resultFailedIconWidget = value;
				OnPropertyChanged(value, "ResultFailedIconWidget");
			}
		}
	}

	[Editor(false)]
	public TextWidget ResultTextWidget
	{
		get
		{
			return _resultTextWidget;
		}
		set
		{
			if (value != _resultTextWidget)
			{
				_resultTextWidget = value;
				OnPropertyChanged(value, "ResultTextWidget");
			}
		}
	}

	[Editor(false)]
	public Widget LoadingAnimationWidget
	{
		get
		{
			return _loadingAnimationWidget;
		}
		set
		{
			if (value != _loadingAnimationWidget)
			{
				_loadingAnimationWidget = value;
				OnPropertyChanged(value, "LoadingAnimationWidget");
			}
		}
	}

	public MultiplayerLobbyArmoryCosmeticObtainPopupWidget(UIContext context)
		: base(context)
	{
	}

	private void OnObtainStateChanged(int newState)
	{
		switch (newState)
		{
		case 0:
			ItemPreviewListPanel.IsVisible = true;
			ActionButtonWidget.IsEnabled = true;
			CancelButtonWidget.IsEnabled = true;
			ResultSuccessfulIconWidget.IsVisible = false;
			ResultFailedIconWidget.IsVisible = false;
			ResultTextWidget.IsVisible = false;
			LoadingAnimationWidget.IsVisible = false;
			break;
		case 1:
			LoadingAnimationWidget.IsVisible = true;
			CancelButtonWidget.IsEnabled = false;
			ActionButtonWidget.IsEnabled = false;
			ItemPreviewListPanel.IsVisible = false;
			ResultSuccessfulIconWidget.IsVisible = false;
			ResultFailedIconWidget.IsVisible = false;
			ResultTextWidget.IsVisible = false;
			break;
		case 2:
		case 3:
			CancelButtonWidget.IsEnabled = true;
			ActionButtonWidget.IsEnabled = true;
			ResultTextWidget.IsVisible = true;
			if (newState == 2)
			{
				ResultSuccessfulIconWidget.IsVisible = true;
			}
			else
			{
				ResultFailedIconWidget.IsVisible = true;
			}
			ItemPreviewListPanel.IsVisible = false;
			LoadingAnimationWidget.IsVisible = false;
			break;
		}
	}
}
