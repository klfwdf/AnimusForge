using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Armory;

public class MultiplayerLobbyClassFilterFactionItemButtonWidget : ButtonWidget
{
	private string _baseBrushName = "MPLobby.ClassFilter.FactionButton";

	private string _culture;

	[Editor(false)]
	public string BaseBrushName
	{
		get
		{
			return _baseBrushName;
		}
		set
		{
			if (value != _baseBrushName)
			{
				_baseBrushName = value;
				OnPropertyChanged(value, "BaseBrushName");
				OnCultureChanged();
			}
		}
	}

	[Editor(false)]
	public string Culture
	{
		get
		{
			return _culture;
		}
		set
		{
			if (_culture != value)
			{
				_culture = value;
				OnPropertyChanged(value, "Culture");
				OnCultureChanged();
			}
		}
	}

	public MultiplayerLobbyClassFilterFactionItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	private void OnCultureChanged()
	{
		if (Culture != null)
		{
			string name = BaseBrushName + "." + Culture[0].ToString().ToUpper() + Culture.Substring(1).ToLower();
			base.Brush = base.Context.BrushFactory.GetBrush(name);
		}
	}
}
