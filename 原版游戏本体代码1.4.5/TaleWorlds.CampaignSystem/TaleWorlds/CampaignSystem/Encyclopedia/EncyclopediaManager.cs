using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Encyclopedia;

public class EncyclopediaManager
{
	private Dictionary<Type, EncyclopediaPage> _pages;

	public const string HOME_ID = "Home";

	public const string LIST_PAGE_ID = "ListPage";

	public const string LAST_PAGE_ID = "LastPage";

	private Action<string, object> _executeLink;

	public IViewDataTracker ViewDataTracker { get; private set; }

	public void CreateEncyclopediaPages()
	{
		_pages = new Dictionary<Type, EncyclopediaPage>();
		ViewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
		List<Type> list = new List<Type>();
		List<Assembly> list2 = new List<Assembly>();
		Assembly assembly = typeof(EncyclopediaModelBase).Assembly;
		list2.Add(assembly);
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly2 in assemblies)
		{
			AssemblyName[] referencedAssemblies = assembly2.GetReferencedAssemblies();
			for (int j = 0; j < referencedAssemblies.Length; j++)
			{
				if (referencedAssemblies[j].ToString() == assembly.GetName().ToString())
				{
					list2.Add(assembly2);
					break;
				}
			}
		}
		foreach (Assembly item in list2)
		{
			list.AddRange(item.GetTypesSafe());
		}
		foreach (Type item2 in list)
		{
			if (!typeof(EncyclopediaPage).IsAssignableFrom(item2))
			{
				continue;
			}
			object[] customAttributesSafe = item2.GetCustomAttributesSafe(typeof(OverrideEncyclopediaModel), inherit: false);
			for (int i = 0; i < customAttributesSafe.Length; i++)
			{
				if (customAttributesSafe[i] is OverrideEncyclopediaModel overrideEncyclopediaModel)
				{
					EncyclopediaPage value = Activator.CreateInstance(item2) as EncyclopediaPage;
					Type[] pageTargetTypes = overrideEncyclopediaModel.PageTargetTypes;
					foreach (Type key in pageTargetTypes)
					{
						_pages.Add(key, value);
					}
				}
			}
		}
		foreach (Type item3 in list)
		{
			if (!typeof(EncyclopediaPage).IsAssignableFrom(item3))
			{
				continue;
			}
			object[] customAttributesSafe = item3.GetCustomAttributesSafe(typeof(EncyclopediaModel), inherit: false);
			for (int i = 0; i < customAttributesSafe.Length; i++)
			{
				if (!(customAttributesSafe[i] is EncyclopediaModel encyclopediaModel))
				{
					continue;
				}
				EncyclopediaPage value2 = Activator.CreateInstance(item3) as EncyclopediaPage;
				Type[] pageTargetTypes = encyclopediaModel.PageTargetTypes;
				foreach (Type key2 in pageTargetTypes)
				{
					if (!_pages.ContainsKey(key2))
					{
						_pages.Add(key2, value2);
					}
				}
			}
		}
	}

	public IEnumerable<EncyclopediaPage> GetEncyclopediaPages()
	{
		return _pages.Values.Distinct();
	}

	public EncyclopediaPage GetPageOf(Type type)
	{
		return _pages[type];
	}

	public string GetIdentifier(Type type)
	{
		return _pages[type].GetIdentifier(type);
	}

	public void GoToLink(string pageType, string stringID)
	{
		if (_executeLink == null || string.IsNullOrEmpty(pageType))
		{
			return;
		}
		if (pageType == "Home" || pageType == "LastPage")
		{
			_executeLink(pageType, null);
			return;
		}
		if (pageType == "ListPage")
		{
			EncyclopediaPage arg = Campaign.Current.EncyclopediaManager.GetEncyclopediaPages().FirstOrDefault((EncyclopediaPage e) => e.HasIdentifier(stringID));
			_executeLink(pageType, arg);
			return;
		}
		EncyclopediaPage encyclopediaPage = Campaign.Current.EncyclopediaManager.GetEncyclopediaPages().FirstOrDefault((EncyclopediaPage e) => e.HasIdentifier(pageType));
		MBObjectBase mBObjectBase = encyclopediaPage.GetObject(pageType, stringID);
		if (encyclopediaPage != null && encyclopediaPage.IsValidEncyclopediaItem(mBObjectBase))
		{
			_executeLink(pageType, mBObjectBase);
		}
	}

	public void GoToLink(string link)
	{
		int num = link.IndexOf('-');
		if (num > 0)
		{
			string pageType = link.Substring(0, num);
			string stringID = link.Substring(num + 1);
			GoToLink(pageType, stringID);
		}
		else
		{
			Debug.FailedAssert("Failed to resolve encyclopedia link: " + link, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\EncyclopediaManager.cs", "GoToLink", 165);
		}
	}

	public void SetLinkCallback(Action<string, object> ExecuteLink)
	{
		_executeLink = ExecuteLink;
	}
}
