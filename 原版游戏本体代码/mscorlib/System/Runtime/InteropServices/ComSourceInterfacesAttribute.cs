using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000921 RID: 2337
	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class ComSourceInterfacesAttribute : Attribute
	{
		// Token: 0x0600600E RID: 24590 RVA: 0x0014B823 File Offset: 0x00149A23
		[__DynamicallyInvokable]
		public ComSourceInterfacesAttribute(string sourceInterfaces)
		{
			this._val = sourceInterfaces;
		}

		// Token: 0x0600600F RID: 24591 RVA: 0x0014B832 File Offset: 0x00149A32
		[__DynamicallyInvokable]
		public ComSourceInterfacesAttribute(Type sourceInterface)
		{
			this._val = sourceInterface.FullName;
		}

		// Token: 0x06006010 RID: 24592 RVA: 0x0014B846 File Offset: 0x00149A46
		[__DynamicallyInvokable]
		public ComSourceInterfacesAttribute(Type sourceInterface1, Type sourceInterface2)
		{
			this._val = sourceInterface1.FullName + "\0" + sourceInterface2.FullName;
		}

		// Token: 0x06006011 RID: 24593 RVA: 0x0014B86C File Offset: 0x00149A6C
		[__DynamicallyInvokable]
		public ComSourceInterfacesAttribute(Type sourceInterface1, Type sourceInterface2, Type sourceInterface3)
		{
			this._val = string.Concat(new string[] { sourceInterface1.FullName, "\0", sourceInterface2.FullName, "\0", sourceInterface3.FullName });
		}

		// Token: 0x06006012 RID: 24594 RVA: 0x0014B8BC File Offset: 0x00149ABC
		[__DynamicallyInvokable]
		public ComSourceInterfacesAttribute(Type sourceInterface1, Type sourceInterface2, Type sourceInterface3, Type sourceInterface4)
		{
			this._val = string.Concat(new string[] { sourceInterface1.FullName, "\0", sourceInterface2.FullName, "\0", sourceInterface3.FullName, "\0", sourceInterface4.FullName });
		}

		// Token: 0x170010DB RID: 4315
		// (get) Token: 0x06006013 RID: 24595 RVA: 0x0014B91D File Offset: 0x00149B1D
		[__DynamicallyInvokable]
		public string Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002A7C RID: 10876
		internal string _val;
	}
}
