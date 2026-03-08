using System;
using Mono.Cecil.Cil;
using Mono.Cecil.PE;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020001EE RID: 494
	internal sealed class ImmediateModuleReader : ModuleReader
	{
		// Token: 0x060009AC RID: 2476 RVA: 0x0001F3A4 File Offset: 0x0001D5A4
		public ImmediateModuleReader(Image image)
			: base(image, ReadingMode.Immediate)
		{
		}

		// Token: 0x060009AD RID: 2477 RVA: 0x0001F3AE File Offset: 0x0001D5AE
		protected override void ReadModule()
		{
			this.module.Read<ModuleDefinition>(this.module, delegate(ModuleDefinition module, MetadataReader reader)
			{
				base.ReadModuleManifest(reader);
				this.ReadModule(module, true);
			});
		}

		// Token: 0x060009AE RID: 2478 RVA: 0x0001F3D0 File Offset: 0x0001D5D0
		public void ReadModule(ModuleDefinition module, bool resolve_attributes)
		{
			this.resolve_attributes = resolve_attributes;
			if (module.HasAssemblyReferences)
			{
				Mixin.Read(module.AssemblyReferences);
			}
			if (module.HasResources)
			{
				Mixin.Read(module.Resources);
			}
			if (module.HasModuleReferences)
			{
				Mixin.Read(module.ModuleReferences);
			}
			if (module.HasTypes)
			{
				this.ReadTypes(module.Types);
			}
			if (module.HasExportedTypes)
			{
				Mixin.Read(module.ExportedTypes);
			}
			this.ReadCustomAttributes(module);
			AssemblyDefinition assembly = module.Assembly;
			if (module.kind == ModuleKind.NetModule || assembly == null)
			{
				return;
			}
			this.ReadCustomAttributes(assembly);
			this.ReadSecurityDeclarations(assembly);
		}

		// Token: 0x060009AF RID: 2479 RVA: 0x0001F470 File Offset: 0x0001D670
		private void ReadTypes(Collection<TypeDefinition> types)
		{
			for (int i = 0; i < types.Count; i++)
			{
				this.ReadType(types[i]);
			}
		}

		// Token: 0x060009B0 RID: 2480 RVA: 0x0001F49C File Offset: 0x0001D69C
		private void ReadType(TypeDefinition type)
		{
			this.ReadGenericParameters(type);
			if (type.HasInterfaces)
			{
				this.ReadInterfaces(type);
			}
			if (type.HasNestedTypes)
			{
				this.ReadTypes(type.NestedTypes);
			}
			if (type.HasLayoutInfo)
			{
				Mixin.Read(type.ClassSize);
			}
			if (type.HasFields)
			{
				this.ReadFields(type);
			}
			if (type.HasMethods)
			{
				this.ReadMethods(type);
			}
			if (type.HasProperties)
			{
				this.ReadProperties(type);
			}
			if (type.HasEvents)
			{
				this.ReadEvents(type);
			}
			this.ReadSecurityDeclarations(type);
			this.ReadCustomAttributes(type);
		}

		// Token: 0x060009B1 RID: 2481 RVA: 0x0001F538 File Offset: 0x0001D738
		private void ReadInterfaces(TypeDefinition type)
		{
			Collection<InterfaceImplementation> interfaces = type.Interfaces;
			for (int i = 0; i < interfaces.Count; i++)
			{
				this.ReadCustomAttributes(interfaces[i]);
			}
		}

		// Token: 0x060009B2 RID: 2482 RVA: 0x0001F56C File Offset: 0x0001D76C
		private void ReadGenericParameters(IGenericParameterProvider provider)
		{
			if (!provider.HasGenericParameters)
			{
				return;
			}
			Collection<GenericParameter> parameters = provider.GenericParameters;
			for (int i = 0; i < parameters.Count; i++)
			{
				GenericParameter parameter = parameters[i];
				if (parameter.HasConstraints)
				{
					this.ReadGenericParameterConstraints(parameter);
				}
				this.ReadCustomAttributes(parameter);
			}
		}

		// Token: 0x060009B3 RID: 2483 RVA: 0x0001F5B8 File Offset: 0x0001D7B8
		private void ReadGenericParameterConstraints(GenericParameter parameter)
		{
			Collection<GenericParameterConstraint> constraints = parameter.Constraints;
			for (int i = 0; i < constraints.Count; i++)
			{
				this.ReadCustomAttributes(constraints[i]);
			}
		}

		// Token: 0x060009B4 RID: 2484 RVA: 0x0001F5EC File Offset: 0x0001D7EC
		private void ReadSecurityDeclarations(ISecurityDeclarationProvider provider)
		{
			if (!provider.HasSecurityDeclarations)
			{
				return;
			}
			Collection<SecurityDeclaration> security_declarations = provider.SecurityDeclarations;
			if (!this.resolve_attributes)
			{
				return;
			}
			for (int i = 0; i < security_declarations.Count; i++)
			{
				Mixin.Read(security_declarations[i].SecurityAttributes);
			}
		}

		// Token: 0x060009B5 RID: 2485 RVA: 0x0001F634 File Offset: 0x0001D834
		private void ReadCustomAttributes(ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
			{
				return;
			}
			Collection<CustomAttribute> custom_attributes = provider.CustomAttributes;
			if (!this.resolve_attributes)
			{
				return;
			}
			for (int i = 0; i < custom_attributes.Count; i++)
			{
				Mixin.Read(custom_attributes[i].ConstructorArguments);
			}
		}

		// Token: 0x060009B6 RID: 2486 RVA: 0x0001F67C File Offset: 0x0001D87C
		private void ReadFields(TypeDefinition type)
		{
			Collection<FieldDefinition> fields = type.Fields;
			for (int i = 0; i < fields.Count; i++)
			{
				FieldDefinition field = fields[i];
				if (field.HasConstant)
				{
					Mixin.Read(field.Constant);
				}
				if (field.HasLayoutInfo)
				{
					Mixin.Read(field.Offset);
				}
				if (field.RVA > 0)
				{
					Mixin.Read(field.InitialValue);
				}
				if (field.HasMarshalInfo)
				{
					Mixin.Read(field.MarshalInfo);
				}
				this.ReadCustomAttributes(field);
			}
		}

		// Token: 0x060009B7 RID: 2487 RVA: 0x0001F704 File Offset: 0x0001D904
		private void ReadMethods(TypeDefinition type)
		{
			Collection<MethodDefinition> methods = type.Methods;
			for (int i = 0; i < methods.Count; i++)
			{
				MethodDefinition method = methods[i];
				this.ReadGenericParameters(method);
				if (method.HasParameters)
				{
					this.ReadParameters(method);
				}
				if (method.HasOverrides)
				{
					Mixin.Read(method.Overrides);
				}
				if (method.IsPInvokeImpl)
				{
					Mixin.Read(method.PInvokeInfo);
				}
				this.ReadSecurityDeclarations(method);
				this.ReadCustomAttributes(method);
				MethodReturnType return_type = method.MethodReturnType;
				if (return_type.HasConstant)
				{
					Mixin.Read(return_type.Constant);
				}
				if (return_type.HasMarshalInfo)
				{
					Mixin.Read(return_type.MarshalInfo);
				}
				this.ReadCustomAttributes(return_type);
			}
		}

		// Token: 0x060009B8 RID: 2488 RVA: 0x0001F7B8 File Offset: 0x0001D9B8
		private void ReadParameters(MethodDefinition method)
		{
			Collection<ParameterDefinition> parameters = method.Parameters;
			for (int i = 0; i < parameters.Count; i++)
			{
				ParameterDefinition parameter = parameters[i];
				if (parameter.HasConstant)
				{
					Mixin.Read(parameter.Constant);
				}
				if (parameter.HasMarshalInfo)
				{
					Mixin.Read(parameter.MarshalInfo);
				}
				this.ReadCustomAttributes(parameter);
			}
		}

		// Token: 0x060009B9 RID: 2489 RVA: 0x0001F814 File Offset: 0x0001DA14
		private void ReadProperties(TypeDefinition type)
		{
			Collection<PropertyDefinition> properties = type.Properties;
			for (int i = 0; i < properties.Count; i++)
			{
				PropertyDefinition property = properties[i];
				Mixin.Read(property.GetMethod);
				if (property.HasConstant)
				{
					Mixin.Read(property.Constant);
				}
				this.ReadCustomAttributes(property);
			}
		}

		// Token: 0x060009BA RID: 2490 RVA: 0x0001F868 File Offset: 0x0001DA68
		private void ReadEvents(TypeDefinition type)
		{
			Collection<EventDefinition> events = type.Events;
			for (int i = 0; i < events.Count; i++)
			{
				EventDefinition @event = events[i];
				Mixin.Read(@event.AddMethod);
				this.ReadCustomAttributes(@event);
			}
		}

		// Token: 0x060009BB RID: 2491 RVA: 0x0001F8A7 File Offset: 0x0001DAA7
		public override void ReadSymbols(ModuleDefinition module)
		{
			if (module.symbol_reader == null)
			{
				return;
			}
			this.ReadTypesSymbols(module.Types, module.symbol_reader);
		}

		// Token: 0x060009BC RID: 2492 RVA: 0x0001F8C4 File Offset: 0x0001DAC4
		private void ReadTypesSymbols(Collection<TypeDefinition> types, ISymbolReader symbol_reader)
		{
			for (int i = 0; i < types.Count; i++)
			{
				TypeDefinition type = types[i];
				type.custom_infos = symbol_reader.Read(type);
				if (type.HasNestedTypes)
				{
					this.ReadTypesSymbols(type.NestedTypes, symbol_reader);
				}
				if (type.HasMethods)
				{
					this.ReadMethodsSymbols(type, symbol_reader);
				}
			}
		}

		// Token: 0x060009BD RID: 2493 RVA: 0x0001F91C File Offset: 0x0001DB1C
		private void ReadMethodsSymbols(TypeDefinition type, ISymbolReader symbol_reader)
		{
			Collection<MethodDefinition> methods = type.Methods;
			for (int i = 0; i < methods.Count; i++)
			{
				MethodDefinition method = methods[i];
				if (method.HasBody && method.token.RID != 0U && method.debug_info == null)
				{
					method.debug_info = symbol_reader.Read(method);
				}
			}
		}

		// Token: 0x0400033C RID: 828
		private bool resolve_attributes;
	}
}
