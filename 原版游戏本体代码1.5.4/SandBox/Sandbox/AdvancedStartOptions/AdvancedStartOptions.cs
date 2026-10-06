using System.Collections.Generic;
using TaleWorlds.CampaignSystem.AdvancedStartOptions;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.AdvancedStartOptions;

public class AdvancedStartOptions
{
	private readonly List<AdvancedStartOption> _options;

	public AdvancedStartOptions()
	{
		_options = new List<AdvancedStartOption>();
	}

	public void Add(AdvancedStartOption option)
	{
		for (int i = 0; i < _options.Count; i++)
		{
			if (_options[i].StringId == option.StringId)
			{
				Debug.Print("Overriding start option id: " + option.StringId);
				_options[i] = option;
				return;
			}
		}
		_options.Add(option);
	}

	public bool RemoveOption(string key)
	{
		for (int i = 0; i < _options.Count; i++)
		{
			if (_options[i].StringId == key)
			{
				_options.RemoveAt(i);
				return true;
			}
		}
		Debug.FailedAssert("Trying to remove nonexistent option", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\AdvancedStartOptions\\AdvancedStartOptions.cs", "RemoveOption", 51);
		return false;
	}

	public AdvancedStartOption GetOption(string key)
	{
		for (int i = 0; i < _options.Count; i++)
		{
			if (_options[i].StringId == key)
			{
				return _options[i];
			}
		}
		return null;
	}

	public TextObject GetOptionName(string key)
	{
		if (HasValue(key, out var option))
		{
			TextObject name = option.Value.GetName(Module.CurrentModule.GlobalTextManager);
			SetTextVariables(key, name);
			return name;
		}
		return TextObject.GetEmpty();
	}

	public TextObject GetOptionDescription(string key)
	{
		if (HasValue(key, out var option))
		{
			TextObject description = option.Value.GetDescription(Module.CurrentModule.GlobalTextManager);
			SetTextVariables(key, description);
			return description;
		}
		return TextObject.GetEmpty();
	}

	public TextObject GetItemDescription(string key)
	{
		if (HasValue(key, out var option) && option is ListAdvancedStartOption listAdvancedStartOption)
		{
			TextObject listItemDescription = listAdvancedStartOption.GetListItemDescription(listAdvancedStartOption.GetValue<string>());
			SetTextVariables(key, listItemDescription);
			return listItemDescription;
		}
		return TextObject.GetEmpty();
	}

	public TextObject GetItemName(string key, string identifier)
	{
		if (HasValue(key, out var option) && option is ListAdvancedStartOption listAdvancedStartOption)
		{
			return listAdvancedStartOption.GetListItemName(identifier);
		}
		return TextObject.GetEmpty();
	}

	private void SetTextVariables(string key, TextObject text)
	{
		foreach (AdvancedStartOption option in _options)
		{
			text.SetTextVariable(option.StringId, option.GetItemName());
		}
	}

	private bool HasValue(string key, out AdvancedStartOption option)
	{
		option = GetOption(key);
		return option != null;
	}

	public bool HasValue(string key)
	{
		AdvancedStartOption option;
		return HasValue(key, out option);
	}

	public T GetOption<T>(string key) where T : AdvancedStartOption
	{
		return GetOption(key) as T;
	}

	public bool HasAnyChange()
	{
		foreach (AdvancedStartOption option in _options)
		{
			if (option.HasValueChanged() && !option.GetIsHidden(this))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsEmpty()
	{
		return _options.Count == 0;
	}

	public IReadOnlyList<AdvancedStartOption> GetAllOptions()
	{
		return _options;
	}

	public AdvancedStartOptionsData GetChangedOptions()
	{
		List<AdvancedStartData> list = new List<AdvancedStartData>();
		foreach (AdvancedStartOption option in _options)
		{
			if (option.HasValueChanged() && !option.GetIsHidden(this))
			{
				list.Add(option.Value);
			}
		}
		return new AdvancedStartOptionsData(list);
	}
}
