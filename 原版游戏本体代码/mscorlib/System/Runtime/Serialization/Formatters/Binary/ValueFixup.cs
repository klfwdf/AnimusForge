using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x0200079A RID: 1946
	internal sealed class ValueFixup
	{
		// Token: 0x06005457 RID: 21591 RVA: 0x00129359 File Offset: 0x00127559
		internal ValueFixup(Array arrayObj, int[] indexMap)
		{
			this.valueFixupEnum = ValueFixupEnum.Array;
			this.arrayObj = arrayObj;
			this.indexMap = indexMap;
		}

		// Token: 0x06005458 RID: 21592 RVA: 0x00129376 File Offset: 0x00127576
		internal ValueFixup(object memberObject, string memberName, ReadObjectInfo objectInfo)
		{
			this.valueFixupEnum = ValueFixupEnum.Member;
			this.memberObject = memberObject;
			this.memberName = memberName;
			this.objectInfo = objectInfo;
		}

		// Token: 0x06005459 RID: 21593 RVA: 0x0012939C File Offset: 0x0012759C
		[SecurityCritical]
		internal void Fixup(ParseRecord record, ParseRecord parent)
		{
			object prnewObj = record.PRnewObj;
			switch (this.valueFixupEnum)
			{
			case ValueFixupEnum.Array:
				this.arrayObj.SetValue(prnewObj, this.indexMap);
				return;
			case ValueFixupEnum.Header:
			{
				Type typeFromHandle = typeof(Header);
				if (ValueFixup.valueInfo == null)
				{
					MemberInfo[] member = typeFromHandle.GetMember("Value");
					if (member.Length != 1)
					{
						throw new SerializationException(Environment.GetResourceString("Serialization_HeaderReflection", new object[] { member.Length }));
					}
					ValueFixup.valueInfo = member[0];
				}
				FormatterServices.SerializationSetValue(ValueFixup.valueInfo, this.header, prnewObj);
				return;
			}
			case ValueFixupEnum.Member:
			{
				if (this.objectInfo.isSi)
				{
					this.objectInfo.objectManager.RecordDelayedFixup(parent.PRobjectId, this.memberName, record.PRobjectId);
					return;
				}
				MemberInfo memberInfo = this.objectInfo.GetMemberInfo(this.memberName);
				if (memberInfo != null)
				{
					this.objectInfo.objectManager.RecordFixup(parent.PRobjectId, memberInfo, record.PRobjectId);
				}
				return;
			}
			default:
				return;
			}
		}

		// Token: 0x04002659 RID: 9817
		internal ValueFixupEnum valueFixupEnum;

		// Token: 0x0400265A RID: 9818
		internal Array arrayObj;

		// Token: 0x0400265B RID: 9819
		internal int[] indexMap;

		// Token: 0x0400265C RID: 9820
		internal object header;

		// Token: 0x0400265D RID: 9821
		internal object memberObject;

		// Token: 0x0400265E RID: 9822
		internal static volatile MemberInfo valueInfo;

		// Token: 0x0400265F RID: 9823
		internal ReadObjectInfo objectInfo;

		// Token: 0x04002660 RID: 9824
		internal string memberName;
	}
}
