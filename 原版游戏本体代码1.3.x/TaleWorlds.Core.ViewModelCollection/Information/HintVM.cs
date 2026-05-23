using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Information
{
	// Token: 0x02000016 RID: 22
	public class HintVM : TooltipBaseVM
	{
		// Token: 0x06000118 RID: 280 RVA: 0x00004597 File Offset: 0x00002797
		public HintVM(Type type, object[] args)
			: base(type, args)
		{
			base.InvokeRefreshData<HintVM>(this);
			base.IsActive = true;
		}

		// Token: 0x06000119 RID: 281 RVA: 0x000045BA File Offset: 0x000027BA
		protected override void OnFinalizeInternal()
		{
			base.IsActive = false;
		}

		// Token: 0x0600011A RID: 282 RVA: 0x000045C4 File Offset: 0x000027C4
		public static void RefreshGenericHintTooltip(HintVM hint, object[] args)
		{
			string text = args[0] as string;
			hint.Text = text;
		}

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x0600011B RID: 283 RVA: 0x000045E1 File Offset: 0x000027E1
		// (set) Token: 0x0600011C RID: 284 RVA: 0x000045E9 File Offset: 0x000027E9
		[DataSourceProperty]
		public string Text
		{
			get
			{
				return this._text;
			}
			set
			{
				if (this._text != value)
				{
					this._text = value;
					base.OnPropertyChangedWithValue<string>(value, "Text");
				}
			}
		}

		// Token: 0x04000077 RID: 119
		private string _text = "";
	}
}
