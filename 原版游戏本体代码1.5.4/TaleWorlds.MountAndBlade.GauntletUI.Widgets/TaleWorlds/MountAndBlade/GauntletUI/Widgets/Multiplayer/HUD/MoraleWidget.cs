using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.HUD;

public class MoraleWidget : Widget
{
	private class MoraleItemWidget
	{
		public Widget ParentWidget { get; private set; }

		public Widget MaskWidget { get; private set; }

		public BrushWidget ItemWidget { get; private set; }

		public BrushWidget ItemGlowWidget { get; private set; }

		public Widget ItemBackgroundWidget { get; private set; }

		public MoraleItemWidget(Widget parentWidget, Widget maskWidget, BrushWidget itemWidget, BrushWidget itemGlowWidget, Widget itemBackgroundWidget)
		{
			ParentWidget = parentWidget;
			MaskWidget = maskWidget;
			ItemWidget = itemWidget;
			ItemGlowWidget = itemGlowWidget;
			ItemBackgroundWidget = itemBackgroundWidget;
		}

		public void SetFillAmount(float fill, int fillMargin)
		{
			bool flag = MBMath.ApproximatelyEquals(fill, 0f);
			bool flag2 = MBMath.ApproximatelyEquals(fill, 1f);
			if (flag)
			{
				MaskWidget.SuggestedHeight = 0f;
			}
			else if (flag2)
			{
				MaskWidget.SuggestedHeight = ItemWidget.SuggestedHeight;
			}
			else
			{
				int num = MathF.Floor(ItemWidget.SuggestedHeight - (float)(fillMargin * 2));
				MaskWidget.SuggestedHeight = (float)fillMargin + (float)num * fill;
			}
			ItemWidget.IsVisible = !flag;
			ItemGlowWidget.IsVisible = flag2;
		}
	}

	private const int ItemCount = 10;

	private const float ItemDistance = 28f;

	private const int ItemWidth = 39;

	private const int ItemHeight = 38;

	private const int FillMargin = 12;

	private MoraleItemWidget[] _moraleItemWidgets;

	private bool _initialized;

	private bool _triggerAnimations;

	private int _animWaitFrame;

	private string _currentStateName;

	private Color _teamColor;

	private Color _teamColorSecondary;

	private int _increaseLevel;

	private int _moralePercentage;

	private Widget _container;

	private Widget _itemContainer;

	private Brush _itemBrush;

	private Brush _itemGlowBrush;

	private Brush _itemBackgroundBrush;

	private MoraleArrowBrushWidget _flowArrowWidget;

	private bool _extendToLeft;

	private bool _areMoralesIndependent;

	private string _teamColorAsStr;

	private string _teamColorAsStrSecondary;

	[DataSourceProperty]
	public int IncreaseLevel
	{
		get
		{
			return _increaseLevel;
		}
		set
		{
			if (_increaseLevel != value)
			{
				_increaseLevel = value;
				OnPropertyChanged(value, "IncreaseLevel");
				UpdateArrows(_increaseLevel);
			}
		}
	}

	[DataSourceProperty]
	public int MoralePercentage
	{
		get
		{
			return _moralePercentage;
		}
		set
		{
			if (_moralePercentage != value)
			{
				_moralePercentage = value;
				OnPropertyChanged(value, "MoralePercentage");
			}
		}
	}

	[DataSourceProperty]
	public Widget Container
	{
		get
		{
			return _container;
		}
		set
		{
			if (_container != value)
			{
				_container = value;
				OnPropertyChanged(value, "Container");
			}
		}
	}

	[DataSourceProperty]
	public Widget ItemContainer
	{
		get
		{
			return _itemContainer;
		}
		set
		{
			if (_itemContainer != value)
			{
				_itemContainer = value;
				OnPropertyChanged(value, "ItemContainer");
			}
		}
	}

	[DataSourceProperty]
	public Brush ItemBrush
	{
		get
		{
			return _itemBrush;
		}
		set
		{
			if (_itemBrush != value)
			{
				_itemBrush = value;
				OnPropertyChanged(value, "ItemBrush");
			}
		}
	}

	[DataSourceProperty]
	public Brush ItemGlowBrush
	{
		get
		{
			return _itemGlowBrush;
		}
		set
		{
			if (_itemGlowBrush != value)
			{
				_itemGlowBrush = value;
				OnPropertyChanged(value, "ItemGlowBrush");
			}
		}
	}

	[DataSourceProperty]
	public Brush ItemBackgroundBrush
	{
		get
		{
			return _itemBackgroundBrush;
		}
		set
		{
			if (_itemBackgroundBrush != value)
			{
				_itemBackgroundBrush = value;
				OnPropertyChanged(value, "ItemBackgroundBrush");
			}
		}
	}

