using System;

namespace TaleWorlds.TwoDimension.Standalone
{
	// Token: 0x02000008 RID: 8
	public class InputData
	{
		// Token: 0x1700000B RID: 11
		// (get) Token: 0x0600004D RID: 77 RVA: 0x00003C9E File Offset: 0x00001E9E
		// (set) Token: 0x0600004E RID: 78 RVA: 0x00003CA6 File Offset: 0x00001EA6
		public bool[] KeyData { get; set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600004F RID: 79 RVA: 0x00003CAF File Offset: 0x00001EAF
		// (set) Token: 0x06000050 RID: 80 RVA: 0x00003CB7 File Offset: 0x00001EB7
		public bool LeftMouse { get; set; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000051 RID: 81 RVA: 0x00003CC0 File Offset: 0x00001EC0
		// (set) Token: 0x06000052 RID: 82 RVA: 0x00003CC8 File Offset: 0x00001EC8
		public bool RightMouse { get; set; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000053 RID: 83 RVA: 0x00003CD1 File Offset: 0x00001ED1
		// (set) Token: 0x06000054 RID: 84 RVA: 0x00003CD9 File Offset: 0x00001ED9
		public int CursorX { get; set; }

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000055 RID: 85 RVA: 0x00003CE2 File Offset: 0x00001EE2
		// (set) Token: 0x06000056 RID: 86 RVA: 0x00003CEA File Offset: 0x00001EEA
		public int CursorY { get; set; }

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000057 RID: 87 RVA: 0x00003CF3 File Offset: 0x00001EF3
		// (set) Token: 0x06000058 RID: 88 RVA: 0x00003CFB File Offset: 0x00001EFB
		public bool MouseMove { get; set; }

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000059 RID: 89 RVA: 0x00003D04 File Offset: 0x00001F04
		// (set) Token: 0x0600005A RID: 90 RVA: 0x00003D0C File Offset: 0x00001F0C
		public float MouseScrollDelta { get; set; }

		// Token: 0x0600005B RID: 91 RVA: 0x00003D18 File Offset: 0x00001F18
		public InputData()
		{
			this.KeyData = new bool[256];
			this.CursorX = 0;
			this.CursorY = 0;
			this.LeftMouse = false;
			this.RightMouse = false;
			this.MouseMove = false;
			this.MouseScrollDelta = 0f;
			for (int i = 0; i < 256; i++)
			{
				this.KeyData[i] = false;
			}
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00003D82 File Offset: 0x00001F82
		public void Reset()
		{
			this.MouseScrollDelta = 0f;
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00003D90 File Offset: 0x00001F90
		public void FillFrom(InputData inputData)
		{
			this.CursorX = inputData.CursorX;
			this.CursorY = inputData.CursorY;
			this.LeftMouse = inputData.LeftMouse;
			this.RightMouse = inputData.RightMouse;
			this.MouseMove = inputData.MouseMove;
			this.MouseScrollDelta = inputData.MouseScrollDelta;
			for (int i = 0; i < 256; i++)
			{
				this.KeyData[i] = inputData.KeyData[i];
			}
		}
	}
}
