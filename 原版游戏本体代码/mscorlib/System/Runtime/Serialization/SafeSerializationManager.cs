using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security;

namespace System.Runtime.Serialization
{
	// Token: 0x02000755 RID: 1877
	[Serializable]
	internal sealed class SafeSerializationManager : IObjectReference, ISerializable
	{
		// Token: 0x1400001D RID: 29
		// (add) Token: 0x060052D2 RID: 21202 RVA: 0x001230C8 File Offset: 0x001212C8
		// (remove) Token: 0x060052D3 RID: 21203 RVA: 0x00123100 File Offset: 0x00121300
		internal event EventHandler<SafeSerializationEventArgs> SerializeObjectState;

		// Token: 0x060052D4 RID: 21204 RVA: 0x00123135 File Offset: 0x00121335
		internal SafeSerializationManager()
		{
		}

		// Token: 0x060052D5 RID: 21205 RVA: 0x00123140 File Offset: 0x00121340
		[SecurityCritical]
		private SafeSerializationManager(SerializationInfo info, StreamingContext context)
		{
			RuntimeType runtimeType = info.GetValueNoThrow("CLR_SafeSerializationManager_RealType", typeof(RuntimeType)) as RuntimeType;
			if (runtimeType == null)
			{
				this.m_serializedStates = info.GetValue("m_serializedStates", typeof(List<object>)) as List<object>;
				return;
			}
			this.m_realType = runtimeType;
			this.m_savedSerializationInfo = info;
		}

		// Token: 0x17000DB3 RID: 3507
		// (get) Token: 0x060052D6 RID: 21206 RVA: 0x001231A6 File Offset: 0x001213A6
		internal bool IsActive
		{
			get
			{
				return this.SerializeObjectState != null;
			}
		}

		// Token: 0x060052D7 RID: 21207 RVA: 0x001231B4 File Offset: 0x001213B4
		[SecurityCritical]
		internal void CompleteSerialization(object serializedObject, SerializationInfo info, StreamingContext context)
		{
			this.m_serializedStates = null;
			EventHandler<SafeSerializationEventArgs> serializeObjectState = this.SerializeObjectState;
			if (serializeObjectState != null)
			{
				SafeSerializationEventArgs safeSerializationEventArgs = new SafeSerializationEventArgs(context);
				serializeObjectState(serializedObject, safeSerializationEventArgs);
				this.m_serializedStates = safeSerializationEventArgs.SerializedStates;
				info.AddValue("CLR_SafeSerializationManager_RealType", serializedObject.GetType(), typeof(RuntimeType));
				info.SetType(typeof(SafeSerializationManager));
			}
		}

		// Token: 0x060052D8 RID: 21208 RVA: 0x00123218 File Offset: 0x00121418
		internal void CompleteDeserialization(object deserializedObject)
		{
			if (this.m_serializedStates != null)
			{
				foreach (object obj in this.m_serializedStates)
				{
					ISafeSerializationData safeSerializationData = (ISafeSerializationData)obj;
					safeSerializationData.CompleteDeserialization(deserializedObject);
				}
			}
		}

		// Token: 0x060052D9 RID: 21209 RVA: 0x00123274 File Offset: 0x00121474
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("m_serializedStates", this.m_serializedStates, typeof(List<IDeserializationCallback>));
		}

		// Token: 0x060052DA RID: 21210 RVA: 0x00123294 File Offset: 0x00121494
		[SecurityCritical]
		object IObjectReference.GetRealObject(StreamingContext context)
		{
			if (this.m_realObject != null)
			{
				return this.m_realObject;
			}
			if (this.m_realType == null)
			{
				return this;
			}
			Stack stack = new Stack();
			RuntimeType runtimeType = this.m_realType;
			do
			{
				stack.Push(runtimeType);
				runtimeType = runtimeType.BaseType as RuntimeType;
			}
			while (runtimeType != typeof(object));
			RuntimeType t;
			RuntimeConstructorInfo runtimeConstructorInfo;
			do
			{
				t = runtimeType;
				runtimeType = stack.Pop() as RuntimeType;
				runtimeConstructorInfo = runtimeType.GetSerializationCtor();
			}
			while (runtimeConstructorInfo != null && runtimeConstructorInfo.IsSecurityCritical);
			runtimeConstructorInfo = ObjectManager.GetConstructor(t);
			object uninitializedObject = FormatterServices.GetUninitializedObject(this.m_realType);
			runtimeConstructorInfo.SerializationInvoke(uninitializedObject, this.m_savedSerializationInfo, context);
			this.m_savedSerializationInfo = null;
			this.m_realType = null;
			this.m_realObject = uninitializedObject;
			return uninitializedObject;
		}

		// Token: 0x060052DB RID: 21211 RVA: 0x00123358 File Offset: 0x00121558
		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (this.m_realObject != null)
			{
				SerializationEvents serializationEventsForType = SerializationEventsCache.GetSerializationEventsForType(this.m_realObject.GetType());
				serializationEventsForType.InvokeOnDeserialized(this.m_realObject, context);
				this.m_realObject = null;
			}
		}

		// Token: 0x040024B8 RID: 9400
		private IList<object> m_serializedStates;

		// Token: 0x040024B9 RID: 9401
		private SerializationInfo m_savedSerializationInfo;

		// Token: 0x040024BA RID: 9402
		private object m_realObject;

		// Token: 0x040024BB RID: 9403
		private RuntimeType m_realType;

		// Token: 0x040024BD RID: 9405
		private const string RealTypeSerializationName = "CLR_SafeSerializationManager_RealType";
	}
}