	[DataSourceProperty]
	public string TeamColorAsStr
	{
		get
		{
			return _teamColorAsStr;
		}
		set
		{
			if (_teamColorAsStr != value && value != null)
			{
				_teamColorAsStr = value;
				OnPropertyChanged(value, "TeamColorAsStr");
				_teamColor = Color.ConvertStringToColor(value);
				SetItemWidgetColors(_teamColor);
			}
		}
	}

	[DataSourceProperty]
	public string TeamColorAsStrSecondary
	{
		get
		{
			return _teamColorAsStrSecondary;
		}
		set
		{
			if (_teamColorAsStrSecondary != value && value != null)
			{
				_teamColorAsStrSecondary = value;
				OnPropertyChanged(value, "TeamColorAsStrSecondary");
				_teamColorSecondary = Color.ConvertStringToColor(value);
				SetItemGlowWidgetColors(_teamColorSecondary);
			}
		}
	}

	[DataSourceProperty]
	public MoraleArrowBrushWidget FlowArrowWidget
	{
		get
		{
			return _flowArrowWidget;
		}
		set
		{
			if (_flowArrowWidget != value)
			{
				_flowArrowWidget = value;
				OnPropertyChanged(value, "FlowArrowWidget");
			}
		}
	}

	[DataSourceProperty]
	public bool ExtendToLeft
	{
		get
		{
			return _extendToLeft;
		}
		set
		{
			if (_extendToLeft != value)
			{
				_extendToLeft = value;
				OnPropertyChanged(value, "ExtendToLeft");
			}
		}
	}

	[DataSourceProperty]
	public bool AreMoralesIndependent
	{
		get
		{
			return _areMoralesIndependent;
		}
		set
		{
			if (_areMoralesIndependent != value)
			{
				_areMoralesIndependent = value;
				OnPropertyChanged(value, "AreMoralesIndependent");
			}
		}
	}

	public MoraleWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnConnectedToRoot()
	{
		base.OnConnectedToRoot();
		_moraleItemWidgets = CreateItemWidgets(ItemContainer);
		SetItemWidgetColors(_teamColor);
		SetItemGlowWidgetColors(_teamColorSecondary);
		RestartAnimations();
	}

	protected override void OnUpdate(float dt)
	{
		if (_triggerAnimations)
		{
			if (_animWaitFrame >= 1)
			{
				HandleAnimation();
				_triggerAnimations = false;
				_animWaitFrame = 0;
			}
			else
			{
				_animWaitFrame++;
			}
		}
		if (!_initialized)
		{
			FlowArrowWidget.LeftSideArrow = ExtendToLeft;
			_initialized = true;
		}
	}

	private void RestartAnimations()
	{
		_triggerAnimations = true;
		_animWaitFrame = 0;
	}

	private void UpdateArrows(int flowLevel)
	{
		if (Container != null && FlowArrowWidget != null)
		{
			FlowArrowWidget.SetFlowLevel(flowLevel);
		}
	}

	private void UpdateMoraleMask()
	{
		int num = MathF.Floor((float)MoralePercentage / 100f * 10f);
		for (int i = 0; i < _moraleItemWidgets.Length; i++)
		{
			MoraleItemWidget moraleItemWidget = _moraleItemWidgets[i];
			float num2 = 0f;
			if (i < num)
			{
				num2 = 1f;
				if (!moraleItemWidget.ItemGlowWidget.IsVisible)
				{
					RestartAnimations();
				}
			}
			else if (i == num)
			{
				float num3 = 10f;
				num2 = ((float)MoralePercentage - (float)num * num3) / num3;
				if (!moraleItemWidget.ItemWidget.IsVisible && !MBMath.ApproximatelyEquals(num2, 0f))
				{
					RestartAnimations();
				}
			}
			moraleItemWidget.SetFillAmount(num2, 12);
		}
	}

	private string GetCurrentStateName()
	{
		if (MoralePercentage < 20)
		{
			return "IsCriticalAnim";
		}
		if (IncreaseLevel > 0)
		{
			return "IncreaseAnim";
		}
		return "Default";
	}

