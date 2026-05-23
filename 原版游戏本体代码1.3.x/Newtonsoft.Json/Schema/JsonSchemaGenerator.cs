using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Schema
{
	/// <summary>
	/// <para>
	/// Generates a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from a specified <see cref="T:System.Type" />.
	/// </para>
	/// <note type="caution">
	/// JSON Schema validation has been moved to its own package. See <see href="https://www.newtonsoft.com/jsonschema">https://www.newtonsoft.com/jsonschema</see> for more details.
	/// </note>
	/// </summary>
	// Token: 0x020000AB RID: 171
	[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
	public class JsonSchemaGenerator
	{
		/// <summary>
		/// Gets or sets how undefined schemas are handled by the serializer.
		/// </summary>
		// Token: 0x17000195 RID: 405
		// (get) Token: 0x06000908 RID: 2312 RVA: 0x000264DD File Offset: 0x000246DD
		// (set) Token: 0x06000909 RID: 2313 RVA: 0x000264E5 File Offset: 0x000246E5
		public UndefinedSchemaIdHandling UndefinedSchemaIdHandling { get; set; }

		/// <summary>
		/// Gets or sets the contract resolver.
		/// </summary>
		/// <value>The contract resolver.</value>
		// Token: 0x17000196 RID: 406
		// (get) Token: 0x0600090A RID: 2314 RVA: 0x000264EE File Offset: 0x000246EE
		// (set) Token: 0x0600090B RID: 2315 RVA: 0x00026504 File Offset: 0x00024704
		public IContractResolver ContractResolver
		{
			get
			{
				if (this._contractResolver == null)
				{
					return DefaultContractResolver.Instance;
				}
				return this._contractResolver;
			}
			set
			{
				this._contractResolver = value;
			}
		}

		// Token: 0x17000197 RID: 407
		// (get) Token: 0x0600090C RID: 2316 RVA: 0x0002650D File Offset: 0x0002470D
		private JsonSchema CurrentSchema
		{
			get
			{
				return this._currentSchema;
			}
		}

		// Token: 0x0600090D RID: 2317 RVA: 0x00026515 File Offset: 0x00024715
		private void Push(JsonSchemaGenerator.TypeSchema typeSchema)
		{
			this._currentSchema = typeSchema.Schema;
			this._stack.Add(typeSchema);
			this._resolver.LoadedSchemas.Add(typeSchema.Schema);
		}

		// Token: 0x0600090E RID: 2318 RVA: 0x00026548 File Offset: 0x00024748
		private JsonSchemaGenerator.TypeSchema Pop()
		{
			JsonSchemaGenerator.TypeSchema result = this._stack[this._stack.Count - 1];
			this._stack.RemoveAt(this._stack.Count - 1);
			JsonSchemaGenerator.TypeSchema typeSchema = this._stack.LastOrDefault<JsonSchemaGenerator.TypeSchema>();
			if (typeSchema != null)
			{
				this._currentSchema = typeSchema.Schema;
				return result;
			}
			this._currentSchema = null;
			return result;
		}

		/// <summary>
		/// Generate a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from the specified type.
		/// </summary>
		/// <param name="type">The type to generate a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> generated from the specified type.</returns>
		// Token: 0x0600090F RID: 2319 RVA: 0x000265A8 File Offset: 0x000247A8
		public JsonSchema Generate(Type type)
		{
			return this.Generate(type, new JsonSchemaResolver(), false);
		}

		/// <summary>
		/// Generate a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from the specified type.
		/// </summary>
		/// <param name="type">The type to generate a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from.</param>
		/// <param name="resolver">The <see cref="T:Newtonsoft.Json.Schema.JsonSchemaResolver" /> used to resolve schema references.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> generated from the specified type.</returns>
		// Token: 0x06000910 RID: 2320 RVA: 0x000265B7 File Offset: 0x000247B7
		public JsonSchema Generate(Type type, JsonSchemaResolver resolver)
		{
			return this.Generate(type, resolver, false);
		}

		/// <summary>
		/// Generate a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from the specified type.
		/// </summary>
		/// <param name="type">The type to generate a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from.</param>
		/// <param name="rootSchemaNullable">Specify whether the generated root <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> will be nullable.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> generated from the specified type.</returns>
		// Token: 0x06000911 RID: 2321 RVA: 0x000265C2 File Offset: 0x000247C2
		public JsonSchema Generate(Type type, bool rootSchemaNullable)
		{
			return this.Generate(type, new JsonSchemaResolver(), rootSchemaNullable);
		}

		/// <summary>
		/// Generate a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from the specified type.
		/// </summary>
		/// <param name="type">The type to generate a <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> from.</param>
		/// <param name="resolver">The <see cref="T:Newtonsoft.Json.Schema.JsonSchemaResolver" /> used to resolve schema references.</param>
		/// <param name="rootSchemaNullable">Specify whether the generated root <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> will be nullable.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Schema.JsonSchema" /> generated from the specified type.</returns>
		// Token: 0x06000912 RID: 2322 RVA: 0x000265D1 File Offset: 0x000247D1
		public JsonSchema Generate(Type type, JsonSchemaResolver resolver, bool rootSchemaNullable)
		{
			ValidationUtils.ArgumentNotNull(type, "type");
			ValidationUtils.ArgumentNotNull(resolver, "resolver");
			this._resolver = resolver;
			return this.GenerateInternal(type, (!rootSchemaNullable) ? Required.Always : Required.Default, false);
		}

		// Token: 0x06000913 RID: 2323 RVA: 0x00026600 File Offset: 0x00024800
		private string GetTitle(Type type)
		{
			JsonContainerAttribute cachedAttribute = JsonTypeReflector.GetCachedAttribute<JsonContainerAttribute>(type);
			if (!StringUtils.IsNullOrEmpty((cachedAttribute != null) ? cachedAttribute.Title : null))
			{
				return cachedAttribute.Title;
			}
			return null;
		}

		// Token: 0x06000914 RID: 2324 RVA: 0x00026630 File Offset: 0x00024830
		private string GetDescription(Type type)
		{
			JsonContainerAttribute cachedAttribute = JsonTypeReflector.GetCachedAttribute<JsonContainerAttribute>(type);
			if (!StringUtils.IsNullOrEmpty((cachedAttribute != null) ? cachedAttribute.Description : null))
			{
				return cachedAttribute.Description;
			}
			DescriptionAttribute attribute = ReflectionUtils.GetAttribute<DescriptionAttribute>(type);
			if (attribute == null)
			{
				return null;
			}
			return attribute.Description;
		}

		// Token: 0x06000915 RID: 2325 RVA: 0x00026670 File Offset: 0x00024870
		private string GetTypeId(Type type, bool explicitOnly)
		{
			JsonContainerAttribute cachedAttribute = JsonTypeReflector.GetCachedAttribute<JsonContainerAttribute>(type);
			if (!StringUtils.IsNullOrEmpty((cachedAttribute != null) ? cachedAttribute.Id : null))
			{
				return cachedAttribute.Id;
			}
			if (explicitOnly)
			{
				return null;
			}
			UndefinedSchemaIdHandling undefinedSchemaIdHandling = this.UndefinedSchemaIdHandling;
			if (undefinedSchemaIdHandling == UndefinedSchemaIdHandling.UseTypeName)
			{
				return type.FullName;
			}
			if (undefinedSchemaIdHandling != UndefinedSchemaIdHandling.UseAssemblyQualifiedName)
			{
				return null;
			}
			return type.AssemblyQualifiedName;
		}

		// Token: 0x06000916 RID: 2326 RVA: 0x000266C4 File Offset: 0x000248C4
		private JsonSchema GenerateInternal(Type type, Required valueRequired, bool required)
		{
			ValidationUtils.ArgumentNotNull(type, "type");
			string typeId = this.GetTypeId(type, false);
			string typeId2 = this.GetTypeId(type, true);
			if (!StringUtils.IsNullOrEmpty(typeId))
			{
				JsonSchema schema = this._resolver.GetSchema(typeId);
				if (schema != null)
				{
					if (valueRequired != Required.Always && !JsonSchemaGenerator.HasFlag(schema.Type, JsonSchemaType.Null))
					{
						schema.Type |= JsonSchemaType.Null;
					}
					if (required)
					{
						bool? required2 = schema.Required;
						bool flag = true;
						if (!((required2.GetValueOrDefault() == flag) & (required2 != null)))
						{
							schema.Required = new bool?(true);
						}
					}
					return schema;
				}
			}
			if (this._stack.Any((JsonSchemaGenerator.TypeSchema tc) => tc.Type == type))
			{
				throw new JsonException("Unresolved circular reference for type '{0}'. Explicitly define an Id for the type using a JsonObject/JsonArray attribute or automatically generate a type Id using the UndefinedSchemaIdHandling property.".FormatWith(CultureInfo.InvariantCulture, type));
			}
			JsonContract jsonContract = this.ContractResolver.ResolveContract(type);
			bool flag2 = (jsonContract.Converter ?? jsonContract.InternalConverter) != null;
			this.Push(new JsonSchemaGenerator.TypeSchema(type, new JsonSchema()));
			if (typeId2 != null)
			{
				this.CurrentSchema.Id = typeId2;
			}
			if (required)
			{
				this.CurrentSchema.Required = new bool?(true);
			}
			this.CurrentSchema.Title = this.GetTitle(type);
			this.CurrentSchema.Description = this.GetDescription(type);
			if (flag2)
			{
				this.CurrentSchema.Type = new JsonSchemaType?(JsonSchemaType.Any);
			}
			else
			{
				switch (jsonContract.ContractType)
				{
				case JsonContractType.Object:
					this.CurrentSchema.Type = new JsonSchemaType?(this.AddNullType(JsonSchemaType.Object, valueRequired));
					this.CurrentSchema.Id = this.GetTypeId(type, false);
					this.GenerateObjectSchema(type, (JsonObjectContract)jsonContract);
					break;
				case JsonContractType.Array:
				{
					this.CurrentSchema.Type = new JsonSchemaType?(this.AddNullType(JsonSchemaType.Array, valueRequired));
					this.CurrentSchema.Id = this.GetTypeId(type, false);
					JsonArrayAttribute cachedAttribute = JsonTypeReflector.GetCachedAttribute<JsonArrayAttribute>(type);
					bool flag3 = cachedAttribute == null || cachedAttribute.AllowNullItems;
					Type collectionItemType = ReflectionUtils.GetCollectionItemType(type);
					if (collectionItemType != null)
					{
						this.CurrentSchema.Items = new List<JsonSchema>();
						this.CurrentSchema.Items.Add(this.GenerateInternal(collectionItemType, (!flag3) ? Required.Always : Required.Default, false));
					}
					break;
				}
				case JsonContractType.Primitive:
				{
					this.CurrentSchema.Type = new JsonSchemaType?(this.GetJsonSchemaType(type, valueRequired));
					JsonSchemaType? type2 = this.CurrentSchema.Type;
					JsonSchemaType jsonSchemaType = JsonSchemaType.Integer;
					if (((type2.GetValueOrDefault() == jsonSchemaType) & (type2 != null)) && type.IsEnum() && !type.IsDefined(typeof(FlagsAttribute), true))
					{
						this.CurrentSchema.Enum = new List<JToken>();
						EnumInfo enumValuesAndNames = EnumUtils.GetEnumValuesAndNames(type);
						for (int i = 0; i < enumValuesAndNames.Names.Length; i++)
						{
							ulong value = enumValuesAndNames.Values[i];
							JToken item = JToken.FromObject(Enum.ToObject(type, value));
							this.CurrentSchema.Enum.Add(item);
						}
					}
					break;
				}
				case JsonContractType.String:
				{
					JsonSchemaType value2 = ((!ReflectionUtils.IsNullable(jsonContract.UnderlyingType)) ? JsonSchemaType.String : this.AddNullType(JsonSchemaType.String, valueRequired));
					this.CurrentSchema.Type = new JsonSchemaType?(value2);
					break;
				}
				case JsonContractType.Dictionary:
				{
					this.CurrentSchema.Type = new JsonSchemaType?(this.AddNullType(JsonSchemaType.Object, valueRequired));
					Type type3;
					Type type4;
					ReflectionUtils.GetDictionaryKeyValueTypes(type, out type3, out type4);
					if (type3 != null && this.ContractResolver.ResolveContract(type3).ContractType == JsonContractType.Primitive)
					{
						this.CurrentSchema.AdditionalProperties = this.GenerateInternal(type4, Required.Default, false);
					}
					break;
				}
				case JsonContractType.Dynamic:
				case JsonContractType.Linq:
					this.CurrentSchema.Type = new JsonSchemaType?(JsonSchemaType.Any);
					break;
				case JsonContractType.Serializable:
					this.CurrentSchema.Type = new JsonSchemaType?(this.AddNullType(JsonSchemaType.Object, valueRequired));
					this.CurrentSchema.Id = this.GetTypeId(type, false);
					this.GenerateISerializableContract(type, (JsonISerializableContract)jsonContract);
					break;
				default:
					throw new JsonException("Unexpected contract type: {0}".FormatWith(CultureInfo.InvariantCulture, jsonContract));
				}
			}
			return this.Pop().Schema;
		}

		// Token: 0x06000917 RID: 2327 RVA: 0x00026B7F File Offset: 0x00024D7F
		private JsonSchemaType AddNullType(JsonSchemaType type, Required valueRequired)
		{
			if (valueRequired != Required.Always)
			{
				return type | JsonSchemaType.Null;
			}
			return type;
		}

		// Token: 0x06000918 RID: 2328 RVA: 0x00026B8B File Offset: 0x00024D8B
		private bool HasFlag(DefaultValueHandling value, DefaultValueHandling flag)
		{
			return (value & flag) == flag;
		}

		// Token: 0x06000919 RID: 2329 RVA: 0x00026B94 File Offset: 0x00024D94
		private void GenerateObjectSchema(Type type, JsonObjectContract contract)
		{
			this.CurrentSchema.Properties = new Dictionary<string, JsonSchema>();
			foreach (JsonProperty jsonProperty in contract.Properties)
			{
				if (!jsonProperty.Ignored)
				{
					NullValueHandling? nullValueHandling = jsonProperty.NullValueHandling;
					NullValueHandling nullValueHandling2 = NullValueHandling.Ignore;
					bool flag = ((nullValueHandling.GetValueOrDefault() == nullValueHandling2) & (nullValueHandling != null)) || this.HasFlag(jsonProperty.DefaultValueHandling.GetValueOrDefault(), DefaultValueHandling.Ignore) || jsonProperty.ShouldSerialize != null || jsonProperty.GetIsSpecified != null;
					JsonSchema jsonSchema = this.GenerateInternal(jsonProperty.PropertyType, jsonProperty.Required, !flag);
					if (jsonProperty.DefaultValue != null)
					{
						jsonSchema.Default = JToken.FromObject(jsonProperty.DefaultValue);
					}
					this.CurrentSchema.Properties.Add(jsonProperty.PropertyName, jsonSchema);
				}
			}
			if (type.IsSealed())
			{
				this.CurrentSchema.AllowAdditionalProperties = false;
			}
		}

		// Token: 0x0600091A RID: 2330 RVA: 0x00026CA4 File Offset: 0x00024EA4
		private void GenerateISerializableContract(Type type, JsonISerializableContract contract)
		{
			this.CurrentSchema.AllowAdditionalProperties = true;
		}

		// Token: 0x0600091B RID: 2331 RVA: 0x00026CB4 File Offset: 0x00024EB4
		internal static bool HasFlag(JsonSchemaType? value, JsonSchemaType flag)
		{
			if (value == null)
			{
				return true;
			}
			JsonSchemaType? jsonSchemaType = value & flag;
			if ((jsonSchemaType.GetValueOrDefault() == flag) & (jsonSchemaType != null))
			{
				return true;
			}
			if (flag == JsonSchemaType.Integer)
			{
				jsonSchemaType = value & JsonSchemaType.Float;
				JsonSchemaType jsonSchemaType2 = JsonSchemaType.Float;
				if ((jsonSchemaType.GetValueOrDefault() == jsonSchemaType2) & (jsonSchemaType != null))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600091C RID: 2332 RVA: 0x00026D50 File Offset: 0x00024F50
		private JsonSchemaType GetJsonSchemaType(Type type, Required valueRequired)
		{
			JsonSchemaType jsonSchemaType = JsonSchemaType.None;
			if (valueRequired != Required.Always && ReflectionUtils.IsNullable(type))
			{
				jsonSchemaType = JsonSchemaType.Null;
				if (ReflectionUtils.IsNullableType(type))
				{
					type = Nullable.GetUnderlyingType(type);
				}
			}
			PrimitiveTypeCode typeCode = ConvertUtils.GetTypeCode(type);
			switch (typeCode)
			{
			case PrimitiveTypeCode.Empty:
			case PrimitiveTypeCode.Object:
				return jsonSchemaType | JsonSchemaType.String;
			case PrimitiveTypeCode.Char:
				return jsonSchemaType | JsonSchemaType.String;
			case PrimitiveTypeCode.Boolean:
				return jsonSchemaType | JsonSchemaType.Boolean;
			case PrimitiveTypeCode.SByte:
			case PrimitiveTypeCode.Int16:
			case PrimitiveTypeCode.UInt16:
			case PrimitiveTypeCode.Int32:
			case PrimitiveTypeCode.Byte:
			case PrimitiveTypeCode.UInt32:
			case PrimitiveTypeCode.Int64:
			case PrimitiveTypeCode.UInt64:
			case PrimitiveTypeCode.BigInteger:
				return jsonSchemaType | JsonSchemaType.Integer;
			case PrimitiveTypeCode.Single:
			case PrimitiveTypeCode.Double:
			case PrimitiveTypeCode.Decimal:
				return jsonSchemaType | JsonSchemaType.Float;
			case PrimitiveTypeCode.DateTime:
			case PrimitiveTypeCode.DateTimeOffset:
				return jsonSchemaType | JsonSchemaType.String;
			case PrimitiveTypeCode.Guid:
			case PrimitiveTypeCode.TimeSpan:
			case PrimitiveTypeCode.Uri:
			case PrimitiveTypeCode.String:
			case PrimitiveTypeCode.Bytes:
				return jsonSchemaType | JsonSchemaType.String;
			case PrimitiveTypeCode.DBNull:
				return jsonSchemaType | JsonSchemaType.Null;
			}
			throw new JsonException("Unexpected type code '{0}' for type '{1}'.".FormatWith(CultureInfo.InvariantCulture, typeCode, type));
		}

		// Token: 0x0400033B RID: 827
		private IContractResolver _contractResolver;

		// Token: 0x0400033C RID: 828
		private JsonSchemaResolver _resolver;

		// Token: 0x0400033D RID: 829
		private readonly IList<JsonSchemaGenerator.TypeSchema> _stack = new List<JsonSchemaGenerator.TypeSchema>();

		// Token: 0x0400033E RID: 830
		private JsonSchema _currentSchema;

		// Token: 0x020001B2 RID: 434
		private class TypeSchema
		{
			// Token: 0x17000297 RID: 663
			// (get) Token: 0x06000F5D RID: 3933 RVA: 0x0004374D File Offset: 0x0004194D
			public Type Type { get; }

			// Token: 0x17000298 RID: 664
			// (get) Token: 0x06000F5E RID: 3934 RVA: 0x00043755 File Offset: 0x00041955
			public JsonSchema Schema { get; }

			// Token: 0x06000F5F RID: 3935 RVA: 0x0004375D File Offset: 0x0004195D
			public TypeSchema(Type type, JsonSchema schema)
			{
				ValidationUtils.ArgumentNotNull(type, "type");
				ValidationUtils.ArgumentNotNull(schema, "schema");
				this.Type = type;
				this.Schema = schema;
			}
		}
	}
}
