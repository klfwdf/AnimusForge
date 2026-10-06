using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Scoreboard;

public class MultiplayerScoreboardSideWidget : Widget
{
	private ContainerItemDescription _nameColumnItemDescription;

	private float _nameColumnWidthRatio = 1f;

	private ListPanel _titlesListPanel;

	private string _cultureId;

	private Color _cultureColor;

	private bool _useSecondary;

	public Color CultureColor
	{
		get
		{
			return _cultureColor;
		}
		set
		{
			if (value != _cultureColor)
			{
				_cultureColor = value;
				OnPropertyChanged(value, "CultureColor");
				UpdateBackgroundColors();
			}
		}
	}

	public string CultureId
	{
		get
		{
			return _cultureId;
		}
		set
		{
			if (value != _cultureId)
			{
				_cultureId = value;
				OnPropertyChanged(value, "CultureId");
				UpdateBackgroundColors();
			}
		}
	}

	public bool UseSecondary
	{
		get
		{
			return _useSecondary;
		}
		set
		{
			if (value != _useSecondary)
			{
				_useSecondary = value;
				OnPropertyChanged(value, "UseSecondary");
				UpdateBackgroundColors();
			}
		}
	}

	public float NameColumnWidthRatio
	{
		get
		{
			return _nameColumnWidthRatio;
		}
		set
		{
			if (value != _nameColumnWidthRatio)
			{
				_nameColumnWidthRatio = value;
				OnPropertyChanged(value, "NameColumnWidthRatio");
				AvatarColumnWidthRatioUpdated();
			}
		}
	}

	public ListPanel TitlesListPanel
	{
		get
		{
			return _titlesListPanel;
		}
		set
		{
			if (value != _titlesListPanel)
			{
				_titlesListPanel = value;
				OnPropertyChanged(value, "TitlesListPanel");
				AvatarColumnWidthRatioUpdated();
			}
		}
	}

	public MultiplayerScoreboardSideWidget(UIContext context)
		: base(context)
	{
		_nameColumnItemDescription = new ContainerItemDescription();
		_nameColumnItemDescription.WidgetIndex = 3;
	}

	private void AvatarColumnWidthRatioUpdated()
	{
		if (TitlesListPanel != null)
		{
			_nameColumnItemDescription.WidthStretchRatio = NameColumnWidthRatio;
			TitlesListPanel.AddItemDescription(_nameColumnItemDescription);
		}
	}

	private void UpdateBackgroundColors()
	{
		if (!string.IsNullOrEmpty(CultureId))
		{
			base.Color = CultureColor;
		}
	}
}
