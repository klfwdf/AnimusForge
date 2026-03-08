using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MCM.Abstractions;
using MCM.UI.Extensions;
using MCM.UI.Utils;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace MCM.UI.GUI.ViewModels
{
	// Token: 0x02000020 RID: 32
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class SettingsPropertyGroupVM : ViewModel
	{
		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060000F6 RID: 246 RVA: 0x00005103 File Offset: 0x00003303
		private ModOptionsVM MainView
		{
			get
			{
				return this.SettingsVM.MainView;
			}
		}

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x060000F7 RID: 247 RVA: 0x00005110 File Offset: 0x00003310
		private SettingsVM SettingsVM { get; }

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x060000F8 RID: 248 RVA: 0x00005118 File Offset: 0x00003318
		public SettingsPropertyGroupDefinition SettingPropertyGroupDefinition { get; }

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x060000F9 RID: 249 RVA: 0x00005120 File Offset: 0x00003320
		// (set) Token: 0x060000FA RID: 250 RVA: 0x00005128 File Offset: 0x00003328
		public string GroupName
		{
			get
			{
				return this._groupName;
			}
			private set
			{
				base.SetField<string>(ref this._groupName, value, "GroupName");
			}
		}

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x060000FB RID: 251 RVA: 0x0000513D File Offset: 0x0000333D
		[Nullable(2)]
		public SettingsPropertyGroupVM ParentGroup
		{
			[NullableContext(2)]
			get;
		}

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x060000FC RID: 252 RVA: 0x00005145 File Offset: 0x00003345
		// (set) Token: 0x060000FD RID: 253 RVA: 0x0000514D File Offset: 0x0000334D
		[Nullable(2)]
		public SettingsPropertyVM GroupToggleSettingProperty
		{
			[NullableContext(2)]
			get;
			[NullableContext(2)]
			private set;
		}

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x060000FE RID: 254 RVA: 0x00005156 File Offset: 0x00003356
		// (set) Token: 0x060000FF RID: 255 RVA: 0x0000515E File Offset: 0x0000335E
		public string HintText
		{
			get
			{
				return this._hintText;
			}
			private set
			{
				base.SetField<string>(ref this._hintText, value, "HintText");
			}
		}

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x06000100 RID: 256 RVA: 0x00005173 File Offset: 0x00003373
		public bool SatisfiesSearch
		{
			get
			{
				return string.IsNullOrEmpty(this.MainView.SearchText) || this.GroupName.IndexOf(this.MainView.SearchText, StringComparison.InvariantCultureIgnoreCase) >= 0 || this.AnyChildSettingSatisfiesSearch;
			}
		}

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x06000101 RID: 257 RVA: 0x000051AC File Offset: 0x000033AC
		public bool AnyChildSettingSatisfiesSearch
		{
			get
			{
				if (!this.SettingProperties.Any((SettingsPropertyVM x) => x.SatisfiesSearch))
				{
					return this.SettingPropertyGroups.Any((SettingsPropertyGroupVM x) => x.SatisfiesSearch);
				}
				return true;
			}
		}

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x06000102 RID: 258 RVA: 0x00005211 File Offset: 0x00003411
		// (set) Token: 0x06000103 RID: 259 RVA: 0x00005219 File Offset: 0x00003419
		[DataSourceProperty]
		public string GroupNameDisplay
		{
			get
			{
				return this._groupNameDisplay;
			}
			set
			{
				base.SetField<string>(ref this._groupNameDisplay, value, "GroupNameDisplay");
			}
		}

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000104 RID: 260 RVA: 0x0000522E File Offset: 0x0000342E
		[DataSourceProperty]
		public MBBindingList<SettingsPropertyVM> SettingProperties { get; } = new MBBindingList<SettingsPropertyVM>();

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000105 RID: 261 RVA: 0x00005236 File Offset: 0x00003436
		[DataSourceProperty]
		public MBBindingList<SettingsPropertyGroupVM> SettingPropertyGroups { get; } = new MBBindingList<SettingsPropertyGroupVM>();

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000106 RID: 262 RVA: 0x0000523E File Offset: 0x0000343E
		// (set) Token: 0x06000107 RID: 263 RVA: 0x00005254 File Offset: 0x00003454
		[DataSourceProperty]
		public bool GroupToggle
		{
			get
			{
				SettingsPropertyVM groupToggleSettingProperty = this.GroupToggleSettingProperty;
				return groupToggleSettingProperty == null || groupToggleSettingProperty.BoolValue;
			}
			set
			{
				if (this.GroupToggleSettingProperty != null && this.GroupToggleSettingProperty.BoolValue != value)
				{
					this.GroupToggleSettingProperty.BoolValue = value;
					base.OnPropertyChanged("GroupToggle");
					base.OnPropertyChanged("IsExpanded");
					this.OnGroupClick();
					this.OnGroupClick();
					base.OnPropertyChanged("GroupNameDisplay");
					foreach (SettingsPropertyVM propSetting in this.SettingProperties)
					{
						propSetting.OnPropertyChanged("IsEnabled");
						propSetting.OnPropertyChanged("IsSettingVisible");
					}
					foreach (SettingsPropertyGroupVM subGroup in this.SettingPropertyGroups)
					{
						subGroup.OnPropertyChanged("IsGroupVisible");
						subGroup.OnPropertyChanged("IsExpanded");
					}
				}
			}
		}

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x06000108 RID: 264 RVA: 0x00005350 File Offset: 0x00003550
		[DataSourceProperty]
		public bool IsGroupVisible
		{
			get
			{
				return (this.SatisfiesSearch || this.AnyChildSettingSatisfiesSearch) && (this.ParentGroup == null || (this.ParentGroup.IsExpanded && this.ParentGroup.GroupToggle));
			}
		}

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x06000109 RID: 265 RVA: 0x00005388 File Offset: 0x00003588
		// (set) Token: 0x0600010A RID: 266 RVA: 0x00005390 File Offset: 0x00003590
		[DataSourceProperty]
		public bool IsExpanded
		{
			get
			{
				return this._isExpanded;
			}
			set
			{
				if (base.SetField<bool>(ref this._isExpanded, value, "IsExpanded"))
				{
					base.OnPropertyChanged("IsGroupVisible");
					foreach (SettingsPropertyGroupVM subGroup in this.SettingPropertyGroups)
					{
						subGroup.OnPropertyChanged("IsGroupVisible");
						subGroup.OnPropertyChanged("IsExpanded");
					}
					foreach (SettingsPropertyVM settingProp in this.SettingProperties)
					{
						settingProp.OnPropertyChanged("IsSettingVisible");
					}
				}
			}
		}

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x0600010B RID: 267 RVA: 0x0000544C File Offset: 0x0000364C
		[DataSourceProperty]
		public bool HasGroupToggle
		{
			get
			{
				return this.GroupToggleSettingProperty != null;
			}
		}

		// Token: 0x0600010C RID: 268 RVA: 0x0000545C File Offset: 0x0000365C
		public SettingsPropertyGroupVM(SettingsPropertyGroupDefinition definition, SettingsVM settingsVM, [Nullable(2)] SettingsPropertyGroupVM parentGroup = null)
		{
			this.SettingsVM = settingsVM;
			this.SettingPropertyGroupDefinition = definition;
			this.ParentGroup = parentGroup;
			this.AddRange(this.SettingPropertyGroupDefinition.SettingProperties);
			this.SettingPropertyGroups.AddRange(from x in this.SettingPropertyGroupDefinition.SubGroups
				select new SettingsPropertyGroupVM(x, this.SettingsVM, this));
			this.RefreshValues();
		}

		// Token: 0x0600010D RID: 269 RVA: 0x00005504 File Offset: 0x00003704
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.GroupName = this.SettingPropertyGroupDefinition.GroupName;
			this.HintText = ((this.GroupToggleSettingProperty != null && !string.IsNullOrWhiteSpace(this.GroupToggleSettingProperty.HintText)) ? this.GroupToggleSettingProperty.HintText : string.Empty);
			this.GroupNameDisplay = (this.GroupToggle ? new TextObject(this.GroupName, null).ToString() : new TextObject("{=SettingsPropertyGroupVM_Disabled}{GROUPNAME} (Disabled)", new Dictionary<string, object> { 
			{
				"GROUPNAME",
				new TextObject(this.GroupName, null).ToString()
			} }).ToString());
			this.SettingProperties.Sort(UISettingsUtils.SettingsPropertyVMComparer);
			this.SettingPropertyGroups.Sort(UISettingsUtils.SettingsPropertyGroupVMComparer);
			foreach (SettingsPropertyVM setting in this.SettingProperties)
			{
				setting.RefreshValues();
			}
			foreach (SettingsPropertyGroupVM setting2 in this.SettingPropertyGroups)
			{
				setting2.RefreshValues();
			}
		}

		// Token: 0x0600010E RID: 270 RVA: 0x00005648 File Offset: 0x00003848
		private void AddRange(IEnumerable<ISettingsPropertyDefinition> definitions)
		{
			foreach (ISettingsPropertyDefinition definition in definitions)
			{
				SettingsPropertyVM sp = new SettingsPropertyVM(definition, this.SettingsVM);
				this.SettingProperties.Add(sp);
				sp.Group = this;
				if (sp.SettingPropertyDefinition.IsToggle)
				{
					bool hasGroupToggle = this.HasGroupToggle;
					this.GroupToggleSettingProperty = sp;
				}
			}
		}

		// Token: 0x0600010F RID: 271 RVA: 0x000056C4 File Offset: 0x000038C4
		public void NotifySearchChanged()
		{
			foreach (SettingsPropertyGroupVM group in this.SettingPropertyGroups)
			{
				group.NotifySearchChanged();
			}
			foreach (SettingsPropertyVM prop in this.SettingProperties)
			{
				prop.OnPropertyChanged("IsSettingVisible");
			}
			base.OnPropertyChanged("IsGroupVisible");
		}

		// Token: 0x06000110 RID: 272 RVA: 0x0000575C File Offset: 0x0000395C
		public void OnHover()
		{
			this.MainView.HintText = this.HintText;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x0000576F File Offset: 0x0000396F
		public void OnHoverEnd()
		{
			this.MainView.HintText = string.Empty;
		}

		// Token: 0x06000112 RID: 274 RVA: 0x00005781 File Offset: 0x00003981
		public void OnGroupClick()
		{
			this.IsExpanded = !this.IsExpanded;
		}

		// Token: 0x06000113 RID: 275 RVA: 0x00005792 File Offset: 0x00003992
		public override string ToString()
		{
			return this.GroupName;
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000579A File Offset: 0x0000399A
		public override int GetHashCode()
		{
			return this.GroupName.GetHashCode();
		}

		// Token: 0x06000115 RID: 277 RVA: 0x000057A8 File Offset: 0x000039A8
		public override void OnFinalize()
		{
			foreach (SettingsPropertyGroupVM settingPropertyGroup in this.SettingPropertyGroups)
			{
				settingPropertyGroup.OnFinalize();
			}
			foreach (SettingsPropertyVM settingProperty in this.SettingProperties)
			{
				settingProperty.OnFinalize();
			}
			base.OnFinalize();
		}

		// Token: 0x0400003E RID: 62
		private bool _isExpanded = true;

		// Token: 0x0400003F RID: 63
		private string _groupName = string.Empty;

		// Token: 0x04000040 RID: 64
		private string _hintText = string.Empty;

		// Token: 0x04000041 RID: 65
		private string _groupNameDisplay = string.Empty;
	}
}
