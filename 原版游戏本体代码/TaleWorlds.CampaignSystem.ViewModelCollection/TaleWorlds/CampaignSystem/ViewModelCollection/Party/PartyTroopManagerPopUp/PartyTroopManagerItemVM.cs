using System;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp
{
	// Token: 0x02000033 RID: 51
	public class PartyTroopManagerItemVM : ViewModel
	{
		// Token: 0x17000166 RID: 358
		// (get) Token: 0x060004F9 RID: 1273 RVA: 0x0001C1CA File Offset: 0x0001A3CA
		// (set) Token: 0x060004FA RID: 1274 RVA: 0x0001C1D2 File Offset: 0x0001A3D2
		public Action<PartyTroopManagerItemVM> SetFocused { get; private set; }

		// Token: 0x060004FB RID: 1275 RVA: 0x0001C1DB File Offset: 0x0001A3DB
		public PartyTroopManagerItemVM(PartyCharacterVM baseTroop, Action<PartyTroopManagerItemVM> setFocused)
		{
			this.PartyCharacter = baseTroop;
			this.SetFocused = setFocused;
		}

		// Token: 0x060004FC RID: 1276 RVA: 0x0001C1F1 File Offset: 0x0001A3F1
		public void ExecuteSetFocused()
		{
			if (this.PartyCharacter.Character != null)
			{
				Action<PartyTroopManagerItemVM> setFocused = this.SetFocused;
				if (setFocused != null)
				{
					setFocused(this);
				}
				this.IsFocused = true;
			}
		}

		// Token: 0x060004FD RID: 1277 RVA: 0x0001C219 File Offset: 0x0001A419
		public void ExecuteSetUnfocused()
		{
			Action<PartyTroopManagerItemVM> setFocused = this.SetFocused;
			if (setFocused != null)
			{
				setFocused(null);
			}
			this.IsFocused = false;
		}

		// Token: 0x060004FE RID: 1278 RVA: 0x0001C234 File Offset: 0x0001A434
		public void ExecuteOpenTroopEncyclopedia()
		{
			this.PartyCharacter.ExecuteOpenTroopEncyclopedia();
		}

		// Token: 0x17000167 RID: 359
		// (get) Token: 0x060004FF RID: 1279 RVA: 0x0001C241 File Offset: 0x0001A441
		// (set) Token: 0x06000500 RID: 1280 RVA: 0x0001C249 File Offset: 0x0001A449
		[DataSourceProperty]
		public bool IsFocused
		{
			get
			{
				return this._isFocused;
			}
			set
			{
				if (value != this._isFocused)
				{
					this._isFocused = value;
					base.OnPropertyChangedWithValue(value, "IsFocused");
				}
			}
		}

		// Token: 0x17000168 RID: 360
		// (get) Token: 0x06000501 RID: 1281 RVA: 0x0001C267 File Offset: 0x0001A467
		// (set) Token: 0x06000502 RID: 1282 RVA: 0x0001C26F File Offset: 0x0001A46F
		[DataSourceProperty]
		public PartyCharacterVM PartyCharacter
		{
			get
			{
				return this._partyCharacter;
			}
			set
			{
				if (value != this._partyCharacter)
				{
					this._partyCharacter = value;
					base.OnPropertyChangedWithValue<PartyCharacterVM>(value, "PartyCharacter");
				}
			}
		}

		// Token: 0x17000169 RID: 361
		// (get) Token: 0x06000503 RID: 1283 RVA: 0x0001C28D File Offset: 0x0001A48D
		// (set) Token: 0x06000504 RID: 1284 RVA: 0x0001C29A File Offset: 0x0001A49A
		[DataSourceProperty]
		public bool IsTroopUpgradable
		{
			get
			{
				return this.PartyCharacter.IsTroopUpgradable;
			}
			set
			{
				if (value != this.PartyCharacter.IsTroopUpgradable)
				{
					this.PartyCharacter.IsTroopUpgradable = value;
					base.OnPropertyChangedWithValue(value, "IsTroopUpgradable");
				}
			}
		}

		// Token: 0x1700016A RID: 362
		// (get) Token: 0x06000505 RID: 1285 RVA: 0x0001C2C2 File Offset: 0x0001A4C2
		// (set) Token: 0x06000506 RID: 1286 RVA: 0x0001C2CF File Offset: 0x0001A4CF
		[DataSourceProperty]
		public bool IsTroopRecruitable
		{
			get
			{
				return this.PartyCharacter.IsTroopRecruitable;
			}
			set
			{
				if (value != this.PartyCharacter.IsTroopRecruitable)
				{
					this.PartyCharacter.IsTroopRecruitable = value;
					base.OnPropertyChangedWithValue(value, "IsTroopRecruitable");
				}
			}
		}

		// Token: 0x04000223 RID: 547
		private bool _isFocused;

		// Token: 0x04000224 RID: 548
		private PartyCharacterVM _partyCharacter;
	}
}
