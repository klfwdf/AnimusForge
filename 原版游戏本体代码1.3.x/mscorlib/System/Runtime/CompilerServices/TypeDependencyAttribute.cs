using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008C6 RID: 2246
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
	internal sealed class TypeDependencyAttribute : Attribute
	{
		// Token: 0x06005DC2 RID: 24002 RVA: 0x00149984 File Offset: 0x00147B84
		public TypeDependencyAttribute(string typeName)
		{
			if (typeName == null)
			{
				throw new ArgumentNullException("typeName");
			}
			this.typeName = typeName;
		}

		// Token: 0x04002A34 RID: 10804
		private string typeName;
	}
}
