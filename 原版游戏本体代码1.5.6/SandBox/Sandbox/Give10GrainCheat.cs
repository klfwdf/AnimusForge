using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace SandBox;

public class Give10GrainCheat : GameplayCheatItem
{
	public override void ExecuteCheat()
	{
		ItemObject itemObject = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()?.FirstOrDefault((ItemObject i) => i.StringId == "grain");
		if (itemObject != null)
		{
			PartyBase.MainParty?.ItemRoster?.AddToCounts(itemObject, 10);
		}
	}

	public override TextObject GetName()
	{
		return new TextObject("{=Jdc2aaYo}Give 10 Grain");
	}
}
