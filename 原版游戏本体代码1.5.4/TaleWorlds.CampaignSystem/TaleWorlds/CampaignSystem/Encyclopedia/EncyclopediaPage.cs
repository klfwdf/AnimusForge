using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Encyclopedia;

public abstract class EncyclopediaPage
{
	private readonly Type[] _identifierTypes;

	private readonly Dictionary<Type, string> _identifiers;

	private IEnumerable<EncyclopediaFilterGroup> _filters;

	private IEnumerable<EncyclopediaListItem> _items;

	private IEnumerable<EncyclopediaSortController> _sortControllers;

	public int HomePageOrderIndex { get; protected set; }

	public EncyclopediaPage Parent { get; }

	protected abstract IEnumerable<EncyclopediaListItem> InitializeListItems();

	protected abstract IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems();

	protected abstract IEnumerable<EncyclopediaSortController> InitializeSortControllers();

	public EncyclopediaPage()
	{
		_filters = InitializeFilterItems();
		_items = InitializeListItems();
		_sortControllers = new List<EncyclopediaSortController>
		{
			new EncyclopediaSortController(new TextObject("{=koX9okuG}None"), new EncyclopediaListItemNameComparer())
		};
		((List<EncyclopediaSortController>)_sortControllers).AddRange(InitializeSortControllers());
		object[] customAttributesSafe = GetType().GetCustomAttributesSafe(typeof(EncyclopediaModel), inherit: true);
		foreach (object obj in customAttributesSafe)
		{
			if (obj is EncyclopediaModel)
			{
				_identifierTypes = (obj as EncyclopediaModel).PageTargetTypes;
				break;
			}
		}
		_identifiers = new Dictionary<Type, string>();
		Type[] identifierTypes = _identifierTypes;
		foreach (Type type in identifierTypes)
		{
			if (Game.Current.ObjectManager.HasType(type))
			{
				_identifiers.Add(type, Game.Current.ObjectManager.FindRegisteredClassPrefix(type));
				continue;
			}
			string text = type.Name.ToString();
			if (text == "Clan")
			{
				text = "Faction";
			}
			_identifiers.Add(type, text);
		}
	}

	public virtual bool IsRelevant()
	{
		return true;
	}

	public bool HasIdentifierType(Type identifierType)
	{
		return _identifierTypes.Contains(identifierType);
	}

	internal bool HasIdentifier(string identifier)
	{
		return _identifiers.ContainsValue(identifier);
	}

	public string GetIdentifier(Type identifierType)
	{
		if (_identifiers.ContainsKey(identifierType))
		{
			return _identifiers[identifierType];
		}
		return "";
	}

	public string[] GetIdentifierNames()
	{
		return _identifiers.Values.ToArray();
	}

	public bool IsFiltered(object o)
	{
		foreach (EncyclopediaFilterGroup filterItem in GetFilterItems())
		{
			if (!filterItem.Predicate(o))
			{
				return true;
			}
		}
		return false;
	}

	public virtual string GetViewFullyQualifiedName()
	{
		return "";
	}

	public virtual string GetStringID()
	{
		return "";
	}

	public virtual TextObject GetName()
	{
		return TextObject.GetEmpty();
	}

	public virtual MBObjectBase GetObject(string typeName, string stringID)
	{
		return MBObjectManager.Instance.GetObject(typeName, stringID);
	}

	public virtual bool IsValidEncyclopediaItem(object o)
	{
		return false;
	}

	public virtual TextObject GetDescriptionText()
	{
		return TextObject.GetEmpty();
	}

	public IEnumerable<EncyclopediaListItem> GetListItems()
	{
		return _items;
	}

	public IEnumerable<EncyclopediaFilterGroup> GetFilterItems()
	{
		return _filters;
	}

	public IEnumerable<EncyclopediaSortController> GetSortControllers()
	{
		return _sortControllers;
	}
}
