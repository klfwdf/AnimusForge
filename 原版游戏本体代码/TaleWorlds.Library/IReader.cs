using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000044 RID: 68
	public interface IReader
	{
		// Token: 0x0600021E RID: 542
		ISerializableObject ReadSerializableObject();

		// Token: 0x0600021F RID: 543
		int ReadInt();

		// Token: 0x06000220 RID: 544
		short ReadShort();

		// Token: 0x06000221 RID: 545
		string ReadString();

		// Token: 0x06000222 RID: 546
		Color ReadColor();

		// Token: 0x06000223 RID: 547
		bool ReadBool();

		// Token: 0x06000224 RID: 548
		float ReadFloat();

		// Token: 0x06000225 RID: 549
		uint ReadUInt();

		// Token: 0x06000226 RID: 550
		ulong ReadULong();

		// Token: 0x06000227 RID: 551
		long ReadLong();

		// Token: 0x06000228 RID: 552
		byte ReadByte();

		// Token: 0x06000229 RID: 553
		byte[] ReadBytes(int length);

		// Token: 0x0600022A RID: 554
		Vec2 ReadVec2();

		// Token: 0x0600022B RID: 555
		Vec3 ReadVec3();

		// Token: 0x0600022C RID: 556
		Vec3i ReadVec3Int();

		// Token: 0x0600022D RID: 557
		sbyte ReadSByte();

		// Token: 0x0600022E RID: 558
		ushort ReadUShort();

		// Token: 0x0600022F RID: 559
		double ReadDouble();
	}
}
