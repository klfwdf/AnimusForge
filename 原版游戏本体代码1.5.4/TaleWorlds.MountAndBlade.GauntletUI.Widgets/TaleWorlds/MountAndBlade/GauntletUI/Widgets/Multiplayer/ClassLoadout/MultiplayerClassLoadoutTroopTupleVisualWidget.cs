using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.ClassLoadout;

public class MultiplayerClassLoadoutTroopTupleVisualWidget : Widget
{
	private bool _initialized;

	private string _factionCode;

	private string _troopTypeCode;

	public string FactionCode
	{
		get
		{
			return _factionCode;
		}
		set
		{
			if (value != _factionCode)
			{
				_factionCode = value;
				OnPropertyChanged(value, "FactionCode");
			}
		}
	}

	public string TroopTypeCode
	{
		get
		{
			return _troopTypeCode;
		}
		set
		{
			if (value != _troopTypeCode)
			{
				_troopTypeCode = value;
				OnPropertyChanged(value, "TroopTypeCode");
			}
		}
	}

	public MultiplayerClassLoadoutTroopTupleVisualWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			base.Sprite = base.Context.SpriteData.GetSprite("MPClassLoadout\\TroopTupleImages\\" + TroopTypeCode + "1");
			base.Sprite = base.Sprite;
			base.SuggestedWidth = base.Sprite.Width;
			base.SuggestedHeight = base.Sprite.Height;
			_initialized = true;
		}
	}
}
