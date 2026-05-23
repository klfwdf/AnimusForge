using System;
using System.Diagnostics;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000794 RID: 1940
	internal sealed class ObjectProgress
	{
		// Token: 0x06005433 RID: 21555 RVA: 0x00128AB0 File Offset: 0x00126CB0
		internal ObjectProgress()
		{
		}

		// Token: 0x06005434 RID: 21556 RVA: 0x00128ACC File Offset: 0x00126CCC
		[Conditional("SER_LOGGING")]
		private void Counter()
		{
			lock (this)
			{
				this.opRecordId = ObjectProgress.opRecordIdCount++;
				if (ObjectProgress.opRecordIdCount > 1000)
				{
					ObjectProgress.opRecordIdCount = 1;
				}
			}
		}

		// Token: 0x06005435 RID: 21557 RVA: 0x00128B28 File Offset: 0x00126D28
		internal void Init()
		{
			this.isInitial = false;
			this.count = 0;
			this.expectedType = BinaryTypeEnum.ObjectUrt;
			this.expectedTypeInformation = null;
			this.name = null;
			this.objectTypeEnum = InternalObjectTypeE.Empty;
			this.memberTypeEnum = InternalMemberTypeE.Empty;
			this.memberValueEnum = InternalMemberValueE.Empty;
			this.dtType = null;
			this.numItems = 0;
			this.nullCount = 0;
			this.typeInformation = null;
			this.memberLength = 0;
			this.binaryTypeEnumA = null;
			this.typeInformationA = null;
			this.memberNames = null;
			this.memberTypes = null;
			this.pr.Init();
		}

		// Token: 0x06005436 RID: 21558 RVA: 0x00128BB7 File Offset: 0x00126DB7
		internal void ArrayCountIncrement(int value)
		{
			this.count += value;
		}

		// Token: 0x06005437 RID: 21559 RVA: 0x00128BC8 File Offset: 0x00126DC8
		internal bool GetNext(out BinaryTypeEnum outBinaryTypeEnum, out object outTypeInformation)
		{
			outBinaryTypeEnum = BinaryTypeEnum.Primitive;
			outTypeInformation = null;
			if (this.objectTypeEnum == InternalObjectTypeE.Array)
			{
				if (this.count == this.numItems)
				{
					return false;
				}
				outBinaryTypeEnum = this.binaryTypeEnum;
				outTypeInformation = this.typeInformation;
				if (this.count == 0)
				{
					this.isInitial = false;
				}
				this.count++;
				return true;
			}
			else
			{
				if (this.count == this.memberLength && !this.isInitial)
				{
					return false;
				}
				outBinaryTypeEnum = this.binaryTypeEnumA[this.count];
				outTypeInformation = this.typeInformationA[this.count];
				if (this.count == 0)
				{
					this.isInitial = false;
				}
				this.name = this.memberNames[this.count];
				Type[] array = this.memberTypes;
				this.dtType = this.memberTypes[this.count];
				this.count++;
				return true;
			}
		}

		// Token: 0x0400260F RID: 9743
		internal static int opRecordIdCount = 1;

		// Token: 0x04002610 RID: 9744
		internal int opRecordId;

		// Token: 0x04002611 RID: 9745
		internal bool isInitial;

		// Token: 0x04002612 RID: 9746
		internal int count;

		// Token: 0x04002613 RID: 9747
		internal BinaryTypeEnum expectedType = BinaryTypeEnum.ObjectUrt;

		// Token: 0x04002614 RID: 9748
		internal object expectedTypeInformation;

		// Token: 0x04002615 RID: 9749
		internal string name;

		// Token: 0x04002616 RID: 9750
		internal InternalObjectTypeE objectTypeEnum;

		// Token: 0x04002617 RID: 9751
		internal InternalMemberTypeE memberTypeEnum;

		// Token: 0x04002618 RID: 9752
		internal InternalMemberValueE memberValueEnum;

		// Token: 0x04002619 RID: 9753
		internal Type dtType;

		// Token: 0x0400261A RID: 9754
		internal int numItems;

		// Token: 0x0400261B RID: 9755
		internal BinaryTypeEnum binaryTypeEnum;

		// Token: 0x0400261C RID: 9756
		internal object typeInformation;

		// Token: 0x0400261D RID: 9757
		internal int nullCount;

		// Token: 0x0400261E RID: 9758
		internal int memberLength;

		// Token: 0x0400261F RID: 9759
		internal BinaryTypeEnum[] binaryTypeEnumA;

		// Token: 0x04002620 RID: 9760
		internal object[] typeInformationA;

		// Token: 0x04002621 RID: 9761
		internal string[] memberNames;

		// Token: 0x04002622 RID: 9762
		internal Type[] memberTypes;

		// Token: 0x04002623 RID: 9763
		internal ParseRecord pr = new ParseRecord();
	}
}
