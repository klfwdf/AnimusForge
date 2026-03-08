using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace SandBox
{
	// Token: 0x02000014 RID: 20
	public class Give10GrainCheat : GameplayCheatItem
	{
		// Token: 0x0600003C RID: 60 RVA: 0x00003BA4 File Offset: 0x00001DA4
		public override void ExecuteCheat()
		{
			MBReadOnlyList<ItemObject> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<ItemObject>();
			ItemObject itemObject;
			if (objectTypeList == null)
			{
				itemObject = null;
			}
			else
			{
				itemObject = objectTypeList.FirstOrDefault((ItemObject i) => i.StringId == "grain");
			}
			ItemObject itemObject2 = itemObject;
			if (itemObject2 != null)
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
				itemRoster.AddToCounts(itemObject2, 10);
			}
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00003C07 File Offset: 0x00001E07
		public override TextObject GetName()
		{
			return new TextObject("{=Jdc2aaYo}Give 10 Grain", null);
		}
	}
}
