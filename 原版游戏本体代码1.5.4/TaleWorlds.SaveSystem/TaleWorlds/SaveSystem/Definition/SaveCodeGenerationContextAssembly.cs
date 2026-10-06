using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.Library.CodeGeneration;

namespace TaleWorlds.SaveSystem.Definition;

internal class SaveCodeGenerationContextAssembly
{
	private List<TypeDefinition> _definitions;

	private List<TypeDefinition> _structDefinitions;

	private List<ContainerDefinition> _containerDefinitions;

	private CodeGenerationContext _codeGenerationContext;

	private DefinitionContext _definitionContext;

	private ClassCode _managerClass;

	private MethodCode _managerMethod;

	private int _delegateCount;

	private int _containerNumber;

	public Assembly Assembly { get; private set; }

	public string Location { get; private set; }

	public string FileName { get; private set; }

	public string DefaultNamespace { get; private set; }

	public SaveCodeGenerationContextAssembly(DefinitionContext definitionContext, Assembly assembly, string defaultNamespace, string location, string fileName)
	{
		Assembly = assembly;
		Location = location;
		FileName = fileName;
		DefaultNamespace = defaultNamespace;
		_definitionContext = definitionContext;
		_definitions = new List<TypeDefinition>();
		_structDefinitions = new List<TypeDefinition>();
		_containerDefinitions = new List<ContainerDefinition>();
		_codeGenerationContext = new CodeGenerationContext();
	}

	public void AddClassDefinition(TypeDefinition classDefinition)
	{
		_definitions.Add(classDefinition);
	}

	public void AddStructDefinition(TypeDefinition classDefinition)
	{
		_structDefinitions.Add(classDefinition);
	}

