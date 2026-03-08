using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x0200031C RID: 796
	internal sealed class SourceLinkDebugInformation : CustomDebugInformation
	{
		// Token: 0x1700054B RID: 1355
		// (get) Token: 0x060014A3 RID: 5283 RVA: 0x000413DA File Offset: 0x0003F5DA
		// (set) Token: 0x060014A4 RID: 5284 RVA: 0x000413E2 File Offset: 0x0003F5E2
		public string Content
		{
			get
			{
				return this.content;
			}
			set
			{
				this.content = value;
			}
		}

		// Token: 0x1700054C RID: 1356
		// (get) Token: 0x060014A5 RID: 5285 RVA: 0x000413EB File Offset: 0x0003F5EB
		public override CustomDebugInformationKind Kind
		{
			get
			{
				return CustomDebugInformationKind.SourceLink;
			}
		}

		// Token: 0x060014A6 RID: 5286 RVA: 0x000413EE File Offset: 0x0003F5EE
		public SourceLinkDebugInformation(string content)
			: base(SourceLinkDebugInformation.KindIdentifier)
		{
			this.content = content;
		}

		// Token: 0x04000A57 RID: 2647
		internal string content;

		// Token: 0x04000A58 RID: 2648
		public static Guid KindIdentifier = new Guid("{CC110556-A091-4D38-9FEC-25AB9A351A6A}");
	}
}
