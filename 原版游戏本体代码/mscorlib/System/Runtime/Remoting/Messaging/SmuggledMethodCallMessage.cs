using System;
using System.Collections;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000877 RID: 2167
	internal class SmuggledMethodCallMessage : MessageSmuggler
	{
		// Token: 0x06005C32 RID: 23602 RVA: 0x0014333C File Offset: 0x0014153C
		[SecurityCritical]
		internal static SmuggledMethodCallMessage SmuggleIfPossible(IMessage msg)
		{
			IMethodCallMessage methodCallMessage = msg as IMethodCallMessage;
			if (methodCallMessage == null)
			{
				return null;
			}
			return new SmuggledMethodCallMessage(methodCallMessage);
		}

		// Token: 0x06005C33 RID: 23603 RVA: 0x0014335B File Offset: 0x0014155B
		private SmuggledMethodCallMessage()
		{
		}

		// Token: 0x06005C34 RID: 23604 RVA: 0x00143364 File Offset: 0x00141564
		[SecurityCritical]
		private SmuggledMethodCallMessage(IMethodCallMessage mcm)
		{
			this._uri = mcm.Uri;
			this._methodName = mcm.MethodName;
			this._typeName = mcm.TypeName;
			ArrayList arrayList = null;
			IInternalMessage internalMessage = mcm as IInternalMessage;
			if (internalMessage == null || internalMessage.HasProperties())
			{
				this._propertyCount = MessageSmuggler.StoreUserPropertiesForMethodMessage(mcm, ref arrayList);
			}
			if (mcm.MethodBase.IsGenericMethod)
			{
				Type[] genericArguments = mcm.MethodBase.GetGenericArguments();
				if (genericArguments != null && genericArguments.Length != 0)
				{
					if (arrayList == null)
					{
						arrayList = new ArrayList();
					}
					this._instantiation = new MessageSmuggler.SerializedArg(arrayList.Count);
					arrayList.Add(genericArguments);
				}
			}
			if (RemotingServices.IsMethodOverloaded(mcm))
			{
				if (arrayList == null)
				{
					arrayList = new ArrayList();
				}
				this._methodSignature = new MessageSmuggler.SerializedArg(arrayList.Count);
				arrayList.Add(mcm.MethodSignature);
			}
			LogicalCallContext logicalCallContext = mcm.LogicalCallContext;
			if (logicalCallContext == null)
			{
				this._callContext = null;
			}
			else if (logicalCallContext.HasInfo)
			{
				if (arrayList == null)
				{
					arrayList = new ArrayList();
				}
				this._callContext = new MessageSmuggler.SerializedArg(arrayList.Count);
				arrayList.Add(logicalCallContext);
			}
			else
			{
				this._callContext = logicalCallContext.RemotingData.LogicalCallID;
			}
			this._args = MessageSmuggler.FixupArgs(mcm.Args, ref arrayList);
			if (arrayList != null)
			{
				MemoryStream memoryStream = CrossAppDomainSerializer.SerializeMessageParts(arrayList);
				this._serializedArgs = memoryStream.GetBuffer();
			}
		}

		// Token: 0x06005C35 RID: 23605 RVA: 0x001434AC File Offset: 0x001416AC
		[SecurityCritical]
		internal ArrayList FixupForNewAppDomain()
		{
			ArrayList result = null;
			if (this._serializedArgs != null)
			{
				result = CrossAppDomainSerializer.DeserializeMessageParts(new MemoryStream(this._serializedArgs));
				this._serializedArgs = null;
			}
			return result;
		}

		// Token: 0x17000FD9 RID: 4057
		// (get) Token: 0x06005C36 RID: 23606 RVA: 0x001434DC File Offset: 0x001416DC
		internal string Uri
		{
			get
			{
				return this._uri;
			}
		}

		// Token: 0x17000FDA RID: 4058
		// (get) Token: 0x06005C37 RID: 23607 RVA: 0x001434E4 File Offset: 0x001416E4
		internal string MethodName
		{
			get
			{
				return this._methodName;
			}
		}

		// Token: 0x17000FDB RID: 4059
		// (get) Token: 0x06005C38 RID: 23608 RVA: 0x001434EC File Offset: 0x001416EC
		internal string TypeName
		{
			get
			{
				return this._typeName;
			}
		}

		// Token: 0x06005C39 RID: 23609 RVA: 0x001434F4 File Offset: 0x001416F4
		internal Type[] GetInstantiation(ArrayList deserializedArgs)
		{
			if (this._instantiation != null)
			{
				return (Type[])deserializedArgs[this._instantiation.Index];
			}
			return null;
		}

		// Token: 0x06005C3A RID: 23610 RVA: 0x00143516 File Offset: 0x00141716
		internal object[] GetMethodSignature(ArrayList deserializedArgs)
		{
			if (this._methodSignature != null)
			{
				return (object[])deserializedArgs[this._methodSignature.Index];
			}
			return null;
		}

		// Token: 0x06005C3B RID: 23611 RVA: 0x00143538 File Offset: 0x00141738
		[SecurityCritical]
		internal object[] GetArgs(ArrayList deserializedArgs)
		{
			return MessageSmuggler.UndoFixupArgs(this._args, deserializedArgs);
		}

		// Token: 0x06005C3C RID: 23612 RVA: 0x00143548 File Offset: 0x00141748
		[SecurityCritical]
		internal LogicalCallContext GetCallContext(ArrayList deserializedArgs)
		{
			if (this._callContext == null)
			{
				return null;
			}
			if (this._callContext is string)
			{
				return new LogicalCallContext
				{
					RemotingData = 
					{
						LogicalCallID = (string)this._callContext
					}
				};
			}
			return (LogicalCallContext)deserializedArgs[((MessageSmuggler.SerializedArg)this._callContext).Index];
		}

		// Token: 0x17000FDC RID: 4060
		// (get) Token: 0x06005C3D RID: 23613 RVA: 0x001435A5 File Offset: 0x001417A5
		internal int MessagePropertyCount
		{
			get
			{
				return this._propertyCount;
			}
		}

		// Token: 0x06005C3E RID: 23614 RVA: 0x001435B0 File Offset: 0x001417B0
		internal void PopulateMessageProperties(IDictionary dict, ArrayList deserializedArgs)
		{
			for (int i = 0; i < this._propertyCount; i++)
			{
				DictionaryEntry dictionaryEntry = (DictionaryEntry)deserializedArgs[i];
				dict[dictionaryEntry.Key] = dictionaryEntry.Value;
			}
		}

		// Token: 0x040029A0 RID: 10656
		private string _uri;

		// Token: 0x040029A1 RID: 10657
		private string _methodName;

		// Token: 0x040029A2 RID: 10658
		private string _typeName;

		// Token: 0x040029A3 RID: 10659
		private object[] _args;

		// Token: 0x040029A4 RID: 10660
		private byte[] _serializedArgs;

		// Token: 0x040029A5 RID: 10661
		private MessageSmuggler.SerializedArg _methodSignature;

		// Token: 0x040029A6 RID: 10662
		private MessageSmuggler.SerializedArg _instantiation;

		// Token: 0x040029A7 RID: 10663
		private object _callContext;

		// Token: 0x040029A8 RID: 10664
		private int _propertyCount;
	}
}
