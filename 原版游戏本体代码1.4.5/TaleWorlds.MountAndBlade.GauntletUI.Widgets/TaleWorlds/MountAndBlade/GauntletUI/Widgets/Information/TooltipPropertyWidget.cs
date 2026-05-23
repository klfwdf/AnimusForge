using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Information;

public class TooltipPropertyWidget : Widget
{
	[Flags]
	public enum TooltipPropertyFlags
	{
		None = 0,
		MultiLine = 1,
		BattleMode = 2,
		BattleModeOver = 4,
		WarFirstEnemy = 8,
		WarFirstAlly = 0x10,
		WarFirstNeutral = 0x20,
		WarSecondEnemy = 0x40,
		WarSecondAlly = 0x80,
		WarSecondNeutral = 0x100,
		RundownSeperator = 0x200,
		DefaultSeperator = 0x400,
		Cost = 0x800,
		Title = 0x1000,
		RundownResult = 0x2000
	}

	private const int HeaderSize = 30;

	private const int DefaultSize = 15;

	private const int SubTextSize = 10;

	private bool _isMultiLine;

	private bool _isBattleMode;

	private bool _isBattleModeOver;

	private bool _isCost;

	private bool _isRundownSeperator;

	private bool _isDefaultSeperator;

	private bool _isRundownResult;

	private bool _isTitle;

	private bool _isSubtext;

	private bool _isEmptySpace;

	private bool _isRelation;

	private bool _useCustomColor;

	private Sprite RundownSeperatorSprite;

	private Sprite DefaultSeperatorSprite;

	private Sprite TitleBackgroundSprite;

	private Sprite _currentSprite;

	private bool _firstFrame = true;

	private bool _modifyDefinitionColor = true;

	private Color _textColor;

	private RichTextWidget _definitionLabel;

	private RichTextWidget _valueLabel;

	private TextWidget _itemModifierLabel;

	private Widget _definitionLabelContainer;

	private Widget _valueLabelContainer;

	private ListPanel _valueBackgroundSpriteWidget;

	private int _textHeight;

	private Brush _titleTextBrush;

	private Brush _subtextBrush;

	private Brush _valueTextBrush;

	private Brush _descriptionTextBrush;

	private Brush _valueNameTextBrush;

	private string _rundownSeperatorSpriteName;

	private string _defaultSeperatorSpriteName;

	private string _titleBackgroundSpriteName;

	private string _definitionText;

	private string _valueText;

	private int _propertyModifier;

	public bool IsTwoColumn { get; private set; }

	public TooltipPropertyFlags PropertyModifierAsFlag { get; private set; }

	private bool _allBrushesInitialized
	{
		get
		{
			if (SubtextBrush != null && ValueTextBrush != null && DescriptionTextBrush != null && ValueNameTextBrush != null && RundownSeperatorSprite != null && DefaultSeperatorSprite != null)
			{
				return TitleBackgroundSprite != null;
			}
			return false;
		}
	}

	public bool IsMultiLine => _isMultiLine;

	public bool IsBattleMode => _isBattleMode;

	public bool IsBattleModeOver => _isBattleModeOver;

	public bool IsCost => _isCost;

	public bool IsRelation => _isRelation;

	[Editor(false)]
	public string RundownSeperatorSpriteName
	{
		get
		{
			return _rundownSeperatorSpriteName;
		}
		set
		{
			if (_rundownSeperatorSpriteName != value)
			{
				_rundownSeperatorSpriteName = value;
				OnPropertyChanged(value, "RundownSeperatorSpriteName");
				RundownSeperatorSprite = base.Context.SpriteData.GetSprite(value);
			}
		}
	}

	[Editor(false)]
	public string DefaultSeperatorSpriteName
	{
		get
		{
			return _defaultSeperatorSpriteName;
		}
		set
		{
			if (_defaultSeperatorSpriteName != value)
			{
				_defaultSeperatorSpriteName = value;
				OnPropertyChanged(value, "DefaultSeperatorSpriteName");
				DefaultSeperatorSprite = base.Context.SpriteData.GetSprite(value);
			}
		}
	}

