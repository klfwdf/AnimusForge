using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000941 RID: 2369
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	[ComVisible(false)]
	public sealed class ManagedToNativeComInteropStubAttribute : Attribute
	{
		// Token: 0x06006062 RID: 24674 RVA: 0x0014C077 File Offset: 0x0014A277
		public ManagedToNativeComInteropStubAttribute(Type classType, string methodName)
		{
			this._classType = classType;
			this._methodName = methodName;
		}

		// Token: 0x170010F4 RID: 4340
		// (get) Token: 0x06006063 RID: 24675 RVA: 0x0014C08D File Offset: 0x0014A28D
		public Type ClassType
		{
			get
			{
				return this._classType;
			}
		}

		// Token: 0x170010F5 RID: 4341
		// (get) Token: 0x06006064 RID: 24676 RVA: 0x0014C095 File Offset: 0x0014A295
		public string MethodName
		{
			get
			{
				return this._methodName;
			}
		}

		// Token: 0x04002B32 RID: 11058
		internal Type _classType;

		// Token: 0x04002B33 RID: 11059
		internal string _methodName;
	}
}
