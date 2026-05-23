using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Encyclopedia
{
	// Token: 0x02000174 RID: 372
	public class EncyclopediaManager
	{
		// Token: 0x170006E9 RID: 1769
		// (get) Token: 0x06001B16 RID: 6934 RVA: 0x0008B148 File Offset: 0x00089348
		// (set) Token: 0x06001B17 RID: 6935 RVA: 0x0008B150 File Offset: 0x00089350
		public IViewDataTracker ViewDataTracker { get; private set; }

		// Token: 0x06001B18 RID: 6936 RVA: 0x0008B15C File Offset: 0x0008935C
		public void CreateEncyclopediaPages()
		{
			this._pages = new Dictionary<Type, EncyclopediaPage>();
			this.ViewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
			List<Type> list = new List<Type>();
			List<Assembly> list2 = new List<Assembly>();
			Assembly assembly = typeof(EncyclopediaModelBase).Assembly;
			list2.Add(assembly);
			foreach (Assembly assembly2 in AppDomain.CurrentDomain.GetAssemblies())
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
			foreach (Assembly assembly3 in list2)
			{
				list.AddRange(assembly3.GetTypesSafe(null));
			}
			foreach (Type type in list)
			{
				if (typeof(EncyclopediaPage).IsAssignableFrom(type))
				{
					object[] customAttributesSafe = type.GetCustomAttributesSafe(typeof(OverrideEncyclopediaModel), false);
					for (int i = 0; i < customAttributesSafe.Length; i++)
					{
						OverrideEncyclopediaModel overrideEncyclopediaModel = customAttributesSafe[i] as OverrideEncyclopediaModel;
						if (overrideEncyclopediaModel != null)
						{
							EncyclopediaPage value = Activator.CreateInstance(type) as EncyclopediaPage;
							foreach (Type key in overrideEncyclopediaModel.PageTargetTypes)
							{
								this._pages.Add(key, value);
							}
						}
					}
				}
			}
			foreach (Type type2 in list)
			{
				if (typeof(EncyclopediaPage).IsAssignableFrom(type2))
				{
					object[] customAttributesSafe = type2.GetCustomAttributesSafe(typeof(EncyclopediaModel), false);
					for (int i = 0; i < customAttributesSafe.Length; i++)
					{
						EncyclopediaModel encyclopediaModel = customAttributesSafe[i] as EncyclopediaModel;
						if (encyclopediaModel != null)
						{
							EncyclopediaPage value2 = Activator.CreateInstance(type2) as EncyclopediaPage;
							foreach (Type key2 in encyclopediaModel.PageTargetTypes)
							{
								if (!this._pages.ContainsKey(key2))
								{
									this._pages.Add(key2, value2);
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06001B19 RID: 6937 RVA: 0x0008B3F8 File Offset: 0x000895F8
		public IEnumerable<EncyclopediaPage> GetEncyclopediaPages()
		{
			return this._pages.Values.Distinct<EncyclopediaPage>();
		}

		// Token: 0x06001B1A RID: 6938 RVA: 0x0008B40A File Offset: 0x0008960A
		public EncyclopediaPage GetPageOf(Type type)
		{
			return this._pages[type];
		}

		// Token: 0x06001B1B RID: 6939 RVA: 0x0008B418 File Offset: 0x00089618
		public string GetIdentifier(Type type)
		{
			return this._pages[type].GetIdentifier(type);
		}

		// Token: 0x06001B1C RID: 6940 RVA: 0x0008B42C File Offset: 0x0008962C
		public void GoToLink(string pageType, string stringID)
		{
			if (this._executeLink == null || string.IsNullOrEmpty(pageType))
			{
				return;
			}
			if (pageType == "Home" || pageType == "LastPage")
			{
				this._executeLink(pageType, null);
				return;
			}
			if (pageType == "ListPage")
			{
				EncyclopediaPage arg = Campaign.Current.EncyclopediaManager.GetEncyclopediaPages().FirstOrDefault((EncyclopediaPage e) => e.HasIdentifier(stringID));
				this._executeLink(pageType, arg);
				return;
			}
			EncyclopediaPage encyclopediaPage = Campaign.Current.EncyclopediaManager.GetEncyclopediaPages().FirstOrDefault((EncyclopediaPage e) => e.HasIdentifier(pageType));
			MBObjectBase @object = encyclopediaPage.GetObject(pageType, stringID);
			if (encyclopediaPage != null && encyclopediaPage.IsValidEncyclopediaItem(@object))
			{
				this._executeLink(pageType, @object);
			}
		}

		// Token: 0x06001B1D RID: 6941 RVA: 0x0008B538 File Offset: 0x00089738
		public void GoToLink(string link)
		{
			int num = link.IndexOf('-');
			if (num > 0)
			{
				string pageType = link.Substring(0, num);
				string stringID = link.Substring(num + 1);
				this.GoToLink(pageType, stringID);
				return;
			}
			Debug.FailedAssert("Failed to resolve encyclopedia link: " + link, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encyclopedia\\EncyclopediaManager.cs", "GoToLink", 165);
		}

		// Token: 0x06001B1E RID: 6942 RVA: 0x0008B58D File Offset: 0x0008978D
		public void SetLinkCallback(Action<string, object> ExecuteLink)
		{
			this._executeLink = ExecuteLink;
		}

		// Token: 0x04000910 RID: 2320
		private Dictionary<Type, EncyclopediaPage> _pages;

		// Token: 0x04000912 RID: 2322
		public const string HOME_ID = "Home";

		// Token: 0x04000913 RID: 2323
		public const string LIST_PAGE_ID = "ListPage";

		// Token: 0x04000914 RID: 2324
		public const string LAST_PAGE_ID = "LastPage";

		// Token: 0x04000915 RID: 2325
		private Action<string, object> _executeLink;
	}
}
