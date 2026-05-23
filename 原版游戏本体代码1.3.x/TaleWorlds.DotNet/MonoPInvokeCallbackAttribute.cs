using System;

// Token: 0x02000003 RID: 3
public class MonoPInvokeCallbackAttribute : Attribute
{
	// Token: 0x06000002 RID: 2 RVA: 0x00002050 File Offset: 0x00000250
	public MonoPInvokeCallbackAttribute(Type type)
	{
		this.Type = type;
	}

	// Token: 0x04000001 RID: 1
	public Type Type;
}
