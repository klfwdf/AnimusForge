using System;
using System.Reflection;

namespace System.Runtime.Serialization
{
	// Token: 0x0200075C RID: 1884
	internal class ValueTypeFixupInfo
	{
		// Token: 0x060052F5 RID: 21237 RVA: 0x00123B98 File Offset: 0x00121D98
		public ValueTypeFixupInfo(long containerID, FieldInfo member, int[] parentIndex)
		{
			if (member == null && parentIndex == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustSupplyParent"));
			}
			if (containerID == 0L && member == null)
			{
				this.m_containerID = containerID;
				this.m_parentField = member;
				this.m_parentIndex = parentIndex;
			}
			if (member != null)
			{
				if (parentIndex != null)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_MemberAndArray"));
				}
				if (member.FieldType.IsValueType && containerID == 0L)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_MustSupplyContainer"));
				}
			}
			this.m_containerID = containerID;
			this.m_parentField = member;
			this.m_parentIndex = parentIndex;
		}

		// Token: 0x17000DB5 RID: 3509
		// (get) Token: 0x060052F6 RID: 21238 RVA: 0x00123C39 File Offset: 0x00121E39
		public long ContainerID
		{
			get
			{
				return this.m_containerID;
			}
		}

		// Token: 0x17000DB6 RID: 3510
		// (get) Token: 0x060052F7 RID: 21239 RVA: 0x00123C41 File Offset: 0x00121E41
		public FieldInfo ParentField
		{
			get
			{
				return this.m_parentField;
			}
		}

		// Token: 0x17000DB7 RID: 3511
		// (get) Token: 0x060052F8 RID: 21240 RVA: 0x00123C49 File Offset: 0x00121E49
		public int[] ParentIndex
		{
			get
			{
				return this.m_parentIndex;
			}
		}

		// Token: 0x040024CA RID: 9418
		private long m_containerID;

		// Token: 0x040024CB RID: 9419
		private FieldInfo m_parentField;

		// Token: 0x040024CC RID: 9420
		private int[] m_parentIndex;
	}
}
