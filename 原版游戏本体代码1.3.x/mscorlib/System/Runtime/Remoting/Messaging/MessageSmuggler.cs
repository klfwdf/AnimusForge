using System;
using System.Collections;
using System.Runtime.Remoting.Proxies;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000875 RID: 2165
	internal class MessageSmuggler
	{
		// Token: 0x06005C29 RID: 23593 RVA: 0x00143029 File Offset: 0x00141229
		private static bool CanSmuggleObjectDirectly(object obj)
		{
			return obj is string || obj.GetType() == typeof(void) || obj.GetType().IsPrimitive;
		}

		// Token: 0x06005C2A RID: 23594 RVA: 0x0014305C File Offset: 0x0014125C
		[SecurityCritical]
		protected static object[] FixupArgs(object[] args, ref ArrayList argsToSerialize)
		{
			object[] array = new object[args.Length];
			int num = args.Length;
			for (int i = 0; i < num; i++)
			{
				array[i] = MessageSmuggler.FixupArg(args[i], ref argsToSerialize);
			}
			return array;
		}

		// Token: 0x06005C2B RID: 23595 RVA: 0x00143090 File Offset: 0x00141290
		[SecurityCritical]
		protected static object FixupArg(object arg, ref ArrayList argsToSerialize)
		{
			if (arg == null)
			{
				return null;
			}
			MarshalByRefObject marshalByRefObject = arg as MarshalByRefObject;
			int count;
			if (marshalByRefObject != null)
			{
				if (!RemotingServices.IsTransparentProxy(marshalByRefObject) || RemotingServices.GetRealProxy(marshalByRefObject) is RemotingProxy)
				{
					ObjRef objRef = RemotingServices.MarshalInternal(marshalByRefObject, null, null);
					if (objRef.CanSmuggle())
					{
						if (!RemotingServices.IsTransparentProxy(marshalByRefObject))
						{
							ServerIdentity serverIdentity = (ServerIdentity)MarshalByRefObject.GetIdentity(marshalByRefObject);
							serverIdentity.SetHandle();
							objRef.SetServerIdentity(serverIdentity.GetHandle());
							objRef.SetDomainID(AppDomain.CurrentDomain.GetId());
						}
						ObjRef objRef2 = objRef.CreateSmuggleableCopy();
						objRef2.SetMarshaledObject();
						return new SmuggledObjRef(objRef2);
					}
				}
				if (argsToSerialize == null)
				{
					argsToSerialize = new ArrayList();
				}
				count = argsToSerialize.Count;
				argsToSerialize.Add(arg);
				return new MessageSmuggler.SerializedArg(count);
			}
			if (MessageSmuggler.CanSmuggleObjectDirectly(arg))
			{
				return arg;
			}
			Array array = arg as Array;
			if (array != null)
			{
				Type elementType = array.GetType().GetElementType();
				if (elementType.IsPrimitive || elementType == typeof(string))
				{
					return array.Clone();
				}
			}
			if (argsToSerialize == null)
			{
				argsToSerialize = new ArrayList();
			}
			count = argsToSerialize.Count;
			argsToSerialize.Add(arg);
			return new MessageSmuggler.SerializedArg(count);
		}

		// Token: 0x06005C2C RID: 23596 RVA: 0x001431B0 File Offset: 0x001413B0
		[SecurityCritical]
		protected static object[] UndoFixupArgs(object[] args, ArrayList deserializedArgs)
		{
			object[] array = new object[args.Length];
			int num = args.Length;
			for (int i = 0; i < num; i++)
			{
				array[i] = MessageSmuggler.UndoFixupArg(args[i], deserializedArgs);
			}
			return array;
		}

		// Token: 0x06005C2D RID: 23597 RVA: 0x001431E4 File Offset: 0x001413E4
		[SecurityCritical]
		protected static object UndoFixupArg(object arg, ArrayList deserializedArgs)
		{
			SmuggledObjRef smuggledObjRef = arg as SmuggledObjRef;
			if (smuggledObjRef != null)
			{
				return smuggledObjRef.ObjRef.GetRealObjectHelper();
			}
			MessageSmuggler.SerializedArg serializedArg = arg as MessageSmuggler.SerializedArg;
			if (serializedArg != null)
			{
				return deserializedArgs[serializedArg.Index];
			}
			return arg;
		}

		// Token: 0x06005C2E RID: 23598 RVA: 0x00143220 File Offset: 0x00141420
		[SecurityCritical]
		protected static int StoreUserPropertiesForMethodMessage(IMethodMessage msg, ref ArrayList argsToSerialize)
		{
			IDictionary properties = msg.Properties;
			MessageDictionary messageDictionary = properties as MessageDictionary;
			if (messageDictionary == null)
			{
				int num = 0;
				foreach (object obj in properties)
				{
					DictionaryEntry dictionaryEntry = (DictionaryEntry)obj;
					if (argsToSerialize == null)
					{
						argsToSerialize = new ArrayList();
					}
					argsToSerialize.Add(dictionaryEntry);
					num++;
				}
				return num;
			}
			if (messageDictionary.HasUserData())
			{
				int num2 = 0;
				foreach (object obj2 in messageDictionary.InternalDictionary)
				{
					DictionaryEntry dictionaryEntry2 = (DictionaryEntry)obj2;
					if (argsToSerialize == null)
					{
						argsToSerialize = new ArrayList();
					}
					argsToSerialize.Add(dictionaryEntry2);
					num2++;
				}
				return num2;
			}
			return 0;
		}

		// Token: 0x02000C7B RID: 3195
		protected class SerializedArg
		{
			// Token: 0x060070C0 RID: 28864 RVA: 0x00184CBD File Offset: 0x00182EBD
			public SerializedArg(int index)
			{
				this._index = index;
			}

			// Token: 0x17001355 RID: 4949
			// (get) Token: 0x060070C1 RID: 28865 RVA: 0x00184CCC File Offset: 0x00182ECC
			public int Index
			{
				get
				{
					return this._index;
				}
			}

			// Token: 0x0400380D RID: 14349
			private int _index;
		}
	}
}
