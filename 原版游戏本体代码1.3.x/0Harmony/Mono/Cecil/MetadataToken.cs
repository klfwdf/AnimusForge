using System;

namespace Mono.Cecil
{
	// Token: 0x020002BA RID: 698
	internal struct MetadataToken : IEquatable<MetadataToken>
	{
		// Token: 0x170004CA RID: 1226
		// (get) Token: 0x060011CF RID: 4559 RVA: 0x0003711C File Offset: 0x0003531C
		public uint RID
		{
			get
			{
				return this.token & 16777215U;
			}
		}

		// Token: 0x170004CB RID: 1227
		// (get) Token: 0x060011D0 RID: 4560 RVA: 0x0003712A File Offset: 0x0003532A
		public TokenType TokenType
		{
			get
			{
				return (TokenType)(this.token & 4278190080U);
			}
		}

		// Token: 0x060011D1 RID: 4561 RVA: 0x00037138 File Offset: 0x00035338
		public MetadataToken(uint token)
		{
			this.token = token;
		}

		// Token: 0x060011D2 RID: 4562 RVA: 0x00037141 File Offset: 0x00035341
		public MetadataToken(TokenType type)
		{
			this = new MetadataToken(type, 0);
		}

		// Token: 0x060011D3 RID: 4563 RVA: 0x0003714B File Offset: 0x0003534B
		public MetadataToken(TokenType type, uint rid)
		{
			this.token = (uint)(type | (TokenType)rid);
		}

		// Token: 0x060011D4 RID: 4564 RVA: 0x0003714B File Offset: 0x0003534B
		public MetadataToken(TokenType type, int rid)
		{
			this.token = (uint)(type | (TokenType)rid);
		}

		// Token: 0x060011D5 RID: 4565 RVA: 0x00037156 File Offset: 0x00035356
		public int ToInt32()
		{
			return (int)this.token;
		}

		// Token: 0x060011D6 RID: 4566 RVA: 0x00037156 File Offset: 0x00035356
		public uint ToUInt32()
		{
			return this.token;
		}

		// Token: 0x060011D7 RID: 4567 RVA: 0x00037156 File Offset: 0x00035356
		public override int GetHashCode()
		{
			return (int)this.token;
		}

		// Token: 0x060011D8 RID: 4568 RVA: 0x0003715E File Offset: 0x0003535E
		public bool Equals(MetadataToken other)
		{
			return other.token == this.token;
		}

		// Token: 0x060011D9 RID: 4569 RVA: 0x0003716E File Offset: 0x0003536E
		public override bool Equals(object obj)
		{
			return obj is MetadataToken && ((MetadataToken)obj).token == this.token;
		}

		// Token: 0x060011DA RID: 4570 RVA: 0x0003718D File Offset: 0x0003538D
		public static bool operator ==(MetadataToken one, MetadataToken other)
		{
			return one.token == other.token;
		}

		// Token: 0x060011DB RID: 4571 RVA: 0x0003719D File Offset: 0x0003539D
		public static bool operator !=(MetadataToken one, MetadataToken other)
		{
			return one.token != other.token;
		}

		// Token: 0x060011DC RID: 4572 RVA: 0x000371B0 File Offset: 0x000353B0
		public override string ToString()
		{
			return string.Format("[{0}:0x{1}]", this.TokenType, this.RID.ToString("x4"));
		}

		// Token: 0x0400067D RID: 1661
		private readonly uint token;

		// Token: 0x0400067E RID: 1662
		public static readonly MetadataToken Zero = new MetadataToken(0U);
	}
}
