using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Load;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.SaveSystem.Definition;

public class TypeDefinition : TypeDefinitionBase
{
	private Dictionary<MemberTypeId, PropertyDefinition> _properties;

	private Dictionary<MemberTypeId, FieldDefinition> _fields;

	private List<string> _errors;

	private List<MethodInfo> _initializationCallbacks;

	private List<MethodInfo> _lateInitializationCallbacks;

	private bool _isClass;

	private readonly IObjectResolver _objectResolver;

	public List<MemberDefinition> MemberDefinitions { get; private set; }

	public IEnumerable<MethodInfo> InitializationCallbacks => _initializationCallbacks;

	public IEnumerable<MethodInfo> LateInitializationCallbacks => _lateInitializationCallbacks;

	public IEnumerable<string> Errors => _errors.AsReadOnly();

	public bool IsClassDefinition => _isClass;

	public List<CustomField> CustomFields { get; private set; }

	public CollectObjectsDelegate CollectObjectsMethod { get; private set; }

	public Dictionary<MemberTypeId, PropertyDefinition>.ValueCollection PropertyDefinitions => _properties.Values;

	public Dictionary<MemberTypeId, FieldDefinition>.ValueCollection FieldDefinitions => _fields.Values;

	public TypeDefinition(Type type, SaveId saveId, IObjectResolver objectResolver)
		: base(type, saveId)
	{
		_isClass = base.Type.IsClass;
		_errors = new List<string>();
		_properties = new Dictionary<MemberTypeId, PropertyDefinition>();
		_fields = new Dictionary<MemberTypeId, FieldDefinition>();
		MemberDefinitions = new List<MemberDefinition>();
		CustomFields = new List<CustomField>();
		_initializationCallbacks = new List<MethodInfo>();
		_lateInitializationCallbacks = new List<MethodInfo>();
		_objectResolver = objectResolver;
	}

	public TypeDefinition(Type type, int saveId, IObjectResolver objectResolver)
		: this(type, new TypeSaveId(saveId), objectResolver)
	{
	}

	public bool CheckIfRequiresAdvancedResolving(object originalObject)
	{
		if (_objectResolver != null)
		{
			return _objectResolver.CheckIfRequiresAdvancedResolving(originalObject);
		}
		return false;
	}

	public object ResolveObject(object originalObject)
	{
		if (_objectResolver != null)
		{
			return _objectResolver.ResolveObject(originalObject);
		}
		return originalObject;
	}

	public object AdvancedResolveObject(object originalObject, MetaData metaData, ObjectLoadData objectLoadData)
	{
		if (_objectResolver != null)
		{
			return _objectResolver.AdvancedResolveObject(originalObject, metaData, objectLoadData);
		}
		return originalObject;
	}

