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
	// Token: 0x02000084 RID: 132
	[NullableContext(2)]
	[Nullable(0)]
	public class JsonArrayContract : JsonContainerContract
	{
		/// <summary>
		/// Gets the <see cref="T:System.Type" /> of the collection items.
		/// </summary>
		/// <value>The <see cref="T:System.Type" /> of the collection items.</value>
		// Token: 0x170000EA RID: 234
		// (get) Token: 0x0600068F RID: 1679 RVA: 0x0001C1F4 File Offset: 0x0001A3F4
		public Type CollectionItemType { get; }

		/// <summary>
		/// Gets a value indicating whether the collection type is a multidimensional array.
		/// </summary>
		/// <value><c>true</c> if the collection type is a multidimensional array; otherwise, <c>false</c>.</value>
		// Token: 0x170000EB RID: 235
		// (get) Token: 0x06000690 RID: 1680 RVA: 0x0001C1FC File Offset: 0x0001A3FC
		public bool IsMultidimensionalArray { get; }

		// Token: 0x170000EC RID: 236
		// (get) Token: 0x06000691 RID: 1681 RVA: 0x0001C204 File Offset: 0x0001A404
		internal bool IsArray { get; }

		// Token: 0x170000ED RID: 237
		// (get) Token: 0x06000692 RID: 1682 RVA: 0x0001C20C File Offset: 0x0001A40C
		internal bool ShouldCreateWrapper { get; }

		// Token: 0x170000EE RID: 238
		// (get) Token: 0x06000693 RID: 1683 RVA: 0x0001C214 File Offset: 0x0001A414
		// (set) Token: 0x06000694 RID: 1684 RVA: 0x0001C21C File Offset: 0x0001A41C
		internal bool CanDeserialize { get; private set; }

		// Token: 0x170000EF RID: 239
		// (get) Token: 0x06000695 RID: 1685 RVA: 0x0001C225 File Offset: 0x0001A425
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
		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x06000696 RID: 1686 RVA: 0x0001C259 File Offset: 0x0001A459
		// (set) Token: 0x06000697 RID: 1687 RVA: 0x0001C261 File Offset: 0x0001A461
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
				this.CanDeserialize = true;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the creator has a parameter with the collection values.
		/// </summary>
		/// <value><c>true</c> if the creator has a parameter with the collection values; otherwise, <c>false</c>.</value>
		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x06000698 RID: 1688 RVA: 0x0001C271 File Offset: 0x0001A471
		// (set) Token: 0x06000699 RID: 1689 RVA: 0x0001C279 File Offset: 0x0001A479
		public bool HasParameterizedCreator { get; set; }

		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x0600069A RID: 1690 RVA: 0x0001C282 File Offset: 0x0001A482
		internal bool HasParameterizedCreatorInternal
		{
			get
			{
				return this.HasParameterizedCreator || this._parameterizedCreator != null || this._parameterizedConstructor != null;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.JsonArrayContract" /> class.
		/// </summary>
		/// <param name="underlyingType">The underlying type for the contract.</param>
		// Token: 0x0600069B RID: 1691 RVA: 0x0001C2A4 File Offset: 0x0001A4A4
		[NullableContext(1)]
		public JsonArrayContract(Type underlyingType)
			: base(underlyingType)
		{
			this.ContractType = JsonContractType.Array;
			this.IsArray = base.CreatedType.IsArray || (this.NonNullableUnderlyingType.IsGenericType() && this.NonNullableUnderlyingType.GetGenericTypeDefinition().FullName == "System.Linq.EmptyPartition`1");
			bool canDeserialize;
			Type type;
			if (this.IsArray)
			{
				this.CollectionItemType = ReflectionUtils.GetCollectionItemType(base.UnderlyingType);
				this.IsReadOnlyOrFixedSize = true;
				this._genericCollectionDefinitionType = typeof(List<>).MakeGenericType(new Type[] { this.CollectionItemType });
				canDeserialize = true;
				this.IsMultidimensionalArray = base.CreatedType.IsArray && base.UnderlyingType.GetArrayRank() > 1;
			}
			else if (typeof(IList).IsAssignableFrom(this.NonNullableUnderlyingType))
			{
				if (ReflectionUtils.ImplementsGenericDefinition(this.NonNullableUnderlyingType, typeof(ICollection<>), out this._genericCollectionDefinitionType))
				{
					this.CollectionItemType = this._genericCollectionDefinitionType.GetGenericArguments()[0];
				}
				else
				{
					this.CollectionItemType = ReflectionUtils.GetCollectionItemType(this.NonNullableUnderlyingType);
				}
				if (this.NonNullableUnderlyingType == typeof(IList))
				{
					base.CreatedType = typeof(List<object>);
				}
				if (this.CollectionItemType != null)
				{
					this._parameterizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(this.NonNullableUnderlyingType, this.CollectionItemType);
				}
				this.IsReadOnlyOrFixedSize = ReflectionUtils.InheritsGenericDefinition(this.NonNullableUnderlyingType, typeof(ReadOnlyCollection<>));
				canDeserialize = true;
			}
			else if (ReflectionUtils.ImplementsGenericDefinition(this.NonNullableUnderlyingType, typeof(ICollection<>), out this._genericCollectionDefinitionType))
			{
				this.CollectionItemType = this._genericCollectionDefinitionType.GetGenericArguments()[0];
				if (ReflectionUtils.IsGenericDefinition(this.NonNullableUnderlyingType, typeof(ICollection<>)) || ReflectionUtils.IsGenericDefinition(this.NonNullableUnderlyingType, typeof(IList<>)))
				{
					base.CreatedType = typeof(List<>).MakeGenericType(new Type[] { this.CollectionItemType });
				}
				if (ReflectionUtils.IsGenericDefinition(this.NonNullableUnderlyingType, typeof(ISet<>)))
				{
					base.CreatedType = typeof(HashSet<>).MakeGenericType(new Type[] { this.CollectionItemType });
				}
				this._parameterizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(this.NonNullableUnderlyingType, this.CollectionItemType);
				canDeserialize = true;
				this.ShouldCreateWrapper = 1;
			}
			else if (ReflectionUtils.ImplementsGenericDefinition(this.NonNullableUnderlyingType, typeof(IReadOnlyCollection<>), out type))
			{
				this.CollectionItemType = type.GetGenericArguments()[0];
				if (ReflectionUtils.IsGenericDefinition(this.NonNullableUnderlyingType, typeof(IReadOnlyCollection<>)) || ReflectionUtils.IsGenericDefinition(this.NonNullableUnderlyingType, typeof(IReadOnlyList<>)))
				{
					base.CreatedType = typeof(ReadOnlyCollection<>).MakeGenericType(new Type[] { this.CollectionItemType });
				}
				this._genericCollectionDefinitionType = typeof(List<>).MakeGenericType(new Type[] { this.CollectionItemType });
				this._parameterizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(base.CreatedType, this.CollectionItemType);
				this.StoreFSharpListCreatorIfNecessary(this.NonNullableUnderlyingType);
				this.IsReadOnlyOrFixedSize = true;
				canDeserialize = this.HasParameterizedCreatorInternal;
			}
			else if (ReflectionUtils.ImplementsGenericDefinition(this.NonNullableUnderlyingType, typeof(IEnumerable<>), out type))
			{
				this.CollectionItemType = type.GetGenericArguments()[0];
				if (ReflectionUtils.IsGenericDefinition(base.UnderlyingType, typeof(IEnumerable<>)))
				{
					base.CreatedType = typeof(List<>).MakeGenericType(new Type[] { this.CollectionItemType });
				}
				this._parameterizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(this.NonNullableUnderlyingType, this.CollectionItemType);
				this.StoreFSharpListCreatorIfNecessary(this.NonNullableUnderlyingType);
				if (this.NonNullableUnderlyingType.IsGenericType() && this.NonNullableUnderlyingType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				{
					this._genericCollectionDefinitionType = type;
					this.IsReadOnlyOrFixedSize = false;
					this.ShouldCreateWrapper = 0;
					canDeserialize = true;
				}
				else
				{
					this._genericCollectionDefinitionType = typeof(List<>).MakeGenericType(new Type[] { this.CollectionItemType });
					this.IsReadOnlyOrFixedSize = true;
					this.ShouldCreateWrapper = 1;
					canDeserialize = this.HasParameterizedCreatorInternal;
				}
			}
			else
			{
				canDeserialize = false;
				this.ShouldCreateWrapper = 1;
			}
			this.CanDeserialize = canDeserialize;
			Type createdType;
			ObjectConstructor<object> parameterizedCreator;
			if (this.CollectionItemType != null && ImmutableCollectionsUtils.TryBuildImmutableForArrayContract(this.NonNullableUnderlyingType, this.CollectionItemType, out createdType, out parameterizedCreator))
			{
				base.CreatedType = createdType;
				this._parameterizedCreator = parameterizedCreator;
				this.IsReadOnlyOrFixedSize = true;
				this.CanDeserialize = true;
			}
		}

		// Token: 0x0600069C RID: 1692 RVA: 0x0001C74C File Offset: 0x0001A94C
		[NullableContext(1)]
		internal IWrappedCollection CreateWrapper(object list)
		{
			if (this._genericWrapperCreator == null)
			{
				this._genericWrapperType = typeof(CollectionWrapper<>).MakeGenericType(new Type[] { this.CollectionItemType });
				Type type;
				if (ReflectionUtils.InheritsGenericDefinition(this._genericCollectionDefinitionType, typeof(List<>)) || this._genericCollectionDefinitionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				{
					type = typeof(ICollection<>).MakeGenericType(new Type[] { this.CollectionItemType });
				}
				else
				{
					type = this._genericCollectionDefinitionType;
				}
				ConstructorInfo constructor = this._genericWrapperType.GetConstructor(new Type[] { type });
				this._genericWrapperCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(constructor);
			}
			return (IWrappedCollection)this._genericWrapperCreator(new object[] { list });
		}

		// Token: 0x0600069D RID: 1693 RVA: 0x0001C824 File Offset: 0x0001AA24
		[NullableContext(1)]
		internal IList CreateTemporaryCollection()
		{
			if (this._genericTemporaryCollectionCreator == null)
			{
				Type type = ((this.IsMultidimensionalArray || this.CollectionItemType == null) ? typeof(object) : this.CollectionItemType);
				Type type2 = typeof(List<>).MakeGenericType(new Type[] { type });
				this._genericTemporaryCollectionCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateDefaultConstructor<object>(type2);
			}
			return (IList)this._genericTemporaryCollectionCreator();
		}

		// Token: 0x0600069E RID: 1694 RVA: 0x0001C89D File Offset: 0x0001AA9D
		[NullableContext(1)]
		private void StoreFSharpListCreatorIfNecessary(Type underlyingType)
		{
			if (!this.HasParameterizedCreatorInternal && underlyingType.Name == "FSharpList`1")
			{
				FSharpUtils.EnsureInitialized(underlyingType.Assembly());
				this._parameterizedCreator = FSharpUtils.Instance.CreateSeq(this.CollectionItemType);
			}
		}

		// Token: 0x0400024E RID: 590
		private readonly Type _genericCollectionDefinitionType;

		// Token: 0x0400024F RID: 591
		private Type _genericWrapperType;

		// Token: 0x04000250 RID: 592
		[Nullable(new byte[] { 2, 1 })]
		private ObjectConstructor<object> _genericWrapperCreator;

		// Token: 0x04000251 RID: 593
		[Nullable(new byte[] { 2, 1 })]
		private Func<object> _genericTemporaryCollectionCreator;

		// Token: 0x04000255 RID: 597
		private readonly ConstructorInfo _parameterizedConstructor;

		// Token: 0x04000256 RID: 598
		[Nullable(new byte[] { 2, 1 })]
		private ObjectConstructor<object> _parameterizedCreator;

		// Token: 0x04000257 RID: 599
		[Nullable(new byte[] { 2, 1 })]
		private ObjectConstructor<object> _overrideCreator;
	}
}
