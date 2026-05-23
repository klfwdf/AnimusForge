using System;
using System.Diagnostics;
using System.IO;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000782 RID: 1922
	internal sealed class SerializationHeaderRecord : IStreamable
	{
		// Token: 0x060053C6 RID: 21446 RVA: 0x00126F1B File Offset: 0x0012511B
		internal SerializationHeaderRecord()
		{
		}

		// Token: 0x060053C7 RID: 21447 RVA: 0x00126F2A File Offset: 0x0012512A
		internal SerializationHeaderRecord(BinaryHeaderEnum binaryHeaderEnum, int topId, int headerId, int majorVersion, int minorVersion)
		{
			this.binaryHeaderEnum = binaryHeaderEnum;
			this.topId = topId;
			this.headerId = headerId;
			this.majorVersion = majorVersion;
			this.minorVersion = minorVersion;
		}

		// Token: 0x060053C8 RID: 21448 RVA: 0x00126F60 File Offset: 0x00125160
		public void Write(__BinaryWriter sout)
		{
			this.majorVersion = this.binaryFormatterMajorVersion;
			this.minorVersion = this.binaryFormatterMinorVersion;
			sout.WriteByte((byte)this.binaryHeaderEnum);
			sout.WriteInt32(this.topId);
			sout.WriteInt32(this.headerId);
			sout.WriteInt32(this.binaryFormatterMajorVersion);
			sout.WriteInt32(this.binaryFormatterMinorVersion);
		}

		// Token: 0x060053C9 RID: 21449 RVA: 0x00126FC2 File Offset: 0x001251C2
		private static int GetInt32(byte[] buffer, int index)
		{
			return (int)buffer[index] | ((int)buffer[index + 1] << 8) | ((int)buffer[index + 2] << 16) | ((int)buffer[index + 3] << 24);
		}

		// Token: 0x060053CA RID: 21450 RVA: 0x00126FE4 File Offset: 0x001251E4
		[SecurityCritical]
		public void Read(__BinaryParser input)
		{
			byte[] array = input.ReadBytes(17);
			if (array.Length < 17)
			{
				__Error.EndOfFile();
			}
			this.majorVersion = SerializationHeaderRecord.GetInt32(array, 9);
			if (this.majorVersion > this.binaryFormatterMajorVersion)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_InvalidFormat", new object[] { BitConverter.ToString(array) }));
			}
			this.binaryHeaderEnum = (BinaryHeaderEnum)array[0];
			this.topId = SerializationHeaderRecord.GetInt32(array, 1);
			this.headerId = SerializationHeaderRecord.GetInt32(array, 5);
			this.minorVersion = SerializationHeaderRecord.GetInt32(array, 13);
		}

		// Token: 0x060053CB RID: 21451 RVA: 0x00127072 File Offset: 0x00125272
		public void Dump()
		{
		}

		// Token: 0x060053CC RID: 21452 RVA: 0x00127074 File Offset: 0x00125274
		[Conditional("_LOGGING")]
		private void DumpInternal()
		{
			BCLDebug.CheckEnabled("BINARY");
		}

		// Token: 0x040025BB RID: 9659
		internal int binaryFormatterMajorVersion = 1;

		// Token: 0x040025BC RID: 9660
		internal int binaryFormatterMinorVersion;

		// Token: 0x040025BD RID: 9661
		internal BinaryHeaderEnum binaryHeaderEnum;

		// Token: 0x040025BE RID: 9662
		internal int topId;

		// Token: 0x040025BF RID: 9663
		internal int headerId;

		// Token: 0x040025C0 RID: 9664
		internal int majorVersion;

		// Token: 0x040025C1 RID: 9665
		internal int minorVersion;
	}
}
