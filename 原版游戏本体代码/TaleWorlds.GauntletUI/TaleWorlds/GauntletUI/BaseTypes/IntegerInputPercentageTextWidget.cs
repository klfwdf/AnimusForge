using System;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x0200005C RID: 92
	public class IntegerInputPercentageTextWidget : IntegerInputTextWidget
	{
		// Token: 0x0600063A RID: 1594 RVA: 0x0001AA8D File Offset: 0x00018C8D
		public IntegerInputPercentageTextWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x0600063B RID: 1595 RVA: 0x0001AA96 File Offset: 0x00018C96
		protected override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);
			if (!base.IsFocused)
			{
				this.SetPercentageText();
			}
		}

		// Token: 0x0600063C RID: 1596 RVA: 0x0001AAAD File Offset: 0x00018CAD
		protected internal override void OnGainFocus()
		{
			base.OnGainFocus();
			this.SetIntText();
		}

		// Token: 0x0600063D RID: 1597 RVA: 0x0001AABB File Offset: 0x00018CBB
		private void SetPercentageText()
		{
			base.Text = this.PercentageText;
		}

		// Token: 0x0600063E RID: 1598 RVA: 0x0001AACC File Offset: 0x00018CCC
		private void SetIntText()
		{
			base.Text = base.IntText.ToString();
		}

		// Token: 0x170001C1 RID: 449
		// (get) Token: 0x0600063F RID: 1599 RVA: 0x0001AAED File Offset: 0x00018CED
		// (set) Token: 0x06000640 RID: 1600 RVA: 0x0001AAF5 File Offset: 0x00018CF5
		[Editor(false)]
		public string PercentageText
		{
			get
			{
				return this._percentageText;
			}
			set
			{
				if (this._percentageText != value)
				{
					this._percentageText = value;
					base.OnPropertyChanged<string>(value, "PercentageText");
				}
			}
		}

		// Token: 0x040002F1 RID: 753
		private string _percentageText;
	}
}
