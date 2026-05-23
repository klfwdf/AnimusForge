using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Metadata
{
	// Token: 0x020007D4 RID: 2004
	internal class RemotingMethodCachedData : RemotingCachedData
	{
		// Token: 0x060056DA RID: 22234 RVA: 0x001346D3 File Offset: 0x001328D3
		internal RemotingMethodCachedData(RuntimeMethodInfo ri)
		{
			this.RI = ri;
		}

		// Token: 0x060056DB RID: 22235 RVA: 0x001346E2 File Offset: 0x001328E2
		internal RemotingMethodCachedData(RuntimeConstructorInfo ri)
		{
			this.RI = ri;
		}

		// Token: 0x060056DC RID: 22236 RVA: 0x001346F4 File Offset: 0x001328F4
		internal override SoapAttribute GetSoapAttributeNoLock()
		{
			object[] customAttributes = this.RI.GetCustomAttributes(typeof(SoapMethodAttribute), true);
			SoapAttribute soapAttribute;
			if (customAttributes != null && customAttributes.Length != 0)
			{
				soapAttribute = (SoapAttribute)customAttributes[0];
			}
			else
			{
				soapAttribute = new SoapMethodAttribute();
			}
			soapAttribute.SetReflectInfo(this.RI);
			return soapAttribute;
		}

		// Token: 0x17000E43 RID: 3651
		// (get) Token: 0x060056DD RID: 22237 RVA: 0x0013473F File Offset: 0x0013293F
		internal string TypeAndAssemblyName
		{
			[SecurityCritical]
			get
			{
				if (this._typeAndAssemblyName == null)
				{
					this.UpdateNames();
				}
				return this._typeAndAssemblyName;
			}
		}

		// Token: 0x17000E44 RID: 3652
		// (get) Token: 0x060056DE RID: 22238 RVA: 0x00134755 File Offset: 0x00132955
		internal string MethodName
		{
			[SecurityCritical]
			get
			{
				if (this._methodName == null)
				{
					this.UpdateNames();
				}
				return this._methodName;
			}
		}

		// Token: 0x060056DF RID: 22239 RVA: 0x0013476C File Offset: 0x0013296C
		[SecurityCritical]
		private void UpdateNames()
		{
			MethodBase ri = this.RI;
			this._methodName = ri.Name;
			if (ri.DeclaringType != null)
			{
				this._typeAndAssemblyName = RemotingServices.GetDefaultQualifiedTypeName((RuntimeType)ri.DeclaringType);
			}
		}

		// Token: 0x17000E45 RID: 3653
		// (get) Token: 0x060056E0 RID: 22240 RVA: 0x001347B0 File Offset: 0x001329B0
		internal ParameterInfo[] Parameters
		{
			get
			{
				if (this._parameters == null)
				{
					this._parameters = this.RI.GetParameters();
				}
				return this._parameters;
			}
		}

		// Token: 0x17000E46 RID: 3654
		// (get) Token: 0x060056E1 RID: 22241 RVA: 0x001347D1 File Offset: 0x001329D1
		internal int[] OutRefArgMap
		{
			get
			{
				if (this._outRefArgMap == null)
				{
					this.GetArgMaps();
				}
				return this._outRefArgMap;
			}
		}

		// Token: 0x17000E47 RID: 3655
		// (get) Token: 0x060056E2 RID: 22242 RVA: 0x001347E7 File Offset: 0x001329E7
		internal int[] OutOnlyArgMap
		{
			get
			{
				if (this._outOnlyArgMap == null)
				{
					this.GetArgMaps();
				}
				return this._outOnlyArgMap;
			}
		}

		// Token: 0x17000E48 RID: 3656
		// (get) Token: 0x060056E3 RID: 22243 RVA: 0x001347FD File Offset: 0x001329FD
		internal int[] NonRefOutArgMap
		{
			get
			{
				if (this._nonRefOutArgMap == null)
				{
					this.GetArgMaps();
				}
				return this._nonRefOutArgMap;
			}
		}

		// Token: 0x17000E49 RID: 3657
		// (get) Token: 0x060056E4 RID: 22244 RVA: 0x00134813 File Offset: 0x00132A13
		internal int[] MarshalRequestArgMap
		{
			get
			{
				if (this._marshalRequestMap == null)
				{
					this.GetArgMaps();
				}
				return this._marshalRequestMap;
			}
		}

		// Token: 0x17000E4A RID: 3658
		// (get) Token: 0x060056E5 RID: 22245 RVA: 0x00134829 File Offset: 0x00132A29
		internal int[] MarshalResponseArgMap
		{
			get
			{
				if (this._marshalResponseMap == null)
				{
					this.GetArgMaps();
				}
				return this._marshalResponseMap;
			}
		}

		// Token: 0x060056E6 RID: 22246 RVA: 0x00134840 File Offset: 0x00132A40
		private void GetArgMaps()
		{
			lock (this)
			{
				if (this._inRefArgMap == null)
				{
					int[] inRefArgMap = null;
					int[] outRefArgMap = null;
					int[] outOnlyArgMap = null;
					int[] nonRefOutArgMap = null;
					int[] marshalRequestMap = null;
					int[] marshalResponseMap = null;
					ArgMapper.GetParameterMaps(this.Parameters, out inRefArgMap, out outRefArgMap, out outOnlyArgMap, out nonRefOutArgMap, out marshalRequestMap, out marshalResponseMap);
					this._inRefArgMap = inRefArgMap;
					this._outRefArgMap = outRefArgMap;
					this._outOnlyArgMap = outOnlyArgMap;
					this._nonRefOutArgMap = nonRefOutArgMap;
					this._marshalRequestMap = marshalRequestMap;
					this._marshalResponseMap = marshalResponseMap;
				}
			}
		}

		// Token: 0x060056E7 RID: 22247 RVA: 0x001348D4 File Offset: 0x00132AD4
		internal bool IsOneWayMethod()
		{
			if ((this.flags & RemotingMethodCachedData.MethodCacheFlags.CheckedOneWay) == RemotingMethodCachedData.MethodCacheFlags.None)
			{
				RemotingMethodCachedData.MethodCacheFlags methodCacheFlags = RemotingMethodCachedData.MethodCacheFlags.CheckedOneWay;
				object[] customAttributes = this.RI.GetCustomAttributes(typeof(OneWayAttribute), true);
				if (customAttributes != null && customAttributes.Length != 0)
				{
					methodCacheFlags |= RemotingMethodCachedData.MethodCacheFlags.IsOneWay;
				}
				this.flags |= methodCacheFlags;
				return (methodCacheFlags & RemotingMethodCachedData.MethodCacheFlags.IsOneWay) > RemotingMethodCachedData.MethodCacheFlags.None;
			}
			return (this.flags & RemotingMethodCachedData.MethodCacheFlags.IsOneWay) > RemotingMethodCachedData.MethodCacheFlags.None;
		}

		// Token: 0x060056E8 RID: 22248 RVA: 0x00134930 File Offset: 0x00132B30
		internal bool IsOverloaded()
		{
			if ((this.flags & RemotingMethodCachedData.MethodCacheFlags.CheckedOverloaded) == RemotingMethodCachedData.MethodCacheFlags.None)
			{
				RemotingMethodCachedData.MethodCacheFlags methodCacheFlags = RemotingMethodCachedData.MethodCacheFlags.CheckedOverloaded;
				MethodBase ri = this.RI;
				RuntimeMethodInfo runtimeMethodInfo;
				if ((runtimeMethodInfo = ri as RuntimeMethodInfo) != null)
				{
					if (runtimeMethodInfo.IsOverloaded)
					{
						methodCacheFlags |= RemotingMethodCachedData.MethodCacheFlags.IsOverloaded;
					}
				}
				else
				{
					RuntimeConstructorInfo runtimeConstructorInfo;
					if (!((runtimeConstructorInfo = ri as RuntimeConstructorInfo) != null))
					{
						throw new NotSupportedException(Environment.GetResourceString("InvalidOperation_Method"));
					}
					if (runtimeConstructorInfo.IsOverloaded)
					{
						methodCacheFlags |= RemotingMethodCachedData.MethodCacheFlags.IsOverloaded;
					}
				}
				this.flags |= methodCacheFlags;
			}
			return (this.flags & RemotingMethodCachedData.MethodCacheFlags.IsOverloaded) > RemotingMethodCachedData.MethodCacheFlags.None;
		}

		// Token: 0x17000E4B RID: 3659
		// (get) Token: 0x060056E9 RID: 22249 RVA: 0x001349BC File Offset: 0x00132BBC
		internal Type ReturnType
		{
			get
			{
				if ((this.flags & RemotingMethodCachedData.MethodCacheFlags.CheckedForReturnType) == RemotingMethodCachedData.MethodCacheFlags.None)
				{
					MethodInfo methodInfo = this.RI as MethodInfo;
					if (methodInfo != null)
					{
						Type returnType = methodInfo.ReturnType;
						if (returnType != typeof(void))
						{
							this._returnType = returnType;
						}
					}
					this.flags |= RemotingMethodCachedData.MethodCacheFlags.CheckedForReturnType;
				}
				return this._returnType;
			}
		}

		// Token: 0x040027BF RID: 10175
		private MethodBase RI;

		// Token: 0x040027C0 RID: 10176
		private ParameterInfo[] _parameters;

		// Token: 0x040027C1 RID: 10177
		private RemotingMethodCachedData.MethodCacheFlags flags;

		// Token: 0x040027C2 RID: 10178
		private string _typeAndAssemblyName;

		// Token: 0x040027C3 RID: 10179
		private string _methodName;

		// Token: 0x040027C4 RID: 10180
		private Type _returnType;

		// Token: 0x040027C5 RID: 10181
		private int[] _inRefArgMap;

		// Token: 0x040027C6 RID: 10182
		private int[] _outRefArgMap;

		// Token: 0x040027C7 RID: 10183
		private int[] _outOnlyArgMap;

		// Token: 0x040027C8 RID: 10184
		private int[] _nonRefOutArgMap;

		// Token: 0x040027C9 RID: 10185
		private int[] _marshalRequestMap;

		// Token: 0x040027CA RID: 10186
		private int[] _marshalResponseMap;

		// Token: 0x02000C6F RID: 3183
		[Flags]
		[Serializable]
		private enum MethodCacheFlags
		{
			// Token: 0x040037E8 RID: 14312
			None = 0,
			// Token: 0x040037E9 RID: 14313
			CheckedOneWay = 1,
			// Token: 0x040037EA RID: 14314
			IsOneWay = 2,
			// Token: 0x040037EB RID: 14315
			CheckedOverloaded = 4,
			// Token: 0x040037EC RID: 14316
			IsOverloaded = 8,
			// Token: 0x040037ED RID: 14317
			CheckedForAsync = 16,
			// Token: 0x040037EE RID: 14318
			CheckedForReturnType = 32
		}
	}
}
