using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using MonoMod.Core.Platforms;
using MonoMod.Utils;

namespace HarmonyLib
{
	/// <summary>A helper class for reflection related functions</summary>
	// Token: 0x020000A7 RID: 167
	public static class AccessTools
	{
		/// <summary>Enumerates all assemblies in the current app domain, excluding visual studio assemblies</summary>
		/// <returns>An enumeration of <see cref="T:System.Reflection.Assembly" /></returns>
		// Token: 0x06000351 RID: 849 RVA: 0x00011CF1 File Offset: 0x0000FEF1
		public static IEnumerable<Assembly> AllAssemblies()
		{
			return from a in AppDomain.CurrentDomain.GetAssemblies()
				where !a.FullName.StartsWith("Microsoft.VisualStudio")
				select a;
		}

		/// <summary>Gets a type by name. Prefers a full name with namespace but falls back to the first type matching the name otherwise</summary>
		/// <param name="name">The name</param>
		/// <returns>A type or null if not found</returns>
		// Token: 0x06000352 RID: 850 RVA: 0x00011D24 File Offset: 0x0000FF24
		public static Type TypeByName(string name)
		{
			Type localType = Type.GetType(name, false);
			if (localType != null)
			{
				return localType;
			}
			foreach (Assembly assembly in AccessTools.AllAssemblies())
			{
				Type specificType = assembly.GetType(name, false);
				if (specificType != null)
				{
					return specificType;
				}
			}
			Type[] allTypes = AccessTools.AllTypes().ToArray<Type>();
			Type fullType = allTypes.FirstOrDefault((Type t) => t.FullName == name);
			if (fullType != null)
			{
				return fullType;
			}
			Type partialType = allTypes.FirstOrDefault((Type t) => t.Name == name);
			if (partialType != null)
			{
				return partialType;
			}
			FileLog.Debug("AccessTools.TypeByName: Could not find type named " + name);
			return null;
		}

