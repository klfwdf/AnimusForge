using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.OrderOfBattle;

public class OrderOfBattleHeroButtonWidget : ButtonWidget
{
	private bool _isMainHero;

	private int _mainHeroHueFactor;

	private bool _canMainHeroAcceptEvents = true;

	public bool IsMainHero
	{
		get
		{
			return _isMainHero;
		}
		set
		{
			if (value != _isMainHero)
			{
				_isMainHero = value;
				OnPropertyChanged(value, "IsMainHero");
				UpdateMainHeroHueFactor();
				UpdateMainHeroAcceptEvents();
			}
		}
	}

	public int MainHeroHueFactor
	{
		get
		{
			return _mainHeroHueFactor;
		}
		set
		{
			if (value != _mainHeroHueFactor)
			{
				_mainHeroHueFactor = value;
				OnPropertyChanged(value, "MainHeroHueFactor");
				UpdateMainHeroHueFactor();
			}
		}
	}

	public bool CanMainHeroAcceptEvents
	{
		get
		{
			return _canMainHeroAcceptEvents;
		}
		set
		{
			if (value != _canMainHeroAcceptEvents)
			{
				_canMainHeroAcceptEvents = value;
				OnPropertyChanged(value, "CanMainHeroAcceptEvents");
				UpdateMainHeroAcceptEvents();
			}
		}
	}

	public OrderOfBattleHeroButtonWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateMainHeroHueFactor()
	{
		foreach (BrushLayer layer in base.Brush.Layers)
		{
			layer.HueFactor = (IsMainHero ? MainHeroHueFactor : 0);
		}
	}

	private void UpdateMainHeroAcceptEvents()
	{
		base.DoNotAcceptEvents = IsMainHero && !CanMainHeroAcceptEvents;
	}
}
