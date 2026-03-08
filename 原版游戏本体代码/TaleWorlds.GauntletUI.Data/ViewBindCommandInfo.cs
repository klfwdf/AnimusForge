using System;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.Data
{
	// Token: 0x0200000D RID: 13
	internal struct ViewBindCommandInfo
	{
		// Token: 0x17000023 RID: 35
		// (get) Token: 0x0600008F RID: 143 RVA: 0x00004420 File Offset: 0x00002620
		// (set) Token: 0x06000090 RID: 144 RVA: 0x00004428 File Offset: 0x00002628
		internal GauntletView Owner { get; private set; }

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000091 RID: 145 RVA: 0x00004431 File Offset: 0x00002631
		// (set) Token: 0x06000092 RID: 146 RVA: 0x00004439 File Offset: 0x00002639
		internal string Command { get; private set; }

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000093 RID: 147 RVA: 0x00004442 File Offset: 0x00002642
		// (set) Token: 0x06000094 RID: 148 RVA: 0x0000444A File Offset: 0x0000264A
		internal BindingPath Path { get; private set; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000095 RID: 149 RVA: 0x00004453 File Offset: 0x00002653
		// (set) Token: 0x06000096 RID: 150 RVA: 0x0000445B File Offset: 0x0000265B
		internal string Parameter { get; private set; }

		// Token: 0x06000097 RID: 151 RVA: 0x00004464 File Offset: 0x00002664
		internal ViewBindCommandInfo(GauntletView view, string command, BindingPath path, string parameter)
		{
			this.Owner = view;
			this.Command = command;
			this.Path = path;
			this.Parameter = parameter;
		}
	}
}
