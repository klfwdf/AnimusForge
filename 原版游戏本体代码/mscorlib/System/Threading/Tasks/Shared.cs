using System;

namespace System.Threading.Tasks
{
	// Token: 0x0200055C RID: 1372
	internal class Shared<T>
	{
		// Token: 0x06004071 RID: 16497 RVA: 0x000F0689 File Offset: 0x000EE889
		internal Shared(T value)
		{
			this.Value = value;
		}

		// Token: 0x04001AEE RID: 6894
		internal T Value;
	}
}
