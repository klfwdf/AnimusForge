using TaleWorlds.CampaignSystem.AdvancedStartOptions;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.AdvancedStartOptions;

public abstract class AdvancedStartOption
{
	public delegate bool AdvancedStartOptionCondition(AdvancedStartOptions options);

	private const string ItemTextId = "str_campaign_starting_options_item_name";

	private readonly AdvancedStartOptionCondition _onCondition;

	private readonly AdvancedStartData _value;

	private readonly AdvancedStartData _defaultValue;

	public string StringId => _defaultValue.StringId;

	public string CategoryId => _defaultValue.CategoryId;

	public AdvancedStartData Value => _value;

	public AdvancedStartData DefaultValue => _defaultValue;

	public virtual bool HasValueChanged()
	{
		return !object.Equals(_value.GetData(), _defaultValue.GetData());
	}

	public void SetValue<T>(T data)
	{
		if (_value is AdvancedStartData<T> advancedStartData)
		{
			advancedStartData.Value = data;
		}
		else
		{
			Debug.FailedAssert("Wrong generic type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\AdvancedStartOptions\\AdvancedStartOption.cs", "SetValue", 38);
		}
	}

	public T GetValue<T>()
	{
		if (_value is AdvancedStartData<T> advancedStartData)
		{
			return advancedStartData.Value;
		}
		Debug.FailedAssert("Wrong generic type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\AdvancedStartOptions\\AdvancedStartOption.cs", "GetValue", 50);
		return default(T);
	}

	public T GetDefaultValue<T>()
	{
		if (_defaultValue is AdvancedStartData<T> advancedStartData)
		{
			return advancedStartData.Value;
		}
		Debug.FailedAssert("Wrong generic type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\AdvancedStartOptions\\AdvancedStartOption.cs", "GetDefaultValue", 63);
		return default(T);
	}

	internal TextObject GetItemName()
	{
		object data = Value.GetData();
		if (data != null)
		{
			if (data is string variation)
			{
				return Module.CurrentModule.GlobalTextManager.FindText("str_campaign_starting_options_item_name", variation);
			}
			object obj;
			if ((obj = data) is bool)
			{
				return new TextObject(((bool)obj) ? "1" : "0");
			}
			return new TextObject(data.ToString());
		}
		return TextObject.GetEmpty();
	}

	protected AdvancedStartOption(AdvancedStartData defaultValue, AdvancedStartOptionCondition onCondition)
	{
		_onCondition = onCondition;
		_value = defaultValue.Clone();
		_defaultValue = defaultValue;
	}

	public bool GetIsHidden(AdvancedStartOptions options)
	{
		if (_onCondition != null)
		{
			return _onCondition(options);
		}
		return false;
	}
}
