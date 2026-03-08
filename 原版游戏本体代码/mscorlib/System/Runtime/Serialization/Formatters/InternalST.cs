using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Serialization.Formatters
{
	// Token: 0x02000763 RID: 1891
	[SecurityCritical]
	[ComVisible(true)]
	public sealed class InternalST
	{
		// Token: 0x0600530C RID: 21260 RVA: 0x00123C67 File Offset: 0x00121E67
		private InternalST()
		{
		}

		// Token: 0x0600530D RID: 21261 RVA: 0x00123C6F File Offset: 0x00121E6F
		[Conditional("_LOGGING")]
		public static void InfoSoap(params object[] messages)
		{
		}

		// Token: 0x0600530E RID: 21262 RVA: 0x00123C71 File Offset: 0x00121E71
		public static bool SoapCheckEnabled()
		{
			return BCLDebug.CheckEnabled("Soap");
		}

		// Token: 0x0600530F RID: 21263 RVA: 0x00123C80 File Offset: 0x00121E80
		[Conditional("SER_LOGGING")]
		public static void Soap(params object[] messages)
		{
			if (!(messages[0] is string))
			{
				messages[0] = messages[0].GetType().Name + " ";
				return;
			}
			int num = 0;
			object obj = messages[0];
			messages[num] = ((obj != null) ? obj.ToString() : null) + " ";
		}

		// Token: 0x06005310 RID: 21264 RVA: 0x00123CCE File Offset: 0x00121ECE
		[Conditional("_DEBUG")]
		public static void SoapAssert(bool condition, string message)
		{
		}

		// Token: 0x06005311 RID: 21265 RVA: 0x00123CD0 File Offset: 0x00121ED0
		public static void SerializationSetValue(FieldInfo fi, object target, object value)
		{
			if (fi == null)
			{
				throw new ArgumentNullException("fi");
			}
			if (target == null)
			{
				throw new ArgumentNullException("target");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			FormatterServices.SerializationSetValue(fi, target, value);
		}

		// Token: 0x06005312 RID: 21266 RVA: 0x00123D0A File Offset: 0x00121F0A
		public static Assembly LoadAssemblyFromString(string assemblyString)
		{
			return FormatterServices.LoadAssemblyFromString(assemblyString);
		}
	}
}
