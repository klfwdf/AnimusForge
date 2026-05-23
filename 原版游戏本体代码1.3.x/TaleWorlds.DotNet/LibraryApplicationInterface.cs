using System;
using System.Collections.Generic;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000016 RID: 22
	internal class LibraryApplicationInterface
	{
		// Token: 0x0600006B RID: 107 RVA: 0x00002FF8 File Offset: 0x000011F8
		private static T GetObject<T>() where T : class
		{
			object obj;
			if (LibraryApplicationInterface._objects.TryGetValue(typeof(T).FullName, out obj))
			{
				return obj as T;
			}
			return default(T);
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00003038 File Offset: 0x00001238
		internal static void SetObjects(Dictionary<string, object> objects)
		{
			LibraryApplicationInterface._objects = objects;
			LibraryApplicationInterface.IManaged = LibraryApplicationInterface.GetObject<IManaged>();
			LibraryApplicationInterface.ITelemetry = LibraryApplicationInterface.GetObject<ITelemetry>();
			LibraryApplicationInterface.ILibrarySizeChecker = LibraryApplicationInterface.GetObject<ILibrarySizeChecker>();
			LibraryApplicationInterface.INativeArray = LibraryApplicationInterface.GetObject<INativeArray>();
			LibraryApplicationInterface.INativeObjectArray = LibraryApplicationInterface.GetObject<INativeObjectArray>();
			LibraryApplicationInterface.INativeStringHelper = LibraryApplicationInterface.GetObject<INativeStringHelper>();
			LibraryApplicationInterface.INativeString = LibraryApplicationInterface.GetObject<INativeString>();
		}

		// Token: 0x04000036 RID: 54
		internal static IManaged IManaged;

		// Token: 0x04000037 RID: 55
		internal static ITelemetry ITelemetry;

		// Token: 0x04000038 RID: 56
		internal static ILibrarySizeChecker ILibrarySizeChecker;

		// Token: 0x04000039 RID: 57
		internal static INativeArray INativeArray;

		// Token: 0x0400003A RID: 58
		internal static INativeObjectArray INativeObjectArray;

		// Token: 0x0400003B RID: 59
		internal static INativeStringHelper INativeStringHelper;

		// Token: 0x0400003C RID: 60
		internal static INativeString INativeString;

		// Token: 0x0400003D RID: 61
		private static Dictionary<string, object> _objects;
	}
}
