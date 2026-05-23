using System;
using System.Reflection;
using System.Xml;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameMenus
{
	// Token: 0x020000EA RID: 234
	public class WaitMenuOption
	{
		// Token: 0x170005F4 RID: 1524
		// (get) Token: 0x060015BB RID: 5563 RVA: 0x00061D79 File Offset: 0x0005FF79
		// (set) Token: 0x060015BC RID: 5564 RVA: 0x00061D81 File Offset: 0x0005FF81
		public int Priority { get; private set; }

		// Token: 0x060015BD RID: 5565 RVA: 0x00061D8A File Offset: 0x0005FF8A
		internal WaitMenuOption()
		{
			this.Priority = 100;
			this._text = null;
			this._tooltip = "";
		}

		// Token: 0x060015BE RID: 5566 RVA: 0x00061DAC File Offset: 0x0005FFAC
		internal WaitMenuOption(string idString, TextObject text, WaitMenuOption.OnConditionDelegate condition, WaitMenuOption.OnConsequenceDelegate consequence, int priority = 100, string tooltip = "")
		{
			this._idString = idString;
			this._text = text;
			this.OnCondition = condition;
			this.OnConsequence = consequence;
			this.Priority = priority;
			this._tooltip = tooltip;
		}

		// Token: 0x060015BF RID: 5567 RVA: 0x00061DE4 File Offset: 0x0005FFE4
		public bool GetConditionsHold(Game game, MapState mapState)
		{
			if (this.OnCondition != null)
			{
				MenuCallbackArgs args = new MenuCallbackArgs(mapState, this.Text);
				return this.OnCondition(args);
			}
			return true;
		}

		// Token: 0x170005F5 RID: 1525
		// (get) Token: 0x060015C0 RID: 5568 RVA: 0x00061E14 File Offset: 0x00060014
		public TextObject Text
		{
			get
			{
				return this._text;
			}
		}

		// Token: 0x170005F6 RID: 1526
		// (get) Token: 0x060015C1 RID: 5569 RVA: 0x00061E1C File Offset: 0x0006001C
		public string IdString
		{
			get
			{
				return this._idString;
			}
		}

		// Token: 0x170005F7 RID: 1527
		// (get) Token: 0x060015C2 RID: 5570 RVA: 0x00061E24 File Offset: 0x00060024
		public string Tooltip
		{
			get
			{
				return this._tooltip;
			}
		}

		// Token: 0x170005F8 RID: 1528
		// (get) Token: 0x060015C3 RID: 5571 RVA: 0x00061E2C File Offset: 0x0006002C
		public bool IsLeave
		{
			get
			{
				return this._isLeave;
			}
		}

		// Token: 0x060015C4 RID: 5572 RVA: 0x00061E34 File Offset: 0x00060034
		public void RunConsequence(Game game, MapState mapState)
		{
			if (this.OnConsequence != null)
			{
				MenuCallbackArgs args = new MenuCallbackArgs(mapState, this.Text);
				this.OnConsequence(args);
			}
		}

		// Token: 0x060015C5 RID: 5573 RVA: 0x00061E64 File Offset: 0x00060064
		public void Deserialize(XmlNode node, Type typeOfWaitMenusCallbacks)
		{
			if (node.Attributes == null)
			{
				throw new TWXmlLoadException("node.Attributes != null");
			}
			this._idString = node.Attributes["id"].Value;
			XmlNode xmlNode = node.Attributes["text"];
			if (xmlNode != null)
			{
				this._text = new TextObject(xmlNode.InnerText, null);
			}
			if (node.Attributes["is_leave"] != null)
			{
				this._isLeave = true;
			}
			XmlNode xmlNode2 = node.Attributes["on_condition"];
			if (xmlNode2 != null)
			{
				string innerText = xmlNode2.InnerText;
				this._methodOnCondition = typeOfWaitMenusCallbacks.GetMethod(innerText);
				if (this._methodOnCondition == null)
				{
					throw new MBNotFoundException("Can not find WaitMenuOption condition:" + innerText);
				}
				this.OnCondition = (WaitMenuOption.OnConditionDelegate)Delegate.CreateDelegate(typeof(WaitMenuOption.OnConditionDelegate), null, this._methodOnCondition);
			}
			XmlNode xmlNode3 = node.Attributes["on_consequence"];
			if (xmlNode3 != null)
			{
				string innerText2 = xmlNode3.InnerText;
				this._methodOnConsequence = typeOfWaitMenusCallbacks.GetMethod(innerText2);
				if (this._methodOnConsequence == null)
				{
					throw new MBNotFoundException("Can not find WaitMenuOption consequence:" + innerText2);
				}
				this.OnConsequence = (WaitMenuOption.OnConsequenceDelegate)Delegate.CreateDelegate(typeof(WaitMenuOption.OnConsequenceDelegate), null, this._methodOnConsequence);
			}
		}

		// Token: 0x0400072C RID: 1836
		private string _idString;

		// Token: 0x0400072D RID: 1837
		private TextObject _text;

		// Token: 0x0400072E RID: 1838
		private string _tooltip;

		// Token: 0x0400072F RID: 1839
		private MethodInfo _methodOnCondition;

		// Token: 0x04000730 RID: 1840
		public WaitMenuOption.OnConditionDelegate OnCondition;

		// Token: 0x04000731 RID: 1841
		private MethodInfo _methodOnConsequence;

		// Token: 0x04000732 RID: 1842
		public WaitMenuOption.OnConsequenceDelegate OnConsequence;

		// Token: 0x04000733 RID: 1843
		private bool _isLeave;

		// Token: 0x02000566 RID: 1382
		// (Invoke) Token: 0x06004CEA RID: 19690
		public delegate bool OnConditionDelegate(MenuCallbackArgs args);

		// Token: 0x02000567 RID: 1383
		// (Invoke) Token: 0x06004CEE RID: 19694
		public delegate void OnConsequenceDelegate(MenuCallbackArgs args);
	}
}
