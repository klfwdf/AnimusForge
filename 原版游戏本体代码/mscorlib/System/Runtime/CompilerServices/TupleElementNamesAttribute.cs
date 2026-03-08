using System;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008FE RID: 2302
	[CLSCompliant(false)]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
	public sealed class TupleElementNamesAttribute : Attribute
	{
		// Token: 0x06005E55 RID: 24149 RVA: 0x0014B5EB File Offset: 0x001497EB
		public TupleElementNamesAttribute(string[] transformNames)
		{
			if (transformNames == null)
			{
				throw new ArgumentNullException("transformNames");
			}
			this._transformNames = transformNames;
		}

		// Token: 0x17001032 RID: 4146
		// (get) Token: 0x06005E56 RID: 24150 RVA: 0x0014B608 File Offset: 0x00149808
		public IList<string> TransformNames
		{
			get
			{
				return this._transformNames;
			}
		}

		// Token: 0x04002A5A RID: 10842
		private readonly string[] _transformNames;
	}
}
