using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace HarmonyLib
{
	/// <summary>Adds extensions to Type for a lot of AccessTools methods</summary>
	// Token: 0x020000C3 RID: 195
	public static class AccessToolsExtensions
	{
		/// <summary>Enumerates all inner types (non-recursive)</summary>
		/// <param name="type">The class/type to start with</param>
		/// <returns>An enumeration of all inner <see cref="T:System.Type" /></returns>
		// Token: 0x0600041C RID: 1052 RVA: 0x000150F9 File Offset: 0x000132F9
		public static IEnumerable<Type> InnerTypes(this Type type)
		{
			return AccessTools.InnerTypes(type);
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
		// Token: 0x0600041D RID: 1053 RVA: 0x00015101 File Offset: 0x00013301
		public static T FindIncludingBaseTypes<T>(this Type type, Func<Type, T> func) where T : class
		{
			return AccessTools.FindIncludingBaseTypes<T>(type, func);
		}

		/// <summary>Applies a function going into inner types and stops at the first non-<c>null</c> result</summary>
		/// <typeparam name="T">Generic type parameter</typeparam>
		/// <param name="type">The class/type to start with</param>
		/// <param name="func">The evaluation function returning T</param>
		/// <returns>The first non-<c>null</c> result, or <c>null</c> if no match</returns>
		// Token: 0x0600041E RID: 1054 RVA: 0x0001510A File Offset: 0x0001330A
		public static T FindIncludingInnerTypes<T>(this Type type, Func<Type, T> func) where T : class
		{
			return AccessTools.FindIncludingInnerTypes<T>(type, func);
		}

		/// <summary>Gets the reflection information for a directly declared field</summary>
		/// <param name="type">The class/type where the field is defined</param>
		/// <param name="name">The name of the field</param>
		/// <returns>A field or null when type/name is null or when the field cannot be found</returns>
		// Token: 0x0600041F RID: 1055 RVA: 0x00015113 File Offset: 0x00013313
		public static FieldInfo DeclaredField(this Type type, string name)
		{
			return AccessTools.DeclaredField(type, name);
		}

		/// <summary>Gets the reflection information for a field by searching the type and all its super types</summary>
		/// <param name="type">The class/type where the field is defined</param>
		/// <param name="name">The name of the field (case sensitive)</param>
		/// <returns>A field or null when type/name is null or when the field cannot be found</returns>
		// Token: 0x06000420 RID: 1056 RVA: 0x0001511C File Offset: 0x0001331C
		public static FieldInfo Field(this Type type, string name)
		{
			return AccessTools.Field(type, name);
		}

		/// <summary>Gets the reflection information for a field</summary>
		/// <param name="type">The class/type where the field is declared</param>
		/// <param name="idx">The zero-based index of the field inside the class definition</param>
		/// <returns>A field or null when type is null or when the field cannot be found</returns>
		// Token: 0x06000421 RID: 1057 RVA: 0x00015125 File Offset: 0x00013325
		public static FieldInfo DeclaredField(this Type type, int idx)
		{
			return AccessTools.DeclaredField(type, idx);
		}

		/// <summary>Gets the reflection information for a directly declared property</summary>
		/// <param name="type">The class/type where the property is declared</param>
		/// <param name="name">The name of the property (case sensitive)</param>
		/// <returns>A property or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x06000422 RID: 1058 RVA: 0x0001512E File Offset: 0x0001332E
		public static PropertyInfo DeclaredProperty(this Type type, string name)
		{
			return AccessTools.DeclaredProperty(type, name);
		}

		/// <summary>Gets the reflection information for a directly declared indexer property</summary>
		/// <param name="type">The class/type where the indexer property is declared</param>
		/// <param name="parameters">Optional parameters to target a specific overload of multiple indexers</param>
		/// <returns>An indexer property or null when type is null or when it cannot be found</returns>
		// Token: 0x06000423 RID: 1059 RVA: 0x00015137 File Offset: 0x00013337
		public static PropertyInfo DeclaredIndexer(this Type type, Type[] parameters = null)
		{
			return AccessTools.DeclaredIndexer(type, parameters);
		}

		/// <summary>Gets the reflection information for the getter method of a directly declared property</summary>
		/// <param name="type">The class/type where the property is declared</param>
		/// <param name="name">The name of the property (case sensitive)</param>
		/// <returns>A method or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x06000424 RID: 1060 RVA: 0x00015140 File Offset: 0x00013340
		public static MethodInfo DeclaredPropertyGetter(this Type type, string name)
		{
			return AccessTools.DeclaredPropertyGetter(type, name);
		}

		/// <summary>Gets the reflection information for the getter method of a directly declared indexer property</summary>
		/// <param name="type">The class/type where the indexer property is declared</param>
		/// <param name="parameters">Optional parameters to target a specific overload of multiple indexers</param>
		/// <returns>A method or null when type is null or when indexer property cannot be found</returns>
		// Token: 0x06000425 RID: 1061 RVA: 0x00015149 File Offset: 0x00013349
		public static MethodInfo DeclaredIndexerGetter(this Type type, Type[] parameters = null)
		{
			return AccessTools.DeclaredIndexerGetter(type, parameters);
		}

		/// <summary>Gets the reflection information for the setter method of a directly declared property</summary>
		/// <param name="type">The class/type where the property is declared</param>
		/// <param name="name">The name of the property (case sensitive)</param>
		/// <returns>A method or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x06000426 RID: 1062 RVA: 0x00015152 File Offset: 0x00013352
		public static MethodInfo DeclaredPropertySetter(this Type type, string name)
		{
			return AccessTools.DeclaredPropertySetter(type, name);
		}

		/// <summary>Gets the reflection information for the setter method of a directly declared indexer property</summary>
		/// <param name="type">The class/type where the indexer property is declared</param>
		/// <param name="parameters">Optional parameters to target a specific overload of multiple indexers</param>
		/// <returns>A method or null when type is null or when indexer property cannot be found</returns>
		// Token: 0x06000427 RID: 1063 RVA: 0x0001515B File Offset: 0x0001335B
		public static MethodInfo DeclaredIndexerSetter(this Type type, Type[] parameters)
		{
			return AccessTools.DeclaredIndexerSetter(type, parameters);
		}

		/// <summary>Gets the reflection information for a property by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="name">The name</param>
		/// <returns>A property or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x06000428 RID: 1064 RVA: 0x00015164 File Offset: 0x00013364
		public static PropertyInfo Property(this Type type, string name)
		{
			return AccessTools.Property(type, name);
		}

		/// <summary>Gets the reflection information for an indexer property by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="parameters">Optional parameters to target a specific overload of multiple indexers</param>
		/// <returns>An indexer property or null when type is null or when it cannot be found</returns>
		// Token: 0x06000429 RID: 1065 RVA: 0x0001516D File Offset: 0x0001336D
		public static PropertyInfo Indexer(this Type type, Type[] parameters = null)
		{
			return AccessTools.Indexer(type, parameters);
		}

		/// <summary>Gets the reflection information for the getter method of a property by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="name">The name</param>
		/// <returns>A method or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x0600042A RID: 1066 RVA: 0x00015176 File Offset: 0x00013376
		public static MethodInfo PropertyGetter(this Type type, string name)
		{
			return AccessTools.PropertyGetter(type, name);
		}

		/// <summary>Gets the reflection information for the getter method of an indexer property by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="parameters">Optional parameters to target a specific overload of multiple indexers</param>
		/// <returns>A method or null when type is null or when the indexer property cannot be found</returns>
		// Token: 0x0600042B RID: 1067 RVA: 0x0001517F File Offset: 0x0001337F
		public static MethodInfo IndexerGetter(this Type type, Type[] parameters = null)
		{
			return AccessTools.IndexerGetter(type, parameters);
		}

		/// <summary>Gets the reflection information for the setter method of a property by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="name">The name</param>
		/// <returns>A method or null when type/name is null or when the property cannot be found</returns>
		// Token: 0x0600042C RID: 1068 RVA: 0x00015188 File Offset: 0x00013388
		public static MethodInfo PropertySetter(this Type type, string name)
		{
			return AccessTools.PropertySetter(type, name);
		}

		/// <summary>Gets the reflection information for the setter method of an indexer property by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="parameters">Optional parameters to target a specific overload of multiple indexers</param>
		/// <returns>A method or null when type is null or when the indexer property cannot be found</returns>
		// Token: 0x0600042D RID: 1069 RVA: 0x00015191 File Offset: 0x00013391
		public static MethodInfo IndexerSetter(this Type type, Type[] parameters = null)
		{
			return AccessTools.IndexerSetter(type, parameters);
		}

		/// <summary>Gets the reflection information for a directly declared event</summary>
		/// <param name="type">The class/type where the event is declared</param>
		/// <param name="name">The name of the event (case sensitive)</param>
		/// <returns>An event or null when type/name is null or when the event cannot be found</returns>
		// Token: 0x0600042E RID: 1070 RVA: 0x0001519A File Offset: 0x0001339A
		public static EventInfo DeclaredEvent(this Type type, string name)
		{
			return AccessTools.DeclaredEvent(type, name);
		}

		/// <summary>Gets the reflection information for an event by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="name">The name</param>
		/// <returns>An event or null when type/name is null or when the event cannot be found</returns>
		// Token: 0x0600042F RID: 1071 RVA: 0x000151A3 File Offset: 0x000133A3
		public static EventInfo Event(this Type type, string name)
		{
			return AccessTools.Event(type, name);
		}

		/// <summary>Gets the reflection information for the add method of a directly declared event</summary>
		/// <param name="type">The class/type where the event is declared</param>
		/// <param name="name">The name of the event (case sensitive)</param>
		/// <returns>A method or null when type/name is null or when the event cannot be found</returns>
		// Token: 0x06000430 RID: 1072 RVA: 0x000151AC File Offset: 0x000133AC
		public static MethodInfo DeclaredEventAdder(this Type type, string name)
		{
			return AccessTools.DeclaredEventAdder(type, name);
		}

		/// <summary>Gets the reflection information for the add method of an event by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="name">The name</param>
		/// <returns>A method or null when type/name is null or when the event cannot be found</returns>
		// Token: 0x06000431 RID: 1073 RVA: 0x000151B5 File Offset: 0x000133B5
		public static MethodInfo EventAdder(this Type type, string name)
		{
			return AccessTools.EventAdder(type, name);
		}

		/// <summary>Gets the reflection information for the remove method of a directly declared event</summary>
		/// <param name="type">The class/type where the event is declared</param>
		/// <param name="name">The name of the event (case sensitive)</param>
		/// <returns>A method or null when type/name is null or when the event cannot be found</returns>
		// Token: 0x06000432 RID: 1074 RVA: 0x000151BE File Offset: 0x000133BE
		public static MethodInfo DeclaredEventRemover(this Type type, string name)
		{
			return AccessTools.DeclaredEventRemover(type, name);
		}

		/// <summary>Gets the reflection information for the remove method of an event by searching the type and all its super types</summary>
		/// <param name="type">The class/type</param>
		/// <param name="name">The name</param>
		/// <returns>A method or null when type/name is null or when the event cannot be found</returns>
		// Token: 0x06000433 RID: 1075 RVA: 0x000151C7 File Offset: 0x000133C7
		public static MethodInfo EventRemover(this Type type, string name)
		{
			return AccessTools.EventRemover(type, name);
		}

		/// <summary>Gets the reflection information for a finalizer</summary>
		/// <param name="type">The class/type that defines the finalizer</param>
		/// <returns>A method or null when type is null or when the finalizer cannot be found</returns>
		// Token: 0x06000434 RID: 1076 RVA: 0x000151D0 File Offset: 0x000133D0
		public static MethodInfo Finalizer(this Type type)
		{
			return AccessTools.Finalizer(type);
		}

		/// <summary>Gets the reflection information for a directly declared finalizer</summary>
		/// <param name="type">The class/type that defines the finalizer</param>
		/// <returns>A method or null when type is null or when the finalizer cannot be found</returns>
		// Token: 0x06000435 RID: 1077 RVA: 0x000151D8 File Offset: 0x000133D8
		public static MethodInfo DeclaredFinalizer(this Type type)
		{
			return AccessTools.DeclaredFinalizer(type);
		}

		/// <summary>Gets the reflection information for a directly declared method</summary>
		/// <param name="type">The class/type where the method is declared</param>
		/// <param name="name">The name of the method (case sensitive)</param>
		/// <param name="parameters">Optional parameters to target a specific overload of the method</param>
		/// <param name="generics">Optional list of types that define the generic version of the method</param>
		/// <returns>A method or null when type/name is null or when the method cannot be found</returns>
		// Token: 0x06000436 RID: 1078 RVA: 0x000151E0 File Offset: 0x000133E0
		public static MethodInfo DeclaredMethod(this Type type, string name, Type[] parameters = null, Type[] generics = null)
		{
			return AccessTools.DeclaredMethod(type, name, parameters, generics);
		}

		/// <summary>Gets the reflection information for a method by searching the type and all its super types</summary>
		/// <param name="type">The class/type where the method is declared</param>
		/// <param name="name">The name of the method (case sensitive)</param>
		/// <param name="parameters">Optional parameters to target a specific overload of the method</param>
		/// <param name="generics">Optional list of types that define the generic version of the method</param>
		/// <returns>A method or null when type/name is null or when the method cannot be found</returns>
		// Token: 0x06000437 RID: 1079 RVA: 0x000151EB File Offset: 0x000133EB
		public static MethodInfo Method(this Type type, string name, Type[] parameters = null, Type[] generics = null)
		{
			return AccessTools.Method(type, name, parameters, generics);
		}

		/// <summary>Gets the names of all method that are declared in a type</summary>
		/// <param name="type">The declaring class/type</param>
		/// <returns>A list of method names</returns>
		// Token: 0x06000438 RID: 1080 RVA: 0x000151F6 File Offset: 0x000133F6
		public static List<string> GetMethodNames(this Type type)
		{
			return AccessTools.GetMethodNames(type);
		}

		/// <summary>Gets the names of all fields that are declared in a type</summary>
		/// <param name="type">The declaring class/type</param>
		/// <returns>A list of field names</returns>
		// Token: 0x06000439 RID: 1081 RVA: 0x000151FE File Offset: 0x000133FE
		public static List<string> GetFieldNames(this Type type)
		{
			return AccessTools.GetFieldNames(type);
		}

		/// <summary>Gets the names of all properties that are declared in a type</summary>
		/// <param name="type">The declaring class/type</param>
		/// <returns>A list of property names</returns>
		// Token: 0x0600043A RID: 1082 RVA: 0x00015206 File Offset: 0x00013406
		public static List<string> GetPropertyNames(this Type type)
		{
			return AccessTools.GetPropertyNames(type);
		}

		/// <summary>Gets the reflection information for a directly declared constructor</summary>
		/// <param name="type">The class/type where the constructor is declared</param>
		/// <param name="parameters">Optional parameters to target a specific overload of the constructor</param>
		/// <param name="searchForStatic">Optional parameters to only consider static constructors</param>
		/// <returns>A constructor info or null when type is null or when the constructor cannot be found</returns>
		// Token: 0x0600043B RID: 1083 RVA: 0x0001520E File Offset: 0x0001340E
		public static ConstructorInfo DeclaredConstructor(this Type type, Type[] parameters = null, bool searchForStatic = false)
		{
			return AccessTools.DeclaredConstructor(type, parameters, searchForStatic);
		}

		/// <summary>Gets the reflection information for a constructor by searching the type and all its super types</summary>
		/// <param name="type">The class/type where the constructor is declared</param>
		/// <param name="parameters">Optional parameters to target a specific overload of the method</param>
		/// <param name="searchForStatic">Optional parameters to only consider static constructors</param>
		/// <returns>A constructor info or null when type is null or when the method cannot be found</returns>
		// Token: 0x0600043C RID: 1084 RVA: 0x00015218 File Offset: 0x00013418
		public static ConstructorInfo Constructor(this Type type, Type[] parameters = null, bool searchForStatic = false)
		{
			return AccessTools.Constructor(type, parameters, searchForStatic);
		}

		/// <summary>Gets reflection information for all declared constructors</summary>
		/// <param name="type">The class/type where the constructors are declared</param>
		/// <param name="searchForStatic">Optional parameters to only consider static constructors</param>
		/// <returns>A list of constructor infos</returns>
		// Token: 0x0600043D RID: 1085 RVA: 0x00015222 File Offset: 0x00013422
		public static List<ConstructorInfo> GetDeclaredConstructors(this Type type, bool? searchForStatic = null)
		{
			return AccessTools.GetDeclaredConstructors(type, searchForStatic);
		}

		/// <summary>Gets reflection information for all declared methods</summary>
		/// <param name="type">The class/type where the methods are declared</param>
		/// <returns>A list of methods</returns>
		// Token: 0x0600043E RID: 1086 RVA: 0x0001522B File Offset: 0x0001342B
		public static List<MethodInfo> GetDeclaredMethods(this Type type)
		{
			return AccessTools.GetDeclaredMethods(type);
		}

		/// <summary>Gets reflection information for all declared properties</summary>
		/// <param name="type">The class/type where the properties are declared</param>
		/// <returns>A list of properties</returns>
		// Token: 0x0600043F RID: 1087 RVA: 0x00015233 File Offset: 0x00013433
		public static List<PropertyInfo> GetDeclaredProperties(this Type type)
		{
			return AccessTools.GetDeclaredProperties(type);
		}

		/// <summary>Gets reflection information for all declared fields</summary>
		/// <param name="type">The class/type where the fields are declared</param>
		/// <returns>A list of fields</returns>
		// Token: 0x06000440 RID: 1088 RVA: 0x0001523B File Offset: 0x0001343B
		public static List<FieldInfo> GetDeclaredFields(this Type type)
		{
			return AccessTools.GetDeclaredFields(type);
		}

		/// <summary>Given a type, returns the first inner type matching a recursive search by name</summary>
		/// <param name="type">The class/type to start searching at</param>
		/// <param name="name">The name of the inner type (case sensitive)</param>
		/// <returns>The inner type or null if type/name is null or if a type with that name cannot be found</returns>
		// Token: 0x06000441 RID: 1089 RVA: 0x00015243 File Offset: 0x00013443
		public static Type Inner(this Type type, string name)
		{
			return AccessTools.Inner(type, name);
		}

		/// <summary>Given a type, returns the first inner type matching a recursive search with a predicate</summary>
		/// <param name="type">The class/type to start searching at</param>
		/// <param name="predicate">The predicate to search with</param>
		/// <returns>The inner type or null if type/predicate is null or if a type with that name cannot be found</returns>
		// Token: 0x06000442 RID: 1090 RVA: 0x0001524C File Offset: 0x0001344C
		public static Type FirstInner(this Type type, Func<Type, bool> predicate)
		{
			return AccessTools.FirstInner(type, predicate);
		}

		/// <summary>Given a type, returns the first method matching a predicate</summary>
		/// <param name="type">The class/type to start searching at</param>
		/// <param name="predicate">The predicate to search with</param>
		/// <returns>The method or null if type/predicate is null or if a type with that name cannot be found</returns>
		// Token: 0x06000443 RID: 1091 RVA: 0x00015255 File Offset: 0x00013455
		public static MethodInfo FirstMethod(this Type type, Func<MethodInfo, bool> predicate)
		{
			return AccessTools.FirstMethod(type, predicate);
		}

		/// <summary>Given a type, returns the first constructor matching a predicate</summary>
		/// <param name="type">The class/type to start searching at</param>
		/// <param name="predicate">The predicate to search with</param>
		/// <returns>The constructor info or null if type/predicate is null or if a type with that name cannot be found</returns>
		// Token: 0x06000444 RID: 1092 RVA: 0x0001525E File Offset: 0x0001345E
		public static ConstructorInfo FirstConstructor(this Type type, Func<ConstructorInfo, bool> predicate)
		{
			return AccessTools.FirstConstructor(type, predicate);
		}

		/// <summary>Given a type, returns the first property matching a predicate</summary>
		/// <param name="type">The class/type to start searching at</param>
		/// <param name="predicate">The predicate to search with</param>
		/// <returns>The property or null if type/predicate is null or if a type with that name cannot be found</returns>
		// Token: 0x06000445 RID: 1093 RVA: 0x00015267 File Offset: 0x00013467
		public static PropertyInfo FirstProperty(this Type type, Func<PropertyInfo, bool> predicate)
		{
			return AccessTools.FirstProperty(type, predicate);
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
		///  Consider using <see cref="M:HarmonyLib.AccessToolsExtensions.StaticFieldRefAccess``1(System.Type,System.String)" /> (and other overloads) instead for static fields.
		///  </para>
		/// </remarks>
		// Token: 0x06000446 RID: 1094 RVA: 0x00015270 File Offset: 0x00013470
		public static AccessTools.FieldRef<object, F> FieldRefAccess<F>(this Type type, string fieldName)
		{
			return AccessTools.FieldRefAccess<F>(type, fieldName);
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
		// Token: 0x06000447 RID: 1095 RVA: 0x00015279 File Offset: 0x00013479
		public static ref F StaticFieldRefAccess<F>(this Type type, string fieldName)
		{
			return AccessTools.StaticFieldRefAccess<F>(type, fieldName);
		}

		/// <summary>Throws a missing member runtime exception</summary>
		/// <param name="type">The type that is involved</param>
		/// <param name="names">A list of names</param>
		// Token: 0x06000448 RID: 1096 RVA: 0x00015282 File Offset: 0x00013482
		public static void ThrowMissingMemberException(this Type type, params string[] names)
		{
			AccessTools.ThrowMissingMemberException(type, names);
		}

		/// <summary>Gets default value for a specific type</summary>
		/// <param name="type">The class/type</param>
		/// <returns>The default value</returns>
		// Token: 0x06000449 RID: 1097 RVA: 0x0001528B File Offset: 0x0001348B
		public static object GetDefaultValue(this Type type)
		{
			return AccessTools.GetDefaultValue(type);
		}

		/// <summary>Creates an (possibly uninitialized) instance of a given type</summary>
		/// <param name="type">The class/type</param>
		/// <returns>The new instance</returns>
		// Token: 0x0600044A RID: 1098 RVA: 0x00015293 File Offset: 0x00013493
		public static object CreateInstance(this Type type)
		{
			return AccessTools.CreateInstance(type);
		}

		/// <summary>Tests if a type is a struct</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type is a struct</returns>
		// Token: 0x0600044B RID: 1099 RVA: 0x0001529B File Offset: 0x0001349B
		public static bool IsStruct(this Type type)
		{
			return AccessTools.IsStruct(type);
		}

		/// <summary>Tests if a type is a class</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type is a class</returns>
		// Token: 0x0600044C RID: 1100 RVA: 0x000152A3 File Offset: 0x000134A3
		public static bool IsClass(this Type type)
		{
			return AccessTools.IsClass(type);
		}

		/// <summary>Tests if a type is a value type</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type is a value type</returns>
		// Token: 0x0600044D RID: 1101 RVA: 0x000152AB File Offset: 0x000134AB
		public static bool IsValue(this Type type)
		{
			return AccessTools.IsValue(type);
		}

		/// <summary>Tests if a type is an integer type</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type represents some integer</returns>
		// Token: 0x0600044E RID: 1102 RVA: 0x000152B3 File Offset: 0x000134B3
		public static bool IsInteger(this Type type)
		{
			return AccessTools.IsInteger(type);
		}

		/// <summary>Tests if a type is a floating point type</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type represents some floating point</returns>
		// Token: 0x0600044F RID: 1103 RVA: 0x000152BB File Offset: 0x000134BB
		public static bool IsFloatingPoint(this Type type)
		{
			return AccessTools.IsFloatingPoint(type);
		}

		/// <summary>Tests if a type is a numerical type</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type represents some number</returns>
		// Token: 0x06000450 RID: 1104 RVA: 0x000152C3 File Offset: 0x000134C3
		public static bool IsNumber(this Type type)
		{
			return AccessTools.IsNumber(type);
		}

		/// <summary>Tests if a type is void</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type is void</returns>
		// Token: 0x06000451 RID: 1105 RVA: 0x000152CB File Offset: 0x000134CB
		public static bool IsVoid(this Type type)
		{
			return AccessTools.IsVoid(type);
		}

		/// <summary>Tests whether a type is static, as defined in C#</summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type is static</returns>
		// Token: 0x06000452 RID: 1106 RVA: 0x000152D3 File Offset: 0x000134D3
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static bool IsStatic(this Type type)
		{
			return AccessTools.IsStatic(type);
		}
	}
}
