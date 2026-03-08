using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005BD RID: 1469
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyFileVersionAttribute : Attribute
	{
		// Token: 0x06004464 RID: 17508 RVA: 0x000FC4E3 File Offset: 0x000FA6E3
		[__DynamicallyInvokable]
		public AssemblyFileVersionAttribute(string version)
		{
			if (version == null)
			{
				throw new ArgumentNullException("version");
			}
			this._version = version;
		}

		// Token: 0x17000A18 RID: 2584
		// (get) Token: 0x06004465 RID: 17509 RVA: 0x000FC500 File Offset: 0x000FA700
		[__DynamicallyInvokable]
		public string Version
		{
			[__DynamicallyInvokable]
			get
			{
				return this._version;
			}
		}

		// Token: 0x04001C07 RID: 7175
		private string _version;
	}
}