	private void HandleAnimation()
	{
		for (int i = 0; i < _moraleItemWidgets.Length; i++)
		{
			MoraleItemWidget obj = _moraleItemWidgets[i];
			obj.ItemWidget.SetState(_currentStateName);
			obj.ItemWidget.BrushRenderer.RestartAnimation();
			obj.ItemGlowWidget.SetState(_currentStateName);
			obj.ItemGlowWidget.BrushRenderer.RestartAnimation();
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		UpdateMoraleMask();
		string currentStateName = GetCurrentStateName();
		if (_currentStateName != currentStateName)
		{
			_currentStateName = currentStateName;
			RestartAnimations();
		}
	}

	private MoraleItemWidget[] CreateItemWidgets(Widget containerWidget)
	{
		MoraleItemWidget[] array = new MoraleItemWidget[10];
		for (int i = 0; i < 10; i++)
		{
			Widget widget = new Widget(base.Context);
			widget.UpdateChildrenStates = true;
			widget.WidthSizePolicy = SizePolicy.Fixed;
			widget.HeightSizePolicy = SizePolicy.Fixed;
			widget.SuggestedWidth = 39f;
			widget.SuggestedHeight = 38f;
			if (ExtendToLeft)
			{
				widget.HorizontalAlignment = HorizontalAlignment.Right;
				widget.MarginRight = (float)i * 28f;
			}
			else
			{
				widget.HorizontalAlignment = HorizontalAlignment.Left;
				widget.MarginLeft = (float)i * 28f;
			}
			widget.AddState("IncreaseAnim");
			widget.AddState("IsCriticalAnim");
			containerWidget.AddChild(widget);
			Widget widget2 = new Widget(base.Context);
			widget2.ClipContents = true;
			widget2.UpdateChildrenStates = true;
			widget2.WidthSizePolicy = SizePolicy.StretchToParent;
			widget2.HeightSizePolicy = SizePolicy.Fixed;
			widget2.VerticalAlignment = VerticalAlignment.Bottom;
			widget.AddChild(widget2);
			BrushWidget brushWidget = new BrushWidget(base.Context);
			brushWidget.WidthSizePolicy = SizePolicy.Fixed;
			brushWidget.HeightSizePolicy = SizePolicy.Fixed;
			brushWidget.VerticalAlignment = VerticalAlignment.Bottom;
			brushWidget.Brush = ItemGlowBrush;
			brushWidget.SuggestedWidth = 39f;
			brushWidget.SuggestedHeight = 38f;
			brushWidget.AddState("IncreaseAnim");
			brushWidget.AddState("IsCriticalAnim");
			widget2.AddChild(brushWidget);
			BrushWidget brushWidget2 = new BrushWidget(base.Context);
			brushWidget2.WidthSizePolicy = SizePolicy.StretchToParent;
			brushWidget2.HeightSizePolicy = SizePolicy.StretchToParent;
			brushWidget2.Brush = ItemBackgroundBrush;
			widget.AddChild(brushWidget2);
			BrushWidget brushWidget3 = new BrushWidget(base.Context);
			brushWidget3.WidthSizePolicy = SizePolicy.Fixed;
			brushWidget3.HeightSizePolicy = SizePolicy.Fixed;
			brushWidget3.VerticalAlignment = VerticalAlignment.Bottom;
			brushWidget3.Brush = ItemBrush;
			brushWidget3.SuggestedWidth = 39f;
			brushWidget3.SuggestedHeight = 38f;
			brushWidget3.AddState("IncreaseAnim");
			brushWidget3.AddState("IsCriticalAnim");
			widget2.AddChild(brushWidget3);
			array[i] = new MoraleItemWidget(widget, widget2, brushWidget3, brushWidget, brushWidget2);
		}
		return array;
	}

	private void SetItemWidgetColors(Color color)
	{
		if (_moraleItemWidgets != null)
		{
			MoraleItemWidget[] moraleItemWidgets = _moraleItemWidgets;
			foreach (MoraleItemWidget widget in moraleItemWidgets)
			{
				SetSingleItemWidgetColor(widget, color);
			}
		}
	}

	private void SetSingleItemWidgetColor(MoraleItemWidget widget, Color color)
	{
		widget.ItemWidget.Brush.Color = color;
		foreach (Style style in widget.ItemWidget.Brush.Styles)
		{
			StyleLayer[] layers = style.GetLayers();
			for (int i = 0; i < layers.Length; i++)
			{
				layers[i].Color = color;
			}
		}
	}

	private void SetItemGlowWidgetColors(Color color)
	{
		if (_moraleItemWidgets != null)
		{
			MoraleItemWidget[] moraleItemWidgets = _moraleItemWidgets;
			foreach (MoraleItemWidget widget in moraleItemWidgets)
			{
				SetSingleItemGlowWidgetColor(widget, color);
			}
		}
	}

	private void SetSingleItemGlowWidgetColor(MoraleItemWidget widget, Color color)
	{
		widget.ItemGlowWidget.Brush.Color = color;
		foreach (Style style in widget.ItemGlowWidget.Brush.Styles)
		{
			StyleLayer[] layers = style.GetLayers();
			for (int i = 0; i < layers.Length; i++)
			{
				layers[i].Color = color;
			}
		}
	}

	private float PingPong(float min, float max, float time)
	{
		float num = max - min;
		bool num2 = (int)(time / num) % 2 == 0;
		float num3 = time % num;
		if (!num2)
		{
			return max - num3;
		}
		return num3 + min;
	}
}
