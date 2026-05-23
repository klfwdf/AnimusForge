using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000267 RID: 615
	internal class MetadataResolver : IMetadataResolver
	{
		// Token: 0x1700033B RID: 827
		// (get) Token: 0x06000DE9 RID: 3561 RVA: 0x0002DA37 File Offset: 0x0002BC37
		public IAssemblyResolver AssemblyResolver
		{
			get
			{
				return this.assembly_resolver;
			}
		}

		// Token: 0x06000DEA RID: 3562 RVA: 0x0002DA3F File Offset: 0x0002BC3F
		public MetadataResolver(IAssemblyResolver assemblyResolver)
		{
			if (assemblyResolver == null)
			{
				throw new ArgumentNullException("assemblyResolver");
			}
			this.assembly_resolver = assemblyResolver;
		}

		// Token: 0x06000DEB RID: 3563 RVA: 0x0002DA5C File Offset: 0x0002BC5C
		public virtual TypeDefinition Resolve(TypeReference type)
		{
			Mixin.CheckType(type);
			type = type.GetElementType();
			IMetadataScope scope = type.Scope;
			if (scope == null)
			{
				return null;
			}
			switch (scope.MetadataScopeType)
			{
			case MetadataScopeType.AssemblyNameReference:
			{
				AssemblyDefinition assembly = this.assembly_resolver.Resolve((AssemblyNameReference)scope);
				if (assembly == null)
				{
					return null;
				}
				return MetadataResolver.GetType(assembly.MainModule, type);
			}
			case MetadataScopeType.ModuleReference:
			{
				if (type.Module.Assembly == null)
				{
					return null;
				}
				Collection<ModuleDefinition> modules = type.Module.Assembly.Modules;
				ModuleReference module_ref = (ModuleReference)scope;
				for (int i = 0; i < modules.Count; i++)
				{
					ModuleDefinition netmodule = modules[i];
					if (netmodule.Name == module_ref.Name)
					{
						return MetadataResolver.GetType(netmodule, type);
					}
				}
				break;
			}
			case MetadataScopeType.ModuleDefinition:
				return MetadataResolver.GetType((ModuleDefinition)scope, type);
			}
			throw new NotSupportedException();
		}

		// Token: 0x06000DEC RID: 3564 RVA: 0x0002DB3C File Offset: 0x0002BD3C
		private static TypeDefinition GetType(ModuleDefinition module, TypeReference reference)
		{
			TypeDefinition type = MetadataResolver.GetTypeDefinition(module, reference);
			if (type != null)
			{
				return type;
			}
			if (!module.HasExportedTypes)
			{
				return null;
			}
			Collection<ExportedType> exported_types = module.ExportedTypes;
			for (int i = 0; i < exported_types.Count; i++)
			{
				ExportedType exported_type = exported_types[i];
				if (!(exported_type.Name != reference.Name) && !(exported_type.Namespace != reference.Namespace))
				{
					return exported_type.Resolve();
				}
			}
			return null;
		}

		// Token: 0x06000DED RID: 3565 RVA: 0x0002DBB0 File Offset: 0x0002BDB0
		private static TypeDefinition GetTypeDefinition(ModuleDefinition module, TypeReference type)
		{
			if (!type.IsNested)
			{
				return module.GetType(type.Namespace, type.Name);
			}
			TypeDefinition declaring_type = type.DeclaringType.Resolve();
			if (declaring_type == null)
			{
				return null;
			}
			return declaring_type.GetNestedType(type.TypeFullName());
		}

		// Token: 0x06000DEE RID: 3566 RVA: 0x0002DBF8 File Offset: 0x0002BDF8
		public virtual FieldDefinition Resolve(FieldReference field)
		{
			Mixin.CheckField(field);
			TypeDefinition type = this.Resolve(field.DeclaringType);
			if (type == null)
			{
				return null;
			}
			if (!type.HasFields)
			{
				return null;
			}
			return this.GetField(type, field);
		}

		// Token: 0x06000DEF RID: 3567 RVA: 0x0002DC30 File Offset: 0x0002BE30
		private FieldDefinition GetField(TypeDefinition type, FieldReference reference)
		{
			while (type != null)
			{
				FieldDefinition field = MetadataResolver.GetField(type.Fields, reference);
				if (field != null)
				{
					return field;
				}
				if (type.BaseType == null)
				{
					return null;
				}
				type = this.Resolve(type.BaseType);
			}
			return null;
		}

		// Token: 0x06000DF0 RID: 3568 RVA: 0x0002DC70 File Offset: 0x0002BE70
		private static FieldDefinition GetField(Collection<FieldDefinition> fields, FieldReference reference)
		{
			for (int i = 0; i < fields.Count; i++)
			{
				FieldDefinition field = fields[i];
				if (!(field.Name != reference.Name) && MetadataResolver.AreSame(field.FieldType, reference.FieldType))
				{
					return field;
				}
			}
			return null;
		}

		// Token: 0x06000DF1 RID: 3569 RVA: 0x0002DCC0 File Offset: 0x0002BEC0
		public virtual MethodDefinition Resolve(MethodReference method)
		{
			Mixin.CheckMethod(method);
			TypeDefinition type = this.Resolve(method.DeclaringType);
			if (type == null)
			{
				return null;
			}
			method = method.GetElementMethod();
			if (!type.HasMethods)
			{
				return null;
			}
			MethodDefinition definition = method as MethodDefinition;
			if (definition != null)
			{
				return definition;
			}
			return this.GetMethod(type, method);
		}

		// Token: 0x06000DF2 RID: 3570 RVA: 0x0002DD0C File Offset: 0x0002BF0C
		private MethodDefinition GetMethod(TypeDefinition type, MethodReference reference)
		{
			while (type != null)
			{
				MethodDefinition method = MetadataResolver.GetMethod(type.Methods, reference);
				if (method != null)
				{
					return method;
				}
				if (type.BaseType == null)
				{
					return null;
				}
				type = this.Resolve(type.BaseType);
			}
			return null;
		}

		// Token: 0x06000DF3 RID: 3571 RVA: 0x0002DD4C File Offset: 0x0002BF4C
		public static MethodDefinition GetMethod(Collection<MethodDefinition> methods, MethodReference reference)
		{
			for (int i = 0; i < methods.Count; i++)
			{
				MethodDefinition method = methods[i];
				if (!(method.Name != reference.Name) && method.HasGenericParameters == reference.HasGenericParameters && (!method.HasGenericParameters || method.GenericParameters.Count == reference.GenericParameters.Count) && MetadataResolver.AreSame(method.ReturnType, reference.ReturnType) && method.HasThis == reference.HasThis && method.IsVarArg() == reference.IsVarArg())
				{
					if (method.IsVarArg() && MetadataResolver.IsVarArgCallTo(method, reference))
					{
						return method;
					}
					if (method.HasParameters == reference.HasParameters)
					{
						if (!method.HasParameters && !reference.HasParameters)
						{
							return method;
						}
						if (MetadataResolver.AreSame(method.Parameters, reference.Parameters))
						{
							return method;
						}
					}
				}
			}
			return null;
		}

		// Token: 0x06000DF4 RID: 3572 RVA: 0x0002DE38 File Offset: 0x0002C038
		private static bool AreSame(Collection<ParameterDefinition> a, Collection<ParameterDefinition> b)
		{
			int count = a.Count;
			if (count != b.Count)
			{
				return false;
			}
			if (count == 0)
			{
				return true;
			}
			for (int i = 0; i < count; i++)
			{
				if (!MetadataResolver.AreSame(a[i].ParameterType, b[i].ParameterType))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000DF5 RID: 3573 RVA: 0x0002DE8C File Offset: 0x0002C08C
		private static bool IsVarArgCallTo(MethodDefinition method, MethodReference reference)
		{
			if (method.Parameters.Count >= reference.Parameters.Count)
			{
				return false;
			}
			if (reference.GetSentinelPosition() != method.Parameters.Count)
			{
				return false;
			}
			for (int i = 0; i < method.Parameters.Count; i++)
			{
				if (!MetadataResolver.AreSame(method.Parameters[i].ParameterType, reference.Parameters[i].ParameterType))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000DF6 RID: 3574 RVA: 0x0002DF0C File Offset: 0x0002C10C
		private static bool AreSame(TypeSpecification a, TypeSpecification b)
		{
			if (!MetadataResolver.AreSame(a.ElementType, b.ElementType))
			{
				return false;
			}
			if (a.IsGenericInstance)
			{
				return MetadataResolver.AreSame((GenericInstanceType)a, (GenericInstanceType)b);
			}
			if (a.IsRequiredModifier || a.IsOptionalModifier)
			{
				return MetadataResolver.AreSame((IModifierType)a, (IModifierType)b);
			}
			if (a.IsArray)
			{
				return MetadataResolver.AreSame((ArrayType)a, (ArrayType)b);
			}
			return !a.IsFunctionPointer || MetadataResolver.AreSame((FunctionPointerType)a, (FunctionPointerType)b);
		}

		// Token: 0x06000DF7 RID: 3575 RVA: 0x0002DFA0 File Offset: 0x0002C1A0
		private static bool AreSame(FunctionPointerType a, FunctionPointerType b)
		{
			return a.HasThis == b.HasThis && a.CallingConvention == b.CallingConvention && MetadataResolver.AreSame(a.ReturnType, b.ReturnType) && a.ContainsGenericParameter == b.ContainsGenericParameter && a.HasParameters == b.HasParameters && (!a.HasParameters || MetadataResolver.AreSame(a.Parameters, b.Parameters));
		}

		// Token: 0x06000DF8 RID: 3576 RVA: 0x0002E022 File Offset: 0x0002C222
		private static bool AreSame(ArrayType a, ArrayType b)
		{
			return a.Rank == b.Rank;
		}

		// Token: 0x06000DF9 RID: 3577 RVA: 0x0002E035 File Offset: 0x0002C235
		private static bool AreSame(IModifierType a, IModifierType b)
		{
			return MetadataResolver.AreSame(a.ModifierType, b.ModifierType);
		}

		// Token: 0x06000DFA RID: 3578 RVA: 0x0002E048 File Offset: 0x0002C248
		private static bool AreSame(GenericInstanceType a, GenericInstanceType b)
		{
			if (a.GenericArguments.Count != b.GenericArguments.Count)
			{
				return false;
			}
			for (int i = 0; i < a.GenericArguments.Count; i++)
			{
				if (!MetadataResolver.AreSame(a.GenericArguments[i], b.GenericArguments[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000DFB RID: 3579 RVA: 0x0002E0A7 File Offset: 0x0002C2A7
		private static bool AreSame(GenericParameter a, GenericParameter b)
		{
			return a.Position == b.Position;
		}

		// Token: 0x06000DFC RID: 3580 RVA: 0x0002E0B8 File Offset: 0x0002C2B8
		private static bool AreSame(TypeReference a, TypeReference b)
		{
			if (a == b)
			{
				return true;
			}
			if (a == null || b == null)
			{
				return false;
			}
			if (a.etype != b.etype)
			{
				return false;
			}
			if (a.IsGenericParameter)
			{
				return MetadataResolver.AreSame((GenericParameter)a, (GenericParameter)b);
			}
			if (a.IsTypeSpecification())
			{
				return MetadataResolver.AreSame((TypeSpecification)a, (TypeSpecification)b);
			}
			return !(a.Name != b.Name) && !(a.Namespace != b.Namespace) && MetadataResolver.AreSame(a.DeclaringType, b.DeclaringType);
		}

		// Token: 0x0400041C RID: 1052
		private readonly IAssemblyResolver assembly_resolver;
	}
}
