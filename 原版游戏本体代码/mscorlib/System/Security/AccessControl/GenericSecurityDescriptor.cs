using System;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x02000238 RID: 568
	public abstract class GenericSecurityDescriptor
	{
		// Token: 0x0600204D RID: 8269 RVA: 0x00071650 File Offset: 0x0006F850
		private static void MarshalInt(byte[] binaryForm, int offset, int number)
		{
			binaryForm[offset] = (byte)number;
			binaryForm[offset + 1] = (byte)(number >> 8);
			binaryForm[offset + 2] = (byte)(number >> 16);
			binaryForm[offset + 3] = (byte)(number >> 24);
		}

		// Token: 0x0600204E RID: 8270 RVA: 0x00071674 File Offset: 0x0006F874
		internal static int UnmarshalInt(byte[] binaryForm, int offset)
		{
			return (int)binaryForm[offset] + ((int)binaryForm[offset + 1] << 8) + ((int)binaryForm[offset + 2] << 16) + ((int)binaryForm[offset + 3] << 24);
		}

		// Token: 0x170003CB RID: 971
		// (get) Token: 0x06002050 RID: 8272
		internal abstract GenericAcl GenericSacl { get; }

		// Token: 0x170003CC RID: 972
		// (get) Token: 0x06002051 RID: 8273
		internal abstract GenericAcl GenericDacl { get; }

		// Token: 0x170003CD RID: 973
		// (get) Token: 0x06002052 RID: 8274 RVA: 0x0007169B File Offset: 0x0006F89B
		private bool IsCraftedAefaDacl
		{
			get
			{
				return this.GenericDacl is DiscretionaryAcl && (this.GenericDacl as DiscretionaryAcl).EveryOneFullAccessForNullDacl;
			}
		}

		// Token: 0x06002053 RID: 8275 RVA: 0x000716BC File Offset: 0x0006F8BC
		public static bool IsSddlConversionSupported()
		{
			return true;
		}

		// Token: 0x170003CE RID: 974
		// (get) Token: 0x06002054 RID: 8276 RVA: 0x000716BF File Offset: 0x0006F8BF
		public static byte Revision
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x170003CF RID: 975
		// (get) Token: 0x06002055 RID: 8277
		public abstract ControlFlags ControlFlags { get; }

		// Token: 0x170003D0 RID: 976
		// (get) Token: 0x06002056 RID: 8278
		// (set) Token: 0x06002057 RID: 8279
		public abstract SecurityIdentifier Owner { get; set; }

		// Token: 0x170003D1 RID: 977
		// (get) Token: 0x06002058 RID: 8280
		// (set) Token: 0x06002059 RID: 8281
		public abstract SecurityIdentifier Group { get; set; }

		// Token: 0x170003D2 RID: 978
		// (get) Token: 0x0600205A RID: 8282 RVA: 0x000716C4 File Offset: 0x0006F8C4
		public int BinaryLength
		{
			get
			{
				int num = 20;
				if (this.Owner != null)
				{
					num += this.Owner.BinaryLength;
				}
				if (this.Group != null)
				{
					num += this.Group.BinaryLength;
				}
				if ((this.ControlFlags & ControlFlags.SystemAclPresent) != ControlFlags.None && this.GenericSacl != null)
				{
					num += this.GenericSacl.BinaryLength;
				}
				if ((this.ControlFlags & ControlFlags.DiscretionaryAclPresent) != ControlFlags.None && this.GenericDacl != null && !this.IsCraftedAefaDacl)
				{
					num += this.GenericDacl.BinaryLength;
				}
				return num;
			}
		}

		// Token: 0x0600205B RID: 8283 RVA: 0x00071758 File Offset: 0x0006F958
		[SecuritySafeCritical]
		public string GetSddlForm(AccessControlSections includeSections)
		{
			byte[] binaryForm = new byte[this.BinaryLength];
			this.GetBinaryForm(binaryForm, 0);
			SecurityInfos securityInfos = (SecurityInfos)0;
			if ((includeSections & AccessControlSections.Owner) != AccessControlSections.None)
			{
				securityInfos |= SecurityInfos.Owner;
			}
			if ((includeSections & AccessControlSections.Group) != AccessControlSections.None)
			{
				securityInfos |= SecurityInfos.Group;
			}
			if ((includeSections & AccessControlSections.Audit) != AccessControlSections.None)
			{
				securityInfos |= SecurityInfos.SystemAcl;
			}
			if ((includeSections & AccessControlSections.Access) != AccessControlSections.None)
			{
				securityInfos |= SecurityInfos.DiscretionaryAcl;
			}
			string result;
			int num = Win32.ConvertSdToSddl(binaryForm, 1, securityInfos, out result);
			if (num == 87 || num == 1305)
			{
				throw new InvalidOperationException();
			}
			if (num != 0)
			{
				throw new InvalidOperationException();
			}
			return result;
		}

		// Token: 0x0600205C RID: 8284 RVA: 0x000717C8 File Offset: 0x0006F9C8
		public void GetBinaryForm(byte[] binaryForm, int offset)
		{
			if (binaryForm == null)
			{
				throw new ArgumentNullException("binaryForm");
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (binaryForm.Length - offset < this.BinaryLength)
			{
				throw new ArgumentOutOfRangeException("binaryForm", Environment.GetResourceString("ArgumentOutOfRange_ArrayTooSmall"));
			}
			int num = offset;
			int binaryLength = this.BinaryLength;
			byte b = ((this is RawSecurityDescriptor && (this.ControlFlags & ControlFlags.RMControlValid) != ControlFlags.None) ? (this as RawSecurityDescriptor).ResourceManagerControl : 0);
			int num2 = (int)this.ControlFlags;
			if (this.IsCraftedAefaDacl)
			{
				num2 &= -5;
			}
			binaryForm[offset] = GenericSecurityDescriptor.Revision;
			binaryForm[offset + 1] = b;
			binaryForm[offset + 2] = (byte)num2;
			binaryForm[offset + 3] = (byte)(num2 >> 8);
			int offset2 = offset + 4;
			int offset3 = offset + 8;
			int offset4 = offset + 12;
			int offset5 = offset + 16;
			offset += 20;
			if (this.Owner != null)
			{
				GenericSecurityDescriptor.MarshalInt(binaryForm, offset2, offset - num);
				this.Owner.GetBinaryForm(binaryForm, offset);
				offset += this.Owner.BinaryLength;
			}
			else
			{
				GenericSecurityDescriptor.MarshalInt(binaryForm, offset2, 0);
			}
			if (this.Group != null)
			{
				GenericSecurityDescriptor.MarshalInt(binaryForm, offset3, offset - num);
				this.Group.GetBinaryForm(binaryForm, offset);
				offset += this.Group.BinaryLength;
			}
			else
			{
				GenericSecurityDescriptor.MarshalInt(binaryForm, offset3, 0);
			}
			if ((this.ControlFlags & ControlFlags.SystemAclPresent) != ControlFlags.None && this.GenericSacl != null)
			{
				GenericSecurityDescriptor.MarshalInt(binaryForm, offset4, offset - num);
				this.GenericSacl.GetBinaryForm(binaryForm, offset);
				offset += this.GenericSacl.BinaryLength;
			}
			else
			{
				GenericSecurityDescriptor.MarshalInt(binaryForm, offset4, 0);
			}
			if ((this.ControlFlags & ControlFlags.DiscretionaryAclPresent) != ControlFlags.None && this.GenericDacl != null && !this.IsCraftedAefaDacl)
			{
				GenericSecurityDescriptor.MarshalInt(binaryForm, offset5, offset - num);
				this.GenericDacl.GetBinaryForm(binaryForm, offset);
				offset += this.GenericDacl.BinaryLength;
				return;
			}
			GenericSecurityDescriptor.MarshalInt(binaryForm, offset5, 0);
		}

		// Token: 0x04000BC2 RID: 3010
		internal const int HeaderLength = 20;

		// Token: 0x04000BC3 RID: 3011
		internal const int OwnerFoundAt = 4;

		// Token: 0x04000BC4 RID: 3012
		internal const int GroupFoundAt = 8;

		// Token: 0x04000BC5 RID: 3013
		internal const int SaclFoundAt = 12;

		// Token: 0x04000BC6 RID: 3014
		internal const int DaclFoundAt = 16;
	}
}
