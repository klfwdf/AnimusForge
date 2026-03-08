using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200003C RID: 60
	public class InformationMessage
	{
		// Token: 0x17000023 RID: 35
		// (get) Token: 0x060001EE RID: 494 RVA: 0x0000769A File Offset: 0x0000589A
		// (set) Token: 0x060001EF RID: 495 RVA: 0x000076A2 File Offset: 0x000058A2
		public string Information { get; set; }

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060001F0 RID: 496 RVA: 0x000076AB File Offset: 0x000058AB
		// (set) Token: 0x060001F1 RID: 497 RVA: 0x000076B3 File Offset: 0x000058B3
		public string Detail { get; set; }

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x060001F2 RID: 498 RVA: 0x000076BC File Offset: 0x000058BC
		// (set) Token: 0x060001F3 RID: 499 RVA: 0x000076C4 File Offset: 0x000058C4
		public Color Color { get; set; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x060001F4 RID: 500 RVA: 0x000076CD File Offset: 0x000058CD
		// (set) Token: 0x060001F5 RID: 501 RVA: 0x000076D5 File Offset: 0x000058D5
		public string SoundEventPath { get; set; }

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x060001F6 RID: 502 RVA: 0x000076DE File Offset: 0x000058DE
		// (set) Token: 0x060001F7 RID: 503 RVA: 0x000076E6 File Offset: 0x000058E6
		public string Category { get; set; }

		// Token: 0x060001F8 RID: 504 RVA: 0x000076EF File Offset: 0x000058EF
		public InformationMessage(string information)
		{
			this.Information = information;
			this.Color = Color.White;
		}

		// Token: 0x060001F9 RID: 505 RVA: 0x00007709 File Offset: 0x00005909
		public InformationMessage(string information, Color color)
		{
			this.Information = information;
			this.Color = color;
		}

		// Token: 0x060001FA RID: 506 RVA: 0x0000771F File Offset: 0x0000591F
		public InformationMessage(string information, Color color, string category)
		{
			this.Information = information;
			this.Color = color;
			this.Category = category;
		}

		// Token: 0x060001FB RID: 507 RVA: 0x0000773C File Offset: 0x0000593C
		public InformationMessage(string information, string soundEventPath)
		{
			this.Information = information;
			this.SoundEventPath = soundEventPath;
			this.Color = Color.White;
		}

		// Token: 0x060001FC RID: 508 RVA: 0x0000775D File Offset: 0x0000595D
		public InformationMessage()
		{
			this.Information = "";
		}
	}
}
