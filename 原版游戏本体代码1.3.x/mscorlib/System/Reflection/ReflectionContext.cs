using System;

namespace System.Reflection
{
	// Token: 0x0200061D RID: 1565
	[__DynamicallyInvokable]
	public abstract class ReflectionContext
	{
		// Token: 0x0600489E RID: 18590 RVA: 0x00107572 File Offset: 0x00105772
		[__DynamicallyInvokable]
		protected ReflectionContext()
		{
		}

		// Token: 0x0600489F RID: 18591
		[__DynamicallyInvokable]
		public abstract Assembly MapAssembly(Assembly assembly);

		// Token: 0x060048A0 RID: 18592
		[__DynamicallyInvokable]
		public abstract TypeInfo MapType(TypeInfo type);

		// Token: 0x060048A1 RID: 18593 RVA: 0x0010757A File Offset: 0x0010577A
		[__DynamicallyInvokable]
		public virtual TypeInfo GetTypeForObject(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return this.MapType(value.GetType().GetTypeInfo());
		}
	}
}
