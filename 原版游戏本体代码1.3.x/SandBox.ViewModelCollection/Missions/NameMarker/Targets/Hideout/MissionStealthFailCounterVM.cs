using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets.Hideout
{
	// Token: 0x0200003F RID: 63
	public class MissionStealthFailCounterVM : ViewModel
	{
		// Token: 0x0600041D RID: 1053 RVA: 0x00010F43 File Offset: 0x0000F143
		public MissionStealthFailCounterVM()
		{
			this._countDownTextObject = new TextObject("{=pY8lnL11}Mission will fail in: {SEC}", null);
		}

		// Token: 0x0600041E RID: 1054 RVA: 0x00010F5C File Offset: 0x0000F15C
		public void UpdateFailCounter(float failCounterElapsedTime, float failCounterMaxTime)
		{
			this.IsCounterActive = !BannerlordConfig.HideBattleUI && !MBCommon.IsPaused && failCounterElapsedTime > 0f;
			this.FailCounterMaxTime = failCounterMaxTime;
			if (this.IsCounterActive)
			{
				this.FailCounterElapsedTime = this.FailCounterMaxTime - failCounterElapsedTime;
				this._countDownTextObject.SetTextVariable("SEC", MathF.Ceiling(this.FailCounterElapsedTime));
				this.CountDownText = this._countDownTextObject.ToString();
			}
		}

		// Token: 0x1700013B RID: 315
		// (get) Token: 0x0600041F RID: 1055 RVA: 0x00010FD2 File Offset: 0x0000F1D2
		// (set) Token: 0x06000420 RID: 1056 RVA: 0x00010FDA File Offset: 0x0000F1DA
		[DataSourceProperty]
		public string CountDownText
		{
			get
			{
				return this._countDownText;
			}
			set
			{
				if (value != this._countDownText)
				{
					this._countDownText = value;
					base.OnPropertyChangedWithValue<string>(value, "CountDownText");
				}
			}
		}

		// Token: 0x1700013C RID: 316
		// (get) Token: 0x06000421 RID: 1057 RVA: 0x00010FFD File Offset: 0x0000F1FD
		// (set) Token: 0x06000422 RID: 1058 RVA: 0x00011005 File Offset: 0x0000F205
		[DataSourceProperty]
		public float FailCounterElapsedTime
		{
			get
			{
				return this._failCounterElapsedTime;
			}
			set
			{
				if (value != this._failCounterElapsedTime)
				{
					this._failCounterElapsedTime = value;
					base.OnPropertyChangedWithValue(value, "FailCounterElapsedTime");
				}
			}
		}

		// Token: 0x1700013D RID: 317
		// (get) Token: 0x06000423 RID: 1059 RVA: 0x00011023 File Offset: 0x0000F223
		// (set) Token: 0x06000424 RID: 1060 RVA: 0x0001102B File Offset: 0x0000F22B
		[DataSourceProperty]
		public float FailCounterMaxTime
		{
			get
			{
				return this._failCounterMaxTime;
			}
			set
			{
				if (value != this._failCounterMaxTime)
				{
					this._failCounterMaxTime = value;
					base.OnPropertyChangedWithValue(value, "FailCounterMaxTime");
				}
			}
		}

		// Token: 0x1700013E RID: 318
		// (get) Token: 0x06000425 RID: 1061 RVA: 0x00011049 File Offset: 0x0000F249
		// (set) Token: 0x06000426 RID: 1062 RVA: 0x00011051 File Offset: 0x0000F251
		[DataSourceProperty]
		public bool IsCounterActive
		{
			get
			{
				return this._isCounterActive;
			}
			set
			{
				if (value != this._isCounterActive)
				{
					this._isCounterActive = value;
					base.OnPropertyChangedWithValue(value, "IsCounterActive");
				}
			}
		}

		// Token: 0x0400021A RID: 538
		private TextObject _countDownTextObject;

		// Token: 0x0400021B RID: 539
		private float _failCounterElapsedTime;

		// Token: 0x0400021C RID: 540
		private string _countDownText;

		// Token: 0x0400021D RID: 541
		private float _failCounterMaxTime;

		// Token: 0x0400021E RID: 542
		private bool _isCounterActive;
	}
}
