using System.Collections.Generic;
using TaleWorlds.CampaignSystem.AdvancedStartOptions;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.AdvancedStartOptions;

public class ListAdvancedStartOption : AdvancedStartOption
{
	public delegate bool ListItemCondition(AdvancedStartOptions options, out TextObject disabledText);

	private readonly List<(string Identifier, ListItemCondition Condition)> _items;

	public ListAdvancedStartOption(string stringId, string categoryId, IReadOnlyList<(string Identifier, ListItemCondition Condition)> items, AdvancedStartOptionCondition onCondition, string defaultValue = "")
		: base(new AdvancedStartData<string>(stringId, categoryId, defaultValue), onCondition)
	{
		_items = new List<(string, ListItemCondition)>(items);
		for (int i = 0; i < _items.Count; i++)
		{
		}
	}

	public IReadOnlyList<(string Identifier, ListItemCondition Condition)> GetItems()
	{
		return _items;
	}

	public void AddItem((string Identifier, ListItemCondition Condition) item)
	{
		for (int i = 0; i < _items.Count; i++)
		{
			if (_items[i].Identifier == item.Identifier)
			{
				Debug.Print("Overriding start option item id: " + item.Identifier);
				_items[i] = item;
				return;
			}
		}
		_items.Add(item);
	}

	private bool TryGetItem(string identifier, out (string Identifier, ListItemCondition Condition) item)
	{
		if (_items == null)
		{
			item = default((string, ListItemCondition));
			return false;
		}
		for (int i = 0; i < _items.Count; i++)
		{
			if (_items[i].Identifier == identifier)
			{
				item = _items[i];
				return true;
			}
		}
		item = default((string, ListItemCondition));
		return false;
	}

	internal TextObject GetListItemName(string identifier)
	{
		return Module.CurrentModule.GlobalTextManager.FindText("str_campaign_starting_options_item_name", identifier);
	}

	internal TextObject GetListItemDescription(string identifier)
	{
		if (Module.CurrentModule.GlobalTextManager.TryGetText("str_campaign_starting_options_item_description", identifier, out var text))
		{
			return text;
		}
		return TextObject.GetEmpty();
	}

	public bool GetItemCondition(string identifier, AdvancedStartOptions options, out TextObject disabledText)
	{
		bool result = false;
		disabledText = null;
		if (TryGetItem(identifier, out (string, ListItemCondition) item) && item.Item2 != null)
		{
			result = item.Item2(options, out disabledText);
		}
		return result;
	}

	public bool RemoveItem(string identifier)
	{
		for (int i = 0; i < _items.Count; i++)
		{
			if (_items[i].Identifier == identifier)
			{
				_items.RemoveAt(i);
				return true;
			}
		}
		return false;
	}
}
