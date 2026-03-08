using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000920 RID: 2336
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, Inherited = false)]
	[Obsolete("This attribute is deprecated and will be removed in a future version.", false)]
	[ComVisible(true)]
	public sealed class IDispatchImplAttribute : Attribute
	{
		// Token: 0x0600600B RID: 24587 RVA: 0x0014B7FD File Offset: 0x001499FD
		public IDispatchImplAttribute(IDispatchImplType implType)
		{
			this._val = implType;
		}

		// Token: 0x0600600C RID: 24588 RVA: 0x0014B80C File Offset: 0x00149A0C
		public IDispatchImplAttribute(short implType)
		{
			this._val = (IDispatchImplType)implType;
		}

		// Token: 0x170010DA RID: 4314
		// (get) Token: 0x0600600D RID: 24589 RVA: 0x0014B81B File Offset: 0x00149A1B
		public IDispatchImplType Value
		{
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002A7B RID: 10875
		internal IDispatchImplType _val;
	}
}
