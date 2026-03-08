using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;

namespace MCM.UI
{
	// Token: 0x0200000F RID: 15
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class MCMUISettings : AttributeGlobalSettings<MCMUISettings>
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000032 RID: 50 RVA: 0x00002718 File Offset: 0x00000918
		public override string Id
		{
			get
			{
				return "MCMUI_v4";
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000033 RID: 51 RVA: 0x00002720 File Offset: 0x00000920
		public override string DisplayName
		{
			get
			{
				string value = "{=MCMUISettings_Name}MCM UI {VERSION}";
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				Dictionary<string, object> dictionary2 = dictionary;
				string key = "VERSION";
				Version version = typeof(MCMUISettings).Assembly.GetName().Version;
				dictionary2.Add(key, ((version != null) ? version.ToString(3) : null) ?? "ERROR");
				return new TextObject(value, dictionary).ToString();
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000034 RID: 52 RVA: 0x0000277D File Offset: 0x0000097D
		public override string FolderName
		{
			get
			{
				return "MCM";
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000035 RID: 53 RVA: 0x00002784 File Offset: 0x00000984
		public override string FormatType
		{
			get
			{
				return "json2";
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000036 RID: 54 RVA: 0x0000278B File Offset: 0x0000098B
		// (set) Token: 0x06000037 RID: 55 RVA: 0x00002793 File Offset: 0x00000993
		[SettingPropertyBool("{=MCMUISettings_Name_HideMainMenuEntry}Hide Main Menu Entry", Order = 1, RequireRestart = false, HintText = "{=MCMUISettings_Name_HideMainMenuEntryDesc}Hides MCM's Main Menu 'Mod Options' Menu Entry.")]
		[SettingPropertyGroup("{=MCMUISettings_Name_General}General")]
		public bool UseStandardOptionScreen
		{
			get
			{
				return this._useStandardOptionScreen;
			}
			set
			{
				if (this._useStandardOptionScreen != value)
				{
					this._useStandardOptionScreen = value;
					this.OnPropertyChanged(null);
				}
			}
		}

		// Token: 0x06000038 RID: 56 RVA: 0x000027AC File Offset: 0x000009AC
		[NullableContext(2)]
		public override void OnPropertyChanged(string propertyName = null)
		{
			base.OnPropertyChanged(propertyName);
			bool flag = propertyName == "SAVE_TRIGGERED" || propertyName == "LOADING_COMPLETE";
			if (flag)
			{
				MCMUISubModule.UpdateOptionScreen(this);
			}
		}

		// Token: 0x0400000C RID: 12
		private bool _useStandardOptionScreen;
	}
}
