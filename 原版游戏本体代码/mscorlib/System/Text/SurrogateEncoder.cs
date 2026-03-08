using System;
using System.Runtime.Serialization;
using System.Security;

namespace System.Text
{
	// Token: 0x02000A7F RID: 2687
	[Serializable]
	internal sealed class SurrogateEncoder : ISerializable, IObjectReference
	{
		// Token: 0x060068DB RID: 26843 RVA: 0x001640FB File Offset: 0x001622FB
		internal SurrogateEncoder(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			this.realEncoding = (Encoding)info.GetValue("m_encoding", typeof(Encoding));
		}

		// Token: 0x060068DC RID: 26844 RVA: 0x00164131 File Offset: 0x00162331
		[SecurityCritical]
		public object GetRealObject(StreamingContext context)
		{
			return this.realEncoding.GetEncoder();
		}

		// Token: 0x060068DD RID: 26845 RVA: 0x0016413E File Offset: 0x0016233E
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new ArgumentException(Environment.GetResourceString("Arg_ExecutionEngineException"));
		}

		// Token: 0x04002F0E RID: 12046
		[NonSerialized]
		private Encoding realEncoding;
	}
}
