using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mono.Cecil.Rocks
{
	// Token: 0x02000449 RID: 1097
	internal class DocCommentId
	{
		// Token: 0x060017BF RID: 6079 RVA: 0x0004AD9D File Offset: 0x00048F9D
		private DocCommentId(IMemberDefinition member)
		{
			this.commentMember = member;
			this.id = new StringBuilder();
		}

		// Token: 0x060017C0 RID: 6080 RVA: 0x0004ADB7 File Offset: 0x00048FB7
		private void WriteField(FieldDefinition field)
		{
			this.WriteDefinition('F', field);
		}

		// Token: 0x060017C1 RID: 6081 RVA: 0x0004ADC2 File Offset: 0x00048FC2
		private void WriteEvent(EventDefinition @event)
		{
			this.WriteDefinition('E', @event);
		}

		// Token: 0x060017C2 RID: 6082 RVA: 0x0004ADCD File Offset: 0x00048FCD
		private void WriteType(TypeDefinition type)
		{
			this.id.Append('T').Append(':');
			this.WriteTypeFullName(type);
		}

		// Token: 0x060017C3 RID: 6083 RVA: 0x0004ADEC File Offset: 0x00048FEC
		private void WriteMethod(MethodDefinition method)
		{
			this.WriteDefinition('M', method);
			if (method.HasGenericParameters)
			{
				this.id.Append('`').Append('`');
				this.id.Append(method.GenericParameters.Count);
			}
			if (method.HasParameters)
			{
				this.WriteParameters(method.Parameters);
			}
			if (DocCommentId.IsConversionOperator(method))
			{
				this.WriteReturnType(method);
			}
		}

		// Token: 0x060017C4 RID: 6084 RVA: 0x0004AE59 File Offset: 0x00049059
		private static bool IsConversionOperator(MethodDefinition self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			return self.IsSpecialName && (self.Name == "op_Explicit" || self.Name == "op_Implicit");
		}

		// Token: 0x060017C5 RID: 6085 RVA: 0x0004AE97 File Offset: 0x00049097
		private void WriteReturnType(MethodDefinition method)
		{
			this.id.Append('~');
			this.WriteTypeSignature(method.ReturnType);
		}

		// Token: 0x060017C6 RID: 6086 RVA: 0x0004AEB3 File Offset: 0x000490B3
		private void WriteProperty(PropertyDefinition property)
		{
			this.WriteDefinition('P', property);
			if (property.HasParameters)
			{
				this.WriteParameters(property.Parameters);
			}
		}

		// Token: 0x060017C7 RID: 6087 RVA: 0x0004AED2 File Offset: 0x000490D2
		private void WriteParameters(IList<ParameterDefinition> parameters)
		{
			this.id.Append('(');
			this.WriteList<ParameterDefinition>(parameters, delegate(ParameterDefinition p)
			{
				this.WriteTypeSignature(p.ParameterType);
			});
			this.id.Append(')');
		}

		// Token: 0x060017C8 RID: 6088 RVA: 0x0004AF04 File Offset: 0x00049104
		private void WriteTypeSignature(TypeReference type)
		{
			MetadataType metadataType = type.MetadataType;
			switch (metadataType)
			{
			case MetadataType.Pointer:
				this.WriteTypeSignature(((PointerType)type).ElementType);
				this.id.Append('*');
				return;
			case MetadataType.ByReference:
				this.WriteTypeSignature(((ByReferenceType)type).ElementType);
				this.id.Append('@');
				return;
			case MetadataType.ValueType:
			case MetadataType.Class:
				break;
			case MetadataType.Var:
				if (this.IsGenericMethodTypeParameter(type))
				{
					this.id.Append('`');
				}
				this.id.Append('`');
				this.id.Append(((GenericParameter)type).Position);
				return;
			case MetadataType.Array:
				this.WriteArrayTypeSignature((ArrayType)type);
				return;
			case MetadataType.GenericInstance:
				this.WriteGenericInstanceTypeSignature((GenericInstanceType)type);
				return;
			default:
				switch (metadataType)
				{
				case MetadataType.FunctionPointer:
					this.WriteFunctionPointerTypeSignature((FunctionPointerType)type);
					return;
				case MetadataType.MVar:
					this.id.Append('`').Append('`');
					this.id.Append(((GenericParameter)type).Position);
					return;
				case MetadataType.RequiredModifier:
					this.WriteModiferTypeSignature((RequiredModifierType)type, '|');
					return;
				case MetadataType.OptionalModifier:
					this.WriteModiferTypeSignature((OptionalModifierType)type, '!');
					return;
				}
				break;
			}
			this.WriteTypeFullName(type);
		}

		// Token: 0x060017C9 RID: 6089 RVA: 0x0004B05C File Offset: 0x0004925C
		private bool IsGenericMethodTypeParameter(TypeReference type)
		{
			MethodDefinition methodDefinition = this.commentMember as MethodDefinition;
			if (methodDefinition != null)
			{
				GenericParameter genericParameter = type as GenericParameter;
				if (genericParameter != null)
				{
					return methodDefinition.GenericParameters.Any((GenericParameter i) => i.Name == genericParameter.Name);
				}
			}
			return false;
		}

		// Token: 0x060017CA RID: 6090 RVA: 0x0004B0AC File Offset: 0x000492AC
		private void WriteGenericInstanceTypeSignature(GenericInstanceType type)
		{
			if (type.ElementType.IsTypeSpecification())
			{
				throw new NotSupportedException();
			}
			DocCommentId.GenericTypeOptions options = new DocCommentId.GenericTypeOptions
			{
				IsArgument = true,
				IsNestedType = type.IsNested,
				Arguments = type.GenericArguments
			};
			this.WriteTypeFullName(type.ElementType, options);
		}

		// Token: 0x060017CB RID: 6091 RVA: 0x0004B100 File Offset: 0x00049300
		private void WriteList<T>(IList<T> list, Action<T> action)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (i > 0)
				{
					this.id.Append(',');
				}
				action(list[i]);
			}
		}

		// Token: 0x060017CC RID: 6092 RVA: 0x0004B13D File Offset: 0x0004933D
		private void WriteModiferTypeSignature(IModifierType type, char id)
		{
			this.WriteTypeSignature(type.ElementType);
			this.id.Append(id);
			this.WriteTypeSignature(type.ModifierType);
		}

		// Token: 0x060017CD RID: 6093 RVA: 0x0004B164 File Offset: 0x00049364
		private void WriteFunctionPointerTypeSignature(FunctionPointerType type)
		{
			this.id.Append("=FUNC:");
			this.WriteTypeSignature(type.ReturnType);
			if (type.HasParameters)
			{
				this.WriteParameters(type.Parameters);
			}
		}

		// Token: 0x060017CE RID: 6094 RVA: 0x0004B198 File Offset: 0x00049398
		private void WriteArrayTypeSignature(ArrayType type)
		{
			this.WriteTypeSignature(type.ElementType);
			if (type.IsVector)
			{
				this.id.Append("[]");
				return;
			}
			this.id.Append("[");
			this.WriteList<ArrayDimension>(type.Dimensions, delegate(ArrayDimension dimension)
			{
				if (dimension.LowerBound != null)
				{
					this.id.Append(dimension.LowerBound.Value);
				}
				this.id.Append(':');
				if (dimension.UpperBound != null)
				{
					this.id.Append(dimension.UpperBound.Value - (dimension.LowerBound.GetValueOrDefault() + 1));
				}
			});
			this.id.Append("]");
		}

		// Token: 0x060017CF RID: 6095 RVA: 0x0004B205 File Offset: 0x00049405
		private void WriteDefinition(char id, IMemberDefinition member)
		{
			this.id.Append(id).Append(':');
			this.WriteTypeFullName(member.DeclaringType);
			this.id.Append('.');
			this.WriteItemName(member.Name);
		}

		// Token: 0x060017D0 RID: 6096 RVA: 0x0004B241 File Offset: 0x00049441
		private void WriteTypeFullName(TypeReference type)
		{
			this.WriteTypeFullName(type, DocCommentId.GenericTypeOptions.Empty());
		}

		// Token: 0x060017D1 RID: 6097 RVA: 0x0004B250 File Offset: 0x00049450
		private void WriteTypeFullName(TypeReference type, DocCommentId.GenericTypeOptions options)
		{
			if (type.DeclaringType != null)
			{
				this.WriteTypeFullName(type.DeclaringType, options);
				this.id.Append('.');
			}
			if (!string.IsNullOrEmpty(type.Namespace))
			{
				this.id.Append(type.Namespace);
				this.id.Append('.');
			}
			string name = type.Name;
			if (options.IsArgument)
			{
				int index = name.LastIndexOf('`');
				if (index > 0)
				{
					name = name.Substring(0, index);
				}
			}
			this.id.Append(name);
			this.WriteGenericTypeParameters(type, options);
		}

		// Token: 0x060017D2 RID: 6098 RVA: 0x0004B2E8 File Offset: 0x000494E8
		private void WriteGenericTypeParameters(TypeReference type, DocCommentId.GenericTypeOptions options)
		{
			if (options.IsArgument && DocCommentId.IsGenericType(type))
			{
				this.id.Append('{');
				this.WriteList<TypeReference>(this.GetGenericTypeArguments(type, options), new Action<TypeReference>(this.WriteTypeSignature));
				this.id.Append('}');
			}
		}

		// Token: 0x060017D3 RID: 6099 RVA: 0x0004B33C File Offset: 0x0004953C
		private static bool IsGenericType(TypeReference type)
		{
			if (type.HasGenericParameters)
			{
				string name = string.Empty;
				int index = type.Name.LastIndexOf('`');
				if (index >= 0)
				{
					name = type.Name.Substring(0, index);
				}
				return type.Name.LastIndexOf('`') == name.Length;
			}
			return false;
		}

		// Token: 0x060017D4 RID: 6100 RVA: 0x0004B390 File Offset: 0x00049590
		private IList<TypeReference> GetGenericTypeArguments(TypeReference type, DocCommentId.GenericTypeOptions options)
		{
			if (options.IsNestedType)
			{
				int typeParameterCount = type.GenericParameters.Count;
				IList<TypeReference> result = options.Arguments.Skip(options.ArgumentIndex).Take(typeParameterCount).ToList<TypeReference>();
				options.ArgumentIndex += typeParameterCount;
				return result;
			}
			return options.Arguments;
		}

		// Token: 0x060017D5 RID: 6101 RVA: 0x0004B3E2 File Offset: 0x000495E2
		private void WriteItemName(string name)
		{
			this.id.Append(name.Replace('.', '#').Replace('<', '{').Replace('>', '}'));
		}

		// Token: 0x060017D6 RID: 6102 RVA: 0x0004B40C File Offset: 0x0004960C
		public override string ToString()
		{
			return this.id.ToString();
		}

		// Token: 0x060017D7 RID: 6103 RVA: 0x0004B41C File Offset: 0x0004961C
		public static string GetDocCommentId(IMemberDefinition member)
		{
			if (member == null)
			{
				throw new ArgumentNullException("member");
			}
			DocCommentId documentId = new DocCommentId(member);
			TokenType tokenType = member.MetadataToken.TokenType;
			if (tokenType <= TokenType.Field)
			{
				if (tokenType == TokenType.TypeDef)
				{
					documentId.WriteType((TypeDefinition)member);
					goto IL_AA;
				}
				if (tokenType == TokenType.Field)
				{
					documentId.WriteField((FieldDefinition)member);
					goto IL_AA;
				}
			}
			else
			{
				if (tokenType == TokenType.Method)
				{
					documentId.WriteMethod((MethodDefinition)member);
					goto IL_AA;
				}
				if (tokenType == TokenType.Event)
				{
					documentId.WriteEvent((EventDefinition)member);
					goto IL_AA;
				}
				if (tokenType == TokenType.Property)
				{
					documentId.WriteProperty((PropertyDefinition)member);
					goto IL_AA;
				}
			}
			throw new NotSupportedException(member.FullName);
			IL_AA:
			return documentId.ToString();
		}

		// Token: 0x04001052 RID: 4178
		private IMemberDefinition commentMember;

		// Token: 0x04001053 RID: 4179
		private StringBuilder id;

		// Token: 0x0200044A RID: 1098
		private class GenericTypeOptions
		{
			// Token: 0x17000597 RID: 1431
			// (get) Token: 0x060017DA RID: 6106 RVA: 0x0004B56D File Offset: 0x0004976D
			// (set) Token: 0x060017DB RID: 6107 RVA: 0x0004B575 File Offset: 0x00049775
			public bool IsArgument { get; set; }

			// Token: 0x17000598 RID: 1432
			// (get) Token: 0x060017DC RID: 6108 RVA: 0x0004B57E File Offset: 0x0004977E
			// (set) Token: 0x060017DD RID: 6109 RVA: 0x0004B586 File Offset: 0x00049786
			public bool IsNestedType { get; set; }

			// Token: 0x17000599 RID: 1433
			// (get) Token: 0x060017DE RID: 6110 RVA: 0x0004B58F File Offset: 0x0004978F
			// (set) Token: 0x060017DF RID: 6111 RVA: 0x0004B597 File Offset: 0x00049797
			public IList<TypeReference> Arguments { get; set; }

			// Token: 0x1700059A RID: 1434
			// (get) Token: 0x060017E0 RID: 6112 RVA: 0x0004B5A0 File Offset: 0x000497A0
			// (set) Token: 0x060017E1 RID: 6113 RVA: 0x0004B5A8 File Offset: 0x000497A8
			public int ArgumentIndex { get; set; }

			// Token: 0x060017E2 RID: 6114 RVA: 0x0004B5B1 File Offset: 0x000497B1
			public static DocCommentId.GenericTypeOptions Empty()
			{
				return new DocCommentId.GenericTypeOptions();
			}
		}
	}
}
