using System;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items
{
	// Token: 0x020000E6 RID: 230
	public class EncyclopediaFamilyMemberVM : HeroVM
	{
		// Token: 0x06001574 RID: 5492 RVA: 0x00054401 File Offset: 0x00052601
		public EncyclopediaFamilyMemberVM(Hero hero, Hero baseHero)
			: base(hero, false)
		{
			this._baseHero = baseHero;
			this.RefreshValues();
		}

		// Token: 0x06001575 RID: 5493 RVA: 0x00054418 File Offset: 0x00052618
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (this._baseHero != null)
			{
				this.Role = ConversationHelper.GetHeroRelationToHeroTextShort(base.Hero, this._baseHero, true);
			}
		}

		// Token: 0x17000719 RID: 1817
		// (get) Token: 0x06001576 RID: 5494 RVA: 0x00054440 File Offset: 0x00052640
		// (set) Token: 0x06001577 RID: 5495 RVA: 0x00054448 File Offset: 0x00052648
		[DataSourceProperty]
		public string Role
		{
			get
			{
				return this._role;
			}
			set
			{
				if (value != this._role)
				{
					this._role = value;
					base.OnPropertyChangedWithValue<string>(value, "Role");
				}
			}
		}

		// Token: 0x040009C5 RID: 2501
		private readonly Hero _baseHero;

		// Token: 0x040009C6 RID: 2502
		private string _role;
	}
}
