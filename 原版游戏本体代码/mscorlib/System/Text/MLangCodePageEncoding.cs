using System;
using System.Runtime.Serialization;
using System.Security;

namespace System.Text
{
	// Token: 0x02000A79 RID: 2681
	[Serializable]
	internal sealed class MLangCodePageEncoding : ISerializable, IObjectReference
	{
		// Token: 0x060068B3 RID: 26803 RVA: 0x001621C4 File Offset: 0x001603C4
		internal MLangCodePageEncoding(SerializationInfo info, StreamingContext context)
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

		// Token: 0x060068B4 RID: 26804 RVA: 0x00162288 File Offset: 0x00160488
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

		// Token: 0x060068B5 RID: 26805 RVA: 0x001622F4 File Offset: 0x001604F4
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new ArgumentException(Environment.GetResourceString("Arg_ExecutionEngineException"));
		}

		// Token: 0x04002ED7 RID: 11991
		[NonSerialized]
		private int m_codePage;

		// Token: 0x04002ED8 RID: 11992
		[NonSerialized]
		private bool m_isReadOnly;

		// Token: 0x04002ED9 RID: 11993
		[NonSerialized]
		private bool m_deserializedFromEverett;

		// Token: 0x04002EDA RID: 11994
		[NonSerialized]
		private EncoderFallback encoderFallback;

		// Token: 0x04002EDB RID: 11995
		[NonSerialized]
		private DecoderFallback decoderFallback;

		// Token: 0x04002EDC RID: 11996
		[NonSerialized]
		private Encoding realEncoding;

		// Token: 0x02000CB6 RID: 3254
		[Serializable]
		internal sealed class MLangEncoder : ISerializable, IObjectReference
		{
			// Token: 0x0600718E RID: 29070 RVA: 0x00186A7C File Offset: 0x00184C7C
			internal MLangEncoder(SerializationInfo info, StreamingContext context)
			{
				if (info == null)
				{
					throw new ArgumentNullException("info");
				}
				this.realEncoding = (Encoding)info.GetValue("m_encoding", typeof(Encoding));
			}

			// Token: 0x0600718F RID: 29071 RVA: 0x00186AB2 File Offset: 0x00184CB2
			[SecurityCritical]
			public object GetRealObject(StreamingContext context)
			{
				return this.realEncoding.GetEncoder();
			}

			// Token: 0x06007190 RID: 29072 RVA: 0x00186ABF File Offset: 0x00184CBF
			[SecurityCritical]
			void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_ExecutionEngineException"));
			}

			// Token: 0x040038C7 RID: 14535
			[NonSerialized]
			private Encoding realEncoding;
		}

		// Token: 0x02000CB7 RID: 3255
		[Serializable]
		internal sealed class MLangDecoder : ISerializable, IObjectReference
		{
			// Token: 0x06007191 RID: 29073 RVA: 0x00186AD0 File Offset: 0x00184CD0
			internal MLangDecoder(SerializationInfo info, StreamingContext context)
			{
				if (info == null)
				{
					throw new ArgumentNullException("info");
				}
				this.realEncoding = (Encoding)info.GetValue("m_encoding", typeof(Encoding));
			}

			// Token: 0x06007192 RID: 29074 RVA: 0x00186B06 File Offset: 0x00184D06
			[SecurityCritical]
			public object GetRealObject(StreamingContext context)
			{
				return this.realEncoding.GetDecoder();
			}

			// Token: 0x06007193 RID: 29075 RVA: 0x00186B13 File Offset: 0x00184D13
			[SecurityCritical]
			void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_ExecutionEngineException"));
			}

			// Token: 0x040038C8 RID: 14536
			[NonSerialized]
			private Encoding realEncoding;
		}
	}
}
