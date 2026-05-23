using System;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200001D RID: 29
	[DataContract]
	[JsonConverter(typeof(PeerIdJsonConverter))]
	[Serializable]
	public struct PeerId
	{
		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000092 RID: 146 RVA: 0x00002E3B File Offset: 0x0000103B
		public bool IsValid
		{
			get
			{
				return this._chunk1 != 0UL || this._chunk2 != 0UL || this._chunk3 != 0UL || this._chunk4 > 0UL;
			}
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00002E64 File Offset: 0x00001064
		public PeerId(Guid guid)
		{
			byte[] value = guid.ToByteArray();
			this._chunk1 = 0UL;
			this._chunk2 = 0UL;
			this._chunk3 = BitConverter.ToUInt64(value, 0);
			this._chunk4 = BitConverter.ToUInt64(value, 8);
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00002EA3 File Offset: 0x000010A3
		public PeerId(byte[] data)
		{
			this._chunk1 = BitConverter.ToUInt64(data, 0);
			this._chunk2 = BitConverter.ToUInt64(data, 8);
			this._chunk3 = BitConverter.ToUInt64(data, 16);
			this._chunk4 = BitConverter.ToUInt64(data, 24);
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00002EDC File Offset: 0x000010DC
		public PeerId(string peerIdAsString)
		{
			int num = peerIdAsString.Length * 2;
			byte[] array = new byte[(num < 32) ? 32 : num];
			Encoding.Unicode.GetBytes(peerIdAsString, 0, peerIdAsString.Length, array, 0);
			this._chunk1 = BitConverter.ToUInt64(array, 0);
			this._chunk2 = BitConverter.ToUInt64(array, 8);
			this._chunk3 = BitConverter.ToUInt64(array, 16);
			this._chunk4 = BitConverter.ToUInt64(array, 24);
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00002F4D File Offset: 0x0000114D
		public PeerId(ulong chunk1, ulong chunk2, ulong chunk3, ulong chunk4)
		{
			this._chunk1 = chunk1;
			this._chunk2 = chunk2;
			this._chunk3 = chunk3;
			this._chunk4 = chunk4;
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00002F6C File Offset: 0x0000116C
		public byte[] ToByteArray()
		{
			byte[] array = new byte[32];
			byte[] bytes = BitConverter.GetBytes(this._chunk1);
			byte[] bytes2 = BitConverter.GetBytes(this._chunk2);
			byte[] bytes3 = BitConverter.GetBytes(this._chunk3);
			byte[] bytes4 = BitConverter.GetBytes(this._chunk4);
			for (int i = 0; i < 8; i++)
			{
				array[i] = bytes[i];
				array[8 + i] = bytes2[i];
				array[16 + i] = bytes3[i];
				array[24 + i] = bytes4[i];
			}
			return array;
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00002FEC File Offset: 0x000011EC
		public override string ToString()
		{
			return string.Concat(new object[] { this._chunk1, ".", this._chunk2, ".", this._chunk3, " .", this._chunk4 });
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00003054 File Offset: 0x00001254
		public static PeerId FromString(string peerIdAsString)
		{
			string[] array = peerIdAsString.Split(new char[] { '.' });
			return new PeerId(ulong.Parse(array[0]), ulong.Parse(array[1]), ulong.Parse(array[2]), ulong.Parse(array[3]));
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00003098 File Offset: 0x00001298
		public static bool operator ==(PeerId a, PeerId b)
		{
			return a._chunk1 == b._chunk1 && a._chunk2 == b._chunk2 && a._chunk3 == b._chunk3 && a._chunk4 == b._chunk4;
		}

		// Token: 0x0600009B RID: 155 RVA: 0x000030D4 File Offset: 0x000012D4
		public static bool operator !=(PeerId a, PeerId b)
		{
			return a._chunk1 != b._chunk1 || a._chunk2 != b._chunk2 || a._chunk3 != b._chunk3 || a._chunk4 != b._chunk4;
		}

		// Token: 0x0600009C RID: 156 RVA: 0x00003114 File Offset: 0x00001314
		public override bool Equals(object o)
		{
			if (o != null && o is PeerId)
			{
				PeerId peerId = (PeerId)o;
				return this._chunk1 == peerId._chunk1 && this._chunk2 == peerId._chunk2 && this._chunk3 == peerId._chunk3 && this._chunk4 == peerId._chunk4;
			}
			return false;
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00003170 File Offset: 0x00001370
		public override int GetHashCode()
		{
			int hashCode = this._chunk1.GetHashCode();
			int hashCode2 = this._chunk2.GetHashCode();
			int hashCode3 = this._chunk3.GetHashCode();
			int hashCode4 = this._chunk4.GetHashCode();
			return hashCode ^ hashCode2 ^ hashCode3 ^ hashCode4;
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x0600009E RID: 158 RVA: 0x000031BE File Offset: 0x000013BE
		public static PeerId Empty
		{
			get
			{
				return new PeerId(0UL, 0UL, 0UL, 0UL);
			}
		}

		// Token: 0x04000033 RID: 51
		[DataMember]
		private readonly ulong _chunk1;

		// Token: 0x04000034 RID: 52
		[DataMember]
		private readonly ulong _chunk2;

		// Token: 0x04000035 RID: 53
		[DataMember]
		private readonly ulong _chunk3;

		// Token: 0x04000036 RID: 54
		[DataMember]
		private readonly ulong _chunk4;
	}
}
