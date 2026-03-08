using System;
using System.Collections.Generic;
using System.Reflection;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x02000003 RID: 3
	public class GeneratedBindCommandInfo
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600000C RID: 12 RVA: 0x000020E2 File Offset: 0x000002E2
		// (set) Token: 0x0600000D RID: 13 RVA: 0x000020EA File Offset: 0x000002EA
		public string Command { get; private set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600000E RID: 14 RVA: 0x000020F3 File Offset: 0x000002F3
		// (set) Token: 0x0600000F RID: 15 RVA: 0x000020FB File Offset: 0x000002FB
		public string Path { get; private set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000010 RID: 16 RVA: 0x00002104 File Offset: 0x00000304
		// (set) Token: 0x06000011 RID: 17 RVA: 0x0000210C File Offset: 0x0000030C
		public MethodInfo Method { get; internal set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000012 RID: 18 RVA: 0x00002115 File Offset: 0x00000315
		// (set) Token: 0x06000013 RID: 19 RVA: 0x0000211D File Offset: 0x0000031D
		public int ParameterCount { get; internal set; }

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000014 RID: 20 RVA: 0x00002126 File Offset: 0x00000326
		// (set) Token: 0x06000015 RID: 21 RVA: 0x0000212E File Offset: 0x0000032E
		public List<GeneratedBindCommandParameterInfo> MethodParameters { get; private set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000016 RID: 22 RVA: 0x00002137 File Offset: 0x00000337
		// (set) Token: 0x06000017 RID: 23 RVA: 0x0000213F File Offset: 0x0000033F
		public bool GotParameter { get; internal set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000018 RID: 24 RVA: 0x00002148 File Offset: 0x00000348
		// (set) Token: 0x06000019 RID: 25 RVA: 0x00002150 File Offset: 0x00000350
		public string Parameter { get; internal set; }

		// Token: 0x0600001A RID: 26 RVA: 0x00002159 File Offset: 0x00000359
		internal GeneratedBindCommandInfo(string command, string path)
		{
			this.Command = command;
			this.Path = path;
			this.MethodParameters = new List<GeneratedBindCommandParameterInfo>();
		}
	}
}
