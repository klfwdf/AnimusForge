using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem.Definition;
using TaleWorlds.SaveSystem.Resolvers;

namespace TaleWorlds.SaveSystem;

public abstract class SaveableTypeDefiner
{
	private DefinitionContext _definitionContext;

	private readonly int _saveBaseId;

	protected SaveableTypeDefiner(int saveBaseId)
	{
		_saveBaseId = saveBaseId;
	}

	internal void Initialize(DefinitionContext definitionContext)
	{
		_definitionContext = definitionContext;
	}

	protected internal virtual void DefineBasicTypes()
	{
	}

	protected internal virtual void DefineClassTypes()
	{
	}

	protected internal virtual void DefineConflictResolvers()
	{
	}

	protected internal virtual void DefineStructTypes()
	{
	}

	protected internal virtual void DefineInterfaceTypes()
	{
	}

	protected internal virtual void DefineEnumTypes()
	{
	}

	protected internal virtual void DefineRootClassTypes()
	{
	}

	protected internal virtual void DefineGenericClassDefinitions()
	{
	}

	protected internal virtual void DefineGenericStructDefinitions()
	{
	}

	protected internal virtual void DefineContainerDefinitions()
	{
	}

	protected void ConstructGenericClassDefinition(Type type)
	{
		_definitionContext.ConstructGenericClassDefinition(type);
	}

	protected void ConstructGenericStructDefinition(Type type)
	{
		_definitionContext.ConstructGenericStructDefinition(type);
	}

	protected void AddBasicTypeDefinition(Type type, int saveId, IBasicTypeSerializer serializer)
	{
		BasicTypeDefinition basicTypeDefinition = new BasicTypeDefinition(type, _saveBaseId + saveId, serializer);
		_definitionContext.AddBasicTypeDefinition(basicTypeDefinition);
	}

	protected void AddConflictResolver(int saveId, IConflictResolver conflictResolver)
	{
		_definitionContext.AddConflictResolver(new TypeSaveId(_saveBaseId + saveId), conflictResolver);
	}

	protected void AddClassDefinition(Type type, int saveId, IObjectResolver resolver = null)
	{
		TypeDefinition classDefinition = new TypeDefinition(type, _saveBaseId + saveId, resolver);
		_definitionContext.AddClassDefinition(classDefinition);
	}

	protected void AddClassDefinitionWithCustomFields(Type type, int saveId, IEnumerable<Tuple<string, short>> fields, IObjectResolver resolver = null)
	{
		TypeDefinition typeDefinition = new TypeDefinition(type, _saveBaseId + saveId, resolver);
		_definitionContext.AddClassDefinition(typeDefinition);
		foreach (Tuple<string, short> field in fields)
		{
			typeDefinition.AddCustomField(field.Item1, field.Item2);
		}
	}

	protected void AddStructDefinitionWithCustomFields(Type type, int saveId, IEnumerable<Tuple<string, short>> fields, IObjectResolver resolver = null)
	{
		StructDefinition structDefinition = new StructDefinition(type, _saveBaseId + saveId, resolver);
		_definitionContext.AddStructDefinition(structDefinition);
		foreach (Tuple<string, short> field in fields)
		{
			structDefinition.AddCustomField(field.Item1, field.Item2);
		}
	}

	protected void AddRootClassDefinition(Type type, int saveId, IObjectResolver resolver = null)
	{
		TypeDefinition rootClassDefinition = new TypeDefinition(type, _saveBaseId + saveId, resolver);
		_definitionContext.AddRootClassDefinition(rootClassDefinition);
	}

	protected void AddStructDefinition(Type type, int saveId, IObjectResolver resolver = null)
	{
		StructDefinition structDefinition = new StructDefinition(type, _saveBaseId + saveId, resolver);
		_definitionContext.AddStructDefinition(structDefinition);
	}

	protected void AddInterfaceDefinition(Type type, int saveId)
	{
		InterfaceDefinition interfaceDefinition = new InterfaceDefinition(type, _saveBaseId + saveId);
		_definitionContext.AddInterfaceDefinition(interfaceDefinition);
	}

	protected void AddEnumDefinition(Type type, int saveId, IEnumResolver enumResolver = null)
	{
		EnumDefinition enumDefinition = new EnumDefinition(type, _saveBaseId + saveId, enumResolver);
		_definitionContext.AddEnumDefinition(enumDefinition);
	}

	protected void ConstructContainerDefinition(Type type)
	{
		if (!_definitionContext.HasDefinition(type))
		{
			Assembly assembly = GetType().Assembly;
			_definitionContext.ConstructContainerDefinition(type, assembly);
		}
		else
		{
			Debug.FailedAssert($"There is duplicate definition for {type}", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\SaveableTypeDefiner.cs", "ConstructContainerDefinition", 176);
		}
	}
}
