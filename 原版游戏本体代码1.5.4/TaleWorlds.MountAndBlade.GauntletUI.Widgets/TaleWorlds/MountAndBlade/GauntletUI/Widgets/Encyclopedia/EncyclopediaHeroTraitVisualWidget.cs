using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Encyclopedia;

public class EncyclopediaHeroTraitVisualWidget : Widget
{
	private string _traitId;

	private int _traitValue;

	[Editor(false)]
	public string TraitId
	{
		get
		{
			return _traitId;
		}
		set
		{
			if (_traitId != value)
			{
				_traitId = value;
				OnPropertyChanged(value, "TraitId");
				SetVisual(value, TraitValue);
			}
		}
	}

	[Editor(false)]
	public int TraitValue
	{
		get
		{
			return _traitValue;
		}
		set
		{
			if (_traitValue != value)
			{
				_traitValue = value;
				OnPropertyChanged(value, "TraitValue");
				SetVisual(TraitId, value);
			}
		}
	}

	public EncyclopediaHeroTraitVisualWidget(UIContext context)
		: base(context)
	{
	}

	private void SetVisual(string traitCode, int value)
	{
		if (!string.IsNullOrEmpty(traitCode))
		{
			string name = "SPGeneral\\SPTraits\\" + traitCode.ToLower() + "_" + value;
			base.Sprite = base.Context.SpriteData.GetSprite(name);
			base.Sprite = base.Context.SpriteData.GetSprite(name);
			if (value < 0)
			{
				base.Color = new Color(0.738f, 0.113f, 0.113f);
			}
			else
			{
				base.Color = new Color(0.992f, 0.75f, 0.33f);
			}
		}
	}
}
