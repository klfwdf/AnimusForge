using System;
using System.Collections;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000878 RID: 2168
	internal class SmuggledMethodReturnMessage : MessageSmuggler
	{
		// Token: 0x06005C3F RID: 23615 RVA: 0x001435F0 File Offset: 0x001417F0
		[SecurityCritical]
		internal static SmuggledMethodReturnMessage SmuggleIfPossible(IMessage msg)
		{
			IMethodReturnMessage methodReturnMessage = msg as IMethodReturnMessage;
			if (methodReturnMessage == null)
			{
				return null;
			}
			return new SmuggledMethodReturnMessage(methodReturnMessage);
		}

		// Token: 0x06005C40 RID: 23616 RVA: 0x0014360F File Offset: 0x0014180F
		private SmuggledMethodReturnMessage()
		{
		}

		// Token: 0x06005C41 RID: 23617 RVA: 0x00143618 File Offset: 0x00141818
		[SecurityCritical]
		private SmuggledMethodReturnMessage(IMethodReturnMessage mrm)
		{
			ArrayList arrayList = null;
			ReturnMessage returnMessage = mrm as ReturnMessage;
			if (returnMessage == null || returnMessage.HasProperties())
			{
				this._propertyCount = MessageSmuggler.StoreUserPropertiesForMethodMessage(mrm, ref arrayList);
			}
			Exception exception = mrm.Exception;
			if (exception != null)
			{
				if (arrayList == null)
				{
					arrayList = new ArrayList();
				}
				this._exception = new MessageSmuggler.SerializedArg(arrayList.Count);
				arrayList.Add(exception);
			}
			LogicalCallContext logicalCallContext = mrm.LogicalCallContext;
			if (logicalCallContext == null)
			{
				this._callContext = null;
			}
			else if (logicalCallContext.HasInfo)
			{
				if (logicalCallContext.Principal != null)
				{
					logicalCallContext.Principal = null;
				}
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
			this._returnValue = MessageSmuggler.FixupArg(mrm.ReturnValue, ref arrayList);
			this._args = MessageSmuggler.FixupArgs(mrm.Args, ref arrayList);
			if (arrayList != null)
			{
				MemoryStream memoryStream = CrossAppDomainSerializer.SerializeMessageParts(arrayList);
				this._serializedArgs = memoryStream.GetBuffer();
			}
		}

		// Token: 0x06005C42 RID: 23618 RVA: 0x00143718 File Offset: 0x00141918
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

		// Token: 0x06005C43 RID: 23619 RVA: 0x00143748 File Offset: 0x00141948
		[SecurityCritical]
		internal object GetReturnValue(ArrayList deserializedArgs)
		{
			return MessageSmuggler.UndoFixupArg(this._returnValue, deserializedArgs);
		}

		// Token: 0x06005C44 RID: 23620 RVA: 0x00143758 File Offset: 0x00141958
		[SecurityCritical]
		internal object[] GetArgs(ArrayList deserializedArgs)
		{
			return MessageSmuggler.UndoFixupArgs(this._args, deserializedArgs);
		}

		// Token: 0x06005C45 RID: 23621 RVA: 0x00143773 File Offset: 0x00141973
		internal Exception GetException(ArrayList deserializedArgs)
		{
			if (this._exception != null)
			{
				return (Exception)deserializedArgs[this._exception.Index];
			}
			return null;
		}

		// Token: 0x06005C46 RID: 23622 RVA: 0x00143798 File Offset: 0x00141998
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

		// Token: 0x17000FDD RID: 4061
		// (get) Token: 0x06005C47 RID: 23623 RVA: 0x001437F5 File Offset: 0x001419F5
		internal int MessagePropertyCount
		{
			get
			{
				return this._propertyCount;
			}
		}

		// Token: 0x06005C48 RID: 23624 RVA: 0x00143800 File Offset: 0x00141A00
		internal void PopulateMessageProperties(IDictionary dict, ArrayList deserializedArgs)
		{
			for (int i = 0; i < this._propertyCount; i++)
			{
				DictionaryEntry dictionaryEntry = (DictionaryEntry)deserializedArgs[i];
				dict[dictionaryEntry.Key] = dictionaryEntry.Value;
			}
		}

		// Token: 0x040029A9 RID: 10665
		private object[] _args;

		// Token: 0x040029AA RID: 10666
		private object _returnValue;

		// Token: 0x040029AB RID: 10667
		private byte[] _serializedArgs;

		// Token: 0x040029AC RID: 10668
		private MessageSmuggler.SerializedArg _exception;

		// Token: 0x040029AD RID: 10669
		private object _callContext;

		// Token: 0x040029AE RID: 10670
		private int _propertyCount;
	}
}
