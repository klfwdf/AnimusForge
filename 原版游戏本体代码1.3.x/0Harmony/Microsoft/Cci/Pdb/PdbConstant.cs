using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000433 RID: 1075
	internal class PdbConstant
	{
		// Token: 0x06001786 RID: 6022 RVA: 0x00048D78 File Offset: 0x00046F78
		internal PdbConstant(string name, uint token, object value)
		{
			this.name = name;
			this.token = token;
			this.value = value;
		}

		// Token: 0x06001787 RID: 6023 RVA: 0x00048D98 File Offset: 0x00046F98
		internal PdbConstant(BitAccess bits)
		{
			bits.ReadUInt32(out this.token);
			byte tag;
			bits.ReadUInt8(out tag);
			byte tag2;
			bits.ReadUInt8(out tag2);
			if (tag2 == 0)
			{
				this.value = tag;
			}
			else if (tag2 == 128)
			{
				switch (tag)
				{
				case 0:
				{
					sbyte sb;
					bits.ReadInt8(out sb);
					this.value = sb;
					break;
				}
				case 1:
				{
					short s;
					bits.ReadInt16(out s);
					this.value = s;
					break;
				}
				case 2:
				{
					ushort us;
					bits.ReadUInt16(out us);
					this.value = us;
					break;
				}
				case 3:
				{
					int i;
					bits.ReadInt32(out i);
					this.value = i;
					break;
				}
				case 4:
				{
					uint ui;
					bits.ReadUInt32(out ui);
					this.value = ui;
					break;
				}
				case 5:
					this.value = bits.ReadFloat();
					break;
				case 6:
					this.value = bits.ReadDouble();
					break;
				case 7:
				case 8:
				case 11:
				case 12:
				case 13:
				case 14:
				case 15:
					break;
				case 9:
				{
					long sl;
					bits.ReadInt64(out sl);
					this.value = sl;
					break;
				}
				case 10:
				{
					ulong ul;
					bits.ReadUInt64(out ul);
					this.value = ul;
					break;
				}
				case 16:
				{
					string str;
					bits.ReadBString(out str);
					this.value = str;
					break;
				}
				default:
					if (tag == 25)
					{
						this.value = bits.ReadDecimal();
					}
					break;
				}
			}
			bits.ReadCString(out this.name);
		}

		// Token: 0x04000FFF RID: 4095
		internal string name;

		// Token: 0x04001000 RID: 4096
		internal uint token;

		// Token: 0x04001001 RID: 4097
		internal object value;
	}
}
