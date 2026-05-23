using System;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x02000005 RID: 5
	public class GeneratedBindDataInfo
	{
		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000020 RID: 32 RVA: 0x000021A4 File Offset: 0x000003A4
		// (set) Token: 0x06000021 RID: 33 RVA: 0x000021AC File Offset: 0x000003AC
		public string Property { get; private set; }

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000022 RID: 34 RVA: 0x000021B5 File Offset: 0x000003B5
		// (set) Token: 0x06000023 RID: 35 RVA: 0x000021BD File Offset: 0x000003BD
		public string Path { get; private set; }

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000024 RID: 36 RVA: 0x000021C6 File Offset: 0x000003C6
		// (set) Token: 0x06000025 RID: 37 RVA: 0x000021CE File Offset: 0x000003CE
		public Type WidgetPropertyType { get; internal set; }

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000026 RID: 38 RVA: 0x000021D7 File Offset: 0x000003D7
		// (set) Token: 0x06000027 RID: 39 RVA: 0x000021DF File Offset: 0x000003DF
		public Type ViewModelPropertType { get; internal set; }

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000028 RID: 40 RVA: 0x000021E8 File Offset: 0x000003E8
		// (set) Token: 0x06000029 RID: 41 RVA: 0x000021F0 File Offset: 0x000003F0
		public bool RequiresConversion { get; internal set; }

		// Token: 0x0600002A RID: 42 RVA: 0x000021F9 File Offset: 0x000003F9
		internal GeneratedBindDataInfo(string property, string path)
		{
			this.Property = property;
			this.Path = path;
		}
	}
}
