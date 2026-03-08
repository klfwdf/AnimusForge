using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x02000104 RID: 260
	public class WeaponAttributeVM : ViewModel
	{
		// Token: 0x170007CE RID: 1998
		// (get) Token: 0x06001768 RID: 5992 RVA: 0x00059938 File Offset: 0x00057B38
		public DamageTypes DamageType { get; }

		// Token: 0x170007CF RID: 1999
		// (get) Token: 0x06001769 RID: 5993 RVA: 0x00059940 File Offset: 0x00057B40
		public CraftingTemplate.CraftingStatTypes AttributeType { get; }

		// Token: 0x170007D0 RID: 2000
		// (get) Token: 0x0600176A RID: 5994 RVA: 0x00059948 File Offset: 0x00057B48
		public float AttributeValue { get; }

		// Token: 0x0600176B RID: 5995 RVA: 0x00059950 File Offset: 0x00057B50
		public WeaponAttributeVM(CraftingTemplate.CraftingStatTypes type, DamageTypes damageType, string attributeName, float attributeValue)
		{
			this.AttributeType = type;
			this.DamageType = damageType;
			this.AttributeValue = attributeValue;
			string str = ((this.AttributeValue > 100f) ? attributeValue.ToString("F0") : attributeValue.ToString("F1"));
			string variable = "<span style=\"Value\">" + str + "</span>";
			TextObject textObject = new TextObject("{=!}{ATTR_NAME}{ATTR_VALUE_RTT}", null);
			textObject.SetTextVariable("ATTR_NAME", attributeName);
			textObject.SetTextVariable("ATTR_VALUE_RTT", variable);
			this.AttributeFieldText = textObject.ToString();
		}

		// Token: 0x170007D1 RID: 2001
		// (get) Token: 0x0600176C RID: 5996 RVA: 0x000599E4 File Offset: 0x00057BE4
		// (set) Token: 0x0600176D RID: 5997 RVA: 0x000599EC File Offset: 0x00057BEC
		[DataSourceProperty]
		public string AttributeFieldText
		{
			get
			{
				return this._attributeFieldText;
			}
			set
			{
				if (value != this._attributeFieldText)
				{
					this._attributeFieldText = value;
					base.OnPropertyChangedWithValue<string>(value, "AttributeFieldText");
				}
			}
		}

		// Token: 0x04000ABB RID: 2747
		private string _attributeFieldText;
	}
}
