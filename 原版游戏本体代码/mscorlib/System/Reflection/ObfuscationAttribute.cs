using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x02000610 RID: 1552
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	public sealed class ObfuscationAttribute : Attribute
	{
		// Token: 0x17000B12 RID: 2834
		// (get) Token: 0x060047F4 RID: 18420 RVA: 0x00105E6B File Offset: 0x0010406B
		// (set) Token: 0x060047F5 RID: 18421 RVA: 0x00105E73 File Offset: 0x00104073
		public bool StripAfterObfuscation
		{
			get
			{
				return this.m_strip;
			}
			set
			{
				this.m_strip = value;
			}
		}

		// Token: 0x17000B13 RID: 2835
		// (get) Token: 0x060047F6 RID: 18422 RVA: 0x00105E7C File Offset: 0x0010407C
		// (set) Token: 0x060047F7 RID: 18423 RVA: 0x00105E84 File Offset: 0x00104084
		public bool Exclude
		{
			get
			{
				return this.m_exclude;
			}
			set
			{
				this.m_exclude = value;
			}
		}

		// Token: 0x17000B14 RID: 2836
		// (get) Token: 0x060047F8 RID: 18424 RVA: 0x00105E8D File Offset: 0x0010408D
		// (set) Token: 0x060047F9 RID: 18425 RVA: 0x00105E95 File Offset: 0x00104095
		public bool ApplyToMembers
		{
			get
			{
				return this.m_applyToMembers;
			}
			set
			{
				this.m_applyToMembers = value;
			}
		}

		// Token: 0x17000B15 RID: 2837
		// (get) Token: 0x060047FA RID: 18426 RVA: 0x00105E9E File Offset: 0x0010409E
		// (set) Token: 0x060047FB RID: 18427 RVA: 0x00105EA6 File Offset: 0x001040A6
		public string Feature
		{
			get
			{
				return this.m_feature;
			}
			set
			{
				this.m_feature = value;
			}
		}

		// Token: 0x04001DC6 RID: 7622
		private bool m_strip = true;

		// Token: 0x04001DC7 RID: 7623
		private bool m_exclude = true;

		// Token: 0x04001DC8 RID: 7624
		private bool m_applyToMembers = true;

		// Token: 0x04001DC9 RID: 7625
		private string m_feature = "all";
	}
}
