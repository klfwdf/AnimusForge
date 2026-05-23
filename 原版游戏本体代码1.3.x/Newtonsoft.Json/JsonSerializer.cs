using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Serializes and deserializes objects into and from the JSON format.
	/// The <see cref="T:Newtonsoft.Json.JsonSerializer" /> enables you to control how objects are encoded into JSON.
	/// </summary>
	// Token: 0x0200002B RID: 43
	[NullableContext(1)]
	[Nullable(0)]
	public class JsonSerializer
	{
		/// <summary>
		/// Occurs when the <see cref="T:Newtonsoft.Json.JsonSerializer" /> errors during serialization and deserialization.
		/// </summary>
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x0600013F RID: 319 RVA: 0x00004CB4 File Offset: 0x00002EB4
		// (remove) Token: 0x06000140 RID: 320 RVA: 0x00004CEC File Offset: 0x00002EEC
		[Nullable(new byte[] { 2, 1 })]
		[Nullable(new byte[] { 2, 1 })]
		public virtual event EventHandler<Newtonsoft.Json.Serialization.ErrorEventArgs> Error;

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.Serialization.IReferenceResolver" /> used by the serializer when resolving references.
		/// </summary>
		// Token: 0x17000042 RID: 66
		// (get) Token: 0x06000141 RID: 321 RVA: 0x00004D21 File Offset: 0x00002F21
		// (set) Token: 0x06000142 RID: 322 RVA: 0x00004D29 File Offset: 0x00002F29
		[Nullable(2)]
		public virtual IReferenceResolver ReferenceResolver
		{
			[NullableContext(2)]
			get
			{
				return this.GetReferenceResolver();
			}
			[NullableContext(2)]
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value", "Reference resolver cannot be null.");
				}
				this._referenceResolver = value;
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="P:Newtonsoft.Json.JsonSerializer.SerializationBinder" /> used by the serializer when resolving type names.
		/// </summary>
		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000143 RID: 323 RVA: 0x00004D48 File Offset: 0x00002F48
		// (set) Token: 0x06000144 RID: 324 RVA: 0x00004D86 File Offset: 0x00002F86
		[Obsolete("Binder is obsolete. Use SerializationBinder instead.")]
		public virtual SerializationBinder Binder
		{
			get
			{
				SerializationBinder serializationBinder = this._serializationBinder as SerializationBinder;
				if (serializationBinder != null)
				{
					return serializationBinder;
				}
				SerializationBinderAdapter serializationBinderAdapter = this._serializationBinder as SerializationBinderAdapter;
				if (serializationBinderAdapter != null)
				{
					return serializationBinderAdapter.SerializationBinder;
				}
				throw new InvalidOperationException("Cannot get SerializationBinder because an ISerializationBinder was previously set.");
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value", "Serialization binder cannot be null.");
				}
				this._serializationBinder = (value as ISerializationBinder) ?? new SerializationBinderAdapter(value);
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.Serialization.ISerializationBinder" /> used by the serializer when resolving type names.
		/// </summary>
		// Token: 0x17000044 RID: 68
		// (get) Token: 0x06000145 RID: 325 RVA: 0x00004DB1 File Offset: 0x00002FB1
		// (set) Token: 0x06000146 RID: 326 RVA: 0x00004DB9 File Offset: 0x00002FB9
		public virtual ISerializationBinder SerializationBinder
		{
			get
			{
				return this._serializationBinder;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value", "Serialization binder cannot be null.");
				}
				this._serializationBinder = value;
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="T:Newtonsoft.Json.Serialization.ITraceWriter" /> used by the serializer when writing trace messages.
		/// </summary>
		/// <value>The trace writer.</value>
		// Token: 0x17000045 RID: 69
		// (get) Token: 0x06000147 RID: 327 RVA: 0x00004DD5 File Offset: 0x00002FD5
		// (set) Token: 0x06000148 RID: 328 RVA: 0x00004DDD File Offset: 0x00002FDD
		[Nullable(2)]
		public virtual ITraceWriter TraceWriter
		{
			[NullableContext(2)]
			get
			{
				return this._traceWriter;
			}
			[NullableContext(2)]
			set
			{
				this._traceWriter = value;
			}
		}

		/// <summary>
		/// Gets or sets the equality comparer used by the serializer when comparing references.
		/// </summary>
		/// <value>The equality comparer.</value>
		// Token: 0x17000046 RID: 70
		// (get) Token: 0x06000149 RID: 329 RVA: 0x00004DE6 File Offset: 0x00002FE6
		// (set) Token: 0x0600014A RID: 330 RVA: 0x00004DEE File Offset: 0x00002FEE
		[Nullable(2)]
		public virtual IEqualityComparer EqualityComparer
		{
			[NullableContext(2)]
			get
			{
				return this._equalityComparer;
			}
			[NullableContext(2)]
			set
			{
				this._equalityComparer = value;
			}
		}

		/// <summary>
		/// Gets or sets how type name writing and reading is handled by the serializer.
		/// The default value is <see cref="F:Newtonsoft.Json.TypeNameHandling.None" />.
		/// </summary>
		/// <remarks>
		/// <see cref="P:Newtonsoft.Json.JsonSerializer.TypeNameHandling" /> should be used with caution when your application deserializes JSON from an external source.
		/// Incoming types should be validated with a custom <see cref="P:Newtonsoft.Json.JsonSerializer.SerializationBinder" />
		/// when deserializing with a value other than <see cref="F:Newtonsoft.Json.TypeNameHandling.None" />.
		/// </remarks>
		// Token: 0x17000047 RID: 71
		// (get) Token: 0x0600014B RID: 331 RVA: 0x00004DF7 File Offset: 0x00002FF7
		// (set) Token: 0x0600014C RID: 332 RVA: 0x00004DFF File Offset: 0x00002FFF
		public virtual TypeNameHandling TypeNameHandling
		{
			get
			{
				return this._typeNameHandling;
			}
			set
			{
				if (value < TypeNameHandling.None || value > TypeNameHandling.Auto)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._typeNameHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how a type name assembly is written and resolved by the serializer.
		/// The default value is <see cref="F:System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple" />.
		/// </summary>
		/// <value>The type name assembly format.</value>
		// Token: 0x17000048 RID: 72
		// (get) Token: 0x0600014D RID: 333 RVA: 0x00004E1B File Offset: 0x0000301B
		// (set) Token: 0x0600014E RID: 334 RVA: 0x00004E23 File Offset: 0x00003023
		[Obsolete("TypeNameAssemblyFormat is obsolete. Use TypeNameAssemblyFormatHandling instead.")]
		public virtual FormatterAssemblyStyle TypeNameAssemblyFormat
		{
			get
			{
				return (FormatterAssemblyStyle)this._typeNameAssemblyFormatHandling;
			}
			set
			{
				if (value < FormatterAssemblyStyle.Simple || value > FormatterAssemblyStyle.Full)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._typeNameAssemblyFormatHandling = (TypeNameAssemblyFormatHandling)value;
			}
		}

		/// <summary>
		/// Gets or sets how a type name assembly is written and resolved by the serializer.
		/// The default value is <see cref="F:Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple" />.
		/// </summary>
		/// <value>The type name assembly format.</value>
		// Token: 0x17000049 RID: 73
		// (get) Token: 0x0600014F RID: 335 RVA: 0x00004E3F File Offset: 0x0000303F
		// (set) Token: 0x06000150 RID: 336 RVA: 0x00004E47 File Offset: 0x00003047
		public virtual TypeNameAssemblyFormatHandling TypeNameAssemblyFormatHandling
		{
			get
			{
				return this._typeNameAssemblyFormatHandling;
			}
			set
			{
				if (value < TypeNameAssemblyFormatHandling.Simple || value > TypeNameAssemblyFormatHandling.Full)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._typeNameAssemblyFormatHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how object references are preserved by the serializer.
		/// The default value is <see cref="F:Newtonsoft.Json.PreserveReferencesHandling.None" />.
		/// </summary>
		// Token: 0x1700004A RID: 74
		// (get) Token: 0x06000151 RID: 337 RVA: 0x00004E63 File Offset: 0x00003063
		// (set) Token: 0x06000152 RID: 338 RVA: 0x00004E6B File Offset: 0x0000306B
		public virtual PreserveReferencesHandling PreserveReferencesHandling
		{
			get
			{
				return this._preserveReferencesHandling;
			}
			set
			{
				if (value < PreserveReferencesHandling.None || value > PreserveReferencesHandling.All)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._preserveReferencesHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how reference loops (e.g. a class referencing itself) is handled.
		/// The default value is <see cref="F:Newtonsoft.Json.ReferenceLoopHandling.Error" />.
		/// </summary>
		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000153 RID: 339 RVA: 0x00004E87 File Offset: 0x00003087
		// (set) Token: 0x06000154 RID: 340 RVA: 0x00004E8F File Offset: 0x0000308F
		public virtual ReferenceLoopHandling ReferenceLoopHandling
		{
			get
			{
				return this._referenceLoopHandling;
			}
			set
			{
				if (value < ReferenceLoopHandling.Error || value > ReferenceLoopHandling.Serialize)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._referenceLoopHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how missing members (e.g. JSON contains a property that isn't a member on the object) are handled during deserialization.
		/// The default value is <see cref="F:Newtonsoft.Json.MissingMemberHandling.Ignore" />.
		/// </summary>
		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000155 RID: 341 RVA: 0x00004EAB File Offset: 0x000030AB
		// (set) Token: 0x06000156 RID: 342 RVA: 0x00004EB3 File Offset: 0x000030B3
		public virtual MissingMemberHandling MissingMemberHandling
		{
			get
			{
				return this._missingMemberHandling;
			}
			set
			{
				if (value < MissingMemberHandling.Ignore || value > MissingMemberHandling.Error)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._missingMemberHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how null values are handled during serialization and deserialization.
		/// The default value is <see cref="F:Newtonsoft.Json.NullValueHandling.Include" />.
		/// </summary>
		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000157 RID: 343 RVA: 0x00004ECF File Offset: 0x000030CF
		// (set) Token: 0x06000158 RID: 344 RVA: 0x00004ED7 File Offset: 0x000030D7
		public virtual NullValueHandling NullValueHandling
		{
			get
			{
				return this._nullValueHandling;
			}
			set
			{
				if (value < NullValueHandling.Include || value > NullValueHandling.Ignore)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._nullValueHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how default values are handled during serialization and deserialization.
		/// The default value is <see cref="F:Newtonsoft.Json.DefaultValueHandling.Include" />.
		/// </summary>
		// Token: 0x1700004E RID: 78
		// (get) Token: 0x06000159 RID: 345 RVA: 0x00004EF3 File Offset: 0x000030F3
		// (set) Token: 0x0600015A RID: 346 RVA: 0x00004EFB File Offset: 0x000030FB
		public virtual DefaultValueHandling DefaultValueHandling
		{
			get
			{
				return this._defaultValueHandling;
			}
			set
			{
				if (value < DefaultValueHandling.Include || value > DefaultValueHandling.IgnoreAndPopulate)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._defaultValueHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how objects are created during deserialization.
		/// The default value is <see cref="F:Newtonsoft.Json.ObjectCreationHandling.Auto" />.
		/// </summary>
		/// <value>The object creation handling.</value>
		// Token: 0x1700004F RID: 79
		// (get) Token: 0x0600015B RID: 347 RVA: 0x00004F17 File Offset: 0x00003117
		// (set) Token: 0x0600015C RID: 348 RVA: 0x00004F1F File Offset: 0x0000311F
		public virtual ObjectCreationHandling ObjectCreationHandling
		{
			get
			{
				return this._objectCreationHandling;
			}
			set
			{
				if (value < ObjectCreationHandling.Auto || value > ObjectCreationHandling.Replace)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._objectCreationHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how constructors are used during deserialization.
		/// The default value is <see cref="F:Newtonsoft.Json.ConstructorHandling.Default" />.
		/// </summary>
		/// <value>The constructor handling.</value>
		// Token: 0x17000050 RID: 80
		// (get) Token: 0x0600015D RID: 349 RVA: 0x00004F3B File Offset: 0x0000313B
		// (set) Token: 0x0600015E RID: 350 RVA: 0x00004F43 File Offset: 0x00003143
		public virtual ConstructorHandling ConstructorHandling
		{
			get
			{
				return this._constructorHandling;
			}
			set
			{
				if (value < ConstructorHandling.Default || value > ConstructorHandling.AllowNonPublicDefaultConstructor)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._constructorHandling = value;
			}
		}

		/// <summary>
		/// Gets or sets how metadata properties are used during deserialization.
		/// The default value is <see cref="F:Newtonsoft.Json.MetadataPropertyHandling.Default" />.
		/// </summary>
		/// <value>The metadata properties handling.</value>
		// Token: 0x17000051 RID: 81
		// (get) Token: 0x0600015F RID: 351 RVA: 0x00004F5F File Offset: 0x0000315F
		// (set) Token: 0x06000160 RID: 352 RVA: 0x00004F67 File Offset: 0x00003167
		public virtual MetadataPropertyHandling MetadataPropertyHandling
		{
			get
			{
				return this._metadataPropertyHandling;
			}
			set
			{
				if (value < MetadataPropertyHandling.Default || value > MetadataPropertyHandling.Ignore)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				this._metadataPropertyHandling = value;
			}
		}

		/// <summary>
		/// Gets a collection <see cref="T:Newtonsoft.Json.JsonConverter" /> that will be used during serialization.
		/// </summary>
		/// <value>Collection <see cref="T:Newtonsoft.Json.JsonConverter" /> that will be used during serialization.</value>
		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000161 RID: 353 RVA: 0x00004F83 File Offset: 0x00003183
		public virtual JsonConverterCollection Converters
		{
			get
			{
				if (this._converters == null)
				{
					this._converters = new JsonConverterCollection();
				}
				return this._converters;
			}
		}

		/// <summary>
		/// Gets or sets the contract resolver used by the serializer when
		/// serializing .NET objects to JSON and vice versa.
		/// </summary>
		// Token: 0x17000053 RID: 83
		// (get) Token: 0x06000162 RID: 354 RVA: 0x00004F9E File Offset: 0x0000319E
		// (set) Token: 0x06000163 RID: 355 RVA: 0x00004FA6 File Offset: 0x000031A6
		public virtual IContractResolver ContractResolver
		{
			get
			{
				return this._contractResolver;
			}
			set
			{
				this._contractResolver = value ?? DefaultContractResolver.Instance;
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="T:System.Runtime.Serialization.StreamingContext" /> used by the serializer when invoking serialization callback methods.
		/// </summary>
		/// <value>The context.</value>
		// Token: 0x17000054 RID: 84
		// (get) Token: 0x06000164 RID: 356 RVA: 0x00004FB8 File Offset: 0x000031B8
		// (set) Token: 0x06000165 RID: 357 RVA: 0x00004FC0 File Offset: 0x000031C0
		public virtual StreamingContext Context
		{
			get
			{
				return this._context;
			}
			set
			{
				this._context = value;
			}
		}

		/// <summary>
		/// Indicates how JSON text output is formatted.
		/// The default value is <see cref="F:Newtonsoft.Json.Formatting.None" />.
		/// </summary>
		// Token: 0x17000055 RID: 85
		// (get) Token: 0x06000166 RID: 358 RVA: 0x00004FC9 File Offset: 0x000031C9
		// (set) Token: 0x06000167 RID: 359 RVA: 0x00004FD6 File Offset: 0x000031D6
		public virtual Formatting Formatting
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
		// Token: 0x17000056 RID: 86
		// (get) Token: 0x06000168 RID: 360 RVA: 0x00004FE4 File Offset: 0x000031E4
		// (set) Token: 0x06000169 RID: 361 RVA: 0x00004FF1 File Offset: 0x000031F1
		public virtual DateFormatHandling DateFormatHandling
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
		// Token: 0x17000057 RID: 87
		// (get) Token: 0x0600016A RID: 362 RVA: 0x00005000 File Offset: 0x00003200
		// (set) Token: 0x0600016B RID: 363 RVA: 0x00005026 File Offset: 0x00003226
		public virtual DateTimeZoneHandling DateTimeZoneHandling
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
		// Token: 0x17000058 RID: 88
		// (get) Token: 0x0600016C RID: 364 RVA: 0x00005034 File Offset: 0x00003234
		// (set) Token: 0x0600016D RID: 365 RVA: 0x0000505A File Offset: 0x0000325A
		public virtual DateParseHandling DateParseHandling
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
		/// Gets or sets how floating point numbers, e.g. 1.0 and 9.9, are parsed when reading JSON text.
		/// The default value is <see cref="F:Newtonsoft.Json.FloatParseHandling.Double" />.
		/// </summary>
		// Token: 0x17000059 RID: 89
		// (get) Token: 0x0600016E RID: 366 RVA: 0x00005068 File Offset: 0x00003268
		// (set) Token: 0x0600016F RID: 367 RVA: 0x00005075 File Offset: 0x00003275
		public virtual FloatParseHandling FloatParseHandling
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
		/// Gets or sets how special floating point numbers, e.g. <see cref="F:System.Double.NaN" />,
		/// <see cref="F:System.Double.PositiveInfinity" /> and <see cref="F:System.Double.NegativeInfinity" />,
		/// are written as JSON text.
		/// The default value is <see cref="F:Newtonsoft.Json.FloatFormatHandling.String" />.
		/// </summary>
		// Token: 0x1700005A RID: 90
		// (get) Token: 0x06000170 RID: 368 RVA: 0x00005083 File Offset: 0x00003283
		// (set) Token: 0x06000171 RID: 369 RVA: 0x00005090 File Offset: 0x00003290
		public virtual FloatFormatHandling FloatFormatHandling
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
		/// Gets or sets how strings are escaped when writing JSON text.
		/// The default value is <see cref="F:Newtonsoft.Json.StringEscapeHandling.Default" />.
		/// </summary>
		// Token: 0x1700005B RID: 91
		// (get) Token: 0x06000172 RID: 370 RVA: 0x0000509E File Offset: 0x0000329E
		// (set) Token: 0x06000173 RID: 371 RVA: 0x000050AB File Offset: 0x000032AB
		public virtual StringEscapeHandling StringEscapeHandling
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
		/// Gets or sets how <see cref="T:System.DateTime" /> and <see cref="T:System.DateTimeOffset" /> values are formatted when writing JSON text,
		/// and the expected date format when reading JSON text.
		/// The default value is <c>"yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK"</c>.
		/// </summary>
		// Token: 0x1700005C RID: 92
		// (get) Token: 0x06000174 RID: 372 RVA: 0x000050B9 File Offset: 0x000032B9
		// (set) Token: 0x06000175 RID: 373 RVA: 0x000050CA File Offset: 0x000032CA
		public virtual string DateFormatString
		{
			get
			{
				return this._dateFormatString ?? "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";
			}
			set
			{
				this._dateFormatString = value;
				this._dateFormatStringSet = true;
			}
		}

		/// <summary>
		/// Gets or sets the culture used when reading JSON.
		/// The default value is <see cref="P:System.Globalization.CultureInfo.InvariantCulture" />.
		/// </summary>
		// Token: 0x1700005D RID: 93
		// (get) Token: 0x06000176 RID: 374 RVA: 0x000050DA File Offset: 0x000032DA
		// (set) Token: 0x06000177 RID: 375 RVA: 0x000050EB File Offset: 0x000032EB
		public virtual CultureInfo Culture
		{
			get
			{
				return this._culture ?? JsonSerializerSettings.DefaultCulture;
			}
			set
			{
				this._culture = value;
			}
		}

		/// <summary>
		/// Gets or sets the maximum depth allowed when reading JSON. Reading past this depth will throw a <see cref="T:Newtonsoft.Json.JsonReaderException" />.
		/// A null value means there is no maximum.
		/// The default value is <c>64</c>.
		/// </summary>
		// Token: 0x1700005E RID: 94
		// (get) Token: 0x06000178 RID: 376 RVA: 0x000050F4 File Offset: 0x000032F4
		// (set) Token: 0x06000179 RID: 377 RVA: 0x000050FC File Offset: 0x000032FC
		public virtual int? MaxDepth
		{
			get
			{
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
		/// Gets a value indicating whether there will be a check for additional JSON content after deserializing an object.
		/// The default value is <c>false</c>.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if there will be a check for additional JSON content after deserializing an object; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x1700005F RID: 95
		// (get) Token: 0x0600017A RID: 378 RVA: 0x00005142 File Offset: 0x00003342
		// (set) Token: 0x0600017B RID: 379 RVA: 0x0000514F File Offset: 0x0000334F
		public virtual bool CheckAdditionalContent
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

		// Token: 0x0600017C RID: 380 RVA: 0x0000515D File Offset: 0x0000335D
		internal bool IsCheckAdditionalContentSet()
		{
			return this._checkAdditionalContent != null;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonSerializer" /> class.
		/// </summary>
		// Token: 0x0600017D RID: 381 RVA: 0x0000516C File Offset: 0x0000336C
		public JsonSerializer()
		{
			this._referenceLoopHandling = ReferenceLoopHandling.Error;
			this._missingMemberHandling = MissingMemberHandling.Ignore;
			this._nullValueHandling = NullValueHandling.Include;
			this._defaultValueHandling = DefaultValueHandling.Include;
			this._objectCreationHandling = ObjectCreationHandling.Auto;
			this._preserveReferencesHandling = PreserveReferencesHandling.None;
			this._constructorHandling = ConstructorHandling.Default;
			this._typeNameHandling = TypeNameHandling.None;
			this._metadataPropertyHandling = MetadataPropertyHandling.Default;
			this._context = JsonSerializerSettings.DefaultContext;
			this._serializationBinder = DefaultSerializationBinder.Instance;
			this._culture = JsonSerializerSettings.DefaultCulture;
			this._contractResolver = DefaultContractResolver.Instance;
		}

		/// <summary>
		/// Creates a new <see cref="T:Newtonsoft.Json.JsonSerializer" /> instance.
		/// The <see cref="T:Newtonsoft.Json.JsonSerializer" /> will not use default settings 
		/// from <see cref="P:Newtonsoft.Json.JsonConvert.DefaultSettings" />.
		/// </summary>
		/// <returns>
		/// A new <see cref="T:Newtonsoft.Json.JsonSerializer" /> instance.
		/// The <see cref="T:Newtonsoft.Json.JsonSerializer" /> will not use default settings 
		/// from <see cref="P:Newtonsoft.Json.JsonConvert.DefaultSettings" />.
		/// </returns>
		// Token: 0x0600017E RID: 382 RVA: 0x000051EA File Offset: 0x000033EA
		public static JsonSerializer Create()
		{
			return new JsonSerializer();
		}

		/// <summary>
		/// Creates a new <see cref="T:Newtonsoft.Json.JsonSerializer" /> instance using the specified <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// The <see cref="T:Newtonsoft.Json.JsonSerializer" /> will not use default settings 
		/// from <see cref="P:Newtonsoft.Json.JsonConvert.DefaultSettings" />.
		/// </summary>
		/// <param name="settings">The settings to be applied to the <see cref="T:Newtonsoft.Json.JsonSerializer" />.</param>
		/// <returns>
		/// A new <see cref="T:Newtonsoft.Json.JsonSerializer" /> instance using the specified <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// The <see cref="T:Newtonsoft.Json.JsonSerializer" /> will not use default settings 
		/// from <see cref="P:Newtonsoft.Json.JsonConvert.DefaultSettings" />.
		/// </returns>
		// Token: 0x0600017F RID: 383 RVA: 0x000051F4 File Offset: 0x000033F4
		public static JsonSerializer Create([Nullable(2)] JsonSerializerSettings settings)
		{
			JsonSerializer jsonSerializer = JsonSerializer.Create();
			if (settings != null)
			{
				JsonSerializer.ApplySerializerSettings(jsonSerializer, settings);
			}
			return jsonSerializer;
		}

		/// <summary>
		/// Creates a new <see cref="T:Newtonsoft.Json.JsonSerializer" /> instance.
		/// The <see cref="T:Newtonsoft.Json.JsonSerializer" /> will use default settings 
		/// from <see cref="P:Newtonsoft.Json.JsonConvert.DefaultSettings" />.
		/// </summary>
		/// <returns>
		/// A new <see cref="T:Newtonsoft.Json.JsonSerializer" /> instance.
		/// The <see cref="T:Newtonsoft.Json.JsonSerializer" /> will use default settings 
		/// from <see cref="P:Newtonsoft.Json.JsonConvert.DefaultSettings" />.
		/// </returns>
		// Token: 0x06000180 RID: 384 RVA: 0x00005212 File Offset: 0x00003412
		public static JsonSerializer CreateDefault()
		{
			Func<JsonSerializerSettings> defaultSettings = JsonConvert.DefaultSettings;
			return JsonSerializer.Create((defaultSettings != null) ? defaultSettings() : null);
		}

		/// <summary>
		/// Creates a new <see cref="T:Newtonsoft.Json.JsonSerializer" /> instance using the specified <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// The <see cref="T:Newtonsoft.Json.JsonSerializer" /> will use default settings 
		/// from <see cref="P:Newtonsoft.Json.JsonConvert.DefaultSettings" /> as well as the specified <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// </summary>
		/// <param name="settings">The settings to be applied to the <see cref="T:Newtonsoft.Json.JsonSerializer" />.</param>
		/// <returns>
		/// A new <see cref="T:Newtonsoft.Json.JsonSerializer" /> instance using the specified <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// The <see cref="T:Newtonsoft.Json.JsonSerializer" /> will use default settings 
		/// from <see cref="P:Newtonsoft.Json.JsonConvert.DefaultSettings" /> as well as the specified <see cref="T:Newtonsoft.Json.JsonSerializerSettings" />.
		/// </returns>
		// Token: 0x06000181 RID: 385 RVA: 0x0000522C File Offset: 0x0000342C
		public static JsonSerializer CreateDefault([Nullable(2)] JsonSerializerSettings settings)
		{
			JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();
			if (settings != null)
			{
				JsonSerializer.ApplySerializerSettings(jsonSerializer, settings);
			}
			return jsonSerializer;
		}

		// Token: 0x06000182 RID: 386 RVA: 0x0000524C File Offset: 0x0000344C
		private static void ApplySerializerSettings(JsonSerializer serializer, JsonSerializerSettings settings)
		{
			if (!CollectionUtils.IsNullOrEmpty<JsonConverter>(settings.Converters))
			{
				for (int i = 0; i < settings.Converters.Count; i++)
				{
					serializer.Converters.Insert(i, settings.Converters[i]);
				}
			}
			if (settings._typeNameHandling != null)
			{
				serializer.TypeNameHandling = settings.TypeNameHandling;
			}
			if (settings._metadataPropertyHandling != null)
			{
				serializer.MetadataPropertyHandling = settings.MetadataPropertyHandling;
			}
			if (settings._typeNameAssemblyFormatHandling != null)
			{
				serializer.TypeNameAssemblyFormatHandling = settings.TypeNameAssemblyFormatHandling;
			}
			if (settings._preserveReferencesHandling != null)
			{
				serializer.PreserveReferencesHandling = settings.PreserveReferencesHandling;
			}
			if (settings._referenceLoopHandling != null)
			{
				serializer.ReferenceLoopHandling = settings.ReferenceLoopHandling;
			}
			if (settings._missingMemberHandling != null)
			{
				serializer.MissingMemberHandling = settings.MissingMemberHandling;
			}
			if (settings._objectCreationHandling != null)
			{
				serializer.ObjectCreationHandling = settings.ObjectCreationHandling;
			}
			if (settings._nullValueHandling != null)
			{
				serializer.NullValueHandling = settings.NullValueHandling;
			}
			if (settings._defaultValueHandling != null)
			{
				serializer.DefaultValueHandling = settings.DefaultValueHandling;
			}
			if (settings._constructorHandling != null)
			{
				serializer.ConstructorHandling = settings.ConstructorHandling;
			}
			if (settings._context != null)
			{
				serializer.Context = settings.Context;
			}
			if (settings._checkAdditionalContent != null)
			{
				serializer._checkAdditionalContent = settings._checkAdditionalContent;
			}
			if (settings.Error != null)
			{
				serializer.Error += settings.Error;
			}
			if (settings.ContractResolver != null)
			{
				serializer.ContractResolver = settings.ContractResolver;
			}
			if (settings.ReferenceResolverProvider != null)
			{
				serializer.ReferenceResolver = settings.ReferenceResolverProvider();
			}
			if (settings.TraceWriter != null)
			{
				serializer.TraceWriter = settings.TraceWriter;
			}
			if (settings.EqualityComparer != null)
			{
				serializer.EqualityComparer = settings.EqualityComparer;
			}
			if (settings.SerializationBinder != null)
			{
				serializer.SerializationBinder = settings.SerializationBinder;
			}
			if (settings._formatting != null)
			{
				serializer._formatting = settings._formatting;
			}
			if (settings._dateFormatHandling != null)
			{
				serializer._dateFormatHandling = settings._dateFormatHandling;
			}
			if (settings._dateTimeZoneHandling != null)
			{
				serializer._dateTimeZoneHandling = settings._dateTimeZoneHandling;
			}
			if (settings._dateParseHandling != null)
			{
				serializer._dateParseHandling = settings._dateParseHandling;
			}
			if (settings._dateFormatStringSet)
			{
				serializer._dateFormatString = settings._dateFormatString;
				serializer._dateFormatStringSet = settings._dateFormatStringSet;
			}
			if (settings._floatFormatHandling != null)
			{
				serializer._floatFormatHandling = settings._floatFormatHandling;
			}
			if (settings._floatParseHandling != null)
			{
				serializer._floatParseHandling = settings._floatParseHandling;
			}
			if (settings._stringEscapeHandling != null)
			{
				serializer._stringEscapeHandling = settings._stringEscapeHandling;
			}
			if (settings._culture != null)
			{
				serializer._culture = settings._culture;
			}
			if (settings._maxDepthSet)
			{
				serializer._maxDepth = settings._maxDepth;
				serializer._maxDepthSet = settings._maxDepthSet;
			}
		}

		/// <summary>
		/// Populates the JSON values onto the target object.
		/// </summary>
		/// <param name="reader">The <see cref="T:System.IO.TextReader" /> that contains the JSON structure to read values from.</param>
		/// <param name="target">The target object to populate values onto.</param>
		// Token: 0x06000183 RID: 387 RVA: 0x00005540 File Offset: 0x00003740
		[DebuggerStepThrough]
		public void Populate(TextReader reader, object target)
		{
			this.Populate(new JsonTextReader(reader), target);
		}

		/// <summary>
		/// Populates the JSON values onto the target object.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> that contains the JSON structure to read values from.</param>
		/// <param name="target">The target object to populate values onto.</param>
		// Token: 0x06000184 RID: 388 RVA: 0x0000554F File Offset: 0x0000374F
		[DebuggerStepThrough]
		public void Populate(JsonReader reader, object target)
		{
			this.PopulateInternal(reader, target);
		}

		// Token: 0x06000185 RID: 389 RVA: 0x0000555C File Offset: 0x0000375C
		internal virtual void PopulateInternal(JsonReader reader, object target)
		{
			ValidationUtils.ArgumentNotNull(reader, "reader");
			ValidationUtils.ArgumentNotNull(target, "target");
			CultureInfo previousCulture;
			DateTimeZoneHandling? previousDateTimeZoneHandling;
			DateParseHandling? previousDateParseHandling;
			FloatParseHandling? previousFloatParseHandling;
			int? previousMaxDepth;
			string previousDateFormatString;
			this.SetupReader(reader, out previousCulture, out previousDateTimeZoneHandling, out previousDateParseHandling, out previousFloatParseHandling, out previousMaxDepth, out previousDateFormatString);
			TraceJsonReader traceJsonReader = ((this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Verbose) ? this.CreateTraceJsonReader(reader) : null);
			new JsonSerializerInternalReader(this).Populate(traceJsonReader ?? reader, target);
			if (traceJsonReader != null)
			{
				this.TraceWriter.Trace(TraceLevel.Verbose, traceJsonReader.GetDeserializedJsonMessage(), null);
			}
			this.ResetReader(reader, previousCulture, previousDateTimeZoneHandling, previousDateParseHandling, previousFloatParseHandling, previousMaxDepth, previousDateFormatString);
		}

		/// <summary>
		/// Deserializes the JSON structure contained by the specified <see cref="T:Newtonsoft.Json.JsonReader" />.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> that contains the JSON structure to deserialize.</param>
		/// <returns>The <see cref="T:System.Object" /> being deserialized.</returns>
		// Token: 0x06000186 RID: 390 RVA: 0x000055EE File Offset: 0x000037EE
		[DebuggerStepThrough]
		[return: Nullable(2)]
		public object Deserialize(JsonReader reader)
		{
			return this.Deserialize(reader, null);
		}

		/// <summary>
		/// Deserializes the JSON structure contained by the specified <see cref="T:System.IO.TextReader" />
		/// into an instance of the specified type.
		/// </summary>
		/// <param name="reader">The <see cref="T:System.IO.TextReader" /> containing the object.</param>
		/// <param name="objectType">The <see cref="T:System.Type" /> of object being deserialized.</param>
		/// <returns>The instance of <paramref name="objectType" /> being deserialized.</returns>
		// Token: 0x06000187 RID: 391 RVA: 0x000055F8 File Offset: 0x000037F8
		[DebuggerStepThrough]
		[return: Nullable(2)]
		public object Deserialize(TextReader reader, Type objectType)
		{
			return this.Deserialize(new JsonTextReader(reader), objectType);
		}

		/// <summary>
		/// Deserializes the JSON structure contained by the specified <see cref="T:Newtonsoft.Json.JsonReader" />
		/// into an instance of the specified type.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> containing the object.</param>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <returns>The instance of <typeparamref name="T" /> being deserialized.</returns>
		// Token: 0x06000188 RID: 392 RVA: 0x00005607 File Offset: 0x00003807
		[NullableContext(2)]
		[DebuggerStepThrough]
		public T Deserialize<T>([Nullable(1)] JsonReader reader)
		{
			return (T)((object)this.Deserialize(reader, typeof(T)));
		}

		/// <summary>
		/// Deserializes the JSON structure contained by the specified <see cref="T:Newtonsoft.Json.JsonReader" />
		/// into an instance of the specified type.
		/// </summary>
		/// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> containing the object.</param>
		/// <param name="objectType">The <see cref="T:System.Type" /> of object being deserialized.</param>
		/// <returns>The instance of <paramref name="objectType" /> being deserialized.</returns>
		// Token: 0x06000189 RID: 393 RVA: 0x0000561F File Offset: 0x0000381F
		[NullableContext(2)]
		[DebuggerStepThrough]
		public object Deserialize([Nullable(1)] JsonReader reader, Type objectType)
		{
			return this.DeserializeInternal(reader, objectType);
		}

		// Token: 0x0600018A RID: 394 RVA: 0x0000562C File Offset: 0x0000382C
		[NullableContext(2)]
		internal virtual object DeserializeInternal([Nullable(1)] JsonReader reader, Type objectType)
		{
			ValidationUtils.ArgumentNotNull(reader, "reader");
			CultureInfo previousCulture;
			DateTimeZoneHandling? previousDateTimeZoneHandling;
			DateParseHandling? previousDateParseHandling;
			FloatParseHandling? previousFloatParseHandling;
			int? previousMaxDepth;
			string previousDateFormatString;
			this.SetupReader(reader, out previousCulture, out previousDateTimeZoneHandling, out previousDateParseHandling, out previousFloatParseHandling, out previousMaxDepth, out previousDateFormatString);
			TraceJsonReader traceJsonReader = ((this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Verbose) ? this.CreateTraceJsonReader(reader) : null);
			object result = new JsonSerializerInternalReader(this).Deserialize(traceJsonReader ?? reader, objectType, this.CheckAdditionalContent);
			if (traceJsonReader != null)
			{
				this.TraceWriter.Trace(TraceLevel.Verbose, traceJsonReader.GetDeserializedJsonMessage(), null);
			}
			this.ResetReader(reader, previousCulture, previousDateTimeZoneHandling, previousDateParseHandling, previousFloatParseHandling, previousMaxDepth, previousDateFormatString);
			return result;
		}

		// Token: 0x0600018B RID: 395 RVA: 0x000056BC File Offset: 0x000038BC
		[NullableContext(2)]
		internal void SetupReader([Nullable(1)] JsonReader reader, out CultureInfo previousCulture, out DateTimeZoneHandling? previousDateTimeZoneHandling, out DateParseHandling? previousDateParseHandling, out FloatParseHandling? previousFloatParseHandling, out int? previousMaxDepth, out string previousDateFormatString)
		{
			if (this._culture != null && !this._culture.Equals(reader.Culture))
			{
				previousCulture = reader.Culture;
				reader.Culture = this._culture;
			}
			else
			{
				previousCulture = null;
			}
			if (this._dateTimeZoneHandling != null)
			{
				DateTimeZoneHandling dateTimeZoneHandling = reader.DateTimeZoneHandling;
				DateTimeZoneHandling? dateTimeZoneHandling2 = this._dateTimeZoneHandling;
				if (!((dateTimeZoneHandling == dateTimeZoneHandling2.GetValueOrDefault()) & (dateTimeZoneHandling2 != null)))
				{
					previousDateTimeZoneHandling = new DateTimeZoneHandling?(reader.DateTimeZoneHandling);
					reader.DateTimeZoneHandling = this._dateTimeZoneHandling.GetValueOrDefault();
					goto IL_8C;
				}
			}
			previousDateTimeZoneHandling = null;
			IL_8C:
			if (this._dateParseHandling != null)
			{
				DateParseHandling dateParseHandling = reader.DateParseHandling;
				DateParseHandling? dateParseHandling2 = this._dateParseHandling;
				if (!((dateParseHandling == dateParseHandling2.GetValueOrDefault()) & (dateParseHandling2 != null)))
				{
					previousDateParseHandling = new DateParseHandling?(reader.DateParseHandling);
					reader.DateParseHandling = this._dateParseHandling.GetValueOrDefault();
					goto IL_E6;
				}
			}
			previousDateParseHandling = null;
			IL_E6:
			if (this._floatParseHandling != null)
			{
				FloatParseHandling floatParseHandling = reader.FloatParseHandling;
				FloatParseHandling? floatParseHandling2 = this._floatParseHandling;
				if (!((floatParseHandling == floatParseHandling2.GetValueOrDefault()) & (floatParseHandling2 != null)))
				{
					previousFloatParseHandling = new FloatParseHandling?(reader.FloatParseHandling);
					reader.FloatParseHandling = this._floatParseHandling.GetValueOrDefault();
					goto IL_140;
				}
			}
			previousFloatParseHandling = null;
			IL_140:
			if (this._maxDepthSet)
			{
				int? maxDepth = reader.MaxDepth;
				int? maxDepth2 = this._maxDepth;
				if (!((maxDepth.GetValueOrDefault() == maxDepth2.GetValueOrDefault()) & (maxDepth != null == (maxDepth2 != null))))
				{
					previousMaxDepth = reader.MaxDepth;
					reader.MaxDepth = this._maxDepth;
					goto IL_19E;
				}
			}
			previousMaxDepth = null;
			IL_19E:
			if (this._dateFormatStringSet && reader.DateFormatString != this._dateFormatString)
			{
				previousDateFormatString = reader.DateFormatString;
				reader.DateFormatString = this._dateFormatString;
			}
			else
			{
				previousDateFormatString = null;
			}
			JsonTextReader jsonTextReader = reader as JsonTextReader;
			if (jsonTextReader != null && jsonTextReader.PropertyNameTable == null)
			{
				DefaultContractResolver defaultContractResolver = this._contractResolver as DefaultContractResolver;
				if (defaultContractResolver != null)
				{
					jsonTextReader.PropertyNameTable = defaultContractResolver.GetNameTable();
				}
			}
		}

		// Token: 0x0600018C RID: 396 RVA: 0x000058D0 File Offset: 0x00003AD0
		[NullableContext(2)]
		private void ResetReader([Nullable(1)] JsonReader reader, CultureInfo previousCulture, DateTimeZoneHandling? previousDateTimeZoneHandling, DateParseHandling? previousDateParseHandling, FloatParseHandling? previousFloatParseHandling, int? previousMaxDepth, string previousDateFormatString)
		{
			if (previousCulture != null)
			{
				reader.Culture = previousCulture;
			}
			if (previousDateTimeZoneHandling != null)
			{
				reader.DateTimeZoneHandling = previousDateTimeZoneHandling.GetValueOrDefault();
			}
			if (previousDateParseHandling != null)
			{
				reader.DateParseHandling = previousDateParseHandling.GetValueOrDefault();
			}
			if (previousFloatParseHandling != null)
			{
				reader.FloatParseHandling = previousFloatParseHandling.GetValueOrDefault();
			}
			if (this._maxDepthSet)
			{
				reader.MaxDepth = previousMaxDepth;
			}
			if (this._dateFormatStringSet)
			{
				reader.DateFormatString = previousDateFormatString;
			}
			JsonTextReader jsonTextReader = reader as JsonTextReader;
			if (jsonTextReader != null && jsonTextReader.PropertyNameTable != null)
			{
				DefaultContractResolver defaultContractResolver = this._contractResolver as DefaultContractResolver;
				if (defaultContractResolver != null && jsonTextReader.PropertyNameTable == defaultContractResolver.GetNameTable())
				{
					jsonTextReader.PropertyNameTable = null;
				}
			}
		}

		/// <summary>
		/// Serializes the specified <see cref="T:System.Object" /> and writes the JSON structure
		/// using the specified <see cref="T:System.IO.TextWriter" />.
		/// </summary>
		/// <param name="textWriter">The <see cref="T:System.IO.TextWriter" /> used to write the JSON structure.</param>
		/// <param name="value">The <see cref="T:System.Object" /> to serialize.</param>
		// Token: 0x0600018D RID: 397 RVA: 0x0000597F File Offset: 0x00003B7F
		public void Serialize(TextWriter textWriter, [Nullable(2)] object value)
		{
			this.Serialize(new JsonTextWriter(textWriter), value);
		}

		/// <summary>
		/// Serializes the specified <see cref="T:System.Object" /> and writes the JSON structure
		/// using the specified <see cref="T:Newtonsoft.Json.JsonWriter" />.
		/// </summary>
		/// <param name="jsonWriter">The <see cref="T:Newtonsoft.Json.JsonWriter" /> used to write the JSON structure.</param>
		/// <param name="value">The <see cref="T:System.Object" /> to serialize.</param>
		/// <param name="objectType">
		/// The type of the value being serialized.
		/// This parameter is used when <see cref="P:Newtonsoft.Json.JsonSerializer.TypeNameHandling" /> is <see cref="F:Newtonsoft.Json.TypeNameHandling.Auto" /> to write out the type name if the type of the value does not match.
		/// Specifying the type is optional.
		/// </param>
		// Token: 0x0600018E RID: 398 RVA: 0x0000598E File Offset: 0x00003B8E
		[NullableContext(2)]
		public void Serialize([Nullable(1)] JsonWriter jsonWriter, object value, Type objectType)
		{
			this.SerializeInternal(jsonWriter, value, objectType);
		}

		/// <summary>
		/// Serializes the specified <see cref="T:System.Object" /> and writes the JSON structure
		/// using the specified <see cref="T:System.IO.TextWriter" />.
		/// </summary>
		/// <param name="textWriter">The <see cref="T:System.IO.TextWriter" /> used to write the JSON structure.</param>
		/// <param name="value">The <see cref="T:System.Object" /> to serialize.</param>
		/// <param name="objectType">
		/// The type of the value being serialized.
		/// This parameter is used when <see cref="P:Newtonsoft.Json.JsonSerializer.TypeNameHandling" /> is Auto to write out the type name if the type of the value does not match.
		/// Specifying the type is optional.
		/// </param>
		// Token: 0x0600018F RID: 399 RVA: 0x00005999 File Offset: 0x00003B99
		public void Serialize(TextWriter textWriter, [Nullable(2)] object value, Type objectType)
		{
			this.Serialize(new JsonTextWriter(textWriter), value, objectType);
		}

		/// <summary>
		/// Serializes the specified <see cref="T:System.Object" /> and writes the JSON structure
		/// using the specified <see cref="T:Newtonsoft.Json.JsonWriter" />.
		/// </summary>
		/// <param name="jsonWriter">The <see cref="T:Newtonsoft.Json.JsonWriter" /> used to write the JSON structure.</param>
		/// <param name="value">The <see cref="T:System.Object" /> to serialize.</param>
		// Token: 0x06000190 RID: 400 RVA: 0x000059A9 File Offset: 0x00003BA9
		public void Serialize(JsonWriter jsonWriter, [Nullable(2)] object value)
		{
			this.SerializeInternal(jsonWriter, value, null);
		}

		// Token: 0x06000191 RID: 401 RVA: 0x000059B4 File Offset: 0x00003BB4
		private TraceJsonReader CreateTraceJsonReader(JsonReader reader)
		{
			TraceJsonReader traceJsonReader = new TraceJsonReader(reader);
			if (reader.TokenType != JsonToken.None)
			{
				traceJsonReader.WriteCurrentToken();
			}
			return traceJsonReader;
		}

		// Token: 0x06000192 RID: 402 RVA: 0x000059D8 File Offset: 0x00003BD8
		[NullableContext(2)]
		internal virtual void SerializeInternal([Nullable(1)] JsonWriter jsonWriter, object value, Type objectType)
		{
			ValidationUtils.ArgumentNotNull(jsonWriter, "jsonWriter");
			Formatting? formatting = null;
			if (this._formatting != null)
			{
				Formatting formatting2 = jsonWriter.Formatting;
				Formatting? formatting3 = this._formatting;
				if (!((formatting2 == formatting3.GetValueOrDefault()) & (formatting3 != null)))
				{
					formatting = new Formatting?(jsonWriter.Formatting);
					jsonWriter.Formatting = this._formatting.GetValueOrDefault();
				}
			}
			DateFormatHandling? dateFormatHandling = null;
			if (this._dateFormatHandling != null)
			{
				DateFormatHandling dateFormatHandling2 = jsonWriter.DateFormatHandling;
				DateFormatHandling? dateFormatHandling3 = this._dateFormatHandling;
				if (!((dateFormatHandling2 == dateFormatHandling3.GetValueOrDefault()) & (dateFormatHandling3 != null)))
				{
					dateFormatHandling = new DateFormatHandling?(jsonWriter.DateFormatHandling);
					jsonWriter.DateFormatHandling = this._dateFormatHandling.GetValueOrDefault();
				}
			}
			DateTimeZoneHandling? dateTimeZoneHandling = null;
			if (this._dateTimeZoneHandling != null)
			{
				DateTimeZoneHandling dateTimeZoneHandling2 = jsonWriter.DateTimeZoneHandling;
				DateTimeZoneHandling? dateTimeZoneHandling3 = this._dateTimeZoneHandling;
				if (!((dateTimeZoneHandling2 == dateTimeZoneHandling3.GetValueOrDefault()) & (dateTimeZoneHandling3 != null)))
				{
					dateTimeZoneHandling = new DateTimeZoneHandling?(jsonWriter.DateTimeZoneHandling);
					jsonWriter.DateTimeZoneHandling = this._dateTimeZoneHandling.GetValueOrDefault();
				}
			}
			FloatFormatHandling? floatFormatHandling = null;
			if (this._floatFormatHandling != null)
			{
				FloatFormatHandling floatFormatHandling2 = jsonWriter.FloatFormatHandling;
				FloatFormatHandling? floatFormatHandling3 = this._floatFormatHandling;
				if (!((floatFormatHandling2 == floatFormatHandling3.GetValueOrDefault()) & (floatFormatHandling3 != null)))
				{
					floatFormatHandling = new FloatFormatHandling?(jsonWriter.FloatFormatHandling);
					jsonWriter.FloatFormatHandling = this._floatFormatHandling.GetValueOrDefault();
				}
			}
			StringEscapeHandling? stringEscapeHandling = null;
			if (this._stringEscapeHandling != null)
			{
				StringEscapeHandling stringEscapeHandling2 = jsonWriter.StringEscapeHandling;
				StringEscapeHandling? stringEscapeHandling3 = this._stringEscapeHandling;
				if (!((stringEscapeHandling2 == stringEscapeHandling3.GetValueOrDefault()) & (stringEscapeHandling3 != null)))
				{
					stringEscapeHandling = new StringEscapeHandling?(jsonWriter.StringEscapeHandling);
					jsonWriter.StringEscapeHandling = this._stringEscapeHandling.GetValueOrDefault();
				}
			}
			CultureInfo cultureInfo = null;
			if (this._culture != null && !this._culture.Equals(jsonWriter.Culture))
			{
				cultureInfo = jsonWriter.Culture;
				jsonWriter.Culture = this._culture;
			}
			string dateFormatString = null;
			if (this._dateFormatStringSet && jsonWriter.DateFormatString != this._dateFormatString)
			{
				dateFormatString = jsonWriter.DateFormatString;
				jsonWriter.DateFormatString = this._dateFormatString;
			}
			TraceJsonWriter traceJsonWriter = ((this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Verbose) ? new TraceJsonWriter(jsonWriter) : null);
			new JsonSerializerInternalWriter(this).Serialize(traceJsonWriter ?? jsonWriter, value, objectType);
			if (traceJsonWriter != null)
			{
				this.TraceWriter.Trace(TraceLevel.Verbose, traceJsonWriter.GetSerializedJsonMessage(), null);
			}
			if (formatting != null)
			{
				jsonWriter.Formatting = formatting.GetValueOrDefault();
			}
			if (dateFormatHandling != null)
			{
				jsonWriter.DateFormatHandling = dateFormatHandling.GetValueOrDefault();
			}
			if (dateTimeZoneHandling != null)
			{
				jsonWriter.DateTimeZoneHandling = dateTimeZoneHandling.GetValueOrDefault();
			}
			if (floatFormatHandling != null)
			{
				jsonWriter.FloatFormatHandling = floatFormatHandling.GetValueOrDefault();
			}
			if (stringEscapeHandling != null)
			{
				jsonWriter.StringEscapeHandling = stringEscapeHandling.GetValueOrDefault();
			}
			if (this._dateFormatStringSet)
			{
				jsonWriter.DateFormatString = dateFormatString;
			}
			if (cultureInfo != null)
			{
				jsonWriter.Culture = cultureInfo;
			}
		}

		// Token: 0x06000193 RID: 403 RVA: 0x00005CCF File Offset: 0x00003ECF
		internal IReferenceResolver GetReferenceResolver()
		{
			if (this._referenceResolver == null)
			{
				this._referenceResolver = new DefaultReferenceResolver();
			}
			return this._referenceResolver;
		}

		// Token: 0x06000194 RID: 404 RVA: 0x00005CEA File Offset: 0x00003EEA
		[return: Nullable(2)]
		internal JsonConverter GetMatchingConverter(Type type)
		{
			return JsonSerializer.GetMatchingConverter(this._converters, type);
		}

		// Token: 0x06000195 RID: 405 RVA: 0x00005CF8 File Offset: 0x00003EF8
		[return: Nullable(2)]
		internal static JsonConverter GetMatchingConverter([Nullable(new byte[] { 2, 1 })] IList<JsonConverter> converters, Type objectType)
		{
			if (converters != null)
			{
				for (int i = 0; i < converters.Count; i++)
				{
					JsonConverter jsonConverter = converters[i];
					if (jsonConverter.CanConvert(objectType))
					{
						return jsonConverter;
					}
				}
			}
			return null;
		}

		// Token: 0x06000196 RID: 406 RVA: 0x00005D2D File Offset: 0x00003F2D
		internal void OnError(Newtonsoft.Json.Serialization.ErrorEventArgs e)
		{
			EventHandler<Newtonsoft.Json.Serialization.ErrorEventArgs> error = this.Error;
			if (error == null)
			{
				return;
			}
			error(this, e);
		}

		// Token: 0x04000073 RID: 115
		internal TypeNameHandling _typeNameHandling;

		// Token: 0x04000074 RID: 116
		internal TypeNameAssemblyFormatHandling _typeNameAssemblyFormatHandling;

		// Token: 0x04000075 RID: 117
		internal PreserveReferencesHandling _preserveReferencesHandling;

		// Token: 0x04000076 RID: 118
		internal ReferenceLoopHandling _referenceLoopHandling;

		// Token: 0x04000077 RID: 119
		internal MissingMemberHandling _missingMemberHandling;

		// Token: 0x04000078 RID: 120
		internal ObjectCreationHandling _objectCreationHandling;

		// Token: 0x04000079 RID: 121
		internal NullValueHandling _nullValueHandling;

		// Token: 0x0400007A RID: 122
		internal DefaultValueHandling _defaultValueHandling;

		// Token: 0x0400007B RID: 123
		internal ConstructorHandling _constructorHandling;

		// Token: 0x0400007C RID: 124
		internal MetadataPropertyHandling _metadataPropertyHandling;

		// Token: 0x0400007D RID: 125
		[Nullable(2)]
		internal JsonConverterCollection _converters;

		// Token: 0x0400007E RID: 126
		internal IContractResolver _contractResolver;

		// Token: 0x0400007F RID: 127
		[Nullable(2)]
		internal ITraceWriter _traceWriter;

		// Token: 0x04000080 RID: 128
		[Nullable(2)]
		internal IEqualityComparer _equalityComparer;

		// Token: 0x04000081 RID: 129
		internal ISerializationBinder _serializationBinder;

		// Token: 0x04000082 RID: 130
		internal StreamingContext _context;

		// Token: 0x04000083 RID: 131
		[Nullable(2)]
		private IReferenceResolver _referenceResolver;

		// Token: 0x04000084 RID: 132
		private Formatting? _formatting;

		// Token: 0x04000085 RID: 133
		private DateFormatHandling? _dateFormatHandling;

		// Token: 0x04000086 RID: 134
		private DateTimeZoneHandling? _dateTimeZoneHandling;

		// Token: 0x04000087 RID: 135
		private DateParseHandling? _dateParseHandling;

		// Token: 0x04000088 RID: 136
		private FloatFormatHandling? _floatFormatHandling;

		// Token: 0x04000089 RID: 137
		private FloatParseHandling? _floatParseHandling;

		// Token: 0x0400008A RID: 138
		private StringEscapeHandling? _stringEscapeHandling;

		// Token: 0x0400008B RID: 139
		private CultureInfo _culture;

		// Token: 0x0400008C RID: 140
		private int? _maxDepth;

		// Token: 0x0400008D RID: 141
		private bool _maxDepthSet;

		// Token: 0x0400008E RID: 142
		private bool? _checkAdditionalContent;

		// Token: 0x0400008F RID: 143
		[Nullable(2)]
		private string _dateFormatString;

		// Token: 0x04000090 RID: 144
		private bool _dateFormatStringSet;
	}
}
