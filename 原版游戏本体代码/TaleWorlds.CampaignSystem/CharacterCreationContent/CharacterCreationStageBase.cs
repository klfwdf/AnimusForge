using System;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent
{
	// Token: 0x02000209 RID: 521
	public abstract class CharacterCreationStageBase
	{
		// Token: 0x170007D6 RID: 2006
		// (get) Token: 0x06001FAE RID: 8110 RVA: 0x0008E4F5 File Offset: 0x0008C6F5
		// (set) Token: 0x06001FAF RID: 8111 RVA: 0x0008E4FD File Offset: 0x0008C6FD
		public ICharacterCreationStageListener Listener { get; set; }

		// Token: 0x06001FB0 RID: 8112 RVA: 0x0008E506 File Offset: 0x0008C706
		protected internal virtual void OnFinalize()
		{
			ICharacterCreationStageListener listener = this.Listener;
			if (listener == null)
			{
				return;
			}
			listener.OnStageFinalize();
		}
	}
}
