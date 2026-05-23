using System;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications
{
	// Token: 0x02000022 RID: 34
	public class SettlementNotificationItemBaseVM : ViewModel
	{
		// Token: 0x17000103 RID: 259
		// (get) Token: 0x06000339 RID: 825 RVA: 0x0000DDCA File Offset: 0x0000BFCA
		// (set) Token: 0x0600033A RID: 826 RVA: 0x0000DDD2 File Offset: 0x0000BFD2
		public int CreatedTick { get; set; }

		// Token: 0x0600033B RID: 827 RVA: 0x0000DDDB File Offset: 0x0000BFDB
		public SettlementNotificationItemBaseVM(Action<SettlementNotificationItemBaseVM> onRemove, int createdTick)
		{
			this._onRemove = onRemove;
			this.RelationType = 0;
			this.CreatedTick = createdTick;
		}

		// Token: 0x0600033C RID: 828 RVA: 0x0000DDF8 File Offset: 0x0000BFF8
		public void ExecuteRemove()
		{
			this._onRemove(this);
		}

		// Token: 0x17000104 RID: 260
		// (get) Token: 0x0600033D RID: 829 RVA: 0x0000DE06 File Offset: 0x0000C006
		// (set) Token: 0x0600033E RID: 830 RVA: 0x0000DE0E File Offset: 0x0000C00E
		public string CharacterName
		{
			get
			{
				return this._characterName;
			}
			set
			{
				if (value != this._characterName)
				{
					this._characterName = value;
					base.OnPropertyChangedWithValue<string>(value, "CharacterName");
				}
			}
		}

		// Token: 0x17000105 RID: 261
		// (get) Token: 0x0600033F RID: 831 RVA: 0x0000DE31 File Offset: 0x0000C031
		// (set) Token: 0x06000340 RID: 832 RVA: 0x0000DE39 File Offset: 0x0000C039
		public int RelationType
		{
			get
			{
				return this._relationType;
			}
			set
			{
				if (value != this._relationType)
				{
					this._relationType = value;
					base.OnPropertyChangedWithValue(value, "RelationType");
				}
			}
		}

		// Token: 0x17000106 RID: 262
		// (get) Token: 0x06000341 RID: 833 RVA: 0x0000DE57 File Offset: 0x0000C057
		// (set) Token: 0x06000342 RID: 834 RVA: 0x0000DE5F File Offset: 0x0000C05F
		public string Text
		{
			get
			{
				return this._text;
			}
			set
			{
				if (value != this._text)
				{
					this._text = value;
					base.OnPropertyChangedWithValue<string>(value, "Text");
				}
			}
		}

		// Token: 0x17000107 RID: 263
		// (get) Token: 0x06000343 RID: 835 RVA: 0x0000DE82 File Offset: 0x0000C082
		// (set) Token: 0x06000344 RID: 836 RVA: 0x0000DE8A File Offset: 0x0000C08A
		public CharacterImageIdentifierVM CharacterVisual
		{
			get
			{
				return this._characterVisual;
			}
			set
			{
				if (value != this._characterVisual)
				{
					this._characterVisual = value;
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "CharacterVisual");
				}
			}
		}

		// Token: 0x040001A3 RID: 419
		private readonly Action<SettlementNotificationItemBaseVM> _onRemove;

		// Token: 0x040001A5 RID: 421
		private CharacterImageIdentifierVM _characterVisual;

		// Token: 0x040001A6 RID: 422
		private string _text;

		// Token: 0x040001A7 RID: 423
		private string _characterName;

		// Token: 0x040001A8 RID: 424
		private int _relationType;
	}
}
