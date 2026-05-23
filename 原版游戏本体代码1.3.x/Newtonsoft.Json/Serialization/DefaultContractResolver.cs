using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Used by <see cref="T:Newtonsoft.Json.JsonSerializer" /> to resolve a <see cref="T:Newtonsoft.Json.Serialization.JsonContract" /> for a given <see cref="T:System.Type" />.
	/// </summary>
	// Token: 0x02000075 RID: 117
	[NullableContext(1)]
	[Nullable(0)]
	public class DefaultContractResolver : IContractResolver
	{
		// Token: 0x170000D7 RID: 215
		// (get) Token: 0x0600061B RID: 1563 RVA: 0x00019C85 File Offset: 0x00017E85
		internal static IContractResolver Instance
		{
			get
			{
				return DefaultContractResolver._instance;
			}
		}

		/// <summary>
		/// Gets a value indicating whether members are being get and set using dynamic code generation.
		/// This value is determined by the runtime permissions available.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if using dynamic code generation; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x170000D8 RID: 216
		// (get) Token: 0x0600061C RID: 1564 RVA: 0x00019C8C File Offset: 0x00017E8C
		public bool DynamicCodeGeneration
		{
			get
			{
				return JsonTypeReflector.DynamicCodeGeneration;
			}
		}

		/// <summary>
		/// Gets or sets the default members search flags.
		/// </summary>
		/// <value>The default members search flags.</value>
		// Token: 0x170000D9 RID: 217
		// (get) Token: 0x0600061D RID: 1565 RVA: 0x00019C93 File Offset: 0x00017E93
		// (set) Token: 0x0600061E RID: 1566 RVA: 0x00019C9B File Offset: 0x00017E9B
		[Obsolete("DefaultMembersSearchFlags is obsolete. To modify the members serialized inherit from DefaultContractResolver and override the GetSerializableMembers method instead.")]
		public BindingFlags DefaultMembersSearchFlags { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether compiler generated members should be serialized.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if serialized compiler generated members; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x170000DA RID: 218
		// (get) Token: 0x0600061F RID: 1567 RVA: 0x00019CA4 File Offset: 0x00017EA4
		// (set) Token: 0x06000620 RID: 1568 RVA: 0x00019CAC File Offset: 0x00017EAC
		public bool SerializeCompilerGeneratedMembers { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to ignore the <see cref="T:System.Runtime.Serialization.ISerializable" /> interface when serializing and deserializing types.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the <see cref="T:System.Runtime.Serialization.ISerializable" /> interface will be ignored when serializing and deserializing types; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x170000DB RID: 219
		// (get) Token: 0x06000621 RID: 1569 RVA: 0x00019CB5 File Offset: 0x00017EB5
		// (set) Token: 0x06000622 RID: 1570 RVA: 0x00019CBD File Offset: 0x00017EBD
		public bool IgnoreSerializableInterface { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to ignore the <see cref="T:System.SerializableAttribute" /> attribute when serializing and deserializing types.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if the <see cref="T:System.SerializableAttribute" /> attribute will be ignored when serializing and deserializing types; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x170000DC RID: 220
		// (get) Token: 0x06000623 RID: 1571 RVA: 0x00019CC6 File Offset: 0x00017EC6
		// (set) Token: 0x06000624 RID: 1572 RVA: 0x00019CCE File Offset: 0x00017ECE
		public bool IgnoreSerializableAttribute { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to ignore IsSpecified members when serializing and deserializing types.
		/// </summary>
		/// <value>
		///     <c>true</c> if the IsSpecified members will be ignored when serializing and deserializing types; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x170000DD RID: 221
		// (get) Token: 0x06000625 RID: 1573 RVA: 0x00019CD7 File Offset: 0x00017ED7
		// (set) Token: 0x06000626 RID: 1574 RVA: 0x00019CDF File Offset: 0x00017EDF
		public bool IgnoreIsSpecifiedMembers { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to ignore ShouldSerialize members when serializing and deserializing types.
		/// </summary>
		/// <value>
		///     <c>true</c> if the ShouldSerialize members will be ignored when serializing and deserializing types; otherwise, <c>false</c>.
		/// </value>
		// Token: 0x170000DE RID: 222
		// (get) Token: 0x06000627 RID: 1575 RVA: 0x00019CE8 File Offset: 0x00017EE8
		// (set) Token: 0x06000628 RID: 1576 RVA: 0x00019CF0 File Offset: 0x00017EF0
		public bool IgnoreShouldSerializeMembers { get; set; }

		/// <summary>
		/// Gets or sets the naming strategy used to resolve how property names and dictionary keys are serialized.
		/// </summary>
		/// <value>The naming strategy used to resolve how property names and dictionary keys are serialized.</value>
		// Token: 0x170000DF RID: 223
		// (get) Token: 0x06000629 RID: 1577 RVA: 0x00019CF9 File Offset: 0x00017EF9
		// (set) Token: 0x0600062A RID: 1578 RVA: 0x00019D01 File Offset: 0x00017F01
		[Nullable(2)]
		public NamingStrategy NamingStrategy
		{
			[NullableContext(2)]
			get;
			[NullableContext(2)]
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.DefaultContractResolver" /> class.
		/// </summary>
		// Token: 0x0600062B RID: 1579 RVA: 0x00019D0A File Offset: 0x00017F0A
		public DefaultContractResolver()
		{
			this.IgnoreSerializableAttribute = true;
			this.DefaultMembersSearchFlags = BindingFlags.Instance | BindingFlags.Public;
			this._contractCache = new ThreadSafeStore<Type, JsonContract>(new Func<Type, JsonContract>(this.CreateContract));
		}

		/// <summary>
		/// Resolves the contract for a given type.
		/// </summary>
		/// <param name="type">The type to resolve a contract for.</param>
		/// <returns>The contract for a given type.</returns>
		// Token: 0x0600062C RID: 1580 RVA: 0x00019D44 File Offset: 0x00017F44
		public virtual JsonContract ResolveContract(Type type)
		{
			ValidationUtils.ArgumentNotNull(type, "type");
			return this._contractCache.Get(type);
		}

		// Token: 0x0600062D RID: 1581 RVA: 0x00019D60 File Offset: 0x00017F60
		private static bool FilterMembers(MemberInfo member)
		{
			PropertyInfo propertyInfo = member as PropertyInfo;
			if (propertyInfo != null)
			{
				return !ReflectionUtils.IsIndexedProperty(propertyInfo) && !ReflectionUtils.IsByRefLikeType(propertyInfo.PropertyType);
			}
			FieldInfo fieldInfo = member as FieldInfo;
			return fieldInfo == null || !ReflectionUtils.IsByRefLikeType(fieldInfo.FieldType);
		}

		/// <summary>
		/// Gets the serializable members for the type.
		/// </summary>
		/// <param name="objectType">The type to get serializable members for.</param>
		/// <returns>The serializable members for the type.</returns>
		// Token: 0x0600062E RID: 1582 RVA: 0x00019DAC File Offset: 0x00017FAC
		protected virtual List<MemberInfo> GetSerializableMembers(Type objectType)
		{
			bool ignoreSerializableAttribute = this.IgnoreSerializableAttribute;
			MemberSerialization objectMemberSerialization = JsonTypeReflector.GetObjectMemberSerialization(objectType, ignoreSerializableAttribute);
			IEnumerable<MemberInfo> enumerable = ReflectionUtils.GetFieldsAndProperties(objectType, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(delegate(MemberInfo m)
			{
				PropertyInfo propertyInfo = m as PropertyInfo;
				return propertyInfo == null || !ReflectionUtils.IsIndexedProperty(propertyInfo);
			});
			List<MemberInfo> list = new List<MemberInfo>();
			if (objectMemberSerialization != MemberSerialization.Fields)
			{
				DataContractAttribute dataContractAttribute = JsonTypeReflector.GetDataContractAttribute(objectType);
				List<MemberInfo> list2 = ReflectionUtils.GetFieldsAndProperties(objectType, this.DefaultMembersSearchFlags).Where(new Func<MemberInfo, bool>(DefaultContractResolver.FilterMembers)).ToList<MemberInfo>();
				foreach (MemberInfo memberInfo in enumerable)
				{
					if (this.SerializeCompilerGeneratedMembers || !memberInfo.IsDefined(typeof(CompilerGeneratedAttribute), true))
					{
						if (list2.Contains(memberInfo))
						{
							list.Add(memberInfo);
						}
						else if (JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(memberInfo) != null)
						{
							list.Add(memberInfo);
						}
						else if (JsonTypeReflector.GetAttribute<JsonRequiredAttribute>(memberInfo) != null)
						{
							list.Add(memberInfo);
						}
						else if (dataContractAttribute != null && JsonTypeReflector.GetAttribute<DataMemberAttribute>(memberInfo) != null)
						{
							list.Add(memberInfo);
						}
						else if (objectMemberSerialization == MemberSerialization.Fields && memberInfo.MemberType() == MemberTypes.Field)
						{
							list.Add(memberInfo);
						}
					}
				}
				Type type;
				if (objectType.AssignableToTypeName("System.Data.Objects.DataClasses.EntityObject", false, out type))
				{
					list = list.Where(new Func<MemberInfo, bool>(this.ShouldSerializeEntityMember)).ToList<MemberInfo>();
				}
				if (typeof(Exception).IsAssignableFrom(objectType))
				{
					list = (from m in list
						where !string.Equals(m.Name, "TargetSite", StringComparison.Ordinal)
						select m).ToList<MemberInfo>();
				}
			}
			else
			{
				foreach (MemberInfo memberInfo2 in enumerable)
				{
					FieldInfo fieldInfo = memberInfo2 as FieldInfo;
					if (fieldInfo != null && !fieldInfo.IsStatic)
					{
						list.Add(memberInfo2);
					}
				}
			}
			return list;
		}

		// Token: 0x0600062F RID: 1583 RVA: 0x00019FB0 File Offset: 0x000181B0
		private bool ShouldSerializeEntityMember(MemberInfo memberInfo)
		{
			PropertyInfo propertyInfo = memberInfo as PropertyInfo;
			return propertyInfo == null || !propertyInfo.PropertyType.IsGenericType() || !(propertyInfo.PropertyType.GetGenericTypeDefinition().FullName == "System.Data.Objects.DataClasses.EntityReference`1");
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonObjectContract" /> for the given type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Serialization.JsonObjectContract" /> for the given type.</returns>
		// Token: 0x06000630 RID: 1584 RVA: 0x00019FF4 File Offset: 0x000181F4
		protected virtual JsonObjectContract CreateObjectContract(Type objectType)
		{
			JsonObjectContract jsonObjectContract = new JsonObjectContract(objectType);
			this.InitializeContract(jsonObjectContract);
			bool ignoreSerializableAttribute = this.IgnoreSerializableAttribute;
			jsonObjectContract.MemberSerialization = JsonTypeReflector.GetObjectMemberSerialization(jsonObjectContract.NonNullableUnderlyingType, ignoreSerializableAttribute);
			jsonObjectContract.Properties.AddRange(this.CreateProperties(jsonObjectContract.NonNullableUnderlyingType, jsonObjectContract.MemberSerialization));
			Func<string, string> func = null;
			JsonObjectAttribute cachedAttribute = JsonTypeReflector.GetCachedAttribute<JsonObjectAttribute>(jsonObjectContract.NonNullableUnderlyingType);
			if (cachedAttribute != null)
			{
				jsonObjectContract.ItemRequired = cachedAttribute._itemRequired;
				jsonObjectContract.ItemNullValueHandling = cachedAttribute._itemNullValueHandling;
				jsonObjectContract.MissingMemberHandling = cachedAttribute._missingMemberHandling;
				if (cachedAttribute.NamingStrategyType != null)
				{
					NamingStrategy namingStrategy = JsonTypeReflector.GetContainerNamingStrategy(cachedAttribute);
					func = (string s) => namingStrategy.GetDictionaryKey(s);
				}
			}
			if (func == null)
			{
				func = new Func<string, string>(this.ResolveExtensionDataName);
			}
			jsonObjectContract.ExtensionDataNameResolver = func;
			if (jsonObjectContract.IsInstantiable)
			{
				ConstructorInfo attributeConstructor = this.GetAttributeConstructor(jsonObjectContract.NonNullableUnderlyingType);
				if (attributeConstructor != null)
				{
					jsonObjectContract.OverrideCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(attributeConstructor);
					jsonObjectContract.CreatorParameters.AddRange(this.CreateConstructorParameters(attributeConstructor, jsonObjectContract.Properties));
				}
				else if (jsonObjectContract.MemberSerialization == MemberSerialization.Fields)
				{
					if (JsonTypeReflector.FullyTrusted)
					{
						jsonObjectContract.DefaultCreator = new Func<object>(jsonObjectContract.GetUninitializedObject);
					}
				}
				else if (jsonObjectContract.DefaultCreator == null || jsonObjectContract.DefaultCreatorNonPublic)
				{
					ConstructorInfo parameterizedConstructor = this.GetParameterizedConstructor(jsonObjectContract.NonNullableUnderlyingType);
					if (parameterizedConstructor != null)
					{
						jsonObjectContract.ParameterizedCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(parameterizedConstructor);
						jsonObjectContract.CreatorParameters.AddRange(this.CreateConstructorParameters(parameterizedConstructor, jsonObjectContract.Properties));
					}
				}
				else if (jsonObjectContract.NonNullableUnderlyingType.IsValueType())
				{
					ConstructorInfo immutableConstructor = this.GetImmutableConstructor(jsonObjectContract.NonNullableUnderlyingType, jsonObjectContract.Properties);
					if (immutableConstructor != null)
					{
						jsonObjectContract.OverrideCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(immutableConstructor);
						jsonObjectContract.CreatorParameters.AddRange(this.CreateConstructorParameters(immutableConstructor, jsonObjectContract.Properties));
					}
				}
			}
			MemberInfo extensionDataMemberForType = this.GetExtensionDataMemberForType(jsonObjectContract.NonNullableUnderlyingType);
			if (extensionDataMemberForType != null)
			{
				DefaultContractResolver.SetExtensionDataDelegates(jsonObjectContract, extensionDataMemberForType);
			}
			if (Array.IndexOf<string>(DefaultContractResolver.BlacklistedTypeNames, objectType.FullName) != -1)
			{
				jsonObjectContract.OnSerializingCallbacks.Add(new SerializationCallback(DefaultContractResolver.ThrowUnableToSerializeError));
			}
			return jsonObjectContract;
		}

		// Token: 0x06000631 RID: 1585 RVA: 0x0001A233 File Offset: 0x00018433
		private static void ThrowUnableToSerializeError(object o, StreamingContext context)
		{
			throw new JsonSerializationException("Unable to serialize instance of '{0}'.".FormatWith(CultureInfo.InvariantCulture, o.GetType()));
		}

		// Token: 0x06000632 RID: 1586 RVA: 0x0001A250 File Offset: 0x00018450
		[return: Nullable(2)]
		private MemberInfo GetExtensionDataMemberForType(Type type)
		{
			return this.GetClassHierarchyForType(type).SelectMany(delegate(Type baseType)
			{
				List<MemberInfo> list = new List<MemberInfo>();
				list.AddRange(baseType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
				list.AddRange(baseType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
				return list;
			}).LastOrDefault(delegate(MemberInfo m)
			{
				MemberTypes memberTypes = m.MemberType();
				if (memberTypes != MemberTypes.Property && memberTypes != MemberTypes.Field)
				{
					return false;
				}
				if (!m.IsDefined(typeof(JsonExtensionDataAttribute), false))
				{
					return false;
				}
				if (!ReflectionUtils.CanReadMemberValue(m, true))
				{
					throw new JsonException("Invalid extension data attribute on '{0}'. Member '{1}' must have a getter.".FormatWith(CultureInfo.InvariantCulture, DefaultContractResolver.GetClrTypeFullName(m.DeclaringType), m.Name));
				}
				Type type2;
				if (ReflectionUtils.ImplementsGenericDefinition(ReflectionUtils.GetMemberUnderlyingType(m), typeof(IDictionary<, >), out type2))
				{
					Type type3 = type2.GetGenericArguments()[0];
					Type type4 = type2.GetGenericArguments()[1];
					if (type3.IsAssignableFrom(typeof(string)) && type4.IsAssignableFrom(typeof(JToken)))
					{
						return true;
					}
				}
				throw new JsonException("Invalid extension data attribute on '{0}'. Member '{1}' type must implement IDictionary<string, JToken>.".FormatWith(CultureInfo.InvariantCulture, DefaultContractResolver.GetClrTypeFullName(m.DeclaringType), m.Name));
			});
		}

		// Token: 0x06000633 RID: 1587 RVA: 0x0001A2AC File Offset: 0x000184AC
		private static void SetExtensionDataDelegates(JsonObjectContract contract, MemberInfo member)
		{
			JsonExtensionDataAttribute attribute = ReflectionUtils.GetAttribute<JsonExtensionDataAttribute>(member);
			if (attribute == null)
			{
				return;
			}
			Type memberUnderlyingType = ReflectionUtils.GetMemberUnderlyingType(member);
			Type type;
			ReflectionUtils.ImplementsGenericDefinition(memberUnderlyingType, typeof(IDictionary<, >), out type);
			Type type2 = type.GetGenericArguments()[0];
			Type type3 = type.GetGenericArguments()[1];
			Type type4;
			if (ReflectionUtils.IsGenericDefinition(memberUnderlyingType, typeof(IDictionary<, >)))
			{
				type4 = typeof(Dictionary<, >).MakeGenericType(new Type[] { type2, type3 });
			}
			else
			{
				type4 = memberUnderlyingType;
			}
			Func<object, object> getExtensionDataDictionary = JsonTypeReflector.ReflectionDelegateFactory.CreateGet<object>(member);
			if (attribute.ReadData)
			{
				Action<object, object> setExtensionDataDictionary = (ReflectionUtils.CanSetMemberValue(member, true, false) ? JsonTypeReflector.ReflectionDelegateFactory.CreateSet<object>(member) : null);
				Func<object> createExtensionDataDictionary = JsonTypeReflector.ReflectionDelegateFactory.CreateDefaultConstructor<object>(type4);
				PropertyInfo property = memberUnderlyingType.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public, null, type3, new Type[] { type2 }, null);
				MethodInfo methodInfo = ((property != null) ? property.GetSetMethod() : null);
				if (methodInfo == null)
				{
					PropertyInfo property2 = type.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public, null, type3, new Type[] { type2 }, null);
					methodInfo = ((property2 != null) ? property2.GetSetMethod() : null);
				}
				MethodCall<object, object> setExtensionDataDictionaryValue = JsonTypeReflector.ReflectionDelegateFactory.CreateMethodCall<object>(methodInfo);
				ExtensionDataSetter extensionDataSetter = delegate(object o, string key, [Nullable(2)] object value)
				{
					object obj = getExtensionDataDictionary(o);
					if (obj == null)
					{
						if (setExtensionDataDictionary == null)
						{
							throw new JsonSerializationException("Cannot set value onto extension data member '{0}'. The extension data collection is null and it cannot be set.".FormatWith(CultureInfo.InvariantCulture, member.Name));
						}
						obj = createExtensionDataDictionary();
						setExtensionDataDictionary(o, obj);
					}
					setExtensionDataDictionaryValue(obj, new object[] { key, value });
				};
				contract.ExtensionDataSetter = extensionDataSetter;
			}
			if (attribute.WriteData)
			{
				ConstructorInfo method = typeof(DefaultContractResolver.EnumerableDictionaryWrapper<, >).MakeGenericType(new Type[] { type2, type3 }).GetConstructors().First<ConstructorInfo>();
				ObjectConstructor<object> createEnumerableWrapper = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(method);
				ExtensionDataGetter extensionDataGetter = delegate(object o)
				{
					object obj = getExtensionDataDictionary(o);
					if (obj == null)
					{
						return null;
					}
					return (IEnumerable<KeyValuePair<object, object>>)createEnumerableWrapper(new object[] { obj });
				};
				contract.ExtensionDataGetter = extensionDataGetter;
			}
			contract.ExtensionDataValueType = type3;
		}

		// Token: 0x06000634 RID: 1588 RVA: 0x0001A4B8 File Offset: 0x000186B8
		[return: Nullable(2)]
		private ConstructorInfo GetAttributeConstructor(Type objectType)
		{
			IEnumerator<ConstructorInfo> enumerator = (from c in objectType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				where c.IsDefined(typeof(JsonConstructorAttribute), true)
				select c).GetEnumerator();
			if (enumerator.MoveNext())
			{
				ConstructorInfo result = enumerator.Current;
				if (enumerator.MoveNext())
				{
					throw new JsonException("Multiple constructors with the JsonConstructorAttribute.");
				}
				return result;
			}
			else
			{
				if (objectType == typeof(Version))
				{
					return objectType.GetConstructor(new Type[]
					{
						typeof(int),
						typeof(int),
						typeof(int),
						typeof(int)
					});
				}
				return null;
			}
		}

		// Token: 0x06000635 RID: 1589 RVA: 0x0001A570 File Offset: 0x00018770
		[return: Nullable(2)]
		private ConstructorInfo GetImmutableConstructor(Type objectType, JsonPropertyCollection memberProperties)
		{
			IEnumerator<ConstructorInfo> enumerator = ((IEnumerable<ConstructorInfo>)objectType.GetConstructors()).GetEnumerator();
			if (enumerator.MoveNext())
			{
				ConstructorInfo constructorInfo = enumerator.Current;
				if (!enumerator.MoveNext())
				{
					ParameterInfo[] parameters = constructorInfo.GetParameters();
					if (parameters.Length != 0)
					{
						foreach (ParameterInfo parameterInfo in parameters)
						{
							JsonProperty jsonProperty = this.MatchProperty(memberProperties, parameterInfo.Name, parameterInfo.ParameterType);
							if (jsonProperty == null || jsonProperty.Writable)
							{
								return null;
							}
						}
						return constructorInfo;
					}
				}
			}
			return null;
		}

		// Token: 0x06000636 RID: 1590 RVA: 0x0001A5F0 File Offset: 0x000187F0
		[return: Nullable(2)]
		private ConstructorInfo GetParameterizedConstructor(Type objectType)
		{
			ConstructorInfo[] constructors = objectType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
			if (constructors.Length == 1)
			{
				return constructors[0];
			}
			return null;
		}

		/// <summary>
		/// Creates the constructor parameters.
		/// </summary>
		/// <param name="constructor">The constructor to create properties for.</param>
		/// <param name="memberProperties">The type's member properties.</param>
		/// <returns>Properties for the given <see cref="T:System.Reflection.ConstructorInfo" />.</returns>
		// Token: 0x06000637 RID: 1591 RVA: 0x0001A614 File Offset: 0x00018814
		protected virtual IList<JsonProperty> CreateConstructorParameters(ConstructorInfo constructor, JsonPropertyCollection memberProperties)
		{
			ParameterInfo[] parameters = constructor.GetParameters();
			JsonPropertyCollection jsonPropertyCollection = new JsonPropertyCollection(constructor.DeclaringType);
			foreach (ParameterInfo parameterInfo in parameters)
			{
				if (parameterInfo.Name != null)
				{
					JsonProperty jsonProperty = this.MatchProperty(memberProperties, parameterInfo.Name, parameterInfo.ParameterType);
					if (jsonProperty != null || parameterInfo.Name != null)
					{
						JsonProperty jsonProperty2 = this.CreatePropertyFromConstructorParameter(jsonProperty, parameterInfo);
						if (jsonProperty2 != null)
						{
							jsonPropertyCollection.AddProperty(jsonProperty2);
						}
					}
				}
			}
			return jsonPropertyCollection;
		}

		// Token: 0x06000638 RID: 1592 RVA: 0x0001A688 File Offset: 0x00018888
		[return: Nullable(2)]
		private JsonProperty MatchProperty(JsonPropertyCollection properties, string name, Type type)
		{
			if (name == null)
			{
				return null;
			}
			JsonProperty closestMatchProperty = properties.GetClosestMatchProperty(name);
			if (closestMatchProperty == null || closestMatchProperty.PropertyType != type)
			{
				return null;
			}
			return closestMatchProperty;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> for the given <see cref="T:System.Reflection.ParameterInfo" />.
		/// </summary>
		/// <param name="matchingMemberProperty">The matching member property.</param>
		/// <param name="parameterInfo">The constructor parameter.</param>
		/// <returns>A created <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> for the given <see cref="T:System.Reflection.ParameterInfo" />.</returns>
		// Token: 0x06000639 RID: 1593 RVA: 0x0001A6B8 File Offset: 0x000188B8
		protected virtual JsonProperty CreatePropertyFromConstructorParameter([Nullable(2)] JsonProperty matchingMemberProperty, ParameterInfo parameterInfo)
		{
			JsonProperty jsonProperty = new JsonProperty();
			jsonProperty.PropertyType = parameterInfo.ParameterType;
			jsonProperty.AttributeProvider = new ReflectionAttributeProvider(parameterInfo);
			bool flag;
			this.SetPropertySettingsFromAttributes(jsonProperty, parameterInfo, parameterInfo.Name, parameterInfo.Member.DeclaringType, MemberSerialization.OptOut, out flag);
			jsonProperty.Readable = false;
			jsonProperty.Writable = true;
			if (matchingMemberProperty != null)
			{
				jsonProperty.PropertyName = ((jsonProperty.PropertyName != parameterInfo.Name) ? jsonProperty.PropertyName : matchingMemberProperty.PropertyName);
				jsonProperty.Converter = jsonProperty.Converter ?? matchingMemberProperty.Converter;
				if (!jsonProperty._hasExplicitDefaultValue && matchingMemberProperty._hasExplicitDefaultValue)
				{
					jsonProperty.DefaultValue = matchingMemberProperty.DefaultValue;
				}
				JsonProperty jsonProperty2 = jsonProperty;
				Required? required = jsonProperty._required;
				jsonProperty2._required = ((required != null) ? required : matchingMemberProperty._required);
				JsonProperty jsonProperty3 = jsonProperty;
				bool? isReference = jsonProperty.IsReference;
				jsonProperty3.IsReference = ((isReference != null) ? isReference : matchingMemberProperty.IsReference);
				JsonProperty jsonProperty4 = jsonProperty;
				NullValueHandling? nullValueHandling = jsonProperty.NullValueHandling;
				jsonProperty4.NullValueHandling = ((nullValueHandling != null) ? nullValueHandling : matchingMemberProperty.NullValueHandling);
				JsonProperty jsonProperty5 = jsonProperty;
				DefaultValueHandling? defaultValueHandling = jsonProperty.DefaultValueHandling;
				jsonProperty5.DefaultValueHandling = ((defaultValueHandling != null) ? defaultValueHandling : matchingMemberProperty.DefaultValueHandling);
				JsonProperty jsonProperty6 = jsonProperty;
				ReferenceLoopHandling? referenceLoopHandling = jsonProperty.ReferenceLoopHandling;
				jsonProperty6.ReferenceLoopHandling = ((referenceLoopHandling != null) ? referenceLoopHandling : matchingMemberProperty.ReferenceLoopHandling);
				JsonProperty jsonProperty7 = jsonProperty;
				ObjectCreationHandling? objectCreationHandling = jsonProperty.ObjectCreationHandling;
				jsonProperty7.ObjectCreationHandling = ((objectCreationHandling != null) ? objectCreationHandling : matchingMemberProperty.ObjectCreationHandling);
				JsonProperty jsonProperty8 = jsonProperty;
				TypeNameHandling? typeNameHandling = jsonProperty.TypeNameHandling;
				jsonProperty8.TypeNameHandling = ((typeNameHandling != null) ? typeNameHandling : matchingMemberProperty.TypeNameHandling);
			}
			return jsonProperty;
		}

		/// <summary>
		/// Resolves the default <see cref="T:Newtonsoft.Json.JsonConverter" /> for the contract.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>The contract's default <see cref="T:Newtonsoft.Json.JsonConverter" />.</returns>
		// Token: 0x0600063A RID: 1594 RVA: 0x0001A850 File Offset: 0x00018A50
		[return: Nullable(2)]
		protected virtual JsonConverter ResolveContractConverter(Type objectType)
		{
			return JsonTypeReflector.GetJsonConverter(objectType);
		}

		// Token: 0x0600063B RID: 1595 RVA: 0x0001A858 File Offset: 0x00018A58
		private Func<object> GetDefaultCreator(Type createdType)
		{
			return JsonTypeReflector.ReflectionDelegateFactory.CreateDefaultConstructor<object>(createdType);
		}

		// Token: 0x0600063C RID: 1596 RVA: 0x0001A868 File Offset: 0x00018A68
		private void InitializeContract(JsonContract contract)
		{
			JsonContainerAttribute cachedAttribute = JsonTypeReflector.GetCachedAttribute<JsonContainerAttribute>(contract.NonNullableUnderlyingType);
			if (cachedAttribute != null)
			{
				contract.IsReference = cachedAttribute._isReference;
			}
			else
			{
				DataContractAttribute dataContractAttribute = JsonTypeReflector.GetDataContractAttribute(contract.NonNullableUnderlyingType);
				if (dataContractAttribute != null && dataContractAttribute.IsReference)
				{
					contract.IsReference = new bool?(true);
				}
			}
			contract.Converter = this.ResolveContractConverter(contract.NonNullableUnderlyingType);
			contract.InternalConverter = JsonSerializer.GetMatchingConverter(DefaultContractResolver.BuiltInConverters, contract.NonNullableUnderlyingType);
			if (contract.IsInstantiable && (ReflectionUtils.HasDefaultConstructor(contract.CreatedType, true) || contract.CreatedType.IsValueType()))
			{
				contract.DefaultCreator = this.GetDefaultCreator(contract.CreatedType);
				contract.DefaultCreatorNonPublic = !contract.CreatedType.IsValueType() && ReflectionUtils.GetDefaultConstructor(contract.CreatedType) == null;
			}
			this.ResolveCallbackMethods(contract, contract.NonNullableUnderlyingType);
		}

		// Token: 0x0600063D RID: 1597 RVA: 0x0001A948 File Offset: 0x00018B48
		private void ResolveCallbackMethods(JsonContract contract, Type t)
		{
			List<SerializationCallback> list;
			List<SerializationCallback> list2;
			List<SerializationCallback> list3;
			List<SerializationCallback> list4;
			List<SerializationErrorCallback> list5;
			this.GetCallbackMethodsForType(t, out list, out list2, out list3, out list4, out list5);
			if (list != null)
			{
				contract.OnSerializingCallbacks.AddRange(list);
			}
			if (list2 != null)
			{
				contract.OnSerializedCallbacks.AddRange(list2);
			}
			if (list3 != null)
			{
				contract.OnDeserializingCallbacks.AddRange(list3);
			}
			if (list4 != null)
			{
				contract.OnDeserializedCallbacks.AddRange(list4);
			}
			if (list5 != null)
			{
				contract.OnErrorCallbacks.AddRange(list5);
			}
		}

		// Token: 0x0600063E RID: 1598 RVA: 0x0001A9B4 File Offset: 0x00018BB4
		private void GetCallbackMethodsForType(Type type, [Nullable(new byte[] { 2, 1 })] out List<SerializationCallback> onSerializing, [Nullable(new byte[] { 2, 1 })] out List<SerializationCallback> onSerialized, [Nullable(new byte[] { 2, 1 })] out List<SerializationCallback> onDeserializing, [Nullable(new byte[] { 2, 1 })] out List<SerializationCallback> onDeserialized, [Nullable(new byte[] { 2, 1 })] out List<SerializationErrorCallback> onError)
		{
			onSerializing = null;
			onSerialized = null;
			onDeserializing = null;
			onDeserialized = null;
			onError = null;
			foreach (Type type2 in this.GetClassHierarchyForType(type))
			{
				MethodInfo currentCallback = null;
				MethodInfo currentCallback2 = null;
				MethodInfo currentCallback3 = null;
				MethodInfo currentCallback4 = null;
				MethodInfo currentCallback5 = null;
				bool flag = DefaultContractResolver.ShouldSkipSerializing(type2);
				bool flag2 = DefaultContractResolver.ShouldSkipDeserialized(type2);
				foreach (MethodInfo methodInfo in type2.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					if (!methodInfo.ContainsGenericParameters)
					{
						Type type3 = null;
						ParameterInfo[] parameters = methodInfo.GetParameters();
						if (!flag && DefaultContractResolver.IsValidCallback(methodInfo, parameters, typeof(OnSerializingAttribute), currentCallback, ref type3))
						{
							onSerializing = onSerializing ?? new List<SerializationCallback>();
							onSerializing.Add(JsonContract.CreateSerializationCallback(methodInfo));
							currentCallback = methodInfo;
						}
						if (DefaultContractResolver.IsValidCallback(methodInfo, parameters, typeof(OnSerializedAttribute), currentCallback2, ref type3))
						{
							onSerialized = onSerialized ?? new List<SerializationCallback>();
							onSerialized.Add(JsonContract.CreateSerializationCallback(methodInfo));
							currentCallback2 = methodInfo;
						}
						if (DefaultContractResolver.IsValidCallback(methodInfo, parameters, typeof(OnDeserializingAttribute), currentCallback3, ref type3))
						{
							onDeserializing = onDeserializing ?? new List<SerializationCallback>();
							onDeserializing.Add(JsonContract.CreateSerializationCallback(methodInfo));
							currentCallback3 = methodInfo;
						}
						if (!flag2 && DefaultContractResolver.IsValidCallback(methodInfo, parameters, typeof(OnDeserializedAttribute), currentCallback4, ref type3))
						{
							onDeserialized = onDeserialized ?? new List<SerializationCallback>();
							onDeserialized.Add(JsonContract.CreateSerializationCallback(methodInfo));
							currentCallback4 = methodInfo;
						}
						if (DefaultContractResolver.IsValidCallback(methodInfo, parameters, typeof(OnErrorAttribute), currentCallback5, ref type3))
						{
							onError = onError ?? new List<SerializationErrorCallback>();
							onError.Add(JsonContract.CreateSerializationErrorCallback(methodInfo));
							currentCallback5 = methodInfo;
						}
					}
				}
			}
		}

		// Token: 0x0600063F RID: 1599 RVA: 0x0001ABA4 File Offset: 0x00018DA4
		private static bool IsConcurrentOrObservableCollection(Type t)
		{
			if (t.IsGenericType())
			{
				string fullName = t.GetGenericTypeDefinition().FullName;
				if (fullName == "System.Collections.Concurrent.ConcurrentQueue`1" || fullName == "System.Collections.Concurrent.ConcurrentStack`1" || fullName == "System.Collections.Concurrent.ConcurrentBag`1" || fullName == "System.Collections.Concurrent.ConcurrentDictionary`2" || fullName == "System.Collections.ObjectModel.ObservableCollection`1")
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000640 RID: 1600 RVA: 0x0001AC09 File Offset: 0x00018E09
		private static bool ShouldSkipDeserialized(Type t)
		{
			return DefaultContractResolver.IsConcurrentOrObservableCollection(t) || (t.Name == "FSharpSet`1" || t.Name == "FSharpMap`2");
		}

		// Token: 0x06000641 RID: 1601 RVA: 0x0001AC3C File Offset: 0x00018E3C
		private static bool ShouldSkipSerializing(Type t)
		{
			return DefaultContractResolver.IsConcurrentOrObservableCollection(t) || (t.Name == "FSharpSet`1" || t.Name == "FSharpMap`2");
		}

		// Token: 0x06000642 RID: 1602 RVA: 0x0001AC70 File Offset: 0x00018E70
		private List<Type> GetClassHierarchyForType(Type type)
		{
			List<Type> list = new List<Type>();
			Type type2 = type;
			while (type2 != null && type2 != typeof(object))
			{
				list.Add(type2);
				type2 = type2.BaseType();
			}
			list.Reverse();
			return list;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonDictionaryContract" /> for the given type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Serialization.JsonDictionaryContract" /> for the given type.</returns>
		// Token: 0x06000643 RID: 1603 RVA: 0x0001ACB8 File Offset: 0x00018EB8
		protected virtual JsonDictionaryContract CreateDictionaryContract(Type objectType)
		{
			JsonDictionaryContract jsonDictionaryContract = new JsonDictionaryContract(objectType);
			this.InitializeContract(jsonDictionaryContract);
			JsonContainerAttribute attribute = JsonTypeReflector.GetAttribute<JsonContainerAttribute>(objectType);
			if (((attribute != null) ? attribute.NamingStrategyType : null) != null)
			{
				NamingStrategy namingStrategy = JsonTypeReflector.GetContainerNamingStrategy(attribute);
				jsonDictionaryContract.DictionaryKeyResolver = (string s) => namingStrategy.GetDictionaryKey(s);
			}
			else
			{
				jsonDictionaryContract.DictionaryKeyResolver = new Func<string, string>(this.ResolveDictionaryKey);
			}
			ConstructorInfo attributeConstructor = this.GetAttributeConstructor(jsonDictionaryContract.NonNullableUnderlyingType);
			if (attributeConstructor != null)
			{
				ParameterInfo[] parameters = attributeConstructor.GetParameters();
				Type type = ((jsonDictionaryContract.DictionaryKeyType != null && jsonDictionaryContract.DictionaryValueType != null) ? typeof(IEnumerable<>).MakeGenericType(new Type[] { typeof(KeyValuePair<, >).MakeGenericType(new Type[] { jsonDictionaryContract.DictionaryKeyType, jsonDictionaryContract.DictionaryValueType }) }) : typeof(IDictionary));
				if (parameters.Length == 0)
				{
					jsonDictionaryContract.HasParameterizedCreator = false;
				}
				else
				{
					if (parameters.Length != 1 || !type.IsAssignableFrom(parameters[0].ParameterType))
					{
						throw new JsonException("Constructor for '{0}' must have no parameters or a single parameter that implements '{1}'.".FormatWith(CultureInfo.InvariantCulture, jsonDictionaryContract.UnderlyingType, type));
					}
					jsonDictionaryContract.HasParameterizedCreator = true;
				}
				jsonDictionaryContract.OverrideCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(attributeConstructor);
			}
			return jsonDictionaryContract;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonArrayContract" /> for the given type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Serialization.JsonArrayContract" /> for the given type.</returns>
		// Token: 0x06000644 RID: 1604 RVA: 0x0001AE10 File Offset: 0x00019010
		protected virtual JsonArrayContract CreateArrayContract(Type objectType)
		{
			JsonArrayContract jsonArrayContract = new JsonArrayContract(objectType);
			this.InitializeContract(jsonArrayContract);
			ConstructorInfo attributeConstructor = this.GetAttributeConstructor(jsonArrayContract.NonNullableUnderlyingType);
			if (attributeConstructor != null)
			{
				ParameterInfo[] parameters = attributeConstructor.GetParameters();
				Type type = ((jsonArrayContract.CollectionItemType != null) ? typeof(IEnumerable<>).MakeGenericType(new Type[] { jsonArrayContract.CollectionItemType }) : typeof(IEnumerable));
				if (parameters.Length == 0)
				{
					jsonArrayContract.HasParameterizedCreator = false;
				}
				else
				{
					if (parameters.Length != 1 || !type.IsAssignableFrom(parameters[0].ParameterType))
					{
						throw new JsonException("Constructor for '{0}' must have no parameters or a single parameter that implements '{1}'.".FormatWith(CultureInfo.InvariantCulture, jsonArrayContract.UnderlyingType, type));
					}
					jsonArrayContract.HasParameterizedCreator = true;
				}
				jsonArrayContract.OverrideCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(attributeConstructor);
			}
			return jsonArrayContract;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonPrimitiveContract" /> for the given type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Serialization.JsonPrimitiveContract" /> for the given type.</returns>
		// Token: 0x06000645 RID: 1605 RVA: 0x0001AEE0 File Offset: 0x000190E0
		protected virtual JsonPrimitiveContract CreatePrimitiveContract(Type objectType)
		{
			JsonPrimitiveContract jsonPrimitiveContract = new JsonPrimitiveContract(objectType);
			this.InitializeContract(jsonPrimitiveContract);
			return jsonPrimitiveContract;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonLinqContract" /> for the given type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Serialization.JsonLinqContract" /> for the given type.</returns>
		// Token: 0x06000646 RID: 1606 RVA: 0x0001AEFC File Offset: 0x000190FC
		protected virtual JsonLinqContract CreateLinqContract(Type objectType)
		{
			JsonLinqContract jsonLinqContract = new JsonLinqContract(objectType);
			this.InitializeContract(jsonLinqContract);
			return jsonLinqContract;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonISerializableContract" /> for the given type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Serialization.JsonISerializableContract" /> for the given type.</returns>
		// Token: 0x06000647 RID: 1607 RVA: 0x0001AF18 File Offset: 0x00019118
		protected virtual JsonISerializableContract CreateISerializableContract(Type objectType)
		{
			JsonISerializableContract jsonISerializableContract = new JsonISerializableContract(objectType);
			this.InitializeContract(jsonISerializableContract);
			if (jsonISerializableContract.IsInstantiable)
			{
				ConstructorInfo constructor = jsonISerializableContract.NonNullableUnderlyingType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[]
				{
					typeof(SerializationInfo),
					typeof(StreamingContext)
				}, null);
				if (constructor != null)
				{
					ObjectConstructor<object> iserializableCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(constructor);
					jsonISerializableContract.ISerializableCreator = iserializableCreator;
				}
			}
			return jsonISerializableContract;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonDynamicContract" /> for the given type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Serialization.JsonDynamicContract" /> for the given type.</returns>
		// Token: 0x06000648 RID: 1608 RVA: 0x0001AF88 File Offset: 0x00019188
		protected virtual JsonDynamicContract CreateDynamicContract(Type objectType)
		{
			JsonDynamicContract jsonDynamicContract = new JsonDynamicContract(objectType);
			this.InitializeContract(jsonDynamicContract);
			JsonContainerAttribute attribute = JsonTypeReflector.GetAttribute<JsonContainerAttribute>(objectType);
			if (((attribute != null) ? attribute.NamingStrategyType : null) != null)
			{
				NamingStrategy namingStrategy = JsonTypeReflector.GetContainerNamingStrategy(attribute);
				jsonDynamicContract.PropertyNameResolver = (string s) => namingStrategy.GetDictionaryKey(s);
			}
			else
			{
				jsonDynamicContract.PropertyNameResolver = new Func<string, string>(this.ResolveDictionaryKey);
			}
			jsonDynamicContract.Properties.AddRange(this.CreateProperties(objectType, MemberSerialization.OptOut));
			return jsonDynamicContract;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonStringContract" /> for the given type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Serialization.JsonStringContract" /> for the given type.</returns>
		// Token: 0x06000649 RID: 1609 RVA: 0x0001B00C File Offset: 0x0001920C
		protected virtual JsonStringContract CreateStringContract(Type objectType)
		{
			JsonStringContract jsonStringContract = new JsonStringContract(objectType);
			this.InitializeContract(jsonStringContract);
			return jsonStringContract;
		}

		/// <summary>
		/// Determines which contract type is created for the given type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>A <see cref="T:Newtonsoft.Json.Serialization.JsonContract" /> for the given type.</returns>
		// Token: 0x0600064A RID: 1610 RVA: 0x0001B028 File Offset: 0x00019228
		protected virtual JsonContract CreateContract(Type objectType)
		{
			Type type = ReflectionUtils.EnsureNotByRefType(objectType);
			if (DefaultContractResolver.IsJsonPrimitiveType(type))
			{
				return this.CreatePrimitiveContract(objectType);
			}
			type = ReflectionUtils.EnsureNotNullableType(type);
			JsonContainerAttribute cachedAttribute = JsonTypeReflector.GetCachedAttribute<JsonContainerAttribute>(type);
			if (cachedAttribute is JsonObjectAttribute)
			{
				return this.CreateObjectContract(objectType);
			}
			if (cachedAttribute is JsonArrayAttribute)
			{
				return this.CreateArrayContract(objectType);
			}
			if (cachedAttribute is JsonDictionaryAttribute)
			{
				return this.CreateDictionaryContract(objectType);
			}
			if (type == typeof(JToken) || type.IsSubclassOf(typeof(JToken)))
			{
				return this.CreateLinqContract(objectType);
			}
			if (CollectionUtils.IsDictionaryType(type))
			{
				return this.CreateDictionaryContract(objectType);
			}
			if (typeof(IEnumerable).IsAssignableFrom(type))
			{
				return this.CreateArrayContract(objectType);
			}
			if (DefaultContractResolver.CanConvertToString(type))
			{
				return this.CreateStringContract(objectType);
			}
			if (!this.IgnoreSerializableInterface && typeof(ISerializable).IsAssignableFrom(type) && JsonTypeReflector.IsSerializable(type))
			{
				return this.CreateISerializableContract(objectType);
			}
			if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(type))
			{
				return this.CreateDynamicContract(objectType);
			}
			if (DefaultContractResolver.IsIConvertible(type))
			{
				return this.CreatePrimitiveContract(type);
			}
			return this.CreateObjectContract(objectType);
		}

		// Token: 0x0600064B RID: 1611 RVA: 0x0001B14C File Offset: 0x0001934C
		internal static bool IsJsonPrimitiveType(Type t)
		{
			PrimitiveTypeCode typeCode = ConvertUtils.GetTypeCode(t);
			return typeCode != PrimitiveTypeCode.Empty && typeCode != PrimitiveTypeCode.Object;
		}

		// Token: 0x0600064C RID: 1612 RVA: 0x0001B16C File Offset: 0x0001936C
		internal static bool IsIConvertible(Type t)
		{
			return (typeof(IConvertible).IsAssignableFrom(t) || (ReflectionUtils.IsNullableType(t) && typeof(IConvertible).IsAssignableFrom(Nullable.GetUnderlyingType(t)))) && !typeof(JToken).IsAssignableFrom(t);
		}

		// Token: 0x0600064D RID: 1613 RVA: 0x0001B1C0 File Offset: 0x000193C0
		internal static bool CanConvertToString(Type type)
		{
			TypeConverter typeConverter;
			return JsonTypeReflector.CanTypeDescriptorConvertString(type, out typeConverter) || (type == typeof(Type) || type.IsSubclassOf(typeof(Type)));
		}

		// Token: 0x0600064E RID: 1614 RVA: 0x0001B200 File Offset: 0x00019400
		private static bool IsValidCallback(MethodInfo method, ParameterInfo[] parameters, Type attributeType, [Nullable(2)] MethodInfo currentCallback, [Nullable(2)] ref Type prevAttributeType)
		{
			if (!method.IsDefined(attributeType, false))
			{
				return false;
			}
			if (currentCallback != null)
			{
				throw new JsonException("Invalid attribute. Both '{0}' and '{1}' in type '{2}' have '{3}'.".FormatWith(CultureInfo.InvariantCulture, method, currentCallback, DefaultContractResolver.GetClrTypeFullName(method.DeclaringType), attributeType));
			}
			if (prevAttributeType != null)
			{
				throw new JsonException("Invalid Callback. Method '{3}' in type '{2}' has both '{0}' and '{1}'.".FormatWith(CultureInfo.InvariantCulture, prevAttributeType, attributeType, DefaultContractResolver.GetClrTypeFullName(method.DeclaringType), method));
			}
			if (method.IsVirtual)
			{
				throw new JsonException("Virtual Method '{0}' of type '{1}' cannot be marked with '{2}' attribute.".FormatWith(CultureInfo.InvariantCulture, method, DefaultContractResolver.GetClrTypeFullName(method.DeclaringType), attributeType));
			}
			if (method.ReturnType != typeof(void))
			{
				throw new JsonException("Serialization Callback '{1}' in type '{0}' must return void.".FormatWith(CultureInfo.InvariantCulture, DefaultContractResolver.GetClrTypeFullName(method.DeclaringType), method));
			}
			if (attributeType == typeof(OnErrorAttribute))
			{
				if (parameters == null || parameters.Length != 2 || parameters[0].ParameterType != typeof(StreamingContext) || parameters[1].ParameterType != typeof(ErrorContext))
				{
					throw new JsonException("Serialization Error Callback '{1}' in type '{0}' must have two parameters of type '{2}' and '{3}'.".FormatWith(CultureInfo.InvariantCulture, DefaultContractResolver.GetClrTypeFullName(method.DeclaringType), method, typeof(StreamingContext), typeof(ErrorContext)));
				}
			}
			else if (parameters == null || parameters.Length != 1 || parameters[0].ParameterType != typeof(StreamingContext))
			{
				throw new JsonException("Serialization Callback '{1}' in type '{0}' must have a single parameter of type '{2}'.".FormatWith(CultureInfo.InvariantCulture, DefaultContractResolver.GetClrTypeFullName(method.DeclaringType), method, typeof(StreamingContext)));
			}
			prevAttributeType = attributeType;
			return true;
		}

		// Token: 0x0600064F RID: 1615 RVA: 0x0001B3AE File Offset: 0x000195AE
		internal static string GetClrTypeFullName(Type type)
		{
			if (type.IsGenericTypeDefinition() || !type.ContainsGenericParameters())
			{
				return type.FullName;
			}
			return "{0}.{1}".FormatWith(CultureInfo.InvariantCulture, type.Namespace, type.Name);
		}

		/// <summary>
		/// Creates properties for the given <see cref="T:Newtonsoft.Json.Serialization.JsonContract" />.
		/// </summary>
		/// <param name="type">The type to create properties for.</param>
		/// /// <param name="memberSerialization">The member serialization mode for the type.</param>
		/// <returns>Properties for the given <see cref="T:Newtonsoft.Json.Serialization.JsonContract" />.</returns>
		// Token: 0x06000650 RID: 1616 RVA: 0x0001B3E4 File Offset: 0x000195E4
		protected virtual IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			List<MemberInfo> serializableMembers = this.GetSerializableMembers(type);
			if (serializableMembers == null)
			{
				throw new JsonSerializationException("Null collection of serializable members returned.");
			}
			DefaultJsonNameTable nameTable = this.GetNameTable();
			JsonPropertyCollection jsonPropertyCollection = new JsonPropertyCollection(type);
			foreach (MemberInfo member in serializableMembers)
			{
				JsonProperty jsonProperty = this.CreateProperty(member, memberSerialization);
				if (jsonProperty != null)
				{
					DefaultJsonNameTable obj = nameTable;
					lock (obj)
					{
						jsonProperty.PropertyName = nameTable.Add(jsonProperty.PropertyName);
					}
					jsonPropertyCollection.AddProperty(jsonProperty);
				}
			}
			return jsonPropertyCollection.OrderBy(delegate(JsonProperty p)
			{
				int? order = p.Order;
				if (order == null)
				{
					return -1;
				}
				return order.GetValueOrDefault();
			}).ToList<JsonProperty>();
		}

		// Token: 0x06000651 RID: 1617 RVA: 0x0001B4CC File Offset: 0x000196CC
		internal virtual DefaultJsonNameTable GetNameTable()
		{
			return this._nameTable;
		}

		/// <summary>
		/// Creates the <see cref="T:Newtonsoft.Json.Serialization.IValueProvider" /> used by the serializer to get and set values from a member.
		/// </summary>
		/// <param name="member">The member.</param>
		/// <returns>The <see cref="T:Newtonsoft.Json.Serialization.IValueProvider" /> used by the serializer to get and set values from a member.</returns>
		// Token: 0x06000652 RID: 1618 RVA: 0x0001B4D4 File Offset: 0x000196D4
		protected virtual IValueProvider CreateMemberValueProvider(MemberInfo member)
		{
			IValueProvider result;
			if (this.DynamicCodeGeneration)
			{
				result = new DynamicValueProvider(member);
			}
			else
			{
				result = new ReflectionValueProvider(member);
			}
			return result;
		}

		/// <summary>
		/// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> for the given <see cref="T:System.Reflection.MemberInfo" />.
		/// </summary>
		/// <param name="memberSerialization">The member's parent <see cref="T:Newtonsoft.Json.MemberSerialization" />.</param>
		/// <param name="member">The member to create a <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> for.</param>
		/// <returns>A created <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> for the given <see cref="T:System.Reflection.MemberInfo" />.</returns>
		// Token: 0x06000653 RID: 1619 RVA: 0x0001B4FC File Offset: 0x000196FC
		protected virtual JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			JsonProperty jsonProperty = new JsonProperty();
			jsonProperty.PropertyType = ReflectionUtils.GetMemberUnderlyingType(member);
			jsonProperty.DeclaringType = member.DeclaringType;
			jsonProperty.ValueProvider = this.CreateMemberValueProvider(member);
			jsonProperty.AttributeProvider = new ReflectionAttributeProvider(member);
			bool flag;
			this.SetPropertySettingsFromAttributes(jsonProperty, member, member.Name, member.DeclaringType, memberSerialization, out flag);
			if (memberSerialization != MemberSerialization.Fields)
			{
				jsonProperty.Readable = ReflectionUtils.CanReadMemberValue(member, flag);
				jsonProperty.Writable = ReflectionUtils.CanSetMemberValue(member, flag, jsonProperty.HasMemberAttribute);
			}
			else
			{
				jsonProperty.Readable = true;
				jsonProperty.Writable = true;
			}
			if (!this.IgnoreShouldSerializeMembers)
			{
				jsonProperty.ShouldSerialize = this.CreateShouldSerializeTest(member);
			}
			if (!this.IgnoreIsSpecifiedMembers)
			{
				this.SetIsSpecifiedActions(jsonProperty, member, flag);
			}
			return jsonProperty;
		}

		// Token: 0x06000654 RID: 1620 RVA: 0x0001B5B4 File Offset: 0x000197B4
		private void SetPropertySettingsFromAttributes(JsonProperty property, object attributeProvider, string name, Type declaringType, MemberSerialization memberSerialization, out bool allowNonPublicAccess)
		{
			bool dataContractAttribute = JsonTypeReflector.GetDataContractAttribute(declaringType) != null;
			MemberInfo memberInfo = attributeProvider as MemberInfo;
			DataMemberAttribute dataMemberAttribute;
			if (dataContractAttribute && memberInfo != null)
			{
				dataMemberAttribute = JsonTypeReflector.GetDataMemberAttribute(memberInfo);
			}
			else
			{
				dataMemberAttribute = null;
			}
			JsonPropertyAttribute attribute = JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(attributeProvider);
			bool attribute2 = JsonTypeReflector.GetAttribute<JsonRequiredAttribute>(attributeProvider) != null;
			string text;
			bool hasSpecifiedName;
			if (attribute != null && attribute.PropertyName != null)
			{
				text = attribute.PropertyName;
				hasSpecifiedName = true;
			}
			else if (dataMemberAttribute != null && dataMemberAttribute.Name != null)
			{
				text = dataMemberAttribute.Name;
				hasSpecifiedName = true;
			}
			else
			{
				text = name;
				hasSpecifiedName = false;
			}
			JsonContainerAttribute attribute3 = JsonTypeReflector.GetAttribute<JsonContainerAttribute>(declaringType);
			NamingStrategy namingStrategy;
			if (((attribute != null) ? attribute.NamingStrategyType : null) != null)
			{
				namingStrategy = JsonTypeReflector.CreateNamingStrategyInstance(attribute.NamingStrategyType, attribute.NamingStrategyParameters);
			}
			else if (((attribute3 != null) ? attribute3.NamingStrategyType : null) != null)
			{
				namingStrategy = JsonTypeReflector.GetContainerNamingStrategy(attribute3);
			}
			else
			{
				namingStrategy = this.NamingStrategy;
			}
			if (namingStrategy != null)
			{
				property.PropertyName = namingStrategy.GetPropertyName(text, hasSpecifiedName);
			}
			else
			{
				property.PropertyName = this.ResolvePropertyName(text);
			}
			property.UnderlyingName = name;
			bool flag = false;
			if (attribute != null)
			{
				property._required = attribute._required;
				property.Order = attribute._order;
				property.DefaultValueHandling = attribute._defaultValueHandling;
				flag = true;
				property.NullValueHandling = attribute._nullValueHandling;
				property.ReferenceLoopHandling = attribute._referenceLoopHandling;
				property.ObjectCreationHandling = attribute._objectCreationHandling;
				property.TypeNameHandling = attribute._typeNameHandling;
				property.IsReference = attribute._isReference;
				property.ItemIsReference = attribute._itemIsReference;
				property.ItemConverter = ((attribute.ItemConverterType != null) ? JsonTypeReflector.CreateJsonConverterInstance(attribute.ItemConverterType, attribute.ItemConverterParameters) : null);
				property.ItemReferenceLoopHandling = attribute._itemReferenceLoopHandling;
				property.ItemTypeNameHandling = attribute._itemTypeNameHandling;
			}
			else
			{
				property.NullValueHandling = null;
				property.ReferenceLoopHandling = null;
				property.ObjectCreationHandling = null;
				property.TypeNameHandling = null;
				property.IsReference = null;
				property.ItemIsReference = null;
				property.ItemConverter = null;
				property.ItemReferenceLoopHandling = null;
				property.ItemTypeNameHandling = null;
				if (dataMemberAttribute != null)
				{
					property._required = new Required?(dataMemberAttribute.IsRequired ? Required.AllowNull : Required.Default);
					property.Order = ((dataMemberAttribute.Order != -1) ? new int?(dataMemberAttribute.Order) : null);
					property.DefaultValueHandling = ((!dataMemberAttribute.EmitDefaultValue) ? new DefaultValueHandling?(DefaultValueHandling.Ignore) : null);
					flag = true;
				}
			}
			if (attribute2)
			{
				property._required = new Required?(Required.Always);
				flag = true;
			}
			property.HasMemberAttribute = flag;
			bool flag2 = JsonTypeReflector.GetAttribute<JsonIgnoreAttribute>(attributeProvider) != null || JsonTypeReflector.GetAttribute<JsonExtensionDataAttribute>(attributeProvider) != null || JsonTypeReflector.IsNonSerializable(attributeProvider);
			if (memberSerialization != MemberSerialization.OptIn)
			{
				bool flag3 = JsonTypeReflector.GetAttribute<IgnoreDataMemberAttribute>(attributeProvider) != null;
				property.Ignored = flag2 || flag3;
			}
			else
			{
				property.Ignored = flag2 || !flag;
			}
			property.Converter = JsonTypeReflector.GetJsonConverter(attributeProvider);
			DefaultValueAttribute attribute4 = JsonTypeReflector.GetAttribute<DefaultValueAttribute>(attributeProvider);
			if (attribute4 != null)
			{
				property.DefaultValue = attribute4.Value;
			}
			allowNonPublicAccess = false;
			if ((this.DefaultMembersSearchFlags & BindingFlags.NonPublic) == BindingFlags.NonPublic)
			{
				allowNonPublicAccess = true;
			}
			if (flag)
			{
				allowNonPublicAccess = true;
			}
			if (memberSerialization == MemberSerialization.Fields)
			{
				allowNonPublicAccess = true;
			}
		}

		// Token: 0x06000655 RID: 1621 RVA: 0x0001B904 File Offset: 0x00019B04
		[return: Nullable(new byte[] { 2, 1 })]
		private Predicate<object> CreateShouldSerializeTest(MemberInfo member)
		{
			MethodInfo method = member.DeclaringType.GetMethod("ShouldSerialize" + member.Name, ReflectionUtils.EmptyTypes);
			if (method == null || method.ReturnType != typeof(bool))
			{
				return null;
			}
			MethodCall<object, object> shouldSerializeCall = JsonTypeReflector.ReflectionDelegateFactory.CreateMethodCall<object>(method);
			return (object o) => (bool)shouldSerializeCall(o, new object[0]);
		}

		// Token: 0x06000656 RID: 1622 RVA: 0x0001B978 File Offset: 0x00019B78
		private void SetIsSpecifiedActions(JsonProperty property, MemberInfo member, bool allowNonPublicAccess)
		{
			MemberInfo memberInfo = member.DeclaringType.GetProperty(member.Name + "Specified", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (memberInfo == null)
			{
				memberInfo = member.DeclaringType.GetField(member.Name + "Specified", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (memberInfo == null || ReflectionUtils.GetMemberUnderlyingType(memberInfo) != typeof(bool))
			{
				return;
			}
			Func<object, object> specifiedPropertyGet = JsonTypeReflector.ReflectionDelegateFactory.CreateGet<object>(memberInfo);
			property.GetIsSpecified = (object o) => (bool)specifiedPropertyGet(o);
			if (ReflectionUtils.CanSetMemberValue(memberInfo, allowNonPublicAccess, false))
			{
				property.SetIsSpecified = JsonTypeReflector.ReflectionDelegateFactory.CreateSet<object>(memberInfo);
			}
		}

		/// <summary>
		/// Resolves the name of the property.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns>Resolved name of the property.</returns>
		// Token: 0x06000657 RID: 1623 RVA: 0x0001BA2F File Offset: 0x00019C2F
		protected virtual string ResolvePropertyName(string propertyName)
		{
			if (this.NamingStrategy != null)
			{
				return this.NamingStrategy.GetPropertyName(propertyName, false);
			}
			return propertyName;
		}

		/// <summary>
		/// Resolves the name of the extension data. By default no changes are made to extension data names.
		/// </summary>
		/// <param name="extensionDataName">Name of the extension data.</param>
		/// <returns>Resolved name of the extension data.</returns>
		// Token: 0x06000658 RID: 1624 RVA: 0x0001BA48 File Offset: 0x00019C48
		protected virtual string ResolveExtensionDataName(string extensionDataName)
		{
			if (this.NamingStrategy != null)
			{
				return this.NamingStrategy.GetExtensionDataName(extensionDataName);
			}
			return extensionDataName;
		}

		/// <summary>
		/// Resolves the key of the dictionary. By default <see cref="M:Newtonsoft.Json.Serialization.DefaultContractResolver.ResolvePropertyName(System.String)" /> is used to resolve dictionary keys.
		/// </summary>
		/// <param name="dictionaryKey">Key of the dictionary.</param>
		/// <returns>Resolved key of the dictionary.</returns>
		// Token: 0x06000659 RID: 1625 RVA: 0x0001BA60 File Offset: 0x00019C60
		protected virtual string ResolveDictionaryKey(string dictionaryKey)
		{
			if (this.NamingStrategy != null)
			{
				return this.NamingStrategy.GetDictionaryKey(dictionaryKey);
			}
			return this.ResolvePropertyName(dictionaryKey);
		}

		/// <summary>
		/// Gets the resolved name of the property.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns>Name of the property.</returns>
		// Token: 0x0600065A RID: 1626 RVA: 0x0001BA7E File Offset: 0x00019C7E
		public string GetResolvedPropertyName(string propertyName)
		{
			return this.ResolvePropertyName(propertyName);
		}

		// Token: 0x0400022E RID: 558
		private static readonly IContractResolver _instance = new DefaultContractResolver();

		// Token: 0x0400022F RID: 559
		private static readonly string[] BlacklistedTypeNames = new string[] { "System.IO.DriveInfo", "System.IO.FileInfo", "System.IO.DirectoryInfo" };

		// Token: 0x04000230 RID: 560
		private static readonly JsonConverter[] BuiltInConverters = new JsonConverter[]
		{
			new EntityKeyMemberConverter(),
			new ExpandoObjectConverter(),
			new XmlNodeConverter(),
			new BinaryConverter(),
			new DataSetConverter(),
			new DataTableConverter(),
			new DiscriminatedUnionConverter(),
			new KeyValuePairConverter(),
			new BsonObjectIdConverter(),
			new RegexConverter()
		};

		// Token: 0x04000231 RID: 561
		private readonly DefaultJsonNameTable _nameTable = new DefaultJsonNameTable();

		// Token: 0x04000232 RID: 562
		private readonly ThreadSafeStore<Type, JsonContract> _contractCache;

		// Token: 0x0200019C RID: 412
		[NullableContext(0)]
		internal class EnumerableDictionaryWrapper<[Nullable(2)] TEnumeratorKey, [Nullable(2)] TEnumeratorValue> : IEnumerable<KeyValuePair<object, object>>, IEnumerable
		{
			// Token: 0x06000F28 RID: 3880 RVA: 0x0004322F File Offset: 0x0004142F
			public EnumerableDictionaryWrapper([Nullable(new byte[] { 1, 0, 1, 1 })] IEnumerable<KeyValuePair<TEnumeratorKey, TEnumeratorValue>> e)
			{
				ValidationUtils.ArgumentNotNull(e, "e");
				this._e = e;
			}

			// Token: 0x06000F29 RID: 3881 RVA: 0x00043249 File Offset: 0x00041449
			[return: Nullable(new byte[] { 1, 0, 1, 1 })]
			public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
			{
				foreach (KeyValuePair<TEnumeratorKey, TEnumeratorValue> keyValuePair in this._e)
				{
					yield return new KeyValuePair<object, object>(keyValuePair.Key, keyValuePair.Value);
				}
				IEnumerator<KeyValuePair<TEnumeratorKey, TEnumeratorValue>> enumerator = null;
				yield break;
				yield break;
			}

			// Token: 0x06000F2A RID: 3882 RVA: 0x00043258 File Offset: 0x00041458
			[NullableContext(1)]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			// Token: 0x04000725 RID: 1829
			[Nullable(new byte[] { 1, 0, 1, 1 })]
			private readonly IEnumerable<KeyValuePair<TEnumeratorKey, TEnumeratorValue>> _e;
		}
	}
}
