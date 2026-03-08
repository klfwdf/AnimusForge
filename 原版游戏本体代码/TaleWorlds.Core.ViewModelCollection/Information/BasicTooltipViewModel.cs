using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Information
{
	// Token: 0x02000012 RID: 18
	public class BasicTooltipViewModel : ViewModel
	{
		// Token: 0x060000E3 RID: 227 RVA: 0x00003B80 File Offset: 0x00001D80
		public BasicTooltipViewModel(Func<string> hintTextDelegate)
		{
			this._hintProperty = hintTextDelegate;
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x00003B8F File Offset: 0x00001D8F
		public BasicTooltipViewModel(Func<List<TooltipProperty>> tooltipPropertiesDelegate)
		{
			this._tooltipProperties = tooltipPropertiesDelegate;
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x00003B9E File Offset: 0x00001D9E
		public BasicTooltipViewModel(Action preBuiltTooltipCallback)
		{
			this._preBuiltTooltipCallback = preBuiltTooltipCallback;
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x00003BAD File Offset: 0x00001DAD
		public BasicTooltipViewModel()
		{
			this._hintProperty = null;
			this._tooltipProperties = null;
			this._preBuiltTooltipCallback = null;
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00003BCA File Offset: 0x00001DCA
		public void SetToolipCallback(Func<List<TooltipProperty>> tooltipPropertiesDelegate)
		{
			this._tooltipProperties = tooltipPropertiesDelegate;
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x00003BD3 File Offset: 0x00001DD3
		public void SetGenericTooltipCallback(Action preBuiltTooltipCallback)
		{
			this._preBuiltTooltipCallback = preBuiltTooltipCallback;
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x00003BDC File Offset: 0x00001DDC
		public void SetHintCallback(Func<string> hintProperty)
		{
			this._hintProperty = hintProperty;
		}

		// Token: 0x060000EA RID: 234 RVA: 0x00003BE8 File Offset: 0x00001DE8
		public void ExecuteBeginHint()
		{
			if (this._hintProperty == null && this._tooltipProperties == null && this._preBuiltTooltipCallback == null)
			{
				return;
			}
			if (this._hintProperty != null)
			{
				Func<List<TooltipProperty>> tooltipProperties = this._tooltipProperties;
			}
			if (this._tooltipProperties != null)
			{
				InformationManager.ShowTooltip(typeof(List<TooltipProperty>), new object[] { this._tooltipProperties() });
				return;
			}
			if (this._hintProperty != null)
			{
				string text = this._hintProperty();
				if (!string.IsNullOrEmpty(text))
				{
					MBInformationManager.ShowHint(text);
					return;
				}
			}
			else if (this._preBuiltTooltipCallback != null)
			{
				this._preBuiltTooltipCallback();
			}
		}

		// Token: 0x060000EB RID: 235 RVA: 0x00003C7F File Offset: 0x00001E7F
		public void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x0400005F RID: 95
		private Func<string> _hintProperty;

		// Token: 0x04000060 RID: 96
		private Func<List<TooltipProperty>> _tooltipProperties;

		// Token: 0x04000061 RID: 97
		private Action _preBuiltTooltipCallback;
	}
}
