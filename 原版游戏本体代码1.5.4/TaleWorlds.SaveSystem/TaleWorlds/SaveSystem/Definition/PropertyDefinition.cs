using System;
using System.Reflection;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Definition;

public class PropertyDefinition : MemberDefinition
{
	public PropertyInfo PropertyInfo { get; private set; }

	public SaveablePropertyAttribute SaveablePropertyAttribute { get; private set; }

	public MethodInfo GetMethod { get; private set; }

	public MethodInfo SetMethod { get; private set; }

	public GetPropertyValueDelegate GetPropertyValueMethod { get; private set; }

	public PropertyDefinition(PropertyInfo propertyInfo, MemberTypeId id)
		: base(propertyInfo, id)
	{
		PropertyInfo = propertyInfo;
		SaveablePropertyAttribute = propertyInfo.GetCustomAttribute<SaveablePropertyAttribute>();
		SetMethod = PropertyInfo.GetSetMethod(nonPublic: true);
		if (SetMethod == null && PropertyInfo.DeclaringType != null)
		{
			PropertyInfo property = PropertyInfo.DeclaringType.GetProperty(PropertyInfo.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				SetMethod = property.GetSetMethod(nonPublic: true);
			}
		}
		if (SetMethod == null)
		{
			Debug.FailedAssert("Property " + PropertyInfo.Name + " at Type " + PropertyInfo.DeclaringType.FullName + " does not have setter method.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\Definition\\PropertyDefinition.cs", ".ctor", 39);
			throw new Exception("Property " + PropertyInfo.Name + " at Type " + PropertyInfo.DeclaringType.FullName + " does not have setter method.");
		}
		GetMethod = PropertyInfo.GetGetMethod(nonPublic: true);
		if (GetMethod == null && PropertyInfo.DeclaringType != null)
		{
			PropertyInfo property2 = PropertyInfo.DeclaringType.GetProperty(PropertyInfo.Name);
			if (property2 != null)
			{
				GetMethod = property2.GetGetMethod(nonPublic: true);
			}
		}
		if (GetMethod == null)
		{
			throw new Exception("Property " + PropertyInfo.Name + " at Type " + PropertyInfo.DeclaringType.FullName + " does not have getter method.");
		}
	}

	public override Type GetMemberType()
	{
		return PropertyInfo.PropertyType;
	}

	public override object GetValue(object target)
	{
		if (GetPropertyValueMethod != null)
		{
			return GetPropertyValueMethod(target);
		}
		return GetMethod.Invoke(target, new object[0]);
	}

	public void InitializeForAutoGeneration(GetPropertyValueDelegate getPropertyValueMethod)
	{
		GetPropertyValueMethod = getPropertyValueMethod;
	}
}
