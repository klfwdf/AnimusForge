using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

internal static class TypeExtensions
{
	internal static bool IsContainer(this Type type)
	{
		ContainerType containerType;
		return type.IsContainer(out containerType);
	}

	internal static bool IsContainer(this Type type, out ContainerType containerType)
	{
		containerType = ContainerType.None;
		if (type.IsGenericType && !type.IsGenericTypeDefinition)
		{
			Type genericTypeDefinition = type.GetGenericTypeDefinition();
			if (genericTypeDefinition == typeof(Dictionary<, >))
			{
				containerType = ContainerType.Dictionary;
				return true;
			}
			if (genericTypeDefinition == typeof(List<>))
			{
				containerType = ContainerType.List;
				return true;
			}
			if (genericTypeDefinition == typeof(MBList<>))
			{
				containerType = ContainerType.CustomList;
				return true;
			}
			if (genericTypeDefinition == typeof(MBReadOnlyList<>))
			{
				containerType = ContainerType.CustomReadOnlyList;
				return true;
			}
			if (genericTypeDefinition == typeof(Queue<>))
			{
				containerType = ContainerType.Queue;
				return true;
			}
		}
		else if (type.IsArray)
		{
			containerType = ContainerType.Array;
			return true;
		}
		return false;
	}
}
