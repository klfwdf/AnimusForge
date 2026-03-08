using System;
using TaleWorlds.Core;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper
{
	// Token: 0x02000146 RID: 326
	public class FocusAddedByPlayerEvent : EventBase
	{
		// Token: 0x17000AA7 RID: 2727
		// (get) Token: 0x06001F40 RID: 8000 RVA: 0x000729C2 File Offset: 0x00070BC2
		// (set) Token: 0x06001F41 RID: 8001 RVA: 0x000729CA File Offset: 0x00070BCA
		public Hero AddedPlayer { get; private set; }

		// Token: 0x17000AA8 RID: 2728
		// (get) Token: 0x06001F42 RID: 8002 RVA: 0x000729D3 File Offset: 0x00070BD3
		// (set) Token: 0x06001F43 RID: 8003 RVA: 0x000729DB File Offset: 0x00070BDB
		public SkillObject AddedSkill { get; private set; }

		// Token: 0x06001F44 RID: 8004 RVA: 0x000729E4 File Offset: 0x00070BE4
		public FocusAddedByPlayerEvent(Hero addedPlayer, SkillObject addedSkill)
		{
			this.AddedPlayer = addedPlayer;
			this.AddedSkill = addedSkill;
		}
	}
}
