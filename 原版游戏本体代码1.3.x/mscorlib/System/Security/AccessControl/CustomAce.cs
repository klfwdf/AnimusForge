using System;
using System.Globalization;

namespace System.Security.AccessControl
{
	// Token: 0x02000202 RID: 514
	public sealed class CustomAce : GenericAce
	{
		// Token: 0x06001E59 RID: 7769 RVA: 0x0006A16C File Offset: 0x0006836C
		public CustomAce(AceType type, AceFlags flags, byte[] opaque)
			: base(type, flags)
		{
			if (type <= AceType.SystemAlarmCallbackObject)
			{
				throw new ArgumentOutOfRangeException("type", Environment.GetResourceString("ArgumentOutOfRange_InvalidUserDefinedAceType"));
			}
			this.SetOpaque(opaque);
		}

		// Token: 0x1700036B RID: 875
		// (get) Token: 0x06001E5A RID: 7770 RVA: 0x0006A197 File Offset: 0x00068397
		public int OpaqueLength
		{
			get
			{
				if (this._opaque == null)
				{
					return 0;
				}
				return this._opaque.Length;
			}
		}

		// Token: 0x1700036C RID: 876
		// (get) Token: 0x06001E5B RID: 7771 RVA: 0x0006A1AB File Offset: 0x000683AB
		public override int BinaryLength
		{
			get
			{
				return 4 + this.OpaqueLength;
			}
		}

		// Token: 0x06001E5C RID: 7772 RVA: 0x0006A1B5 File Offset: 0x000683B5
		public byte[] GetOpaque()
		{
			return this._opaque;
		}

		// Token: 0x06001E5D RID: 7773 RVA: 0x0006A1C0 File Offset: 0x000683C0
		public void SetOpaque(byte[] opaque)
		{
			if (opaque != null)
			{
				if (opaque.Length > CustomAce.MaxOpaqueLength)
				{
					throw new ArgumentOutOfRangeException("opaque", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_ArrayLength"), 0, CustomAce.MaxOpaqueLength));
				}
				if (opaque.Length % 4 != 0)
				{
					throw new ArgumentOutOfRangeException("opaque", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_ArrayLengthMultiple"), 4));
				}
			}
			this._opaque = opaque;
		}

		// Token: 0x06001E5E RID: 7774 RVA: 0x0006A23C File Offset: 0x0006843C
		public override void GetBinaryForm(byte[] binaryForm, int offset)
		{
			base.MarshalHeader(binaryForm, offset);
			offset += 4;
			if (this.OpaqueLength != 0)
			{
				if (this.OpaqueLength > CustomAce.MaxOpaqueLength)
				{
					throw new SystemException();
				}
				this.GetOpaque().CopyTo(binaryForm, offset);
			}
		}

		// Token: 0x04000AEC RID: 2796
		private byte[] _opaque;

		// Token: 0x04000AED RID: 2797
		public static readonly int MaxOpaqueLength = 65531;
	}
}
