using System;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000030 RID: 48
	public class AudioProperty
	{
		// Token: 0x170000FE RID: 254
		// (get) Token: 0x06000354 RID: 852 RVA: 0x0000ED84 File Offset: 0x0000CF84
		// (set) Token: 0x06000355 RID: 853 RVA: 0x0000ED8C File Offset: 0x0000CF8C
		[Editor(false)]
		public string AudioName { get; set; }

		// Token: 0x170000FF RID: 255
		// (get) Token: 0x06000356 RID: 854 RVA: 0x0000ED95 File Offset: 0x0000CF95
		// (set) Token: 0x06000357 RID: 855 RVA: 0x0000ED9D File Offset: 0x0000CF9D
		[Editor(false)]
		public bool Delay { get; set; }

		// Token: 0x17000100 RID: 256
		// (get) Token: 0x06000358 RID: 856 RVA: 0x0000EDA6 File Offset: 0x0000CFA6
		// (set) Token: 0x06000359 RID: 857 RVA: 0x0000EDAE File Offset: 0x0000CFAE
		[Editor(false)]
		public float DelaySeconds { get; set; }

		// Token: 0x0600035A RID: 858 RVA: 0x0000EDB7 File Offset: 0x0000CFB7
		public void FillFrom(AudioProperty audioProperty)
		{
			this.AudioName = audioProperty.AudioName;
			this.Delay = audioProperty.Delay;
			this.DelaySeconds = audioProperty.DelaySeconds;
		}
	}
}
