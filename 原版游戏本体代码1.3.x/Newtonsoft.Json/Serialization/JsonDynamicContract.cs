using System;
using System.Dynamic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Contract details for a <see cref="T:System.Type" /> used by the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	// Token: 0x0200008D RID: 141
	[NullableContext(1)]
	[Nullable(0)]
	public class JsonDynamicContract : JsonContainerContract
	{
		/// <summary>
		/// Gets the object's properties.
		/// </summary>
		/// <value>The object's properties.</value>
		// Token: 0x1700010E RID: 270
		// (get) Token: 0x060006E5 RID: 1765 RVA: 0x0001D20E File Offset: 0x0001B40E
		public JsonPropertyCollection Properties { get; }

		/// <summary>
		/// Gets or sets the property name resolver.
		/// </summary>
		/// <value>The property name resolver.</value>
		// Token: 0x1700010F RID: 271
		// (get) Token: 0x060006E6 RID: 1766 RVA: 0x0001D216 File Offset: 0x0001B416
		// (set) Token: 0x060006E7 RID: 1767 RVA: 0x0001D21E File Offset: 0x0001B41E
		[Nullable(new byte[] { 2, 1, 1 })]
		public Func<string, string> PropertyNameResolver
		{
			[return: Nullable(new byte[] { 2, 1, 1 })]
			get;
			[param: Nullable(new byte[] { 2, 1, 1 })]
			set;
		}

		// Token: 0x060006E8 RID: 1768 RVA: 0x0001D227 File Offset: 0x0001B427
		private static CallSite<Func<CallSite, object, object>> CreateCallSiteGetter(string name)
		{
			return CallSite<Func<CallSite, object, object>>.Create(new NoThrowGetBinderMember((GetMemberBinder)DynamicUtils.BinderWrapper.GetMember(name, typeof(DynamicUtils))));
		}

		// Token: 0x060006E9 RID: 1769 RVA: 0x0001D248 File Offset: 0x0001B448
		[return: Nullable(new byte[] { 1, 1, 1, 1, 2, 1 })]
		private static CallSite<Func<CallSite, object, object, object>> CreateCallSiteSetter(string name)
		{
			return CallSite<Func<CallSite, object, object, object>>.Create(new NoThrowSetBinderMember((SetMemberBinder)DynamicUtils.BinderWrapper.SetMember(name, typeof(DynamicUtils))));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.JsonDynamicContract" /> class.
		/// </summary>
		/// <param name="underlyingType">The underlying type for the contract.</param>
		// Token: 0x060006EA RID: 1770 RVA: 0x0001D26C File Offset: 0x0001B46C
		public JsonDynamicContract(Type underlyingType)
			: base(underlyingType)
		{
			this.ContractType = JsonContractType.Dynamic;
			this.Properties = new JsonPropertyCollection(base.UnderlyingType);
		}

		// Token: 0x060006EB RID: 1771 RVA: 0x0001D2C8 File Offset: 0x0001B4C8
		internal bool TryGetMember(IDynamicMetaObjectProvider dynamicProvider, string name, [Nullable(2)] out object value)
		{
			ValidationUtils.ArgumentNotNull(dynamicProvider, "dynamicProvider");
			CallSite<Func<CallSite, object, object>> callSite = this._callSiteGetters.Get(name);
			object obj = callSite.Target(callSite, dynamicProvider);
			if (obj != NoThrowExpressionVisitor.ErrorResult)
			{
				value = obj;
				return true;
			}
			value = null;
			return false;
		}

		// Token: 0x060006EC RID: 1772 RVA: 0x0001D30C File Offset: 0x0001B50C
		internal bool TrySetMember(IDynamicMetaObjectProvider dynamicProvider, string name, [Nullable(2)] object value)
		{
			ValidationUtils.ArgumentNotNull(dynamicProvider, "dynamicProvider");
			CallSite<Func<CallSite, object, object, object>> callSite = this._callSiteSetters.Get(name);
			return callSite.Target(callSite, dynamicProvider, value) != NoThrowExpressionVisitor.ErrorResult;
		}

		// Token: 0x0400028D RID: 653
		private readonly ThreadSafeStore<string, CallSite<Func<CallSite, object, object>>> _callSiteGetters = new ThreadSafeStore<string, CallSite<Func<CallSite, object, object>>>(new Func<string, CallSite<Func<CallSite, object, object>>>(JsonDynamicContract.CreateCallSiteGetter));

		// Token: 0x0400028E RID: 654
		[Nullable(new byte[] { 1, 1, 1, 1, 1, 1, 2, 1 })]
		private readonly ThreadSafeStore<string, CallSite<Func<CallSite, object, object, object>>> _callSiteSetters = new ThreadSafeStore<string, CallSite<Func<CallSite, object, object, object>>>(new Func<string, CallSite<Func<CallSite, object, object, object>>>(JsonDynamicContract.CreateCallSiteSetter));
	}
}