		/// <summary>Searches a type by regular expression; for exact searching, use <see cref="M:HarmonyLib.AccessTools.TypeByName(System.String)" /></summary>
		/// <param name="search">The regular expression that matches against Type.FullName or Type.Name</param>
		/// <param name="invalidateCache">Refetches the cached types if set to true</param>
		/// <returns>The first type where FullName or Name matches the search</returns>
		// Token: 0x06000353 RID: 851 RVA: 0x00011E00 File Offset: 0x00010000
		public static Type TypeSearch(Regex search, bool invalidateCache = false)
		{
			if (AccessTools.allTypesCached == null || invalidateCache)
			{
				AccessTools.allTypesCached = AccessTools.AllTypes().ToArray<Type>();
			}
			Type fullType = AccessTools.allTypesCached.FirstOrDefault((Type t) => search.IsMatch(t.FullName));
			if (fullType != null)
			{
				return fullType;
			}
			Type partialType = AccessTools.allTypesCached.FirstOrDefault((Type t) => search.IsMatch(t.Name));
			if (partialType != null)
			{
				return partialType;
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(68, 1);
			defaultInterpolatedStringHandler.AppendLiteral("AccessTools.TypeSearch: Could not find type with regular expression ");
			defaultInterpolatedStringHandler.AppendFormatted<Regex>(search);
			FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			return null;
		}

		/// <summary>Clears the type cache that <see cref="M:HarmonyLib.AccessTools.TypeSearch(System.Text.RegularExpressions.Regex,System.Boolean)" /> uses</summary>
		// Token: 0x06000354 RID: 852 RVA: 0x00011E9D File Offset: 0x0001009D
		public static void ClearTypeSearchCache()
		{
			AccessTools.allTypesCached = null;
		}

		/// <summary>Gets all successfully loaded types from a given assembly</summary>
		/// <param name="assembly">The assembly</param>
		/// <returns>An array of types</returns>
		/// <remarks>
		///  This calls and returns <see cref="M:System.Reflection.Assembly.GetTypes" />, while catching any thrown <see cref="T:System.Reflection.ReflectionTypeLoadException" />.
		///  If such an exception is thrown, returns the successfully loaded types (<see cref="P:System.Reflection.ReflectionTypeLoadException.Types" />,
		///  filtered for non-null values).
		///  </remarks>
		// Token: 0x06000355 RID: 853 RVA: 0x00011EA8 File Offset: 0x000100A8
		public static Type[] GetTypesFromAssembly(Assembly assembly)
		{
			Type[] result;
			try
			{
				result = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(47, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.GetTypesFromAssembly: assembly ");
				defaultInterpolatedStringHandler.AppendFormatted<Assembly>(assembly);
				defaultInterpolatedStringHandler.AppendLiteral(" => ");
				defaultInterpolatedStringHandler.AppendFormatted<ReflectionTypeLoadException>(ex);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
				result = (from type in ex.Types
					where type != null
					select type).ToArray<Type>();
			}
			return result;
		}

		/// <summary>Enumerates all successfully loaded types in the current app domain, excluding visual studio assemblies</summary>
		/// <returns>An enumeration of all <see cref="T:System.Type" /> in all assemblies, excluding visual studio assemblies</returns>
		// Token: 0x06000356 RID: 854 RVA: 0x00011F40 File Offset: 0x00010140
		public static IEnumerable<Type> AllTypes()
		{
			IEnumerable<Assembly> source = AccessTools.AllAssemblies();
			Func<Assembly, IEnumerable<Type>> selector;
			if ((selector = AccessTools.<>O.<0>__GetTypesFromAssembly) == null)
			{
				selector = (AccessTools.<>O.<0>__GetTypesFromAssembly = new Func<Assembly, IEnumerable<Type>>(AccessTools.GetTypesFromAssembly));
			}
			return source.SelectMany(selector);
		}

		/// <summary>Enumerates all inner types (non-recursive) of a given type</summary>
		/// <param name="type">The class/type to start with</param>
		/// <returns>An enumeration of all inner <see cref="T:System.Type" /></returns>
		// Token: 0x06000357 RID: 855 RVA: 0x00011F67 File Offset: 0x00010167
		public static IEnumerable<Type> InnerTypes(Type type)
		{
			return type.GetNestedTypes(AccessTools.all);
		}

		/// <summary>Applies a function going up the type hierarchy and stops at the first non-<c>null</c> result</summary>
		/// <typeparam name="T">Result type of func()</typeparam>
		/// <param name="type">The class/type to start with</param>
		/// <param name="func">The evaluation function returning T</param>
		/// <returns>The first non-<c>null</c> result, or <c>null</c> if no match</returns>
		/// <remarks>
		///  The type hierarchy of a class or value type (including struct) does NOT include implemented interfaces,
		///  and the type hierarchy of an interface is only itself (regardless of whether that interface implements other interfaces).
		///  The top-most type in the type hierarchy of all non-interface types (including value types) is <see cref="T:System.Object" />.
		///  </remarks>
		// Token: 0x06000358 RID: 856 RVA: 0x00011F74 File Offset: 0x00010174
		public static T FindIncludingBaseTypes<T>(Type type, Func<Type, T> func) where T : class
		{
			T result;
			for (;;)
			{
				result = func(type);
				if (result != null)
				{
					break;
				}
				type = type.BaseType;
				if (type == null)
				{
					goto Block_1;
				}
			}
			return result;
			Block_1:
			return default(T);
		}

		/// <summary>Applies a function going into inner types and stops at the first non-<c>null</c> result</summary>
		/// <typeparam name="T">Generic type parameter</typeparam>
		/// <param name="type">The class/type to start with</param>
		/// <param name="func">The evaluation function returning T</param>
		/// <returns>The first non-<c>null</c> result, or <c>null</c> if no match</returns>
		// Token: 0x06000359 RID: 857 RVA: 0x00011FA8 File Offset: 0x000101A8
		public static T FindIncludingInnerTypes<T>(Type type, Func<Type, T> func) where T : class
		{
			T result = func(type);
			if (result != null)
			{
				return result;
			}
			foreach (Type subType in type.GetNestedTypes(AccessTools.all))
			{
				result = AccessTools.FindIncludingInnerTypes<T>(subType, func);
				if (result != null)
				{
					break;
				}
			}
			return result;
		}

		/// <summary>Creates an identifiable version of a method</summary>
		/// <param name="method">The method</param>
		/// <returns />
		// Token: 0x0600035A RID: 858 RVA: 0x00011FF6 File Offset: 0x000101F6
		public static MethodInfo Identifiable(this MethodInfo method)
		{
			return (PlatformTriple.Current.GetIdentifiable(method) as MethodInfo) ?? method;
		}

		/// <summary>Gets the reflection information for a directly declared field</summary>
		/// <param name="type">The class/type where the field is defined</param>
		/// <param name="name">The name of the field</param>
		/// <returns>A field or null when type/name is null or when the field cannot be found</returns>
		// Token: 0x0600035B RID: 859 RVA: 0x00012010 File Offset: 0x00010210
		public static FieldInfo DeclaredField(Type type, string name)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.DeclaredField: type is null");
				return null;
			}
			if (string.IsNullOrEmpty(name))
			{
				FileLog.Debug("AccessTools.DeclaredField: name is null/empty");
				return null;
			}
			FieldInfo fieldInfo = type.GetField(name, AccessTools.allDeclared);
			if (fieldInfo == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.DeclaredField: Could not find field for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(name);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return fieldInfo;
		}

		/// <summary>Gets the reflection information for a directly declared field</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A field or null when the field cannot be found</returns>
		// Token: 0x0600035C RID: 860 RVA: 0x00012090 File Offset: 0x00010290
		public static FieldInfo DeclaredField(string typeColonName)
		{
			Tools.TypeAndName info = Tools.TypColonName(typeColonName);
			FieldInfo fieldInfo = info.type.GetField(info.name, AccessTools.allDeclared);
			if (fieldInfo == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.DeclaredField: Could not find field for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(info.type);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(info.name);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return fieldInfo;
		}

		/// <summary>Gets the reflection information for a field by searching the type and all its super types</summary>
		/// <param name="type">The class/type where the field is defined</param>
		/// <param name="name">The name of the field (case sensitive)</param>
		/// <returns>A field or null when type/name is null or when the field cannot be found</returns>
		// Token: 0x0600035D RID: 861 RVA: 0x00012108 File Offset: 0x00010308
		public static FieldInfo Field(Type type, string name)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.Field: type is null");
				return null;
			}
			if (string.IsNullOrEmpty(name))
			{
				FileLog.Debug("AccessTools.Field: name is null/empty");
				return null;
			}
			FieldInfo fieldInfo = AccessTools.FindIncludingBaseTypes<FieldInfo>(type, (Type t) => t.GetField(name, AccessTools.all));
			if (fieldInfo == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.Field: Could not find field for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(name);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return fieldInfo;
		}

		/// <summary>Gets the reflection information for a field by searching the type and all its super types</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A field or null when the field cannot be found</returns>
		// Token: 0x0600035E RID: 862 RVA: 0x000121A4 File Offset: 0x000103A4
		public static FieldInfo Field(string typeColonName)
		{
			Tools.TypeAndName info = Tools.TypColonName(typeColonName);
			FieldInfo fieldInfo = AccessTools.FindIncludingBaseTypes<FieldInfo>(info.type, (Type t) => t.GetField(info.name, AccessTools.all));
			if (fieldInfo == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.Field: Could not find field for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(info.type);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(info.name);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return fieldInfo;
		}

		/// <summary>Gets the reflection information for a field</summary>
		/// <param name="type">The class/type where the field is declared</param>
		/// <param name="idx">The zero-based index of the field inside the class definition</param>
		/// <returns>A field or null when type is null or when the field cannot be found</returns>
		// Token: 0x0600035F RID: 863 RVA: 0x00012238 File Offset: 0x00010438
		public static FieldInfo DeclaredField(Type type, int idx)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.DeclaredField: type is null");
				return null;
			}
			FieldInfo fieldInfo = AccessTools.GetDeclaredFields(type).ElementAtOrDefault(idx);
			if (fieldInfo == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(66, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.DeclaredField: Could not find field for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler.AppendLiteral(" and idx ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(idx);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return fieldInfo;
		}

		/// <summary>Gets the reflection information for a directly declared property</summary>
		/// <param name="type">The class/type where the property is declared</param>
		/// <param name="name">The name of the property (case sensitive)</param>
		/// <returns>A property or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x06000360 RID: 864 RVA: 0x000122A4 File Offset: 0x000104A4
		public static PropertyInfo DeclaredProperty(Type type, string name)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.DeclaredProperty: type is null");
				return null;
			}
			if (string.IsNullOrEmpty(name))
			{
				FileLog.Debug("AccessTools.DeclaredProperty: name is null/empty");
				return null;
			}
			PropertyInfo property = type.GetProperty(name, AccessTools.allDeclared);
			if (property == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(73, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.DeclaredProperty: Could not find property for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(name);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return property;
		}

		/// <summary>Gets the reflection information for a directly declared property</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A property or null when the property cannot be found</returns>
		// Token: 0x06000361 RID: 865 RVA: 0x00012324 File Offset: 0x00010524
		public static PropertyInfo DeclaredProperty(string typeColonName)
		{
			Tools.TypeAndName info = Tools.TypColonName(typeColonName);
			PropertyInfo property = info.type.GetProperty(info.name, AccessTools.allDeclared);
			if (property == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(73, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.DeclaredProperty: Could not find property for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(info.type);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(info.name);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return property;
		}

		/// <summary>Gets the reflection information for a directly declared indexer property</summary>
		/// <param name="type">The class/type where the indexer property is declared</param>
		/// <param name="parameters">Optional parameters to target a specific overload of multiple indexers</param>
		/// <returns>An indexer property or null when type is null or when it cannot be found</returns>
		// Token: 0x06000362 RID: 866 RVA: 0x0001239C File Offset: 0x0001059C
		public static PropertyInfo DeclaredIndexer(Type type, Type[] parameters = null)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.DeclaredIndexer: type is null");
				return null;
			}
			PropertyInfo result;
			try
			{
				PropertyInfo propertyInfo;
				if (parameters != null)
				{
					propertyInfo = type.GetProperties(AccessTools.allDeclared).FirstOrDefault((PropertyInfo property) => (from param in property.GetIndexParameters()
						select param.ParameterType).SequenceEqual(parameters));
				}
				else
				{
					propertyInfo = type.GetProperties(AccessTools.allDeclared).SingleOrDefault((PropertyInfo property) => property.GetIndexParameters().Length != 0);
				}
				PropertyInfo indexer = propertyInfo;
				if (indexer == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(77, 2);
					defaultInterpolatedStringHandler.AppendLiteral("AccessTools.DeclaredIndexer: Could not find indexer for type ");
					defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
					defaultInterpolatedStringHandler.AppendLiteral(" and parameters ");
					Type[] parameters2 = parameters;
					defaultInterpolatedStringHandler.AppendFormatted((parameters2 != null) ? parameters2.Description() : null);
					FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				result = indexer;
			}
			catch (InvalidOperationException ex)
			{
				throw new AmbiguousMatchException("Multiple possible indexers were found.", ex);
			}
			return result;
		}

		/// <summary>Gets the reflection information for the getter method of a directly declared property</summary>
		/// <param name="type">The class/type where the property is declared</param>
		/// <param name="name">The name of the property (case sensitive)</param>
		/// <returns>A method or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x06000363 RID: 867 RVA: 0x00012494 File Offset: 0x00010694
		public static MethodInfo DeclaredPropertyGetter(Type type, string name)
		{
			PropertyInfo propertyInfo = AccessTools.DeclaredProperty(type, name);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetGetMethod(true);
		}

		/// <summary>Gets the reflection information for the getter method of a directly declared property</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A method or null when the property cannot be found</returns>
		// Token: 0x06000364 RID: 868 RVA: 0x000124A9 File Offset: 0x000106A9
		public static MethodInfo DeclaredPropertyGetter(string typeColonName)
		{
			PropertyInfo propertyInfo = AccessTools.DeclaredProperty(typeColonName);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetGetMethod(true);
		}

		/// <summary>Gets the reflection information for the getter method of a directly declared indexer property</summary>
		/// <param name="type">The class/type where the indexer property is declared</param>
		/// <param name="parameters">Optional parameters to target a specific overload of multiple indexers</param>
		/// <returns>A method or null when type is null or when indexer property cannot be found</returns>
		// Token: 0x06000365 RID: 869 RVA: 0x000124BD File Offset: 0x000106BD
		public static MethodInfo DeclaredIndexerGetter(Type type, Type[] parameters = null)
		{
			PropertyInfo propertyInfo = AccessTools.DeclaredIndexer(type, parameters);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetGetMethod(true);
		}

		/// <summary>Gets the reflection information for the setter method of a directly declared property</summary>
		/// <param name="type">The class/type where the property is declared</param>
		/// <param name="name">The name of the property (case sensitive)</param>
		/// <returns>A method or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x06000366 RID: 870 RVA: 0x000124D2 File Offset: 0x000106D2
		public static MethodInfo DeclaredPropertySetter(Type type, string name)
		{
			PropertyInfo propertyInfo = AccessTools.DeclaredProperty(type, name);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetSetMethod(true);
		}

		/// <summary>Gets the reflection information for the Setter method of a directly declared property</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A method or null when the property cannot be found</returns>
		// Token: 0x06000367 RID: 871 RVA: 0x000124E7 File Offset: 0x000106E7
		public static MethodInfo DeclaredPropertySetter(string typeColonName)
		{
			PropertyInfo propertyInfo = AccessTools.DeclaredProperty(typeColonName);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetSetMethod(true);
		}

		/// <summary>Gets the reflection information for the setter method of a directly declared indexer property</summary>
		/// <param name="type">The class/type where the indexer property is declared</param>
		/// <param name="parameters">Optional parameters to target a specific overload of multiple indexers</param>
		/// <returns>A method or null when type is null or when indexer property cannot be found</returns>
		// Token: 0x06000368 RID: 872 RVA: 0x000124FB File Offset: 0x000106FB
		public static MethodInfo DeclaredIndexerSetter(Type type, Type[] parameters)
		{
			PropertyInfo propertyInfo = AccessTools.DeclaredIndexer(type, parameters);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetSetMethod(true);
		}

		/// <summary>Gets the reflection information for a property by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="name">The name</param>
		/// <returns>A property or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x06000369 RID: 873 RVA: 0x00012510 File Offset: 0x00010710
		public static PropertyInfo Property(Type type, string name)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.Property: type is null");
				return null;
			}
			if (string.IsNullOrEmpty(name))
			{
				FileLog.Debug("AccessTools.Property: name is null/empty");
				return null;
			}
			PropertyInfo property = AccessTools.FindIncludingBaseTypes<PropertyInfo>(type, (Type t) => t.GetProperty(name, AccessTools.all));
			if (property == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(65, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.Property: Could not find property for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(name);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return property;
		}

		/// <summary>Gets the reflection information for a property by searching the type and all its super types</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A property or null when the property cannot be found</returns>
		// Token: 0x0600036A RID: 874 RVA: 0x000125AC File Offset: 0x000107AC
		public static PropertyInfo Property(string typeColonName)
		{
			Tools.TypeAndName info = Tools.TypColonName(typeColonName);
			PropertyInfo property = AccessTools.FindIncludingBaseTypes<PropertyInfo>(info.type, (Type t) => t.GetProperty(info.name, AccessTools.all));
			if (property == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(65, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.Property: Could not find property for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(info.type);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(info.name);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return property;
		}

		/// <summary>Gets the reflection information for an indexer property by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="parameters">Optional parameters to target a specific overload of multiple indexers</param>
		/// <returns>An indexer property or null when type is null or when it cannot be found</returns>
		// Token: 0x0600036B RID: 875 RVA: 0x00012640 File Offset: 0x00010840
		public static PropertyInfo Indexer(Type type, Type[] parameters = null)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.Indexer: type is null");
				return null;
			}
			Func<Type, PropertyInfo> func2;
			if (parameters != null)
			{
				Func<PropertyInfo, bool> <>9__3;
				func2 = delegate(Type t)
				{
					IEnumerable<PropertyInfo> properties = t.GetProperties(AccessTools.all);
					Func<PropertyInfo, bool> predicate;
					if ((predicate = <>9__3) == null)
					{
						predicate = (<>9__3 = (PropertyInfo property) => (from param in property.GetIndexParameters()
							select param.ParameterType).SequenceEqual(parameters));
					}
					return properties.FirstOrDefault(predicate);
				};
			}
			else
			{
				func2 = (Type t) => t.GetProperties(AccessTools.all).SingleOrDefault((PropertyInfo property) => property.GetIndexParameters().Length != 0);
			}
			Func<Type, PropertyInfo> func = func2;
			PropertyInfo result;
			try
			{
				PropertyInfo indexer = AccessTools.FindIncludingBaseTypes<PropertyInfo>(type, func);
				if (indexer == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(69, 2);
					defaultInterpolatedStringHandler.AppendLiteral("AccessTools.Indexer: Could not find indexer for type ");
					defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
					defaultInterpolatedStringHandler.AppendLiteral(" and parameters ");
					Type[] parameters2 = parameters;
					defaultInterpolatedStringHandler.AppendFormatted((parameters2 != null) ? parameters2.Description() : null);
					FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				result = indexer;
			}
			catch (InvalidOperationException ex)
			{
				throw new AmbiguousMatchException("Multiple possible indexers were found.", ex);
			}
			return result;
		}

		/// <summary>Gets the reflection information for the getter method of a property by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="name">The name</param>
		/// <returns>A method or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x0600036C RID: 876 RVA: 0x00012720 File Offset: 0x00010920
		public static MethodInfo PropertyGetter(Type type, string name)
		{
			PropertyInfo propertyInfo = AccessTools.Property(type, name);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetGetMethod(true);
		}

		/// <summary>Gets the reflection information for the getter method of a property by searching the type and all its super types</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A method or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x0600036D RID: 877 RVA: 0x00012735 File Offset: 0x00010935
		public static MethodInfo PropertyGetter(string typeColonName)
		{
			PropertyInfo propertyInfo = AccessTools.Property(typeColonName);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetGetMethod(true);
		}

		/// <summary>Gets the reflection information for the getter method of an indexer property by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="parameters">Optional parameters to target a specific overload of multiple indexers</param>
		/// <returns>A method or null when type is null or when the indexer property cannot be found</returns>
		// Token: 0x0600036E RID: 878 RVA: 0x00012749 File Offset: 0x00010949
		public static MethodInfo IndexerGetter(Type type, Type[] parameters = null)
		{
			PropertyInfo propertyInfo = AccessTools.Indexer(type, parameters);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetGetMethod(true);
		}

		/// <summary>Gets the reflection information for the setter method of a property by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="name">The name</param>
		/// <returns>A method or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x0600036F RID: 879 RVA: 0x0001275E File Offset: 0x0001095E
		public static MethodInfo PropertySetter(Type type, string name)
		{
			PropertyInfo propertyInfo = AccessTools.Property(type, name);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetSetMethod(true);
		}

		/// <summary>Gets the reflection information for the setter method of a property by searching the type and all its super types</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A method or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x06000370 RID: 880 RVA: 0x00012773 File Offset: 0x00010973
		public static MethodInfo PropertySetter(string typeColonName)
		{
			PropertyInfo propertyInfo = AccessTools.Property(typeColonName);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetSetMethod(true);
		}

		/// <summary>Gets the reflection information for the setter method of an indexer property by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="parameters">Optional parameters to target a specific overload of multiple indexers</param>
		/// <returns>A method or null when type is null or when the indexer property cannot be found</returns>
		// Token: 0x06000371 RID: 881 RVA: 0x00012787 File Offset: 0x00010987
		public static MethodInfo IndexerSetter(Type type, Type[] parameters = null)
		{
			PropertyInfo propertyInfo = AccessTools.Indexer(type, parameters);
			if (propertyInfo == null)
			{
				return null;
			}
			return propertyInfo.GetSetMethod(true);
		}

		/// <summary>Gets the reflection information for a directly declared event</summary>
		/// <param name="type">The class/type where the event is declared</param>
		/// <param name="name">The name of the event (case sensitive)</param>
		/// <returns>An event or null when type/name is null or when the event cannot be found</returns>
		// Token: 0x06000372 RID: 882 RVA: 0x0001279C File Offset: 0x0001099C
		public static EventInfo DeclaredEvent(Type type, string name)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.DeclaredEvent: type is null");
				return null;
			}
			if (string.IsNullOrEmpty(name))
			{
				FileLog.Debug("AccessTools.DeclaredEvent: name is null/empty");
				return null;
			}
			EventInfo eventInfo = type.GetEvent(name, AccessTools.allDeclared);
			if (eventInfo == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.DeclaredEvent: Could not find event for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(name);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return eventInfo;
		}

		/// <summary>Gets the reflection information for a directly declared event</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>An event or null when the event cannot be found</returns>
		// Token: 0x06000373 RID: 883 RVA: 0x0001281C File Offset: 0x00010A1C
		public static EventInfo DeclaredEvent(string typeColonName)
		{
			Tools.TypeAndName info = Tools.TypColonName(typeColonName);
			EventInfo eventInfo = info.type.GetEvent(info.name, AccessTools.allDeclared);
			if (eventInfo == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.DeclaredEvent: Could not find event for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(info.type);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(info.name);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return eventInfo;
		}

		/// <summary>Gets the reflection information for an event by searching the type and all its super types</summary>
		/// <param name="type">The class/type where the event is declared</param>
		/// <param name="name">The name of the event (case sensitive)</param>
		/// <returns>An event or null when type/name is null or when the event cannot be found</returns>
		// Token: 0x06000374 RID: 884 RVA: 0x00012894 File Offset: 0x00010A94
		public static EventInfo Event(Type type, string name)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.Event: type is null");
				return null;
			}
			if (string.IsNullOrEmpty(name))
			{
				FileLog.Debug("AccessTools.Event: name is null/empty");
				return null;
			}
			EventInfo eventInfo = AccessTools.FindIncludingBaseTypes<EventInfo>(type, (Type t) => t.GetEvent(name, AccessTools.all));
			if (eventInfo == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.Event: Could not find event for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(name);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return eventInfo;
		}

		/// <summary>Gets the reflection information for an event by searching the type and all its super types</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>An event or null when the event cannot be found</returns>
		// Token: 0x06000375 RID: 885 RVA: 0x00012930 File Offset: 0x00010B30
		public static EventInfo Event(string typeColonName)
		{
			Tools.TypeAndName info = Tools.TypColonName(typeColonName);
			EventInfo eventInfo = AccessTools.FindIncludingBaseTypes<EventInfo>(info.type, (Type t) => t.GetEvent(info.name, AccessTools.all));
			if (eventInfo == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 2);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.Event: Could not find event for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(info.type);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(info.name);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return eventInfo;
		}

		/// <summary>Gets the reflection information for the add method of a directly declared event</summary>
		/// <param name="type">The class/type where the event is declared</param>
		/// <param name="name">The name of the event (case sensitive)</param>
		/// <returns>A method or null when type/name is null or when the event cannot be found</returns>
		// Token: 0x06000376 RID: 886 RVA: 0x000129C2 File Offset: 0x00010BC2
		public static MethodInfo DeclaredEventAdder(Type type, string name)
		{
			EventInfo eventInfo = AccessTools.DeclaredEvent(type, name);
			if (eventInfo == null)
			{
				return null;
			}
			return eventInfo.GetAddMethod(true);
		}

		/// <summary>Gets the reflection information for the add method of a directly declared event</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A method or null when the event cannot be found</returns>
		// Token: 0x06000377 RID: 887 RVA: 0x000129D7 File Offset: 0x00010BD7
		public static MethodInfo DeclaredEventAdder(string typeColonName)
		{
			EventInfo eventInfo = AccessTools.DeclaredEvent(typeColonName);
			if (eventInfo == null)
			{
				return null;
			}
			return eventInfo.GetAddMethod(true);
		}

		/// <summary>Gets the reflection information for the add method of an event by searching the type and all its super types</summary>
		/// <param name="type">The class/type where the event is declared</param>
		/// <param name="name">The name of the event (case sensitive)</param>
		/// <returns>A method or null when type/name is null or when the event cannot be found</returns>
		// Token: 0x06000378 RID: 888 RVA: 0x000129EB File Offset: 0x00010BEB
		public static MethodInfo EventAdder(Type type, string name)
		{
			EventInfo eventInfo = AccessTools.Event(type, name);
			if (eventInfo == null)
			{
				return null;
			}
			return eventInfo.GetAddMethod(true);
		}

		/// <summary>Gets the reflection information for the add method of an event by searching the type and all its super types</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A method or null when the event cannot be found</returns>
		// Token: 0x06000379 RID: 889 RVA: 0x00012A00 File Offset: 0x00010C00
		public static MethodInfo EventAdder(string typeColonName)
		{
			EventInfo eventInfo = AccessTools.Event(typeColonName);
			if (eventInfo == null)
			{
				return null;
			}
			return eventInfo.GetAddMethod(true);
		}

		/// <summary>Gets the reflection information for the remove method of a directly declared event</summary>
		/// <param name="type">The class/type where the event is declared</param>
		/// <param name="name">The name of the event (case sensitive)</param>
		/// <returns>A method or null when type/name is null or when the event cannot be found</returns>
		// Token: 0x0600037A RID: 890 RVA: 0x00012A14 File Offset: 0x00010C14
		public static MethodInfo DeclaredEventRemover(Type type, string name)
		{
			EventInfo eventInfo = AccessTools.DeclaredEvent(type, name);
			if (eventInfo == null)
			{
				return null;
			}
			return eventInfo.GetRemoveMethod(true);
		}

		/// <summary>Gets the reflection information for the remove method of a directly declared event</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A method or null when the event cannot be found</returns>
		// Token: 0x0600037B RID: 891 RVA: 0x00012A29 File Offset: 0x00010C29
		public static MethodInfo DeclaredEventRemover(string typeColonName)
		{
			EventInfo eventInfo = AccessTools.DeclaredEvent(typeColonName);
			if (eventInfo == null)
			{
				return null;
			}
			return eventInfo.GetRemoveMethod(true);
		}

		/// <summary>Gets the reflection information for the remove method of an event by searching the type and all its super types</summary>
		/// <param name="type">The class/type where the event is declared</param>
		/// <param name="name">The name of the event (case sensitive)</param>
		/// <returns>A method or null when type/name is null or when the event cannot be found</returns>
		// Token: 0x0600037C RID: 892 RVA: 0x00012A3D File Offset: 0x00010C3D
		public static MethodInfo EventRemover(Type type, string name)
		{
			EventInfo eventInfo = AccessTools.Event(type, name);
			if (eventInfo == null)
			{
				return null;
			}
			return eventInfo.GetRemoveMethod(true);
		}

		/// <summary>Gets the reflection information for the remove method of an event by searching the type and all its super types</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A method or null when the event cannot be found</returns>
		// Token: 0x0600037D RID: 893 RVA: 0x00012A52 File Offset: 0x00010C52
		public static MethodInfo EventRemover(string typeColonName)
		{
			EventInfo eventInfo = AccessTools.Event(typeColonName);
			if (eventInfo == null)
			{
				return null;
			}
			return eventInfo.GetRemoveMethod(true);
		}

		/// <summary>Gets the reflection information for a directly declared method</summary>
		/// <param name="type">The class/type where the method is declared</param>
		/// <param name="name">The name of the method (case sensitive)</param>
		/// <param name="parameters">Optional parameters to target a specific overload of the method</param>
		/// <param name="generics">Optional list of types that define the generic version of the method</param>
		/// <returns>A method or null when type/name is null or when the method cannot be found</returns>
		// Token: 0x0600037E RID: 894 RVA: 0x00012A68 File Offset: 0x00010C68
		public static MethodInfo DeclaredMethod(Type type, string name, Type[] parameters = null, Type[] generics = null)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.DeclaredMethod: type is null");
				return null;
			}
			if (string.IsNullOrEmpty(name))
			{
				FileLog.Debug("AccessTools.DeclaredMethod: name is null/empty");
				return null;
			}
			ParameterModifier[] modifiers = new ParameterModifier[0];
			MethodInfo result;
			if (parameters == null)
			{
				result = type.GetMethod(name, AccessTools.allDeclared);
			}
			else
			{
				result = type.GetMethod(name, AccessTools.allDeclared, null, parameters, modifiers);
			}
			if (result == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(85, 3);
				defaultInterpolatedStringHandler.AppendLiteral("AccessTools.DeclaredMethod: Could not find method for type ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler.AppendFormatted(name);
				defaultInterpolatedStringHandler.AppendLiteral(" and parameters ");
				defaultInterpolatedStringHandler.AppendFormatted((parameters != null) ? parameters.Description() : null);
				FileLog.Debug(defaultInterpolatedStringHandler.ToStringAndClear());
				return null;
			}
			if (generics != null)
			{
				result = result.MakeGenericMethod(generics);
			}
			return result;
		}

		/// <summary>Gets the reflection information for a directly declared method</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <param name="parameters">Optional parameters to target a specific overload of the method</param>
		/// <param name="generics">Optional list of types that define the generic version of the method</param>
		/// <returns>A method or null when the method cannot be found</returns>
		// Token: 0x0600037F RID: 895 RVA: 0x00012B30 File Offset: 0x00010D30
		public static MethodInfo DeclaredMethod(string typeColonName, Type[] parameters = null, Type[] generics = null)
		{
			Tools.TypeAndName info = Tools.TypColonName(typeColonName);
			return AccessTools.DeclaredMethod(info.type, info.name, parameters, generics);
		}

		/// <summary>Gets the reflection information for a method by searching the type and all its super types</summary>
		/// <param name="type">The class/type where the method is declared</param>
		/// <param name="name">The name of the method (case sensitive)</param>
		/// <param name="parameters">Optional parameters to target a specific overload of the method</param>
		/// <param name="generics">Optional list of types that define the generic version of the method</param>
		/// <returns>A method or null when type/name is null or when the method cannot be found</returns>
		// Token: 0x06000380 RID: 896 RVA: 0x00012B58 File Offset: 0x00010D58
		public static MethodInfo Method(Type type, string name, Type[] parameters = null, Type[] generics = null)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.Method: type is null");
				return null;
			}
			if (string.IsNullOrEmpty(name))
			{
				FileLog.Debug("AccessTools.Method: name is null/empty");
				return null;
			}
			ParameterModifier[] modifiers = new ParameterModifier[0];
			MethodInfo result;
			if (parameters == null)
			{
				try
				{
					result = AccessTools.FindIncludingBaseTypes<MethodInfo>(type, (Type t) => t.GetMethod(name, AccessTools.all));
					goto IL_D6;
				}
				catch (AmbiguousMatchException ex)
				{
					result = AccessTools.FindIncludingBaseTypes<MethodInfo>(type, (Type t) => t.GetMethod(name, AccessTools.all, null, Array.Empty<Type>(), modifiers));
					if (result == null)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Ambiguous match in Harmony patch for ");
						defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
						defaultInterpolatedStringHandler.AppendLiteral(":");
						defaultInterpolatedStringHandler.AppendFormatted(name);
						throw new AmbiguousMatchException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
					}
					goto IL_D6;
				}
			}
			result = AccessTools.FindIncludingBaseTypes<MethodInfo>(type, (Type t) => t.GetMethod(name, AccessTools.all, null, parameters, modifiers));
			IL_D6:
			if (result == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(77, 3);
				defaultInterpolatedStringHandler2.AppendLiteral("AccessTools.Method: Could not find method for type ");
				defaultInterpolatedStringHandler2.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler2.AppendLiteral(" and name ");
				defaultInterpolatedStringHandler2.AppendFormatted(name);
				defaultInterpolatedStringHandler2.AppendLiteral(" and parameters ");
				Type[] parameters2 = parameters;
				defaultInterpolatedStringHandler2.AppendFormatted((parameters2 != null) ? parameters2.Description() : null);
				FileLog.Debug(defaultInterpolatedStringHandler2.ToStringAndClear());
				return null;
			}
			if (generics != null)
			{
				result = result.MakeGenericMethod(generics);
			}
			return result;
		}

