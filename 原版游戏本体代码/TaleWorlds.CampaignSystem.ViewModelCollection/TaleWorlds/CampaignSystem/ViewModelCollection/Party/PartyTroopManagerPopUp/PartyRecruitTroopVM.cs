using System;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp
{
	// Token: 0x02000032 RID: 50
	public class PartyRecruitTroopVM : PartyTroopManagerVM
	{
		// Token: 0x060004E9 RID: 1257 RVA: 0x0001BEE8 File Offset: 0x0001A0E8
		public PartyRecruitTroopVM(PartyVM partyVM)
			: base(partyVM)
		{
			this.RefreshValues();
			base.IsUpgradePopUp = false;
			this._openButtonEnabledHint = new TextObject("{=tnbCJyax}Some of your prisoners are recruitable.", null);
			this._openButtonNoTroopsHint = new TextObject("{=1xf8rHLH}You don't have any recruitable prisoners.", null);
			this._openButtonIrrelevantScreenHint = new TextObject("{=zduu7dpz}Prisoners are not recruitable in this screen.", null);
			this._openButtonUpgradesDisabledHint = new TextObject("{=HfsUngkh}Recruitment is currently disabled.", null);
		}

		// Token: 0x060004EA RID: 1258 RVA: 0x0001BF50 File Offset: 0x0001A150
		public override void RefreshValues()
		{
			base.RefreshValues();
			base.TitleText = new TextObject("{=b8CqpGHx}Recruit Prisoners", null).ToString();
			this.EffectText = new TextObject("{=opVqBNLh}Effect", null).ToString();
			this.RecruitText = new TextObject("{=recruitVerb}Recruit", null).ToString();
			this.RecruitAllText = new TextObject("{=YJaNtktT}Recruit All", null).ToString();
		}

		// Token: 0x060004EB RID: 1259 RVA: 0x0001BFBC File Offset: 0x0001A1BC
		public void OnTroopRecruited(PartyCharacterVM recruitedCharacter)
		{
			if (!base.IsOpen)
			{
				return;
			}
			this._hasMadeChanges = true;
			PartyTroopManagerItemVM item = base.Troops.FirstOrDefault((PartyTroopManagerItemVM x) => x.PartyCharacter == recruitedCharacter);
			recruitedCharacter.UpdateRecruitable();
			if (!recruitedCharacter.IsTroopRecruitable)
			{
				base.Troops.Remove(item);
			}
			base.UpdateLabels();
		}

		// Token: 0x060004EC RID: 1260 RVA: 0x0001C029 File Offset: 0x0001A229
		public override void OpenPopUp()
		{
			base.OpenPopUp();
			this.PopulateTroops();
		}

		// Token: 0x060004ED RID: 1261 RVA: 0x0001C037 File Offset: 0x0001A237
		public override void ExecuteDone()
		{
			base.ExecuteDone();
			this._partyVM.OnRecruitPopUpClosed(false);
		}

		// Token: 0x060004EE RID: 1262 RVA: 0x0001C04B File Offset: 0x0001A24B
		public override void ExecuteCancel()
		{
			base.ShowCancelInquiry(new Action(this.ConfirmCancel));
		}

		// Token: 0x060004EF RID: 1263 RVA: 0x0001C060 File Offset: 0x0001A260
		protected override void ConfirmCancel()
		{
			base.ConfirmCancel();
			this._partyVM.OnRecruitPopUpClosed(true);
		}

		// Token: 0x060004F0 RID: 1264 RVA: 0x0001C074 File Offset: 0x0001A274
		private void PopulateTroops()
		{
			base.Troops = new MBBindingList<PartyTroopManagerItemVM>();
			foreach (PartyCharacterVM partyCharacterVM in this._partyVM.MainPartyPrisoners)
			{
				if (partyCharacterVM.IsTroopRecruitable)
				{
					base.Troops.Add(new PartyTroopManagerItemVM(partyCharacterVM, new Action<PartyTroopManagerItemVM>(base.SetFocusedCharacter)));
				}
			}
		}

		// Token: 0x060004F1 RID: 1265 RVA: 0x0001C0F0 File Offset: 0x0001A2F0
		public override void ExecuteItemPrimaryAction()
		{
			PartyTroopManagerItemVM focusedTroop = base.FocusedTroop;
			if (focusedTroop == null)
			{
				return;
			}
			focusedTroop.PartyCharacter.ExecuteRecruitTroop();
		}

		// Token: 0x060004F2 RID: 1266 RVA: 0x0001C108 File Offset: 0x0001A308
		public void ExecuteRecruitAll()
		{
			for (int i = base.Troops.Count - 1; i >= 0; i--)
			{
				PartyCharacterVM partyCharacter = base.Troops[i].PartyCharacter;
				if (partyCharacter != null)
				{
					partyCharacter.RecruitAll();
				}
			}
		}

		// Token: 0x17000163 RID: 355
		// (get) Token: 0x060004F3 RID: 1267 RVA: 0x0001C149 File Offset: 0x0001A349
		// (set) Token: 0x060004F4 RID: 1268 RVA: 0x0001C151 File Offset: 0x0001A351
		[DataSourceProperty]
		public string EffectText
		{
			get
			{
				return this._effectText;
			}
			set
			{
				if (value != this._effectText)
				{
					this._effectText = value;
					base.OnPropertyChangedWithValue<string>(value, "EffectText");
				}
			}
		}

		// Token: 0x17000164 RID: 356
		// (get) Token: 0x060004F5 RID: 1269 RVA: 0x0001C174 File Offset: 0x0001A374
		// (set) Token: 0x060004F6 RID: 1270 RVA: 0x0001C17C File Offset: 0x0001A37C
		[DataSourceProperty]
		public string RecruitText
		{
			get
			{
				return this._recruitText;
			}
			set
			{
				if (value != this._recruitText)
				{
					this._recruitText = value;
					base.OnPropertyChangedWithValue<string>(value, "RecruitText");
				}
			}
		}

		// Token: 0x17000165 RID: 357
		// (get) Token: 0x060004F7 RID: 1271 RVA: 0x0001C19F File Offset: 0x0001A39F
		// (set) Token: 0x060004F8 RID: 1272 RVA: 0x0001C1A7 File Offset: 0x0001A3A7
		[DataSourceProperty]
		public string RecruitAllText
		{
			get
			{
				return this._recruitAllText;
			}
			set
			{
				if (value != this._recruitAllText)
				{
					this._recruitAllText = value;
					base.OnPropertyChangedWithValue<string>(value, "RecruitAllText");
				}
			}
		}

		// Token: 0x0400021F RID: 543
		private string _effectText;

		// Token: 0x04000220 RID: 544
		private string _recruitText;

		// Token: 0x04000221 RID: 545
		private string _recruitAllText;
	}
}
