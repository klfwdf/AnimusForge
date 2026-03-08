using System;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x02000204 RID: 516
	public sealed class CompoundAce : KnownAce
	{
		// Token: 0x06001E60 RID: 7776 RVA: 0x0006A27F File Offset: 0x0006847F
		public CompoundAce(AceFlags flags, int accessMask, CompoundAceType compoundAceType, SecurityIdentifier sid)
			: base(AceType.AccessAllowedCompound, flags, accessMask, sid)
		{
			this._compoundAceType = compoundAceType;
		}

		// Token: 0x06001E61 RID: 7777 RVA: 0x0006A294 File Offset: 0x00068494
		internal static bool ParseBinaryForm(byte[] binaryForm, int offset, out int accessMask, out CompoundAceType compoundAceType, out SecurityIdentifier sid)
		{
			GenericAce.VerifyHeader(binaryForm, offset);
			if (binaryForm.Length - offset >= 12 + SecurityIdentifier.MinBinaryLength)
			{
				int num = offset + 4;
				int num2 = 0;
				accessMask = (int)binaryForm[num] + ((int)binaryForm[num + 1] << 8) + ((int)binaryForm[num + 2] << 16) + ((int)binaryForm[num + 3] << 24);
				num2 += 4;
				compoundAceType = (CompoundAceType)((int)binaryForm[num + num2] + ((int)binaryForm[num + num2 + 1] << 8));
				num2 += 4;
				sid = new SecurityIdentifier(binaryForm, num + num2);
				return true;
			}
			accessMask = 0;
			compoundAceType = (CompoundAceType)0;
			sid = null;
			return false;
		}

		// Token: 0x1700036D RID: 877
		// (get) Token: 0x06001E62 RID: 7778 RVA: 0x0006A30E File Offset: 0x0006850E
		// (set) Token: 0x06001E63 RID: 7779 RVA: 0x0006A316 File Offset: 0x00068516
		public CompoundAceType CompoundAceType
		{
			get
			{
				return this._compoundAceType;
			}
			set
			{
				this._compoundAceType = value;
			}
		}

		// Token: 0x1700036E RID: 878
		// (get) Token: 0x06001E64 RID: 7780 RVA: 0x0006A31F File Offset: 0x0006851F
		public override int BinaryLength
		{
			get
			{
				return 12 + base.SecurityIdentifier.BinaryLength;
			}
		}

		// Token: 0x06001E65 RID: 7781 RVA: 0x0006A330 File Offset: 0x00068530
		public override void GetBinaryForm(byte[] binaryForm, int offset)
		{
			base.MarshalHeader(binaryForm, offset);
			int num = offset + 4;
			int num2 = 0;
			binaryForm[num] = (byte)base.AccessMask;
			binaryForm[num + 1] = (byte)(base.AccessMask >> 8);
			binaryForm[num + 2] = (byte)(base.AccessMask >> 16);
			binaryForm[num + 3] = (byte)(base.AccessMask >> 24);
			num2 += 4;
			binaryForm[num + num2] = (byte)((ushort)this.CompoundAceType);
			binaryForm[num + num2 + 1] = (byte)((ushort)this.CompoundAceType >> 8);
			binaryForm[num + num2 + 2] = 0;
			binaryForm[num + num2 + 3] = 0;
			num2 += 4;
			base.SecurityIdentifier.GetBinaryForm(binaryForm, num + num2);
		}

		// Token: 0x04000AF0 RID: 2800
		private CompoundAceType _compoundAceType;

		// Token: 0x04000AF1 RID: 2801
		private const int AceTypeLength = 4;
	}
}
