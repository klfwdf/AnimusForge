using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.Parley
{
	// Token: 0x02000037 RID: 55
	public class MapParleyAnimationVM : ViewModel
	{
		// Token: 0x06000573 RID: 1395 RVA: 0x0001D8B2 File Offset: 0x0001BAB2
		public MapParleyAnimationVM(PartyBase parleyedParty, float animationDuration)
		{
			this._parleyedParty = parleyedParty;
			this.AnimationDuration = animationDuration;
			this.RefreshValues();
		}

		// Token: 0x06000574 RID: 1396 RVA: 0x0001D8DF File Offset: 0x0001BADF
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ParleyTextObj.SetTextVariable("PARTY_NAME", this._parleyedParty.Name);
			this.ParleyText = this.ParleyTextObj.ToString();
		}

		// Token: 0x06000575 RID: 1397 RVA: 0x0001D914 File Offset: 0x0001BB14
		public override void OnFinalize()
		{
			base.OnFinalize();
			this._parleyedParty = null;
		}

		// Token: 0x17000188 RID: 392
		// (get) Token: 0x06000576 RID: 1398 RVA: 0x0001D923 File Offset: 0x0001BB23
		// (set) Token: 0x06000577 RID: 1399 RVA: 0x0001D92B File Offset: 0x0001BB2B
		[DataSourceProperty]
		public string ParleyText
		{
			get
			{
				return this._parleyText;
			}
			set
			{
				if (this._parleyText != value)
				{
					this._parleyText = value;
					base.OnPropertyChangedWithValue<string>(value, "ParleyText");
				}
			}
		}

		// Token: 0x17000189 RID: 393
		// (get) Token: 0x06000578 RID: 1400 RVA: 0x0001D94E File Offset: 0x0001BB4E
		// (set) Token: 0x06000579 RID: 1401 RVA: 0x0001D956 File Offset: 0x0001BB56
		[DataSourceProperty]
		public float AnimationDuration
		{
			get
			{
				return this._animationDuration;
			}
			set
			{
				if (this._animationDuration != value)
				{
					this._animationDuration = value;
					base.OnPropertyChangedWithValue(value, "AnimationDuration");
				}
			}
		}

		// Token: 0x04000251 RID: 593
		private readonly TextObject ParleyTextObj = new TextObject("{=LZbHWkCB}Parleying with {PARTY_NAME}", null);

		// Token: 0x04000252 RID: 594
		private PartyBase _parleyedParty;

		// Token: 0x04000253 RID: 595
		private string _parleyText;

		// Token: 0x04000254 RID: 596
		private float _animationDuration;
	}
}
