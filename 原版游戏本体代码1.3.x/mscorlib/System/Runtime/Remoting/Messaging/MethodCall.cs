using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000869 RID: 2153
	[SecurityCritical]
	[CLSCompliant(false)]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	[Serializable]
	public class MethodCall : IMethodCallMessage, IMethodMessage, IMessage, ISerializable, IInternalMessage, ISerializationRootObject
	{
		// Token: 0x06005B63 RID: 23395 RVA: 0x001403D3 File Offset: 0x0013E5D3
		[SecurityCritical]
		public MethodCall(Header[] h1)
		{
			this.Init();
			this.fSoap = true;
			this.FillHeaders(h1);
			this.ResolveMethod();
		}

		// Token: 0x06005B64 RID: 23396 RVA: 0x001403F8 File Offset: 0x0013E5F8
		[SecurityCritical]
		public MethodCall(IMessage msg)
		{
			if (msg == null)
			{
				throw new ArgumentNullException("msg");
			}
			this.Init();
			IDictionaryEnumerator enumerator = msg.Properties.GetEnumerator();
			while (enumerator.MoveNext())
			{
				this.FillHeader(enumerator.Key.ToString(), enumerator.Value);
			}
			IMethodCallMessage methodCallMessage = msg as IMethodCallMessage;
			if (methodCallMessage != null)
			{
				this.MI = methodCallMessage.MethodBase;
			}
			this.ResolveMethod();
		}

		// Token: 0x06005B65 RID: 23397 RVA: 0x00140468 File Offset: 0x0013E668
		[SecurityCritical]
		internal MethodCall(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			this.Init();
			this.SetObjectData(info, context);
		}

		// Token: 0x06005B66 RID: 23398 RVA: 0x0014048C File Offset: 0x0013E68C
		[SecurityCritical]
		internal MethodCall(SmuggledMethodCallMessage smuggledMsg, ArrayList deserializedArgs)
		{
			this.uri = smuggledMsg.Uri;
			this.typeName = smuggledMsg.TypeName;
			this.methodName = smuggledMsg.MethodName;
			this.methodSignature = (Type[])smuggledMsg.GetMethodSignature(deserializedArgs);
			this.args = smuggledMsg.GetArgs(deserializedArgs);
			this.instArgs = smuggledMsg.GetInstantiation(deserializedArgs);
			this.callContext = smuggledMsg.GetCallContext(deserializedArgs);
			this.ResolveMethod();
			if (smuggledMsg.MessagePropertyCount > 0)
			{
				smuggledMsg.PopulateMessageProperties(this.Properties, deserializedArgs);
			}
		}

		// Token: 0x06005B67 RID: 23399 RVA: 0x00140518 File Offset: 0x0013E718
		[SecurityCritical]
		internal MethodCall(object handlerObject, BinaryMethodCallMessage smuggledMsg)
		{
			if (handlerObject != null)
			{
				this.uri = handlerObject as string;
				if (this.uri == null)
				{
					MarshalByRefObject marshalByRefObject = handlerObject as MarshalByRefObject;
					if (marshalByRefObject != null)
					{
						bool flag;
						this.srvID = MarshalByRefObject.GetIdentity(marshalByRefObject, out flag) as ServerIdentity;
						this.uri = this.srvID.URI;
					}
				}
			}
			this.typeName = smuggledMsg.TypeName;
			this.methodName = smuggledMsg.MethodName;
			this.methodSignature = (Type[])smuggledMsg.MethodSignature;
			this.args = smuggledMsg.Args;
			this.instArgs = smuggledMsg.InstantiationArgs;
			this.callContext = smuggledMsg.LogicalCallContext;
			this.ResolveMethod();
			if (smuggledMsg.HasProperties)
			{
				smuggledMsg.PopulateMessageProperties(this.Properties);
			}
		}

		// Token: 0x06005B68 RID: 23400 RVA: 0x001405D7 File Offset: 0x0013E7D7
		[SecurityCritical]
		public void RootSetObjectData(SerializationInfo info, StreamingContext ctx)
		{
			this.SetObjectData(info, ctx);
		}

		// Token: 0x06005B69 RID: 23401 RVA: 0x001405E4 File Offset: 0x0013E7E4
		[SecurityCritical]
		internal void SetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			if (this.fSoap)
			{
				this.SetObjectFromSoapData(info);
				return;
			}
			SerializationInfoEnumerator enumerator = info.GetEnumerator();
			while (enumerator.MoveNext())
			{
				this.FillHeader(enumerator.Name, enumerator.Value);
			}
			if (context.State == StreamingContextStates.Remoting && context.Context != null)
			{
				Header[] array = context.Context as Header[];
				if (array != null)
				{
					for (int i = 0; i < array.Length; i++)
					{
						this.FillHeader(array[i].Name, array[i].Value);
					}
				}
			}
		}

		// Token: 0x06005B6A RID: 23402 RVA: 0x0014067C File Offset: 0x0013E87C
		private static Type ResolveTypeRelativeTo(string typeName, int offset, int count, Type serverType)
		{
			Type type = MethodCall.ResolveTypeRelativeToBaseTypes(typeName, offset, count, serverType);
			if (type == null)
			{
				Type[] interfaces = serverType.GetInterfaces();
				foreach (Type type2 in interfaces)
				{
					string fullName = type2.FullName;
					if (fullName.Length == count && string.CompareOrdinal(typeName, offset, fullName, 0, count) == 0)
					{
						return type2;
					}
				}
			}
			return type;
		}

		// Token: 0x06005B6B RID: 23403 RVA: 0x001406DC File Offset: 0x0013E8DC
		private static Type ResolveTypeRelativeToBaseTypes(string typeName, int offset, int count, Type serverType)
		{
			if (typeName == null || serverType == null)
			{
				return null;
			}
			string fullName = serverType.FullName;
			if (fullName.Length == count && string.CompareOrdinal(typeName, offset, fullName, 0, count) == 0)
			{
				return serverType;
			}
			return MethodCall.ResolveTypeRelativeToBaseTypes(typeName, offset, count, serverType.BaseType);
		}

		// Token: 0x06005B6C RID: 23404 RVA: 0x00140724 File Offset: 0x0013E924
		internal Type ResolveType()
		{
			Type type = null;
			if (this.srvID == null)
			{
				this.srvID = IdentityHolder.CasualResolveIdentity(this.uri) as ServerIdentity;
			}
			if (this.srvID != null)
			{
				Type type2 = this.srvID.GetLastCalledType(this.typeName);
				if (type2 != null)
				{
					return type2;
				}
				int num = 0;
				if (string.CompareOrdinal(this.typeName, 0, "clr:", 0, 4) == 0)
				{
					num = 4;
				}
				int num2 = this.typeName.IndexOf(',', num);
				if (num2 == -1)
				{
					num2 = this.typeName.Length;
				}
				type2 = this.srvID.ServerType;
				type = MethodCall.ResolveTypeRelativeTo(this.typeName, num, num2 - num, type2);
			}
			if (type == null)
			{
				type = RemotingServices.InternalGetTypeFromQualifiedTypeName(this.typeName);
			}
			if (this.srvID != null)
			{
				this.srvID.SetLastCalledType(this.typeName, type);
			}
			return type;
		}

		// Token: 0x06005B6D RID: 23405 RVA: 0x001407FB File Offset: 0x0013E9FB
		[SecurityCritical]
		public void ResolveMethod()
		{
			this.ResolveMethod(true);
		}

		// Token: 0x06005B6E RID: 23406 RVA: 0x00140804 File Offset: 0x0013EA04
		[SecurityCritical]
		internal void ResolveMethod(bool bThrowIfNotResolved)
		{
			if (this.MI == null && this.methodName != null)
			{
				RuntimeType runtimeType = this.ResolveType() as RuntimeType;
				if (this.methodName.Equals(".ctor"))
				{
					return;
				}
				if (runtimeType == null)
				{
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_BadType"), this.typeName));
				}
				if (this.methodSignature != null)
				{
					bool flag = false;
					int num = ((this.instArgs == null) ? 0 : this.instArgs.Length);
					if (num == 0)
					{
						try
						{
							this.MI = runtimeType.GetMethod(this.methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, CallingConventions.Any, this.methodSignature, null);
							flag = true;
						}
						catch (AmbiguousMatchException)
						{
						}
					}
					if (!flag)
					{
						MemberInfo[] array = runtimeType.FindMembers(MemberTypes.Method, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, Type.FilterName, this.methodName);
						int num2 = 0;
						for (int i = 0; i < array.Length; i++)
						{
							try
							{
								MethodInfo methodInfo = (MethodInfo)array[i];
								int num3 = (methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0);
								if (num3 == num)
								{
									if (num > 0)
									{
										methodInfo = methodInfo.MakeGenericMethod(this.instArgs);
									}
									array[num2] = methodInfo;
									num2++;
								}
							}
							catch (ArgumentException)
							{
							}
							catch (VerificationException)
							{
							}
						}
						MethodInfo[] array2 = new MethodInfo[num2];
						for (int j = 0; j < num2; j++)
						{
							array2[j] = (MethodInfo)array[j];
						}
						Binder defaultBinder = Type.DefaultBinder;
						BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
						MethodBase[] match = array2;
						this.MI = defaultBinder.SelectMethod(bindingAttr, match, this.methodSignature, null);
					}
				}
				else
				{
					RemotingTypeCachedData remotingTypeCachedData = null;
					if (this.instArgs == null)
					{
						remotingTypeCachedData = InternalRemotingServices.GetReflectionCachedData(runtimeType);
						this.MI = remotingTypeCachedData.GetLastCalledMethod(this.methodName);
						if (this.MI != null)
						{
							return;
						}
					}
					bool flag2 = false;
					try
					{
						this.MI = runtimeType.GetMethod(this.methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
						if (this.instArgs != null && this.instArgs.Length != 0)
						{
							this.MI = ((MethodInfo)this.MI).MakeGenericMethod(this.instArgs);
						}
					}
					catch (AmbiguousMatchException)
					{
						flag2 = true;
						this.ResolveOverloadedMethod(runtimeType);
					}
					if (this.MI != null && !flag2 && remotingTypeCachedData != null)
					{
						remotingTypeCachedData.SetLastCalledMethod(this.methodName, this.MI);
					}
				}
				if (this.MI == null && bThrowIfNotResolved)
				{
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Message_MethodMissing"), this.methodName, this.typeName));
				}
			}
		}

		// Token: 0x06005B6F RID: 23407 RVA: 0x00140AA4 File Offset: 0x0013ECA4
		private void ResolveOverloadedMethod(RuntimeType t)
		{
			if (this.args == null)
			{
				return;
			}
			MemberInfo[] member = t.GetMember(this.methodName, MemberTypes.Method, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			int num = member.Length;
			if (num == 1)
			{
				this.MI = member[0] as MethodBase;
				return;
			}
			if (num == 0)
			{
				return;
			}
			int num2 = this.args.Length;
			MethodBase methodBase = null;
			for (int i = 0; i < num; i++)
			{
				MethodBase methodBase2 = member[i] as MethodBase;
				if (methodBase2.GetParameters().Length == num2)
				{
					if (methodBase != null)
					{
						throw new RemotingException(Environment.GetResourceString("Remoting_AmbiguousMethod"));
					}
					methodBase = methodBase2;
				}
			}
			if (methodBase != null)
			{
				this.MI = methodBase;
			}
		}

		// Token: 0x06005B70 RID: 23408 RVA: 0x00140B44 File Offset: 0x0013ED44
		private void ResolveOverloadedMethod(RuntimeType t, string methodName, ArrayList argNames, ArrayList argValues)
		{
			MemberInfo[] member = t.GetMember(methodName, MemberTypes.Method, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			int num = member.Length;
			if (num == 1)
			{
				this.MI = member[0] as MethodBase;
				return;
			}
			if (num == 0)
			{
				return;
			}
			MethodBase methodBase = null;
			for (int i = 0; i < num; i++)
			{
				MethodBase methodBase2 = member[i] as MethodBase;
				ParameterInfo[] parameters = methodBase2.GetParameters();
				if (parameters.Length == argValues.Count)
				{
					bool flag = true;
					for (int j = 0; j < parameters.Length; j++)
					{
						Type type = parameters[j].ParameterType;
						if (type.IsByRef)
						{
							type = type.GetElementType();
						}
						if (type != argValues[j].GetType())
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						methodBase = methodBase2;
						break;
					}
				}
			}
			if (methodBase == null)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_AmbiguousMethod"));
			}
			this.MI = methodBase;
		}

		// Token: 0x06005B71 RID: 23409 RVA: 0x00140C21 File Offset: 0x0013EE21
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
		}

		// Token: 0x06005B72 RID: 23410 RVA: 0x00140C34 File Offset: 0x0013EE34
		[SecurityCritical]
		internal void SetObjectFromSoapData(SerializationInfo info)
		{
			this.methodName = info.GetString("__methodName");
			ArrayList arrayList = (ArrayList)info.GetValue("__paramNameList", typeof(ArrayList));
			Hashtable keyToNamespaceTable = (Hashtable)info.GetValue("__keyToNamespaceTable", typeof(Hashtable));
			if (this.MI == null)
			{
				ArrayList arrayList2 = new ArrayList();
				ArrayList arrayList3 = arrayList;
				for (int i = 0; i < arrayList3.Count; i++)
				{
					arrayList2.Add(info.GetValue((string)arrayList3[i], typeof(object)));
				}
				RuntimeType runtimeType = this.ResolveType() as RuntimeType;
				if (runtimeType == null)
				{
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_BadType"), this.typeName));
				}
				this.ResolveOverloadedMethod(runtimeType, this.methodName, arrayList3, arrayList2);
				if (this.MI == null)
				{
					throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Message_MethodMissing"), this.methodName, this.typeName));
				}
			}
			RemotingMethodCachedData reflectionCachedData = InternalRemotingServices.GetReflectionCachedData(this.MI);
			ParameterInfo[] parameters = reflectionCachedData.Parameters;
			int[] marshalRequestArgMap = reflectionCachedData.MarshalRequestArgMap;
			object obj = ((this.InternalProperties == null) ? null : this.InternalProperties["__UnorderedParams"]);
			this.args = new object[parameters.Length];
			if (obj != null && obj is bool && (bool)obj)
			{
				for (int j = 0; j < arrayList.Count; j++)
				{
					string text = (string)arrayList[j];
					int num = -1;
					for (int k = 0; k < parameters.Length; k++)
					{
						if (text.Equals(parameters[k].Name))
						{
							num = parameters[k].Position;
							break;
						}
					}
					if (num == -1)
					{
						if (!text.StartsWith("__param", StringComparison.Ordinal))
						{
							throw new RemotingException(Environment.GetResourceString("Remoting_Message_BadSerialization"));
						}
						num = int.Parse(text.Substring(7), CultureInfo.InvariantCulture);
					}
					if (num >= this.args.Length)
					{
						throw new RemotingException(Environment.GetResourceString("Remoting_Message_BadSerialization"));
					}
					this.args[num] = Message.SoapCoerceArg(info.GetValue(text, typeof(object)), parameters[num].ParameterType, keyToNamespaceTable);
				}
				return;
			}
			for (int l = 0; l < arrayList.Count; l++)
			{
				string name = (string)arrayList[l];
				this.args[marshalRequestArgMap[l]] = Message.SoapCoerceArg(info.GetValue(name, typeof(object)), parameters[marshalRequestArgMap[l]].ParameterType, keyToNamespaceTable);
			}
			this.PopulateOutArguments(reflectionCachedData);
		}

		// Token: 0x06005B73 RID: 23411 RVA: 0x00140EFC File Offset: 0x0013F0FC
		[SecurityCritical]
		[PermissionSet(SecurityAction.Assert, Unrestricted = true)]
		private void PopulateOutArguments(RemotingMethodCachedData methodCache)
		{
			ParameterInfo[] parameters = methodCache.Parameters;
			foreach (int num in methodCache.OutOnlyArgMap)
			{
				Type elementType = parameters[num].ParameterType.GetElementType();
				if (elementType.IsValueType)
				{
					this.args[num] = Activator.CreateInstance(elementType, true);
				}
			}
		}

		// Token: 0x06005B74 RID: 23412 RVA: 0x00140F51 File Offset: 0x0013F151
		public virtual void Init()
		{
		}

		// Token: 0x17000F82 RID: 3970
		// (get) Token: 0x06005B75 RID: 23413 RVA: 0x00140F53 File Offset: 0x0013F153
		public int ArgCount
		{
			[SecurityCritical]
			get
			{
				if (this.args != null)
				{
					return this.args.Length;
				}
				return 0;
			}
		}

		// Token: 0x06005B76 RID: 23414 RVA: 0x00140F67 File Offset: 0x0013F167
		[SecurityCritical]
		public object GetArg(int argNum)
		{
			return this.args[argNum];
		}

		// Token: 0x06005B77 RID: 23415 RVA: 0x00140F74 File Offset: 0x0013F174
		[SecurityCritical]
		public string GetArgName(int index)
		{
			this.ResolveMethod();
			RemotingMethodCachedData reflectionCachedData = InternalRemotingServices.GetReflectionCachedData(this.MI);
			return reflectionCachedData.Parameters[index].Name;
		}

		// Token: 0x17000F83 RID: 3971
		// (get) Token: 0x06005B78 RID: 23416 RVA: 0x00140FA0 File Offset: 0x0013F1A0
		public object[] Args
		{
			[SecurityCritical]
			get
			{
				return this.args;
			}
		}

		// Token: 0x17000F84 RID: 3972
		// (get) Token: 0x06005B79 RID: 23417 RVA: 0x00140FA8 File Offset: 0x0013F1A8
		public int InArgCount
		{
			[SecurityCritical]
			get
			{
				if (this.argMapper == null)
				{
					this.argMapper = new ArgMapper(this, false);
				}
				return this.argMapper.ArgCount;
			}
		}

		// Token: 0x06005B7A RID: 23418 RVA: 0x00140FCA File Offset: 0x0013F1CA
		[SecurityCritical]
		public object GetInArg(int argNum)
		{
			if (this.argMapper == null)
			{
				this.argMapper = new ArgMapper(this, false);
			}
			return this.argMapper.GetArg(argNum);
		}

		// Token: 0x06005B7B RID: 23419 RVA: 0x00140FED File Offset: 0x0013F1ED
		[SecurityCritical]
		public string GetInArgName(int index)
		{
			if (this.argMapper == null)
			{
				this.argMapper = new ArgMapper(this, false);
			}
			return this.argMapper.GetArgName(index);
		}

		// Token: 0x17000F85 RID: 3973
		// (get) Token: 0x06005B7C RID: 23420 RVA: 0x00141010 File Offset: 0x0013F210
		public object[] InArgs
		{
			[SecurityCritical]
			get
			{
				if (this.argMapper == null)
				{
					this.argMapper = new ArgMapper(this, false);
				}
				return this.argMapper.Args;
			}
		}

		// Token: 0x17000F86 RID: 3974
		// (get) Token: 0x06005B7D RID: 23421 RVA: 0x00141032 File Offset: 0x0013F232
		public string MethodName
		{
			[SecurityCritical]
			get
			{
				return this.methodName;
			}
		}

		// Token: 0x17000F87 RID: 3975
		// (get) Token: 0x06005B7E RID: 23422 RVA: 0x0014103A File Offset: 0x0013F23A
		public string TypeName
		{
			[SecurityCritical]
			get
			{
				return this.typeName;
			}
		}

		// Token: 0x17000F88 RID: 3976
		// (get) Token: 0x06005B7F RID: 23423 RVA: 0x00141042 File Offset: 0x0013F242
		public object MethodSignature
		{
			[SecurityCritical]
			get
			{
				if (this.methodSignature != null)
				{
					return this.methodSignature;
				}
				if (this.MI != null)
				{
					this.methodSignature = Message.GenerateMethodSignature(this.MethodBase);
				}
				return null;
			}
		}

		// Token: 0x17000F89 RID: 3977
		// (get) Token: 0x06005B80 RID: 23424 RVA: 0x00141073 File Offset: 0x0013F273
		public MethodBase MethodBase
		{
			[SecurityCritical]
			get
			{
				if (this.MI == null)
				{
					this.MI = RemotingServices.InternalGetMethodBaseFromMethodMessage(this);
				}
				return this.MI;
			}
		}

		// Token: 0x17000F8A RID: 3978
		// (get) Token: 0x06005B81 RID: 23425 RVA: 0x00141095 File Offset: 0x0013F295
		// (set) Token: 0x06005B82 RID: 23426 RVA: 0x0014109D File Offset: 0x0013F29D
		public string Uri
		{
			[SecurityCritical]
			get
			{
				return this.uri;
			}
			set
			{
				this.uri = value;
			}
		}

		// Token: 0x17000F8B RID: 3979
		// (get) Token: 0x06005B83 RID: 23427 RVA: 0x001410A6 File Offset: 0x0013F2A6
		public bool HasVarArgs
		{
			[SecurityCritical]
			get
			{
				return this.fVarArgs;
			}
		}

		// Token: 0x17000F8C RID: 3980
		// (get) Token: 0x06005B84 RID: 23428 RVA: 0x001410B0 File Offset: 0x0013F2B0
		public virtual IDictionary Properties
		{
			[SecurityCritical]
			get
			{
				IDictionary externalProperties;
				lock (this)
				{
					if (this.InternalProperties == null)
					{
						this.InternalProperties = new Hashtable();
					}
					if (this.ExternalProperties == null)
					{
						this.ExternalProperties = new MCMDictionary(this, this.InternalProperties);
					}
					externalProperties = this.ExternalProperties;
				}
				return externalProperties;
			}
		}

		// Token: 0x17000F8D RID: 3981
		// (get) Token: 0x06005B85 RID: 23429 RVA: 0x0014111C File Offset: 0x0013F31C
		public LogicalCallContext LogicalCallContext
		{
			[SecurityCritical]
			get
			{
				return this.GetLogicalCallContext();
			}
		}

		// Token: 0x06005B86 RID: 23430 RVA: 0x00141124 File Offset: 0x0013F324
		[SecurityCritical]
		internal LogicalCallContext GetLogicalCallContext()
		{
			if (this.callContext == null)
			{
				this.callContext = new LogicalCallContext();
			}
			return this.callContext;
		}

		// Token: 0x06005B87 RID: 23431 RVA: 0x00141140 File Offset: 0x0013F340
		internal LogicalCallContext SetLogicalCallContext(LogicalCallContext ctx)
		{
			LogicalCallContext result = this.callContext;
			this.callContext = ctx;
			return result;
		}

		// Token: 0x17000F8E RID: 3982
		// (get) Token: 0x06005B88 RID: 23432 RVA: 0x0014115C File Offset: 0x0013F35C
		// (set) Token: 0x06005B89 RID: 23433 RVA: 0x00141164 File Offset: 0x0013F364
		ServerIdentity IInternalMessage.ServerIdentityObject
		{
			[SecurityCritical]
			get
			{
				return this.srvID;
			}
			[SecurityCritical]
			set
			{
				this.srvID = value;
			}
		}

		// Token: 0x17000F8F RID: 3983
		// (get) Token: 0x06005B8A RID: 23434 RVA: 0x0014116D File Offset: 0x0013F36D
		// (set) Token: 0x06005B8B RID: 23435 RVA: 0x00141175 File Offset: 0x0013F375
		Identity IInternalMessage.IdentityObject
		{
			[SecurityCritical]
			get
			{
				return this.identity;
			}
			[SecurityCritical]
			set
			{
				this.identity = value;
			}
		}

		// Token: 0x06005B8C RID: 23436 RVA: 0x0014117E File Offset: 0x0013F37E
		[SecurityCritical]
		void IInternalMessage.SetURI(string val)
		{
			this.uri = val;
		}

		// Token: 0x06005B8D RID: 23437 RVA: 0x00141187 File Offset: 0x0013F387
		[SecurityCritical]
		void IInternalMessage.SetCallContext(LogicalCallContext newCallContext)
		{
			this.callContext = newCallContext;
		}

		// Token: 0x06005B8E RID: 23438 RVA: 0x00141190 File Offset: 0x0013F390
		[SecurityCritical]
		bool IInternalMessage.HasProperties()
		{
			return this.ExternalProperties != null || this.InternalProperties != null;
		}

		// Token: 0x06005B8F RID: 23439 RVA: 0x001411A5 File Offset: 0x0013F3A5
		[SecurityCritical]
		internal void FillHeaders(Header[] h)
		{
			this.FillHeaders(h, false);
		}

		// Token: 0x06005B90 RID: 23440 RVA: 0x001411B0 File Offset: 0x0013F3B0
		[SecurityCritical]
		private void FillHeaders(Header[] h, bool bFromHeaderHandler)
		{
			if (h == null)
			{
				return;
			}
			if (bFromHeaderHandler && this.fSoap)
			{
				foreach (Header header in h)
				{
					if (header.HeaderNamespace == "http://schemas.microsoft.com/clr/soap/messageProperties")
					{
						this.FillHeader(header.Name, header.Value);
					}
					else
					{
						string propertyKeyForHeader = LogicalCallContext.GetPropertyKeyForHeader(header);
						this.FillHeader(propertyKeyForHeader, header);
					}
				}
				return;
			}
			for (int j = 0; j < h.Length; j++)
			{
				this.FillHeader(h[j].Name, h[j].Value);
			}
		}

		// Token: 0x06005B91 RID: 23441 RVA: 0x00141238 File Offset: 0x0013F438
		[SecurityCritical]
		internal virtual bool FillSpecialHeader(string key, object value)
		{
			if (key != null)
			{
				if (key.Equals("__Uri"))
				{
					this.uri = (string)value;
				}
				else if (key.Equals("__MethodName"))
				{
					this.methodName = (string)value;
				}
				else if (key.Equals("__MethodSignature"))
				{
					this.methodSignature = (Type[])value;
				}
				else if (key.Equals("__TypeName"))
				{
					this.typeName = (string)value;
				}
				else if (key.Equals("__Args"))
				{
					this.args = (object[])value;
				}
				else
				{
					if (!key.Equals("__CallContext"))
					{
						return false;
					}
					if (value is string)
					{
						this.callContext = new LogicalCallContext();
						this.callContext.RemotingData.LogicalCallID = (string)value;
					}
					else
					{
						this.callContext = (LogicalCallContext)value;
					}
				}
			}
			return true;
		}

		// Token: 0x06005B92 RID: 23442 RVA: 0x00141321 File Offset: 0x0013F521
		[SecurityCritical]
		internal void FillHeader(string key, object value)
		{
			if (!this.FillSpecialHeader(key, value))
			{
				if (this.InternalProperties == null)
				{
					this.InternalProperties = new Hashtable();
				}
				this.InternalProperties[key] = value;
			}
		}

		// Token: 0x06005B93 RID: 23443 RVA: 0x00141350 File Offset: 0x0013F550
		[SecurityCritical]
		public virtual object HeaderHandler(Header[] h)
		{
			SerializationMonkey serializationMonkey = (SerializationMonkey)FormatterServices.GetUninitializedObject(typeof(SerializationMonkey));
			Header[] array;
			if (h != null && h.Length != 0 && h[0].Name == "__methodName")
			{
				this.methodName = (string)h[0].Value;
				if (h.Length > 1)
				{
					array = new Header[h.Length - 1];
					Array.Copy(h, 1, array, 0, h.Length - 1);
				}
				else
				{
					array = null;
				}
			}
			else
			{
				array = h;
			}
			this.FillHeaders(array, true);
			this.ResolveMethod(false);
			serializationMonkey._obj = this;
			if (this.MI != null)
			{
				ArgMapper argMapper = new ArgMapper(this.MI, false);
				serializationMonkey.fieldNames = argMapper.ArgNames;
				serializationMonkey.fieldTypes = argMapper.ArgTypes;
			}
			return serializationMonkey;
		}

		// Token: 0x0400295B RID: 10587
		private const BindingFlags LookupAll = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		// Token: 0x0400295C RID: 10588
		private const BindingFlags LookupPublic = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

		// Token: 0x0400295D RID: 10589
		private string uri;

		// Token: 0x0400295E RID: 10590
		private string methodName;

		// Token: 0x0400295F RID: 10591
		private MethodBase MI;

		// Token: 0x04002960 RID: 10592
		private string typeName;

		// Token: 0x04002961 RID: 10593
		private object[] args;

		// Token: 0x04002962 RID: 10594
		private Type[] instArgs;

		// Token: 0x04002963 RID: 10595
		private LogicalCallContext callContext;

		// Token: 0x04002964 RID: 10596
		private Type[] methodSignature;

		// Token: 0x04002965 RID: 10597
		protected IDictionary ExternalProperties;

		// Token: 0x04002966 RID: 10598
		protected IDictionary InternalProperties;

		// Token: 0x04002967 RID: 10599
		private ServerIdentity srvID;

		// Token: 0x04002968 RID: 10600
		private Identity identity;

		// Token: 0x04002969 RID: 10601
		private bool fSoap;

		// Token: 0x0400296A RID: 10602
		private bool fVarArgs;

		// Token: 0x0400296B RID: 10603
		private ArgMapper argMapper;
	}
}
