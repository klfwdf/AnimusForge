using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000919 RID: 2329
	[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
	[ComVisible(true)]
	public sealed class TypeLibImportClassAttribute : Attribute
	{
		// Token: 0x06006001 RID: 24577 RVA: 0x0014B78C File Offset: 0x0014998C
		public TypeLibImportClassAttribute(Type importClass)
		{
			this._importClassName = importClass.ToString();
		}

		// Token: 0x170010D6 RID: 4310
		// (get) Token: 0x06006002 RID: 24578 RVA: 0x0014B7A0 File Offset: 0x001499A0
		public string Value
		{
			get
			{
				return this._importClassName;
			}
		}

		// Token: 0x04002A73 RID: 10867
		internal string _importClassName;
	}
}
