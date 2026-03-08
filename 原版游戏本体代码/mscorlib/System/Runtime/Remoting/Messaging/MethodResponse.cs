using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200086B RID: 2155
	[SecurityCritical]
	[CLSCompliant(false)]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	[Serializable]
	public class MethodResponse : IMethodReturnMessage, IMethodMessage, IMessage, ISerializable, ISerializationRootObject, IInternalMessage
	{
		// Token: 0x06005B9F RID: 23455 RVA: 0x001415AC File Offset: 0x0013F7AC
		[SecurityCritical]
		public MethodResponse(Header[] h1, IMethodCallMessage mcm)
		{
			if (mcm == null)
			{
				throw new ArgumentNullException("mcm");
			}
			Message message = mcm as Message;
			if (message != null)
			{
				this.MI = message.GetMethodBase();
			}
			else
			{
				this.MI = mcm.MethodBase;
			}
			if (this.MI == null)
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Message_MethodMissing"), mcm.MethodName, mcm.TypeName));
			}
			this._methodCache = InternalRemotingServices.GetReflectionCachedData(this.MI);
			this.argCount = this._methodCache.Parameters.Length;
			this.fSoap = true;
			this.FillHeaders(h1);
		}

		// Token: 0x06005BA0 RID: 23456 RVA: 0x00141658 File Offset: 0x0013F858
		[SecurityCritical]
		internal MethodResponse(IMethodCallMessage msg, SmuggledMethodReturnMessage smuggledMrm, ArrayList deserializedArgs)
		{
			this.MI = msg.MethodBase;
			this._methodCache = InternalRemotingServices.GetReflectionCachedData(this.MI);
			this.methodName = msg.MethodName;
			this.uri = msg.Uri;
			this.typeName = msg.TypeName;
			if (this._methodCache.IsOverloaded())
			{
				this.methodSignature = (Type[])msg.MethodSignature;
			}
			this.retVal = smuggledMrm.GetReturnValue(deserializedArgs);
			this.outArgs = smuggledMrm.GetArgs(deserializedArgs);
			this.fault = smuggledMrm.GetException(deserializedArgs);
			this.callContext = smuggledMrm.GetCallContext(deserializedArgs);
			if (smuggledMrm.MessagePropertyCount > 0)
			{
				smuggledMrm.PopulateMessageProperties(this.Properties, deserializedArgs);
			}
			this.argCount = this._methodCache.Parameters.Length;
			this.fSoap = false;
		}

		// Token: 0x06005BA1 RID: 23457 RVA: 0x00141730 File Offset: 0x0013F930
		[SecurityCritical]
		internal MethodResponse(IMethodCallMessage msg, object handlerObject, BinaryMethodReturnMessage smuggledMrm)
		{
			if (msg != null)
			{
				this.MI = msg.MethodBase;
				this._methodCache = InternalRemotingServices.GetReflectionCachedData(this.MI);
				this.methodName = msg.MethodName;
				this.uri = msg.Uri;
				this.typeName = msg.TypeName;
				if (this._methodCache.IsOverloaded())
				{
					this.methodSignature = (Type[])msg.MethodSignature;
				}
				this.argCount = this._methodCache.Parameters.Length;
			}
			this.retVal = smuggledMrm.ReturnValue;
			this.outArgs = smuggledMrm.Args;
			this.fault = smuggledMrm.Exception;
			this.callContext = smuggledMrm.LogicalCallContext;
			if (smuggledMrm.HasProperties)
			{
				smuggledMrm.PopulateMessageProperties(this.Properties);
			}
			this.fSoap = false;
		}

		// Token: 0x06005BA2 RID: 23458 RVA: 0x00141803 File Offset: 0x0013FA03
		[SecurityCritical]
		internal MethodResponse(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			this.SetObjectData(info, context);
		}

		// Token: 0x06005BA3 RID: 23459 RVA: 0x00141824 File Offset: 0x0013FA24
		[SecurityCritical]
		public virtual object HeaderHandler(Header[] h)
		{
			SerializationMonkey serializationMonkey = (SerializationMonkey)FormatterServices.GetUninitializedObject(typeof(SerializationMonkey));
			Header[] array;
			if (h != null && h.Length != 0 && h[0].Name == "__methodName")
			{
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
			Type type = null;
			MethodInfo methodInfo = this.MI as MethodInfo;
			if (methodInfo != null)
			{
				type = methodInfo.ReturnType;
			}
			ParameterInfo[] parameters = this._methodCache.Parameters;
			int num = this._methodCache.MarshalResponseArgMap.Length;
			if (!(type == null) && !(type == typeof(void)))
			{
				num++;
			}
			Type[] array2 = new Type[num];
			string[] array3 = new string[num];
			int num2 = 0;
			if (!(type == null) && !(type == typeof(void)))
			{
				array2[num2++] = type;
			}
			foreach (int num3 in this._methodCache.MarshalResponseArgMap)
			{
				array3[num2] = parameters[num3].Name;
				if (parameters[num3].ParameterType.IsByRef)
				{
					array2[num2++] = parameters[num3].ParameterType.GetElementType();
				}
				else
				{
					array2[num2++] = parameters[num3].ParameterType;
				}
			}
			((IFieldInfo)serializationMonkey).FieldTypes = array2;
			((IFieldInfo)serializationMonkey).FieldNames = array3;
			this.FillHeaders(array, true);
			serializationMonkey._obj = this;
			return serializationMonkey;
		}

		// Token: 0x06005BA4 RID: 23460 RVA: 0x001419B6 File Offset: 0x0013FBB6
		[SecurityCritical]
		public void RootSetObjectData(SerializationInfo info, StreamingContext ctx)
		{
			this.SetObjectData(info, ctx);
		}

		// Token: 0x06005BA5 RID: 23461 RVA: 0x001419C0 File Offset: 0x0013FBC0
		[SecurityCritical]
		internal void SetObjectData(SerializationInfo info, StreamingContext ctx)
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
			bool flag = false;
			bool flag2 = false;
			while (enumerator.MoveNext())
			{
				if (enumerator.Name.Equals("__return"))
				{
					flag = true;
					break;
				}
				if (enumerator.Name.Equals("__fault"))
				{
					flag2 = true;
					this.fault = (Exception)enumerator.Value;
					break;
				}
				this.FillHeader(enumerator.Name, enumerator.Value);
			}
			if (flag2 && flag)
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Message_BadSerialization"));
			}
		}

		// Token: 0x06005BA6 RID: 23462 RVA: 0x00141A64 File Offset: 0x0013FC64
		[SecurityCritical]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
		}

		// Token: 0x06005BA7 RID: 23463 RVA: 0x00141A78 File Offset: 0x0013FC78
		internal void SetObjectFromSoapData(SerializationInfo info)
		{
			Hashtable keyToNamespaceTable = (Hashtable)info.GetValue("__keyToNamespaceTable", typeof(Hashtable));
			ArrayList arrayList = (ArrayList)info.GetValue("__paramNameList", typeof(ArrayList));
			SoapFault soapFault = (SoapFault)info.GetValue("__fault", typeof(SoapFault));
			if (soapFault != null)
			{
				ServerFault serverFault = soapFault.Detail as ServerFault;
				if (serverFault != null)
				{
					if (serverFault.Exception != null)
					{
						this.fault = serverFault.Exception;
						return;
					}
					Type type = Type.GetType(serverFault.ExceptionType, false, false);
					if (type == null)
					{
						StringBuilder stringBuilder = new StringBuilder();
						stringBuilder.Append("\nException Type: ");
						stringBuilder.Append(serverFault.ExceptionType);
						stringBuilder.Append("\n");
						stringBuilder.Append("Exception Message: ");
						stringBuilder.Append(serverFault.ExceptionMessage);
						stringBuilder.Append("\n");
						stringBuilder.Append(serverFault.StackTrace);
						this.fault = new ServerException(stringBuilder.ToString());
						return;
					}
					object[] args = new object[] { serverFault.ExceptionMessage };
					this.fault = (Exception)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, args, null, null);
					return;
				}
				else
				{
					if (soapFault.Detail != null && soapFault.Detail.GetType() == typeof(string) && ((string)soapFault.Detail).Length != 0)
					{
						this.fault = new ServerException((string)soapFault.Detail);
						return;
					}
					this.fault = new ServerException(soapFault.FaultString);
					return;
				}
			}
			else
			{
				MethodInfo methodInfo = this.MI as MethodInfo;
				int num = 0;
				if (methodInfo != null)
				{
					Type returnType = methodInfo.ReturnType;
					if (returnType != typeof(void))
					{
						num++;
						object value = info.GetValue((string)arrayList[0], typeof(object));
						if (value is string)
						{
							this.retVal = Message.SoapCoerceArg(value, returnType, keyToNamespaceTable);
						}
						else
						{
							this.retVal = value;
						}
					}
				}
				ParameterInfo[] parameters = this._methodCache.Parameters;
				object obj = ((this.InternalProperties == null) ? null : this.InternalProperties["__UnorderedParams"]);
				if (obj != null && obj is bool && (bool)obj)
				{
					for (int i = num; i < arrayList.Count; i++)
					{
						string text = (string)arrayList[i];
						int num2 = -1;
						for (int j = 0; j < parameters.Length; j++)
						{
							if (text.Equals(parameters[j].Name))
							{
								num2 = parameters[j].Position;
							}
						}
						if (num2 == -1)
						{
							if (!text.StartsWith("__param", StringComparison.Ordinal))
							{
								throw new RemotingException(Environment.GetResourceString("Remoting_Message_BadSerialization"));
							}
							num2 = int.Parse(text.Substring(7), CultureInfo.InvariantCulture);
						}
						if (num2 >= this.argCount)
						{
							throw new RemotingException(Environment.GetResourceString("Remoting_Message_BadSerialization"));
						}
						if (this.outArgs == null)
						{
							this.outArgs = new object[this.argCount];
						}
						this.outArgs[num2] = Message.SoapCoerceArg(info.GetValue(text, typeof(object)), parameters[num2].ParameterType, keyToNamespaceTable);
					}
					return;
				}
				if (this.argMapper == null)
				{
					this.argMapper = new ArgMapper(this, true);
				}
				for (int k = num; k < arrayList.Count; k++)
				{
					string name = (string)arrayList[k];
					if (this.outArgs == null)
					{
						this.outArgs = new object[this.argCount];
					}
					int num3 = this.argMapper.Map[k - num];
					this.outArgs[num3] = Message.SoapCoerceArg(info.GetValue(name, typeof(object)), parameters[num3].ParameterType, keyToNamespaceTable);
				}
				return;
			}
		}

		// Token: 0x06005BA8 RID: 23464 RVA: 0x00141E75 File Offset: 0x00140075
		[SecurityCritical]
		internal LogicalCallContext GetLogicalCallContext()
		{
			if (this.callContext == null)
			{
				this.callContext = new LogicalCallContext();
			}
			return this.callContext;
		}

		// Token: 0x06005BA9 RID: 23465 RVA: 0x00141E90 File Offset: 0x00140090
		internal LogicalCallContext SetLogicalCallContext(LogicalCallContext ctx)
		{
			LogicalCallContext result = this.callContext;
			this.callContext = ctx;
			return result;
		}

		// Token: 0x17000F96 RID: 3990
		// (get) Token: 0x06005BAA RID: 23466 RVA: 0x00141EAC File Offset: 0x001400AC
		// (set) Token: 0x06005BAB RID: 23467 RVA: 0x00141EB4 File Offset: 0x001400B4
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

		// Token: 0x17000F97 RID: 3991
		// (get) Token: 0x06005BAC RID: 23468 RVA: 0x00141EBD File Offset: 0x001400BD
		public string MethodName
		{
			[SecurityCritical]
			get
			{
				return this.methodName;
			}
		}

		// Token: 0x17000F98 RID: 3992
		// (get) Token: 0x06005BAD RID: 23469 RVA: 0x00141EC5 File Offset: 0x001400C5
		public string TypeName
		{
			[SecurityCritical]
			get
			{
				return this.typeName;
			}
		}

		// Token: 0x17000F99 RID: 3993
		// (get) Token: 0x06005BAE RID: 23470 RVA: 0x00141ECD File Offset: 0x001400CD
		public object MethodSignature
		{
			[SecurityCritical]
			get
			{
				return this.methodSignature;
			}
		}

		// Token: 0x17000F9A RID: 3994
		// (get) Token: 0x06005BAF RID: 23471 RVA: 0x00141ED5 File Offset: 0x001400D5
		public MethodBase MethodBase
		{
			[SecurityCritical]
			get
			{
				return this.MI;
			}
		}

		// Token: 0x17000F9B RID: 3995
		// (get) Token: 0x06005BB0 RID: 23472 RVA: 0x00141EDD File Offset: 0x001400DD
		public bool HasVarArgs
		{
			[SecurityCritical]
			get
			{
				return false;
			}
		}

		// Token: 0x17000F9C RID: 3996
		// (get) Token: 0x06005BB1 RID: 23473 RVA: 0x00141EE0 File Offset: 0x001400E0
		public int ArgCount
		{
			[SecurityCritical]
			get
			{
				if (this.outArgs == null)
				{
					return 0;
				}
				return this.outArgs.Length;
			}
		}

		// Token: 0x06005BB2 RID: 23474 RVA: 0x00141EF4 File Offset: 0x001400F4
		[SecurityCritical]
		public object GetArg(int argNum)
		{
			return this.outArgs[argNum];
		}

		// Token: 0x06005BB3 RID: 23475 RVA: 0x00141F00 File Offset: 0x00140100
		[SecurityCritical]
		public string GetArgName(int index)
		{
			if (!(this.MI != null))
			{
				return "__param" + index.ToString();
			}
			RemotingMethodCachedData reflectionCachedData = InternalRemotingServices.GetReflectionCachedData(this.MI);
			ParameterInfo[] parameters = reflectionCachedData.Parameters;
			if (index < 0 || index >= parameters.Length)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			return reflectionCachedData.Parameters[index].Name;
		}

		// Token: 0x17000F9D RID: 3997
		// (get) Token: 0x06005BB4 RID: 23476 RVA: 0x00141F62 File Offset: 0x00140162
		public object[] Args
		{
			[SecurityCritical]
			get
			{
				return this.outArgs;
			}
		}

		// Token: 0x17000F9E RID: 3998
		// (get) Token: 0x06005BB5 RID: 23477 RVA: 0x00141F6A File Offset: 0x0014016A
		public int OutArgCount
		{
			[SecurityCritical]
			get
			{
				if (this.argMapper == null)
				{
					this.argMapper = new ArgMapper(this, true);
				}
				return this.argMapper.ArgCount;
			}
		}

		// Token: 0x06005BB6 RID: 23478 RVA: 0x00141F8C File Offset: 0x0014018C
		[SecurityCritical]
		public object GetOutArg(int argNum)
		{
			if (this.argMapper == null)
			{
				this.argMapper = new ArgMapper(this, true);
			}
			return this.argMapper.GetArg(argNum);
		}

		// Token: 0x06005BB7 RID: 23479 RVA: 0x00141FAF File Offset: 0x001401AF
		[SecurityCritical]
		public string GetOutArgName(int index)
		{
			if (this.argMapper == null)
			{
				this.argMapper = new ArgMapper(this, true);
			}
			return this.argMapper.GetArgName(index);
		}

		// Token: 0x17000F9F RID: 3999
		// (get) Token: 0x06005BB8 RID: 23480 RVA: 0x00141FD2 File Offset: 0x001401D2
		public object[] OutArgs
		{
			[SecurityCritical]
			get
			{
				if (this.argMapper == null)
				{
					this.argMapper = new ArgMapper(this, true);
				}
				return this.argMapper.Args;
			}
		}

		// Token: 0x17000FA0 RID: 4000
		// (get) Token: 0x06005BB9 RID: 23481 RVA: 0x00141FF4 File Offset: 0x001401F4
		public Exception Exception
		{
			[SecurityCritical]
			get
			{
				return this.fault;
			}
		}

		// Token: 0x17000FA1 RID: 4001
		// (get) Token: 0x06005BBA RID: 23482 RVA: 0x00141FFC File Offset: 0x001401FC
		public object ReturnValue
		{
			[SecurityCritical]
			get
			{
				return this.retVal;
			}
		}

		// Token: 0x17000FA2 RID: 4002
		// (get) Token: 0x06005BBB RID: 23483 RVA: 0x00142004 File Offset: 0x00140204
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
						this.ExternalProperties = new MRMDictionary(this, this.InternalProperties);
					}
					externalProperties = this.ExternalProperties;
				}
				return externalProperties;
			}
		}

		// Token: 0x17000FA3 RID: 4003
		// (get) Token: 0x06005BBC RID: 23484 RVA: 0x00142070 File Offset: 0x00140270
		public LogicalCallContext LogicalCallContext
		{
			[SecurityCritical]
			get
			{
				return this.GetLogicalCallContext();
			}
		}

		// Token: 0x06005BBD RID: 23485 RVA: 0x00142078 File Offset: 0x00140278
		[SecurityCritical]
		internal void FillHeaders(Header[] h)
		{
			this.FillHeaders(h, false);
		}

		// Token: 0x06005BBE RID: 23486 RVA: 0x00142084 File Offset: 0x00140284
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

		// Token: 0x06005BBF RID: 23487 RVA: 0x0014210C File Offset: 0x0014030C
		[SecurityCritical]
		internal void FillHeader(string name, object value)
		{
			if (name.Equals("__MethodName"))
			{
				this.methodName = (string)value;
				return;
			}
			if (name.Equals("__Uri"))
			{
				this.uri = (string)value;
				return;
			}
			if (name.Equals("__MethodSignature"))
			{
				this.methodSignature = (Type[])value;
				return;
			}
			if (name.Equals("__TypeName"))
			{
				this.typeName = (string)value;
				return;
			}
			if (name.Equals("__OutArgs"))
			{
				this.outArgs = (object[])value;
				return;
			}
			if (name.Equals("__CallContext"))
			{
				if (value is string)
				{
					this.callContext = new LogicalCallContext();
					this.callContext.RemotingData.LogicalCallID = (string)value;
					return;
				}
				this.callContext = (LogicalCallContext)value;
				return;
			}
			else
			{
				if (name.Equals("__Return"))
				{
					this.retVal = value;
					return;
				}
				if (this.InternalProperties == null)
				{
					this.InternalProperties = new Hashtable();
				}
				this.InternalProperties[name] = value;
				return;
			}
		}

		// Token: 0x17000FA4 RID: 4004
		// (get) Token: 0x06005BC0 RID: 23488 RVA: 0x00142214 File Offset: 0x00140414
		// (set) Token: 0x06005BC1 RID: 23489 RVA: 0x00142217 File Offset: 0x00140417
		ServerIdentity IInternalMessage.ServerIdentityObject
		{
			[SecurityCritical]
			get
			{
				return null;
			}
			[SecurityCritical]
			set
			{
			}
		}

		// Token: 0x17000FA5 RID: 4005
		// (get) Token: 0x06005BC2 RID: 23490 RVA: 0x00142219 File Offset: 0x00140419
		// (set) Token: 0x06005BC3 RID: 23491 RVA: 0x0014221C File Offset: 0x0014041C
		Identity IInternalMessage.IdentityObject
		{
			[SecurityCritical]
			get
			{
				return null;
			}
			[SecurityCritical]
			set
			{
			}
		}

		// Token: 0x06005BC4 RID: 23492 RVA: 0x0014221E File Offset: 0x0014041E
		[SecurityCritical]
		void IInternalMessage.SetURI(string val)
		{
			this.uri = val;
		}

		// Token: 0x06005BC5 RID: 23493 RVA: 0x00142227 File Offset: 0x00140427
		[SecurityCritical]
		void IInternalMessage.SetCallContext(LogicalCallContext newCallContext)
		{
			this.callContext = newCallContext;
		}

		// Token: 0x06005BC6 RID: 23494 RVA: 0x00142230 File Offset: 0x00140430
		[SecurityCritical]
		bool IInternalMessage.HasProperties()
		{
			return this.ExternalProperties != null || this.InternalProperties != null;
		}

		// Token: 0x04002971 RID: 10609
		private MethodBase MI;

		// Token: 0x04002972 RID: 10610
		private string methodName;

		// Token: 0x04002973 RID: 10611
		private Type[] methodSignature;

		// Token: 0x04002974 RID: 10612
		private string uri;

		// Token: 0x04002975 RID: 10613
		private string typeName;

		// Token: 0x04002976 RID: 10614
		private object retVal;

		// Token: 0x04002977 RID: 10615
		private Exception fault;

		// Token: 0x04002978 RID: 10616
		private object[] outArgs;

		// Token: 0x04002979 RID: 10617
		private LogicalCallContext callContext;

		// Token: 0x0400297A RID: 10618
		protected IDictionary InternalProperties;

		// Token: 0x0400297B RID: 10619
		protected IDictionary ExternalProperties;

		// Token: 0x0400297C RID: 10620
		private int argCount;

		// Token: 0x0400297D RID: 10621
		private bool fSoap;

		// Token: 0x0400297E RID: 10622
		private ArgMapper argMapper;

		// Token: 0x0400297F RID: 10623
		private RemotingMethodCachedData _methodCache;
	}
}
