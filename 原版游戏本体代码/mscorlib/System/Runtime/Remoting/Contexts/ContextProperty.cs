using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x0200080B RID: 2059
	[SecurityCritical]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	public class ContextProperty
	{
		// Token: 0x17000EB6 RID: 3766
		// (get) Token: 0x060058BC RID: 22716 RVA: 0x00138EE5 File Offset: 0x001370E5
		public virtual string Name
		{
			get
			{
				return this._name;
			}
		}

		// Token: 0x17000EB7 RID: 3767
		// (get) Token: 0x060058BD RID: 22717 RVA: 0x00138EED File Offset: 0x001370ED
		public virtual object Property
		{
			get
			{
				return this._property;
			}
		}

		// Token: 0x060058BE RID: 22718 RVA: 0x00138EF5 File Offset: 0x001370F5
		internal ContextProperty(string name, object prop)
		{
			this._name = name;
			this._property = prop;
		}

		// Token: 0x04002873 RID: 10355
		internal string _name;

		// Token: 0x04002874 RID: 10356
		internal object _property;
	}
}
