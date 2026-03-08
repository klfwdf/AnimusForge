using System;
using System.Collections;
using System.Collections.Generic;

namespace System
{
	// Token: 0x020000BD RID: 189
	internal class ConfigNode
	{
		// Token: 0x06000AF3 RID: 2803 RVA: 0x00022B05 File Offset: 0x00020D05
		internal ConfigNode(string name, ConfigNode parent)
		{
			this.m_name = name;
			this.m_parent = parent;
		}

		// Token: 0x17000151 RID: 337
		// (get) Token: 0x06000AF4 RID: 2804 RVA: 0x00022B33 File Offset: 0x00020D33
		internal string Name
		{
			get
			{
				return this.m_name;
			}
		}

		// Token: 0x17000152 RID: 338
		// (get) Token: 0x06000AF5 RID: 2805 RVA: 0x00022B3B File Offset: 0x00020D3B
		// (set) Token: 0x06000AF6 RID: 2806 RVA: 0x00022B43 File Offset: 0x00020D43
		internal string Value
		{
			get
			{
				return this.m_value;
			}
			set
			{
				this.m_value = value;
			}
		}

		// Token: 0x17000153 RID: 339
		// (get) Token: 0x06000AF7 RID: 2807 RVA: 0x00022B4C File Offset: 0x00020D4C
		internal ConfigNode Parent
		{
			get
			{
				return this.m_parent;
			}
		}

		// Token: 0x17000154 RID: 340
		// (get) Token: 0x06000AF8 RID: 2808 RVA: 0x00022B54 File Offset: 0x00020D54
		internal List<ConfigNode> Children
		{
			get
			{
				return this.m_children;
			}
		}

		// Token: 0x17000155 RID: 341
		// (get) Token: 0x06000AF9 RID: 2809 RVA: 0x00022B5C File Offset: 0x00020D5C
		internal List<DictionaryEntry> Attributes
		{
			get
			{
				return this.m_attributes;
			}
		}

		// Token: 0x06000AFA RID: 2810 RVA: 0x00022B64 File Offset: 0x00020D64
		internal void AddChild(ConfigNode child)
		{
			child.m_parent = this;
			this.m_children.Add(child);
		}

		// Token: 0x06000AFB RID: 2811 RVA: 0x00022B79 File Offset: 0x00020D79
		internal int AddAttribute(string key, string value)
		{
			this.m_attributes.Add(new DictionaryEntry(key, value));
			return this.m_attributes.Count - 1;
		}

		// Token: 0x06000AFC RID: 2812 RVA: 0x00022B9A File Offset: 0x00020D9A
		internal void ReplaceAttribute(int index, string key, string value)
		{
			this.m_attributes[index] = new DictionaryEntry(key, value);
		}

		// Token: 0x0400045A RID: 1114
		private string m_name;

		// Token: 0x0400045B RID: 1115
		private string m_value;

		// Token: 0x0400045C RID: 1116
		private ConfigNode m_parent;

		// Token: 0x0400045D RID: 1117
		private List<ConfigNode> m_children = new List<ConfigNode>(5);

		// Token: 0x0400045E RID: 1118
		private List<DictionaryEntry> m_attributes = new List<DictionaryEntry>(5);
	}
}
