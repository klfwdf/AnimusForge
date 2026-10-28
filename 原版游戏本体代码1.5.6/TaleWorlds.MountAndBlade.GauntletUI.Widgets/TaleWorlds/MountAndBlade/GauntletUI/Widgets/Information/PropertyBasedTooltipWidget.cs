using System.Collections.Generic;
using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Information;

public class PropertyBasedTooltipWidget : TooltipWidget
{
	private Brush _definitionRelationBrush;

	private Brush _valueRelationBrush;

	private bool _firstFrame = true;

	private int _mode;

	private Brush _neutralTroopsTextBrush;

	private Brush _allyTroopsTextBrush;

	private Brush _enemyTroopsTextBrush;

	public Color AllyColor { get; set; }

	public Color EnemyColor { get; set; }

	public Color NeutralColor { get; set; }

	public Widget PropertyListBackground { get; set; }

	public ListPanel PropertyList { get; set; }

	private IEnumerable<TooltipPropertyWidget> PropertyWidgets
	{
		get
		{
			if (PropertyList == null)
			{
				yield break;
			}
			for (int i = 0; i < PropertyList.ChildCount; i++)
			{
				if (PropertyList.GetChild(i) is TooltipPropertyWidget tooltipPropertyWidget)
				{
					yield return tooltipPropertyWidget;
				}
			}
		}
	}

	[Editor(false)]
	public int Mode
	{
		get
		{
			return _mode;
		}
		set
		{
			if (_mode != value)
			{
				_mode = value;
			}
		}
	}

	[Editor(false)]
	public Brush NeutralTroopsTextBrush
	{
		get
		{
			return _neutralTroopsTextBrush;
		}
		set
		{
			if (_neutralTroopsTextBrush != value)
			{
				_neutralTroopsTextBrush = value;
				OnPropertyChanged(value, "NeutralTroopsTextBrush");
			}
		}
	}

	[Editor(false)]
	public Brush EnemyTroopsTextBrush
	{
		get
		{
			return _enemyTroopsTextBrush;
		}
		set
		{
			if (_enemyTroopsTextBrush != value)
			{
				_enemyTroopsTextBrush = value;
				OnPropertyChanged(value, "EnemyTroopsTextBrush");
			}
		}
	}

	[Editor(false)]
	public Brush AllyTroopsTextBrush
	{
		get
		{
			return _allyTroopsTextBrush;
		}
		set
		{
			if (_allyTroopsTextBrush != value)
			{
				_allyTroopsTextBrush = value;
				OnPropertyChanged(value, "AllyTroopsTextBrush");
			}
		}
	}

	public PropertyBasedTooltipWidget(UIContext context)
		: base(context)
	{
		_animationDelayInFrames = 2;
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		UpdateBattleScopes();
	}

	private void UpdateBattleScopes()
	{
		bool battleScope = false;
		foreach (TooltipPropertyWidget propertyWidget in PropertyWidgets)
		{
			if (propertyWidget.IsBattleMode)
			{
				battleScope = true;
			}
			else if (propertyWidget.IsBattleModeOver)
			{
				battleScope = false;
			}
			propertyWidget.SetBattleScope(battleScope);
		}
	}

	private float GetBattleScopeSize()
	{
		bool flag = false;
		float num = 0f;
		if (PropertyList != null)
		{
			for (int i = 0; i < PropertyList.ChildCount; i++)
			{
				if (!(PropertyList.GetChild(i) is TooltipPropertyWidget tooltipPropertyWidget))
				{
					continue;
				}
				if (tooltipPropertyWidget.IsBattleMode)
				{
					flag = true;
				}
				else if (tooltipPropertyWidget.IsBattleModeOver)
				{
					flag = false;
				}
				if (flag)
				{
					float num2 = ((tooltipPropertyWidget.ValueLabel.Size.X > tooltipPropertyWidget.DefinitionLabel.Size.X) ? tooltipPropertyWidget.ValueLabel.Size.X : tooltipPropertyWidget.DefinitionLabel.Size.X);
					if (num2 > num)
					{
						num = num2;
					}
				}
			}
		}
		return num;
	}

