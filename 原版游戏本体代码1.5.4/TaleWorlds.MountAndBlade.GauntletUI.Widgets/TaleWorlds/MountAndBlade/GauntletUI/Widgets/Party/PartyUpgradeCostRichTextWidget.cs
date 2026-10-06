using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Party;

public class PartyUpgradeCostRichTextWidget : RichTextWidget
{
	private bool _requiresRefresh = true;

	private bool _isSufficient;

	private Color _normalColor;

	private Color _insufficientColor;

	[Editor(false)]
	public bool IsSufficient
	{
		get
		{
			return _isSufficient;
		}
		set
		{
			if (value != _isSufficient)
			{
				_isSufficient = value;
				OnPropertyChanged(value, "IsSufficient");
				_requiresRefresh = true;
			}
		}
	}

	public Color NormalColor
	{
		get
		{
			return _normalColor;
		}
		set
		{
			if (value != _normalColor)
			{
				_normalColor = value;
				_requiresRefresh = true;
			}
		}
	}

	public Color InsufficientColor
	{
		get
		{
			return _insufficientColor;
		}
		set
		{
			if (value != _insufficientColor)
			{
				_insufficientColor = value;
				_requiresRefresh = true;
			}
		}
	}

	public PartyUpgradeCostRichTextWidget(UIContext context)
		: base(context)
	{
		NormalColor = new Color(1f, 1f, 1f);
		InsufficientColor = new Color(0.753f, 0.071f, 0.098f);
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_requiresRefresh)
		{
			base.Brush.FontColor = (IsSufficient ? NormalColor : InsufficientColor);
			_requiresRefresh = false;
		}
	}
}
