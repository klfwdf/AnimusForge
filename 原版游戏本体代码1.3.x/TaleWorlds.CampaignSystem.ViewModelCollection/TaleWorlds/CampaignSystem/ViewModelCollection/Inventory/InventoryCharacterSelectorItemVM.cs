using System;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory
{
	// Token: 0x02000098 RID: 152
	public class InventoryCharacterSelectorItemVM : SelectorItemVM
	{
		// Token: 0x170004B7 RID: 1207
		// (get) Token: 0x06000E90 RID: 3728 RVA: 0x0003CD99 File Offset: 0x0003AF99
		// (set) Token: 0x06000E91 RID: 3729 RVA: 0x0003CDA1 File Offset: 0x0003AFA1
		public string CharacterID { get; private set; }

		// Token: 0x170004B8 RID: 1208
		// (get) Token: 0x06000E92 RID: 3730 RVA: 0x0003CDAA File Offset: 0x0003AFAA
		// (set) Token: 0x06000E93 RID: 3731 RVA: 0x0003CDB2 File Offset: 0x0003AFB2
		public Hero Hero { get; private set; }

		// Token: 0x06000E94 RID: 3732 RVA: 0x0003CDBB File Offset: 0x0003AFBB
		public InventoryCharacterSelectorItemVM(string characterID, Hero hero, TextObject characterName)
			: base(characterName)
		{
			this.Hero = hero;
			this.CharacterID = characterID;
		}
	}
}
