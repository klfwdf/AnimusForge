using System;
using System.Collections;

namespace System.Security.AccessControl
{
	// Token: 0x0200020C RID: 524
	public sealed class RawAcl : GenericAcl
	{
		// Token: 0x06001E98 RID: 7832 RVA: 0x0006AE44 File Offset: 0x00069044
		private static void VerifyHeader(byte[] binaryForm, int offset, out byte revision, out int count, out int length)
		{
			if (binaryForm == null)
			{
				throw new ArgumentNullException("binaryForm");
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (binaryForm.Length - offset >= 8)
			{
				revision = binaryForm[offset];
				length = (int)binaryForm[offset + 2] + ((int)binaryForm[offset + 3] << 8);
				count = (int)binaryForm[offset + 4] + ((int)binaryForm[offset + 5] << 8);
				if (length <= binaryForm.Length - offset)
				{
					return;
				}
			}
			throw new ArgumentOutOfRangeException("binaryForm", Environment.GetResourceString("ArgumentOutOfRange_ArrayTooSmall"));
		}

		// Token: 0x06001E99 RID: 7833 RVA: 0x0006AEC4 File Offset: 0x000690C4
		private void MarshalHeader(byte[] binaryForm, int offset)
		{
			if (binaryForm == null)
			{
				throw new ArgumentNullException("binaryForm");
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (this.BinaryLength > GenericAcl.MaxBinaryLength)
			{
				throw new InvalidOperationException(Environment.GetResourceString("AccessControl_AclTooLong"));
			}
			if (binaryForm.Length - offset < this.BinaryLength)
			{
				throw new ArgumentOutOfRangeException("binaryForm", Environment.GetResourceString("ArgumentOutOfRange_ArrayTooSmall"));
			}
			binaryForm[offset] = this.Revision;
			binaryForm[offset + 1] = 0;
			binaryForm[offset + 2] = (byte)this.BinaryLength;
			binaryForm[offset + 3] = (byte)(this.BinaryLength >> 8);
			binaryForm[offset + 4] = (byte)this.Count;
			binaryForm[offset + 5] = (byte)(this.Count >> 8);
			binaryForm[offset + 6] = 0;
			binaryForm[offset + 7] = 0;
		}

		// Token: 0x06001E9A RID: 7834 RVA: 0x0006AF88 File Offset: 0x00069188
		internal void SetBinaryForm(byte[] binaryForm, int offset)
		{
			int num;
			int num2;
			RawAcl.VerifyHeader(binaryForm, offset, out this._revision, out num, out num2);
			num2 += offset;
			offset += 8;
			this._aces = new ArrayList(num);
			int num3 = 8;
			for (int i = 0; i < num; i++)
			{
				GenericAce genericAce = GenericAce.CreateFromBinaryForm(binaryForm, offset);
				int binaryLength = genericAce.BinaryLength;
				if (num3 + binaryLength > GenericAcl.MaxBinaryLength)
				{
					throw new ArgumentException(Environment.GetResourceString("ArgumentException_InvalidAclBinaryForm"), "binaryForm");
				}
				this._aces.Add(genericAce);
				if (binaryLength % 4 != 0)
				{
					throw new SystemException();
				}
				num3 += binaryLength;
				if (this._revision == GenericAcl.AclRevisionDS)
				{
					offset += (int)binaryForm[offset + 2] + ((int)binaryForm[offset + 3] << 8);
				}
				else
				{
					offset += binaryLength;
				}
				if (offset > num2)
				{
					throw new ArgumentException(Environment.GetResourceString("ArgumentException_InvalidAclBinaryForm"), "binaryForm");
				}
			}
		}

		// Token: 0x06001E9B RID: 7835 RVA: 0x0006B05C File Offset: 0x0006925C
		public RawAcl(byte revision, int capacity)
		{
			this._revision = revision;
			this._aces = new ArrayList(capacity);
		}

		// Token: 0x06001E9C RID: 7836 RVA: 0x0006B077 File Offset: 0x00069277
		public RawAcl(byte[] binaryForm, int offset)
		{
			this.SetBinaryForm(binaryForm, offset);
		}

		// Token: 0x17000382 RID: 898
		// (get) Token: 0x06001E9D RID: 7837 RVA: 0x0006B087 File Offset: 0x00069287
		public override byte Revision
		{
			get
			{
				return this._revision;
			}
		}

		// Token: 0x17000383 RID: 899
		// (get) Token: 0x06001E9E RID: 7838 RVA: 0x0006B08F File Offset: 0x0006928F
		public override int Count
		{
			get
			{
				return this._aces.Count;
			}
		}

		// Token: 0x17000384 RID: 900
		// (get) Token: 0x06001E9F RID: 7839 RVA: 0x0006B09C File Offset: 0x0006929C
		public override int BinaryLength
		{
			get
			{
				int num = 8;
				for (int i = 0; i < this.Count; i++)
				{
					GenericAce genericAce = this._aces[i] as GenericAce;
					num += genericAce.BinaryLength;
				}
				return num;
			}
		}

		// Token: 0x06001EA0 RID: 7840 RVA: 0x0006B0D8 File Offset: 0x000692D8
		public override void GetBinaryForm(byte[] binaryForm, int offset)
		{
			this.MarshalHeader(binaryForm, offset);
			offset += 8;
			for (int i = 0; i < this.Count; i++)
			{
				GenericAce genericAce = this._aces[i] as GenericAce;
				genericAce.GetBinaryForm(binaryForm, offset);
				int binaryLength = genericAce.BinaryLength;
				if (binaryLength % 4 != 0)
				{
					throw new SystemException();
				}
				offset += binaryLength;
			}
		}

		// Token: 0x17000385 RID: 901
		public override GenericAce this[int index]
		{
			get
			{
				return this._aces[index] as GenericAce;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (value.BinaryLength % 4 != 0)
				{
					throw new SystemException();
				}
				int num = this.BinaryLength - ((index < this._aces.Count) ? (this._aces[index] as GenericAce).BinaryLength : 0) + value.BinaryLength;
				if (num > GenericAcl.MaxBinaryLength)
				{
					throw new OverflowException(Environment.GetResourceString("AccessControl_AclTooLong"));
				}
				this._aces[index] = value;
			}
		}

		// Token: 0x06001EA3 RID: 7843 RVA: 0x0006B1D4 File Offset: 0x000693D4
		public void InsertAce(int index, GenericAce ace)
		{
			if (ace == null)
			{
				throw new ArgumentNullException("ace");
			}
			if (this.BinaryLength + ace.BinaryLength > GenericAcl.MaxBinaryLength)
			{
				throw new OverflowException(Environment.GetResourceString("AccessControl_AclTooLong"));
			}
			this._aces.Insert(index, ace);
		}

		// Token: 0x06001EA4 RID: 7844 RVA: 0x0006B228 File Offset: 0x00069428
		public void RemoveAce(int index)
		{
			GenericAce genericAce = this._aces[index] as GenericAce;
			this._aces.RemoveAt(index);
		}

		// Token: 0x04000B0A RID: 2826
		private byte _revision;

		// Token: 0x04000B0B RID: 2827
		private ArrayList _aces;
	}
}
