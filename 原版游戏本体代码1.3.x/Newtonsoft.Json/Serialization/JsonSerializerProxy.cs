using System;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	// Token: 0x02000098 RID: 152
	[NullableContext(1)]
	[Nullable(0)]
	internal class JsonSerializerProxy : JsonSerializer
	{
		// Token: 0x14000003 RID: 3
		// (add) Token: 0x060007C6 RID: 1990 RVA: 0x000234F1 File Offset: 0x000216F1
		// (remove) Token: 0x060007C7 RID: 1991 RVA: 0x000234FF File Offset: 0x000216FF
		[Nullable(new byte[] { 2, 1 })]
		public override event EventHandler<ErrorEventArgs> Error
		{
			add
			{
				this._serializer.Error += value;
			}
			remove
			{
				this._serializer.Error -= value;
			}
		}

		// Token: 0x1700013F RID: 319
		// (get) Token: 0x060007C8 RID: 1992 RVA: 0x0002350D File Offset: 0x0002170D
		// (set) Token: 0x060007C9 RID: 1993 RVA: 0x0002351A File Offset: 0x0002171A
		[Nullable(2)]
		public override IReferenceResolver ReferenceResolver
		{
			[NullableContext(2)]
			get
			{
				return this._serializer.ReferenceResolver;
			}
			[NullableContext(2)]
			set
			{
				this._serializer.ReferenceResolver = value;
			}
		}

		// Token: 0x17000140 RID: 320
		// (get) Token: 0x060007CA RID: 1994 RVA: 0x00023528 File Offset: 0x00021728
		// (set) Token: 0x060007CB RID: 1995 RVA: 0x00023535 File Offset: 0x00021735
		[Nullable(2)]
		public override ITraceWriter TraceWriter
		{
			[NullableContext(2)]
			get
			{
				return this._serializer.TraceWriter;
			}
			[NullableContext(2)]
			set
			{
				this._serializer.TraceWriter = value;
			}
		}

		// Token: 0x17000141 RID: 321
		// (get) Token: 0x060007CC RID: 1996 RVA: 0x00023543 File Offset: 0x00021743
		// (set) Token: 0x060007CD RID: 1997 RVA: 0x00023550 File Offset: 0x00021750
		[Nullable(2)]
		public override IEqualityComparer EqualityComparer
		{
			[NullableContext(2)]
			get
			{
				return this._serializer.EqualityComparer;
			}
			[NullableContext(2)]
			set
			{
				this._serializer.EqualityComparer = value;
			}
		}

		// Token: 0x17000142 RID: 322
		// (get) Token: 0x060007CE RID: 1998 RVA: 0x0002355E File Offset: 0x0002175E
		public override JsonConverterCollection Converters
		{
			get
			{
				return this._serializer.Converters;
			}
		}

		// Token: 0x17000143 RID: 323
		// (get) Token: 0x060007CF RID: 1999 RVA: 0x0002356B File Offset: 0x0002176B
		// (set) Token: 0x060007D0 RID: 2000 RVA: 0x00023578 File Offset: 0x00021778
		public override DefaultValueHandling DefaultValueHandling
		{
			get
			{
				return this._serializer.DefaultValueHandling;
			}
			set
			{
				this._serializer.DefaultValueHandling = value;
			}
		}

		// Token: 0x17000144 RID: 324
		// (get) Token: 0x060007D1 RID: 2001 RVA: 0x00023586 File Offset: 0x00021786
		// (set) Token: 0x060007D2 RID: 2002 RVA: 0x00023593 File Offset: 0x00021793
		public override IContractResolver ContractResolver
		{
			get
			{
				return this._serializer.ContractResolver;
			}
			set
			{
				this._serializer.ContractResolver = value;
			}
		}

		// Token: 0x17000145 RID: 325
		// (get) Token: 0x060007D3 RID: 2003 RVA: 0x000235A1 File Offset: 0x000217A1
		// (set) Token: 0x060007D4 RID: 2004 RVA: 0x000235AE File Offset: 0x000217AE
		public override MissingMemberHandling MissingMemberHandling
		{
			get
			{
				return this._serializer.MissingMemberHandling;
			}
			set
			{
				this._serializer.MissingMemberHandling = value;
			}
		}

		// Token: 0x17000146 RID: 326
		// (get) Token: 0x060007D5 RID: 2005 RVA: 0x000235BC File Offset: 0x000217BC
		// (set) Token: 0x060007D6 RID: 2006 RVA: 0x000235C9 File Offset: 0x000217C9
		public override NullValueHandling NullValueHandling
		{
			get
			{
				return this._serializer.NullValueHandling;
			}
			set
			{
				this._serializer.NullValueHandling = value;
			}
		}

		// Token: 0x17000147 RID: 327
		// (get) Token: 0x060007D7 RID: 2007 RVA: 0x000235D7 File Offset: 0x000217D7
		// (set) Token: 0x060007D8 RID: 2008 RVA: 0x000235E4 File Offset: 0x000217E4
		public override ObjectCreationHandling ObjectCreationHandling
		{
			get
			{
				return this._serializer.ObjectCreationHandling;
			}
			set
			{
				this._serializer.ObjectCreationHandling = value;
			}
		}

		// Token: 0x17000148 RID: 328
		// (get) Token: 0x060007D9 RID: 2009 RVA: 0x000235F2 File Offset: 0x000217F2
		// (set) Token: 0x060007DA RID: 2010 RVA: 0x000235FF File Offset: 0x000217FF
		public override ReferenceLoopHandling ReferenceLoopHandling
		{
			get
			{
				return this._serializer.ReferenceLoopHandling;
			}
			set
			{
				this._serializer.ReferenceLoopHandling = value;
			}
		}

		// Token: 0x17000149 RID: 329
		// (get) Token: 0x060007DB RID: 2011 RVA: 0x0002360D File Offset: 0x0002180D
		// (set) Token: 0x060007DC RID: 2012 RVA: 0x0002361A File Offset: 0x0002181A
		public override PreserveReferencesHandling PreserveReferencesHandling
		{
			get
			{
				return this._serializer.PreserveReferencesHandling;
			}
			set
			{
				this._serializer.PreserveReferencesHandling = value;
			}
		}

		// Token: 0x1700014A RID: 330
		// (get) Token: 0x060007DD RID: 2013 RVA: 0x00023628 File Offset: 0x00021828
		// (set) Token: 0x060007DE RID: 2014 RVA: 0x00023635 File Offset: 0x00021835
		public override TypeNameHandling TypeNameHandling
		{
			get
			{
				return this._serializer.TypeNameHandling;
			}
			set
			{
				this._serializer.TypeNameHandling = value;
			}
		}

		// Token: 0x1700014B RID: 331
		// (get) Token: 0x060007DF RID: 2015 RVA: 0x00023643 File Offset: 0x00021843
		// (set) Token: 0x060007E0 RID: 2016 RVA: 0x00023650 File Offset: 0x00021850
		public override MetadataPropertyHandling MetadataPropertyHandling
		{
			get
			{
				return this._serializer.MetadataPropertyHandling;
			}
			set
			{
				this._serializer.MetadataPropertyHandling = value;
			}
		}

		// Token: 0x1700014C RID: 332
		// (get) Token: 0x060007E1 RID: 2017 RVA: 0x0002365E File Offset: 0x0002185E
		// (set) Token: 0x060007E2 RID: 2018 RVA: 0x0002366B File Offset: 0x0002186B
		[Obsolete("TypeNameAssemblyFormat is obsolete. Use TypeNameAssemblyFormatHandling instead.")]
		public override FormatterAssemblyStyle TypeNameAssemblyFormat
		{
			get
			{
				return this._serializer.TypeNameAssemblyFormat;
			}
			set
			{
				this._serializer.TypeNameAssemblyFormat = value;
			}
		}

		// Token: 0x1700014D RID: 333
		// (get) Token: 0x060007E3 RID: 2019 RVA: 0x00023679 File Offset: 0x00021879
		// (set) Token: 0x060007E4 RID: 2020 RVA: 0x00023686 File Offset: 0x00021886
		public override TypeNameAssemblyFormatHandling TypeNameAssemblyFormatHandling
		{
			get
			{
				return this._serializer.TypeNameAssemblyFormatHandling;
			}
			set
			{
				this._serializer.TypeNameAssemblyFormatHandling = value;
			}
		}

		// Token: 0x1700014E RID: 334
		// (get) Token: 0x060007E5 RID: 2021 RVA: 0x00023694 File Offset: 0x00021894
		// (set) Token: 0x060007E6 RID: 2022 RVA: 0x000236A1 File Offset: 0x000218A1
		public override ConstructorHandling ConstructorHandling
		{
			get
			{
				return this._serializer.ConstructorHandling;
			}
			set
			{
				this._serializer.ConstructorHandling = value;
			}
		}

		// Token: 0x1700014F RID: 335
		// (get) Token: 0x060007E7 RID: 2023 RVA: 0x000236AF File Offset: 0x000218AF
		// (set) Token: 0x060007E8 RID: 2024 RVA: 0x000236BC File Offset: 0x000218BC
		[Obsolete("Binder is obsolete. Use SerializationBinder instead.")]
		public override SerializationBinder Binder
		{
			get
			{
				return this._serializer.Binder;
			}
			set
			{
				this._serializer.Binder = value;
			}
		}

		// Token: 0x17000150 RID: 336
		// (get) Token: 0x060007E9 RID: 2025 RVA: 0x000236CA File Offset: 0x000218CA
		// (set) Token: 0x060007EA RID: 2026 RVA: 0x000236D7 File Offset: 0x000218D7
		public override ISerializationBinder SerializationBinder
		{
			get
			{
				return this._serializer.SerializationBinder;
			}
			set
			{
				this._serializer.SerializationBinder = value;
			}
		}

		// Token: 0x17000151 RID: 337
		// (get) Token: 0x060007EB RID: 2027 RVA: 0x000236E5 File Offset: 0x000218E5
		// (set) Token: 0x060007EC RID: 2028 RVA: 0x000236F2 File Offset: 0x000218F2
		public override StreamingContext Context
		{
			get
			{
				return this._serializer.Context;
			}
			set
			{
				this._serializer.Context = value;
			}
		}

		// Token: 0x17000152 RID: 338
		// (get) Token: 0x060007ED RID: 2029 RVA: 0x00023700 File Offset: 0x00021900
		// (set) Token: 0x060007EE RID: 2030 RVA: 0x0002370D File Offset: 0x0002190D
		public override Formatting Formatting
		{
			get
			{
				return this._serializer.Formatting;
			}
			set
			{
				this._serializer.Formatting = value;
			}
		}

		// Token: 0x17000153 RID: 339
		// (get) Token: 0x060007EF RID: 2031 RVA: 0x0002371B File Offset: 0x0002191B
		// (set) Token: 0x060007F0 RID: 2032 RVA: 0x00023728 File Offset: 0x00021928
		public override DateFormatHandling DateFormatHandling
		{
			get
			{
				return this._serializer.DateFormatHandling;
			}
			set
			{
				this._serializer.DateFormatHandling = value;
			}
		}

		// Token: 0x17000154 RID: 340
		// (get) Token: 0x060007F1 RID: 2033 RVA: 0x00023736 File Offset: 0x00021936
		// (set) Token: 0x060007F2 RID: 2034 RVA: 0x00023743 File Offset: 0x00021943
		public override DateTimeZoneHandling DateTimeZoneHandling
		{
			get
			{
				return this._serializer.DateTimeZoneHandling;
			}
			set
			{
				this._serializer.DateTimeZoneHandling = value;
			}
		}

		// Token: 0x17000155 RID: 341
		// (get) Token: 0x060007F3 RID: 2035 RVA: 0x00023751 File Offset: 0x00021951
		// (set) Token: 0x060007F4 RID: 2036 RVA: 0x0002375E File Offset: 0x0002195E
		public override DateParseHandling DateParseHandling
		{
			get
			{
				return this._serializer.DateParseHandling;
			}
			set
			{
				this._serializer.DateParseHandling = value;
			}
		}

		// Token: 0x17000156 RID: 342
		// (get) Token: 0x060007F5 RID: 2037 RVA: 0x0002376C File Offset: 0x0002196C
		// (set) Token: 0x060007F6 RID: 2038 RVA: 0x00023779 File Offset: 0x00021979
		public override FloatFormatHandling FloatFormatHandling
		{
			get
			{
				return this._serializer.FloatFormatHandling;
			}
			set
			{
				this._serializer.FloatFormatHandling = value;
			}
		}

		// Token: 0x17000157 RID: 343
		// (get) Token: 0x060007F7 RID: 2039 RVA: 0x00023787 File Offset: 0x00021987
		// (set) Token: 0x060007F8 RID: 2040 RVA: 0x00023794 File Offset: 0x00021994
		public override FloatParseHandling FloatParseHandling
		{
			get
			{
				return this._serializer.FloatParseHandling;
			}
			set
			{
				this._serializer.FloatParseHandling = value;
			}
		}

		// Token: 0x17000158 RID: 344
		// (get) Token: 0x060007F9 RID: 2041 RVA: 0x000237A2 File Offset: 0x000219A2
		// (set) Token: 0x060007FA RID: 2042 RVA: 0x000237AF File Offset: 0x000219AF
		public override StringEscapeHandling StringEscapeHandling
		{
			get
			{
				return this._serializer.StringEscapeHandling;
			}
			set
			{
				this._serializer.StringEscapeHandling = value;
			}
		}

		// Token: 0x17000159 RID: 345
		// (get) Token: 0x060007FB RID: 2043 RVA: 0x000237BD File Offset: 0x000219BD
		// (set) Token: 0x060007FC RID: 2044 RVA: 0x000237CA File Offset: 0x000219CA
		public override string DateFormatString
		{
			get
			{
				return this._serializer.DateFormatString;
			}
			set
			{
				this._serializer.DateFormatString = value;
			}
		}

		// Token: 0x1700015A RID: 346
		// (get) Token: 0x060007FD RID: 2045 RVA: 0x000237D8 File Offset: 0x000219D8
		// (set) Token: 0x060007FE RID: 2046 RVA: 0x000237E5 File Offset: 0x000219E5
		public override CultureInfo Culture
		{
			get
			{
				return this._serializer.Culture;
			}
			set
			{
				this._serializer.Culture = value;
			}
		}

		// Token: 0x1700015B RID: 347
		// (get) Token: 0x060007FF RID: 2047 RVA: 0x000237F3 File Offset: 0x000219F3
		// (set) Token: 0x06000800 RID: 2048 RVA: 0x00023800 File Offset: 0x00021A00
		public override int? MaxDepth
		{
			get
			{
				return this._serializer.MaxDepth;
			}
			set
			{
				this._serializer.MaxDepth = value;
			}
		}

		// Token: 0x1700015C RID: 348
		// (get) Token: 0x06000801 RID: 2049 RVA: 0x0002380E File Offset: 0x00021A0E
		// (set) Token: 0x06000802 RID: 2050 RVA: 0x0002381B File Offset: 0x00021A1B
		public override bool CheckAdditionalContent
		{
			get
			{
				return this._serializer.CheckAdditionalContent;
			}
			set
			{
				this._serializer.CheckAdditionalContent = value;
			}
		}

		// Token: 0x06000803 RID: 2051 RVA: 0x00023829 File Offset: 0x00021A29
		internal JsonSerializerInternalBase GetInternalSerializer()
		{
			if (this._serializerReader != null)
			{
				return this._serializerReader;
			}
			return this._serializerWriter;
		}

		// Token: 0x06000804 RID: 2052 RVA: 0x00023840 File Offset: 0x00021A40
		public JsonSerializerProxy(JsonSerializerInternalReader serializerReader)
		{
			ValidationUtils.ArgumentNotNull(serializerReader, "serializerReader");
			this._serializerReader = serializerReader;
			this._serializer = serializerReader.Serializer;
		}

		// Token: 0x06000805 RID: 2053 RVA: 0x00023866 File Offset: 0x00021A66
		public JsonSerializerProxy(JsonSerializerInternalWriter serializerWriter)
		{
			ValidationUtils.ArgumentNotNull(serializerWriter, "serializerWriter");
			this._serializerWriter = serializerWriter;
			this._serializer = serializerWriter.Serializer;
		}

		// Token: 0x06000806 RID: 2054 RVA: 0x0002388C File Offset: 0x00021A8C
		[NullableContext(2)]
		internal override object DeserializeInternal([Nullable(1)] JsonReader reader, Type objectType)
		{
			if (this._serializerReader != null)
			{
				return this._serializerReader.Deserialize(reader, objectType, false);
			}
			return this._serializer.Deserialize(reader, objectType);
		}

		// Token: 0x06000807 RID: 2055 RVA: 0x000238B2 File Offset: 0x00021AB2
		internal override void PopulateInternal(JsonReader reader, object target)
		{
			if (this._serializerReader != null)
			{
				this._serializerReader.Populate(reader, target);
				return;
			}
			this._serializer.Populate(reader, target);
		}

		// Token: 0x06000808 RID: 2056 RVA: 0x000238D7 File Offset: 0x00021AD7
		[NullableContext(2)]
		internal override void SerializeInternal([Nullable(1)] JsonWriter jsonWriter, object value, Type rootType)
		{
			if (this._serializerWriter != null)
			{
				this._serializerWriter.Serialize(jsonWriter, value, rootType);
				return;
			}
			this._serializer.Serialize(jsonWriter, value);
		}

		// Token: 0x040002CD RID: 717
		[Nullable(2)]
		private readonly JsonSerializerInternalReader _serializerReader;

		// Token: 0x040002CE RID: 718
		[Nullable(2)]
		private readonly JsonSerializerInternalWriter _serializerWriter;

		// Token: 0x040002CF RID: 719
		internal readonly JsonSerializer _serializer;
	}
}
