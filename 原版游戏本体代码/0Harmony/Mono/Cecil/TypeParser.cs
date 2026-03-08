using System;
using System.Text;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020002A7 RID: 679
	internal class TypeParser
	{
		// Token: 0x06001110 RID: 4368 RVA: 0x000331FA File Offset: 0x000313FA
		private TypeParser(string fullname)
		{
			this.fullname = fullname;
			this.length = fullname.Length;
		}

		// Token: 0x06001111 RID: 4369 RVA: 0x00033218 File Offset: 0x00031418
		private TypeParser.Type ParseType(bool fq_name)
		{
			TypeParser.Type type = new TypeParser.Type();
			type.type_fullname = this.ParsePart();
			type.nested_names = this.ParseNestedNames();
			if (TypeParser.TryGetArity(type))
			{
				type.generic_arguments = this.ParseGenericArguments(type.arity);
			}
			type.specs = this.ParseSpecs();
			if (fq_name)
			{
				type.assembly = this.ParseAssemblyName();
			}
			return type;
		}

		// Token: 0x06001112 RID: 4370 RVA: 0x0003327C File Offset: 0x0003147C
		private static bool TryGetArity(TypeParser.Type type)
		{
			int arity = 0;
			TypeParser.TryAddArity(type.type_fullname, ref arity);
			string[] nested_names = type.nested_names;
			if (!nested_names.IsNullOrEmpty<string>())
			{
				for (int i = 0; i < nested_names.Length; i++)
				{
					TypeParser.TryAddArity(nested_names[i], ref arity);
				}
			}
			type.arity = arity;
			return arity > 0;
		}

		// Token: 0x06001113 RID: 4371 RVA: 0x000332CC File Offset: 0x000314CC
		private static bool TryGetArity(string name, out int arity)
		{
			arity = 0;
			int index = name.LastIndexOf('`');
			return index != -1 && TypeParser.ParseInt32(name.Substring(index + 1), out arity);
		}

		// Token: 0x06001114 RID: 4372 RVA: 0x000332FA File Offset: 0x000314FA
		private static bool ParseInt32(string value, out int result)
		{
			return int.TryParse(value, out result);
		}

		// Token: 0x06001115 RID: 4373 RVA: 0x00033304 File Offset: 0x00031504
		private static void TryAddArity(string name, ref int arity)
		{
			int type_arity;
			if (!TypeParser.TryGetArity(name, out type_arity))
			{
				return;
			}
			arity += type_arity;
		}

		// Token: 0x06001116 RID: 4374 RVA: 0x00033324 File Offset: 0x00031524
		private string ParsePart()
		{
			StringBuilder part = new StringBuilder();
			while (this.position < this.length && !TypeParser.IsDelimiter(this.fullname[this.position]))
			{
				if (this.fullname[this.position] == '\\')
				{
					this.position++;
				}
				StringBuilder stringBuilder = part;
				string text = this.fullname;
				int num = this.position;
				this.position = num + 1;
				stringBuilder.Append(text[num]);
			}
			return part.ToString();
		}

		// Token: 0x06001117 RID: 4375 RVA: 0x000333AB File Offset: 0x000315AB
		private static bool IsDelimiter(char chr)
		{
			return "+,[]*&".IndexOf(chr) != -1;
		}

		// Token: 0x06001118 RID: 4376 RVA: 0x000333BE File Offset: 0x000315BE
		private void TryParseWhiteSpace()
		{
			while (this.position < this.length && char.IsWhiteSpace(this.fullname[this.position]))
			{
				this.position++;
			}
		}

		// Token: 0x06001119 RID: 4377 RVA: 0x000333F8 File Offset: 0x000315F8
		private string[] ParseNestedNames()
		{
			string[] nested_names = null;
			while (this.TryParse('+'))
			{
				TypeParser.Add<string>(ref nested_names, this.ParsePart());
			}
			return nested_names;
		}

		// Token: 0x0600111A RID: 4378 RVA: 0x00033421 File Offset: 0x00031621
		private bool TryParse(char chr)
		{
			if (this.position < this.length && this.fullname[this.position] == chr)
			{
				this.position++;
				return true;
			}
			return false;
		}

		// Token: 0x0600111B RID: 4379 RVA: 0x00033456 File Offset: 0x00031656
		private static void Add<T>(ref T[] array, T item)
		{
			array = array.Add(item);
		}

		// Token: 0x0600111C RID: 4380 RVA: 0x00033464 File Offset: 0x00031664
		private int[] ParseSpecs()
		{
			int[] specs = null;
			while (this.position < this.length)
			{
				char c = this.fullname[this.position];
				if (c != '&')
				{
					if (c != '*')
					{
						if (c != '[')
						{
							return specs;
						}
						this.position++;
						char c2 = this.fullname[this.position];
						if (c2 != '*')
						{
							if (c2 == ']')
							{
								this.position++;
								TypeParser.Add<int>(ref specs, -3);
							}
							else
							{
								int rank = 1;
								while (this.TryParse(','))
								{
									rank++;
								}
								TypeParser.Add<int>(ref specs, rank);
								this.TryParse(']');
							}
						}
						else
						{
							this.position++;
							TypeParser.Add<int>(ref specs, 1);
						}
					}
					else
					{
						this.position++;
						TypeParser.Add<int>(ref specs, -1);
					}
				}
				else
				{
					this.position++;
					TypeParser.Add<int>(ref specs, -2);
				}
			}
			return specs;
		}

		// Token: 0x0600111D RID: 4381 RVA: 0x0003356C File Offset: 0x0003176C
		private TypeParser.Type[] ParseGenericArguments(int arity)
		{
			TypeParser.Type[] generic_arguments = null;
			if (this.position == this.length || this.fullname[this.position] != '[')
			{
				return generic_arguments;
			}
			this.TryParse('[');
			for (int i = 0; i < arity; i++)
			{
				bool fq_argument = this.TryParse('[');
				TypeParser.Add<TypeParser.Type>(ref generic_arguments, this.ParseType(fq_argument));
				if (fq_argument)
				{
					this.TryParse(']');
				}
				this.TryParse(',');
				this.TryParseWhiteSpace();
			}
			this.TryParse(']');
			return generic_arguments;
		}

		// Token: 0x0600111E RID: 4382 RVA: 0x000335F4 File Offset: 0x000317F4
		private string ParseAssemblyName()
		{
			if (!this.TryParse(','))
			{
				return string.Empty;
			}
			this.TryParseWhiteSpace();
			int start = this.position;
			while (this.position < this.length)
			{
				char chr = this.fullname[this.position];
				if (chr == '[' || chr == ']')
				{
					break;
				}
				this.position++;
			}
			return this.fullname.Substring(start, this.position - start);
		}

		// Token: 0x0600111F RID: 4383 RVA: 0x0003366C File Offset: 0x0003186C
		public static TypeReference ParseType(ModuleDefinition module, string fullname, bool typeDefinitionOnly = false)
		{
			if (string.IsNullOrEmpty(fullname))
			{
				return null;
			}
			TypeParser parser = new TypeParser(fullname);
			return TypeParser.GetTypeReference(module, parser.ParseType(true), typeDefinitionOnly);
		}

		// Token: 0x06001120 RID: 4384 RVA: 0x00033698 File Offset: 0x00031898
		private static TypeReference GetTypeReference(ModuleDefinition module, TypeParser.Type type_info, bool type_def_only)
		{
			TypeReference type;
			if (!TypeParser.TryGetDefinition(module, type_info, out type))
			{
				if (type_def_only)
				{
					return null;
				}
				type = TypeParser.CreateReference(type_info, module, TypeParser.GetMetadataScope(module, type_info));
			}
			return TypeParser.CreateSpecs(type, type_info);
		}

		// Token: 0x06001121 RID: 4385 RVA: 0x000336CC File Offset: 0x000318CC
		private static TypeReference CreateSpecs(TypeReference type, TypeParser.Type type_info)
		{
			type = TypeParser.TryCreateGenericInstanceType(type, type_info);
			int[] specs = type_info.specs;
			if (specs.IsNullOrEmpty<int>())
			{
				return type;
			}
			for (int i = 0; i < specs.Length; i++)
			{
				switch (specs[i])
				{
				case -3:
					type = new ArrayType(type);
					break;
				case -2:
					type = new ByReferenceType(type);
					break;
				case -1:
					type = new PointerType(type);
					break;
				default:
				{
					ArrayType array = new ArrayType(type);
					array.Dimensions.Clear();
					for (int j = 0; j < specs[i]; j++)
					{
						array.Dimensions.Add(default(ArrayDimension));
					}
					type = array;
					break;
				}
				}
			}
			return type;
		}

		// Token: 0x06001122 RID: 4386 RVA: 0x00033778 File Offset: 0x00031978
		private static TypeReference TryCreateGenericInstanceType(TypeReference type, TypeParser.Type type_info)
		{
			TypeParser.Type[] generic_arguments = type_info.generic_arguments;
			if (generic_arguments.IsNullOrEmpty<TypeParser.Type>())
			{
				return type;
			}
			GenericInstanceType instance = new GenericInstanceType(type, generic_arguments.Length);
			Collection<TypeReference> instance_arguments = instance.GenericArguments;
			for (int i = 0; i < generic_arguments.Length; i++)
			{
				instance_arguments.Add(TypeParser.GetTypeReference(type.Module, generic_arguments[i], false));
			}
			return instance;
		}

		// Token: 0x06001123 RID: 4387 RVA: 0x000337CC File Offset: 0x000319CC
		public static void SplitFullName(string fullname, out string @namespace, out string name)
		{
			int last_dot = fullname.LastIndexOf('.');
			if (last_dot == -1)
			{
				@namespace = string.Empty;
				name = fullname;
				return;
			}
			@namespace = fullname.Substring(0, last_dot);
			name = fullname.Substring(last_dot + 1);
		}

		// Token: 0x06001124 RID: 4388 RVA: 0x00033808 File Offset: 0x00031A08
		private static TypeReference CreateReference(TypeParser.Type type_info, ModuleDefinition module, IMetadataScope scope)
		{
			string @namespace;
			string name;
			TypeParser.SplitFullName(type_info.type_fullname, out @namespace, out name);
			TypeReference type = new TypeReference(@namespace, name, module, scope);
			MetadataSystem.TryProcessPrimitiveTypeReference(type);
			TypeParser.AdjustGenericParameters(type);
			string[] nested_names = type_info.nested_names;
			if (nested_names.IsNullOrEmpty<string>())
			{
				return type;
			}
			for (int i = 0; i < nested_names.Length; i++)
			{
				type = new TypeReference(string.Empty, nested_names[i], module, null)
				{
					DeclaringType = type
				};
				TypeParser.AdjustGenericParameters(type);
			}
			return type;
		}

		// Token: 0x06001125 RID: 4389 RVA: 0x0003387C File Offset: 0x00031A7C
		private static void AdjustGenericParameters(TypeReference type)
		{
			int arity;
			if (!TypeParser.TryGetArity(type.Name, out arity))
			{
				return;
			}
			for (int i = 0; i < arity; i++)
			{
				type.GenericParameters.Add(new GenericParameter(type));
			}
		}

		// Token: 0x06001126 RID: 4390 RVA: 0x000338B8 File Offset: 0x00031AB8
		private static IMetadataScope GetMetadataScope(ModuleDefinition module, TypeParser.Type type_info)
		{
			if (string.IsNullOrEmpty(type_info.assembly))
			{
				return module.TypeSystem.CoreLibrary;
			}
			AssemblyNameReference reference = AssemblyNameReference.Parse(type_info.assembly);
			AssemblyNameReference match;
			if (!module.TryGetAssemblyNameReference(reference, out match))
			{
				return reference;
			}
			return match;
		}

		// Token: 0x06001127 RID: 4391 RVA: 0x000338F8 File Offset: 0x00031AF8
		private static bool TryGetDefinition(ModuleDefinition module, TypeParser.Type type_info, out TypeReference type)
		{
			type = null;
			if (!TypeParser.TryCurrentModule(module, type_info))
			{
				return false;
			}
			TypeDefinition typedef = module.GetType(type_info.type_fullname);
			if (typedef == null)
			{
				return false;
			}
			string[] nested_names = type_info.nested_names;
			if (!nested_names.IsNullOrEmpty<string>())
			{
				for (int i = 0; i < nested_names.Length; i++)
				{
					TypeDefinition nested_type = typedef.GetNestedType(nested_names[i]);
					if (nested_type == null)
					{
						return false;
					}
					typedef = nested_type;
				}
			}
			type = typedef;
			return true;
		}

		// Token: 0x06001128 RID: 4392 RVA: 0x00033957 File Offset: 0x00031B57
		private static bool TryCurrentModule(ModuleDefinition module, TypeParser.Type type_info)
		{
			return string.IsNullOrEmpty(type_info.assembly) || (module.assembly != null && module.assembly.Name.FullName == type_info.assembly);
		}

		// Token: 0x06001129 RID: 4393 RVA: 0x00033990 File Offset: 0x00031B90
		public static string ToParseable(TypeReference type, bool top_level = true)
		{
			if (type == null)
			{
				return null;
			}
			StringBuilder name = new StringBuilder();
			TypeParser.AppendType(type, name, true, top_level);
			return name.ToString();
		}

		// Token: 0x0600112A RID: 4394 RVA: 0x000339B8 File Offset: 0x00031BB8
		private static void AppendNamePart(string part, StringBuilder name)
		{
			foreach (char c in part)
			{
				if (TypeParser.IsDelimiter(c))
				{
					name.Append('\\');
				}
				name.Append(c);
			}
		}

		// Token: 0x0600112B RID: 4395 RVA: 0x000339FC File Offset: 0x00031BFC
		private static void AppendType(TypeReference type, StringBuilder name, bool fq_name, bool top_level)
		{
			TypeReference elementType = type.GetElementType();
			TypeReference declaring_type = elementType.DeclaringType;
			if (declaring_type != null)
			{
				TypeParser.AppendType(declaring_type, name, false, top_level);
				name.Append('+');
			}
			string @namespace = type.Namespace;
			if (!string.IsNullOrEmpty(@namespace))
			{
				TypeParser.AppendNamePart(@namespace, name);
				name.Append('.');
			}
			TypeParser.AppendNamePart(elementType.Name, name);
			if (!fq_name)
			{
				return;
			}
			if (type.IsTypeSpecification())
			{
				TypeParser.AppendTypeSpecification((TypeSpecification)type, name);
			}
			if (TypeParser.RequiresFullyQualifiedName(type, top_level))
			{
				name.Append(", ");
				name.Append(TypeParser.GetScopeFullName(type));
			}
		}

		// Token: 0x0600112C RID: 4396 RVA: 0x00033A90 File Offset: 0x00031C90
		private static string GetScopeFullName(TypeReference type)
		{
			IMetadataScope scope = type.Scope;
			MetadataScopeType metadataScopeType = scope.MetadataScopeType;
			if (metadataScopeType == MetadataScopeType.AssemblyNameReference)
			{
				return ((AssemblyNameReference)scope).FullName;
			}
			if (metadataScopeType != MetadataScopeType.ModuleDefinition)
			{
				throw new ArgumentException();
			}
			return ((ModuleDefinition)scope).Assembly.Name.FullName;
		}

		// Token: 0x0600112D RID: 4397 RVA: 0x00033ADC File Offset: 0x00031CDC
		private static void AppendTypeSpecification(TypeSpecification type, StringBuilder name)
		{
			if (type.ElementType.IsTypeSpecification())
			{
				TypeParser.AppendTypeSpecification((TypeSpecification)type.ElementType, name);
			}
			ElementType etype = type.etype;
			switch (etype)
			{
			case ElementType.Ptr:
				name.Append('*');
				return;
			case ElementType.ByRef:
				name.Append('&');
				return;
			case ElementType.ValueType:
			case ElementType.Class:
			case ElementType.Var:
				return;
			case ElementType.Array:
				break;
			case ElementType.GenericInst:
			{
				Collection<TypeReference> arguments = ((GenericInstanceType)type).GenericArguments;
				name.Append('[');
				for (int i = 0; i < arguments.Count; i++)
				{
					if (i > 0)
					{
						name.Append(',');
					}
					TypeReference argument = arguments[i];
					bool flag = argument.Scope != argument.Module;
					if (flag)
					{
						name.Append('[');
					}
					TypeParser.AppendType(argument, name, true, false);
					if (flag)
					{
						name.Append(']');
					}
				}
				name.Append(']');
				return;
			}
			default:
				if (etype != ElementType.SzArray)
				{
					return;
				}
				break;
			}
			ArrayType array = (ArrayType)type;
			if (array.IsVector)
			{
				name.Append("[]");
				return;
			}
			name.Append('[');
			for (int j = 1; j < array.Rank; j++)
			{
				name.Append(',');
			}
			name.Append(']');
		}

		// Token: 0x0600112E RID: 4398 RVA: 0x00033C16 File Offset: 0x00031E16
		private static bool RequiresFullyQualifiedName(TypeReference type, bool top_level)
		{
			return type.Scope != type.Module && (!(type.Scope.Name == "mscorlib") || !top_level);
		}

		// Token: 0x040005FD RID: 1533
		private readonly string fullname;

		// Token: 0x040005FE RID: 1534
		private readonly int length;

		// Token: 0x040005FF RID: 1535
		private int position;

		// Token: 0x020002A8 RID: 680
		private class Type
		{
			// Token: 0x04000600 RID: 1536
			public const int Ptr = -1;

			// Token: 0x04000601 RID: 1537
			public const int ByRef = -2;

			// Token: 0x04000602 RID: 1538
			public const int SzArray = -3;

			// Token: 0x04000603 RID: 1539
			public string type_fullname;

			// Token: 0x04000604 RID: 1540
			public string[] nested_names;

			// Token: 0x04000605 RID: 1541
			public int arity;

			// Token: 0x04000606 RID: 1542
			public int[] specs;

			// Token: 0x04000607 RID: 1543
			public TypeParser.Type[] generic_arguments;

			// Token: 0x04000608 RID: 1544
			public string assembly;
		}
	}
}
