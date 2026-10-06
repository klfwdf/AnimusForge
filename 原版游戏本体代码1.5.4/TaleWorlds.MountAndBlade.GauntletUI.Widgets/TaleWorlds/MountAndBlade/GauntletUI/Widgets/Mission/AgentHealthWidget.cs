using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class AgentHealthWidget : Widget
{
	public class HealthDropData
	{
		public BrushWidget Widget;

		public float LifeTime;

		public HealthDropData(BrushWidget widget, float lifeTime)
		{
			Widget = widget;
			LifeTime = lifeTime;
		}
	}

	private float AnimationDelay = 0.2f;

	private float AnimationDuration = 0.8f;

	private float _previousHealthRatio;

	private List<HealthDropData> _healthDrops;

	private int _health;

	private int _maxHealth;

	private bool _showHealthBar;

	private FillBarWidget _healthBar;

	private Widget _healthDropContainer;

	private Brush _healthDropBrush;

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
			}
		}
	}

	[Editor(false)]
	public int MaxHealth
	{
		get
		{
			return _maxHealth;
		}
		set
		{
			if (_maxHealth != value)
			{
				_maxHealth = value;
				OnPropertyChanged(value, "MaxHealth");
			}
		}
	}

	[Editor(false)]
	public FillBarWidget HealthBar
	{
		get
		{
			return _healthBar;
		}
		set
		{
			if (_healthBar != value)
			{
				_healthBar = value;
				OnPropertyChanged(value, "HealthBar");
			}
		}
	}

	[Editor(false)]
	public Widget HealthDropContainer
	{
		get
		{
			return _healthDropContainer;
		}
		set
		{
			if (_healthDropContainer != value)
			{
				_healthDropContainer = value;
				OnPropertyChanged(value, "HealthDropContainer");
			}
		}
	}

	[Editor(false)]
	public Brush HealthDropBrush
	{
		get
		{
			return _healthDropBrush;
		}
		set
		{
			if (_healthDropBrush != value)
			{
				_healthDropBrush = value;
				OnPropertyChanged(value, "HealthDropBrush");
			}
		}
	}

	[Editor(false)]
	public bool ShowHealthBar
	{
		get
		{
			return _showHealthBar;
		}
		set
		{
			if (_showHealthBar != value)
			{
				_showHealthBar = value;
				OnPropertyChanged(value, "ShowHealthBar");
			}
		}
	}

	public AgentHealthWidget(UIContext context)
		: base(context)
	{
		_healthDrops = new List<HealthDropData>();
		CheckVisibility();
	}

	private void CreateHealthDrop(Widget container, float previousHealthRatio, float currentHealthRatio)
	{
		float num = container.Size.X / base._scaleToUse;
		float suggestedWidth = Mathf.Ceil(num * (previousHealthRatio - currentHealthRatio));
		float positionXOffset = Mathf.Floor(num * currentHealthRatio);
		BrushWidget brushWidget = new BrushWidget(base.Context);
		brushWidget.WidthSizePolicy = SizePolicy.Fixed;
		brushWidget.HeightSizePolicy = SizePolicy.Fixed;
		brushWidget.Brush = HealthDropBrush;
		brushWidget.SuggestedWidth = suggestedWidth;
		brushWidget.SuggestedHeight = brushWidget.ReadOnlyBrush.Sprite.Height;
		brushWidget.HorizontalAlignment = HorizontalAlignment.Left;
		brushWidget.VerticalAlignment = VerticalAlignment.Center;
		brushWidget.PositionXOffset = positionXOffset;
		brushWidget.ParentWidget = container;
		_healthDrops.Add(new HealthDropData(brushWidget, AnimationDelay + AnimationDuration));
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (HealthBar != null)
		{
			HealthBar.MaxAmount = MaxHealth;
			HealthBar.InitialAmount = Health;
		}
		if (HealthDropContainer != null)
		{
			HandleHealthDrops(dt);
		}
		CheckVisibility();
	}

	private void HandleHealthDrops(float dt)
	{
		for (int num = _healthDrops.Count - 1; num >= 0; num--)
		{
			HealthDropData healthDropData = _healthDrops[num];
			healthDropData.LifeTime -= dt;
			if (healthDropData.LifeTime <= 0f)
			{
				HealthDropContainer.RemoveChild(healthDropData.Widget);
				_healthDrops.RemoveAt(num);
			}
			else
			{
				float alphaFactor = Mathf.Min(1f, healthDropData.LifeTime / AnimationDuration);
				healthDropData.Widget.Brush.AlphaFactor = alphaFactor;
			}
		}
		float value = ((MaxHealth != 0) ? ((float)Health / (float)MaxHealth) : 0f);
		value = MathF.Clamp(value, 0f, 1f);
		if (value == _previousHealthRatio)
		{
			return;
		}
		if (value > _previousHealthRatio)
		{
			for (int num2 = _healthDrops.Count - 1; num2 >= 0; num2--)
			{
				HealthDropContainer.RemoveChild(_healthDrops[num2].Widget);
				_healthDrops.RemoveAt(num2);
			}
		}
		else if (base.IsVisible)
		{
			CreateHealthDrop(HealthDropContainer, _previousHealthRatio, value);
		}
		_previousHealthRatio = value;
	}

	private void CheckVisibility()
	{
		bool flag = ShowHealthBar;
		if (flag)
		{
			flag = (float)_health > 0f || _healthDrops.Count > 0;
		}
		base.IsVisible = flag;
	}
}
