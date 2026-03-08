using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x0200000E RID: 14
	[NullableContext(2)]
	[Nullable(0)]
	public readonly struct SequencePosition : IEquatable<SequencePosition>
	{
		// Token: 0x0600000D RID: 13 RVA: 0x000020EA File Offset: 0x000002EA
		public SequencePosition(object @object, int integer)
		{
			this._object = @object;
			this._integer = integer;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000020FA File Offset: 0x000002FA
		[EditorBrowsable(EditorBrowsableState.Never)]
		public object GetObject()
		{
			return this._object;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002102 File Offset: 0x00000302
		[EditorBrowsable(EditorBrowsableState.Never)]
		public int GetInteger()
		{
			return this._integer;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000210A File Offset: 0x0000030A
		public bool Equals(SequencePosition other)
		{
			return this._integer == other._integer && object.Equals(this._object, other._object);
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002130 File Offset: 0x00000330
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object obj)
		{
			if (obj is SequencePosition)
			{
				SequencePosition other = (SequencePosition)obj;
				return this.Equals(other);
			}
			return false;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002155 File Offset: 0x00000355
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			return HashCode.Combine<object, int>(this._object, this._integer);
		}

		// Token: 0x04000012 RID: 18
		private readonly object _object;

		// Token: 0x04000013 RID: 19
		private readonly int _integer;
	}
}
