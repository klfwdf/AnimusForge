using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace Microsoft.Win32
{
	// Token: 0x0200000F RID: 15
	internal static class OAVariantLib
	{
		// Token: 0x060000ED RID: 237 RVA: 0x000023C0 File Offset: 0x000005C0
		[SecurityCritical]
		internal static Variant ChangeType(Variant source, Type targetClass, short options, CultureInfo culture)
		{
			if (targetClass == null)
			{
				throw new ArgumentNullException("targetClass");
			}
			if (culture == null)
			{
				throw new ArgumentNullException("culture");
			}
			Variant result = default(Variant);
			OAVariantLib.ChangeTypeEx(ref result, ref source, culture.LCID, targetClass.TypeHandle.Value, OAVariantLib.GetCVTypeFromClass(targetClass), options);
			return result;
		}

		// Token: 0x060000EE RID: 238 RVA: 0x0000241C File Offset: 0x0000061C
		private static int GetCVTypeFromClass(Type ctype)
		{
			int num = -1;
			for (int i = 0; i < OAVariantLib.ClassTypes.Length; i++)
			{
				if (ctype.Equals(OAVariantLib.ClassTypes[i]))
				{
					num = i;
					break;
				}
			}
			if (num == -1)
			{
				num = 18;
			}
			return num;
		}

		// Token: 0x060000EF RID: 239
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void ChangeTypeEx(ref Variant result, ref Variant source, int lcid, IntPtr typeHandle, int cvType, short flags);

		// Token: 0x0400016A RID: 362
		public const int NoValueProp = 1;

		// Token: 0x0400016B RID: 363
		public const int AlphaBool = 2;

		// Token: 0x0400016C RID: 364
		public const int NoUserOverride = 4;

		// Token: 0x0400016D RID: 365
		public const int CalendarHijri = 8;

		// Token: 0x0400016E RID: 366
		public const int LocalBool = 16;

		// Token: 0x0400016F RID: 367
		internal static readonly Type[] ClassTypes = new Type[]
		{
			typeof(Empty),
			typeof(void),
			typeof(bool),
			typeof(char),
			typeof(sbyte),
			typeof(byte),
			typeof(short),
			typeof(ushort),
			typeof(int),
			typeof(uint),
			typeof(long),
			typeof(ulong),
			typeof(float),
			typeof(double),
			typeof(string),
			typeof(void),
			typeof(DateTime),
			typeof(TimeSpan),
			typeof(object),
			typeof(decimal),
			null,
			typeof(Missing),
			typeof(DBNull)
		};

		// Token: 0x04000170 RID: 368
		private const int CV_OBJECT = 18;
	}
}