	[Editor(false)]
	public string TitleBackgroundSpriteName
	{
		get
		{
			return _titleBackgroundSpriteName;
		}
		set
		{
			if (_titleBackgroundSpriteName != value)
			{
				_titleBackgroundSpriteName = value;
				OnPropertyChanged(value, "TitleBackgroundSpriteName");
				TitleBackgroundSprite = base.Context.SpriteData.GetSprite(value);
			}
		}
	}

	[Editor(false)]
	public Brush ValueNameTextBrush
	{
		get
		{
			return _valueNameTextBrush;
		}
		set
		{
			if (_valueNameTextBrush != value)
			{
				_valueNameTextBrush = value;
				OnPropertyChanged(value, "ValueNameTextBrush");
			}
		}
	}

	[Editor(false)]
	public Brush TitleTextBrush
	{
		get
		{
			return _titleTextBrush;
		}
		set
		{
			if (_titleTextBrush != value)
			{
				_titleTextBrush = value;
				OnPropertyChanged(value, "TitleTextBrush");
			}
		}
	}

	[Editor(false)]
	public Brush SubtextBrush
	{
		get
		{
			return _subtextBrush;
		}
		set
		{
			if (_subtextBrush != value)
			{
				_subtextBrush = value;
				OnPropertyChanged(value, "SubtextBrush");
			}
		}
	}

	[Editor(false)]
	public Brush ValueTextBrush
	{
		get
		{
			return _valueTextBrush;
		}
		set
		{
			if (_valueTextBrush != value)
			{
				_valueTextBrush = value;
				OnPropertyChanged(value, "ValueTextBrush");
			}
		}
	}

	[Editor(false)]
	public Brush DescriptionTextBrush
	{
		get
		{
			return _descriptionTextBrush;
		}
		set
		{
			if (_descriptionTextBrush != value)
			{
				_descriptionTextBrush = value;
				OnPropertyChanged(value, "DescriptionTextBrush");
			}
		}
	}

	[Editor(false)]
	public bool ModifyDefinitionColor
	{
		get
		{
			return _modifyDefinitionColor;
		}
		set
		{
			if (_modifyDefinitionColor != value)
			{
				_modifyDefinitionColor = value;
				OnPropertyChanged(value, "ModifyDefinitionColor");
			}
		}
	}

	[Editor(false)]
	public RichTextWidget DefinitionLabel
	{
		get
		{
			return _definitionLabel;
		}
		set
		{
			if (_definitionLabel != value)
			{
				_definitionLabel = value;
				OnPropertyChanged(value, "DefinitionLabel");
			}
		}
	}

	[Editor(false)]
	public RichTextWidget ValueLabel
	{
		get
		{
			return _valueLabel;
		}
		set
		{
			if (_valueLabel != value)
			{
				_valueLabel = value;
				OnPropertyChanged(value, "ValueLabel");
			}
		}
	}

	[Editor(false)]
	public TextWidget ItemModifierLabel
	{
		get
		{
			return _itemModifierLabel;
		}
		set
		{
			if (_itemModifierLabel != value)
			{
				_itemModifierLabel = value;
				OnPropertyChanged(value, "ItemModifierLabel");
			}
		}
	}

	[Editor(false)]
	public ListPanel ValueBackgroundSpriteWidget
	{
		get
		{
			return _valueBackgroundSpriteWidget;
		}
		set
		{
			if (_valueBackgroundSpriteWidget != value)
			{
				_valueBackgroundSpriteWidget = value;
				OnPropertyChanged(value, "ValueBackgroundSpriteWidget");
			}
		}
	}

