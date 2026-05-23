using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.SaveLoad
{
	// Token: 0x02000012 RID: 18
	public class MapSaveVM : ViewModel
	{
		// Token: 0x06000193 RID: 403 RVA: 0x00008084 File Offset: 0x00006284
		public MapSaveVM(Action<bool> onActiveStateChange)
		{
			this._onActiveStateChange = onActiveStateChange;
			CampaignEvents.OnSaveStartedEvent.AddNonSerializedListener(this, new Action(this.OnSaveStarted));
			CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(this, new Action<bool, string>(this.OnSaveOver));
			this.RefreshValues();
		}

		// Token: 0x06000194 RID: 404 RVA: 0x000080D4 File Offset: 0x000062D4
		public override void RefreshValues()
		{
			base.RefreshValues();
			TextObject textObject = new TextObject("{=cp2XDjeq}Saving...", null);
			this.SavingText = textObject.ToString();
		}

		// Token: 0x06000195 RID: 405 RVA: 0x000080FF File Offset: 0x000062FF
		private void OnSaveOver(bool isSuccessful, string saveName)
		{
			this.IsActive = false;
			Action<bool> onActiveStateChange = this._onActiveStateChange;
			if (onActiveStateChange == null)
			{
				return;
			}
			onActiveStateChange(false);
		}

		// Token: 0x06000196 RID: 406 RVA: 0x00008119 File Offset: 0x00006319
		private void OnSaveStarted()
		{
			this.IsActive = true;
			Action<bool> onActiveStateChange = this._onActiveStateChange;
			if (onActiveStateChange == null)
			{
				return;
			}
			onActiveStateChange(true);
		}

		// Token: 0x06000197 RID: 407 RVA: 0x00008133 File Offset: 0x00006333
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEvents.OnSaveStartedEvent.ClearListeners(this);
			CampaignEvents.OnSaveOverEvent.ClearListeners(this);
		}

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x06000198 RID: 408 RVA: 0x00008151 File Offset: 0x00006351
		// (set) Token: 0x06000199 RID: 409 RVA: 0x00008159 File Offset: 0x00006359
		[DataSourceProperty]
		public bool IsActive
		{
			get
			{
				return this._isActive;
			}
			set
			{
				if (value != this._isActive)
				{
					this._isActive = value;
					base.OnPropertyChangedWithValue(value, "IsActive");
				}
			}
		}

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x0600019A RID: 410 RVA: 0x00008177 File Offset: 0x00006377
		// (set) Token: 0x0600019B RID: 411 RVA: 0x0000817F File Offset: 0x0000637F
		[DataSourceProperty]
		public string SavingText
		{
			get
			{
				return this._savingText;
			}
			set
			{
				if (value != this._savingText)
				{
					this._savingText = value;
					base.OnPropertyChangedWithValue<string>(value, "SavingText");
				}
			}
		}

		// Token: 0x040000B0 RID: 176
		private readonly Action<bool> _onActiveStateChange;

		// Token: 0x040000B1 RID: 177
		private string _savingText;

		// Token: 0x040000B2 RID: 178
		private bool _isActive;
	}
}
