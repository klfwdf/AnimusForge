using System;
using System.Runtime.Serialization;
using System.Security;

namespace System.Text
{
	// Token: 0x02000A5C RID: 2652
	[Serializable]
	internal sealed class CodePageEncoding : ISerializable, IObjectReference
	{
		// Token: 0x0600675E RID: 26462 RVA: 0x0015D2D8 File Offset: 0x0015B4D8
		internal CodePageEncoding(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			this.m_codePage = (int)info.GetValue("m_codePage", typeof(int));
			try
			{
				this.m_isReadOnly = (bool)info.GetValue("m_isReadOnly", typeof(bool));
				this.encoderFallback = (EncoderFallback)info.GetValue("encoderFallback", typeof(EncoderFallback));
				this.decoderFallback = (DecoderFallback)info.GetValue("decoderFallback", typeof(DecoderFallback));
			}
			catch (SerializationException)
			{
				this.m_deserializedFromEverett = true;
				this.m_isReadOnly = true;
			}
		}

		// Token: 0x0600675F RID: 26463 RVA: 0x0015D39C File Offset: 0x0015B59C
		[SecurityCritical]
		public object GetRealObject(StreamingContext context)
		{
			this.realEncoding = Encoding.GetEncoding(this.m_codePage);
			if (!this.m_deserializedFromEverett && !this.m_isReadOnly)
			{
				this.realEncoding = (Encoding)this.realEncoding.Clone();
				this.realEncoding.EncoderFallback = this.encoderFallback;
				this.realEncoding.DecoderFallback = this.decoderFallback;
			}
			return this.realEncoding;
		}

		// Token: 0x06006760 RID: 26464 RVA: 0x0015D408 File Offset: 0x0015B608
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new ArgumentException(Environment.GetResourceString("Arg_ExecutionEngineException"));
		}

		// Token: 0x04002E35 RID: 11829
		[NonSerialized]
		private int m_codePage;

		// Token: 0x04002E36 RID: 11830
		[NonSerialized]
		private bool m_isReadOnly;

		// Token: 0x04002E37 RID: 11831
		[NonSerialized]
		private bool m_deserializedFromEverett;

		// Token: 0x04002E38 RID: 11832
		[NonSerialized]
		private EncoderFallback encoderFallback;

		// Token: 0x04002E39 RID: 11833
		[NonSerialized]
		private DecoderFallback decoderFallback;

		// Token: 0x04002E3A RID: 11834
		[NonSerialized]
		private Encoding realEncoding;

		// Token: 0x02000CAF RID: 3247
		[Serializable]
		internal sealed class Decoder : ISerializable, IObjectReference
		{
			// Token: 0x06007157 RID: 29015 RVA: 0x0018606F File Offset: 0x0018426F
			internal Decoder(SerializationInfo info, StreamingContext context)
			{
				if (info == null)
				{
					throw new ArgumentNullException("info");
				}
				this.realEncoding = (Encoding)info.GetValue("encoding", typeof(Encoding));
			}

			// Token: 0x06007158 RID: 29016 RVA: 0x001860A5 File Offset: 0x001842A5
			[SecurityCritical]
			public object GetRealObject(StreamingContext context)
			{
				return this.realEncoding.GetDecoder();
			}

			// Token: 0x06007159 RID: 29017 RVA: 0x001860B2 File Offset: 0x001842B2
			[SecurityCritical]
			void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_ExecutionEngineException"));
			}

			// Token: 0x040038A4 RID: 14500
			[NonSerialized]
			private Encoding realEncoding;
		}
	}
}
