using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json.Serialization;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies the settings on a <see cref="T:Newtonsoft.Json.JsonSerializer" /> object.
	/// </summary>
	// Token: 0x0200002C RID: 44
	[NullableContext(2)]
	[Nullable(0)]
	public class JsonSerializerSettings
	{
		/// <summary>
		/// Gets or sets how reference loops (e.g. a class referencing itself) are handled.
		/// The default value is <see cref="F:Newtonsoft.Json.ReferenceLoopHandling.Error" />.
		/// </summary>
		/// <value>Reference loop handling.</value>
		// Token: 0x17000060 RID: 96
		// (get) Token: 0x06000197 RID: 407 RVA: 0x00005D41 File Offset: 0x00003F41
		// (set) Token: 0x06000198 RID: 408 RVA: 0x00005D4E File Offset: 0x00003F4E
		public ReferenceLoopHandling ReferenceLoopHandling
		{
			get
			{
				return this._referenceLoopHandling.GetValueOrDefault();
			}
			set
			{
				this._referenceLoopHandling = new ReferenceLoopHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how missing members (e.g. JSON contains a property that isn't a member on the object) are handled during deserialization.
		/// The default value is <see cref="F:Newtonsoft.Json.MissingMemberHandling.Ignore" />.
		/// </summary>
		/// <value>Missing member handling.</value>
		// Token: 0x17000061 RID: 97
		// (get) Token: 0x06000199 RID: 409 RVA: 0x00005D5C File Offset: 0x00003F5C
		// (set) Token: 0x0600019A RID: 410 RVA: 0x00005D69 File Offset: 0x00003F69
		public MissingMemberHandling MissingMemberHandling
		{
			get
			{
				return this._missingMemberHandling.GetValueOrDefault();
			}
			set
			{
				this._missingMemberHandling = new MissingMemberHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how objects are created during deserialization.
		/// The default value is <see cref="F:Newtonsoft.Json.ObjectCreationHandling.Auto" />.
		/// </summary>
		/// <value>The object creation handling.</value>
		// Token: 0x17000062 RID: 98
		// (get) Token: 0x0600019B RID: 411 RVA: 0x00005D77 File Offset: 0x00003F77
		// (set) Token: 0x0600019C RID: 412 RVA: 0x00005D84 File Offset: 0x00003F84
		public ObjectCreationHandling ObjectCreationHandling
		{
			get
			{
				return this._objectCreationHandling.GetValueOrDefault();
			}
			set
			{
				this._objectCreationHandling = new ObjectCreationHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how null values are handled during serialization and deserialization.
		/// The default value is <see cref="F:Newtonsoft.Json.NullValueHandling.Include" />.
		/// </summary>
		/// <value>Null value handling.</value>
		// Token: 0x17000063 RID: 99
		// (get) Token: 0x0600019D RID: 413 RVA: 0x00005D92 File Offset: 0x00003F92
		// (set) Token: 0x0600019E RID: 414 RVA: 0x00005D9F File Offset: 0x00003F9F
		public NullValueHandling NullValueHandling
		{
			get
			{
				return this._nullValueHandling.GetValueOrDefault();
			}
			set
			{
				this._nullValueHandling = new NullValueHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how default values are handled during serialization and deserialization.
		/// The default value is <see cref="F:Newtonsoft.Json.DefaultValueHandling.Include" />.
		/// </summary>
		/// <value>The default value handling.</value>
		// Token: 0x17000064 RID: 100
		// (get) Token: 0x0600019F RID: 415 RVA: 0x00005DAD File Offset: 0x00003FAD
		// (set) Token: 0x060001A0 RID: 416 RVA: 0x00005DBA File Offset: 0x00003FBA
		public DefaultValueHandling DefaultValueHandling
		{
			get
			{
				return this._defaultValueHandling.GetValueOrDefault();
			}
			set
			{
				this._defaultValueHandling = new DefaultValueHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets a <see cref="T:Newtonsoft.Json.JsonConverter" /> collection that will be used during serialization.
		/// </summary>
		/// <value>The converters.</value>
		// Token: 0x17000065 RID: 101
		// (get) Token: 0x060001A1 RID: 417 RVA: 0x00005DC8 File Offset: 0x00003FC8
		// (set) Token: 0x060001A2 RID: 418 RVA: 0x00005DD0 File Offset: 0x00003FD0
		[Nullable(1)]
		public IList<JsonConverter> Converters
		{
			[NullableContext(1)]
			get;
			[NullableContext(1)]
			set;
		}

		/// <summary>
		/// Gets or sets how object references are preserved by the serializer.
		/// The default value is <see cref="F:Newtonsoft.Json.PreserveReferencesHandling.None" />.
		/// </summary>
		/// <value>The preserve references handling.</value>
		// Token: 0x17000066 RID: 102
		// (get) Token: 0x060001A3 RID: 419 RVA: 0x00005DD9 File Offset: 0x00003FD9
		// (set) Token: 0x060001A4 RID: 420 RVA: 0x00005DE6 File Offset: 0x00003FE6
		public PreserveReferencesHandling PreserveReferencesHandling
		{
			get
			{
				return this._preserveReferencesHandling.GetValueOrDefault();
			}
			set
			{
				this._preserveReferencesHandling = new PreserveReferencesHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how type name writing and reading is handled by the serializer.
		/// The default value is <see cref="F:Newtonsoft.Json.TypeNameHandling.None" />.
		/// </summary>
		/// <remarks>
		/// <see cref="P:Newtonsoft.Json.JsonSerializerSettings.TypeNameHandling" /> should be used with caution when your application deserializes JSON from an external source.
		/// Incoming types should be validated with a custom <see cref="P:Newtonsoft.Json.JsonSerializerSettings.SerializationBinder" />
		/// when deserializing with a value other than <see cref="F:Newtonsoft.Json.TypeNameHandling.None" />.
		/// </remarks>
		/// <value>The type name handling.</value>
		// Token: 0x17000067 RID: 103
		// (get) Token: 0x060001A5 RID: 421 RVA: 0x00005DF4 File Offset: 0x00003FF4
		// (set) Token: 0x060001A6 RID: 422 RVA: 0x00005E01 File Offset: 0x00004001
		public TypeNameHandling TypeNameHandling
		{
			get
			{
				return this._typeNameHandling.GetValueOrDefault();
			}
			set
			{
				this._typeNameHandling = new TypeNameHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how metadata properties are used during deserialization.
		/// The default value is <see cref="F:Newtonsoft.Json.MetadataPropertyHandling.Default" />.
		/// </summary>
		/// <value>The metadata properties handling.</value>
		// Token: 0x17000068 RID: 104
		// (get) Token: 0x060001A7 RID: 423 RVA: 0x00005E0F File Offset: 0x0000400F
		// (set) Token: 0x060001A8 RID: 424 RVA: 0x00005E1C File Offset: 0x0000401C
		public MetadataPropertyHandling MetadataPropertyHandling
		{
			get
			{
				return this._metadataPropertyHandling.GetValueOrDefault();
			}
			set
			{
				this._metadataPropertyHandling = new MetadataPropertyHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how a type name assembly is written and resolved by the serializer.
		/// The default value is <see cref="F:System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple" />.
		/// </summary>
		/// <value>The type name assembly format.</value>
		// Token: 0x17000069 RID: 105
		// (get) Token: 0x060001A9 RID: 425 RVA: 0x00005E2A File Offset: 0x0000402A
		// (set) Token: 0x060001AA RID: 426 RVA: 0x00005E32 File Offset: 0x00004032
		[Obsolete("TypeNameAssemblyFormat is obsolete. Use TypeNameAssemblyFormatHandling instead.")]
		public FormatterAssemblyStyle TypeNameAssemblyFormat
		{
			get
			{
				return (FormatterAssemblyStyle)this.TypeNameAssemblyFormatHandling;
			}
			set
			{
				this.TypeNameAssemblyFormatHandling = (TypeNameAssemblyFormatHandling)value;
			}
		}

		/// <summary>
		/// Gets or sets how a type name assembly is written and resolved by the serializer.
		/// The default value is <see cref="F:Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple" />.
		/// </summary>
		/// <value>The type name assembly format.</value>
		// Token: 0x1700006A RID: 106
		// (get) Token: 0x060001AB RID: 427 RVA: 0x00005E3B File Offset: 0x0000403B
		// (set) Token: 0x060001AC RID: 428 RVA: 0x00005E48 File Offset: 0x00004048
		public TypeNameAssemblyFormatHandling TypeNameAssemblyFormatHandling
		{
			get
			{
				return this._typeNameAssemblyFormatHandling.GetValueOrDefault();
			}
			set
			{
				this._typeNameAssemblyFormatHandling = new TypeNameAssemblyFormatHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how constructors are used during deserialization.
		/// The default value is <see cref="F:Newtonsoft.Json.ConstructorHandling.Default" />.
		/// </summary>
		/// <value>The constructor handling.</value>
		// Token: 0x1700006B RID: 107
		// (get) Token: 0x060001AD RID: 429 RVA: 0x00005E56 File Offset: 0x00004056
		// (set) Token: 0x060001AE RID: 430 RVA: 0x00005E63 File Offset: 0x00004063
		public ConstructorHandling ConstructorHandling
		{
			get
			{
				return this._constructorHandling.GetValueOrDefault();
			}
			set
			{
				this._constructorHandling = new ConstructorHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets the contract resolver used by the serializer when
		/// serializing .NET objects to JSON and vice versa.
		/// </summary>
		/// <value>The contract resolver.</value>
		// Token: 0x1700006C RID: 108
		// (get) Token: 0x060001AF RID: 431 RVA: 0x00005E71 File Offset: 0x00004071
		// (set) Token: 0x060001B0 RID: 432 RVA: 0x00005E79 File Offset: 0x00004079
		public IContractResolver ContractResolver { get; set; }

		/// <summary>
		/// Gets or sets the equality comparer used by the serializer when comparing references.
		/// </summary>
		/// <value>The equality comparer.</value>
		// Token: 0x1700006D RID: 109
		// (get) Token: 0x060001B1 RID: 433 RVA: 0x00005E82 File Offset: 0x00004082
		// (set) Token: 0x060001B2 RID: 434 RVA: 0x00005E8A File Offset: 0x0000408A
		public IEqualityComparer EqualityComparer { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.Serialization.IReferenceResolver" /> used by the serializer when resolving references.
		/// </summary>
		/// <value>The reference resolver.</value>
		// Token: 0x1700006E RID: 110
		// (get) Token: 0x060001B3 RID: 435 RVA: 0x00005E93 File Offset: 0x00004093
		// (set) Token: 0x060001B4 RID: 436 RVA: 0x00005EA8 File Offset: 0x000040A8
		[Obsolete("ReferenceResolver property is obsolete. Use the ReferenceResolverProvider property to set the IReferenceResolver: settings.ReferenceResolverProvider = () => resolver")]
		public IReferenceResolver ReferenceResolver
		{
			get
			{
				Func<IReferenceResolver> referenceResolverProvider = this.ReferenceResolverProvider;
				if (referenceResolverProvider == null)
				{
					return null;
				}
				return referenceResolverProvider();
			}
			set
			{
				this.ReferenceResolverProvider = ((value != null) ? (() => value) : null);
			}
		}

		/// <summary>
		/// Gets or sets a function that creates the <see cref="T:Newtonsoft.Json.Serialization.IReferenceResolver" /> used by the serializer when resolving references.
		/// </summary>
		/// <value>A function that creates the <see cref="T:Newtonsoft.Json.Serialization.IReferenceResolver" /> used by the serializer when resolving references.</value>
		// Token: 0x1700006F RID: 111
		// (get) Token: 0x060001B5 RID: 437 RVA: 0x00005EDF File Offset: 0x000040DF
		// (set) Token: 0x060001B6 RID: 438 RVA: 0x00005EE7 File Offset: 0x000040E7
		public Func<IReferenceResolver> ReferenceResolverProvider { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.Serialization.ITraceWriter" /> used by the serializer when writing trace messages.
		/// </summary>
		/// <value>The trace writer.</value>
		// Token: 0x17000070 RID: 112
		// (get) Token: 0x060001B7 RID: 439 RVA: 0x00005EF0 File Offset: 0x000040F0
		// (set) Token: 0x060001B8 RID: 440 RVA: 0x00005EF8 File Offset: 0x000040F8
		public ITraceWriter TraceWriter { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="P:Newtonsoft.Json.JsonSerializerSettings.SerializationBinder" /> used by the serializer when resolving type names.
		/// </summary>
		/// <value>The binder.</value>
		// Token: 0x17000071 RID: 113
		// (get) Token: 0x060001B9 RID: 441 RVA: 0x00005F04 File Offset: 0x00004104
		// (set) Token: 0x060001BA RID: 442 RVA: 0x00005F3B File Offset: 0x0000413B
		[Obsolete("Binder is obsolete. Use SerializationBinder instead.")]
		public SerializationBinder Binder
		{
			get
			{
				if (this.SerializationBinder == null)
				{
					return null;
				}
				SerializationBinderAdapter serializationBinderAdapter = this.SerializationBinder as SerializationBinderAdapter;
				if (serializationBinderAdapter != null)
				{
					return serializationBinderAdapter.SerializationBinder;
				}
				throw new InvalidOperationException("Cannot get SerializationBinder because an ISerializationBinder was previously set.");
			}
			set
			{
				this.SerializationBinder = ((value == null) ? null : new SerializationBinderAdapter(value));
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.Serialization.ISerializationBinder" /> used by the serializer when resolving type names.
		/// </summary>
		/// <value>The binder.</value>
		// Token: 0x17000072 RID: 114
		// (get) Token: 0x060001BB RID: 443 RVA: 0x00005F4F File Offset: 0x0000414F
		// (set) Token: 0x060001BC RID: 444 RVA: 0x00005F57 File Offset: 0x00004157
		public ISerializationBinder SerializationBinder { get; set; }

		/// <summary>
		/// Gets or sets the error handler called during serialization and deserialization.
		/// </summary>
		/// <value>The error handler called during serialization and deserialization.</value>
		// Token: 0x17000073 RID: 115
		// (get) Token: 0x060001BD RID: 445 RVA: 0x00005F60 File Offset: 0x00004160
		// (set) Token: 0x060001BE RID: 446 RVA: 0x00005F68 File Offset: 0x00004168
		[Nullable(new byte[] { 2, 1 })]
		public EventHandler<ErrorEventArgs> Error
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get;
			[param: Nullable(new byte[] { 2, 1 })]
			set;
		}

		/// <summary>
		/// Gets or sets the <see cref="T:System.Runtime.Serialization.StreamingContext" /> used by the serializer when invoking serialization callback methods.
		/// </summary>
		/// <value>The context.</value>
		// Token: 0x17000074 RID: 116
		// (get) Token: 0x060001BF RID: 447 RVA: 0x00005F74 File Offset: 0x00004174
		// (set) Token: 0x060001C0 RID: 448 RVA: 0x00005F9E File Offset: 0x0000419E
		public StreamingContext Context
		{
			get
			{
				StreamingContext? context = this._context;
				if (context == null)
				{
					return JsonSerializerSettings.DefaultContext;
				}
				return context.GetValueOrDefault();
			}
			set
			{
				this._context = new StreamingContext?(value);
			}
		}

		/// <summary>
		/// Gets or sets how <see cref="T:System.DateTime" /> and <see cref="T:System.DateTimeOffset" /> values are formatted when writing JSON text,
		/// and the expected date format when reading JSON text.
		/// The default value is <c>"yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK"</c>.
		/// </summary>
		// Token: 0x17000075 RID: 117
		// (get) Token: 0x060001C1 RID: 449 RVA: 0x00005FAC File Offset: 0x000041AC
		// (set) Token: 0x060001C2 RID: 450 RVA: 0x00005FBD File Offset: 0x000041BD
		[Nullable(1)]
		public string DateFormatString
		{
			[NullableContext(1)]
			get
			{
				return this._dateFormatString ?? "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";
			}
			[NullableContext(1)]
			set
			{
				this._dateFormatString = value;
				this._dateFormatStringSet = true;
			}
		}

		/// <summary>
		/// Gets or sets the maximum depth allowed when reading JSON. Reading past this depth will throw a <see cref="T:Newtonsoft.Json.JsonReaderException" />.
		/// A null value means there is no maximum.
		/// The default value is <c>64</c>.
		/// </summary>
		// Token: 0x17000076 RID: 118
		// (get) Token: 0x060001C3 RID: 451 RVA: 0x00005FCD File Offset: 0x000041CD
		// (set) Token: 0x060001C4 RID: 452 RVA: 0x00005FE8 File Offset: 0x000041E8
		public int? MaxDepth
		{
			get
			{
				if (!this._maxDepthSet)
				{
					return new int?(64);
				}
				return this._maxDepth;
			}
			set
			{
				int? num = value;
				int num2 = 0;
				if ((num.GetValueOrDefault() <= num2) & (num != null))
				{
					throw new ArgumentException("Value must be positive.", "value");
				}
				this._maxDepth = value;
				this._maxDepthSet = true;
			}
		}

		/// <summary>
		/// Indicates how JSON text output is formatted.
		/// The default value is <see cref="F:Newtonsoft.Json.Formatting.None" />.
		/// </summary>
		// Token: 0x17000077 RID: 119
		// (get) Token: 0x060001C5 RID: 453 RVA: 0x0000602E File Offset: 0x0000422E
		// (set) Token: 0x060001C6 RID: 454 RVA: 0x0000603B File Offset: 0x0000423B
		public Formatting Formatting
		{
			get
			{
				return this._formatting.GetValueOrDefault();
			}
			set
			{
				this._formatting = new Formatting?(value);
			}
		}

		/// <summary>
		/// Gets or sets how dates are written to JSON text.
		/// The default value is <see cref="F:Newtonsoft.Json.DateFormatHandling.IsoDateFormat" />.
		/// </summary>
		// Token: 0x17000078 RID: 120
		// (get) Token: 0x060001C7 RID: 455 RVA: 0x00006049 File Offset: 0x00004249
		// (set) Token: 0x060001C8 RID: 456 RVA: 0x00006056 File Offset: 0x00004256
		public DateFormatHandling DateFormatHandling
		{
			get
			{
				return this._dateFormatHandling.GetValueOrDefault();
			}
			set
			{
				this._dateFormatHandling = new DateFormatHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how <see cref="T:System.DateTime" /> time zones are handled during serialization and deserialization.
		/// The default value is <see cref="F:Newtonsoft.Json.DateTimeZoneHandling.RoundtripKind" />.
		/// </summary>
		// Token: 0x17000079 RID: 121
		// (get) Token: 0x060001C9 RID: 457 RVA: 0x00006064 File Offset: 0x00004264
		// (set) Token: 0x060001CA RID: 458 RVA: 0x0000608A File Offset: 0x0000428A
		public DateTimeZoneHandling DateTimeZoneHandling
		{
			get
			{
				DateTimeZoneHandling? dateTimeZoneHandling = this._dateTimeZoneHandling;
				if (dateTimeZoneHandling == null)
				{
					return DateTimeZoneHandling.RoundtripKind;
				}
				return dateTimeZoneHandling.GetValueOrDefault();
			}
			set
			{
				this._dateTimeZoneHandling = new DateTimeZoneHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how date formatted strings, e.g. <c>"\/Date(1198908717056)\/"</c> and <c>"2012-03-21T05:40Z"</c>, are parsed when reading JSON.
		/// The default value is <see cref="F:Newtonsoft.Json.DateParseHandling.DateTime" />.
		/// </summary>
		// Token: 0x1700007A RID: 122
		// (get) Token: 0x060001CB RID: 459 RVA: 0x00006098 File Offset: 0x00004298
		// (set) Token: 0x060001CC RID: 460 RVA: 0x000060BE File Offset: 0x000042BE
		public DateParseHandling DateParseHandling
		{
			get
			{
				DateParseHandling? dateParseHandling = this._dateParseHandling;
				if (dateParseHandling == null)
				{
					return DateParseHandling.DateTime;
				}
				return dateParseHandling.GetValueOrDefault();
			}
			set
			{
				this._dateParseHandling = new DateParseHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how special floating point numbers, e.g. <see cref="F:System.Double.NaN" />,
		/// <see cref="F:System.Double.PositiveInfinity" /> and <see cref="F:System.Double.NegativeInfinity" />,
		/// are written as JSON.
		/// The default value is <see cref="F:Newtonsoft.Json.FloatFormatHandling.String" />.
		/// </summary>
		// Token: 0x1700007B RID: 123
		// (get) Token: 0x060001CD RID: 461 RVA: 0x000060CC File Offset: 0x000042CC
		// (set) Token: 0x060001CE RID: 462 RVA: 0x000060D9 File Offset: 0x000042D9
		public FloatFormatHandling FloatFormatHandling
		{
			get
			{
				return this._floatFormatHandling.GetValueOrDefault();
			}
			set
			{
				this._floatFormatHandling = new FloatFormatHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how floating point numbers, e.g. 1.0 and 9.9, are parsed when reading JSON text.
		/// The default value is <see cref="F:Newtonsoft.Json.FloatParseHandling.Double" />.
		/// </summary>
		// Token: 0x1700007C RID: 124
		// (get) Token: 0x060001CF RID: 463 RVA: 0x000060E7 File Offset: 0x000042E7
		// (set) Token: 0x060001D0 RID: 464 RVA: 0x000060F4 File Offset: 0x000042F4
		public FloatParseHandling FloatParseHandling
		{
			get
			{
				return this._floatParseHandling.GetValueOrDefault();
			}
			set
			{
				this._floatParseHandling = new FloatParseHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets how strings are escaped when writing JSON text.
		/// The default value is <see cref="F:Newtonsoft.Json.StringEscapeHandling.Default" />.
		/// </summary>
		// Token: 0x1700007D RID: 125
		// (get) Token: 0x060001D1 RID: 465 RVA: 0x00006102 File Offset: 0x00004302
		// (set) Token: 0x060001D2 RID: 466 RVA: 0x0000610F File Offset: 0x0000430F
		public StringEscapeHandling StringEscapeHandling
		{
			get
			{
				return this._stringEscapeHandling.GetValueOrDefault();
			}
			set
			{
				this._stringEscapeHandling = new StringEscapeHandling?(value);
			}
		}

		/// <summary>
		/// Gets or sets the culture used when reading JSON.
		/// The default value is <see cref="P:System.Globalization.CultureInfo.InvariantCulture" />.
		/// </summary>
		// Token: 0x1700007E RID: 126
		// (get) Token: 0x060001D3 RID: 467 RVA: 0x0000611D File Offset: 0x0000431D
		// (set) Token: 0x060001D4 RID: 468 RVA: 0x0000612E File Offset: 0x0000432E
		[Nullable(1)]
		public CultureInfo Culture
		{
			[NullableContext(1)]
			get
			{
				return this._culture ?? JsonSerializerSettings.DefaultCulture;
			}
			[NullableContext(1)]
			set
			{
				this._culture = value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether there will be a check for additional content after deserializing an object.
		/// The default value is <c>false</c>.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if there will be a check for additional content after deserializing an object; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x1700007F RID: 127
		// (get) Token: 0x060001D5 RID: 469 RVA: 0x00006137 File Offset: 0x00004337
		// (set) Token: 0x060001D6 RID: 470 RVA: 0x00006144 File Offset: 0x00004344
		public bool CheckAdditionalContent
		{
			get
			{
				return this._checkAdditionalContent.GetValueOrDefault();
			}
			set
			{
				this._checkAdditionalContent = new bool?(value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonSerializerSettings" /> class.
		/// </summary>
		// Token: 0x060001D8 RID: 472 RVA: 0x00006169 File Offset: 0x00004369
		[DebuggerStepThrough]
		public JsonSerializerSettings()
		{
			this.Converters = new List<JsonConverter>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonSerializerSettings" /> class
		/// using values copied from the passed in <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// </summary>
		// Token: 0x060001D9 RID: 473 RVA: 0x0000617C File Offset: 0x0000437C
		[NullableContext(1)]
		public JsonSerializerSettings(JsonSerializerSettings original)
		{
			this._floatParseHandling = original._floatParseHandling;
			this._floatFormatHandling = original._floatFormatHandling;
			this._dateParseHandling = original._dateParseHandling;
			this._dateTimeZoneHandling = original._dateTimeZoneHandling;
			this._dateFormatHandling = original._dateFormatHandling;
			this._formatting = original._formatting;
			this._maxDepth = original._maxDepth;
			this._maxDepthSet = original._maxDepthSet;
			this._dateFormatString = original._dateFormatString;
			this._dateFormatStringSet = original._dateFormatStringSet;
			this._context = original._context;
			this.Error = original.Error;
			this.SerializationBinder = original.SerializationBinder;
			this.TraceWriter = original.TraceWriter;
			this._culture = original._culture;
			this.ReferenceResolverProvider = original.ReferenceResolverProvider;
			this.EqualityComparer = original.EqualityComparer;
			this.ContractResolver = original.ContractResolver;
			this._constructorHandling = original._constructorHandling;
			this._typeNameAssemblyFormatHandling = original._typeNameAssemblyFormatHandling;
			this._metadataPropertyHandling = original._metadataPropertyHandling;
			this._typeNameHandling = original._typeNameHandling;
			this._preserveReferencesHandling = original._preserveReferencesHandling;
			this.Converters = original.Converters.ToList<JsonConverter>();
			this._defaultValueHandling = original._defaultValueHandling;
			this._nullValueHandling = original._nullValueHandling;
			this._objectCreationHandling = original._objectCreationHandling;
			this._missingMemberHandling = original._missingMemberHandling;
			this._referenceLoopHandling = original._referenceLoopHandling;
			this._checkAdditionalContent = original._checkAdditionalContent;
			this._stringEscapeHandling = original._stringEscapeHandling;
		}

		// Token: 0x04000092 RID: 146
		internal const ReferenceLoopHandling DefaultReferenceLoopHandling = ReferenceLoopHandling.Error;

		// Token: 0x04000093 RID: 147
		internal const MissingMemberHandling DefaultMissingMemberHandling = MissingMemberHandling.Ignore;

		// Token: 0x04000094 RID: 148
		internal const NullValueHandling DefaultNullValueHandling = NullValueHandling.Include;

		// Token: 0x04000095 RID: 149
		internal const DefaultValueHandling DefaultDefaultValueHandling = DefaultValueHandling.Include;

		// Token: 0x04000096 RID: 150
		internal const ObjectCreationHandling DefaultObjectCreationHandling = ObjectCreationHandling.Auto;

		// Token: 0x04000097 RID: 151
		internal const PreserveReferencesHandling DefaultPreserveReferencesHandling = PreserveReferencesHandling.None;

		// Token: 0x04000098 RID: 152
		internal const ConstructorHandling DefaultConstructorHandling = ConstructorHandling.Default;

		// Token: 0x04000099 RID: 153
		internal const TypeNameHandling DefaultTypeNameHandling = TypeNameHandling.None;

		// Token: 0x0400009A RID: 154
		internal const MetadataPropertyHandling DefaultMetadataPropertyHandling = MetadataPropertyHandling.Default;

		// Token: 0x0400009B RID: 155
		internal static readonly StreamingContext DefaultContext = default(StreamingContext);

		// Token: 0x0400009C RID: 156
		internal const Formatting DefaultFormatting = Formatting.None;

		// Token: 0x0400009D RID: 157
		internal const DateFormatHandling DefaultDateFormatHandling = DateFormatHandling.IsoDateFormat;

		// Token: 0x0400009E RID: 158
		internal const DateTimeZoneHandling DefaultDateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;

		// Token: 0x0400009F RID: 159
		internal const DateParseHandling DefaultDateParseHandling = DateParseHandling.DateTime;

		// Token: 0x040000A0 RID: 160
		internal const FloatParseHandling DefaultFloatParseHandling = FloatParseHandling.Double;

		// Token: 0x040000A1 RID: 161
		internal const FloatFormatHandling DefaultFloatFormatHandling = FloatFormatHandling.String;

		// Token: 0x040000A2 RID: 162
		internal const StringEscapeHandling DefaultStringEscapeHandling = StringEscapeHandling.Default;

		// Token: 0x040000A3 RID: 163
		internal const TypeNameAssemblyFormatHandling DefaultTypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;

		// Token: 0x040000A4 RID: 164
		[Nullable(1)]
		internal static readonly CultureInfo DefaultCulture = CultureInfo.InvariantCulture;

		// Token: 0x040000A5 RID: 165
		internal const bool DefaultCheckAdditionalContent = false;

		// Token: 0x040000A6 RID: 166
		[Nullable(1)]
		internal const string DefaultDateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

		// Token: 0x040000A7 RID: 167
		internal const int DefaultMaxDepth = 64;

		// Token: 0x040000A8 RID: 168
		internal Formatting? _formatting;

		// Token: 0x040000A9 RID: 169
		internal DateFormatHandling? _dateFormatHandling;

		// Token: 0x040000AA RID: 170
		internal DateTimeZoneHandling? _dateTimeZoneHandling;

		// Token: 0x040000AB RID: 171
		internal DateParseHandling? _dateParseHandling;

		// Token: 0x040000AC RID: 172
		internal FloatFormatHandling? _floatFormatHandling;

		// Token: 0x040000AD RID: 173
		internal FloatParseHandling? _floatParseHandling;

		// Token: 0x040000AE RID: 174
		internal StringEscapeHandling? _stringEscapeHandling;

		// Token: 0x040000AF RID: 175
		internal CultureInfo _culture;

		// Token: 0x040000B0 RID: 176
		internal bool? _checkAdditionalContent;

		// Token: 0x040000B1 RID: 177
		internal int? _maxDepth;

		// Token: 0x040000B2 RID: 178
		internal bool _maxDepthSet;

		// Token: 0x040000B3 RID: 179
		internal string _dateFormatString;

		// Token: 0x040000B4 RID: 180
		internal bool _dateFormatStringSet;

		// Token: 0x040000B5 RID: 181
		internal TypeNameAssemblyFormatHandling? _typeNameAssemblyFormatHandling;

		// Token: 0x040000B6 RID: 182
		internal DefaultValueHandling? _defaultValueHandling;

		// Token: 0x040000B7 RID: 183
		internal PreserveReferencesHandling? _preserveReferencesHandling;

		// Token: 0x040000B8 RID: 184
		internal NullValueHandling? _nullValueHandling;

		// Token: 0x040000B9 RID: 185
		internal ObjectCreationHandling? _objectCreationHandling;

		// Token: 0x040000BA RID: 186
		internal MissingMemberHandling? _missingMemberHandling;

		// Token: 0x040000BB RID: 187
		internal ReferenceLoopHandling? _referenceLoopHandling;

		// Token: 0x040000BC RID: 188
		internal StreamingContext? _context;

		// Token: 0x040000BD RID: 189
		internal ConstructorHandling? _constructorHandling;

		// Token: 0x040000BE RID: 190
		internal TypeNameHandling? _typeNameHandling;

		// Token: 0x040000BF RID: 191
		internal MetadataPropertyHandling? _metadataPropertyHandling;
	}
}