	[Editor(false)]
	public Widget DefinitionLabelContainer
	{
		get
		{
			return _definitionLabelContainer;
		}
		set
		{
			if (_definitionLabelContainer != value)
			{
				_definitionLabelContainer = value;
				OnPropertyChanged(value, "DefinitionLabelContainer");
			}
		}
	}

	[Editor(false)]
	public Widget ValueLabelContainer
	{
		get
		{
			return _valueLabelContainer;
		}
		set
		{
			if (_valueLabelContainer != value)
			{
				_valueLabelContainer = value;
				OnPropertyChanged(value, "ValueLabelContainer");
			}
		}
	}

	[Editor(false)]
	public Color TextColor
	{
		get
		{
			return _textColor;
		}
		set
		{
			if (_textColor != value)
			{
				_textColor = value;
				OnPropertyChanged(value, "TextColor");
				_useCustomColor = true;
			}
		}
	}

	[Editor(false)]
	public int TextHeight
	{
		get
		{
			return _textHeight;
		}
		set
		{
			if (_textHeight != value)
			{
				_textHeight = value;
				OnPropertyChanged(value, "TextHeight");
			}
		}
	}

	[Editor(false)]
	public string DefinitionText
	{
		get
		{
			return _definitionText;
		}
		set
		{
			if (_definitionText != value)
			{
				_definitionText = value;
				OnPropertyChanged(value, "DefinitionText");
				_firstFrame = true;
			}
		}
	}

	[Editor(false)]
	public string ValueText
	{
		get
		{
			return _valueText;
		}
		set
		{
			if (_valueText != value)
			{
				_valueText = value;
				OnPropertyChanged(value, "ValueText");
				_firstFrame = true;
			}
		}
	}

	[Editor(false)]
	public int PropertyModifier
	{
		get
		{
			return _propertyModifier;
		}
		set
		{
			if (_propertyModifier != value)
			{
				_propertyModifier = value;
				OnPropertyChanged(value, "PropertyModifier");
			}
		}
	}

	public TooltipPropertyWidget(UIContext context)
		: base(context)
	{
		_isMultiLine = false;
		_isBattleMode = false;
		_isBattleModeOver = false;
	}

	public void SetBattleScope(bool battleScope)
	{
		if (battleScope)
		{
			DefinitionLabel.HorizontalAlignment = HorizontalAlignment.Center;
			ValueLabel.HorizontalAlignment = HorizontalAlignment.Center;
		}
		else
		{
			DefinitionLabel.HorizontalAlignment = HorizontalAlignment.Right;
			ValueLabel.HorizontalAlignment = HorizontalAlignment.Left;
		}
	}

