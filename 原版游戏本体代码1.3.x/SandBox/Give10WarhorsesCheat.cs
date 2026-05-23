using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace SandBox
{
	// Token: 0x02000015 RID: 21
	public class Give10WarhorsesCheat : GameplayCheatItem
	{
		// Token: 0x0600003F RID: 63 RVA: 0x00003C1C File Offset: 0x00001E1C
		public override void ExecuteCheat()
		{
			ItemObject itemObject = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().FirstOrDefault((ItemObject i) => i.StringId == "war_horse");
			if (itemObject != null)
			{
				PartyBase mainParty = PartyBase.MainParty;
				if (mainParty == null)
				{
					return;
				}
				ItemRoster itemRoster = mainParty.ItemRoster;
				if (itemRoster == null)
				{
					return;
				}
				itemRoster.AddToCounts(itemObject, 10);
			}
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00003C78 File Offset: 0x00001E78
		public override TextObject GetName()
		{
			return new TextObject("{=4KvAwfEZ}Give 10 War Horses", null);
		}
	}
}
