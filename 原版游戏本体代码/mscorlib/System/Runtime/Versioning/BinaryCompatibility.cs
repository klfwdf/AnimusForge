using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.Versioning
{
	// Token: 0x0200071C RID: 1820
	[FriendAccessAllowed]
	internal static class BinaryCompatibility
	{
		// Token: 0x17000D5C RID: 3420
		// (get) Token: 0x06005144 RID: 20804 RVA: 0x0011E99F File Offset: 0x0011CB9F
		[FriendAccessAllowed]
		internal static bool TargetsAtLeast_Phone_V7_1
		{
			[FriendAccessAllowed]
			get
			{
				return BinaryCompatibility.s_map.TargetsAtLeast_Phone_V7_1;
			}
		}

		// Token: 0x17000D5D RID: 3421
		// (get) Token: 0x06005145 RID: 20805 RVA: 0x0011E9AB File Offset: 0x0011CBAB
		[FriendAccessAllowed]
		internal static bool TargetsAtLeast_Phone_V8_0
		{
			[FriendAccessAllowed]
			get
			{
				return BinaryCompatibility.s_map.TargetsAtLeast_Phone_V8_0;
			}
		}

		// Token: 0x17000D5E RID: 3422
		// (get) Token: 0x06005146 RID: 20806 RVA: 0x0011E9B7 File Offset: 0x0011CBB7
		[FriendAccessAllowed]
		internal static bool TargetsAtLeast_Desktop_V4_5
		{
			[FriendAccessAllowed]
			get
			{
				return BinaryCompatibility.s_map.TargetsAtLeast_Desktop_V4_5;
			}
		}

		// Token: 0x17000D5F RID: 3423
		// (get) Token: 0x06005147 RID: 20807 RVA: 0x0011E9C3 File Offset: 0x0011CBC3
		[FriendAccessAllowed]
		internal static bool TargetsAtLeast_Desktop_V4_5_1
		{
			[FriendAccessAllowed]
			get
			{
				return BinaryCompatibility.s_map.TargetsAtLeast_Desktop_V4_5_1;
			}
		}

		// Token: 0x17000D60 RID: 3424
		// (get) Token: 0x06005148 RID: 20808 RVA: 0x0011E9CF File Offset: 0x0011CBCF
		[FriendAccessAllowed]
		internal static bool TargetsAtLeast_Desktop_V4_5_2
		{
			[FriendAccessAllowed]
			get
			{
				return BinaryCompatibility.s_map.TargetsAtLeast_Desktop_V4_5_2;
			}
		}

		// Token: 0x17000D61 RID: 3425
		// (get) Token: 0x06005149 RID: 20809 RVA: 0x0011E9DB File Offset: 0x0011CBDB
		[FriendAccessAllowed]
		internal static bool TargetsAtLeast_Desktop_V4_5_3
		{
			[FriendAccessAllowed]
			get
			{
				return BinaryCompatibility.s_map.TargetsAtLeast_Desktop_V4_5_3;
			}
		}

		// Token: 0x17000D62 RID: 3426
		// (get) Token: 0x0600514A RID: 20810 RVA: 0x0011E9E7 File Offset: 0x0011CBE7
		[FriendAccessAllowed]
		internal static bool TargetsAtLeast_Desktop_V4_5_4
		{
			[FriendAccessAllowed]
			get
			{
				return BinaryCompatibility.s_map.TargetsAtLeast_Desktop_V4_5_4;
			}
		}

		// Token: 0x17000D63 RID: 3427
		// (get) Token: 0x0600514B RID: 20811 RVA: 0x0011E9F3 File Offset: 0x0011CBF3
		[FriendAccessAllowed]
		internal static bool TargetsAtLeast_Desktop_V5_0
		{
			[FriendAccessAllowed]
			get
			{
				return BinaryCompatibility.s_map.TargetsAtLeast_Desktop_V5_0;
			}
		}

		// Token: 0x17000D64 RID: 3428
		// (get) Token: 0x0600514C RID: 20812 RVA: 0x0011E9FF File Offset: 0x0011CBFF
		[FriendAccessAllowed]
		internal static bool TargetsAtLeast_Silverlight_V4
		{
			[FriendAccessAllowed]
			get
			{
				return BinaryCompatibility.s_map.TargetsAtLeast_Silverlight_V4;
			}
		}

		// Token: 0x17000D65 RID: 3429
		// (get) Token: 0x0600514D RID: 20813 RVA: 0x0011EA0B File Offset: 0x0011CC0B
		[FriendAccessAllowed]
		internal static bool TargetsAtLeast_Silverlight_V5
		{
			[FriendAccessAllowed]
			get
			{
				return BinaryCompatibility.s_map.TargetsAtLeast_Silverlight_V5;
			}
		}

		// Token: 0x17000D66 RID: 3430
		// (get) Token: 0x0600514E RID: 20814 RVA: 0x0011EA17 File Offset: 0x0011CC17
		[FriendAccessAllowed]
		internal static bool TargetsAtLeast_Silverlight_V6
		{
			[FriendAccessAllowed]
			get
			{
				return BinaryCompatibility.s_map.TargetsAtLeast_Silverlight_V6;
			}
		}

		// Token: 0x17000D67 RID: 3431
		// (get) Token: 0x0600514F RID: 20815 RVA: 0x0011EA23 File Offset: 0x0011CC23
		[FriendAccessAllowed]
		internal static TargetFrameworkId AppWasBuiltForFramework
		{
			[FriendAccessAllowed]
			get
			{
				if (BinaryCompatibility.s_AppWasBuiltForFramework == TargetFrameworkId.NotYetChecked)
				{
					BinaryCompatibility.ReadTargetFrameworkId();
				}
				return BinaryCompatibility.s_AppWasBuiltForFramework;
			}
		}

		// Token: 0x17000D68 RID: 3432
		// (get) Token: 0x06005150 RID: 20816 RVA: 0x0011EA36 File Offset: 0x0011CC36
		[FriendAccessAllowed]
		internal static int AppWasBuiltForVersion
		{
			[FriendAccessAllowed]
			get
			{
				if (BinaryCompatibility.s_AppWasBuiltForFramework == TargetFrameworkId.NotYetChecked)
				{
					BinaryCompatibility.ReadTargetFrameworkId();
				}
				return BinaryCompatibility.s_AppWasBuiltForVersion;
			}
		}

		// Token: 0x06005151 RID: 20817 RVA: 0x0011EA4C File Offset: 0x0011CC4C
		private static bool ParseTargetFrameworkMonikerIntoEnum(string targetFrameworkMoniker, out TargetFrameworkId targetFramework, out int targetFrameworkVersion)
		{
			targetFramework = TargetFrameworkId.NotYetChecked;
			targetFrameworkVersion = 0;
			string a = null;
			string text = null;
			BinaryCompatibility.ParseFrameworkName(targetFrameworkMoniker, out a, out targetFrameworkVersion, out text);
			if (!(a == ".NETFramework"))
			{
				if (!(a == ".NETPortable"))
				{
					if (!(a == ".NETCore"))
					{
						if (!(a == "WindowsPhone"))
						{
							if (!(a == "WindowsPhoneApp"))
							{
								if (!(a == "Silverlight"))
								{
									targetFramework = TargetFrameworkId.Unrecognized;
								}
								else
								{
									targetFramework = TargetFrameworkId.Silverlight;
									if (!string.IsNullOrEmpty(text))
									{
										if (text == "WindowsPhone")
										{
											targetFramework = TargetFrameworkId.Phone;
											targetFrameworkVersion = 70000;
										}
										else if (text == "WindowsPhone71")
										{
											targetFramework = TargetFrameworkId.Phone;
											targetFrameworkVersion = 70100;
										}
										else if (text == "WindowsPhone8")
										{
											targetFramework = TargetFrameworkId.Phone;
											targetFrameworkVersion = 80000;
										}
										else if (text.StartsWith("WindowsPhone", StringComparison.Ordinal))
										{
											targetFramework = TargetFrameworkId.Unrecognized;
											targetFrameworkVersion = 70100;
										}
										else
										{
											targetFramework = TargetFrameworkId.Unrecognized;
										}
									}
								}
							}
							else
							{
								targetFramework = TargetFrameworkId.Phone;
							}
						}
						else if (targetFrameworkVersion >= 80100)
						{
							targetFramework = TargetFrameworkId.Phone;
						}
						else
						{
							targetFramework = TargetFrameworkId.Unspecified;
						}
					}
					else
					{
						targetFramework = TargetFrameworkId.NetCore;
					}
				}
				else
				{
					targetFramework = TargetFrameworkId.Portable;
				}
			}
			else
			{
				targetFramework = TargetFrameworkId.NetFramework;
			}
			return true;
		}

		// Token: 0x06005152 RID: 20818 RVA: 0x0011EB70 File Offset: 0x0011CD70
		private static void ParseFrameworkName(string frameworkName, out string identifier, out int version, out string profile)
		{
			if (frameworkName == null)
			{
				throw new ArgumentNullException("frameworkName");
			}
			if (frameworkName.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_StringZeroLength"), "frameworkName");
			}
			string[] array = frameworkName.Split(new char[] { ',' });
			version = 0;
			if (array.Length < 2 || array.Length > 3)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_FrameworkNameTooShort"), "frameworkName");
			}
			identifier = array[0].Trim();
			if (identifier.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_FrameworkNameInvalid"), "frameworkName");
			}
			bool flag = false;
			profile = null;
			for (int i = 1; i < array.Length; i++)
			{
				string[] array2 = array[i].Split(new char[] { '=' });
				if (array2.Length != 2)
				{
					throw new ArgumentException(Environment.GetResourceString("SR.Argument_FrameworkNameInvalid"), "frameworkName");
				}
				string text = array2[0].Trim();
				string text2 = array2[1].Trim();
				if (text.Equals("Version", StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
					if (text2.Length > 0 && (text2[0] == 'v' || text2[0] == 'V'))
					{
						text2 = text2.Substring(1);
					}
					Version version2 = new Version(text2);
					version = version2.Major * 10000;
					if (version2.Minor > 0)
					{
						version += version2.Minor * 100;
					}
					if (version2.Build > 0)
					{
						version += version2.Build;
					}
				}
				else
				{
					if (!text.Equals("Profile", StringComparison.OrdinalIgnoreCase))
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_FrameworkNameInvalid"), "frameworkName");
					}
					if (!string.IsNullOrEmpty(text2))
					{
						profile = text2;
					}
				}
			}
			if (!flag)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_FrameworkNameMissingVersion"), "frameworkName");
			}
		}

		// Token: 0x06005153 RID: 20819 RVA: 0x0011ED34 File Offset: 0x0011CF34
		[SecuritySafeCritical]
		private static void ReadTargetFrameworkId()
		{
			string text = AppDomain.CurrentDomain.GetTargetFrameworkName();
			string valueInternal = CompatibilitySwitch.GetValueInternal("TargetFrameworkMoniker");
			if (!string.IsNullOrEmpty(valueInternal))
			{
				text = valueInternal;
			}
			int num = 0;
			TargetFrameworkId targetFrameworkId;
			if (text == null)
			{
				targetFrameworkId = TargetFrameworkId.Unspecified;
			}
			else if (!BinaryCompatibility.ParseTargetFrameworkMonikerIntoEnum(text, out targetFrameworkId, out num))
			{
				targetFrameworkId = TargetFrameworkId.Unrecognized;
			}
			BinaryCompatibility.s_AppWasBuiltForFramework = targetFrameworkId;
			BinaryCompatibility.s_AppWasBuiltForVersion = num;
		}

		// Token: 0x04002402 RID: 9218
		private static TargetFrameworkId s_AppWasBuiltForFramework;

		// Token: 0x04002403 RID: 9219
		private static int s_AppWasBuiltForVersion;

		// Token: 0x04002404 RID: 9220
		private static readonly BinaryCompatibility.BinaryCompatibilityMap s_map = new BinaryCompatibility.BinaryCompatibilityMap();

		// Token: 0x04002405 RID: 9221
		private const char c_componentSeparator = ',';

		// Token: 0x04002406 RID: 9222
		private const char c_keyValueSeparator = '=';

		// Token: 0x04002407 RID: 9223
		private const char c_versionValuePrefix = 'v';

		// Token: 0x04002408 RID: 9224
		private const string c_versionKey = "Version";

		// Token: 0x04002409 RID: 9225
		private const string c_profileKey = "Profile";

		// Token: 0x02000C64 RID: 3172
		private sealed class BinaryCompatibilityMap
		{
			// Token: 0x0600707C RID: 28796 RVA: 0x001835D8 File Offset: 0x001817D8
			internal BinaryCompatibilityMap()
			{
				this.AddQuirksForFramework(BinaryCompatibility.AppWasBuiltForFramework, BinaryCompatibility.AppWasBuiltForVersion);
			}

			// Token: 0x0600707D RID: 28797 RVA: 0x001835F0 File Offset: 0x001817F0
			private void AddQuirksForFramework(TargetFrameworkId builtAgainstFramework, int buildAgainstVersion)
			{
				switch (builtAgainstFramework)
				{
				case TargetFrameworkId.NotYetChecked:
				case TargetFrameworkId.Unrecognized:
				case TargetFrameworkId.Unspecified:
				case TargetFrameworkId.Portable:
					break;
				case TargetFrameworkId.NetFramework:
				case TargetFrameworkId.NetCore:
					if (buildAgainstVersion >= 50000)
					{
						this.TargetsAtLeast_Desktop_V5_0 = true;
					}
					if (buildAgainstVersion >= 40504)
					{
						this.TargetsAtLeast_Desktop_V4_5_4 = true;
					}
					if (buildAgainstVersion >= 40503)
					{
						this.TargetsAtLeast_Desktop_V4_5_3 = true;
					}
					if (buildAgainstVersion >= 40502)
					{
						this.TargetsAtLeast_Desktop_V4_5_2 = true;
					}
					if (buildAgainstVersion >= 40501)
					{
						this.TargetsAtLeast_Desktop_V4_5_1 = true;
					}
					if (buildAgainstVersion >= 40500)
					{
						this.TargetsAtLeast_Desktop_V4_5 = true;
						this.AddQuirksForFramework(TargetFrameworkId.Phone, 70100);
						this.AddQuirksForFramework(TargetFrameworkId.Silverlight, 50000);
						return;
					}
					break;
				case TargetFrameworkId.Silverlight:
					if (buildAgainstVersion >= 40000)
					{
						this.TargetsAtLeast_Silverlight_V4 = true;
					}
					if (buildAgainstVersion >= 50000)
					{
						this.TargetsAtLeast_Silverlight_V5 = true;
					}
					if (buildAgainstVersion >= 60000)
					{
						this.TargetsAtLeast_Silverlight_V6 = true;
					}
					break;
				case TargetFrameworkId.Phone:
					if (buildAgainstVersion >= 80000)
					{
						this.TargetsAtLeast_Phone_V8_0 = true;
					}
					if (buildAgainstVersion >= 80100)
					{
						this.TargetsAtLeast_Desktop_V4_5 = true;
						this.TargetsAtLeast_Desktop_V4_5_1 = true;
					}
					if (buildAgainstVersion >= 710)
					{
						this.TargetsAtLeast_Phone_V7_1 = true;
						return;
					}
					break;
				default:
					return;
				}
			}

			// Token: 0x040037C2 RID: 14274
			internal bool TargetsAtLeast_Phone_V7_1;

			// Token: 0x040037C3 RID: 14275
			internal bool TargetsAtLeast_Phone_V8_0;

			// Token: 0x040037C4 RID: 14276
			internal bool TargetsAtLeast_Phone_V8_1;

			// Token: 0x040037C5 RID: 14277
			internal bool TargetsAtLeast_Desktop_V4_5;

			// Token: 0x040037C6 RID: 14278
			internal bool TargetsAtLeast_Desktop_V4_5_1;

			// Token: 0x040037C7 RID: 14279
			internal bool TargetsAtLeast_Desktop_V4_5_2;

			// Token: 0x040037C8 RID: 14280
			internal bool TargetsAtLeast_Desktop_V4_5_3;

			// Token: 0x040037C9 RID: 14281
			internal bool TargetsAtLeast_Desktop_V4_5_4;

			// Token: 0x040037CA RID: 14282
			internal bool TargetsAtLeast_Desktop_V5_0;

			// Token: 0x040037CB RID: 14283
			internal bool TargetsAtLeast_Silverlight_V4;

			// Token: 0x040037CC RID: 14284
			internal bool TargetsAtLeast_Silverlight_V5;

			// Token: 0x040037CD RID: 14285
			internal bool TargetsAtLeast_Silverlight_V6;
		}
	}
}
