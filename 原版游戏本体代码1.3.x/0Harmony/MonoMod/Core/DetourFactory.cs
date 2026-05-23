using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.Core.Platforms;
using MonoMod.Utils;

namespace MonoMod.Core
{
	// Token: 0x020004E1 RID: 1249
	[NullableContext(1)]
	[Nullable(0)]
	[CLSCompliant(true)]
	internal static class DetourFactory
	{
		// Token: 0x170005FF RID: 1535
		// (get) Token: 0x06001BC6 RID: 7110 RVA: 0x00058DA3 File Offset: 0x00056FA3
		public static IDetourFactory Default
		{
			get
			{
				return Helpers.GetOrInitWithLock<IDetourFactory>(ref DetourFactory.lazyDefault, DetourFactory.currentLock, ldftn(CreateDefault));
			}
		}

		// Token: 0x06001BC7 RID: 7111 RVA: 0x00058DBA File Offset: 0x00056FBA
		private static IDetourFactory CreateDefault()
		{
			return new PlatformTripleDetourFactory(PlatformTriple.Current);
		}

		// Token: 0x17000600 RID: 1536
		// (get) Token: 0x06001BC8 RID: 7112 RVA: 0x00058DC6 File Offset: 0x00056FC6
		public static IDetourFactory Current
		{
			get
			{
				return Helpers.GetOrInitWithLock<IDetourFactory>(ref DetourFactory.lazyCurrent, DetourFactory.currentLock, ldftn(CreateCurrent));
			}
		}

		// Token: 0x06001BC9 RID: 7113 RVA: 0x00058DDD File Offset: 0x00056FDD
		private static IDetourFactory CreateCurrent()
		{
			return DetourFactory.Default;
		}

		// Token: 0x06001BCA RID: 7114 RVA: 0x00058DE4 File Offset: 0x00056FE4
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void SetCurrentFactory(Func<IDetourFactory, IDetourFactory> creator)
		{
			Helpers.ThrowIfArgumentNull<Func<IDetourFactory, IDetourFactory>>(creator, "creator");
			object obj = DetourFactory.currentLock;
			lock (obj)
			{
				DetourFactory.lazyCurrent = creator(DetourFactory.Current);
			}
		}

		// Token: 0x06001BCB RID: 7115 RVA: 0x00058E38 File Offset: 0x00057038
		public static ICoreDetour CreateDetour(this IDetourFactory factory, MethodBase source, MethodBase target, bool applyByDefault = true)
		{
			Helpers.ThrowIfArgumentNull<IDetourFactory>(factory, "factory");
			return factory.CreateDetour(new CreateDetourRequest(source, target)
			{
				ApplyByDefault = applyByDefault
			});
		}

		// Token: 0x06001BCC RID: 7116 RVA: 0x00058E68 File Offset: 0x00057068
		public static ICoreNativeDetour CreateNativeDetour(this IDetourFactory factory, IntPtr source, IntPtr target, bool applyByDefault = true)
		{
			Helpers.ThrowIfArgumentNull<IDetourFactory>(factory, "factory");
			return factory.CreateNativeDetour(new CreateNativeDetourRequest(source, target)
			{
				ApplyByDefault = applyByDefault
			});
		}

		// Token: 0x0400115B RID: 4443
		private static object currentLock = new object();

		// Token: 0x0400115C RID: 4444
		[Nullable(2)]
		private static IDetourFactory lazyDefault;

		// Token: 0x0400115D RID: 4445
		[Nullable(2)]
		private static IDetourFactory lazyCurrent;
	}
}
