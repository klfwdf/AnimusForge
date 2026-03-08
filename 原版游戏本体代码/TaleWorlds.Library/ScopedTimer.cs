using System;
using System.Diagnostics;

namespace TaleWorlds.Library
{
	// Token: 0x0200008C RID: 140
	public class ScopedTimer : IDisposable
	{
		// Token: 0x06000504 RID: 1284 RVA: 0x000122FB File Offset: 0x000104FB
		public ScopedTimer(string scopeName)
		{
			this.scopeName_ = scopeName;
			this.watch_ = new Stopwatch();
			this.watch_.Start();
		}

		// Token: 0x06000505 RID: 1285 RVA: 0x00012320 File Offset: 0x00010520
		public void Dispose()
		{
			this.watch_.Stop();
			Console.WriteLine(string.Concat(new object[]
			{
				"ScopedTimer: ",
				this.scopeName_,
				" elapsed ms: ",
				this.watch_.Elapsed.TotalMilliseconds
			}));
		}

		// Token: 0x0400018E RID: 398
		private readonly Stopwatch watch_;

		// Token: 0x0400018F RID: 399
		private readonly string scopeName_;
	}
}
