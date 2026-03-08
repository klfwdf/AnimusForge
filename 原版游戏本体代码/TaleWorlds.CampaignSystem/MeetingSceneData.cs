using System;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200008B RID: 139
	public struct MeetingSceneData
	{
		// Token: 0x170004DA RID: 1242
		// (get) Token: 0x0600122B RID: 4651 RVA: 0x00052AF5 File Offset: 0x00050CF5
		// (set) Token: 0x0600122C RID: 4652 RVA: 0x00052AFD File Offset: 0x00050CFD
		public string SceneID { get; private set; }

		// Token: 0x170004DB RID: 1243
		// (get) Token: 0x0600122D RID: 4653 RVA: 0x00052B06 File Offset: 0x00050D06
		// (set) Token: 0x0600122E RID: 4654 RVA: 0x00052B0E File Offset: 0x00050D0E
		public string CultureString { get; private set; }

		// Token: 0x170004DC RID: 1244
		// (get) Token: 0x0600122F RID: 4655 RVA: 0x00052B17 File Offset: 0x00050D17
		public CultureObject Culture
		{
			get
			{
				return MBObjectManager.Instance.GetObject<CultureObject>(this.CultureString);
			}
		}

		// Token: 0x06001230 RID: 4656 RVA: 0x00052B29 File Offset: 0x00050D29
		public MeetingSceneData(string sceneID, string cultureString)
		{
			this.SceneID = sceneID;
			this.CultureString = cultureString;
		}
	}
}