		/// <summary>Gets the reflection information for a method by searching the type and all its super types</summary>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <param name="parameters">Optional parameters to target a specific overload of the method</param>
		/// <param name="generics">Optional list of types that define the generic version of the method</param>
		/// <returns>A method or null when the method cannot be found</returns>
		// Token: 0x06000381 RID: 897 RVA: 0x00012CC4 File Offset: 0x00010EC4
		public static MethodInfo Method(string typeColonName, Type[] parameters = null, Type[] generics = null)
		{
			Tools.TypeAndName info = Tools.TypColonName(typeColonName);
			return AccessTools.Method(info.type, info.name, parameters, generics);
		}

		/// <summary>Gets the <see cref="M:System.Collections.IEnumerator.MoveNext" /> method of an enumerator method</summary>
		/// <param name="method">Enumerator method that creates the enumerator <see cref="T:System.Collections.IEnumerator" /></param>
		/// <returns>The internal <see cref="M:System.Collections.IEnumerator.MoveNext" /> method of the enumerator or <b>null</b> if no valid enumerator is detected</returns>
		// Token: 0x06000382 RID: 898 RVA: 0x00012CEC File Offset: 0x00010EEC
		public static MethodInfo EnumeratorMoveNext(MethodBase method)
		{
			if (method == null)
			{
				FileLog.Debug("AccessTools.EnumeratorMoveNext: method is null");
				return null;
			}
			IEnumerable<KeyValuePair<OpCode, object>> codes = from pair in PatchProcessor.ReadMethodBody(method)
				where pair.Key == OpCodes.Newobj
				select pair;
			if (codes.Count<KeyValuePair<OpCode, object>>() != 1)
			{
				FileLog.Debug("AccessTools.EnumeratorMoveNext: " + method.FullDescription() + " contains no Newobj opcode");
				return null;
			}
			ConstructorInfo ctor = codes.First<KeyValuePair<OpCode, object>>().Value as ConstructorInfo;
			if (ctor == null)
			{
				FileLog.Debug("AccessTools.EnumeratorMoveNext: " + method.FullDescription() + " contains no constructor");
				return null;
			}
			Type type = ctor.DeclaringType;
			if (type == null)
			{
				FileLog.Debug("AccessTools.EnumeratorMoveNext: " + method.FullDescription() + " refers to a global type");
				return null;
			}
			return AccessTools.Method(type, "MoveNext", null, null);
		}

		/// <summary>Gets the <see cref="M:System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext" /> method of an async method's state machine</summary>
		/// <param name="method">Async method that creates the state machine internally</param>
		/// <returns>The internal <see cref="M:System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext" /> method of the async state machine or <b>null</b> if no valid async method is detected</returns>
		// Token: 0x06000383 RID: 899 RVA: 0x00012DCC File Offset: 0x00010FCC
		public static MethodInfo AsyncMoveNext(MethodBase method)
		{
			if (method == null)
			{
				FileLog.Debug("AccessTools.AsyncMoveNext: method is null");
				return null;
			}
			AsyncStateMachineAttribute asyncAttribute = method.GetCustomAttribute<AsyncStateMachineAttribute>();
			if (asyncAttribute == null)
			{
				FileLog.Debug("AccessTools.AsyncMoveNext: Could not find AsyncStateMachine for " + method.FullDescription());
				return null;
			}
			Type asyncStateMachineType = asyncAttribute.StateMachineType;
			MethodInfo asyncMethodBody = AccessTools.DeclaredMethod(asyncStateMachineType, "MoveNext", null, null);
			if (asyncMethodBody == null)
			{
				FileLog.Debug("AccessTools.AsyncMoveNext: Could not find async method body for " + method.FullDescription());
				return null;
			}
			return asyncMethodBody;
		}

		/// <summary>Gets the reflection information for a finalizer</summary>
		/// <param name="type">The class/type that defines the finalizer</param>
		/// <returns>A method or null when type is null or when the finalizer cannot be found</returns>
		// Token: 0x06000384 RID: 900 RVA: 0x00012E39 File Offset: 0x00011039
		public static MethodInfo Finalizer(Type type)
		{
			return AccessTools.Method(type, "Finalize", null, null);
		}

		/// <summary>Gets the reflection information for a directly declared finalizer</summary>
		/// <param name="type">The class/type that defines the finalizer</param>
		/// <returns>A method or null when type is null or when the finalizer cannot be found</returns>
		// Token: 0x06000385 RID: 901 RVA: 0x00012E48 File Offset: 0x00011048
		public static MethodInfo DeclaredFinalizer(Type type)
		{
			return AccessTools.DeclaredMethod(type, "Finalize", null, null);
		}

