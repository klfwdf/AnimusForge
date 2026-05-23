using System;

namespace System.Threading
{
	// Token: 0x0200053F RID: 1343
	internal static class LazyHelpers<T>
	{
		// Token: 0x06003EE7 RID: 16103 RVA: 0x000EA074 File Offset: 0x000E8274
		private static T ActivatorFactorySelector()
		{
			T result;
			try
			{
				result = (T)((object)Activator.CreateInstance(typeof(T)));
			}
			catch (MissingMethodException)
			{
				throw new MissingMemberException(Environment.GetResourceString("Lazy_CreateValue_NoParameterlessCtorForT"));
			}
			return result;
		}

		// Token: 0x04001A77 RID: 6775
		internal static Func<T> s_activatorFactorySelector = new Func<T>(LazyHelpers<T>.ActivatorFactorySelector);
	}
}