	public void RefreshSize(bool inBattleScope, float battleScopeSize, float maxValueLabelSizeX, float maxDefinitionLabelSizeX, Brush definitionRelationBrush = null, Brush valueRelationBrush = null)
	{
		if (_isMultiLine || _isSubtext)
		{
			DefinitionLabelContainer.IsVisible = false;
			DefinitionLabelContainer.ScaledSuggestedWidth = 0f;
			ValueLabel.WidthSizePolicy = SizePolicy.Fixed;
			ValueLabelContainer.WidthSizePolicy = SizePolicy.Fixed;
			ValueLabel.ScaledSuggestedWidth = base.ParentWidget.Size.X - (base.ScaledMarginLeft + base.ScaledMarginRight);
			ValueLabelContainer.ScaledSuggestedWidth = base.ParentWidget.Size.X - (base.ScaledMarginLeft + base.ScaledMarginRight);
		}
		else if (inBattleScope)
		{
			DefinitionLabelContainer.ScaledSuggestedWidth = battleScopeSize;
			DefinitionLabel.Brush = definitionRelationBrush;
			ValueLabelContainer.ScaledSuggestedWidth = battleScopeSize;
			ValueLabel.Brush = valueRelationBrush;
			ValueLabelContainer.HorizontalAlignment = HorizontalAlignment.Left;
			ValueLabel.HorizontalAlignment = HorizontalAlignment.Left;
			ValueLabel.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Left;
		}
		else if (!IsTwoColumn)
		{
			if (!string.IsNullOrEmpty(DefinitionLabel.Text))
			{
				float scaledSuggestedWidth = ((DefinitionLabel.Size.X > ValueLabel.Size.X) ? DefinitionLabel.Size.X : ValueLabel.Size.X);
				DefinitionLabelContainer.ScaledSuggestedWidth = scaledSuggestedWidth;
				ValueLabelContainer.ScaledSuggestedWidth = scaledSuggestedWidth;
			}
			else
			{
				DefinitionLabelContainer.ScaledSuggestedWidth = 0f;
				DefinitionLabelContainer.IsVisible = false;
				ValueLabelContainer.ScaledSuggestedWidth = ValueLabel.Size.X;
			}
		}
		if (IsTwoColumn && !_isMultiLine && (!_isTitle || (_isTitle && IsTwoColumn)))
		{
			ValueLabelContainer.WidthSizePolicy = SizePolicy.Fixed;
			ValueLabelContainer.ScaledSuggestedWidth = TaleWorlds.Library.MathF.Max(53f * base._scaleToUse, maxValueLabelSizeX);
			if (ItemModifierLabel != null && ItemModifierLabel.IsVisible)
			{
				ValueLabel.WidthSizePolicy = SizePolicy.CoverChildren;
				ValueLabel.MaxWidth = TaleWorlds.Library.MathF.Max(53f, maxValueLabelSizeX * base._inverseScaleToUse) - ItemModifierLabel.Size.X * base._inverseScaleToUse - (ItemModifierLabel.MarginLeft + ItemModifierLabel.MarginRight);
			}
			else
			{
				ValueLabel.WidthSizePolicy = SizePolicy.Fixed;
				ValueLabel.ScaledSuggestedWidth = TaleWorlds.Library.MathF.Max(53f * base._scaleToUse, maxValueLabelSizeX);
			}
		}
		if (IsTwoColumn && !_isMultiLine && _isTitle)
		{
			DefinitionLabel.WidthSizePolicy = SizePolicy.Fixed;
			DefinitionLabel.ScaledSuggestedWidth = TaleWorlds.Library.MathF.Max(53f * base._scaleToUse, maxDefinitionLabelSizeX);
			DefinitionLabelContainer.WidthSizePolicy = SizePolicy.Fixed;
			DefinitionLabelContainer.ScaledSuggestedWidth = TaleWorlds.Library.MathF.Max(53f * base._scaleToUse, maxDefinitionLabelSizeX);
		}
		this.SetGlobalAlphaRecursively(base.ParentWidget?.AlphaFactor ?? 0f);
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (_firstFrame)
		{
			RefreshText();
			_firstFrame = false;
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		ValueBackgroundSpriteWidget.HeightSizePolicy = SizePolicy.CoverChildren;
		if (_currentSprite != null)
		{
			if (DefinitionLabelContainer.Size.X + ValueLabelContainer.Size.X > base.ParentWidget.Size.X)
			{
				ValueBackgroundSpriteWidget.WidthSizePolicy = SizePolicy.Fixed;
				ValueBackgroundSpriteWidget.ScaledSuggestedWidth = DefinitionLabelContainer.Size.X + ValueLabelContainer.Size.X;
				base.MarginLeft = 0f;
				base.MarginRight = 0f;
			}
			else
			{
				ValueBackgroundSpriteWidget.WidthSizePolicy = SizePolicy.Fixed;
				ValueBackgroundSpriteWidget.ScaledSuggestedWidth = base.ParentWidget.Size.X - (base.MarginLeft + base.MarginRight) * base._scaleToUse;
			}
			ValueBackgroundSpriteWidget.MinHeight = _currentSprite.Height;
			if (_isTitle)
			{
				base.PositionXOffset = 0f - base.MarginLeft;
				ValueLabelContainer.PositionYOffset = 0f;
				if (!IsTwoColumn)
				{
					ValueLabelContainer.MarginLeft = base.MarginLeft;
					ValueBackgroundSpriteWidget.ScaledSuggestedHeight = ValueLabel.Size.Y;
				}
				else
				{
					DefinitionLabelContainer.MarginLeft = base.MarginLeft;
					DefinitionLabel.HorizontalAlignment = HorizontalAlignment.Left;
				}
			}
		}
		else
		{
			ValueBackgroundSpriteWidget.SuggestedWidth = 0f;
		}
	}

	private void RefreshText()
	{
		DefinitionLabel.Text = _definitionText;
		ValueLabel.Text = _valueText;
		DetermineTypeOfTooltipProperty();
		ValueLabelContainer.IsVisible = true;
		DefinitionLabelContainer.IsVisible = true;
		DefinitionLabel.IsVisible = true;
		ValueLabel.IsVisible = true;
		_currentSprite = null;
		if (_allBrushesInitialized)
		{
			if (_isRelation)
			{
				DefinitionLabel.Text = "";
				ValueLabel.Text = "";
			}
			else if (_isBattleMode)
			{
				DefinitionLabel.Text = "";
				ValueLabel.Text = "";
			}
			else if (_isBattleModeOver)
			{
				DefinitionLabel.Text = "";
				ValueLabel.Text = "";
			}
			else if (_isMultiLine)
			{
				DefinitionLabelContainer.IsVisible = false;
				ValueLabel.Text = _valueText;
				ValueLabel.Brush = ((TextHeight < 0) ? SubtextBrush : DescriptionTextBrush);
				ValueLabel.WidthSizePolicy = SizePolicy.CoverChildren;
				ValueLabelContainer.WidthSizePolicy = SizePolicy.Fixed;
				ValueLabelContainer.SuggestedWidth = 0f;
			}
			else if (_isCost)
			{
				DefinitionLabel.Text = "";
				ValueLabel.Text = _valueText;
				base.HorizontalAlignment = HorizontalAlignment.Center;
				ValueLabelContainer.WidthSizePolicy = SizePolicy.CoverChildren;
				ValueLabel.WidthSizePolicy = SizePolicy.CoverChildren;
			}
			else if (_isRundownSeperator)
			{
				ValueLabel.IsVisible = false;
				DefinitionLabelContainer.IsVisible = false;
				ValueBackgroundSpriteWidget.IsVisible = true;
				ValueLabelContainer.WidthSizePolicy = SizePolicy.CoverChildren;
				_currentSprite = RundownSeperatorSprite;
				ValueBackgroundSpriteWidget.HorizontalAlignment = HorizontalAlignment.Right;
				ValueBackgroundSpriteWidget.PositionXOffset = base.Right * base._inverseScaleToUse;
				ValueBackgroundSpriteWidget.Sprite = _currentSprite;
				ValueBackgroundSpriteWidget.HeightSizePolicy = SizePolicy.Fixed;
				ValueBackgroundSpriteWidget.WidthSizePolicy = SizePolicy.Fixed;
			}
			else if (_isDefaultSeperator)
			{
				ValueLabel.IsVisible = false;
				DefinitionLabelContainer.IsVisible = false;
				ValueBackgroundSpriteWidget.IsVisible = true;
				ValueLabelContainer.WidthSizePolicy = SizePolicy.CoverChildren;
				_currentSprite = DefaultSeperatorSprite;
				ValueBackgroundSpriteWidget.HorizontalAlignment = HorizontalAlignment.Right;
				ValueBackgroundSpriteWidget.PositionXOffset = base.Right * base._inverseScaleToUse;
				ValueBackgroundSpriteWidget.Sprite = _currentSprite;
				ValueBackgroundSpriteWidget.AlphaFactor = 0.5f;
				ValueBackgroundSpriteWidget.HeightSizePolicy = SizePolicy.Fixed;
				ValueBackgroundSpriteWidget.WidthSizePolicy = SizePolicy.Fixed;
			}
			else if (_isTitle)
			{
				DefinitionLabel.Brush = TitleTextBrush;
				ValueLabel.Brush = TitleTextBrush;
				DefinitionLabel.HeightSizePolicy = SizePolicy.CoverChildren;
				ValueLabel.HeightSizePolicy = SizePolicy.CoverChildren;
				DefinitionLabelContainer.HeightSizePolicy = SizePolicy.CoverChildren;
				ValueLabelContainer.HeightSizePolicy = SizePolicy.CoverChildren;
				if (IsTwoColumn)
				{
					DefinitionLabelContainer.WidthSizePolicy = SizePolicy.CoverChildren;
					DefinitionLabelContainer.HorizontalAlignment = HorizontalAlignment.Left;
					DefinitionLabel.WidthSizePolicy = SizePolicy.CoverChildren;
					DefinitionLabel.HorizontalAlignment = HorizontalAlignment.Left;
					DefinitionLabel.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Left;
					ValueLabel.WidthSizePolicy = SizePolicy.CoverChildren;
					ValueLabelContainer.WidthSizePolicy = SizePolicy.CoverChildren;
					ValueLabel.MarginLeft = base.MarginLeft;
				}
				else
				{
					ValueLabelContainer.WidthSizePolicy = SizePolicy.CoverChildren;
					ValueLabel.WidthSizePolicy = SizePolicy.CoverChildren;
				}
				_currentSprite = TitleBackgroundSprite;
				ValueBackgroundSpriteWidget.HeightSizePolicy = SizePolicy.CoverChildren;
				ValueBackgroundSpriteWidget.Sprite = _currentSprite;
				ValueBackgroundSpriteWidget.IsVisible = true;
			}
			else if (IsTwoColumn)
			{
				DefinitionLabelContainer.WidthSizePolicy = SizePolicy.CoverChildren;
				DefinitionLabel.WidthSizePolicy = SizePolicy.CoverChildren;
				DefinitionLabelContainer.HorizontalAlignment = HorizontalAlignment.Right;
				DefinitionLabel.HorizontalAlignment = HorizontalAlignment.Right;
				ValueLabel.WidthSizePolicy = SizePolicy.CoverChildren;
				ValueLabelContainer.WidthSizePolicy = SizePolicy.CoverChildren;
				base.HorizontalAlignment = HorizontalAlignment.Right;
				ValueLabel.MarginLeft = base.MarginLeft;
				DefinitionLabel.Brush = ValueNameTextBrush;
				ValueLabel.Brush = ValueTextBrush;
			}
			else if (_isSubtext)
			{
				DefinitionLabelContainer.IsVisible = false;
				ValueLabel.Brush = SubtextBrush;
				ValueLabelContainer.WidthSizePolicy = SizePolicy.CoverChildren;
				ValueLabel.WidthSizePolicy = SizePolicy.CoverChildren;
			}
			else if (_isEmptySpace)
			{
				DefinitionLabel.IsVisible = false;
				ValueLabel.Text = " ";
				ValueLabel.WidthSizePolicy = SizePolicy.CoverChildren;
				ValueLabelContainer.WidthSizePolicy = SizePolicy.CoverChildren;
				if (TextHeight > 0)
				{
					ValueLabel.Brush.FontSize = 30;
				}
				else if (TextHeight < 0)
				{
					ValueLabel.Brush.FontSize = 10;
				}
				else
				{
					ValueLabel.Brush.FontSize = 15;
				}
			}
			else if (DefinitionLabel.Text == string.Empty && ValueLabel.Text != string.Empty)
			{
				DefinitionLabelContainer.IsVisible = false;
				ValueLabelContainer.WidthSizePolicy = SizePolicy.CoverChildren;
				ValueLabel.WidthSizePolicy = SizePolicy.CoverChildren;
				ValueLabel.Brush = DescriptionTextBrush;
				ValueLabel.WidthSizePolicy = SizePolicy.CoverChildren;
				ValueLabelContainer.WidthSizePolicy = SizePolicy.CoverChildren;
			}
			else
			{
				ValueLabel.WidthSizePolicy = SizePolicy.CoverChildren;
				DefinitionLabel.WidthSizePolicy = SizePolicy.CoverChildren;
			}
			if (_useCustomColor)
			{
				ValueLabel.Brush.FontColor = TextColor;
				ValueLabel.Brush.TextAlphaFactor = TextColor.Alpha;
			}
			if (_isRundownResult)
			{
				ValueLabel.Brush.FontSize = (int)((float)ValueLabel.ReadOnlyBrush.FontSize * 1.3f);
				DefinitionLabel.Brush.FontSize = (int)((float)DefinitionLabel.ReadOnlyBrush.FontSize * 1.3f);
			}
		}
		this.SetGlobalAlphaRecursively(base.ParentWidget?.AlphaFactor ?? 0f);
	}

	private void DetermineTypeOfTooltipProperty()
	{
		PropertyModifierAsFlag = (TooltipPropertyFlags)PropertyModifier;
		_isMultiLine = (PropertyModifierAsFlag & TooltipPropertyFlags.MultiLine) == TooltipPropertyFlags.MultiLine;
		_isBattleMode = (PropertyModifierAsFlag & TooltipPropertyFlags.BattleMode) == TooltipPropertyFlags.BattleMode;
		_isBattleModeOver = (PropertyModifierAsFlag & TooltipPropertyFlags.BattleModeOver) == TooltipPropertyFlags.BattleModeOver;
		_isCost = (PropertyModifierAsFlag & TooltipPropertyFlags.Cost) == TooltipPropertyFlags.Cost;
		_isTitle = (PropertyModifierAsFlag & TooltipPropertyFlags.Title) == TooltipPropertyFlags.Title;
		_isRelation = (PropertyModifierAsFlag & TooltipPropertyFlags.WarFirstEnemy) == TooltipPropertyFlags.WarFirstEnemy || (PropertyModifierAsFlag & TooltipPropertyFlags.WarFirstAlly) == TooltipPropertyFlags.WarFirstAlly || (PropertyModifierAsFlag & TooltipPropertyFlags.WarFirstNeutral) == TooltipPropertyFlags.WarFirstNeutral || (PropertyModifierAsFlag & TooltipPropertyFlags.WarSecondEnemy) == TooltipPropertyFlags.WarSecondEnemy || (PropertyModifierAsFlag & TooltipPropertyFlags.WarSecondAlly) == TooltipPropertyFlags.WarSecondAlly || (PropertyModifierAsFlag & TooltipPropertyFlags.WarSecondNeutral) == TooltipPropertyFlags.WarSecondNeutral;
		_isRundownSeperator = (PropertyModifierAsFlag & TooltipPropertyFlags.RundownSeperator) == TooltipPropertyFlags.RundownSeperator;
		_isDefaultSeperator = (PropertyModifierAsFlag & TooltipPropertyFlags.DefaultSeperator) == TooltipPropertyFlags.DefaultSeperator;
		_isRundownResult = (PropertyModifierAsFlag & TooltipPropertyFlags.RundownResult) == TooltipPropertyFlags.RundownResult;
		IsTwoColumn = false;
		_isSubtext = false;
		_isEmptySpace = false;
		if (!_isMultiLine && !_isBattleMode && !_isBattleModeOver && !_isCost && !_isRundownSeperator && !_isDefaultSeperator)
		{
			_isEmptySpace = DefinitionText == string.Empty && ValueText == string.Empty;
			IsTwoColumn = DefinitionText != string.Empty && ValueText != string.Empty && TextHeight == 0;
			_isSubtext = DefinitionText == string.Empty && ValueText != string.Empty && TextHeight < 0;
		}
	}
}
