using System;
using System.Diagnostics;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x0200078C RID: 1932
	internal sealed class BinaryObjectWithMap : IStreamable
	{
		// Token: 0x06005403 RID: 21507 RVA: 0x00127E9D File Offset: 0x0012609D
		internal BinaryObjectWithMap()
		{
		}

		// Token: 0x06005404 RID: 21508 RVA: 0x00127EA5 File Offset: 0x001260A5
		internal BinaryObjectWithMap(BinaryHeaderEnum binaryHeaderEnum)
		{
			this.binaryHeaderEnum = binaryHeaderEnum;
		}

		// Token: 0x06005405 RID: 21509 RVA: 0x00127EB4 File Offset: 0x001260B4
		internal void Set(int objectId, string name, int numMembers, string[] memberNames, int assemId)
		{
			this.objectId = objectId;
			this.name = name;
			this.numMembers = numMembers;
			this.memberNames = memberNames;
			this.assemId = assemId;
			if (assemId > 0)
			{
				this.binaryHeaderEnum = BinaryHeaderEnum.ObjectWithMapAssemId;
				return;
			}
			this.binaryHeaderEnum = BinaryHeaderEnum.ObjectWithMap;
		}

		// Token: 0x06005406 RID: 21510 RVA: 0x00127EF0 File Offset: 0x001260F0
		public void Write(__BinaryWriter sout)
		{
			sout.WriteByte((byte)this.binaryHeaderEnum);
			sout.WriteInt32(this.objectId);
			sout.WriteString(this.name);
			sout.WriteInt32(this.numMembers);
			for (int i = 0; i < this.numMembers; i++)
			{
				sout.WriteString(this.memberNames[i]);
			}
			if (this.assemId > 0)
			{
				sout.WriteInt32(this.assemId);
			}
		}

		// Token: 0x06005407 RID: 21511 RVA: 0x00127F64 File Offset: 0x00126164
		[SecurityCritical]
		public void Read(__BinaryParser input)
		{
			this.objectId = input.ReadInt32();
			this.name = input.ReadString();
			this.numMembers = input.ReadInt32();
			this.memberNames = new string[this.numMembers];
			for (int i = 0; i < this.numMembers; i++)
			{
				this.memberNames[i] = input.ReadString();
			}
			if (this.binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMapAssemId)
			{
				this.assemId = input.ReadInt32();
			}
		}

		// Token: 0x06005408 RID: 21512 RVA: 0x00127FDA File Offset: 0x001261DA
		public void Dump()
		{
		}

		// Token: 0x06005409 RID: 21513 RVA: 0x00127FDC File Offset: 0x001261DC
		[Conditional("_LOGGING")]
		private void DumpInternal()
		{
			if (BCLDebug.CheckEnabled("BINARY"))
			{
				for (int i = 0; i < this.numMembers; i++)
				{
				}
				BinaryHeaderEnum binaryHeaderEnum = this.binaryHeaderEnum;
			}
		}

		// Token: 0x040025E8 RID: 9704
		internal BinaryHeaderEnum binaryHeaderEnum;

		// Token: 0x040025E9 RID: 9705
		internal int objectId;

		// Token: 0x040025EA RID: 9706
		internal string name;

		// Token: 0x040025EB RID: 9707
		internal int numMembers;

		// Token: 0x040025EC RID: 9708
		internal string[] memberNames;

		// Token: 0x040025ED RID: 9709
		internal int assemId;
	}
}
