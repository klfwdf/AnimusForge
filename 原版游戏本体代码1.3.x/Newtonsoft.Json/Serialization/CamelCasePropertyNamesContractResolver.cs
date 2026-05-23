using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Resolves member mappings for a type, camel casing property names.
	/// </summary>
	// Token: 0x02000074 RID: 116
	[NullableContext(1)]
	[Nullable(0)]
	public class CamelCasePropertyNamesContractResolver : DefaultContractResolver
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver" /> class.
		/// </summary>
		// Token: 0x06000617 RID: 1559 RVA: 0x00019BA4 File Offset: 0x00017DA4
		public CamelCasePropertyNamesContractResolver()
		{
			base.NamingStrategy = new CamelCaseNamingStrategy
			{
				ProcessDictionaryKeys = true,
				OverrideSpecifiedNames = true
			};
		}

		/// <summary>
		/// Resolves the contract for a given type.
		/// </summary>
		/// <param name="type">The type to resolve a contract for.</param>
		/// <returns>The contract for a given type.</returns>
		// Token: 0x06000618 RID: 1560 RVA: 0x00019BC8 File Offset: 0x00017DC8
		public override JsonContract ResolveContract(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			StructMultiKey<Type, Type> key = new StructMultiKey<Type, Type>(base.GetType(), type);
			Dictionary<StructMultiKey<Type, Type>, JsonContract> contractCache = CamelCasePropertyNamesContractResolver._contractCache;
			JsonContract jsonContract;
			if (contractCache == null || !contractCache.TryGetValue(key, out jsonContract))
			{
				jsonContract = this.CreateContract(type);
				object typeContractCacheLock = CamelCasePropertyNamesContractResolver.TypeContractCacheLock;
				lock (typeContractCacheLock)
				{
					contractCache = CamelCasePropertyNamesContractResolver._contractCache;
					Dictionary<StructMultiKey<Type, Type>, JsonContract> dictionary = ((contractCache != null) ? new Dictionary<StructMultiKey<Type, Type>, JsonContract>(contractCache) : new Dictionary<StructMultiKey<Type, Type>, JsonContract>());
					dictionary[key] = jsonContract;
					CamelCasePropertyNamesContractResolver._contractCache = dictionary;
				}
			}
			return jsonContract;
		}

		// Token: 0x06000619 RID: 1561 RVA: 0x00019C68 File Offset: 0x00017E68
		internal override DefaultJsonNameTable GetNameTable()
		{
			return CamelCasePropertyNamesContractResolver.NameTable;
		}

		// Token: 0x0400022B RID: 555
		private static readonly object TypeContractCacheLock = new object();

		// Token: 0x0400022C RID: 556
		private static readonly DefaultJsonNameTable NameTable = new DefaultJsonNameTable();

		// Token: 0x0400022D RID: 557
		[Nullable(new byte[] { 2, 0, 1, 1, 1 })]
		private static Dictionary<StructMultiKey<Type, Type>, JsonContract> _contractCache;
	}
}
