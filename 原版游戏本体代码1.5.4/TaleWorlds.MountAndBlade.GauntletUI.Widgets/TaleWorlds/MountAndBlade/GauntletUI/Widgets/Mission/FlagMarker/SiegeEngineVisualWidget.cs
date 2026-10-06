using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.FlagMarker;

public class SiegeEngineVisualWidget : Widget
{
	private bool _hasVisualSet;

	private Sprite _fallbackSprite;

	private const string SpritePathPrefix = "MPHud\\SiegeMarkers\\";

	private string _engineID = string.Empty;

	private Widget _outlineWidget;

	private Widget _iconWidget;

	[Editor(false)]
	public string EngineID
	{
		get
		{
			return _engineID;
		}
		set
		{
			if (value != _engineID)
			{
				_engineID = value;
				OnPropertyChanged(value, "EngineID");
			}
		}
	}

	public Widget OutlineWidget
	{
		get
		{
			return _outlineWidget;
		}
		set
		{
			if (_outlineWidget != value)
			{
				_outlineWidget = value;
				OnPropertyChanged(value, "OutlineWidget");
			}
		}
	}

	public Widget IconWidget
	{
		get
		{
			return _iconWidget;
		}
		set
		{
			if (_iconWidget != value)
			{
				_iconWidget = value;
				OnPropertyChanged(value, "IconWidget");
			}
		}
	}

	public SiegeEngineVisualWidget(UIContext context)
		: base(context)
	{
		_fallbackSprite = GetSprite("BlankWhiteCircle");
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_hasVisualSet && EngineID != string.Empty && OutlineWidget != null && IconWidget != null)
		{
			string text = string.Empty;
			switch (EngineID)
			{
			case "ram":
				text = "battering_ram";
				break;
			case "ballista":
			case "fire_ballista":
				text = "ballista";
				break;
			case "catapult":
			case "fire_catapult":
			case "onager":
			case "fire_onager":
				text = "catapult";
				break;
			case "ladder":
				text = "ladder";
				break;
			case "trebuchet":
				text = "trebuchet";
				break;
			case "siege_tower_level1":
			case "siege_tower_level2":
			case "siege_tower_level3":
				text = "siege_tower";
				break;
			}
			OutlineWidget.Sprite = ((text == string.Empty) ? _fallbackSprite : GetSprite("MPHud\\SiegeMarkers\\" + text + "_outline"));
			IconWidget.Sprite = ((text == string.Empty) ? _fallbackSprite : GetSprite("MPHud\\SiegeMarkers\\" + text));
			_hasVisualSet = true;
		}
	}

	private Sprite GetSprite(string path)
	{
		return base.Context.SpriteData.GetSprite(path);
	}
}