	public bool CheckIfGotAnyNonPrimitiveMembers(TypeDefinition typeDefinition)
	{
		MemberDefinition[] array = typeDefinition.MemberDefinitions.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			Type memberType = array[i].GetMemberType();
			if (memberType.IsClass && memberType != typeof(string))
			{
				return true;
			}
			if (!(_definitionContext.GetTypeDefinition(memberType) is BasicTypeDefinition))
			{
				return true;
			}
		}
		return false;
	}

	private string[] GetNestedClasses(string fullClassName)
	{
		return fullClassName.Split(new char[1] { '.' }, StringSplitOptions.None);
	}

	private bool CheckIfBaseTypeDefind(Type type)
	{
		Type baseType = type.BaseType;
		TypeDefinitionBase typeDefinitionBase = null;
		while (baseType != null && baseType != typeof(object))
		{
			Type type2 = baseType;
			if (baseType.IsGenericType && !baseType.IsGenericTypeDefinition)
			{
				type2 = baseType.GetGenericTypeDefinition();
			}
			typeDefinitionBase = _definitionContext.GetTypeDefinition(type2);
			if (typeDefinitionBase != null)
			{
				break;
			}
			baseType = baseType.BaseType;
		}
		bool flag = false;
		if (typeDefinitionBase == null || typeDefinitionBase is BasicTypeDefinition)
		{
			return false;
		}
		return true;
	}

	private bool CheckIfTypeDefined(Type type)
	{
		Type type2 = type;
		if (type.IsGenericType && !type.IsGenericTypeDefinition)
		{
			type2 = type.GetGenericTypeDefinition();
		}
		TypeDefinitionBase typeDefinition = _definitionContext.GetTypeDefinition(type2);
		bool flag = false;
		if (typeDefinition == null || typeDefinition is BasicTypeDefinition)
		{
			return false;
		}
		return true;
	}

	private static bool IsBuitlinTypeByDotNet(Type type)
	{
		if (type.FullName.StartsWith("System."))
		{
			return true;
		}
		return false;
	}

	private bool CheckIfPrimitiveOrPrimiteHolderStruct(Type type)
	{
		bool flag = false;
		TypeDefinitionBase typeDefinition = _definitionContext.GetTypeDefinition(type);
		if (typeDefinition is BasicTypeDefinition)
		{
			flag = true;
		}
		else if (typeDefinition is EnumDefinition)
		{
			flag = true;
		}
		if (!flag && typeDefinition is TypeDefinition)
		{
			TypeDefinition typeDefinition2 = (TypeDefinition)typeDefinition;
			if (!typeDefinition2.IsClassDefinition && !CheckIfGotAnyNonPrimitiveMembers(typeDefinition2))
			{
				flag = true;
			}
		}
		return flag;
	}

	private int GetClassGenericInformation(string className)
	{
		if (className.Length > 2 && className[className.Length - 2] == '`')
		{
			return Convert.ToInt32(className[className.Length - 1].ToString());
		}
		return -1;
	}

	private void GenerateForClassOrStruct(TypeDefinition typeDefinition)
	{
		Type type = typeDefinition.Type;
		bool isClass = type.IsClass;
		bool flag = !isClass;
		bool flag2 = CheckIfBaseTypeDefind(type);
		string text = type.Namespace;
		string text2 = type.FullName.Replace('+', '.');
		string text3 = text2.Substring(text.Length + 1, text2.Length - text.Length - 1);
		text3 = text3.Replace('+', '.');
		string[] nestedClasses = GetNestedClasses(text3);
		NamespaceCode namespaceCode = _codeGenerationContext.FindOrCreateNamespace(text);
		string text4 = nestedClasses[^1];
		ClassCode classCode = null;
		for (int i = 0; i < nestedClasses.Length; i++)
		{
			string text5 = nestedClasses[i];
			ClassCode classCode2 = new ClassCode();
			classCode2.IsPartial = true;
			if (i + 1 == nestedClasses.Length)
			{
				classCode2.IsClass = isClass;
			}
			classCode2.AccessModifier = ClassCodeAccessModifier.DoNotMention;
			int classGenericInformation = GetClassGenericInformation(text5);
			if (classGenericInformation >= 0)
			{
				classCode2.IsGeneric = true;
				classCode2.GenericTypeCount = classGenericInformation;
				text5 = text5.Substring(0, text5.Length - 2);
			}
			classCode2.Name = text5;
			if (classCode != null)
			{
				classCode.AddNestedClass(classCode2);
			}
			else
			{
				namespaceCode.AddClass(classCode2);
			}
			classCode = classCode2;
		}
		TypeSaveId typeSaveId = (TypeSaveId)typeDefinition.SaveId;
		int delegateCount = _delegateCount;
		_delegateCount++;
		_managerMethod.AddLine("var typeDefinition" + delegateCount + " =  (global::TaleWorlds.SaveSystem.Definition.TypeDefinition)definitionContext.TryGetTypeDefinition(new global::TaleWorlds.SaveSystem.Definition.TypeSaveId(" + typeSaveId.Id + "));");
		if (!type.IsGenericTypeDefinition && !type.IsAbstract)
		{
			MethodCode methodCode = new MethodCode();
			methodCode.IsStatic = true;
			methodCode.AccessModifier = ((!flag) ? MethodCodeAccessModifier.Internal : MethodCodeAccessModifier.Public);
			methodCode.Name = "AutoGeneratedStaticCollectObjects" + text4;
			methodCode.MethodSignature = "(object o, global::System.Collections.Generic.List<object> collectedObjects)";
			methodCode.AddLine("var target = (global::" + text2 + ")o;");
			methodCode.AddLine("target.AutoGeneratedInstanceCollectObjects(collectedObjects);");
			classCode.AddMethod(methodCode);
			_managerMethod.AddLine("TaleWorlds.SaveSystem.Definition.CollectObjectsDelegate d" + delegateCount + " = global::" + text + "." + text3 + "." + methodCode.Name + ";");
			_managerMethod.AddLine("typeDefinition" + delegateCount + ".InitializeForAutoGeneration(d" + delegateCount + ");");
		}
		_managerMethod.AddLine("");
		MethodCode methodCode2 = new MethodCode();
		if (flag2)
		{
			methodCode2.PolymorphismInfo = MethodCodePolymorphismInfo.Override;
			methodCode2.AccessModifier = MethodCodeAccessModifier.Protected;
		}
		else if (!type.IsSealed)
		{
			methodCode2.PolymorphismInfo = MethodCodePolymorphismInfo.Virtual;
			methodCode2.AccessModifier = MethodCodeAccessModifier.Protected;
		}
		else
		{
			methodCode2.PolymorphismInfo = MethodCodePolymorphismInfo.None;
			methodCode2.AccessModifier = MethodCodeAccessModifier.Private;
		}
		methodCode2.Name = "AutoGeneratedInstanceCollectObjects";
		methodCode2.MethodSignature = "(global::System.Collections.Generic.List<object> collectedObjects)";
		if (flag2)
		{
			methodCode2.AddLine("base.AutoGeneratedInstanceCollectObjects(collectedObjects);");
			methodCode2.AddLine("");
		}
		foreach (MemberDefinition memberDefinition in typeDefinition.MemberDefinitions)
		{
			if (!(memberDefinition is FieldDefinition))
			{
				continue;
			}
			FieldInfo fieldInfo = (memberDefinition as FieldDefinition).FieldInfo;
			Type fieldType = fieldInfo.FieldType;
			string name = fieldInfo.Name;
			if (fieldType.IsClass || fieldType.IsInterface)
			{
				if (fieldType != typeof(string))
				{
					bool flag3 = false;
					Type declaringType = fieldInfo.DeclaringType;
					if (declaringType != type)
					{
						flag3 = CheckIfTypeDefined(declaringType);
					}
					string text6 = "";
					if (flag3)
					{
						text6 += "//";
					}
					text6 = text6 + "collectedObjects.Add(this." + name + ");";
					methodCode2.AddLine(text6);
				}
			}
			else if (!fieldType.IsClass && _definitionContext.GetStructDefinition(fieldType) != null)
			{
				string text7 = "";
				bool flag4 = false;
				Type declaringType2 = fieldInfo.DeclaringType;
				if (declaringType2 != type)
				{
					flag4 = CheckIfTypeDefined(declaringType2);
				}
				if (flag4)
				{
					text7 += "//";
				}
				string fullTypeName = GetFullTypeName(fieldType);
				string usableTypeName = GetUsableTypeName(fieldType);
				text7 = text7 + "global::" + fullTypeName + ".AutoGeneratedStaticCollectObjects" + usableTypeName + "(this." + name + ", collectedObjects);";
				methodCode2.AddLine(text7);
			}
		}
		methodCode2.AddLine("");
		foreach (MemberDefinition memberDefinition2 in typeDefinition.MemberDefinitions)
		{
			if (!(memberDefinition2 is PropertyDefinition))
			{
				continue;
			}
			PropertyDefinition obj = memberDefinition2 as PropertyDefinition;
			PropertyInfo propertyInfo = obj.PropertyInfo;
			Type propertyType = obj.PropertyInfo.PropertyType;
			string name2 = propertyInfo.Name;
			if (propertyType.IsClass || propertyType.IsInterface)
			{
				if (propertyType != typeof(string))
				{
					bool flag5 = false;
					Type declaringType3 = propertyInfo.DeclaringType;
					if (declaringType3 != type)
					{
						flag5 = CheckIfTypeDefined(declaringType3);
					}
					string text8 = "";
					if (flag5)
					{
						text8 += "//";
					}
					text8 = text8 + "collectedObjects.Add(this." + name2 + ");";
					methodCode2.AddLine(text8);
				}
			}
			else if (!propertyType.IsClass && _definitionContext.GetStructDefinition(propertyType) != null)
			{
				bool flag6 = false;
				Type declaringType4 = propertyInfo.DeclaringType;
				if (declaringType4 != type)
				{
					flag6 = CheckIfTypeDefined(declaringType4);
				}
				string text9 = "";
				if (flag6)
				{
					text9 += "//";
				}
				string fullTypeName2 = GetFullTypeName(propertyType);
				string usableTypeName2 = GetUsableTypeName(propertyType);
				text9 = text9 + "global::" + fullTypeName2 + ".AutoGeneratedStaticCollectObjects" + usableTypeName2 + "(this." + name2 + ", collectedObjects);";
				methodCode2.AddLine(text9);
			}
		}
		classCode.AddMethod(methodCode2);
		foreach (MemberDefinition memberDefinition3 in typeDefinition.MemberDefinitions)
		{
			if (type.IsGenericTypeDefinition)
			{
				continue;
			}
			MethodCode methodCode3 = new MethodCode();
			string text10 = "";
			Type type2 = null;
			if (memberDefinition3 is PropertyDefinition)
			{
				PropertyDefinition obj2 = memberDefinition3 as PropertyDefinition;
				text10 = obj2.PropertyInfo.Name;
				type2 = obj2.PropertyInfo.DeclaringType;
			}
			else if (memberDefinition3 is FieldDefinition)
			{
				FieldDefinition obj3 = memberDefinition3 as FieldDefinition;
				text10 = obj3.FieldInfo.Name;
				type2 = obj3.FieldInfo.DeclaringType;
			}
			bool flag7 = false;
			if (type2 != type)
			{
				flag7 = CheckIfTypeDefined(type2);
			}
			if (!flag7)
			{
				methodCode3.Name = "AutoGeneratedGetMemberValue" + text10;
				methodCode3.MethodSignature = "(object o)";
				methodCode3.IsStatic = true;
				methodCode3.AccessModifier = MethodCodeAccessModifier.Internal;
				methodCode3.PolymorphismInfo = MethodCodePolymorphismInfo.None;
				methodCode3.ReturnParameter = "object";
				methodCode3.AddLine("var target = (global::" + text2 + ")o;");
				methodCode3.AddLine("return (object)target." + text10 + ";");
				classCode.AddMethod(methodCode3);
				string text11 = "GetPropertyDefinitionWithId";
				if (memberDefinition3 is FieldDefinition)
				{
					text11 = "GetFieldDefinitionWithId";
				}
				_managerMethod.AddLine("{");
				_managerMethod.AddLine("var memberDefinition = typeDefinition" + delegateCount + "." + text11 + "(new global::TaleWorlds.SaveSystem.Definition.MemberTypeId(" + memberDefinition3.Id.TypeLevel + "," + memberDefinition3.Id.LocalSaveId + "));");
				string text12 = "global::" + text + "." + text3 + "." + methodCode3.Name;
				_managerMethod.AddLine("memberDefinition.InitializeForAutoGeneration(" + text12 + ");");
				_managerMethod.AddLine("}");
				_managerMethod.AddLine("");
			}
		}
	}

	private static string GetFullTypeName(Type type)
	{
		if (type.IsArray)
		{
			return GetFullTypeName(type.GetElementType()) + "[]";
		}
		if (type.IsGenericType)
		{
			string[] array = new string[type.GenericTypeArguments.Length];
			string text = "<";
			for (int i = 0; i < type.GenericTypeArguments.Length; i++)
			{
				string fullTypeName = GetFullTypeName(type.GenericTypeArguments[i]);
				text += fullTypeName;
				array[i] = fullTypeName;
				if (i + 1 < type.GenericTypeArguments.Length)
				{
					text += ",";
				}
			}
			Type genericTypeDefinition = type.GetGenericTypeDefinition();
			text += ">";
			string text2 = genericTypeDefinition.FullName.Replace('+', '.');
			text2 = text2.Substring(0, text2.Length - 2);
			return text2 + text;
		}
		return type.FullName.Replace('+', '.');
	}

	private static string GetUsableTypeName(Type type)
	{
		return type.Name.Replace('+', '.');
	}

	private void AddCustomCollectionByBuiltinTypes(CodeBlock codeBlock, Type elementType, string variableName)
	{
		codeBlock.AddLine("//custom code here - begins ");
		if (elementType.GetGenericTypeDefinition() == typeof(Tuple<, >))
		{
			Type elementType2 = elementType.GenericTypeArguments[0];
			Type elementType3 = elementType.GenericTypeArguments[1];
			codeBlock.AddLine("//Tuple here");
			codeBlock.AddLine("");
			codeBlock.AddLine("if (" + variableName + " != null)");
			codeBlock.AddLine("{");
			codeBlock.AddLine("collectedObjects.Add(" + variableName + ");");
			codeBlock.AddLine("");
			codeBlock.AddLine("var " + variableName + "_item1 = " + variableName + ".Item1;");
			codeBlock.AddLine("var " + variableName + "_item2 = " + variableName + ".Item2;");
			AddCodeForType(codeBlock, elementType2, variableName + "_item1");
			AddCodeForType(codeBlock, elementType3, variableName + "_item2");
			codeBlock.AddLine("}");
		}
		else if (elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<, >))
		{
			Type elementType4 = elementType.GenericTypeArguments[0];
			Type elementType5 = elementType.GenericTypeArguments[1];
			codeBlock.AddLine("//KeyValuePair here");
			codeBlock.AddLine("");
			codeBlock.AddLine("var " + variableName + "_key = " + variableName + ".Key;");
			codeBlock.AddLine("var " + variableName + "_value = " + variableName + ".Value;");
			AddCodeForType(codeBlock, elementType4, variableName + "_key");
			AddCodeForType(codeBlock, elementType5, variableName + "_value");
		}
	}

	private void AddCodeForType(CodeBlock codeBlock, Type elementType, string elementVariableName)
	{
		bool flag = CheckIfPrimitiveOrPrimiteHolderStruct(elementType);
		TypeDefinition structDefinition = _definitionContext.GetStructDefinition(elementType);
		bool flag2 = IsBuitlinTypeByDotNet(elementType);
		if (elementType != typeof(object) && !flag)
		{
			if (flag2)
			{
				codeBlock.AddLine("//Builtin type in dot net: " + elementType);
				AddCustomCollectionByBuiltinTypes(codeBlock, elementType, elementVariableName);
			}
			else if (structDefinition != null)
			{
				string fullTypeName = GetFullTypeName(elementType);
				string usableTypeName = GetUsableTypeName(elementType);
				codeBlock.AddLine("global::" + fullTypeName + ".AutoGeneratedStaticCollectObjects" + usableTypeName + "(" + elementVariableName + ", collectedObjects);");
			}
			else
			{
				codeBlock.AddLine("collectedObjects.Add(" + elementVariableName + ");");
			}
		}
	}

	private void GenerateForList(ContainerDefinition containerDefinition)
	{
		Type type = containerDefinition.Type;
		Type type2 = type.GetGenericArguments()[0];
		bool flag = CheckIfPrimitiveOrPrimiteHolderStruct(type2);
		if (type2 != typeof(object))
		{
			MethodCode methodCode = new MethodCode();
			methodCode.IsStatic = true;
			methodCode.AccessModifier = MethodCodeAccessModifier.Private;
			methodCode.Comment = "//" + type.FullName;
			methodCode.Name = "AutoGeneratedStaticCollectObjectsForList" + _containerNumber;
			methodCode.MethodSignature = "(object o, global::System.Collections.Generic.List<object> collectedObjects)";
			CodeBlock codeBlock = new CodeBlock();
			AddCodeForType(codeBlock, type2, "element");
			if (flag)
			{
				methodCode.AddLine("//Got no child, type: " + type.FullName);
			}
			else
			{
				string fullTypeName = GetFullTypeName(type2);
				methodCode.AddLine("var target = (global::System.Collections.IList)o;");
				methodCode.AddLine("");
				methodCode.AddLine("for (int i = 0; i < target.Count; i++)");
				methodCode.AddLine("{");
				methodCode.AddLine("var element = (" + fullTypeName + ")target[i];");
				methodCode.AddCodeBlock(codeBlock);
				methodCode.AddLine("}");
			}
			SaveId saveId = containerDefinition.SaveId;
			StringWriter stringWriter = new StringWriter();
			saveId.WriteTo(stringWriter);
			string text = (flag ? "true" : "false");
			_managerMethod.AddLine("var saveId" + _delegateCount + " = global::TaleWorlds.SaveSystem.Definition.SaveId.ReadSaveIdFrom(new global::TaleWorlds.Library.StringReader(\"" + stringWriter.Data + "\"));");
			_managerMethod.AddLine("var typeDefinition" + _delegateCount + " =  (global::TaleWorlds.SaveSystem.Definition.ContainerDefinition)definitionContext.TryGetTypeDefinition(saveId" + _delegateCount + ");");
			_managerMethod.AddLine("TaleWorlds.SaveSystem.Definition.CollectObjectsDelegate d" + _delegateCount + " = " + methodCode.Name + ";");
			_managerMethod.AddLine("typeDefinition" + _delegateCount + ".InitializeForAutoGeneration(d" + _delegateCount + ", " + text + ");");
			_managerMethod.AddLine("");
			_delegateCount++;
			_managerClass.AddMethod(methodCode);
			_containerNumber++;
		}
	}

	private void GenerateForArray(ContainerDefinition containerDefinition)
	{
		Type type = containerDefinition.Type;
		Type elementType = type.GetElementType();
		bool flag = CheckIfPrimitiveOrPrimiteHolderStruct(elementType);
		CodeBlock codeBlock = new CodeBlock();
		AddCodeForType(codeBlock, elementType, "element");
		if (elementType != typeof(object))
		{
			MethodCode methodCode = new MethodCode();
			methodCode.IsStatic = true;
			methodCode.AccessModifier = MethodCodeAccessModifier.Private;
			methodCode.Comment = "//" + type.FullName;
			methodCode.Name = "AutoGeneratedStaticCollectObjectsForArray" + _containerNumber;
			methodCode.MethodSignature = "(object o, global::System.Collections.Generic.List<object> collectedObjects)";
			if (flag)
			{
				methodCode.AddLine("//Got no child, type: " + type.FullName);
			}
			else
			{
				string fullTypeName = GetFullTypeName(type);
				methodCode.AddLine("//Builtin type in dot net: " + type.FullName);
				methodCode.AddLine("var target = (global::" + fullTypeName + ")o;");
				methodCode.AddLine("");
				methodCode.AddLine("for (int i = 0; i < target.Length; i++)");
				methodCode.AddLine("{");
				methodCode.AddLine("var element = target[i];");
				methodCode.AddLine("");
				methodCode.AddCodeBlock(codeBlock);
				methodCode.AddLine("}");
			}
			SaveId saveId = containerDefinition.SaveId;
			StringWriter stringWriter = new StringWriter();
			saveId.WriteTo(stringWriter);
			string text = (flag ? "true" : "false");
			_managerMethod.AddLine("var saveId" + _delegateCount + " = global::TaleWorlds.SaveSystem.Definition.SaveId.ReadSaveIdFrom(new global::TaleWorlds.Library.StringReader(\"" + stringWriter.Data + "\"));");
			_managerMethod.AddLine("var typeDefinition" + _delegateCount + " =  (global::TaleWorlds.SaveSystem.Definition.ContainerDefinition)definitionContext.TryGetTypeDefinition(saveId" + _delegateCount + ");");
			_managerMethod.AddLine("TaleWorlds.SaveSystem.Definition.CollectObjectsDelegate d" + _delegateCount + " = " + methodCode.Name + ";");
			_managerMethod.AddLine("typeDefinition" + _delegateCount + ".InitializeForAutoGeneration(d" + _delegateCount + ", " + text + ");");
			_managerMethod.AddLine("");
			_delegateCount++;
			_managerClass.AddMethod(methodCode);
			_containerNumber++;
		}
	}

	private void GenerateForQueue(ContainerDefinition containerDefinition)
	{
		Type type = containerDefinition.Type;
		Type type2 = type.GetGenericArguments()[0];
		if (CheckIfPrimitiveOrPrimiteHolderStruct(type2))
		{
			MethodCode methodCode = new MethodCode();
			methodCode.IsStatic = true;
			methodCode.AccessModifier = MethodCodeAccessModifier.Private;
			methodCode.Comment = "//" + type.FullName;
			methodCode.Name = "AutoGeneratedStaticCollectObjectsForQueue" + _containerNumber;
			methodCode.MethodSignature = "(object o, global::System.Collections.Generic.List<object> collectedObjects)";
			methodCode.AddLine("//Got no child, type: " + type.FullName);
			SaveId saveId = containerDefinition.SaveId;
			StringWriter stringWriter = new StringWriter();
			saveId.WriteTo(stringWriter);
			_managerMethod.AddLine("var saveId" + _delegateCount + " = global::TaleWorlds.SaveSystem.Definition.SaveId.ReadSaveIdFrom(new global::TaleWorlds.Library.StringReader(\"" + stringWriter.Data + "\"));");
			_managerMethod.AddLine("var typeDefinition" + _delegateCount + " =  (global::TaleWorlds.SaveSystem.Definition.ContainerDefinition)definitionContext.TryGetTypeDefinition(saveId" + _delegateCount + ");");
			_managerMethod.AddLine("TaleWorlds.SaveSystem.Definition.CollectObjectsDelegate d" + _delegateCount + " = " + methodCode.Name + ";");
			_managerMethod.AddLine("typeDefinition" + _delegateCount + ".InitializeForAutoGeneration(d" + _delegateCount + ", true);");
			_managerMethod.AddLine("");
			_delegateCount++;
			_managerClass.AddMethod(methodCode);
			_containerNumber++;
		}
	}

	private void GenerateForDictionary(ContainerDefinition containerDefinition)
	{
		Type type = containerDefinition.Type;
		Type type2 = type.GetGenericArguments()[0];
		Type type3 = type.GetGenericArguments()[1];
		bool num = CheckIfPrimitiveOrPrimiteHolderStruct(type2);
		bool flag = CheckIfPrimitiveOrPrimiteHolderStruct(type3);
		TypeDefinition structDefinition = _definitionContext.GetStructDefinition(type2);
		TypeDefinition structDefinition2 = _definitionContext.GetStructDefinition(type3);
		bool flag2 = num && flag;
		if ((!num && structDefinition == null) || (!flag && structDefinition2 == null) || !(type2 != typeof(object)) || !(type3 != typeof(object)))
		{
			return;
		}
		MethodCode methodCode = new MethodCode();
		methodCode.IsStatic = true;
		methodCode.AccessModifier = MethodCodeAccessModifier.Private;
		methodCode.Comment = "//" + type.FullName;
		methodCode.Name = "AutoGeneratedStaticCollectObjectsForDictionary" + _containerNumber;
		methodCode.MethodSignature = "(object o, global::System.Collections.Generic.List<object> collectedObjects)";
		if (flag2)
		{
			methodCode.AddLine("//Got no child, type: " + type.FullName);
		}
		else
		{
			methodCode.AddLine("var target = (global::System.Collections.IDictionary)o;");
			methodCode.AddLine("");
			if (structDefinition != null)
			{
				string fullTypeName = GetFullTypeName(type2);
				methodCode.AddLine("foreach (var key in target.Keys)");
				methodCode.AddLine("{");
				string usableTypeName = GetUsableTypeName(type2);
				methodCode.AddLine("global::" + fullTypeName + ".AutoGeneratedStaticCollectObjects" + usableTypeName + "(key, collectedObjects);");
				methodCode.AddLine("}");
			}
			methodCode.AddLine("");
			if (structDefinition2 != null)
			{
				string fullTypeName2 = GetFullTypeName(type3);
				methodCode.AddLine("foreach (var value in target.Values)");
				methodCode.AddLine("{");
				string usableTypeName2 = GetUsableTypeName(type3);
				methodCode.AddLine("global::" + fullTypeName2 + ".AutoGeneratedStaticCollectObjects" + usableTypeName2 + "(value, collectedObjects);");
				methodCode.AddLine("}");
			}
		}
		SaveId saveId = containerDefinition.SaveId;
		StringWriter stringWriter = new StringWriter();
		saveId.WriteTo(stringWriter);
		string text = (flag2 ? "true" : "false");
		_managerMethod.AddLine("var saveId" + _delegateCount + " = global::TaleWorlds.SaveSystem.Definition.SaveId.ReadSaveIdFrom(new global::TaleWorlds.Library.StringReader(\"" + stringWriter.Data + "\"));");
		_managerMethod.AddLine("var typeDefinition" + _delegateCount + " =  (global::TaleWorlds.SaveSystem.Definition.ContainerDefinition)definitionContext.TryGetTypeDefinition(saveId" + _delegateCount + ");");
		_managerMethod.AddLine("TaleWorlds.SaveSystem.Definition.CollectObjectsDelegate d" + _delegateCount + " = " + methodCode.Name + ";");
		_managerMethod.AddLine("typeDefinition" + _delegateCount + ".InitializeForAutoGeneration(d" + _delegateCount + ", " + text + ");");
		_managerMethod.AddLine("");
		_delegateCount++;
		_managerClass.AddMethod(methodCode);
		_containerNumber++;
	}

	public void Generate()
	{
		NamespaceCode namespaceCode = _codeGenerationContext.FindOrCreateNamespace(DefaultNamespace);
		ClassCode classCode = new ClassCode();
		classCode.AccessModifier = ClassCodeAccessModifier.Internal;
		classCode.Name = "AutoGeneratedSaveManager";
		classCode.AddInterface("global::TaleWorlds.SaveSystem.Definition.IAutoGeneratedSaveManager");
		MethodCode methodCode = new MethodCode
		{
			IsStatic = false,
			AccessModifier = MethodCodeAccessModifier.Public,
			Name = "Initialize",
			MethodSignature = "(global::TaleWorlds.SaveSystem.Definition.DefinitionContext definitionContext)"
		};
		classCode.AddMethod(methodCode);
		_managerMethod = methodCode;
		_managerClass = classCode;
		namespaceCode.AddClass(classCode);
		foreach (TypeDefinition definition in _definitions)
		{
			GenerateForClassOrStruct(definition);
		}
		foreach (TypeDefinition structDefinition in _structDefinitions)
		{
			GenerateForClassOrStruct(structDefinition);
		}
		foreach (ContainerDefinition containerDefinition in _containerDefinitions)
		{
			if (containerDefinition.Type.IsContainer(out var containerType))
			{
				switch (containerType)
				{
				case ContainerType.List:
				case ContainerType.CustomList:
				case ContainerType.CustomReadOnlyList:
					GenerateForList(containerDefinition);
					break;
				case ContainerType.Dictionary:
					GenerateForDictionary(containerDefinition);
					break;
				case ContainerType.Array:
					GenerateForArray(containerDefinition);
					break;
				case ContainerType.Queue:
					GenerateForQueue(containerDefinition);
					break;
				}
			}
		}
	}

	public string GenerateText()
	{
		CodeGenerationFile codeGenerationFile = new CodeGenerationFile();
		_codeGenerationContext.GenerateInto(codeGenerationFile);
		return codeGenerationFile.GenerateText();
	}

	internal void AddContainerDefinition(ContainerDefinition containerDefinition)
	{
		_containerDefinitions.Add(containerDefinition);
	}
}
