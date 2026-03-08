using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200005F RID: 95
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public class HasTableauCache : Attribute
	{
		// Token: 0x17000050 RID: 80
		// (get) Token: 0x06000955 RID: 2389 RVA: 0x00008B28 File Offset: 0x00006D28
		// (set) Token: 0x06000956 RID: 2390 RVA: 0x00008B30 File Offset: 0x00006D30
		public Type TableauCacheType { get; set; }

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x06000957 RID: 2391 RVA: 0x00008B39 File Offset: 0x00006D39
		// (set) Token: 0x06000958 RID: 2392 RVA: 0x00008B41 File Offset: 0x00006D41
		public Type MaterialCacheIDGetType { get; set; }

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000959 RID: 2393 RVA: 0x00008B4A File Offset: 0x00006D4A
		// (set) Token: 0x0600095A RID: 2394 RVA: 0x00008B51 File Offset: 0x00006D51
		internal static Dictionary<Type, MaterialCacheIDGetMethodDelegate> TableauCacheTypes { get; private set; }

		// Token: 0x0600095B RID: 2395 RVA: 0x00008B59 File Offset: 0x00006D59
		public HasTableauCache(Type tableauCacheType, Type materialCacheIDGetType)
		{
			this.TableauCacheType = tableauCacheType;
			this.MaterialCacheIDGetType = materialCacheIDGetType;
		}

		// Token: 0x0600095C RID: 2396 RVA: 0x00008B70 File Offset: 0x00006D70
		public static void CollectTableauCacheTypes()
		{
			HasTableauCache.TableauCacheTypes = new Dictionary<Type, MaterialCacheIDGetMethodDelegate>();
			HasTableauCache.CollectTableauCacheTypesFrom(typeof(HasTableauCache).Assembly);
			Assembly[] referencingAssembliesSafe = typeof(HasTableauCache).Assembly.GetReferencingAssembliesSafe(null);
			for (int i = 0; i < referencingAssembliesSafe.Length; i++)
			{
				HasTableauCache.CollectTableauCacheTypesFrom(referencingAssembliesSafe[i]);
			}
		}

		// Token: 0x0600095D RID: 2397 RVA: 0x00008BC8 File Offset: 0x00006DC8
		private static void CollectTableauCacheTypesFrom(Assembly assembly)
		{
			object[] customAttributesSafe = assembly.GetCustomAttributesSafe(typeof(HasTableauCache), true);
			if (customAttributesSafe.Length != 0)
			{
				foreach (HasTableauCache hasTableauCache in customAttributesSafe)
				{
					MethodInfo method = hasTableauCache.MaterialCacheIDGetType.GetMethod("GetMaterialCacheID", BindingFlags.Static | BindingFlags.Public);
					MaterialCacheIDGetMethodDelegate value = (MaterialCacheIDGetMethodDelegate)Delegate.CreateDelegate(typeof(MaterialCacheIDGetMethodDelegate), method);
					HasTableauCache.TableauCacheTypes.Add(hasTableauCache.TableauCacheType, value);
				}
			}
		}
	}
}
