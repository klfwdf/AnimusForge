using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Information;

public class TooltipProperty : ViewModel, ISerializableObject
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

	private Func<string> valueFunc;

	private Func<string> definitionFunc;

	private string _definitionLabel;

	private string _valueLabel;

	private Color _textColor = new Color(0f, 0f, 0f, 0f);

	private int _textHeight;

	private int _propertyModifier;

	public bool OnlyShowWhenExtended { get; set; }

	public bool OnlyShowWhenNotExtended { get; set; }

	public int TextHeight
	{
		get
		{
			return _textHeight;
		}
		set
		{
			if (value != _textHeight)
			{
				_textHeight = value;
				OnPropertyChangedWithValue(value, "TextHeight");
			}
		}
	}

	public Color TextColor
	{
		get
		{
			return _textColor;
		}
		set
		{
			if (value != _textColor)
			{
				_textColor = value;
				OnPropertyChangedWithValue(value, "TextColor");
			}
		}
	}

	public string DefinitionLabel
	{
		get
		{
			return _definitionLabel;
		}
		set
		{
			if (value != _definitionLabel)
			{
				_definitionLabel = value;
				OnPropertyChangedWithValue(value, "DefinitionLabel");
			}
		}
	}

	public string ValueLabel
	{
		get
		{
			return _valueLabel;
		}
		set
		{
			if (value != _valueLabel)
			{
				_valueLabel = value;
				OnPropertyChangedWithValue(value, "ValueLabel");
			}
		}
	}

	public int PropertyModifier
	{
		get
		{
			return _propertyModifier;
		}
		set
		{
			if (value != _propertyModifier)
			{
				_propertyModifier = value;
				OnPropertyChangedWithValue(value, "PropertyModifier");
			}
		}
	}

	public TooltipProperty()
	{
	}

	public void RefreshValue()
	{
		if (valueFunc != null)
		{
			string text = valueFunc();
			if (text != null)
			{
				ValueLabel = text;
			}
		}
	}

	public void RefreshDefinition()
	{
		if (definitionFunc != null)
		{
			DefinitionLabel = definitionFunc();
		}
	}

	public TooltipProperty(string definition, string value, int textHeight, bool onlyShowWhenExtended = false, TooltipPropertyFlags modifier = TooltipPropertyFlags.None)
	{
		TextHeight = textHeight;
		DefinitionLabel = definition;
		ValueLabel = value;
		OnlyShowWhenExtended = onlyShowWhenExtended;
		PropertyModifier = (int)modifier;
	}

	public TooltipProperty(string definition, Func<string> _valueFunc, int textHeight, bool onlyShowWhenExtended = false, TooltipPropertyFlags modifier = TooltipPropertyFlags.None)
	{
		valueFunc = _valueFunc;
		TextHeight = textHeight;
		DefinitionLabel = definition;
		OnlyShowWhenExtended = onlyShowWhenExtended;
		PropertyModifier = (int)modifier;
		RefreshValue();
	}

	public TooltipProperty(Func<string> _definitionFunc, Func<string> _valueFunc, int textHeight, bool onlyShowWhenExtended = false, TooltipPropertyFlags modifier = TooltipPropertyFlags.None)
	{
		valueFunc = _valueFunc;
		TextHeight = textHeight;
		definitionFunc = _definitionFunc;
		OnlyShowWhenExtended = onlyShowWhenExtended;
		PropertyModifier = (int)modifier;
		RefreshDefinition();
		RefreshValue();
	}

	public TooltipProperty(Func<string> _definitionFunc, Func<string> _valueFunc, object[] valueArgs, int textHeight, bool onlyShowWhenExtended = false, TooltipPropertyFlags modifier = TooltipPropertyFlags.None)
	{
		valueFunc = _valueFunc;
		TextHeight = textHeight;
		definitionFunc = _definitionFunc;
		OnlyShowWhenExtended = onlyShowWhenExtended;
		PropertyModifier = (int)modifier;
		RefreshDefinition();
		RefreshValue();
	}

	public TooltipProperty(string definition, string value, int textHeight, Color color, bool onlyShowWhenExtended = false, TooltipPropertyFlags modifier = TooltipPropertyFlags.None)
	{
		TextHeight = textHeight;
		TextColor = color;
		DefinitionLabel = definition;
		ValueLabel = value;
		OnlyShowWhenExtended = onlyShowWhenExtended;
		PropertyModifier = (int)modifier;
	}

	public TooltipProperty(string definition, Func<string> _valueFunc, int textHeight, Color color, bool onlyShowWhenExtended = false, TooltipPropertyFlags modifier = TooltipPropertyFlags.None)
	{
		valueFunc = _valueFunc;
		TextHeight = textHeight;
		TextColor = color;
		DefinitionLabel = definition;
		OnlyShowWhenExtended = onlyShowWhenExtended;
		PropertyModifier = (int)modifier;
		RefreshValue();
	}

	public TooltipProperty(Func<string> _definitionFunc, Func<string> _valueFunc, int textHeight, Color color, bool onlyShowWhenExtended = false, TooltipPropertyFlags modifier = TooltipPropertyFlags.None)
	{
		valueFunc = _valueFunc;
		definitionFunc = _definitionFunc;
		TextHeight = textHeight;
		TextColor = color;
		OnlyShowWhenExtended = onlyShowWhenExtended;
		PropertyModifier = (int)modifier;
		RefreshDefinition();
		RefreshValue();
	}

	public TooltipProperty(TooltipProperty property)
	{
		TextHeight = property.TextHeight;
		TextColor = property.TextColor;
		DefinitionLabel = property.DefinitionLabel;
		ValueLabel = property.ValueLabel;
		OnlyShowWhenExtended = property.OnlyShowWhenExtended;
		PropertyModifier = property.PropertyModifier;
	}

	public void DeserializeFrom(IReader reader)
	{
		TextHeight = reader.ReadInt();
		TextColor = reader.ReadColor();
		DefinitionLabel = reader.ReadString();
		ValueLabel = reader.ReadString();
	}

	public void SerializeTo(IWriter writer)
	{
		writer.WriteInt(TextHeight);
		writer.WriteColor(TextColor);
		writer.WriteString(DefinitionLabel);
		writer.WriteString(ValueLabel);
	}
}
