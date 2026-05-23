using System;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000066 RID: 102
	public class SelectedStateBrushWidget : BrushWidget
	{
		// Token: 0x060006F2 RID: 1778 RVA: 0x0001E134 File Offset: 0x0001C334
		public SelectedStateBrushWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x060006F3 RID: 1779 RVA: 0x0001E150 File Offset: 0x0001C350
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			if (!this._isBrushStatesRegistered)
			{
				this.RegisterBrushStatesOfWidget();
				this._isBrushStatesRegistered = true;
			}
			if (this._isDirty)
			{
				this.SetState(this.SelectedState ?? "Default");
				this._isDirty = false;
			}
		}

		// Token: 0x170001F5 RID: 501
		// (get) Token: 0x060006F4 RID: 1780 RVA: 0x0001E19D File Offset: 0x0001C39D
		// (set) Token: 0x060006F5 RID: 1781 RVA: 0x0001E1A5 File Offset: 0x0001C3A5
		[Editor(false)]
		public string SelectedState
		{
			get
			{
				return this._selectedState;
			}
			set
			{
				if (this._selectedState != value)
				{
					this._selectedState = value;
					base.OnPropertyChanged<string>(value, "SelectedState");
					this._isDirty = true;
				}
			}
		}

		// Token: 0x04000341 RID: 833
		private bool _isDirty = true;

		// Token: 0x04000342 RID: 834
		private bool _isBrushStatesRegistered;

		// Token: 0x04000343 RID: 835
		private string _selectedState = "Default";
	}
}
