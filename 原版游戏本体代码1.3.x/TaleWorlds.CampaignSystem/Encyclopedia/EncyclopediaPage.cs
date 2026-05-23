using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Encyclopedia
{
	// Token: 0x02000175 RID: 373
	public abstract class EncyclopediaPage
	{
		// Token: 0x06001B20 RID: 6944
		protected abstract IEnumerable<EncyclopediaListItem> InitializeListItems();

		// Token: 0x06001B21 RID: 6945
		protected abstract IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems();

		// Token: 0x06001B22 RID: 6946
		protected abstract IEnumerable<EncyclopediaSortController> InitializeSortControllers();

		// Token: 0x170006EA RID: 1770
		// (get) Token: 0x06001B23 RID: 6947 RVA: 0x0008B59E File Offset: 0x0008979E
		// (set) Token: 0x06001B24 RID: 6948 RVA: 0x0008B5A6 File Offset: 0x000897A6
		public int HomePageOrderIndex { get; protected set; }

		// Token: 0x170006EB RID: 1771
		// (get) Token: 0x06001B25 RID: 6949 RVA: 0x0008B5AF File Offset: 0x000897AF
		public EncyclopediaPage Parent { get; }

		// Token: 0x06001B26 RID: 6950 RVA: 0x0008B5B8 File Offset: 0x000897B8
		public EncyclopediaPage()
		{
			this._filters = this.InitializeFilterItems();
			this._items = this.InitializeListItems();
			this._sortControllers = new List<EncyclopediaSortController>
			{
				new EncyclopediaSortController(new TextObject("{=koX9okuG}None", null), new EncyclopediaListItemNameComparer())
			};
			((List<EncyclopediaSortController>)this._sortControllers).AddRange(this.InitializeSortControllers());
			foreach (object obj in base.GetType().GetCustomAttributesSafe(typeof(EncyclopediaModel), true))
			{
				if (obj is EncyclopediaModel)
				{
					this._identifierTypes = (obj as EncyclopediaModel).PageTargetTypes;
					break;
				}
			}
			this._identifiers = new Dictionary<Type, string>();
			foreach (Type type in this._identifierTypes)
			{
				if (Game.Current.ObjectManager.HasType(type))
				{
					this._identifiers.Add(type, Game.Current.ObjectManager.FindRegisteredClassPrefix(type));
				}
				else
				{
					string text = type.Name.ToString();
					if (text == "Clan")
					{
						text = "Faction";
					}
					this._identifiers.Add(type, text);
				}
			}
		}

		// Token: 0x06001B27 RID: 6951 RVA: 0x0008B6ED File Offset: 0x000898ED
		public virtual bool IsRelevant()
		{
			return true;
		}

		// Token: 0x06001B28 RID: 6952 RVA: 0x0008B6F0 File Offset: 0x000898F0
		public bool HasIdentifierType(Type identifierType)
		{
			return this._identifierTypes.Contains(identifierType);
		}

		// Token: 0x06001B29 RID: 6953 RVA: 0x0008B6FE File Offset: 0x000898FE
		internal bool HasIdentifier(string identifier)
		{
			return this._identifiers.ContainsValue(identifier);
		}

		// Token: 0x06001B2A RID: 6954 RVA: 0x0008B70C File Offset: 0x0008990C
		public string GetIdentifier(Type identifierType)
		{
			if (this._identifiers.ContainsKey(identifierType))
			{
				return this._identifiers[identifierType];
			}
			return "";
		}

		// Token: 0x06001B2B RID: 6955 RVA: 0x0008B72E File Offset: 0x0008992E
		public string[] GetIdentifierNames()
		{
			return this._identifiers.Values.ToArray<string>();
		}

		// Token: 0x06001B2C RID: 6956 RVA: 0x0008B740 File Offset: 0x00089940
		public bool IsFiltered(object o)
		{
			using (IEnumerator<EncyclopediaFilterGroup> enumerator = this.GetFilterItems().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (!enumerator.Current.Predicate(o))
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06001B2D RID: 6957 RVA: 0x0008B79C File Offset: 0x0008999C
		public virtual string GetViewFullyQualifiedName()
		{
			return "";
		}

		// Token: 0x06001B2E RID: 6958 RVA: 0x0008B7A3 File Offset: 0x000899A3
		public virtual string GetStringID()
		{
			return "";
		}

		// Token: 0x06001B2F RID: 6959 RVA: 0x0008B7AA File Offset: 0x000899AA
		public virtual TextObject GetName()
		{
			return TextObject.GetEmpty();
		}

		// Token: 0x06001B30 RID: 6960 RVA: 0x0008B7B1 File Offset: 0x000899B1
		public virtual MBObjectBase GetObject(string typeName, string stringID)
		{
			return MBObjectManager.Instance.GetObject(typeName, stringID);
		}

		// Token: 0x06001B31 RID: 6961 RVA: 0x0008B7BF File Offset: 0x000899BF
		public virtual bool IsValidEncyclopediaItem(object o)
		{
			return false;
		}

		// Token: 0x06001B32 RID: 6962 RVA: 0x0008B7C2 File Offset: 0x000899C2
		public virtual TextObject GetDescriptionText()
		{
			return TextObject.GetEmpty();
		}

		// Token: 0x06001B33 RID: 6963 RVA: 0x0008B7C9 File Offset: 0x000899C9
		public IEnumerable<EncyclopediaListItem> GetListItems()
		{
			return this._items;
		}

		// Token: 0x06001B34 RID: 6964 RVA: 0x0008B7D1 File Offset: 0x000899D1
		public IEnumerable<EncyclopediaFilterGroup> GetFilterItems()
		{
			return this._filters;
		}

		// Token: 0x06001B35 RID: 6965 RVA: 0x0008B7D9 File Offset: 0x000899D9
		public IEnumerable<EncyclopediaSortController> GetSortControllers()
		{
			return this._sortControllers;
		}

		// Token: 0x04000916 RID: 2326
		private readonly Type[] _identifierTypes;

		// Token: 0x04000917 RID: 2327
		private readonly Dictionary<Type, string> _identifiers;

		// Token: 0x04000918 RID: 2328
		private IEnumerable<EncyclopediaFilterGroup> _filters;

		// Token: 0x04000919 RID: 2329
		private IEnumerable<EncyclopediaListItem> _items;

		// Token: 0x0400091A RID: 2330
		private IEnumerable<EncyclopediaSortController> _sortControllers;
	}
}
