using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace MonoMod.Core.Interop
{
	// Token: 0x020005F5 RID: 1525
	[DebuggerDisplay("{DebuggerDisplay}")]
	internal readonly struct PCSTR : IEquatable<PCSTR>
	{
		// Token: 0x0600205C RID: 8284 RVA: 0x00067DC3 File Offset: 0x00065FC3
		internal unsafe PCSTR(byte* value)
		{
			this.Value = value;
		}

		// Token: 0x0600205D RID: 8285 RVA: 0x00067DCC File Offset: 0x00065FCC
		public unsafe static implicit operator byte*(PCSTR value)
		{
			return value.Value;
		}

		// Token: 0x0600205E RID: 8286 RVA: 0x00067DD4 File Offset: 0x00065FD4
		public unsafe static explicit operator PCSTR(byte* value)
		{
			return new PCSTR(value);
		}

		// Token: 0x0600205F RID: 8287 RVA: 0x00067DDC File Offset: 0x00065FDC
		public bool Equals(PCSTR other)
		{
			return this.Value == other.Value;
		}

		// Token: 0x06002060 RID: 8288 RVA: 0x00067DEC File Offset: 0x00065FEC
		[NullableContext(2)]
		public override bool Equals(object obj)
		{
			if (obj is PCSTR)
			{
				PCSTR other = (PCSTR)obj;
				return this.Equals(other);
			}
			return false;
		}

		// Token: 0x06002061 RID: 8289 RVA: 0x00067E11 File Offset: 0x00066011
		public override int GetHashCode()
		{
			return this.Value;
		}

		// Token: 0x1700074A RID: 1866
		// (get) Token: 0x06002062 RID: 8290 RVA: 0x00067E1C File Offset: 0x0006601C
		internal unsafe int Length
		{
			get
			{
				byte* p = this.Value;
				if (p == null)
				{
					return 0;
				}
				while (*p != 0)
				{
					p++;
				}
				return checked((int)(unchecked((long)(p - this.Value))));
			}
		}

		// Token: 0x06002063 RID: 8291 RVA: 0x00067E4B File Offset: 0x0006604B
		[NullableContext(2)]
		public unsafe override string ToString()
		{
			if (this.Value != null)
			{
				return new string((sbyte*)this.Value, 0, this.Length, Encoding.UTF8);
			}
			return null;
		}

		// Token: 0x1700074B RID: 1867
		// (get) Token: 0x06002064 RID: 8292 RVA: 0x00067E70 File Offset: 0x00066070
		[Nullable(2)]
		private string DebuggerDisplay
		{
			[NullableContext(2)]
			get
			{
				return this.ToString();
			}
		}

		// Token: 0x06002065 RID: 8293 RVA: 0x00067E80 File Offset: 0x00066080
		internal unsafe ReadOnlySpan<byte> AsSpan()
		{
			if (this.Value != null)
			{
				return new ReadOnlySpan<byte>((void*)this.Value, this.Length);
			}
			return default(ReadOnlySpan<byte>);
		}

		// Token: 0x04001593 RID: 5523
		internal unsafe readonly byte* Value;
	}
}
