using System;
using System.Diagnostics;

namespace TaleWorlds.Library
{
	// Token: 0x0200007B RID: 123
	public class PerformanceTestBlock : IDisposable
	{
		// Token: 0x0600045A RID: 1114 RVA: 0x0000F668 File Offset: 0x0000D868
		public PerformanceTestBlock(string name)
		{
			this._name = name;
			Debug.Print(this._name + " block is started.", 0, Debug.DebugColor.White, 17592186044416UL);
			this._stopwatch = new Stopwatch();
			this._stopwatch.Start();
		}

		// Token: 0x0600045B RID: 1115 RVA: 0x0000F6BC File Offset: 0x0000D8BC
		void IDisposable.Dispose()
		{
			float num = (float)this._stopwatch.ElapsedMilliseconds / 1000f;
			Debug.Print(string.Concat(new object[] { this._name, " completed in ", num, " seconds." }), 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x0400015A RID: 346
		private readonly string _name;

		// Token: 0x0400015B RID: 347
		private readonly Stopwatch _stopwatch;
	}
}
