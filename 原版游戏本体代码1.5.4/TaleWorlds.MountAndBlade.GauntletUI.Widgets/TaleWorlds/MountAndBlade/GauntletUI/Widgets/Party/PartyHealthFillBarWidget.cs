using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Party;

public class PartyHealthFillBarWidget : FillBar
{
	private readonly int FullHealthyLimit = 90;

	private readonly Color WoundedColor = Color.FromUint(4290199102u);

	private readonly Color HealthyColor = Color.FromUint(4291732560u);

	private readonly Color FullHealthyColor = Color.FromUint(4284921662u);

	private BrushLayer brushLayer;

	private int _health;

	private bool _isWounded;

	private TextWidget _healthText;

	[Editor(false)]
	public int Health
	{
		get
		{
			return _health;
		}
		set
		{
			if (_health != value)
			{
				_health = value;
				OnPropertyChanged(value, "Health");
				HealthUpdated();
			}
		}
	}

	[Editor(false)]
	public bool IsWounded
	{
		get
		{
			return _isWounded;
		}
		set
		{
			if (_isWounded != value)
			{
				_isWounded = value;
				OnPropertyChanged(value, "IsWounded");
				HealthUpdated();
			}
		}
	}

	[Editor(false)]
	public TextWidget HealthText
	{
		get
		{
			return _healthText;
		}
		set
		{
			if (_healthText != value)
			{
				_healthText = value;
				OnPropertyChanged(value, "HealthText");
				HealthUpdated();
			}
		}
	}

	public PartyHealthFillBarWidget(UIContext context)
		: base(context)
	{
	}

	private void HealthUpdated()
	{
		if (brushLayer == null)
		{
			brushLayer = base.Brush.GetLayer("DefaultFill");
		}
		int currentAmount = (base.InitialAmount = Health);
		base.CurrentAmount = currentAmount;
		if (IsWounded)
		{
			brushLayer.Color = WoundedColor;
		}
		else if (Health >= FullHealthyLimit)
		{
			brushLayer.Color = FullHealthyColor;
		}
		else
		{
			brushLayer.Color = HealthyColor;
		}
		if (HealthText != null)
		{
			HealthText.Text = Health + "%";
		}
	}
}
