using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000072 RID: 114
	public class MBWorkspace<T> where T : IMBCollection, new()
	{
		// Token: 0x06000413 RID: 1043 RVA: 0x0000E705 File Offset: 0x0000C905
		public T StartUsingWorkspace()
		{
			this._isBeingUsed = true;
			if (this._workspace == null)
			{
				this._workspace = Activator.CreateInstance<T>();
			}
			return this._workspace;
		}

		// Token: 0x06000414 RID: 1044 RVA: 0x0000E72C File Offset: 0x0000C92C
		public void StopUsingWorkspace()
		{
			this._isBeingUsed = false;
			this._workspace.Clear();
		}

		// Token: 0x06000415 RID: 1045 RVA: 0x0000E746 File Offset: 0x0000C946
		public T GetWorkspace()
		{
			return this._workspace;
		}

		// Token: 0x04000144 RID: 324
		private bool _isBeingUsed;

		// Token: 0x04000145 RID: 325
		private T _workspace;
	}
}
