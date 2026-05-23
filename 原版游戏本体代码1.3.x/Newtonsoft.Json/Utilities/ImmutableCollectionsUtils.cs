using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Serialization;

namespace Newtonsoft.Json.Utilities
{
	/// <summary>
	/// Helper class for serializing immutable collections.
	/// Note that this is used by all builds, even those that don't support immutable collections, in case the DLL is GACed
	/// https://github.com/JamesNK/Newtonsoft.Json/issues/652
	/// </summary>
	// Token: 0x0200005D RID: 93
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ImmutableCollectionsUtils
	{
		// Token: 0x0600054E RID: 1358 RVA: 0x00016A8C File Offset: 0x00014C8C
		internal static bool TryBuildImmutableForArrayContract(Type underlyingType, Type collectionItemType, [Nullable(2)] [NotNullWhen(true)] out Type createdType, [Nullable(new byte[] { 2, 1 })] [NotNullWhen(true)] out ObjectConstructor<object> parameterizedCreator)
		{
			if (underlyingType.IsGenericType())
			{
				Type genericTypeDefinition = underlyingType.GetGenericTypeDefinition();
				string name = genericTypeDefinition.FullName;
				ImmutableCollectionsUtils.ImmutableCollectionTypeInfo immutableCollectionTypeInfo = ImmutableCollectionsUtils.ArrayContractImmutableCollectionDefinitions.FirstOrDefault((ImmutableCollectionsUtils.ImmutableCollectionTypeInfo d) => d.ContractTypeName == name);
				if (immutableCollectionTypeInfo != null)
				{
					Type type = genericTypeDefinition.Assembly().GetType(immutableCollectionTypeInfo.CreatedTypeName);
					Type type2 = genericTypeDefinition.Assembly().GetType(immutableCollectionTypeInfo.BuilderTypeName);
					if (type != null && type2 != null)
					{
						MethodInfo methodInfo = type2.GetMethods().FirstOrDefault((MethodInfo m) => m.Name == "CreateRange" && m.GetParameters().Length == 1);
						if (methodInfo != null)
						{
							createdType = type.MakeGenericType(new Type[] { collectionItemType });
							MethodInfo method = methodInfo.MakeGenericMethod(new Type[] { collectionItemType });
							parameterizedCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(method);
							return true;
						}
					}
				}
			}
			createdType = null;
			parameterizedCreator = null;
			return false;
		}

		// Token: 0x0600054F RID: 1359 RVA: 0x00016B88 File Offset: 0x00014D88
		internal static bool TryBuildImmutableForDictionaryContract(Type underlyingType, Type keyItemType, Type valueItemType, [Nullable(2)] [NotNullWhen(true)] out Type createdType, [Nullable(new byte[] { 2, 1 })] [NotNullWhen(true)] out ObjectConstructor<object> parameterizedCreator)
		{
			if (underlyingType.IsGenericType())
			{
				Type genericTypeDefinition = underlyingType.GetGenericTypeDefinition();
				string name = genericTypeDefinition.FullName;
				ImmutableCollectionsUtils.ImmutableCollectionTypeInfo immutableCollectionTypeInfo = ImmutableCollectionsUtils.DictionaryContractImmutableCollectionDefinitions.FirstOrDefault((ImmutableCollectionsUtils.ImmutableCollectionTypeInfo d) => d.ContractTypeName == name);
				if (immutableCollectionTypeInfo != null)
				{
					Type type = genericTypeDefinition.Assembly().GetType(immutableCollectionTypeInfo.CreatedTypeName);
					Type type2 = genericTypeDefinition.Assembly().GetType(immutableCollectionTypeInfo.BuilderTypeName);
					if (type != null && type2 != null)
					{
						MethodInfo methodInfo = type2.GetMethods().FirstOrDefault(delegate(MethodInfo m)
						{
							ParameterInfo[] parameters = m.GetParameters();
							return m.Name == "CreateRange" && parameters.Length == 1 && parameters[0].ParameterType.IsGenericType() && parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
						});
						if (methodInfo != null)
						{
							createdType = type.MakeGenericType(new Type[] { keyItemType, valueItemType });
							MethodInfo method = methodInfo.MakeGenericMethod(new Type[] { keyItemType, valueItemType });
							parameterizedCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(method);
							return true;
						}
					}
				}
			}
			createdType = null;
			parameterizedCreator = null;
			return false;
		}

		// Token: 0x040001F9 RID: 505
		private const string ImmutableListGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableList`1";

		// Token: 0x040001FA RID: 506
		private const string ImmutableQueueGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableQueue`1";

		// Token: 0x040001FB RID: 507
		private const string ImmutableStackGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableStack`1";

		// Token: 0x040001FC RID: 508
		private const string ImmutableSetGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableSet`1";

		// Token: 0x040001FD RID: 509
		private const string ImmutableArrayTypeName = "System.Collections.Immutable.ImmutableArray";

		// Token: 0x040001FE RID: 510
		private const string ImmutableArrayGenericTypeName = "System.Collections.Immutable.ImmutableArray`1";

		// Token: 0x040001FF RID: 511
		private const string ImmutableListTypeName = "System.Collections.Immutable.ImmutableList";

		// Token: 0x04000200 RID: 512
		private const string ImmutableListGenericTypeName = "System.Collections.Immutable.ImmutableList`1";

		// Token: 0x04000201 RID: 513
		private const string ImmutableQueueTypeName = "System.Collections.Immutable.ImmutableQueue";

		// Token: 0x04000202 RID: 514
		private const string ImmutableQueueGenericTypeName = "System.Collections.Immutable.ImmutableQueue`1";

		// Token: 0x04000203 RID: 515
		private const string ImmutableStackTypeName = "System.Collections.Immutable.ImmutableStack";

		// Token: 0x04000204 RID: 516
		private const string ImmutableStackGenericTypeName = "System.Collections.Immutable.ImmutableStack`1";

		// Token: 0x04000205 RID: 517
		private const string ImmutableSortedSetTypeName = "System.Collections.Immutable.ImmutableSortedSet";

		// Token: 0x04000206 RID: 518
		private const string ImmutableSortedSetGenericTypeName = "System.Collections.Immutable.ImmutableSortedSet`1";

		// Token: 0x04000207 RID: 519
		private const string ImmutableHashSetTypeName = "System.Collections.Immutable.ImmutableHashSet";

		// Token: 0x04000208 RID: 520
		private const string ImmutableHashSetGenericTypeName = "System.Collections.Immutable.ImmutableHashSet`1";

		// Token: 0x04000209 RID: 521
		private static readonly IList<ImmutableCollectionsUtils.ImmutableCollectionTypeInfo> ArrayContractImmutableCollectionDefinitions = new List<ImmutableCollectionsUtils.ImmutableCollectionTypeInfo>
		{
			new ImmutableCollectionsUtils.ImmutableCollectionTypeInfo("System.Collections.Immutable.IImmutableList`1", "System.Collections.Immutable.ImmutableList`1", "System.Collections.Immutable.ImmutableList"),
			new ImmutableCollectionsUtils.ImmutableCollectionTypeInfo("System.Collections.Immutable.ImmutableList`1", "System.Collections.Immutable.ImmutableList`1", "System.Collections.Immutable.ImmutableList"),
			new ImmutableCollectionsUtils.ImmutableCollectionTypeInfo("System.Collections.Immutable.IImmutableQueue`1", "System.Collections.Immutable.ImmutableQueue`1", "System.Collections.Immutable.ImmutableQueue"),
			new ImmutableCollectionsUtils.ImmutableCollectionTypeInfo("System.Collections.Immutable.ImmutableQueue`1", "System.Collections.Immutable.ImmutableQueue`1", "System.Collections.Immutable.ImmutableQueue"),
			new ImmutableCollectionsUtils.ImmutableCollectionTypeInfo("System.Collections.Immutable.IImmutableStack`1", "System.Collections.Immutable.ImmutableStack`1", "System.Collections.Immutable.ImmutableStack"),
			new ImmutableCollectionsUtils.ImmutableCollectionTypeInfo("System.Collections.Immutable.ImmutableStack`1", "System.Collections.Immutable.ImmutableStack`1", "System.Collections.Immutable.ImmutableStack"),
			new ImmutableCollectionsUtils.ImmutableCollectionTypeInfo("System.Collections.Immutable.IImmutableSet`1", "System.Collections.Immutable.ImmutableHashSet`1", "System.Collections.Immutable.ImmutableHashSet"),
			new ImmutableCollectionsUtils.ImmutableCollectionTypeInfo("System.Collections.Immutable.ImmutableSortedSet`1", "System.Collections.Immutable.ImmutableSortedSet`1", "System.Collections.Immutable.ImmutableSortedSet"),
			new ImmutableCollectionsUtils.ImmutableCollectionTypeInfo("System.Collections.Immutable.ImmutableHashSet`1", "System.Collections.Immutable.ImmutableHashSet`1", "System.Collections.Immutable.ImmutableHashSet"),
			new ImmutableCollectionsUtils.ImmutableCollectionTypeInfo("System.Collections.Immutable.ImmutableArray`1", "System.Collections.Immutable.ImmutableArray`1", "System.Collections.Immutable.ImmutableArray")
		};

		// Token: 0x0400020A RID: 522
		private const string ImmutableDictionaryGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableDictionary`2";

		// Token: 0x0400020B RID: 523
		private const string ImmutableDictionaryTypeName = "System.Collections.Immutable.ImmutableDictionary";

		// Token: 0x0400020C RID: 524
		private const string ImmutableDictionaryGenericTypeName = "System.Collections.Immutable.ImmutableDictionary`2";

		// Token: 0x0400020D RID: 525
		private const string ImmutableSortedDictionaryTypeName = "System.Collections.Immutable.ImmutableSortedDictionary";

		// Token: 0x0400020E RID: 526
		private const string ImmutableSortedDictionaryGenericTypeName = "System.Collections.Immutable.ImmutableSortedDictionary`2";

		// Token: 0x0400020F RID: 527
		private static readonly IList<ImmutableCollectionsUtils.ImmutableCollectionTypeInfo> DictionaryContractImmutableCollectionDefinitions = new List<ImmutableCollectionsUtils.ImmutableCollectionTypeInfo>
		{
			new ImmutableCollectionsUtils.ImmutableCollectionTypeInfo("System.Collections.Immutable.IImmutableDictionary`2", "System.Collections.Immutable.ImmutableDictionary`2", "System.Collections.Immutable.ImmutableDictionary"),
			new ImmutableCollectionsUtils.ImmutableCollectionTypeInfo("System.Collections.Immutable.ImmutableSortedDictionary`2", "System.Collections.Immutable.ImmutableSortedDictionary`2", "System.Collections.Immutable.ImmutableSortedDictionary"),
			new ImmutableCollectionsUtils.ImmutableCollectionTypeInfo("System.Collections.Immutable.ImmutableDictionary`2", "System.Collections.Immutable.ImmutableDictionary`2", "System.Collections.Immutable.ImmutableDictionary")
		};

		// Token: 0x02000184 RID: 388
		[Nullable(0)]
		internal class ImmutableCollectionTypeInfo
		{
			// Token: 0x06000EEA RID: 3818 RVA: 0x00042517 File Offset: 0x00040717
			public ImmutableCollectionTypeInfo(string contractTypeName, string createdTypeName, string builderTypeName)
			{
				this.ContractTypeName = contractTypeName;
				this.CreatedTypeName = createdTypeName;
				this.BuilderTypeName = builderTypeName;
			}

			// Token: 0x17000294 RID: 660
			// (get) Token: 0x06000EEB RID: 3819 RVA: 0x00042534 File Offset: 0x00040734
			// (set) Token: 0x06000EEC RID: 3820 RVA: 0x0004253C File Offset: 0x0004073C
			public string ContractTypeName { get; set; }

			// Token: 0x17000295 RID: 661
			// (get) Token: 0x06000EED RID: 3821 RVA: 0x00042545 File Offset: 0x00040745
			// (set) Token: 0x06000EEE RID: 3822 RVA: 0x0004254D File Offset: 0x0004074D
			public string CreatedTypeName { get; set; }

			// Token: 0x17000296 RID: 662
			// (get) Token: 0x06000EEF RID: 3823 RVA: 0x00042556 File Offset: 0x00040756
			// (set) Token: 0x06000EF0 RID: 3824 RVA: 0x0004255E File Offset: 0x0004075E
			public string BuilderTypeName { get; set; }
		}
	}
}
