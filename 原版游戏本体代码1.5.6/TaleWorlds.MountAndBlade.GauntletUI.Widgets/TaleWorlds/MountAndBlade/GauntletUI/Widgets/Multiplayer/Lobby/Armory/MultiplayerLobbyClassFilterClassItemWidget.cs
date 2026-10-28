using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Armory;

public class MultiplayerLobbyClassFilterClassItemWidget : ToggleStateButtonWidget
{
	private string _troopType;

	private Color _cultureColor;

	private Brush _iconBrush;

	private Widget _iconWidget;

	private Widget _factionColorWidget;

	[Editor(false)]
	public string TroopType
	{
		get
		{
			return _troopType;
		}
		set
		{
			if (value != _troopType)
			{
				_troopType = value;
				OnPropertyChanged(value, "TroopType");
				UpdateIcon();
			}
		}
	}

	[Editor(false)]
	public Color CultureColor
	{
		get
		{
			return _cultureColor;
		}
		set
		{
			if (_cultureColor != value)
			{
				_cultureColor = value;
				OnPropertyChanged(value, "CultureColor");
				SetFactionColor();
			}
		}
	}

	[Editor(false)]
	public Brush IconBrush
	{
		get
		{
			return _iconBrush;
		}
		set
		{
			if (_iconBrush != value)
			{
				_iconBrush = value;
				OnPropertyChanged(value, "IconBrush");
			}
		}
	}

	[DataSourceProperty]
	public Widget IconWidget
	{
		get
		{
			return _iconWidget;
		}
		set
		{
			if (value != _iconWidget)
			{
				_iconWidget = value;
				OnPropertyChanged(value, "IconWidget");
				UpdateIcon();
			}
		}
	}

	[Editor(false)]
	public Widget FactionColorWidget
	{
		get
		{
			return _factionColorWidget;
		}
		set
		{
			if (_factionColorWidget != value)
			{
				_factionColorWidget = value;
				OnPropertyChanged(value, "FactionColorWidget");
				SetFactionColor();
			}
		}
	}

	public MultiplayerLobbyClassFilterClassItemWidget(UIContext context)
		: base(context)
	{
	}

	private void SetFactionColor()
	{
		if (FactionColorWidget != null)
		{
			FactionColorWidget.Color = CultureColor;
		}
	}

	private void UpdateIcon()
	{
		if (!string.IsNullOrEmpty(TroopType) && _iconWidget != null)
		{
			IconWidget.Sprite = IconBrush?.GetLayer(TroopType)?.Sprite;
		}
	}
}
