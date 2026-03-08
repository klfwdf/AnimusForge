using System;

// Token: 0x02000179 RID: 377
public class MonoPInvokeCallbackAttribute : Attribute
{
	// Token: 0x06000D56 RID: 3414 RVA: 0x0001AD06 File Offset: 0x00018F06
	public MonoPInvokeCallbackAttribute(Type t)
	{
		this.type = t;
	}

	// Token: 0x0400031A RID: 794
	private Type type;
}
