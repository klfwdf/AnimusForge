using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Metadata;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200085E RID: 2142
	[Serializable]
	internal class Message : IMethodCallMessage, IMethodMessage, IMessage, IInternalMessage, ISerializable
	{
		// Token: 0x06005A99 RID: 23193 RVA: 0x0013DFAB File Offset: 0x0013C1AB
		public virtual Exception GetFault()
		{
			return this._Fault;
		}

		// Token: 0x06005A9A RID: 23194 RVA: 0x0013DFB3 File Offset: 0x0013C1B3
		public virtual void SetFault(Exception e)
		{
			this._Fault = e;
		}

		// Token: 0x06005A9B RID: 23195 RVA: 0x0013DFBC File Offset: 0x0013C1BC
		internal virtual void SetOneWay()
		{
			this._flags |= 8;
		}

		// Token: 0x06005A9C RID: 23196 RVA: 0x0013DFCC File Offset: 0x0013C1CC
		public virtual int GetCallType()
		{
			this.InitIfNecessary();
			return this._flags;
		}

		// Token: 0x06005A9D RID: 23197 RVA: 0x0013DFDA File Offset: 0x0013C1DA
		internal IntPtr GetFramePtr()
		{
			return this._frame;
		}

		// Token: 0x06005A9E RID: 23198
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void GetAsyncBeginInfo(out AsyncCallback acbd, out object state);

		// Token: 0x06005A9F RID: 23199
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern object GetThisPtr();

		// Token: 0x06005AA0 RID: 23200
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern IAsyncResult GetAsyncResult();

		// Token: 0x06005AA1 RID: 23201 RVA: 0x0013DFE2 File Offset: 0x0013C1E2
		public void Init()
		{
		}

		// Token: 0x06005AA2 RID: 23202
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern object GetReturnValue();

		// Token: 0x06005AA3 RID: 23203 RVA: 0x0013DFE4 File Offset: 0x0013C1E4
		internal Message()
		{
		}

		// Token: 0x06005AA4 RID: 23204 RVA: 0x0013DFEC File Offset: 0x0013C1EC
		[SecurityCritical]
		internal void InitFields(MessageData msgData)
		{
			this._frame = msgData.pFrame;
			this._delegateMD = msgData.pDelegateMD;
			this._methodDesc = msgData.pMethodDesc;
			this._flags = msgData.iFlags;
			this._initDone = true;
			this._metaSigHolder = msgData.pSig;
			this._governingType = msgData.thGoverningType;
			this._MethodName = null;
			this._MethodSignature = null;
			this._MethodBase = null;
			this._URI = null;
			this._Fault = null;
			this._ID = null;
			this._srvID = null;
			this._callContext = null;
			if (this._properties != null)
			{
				((IDictionary)this._properties).Clear();
			}
		}

		// Token: 0x06005AA5 RID: 23205 RVA: 0x0013E098 File Offset: 0x0013C298
		private void InitIfNecessary()
		{
			if (!this._initDone)
			{
				this.Init();
				this._initDone = true;
			}
		}

		// Token: 0x17000F35 RID: 3893
		// (get) Token: 0x06005AA6 RID: 23206 RVA: 0x0013E0AF File Offset: 0x0013C2AF
		// (set) Token: 0x06005AA7 RID: 23207 RVA: 0x0013E0B7 File Offset: 0x0013C2B7
		ServerIdentity IInternalMessage.ServerIdentityObject
		{
			[SecurityCritical]
			get
			{
				return this._srvID;
			}
			[SecurityCritical]
			set
			{
				this._srvID = value;
			}
		}

		// Token: 0x17000F36 RID: 3894
		// (get) Token: 0x06005AA8 RID: 23208 RVA: 0x0013E0C0 File Offset: 0x0013C2C0
		// (set) Token: 0x06005AA9 RID: 23209 RVA: 0x0013E0C8 File Offset: 0x0013C2C8
		Identity IInternalMessage.IdentityObject
		{
			[SecurityCritical]
			get
			{
				return this._ID;
			}
			[SecurityCritical]
			set
			{
				this._ID = value;
			}
		}

		// Token: 0x06005AAA RID: 23210 RVA: 0x0013E0D1 File Offset: 0x0013C2D1
		[SecurityCritical]
		void IInternalMessage.SetURI(string URI)
		{
			this._URI = URI;
		}

		// Token: 0x06005AAB RID: 23211 RVA: 0x0013E0DA File Offset: 0x0013C2DA
		[SecurityCritical]
		void IInternalMessage.SetCallContext(LogicalCallContext callContext)
		{
			this._callContext = callContext;
		}

		// Token: 0x06005AAC RID: 23212 RVA: 0x0013E0E3 File Offset: 0x0013C2E3
		[SecurityCritical]
		bool IInternalMessage.HasProperties()
		{
			return this._properties != null;
		}

		// Token: 0x17000F37 RID: 3895
		// (get) Token: 0x06005AAD RID: 23213 RVA: 0x0013E0EE File Offset: 0x0013C2EE
		public IDictionary Properties
		{
			[SecurityCritical]
			get
			{
				if (this._properties == null)
				{
					Interlocked.CompareExchange(ref this._properties, new MCMDictionary(this, null), null);
				}
				return (IDictionary)this._properties;
			}
		}

		// Token: 0x17000F38 RID: 3896
		// (get) Token: 0x06005AAE RID: 23214 RVA: 0x0013E117 File Offset: 0x0013C317
		// (set) Token: 0x06005AAF RID: 23215 RVA: 0x0013E11F File Offset: 0x0013C31F
		public string Uri
		{
			[SecurityCritical]
			get
			{
				return this._URI;
			}
			set
			{
				this._URI = value;
			}
		}

		// Token: 0x17000F39 RID: 3897
		// (get) Token: 0x06005AB0 RID: 23216 RVA: 0x0013E128 File Offset: 0x0013C328
		public bool HasVarArgs
		{
			[SecurityCritical]
			get
			{
				if ((this._flags & 16) == 0 && (this._flags & 32) == 0)
				{
					if (!this.InternalHasVarArgs())
					{
						this._flags |= 16;
					}
					else
					{
						this._flags |= 32;
					}
				}
				return 1 == (this._flags & 32);
			}
		}

		// Token: 0x17000F3A RID: 3898
		// (get) Token: 0x06005AB1 RID: 23217 RVA: 0x0013E17F File Offset: 0x0013C37F
		public int ArgCount
		{
			[SecurityCritical]
			get
			{
				return this.InternalGetArgCount();
			}
		}

		// Token: 0x06005AB2 RID: 23218 RVA: 0x0013E187 File Offset: 0x0013C387
		[SecurityCritical]
		public object GetArg(int argNum)
		{
			return this.InternalGetArg(argNum);
		}

		// Token: 0x06005AB3 RID: 23219 RVA: 0x0013E190 File Offset: 0x0013C390
		[SecurityCritical]
		public string GetArgName(int index)
		{
			if (index >= this.ArgCount)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			RemotingMethodCachedData reflectionCachedData = InternalRemotingServices.GetReflectionCachedData(this.GetMethodBase());
			ParameterInfo[] parameters = reflectionCachedData.Parameters;
			if (index < parameters.Length)
			{
				return parameters[index].Name;
			}
			return "VarArg" + (index - parameters.Length).ToString();
		}

		// Token: 0x17000F3B RID: 3899
		// (get) Token: 0x06005AB4 RID: 23220 RVA: 0x0013E1EA File Offset: 0x0013C3EA
		public object[] Args
		{
			[SecurityCritical]
			get
			{
				return this.InternalGetArgs();
			}
		}

		// Token: 0x17000F3C RID: 3900
		// (get) Token: 0x06005AB5 RID: 23221 RVA: 0x0013E1F2 File Offset: 0x0013C3F2
		public int InArgCount
		{
			[SecurityCritical]
			get
			{
				if (this._argMapper == null)
				{
					this._argMapper = new ArgMapper(this, false);
				}
				return this._argMapper.ArgCount;
			}
		}

		// Token: 0x06005AB6 RID: 23222 RVA: 0x0013E214 File Offset: 0x0013C414
		[SecurityCritical]
		public object GetInArg(int argNum)
		{
			if (this._argMapper == null)
			{
				this._argMapper = new ArgMapper(this, false);
			}
			return this._argMapper.GetArg(argNum);
		}

		// Token: 0x06005AB7 RID: 23223 RVA: 0x0013E237 File Offset: 0x0013C437
		[SecurityCritical]
		public string GetInArgName(int index)
		{
			if (this._argMapper == null)
			{
				this._argMapper = new ArgMapper(this, false);
			}
			return this._argMapper.GetArgName(index);
		}

		// Token: 0x17000F3D RID: 3901
		// (get) Token: 0x06005AB8 RID: 23224 RVA: 0x0013E25A File Offset: 0x0013C45A
		public object[] InArgs
		{
			[SecurityCritical]
			get
			{
				if (this._argMapper == null)
				{
					this._argMapper = new ArgMapper(this, false);
				}
				return this._argMapper.Args;
			}
		}

		// Token: 0x06005AB9 RID: 23225 RVA: 0x0013E27C File Offset: 0x0013C47C
		[SecurityCritical]
		private void UpdateNames()
		{
			RemotingMethodCachedData reflectionCachedData = InternalRemotingServices.GetReflectionCachedData(this.GetMethodBase());
			this._typeName = reflectionCachedData.TypeAndAssemblyName;
			this._MethodName = reflectionCachedData.MethodName;
		}

		// Token: 0x17000F3E RID: 3902
		// (get) Token: 0x06005ABA RID: 23226 RVA: 0x0013E2AD File Offset: 0x0013C4AD
		public string MethodName
		{
			[SecurityCritical]
			get
			{
				if (this._MethodName == null)
				{
					this.UpdateNames();
				}
				return this._MethodName;
			}
		}

		// Token: 0x17000F3F RID: 3903
		// (get) Token: 0x06005ABB RID: 23227 RVA: 0x0013E2C3 File Offset: 0x0013C4C3
		public string TypeName
		{
			[SecurityCritical]
			get
			{
				if (this._typeName == null)
				{
					this.UpdateNames();
				}
				return this._typeName;
			}
		}

		// Token: 0x17000F40 RID: 3904
		// (get) Token: 0x06005ABC RID: 23228 RVA: 0x0013E2D9 File Offset: 0x0013C4D9
		public object MethodSignature
		{
			[SecurityCritical]
			get
			{
				if (this._MethodSignature == null)
				{
					this._MethodSignature = Message.GenerateMethodSignature(this.GetMethodBase());
				}
				return this._MethodSignature;
			}
		}

		// Token: 0x17000F41 RID: 3905
		// (get) Token: 0x06005ABD RID: 23229 RVA: 0x0013E2FA File Offset: 0x0013C4FA
		public LogicalCallContext LogicalCallContext
		{
			[SecurityCritical]
			get
			{
				return this.GetLogicalCallContext();
			}
		}

		// Token: 0x17000F42 RID: 3906
		// (get) Token: 0x06005ABE RID: 23230 RVA: 0x0013E302 File Offset: 0x0013C502
		public MethodBase MethodBase
		{
			[SecurityCritical]
			get
			{
				return this.GetMethodBase();
			}
		}

		// Token: 0x06005ABF RID: 23231 RVA: 0x0013E30A File Offset: 0x0013C50A
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
		}

		// Token: 0x06005AC0 RID: 23232 RVA: 0x0013E31C File Offset: 0x0013C51C
		[SecurityCritical]
		internal MethodBase GetMethodBase()
		{
			if (null == this._MethodBase)
			{
				IRuntimeMethodInfo methodHandle = new RuntimeMethodInfoStub(this._methodDesc, null);
				this._MethodBase = RuntimeType.GetMethodBase(Type.GetTypeFromHandleUnsafe(this._governingType), methodHandle);
			}
			return this._MethodBase;
		}

		// Token: 0x06005AC1 RID: 23233 RVA: 0x0013E364 File Offset: 0x0013C564
		[SecurityCritical]
		internal LogicalCallContext SetLogicalCallContext(LogicalCallContext callCtx)
		{
			LogicalCallContext callContext = this._callContext;
			this._callContext = callCtx;
			return callContext;
		}

		// Token: 0x06005AC2 RID: 23234 RVA: 0x0013E380 File Offset: 0x0013C580
		[SecurityCritical]
		internal LogicalCallContext GetLogicalCallContext()
		{
			if (this._callContext == null)
			{
				this._callContext = new LogicalCallContext();
			}
			return this._callContext;
		}

		// Token: 0x06005AC3 RID: 23235 RVA: 0x0013E39C File Offset: 0x0013C59C
		[SecurityCritical]
		internal static Type[] GenerateMethodSignature(MethodBase mb)
		{
			RemotingMethodCachedData reflectionCachedData = InternalRemotingServices.GetReflectionCachedData(mb);
			ParameterInfo[] parameters = reflectionCachedData.Parameters;
			Type[] array = new Type[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				array[i] = parameters[i].ParameterType;
			}
			return array;
		}

		// Token: 0x06005AC4 RID: 23236 RVA: 0x0013E3DC File Offset: 0x0013C5DC
		[SecurityCritical]
		internal static object[] CoerceArgs(IMethodMessage m)
		{
			MethodBase methodBase = m.MethodBase;
			RemotingMethodCachedData reflectionCachedData = InternalRemotingServices.GetReflectionCachedData(methodBase);
			return Message.CoerceArgs(m, reflectionCachedData.Parameters);
		}

		// Token: 0x06005AC5 RID: 23237 RVA: 0x0013E403 File Offset: 0x0013C603
		[SecurityCritical]
		internal static object[] CoerceArgs(IMethodMessage m, ParameterInfo[] pi)
		{
			return Message.CoerceArgs(m.MethodBase, m.Args, pi);
		}

		// Token: 0x06005AC6 RID: 23238 RVA: 0x0013E418 File Offset: 0x0013C618
		[SecurityCritical]
		internal static object[] CoerceArgs(MethodBase mb, object[] args, ParameterInfo[] pi)
		{
			if (pi == null)
			{
				throw new ArgumentNullException("pi");
			}
			if (pi.Length != args.Length)
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Message_ArgMismatch"), new object[]
				{
					mb.DeclaringType.FullName,
					mb.Name,
					args.Length,
					pi.Length
				}));
			}
			for (int i = 0; i < pi.Length; i++)
			{
				ParameterInfo parameterInfo = pi[i];
				Type parameterType = parameterInfo.ParameterType;
				object obj = args[i];
				if (obj != null)
				{
					args[i] = Message.CoerceArg(obj, parameterType);
				}
				else if (parameterType.IsByRef)
				{
					Type elementType = parameterType.GetElementType();
					if (elementType.IsValueType)
					{
						if (parameterInfo.IsOut)
						{
							args[i] = Activator.CreateInstance(elementType, true);
						}
						else if (!elementType.IsGenericType || !(elementType.GetGenericTypeDefinition() == typeof(Nullable<>)))
						{
							throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Message_MissingArgValue"), elementType.FullName, i));
						}
					}
				}
				else if (parameterType.IsValueType && (!parameterType.IsGenericType || !(parameterType.GetGenericTypeDefinition() == typeof(Nullable<>))))
				{
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Message_MissingArgValue"), parameterType.FullName, i));
				}
			}
			return args;
		}

		// Token: 0x06005AC7 RID: 23239 RVA: 0x0013E588 File Offset: 0x0013C788
		[SecurityCritical]
		internal static object CoerceArg(object value, Type pt)
		{
			object obj = null;
			if (value != null)
			{
				Exception innerException = null;
				try
				{
					if (pt.IsByRef)
					{
						pt = pt.GetElementType();
					}
					if (pt.IsInstanceOfType(value))
					{
						obj = value;
					}
					else
					{
						obj = Convert.ChangeType(value, pt, CultureInfo.InvariantCulture);
					}
				}
				catch (Exception ex)
				{
					innerException = ex;
				}
				if (obj == null)
				{
					string arg;
					if (RemotingServices.IsTransparentProxy(value))
					{
						arg = typeof(MarshalByRefObject).ToString();
					}
					else
					{
						arg = value.ToString();
					}
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Message_CoercionFailed"), arg, pt), innerException);
				}
			}
			return obj;
		}

		// Token: 0x06005AC8 RID: 23240 RVA: 0x0013E624 File Offset: 0x0013C824
		[SecurityCritical]
		internal static object SoapCoerceArg(object value, Type pt, Hashtable keyToNamespaceTable)
		{
			object obj = null;
			if (value != null)
			{
				try
				{
					if (pt.IsByRef)
					{
						pt = pt.GetElementType();
					}
					if (pt.IsInstanceOfType(value))
					{
						obj = value;
					}
					else
					{
						string text = value as string;
						if (text != null)
						{
							if (pt == typeof(double))
							{
								if (text == "INF")
								{
									obj = double.PositiveInfinity;
								}
								else if (text == "-INF")
								{
									obj = double.NegativeInfinity;
								}
								else
								{
									obj = double.Parse(text, CultureInfo.InvariantCulture);
								}
							}
							else if (pt == typeof(float))
							{
								if (text == "INF")
								{
									obj = float.PositiveInfinity;
								}
								else if (text == "-INF")
								{
									obj = float.NegativeInfinity;
								}
								else
								{
									obj = float.Parse(text, CultureInfo.InvariantCulture);
								}
							}
							else if (SoapType.typeofISoapXsd.IsAssignableFrom(pt))
							{
								if (pt == SoapType.typeofSoapTime)
								{
									obj = SoapTime.Parse(text);
								}
								else if (pt == SoapType.typeofSoapDate)
								{
									obj = SoapDate.Parse(text);
								}
								else if (pt == SoapType.typeofSoapYearMonth)
								{
									obj = SoapYearMonth.Parse(text);
								}
								else if (pt == SoapType.typeofSoapYear)
								{
									obj = SoapYear.Parse(text);
								}
								else if (pt == SoapType.typeofSoapMonthDay)
								{
									obj = SoapMonthDay.Parse(text);
								}
								else if (pt == SoapType.typeofSoapDay)
								{
									obj = SoapDay.Parse(text);
								}
								else if (pt == SoapType.typeofSoapMonth)
								{
									obj = SoapMonth.Parse(text);
								}
								else if (pt == SoapType.typeofSoapHexBinary)
								{
									obj = SoapHexBinary.Parse(text);
								}
								else if (pt == SoapType.typeofSoapBase64Binary)
								{
									obj = SoapBase64Binary.Parse(text);
								}
								else if (pt == SoapType.typeofSoapInteger)
								{
									obj = SoapInteger.Parse(text);
								}
								else if (pt == SoapType.typeofSoapPositiveInteger)
								{
									obj = SoapPositiveInteger.Parse(text);
								}
								else if (pt == SoapType.typeofSoapNonPositiveInteger)
								{
									obj = SoapNonPositiveInteger.Parse(text);
								}
								else if (pt == SoapType.typeofSoapNonNegativeInteger)
								{
									obj = SoapNonNegativeInteger.Parse(text);
								}
								else if (pt == SoapType.typeofSoapNegativeInteger)
								{
									obj = SoapNegativeInteger.Parse(text);
								}
								else if (pt == SoapType.typeofSoapAnyUri)
								{
									obj = SoapAnyUri.Parse(text);
								}
								else if (pt == SoapType.typeofSoapQName)
								{
									obj = SoapQName.Parse(text);
									SoapQName soapQName = (SoapQName)obj;
									if (soapQName.Key.Length == 0)
									{
										soapQName.Namespace = (string)keyToNamespaceTable["xmlns"];
									}
									else
									{
										soapQName.Namespace = (string)keyToNamespaceTable["xmlns:" + soapQName.Key];
									}
								}
								else if (pt == SoapType.typeofSoapNotation)
								{
									obj = SoapNotation.Parse(text);
								}
								else if (pt == SoapType.typeofSoapNormalizedString)
								{
									obj = SoapNormalizedString.Parse(text);
								}
								else if (pt == SoapType.typeofSoapToken)
								{
									obj = SoapToken.Parse(text);
								}
								else if (pt == SoapType.typeofSoapLanguage)
								{
									obj = SoapLanguage.Parse(text);
								}
								else if (pt == SoapType.typeofSoapName)
								{
									obj = SoapName.Parse(text);
								}
								else if (pt == SoapType.typeofSoapIdrefs)
								{
									obj = SoapIdrefs.Parse(text);
								}
								else if (pt == SoapType.typeofSoapEntities)
								{
									obj = SoapEntities.Parse(text);
								}
								else if (pt == SoapType.typeofSoapNmtoken)
								{
									obj = SoapNmtoken.Parse(text);
								}
								else if (pt == SoapType.typeofSoapNmtokens)
								{
									obj = SoapNmtokens.Parse(text);
								}
								else if (pt == SoapType.typeofSoapNcName)
								{
									obj = SoapNcName.Parse(text);
								}
								else if (pt == SoapType.typeofSoapId)
								{
									obj = SoapId.Parse(text);
								}
								else if (pt == SoapType.typeofSoapIdref)
								{
									obj = SoapIdref.Parse(text);
								}
								else if (pt == SoapType.typeofSoapEntity)
								{
									obj = SoapEntity.Parse(text);
								}
							}
							else if (pt == typeof(bool))
							{
								if (text == "1" || text == "true")
								{
									obj = true;
								}
								else
								{
									if (!(text == "0") && !(text == "false"))
									{
										throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Message_CoercionFailed"), text, pt));
									}
									obj = false;
								}
							}
							else if (pt == typeof(DateTime))
							{
								obj = SoapDateTime.Parse(text);
							}
							else if (pt.IsPrimitive)
							{
								obj = Convert.ChangeType(value, pt, CultureInfo.InvariantCulture);
							}
							else if (pt == typeof(TimeSpan))
							{
								obj = SoapDuration.Parse(text);
							}
							else if (pt == typeof(char))
							{
								obj = text[0];
							}
							else
							{
								obj = Convert.ChangeType(value, pt, CultureInfo.InvariantCulture);
							}
						}
						else
						{
							obj = Convert.ChangeType(value, pt, CultureInfo.InvariantCulture);
						}
					}
				}
				catch (Exception)
				{
				}
				if (obj == null)
				{
					string arg;
					if (RemotingServices.IsTransparentProxy(value))
					{
						arg = typeof(MarshalByRefObject).ToString();
					}
					else
					{
						arg = value.ToString();
					}
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Message_CoercionFailed"), arg, pt));
				}
			}
			return obj;
		}

		// Token: 0x06005AC9 RID: 23241
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern bool InternalHasVarArgs();

		// Token: 0x06005ACA RID: 23242
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern int InternalGetArgCount();

		// Token: 0x06005ACB RID: 23243
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern object InternalGetArg(int argNum);

		// Token: 0x06005ACC RID: 23244
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern object[] InternalGetArgs();

		// Token: 0x06005ACD RID: 23245
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void PropagateOutParameters(object[] OutArgs, object retVal);

		// Token: 0x06005ACE RID: 23246
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern bool Dispatch(object target);

		// Token: 0x06005ACF RID: 23247 RVA: 0x0013EBDC File Offset: 0x0013CDDC
		[SecurityCritical]
		[Conditional("_REMOTING_DEBUG")]
		public static void DebugOut(string s)
		{
			Message.OutToUnmanagedDebugger("\nRMTING: Thrd " + Thread.CurrentThread.GetHashCode().ToString() + " : " + s);
		}

		// Token: 0x06005AD0 RID: 23248
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void OutToUnmanagedDebugger(string s);

		// Token: 0x06005AD1 RID: 23249 RVA: 0x0013EC10 File Offset: 0x0013CE10
		[SecurityCritical]
		internal static LogicalCallContext PropagateCallContextFromMessageToThread(IMessage msg)
		{
			return CallContext.SetLogicalCallContext((LogicalCallContext)msg.Properties[Message.CallContextKey]);
		}

		// Token: 0x06005AD2 RID: 23250 RVA: 0x0013EC2C File Offset: 0x0013CE2C
		[SecurityCritical]
		internal static void PropagateCallContextFromThreadToMessage(IMessage msg)
		{
			LogicalCallContext logicalCallContext = Thread.CurrentThread.GetMutableExecutionContext().LogicalCallContext;
			msg.Properties[Message.CallContextKey] = logicalCallContext;
		}

		// Token: 0x06005AD3 RID: 23251 RVA: 0x0013EC5A File Offset: 0x0013CE5A
		[SecurityCritical]
		internal static void PropagateCallContextFromThreadToMessage(IMessage msg, LogicalCallContext oldcctx)
		{
			Message.PropagateCallContextFromThreadToMessage(msg);
			CallContext.SetLogicalCallContext(oldcctx);
		}

		// Token: 0x0400290E RID: 10510
		internal const int Sync = 0;

		// Token: 0x0400290F RID: 10511
		internal const int BeginAsync = 1;

		// Token: 0x04002910 RID: 10512
		internal const int EndAsync = 2;

		// Token: 0x04002911 RID: 10513
		internal const int Ctor = 4;

		// Token: 0x04002912 RID: 10514
		internal const int OneWay = 8;

		// Token: 0x04002913 RID: 10515
		internal const int CallMask = 15;

		// Token: 0x04002914 RID: 10516
		internal const int FixedArgs = 16;

		// Token: 0x04002915 RID: 10517
		internal const int VarArgs = 32;

		// Token: 0x04002916 RID: 10518
		private string _MethodName;

		// Token: 0x04002917 RID: 10519
		private Type[] _MethodSignature;

		// Token: 0x04002918 RID: 10520
		private MethodBase _MethodBase;

		// Token: 0x04002919 RID: 10521
		private object _properties;

		// Token: 0x0400291A RID: 10522
		private string _URI;

		// Token: 0x0400291B RID: 10523
		private string _typeName;

		// Token: 0x0400291C RID: 10524
		private Exception _Fault;

		// Token: 0x0400291D RID: 10525
		private Identity _ID;

		// Token: 0x0400291E RID: 10526
		private ServerIdentity _srvID;

		// Token: 0x0400291F RID: 10527
		private ArgMapper _argMapper;

		// Token: 0x04002920 RID: 10528
		[SecurityCritical]
		private LogicalCallContext _callContext;

		// Token: 0x04002921 RID: 10529
		private IntPtr _frame;

		// Token: 0x04002922 RID: 10530
		private IntPtr _methodDesc;

		// Token: 0x04002923 RID: 10531
		private IntPtr _metaSigHolder;

		// Token: 0x04002924 RID: 10532
		private IntPtr _delegateMD;

		// Token: 0x04002925 RID: 10533
		private IntPtr _governingType;

		// Token: 0x04002926 RID: 10534
		private int _flags;

		// Token: 0x04002927 RID: 10535
		private bool _initDone;

		// Token: 0x04002928 RID: 10536
		internal static string CallContextKey = "__CallContext";

		// Token: 0x04002929 RID: 10537
		internal static string UriKey = "__Uri";
	}
}
