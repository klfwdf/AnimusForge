using System;
using System.Security.Permissions;

namespace System.Threading
{
	// Token: 0x0200053E RID: 1342
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public static class LazyInitializer
	{
		// Token: 0x06003EE1 RID: 16097 RVA: 0x000E9F49 File Offset: 0x000E8149
		[__DynamicallyInvokable]
		public static T EnsureInitialized<T>(ref T target) where T : class
		{
			if (Volatile.Read<T>(ref target) != null)
			{
				return target;
			}
			return LazyInitializer.EnsureInitializedCore<T>(ref target, LazyHelpers<T>.s_activatorFactorySelector);
		}

		// Token: 0x06003EE2 RID: 16098 RVA: 0x000E9F6A File Offset: 0x000E816A
		[__DynamicallyInvokable]
		public static T EnsureInitialized<T>(ref T target, Func<T> valueFactory) where T : class
		{
			if (Volatile.Read<T>(ref target) != null)
			{
				return target;
			}
			return LazyInitializer.EnsureInitializedCore<T>(ref target, valueFactory);
		}

		// Token: 0x06003EE3 RID: 16099 RVA: 0x000E9F88 File Offset: 0x000E8188
		private static T EnsureInitializedCore<T>(ref T target, Func<T> valueFactory) where T : class
		{
			T t = valueFactory();
			if (t == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Lazy_StaticInit_InvalidOperation"));
			}
			Interlocked.CompareExchange<T>(ref target, t, default(T));
			return target;
		}

		// Token: 0x06003EE4 RID: 16100 RVA: 0x000E9FCB File Offset: 0x000E81CB
		[__DynamicallyInvokable]
		public static T EnsureInitialized<T>(ref T target, ref bool initialized, ref object syncLock)
		{
			if (Volatile.Read(ref initialized))
			{
				return target;
			}
			return LazyInitializer.EnsureInitializedCore<T>(ref target, ref initialized, ref syncLock, LazyHelpers<T>.s_activatorFactorySelector);
		}

		// Token: 0x06003EE5 RID: 16101 RVA: 0x000E9FE9 File Offset: 0x000E81E9
		[__DynamicallyInvokable]
		public static T EnsureInitialized<T>(ref T target, ref bool initialized, ref object syncLock, Func<T> valueFactory)
		{
			if (Volatile.Read(ref initialized))
			{
				return target;
			}
			return LazyInitializer.EnsureInitializedCore<T>(ref target, ref initialized, ref syncLock, valueFactory);
		}

		// Token: 0x06003EE6 RID: 16102 RVA: 0x000EA004 File Offset: 0x000E8204
		private static T EnsureInitializedCore<T>(ref T target, ref bool initialized, ref object syncLock, Func<T> valueFactory)
		{
			object obj = syncLock;
			if (obj == null)
			{
				object obj2 = new object();
				obj = Interlocked.CompareExchange(ref syncLock, obj2, null);
				if (obj == null)
				{
					obj = obj2;
				}
			}
			object obj3 = obj;
			lock (obj3)
			{
				if (!Volatile.Read(ref initialized))
				{
					target = valueFactory();
					Volatile.Write(ref initialized, true);
				}
			}
			return target;
		}
	}
}
