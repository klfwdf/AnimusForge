using System;
using System.Collections;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200083A RID: 2106
	internal static class CrossAppDomainSerializer
	{
		// Token: 0x060059F4 RID: 23028 RVA: 0x0013D400 File Offset: 0x0013B600
		[SecurityCritical]
		internal static MemoryStream SerializeMessage(IMessage msg)
		{
			MemoryStream memoryStream = new MemoryStream();
			RemotingSurrogateSelector surrogateSelector = new RemotingSurrogateSelector();
			new BinaryFormatter
			{
				SurrogateSelector = surrogateSelector,
				Context = new StreamingContext(StreamingContextStates.CrossAppDomain)
			}.Serialize(memoryStream, msg, null, false);
			memoryStream.Position = 0L;
			return memoryStream;
		}

		// Token: 0x060059F5 RID: 23029 RVA: 0x0013D44C File Offset: 0x0013B64C
		[SecurityCritical]
		internal static MemoryStream SerializeMessageParts(ArrayList argsToSerialize)
		{
			MemoryStream memoryStream = new MemoryStream();
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			RemotingSurrogateSelector surrogateSelector = new RemotingSurrogateSelector();
			binaryFormatter.SurrogateSelector = surrogateSelector;
			binaryFormatter.Context = new StreamingContext(StreamingContextStates.CrossAppDomain);
			binaryFormatter.Serialize(memoryStream, argsToSerialize, null, false);
			memoryStream.Position = 0L;
			return memoryStream;
		}

		// Token: 0x060059F6 RID: 23030 RVA: 0x0013D498 File Offset: 0x0013B698
		[SecurityCritical]
		internal static void SerializeObject(object obj, MemoryStream stm)
		{
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			RemotingSurrogateSelector surrogateSelector = new RemotingSurrogateSelector();
			binaryFormatter.SurrogateSelector = surrogateSelector;
			binaryFormatter.Context = new StreamingContext(StreamingContextStates.CrossAppDomain);
			binaryFormatter.Serialize(stm, obj, null, false);
		}

		// Token: 0x060059F7 RID: 23031 RVA: 0x0013D4D4 File Offset: 0x0013B6D4
		[SecurityCritical]
		internal static MemoryStream SerializeObject(object obj)
		{
			MemoryStream memoryStream = new MemoryStream();
			CrossAppDomainSerializer.SerializeObject(obj, memoryStream);
			memoryStream.Position = 0L;
			return memoryStream;
		}

		// Token: 0x060059F8 RID: 23032 RVA: 0x0013D4F7 File Offset: 0x0013B6F7
		[SecurityCritical]
		internal static IMessage DeserializeMessage(MemoryStream stm)
		{
			return CrossAppDomainSerializer.DeserializeMessage(stm, null);
		}

		// Token: 0x060059F9 RID: 23033 RVA: 0x0013D500 File Offset: 0x0013B700
		[SecurityCritical]
		internal static IMessage DeserializeMessage(MemoryStream stm, IMethodCallMessage reqMsg)
		{
			if (stm == null)
			{
				throw new ArgumentNullException("stm");
			}
			stm.Position = 0L;
			return (IMessage)new BinaryFormatter
			{
				SurrogateSelector = null,
				Context = new StreamingContext(StreamingContextStates.CrossAppDomain)
			}.Deserialize(stm, null, false, true, reqMsg);
		}

		// Token: 0x060059FA RID: 23034 RVA: 0x0013D550 File Offset: 0x0013B750
		[SecurityCritical]
		internal static ArrayList DeserializeMessageParts(MemoryStream stm)
		{
			return (ArrayList)CrossAppDomainSerializer.DeserializeObject(stm);
		}

		// Token: 0x060059FB RID: 23035 RVA: 0x0013D560 File Offset: 0x0013B760
		[SecurityCritical]
		internal static object DeserializeObject(MemoryStream stm)
		{
			stm.Position = 0L;
			return new BinaryFormatter
			{
				Context = new StreamingContext(StreamingContextStates.CrossAppDomain)
			}.Deserialize(stm, null, false, true, null);
		}
	}
}
