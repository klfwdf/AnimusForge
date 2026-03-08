using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200008D RID: 141
	public class SerialTask : ITask
	{
		// Token: 0x06000506 RID: 1286 RVA: 0x0001237C File Offset: 0x0001057C
		public SerialTask(SerialTask.DelegateDefinition function)
		{
			this._instance = function;
		}

		// Token: 0x06000507 RID: 1287 RVA: 0x0001238B File Offset: 0x0001058B
		void ITask.Invoke()
		{
			this._instance();
		}

		// Token: 0x06000508 RID: 1288 RVA: 0x00012398 File Offset: 0x00010598
		void ITask.Wait()
		{
		}

		// Token: 0x04000190 RID: 400
		private SerialTask.DelegateDefinition _instance;

		// Token: 0x020000E2 RID: 226
		// (Invoke) Token: 0x0600079B RID: 1947
		public delegate void DelegateDefinition();
	}
}
