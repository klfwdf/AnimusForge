using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Contract details for a <see cref="T:System.Type" /> used by the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	// Token: 0x0200008C RID: 140
	[NullableContext(2)]
	[Nullable(0)]
	public class JsonDictionaryContract : JsonContainerContract
	{
		/// <summary>
		/// Gets or sets the dictionary key resolver.
		/// </summary>
		/// <value>The dictionary key resolver.</value>
		// Token: 0x17000105 RID: 261
		// (get) Token: 0x060006D5 RID: 1749 RVA: 0x0001CDA6 File Offset: 0x0001AFA6
		// (set) Token: 0x060006D6 RID: 1750 RVA: 0x0001CDAE File Offset: 0x0001AFAE
		[Nullable(new byte[] { 2, 1, 1 })]
		public Func<string, string> DictionaryKeyResolver
		{
			[return: Nullable(new byte[] { 2, 1, 1 })]
			get;
			[param: Nullable(new byte[] { 2, 1, 1 })]
			set;
		}

		/// <summary>
		/// Gets the <see cref="T:System.Type" /> of the dictionary keys.
		/// </summary>
		/// <value>The <see cref="T:System.Type" /> of the dictionary keys.</value>
		// Token: 0x17000106 RID: 262
		// (get) Token: 0x060006D7 RID: 1751 RVA: 0x0001CDB7 File Offset: 0x0001AFB7
		public Type DictionaryKeyType { get; }

		/// <summary>
		/// Gets the <see cref="T:System.Type" /> of the dictionary values.
		/// </summary>
		/// <value>The <see cref="T:System.Type" /> of the dictionary values.</value>
		// Token: 0x17000107 RID: 263
		// (get) Token: 0x060006D8 RID: 1752 RVA: 0x0001CDBF File Offset: 0x0001AFBF
		public Type DictionaryValueType { get; }

		// Token: 0x17000108 RID: 264
		// (get) Token: 0x060006D9 RID: 1753 RVA: 0x0001CDC7 File Offset: 0x0001AFC7
		// (set) Token: 0x060006DA RID: 1754 RVA: 0x0001CDCF File Offset: 0x0001AFCF
		internal JsonContract KeyContract { get; set; }

		// Token: 0x17000109 RID: 265
		// (get) Token: 0x060006DB RID: 1755 RVA: 0x0001CDD8 File Offset: 0x0001AFD8
		internal bool ShouldCreateWrapper { get; }

		// Token: 0x1700010A RID: 266
		// (get) Token: 0x060006DC RID: 1756 RVA: 0x0001CDE0 File Offset: 0x0001AFE0
		[Nullable(new byte[] { 2, 1 })]
		internal ObjectConstructor<object> ParameterizedCreator
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get
			{
				if (this._parameterizedCreator == null && this._parameterizedConstructor != null)
				{
					this._parameterizedCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(this._parameterizedConstructor);
				}
				return this._parameterizedCreator;
			}
		}

		/// <summary>
		/// Gets or sets the function used to create the object. When set this function will override <see cref="P:Newtonsoft.Json.Serialization.JsonContract.DefaultCreator" />.
		/// </summary>
		/// <value>The function used to create the object.</value>
		// Token: 0x1700010B RID: 267
		// (get) Token: 0x060006DD RID: 1757 RVA: 0x0001CE14 File Offset: 0x0001B014
		// (set) Token: 0x060006DE RID: 1758 RVA: 0x0001CE1C File Offset: 0x0001B01C
		[Nullable(new byte[] { 2, 1 })]
		public ObjectConstructor<object> OverrideCreator
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get
			{
				return this._overrideCreator;
			}
			[param: Nullable(new byte[] { 2, 1 })]
			set
			{
				this._overrideCreator = value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the creator has a parameter with the dictionary values.
		/// </summary>
		/// <value><c>true</c> if the creator has a parameter with the dictionary values; otherwise, <c>false</c>.</value>
		// Token: 0x1700010C RID: 268
		// (get) Token: 0x060006DF RID: 1759 RVA: 0x0001CE25 File Offset: 0x0001B025
		// (set) Token: 0x060006E0 RID: 1760 RVA: 0x0001CE2D File Offset: 0x0001B02D
		public bool HasParameterizedCreator { get; set; }

		// Token: 0x1700010D RID: 269
		// (get) Token: 0x060006E1 RID: 1761 RVA: 0x0001CE36 File Offset: 0x0001B036
		internal bool HasParameterizedCreatorInternal
		{
			get
			{
				return this.HasParameterizedCreator || this._parameterizedCreator != null || this._parameterizedConstructor != null;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.JsonDictionaryContract" /> class.
		/// </summary>
		/// <param name="underlyingType">The underlying type for the contract.</param>
		// Token: 0x060006E2 RID: 1762 RVA: 0x0001CE58 File Offset: 0x0001B058
		[NullableContext(1)]
		public JsonDictionaryContract(Type underlyingType)
			: base(underlyingType)
		{
			this.ContractType = JsonContractType.Dictionary;
			Type type;
			Type type2;
			if (ReflectionUtils.ImplementsGenericDefinition(this.NonNullableUnderlyingType, typeof(IDictionary<, >), out this._genericCollectionDefinitionType))
			{
				type = this._genericCollectionDefinitionType.GetGenericArguments()[0];
				type2 = this._genericCollectionDefinitionType.GetGenericArguments()[1];
				if (ReflectionUtils.IsGenericDefinition(this.NonNullableUnderlyingType, typeof(IDictionary<, >)))
				{
					base.CreatedType = typeof(Dictionary<, >).MakeGenericType(new Type[] { type, type2 });
				}
				else if (this.NonNullableUnderlyingType.IsGenericType() && this.NonNullableUnderlyingType.GetGenericTypeDefinition().FullName == "System.Collections.Concurrent.ConcurrentDictionary`2")
				{
					this.ShouldCreateWrapper = 1;
				}
				this.IsReadOnlyOrFixedSize = ReflectionUtils.InheritsGenericDefinition(this.NonNullableUnderlyingType, typeof(ReadOnlyDictionary<, >));
			}
			else if (ReflectionUtils.ImplementsGenericDefinition(this.NonNullableUnderlyingType, typeof(IReadOnlyDictionary<, >), out this._genericCollectionDefinitionType))
			{
				type = this._genericCollectionDefinitionType.GetGenericArguments()[0];
				type2 = this._genericCollectionDefinitionType.GetGenericArguments()[1];
				if (ReflectionUtils.IsGenericDefinition(this.NonNullableUnderlyingType, typeof(IReadOnlyDictionary<, >)))
				{
					base.CreatedType = typeof(ReadOnlyDictionary<, >).MakeGenericType(new Type[] { type, type2 });
				}
				this.IsReadOnlyOrFixedSize = true;
			}
			else
			{
				ReflectionUtils.GetDictionaryKeyValueTypes(this.NonNullableUnderlyingType, out type, out type2);
				if (this.NonNullableUnderlyingType == typeof(IDictionary))
				{
					base.CreatedType = typeof(Dictionary<object, object>);
				}
			}
			if (type != null && type2 != null)
			{
				this._parameterizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(base.CreatedType, typeof(KeyValuePair<, >).MakeGenericType(new Type[] { type, type2 }), typeof(IDictionary<, >).MakeGenericType(new Type[] { type, type2 }));
				if (!this.HasParameterizedCreatorInternal && this.NonNullableUnderlyingType.Name == "FSharpMap`2")
				{
					FSharpUtils.EnsureInitialized(this.NonNullableUnderlyingType.Assembly());
					this._parameterizedCreator = FSharpUtils.Instance.CreateMap(type, type2);
				}
			}
			if (!typeof(IDictionary).IsAssignableFrom(base.CreatedType))
			{
				this.ShouldCreateWrapper = 1;
			}
			this.DictionaryKeyType = type;
			this.DictionaryValueType = type2;
			Type createdType;
			ObjectConstructor<object> parameterizedCreator;
			if (this.DictionaryKeyType != null && this.DictionaryValueType != null && ImmutableCollectionsUtils.TryBuildImmutableForDictionaryContract(this.NonNullableUnderlyingType, this.DictionaryKeyType, this.DictionaryValueType, out createdType, out parameterizedCreator))
			{
				base.CreatedType = createdType;
				this._parameterizedCreator = parameterizedCreator;
				this.IsReadOnlyOrFixedSize = true;
			}
		}

		// Token: 0x060006E3 RID: 1763 RVA: 0x0001D10C File Offset: 0x0001B30C
		[NullableContext(1)]
		internal IWrappedDictionary CreateWrapper(object dictionary)
		{
			if (this._genericWrapperCreator == null)
			{
				this._genericWrapperType = typeof(DictionaryWrapper<, >).MakeGenericType(new Type[] { this.DictionaryKeyType, this.DictionaryValueType });
				ConstructorInfo constructor = this._genericWrapperType.GetConstructor(new Type[] { this._genericCollectionDefinitionType });
				this._genericWrapperCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(constructor);
			}
			return (IWrappedDictionary)this._genericWrapperCreator(new object[] { dictionary });
		}

		// Token: 0x060006E4 RID: 1764 RVA: 0x0001D194 File Offset: 0x0001B394
		[NullableContext(1)]
		internal IDictionary CreateTemporaryDictionary()
		{
			if (this._genericTemporaryDictionaryCreator == null)
			{
				Type type = typeof(Dictionary<, >).MakeGenericType(new Type[]
				{
					this.DictionaryKeyType ?? typeof(object),
					this.DictionaryValueType ?? typeof(object)
				});
				this._genericTemporaryDictionaryCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateDefaultConstructor<object>(type);
			}
			return (IDictionary)this._genericTemporaryDictionaryCreator();
		}

		// Token: 0x04000282 RID: 642
		private readonly Type _genericCollectionDefinitionType;

		// Token: 0x04000283 RID: 643
		private Type _genericWrapperType;

		// Token: 0x04000284 RID: 644
		[Nullable(new byte[] { 2, 1 })]
		private ObjectConstructor<object> _genericWrapperCreator;

		// Token: 0x04000285 RID: 645
		[Nullable(new byte[] { 2, 1 })]
		private Func<object> _genericTemporaryDictionaryCreator;

		// Token: 0x04000287 RID: 647
		private readonly ConstructorInfo _parameterizedConstructor;

		// Token: 0x04000288 RID: 648
		[Nullable(new byte[] { 2, 1 })]
		private ObjectConstructor<object> _overrideCreator;

		// Token: 0x04000289 RID: 649
		[Nullable(new byte[] { 2, 1 })]
		private ObjectConstructor<object> _parameterizedCreator;
	}
}
