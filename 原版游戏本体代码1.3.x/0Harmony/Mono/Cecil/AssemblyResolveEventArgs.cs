using System;

namespace Mono.Cecil
{
	// Token: 0x02000227 RID: 551
	internal sealed class AssemblyResolveEventArgs : EventArgs
	{
		// Token: 0x17000251 RID: 593
		// (get) Token: 0x06000BB0 RID: 2992 RVA: 0x000298EE File Offset: 0x00027AEE
		public AssemblyNameReference AssemblyReference
		{
			get
			{
				return this.reference;
			}
		}

		// Token: 0x06000BB1 RID: 2993 RVA: 0x000298F6 File Offset: 0x00027AF6
		public AssemblyResolveEventArgs(AssemblyNameReference reference)
		{
			this.reference = reference;
		}

		// Token: 0x04000389 RID: 905
		private readonly AssemblyNameReference reference;
	}
}