	public void CollectInitializationCallbacks()
	{
		Type type = base.Type;
		while (type != typeof(object))
		{
			MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.DeclaringType == type)
				{
					if (methodInfo.GetCustomAttributesSafe(typeof(LoadInitializationCallback)).ToArray().Length != 0 && !_initializationCallbacks.Contains(methodInfo))
					{
						_initializationCallbacks.Insert(0, methodInfo);
					}
					if (methodInfo.GetCustomAttributesSafe(typeof(LateLoadInitializationCallback)).ToArray().Length != 0 && !_lateInitializationCallbacks.Contains(methodInfo))
					{
						_lateInitializationCallbacks.Insert(0, methodInfo);
					}
				}
			}
			type = type.BaseType;
		}
	}

	public void CollectProperties()
	{
		PropertyInfo[] properties = base.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			Attribute[] array = propertyInfo.GetCustomAttributesSafe(typeof(SaveablePropertyAttribute)).ToArray();
			if (array.Length != 0)
			{
				SaveablePropertyAttribute saveablePropertyAttribute = (SaveablePropertyAttribute)array[0];
				byte classLevel = TypeDefinitionBase.GetClassLevel(propertyInfo.DeclaringType);
				MemberTypeId memberTypeId = new MemberTypeId(classLevel, saveablePropertyAttribute.LocalSaveId);
				PropertyDefinition propertyDefinition = new PropertyDefinition(propertyInfo, memberTypeId);
				if (_properties.ContainsKey(memberTypeId))
				{
					_errors.Add(string.Concat("SaveId ", memberTypeId, " of property ", propertyDefinition.PropertyInfo.Name, " is already defined in type ", base.Type.FullName));
				}
				else
				{
					_properties.Add(memberTypeId, propertyDefinition);
					MemberDefinitions.Add(propertyDefinition);
				}
			}
		}
	}

	private static IEnumerable<FieldInfo> GetFieldsOfType(Type type)
	{
		FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		FieldInfo[] array = fields;
		foreach (FieldInfo fieldInfo in array)
		{
			if (!fieldInfo.IsPrivate)
			{
				yield return fieldInfo;
			}
		}
		Type typeToCheck = type;
		while (typeToCheck != typeof(object))
		{
			FieldInfo[] fields2 = typeToCheck.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
			array = fields2;
			foreach (FieldInfo fieldInfo2 in array)
			{
				if (fieldInfo2.IsPrivate)
				{
					yield return fieldInfo2;
				}
			}
			typeToCheck = typeToCheck.BaseType;
		}
	}

	public void CollectFields()
	{
		FieldInfo[] array = GetFieldsOfType(base.Type).ToArray();
		foreach (FieldInfo fieldInfo in array)
		{
			Attribute[] array2 = fieldInfo.GetCustomAttributesSafe(typeof(SaveableFieldAttribute)).ToArray();
			if (array2.Length != 0)
			{
				SaveableFieldAttribute saveableFieldAttribute = (SaveableFieldAttribute)array2[0];
				byte classLevel = TypeDefinitionBase.GetClassLevel(fieldInfo.DeclaringType);
				MemberTypeId memberTypeId = new MemberTypeId(classLevel, saveableFieldAttribute.LocalSaveId);
				FieldDefinition fieldDefinition = new FieldDefinition(fieldInfo, memberTypeId);
				if (_fields.ContainsKey(memberTypeId))
				{
					_errors.Add(string.Concat("SaveId ", memberTypeId, " of field ", fieldDefinition.FieldInfo, " is already defined in type ", base.Type.FullName));
				}
				else
				{
					_fields.Add(memberTypeId, fieldDefinition);
					MemberDefinitions.Add(fieldDefinition);
				}
			}
		}
		foreach (CustomField customField in CustomFields)
		{
			string name = customField.Name;
			short saveId = customField.SaveId;
			FieldInfo? field = base.Type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			byte classLevel2 = TypeDefinitionBase.GetClassLevel(field.DeclaringType);
			MemberTypeId memberTypeId2 = new MemberTypeId(classLevel2, saveId);
			FieldDefinition fieldDefinition2 = new FieldDefinition(field, memberTypeId2);
			if (_fields.ContainsKey(memberTypeId2))
			{
				_errors.Add(string.Concat("SaveId ", memberTypeId2, " of field ", fieldDefinition2.FieldInfo, " is already defined in type ", base.Type.FullName));
			}
			else
			{
				_fields.Add(memberTypeId2, fieldDefinition2);
				MemberDefinitions.Add(fieldDefinition2);
			}
		}
	}

	public void AddCustomField(string fieldName, short saveId)
	{
		CustomFields.Add(new CustomField(fieldName, saveId));
	}

	public PropertyDefinition GetPropertyDefinitionWithId(MemberTypeId id)
	{
		_properties.TryGetValue(id, out var value);
		return value;
	}

	public FieldDefinition GetFieldDefinitionWithId(MemberTypeId id)
	{
		_fields.TryGetValue(id, out var value);
		return value;
	}

	public void InitializeForAutoGeneration(CollectObjectsDelegate collectObjectsDelegate)
	{
		CollectObjectsMethod = collectObjectsDelegate;
	}
}
