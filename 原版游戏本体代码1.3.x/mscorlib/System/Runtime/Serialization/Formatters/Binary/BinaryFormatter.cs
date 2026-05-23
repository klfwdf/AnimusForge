using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000779 RID: 1913
	[ComVisible(true)]
	public sealed class BinaryFormatter : IRemotingFormatter, IFormatter
	{
		// Token: 0x17000DCE RID: 3534
		// (get) Token: 0x06005337 RID: 21303 RVA: 0x00123FF8 File Offset: 0x001221F8
		// (set) Token: 0x06005338 RID: 21304 RVA: 0x00124000 File Offset: 0x00122200
		public FormatterTypeStyle TypeFormat
		{
			get
			{
				return this.m_typeFormat;
			}
			set
			{
				this.m_typeFormat = value;
			}
		}

		// Token: 0x17000DCF RID: 3535
		// (get) Token: 0x06005339 RID: 21305 RVA: 0x00124009 File Offset: 0x00122209
		// (set) Token: 0x0600533A RID: 21306 RVA: 0x00124011 File Offset: 0x00122211
		public FormatterAssemblyStyle AssemblyFormat
		{
			get
			{
				return this.m_assemblyFormat;
			}
			set
			{
				this.m_assemblyFormat = value;
			}
		}

		// Token: 0x17000DD0 RID: 3536
		// (get) Token: 0x0600533B RID: 21307 RVA: 0x0012401A File Offset: 0x0012221A
		// (set) Token: 0x0600533C RID: 21308 RVA: 0x00124022 File Offset: 0x00122222
		public TypeFilterLevel FilterLevel
		{
			get
			{
				return this.m_securityLevel;
			}
			set
			{
				this.m_securityLevel = value;
			}
		}

		// Token: 0x17000DD1 RID: 3537
		// (get) Token: 0x0600533D RID: 21309 RVA: 0x0012402B File Offset: 0x0012222B
		// (set) Token: 0x0600533E RID: 21310 RVA: 0x00124033 File Offset: 0x00122233
		public ISurrogateSelector SurrogateSelector
		{
			get
			{
				return this.m_surrogates;
			}
			set
			{
				this.m_surrogates = value;
			}
		}

		// Token: 0x17000DD2 RID: 3538
		// (get) Token: 0x0600533F RID: 21311 RVA: 0x0012403C File Offset: 0x0012223C
		// (set) Token: 0x06005340 RID: 21312 RVA: 0x00124044 File Offset: 0x00122244
		public SerializationBinder Binder
		{
			get
			{
				return this.m_binder;
			}
			set
			{
				this.m_binder = value;
			}
		}

		// Token: 0x17000DD3 RID: 3539
		// (get) Token: 0x06005341 RID: 21313 RVA: 0x0012404D File Offset: 0x0012224D
		// (set) Token: 0x06005342 RID: 21314 RVA: 0x00124055 File Offset: 0x00122255
		public StreamingContext Context
		{
			get
			{
				return this.m_context;
			}
			set
			{
				this.m_context = value;
			}
		}

		// Token: 0x06005343 RID: 21315 RVA: 0x0012405E File Offset: 0x0012225E
		public BinaryFormatter()
		{
			this.m_surrogates = null;
			this.m_context = new StreamingContext(StreamingContextStates.All);
		}

		// Token: 0x06005344 RID: 21316 RVA: 0x0012408B File Offset: 0x0012228B
		public BinaryFormatter(ISurrogateSelector selector, StreamingContext context)
		{
			this.m_surrogates = selector;
			this.m_context = context;
		}

		// Token: 0x06005345 RID: 21317 RVA: 0x001240AF File Offset: 0x001222AF
		public object Deserialize(Stream serializationStream)
		{
			return this.Deserialize(serializationStream, null);
		}

		// Token: 0x06005346 RID: 21318 RVA: 0x001240B9 File Offset: 0x001222B9
		[SecurityCritical]
		internal object Deserialize(Stream serializationStream, HeaderHandler handler, bool fCheck)
		{
			return this.Deserialize(serializationStream, handler, fCheck, null);
		}

		// Token: 0x06005347 RID: 21319 RVA: 0x001240C5 File Offset: 0x001222C5
		[SecuritySafeCritical]
		public object Deserialize(Stream serializationStream, HeaderHandler handler)
		{
			return this.Deserialize(serializationStream, handler, true);
		}

		// Token: 0x06005348 RID: 21320 RVA: 0x001240D0 File Offset: 0x001222D0
		[SecuritySafeCritical]
		public object DeserializeMethodResponse(Stream serializationStream, HeaderHandler handler, IMethodCallMessage methodCallMessage)
		{
			return this.Deserialize(serializationStream, handler, true, methodCallMessage);
		}

		// Token: 0x06005349 RID: 21321 RVA: 0x001240DC File Offset: 0x001222DC
		[SecurityCritical]
		[ComVisible(false)]
		public object UnsafeDeserialize(Stream serializationStream, HeaderHandler handler)
		{
			return this.Deserialize(serializationStream, handler, false);
		}

		// Token: 0x0600534A RID: 21322 RVA: 0x001240E7 File Offset: 0x001222E7
		[SecurityCritical]
		[ComVisible(false)]
		public object UnsafeDeserializeMethodResponse(Stream serializationStream, HeaderHandler handler, IMethodCallMessage methodCallMessage)
		{
			return this.Deserialize(serializationStream, handler, false, methodCallMessage);
		}

		// Token: 0x0600534B RID: 21323 RVA: 0x001240F3 File Offset: 0x001222F3
		[SecurityCritical]
		internal object Deserialize(Stream serializationStream, HeaderHandler handler, bool fCheck, IMethodCallMessage methodCallMessage)
		{
			return this.Deserialize(serializationStream, handler, fCheck, false, methodCallMessage);
		}

		// Token: 0x0600534C RID: 21324 RVA: 0x00124104 File Offset: 0x00122304
		[SecurityCritical]
		internal object Deserialize(Stream serializationStream, HeaderHandler handler, bool fCheck, bool isCrossAppDomain, IMethodCallMessage methodCallMessage)
		{
			if (serializationStream == null)
			{
				throw new ArgumentNullException("serializationStream", Environment.GetResourceString("ArgumentNull_WithParamName", new object[] { serializationStream }));
			}
			if (serializationStream.CanSeek && serializationStream.Length == 0L)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_Stream"));
			}
			InternalFE internalFE = new InternalFE();
			internalFE.FEtypeFormat = this.m_typeFormat;
			internalFE.FEserializerTypeEnum = InternalSerializerTypeE.Binary;
			internalFE.FEassemblyFormat = this.m_assemblyFormat;
			internalFE.FEsecurityLevel = this.m_securityLevel;
			ObjectReader objectReader = new ObjectReader(serializationStream, this.m_surrogates, this.m_context, internalFE, this.m_binder);
			objectReader.crossAppDomainArray = this.m_crossAppDomainArray;
			return objectReader.Deserialize(handler, new __BinaryParser(serializationStream, objectReader), fCheck, isCrossAppDomain, methodCallMessage);
		}

		// Token: 0x0600534D RID: 21325 RVA: 0x001241BD File Offset: 0x001223BD
		public void Serialize(Stream serializationStream, object graph)
		{
			this.Serialize(serializationStream, graph, null);
		}

		// Token: 0x0600534E RID: 21326 RVA: 0x001241C8 File Offset: 0x001223C8
		[SecuritySafeCritical]
		public void Serialize(Stream serializationStream, object graph, Header[] headers)
		{
			this.Serialize(serializationStream, graph, headers, true);
		}

		// Token: 0x0600534F RID: 21327 RVA: 0x001241D4 File Offset: 0x001223D4
		[SecurityCritical]
		internal void Serialize(Stream serializationStream, object graph, Header[] headers, bool fCheck)
		{
			if (serializationStream == null)
			{
				throw new ArgumentNullException("serializationStream", Environment.GetResourceString("ArgumentNull_WithParamName", new object[] { serializationStream }));
			}
			InternalFE internalFE = new InternalFE();
			internalFE.FEtypeFormat = this.m_typeFormat;
			internalFE.FEserializerTypeEnum = InternalSerializerTypeE.Binary;
			internalFE.FEassemblyFormat = this.m_assemblyFormat;
			ObjectWriter objectWriter = new ObjectWriter(this.m_surrogates, this.m_context, internalFE, this.m_binder);
			__BinaryWriter serWriter = new __BinaryWriter(serializationStream, objectWriter, this.m_typeFormat);
			objectWriter.Serialize(graph, headers, serWriter, fCheck);
			this.m_crossAppDomainArray = objectWriter.crossAppDomainArray;
		}

		// Token: 0x06005350 RID: 21328 RVA: 0x00124268 File Offset: 0x00122468
		internal static TypeInformation GetTypeInformation(Type type)
		{
			if (AppContextSwitches.UseConcurrentFormatterTypeCache)
			{
				return BinaryFormatter.concurrentTypeNameCache.Value.GetOrAdd(type, delegate(Type t)
				{
					bool hasTypeForwardedFrom2;
					string clrAssemblyName2 = FormatterServices.GetClrAssemblyName(t, out hasTypeForwardedFrom2);
					return new TypeInformation(FormatterServices.GetClrTypeFullName(t), clrAssemblyName2, hasTypeForwardedFrom2);
				});
			}
			Dictionary<Type, TypeInformation> obj = BinaryFormatter.typeNameCache;
			TypeInformation result;
			lock (obj)
			{
				TypeInformation typeInformation = null;
				if (!BinaryFormatter.typeNameCache.TryGetValue(type, out typeInformation))
				{
					bool hasTypeForwardedFrom;
					string clrAssemblyName = FormatterServices.GetClrAssemblyName(type, out hasTypeForwardedFrom);
					typeInformation = new TypeInformation(FormatterServices.GetClrTypeFullName(type), clrAssemblyName, hasTypeForwardedFrom);
					BinaryFormatter.typeNameCache.Add(type, typeInformation);
				}
				result = typeInformation;
			}
			return result;
		}

		// Token: 0x0400257B RID: 9595
		internal ISurrogateSelector m_surrogates;

		// Token: 0x0400257C RID: 9596
		internal StreamingContext m_context;

		// Token: 0x0400257D RID: 9597
		internal SerializationBinder m_binder;

		// Token: 0x0400257E RID: 9598
		internal FormatterTypeStyle m_typeFormat = FormatterTypeStyle.TypesAlways;

		// Token: 0x0400257F RID: 9599
		internal FormatterAssemblyStyle m_assemblyFormat;

		// Token: 0x04002580 RID: 9600
		internal TypeFilterLevel m_securityLevel = TypeFilterLevel.Full;

		// Token: 0x04002581 RID: 9601
		internal object[] m_crossAppDomainArray;

		// Token: 0x04002582 RID: 9602
		private static Dictionary<Type, TypeInformation> typeNameCache = new Dictionary<Type, TypeInformation>();

		// Token: 0x04002583 RID: 9603
		private static Lazy<ConcurrentDictionary<Type, TypeInformation>> concurrentTypeNameCache = new Lazy<ConcurrentDictionary<Type, TypeInformation>>(() => new ConcurrentDictionary<Type, TypeInformation>());
	}
}
