using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000023 RID: 35
	public class ManagedDelegate : DotNetObject
	{
		// Token: 0x17000023 RID: 35
		// (get) Token: 0x060000D2 RID: 210 RVA: 0x000042CC File Offset: 0x000024CC
		// (set) Token: 0x060000D3 RID: 211 RVA: 0x000042D4 File Offset: 0x000024D4
		public ManagedDelegate.DelegateDefinition Instance
		{
			get
			{
				return this._instance;
			}
			set
			{
				this._instance = value;
			}
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x000042E5 File Offset: 0x000024E5
		[LibraryCallback(null, false)]
		public void InvokeAux()
		{
			this.Instance();
		}

		// Token: 0x04000050 RID: 80
		private ManagedDelegate.DelegateDefinition _instance;

		// Token: 0x02000042 RID: 66
		// (Invoke) Token: 0x06000175 RID: 373
		public delegate void DelegateDefinition();
	}
}
