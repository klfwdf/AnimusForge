using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security;

namespace System.Runtime.Serialization
{
	// Token: 0x02000757 RID: 1879
	internal class SerializationEvents
	{
		// Token: 0x060052E0 RID: 21216 RVA: 0x00123454 File Offset: 0x00121654
		private List<MethodInfo> GetMethodsWithAttribute(Type attribute, Type t)
		{
			List<MethodInfo> list = new List<MethodInfo>();
			Type type = t;
			while (type != null && type != typeof(object))
			{
				RuntimeType runtimeType = (RuntimeType)type;
				MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (MethodInfo methodInfo in methods)
				{
					if (methodInfo.IsDefined(attribute, false))
					{
						list.Add(methodInfo);
					}
				}
				type = type.BaseType;
			}
			list.Reverse();
			if (list.Count != 0)
			{
				return list;
			}
			return null;
		}

		// Token: 0x060052E1 RID: 21217 RVA: 0x001234E0 File Offset: 0x001216E0
		internal SerializationEvents(Type t)
		{
			this.m_OnSerializingMethods = this.GetMethodsWithAttribute(typeof(OnSerializingAttribute), t);
			this.m_OnSerializedMethods = this.GetMethodsWithAttribute(typeof(OnSerializedAttribute), t);
			this.m_OnDeserializingMethods = this.GetMethodsWithAttribute(typeof(OnDeserializingAttribute), t);
			this.m_OnDeserializedMethods = this.GetMethodsWithAttribute(typeof(OnDeserializedAttribute), t);
		}

		// Token: 0x17000DB4 RID: 3508
		// (get) Token: 0x060052E2 RID: 21218 RVA: 0x0012354F File Offset: 0x0012174F
		internal bool HasOnSerializingEvents
		{
			get
			{
				return this.m_OnSerializingMethods != null || this.m_OnSerializedMethods != null;
			}
		}

		// Token: 0x060052E3 RID: 21219 RVA: 0x00123564 File Offset: 0x00121764
		[SecuritySafeCritical]
		internal void InvokeOnSerializing(object obj, StreamingContext context)
		{
			if (this.m_OnSerializingMethods != null)
			{
				object[] array = new object[] { context };
				SerializationEventHandler serializationEventHandler = null;
				foreach (MethodInfo method in this.m_OnSerializingMethods)
				{
					SerializationEventHandler b = (SerializationEventHandler)Delegate.CreateDelegateNoSecurityCheck((RuntimeType)typeof(SerializationEventHandler), obj, method);
					serializationEventHandler = (SerializationEventHandler)Delegate.Combine(serializationEventHandler, b);
				}
				serializationEventHandler(context);
			}
		}

		// Token: 0x060052E4 RID: 21220 RVA: 0x001235FC File Offset: 0x001217FC
		[SecuritySafeCritical]
		internal void InvokeOnDeserializing(object obj, StreamingContext context)
		{
			if (this.m_OnDeserializingMethods != null)
			{
				object[] array = new object[] { context };
				SerializationEventHandler serializationEventHandler = null;
				foreach (MethodInfo method in this.m_OnDeserializingMethods)
				{
					SerializationEventHandler b = (SerializationEventHandler)Delegate.CreateDelegateNoSecurityCheck((RuntimeType)typeof(SerializationEventHandler), obj, method);
					serializationEventHandler = (SerializationEventHandler)Delegate.Combine(serializationEventHandler, b);
				}
				serializationEventHandler(context);
			}
		}

		// Token: 0x060052E5 RID: 21221 RVA: 0x00123694 File Offset: 0x00121894
		[SecuritySafeCritical]
		internal void InvokeOnDeserialized(object obj, StreamingContext context)
		{
			if (this.m_OnDeserializedMethods != null)
			{
				object[] array = new object[] { context };
				SerializationEventHandler serializationEventHandler = null;
				foreach (MethodInfo method in this.m_OnDeserializedMethods)
				{
					SerializationEventHandler b = (SerializationEventHandler)Delegate.CreateDelegateNoSecurityCheck((RuntimeType)typeof(SerializationEventHandler), obj, method);
					serializationEventHandler = (SerializationEventHandler)Delegate.Combine(serializationEventHandler, b);
				}
				serializationEventHandler(context);
			}
		}

		// Token: 0x060052E6 RID: 21222 RVA: 0x0012372C File Offset: 0x0012192C
		[SecurityCritical]
		internal SerializationEventHandler AddOnSerialized(object obj, SerializationEventHandler handler)
		{
			if (this.m_OnSerializedMethods != null)
			{
				foreach (MethodInfo method in this.m_OnSerializedMethods)
				{
					SerializationEventHandler b = (SerializationEventHandler)Delegate.CreateDelegateNoSecurityCheck((RuntimeType)typeof(SerializationEventHandler), obj, method);
					handler = (SerializationEventHandler)Delegate.Combine(handler, b);
				}
			}
			return handler;
		}

		// Token: 0x060052E7 RID: 21223 RVA: 0x001237AC File Offset: 0x001219AC
		[SecurityCritical]
		internal SerializationEventHandler AddOnDeserialized(object obj, SerializationEventHandler handler)
		{
			if (this.m_OnDeserializedMethods != null)
			{
				foreach (MethodInfo method in this.m_OnDeserializedMethods)
				{
					SerializationEventHandler b = (SerializationEventHandler)Delegate.CreateDelegateNoSecurityCheck((RuntimeType)typeof(SerializationEventHandler), obj, method);
					handler = (SerializationEventHandler)Delegate.Combine(handler, b);
				}
			}
			return handler;
		}

		// Token: 0x040024C1 RID: 9409
		private List<MethodInfo> m_OnSerializingMethods;

		// Token: 0x040024C2 RID: 9410
		private List<MethodInfo> m_OnSerializedMethods;

		// Token: 0x040024C3 RID: 9411
		private List<MethodInfo> m_OnDeserializingMethods;

		// Token: 0x040024C4 RID: 9412
		private List<MethodInfo> m_OnDeserializedMethods;
	}
}
