using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Globalization
{
	// Token: 0x020003CF RID: 975
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class RegionInfo
	{
		// Token: 0x06003149 RID: 12617 RVA: 0x000BD74C File Offset: 0x000BB94C
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public RegionInfo(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (name.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NoRegionInvariantCulture"));
			}
			this.m_cultureData = CultureData.GetCultureDataForRegion(name, true);
			if (this.m_cultureData == null)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidCultureName"), name), "name");
			}
			if (this.m_cultureData.IsNeutralCulture)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNeutralRegionName", new object[] { name }), "name");
			}
			this.SetName(name);
		}

		// Token: 0x0600314A RID: 12618 RVA: 0x000BD7F0 File Offset: 0x000BB9F0
		[SecuritySafeCritical]
		public RegionInfo(int culture)
		{
			if (culture == 127)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NoRegionInvariantCulture"));
			}
			if (culture == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_CultureIsNeutral", new object[] { culture }), "culture");
			}
			if (culture == 3072)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_CustomCultureCannotBePassedByNumber", new object[] { culture }), "culture");
			}
			this.m_cultureData = CultureData.GetCultureData(culture, true);
			this.m_name = this.m_cultureData.SREGIONNAME;
			if (this.m_cultureData.IsNeutralCulture)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_CultureIsNeutral", new object[] { culture }), "culture");
			}
			this.m_cultureId = culture;
		}

		// Token: 0x0600314B RID: 12619 RVA: 0x000BD8C1 File Offset: 0x000BBAC1
		[SecuritySafeCritical]
		internal RegionInfo(CultureData cultureData)
		{
			this.m_cultureData = cultureData;
			this.m_name = this.m_cultureData.SREGIONNAME;
		}

		// Token: 0x0600314C RID: 12620 RVA: 0x000BD8E1 File Offset: 0x000BBAE1
		[SecurityCritical]
		private void SetName(string name)
		{
			this.m_name = (name.Equals(this.m_cultureData.SREGIONNAME, StringComparison.OrdinalIgnoreCase) ? this.m_cultureData.SREGIONNAME : this.m_cultureData.CultureName);
		}

		// Token: 0x0600314D RID: 12621 RVA: 0x000BD918 File Offset: 0x000BBB18
		[SecurityCritical]
		[OnDeserialized]
		private void OnDeserialized(StreamingContext ctx)
		{
			if (this.m_name == null)
			{
				this.m_cultureId = RegionInfo.IdFromEverettRegionInfoDataItem[this.m_dataItem];
			}
			if (this.m_cultureId == 0)
			{
				this.m_cultureData = CultureData.GetCultureDataForRegion(this.m_name, true);
			}
			else
			{
				this.m_cultureData = CultureData.GetCultureData(this.m_cultureId, true);
			}
			if (this.m_cultureData == null)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidCultureName"), this.m_name), "m_name");
			}
			if (this.m_cultureId == 0)
			{
				this.SetName(this.m_name);
				return;
			}
			this.m_name = this.m_cultureData.SREGIONNAME;
		}

		// Token: 0x0600314E RID: 12622 RVA: 0x000BD9C0 File Offset: 0x000BBBC0
		[OnSerializing]
		private void OnSerializing(StreamingContext ctx)
		{
		}

		// Token: 0x170006D9 RID: 1753
		// (get) Token: 0x0600314F RID: 12623 RVA: 0x000BD9C4 File Offset: 0x000BBBC4
		[__DynamicallyInvokable]
		public static RegionInfo CurrentRegion
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				RegionInfo regionInfo = RegionInfo.s_currentRegionInfo;
				if (regionInfo == null)
				{
					regionInfo = new RegionInfo(CultureInfo.CurrentCulture.m_cultureData);
					regionInfo.m_name = regionInfo.m_cultureData.SREGIONNAME;
					RegionInfo.s_currentRegionInfo = regionInfo;
				}
				return regionInfo;
			}
		}

		// Token: 0x170006DA RID: 1754
		// (get) Token: 0x06003150 RID: 12624 RVA: 0x000BDA06 File Offset: 0x000BBC06
		[__DynamicallyInvokable]
		public virtual string Name
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_name;
			}
		}

		// Token: 0x170006DB RID: 1755
		// (get) Token: 0x06003151 RID: 12625 RVA: 0x000BDA0E File Offset: 0x000BBC0E
		[__DynamicallyInvokable]
		public virtual string EnglishName
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				return this.m_cultureData.SENGCOUNTRY;
			}
		}

		// Token: 0x170006DC RID: 1756
		// (get) Token: 0x06003152 RID: 12626 RVA: 0x000BDA1B File Offset: 0x000BBC1B
		[__DynamicallyInvokable]
		public virtual string DisplayName
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				return this.m_cultureData.SLOCALIZEDCOUNTRY;
			}
		}

		// Token: 0x170006DD RID: 1757
		// (get) Token: 0x06003153 RID: 12627 RVA: 0x000BDA28 File Offset: 0x000BBC28
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public virtual string NativeName
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				return this.m_cultureData.SNATIVECOUNTRY;
			}
		}

		// Token: 0x170006DE RID: 1758
		// (get) Token: 0x06003154 RID: 12628 RVA: 0x000BDA35 File Offset: 0x000BBC35
		[__DynamicallyInvokable]
		public virtual string TwoLetterISORegionName
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				return this.m_cultureData.SISO3166CTRYNAME;
			}
		}

		// Token: 0x170006DF RID: 1759
		// (get) Token: 0x06003155 RID: 12629 RVA: 0x000BDA42 File Offset: 0x000BBC42
		public virtual string ThreeLetterISORegionName
		{
			[SecuritySafeCritical]
			get
			{
				return this.m_cultureData.SISO3166CTRYNAME2;
			}
		}

		// Token: 0x170006E0 RID: 1760
		// (get) Token: 0x06003156 RID: 12630 RVA: 0x000BDA4F File Offset: 0x000BBC4F
		public virtual string ThreeLetterWindowsRegionName
		{
			[SecuritySafeCritical]
			get
			{
				return this.m_cultureData.SABBREVCTRYNAME;
			}
		}

		// Token: 0x170006E1 RID: 1761
		// (get) Token: 0x06003157 RID: 12631 RVA: 0x000BDA5C File Offset: 0x000BBC5C
		[__DynamicallyInvokable]
		public virtual bool IsMetric
		{
			[__DynamicallyInvokable]
			get
			{
				int imeasure = this.m_cultureData.IMEASURE;
				return imeasure == 0;
			}
		}

		// Token: 0x170006E2 RID: 1762
		// (get) Token: 0x06003158 RID: 12632 RVA: 0x000BDA79 File Offset: 0x000BBC79
		[ComVisible(false)]
		public virtual int GeoId
		{
			get
			{
				return this.m_cultureData.IGEOID;
			}
		}

		// Token: 0x170006E3 RID: 1763
		// (get) Token: 0x06003159 RID: 12633 RVA: 0x000BDA86 File Offset: 0x000BBC86
		[ComVisible(false)]
		public virtual string CurrencyEnglishName
		{
			[SecuritySafeCritical]
			get
			{
				return this.m_cultureData.SENGLISHCURRENCY;
			}
		}

		// Token: 0x170006E4 RID: 1764
		// (get) Token: 0x0600315A RID: 12634 RVA: 0x000BDA93 File Offset: 0x000BBC93
		[ComVisible(false)]
		public virtual string CurrencyNativeName
		{
			[SecuritySafeCritical]
			get
			{
				return this.m_cultureData.SNATIVECURRENCY;
			}
		}

		// Token: 0x170006E5 RID: 1765
		// (get) Token: 0x0600315B RID: 12635 RVA: 0x000BDAA0 File Offset: 0x000BBCA0
		[__DynamicallyInvokable]
		public virtual string CurrencySymbol
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				return this.m_cultureData.SCURRENCY;
			}
		}

		// Token: 0x170006E6 RID: 1766
		// (get) Token: 0x0600315C RID: 12636 RVA: 0x000BDAAD File Offset: 0x000BBCAD
		[__DynamicallyInvokable]
		public virtual string ISOCurrencySymbol
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				return this.m_cultureData.SINTLSYMBOL;
			}
		}

		// Token: 0x0600315D RID: 12637 RVA: 0x000BDABC File Offset: 0x000BBCBC
		[__DynamicallyInvokable]
		public override bool Equals(object value)
		{
			RegionInfo regionInfo = value as RegionInfo;
			return regionInfo != null && this.Name.Equals(regionInfo.Name);
		}

		// Token: 0x0600315E RID: 12638 RVA: 0x000BDAE6 File Offset: 0x000BBCE6
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		// Token: 0x0600315F RID: 12639 RVA: 0x000BDAF3 File Offset: 0x000BBCF3
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return this.Name;
		}

		// Token: 0x04001514 RID: 5396
		internal string m_name;

		// Token: 0x04001515 RID: 5397
		[NonSerialized]
		internal CultureData m_cultureData;

		// Token: 0x04001516 RID: 5398
		internal static volatile RegionInfo s_currentRegionInfo;

		// Token: 0x04001517 RID: 5399
		[OptionalField(VersionAdded = 2)]
		private int m_cultureId;

		// Token: 0x04001518 RID: 5400
		[OptionalField(VersionAdded = 2)]
		internal int m_dataItem;

		// Token: 0x04001519 RID: 5401
		private static readonly int[] IdFromEverettRegionInfoDataItem = new int[]
		{
			14337, 1052, 1067, 11274, 3079, 3081, 1068, 2060, 1026, 15361,
			2110, 16394, 1046, 1059, 10249, 3084, 9225, 2055, 13322, 2052,
			9226, 5130, 1029, 1031, 1030, 7178, 5121, 12298, 1061, 3073,
			1027, 1035, 1080, 1036, 2057, 1079, 1032, 4106, 3076, 18442,
			1050, 1038, 1057, 6153, 1037, 1081, 2049, 1065, 1039, 1040,
			8201, 11265, 1041, 1089, 1088, 1042, 13313, 1087, 12289, 5127,
			1063, 4103, 1062, 4097, 6145, 6156, 1071, 1104, 5124, 1125,
			2058, 1086, 19466, 1043, 1044, 5129, 8193, 6154, 10250, 13321,
			1056, 1045, 20490, 2070, 15370, 16385, 1048, 1049, 1025, 1053,
			4100, 1060, 1051, 2074, 17418, 1114, 1054, 7169, 1055, 11273,
			1028, 1058, 1033, 14346, 1091, 8202, 1066, 9217, 1078, 12297
		};
	}
}
