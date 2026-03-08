using System;
using System.Collections;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Globalization
{
	// Token: 0x020003A9 RID: 937
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class CultureInfo : ICloneable, IFormatProvider
	{
		// Token: 0x06002E4B RID: 11851 RVA: 0x000B10E4 File Offset: 0x000AF2E4
		private static bool Init()
		{
			if (CultureInfo.s_InvariantCultureInfo == null)
			{
				CultureInfo.s_InvariantCultureInfo = new CultureInfo("", false)
				{
					m_isReadOnly = true
				};
			}
			CultureInfo.s_userDefaultCulture = (CultureInfo.s_userDefaultUICulture = CultureInfo.s_InvariantCultureInfo);
			CultureInfo.s_userDefaultCulture = CultureInfo.InitUserDefaultCulture();
			CultureInfo.s_userDefaultUICulture = CultureInfo.InitUserDefaultUICulture();
			return true;
		}

		// Token: 0x06002E4C RID: 11852 RVA: 0x000B1144 File Offset: 0x000AF344
		[SecuritySafeCritical]
		private static CultureInfo InitUserDefaultCulture()
		{
			string defaultLocaleName = CultureInfo.GetDefaultLocaleName(1024);
			if (defaultLocaleName == null)
			{
				defaultLocaleName = CultureInfo.GetDefaultLocaleName(2048);
				if (defaultLocaleName == null)
				{
					return CultureInfo.InvariantCulture;
				}
			}
			CultureInfo cultureByName = CultureInfo.GetCultureByName(defaultLocaleName, true);
			cultureByName.m_isReadOnly = true;
			return cultureByName;
		}

		// Token: 0x06002E4D RID: 11853 RVA: 0x000B1184 File Offset: 0x000AF384
		private static CultureInfo InitUserDefaultUICulture()
		{
			string userDefaultUILanguage = CultureInfo.GetUserDefaultUILanguage();
			if (userDefaultUILanguage == CultureInfo.UserDefaultCulture.Name)
			{
				return CultureInfo.UserDefaultCulture;
			}
			CultureInfo cultureByName = CultureInfo.GetCultureByName(userDefaultUILanguage, true);
			if (cultureByName == null)
			{
				return CultureInfo.InvariantCulture;
			}
			cultureByName.m_isReadOnly = true;
			return cultureByName;
		}

		// Token: 0x06002E4E RID: 11854 RVA: 0x000B11C8 File Offset: 0x000AF3C8
		[SecuritySafeCritical]
		internal static CultureInfo GetCultureInfoForUserPreferredLanguageInAppX()
		{
			if (CultureInfo.ts_IsDoingAppXCultureInfoLookup)
			{
				return null;
			}
			if (AppDomain.IsAppXNGen)
			{
				return null;
			}
			CultureInfo result = null;
			try
			{
				CultureInfo.ts_IsDoingAppXCultureInfoLookup = true;
				if (CultureInfo.s_WindowsRuntimeResourceManager == null)
				{
					CultureInfo.s_WindowsRuntimeResourceManager = ResourceManager.GetWinRTResourceManager();
				}
				result = CultureInfo.s_WindowsRuntimeResourceManager.GlobalResourceContextBestFitCultureInfo;
			}
			finally
			{
				CultureInfo.ts_IsDoingAppXCultureInfoLookup = false;
			}
			return result;
		}

		// Token: 0x06002E4F RID: 11855 RVA: 0x000B122C File Offset: 0x000AF42C
		[SecuritySafeCritical]
		internal static bool SetCultureInfoForUserPreferredLanguageInAppX(CultureInfo ci)
		{
			if (AppDomain.IsAppXNGen)
			{
				return false;
			}
			if (CultureInfo.s_WindowsRuntimeResourceManager == null)
			{
				CultureInfo.s_WindowsRuntimeResourceManager = ResourceManager.GetWinRTResourceManager();
			}
			return CultureInfo.s_WindowsRuntimeResourceManager.SetGlobalResourceContextDefaultCulture(ci);
		}

		// Token: 0x06002E50 RID: 11856 RVA: 0x000B1259 File Offset: 0x000AF459
		[__DynamicallyInvokable]
		public CultureInfo(string name)
			: this(name, true)
		{
		}

		// Token: 0x06002E51 RID: 11857 RVA: 0x000B1264 File Offset: 0x000AF464
		public CultureInfo(string name, bool useUserOverride)
		{
			this.cultureID = 127;
			base..ctor();
			if (name == null)
			{
				throw new ArgumentNullException("name", Environment.GetResourceString("ArgumentNull_String"));
			}
			this.m_cultureData = CultureData.GetCultureData(name, useUserOverride);
			if (this.m_cultureData == null)
			{
				throw new CultureNotFoundException("name", name, Environment.GetResourceString("Argument_CultureNotSupported"));
			}
			this.m_name = this.m_cultureData.CultureName;
			this.m_isInherited = base.GetType() != typeof(CultureInfo);
		}

		// Token: 0x06002E52 RID: 11858 RVA: 0x000B12EE File Offset: 0x000AF4EE
		private CultureInfo(CultureData cultureData)
		{
			this.cultureID = 127;
			base..ctor();
			this.m_cultureData = cultureData;
			this.m_name = cultureData.CultureName;
			this.m_isInherited = false;
		}

		// Token: 0x06002E53 RID: 11859 RVA: 0x000B1318 File Offset: 0x000AF518
		private static CultureInfo CreateCultureInfoNoThrow(string name, bool useUserOverride)
		{
			CultureData cultureData = CultureData.GetCultureData(name, useUserOverride);
			if (cultureData == null)
			{
				return null;
			}
			return new CultureInfo(cultureData);
		}

		// Token: 0x06002E54 RID: 11860 RVA: 0x000B1338 File Offset: 0x000AF538
		public CultureInfo(int culture)
			: this(culture, true)
		{
		}

		// Token: 0x06002E55 RID: 11861 RVA: 0x000B1342 File Offset: 0x000AF542
		public CultureInfo(int culture, bool useUserOverride)
		{
			this.cultureID = 127;
			base..ctor();
			if (culture < 0)
			{
				throw new ArgumentOutOfRangeException("culture", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			this.InitializeFromCultureId(culture, useUserOverride);
		}

		// Token: 0x06002E56 RID: 11862 RVA: 0x000B1374 File Offset: 0x000AF574
		private void InitializeFromCultureId(int culture, bool useUserOverride)
		{
			if (culture <= 1024)
			{
				if (culture != 0 && culture != 1024)
				{
					goto IL_43;
				}
			}
			else if (culture != 2048 && culture != 3072 && culture != 4096)
			{
				goto IL_43;
			}
			throw new CultureNotFoundException("culture", culture, Environment.GetResourceString("Argument_CultureNotSupported"));
			IL_43:
			this.m_cultureData = CultureData.GetCultureData(culture, useUserOverride);
			this.m_isInherited = base.GetType() != typeof(CultureInfo);
			this.m_name = this.m_cultureData.CultureName;
		}

		// Token: 0x06002E57 RID: 11863 RVA: 0x000B1400 File Offset: 0x000AF600
		internal static void CheckDomainSafetyObject(object obj, object container)
		{
			if (obj.GetType().Assembly != typeof(CultureInfo).Assembly)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("InvalidOperation_SubclassedObject"), obj.GetType(), container.GetType()));
			}
		}

		// Token: 0x06002E58 RID: 11864 RVA: 0x000B1454 File Offset: 0x000AF654
		[OnDeserialized]
		private void OnDeserialized(StreamingContext ctx)
		{
			if (this.m_name == null || CultureInfo.IsAlternateSortLcid(this.cultureID))
			{
				this.InitializeFromCultureId(this.cultureID, this.m_useUserOverride);
			}
			else
			{
				this.m_cultureData = CultureData.GetCultureData(this.m_name, this.m_useUserOverride);
				if (this.m_cultureData == null)
				{
					throw new CultureNotFoundException("m_name", this.m_name, Environment.GetResourceString("Argument_CultureNotSupported"));
				}
			}
			this.m_isInherited = base.GetType() != typeof(CultureInfo);
			if (base.GetType().Assembly == typeof(CultureInfo).Assembly)
			{
				if (this.textInfo != null)
				{
					CultureInfo.CheckDomainSafetyObject(this.textInfo, this);
				}
				if (this.compareInfo != null)
				{
					CultureInfo.CheckDomainSafetyObject(this.compareInfo, this);
				}
			}
		}

		// Token: 0x06002E59 RID: 11865 RVA: 0x000B1528 File Offset: 0x000AF728
		private static bool IsAlternateSortLcid(int lcid)
		{
			return lcid == 1034 || (lcid & 983040) != 0;
		}

		// Token: 0x06002E5A RID: 11866 RVA: 0x000B153E File Offset: 0x000AF73E
		[OnSerializing]
		private void OnSerializing(StreamingContext ctx)
		{
			this.m_name = this.m_cultureData.CultureName;
			this.m_useUserOverride = this.m_cultureData.UseUserOverride;
			this.cultureID = this.m_cultureData.ILANGUAGE;
		}

		// Token: 0x17000600 RID: 1536
		// (get) Token: 0x06002E5B RID: 11867 RVA: 0x000B1573 File Offset: 0x000AF773
		internal bool IsSafeCrossDomain
		{
			get
			{
				return this.m_isSafeCrossDomain;
			}
		}

		// Token: 0x17000601 RID: 1537
		// (get) Token: 0x06002E5C RID: 11868 RVA: 0x000B157B File Offset: 0x000AF77B
		internal int CreatedDomainID
		{
			get
			{
				return this.m_createdDomainID;
			}
		}

		// Token: 0x06002E5D RID: 11869 RVA: 0x000B1583 File Offset: 0x000AF783
		internal void StartCrossDomainTracking()
		{
			if (this.m_createdDomainID != 0)
			{
				return;
			}
			if (this.CanSendCrossDomain())
			{
				this.m_isSafeCrossDomain = true;
			}
			Thread.MemoryBarrier();
			this.m_createdDomainID = Thread.GetDomainID();
		}

		// Token: 0x06002E5E RID: 11870 RVA: 0x000B15B0 File Offset: 0x000AF7B0
		internal bool CanSendCrossDomain()
		{
			bool result = false;
			if (base.GetType() == typeof(CultureInfo))
			{
				result = true;
			}
			return result;
		}

		// Token: 0x06002E5F RID: 11871 RVA: 0x000B15DC File Offset: 0x000AF7DC
		internal CultureInfo(string cultureName, string textAndCompareCultureName)
		{
			this.cultureID = 127;
			base..ctor();
			if (cultureName == null)
			{
				throw new ArgumentNullException("cultureName", Environment.GetResourceString("ArgumentNull_String"));
			}
			this.m_cultureData = CultureData.GetCultureData(cultureName, false);
			if (this.m_cultureData == null)
			{
				throw new CultureNotFoundException("cultureName", cultureName, Environment.GetResourceString("Argument_CultureNotSupported"));
			}
			this.m_name = this.m_cultureData.CultureName;
			CultureInfo cultureInfo = CultureInfo.GetCultureInfo(textAndCompareCultureName);
			this.compareInfo = cultureInfo.CompareInfo;
			this.textInfo = cultureInfo.TextInfo;
		}

		// Token: 0x06002E60 RID: 11872 RVA: 0x000B166C File Offset: 0x000AF86C
		private static CultureInfo GetCultureByName(string name, bool userOverride)
		{
			try
			{
				return userOverride ? new CultureInfo(name) : CultureInfo.GetCultureInfo(name);
			}
			catch (ArgumentException)
			{
			}
			return null;
		}

		// Token: 0x06002E61 RID: 11873 RVA: 0x000B16A4 File Offset: 0x000AF8A4
		public static CultureInfo CreateSpecificCulture(string name)
		{
			CultureInfo cultureInfo;
			try
			{
				cultureInfo = new CultureInfo(name);
			}
			catch (ArgumentException)
			{
				cultureInfo = null;
				for (int i = 0; i < name.Length; i++)
				{
					if ('-' == name[i])
					{
						try
						{
							cultureInfo = new CultureInfo(name.Substring(0, i));
							break;
						}
						catch (ArgumentException)
						{
							throw;
						}
					}
				}
				if (cultureInfo == null)
				{
					throw;
				}
			}
			if (!cultureInfo.IsNeutralCulture)
			{
				return cultureInfo;
			}
			return new CultureInfo(cultureInfo.m_cultureData.SSPECIFICCULTURE);
		}

		// Token: 0x06002E62 RID: 11874 RVA: 0x000B172C File Offset: 0x000AF92C
		internal static bool VerifyCultureName(string cultureName, bool throwException)
		{
			int i = 0;
			while (i < cultureName.Length)
			{
				char c = cultureName[i];
				if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
				{
					if (throwException)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_InvalidResourceCultureName", new object[] { cultureName }));
					}
					return false;
				}
				else
				{
					i++;
				}
			}
			return true;
		}

		// Token: 0x06002E63 RID: 11875 RVA: 0x000B1784 File Offset: 0x000AF984
		internal static bool VerifyCultureName(CultureInfo culture, bool throwException)
		{
			return !culture.m_isInherited || CultureInfo.VerifyCultureName(culture.Name, throwException);
		}

		// Token: 0x17000602 RID: 1538
		// (get) Token: 0x06002E64 RID: 11876 RVA: 0x000B179C File Offset: 0x000AF99C
		// (set) Token: 0x06002E65 RID: 11877 RVA: 0x000B17A8 File Offset: 0x000AF9A8
		[__DynamicallyInvokable]
		public static CultureInfo CurrentCulture
		{
			[__DynamicallyInvokable]
			get
			{
				return Thread.CurrentThread.CurrentCulture;
			}
			[__DynamicallyInvokable]
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (AppDomain.IsAppXModel() && CultureInfo.SetCultureInfoForUserPreferredLanguageInAppX(value))
				{
					return;
				}
				Thread.CurrentThread.CurrentCulture = value;
			}
		}

		// Token: 0x17000603 RID: 1539
		// (get) Token: 0x06002E66 RID: 11878 RVA: 0x000B17D4 File Offset: 0x000AF9D4
		internal static CultureInfo UserDefaultCulture
		{
			get
			{
				CultureInfo cultureInfo = CultureInfo.s_userDefaultCulture;
				if (cultureInfo == null)
				{
					CultureInfo.s_userDefaultCulture = CultureInfo.InvariantCulture;
					cultureInfo = CultureInfo.InitUserDefaultCulture();
					CultureInfo.s_userDefaultCulture = cultureInfo;
				}
				return cultureInfo;
			}
		}

		// Token: 0x17000604 RID: 1540
		// (get) Token: 0x06002E67 RID: 11879 RVA: 0x000B1808 File Offset: 0x000AFA08
		internal static CultureInfo UserDefaultUICulture
		{
			get
			{
				CultureInfo cultureInfo = CultureInfo.s_userDefaultUICulture;
				if (cultureInfo == null)
				{
					CultureInfo.s_userDefaultUICulture = CultureInfo.InvariantCulture;
					cultureInfo = CultureInfo.InitUserDefaultUICulture();
					CultureInfo.s_userDefaultUICulture = cultureInfo;
				}
				return cultureInfo;
			}
		}

		// Token: 0x17000605 RID: 1541
		// (get) Token: 0x06002E68 RID: 11880 RVA: 0x000B183B File Offset: 0x000AFA3B
		// (set) Token: 0x06002E69 RID: 11881 RVA: 0x000B1847 File Offset: 0x000AFA47
		[__DynamicallyInvokable]
		public static CultureInfo CurrentUICulture
		{
			[__DynamicallyInvokable]
			get
			{
				return Thread.CurrentThread.CurrentUICulture;
			}
			[__DynamicallyInvokable]
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (AppDomain.IsAppXModel() && CultureInfo.SetCultureInfoForUserPreferredLanguageInAppX(value))
				{
					return;
				}
				Thread.CurrentThread.CurrentUICulture = value;
			}
		}

		// Token: 0x17000606 RID: 1542
		// (get) Token: 0x06002E6A RID: 11882 RVA: 0x000B1874 File Offset: 0x000AFA74
		public static CultureInfo InstalledUICulture
		{
			get
			{
				CultureInfo cultureInfo = CultureInfo.s_InstalledUICultureInfo;
				if (cultureInfo == null)
				{
					string systemDefaultUILanguage = CultureInfo.GetSystemDefaultUILanguage();
					cultureInfo = CultureInfo.GetCultureByName(systemDefaultUILanguage, true);
					if (cultureInfo == null)
					{
						cultureInfo = CultureInfo.InvariantCulture;
					}
					cultureInfo.m_isReadOnly = true;
					CultureInfo.s_InstalledUICultureInfo = cultureInfo;
				}
				return cultureInfo;
			}
		}

		// Token: 0x17000607 RID: 1543
		// (get) Token: 0x06002E6B RID: 11883 RVA: 0x000B18B3 File Offset: 0x000AFAB3
		// (set) Token: 0x06002E6C RID: 11884 RVA: 0x000B18BC File Offset: 0x000AFABC
		[__DynamicallyInvokable]
		public static CultureInfo DefaultThreadCurrentCulture
		{
			[__DynamicallyInvokable]
			get
			{
				return CultureInfo.s_DefaultThreadCurrentCulture;
			}
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			[SecurityPermission(SecurityAction.Demand, ControlThread = true)]
			set
			{
				CultureInfo.s_DefaultThreadCurrentCulture = value;
			}
		}

		// Token: 0x17000608 RID: 1544
		// (get) Token: 0x06002E6D RID: 11885 RVA: 0x000B18C6 File Offset: 0x000AFAC6
		// (set) Token: 0x06002E6E RID: 11886 RVA: 0x000B18CF File Offset: 0x000AFACF
		[__DynamicallyInvokable]
		public static CultureInfo DefaultThreadCurrentUICulture
		{
			[__DynamicallyInvokable]
			get
			{
				return CultureInfo.s_DefaultThreadCurrentUICulture;
			}
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			[SecurityPermission(SecurityAction.Demand, ControlThread = true)]
			set
			{
				if (value != null)
				{
					CultureInfo.VerifyCultureName(value, true);
				}
				CultureInfo.s_DefaultThreadCurrentUICulture = value;
			}
		}

		// Token: 0x17000609 RID: 1545
		// (get) Token: 0x06002E6F RID: 11887 RVA: 0x000B18E4 File Offset: 0x000AFAE4
		[__DynamicallyInvokable]
		public static CultureInfo InvariantCulture
		{
			[__DynamicallyInvokable]
			get
			{
				return CultureInfo.s_InvariantCultureInfo;
			}
		}

		// Token: 0x1700060A RID: 1546
		// (get) Token: 0x06002E70 RID: 11888 RVA: 0x000B18F0 File Offset: 0x000AFAF0
		[__DynamicallyInvokable]
		public virtual CultureInfo Parent
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				if (this.m_parent == null)
				{
					string sparent = this.m_cultureData.SPARENT;
					CultureInfo cultureInfo;
					if (string.IsNullOrEmpty(sparent))
					{
						cultureInfo = CultureInfo.InvariantCulture;
					}
					else
					{
						cultureInfo = CultureInfo.CreateCultureInfoNoThrow(sparent, this.m_cultureData.UseUserOverride);
						if (cultureInfo == null)
						{
							cultureInfo = CultureInfo.InvariantCulture;
						}
					}
					Interlocked.CompareExchange<CultureInfo>(ref this.m_parent, cultureInfo, null);
				}
				return this.m_parent;
			}
		}

		// Token: 0x1700060B RID: 1547
		// (get) Token: 0x06002E71 RID: 11889 RVA: 0x000B1952 File Offset: 0x000AFB52
		public virtual int LCID
		{
			get
			{
				return this.m_cultureData.ILANGUAGE;
			}
		}

		// Token: 0x1700060C RID: 1548
		// (get) Token: 0x06002E72 RID: 11890 RVA: 0x000B1960 File Offset: 0x000AFB60
		[ComVisible(false)]
		public virtual int KeyboardLayoutId
		{
			get
			{
				return this.m_cultureData.IINPUTLANGUAGEHANDLE;
			}
		}

		// Token: 0x06002E73 RID: 11891 RVA: 0x000B197A File Offset: 0x000AFB7A
		public static CultureInfo[] GetCultures(CultureTypes types)
		{
			if ((types & CultureTypes.UserCustomCulture) == CultureTypes.UserCustomCulture)
			{
				types |= CultureTypes.ReplacementCultures;
			}
			return CultureData.GetCultures(types);
		}

		// Token: 0x1700060D RID: 1549
		// (get) Token: 0x06002E74 RID: 11892 RVA: 0x000B198E File Offset: 0x000AFB8E
		[__DynamicallyInvokable]
		public virtual string Name
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.m_nonSortName == null)
				{
					this.m_nonSortName = this.m_cultureData.SNAME;
					if (this.m_nonSortName == null)
					{
						this.m_nonSortName = string.Empty;
					}
				}
				return this.m_nonSortName;
			}
		}

		// Token: 0x1700060E RID: 1550
		// (get) Token: 0x06002E75 RID: 11893 RVA: 0x000B19C2 File Offset: 0x000AFBC2
		internal string SortName
		{
			get
			{
				if (this.m_sortName == null)
				{
					this.m_sortName = this.m_cultureData.SCOMPAREINFO;
				}
				return this.m_sortName;
			}
		}

		// Token: 0x1700060F RID: 1551
		// (get) Token: 0x06002E76 RID: 11894 RVA: 0x000B19E4 File Offset: 0x000AFBE4
		[ComVisible(false)]
		public string IetfLanguageTag
		{
			get
			{
				string name = this.Name;
				if (name == "zh-CHT")
				{
					return "zh-Hant";
				}
				if (!(name == "zh-CHS"))
				{
					return this.Name;
				}
				return "zh-Hans";
			}
		}

		// Token: 0x17000610 RID: 1552
		// (get) Token: 0x06002E77 RID: 11895 RVA: 0x000B1A26 File Offset: 0x000AFC26
		[__DynamicallyInvokable]
		public virtual string DisplayName
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				return this.m_cultureData.SLOCALIZEDDISPLAYNAME;
			}
		}

		// Token: 0x17000611 RID: 1553
		// (get) Token: 0x06002E78 RID: 11896 RVA: 0x000B1A33 File Offset: 0x000AFC33
		[__DynamicallyInvokable]
		public virtual string NativeName
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				return this.m_cultureData.SNATIVEDISPLAYNAME;
			}
		}

		// Token: 0x17000612 RID: 1554
		// (get) Token: 0x06002E79 RID: 11897 RVA: 0x000B1A40 File Offset: 0x000AFC40
		[__DynamicallyInvokable]
		public virtual string EnglishName
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				return this.m_cultureData.SENGDISPLAYNAME;
			}
		}

		// Token: 0x17000613 RID: 1555
		// (get) Token: 0x06002E7A RID: 11898 RVA: 0x000B1A4D File Offset: 0x000AFC4D
		[__DynamicallyInvokable]
		public virtual string TwoLetterISOLanguageName
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				return this.m_cultureData.SISO639LANGNAME;
			}
		}

		// Token: 0x17000614 RID: 1556
		// (get) Token: 0x06002E7B RID: 11899 RVA: 0x000B1A5A File Offset: 0x000AFC5A
		public virtual string ThreeLetterISOLanguageName
		{
			[SecuritySafeCritical]
			get
			{
				return this.m_cultureData.SISO639LANGNAME2;
			}
		}

		// Token: 0x17000615 RID: 1557
		// (get) Token: 0x06002E7C RID: 11900 RVA: 0x000B1A67 File Offset: 0x000AFC67
		public virtual string ThreeLetterWindowsLanguageName
		{
			[SecuritySafeCritical]
			get
			{
				return this.m_cultureData.SABBREVLANGNAME;
			}
		}

		// Token: 0x17000616 RID: 1558
		// (get) Token: 0x06002E7D RID: 11901 RVA: 0x000B1A74 File Offset: 0x000AFC74
		[__DynamicallyInvokable]
		public virtual CompareInfo CompareInfo
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.compareInfo == null)
				{
					CompareInfo result = (this.UseUserOverride ? CultureInfo.GetCultureInfo(this.m_name).CompareInfo : new CompareInfo(this));
					if (!CompatibilitySwitches.IsCompatibilityBehaviorDefined)
					{
						return result;
					}
					this.compareInfo = result;
				}
				return this.compareInfo;
			}
		}

		// Token: 0x17000617 RID: 1559
		// (get) Token: 0x06002E7E RID: 11902 RVA: 0x000B1AC4 File Offset: 0x000AFCC4
		private RegionInfo Region
		{
			get
			{
				if (this.regionInfo == null)
				{
					RegionInfo regionInfo = new RegionInfo(this.m_cultureData);
					this.regionInfo = regionInfo;
				}
				return this.regionInfo;
			}
		}

		// Token: 0x17000618 RID: 1560
		// (get) Token: 0x06002E7F RID: 11903 RVA: 0x000B1AF4 File Offset: 0x000AFCF4
		[__DynamicallyInvokable]
		public virtual TextInfo TextInfo
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.textInfo == null)
				{
					TextInfo textInfo = new TextInfo(this.m_cultureData);
					textInfo.SetReadOnlyState(this.m_isReadOnly);
					if (!CompatibilitySwitches.IsCompatibilityBehaviorDefined)
					{
						return textInfo;
					}
					this.textInfo = textInfo;
				}
				return this.textInfo;
			}
		}

		// Token: 0x06002E80 RID: 11904 RVA: 0x000B1B3C File Offset: 0x000AFD3C
		[__DynamicallyInvokable]
		public override bool Equals(object value)
		{
			if (this == value)
			{
				return true;
			}
			CultureInfo cultureInfo = value as CultureInfo;
			return cultureInfo != null && this.Name.Equals(cultureInfo.Name) && this.CompareInfo.Equals(cultureInfo.CompareInfo);
		}

		// Token: 0x06002E81 RID: 11905 RVA: 0x000B1B81 File Offset: 0x000AFD81
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return this.Name.GetHashCode() + this.CompareInfo.GetHashCode();
		}

		// Token: 0x06002E82 RID: 11906 RVA: 0x000B1B9A File Offset: 0x000AFD9A
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return this.m_name;
		}

		// Token: 0x06002E83 RID: 11907 RVA: 0x000B1BA2 File Offset: 0x000AFDA2
		[__DynamicallyInvokable]
		public virtual object GetFormat(Type formatType)
		{
			if (formatType == typeof(NumberFormatInfo))
			{
				return this.NumberFormat;
			}
			if (formatType == typeof(DateTimeFormatInfo))
			{
				return this.DateTimeFormat;
			}
			return null;
		}

		// Token: 0x17000619 RID: 1561
		// (get) Token: 0x06002E84 RID: 11908 RVA: 0x000B1BD7 File Offset: 0x000AFDD7
		[__DynamicallyInvokable]
		public virtual bool IsNeutralCulture
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_cultureData.IsNeutralCulture;
			}
		}

		// Token: 0x1700061A RID: 1562
		// (get) Token: 0x06002E85 RID: 11909 RVA: 0x000B1BE4 File Offset: 0x000AFDE4
		[ComVisible(false)]
		public CultureTypes CultureTypes
		{
			get
			{
				CultureTypes cultureTypes = (CultureTypes)0;
				if (this.m_cultureData.IsNeutralCulture)
				{
					cultureTypes |= CultureTypes.NeutralCultures;
				}
				else
				{
					cultureTypes |= CultureTypes.SpecificCultures;
				}
				cultureTypes |= (this.m_cultureData.IsWin32Installed ? CultureTypes.InstalledWin32Cultures : ((CultureTypes)0));
				cultureTypes |= (this.m_cultureData.IsFramework ? CultureTypes.FrameworkCultures : ((CultureTypes)0));
				cultureTypes |= (this.m_cultureData.IsSupplementalCustomCulture ? CultureTypes.UserCustomCulture : ((CultureTypes)0));
				return cultureTypes | (this.m_cultureData.IsReplacementCulture ? (CultureTypes.UserCustomCulture | CultureTypes.ReplacementCultures) : ((CultureTypes)0));
			}
		}

		// Token: 0x1700061B RID: 1563
		// (get) Token: 0x06002E86 RID: 11910 RVA: 0x000B1C60 File Offset: 0x000AFE60
		// (set) Token: 0x06002E87 RID: 11911 RVA: 0x000B1C9A File Offset: 0x000AFE9A
		[__DynamicallyInvokable]
		public virtual NumberFormatInfo NumberFormat
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.numInfo == null)
				{
					this.numInfo = new NumberFormatInfo(this.m_cultureData)
					{
						isReadOnly = this.m_isReadOnly
					};
				}
				return this.numInfo;
			}
			[__DynamicallyInvokable]
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Obj"));
				}
				this.VerifyWritable();
				this.numInfo = value;
			}
		}

		// Token: 0x1700061C RID: 1564
		// (get) Token: 0x06002E88 RID: 11912 RVA: 0x000B1CC4 File Offset: 0x000AFEC4
		// (set) Token: 0x06002E89 RID: 11913 RVA: 0x000B1D09 File Offset: 0x000AFF09
		[__DynamicallyInvokable]
		public virtual DateTimeFormatInfo DateTimeFormat
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.dateTimeInfo == null)
				{
					DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo(this.m_cultureData, this.Calendar);
					dateTimeFormatInfo.m_isReadOnly = this.m_isReadOnly;
					Thread.MemoryBarrier();
					this.dateTimeInfo = dateTimeFormatInfo;
				}
				return this.dateTimeInfo;
			}
			[__DynamicallyInvokable]
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Obj"));
				}
				this.VerifyWritable();
				this.dateTimeInfo = value;
			}
		}

		// Token: 0x06002E8A RID: 11914 RVA: 0x000B1D30 File Offset: 0x000AFF30
		public void ClearCachedData()
		{
			CultureInfo.s_userDefaultUICulture = null;
			CultureInfo.s_userDefaultCulture = null;
			RegionInfo.s_currentRegionInfo = null;
			TimeZone.ResetTimeZone();
			TimeZoneInfo.ClearCachedData();
			CultureInfo.s_LcidCachedCultures = null;
			CultureInfo.s_NameCachedCultures = null;
			CultureData.ClearCachedData();
		}

		// Token: 0x06002E8B RID: 11915 RVA: 0x000B1D69 File Offset: 0x000AFF69
		internal static Calendar GetCalendarInstance(int calType)
		{
			if (calType == 1)
			{
				return new GregorianCalendar();
			}
			return CultureInfo.GetCalendarInstanceRare(calType);
		}

		// Token: 0x06002E8C RID: 11916 RVA: 0x000B1D7C File Offset: 0x000AFF7C
		internal static Calendar GetCalendarInstanceRare(int calType)
		{
			switch (calType)
			{
			case 2:
			case 9:
			case 10:
			case 11:
			case 12:
				return new GregorianCalendar((GregorianCalendarTypes)calType);
			case 3:
				return new JapaneseCalendar();
			case 4:
				return new TaiwanCalendar();
			case 5:
				return new KoreanCalendar();
			case 6:
				return new HijriCalendar();
			case 7:
				return new ThaiBuddhistCalendar();
			case 8:
				return new HebrewCalendar();
			case 14:
				return new JapaneseLunisolarCalendar();
			case 15:
				return new ChineseLunisolarCalendar();
			case 20:
				return new KoreanLunisolarCalendar();
			case 21:
				return new TaiwanLunisolarCalendar();
			case 22:
				return new PersianCalendar();
			case 23:
				return new UmAlQuraCalendar();
			}
			return new GregorianCalendar();
		}

		// Token: 0x1700061D RID: 1565
		// (get) Token: 0x06002E8D RID: 11917 RVA: 0x000B1E40 File Offset: 0x000B0040
		[__DynamicallyInvokable]
		public virtual Calendar Calendar
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.calendar == null)
				{
					Calendar defaultCalendar = this.m_cultureData.DefaultCalendar;
					Thread.MemoryBarrier();
					defaultCalendar.SetReadOnlyState(this.m_isReadOnly);
					this.calendar = defaultCalendar;
				}
				return this.calendar;
			}
		}

		// Token: 0x1700061E RID: 1566
		// (get) Token: 0x06002E8E RID: 11918 RVA: 0x000B1E80 File Offset: 0x000B0080
		[__DynamicallyInvokable]
		public virtual Calendar[] OptionalCalendars
		{
			[__DynamicallyInvokable]
			get
			{
				int[] calendarIds = this.m_cultureData.CalendarIds;
				Calendar[] array = new Calendar[calendarIds.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = CultureInfo.GetCalendarInstance(calendarIds[i]);
				}
				return array;
			}
		}

		// Token: 0x1700061F RID: 1567
		// (get) Token: 0x06002E8F RID: 11919 RVA: 0x000B1EBC File Offset: 0x000B00BC
		public bool UseUserOverride
		{
			get
			{
				return this.m_cultureData.UseUserOverride;
			}
		}

		// Token: 0x06002E90 RID: 11920 RVA: 0x000B1ECC File Offset: 0x000B00CC
		[SecuritySafeCritical]
		[ComVisible(false)]
		public CultureInfo GetConsoleFallbackUICulture()
		{
			CultureInfo cultureInfo = this.m_consoleFallbackCulture;
			if (cultureInfo == null)
			{
				cultureInfo = CultureInfo.CreateSpecificCulture(this.m_cultureData.SCONSOLEFALLBACKNAME);
				cultureInfo.m_isReadOnly = true;
				this.m_consoleFallbackCulture = cultureInfo;
			}
			return cultureInfo;
		}

		// Token: 0x06002E91 RID: 11921 RVA: 0x000B1F04 File Offset: 0x000B0104
		[__DynamicallyInvokable]
		public virtual object Clone()
		{
			CultureInfo cultureInfo = (CultureInfo)base.MemberwiseClone();
			cultureInfo.m_isReadOnly = false;
			if (!this.m_isInherited)
			{
				if (this.dateTimeInfo != null)
				{
					cultureInfo.dateTimeInfo = (DateTimeFormatInfo)this.dateTimeInfo.Clone();
				}
				if (this.numInfo != null)
				{
					cultureInfo.numInfo = (NumberFormatInfo)this.numInfo.Clone();
				}
			}
			else
			{
				cultureInfo.DateTimeFormat = (DateTimeFormatInfo)this.DateTimeFormat.Clone();
				cultureInfo.NumberFormat = (NumberFormatInfo)this.NumberFormat.Clone();
			}
			if (this.textInfo != null)
			{
				cultureInfo.textInfo = (TextInfo)this.textInfo.Clone();
			}
			if (this.calendar != null)
			{
				cultureInfo.calendar = (Calendar)this.calendar.Clone();
			}
			return cultureInfo;
		}

		// Token: 0x06002E92 RID: 11922 RVA: 0x000B1FD4 File Offset: 0x000B01D4
		[__DynamicallyInvokable]
		public static CultureInfo ReadOnly(CultureInfo ci)
		{
			if (ci == null)
			{
				throw new ArgumentNullException("ci");
			}
			if (ci.IsReadOnly)
			{
				return ci;
			}
			CultureInfo cultureInfo = (CultureInfo)ci.MemberwiseClone();
			if (!ci.IsNeutralCulture)
			{
				if (!ci.m_isInherited)
				{
					if (ci.dateTimeInfo != null)
					{
						cultureInfo.dateTimeInfo = DateTimeFormatInfo.ReadOnly(ci.dateTimeInfo);
					}
					if (ci.numInfo != null)
					{
						cultureInfo.numInfo = NumberFormatInfo.ReadOnly(ci.numInfo);
					}
				}
				else
				{
					cultureInfo.DateTimeFormat = DateTimeFormatInfo.ReadOnly(ci.DateTimeFormat);
					cultureInfo.NumberFormat = NumberFormatInfo.ReadOnly(ci.NumberFormat);
				}
			}
			if (ci.textInfo != null)
			{
				cultureInfo.textInfo = TextInfo.ReadOnly(ci.textInfo);
			}
			if (ci.calendar != null)
			{
				cultureInfo.calendar = Calendar.ReadOnly(ci.calendar);
			}
			cultureInfo.m_isReadOnly = true;
			return cultureInfo;
		}

		// Token: 0x17000620 RID: 1568
		// (get) Token: 0x06002E93 RID: 11923 RVA: 0x000B20A5 File Offset: 0x000B02A5
		[__DynamicallyInvokable]
		public bool IsReadOnly
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_isReadOnly;
			}
		}

		// Token: 0x06002E94 RID: 11924 RVA: 0x000B20AD File Offset: 0x000B02AD
		private void VerifyWritable()
		{
			if (this.m_isReadOnly)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
			}
		}

		// Token: 0x17000621 RID: 1569
		// (get) Token: 0x06002E95 RID: 11925 RVA: 0x000B20C7 File Offset: 0x000B02C7
		internal bool HasInvariantCultureName
		{
			get
			{
				return this.Name == CultureInfo.InvariantCulture.Name;
			}
		}

		// Token: 0x06002E96 RID: 11926 RVA: 0x000B20E0 File Offset: 0x000B02E0
		internal static CultureInfo GetCultureInfoHelper(int lcid, string name, string altName)
		{
			Hashtable hashtable = CultureInfo.s_NameCachedCultures;
			if (name != null)
			{
				name = CultureData.AnsiToLower(name);
			}
			if (altName != null)
			{
				altName = CultureData.AnsiToLower(altName);
			}
			CultureInfo cultureInfo;
			if (hashtable == null)
			{
				hashtable = Hashtable.Synchronized(new Hashtable());
			}
			else if (lcid == -1)
			{
				cultureInfo = (CultureInfo)hashtable[name + "\ufffd" + altName];
				if (cultureInfo != null)
				{
					return cultureInfo;
				}
			}
			else if (lcid == 0)
			{
				cultureInfo = (CultureInfo)hashtable[name];
				if (cultureInfo != null)
				{
					return cultureInfo;
				}
			}
			Hashtable hashtable2 = CultureInfo.s_LcidCachedCultures;
			if (hashtable2 == null)
			{
				hashtable2 = Hashtable.Synchronized(new Hashtable());
			}
			else if (lcid > 0)
			{
				cultureInfo = (CultureInfo)hashtable2[lcid];
				if (cultureInfo != null)
				{
					return cultureInfo;
				}
			}
			try
			{
				if (lcid != -1)
				{
					if (lcid != 0)
					{
						cultureInfo = new CultureInfo(lcid, false);
					}
					else
					{
						cultureInfo = new CultureInfo(name, false);
					}
				}
				else
				{
					cultureInfo = new CultureInfo(name, altName);
				}
			}
			catch (ArgumentException)
			{
				return null;
			}
			cultureInfo.m_isReadOnly = true;
			if (lcid == -1)
			{
				hashtable[name + "\ufffd" + altName] = cultureInfo;
				cultureInfo.TextInfo.SetReadOnlyState(true);
			}
			else
			{
				string text = CultureData.AnsiToLower(cultureInfo.m_name);
				hashtable[text] = cultureInfo;
				if ((cultureInfo.LCID != 4 || !(text == "zh-hans")) && (cultureInfo.LCID != 31748 || !(text == "zh-hant")))
				{
					hashtable2[cultureInfo.LCID] = cultureInfo;
				}
			}
			if (-1 != lcid)
			{
				CultureInfo.s_LcidCachedCultures = hashtable2;
			}
			CultureInfo.s_NameCachedCultures = hashtable;
			return cultureInfo;
		}

		// Token: 0x06002E97 RID: 11927 RVA: 0x000B2264 File Offset: 0x000B0464
		public static CultureInfo GetCultureInfo(int culture)
		{
			if (culture <= 0)
			{
				throw new ArgumentOutOfRangeException("culture", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			CultureInfo cultureInfoHelper = CultureInfo.GetCultureInfoHelper(culture, null, null);
			if (cultureInfoHelper == null)
			{
				throw new CultureNotFoundException("culture", culture, Environment.GetResourceString("Argument_CultureNotSupported"));
			}
			return cultureInfoHelper;
		}

		// Token: 0x06002E98 RID: 11928 RVA: 0x000B22B0 File Offset: 0x000B04B0
		public static CultureInfo GetCultureInfo(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			CultureInfo cultureInfoHelper = CultureInfo.GetCultureInfoHelper(0, name, null);
			if (cultureInfoHelper == null)
			{
				throw new CultureNotFoundException("name", name, Environment.GetResourceString("Argument_CultureNotSupported"));
			}
			return cultureInfoHelper;
		}

		// Token: 0x06002E99 RID: 11929 RVA: 0x000B22F0 File Offset: 0x000B04F0
		public static CultureInfo GetCultureInfo(string name, string altName)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (altName == null)
			{
				throw new ArgumentNullException("altName");
			}
			CultureInfo cultureInfoHelper = CultureInfo.GetCultureInfoHelper(-1, name, altName);
			if (cultureInfoHelper == null)
			{
				throw new CultureNotFoundException("name or altName", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_OneOfCulturesNotSupported"), name, altName));
			}
			return cultureInfoHelper;
		}

		// Token: 0x06002E9A RID: 11930 RVA: 0x000B2348 File Offset: 0x000B0548
		public static CultureInfo GetCultureInfoByIetfLanguageTag(string name)
		{
			if (name == "zh-CHT" || name == "zh-CHS")
			{
				throw new CultureNotFoundException("name", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_CultureIetfNotSupported"), name));
			}
			CultureInfo cultureInfo = CultureInfo.GetCultureInfo(name);
			if (cultureInfo.LCID > 65535 || cultureInfo.LCID == 1034)
			{
				throw new CultureNotFoundException("name", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_CultureIetfNotSupported"), name));
			}
			return cultureInfo;
		}

		// Token: 0x17000622 RID: 1570
		// (get) Token: 0x06002E9B RID: 11931 RVA: 0x000B23D1 File Offset: 0x000B05D1
		internal static bool IsTaiwanSku
		{
			get
			{
				if (!CultureInfo.s_haveIsTaiwanSku)
				{
					CultureInfo.s_isTaiwanSku = CultureInfo.GetSystemDefaultUILanguage() == "zh-TW";
					CultureInfo.s_haveIsTaiwanSku = true;
				}
				return CultureInfo.s_isTaiwanSku;
			}
		}

		// Token: 0x06002E9C RID: 11932
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern string nativeGetLocaleInfoEx(string localeName, uint field);

		// Token: 0x06002E9D RID: 11933
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int nativeGetLocaleInfoExInt(string localeName, uint field);

		// Token: 0x06002E9E RID: 11934
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool nativeSetThreadLocale(string localeName);

		// Token: 0x06002E9F RID: 11935 RVA: 0x000B2404 File Offset: 0x000B0604
		[SecurityCritical]
		private static string GetDefaultLocaleName(int localeType)
		{
			string result = null;
			if (CultureInfo.InternalGetDefaultLocaleName(localeType, JitHelpers.GetStringHandleOnStack(ref result)))
			{
				return result;
			}
			return string.Empty;
		}

		// Token: 0x06002EA0 RID: 11936
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool InternalGetDefaultLocaleName(int localetype, StringHandleOnStack localeString);

		// Token: 0x06002EA1 RID: 11937 RVA: 0x000B242C File Offset: 0x000B062C
		[SecuritySafeCritical]
		private static string GetUserDefaultUILanguage()
		{
			string result = null;
			if (CultureInfo.InternalGetUserDefaultUILanguage(JitHelpers.GetStringHandleOnStack(ref result)))
			{
				return result;
			}
			return string.Empty;
		}

		// Token: 0x06002EA2 RID: 11938
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool InternalGetUserDefaultUILanguage(StringHandleOnStack userDefaultUiLanguage);

		// Token: 0x06002EA3 RID: 11939 RVA: 0x000B2450 File Offset: 0x000B0650
		[SecuritySafeCritical]
		private static string GetSystemDefaultUILanguage()
		{
			string result = null;
			if (CultureInfo.InternalGetSystemDefaultUILanguage(JitHelpers.GetStringHandleOnStack(ref result)))
			{
				return result;
			}
			return string.Empty;
		}

		// Token: 0x06002EA4 RID: 11940
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool InternalGetSystemDefaultUILanguage(StringHandleOnStack systemDefaultUiLanguage);

		// Token: 0x0400132F RID: 4911
		internal bool m_isReadOnly;

		// Token: 0x04001330 RID: 4912
		internal CompareInfo compareInfo;

		// Token: 0x04001331 RID: 4913
		internal TextInfo textInfo;

		// Token: 0x04001332 RID: 4914
		[NonSerialized]
		internal RegionInfo regionInfo;

		// Token: 0x04001333 RID: 4915
		internal NumberFormatInfo numInfo;

		// Token: 0x04001334 RID: 4916
		internal DateTimeFormatInfo dateTimeInfo;

		// Token: 0x04001335 RID: 4917
		internal Calendar calendar;

		// Token: 0x04001336 RID: 4918
		[OptionalField(VersionAdded = 1)]
		internal int m_dataItem;

		// Token: 0x04001337 RID: 4919
		[OptionalField(VersionAdded = 1)]
		internal int cultureID;

		// Token: 0x04001338 RID: 4920
		[NonSerialized]
		internal CultureData m_cultureData;

		// Token: 0x04001339 RID: 4921
		[NonSerialized]
		internal bool m_isInherited;

		// Token: 0x0400133A RID: 4922
		[NonSerialized]
		private bool m_isSafeCrossDomain;

		// Token: 0x0400133B RID: 4923
		[NonSerialized]
		private int m_createdDomainID;

		// Token: 0x0400133C RID: 4924
		[NonSerialized]
		private CultureInfo m_consoleFallbackCulture;

		// Token: 0x0400133D RID: 4925
		internal string m_name;

		// Token: 0x0400133E RID: 4926
		[NonSerialized]
		private string m_nonSortName;

		// Token: 0x0400133F RID: 4927
		[NonSerialized]
		private string m_sortName;

		// Token: 0x04001340 RID: 4928
		private static volatile CultureInfo s_userDefaultCulture;

		// Token: 0x04001341 RID: 4929
		private static volatile CultureInfo s_InvariantCultureInfo;

		// Token: 0x04001342 RID: 4930
		private static volatile CultureInfo s_userDefaultUICulture;

		// Token: 0x04001343 RID: 4931
		private static volatile CultureInfo s_InstalledUICultureInfo;

		// Token: 0x04001344 RID: 4932
		private static volatile CultureInfo s_DefaultThreadCurrentUICulture;

		// Token: 0x04001345 RID: 4933
		private static volatile CultureInfo s_DefaultThreadCurrentCulture;

		// Token: 0x04001346 RID: 4934
		private static volatile Hashtable s_LcidCachedCultures;

		// Token: 0x04001347 RID: 4935
		private static volatile Hashtable s_NameCachedCultures;

		// Token: 0x04001348 RID: 4936
		[SecurityCritical]
		private static volatile WindowsRuntimeResourceManagerBase s_WindowsRuntimeResourceManager;

		// Token: 0x04001349 RID: 4937
		[ThreadStatic]
		private static bool ts_IsDoingAppXCultureInfoLookup;

		// Token: 0x0400134A RID: 4938
		[NonSerialized]
		private CultureInfo m_parent;

		// Token: 0x0400134B RID: 4939
		internal const int LOCALE_NEUTRAL = 0;

		// Token: 0x0400134C RID: 4940
		private const int LOCALE_USER_DEFAULT = 1024;

		// Token: 0x0400134D RID: 4941
		private const int LOCALE_SYSTEM_DEFAULT = 2048;

		// Token: 0x0400134E RID: 4942
		internal const int LOCALE_CUSTOM_DEFAULT = 3072;

		// Token: 0x0400134F RID: 4943
		internal const int LOCALE_CUSTOM_UNSPECIFIED = 4096;

		// Token: 0x04001350 RID: 4944
		internal const int LOCALE_INVARIANT = 127;

		// Token: 0x04001351 RID: 4945
		private const int LOCALE_TRADITIONAL_SPANISH = 1034;

		// Token: 0x04001352 RID: 4946
		private static readonly bool init = CultureInfo.Init();

		// Token: 0x04001353 RID: 4947
		private bool m_useUserOverride;

		// Token: 0x04001354 RID: 4948
		private const int LOCALE_SORTID_MASK = 983040;

		// Token: 0x04001355 RID: 4949
		private static volatile bool s_isTaiwanSku;

		// Token: 0x04001356 RID: 4950
		private static volatile bool s_haveIsTaiwanSku;
	}
}