	private void UpdateRelationBrushes()
	{
		TooltipPropertyWidget tooltipPropertyWidget = PropertyWidgets.SingleOrDefault((TooltipPropertyWidget p) => p.IsRelation);
		if (tooltipPropertyWidget != null)
		{
			if ((tooltipPropertyWidget.PropertyModifierAsFlag & TooltipPropertyWidget.TooltipPropertyFlags.WarFirstAlly) == TooltipPropertyWidget.TooltipPropertyFlags.WarFirstAlly)
			{
				_definitionRelationBrush = AllyTroopsTextBrush;
			}
			else if ((tooltipPropertyWidget.PropertyModifierAsFlag & TooltipPropertyWidget.TooltipPropertyFlags.WarFirstEnemy) == TooltipPropertyWidget.TooltipPropertyFlags.WarFirstEnemy)
			{
				_definitionRelationBrush = EnemyTroopsTextBrush;
			}
			else
			{
				_definitionRelationBrush = NeutralTroopsTextBrush;
			}
			if ((tooltipPropertyWidget.PropertyModifierAsFlag & TooltipPropertyWidget.TooltipPropertyFlags.WarSecondAlly) == TooltipPropertyWidget.TooltipPropertyFlags.WarSecondAlly)
			{
				_valueRelationBrush = AllyTroopsTextBrush;
			}
			else if ((tooltipPropertyWidget.PropertyModifierAsFlag & TooltipPropertyWidget.TooltipPropertyFlags.WarSecondEnemy) == TooltipPropertyWidget.TooltipPropertyFlags.WarSecondEnemy)
			{
				_valueRelationBrush = EnemyTroopsTextBrush;
			}
			else
			{
				_valueRelationBrush = NeutralTroopsTextBrush;
			}
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		bool flag = false;
		float battleScopeSize = GetBattleScopeSize();
		float num = -1f;
		float num2 = -1f;
		_definitionRelationBrush = null;
		_valueRelationBrush = null;
		if (PropertyList != null)
		{
			if (_firstFrame)
			{
				_firstFrame = false;
				return;
			}
			for (int i = 0; i < PropertyList.ChildCount; i++)
			{
				if (PropertyList.GetChild(i) is TooltipPropertyWidget { IsTwoColumn: not false, IsMultiLine: false } tooltipPropertyWidget)
				{
					if (num < tooltipPropertyWidget.ValueLabelContainer.Size.X)
					{
						num = tooltipPropertyWidget.ValueLabelContainer.Size.X;
					}
					if (num2 < tooltipPropertyWidget.DefinitionLabelContainer.Size.X)
					{
						num2 = tooltipPropertyWidget.DefinitionLabelContainer.Size.X;
					}
				}
			}
			for (int j = 0; j < PropertyList.ChildCount; j++)
			{
				if (PropertyList.GetChild(j) is TooltipPropertyWidget tooltipPropertyWidget2)
				{
					if (tooltipPropertyWidget2.IsBattleMode)
					{
						flag = true;
					}
					else if (tooltipPropertyWidget2.IsBattleModeOver)
					{
						flag = false;
					}
					if (flag && (_definitionRelationBrush == null || _valueRelationBrush == null))
					{
						UpdateRelationBrushes();
					}
					tooltipPropertyWidget2.RefreshSize(flag, battleScopeSize, num, num2, _definitionRelationBrush, _valueRelationBrush);
				}
			}
		}
		else
		{
			_firstFrame = true;
		}
		if (PropertyListBackground != null)
		{
			if (Mode == 2)
			{
				PropertyListBackground.Color = AllyColor;
			}
			else if (Mode == 3)
			{
				PropertyListBackground.Color = EnemyColor;
			}
			else
			{
				PropertyListBackground.Color = NeutralColor;
			}
		}
	}
}
