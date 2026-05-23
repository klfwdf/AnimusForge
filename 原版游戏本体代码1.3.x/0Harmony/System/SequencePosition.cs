using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x02000469 RID: 1129
	[NullableContext(2)]
	[Nullable(0)]
	internal readonly struct SequencePosition : IEquatable<SequencePosition>
	{
		// Token: 0x06001854 RID: 6228 RVA: 0x0004D080 File Offset: 0x0004B280
		public SequencePosition(object @object, int integer)
		{
			this._object = @object;
			this._integer = integer;
		}

		// Token: 0x06001855 RID: 6229 RVA: 0x0004D090 File Offset: 0x0004B290
		[EditorBrowsable(EditorBrowsableState.Never)]
		public object GetObject()
		{
			return this._object;
		}

		// Token: 0x06001856 RID: 6230 RVA: 0x0004D098 File Offset: 0x0004B298
		[EditorBrowsable(EditorBrowsableState.Never)]
		public int GetInteger()
		{
			return this._integer;
		}

		// Token: 0x06001857 RID: 6231 RVA: 0x0004D0A0 File Offset: 0x0004B2A0
		public bool Equals(SequencePosition other)
		{
			return this._integer == other._integer && object.Equals(this._object, other._object);
		}

		// Token: 0x06001858 RID: 6232 RVA: 0x0004D0C4 File Offset: 0x0004B2C4
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

		// Token: 0x06001859 RID: 6233 RVA: 0x0004D0E9 File Offset: 0x0004B2E9
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			return HashCode.Combine<object, int>(this._object, this._integer);
		}

		// Token: 0x04001082 RID: 4226
		private readonly object _object;

		// Token: 0x04001083 RID: 4227
		private readonly int _integer;
	}
}
