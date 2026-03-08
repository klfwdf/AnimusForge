using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace MonoMod
{
	// Token: 0x02000807 RID: 2055
	[NullableContext(1)]
	[Nullable(0)]
	internal static class Switches
	{
		// Token: 0x06002730 RID: 10032 RVA: 0x00087558 File Offset: 0x00085758
		static Switches()
		{
			Type type = Switches.tAppContext;
			Switches.miTryGetSwitch = ((type != null) ? type.GetMethod("TryGetSwitch", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[]
			{
				typeof(string),
				typeof(bool).MakeByRefType()
			}, null) : null);
			MethodInfo methodInfo = Switches.miTryGetSwitch;
			Switches.dTryGetSwitch = ((methodInfo != null) ? methodInfo.TryCreateDelegate<Switches.TryGetSwitchFunc>() : null);
			foreach (object obj in Environment.GetEnvironmentVariables())
			{
				DictionaryEntry envVar = (DictionaryEntry)obj;
				string key = (string)envVar.Key;
				if (key.StartsWith("MONOMOD_", StringComparison.Ordinal) && envVar.Value != null)
				{
					string sw = key.Substring("MONOMOD_".Length);
					Switches.switchValues.TryAdd(sw, Switches.BestEffortParseEnvVar((string)envVar.Value));
				}
			}
		}

		// Token: 0x06002731 RID: 10033 RVA: 0x0008768C File Offset: 0x0008588C
		[return: Nullable(2)]
		private static object BestEffortParseEnvVar(string value)
		{
			if (value.Length == 0)
			{
				return null;
			}
			int ires;
			if (int.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ires))
			{
				return ires;
			}
			long lres;
			if (long.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out lres))
			{
				return lres;
			}
			if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ires))
			{
				return ires;
			}
			if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out lres))
			{
				return lres;
			}
			char c = value[0];
			if (c <= 'Y')
			{
				if (c <= 'N')
				{
					if (c != 'F' && c != 'N')
					{
						goto IL_B7;
					}
				}
				else if (c != 'T' && c != 'Y')
				{
					goto IL_B7;
				}
			}
			else if (c <= 'n')
			{
				if (c != 'f' && c != 'n')
				{
					goto IL_B7;
				}
			}
			else if (c != 't' && c != 'y')
			{
				goto IL_B7;
			}
			bool flag = true;
			goto IL_B9;
			IL_B7:
			flag = false;
			IL_B9:
			if (flag)
			{
				bool bresult;
				if (bool.TryParse(value, out bresult))
				{
					return bresult;
				}
				if (value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Equals("y", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				if (value.Equals("no", StringComparison.OrdinalIgnoreCase) || value.Equals("n", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}
			return value;
		}

		// Token: 0x06002732 RID: 10034 RVA: 0x000877AE File Offset: 0x000859AE
		public static void SetSwitchValue(string @switch, [Nullable(2)] object value)
		{
			Switches.switchValues[@switch] = value;
		}

		// Token: 0x06002733 RID: 10035 RVA: 0x000877BC File Offset: 0x000859BC
		public static void ClearSwitchValue(string @switch)
		{
			object obj;
			Switches.switchValues.TryRemove(@switch, out obj);
		}

		// Token: 0x06002734 RID: 10036 RVA: 0x000877D8 File Offset: 0x000859D8
		[return: Nullable(new byte[] { 1, 1, 2 })]
		private static Func<string, object> MakeGetDataDelegate()
		{
			Type type = Switches.tAppContext;
			MethodInfo methodInfo = ((type != null) ? type.GetMethod("GetData", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null) : null);
			Func<string, object> del = ((methodInfo != null) ? methodInfo.TryCreateDelegate<Func<string, object>>() : null);
			if (del != null)
			{
				try
				{
					del("MonoMod.LogToFile");
				}
				catch
				{
					del = null;
				}
			}
			if (del == null)
			{
				del = new Func<string, object>(AppDomain.CurrentDomain.GetData);
			}
			return del;
		}

		// Token: 0x06002735 RID: 10037 RVA: 0x0008785C File Offset: 0x00085A5C
		public static bool TryGetSwitchValue(string @switch, [Nullable(2)] out object value)
		{
			if (Switches.switchValues.TryGetValue(@switch, out value))
			{
				return true;
			}
			if (Switches.dGetData != null || Switches.dTryGetSwitch != null)
			{
				string appCtxSwitchName = "MonoMod." + @switch;
				Func<string, object> func = Switches.dGetData;
				object res = ((func != null) ? func(appCtxSwitchName) : null);
				if (res != null)
				{
					value = res;
					return true;
				}
				Switches.TryGetSwitchFunc tryGetSwitch = Switches.dTryGetSwitch;
				bool switchEnabled;
				if (tryGetSwitch != null && tryGetSwitch(appCtxSwitchName, out switchEnabled))
				{
					value = switchEnabled;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002736 RID: 10038 RVA: 0x000878D0 File Offset: 0x00085AD0
		public static bool TryGetSwitchEnabled(string @switch, out bool isEnabled)
		{
			object orig;
			if (Switches.switchValues.TryGetValue(@switch, out orig) && orig != null && Switches.TryProcessBoolData(orig, out isEnabled))
			{
				return true;
			}
			if (Switches.dGetData != null || Switches.dTryGetSwitch != null)
			{
				string appCtxSwitchName = "MonoMod." + @switch;
				Switches.TryGetSwitchFunc tryGetSwitch = Switches.dTryGetSwitch;
				if (tryGetSwitch != null && tryGetSwitch(appCtxSwitchName, out isEnabled))
				{
					return true;
				}
				Func<string, object> func = Switches.dGetData;
				object res = ((func != null) ? func(appCtxSwitchName) : null);
				if (res != null && Switches.TryProcessBoolData(res, out isEnabled))
				{
					return true;
				}
			}
			isEnabled = false;
			return false;
		}

		// Token: 0x06002737 RID: 10039 RVA: 0x00087950 File Offset: 0x00085B50
		private static bool TryProcessBoolData(object data, out bool boolVal)
		{
			if (data is bool)
			{
				bool flag = (bool)data;
				bool b = flag;
				boolVal = b;
				return true;
			}
			if (data is int)
			{
				int num = (int)data;
				int i = num;
				boolVal = i != 0;
				return true;
			}
			if (data is long)
			{
				long num2 = (long)data;
				long j = num2;
				boolVal = j != 0L;
				return true;
			}
			string text = data as string;
			IConvertible convertible;
			if (text == null)
			{
				convertible = data as IConvertible;
				if (convertible == null)
				{
					boolVal = false;
					return false;
				}
			}
			else
			{
				if (bool.TryParse(text, out boolVal))
				{
					return true;
				}
				convertible = (IConvertible)data;
			}
			IConvertible conv = convertible;
			boolVal = conv.ToBoolean(CultureInfo.CurrentCulture);
			return true;
		}

		// Token: 0x040039C7 RID: 14791
		[Nullable(new byte[] { 1, 1, 2 })]
		private static readonly ConcurrentDictionary<string, object> switchValues = new ConcurrentDictionary<string, object>();

		// Token: 0x040039C8 RID: 14792
		private const string Prefix = "MONOMOD_";

		// Token: 0x040039C9 RID: 14793
		public const string RunningOnWine = "RunningOnWine";

		// Token: 0x040039CA RID: 14794
		public const string DebugClr = "DebugClr";

		// Token: 0x040039CB RID: 14795
		public const string JitPath = "JitPath";

		// Token: 0x040039CC RID: 14796
		public const string HelperDropPath = "HelperDropPath";

		// Token: 0x040039CD RID: 14797
		public const string LogRecordHoles = "LogRecordHoles";

		// Token: 0x040039CE RID: 14798
		public const string LogInMemory = "LogInMemory";

		// Token: 0x040039CF RID: 14799
		public const string LogSpam = "LogSpam";

		// Token: 0x040039D0 RID: 14800
		public const string LogReplayQueueLength = "LogReplayQueueLength";

		// Token: 0x040039D1 RID: 14801
		public const string LogToFile = "LogToFile";

		// Token: 0x040039D2 RID: 14802
		public const string LogToFileFilter = "LogToFileFilter";

		// Token: 0x040039D3 RID: 14803
		public const string DMDType = "DMDType";

		// Token: 0x040039D4 RID: 14804
		public const string DMDDebug = "DMDDebug";

		// Token: 0x040039D5 RID: 14805
		public const string DMDDumpTo = "DMDDumpTo";

		// Token: 0x040039D6 RID: 14806
		[Nullable(2)]
		private static readonly Type tAppContext = typeof(AppDomain).Assembly.GetType("System.AppContext");

		// Token: 0x040039D7 RID: 14807
		[Nullable(new byte[] { 1, 1, 2 })]
		private static readonly Func<string, object> dGetData = Switches.MakeGetDataDelegate();

		// Token: 0x040039D8 RID: 14808
		[Nullable(2)]
		private static readonly MethodInfo miTryGetSwitch;

		// Token: 0x040039D9 RID: 14809
		[Nullable(2)]
		private static readonly Switches.TryGetSwitchFunc dTryGetSwitch;

		// Token: 0x02000808 RID: 2056
		// (Invoke) Token: 0x06002739 RID: 10041
		[NullableContext(0)]
		private delegate bool TryGetSwitchFunc(string @switch, out bool isEnabled);
	}
}
