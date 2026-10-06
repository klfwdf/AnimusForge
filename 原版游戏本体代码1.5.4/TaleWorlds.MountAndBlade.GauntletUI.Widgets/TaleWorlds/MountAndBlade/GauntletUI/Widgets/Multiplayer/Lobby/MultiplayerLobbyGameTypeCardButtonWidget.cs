using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyGameTypeCardButtonWidget : ButtonWidget
{
	private string _gameTypeId;

	private BrushWidget _gameTypeImageWidget;

	private Widget _checkboxWidget;

	[Editor(false)]
	public string GameTypeId
	{
		get
		{
			return _gameTypeId;
		}
		set
		{
			if (_gameTypeId != value)
			{
				_gameTypeId = value;
				OnPropertyChanged(value, "GameTypeId");
				UpdateGameTypeImage();
			}
		}
	}

	[Editor(false)]
	public BrushWidget GameTypeImageWidget
	{
		get
		{
			return _gameTypeImageWidget;
		}
		set
		{
			if (_gameTypeImageWidget != value)
			{
				_gameTypeImageWidget = value;
				OnPropertyChanged(value, "GameTypeImageWidget");
				UpdateGameTypeImage();
			}
		}
	}

	[Editor(false)]
	public Widget CheckboxWidget
	{
		get
		{
			return _checkboxWidget;
		}
		set
		{
			if (_checkboxWidget != value)
			{
				_checkboxWidget = value;
				OnPropertyChanged(value, "CheckboxWidget");
			}
		}
	}

	public MultiplayerLobbyGameTypeCardButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void RefreshState()
	{
		base.RefreshState();
		if (!base.OverrideDefaultStateSwitchingEnabled)
		{
			if (base.IsDisabled)
			{
				SetState(base.IsSelected ? "SelectedDisabled" : "Disabled");
			}
			else if (base.IsSelected)
			{
				SetState("Selected");
			}
			else if (base.IsPressed)
			{
				SetState("Pressed");
			}
			else if (base.IsHovered)
			{
				SetState("Hovered");
			}
			else
			{
				SetState("Default");
			}
		}
		if (base.UpdateChildrenStates)
		{
			for (int i = 0; i < base.ChildCount; i++)
			{
				GetChild(i).SetState(base.CurrentState);
			}
		}
	}

	private void UpdateGameTypeImage()
	{
		if (GameTypeImageWidget == null || string.IsNullOrEmpty(GameTypeId))
		{
			return;
		}
		Sprite sprite = base.Context.SpriteData.GetSprite("MPLobby\\Matchmaking\\GameTypeCards\\" + GameTypeId);
		foreach (Style style in GameTypeImageWidget.Brush.Styles)
		{
			StyleLayer[] layers = style.GetLayers();
			for (int i = 0; i < layers.Length; i++)
			{
				layers[i].Sprite = sprite;
			}
		}
	}
}
