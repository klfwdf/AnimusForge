using System;

namespace Mono
{
	// Token: 0x020001D8 RID: 472
	internal static class Disposable
	{
		// Token: 0x0600088B RID: 2187 RVA: 0x0001B844 File Offset: 0x00019A44
		public static Disposable<T> Owned<T>(T value) where T : class, IDisposable
		{
			return new Disposable<T>(value, true);
		}

		// Token: 0x0600088C RID: 2188 RVA: 0x0001B84D File Offset: 0x00019A4D
		public static Disposable<T> NotOwned<T>(T value) where T : class, IDisposable
		{
			return new Disposable<T>(value, false);
		}
	}
}