		/// <summary>Gets the names of all method that are declared in a type</summary>
		/// <param name="type">The declaring class/type</param>
		/// <returns>A list of method names</returns>
		// Token: 0x06000386 RID: 902 RVA: 0x00012E58 File Offset: 0x00011058
		public static List<string> GetMethodNames(Type type)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.GetMethodNames: type is null");
				return new List<string>();
			}
			return (from m in AccessTools.GetDeclaredMethods(type)
				select m.Name).ToList<string>();
		}

		/// <summary>Gets the names of all method that are declared in the type of the instance</summary>
		/// <param name="instance">An instance of the type to search in</param>
		/// <returns>A list of method names</returns>
		// Token: 0x06000387 RID: 903 RVA: 0x00012EA7 File Offset: 0x000110A7
		public static List<string> GetMethodNames(object instance)
		{
			if (instance == null)
			{
				FileLog.Debug("AccessTools.GetMethodNames: instance is null");
				return new List<string>();
			}
			return AccessTools.GetMethodNames(instance.GetType());
		}

		/// <summary>Gets the names of all fields that are declared in a type</summary>
		/// <param name="type">The declaring class/type</param>
		/// <returns>A list of field names</returns>
		// Token: 0x06000388 RID: 904 RVA: 0x00012EC8 File Offset: 0x000110C8
		public static List<string> GetFieldNames(Type type)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.GetFieldNames: type is null");
				return new List<string>();
			}
			return (from f in AccessTools.GetDeclaredFields(type)
				select f.Name).ToList<string>();
		}

		/// <summary>Gets the names of all fields that are declared in the type of the instance</summary>
		/// <param name="instance">An instance of the type to search in</param>
		/// <returns>A list of field names</returns>
		// Token: 0x06000389 RID: 905 RVA: 0x00012F17 File Offset: 0x00011117
		public static List<string> GetFieldNames(object instance)
		{
			if (instance == null)
			{
				FileLog.Debug("AccessTools.GetFieldNames: instance is null");
				return new List<string>();
			}
			return AccessTools.GetFieldNames(instance.GetType());
		}

		/// <summary>Gets the names of all properties that are declared in a type</summary>
		/// <param name="type">The declaring class/type</param>
		/// <returns>A list of property names</returns>
		// Token: 0x0600038A RID: 906 RVA: 0x00012F38 File Offset: 0x00011138
		public static List<string> GetPropertyNames(Type type)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.GetPropertyNames: type is null");
				return new List<string>();
			}
			return (from f in AccessTools.GetDeclaredProperties(type)
				select f.Name).ToList<string>();
		}

		/// <summary>Gets the names of all properties that are declared in the type of the instance</summary>
		/// <param name="instance">An instance of the type to search in</param>
		/// <returns>A list of property names</returns>
		// Token: 0x0600038B RID: 907 RVA: 0x00012F87 File Offset: 0x00011187
		public static List<string> GetPropertyNames(object instance)
		{
			if (instance == null)
			{
				FileLog.Debug("AccessTools.GetPropertyNames: instance is null");
				return new List<string>();
			}
			return AccessTools.GetPropertyNames(instance.GetType());
		}

		/// <summary>Gets the type of any class member of</summary>
		/// <param name="member">A member</param>
		/// <returns>The class/type of this member</returns>
		// Token: 0x0600038C RID: 908 RVA: 0x00012FA8 File Offset: 0x000111A8
		public static Type GetUnderlyingType(this MemberInfo member)
		{
			MemberTypes memberType = member.MemberType;
			if (memberType <= MemberTypes.Field)
			{
				if (memberType == MemberTypes.Event)
				{
					return ((EventInfo)member).EventHandlerType;
				}
				if (memberType == MemberTypes.Field)
				{
					return ((FieldInfo)member).FieldType;
				}
			}
			else
			{
				if (memberType == MemberTypes.Method)
				{
					return ((MethodInfo)member).ReturnType;
				}
				if (memberType == MemberTypes.Property)
				{
					return ((PropertyInfo)member).PropertyType;
				}
			}
			throw new ArgumentException("Member must be of type EventInfo, FieldInfo, MethodInfo, or PropertyInfo");
		}

		/// <summary>Returns a <see cref="T:System.Reflection.MethodInfo" /> by searching for module-id and token</summary>
		/// <param name="moduleGUID">The module of the method</param>
		/// <param name="token">The token of the method</param>
		/// <returns />
		// Token: 0x0600038D RID: 909 RVA: 0x0001301C File Offset: 0x0001121C
		public static MethodInfo GetMethodByModuleAndToken(string moduleGUID, int token)
		{
			Module module = (from a in AppDomain.CurrentDomain.GetAssemblies()
				where !a.FullName.StartsWith("Microsoft.VisualStudio")
				select a).SelectMany((Assembly a) => a.GetLoadedModules()).First((Module m) => m.ModuleVersionId.ToString() == moduleGUID);
			if (!(module == null))
			{
				return (MethodInfo)module.ResolveMethod(token);
			}
			return null;
		}

		/// <summary>Test if a class member is actually an concrete implementation</summary>
		/// <param name="member">A member</param>
		/// <returns>True if the member is a declared</returns>
		// Token: 0x0600038E RID: 910 RVA: 0x000130B1 File Offset: 0x000112B1
		public static bool IsDeclaredMember<T>(this T member) where T : MemberInfo
		{
			return member.DeclaringType == member.ReflectedType;
		}

		/// <summary>Gets the real implementation of a class member</summary>
		/// <param name="member">A member</param>
		/// <returns>The member itself if its declared. Otherwise the member that is actually implemented in some base type</returns>
		// Token: 0x0600038F RID: 911 RVA: 0x000130D0 File Offset: 0x000112D0
		public static T GetDeclaredMember<T>(this T member) where T : MemberInfo
		{
			if (member.DeclaringType == null || member.IsDeclaredMember<T>())
			{
				return member;
			}
			int metaToken = member.MetadataToken;
			Type declaringType = member.DeclaringType;
			MemberInfo[] members = ((declaringType != null) ? declaringType.GetMembers(AccessTools.all) : null) ?? Array.Empty<MemberInfo>();
			foreach (MemberInfo other in members)
			{
				if (other.MetadataToken == metaToken)
				{
					return (T)((object)other);
				}
			}
			return member;
		}

		/// <summary>Gets the reflection information for a directly declared constructor</summary>
		/// <param name="type">The class/type where the constructor is declared</param>
		/// <param name="parameters">Optional parameters to target a specific overload of the constructor</param>
		/// <param name="searchForStatic">Optional parameters to only consider static constructors</param>
		/// <returns>A constructor info or null when type is null or when the constructor cannot be found</returns>
		// Token: 0x06000390 RID: 912 RVA: 0x00013150 File Offset: 0x00011350
		public static ConstructorInfo DeclaredConstructor(Type type, Type[] parameters = null, bool searchForStatic = false)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.DeclaredConstructor: type is null");
				return null;
			}
			if (parameters == null)
			{
				parameters = Array.Empty<Type>();
			}
			BindingFlags flags = (searchForStatic ? (AccessTools.allDeclared & ~BindingFlags.Instance) : (AccessTools.allDeclared & ~BindingFlags.Static));
			return type.GetConstructor(flags, null, parameters, Array.Empty<ParameterModifier>());
		}

		/// <summary>Gets the reflection information for a constructor by searching the type and all its super types</summary>
		/// <param name="type">The class/type where the constructor is declared</param>
		/// <param name="parameters">Optional parameters to target a specific overload of the method</param>
		/// <param name="searchForStatic">Optional parameters to only consider static constructors</param>
		/// <returns>A constructor info or null when type is null or when the method cannot be found</returns>
		// Token: 0x06000391 RID: 913 RVA: 0x0001319C File Offset: 0x0001139C
		public static ConstructorInfo Constructor(Type type, Type[] parameters = null, bool searchForStatic = false)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.ConstructorInfo: type is null");
				return null;
			}
			if (parameters == null)
			{
				parameters = Array.Empty<Type>();
			}
			BindingFlags flags = (searchForStatic ? (AccessTools.all & ~BindingFlags.Instance) : (AccessTools.all & ~BindingFlags.Static));
			return AccessTools.FindIncludingBaseTypes<ConstructorInfo>(type, (Type t) => t.GetConstructor(flags, null, parameters, Array.Empty<ParameterModifier>()));
		}

		/// <summary>Gets reflection information for all declared constructors</summary>
		/// <param name="type">The class/type where the constructors are declared</param>
		/// <param name="searchForStatic">Optional parameters to only consider static constructors</param>
		/// <returns>A list of constructor infos</returns>
		// Token: 0x06000392 RID: 914 RVA: 0x00013208 File Offset: 0x00011408
		public static List<ConstructorInfo> GetDeclaredConstructors(Type type, bool? searchForStatic = null)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.GetDeclaredConstructors: type is null");
				return new List<ConstructorInfo>();
			}
			BindingFlags flags = AccessTools.allDeclared;
			if (searchForStatic != null)
			{
				flags = (searchForStatic.Value ? (flags & ~BindingFlags.Instance) : (flags & ~BindingFlags.Static));
			}
			return (from method in type.GetConstructors(flags)
				where method.DeclaringType == type
				select method).ToList<ConstructorInfo>();
		}

		/// <summary>Gets reflection information for all declared methods</summary>
		/// <param name="type">The class/type where the methods are declared</param>
		/// <returns>A list of methods</returns>
		// Token: 0x06000393 RID: 915 RVA: 0x0001327F File Offset: 0x0001147F
		public static List<MethodInfo> GetDeclaredMethods(Type type)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.GetDeclaredMethods: type is null");
				return new List<MethodInfo>();
			}
			return type.GetMethods(AccessTools.allDeclared).ToList<MethodInfo>();
		}

		/// <summary>Gets reflection information for all declared properties</summary>
		/// <param name="type">The class/type where the properties are declared</param>
		/// <returns>A list of properties</returns>
		// Token: 0x06000394 RID: 916 RVA: 0x000132A4 File Offset: 0x000114A4
		public static List<PropertyInfo> GetDeclaredProperties(Type type)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.GetDeclaredProperties: type is null");
				return new List<PropertyInfo>();
			}
			return type.GetProperties(AccessTools.allDeclared).ToList<PropertyInfo>();
		}

		/// <summary>Gets reflection information for all declared fields</summary>
		/// <param name="type">The class/type where the fields are declared</param>
		/// <returns>A list of fields</returns>
		// Token: 0x06000395 RID: 917 RVA: 0x000132C9 File Offset: 0x000114C9
		public static List<FieldInfo> GetDeclaredFields(Type type)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.GetDeclaredFields: type is null");
				return new List<FieldInfo>();
			}
			return type.GetFields(AccessTools.allDeclared).ToList<FieldInfo>();
		}

		/// <summary>Gets the return type of a method or constructor</summary>
		/// <param name="methodOrConstructor">The method/constructor</param>
		/// <returns>The return type</returns>
		// Token: 0x06000396 RID: 918 RVA: 0x000132F0 File Offset: 0x000114F0
		public static Type GetReturnedType(MethodBase methodOrConstructor)
		{
			if (methodOrConstructor == null)
			{
				FileLog.Debug("AccessTools.GetReturnedType: methodOrConstructor is null");
				return null;
			}
			ConstructorInfo constructor = methodOrConstructor as ConstructorInfo;
			if (constructor != null)
			{
				return typeof(void);
			}
			return ((MethodInfo)methodOrConstructor).ReturnType;
		}

		/// <summary>Given a type, returns the first inner type matching a recursive search by name</summary>
		/// <param name="type">The class/type to start searching at</param>
		/// <param name="name">The name of the inner type (case sensitive)</param>
		/// <returns>The inner type or null if type/name is null or if a type with that name cannot be found</returns>
		// Token: 0x06000397 RID: 919 RVA: 0x0001332C File Offset: 0x0001152C
		public static Type Inner(Type type, string name)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.Inner: type is null");
				return null;
			}
			if (string.IsNullOrEmpty(name))
			{
				FileLog.Debug("AccessTools.Inner: name is null/empty");
				return null;
			}
			return AccessTools.FindIncludingBaseTypes<Type>(type, (Type t) => t.GetNestedType(name, AccessTools.all));
		}

		/// <summary>Given a type, returns the first inner type matching a recursive search with a predicate</summary>
		/// <param name="type">The class/type to start searching at</param>
		/// <param name="predicate">The predicate to search with</param>
		/// <returns>The inner type or null if type/predicate is null or if a type with that name cannot be found</returns>
		// Token: 0x06000398 RID: 920 RVA: 0x00013380 File Offset: 0x00011580
		public static Type FirstInner(Type type, Func<Type, bool> predicate)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.FirstInner: type is null");
				return null;
			}
			if (predicate == null)
			{
				FileLog.Debug("AccessTools.FirstInner: predicate is null");
				return null;
			}
			return type.GetNestedTypes(AccessTools.all).FirstOrDefault((Type subType) => predicate(subType));
		}

		/// <summary>Given a type, returns the first method matching a predicate</summary>
		/// <param name="type">The class/type to start searching at</param>
		/// <param name="predicate">The predicate to search with</param>
		/// <returns>The method or null if type/predicate is null or if a type with that name cannot be found</returns>
		// Token: 0x06000399 RID: 921 RVA: 0x000133DC File Offset: 0x000115DC
		public static MethodInfo FirstMethod(Type type, Func<MethodInfo, bool> predicate)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.FirstMethod: type is null");
				return null;
			}
			if (predicate == null)
			{
				FileLog.Debug("AccessTools.FirstMethod: predicate is null");
				return null;
			}
			return type.GetMethods(AccessTools.allDeclared).FirstOrDefault((MethodInfo method) => predicate(method));
		}

		/// <summary>Given a type, returns the first constructor matching a predicate</summary>
		/// <param name="type">The class/type to start searching at</param>
		/// <param name="predicate">The predicate to search with</param>
		/// <returns>The constructor info or null if type/predicate is null or if a type with that name cannot be found</returns>
		// Token: 0x0600039A RID: 922 RVA: 0x00013438 File Offset: 0x00011638
		public static ConstructorInfo FirstConstructor(Type type, Func<ConstructorInfo, bool> predicate)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.FirstConstructor: type is null");
				return null;
			}
			if (predicate == null)
			{
				FileLog.Debug("AccessTools.FirstConstructor: predicate is null");
				return null;
			}
			return type.GetConstructors(AccessTools.allDeclared).FirstOrDefault((ConstructorInfo constructor) => predicate(constructor));
		}

		/// <summary>Given a type, returns the first property matching a predicate</summary>
		/// <param name="type">The class/type to start searching at</param>
		/// <param name="predicate">The predicate to search with</param>
		/// <returns>The property or null if type/predicate is null or if a type with that name cannot be found</returns>
		// Token: 0x0600039B RID: 923 RVA: 0x00013494 File Offset: 0x00011694
		public static PropertyInfo FirstProperty(Type type, Func<PropertyInfo, bool> predicate)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.FirstProperty: type is null");
				return null;
			}
			if (predicate == null)
			{
				FileLog.Debug("AccessTools.FirstProperty: predicate is null");
				return null;
			}
			return type.GetProperties(AccessTools.allDeclared).FirstOrDefault((PropertyInfo property) => predicate(property));
		}

		/// <summary>Returns an array containing the type of each object in the given array</summary>
		/// <param name="parameters">An array of objects</param>
		/// <returns>An array of types or an empty array if parameters is null (if an object is null, the type for it will be object)</returns>
		// Token: 0x0600039C RID: 924 RVA: 0x000134ED File Offset: 0x000116ED
		public static Type[] GetTypes(object[] parameters)
		{
			if (parameters == null)
			{
				return Array.Empty<Type>();
			}
			return parameters.Select(delegate(object p)
			{
				if (p != null)
				{
					return p.GetType();
				}
				return typeof(object);
			}).ToArray<Type>();
		}

		/// <summary>Creates an array of input parameters for a given method and a given set of potential inputs</summary>
		/// <param name="method">The method/constructor you are planing to call</param>
		/// <param name="inputs"> The possible input parameters in any order</param>
		/// <returns>An object array matching the method signature</returns>
		// Token: 0x0600039D RID: 925 RVA: 0x00013524 File Offset: 0x00011724
		public static object[] ActualParameters(MethodBase method, object[] inputs)
		{
			List<Type> inputTypes = inputs.Select(delegate(object obj)
			{
				if (obj == null)
				{
					return null;
				}
				return obj.GetType();
			}).ToList<Type>();
			return (from p in method.GetParameters()
				select p.ParameterType).Select(delegate(Type pType)
			{
				int index = inputTypes.FindIndex((Type inType) => inType != null && pType.IsAssignableFrom(inType));
				if (index >= 0)
				{
					return inputs[index];
				}
				return AccessTools.GetDefaultValue(pType);
			}).ToArray<object>();
		}

		/// <summary>Creates a field reference delegate for an instance field of a class</summary>
		/// <typeparam name="T">The class that defines the instance field, or derived class of this type</typeparam>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="fieldName">The name of the field</param>
		/// <returns>A readable/assignable <see cref="T:HarmonyLib.AccessTools.FieldRef`2" /> delegate</returns>
		/// <remarks>
		///     <para>
		///  For backwards compatibility, there is no class constraint on <typeparamref name="T" />.
		///  Instead, the non-value-type check is done at runtime within the method.
		///  </para>
		/// </remarks>
		// Token: 0x0600039E RID: 926 RVA: 0x000135B4 File Offset: 0x000117B4
		public static AccessTools.FieldRef<T, F> FieldRefAccess<T, F>(string fieldName)
		{
			if (fieldName == null)
			{
				throw new ArgumentNullException("fieldName");
			}
			AccessTools.FieldRef<T, F> result;
			try
			{
				Type delegateInstanceType = typeof(T);
				if (delegateInstanceType.IsValueType)
				{
					throw new ArgumentException("T (FieldRefAccess instance type) must not be a value type");
				}
				result = Tools.FieldRefAccess<T, F>(Tools.GetInstanceField(delegateInstanceType, fieldName), false);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 3);
				defaultInterpolatedStringHandler.AppendLiteral("FieldRefAccess<");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(T));
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(F));
				defaultInterpolatedStringHandler.AppendLiteral("> for ");
				defaultInterpolatedStringHandler.AppendFormatted(fieldName);
				defaultInterpolatedStringHandler.AppendLiteral(" caused an exception");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
			return result;
		}

		/// <summary>Creates an instance field reference for a specific instance of a class</summary>
		/// <typeparam name="T">The class that defines the instance field, or derived class of this type</typeparam>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="instance">The instance</param>
		/// <param name="fieldName">The name of the field</param>
		/// <returns>A readable/assignable reference to the field</returns>
		/// <remarks>
		///     <para>
		///  This method is meant for one-off access to a field's value for a single instance.
		///  If you need to access a field's value for potentially multiple instances, use <see cref="M:HarmonyLib.AccessTools.FieldRefAccess``2(System.String)" /> instead.
		///  <c>FieldRefAccess&lt;T, F&gt;(instance, fieldName)</c> is functionally equivalent to <c>FieldRefAccess&lt;T, F&gt;(fieldName)(instance)</c>.
		///  </para>
		///     <para>
		///  For backwards compatibility, there is no class constraint on <typeparamref name="T" />.
		///  Instead, the non-value-type check is done at runtime within the method.
		///  </para>
		/// </remarks>
		// Token: 0x0600039F RID: 927 RVA: 0x00013684 File Offset: 0x00011884
		public static ref F FieldRefAccess<T, F>(T instance, string fieldName)
		{
			if (instance == null)
			{
				throw new ArgumentNullException("instance");
			}
			if (fieldName == null)
			{
				throw new ArgumentNullException("fieldName");
			}
			F result;
			try
			{
				Type delegateInstanceType = typeof(T);
				if (delegateInstanceType.IsValueType)
				{
					throw new ArgumentException("T (FieldRefAccess instance type) must not be a value type");
				}
				result = Tools.FieldRefAccess<T, F>(Tools.GetInstanceField(delegateInstanceType, fieldName), false)(instance);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 4);
				defaultInterpolatedStringHandler.AppendLiteral("FieldRefAccess<");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(T));
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(F));
				defaultInterpolatedStringHandler.AppendLiteral("> for ");
				defaultInterpolatedStringHandler.AppendFormatted<T>(instance);
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted(fieldName);
				defaultInterpolatedStringHandler.AppendLiteral(" caused an exception");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
			return ref result;
		}

		/// <summary>Creates a field reference delegate for an instance field of a class or static field (NOT an instance field of a struct)</summary>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="type">
		///  The type that defines the field, or derived class of this type; must not be a struct type unless the field is static
		///  </param>
		/// <param name="fieldName">The name of the field</param>
		/// <returns>
		///  A readable/assignable <see cref="T:HarmonyLib.AccessTools.FieldRef`2" /> delegate with <c>T=object</c>
		///  (for static fields, the <c>instance</c> delegate parameter is ignored)
		///  </returns>
		/// <remarks>
		///     <para>
		///  This method is meant for cases where the given type is only known at runtime and thus can't be used as a type parameter <c>T</c>
		///  in e.g. <see cref="M:HarmonyLib.AccessTools.FieldRefAccess``2(System.String)" />.
		///  </para>
		///     <para>
		///  This method supports static fields, even those defined in structs, for legacy reasons.
		///  Consider using <see cref="M:HarmonyLib.AccessTools.StaticFieldRefAccess``1(System.Type,System.String)" /> (and other overloads) instead for static fields.
		///  </para>
		/// </remarks>
		// Token: 0x060003A0 RID: 928 RVA: 0x00013784 File Offset: 0x00011984
		public static AccessTools.FieldRef<object, F> FieldRefAccess<F>(Type type, string fieldName)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (fieldName == null)
			{
				throw new ArgumentNullException("fieldName");
			}
			AccessTools.FieldRef<object, F> result;
			try
			{
				FieldInfo fieldInfo = AccessTools.Field(type, fieldName);
				if (fieldInfo == null)
				{
					throw new MissingFieldException(type.Name, fieldName);
				}
				if (!fieldInfo.IsStatic)
				{
					Type declaringType = fieldInfo.DeclaringType;
					if (declaringType != null && declaringType.IsValueType)
					{
						throw new ArgumentException("Either FieldDeclaringType must be a class or field must be static");
					}
				}
				result = Tools.FieldRefAccess<object, F>(fieldInfo, true);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 3);
				defaultInterpolatedStringHandler.AppendLiteral("FieldRefAccess<");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(F));
				defaultInterpolatedStringHandler.AppendLiteral("> for ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted(fieldName);
				defaultInterpolatedStringHandler.AppendLiteral(" caused an exception");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
			return result;
		}

		/// <summary>Creates a field reference delegate for an instance field of a class or static field (NOT an instance field of a struct)</summary>
		/// <typeparam name="F"> type of the field</typeparam>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A readable/assignable <see cref="T:HarmonyLib.AccessTools.FieldRef`2" /> delegate with <c>T=object</c></returns>
		// Token: 0x060003A1 RID: 929 RVA: 0x00013870 File Offset: 0x00011A70
		public static AccessTools.FieldRef<object, F> FieldRefAccess<F>(string typeColonName)
		{
			Tools.TypeAndName info = Tools.TypColonName(typeColonName);
			return AccessTools.FieldRefAccess<F>(info.type, info.name);
		}

		/// <summary>Creates a field reference delegate for an instance field of a class or static field (NOT an instance field of a struct)</summary>
		/// <typeparam name="T">
		///  An arbitrary type if the field is static; otherwise the class that defines the field, or a parent class (including <see cref="T:System.Object" />),
		///  implemented interface, or derived class of this type ("<c>instanceOfT is FieldDeclaringType</c>" must be possible)
		///  </typeparam>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="fieldInfo">The field</param>
		/// <returns>A readable/assignable <see cref="T:HarmonyLib.AccessTools.FieldRef`2" /> delegate</returns>
		/// <remarks>
		///     <para>
		///  This method is meant for cases where the field has already been obtained, avoiding the field searching cost in
		///  e.g. <see cref="M:HarmonyLib.AccessTools.FieldRefAccess``2(System.String)" />.
		///  </para>
		///     <para>
		///  This method supports static fields, even those defined in structs, for legacy reasons.
		///  For such static fields, <typeparamref name="T" /> is effectively ignored.
		///  Consider using <see cref="M:HarmonyLib.AccessTools.StaticFieldRefAccess``2(System.Reflection.FieldInfo)" /> (and other overloads) instead for static fields.
		///  </para>
		///     <para>
		///  For backwards compatibility, there is no class constraint on <typeparamref name="T" />.
		///  Instead, the non-value-type check is done at runtime within the method.
		///  </para>
		/// </remarks>
		// Token: 0x060003A2 RID: 930 RVA: 0x00013898 File Offset: 0x00011A98
		public static AccessTools.FieldRef<T, F> FieldRefAccess<T, F>(FieldInfo fieldInfo)
		{
			if (fieldInfo == null)
			{
				throw new ArgumentNullException("fieldInfo");
			}
			AccessTools.FieldRef<T, F> result;
			try
			{
				Type delegateInstanceType = typeof(T);
				if (delegateInstanceType.IsValueType)
				{
					throw new ArgumentException("T (FieldRefAccess instance type) must not be a value type");
				}
				bool needCastclass = false;
				if (!fieldInfo.IsStatic)
				{
					Type declaringType = fieldInfo.DeclaringType;
					if (declaringType != null)
					{
						if (declaringType.IsValueType)
						{
							throw new ArgumentException("Either FieldDeclaringType must be a class or field must be static");
						}
						needCastclass = Tools.FieldRefNeedsClasscast(delegateInstanceType, declaringType);
					}
				}
				result = Tools.FieldRefAccess<T, F>(fieldInfo, needCastclass);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 3);
				defaultInterpolatedStringHandler.AppendLiteral("FieldRefAccess<");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(T));
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(F));
				defaultInterpolatedStringHandler.AppendLiteral("> for ");
				defaultInterpolatedStringHandler.AppendFormatted<FieldInfo>(fieldInfo);
				defaultInterpolatedStringHandler.AppendLiteral(" caused an exception");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
			return result;
		}

		/// <summary>Creates a field reference for an instance field of a class</summary>
		/// <typeparam name="T">
		///  The type that defines the field; or a parent class (including <see cref="T:System.Object" />), implemented interface, or derived class of this type
		///  ("<c>instanceOfT is FieldDeclaringType</c>" must be possible)
		///  </typeparam>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="instance">The instance</param>
		/// <param name="fieldInfo">The field</param>
		/// <returns>A readable/assignable reference to the field</returns>
		/// <remarks>
		///     <para>
		///  This method is meant for one-off access to a field's value for a single instance and where the field has already been obtained.
		///  If you need to access a field's value for potentially multiple instances, use <see cref="M:HarmonyLib.AccessTools.FieldRefAccess``2(System.Reflection.FieldInfo)" /> instead.
		///  <c>FieldRefAccess&lt;T, F&gt;(instance, fieldInfo)</c> is functionally equivalent to <c>FieldRefAccess&lt;T, F&gt;(fieldInfo)(instance)</c>.
		///  </para>
		///     <para>
		///  For backwards compatibility, there is no class constraint on <typeparamref name="T" />.
		///  Instead, the non-value-type check is done at runtime within the method.
		///  </para>
		/// </remarks>
		// Token: 0x060003A3 RID: 931 RVA: 0x00013994 File Offset: 0x00011B94
		public static ref F FieldRefAccess<T, F>(T instance, FieldInfo fieldInfo)
		{
			if (instance == null)
			{
				throw new ArgumentNullException("instance");
			}
			if (fieldInfo == null)
			{
				throw new ArgumentNullException("fieldInfo");
			}
			F result;
			try
			{
				Type delegateInstanceType = typeof(T);
				if (delegateInstanceType.IsValueType)
				{
					throw new ArgumentException("T (FieldRefAccess instance type) must not be a value type");
				}
				if (fieldInfo.IsStatic)
				{
					throw new ArgumentException("Field must not be static");
				}
				bool needCastclass = false;
				Type declaringType = fieldInfo.DeclaringType;
				if (declaringType != null)
				{
					if (declaringType.IsValueType)
					{
						throw new ArgumentException("FieldDeclaringType must be a class");
					}
					needCastclass = Tools.FieldRefNeedsClasscast(delegateInstanceType, declaringType);
				}
				result = Tools.FieldRefAccess<T, F>(fieldInfo, needCastclass)(instance);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 4);
				defaultInterpolatedStringHandler.AppendLiteral("FieldRefAccess<");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(T));
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(F));
				defaultInterpolatedStringHandler.AppendLiteral("> for ");
				defaultInterpolatedStringHandler.AppendFormatted<T>(instance);
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted<FieldInfo>(fieldInfo);
				defaultInterpolatedStringHandler.AppendLiteral(" caused an exception");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
			return ref result;
		}

		/// <summary>Creates a field reference delegate for an instance field of a struct</summary>
		/// <typeparam name="T">The struct that defines the instance field</typeparam>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="fieldName">The name of the field</param>
		/// <returns>A readable/assignable <see cref="T:HarmonyLib.AccessTools.StructFieldRef`2" /> delegate</returns>
		// Token: 0x060003A4 RID: 932 RVA: 0x00013AC8 File Offset: 0x00011CC8
		public static AccessTools.StructFieldRef<T, F> StructFieldRefAccess<T, F>(string fieldName) where T : struct
		{
			if (fieldName == null)
			{
				throw new ArgumentNullException("fieldName");
			}
			AccessTools.StructFieldRef<T, F> result;
			try
			{
				result = Tools.StructFieldRefAccess<T, F>(Tools.GetInstanceField(typeof(T), fieldName));
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(49, 3);
				defaultInterpolatedStringHandler.AppendLiteral("StructFieldRefAccess<");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(T));
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(F));
				defaultInterpolatedStringHandler.AppendLiteral("> for ");
				defaultInterpolatedStringHandler.AppendFormatted(fieldName);
				defaultInterpolatedStringHandler.AppendLiteral(" caused an exception");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
			return result;
		}

		/// <summary>Creates an instance field reference for a specific instance of a struct</summary>
		/// <typeparam name="T">The struct that defines the instance field</typeparam>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="instance">The instance</param>
		/// <param name="fieldName">The name of the field</param>
		/// <returns>A readable/assignable reference to the field</returns>
		/// <remarks>
		///     <para>
		///  This method is meant for one-off access to a field's value for a single instance.
		///  If you need to access a field's value for potentially multiple instances, use <see cref="M:HarmonyLib.AccessTools.StructFieldRefAccess``2(System.String)" /> instead.
		///  <c>StructFieldRefAccess&lt;T, F&gt;(ref instance, fieldName)</c> is functionally equivalent to <c>StructFieldRefAccess&lt;T, F&gt;(fieldName)(ref instance)</c>.
		///  </para>
		/// </remarks>
		// Token: 0x060003A5 RID: 933 RVA: 0x00013B80 File Offset: 0x00011D80
		public static ref F StructFieldRefAccess<T, F>(ref T instance, string fieldName) where T : struct
		{
			if (fieldName == null)
			{
				throw new ArgumentNullException("fieldName");
			}
			F result;
			try
			{
				result = Tools.StructFieldRefAccess<T, F>(Tools.GetInstanceField(typeof(T), fieldName))(ref instance);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 4);
				defaultInterpolatedStringHandler.AppendLiteral("StructFieldRefAccess<");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(T));
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(F));
				defaultInterpolatedStringHandler.AppendLiteral("> for ");
				defaultInterpolatedStringHandler.AppendFormatted<T>(instance);
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted(fieldName);
				defaultInterpolatedStringHandler.AppendLiteral(" caused an exception");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
			return ref result;
		}

		/// <summary>Creates a field reference delegate for an instance field of a struct</summary>
		/// <typeparam name="T">The struct that defines the instance field</typeparam>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="fieldInfo">The field</param>
		/// <returns>A readable/assignable <see cref="T:HarmonyLib.AccessTools.StructFieldRef`2" /> delegate</returns>
		/// <remarks>
		///     <para>
		///  This method is meant for cases where the field has already been obtained, avoiding the field searching cost in
		///  e.g. <see cref="M:HarmonyLib.AccessTools.StructFieldRefAccess``2(System.String)" />.
		///  </para>
		/// </remarks>
		// Token: 0x060003A6 RID: 934 RVA: 0x00013C5C File Offset: 0x00011E5C
		public static AccessTools.StructFieldRef<T, F> StructFieldRefAccess<T, F>(FieldInfo fieldInfo) where T : struct
		{
			if (fieldInfo == null)
			{
				throw new ArgumentNullException("fieldInfo");
			}
			AccessTools.StructFieldRef<T, F> result;
			try
			{
				Tools.ValidateStructField<T, F>(fieldInfo);
				result = Tools.StructFieldRefAccess<T, F>(fieldInfo);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(49, 3);
				defaultInterpolatedStringHandler.AppendLiteral("StructFieldRefAccess<");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(T));
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(F));
				defaultInterpolatedStringHandler.AppendLiteral("> for ");
				defaultInterpolatedStringHandler.AppendFormatted<FieldInfo>(fieldInfo);
				defaultInterpolatedStringHandler.AppendLiteral(" caused an exception");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
			return result;
		}

		/// <summary>Creates a field reference for an instance field of a struct</summary>
		/// <typeparam name="T">The struct that defines the instance field</typeparam>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="instance">The instance</param>
		/// <param name="fieldInfo">The field</param>
		/// <returns>A readable/assignable reference to the field</returns>
		/// <remarks>
		///     <para>
		///  This method is meant for one-off access to a field's value for a single instance and where the field has already been obtained.
		///  If you need to access a field's value for potentially multiple instances, use <see cref="M:HarmonyLib.AccessTools.StructFieldRefAccess``2(System.Reflection.FieldInfo)" /> instead.
		///  <c>StructFieldRefAccess&lt;T, F&gt;(ref instance, fieldInfo)</c> is functionally equivalent to <c>StructFieldRefAccess&lt;T, F&gt;(fieldInfo)(ref instance)</c>.
		///  </para>
		/// </remarks>
		// Token: 0x060003A7 RID: 935 RVA: 0x00013D0C File Offset: 0x00011F0C
		public static ref F StructFieldRefAccess<T, F>(ref T instance, FieldInfo fieldInfo) where T : struct
		{
			if (fieldInfo == null)
			{
				throw new ArgumentNullException("fieldInfo");
			}
			F result;
			try
			{
				Tools.ValidateStructField<T, F>(fieldInfo);
				result = Tools.StructFieldRefAccess<T, F>(fieldInfo)(ref instance);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 4);
				defaultInterpolatedStringHandler.AppendLiteral("StructFieldRefAccess<");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(T));
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(F));
				defaultInterpolatedStringHandler.AppendLiteral("> for ");
				defaultInterpolatedStringHandler.AppendFormatted<T>(instance);
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted<FieldInfo>(fieldInfo);
				defaultInterpolatedStringHandler.AppendLiteral(" caused an exception");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
			return ref result;
		}

		/// <summary>Creates a static field reference</summary>
		/// <typeparam name="T">The type (can be class or struct) the field is defined in</typeparam>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="fieldName">The name of the field</param>
		/// <returns>A readable/assignable reference to the field</returns>
		// Token: 0x060003A8 RID: 936 RVA: 0x00013DE0 File Offset: 0x00011FE0
		public static ref F StaticFieldRefAccess<T, F>(string fieldName)
		{
			return AccessTools.StaticFieldRefAccess<F>(typeof(T), fieldName);
		}

		/// <summary>Creates a static field reference</summary>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="type">The type (can be class or struct) the field is defined in</param>
		/// <param name="fieldName">The name of the field</param>
		/// <returns>A readable/assignable reference to the field</returns>
		// Token: 0x060003A9 RID: 937 RVA: 0x00013DF4 File Offset: 0x00011FF4
		public static ref F StaticFieldRefAccess<F>(Type type, string fieldName)
		{
			F result;
			try
			{
				FieldInfo fieldInfo = AccessTools.Field(type, fieldName);
				if (fieldInfo == null)
				{
					throw new MissingFieldException(type.Name, fieldName);
				}
				result = Tools.StaticFieldRefAccess<F>(fieldInfo)();
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(49, 3);
				defaultInterpolatedStringHandler.AppendLiteral("StaticFieldRefAccess<");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(F));
				defaultInterpolatedStringHandler.AppendLiteral("> for ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted(fieldName);
				defaultInterpolatedStringHandler.AppendLiteral(" caused an exception");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
			return ref result;
		}

		/// <summary>Creates a static field reference</summary>
		/// <typeparam name="F">The type of the field</typeparam>
		/// <param name="typeColonName">The member in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <returns>A readable/assignable reference to the field</returns>
		// Token: 0x060003AA RID: 938 RVA: 0x00013EA4 File Offset: 0x000120A4
		public static ref F StaticFieldRefAccess<F>(string typeColonName)
		{
			Tools.TypeAndName info = Tools.TypColonName(typeColonName);
			return AccessTools.StaticFieldRefAccess<F>(info.type, info.name);
		}

		/// <summary>Creates a static field reference</summary>
		/// <typeparam name="T">An arbitrary type (by convention, the type the field is defined in)</typeparam>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="fieldInfo">The field</param>
		/// <returns>A readable/assignable reference to the field</returns>
		/// <remarks>
		///  The type parameter <typeparamref name="T" /> is only used in exception messaging and to distinguish between this method overload
		///  and the <see cref="M:HarmonyLib.AccessTools.StaticFieldRefAccess``1(System.Reflection.FieldInfo)" /> overload (which returns a <see cref="T:HarmonyLib.AccessTools.FieldRef`1" /> rather than a reference).
		///  </remarks>
		// Token: 0x060003AB RID: 939 RVA: 0x00013ECC File Offset: 0x000120CC
		public static ref F StaticFieldRefAccess<T, F>(FieldInfo fieldInfo)
		{
			if (fieldInfo == null)
			{
				throw new ArgumentNullException("fieldInfo");
			}
			F result;
			try
			{
				result = Tools.StaticFieldRefAccess<F>(fieldInfo)();
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(49, 3);
				defaultInterpolatedStringHandler.AppendLiteral("StaticFieldRefAccess<");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(T));
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(F));
				defaultInterpolatedStringHandler.AppendLiteral("> for ");
				defaultInterpolatedStringHandler.AppendFormatted<FieldInfo>(fieldInfo);
				defaultInterpolatedStringHandler.AppendLiteral(" caused an exception");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
			return ref result;
		}

		/// <summary>Creates a static field reference delegate</summary>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="fieldInfo">The field</param>
		/// <returns>A readable/assignable <see cref="T:HarmonyLib.AccessTools.FieldRef`1" /> delegate</returns>
		// Token: 0x060003AC RID: 940 RVA: 0x00013F7C File Offset: 0x0001217C
		public static AccessTools.FieldRef<F> StaticFieldRefAccess<F>(FieldInfo fieldInfo)
		{
			if (fieldInfo == null)
			{
				throw new ArgumentNullException("fieldInfo");
			}
			AccessTools.FieldRef<F> result;
			try
			{
				result = Tools.StaticFieldRefAccess<F>(fieldInfo);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(47, 2);
				defaultInterpolatedStringHandler.AppendLiteral("StaticFieldRefAccess<");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(F));
				defaultInterpolatedStringHandler.AppendLiteral("> for ");
				defaultInterpolatedStringHandler.AppendFormatted<FieldInfo>(fieldInfo);
				defaultInterpolatedStringHandler.AppendLiteral(" caused an exception");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
			return result;
		}

		// Token: 0x060003AD RID: 941 RVA: 0x00014008 File Offset: 0x00012208
		[Obsolete("This overload only exists for runtime backwards compatibility and will be removed in Harmony 3. Use MethodDelegate(MethodInfo, object, bool, Type[]) instead")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static DelegateType MethodDelegate<DelegateType>(MethodInfo method, object instance, bool virtualCall) where DelegateType : Delegate
		{
			return AccessTools.MethodDelegate<DelegateType>(method, instance, virtualCall, null);
		}

		/// <summary>Creates a delegate to a given method</summary>
		/// <typeparam name="DelegateType">The delegate Type</typeparam>
		/// <param name="method">The method to create a delegate from.</param>
		/// <param name="instance">
		///  Only applies for instance methods. If <c>null</c> (default), returned delegate is an open (a.k.a. unbound) instance delegate
		///  where an instance is supplied as the first argument to the delegate invocation; else, delegate is a closed (a.k.a. bound)
		///  instance delegate where the delegate invocation always applies to the given <paramref name="instance" />.
		///  </param>
		/// <param name="virtualCall">
		///  Only applies for instance methods. If <c>true</c> (default) and <paramref name="method" /> is virtual, invocation of the delegate
		///  calls the instance method virtually (the instance type's most-derived/overriden implementation of the method is called);
		///  else, invocation of the delegate calls the exact specified <paramref name="method" /> (this is useful for calling base class methods)
		///  Note: if <c>false</c> and <paramref name="method" /> is an interface method, an ArgumentException is thrown.
		///  </param>
		/// <param name="delegateArgs">
		///  Only applies for instance methods, and if argument <paramref name="instance" /> is null.
		///  This argument only matters if the target <paramref name="method" /> signature contains a value type (such as struct or primitive types),
		///  and your <typeparamref name="DelegateType" /> argument is replaced by a non-value type
		///  (usually <c>object</c>) instead of using said value type.
		///  Use this if the generic arguments of <typeparamref name="DelegateType" /> doesn't represent the delegate's
		///  arguments, and calling this function fails
		///  <returns>A delegate of given <typeparamref name="DelegateType" /> to given <paramref name="method" /></returns></param>
		/// <remarks>
		///     <para>
		///  Delegate invocation is more performant and more convenient to use than <see cref="M:System.Reflection.MethodBase.Invoke(System.Object,System.Object[])" />
		///  at a one-time setup cost.
		///  </para>
		///     <para>
		///  Works for both type of static and instance methods, both open and closed (a.k.a. unbound and bound) instance methods,
		///  and both class and struct methods.
		///  </para>
		/// </remarks>
		// Token: 0x060003AE RID: 942 RVA: 0x00014014 File Offset: 0x00012214
		public static DelegateType MethodDelegate<DelegateType>(MethodInfo method, object instance = null, bool virtualCall = true, Type[] delegateArgs = null) where DelegateType : Delegate
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			Type delegateType = typeof(DelegateType);
			if (method.IsStatic)
			{
				return (DelegateType)((object)Delegate.CreateDelegate(delegateType, method));
			}
			Type declaringType = method.DeclaringType;
			if (declaringType != null && declaringType.IsInterface && !virtualCall)
			{
				throw new ArgumentException("Interface methods must be called virtually");
			}
			if (instance == null)
			{
				ParameterInfo[] delegateParameters = delegateType.GetMethod("Invoke").GetParameters();
				if (delegateParameters.Length == 0)
				{
					Delegate.CreateDelegate(typeof(DelegateType), method);
					throw new ArgumentException("Invalid delegate type");
				}
				Type delegateInstanceType = delegateParameters[0].ParameterType;
				if (declaringType != null && declaringType.IsInterface && delegateInstanceType.IsValueType)
				{
					InterfaceMapping interfaceMapping = delegateInstanceType.GetInterfaceMap(declaringType);
					method = interfaceMapping.TargetMethods[Array.IndexOf<MethodInfo>(interfaceMapping.InterfaceMethods, method)];
					declaringType = delegateInstanceType;
				}
				if (declaringType != null && virtualCall)
				{
					if (declaringType.IsInterface)
					{
						return (DelegateType)((object)Delegate.CreateDelegate(delegateType, method));
					}
					if (delegateInstanceType.IsInterface)
					{
						InterfaceMapping interfaceMapping2 = declaringType.GetInterfaceMap(delegateInstanceType);
						MethodInfo interfaceMethod = interfaceMapping2.InterfaceMethods[Array.IndexOf<MethodInfo>(interfaceMapping2.TargetMethods, method)];
						return (DelegateType)((object)Delegate.CreateDelegate(delegateType, interfaceMethod));
					}
					if (!declaringType.IsValueType)
					{
						return (DelegateType)((object)Delegate.CreateDelegate(delegateType, method.GetBaseDefinition()));
					}
				}
				ParameterInfo[] parameters = method.GetParameters();
				int numParameters = parameters.Length;
				Type[] parameterTypes = new Type[numParameters + 1];
				parameterTypes[0] = declaringType;
				for (int i = 0; i < numParameters; i++)
				{
					parameterTypes[i + 1] = parameters[i].ParameterType;
				}
				Type[] delegateArgsResolved = delegateArgs ?? delegateType.GetGenericArguments();
				Type[] dynMethodReturn = ((delegateArgsResolved.Length < parameterTypes.Length) ? parameterTypes : delegateArgsResolved);
				DynamicMethodDefinition dmd = new DynamicMethodDefinition("OpenInstanceDelegate_" + method.Name, method.ReturnType, dynMethodReturn);
				ILGenerator ilGen = dmd.GetILGenerator();
				if (declaringType != null && declaringType.IsValueType && delegateArgsResolved.Length != 0 && !delegateArgsResolved[0].IsByRef)
				{
					ilGen.Emit(OpCodes.Ldarga_S, 0);
				}
				else
				{
					ilGen.Emit(OpCodes.Ldarg_0);
				}
				for (int j = 1; j < parameterTypes.Length; j++)
				{
					ilGen.Emit(OpCodes.Ldarg, j);
					if (parameterTypes[j].IsValueType && j < delegateArgsResolved.Length && !delegateArgsResolved[j].IsValueType)
					{
						ilGen.Emit(OpCodes.Unbox_Any, parameterTypes[j]);
					}
				}
				ilGen.Emit(OpCodes.Call, method);
				ilGen.Emit(OpCodes.Ret);
				return (DelegateType)((object)dmd.Generate().CreateDelegate(delegateType));
			}
			else
			{
				if (virtualCall)
				{
					return (DelegateType)((object)Delegate.CreateDelegate(delegateType, instance, method.GetBaseDefinition()));
				}
				if (declaringType != null && !declaringType.IsInstanceOfType(instance))
				{
					Delegate.CreateDelegate(typeof(DelegateType), instance, method);
					throw new ArgumentException("Invalid delegate type");
				}
				if (AccessTools.IsMonoRuntime)
				{
					DynamicMethodDefinition dmd2 = new DynamicMethodDefinition("LdftnDelegate_" + method.Name, delegateType, new Type[] { typeof(object) });
					ILGenerator ilGen2 = dmd2.GetILGenerator();
					ilGen2.Emit(OpCodes.Ldarg_0);
					ilGen2.Emit(OpCodes.Ldftn, method);
					ilGen2.Emit(OpCodes.Newobj, delegateType.GetConstructor(new Type[]
					{
						typeof(object),
						typeof(IntPtr)
					}));
					ilGen2.Emit(OpCodes.Ret);
					return (DelegateType)((object)dmd2.Generate().Invoke(null, new object[] { instance }));
				}
				return (DelegateType)((object)Activator.CreateInstance(delegateType, new object[]
				{
					instance,
					method.MethodHandle.GetFunctionPointer()
				}));
			}
		}

		// Token: 0x060003AF RID: 943 RVA: 0x000143C4 File Offset: 0x000125C4
		[Obsolete("This overload only exists for runtime backwards compatibility and will be removed in Harmony 3. Use MethodDelegate(string, object, bool, Type[]) instead")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static DelegateType MethodDelegate<DelegateType>(string typeColonName, object instance, bool virtualCall) where DelegateType : Delegate
		{
			return AccessTools.MethodDelegate<DelegateType>(typeColonName, instance, virtualCall, null);
		}

		/// <summary>Creates a delegate to a given method</summary>
		/// <typeparam name="DelegateType">The delegate Type</typeparam>
		/// <param name="typeColonName">The method in the form <c>TypeFullName:MemberName</c>, where TypeFullName matches the form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <param name="instance">
		///  Only applies for instance methods. If <c>null</c> (default), returned delegate is an open (a.k.a. unbound) instance delegate
		///  where an instance is supplied as the first argument to the delegate invocation; else, delegate is a closed (a.k.a. bound)
		///  instance delegate where the delegate invocation always applies to the given <paramref name="instance" />.
		///  </param>
		/// <param name="virtualCall">
		///  Only applies for instance methods. If <c>true</c> (default) and <paramref name="typeColonName" /> is virtual, invocation of the delegate
		///  calls the instance method virtually (the instance type's most-derived/overriden implementation of the method is called);
		///  else, invocation of the delegate calls the exact specified <paramref name="typeColonName" /> (this is useful for calling base class methods)
		///  Note: if <c>false</c> and <paramref name="typeColonName" /> is an interface method, an ArgumentException is thrown.
		///  </param>
		/// <param name="delegateArgs">
		///  Only applies for instance methods, and if argument <paramref name="instance" /> is null.
		///  This argument only matters if the target <paramref name="typeColonName" /> signature contains a value type (such as struct or primitive types),
		///  and your <typeparamref name="DelegateType" /> argument is replaced by a non-value type
		///  (usually <c>object</c>) instead of using said value type.
		///  Use this if the generic arguments of <typeparamref name="DelegateType" /> doesn't represent the delegate's
		///  arguments, and calling this function fails
		///  <returns>A delegate of given <typeparamref name="DelegateType" /> to given <paramref name="typeColonName" /></returns></param>
		/// <remarks>
		///     <para>
		///  Delegate invocation is more performant and more convenient to use than <see cref="M:System.Reflection.MethodBase.Invoke(System.Object,System.Object[])" />
		///  at a one-time setup cost.
		///  </para>
		///     <para>
		///  Works for both type of static and instance methods, both open and closed (a.k.a. unbound and bound) instance methods,
		///  and both class and struct methods.
		///  </para>
		/// </remarks>
		// Token: 0x060003B0 RID: 944 RVA: 0x000143CF File Offset: 0x000125CF
		public static DelegateType MethodDelegate<DelegateType>(string typeColonName, object instance = null, bool virtualCall = true, Type[] delegateArgs = null) where DelegateType : Delegate
		{
			return AccessTools.MethodDelegate<DelegateType>(AccessTools.DeclaredMethod(typeColonName, null, null), instance, virtualCall, delegateArgs);
		}

		/// <summary>Creates a delegate for a given delegate definition, attributed with [<see cref="T:HarmonyLib.HarmonyDelegate" />]</summary>
		/// <typeparam name="DelegateType">The delegate Type, attributed with [<see cref="T:HarmonyLib.HarmonyDelegate" />]</typeparam>
		/// <param name="instance">
		///  Only applies for instance methods. If <c>null</c> (default), returned delegate is an open (a.k.a. unbound) instance delegate
		///  where an instance is supplied as the first argument to the delegate invocation; else, delegate is a closed (a.k.a. bound)
		///  instance delegate where the delegate invocation always applies to the given <paramref name="instance" />.
		///  </param>
		/// <returns>A delegate of given <typeparamref name="DelegateType" /> to the method specified via [<see cref="T:HarmonyLib.HarmonyDelegate" />]
		///  attributes on <typeparamref name="DelegateType" /></returns>
		/// <remarks>
		///  This calls <see cref="M:HarmonyLib.AccessTools.MethodDelegate``1(System.Reflection.MethodInfo,System.Object,System.Boolean,System.Type[])" /> with the <c>method</c> and <c>virtualCall</c> arguments
		///  determined from the [<see cref="T:HarmonyLib.HarmonyDelegate" />] attributes on <typeparamref name="DelegateType" />,
		///  and the given <paramref name="instance" /> (for closed instance delegates).
		///  </remarks>
		// Token: 0x060003B1 RID: 945 RVA: 0x000143E4 File Offset: 0x000125E4
		public static DelegateType HarmonyDelegate<DelegateType>(object instance = null) where DelegateType : Delegate
		{
			HarmonyMethod harmonyMethod = HarmonyMethodExtensions.GetMergedFromType(typeof(DelegateType));
			HarmonyMethod harmonyMethod2 = harmonyMethod;
			MethodType value = harmonyMethod2.methodType.GetValueOrDefault();
			if (harmonyMethod2.methodType == null)
			{
				value = MethodType.Normal;
				harmonyMethod2.methodType = new MethodType?(value);
			}
			MethodInfo method = harmonyMethod.GetOriginalMethod() as MethodInfo;
			if (method == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Delegate ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(typeof(DelegateType));
				defaultInterpolatedStringHandler.AppendLiteral(" has no defined original method");
				throw new NullReferenceException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return AccessTools.MethodDelegate<DelegateType>(method, instance, !harmonyMethod.nonVirtualDelegate, null);
		}

		/// <summary>Returns who called the current method</summary>
		/// <returns>The calling method/constructor (excluding the caller)</returns>
		// Token: 0x060003B2 RID: 946 RVA: 0x0001448C File Offset: 0x0001268C
		public static MethodBase GetOutsideCaller()
		{
			StackTrace trace = new StackTrace(true);
			foreach (StackFrame frame in trace.GetFrames())
			{
				MethodBase method = frame.GetMethod();
				Type declaringType = method.DeclaringType;
				if (((declaringType != null) ? declaringType.Namespace : null) != typeof(Harmony).Namespace)
				{
					return method;
				}
			}
			throw new Exception("Unexpected end of stack trace");
		}

		/// <summary>Rethrows an exception while preserving its stack trace (throw statement typically clobbers existing stack traces)</summary>
		/// <param name="exception">The exception to rethrow</param>
		// Token: 0x060003B3 RID: 947 RVA: 0x000144F7 File Offset: 0x000126F7
		public static void RethrowException(Exception exception)
		{
			ExceptionDispatchInfo.Capture(exception).Throw();
			throw exception;
		}

		/// <summary>True if the current runtime is based on Mono, false otherwise (.NET)</summary>
		// Token: 0x17000023 RID: 35
		// (get) Token: 0x060003B4 RID: 948 RVA: 0x00014505 File Offset: 0x00012705
		public static bool IsMonoRuntime { get; } = Type.GetType("Mono.Runtime") != null;

		/// <summary>True if the current runtime is .NET Framework, false otherwise (.NET Core or Mono, although latter isn't guaranteed)</summary>
		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060003B5 RID: 949 RVA: 0x0001450C File Offset: 0x0001270C
		public static bool IsNetFrameworkRuntime { get; }

		/// <summary>True if the current runtime is .NET Core, false otherwise (Mono or .NET Framework)</summary>
		// Token: 0x17000025 RID: 37
		// (get) Token: 0x060003B6 RID: 950 RVA: 0x00014513 File Offset: 0x00012713
		public static bool IsNetCoreRuntime { get; }

		/// <summary>Throws a missing member runtime exception</summary>
		/// <param name="type">The type that is involved</param>
		/// <param name="names">A list of names</param>
		// Token: 0x060003B7 RID: 951 RVA: 0x0001451C File Offset: 0x0001271C
		public static void ThrowMissingMemberException(Type type, params string[] names)
		{
			string fields = string.Join(",", AccessTools.GetFieldNames(type).ToArray());
			string properties = string.Join(",", AccessTools.GetPropertyNames(type).ToArray());
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 3);
			defaultInterpolatedStringHandler.AppendFormatted(string.Join(",", names));
			defaultInterpolatedStringHandler.AppendLiteral("; available fields: ");
			defaultInterpolatedStringHandler.AppendFormatted(fields);
			defaultInterpolatedStringHandler.AppendLiteral("; available properties: ");
			defaultInterpolatedStringHandler.AppendFormatted(properties);
			throw new MissingMemberException(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		/// <summary>Gets default value for a specific type</summary>
		/// <param name="type">The class/type</param>
		/// <returns>The default value</returns>
		// Token: 0x060003B8 RID: 952 RVA: 0x000145A5 File Offset: 0x000127A5
		public static object GetDefaultValue(Type type)
		{
			if (type == null)
			{
				FileLog.Debug("AccessTools.GetDefaultValue: type is null");
				return null;
			}
			if (type == typeof(void))
			{
				return null;
			}
			if (type.IsValueType)
			{
				return Activator.CreateInstance(type);
			}
			return null;
		}

		/// <summary>Creates an (possibly uninitialized) instance of a given type</summary>
		/// <param name="type">The class/type</param>
		/// <returns>The new instance</returns>
		// Token: 0x060003B9 RID: 953 RVA: 0x000145DC File Offset: 0x000127DC
		public static object CreateInstance(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			ConstructorInfo ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, CallingConventions.Any, Array.Empty<Type>(), null);
			if (ctor != null)
			{
				return ctor.Invoke(null);
			}
			return FormatterServices.GetUninitializedObject(type);
		}

		/// <summary>Creates an (possibly uninitialized) instance of a given type</summary>
		/// <typeparam name="T">The class/type</typeparam>
		/// <returns>The new instance</returns>
		// Token: 0x060003BA RID: 954 RVA: 0x0001461C File Offset: 0x0001281C
		public static T CreateInstance<T>()
		{
			object instance = AccessTools.CreateInstance(typeof(T));
			if (instance is T)
			{
				return (T)((object)instance);
			}
			return default(T);
		}

		/// <summary>Makes a deep copy of any object</summary>
		/// <typeparam name="T">The type of the instance that should be created; for legacy reasons, this must be a class or interface</typeparam>
		/// <param name="source">The original object</param>
		/// <returns>A copy of the original object but of type T</returns>
		// Token: 0x060003BB RID: 955 RVA: 0x00014653 File Offset: 0x00012853
		public static T MakeDeepCopy<T>(object source) where T : class
		{
			return AccessTools.MakeDeepCopy(source, typeof(T), null, "") as T;
		}

		/// <summary>Makes a deep copy of any object</summary>
		/// <typeparam name="T">The type of the instance that should be created</typeparam>
		/// <param name="source">The original object</param>
		/// <param name="result">[out] The copy of the original object</param>
		/// <param name="processor">Optional value transformation function (taking a field name and src/dst <see cref="T:HarmonyLib.Traverse" /> instances)</param>
		/// <param name="pathRoot">The optional path root to start with</param>
		// Token: 0x060003BC RID: 956 RVA: 0x00014675 File Offset: 0x00012875
		public static void MakeDeepCopy<T>(object source, out T result, Func<string, Traverse, Traverse, object> processor = null, string pathRoot = "")
		{
			result = (T)((object)AccessTools.MakeDeepCopy(source, typeof(T), processor, pathRoot));
		}

		/// <summary>Makes a deep copy of any object</summary>
		/// <param name="source">The original object</param>
		/// <param name="resultType">The type of the instance that should be created</param>
		/// <param name="processor">Optional value transformation function (taking a field name and src/dst <see cref="T:HarmonyLib.Traverse" /> instances)</param>
		/// <param name="pathRoot">The optional path root to start with</param>
		/// <returns>The copy of the original object</returns>
		// Token: 0x060003BD RID: 957 RVA: 0x00014694 File Offset: 0x00012894
		public static object MakeDeepCopy(object source, Type resultType, Func<string, Traverse, Traverse, object> processor = null, string pathRoot = "")
		{
			if (source == null || resultType == null)
			{
				return null;
			}
			resultType = Nullable.GetUnderlyingType(resultType) ?? resultType;
			Type type = source.GetType();
			if (type.IsPrimitive)
			{
				return source;
			}
			if (type.IsEnum)
			{
				return Enum.ToObject(resultType, (int)source);
			}
			if (type.IsGenericType && resultType.IsGenericType)
			{
				AccessTools.addHandlerCacheLock.EnterUpgradeableReadLock();
				try
				{
					FastInvokeHandler addInvoker;
					if (!AccessTools.addHandlerCache.TryGetValue(resultType, out addInvoker))
					{
						MethodInfo addOperation = AccessTools.FirstMethod(resultType, (MethodInfo m) => m.Name == "Add" && m.GetParameters().Length == 1);
						if (addOperation != null)
						{
							addInvoker = MethodInvoker.GetHandler(addOperation, false);
						}
						AccessTools.addHandlerCacheLock.EnterWriteLock();
						try
						{
							AccessTools.addHandlerCache[resultType] = addInvoker;
						}
						finally
						{
							AccessTools.addHandlerCacheLock.ExitWriteLock();
						}
					}
					if (addInvoker != null)
					{
						object addableResult = Activator.CreateInstance(resultType);
						Type newElementType = resultType.GetGenericArguments()[0];
						int i = 0;
						foreach (object element in (source as IEnumerable))
						{
							string iStr = i++.ToString();
							string path = ((pathRoot.Length > 0) ? (pathRoot + "." + iStr) : iStr);
							object newElement = AccessTools.MakeDeepCopy(element, newElementType, processor, path);
							addInvoker(addableResult, new object[] { newElement });
						}
						return addableResult;
					}
				}
				finally
				{
					AccessTools.addHandlerCacheLock.ExitUpgradeableReadLock();
				}
			}
			if (type.IsArray && resultType.IsArray)
			{
				Type elementType = resultType.GetElementType();
				int length = ((Array)source).Length;
				object[] arrayResult = Activator.CreateInstance(resultType, new object[] { length }) as object[];
				object[] originalArray = source as object[];
				for (int j = 0; j < length; j++)
				{
					string iStr2 = j.ToString();
					string path2 = ((pathRoot.Length > 0) ? (pathRoot + "." + iStr2) : iStr2);
					arrayResult[j] = AccessTools.MakeDeepCopy(originalArray[j], elementType, processor, path2);
				}
				return arrayResult;
			}
			string ns = type.Namespace;
			if (ns == "System" || (ns != null && ns.StartsWith("System.")))
			{
				return source;
			}
			object result = AccessTools.CreateInstance((resultType == typeof(object)) ? type : resultType);
			Traverse.IterateFields(source, result, delegate(string name, Traverse src, Traverse dst)
			{
				string path3 = ((pathRoot.Length > 0) ? (pathRoot + "." + name) : name);
				object value = ((processor != null) ? processor(path3, src, dst) : src.GetValue());
				if (dst.IsWriteable)
				{
					dst.SetValue(AccessTools.MakeDeepCopy(value, dst.GetValueType(), processor, path3));
				}
			});
			return result;
		}

		/// <summary>Tests if a type is a struct</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type is a struct</returns>
		// Token: 0x060003BE RID: 958 RVA: 0x00014994 File Offset: 0x00012B94
		public static bool IsStruct(Type type)
		{
			return !(type == null) && (type.IsValueType && !AccessTools.IsValue(type)) && !AccessTools.IsVoid(type);
		}

		/// <summary>Tests if a type is a class</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type is a class</returns>
		// Token: 0x060003BF RID: 959 RVA: 0x000149BC File Offset: 0x00012BBC
		public static bool IsClass(Type type)
		{
			return !(type == null) && !type.IsValueType;
		}

		/// <summary>Tests if a type is a value type</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type is a value type</returns>
		// Token: 0x060003C0 RID: 960 RVA: 0x000149D2 File Offset: 0x00012BD2
		public static bool IsValue(Type type)
		{
			return !(type == null) && (type.IsPrimitive || type.IsEnum);
		}

		/// <summary>Tests if a type is an integer type</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type represents some integer</returns>
		// Token: 0x060003C1 RID: 961 RVA: 0x000149F0 File Offset: 0x00012BF0
		public static bool IsInteger(Type type)
		{
			if (type == null)
			{
				return false;
			}
			TypeCode typeCode = Type.GetTypeCode(type);
			return typeCode - TypeCode.SByte <= 7;
		}

		/// <summary>Tests if a type is a floating point type</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type represents some floating point</returns>
		// Token: 0x060003C2 RID: 962 RVA: 0x00014A1C File Offset: 0x00012C1C
		public static bool IsFloatingPoint(Type type)
		{
			if (type == null)
			{
				return false;
			}
			TypeCode typeCode = Type.GetTypeCode(type);
			return typeCode - TypeCode.Single <= 2;
		}

		/// <summary>Tests if a type is a numerical type</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type represents some number</returns>
		// Token: 0x060003C3 RID: 963 RVA: 0x00014A49 File Offset: 0x00012C49
		public static bool IsNumber(Type type)
		{
			return AccessTools.IsInteger(type) || AccessTools.IsFloatingPoint(type);
		}

		/// <summary>Tests if a type is void</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type is void</returns>
		// Token: 0x060003C4 RID: 964 RVA: 0x00014A5B File Offset: 0x00012C5B
		public static bool IsVoid(Type type)
		{
			return type == typeof(void);
		}

		/// <summary>Test whether an instance is of a nullable type</summary>
		/// <typeparam name="T">Type of instance</typeparam>
		/// <param name="instance">An instance to test</param>
		/// <returns>True if instance is of nullable type, false if not</returns>
		// Token: 0x060003C5 RID: 965 RVA: 0x00014A6D File Offset: 0x00012C6D
		public static bool IsOfNullableType<T>(T instance)
		{
			return Nullable.GetUnderlyingType(typeof(T)) != null;
		}

		/// <summary>Tests whether a type or member is static, as defined in C#</summary>
		/// <param name="member">The type or member</param>
		/// <returns>True if the type or member is static</returns>
		// Token: 0x060003C6 RID: 966 RVA: 0x00014A84 File Offset: 0x00012C84
		public static bool IsStatic(MemberInfo member)
		{
			if (member == null)
			{
				throw new ArgumentNullException("member");
			}
			MemberTypes memberType = member.MemberType;
			if (memberType <= MemberTypes.Method)
			{
				switch (memberType)
				{
				case MemberTypes.Constructor:
					break;
				case MemberTypes.Event:
					return AccessTools.IsStatic((EventInfo)member);
				case MemberTypes.Constructor | MemberTypes.Event:
					goto IL_91;
				case MemberTypes.Field:
					return ((FieldInfo)member).IsStatic;
				default:
					if (memberType != MemberTypes.Method)
					{
						goto IL_91;
					}
					break;
				}
				return ((MethodBase)member).IsStatic;
			}
			if (memberType == MemberTypes.Property)
			{
				return AccessTools.IsStatic((PropertyInfo)member);
			}
			if (memberType == MemberTypes.TypeInfo || memberType == MemberTypes.NestedType)
			{
				return AccessTools.IsStatic((Type)member);
			}
			IL_91:
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Unknown member type: ");
			defaultInterpolatedStringHandler.AppendFormatted<MemberTypes>(member.MemberType);
			throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		/// <summary>Tests whether a type is static, as defined in C#</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type is static</returns>
		// Token: 0x060003C7 RID: 967 RVA: 0x00014B53 File Offset: 0x00012D53
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static bool IsStatic(Type type)
		{
			return type != null && type.IsAbstract && type.IsSealed;
		}

		/// <summary>Tests whether a property is static, as defined in C#</summary>
		/// <param name="propertyInfo">The property</param>
		/// <returns>True if the property is static</returns>
		// Token: 0x060003C8 RID: 968 RVA: 0x00014B6A File Offset: 0x00012D6A
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static bool IsStatic(PropertyInfo propertyInfo)
		{
			if (propertyInfo == null)
			{
				throw new ArgumentNullException("propertyInfo");
			}
			return propertyInfo.GetAccessors(true)[0].IsStatic;
		}

		/// <summary>Tests whether an event is static, as defined in C#</summary>
		/// <param name="eventInfo">The event</param>
		/// <returns>True if the event is static</returns>
		// Token: 0x060003C9 RID: 969 RVA: 0x00014B88 File Offset: 0x00012D88
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static bool IsStatic(EventInfo eventInfo)
		{
			if (eventInfo == null)
			{
				throw new ArgumentNullException("eventInfo");
			}
			return eventInfo.GetAddMethod(true).IsStatic;
		}

		/// <summary>Calculates a combined hash code for an enumeration of objects</summary>
		/// <param name="objects">The objects</param>
		/// <returns>The hash code</returns>
		// Token: 0x060003CA RID: 970 RVA: 0x00014BA4 File Offset: 0x00012DA4
		public static int CombinedHashCode(IEnumerable<object> objects)
		{
			int hash = 352654597;
			int hash2 = hash;
			int i = 0;
			foreach (object obj in objects)
			{
				if (i % 2 == 0)
				{
					hash = ((hash << 5) + hash + (hash >> 27)) ^ obj.GetHashCode();
				}
				else
				{
					hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ obj.GetHashCode();
				}
				i++;
			}
			return hash + hash2 * 1566083941;
		}

		// Token: 0x060003CB RID: 971 RVA: 0x00014C2C File Offset: 0x00012E2C
		// Note: this type is marked as 'beforefieldinit'.
		static AccessTools()
		{
			Type type = Type.GetType("System.Runtime.InteropServices.RuntimeInformation", false);
			AccessTools.IsNetFrameworkRuntime = ((type != null) ? type.GetProperty("FrameworkDescription").GetValue(null, null).ToString()
				.StartsWith(".NET Framework") : (!AccessTools.IsMonoRuntime));
			Type type2 = Type.GetType("System.Runtime.InteropServices.RuntimeInformation", false);
			AccessTools.IsNetCoreRuntime = type2 != null && type2.GetProperty("FrameworkDescription").GetValue(null, null).ToString()
				.StartsWith(".NET Core");
			AccessTools.addHandlerCache = new Dictionary<Type, FastInvokeHandler>();
			AccessTools.addHandlerCacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		// Token: 0x0400023C RID: 572
		private static Type[] allTypesCached = null;

		/// <summary>Shortcut for <see cref="T:System.Reflection.BindingFlags" /> to simplify the use of reflections and make it work for any access level</summary>
		// Token: 0x0400023D RID: 573
		public static readonly BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty;

		/// <summary>Shortcut for <see cref="T:System.Reflection.BindingFlags" /> to simplify the use of reflections and make it work for any access level but only within the current type</summary>
		// Token: 0x0400023E RID: 574
		public static readonly BindingFlags allDeclared = AccessTools.all | BindingFlags.DeclaredOnly;

		/// <summary>
		///  A cache for the <see cref="M:System.Collections.Generic.ICollection`1.Add(`0)" /> or similar Add methods for different types.
		///  </summary>
		// Token: 0x04000242 RID: 578
		private static readonly Dictionary<Type, FastInvokeHandler> addHandlerCache;

		// Token: 0x04000243 RID: 579
		private static readonly ReaderWriterLockSlim addHandlerCacheLock;

		/// <summary>A readable/assignable reference delegate to an instance field of a class or static field (NOT an instance field of a struct)</summary>
		/// <typeparam name="T">
		///  An arbitrary type if the field is static; otherwise the class that defines the field, or a parent class (including <see cref="T:System.Object" />),
		///  implemented interface, or derived class of this type
		///  </typeparam>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="instance">The runtime instance to access the field (ignored and can be omitted for static fields)</param>
		/// <returns>A readable/assignable reference to the field</returns>
		/// <exception cref="T:System.NullReferenceException">Null instance passed to a non-static field ref delegate</exception>
		/// <exception cref="T:System.InvalidCastException">
		///  Instance of invalid type passed to a non-static field ref delegate
		///  (this can happen if <typeparamref name="T" /> is a parent class or interface of the field's declaring type)
		///  </exception>
		/// <remarks>
		///     <para>
		///  This delegate cannot be used for instance fields of structs, since a struct instance passed to the delegate would be passed by
		///  value and thus would be a copy that only exists within the delegate's invocation. This is fine for a readonly reference,
		///  but makes assignment futile. Use <see cref="T:HarmonyLib.AccessTools.StructFieldRef`2" /> instead.
		///  </para>
		///     <para>
		///  Note that <typeparamref name="T" /> is not required to be the field's declaring type. It can be a parent class (including <see cref="T:System.Object" />),
		///  implemented interface, or a derived class of the field's declaring type ("<c>instanceOfT is FieldDeclaringType</c>" must be possible).
		///  Specifically, <typeparamref name="F" /> must be <see cref="M:System.Type.IsAssignableFrom(System.Type)">assignable from</see> OR to the field's declaring type.
		///  Technically, this allows <c>Nullable</c>, although <c>Nullable</c> is only relevant for structs, and since only static fields of structs
		///  are allowed for this delegate, and the instance passed to such a delegate is ignored, this hardly matters.
		///  </para>
		///     <para>
		///  Similarly, <typeparamref name="F" /> is not required to be the field's field type, unless that type is a non-enum value type.
		///  It can be a parent class (including <c>object</c>) or implemented interface of the field's field type. It cannot be a derived class.
		///  This variance is not allowed for value types, since that would require boxing/unboxing, which is not allowed for ref values.
		///  Special case for enum types: <typeparamref name="F" /> can also be the underlying integral type of the enum type.
		///  Specifically, for reference types, <typeparamref name="F" /> must be <see cref="M:System.Type.IsAssignableFrom(System.Type)">assignable from</see>
		///  the field's field type; for non-enum value types, <typeparamref name="F" /> must be exactly the field's field type; for enum types,
		///  <typeparamref name="F" /> must be either the field's field type or the underyling integral type of that field type.
		///  </para>
		///     <para>
		///  This delegate supports static fields, even those defined in structs, for legacy reasons.
		///  For such static fields, <typeparamref name="T" /> is effectively ignored.
		///  Consider using <see cref="T:HarmonyLib.AccessTools.FieldRef`1" /> (and <c>StaticFieldRefAccess</c> methods that return it) instead for static fields.
		///  </para>
		/// </remarks>
		// Token: 0x020000A8 RID: 168
		// (Invoke) Token: 0x060003CD RID: 973
		public unsafe delegate F* FieldRef<in T, F>(T instance = default(T));

		/// <summary>A readable/assignable reference delegate to an instance field of a struct</summary>
		/// <typeparam name="T">The struct that defines the instance field</typeparam>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <param name="instance">A reference to the runtime instance to access the field</param>
		/// <returns>A readable/assignable reference to the field</returns>
		// Token: 0x020000A9 RID: 169
		// (Invoke) Token: 0x060003D1 RID: 977
		public unsafe delegate F* StructFieldRef<T, F>(ref T instance) where T : struct;

		/// <summary>A readable/assignable reference delegate to a static field</summary>
		/// <typeparam name="F">
		///  The type of the field; or if the field's type is a reference type (a class or interface, NOT a struct or other value type),
		///  a type that <see cref="M:System.Type.IsAssignableFrom(System.Type)">is assignable from</see> that type; or if the field's type is an enum type,
		///  either that type or the underlying integral type of that enum type
		///  </typeparam>
		/// <returns>A readable/assignable reference to the field</returns>
		// Token: 0x020000AA RID: 170
		// (Invoke) Token: 0x060003D5 RID: 981
		public unsafe delegate F* FieldRef<F>();

		// Token: 0x020000AB RID: 171
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x04000244 RID: 580
			public static Func<Assembly, IEnumerable<Type>> <0>__GetTypesFromAssembly;
		}
	}
}
