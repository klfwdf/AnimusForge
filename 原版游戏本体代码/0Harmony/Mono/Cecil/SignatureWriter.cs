using System;
using System.Text;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000225 RID: 549
	internal sealed class SignatureWriter : ByteBuffer
	{
		// Token: 0x06000B88 RID: 2952 RVA: 0x00028AE0 File Offset: 0x00026CE0
		public SignatureWriter(MetadataBuilder metadata)
			: base(6)
		{
			this.metadata = metadata;
		}

		// Token: 0x06000B89 RID: 2953 RVA: 0x00028AF0 File Offset: 0x00026CF0
		public void WriteElementType(ElementType element_type)
		{
			base.WriteByte((byte)element_type);
		}

		// Token: 0x06000B8A RID: 2954 RVA: 0x00028AFC File Offset: 0x00026CFC
		public void WriteUTF8String(string @string)
		{
			if (@string == null)
			{
				base.WriteByte(byte.MaxValue);
				return;
			}
			byte[] bytes = Encoding.UTF8.GetBytes(@string);
			base.WriteCompressedUInt32((uint)bytes.Length);
			base.WriteBytes(bytes);
		}

		// Token: 0x06000B8B RID: 2955 RVA: 0x00028B34 File Offset: 0x00026D34
		public void WriteMethodSignature(IMethodSignature method)
		{
			byte calling_convention = (byte)method.CallingConvention;
			if (method.HasThis)
			{
				calling_convention |= 32;
			}
			if (method.ExplicitThis)
			{
				calling_convention |= 64;
			}
			IGenericParameterProvider generic_provider = method as IGenericParameterProvider;
			int generic_arity = ((generic_provider != null && generic_provider.HasGenericParameters) ? generic_provider.GenericParameters.Count : 0);
			if (generic_arity > 0)
			{
				calling_convention |= 16;
			}
			int param_count = (method.HasParameters ? method.Parameters.Count : 0);
			base.WriteByte(calling_convention);
			if (generic_arity > 0)
			{
				base.WriteCompressedUInt32((uint)generic_arity);
			}
			base.WriteCompressedUInt32((uint)param_count);
			this.WriteTypeSignature(method.ReturnType);
			if (param_count == 0)
			{
				return;
			}
			Collection<ParameterDefinition> parameters = method.Parameters;
			for (int i = 0; i < param_count; i++)
			{
				this.WriteTypeSignature(parameters[i].ParameterType);
			}
		}

		// Token: 0x06000B8C RID: 2956 RVA: 0x00028BFB File Offset: 0x00026DFB
		private uint MakeTypeDefOrRefCodedRID(TypeReference type)
		{
			return CodedIndex.TypeDefOrRef.CompressMetadataToken(this.metadata.LookupToken(type));
		}

		// Token: 0x06000B8D RID: 2957 RVA: 0x00028C0F File Offset: 0x00026E0F
		public void WriteTypeToken(TypeReference type)
		{
			base.WriteCompressedUInt32(this.MakeTypeDefOrRefCodedRID(type));
		}

		// Token: 0x06000B8E RID: 2958 RVA: 0x00028C20 File Offset: 0x00026E20
		public void WriteTypeSignature(TypeReference type)
		{
			if (type == null)
			{
				throw new ArgumentNullException();
			}
			ElementType etype = type.etype;
			if (etype <= ElementType.GenericInst)
			{
				if (etype == ElementType.None)
				{
					this.WriteElementType(type.IsValueType ? ElementType.ValueType : ElementType.Class);
					base.WriteCompressedUInt32(this.MakeTypeDefOrRefCodedRID(type));
					return;
				}
				switch (etype)
				{
				case ElementType.Ptr:
				case ElementType.ByRef:
					goto IL_D7;
				case ElementType.ValueType:
				case ElementType.Class:
					goto IL_16F;
				case ElementType.Var:
					break;
				case ElementType.Array:
				{
					ArrayType array = (ArrayType)type;
					if (!array.IsVector)
					{
						this.WriteArrayTypeSignature(array);
						return;
					}
					this.WriteElementType(ElementType.SzArray);
					this.WriteTypeSignature(array.ElementType);
					return;
				}
				case ElementType.GenericInst:
				{
					GenericInstanceType generic_instance = (GenericInstanceType)type;
					this.WriteElementType(ElementType.GenericInst);
					this.WriteElementType(generic_instance.IsValueType ? ElementType.ValueType : ElementType.Class);
					base.WriteCompressedUInt32(this.MakeTypeDefOrRefCodedRID(generic_instance.ElementType));
					this.WriteGenericInstanceSignature(generic_instance);
					return;
				}
				default:
					goto IL_16F;
				}
			}
			else
			{
				switch (etype)
				{
				case ElementType.FnPtr:
				{
					FunctionPointerType fptr = (FunctionPointerType)type;
					this.WriteElementType(ElementType.FnPtr);
					this.WriteMethodSignature(fptr);
					return;
				}
				case ElementType.Object:
				case ElementType.SzArray:
					goto IL_16F;
				case ElementType.MVar:
					break;
				case ElementType.CModReqD:
				case ElementType.CModOpt:
				{
					IModifierType modifier = (IModifierType)type;
					this.WriteModifierSignature(etype, modifier);
					return;
				}
				default:
					if (etype != ElementType.Sentinel && etype != ElementType.Pinned)
					{
						goto IL_16F;
					}
					goto IL_D7;
				}
			}
			GenericParameter genericParameter = (GenericParameter)type;
			this.WriteElementType(etype);
			int position = genericParameter.Position;
			if (position == -1)
			{
				throw new NotSupportedException();
			}
			base.WriteCompressedUInt32((uint)position);
			return;
			IL_D7:
			TypeSpecification type_spec = (TypeSpecification)type;
			this.WriteElementType(etype);
			this.WriteTypeSignature(type_spec.ElementType);
			return;
			IL_16F:
			if (!this.TryWriteElementType(type))
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x06000B8F RID: 2959 RVA: 0x00028DAC File Offset: 0x00026FAC
		private void WriteArrayTypeSignature(ArrayType array)
		{
			this.WriteElementType(ElementType.Array);
			this.WriteTypeSignature(array.ElementType);
			Collection<ArrayDimension> dimensions = array.Dimensions;
			int rank = dimensions.Count;
			base.WriteCompressedUInt32((uint)rank);
			int sized = 0;
			int lbounds = 0;
			for (int i = 0; i < rank; i++)
			{
				ArrayDimension dimension = dimensions[i];
				if (dimension.UpperBound != null)
				{
					sized++;
					lbounds++;
				}
				else if (dimension.LowerBound != null)
				{
					lbounds++;
				}
			}
			int[] sizes = new int[sized];
			int[] low_bounds = new int[lbounds];
			for (int j = 0; j < lbounds; j++)
			{
				ArrayDimension dimension2 = dimensions[j];
				low_bounds[j] = dimension2.LowerBound.GetValueOrDefault();
				if (dimension2.UpperBound != null)
				{
					sizes[j] = dimension2.UpperBound.Value - low_bounds[j] + 1;
				}
			}
			base.WriteCompressedUInt32((uint)sized);
			for (int k = 0; k < sized; k++)
			{
				base.WriteCompressedUInt32((uint)sizes[k]);
			}
			base.WriteCompressedUInt32((uint)lbounds);
			for (int l = 0; l < lbounds; l++)
			{
				base.WriteCompressedInt32(low_bounds[l]);
			}
		}

		// Token: 0x06000B90 RID: 2960 RVA: 0x00028EE4 File Offset: 0x000270E4
		public void WriteGenericInstanceSignature(IGenericInstance instance)
		{
			Collection<TypeReference> generic_arguments = instance.GenericArguments;
			int arity = generic_arguments.Count;
			base.WriteCompressedUInt32((uint)arity);
			for (int i = 0; i < arity; i++)
			{
				this.WriteTypeSignature(generic_arguments[i]);
			}
		}

		// Token: 0x06000B91 RID: 2961 RVA: 0x00028F1F File Offset: 0x0002711F
		private void WriteModifierSignature(ElementType element_type, IModifierType type)
		{
			this.WriteElementType(element_type);
			base.WriteCompressedUInt32(this.MakeTypeDefOrRefCodedRID(type.ModifierType));
			this.WriteTypeSignature(type.ElementType);
		}

		// Token: 0x06000B92 RID: 2962 RVA: 0x00028F48 File Offset: 0x00027148
		private bool TryWriteElementType(TypeReference type)
		{
			ElementType element = type.etype;
			if (element == ElementType.None)
			{
				return false;
			}
			this.WriteElementType(element);
			return true;
		}

		// Token: 0x06000B93 RID: 2963 RVA: 0x00028F69 File Offset: 0x00027169
		public void WriteConstantString(string value)
		{
			if (value != null)
			{
				base.WriteBytes(Encoding.Unicode.GetBytes(value));
				return;
			}
			base.WriteByte(byte.MaxValue);
		}

		// Token: 0x06000B94 RID: 2964 RVA: 0x00028F8B File Offset: 0x0002718B
		public void WriteConstantPrimitive(object value)
		{
			this.WritePrimitiveValue(value);
		}

		// Token: 0x06000B95 RID: 2965 RVA: 0x00028F94 File Offset: 0x00027194
		public void WriteCustomAttributeConstructorArguments(CustomAttribute attribute)
		{
			if (!attribute.HasConstructorArguments)
			{
				return;
			}
			Collection<CustomAttributeArgument> arguments = attribute.ConstructorArguments;
			Collection<ParameterDefinition> parameters = attribute.Constructor.Parameters;
			if (parameters.Count != arguments.Count)
			{
				throw new InvalidOperationException();
			}
			for (int i = 0; i < arguments.Count; i++)
			{
				TypeReference parameterType = GenericParameterResolver.ResolveParameterTypeIfNeeded(attribute.Constructor, parameters[i]);
				this.WriteCustomAttributeFixedArgument(parameterType, arguments[i]);
			}
		}

		// Token: 0x06000B96 RID: 2966 RVA: 0x00029003 File Offset: 0x00027203
		private void WriteCustomAttributeFixedArgument(TypeReference type, CustomAttributeArgument argument)
		{
			if (type.IsArray)
			{
				this.WriteCustomAttributeFixedArrayArgument((ArrayType)type, argument);
				return;
			}
			this.WriteCustomAttributeElement(type, argument);
		}

		// Token: 0x06000B97 RID: 2967 RVA: 0x00029024 File Offset: 0x00027224
		private void WriteCustomAttributeFixedArrayArgument(ArrayType type, CustomAttributeArgument argument)
		{
			CustomAttributeArgument[] values = argument.Value as CustomAttributeArgument[];
			if (values == null)
			{
				base.WriteUInt32(uint.MaxValue);
				return;
			}
			base.WriteInt32(values.Length);
			if (values.Length == 0)
			{
				return;
			}
			TypeReference element_type = type.ElementType;
			for (int i = 0; i < values.Length; i++)
			{
				this.WriteCustomAttributeElement(element_type, values[i]);
			}
		}

		// Token: 0x06000B98 RID: 2968 RVA: 0x0002907C File Offset: 0x0002727C
		private void WriteCustomAttributeElement(TypeReference type, CustomAttributeArgument argument)
		{
			if (type.IsArray)
			{
				this.WriteCustomAttributeFixedArrayArgument((ArrayType)type, argument);
				return;
			}
			if (type.etype == ElementType.Object)
			{
				argument = (CustomAttributeArgument)argument.Value;
				type = argument.Type;
				this.WriteCustomAttributeFieldOrPropType(type);
				this.WriteCustomAttributeElement(type, argument);
				return;
			}
			this.WriteCustomAttributeValue(type, argument.Value);
		}

		// Token: 0x06000B99 RID: 2969 RVA: 0x000290E0 File Offset: 0x000272E0
		private void WriteCustomAttributeValue(TypeReference type, object value)
		{
			ElementType etype = type.etype;
			if (etype != ElementType.None)
			{
				if (etype != ElementType.String)
				{
					if (etype != ElementType.GenericInst)
					{
						this.WritePrimitiveValue(value);
						return;
					}
					this.WriteCustomAttributeEnumValue(type, value);
					return;
				}
				else
				{
					string @string = (string)value;
					if (@string == null)
					{
						base.WriteByte(byte.MaxValue);
						return;
					}
					this.WriteUTF8String(@string);
					return;
				}
			}
			else
			{
				if (type.IsTypeOf("System", "Type"))
				{
					this.WriteCustomAttributeTypeValue((TypeReference)value);
					return;
				}
				this.WriteCustomAttributeEnumValue(type, value);
				return;
			}
		}

		// Token: 0x06000B9A RID: 2970 RVA: 0x0002915C File Offset: 0x0002735C
		private void WriteCustomAttributeTypeValue(TypeReference value)
		{
			TypeDefinition typeDefinition = value as TypeDefinition;
			if (typeDefinition != null)
			{
				TypeDefinition outermostDeclaringType = typeDefinition;
				while (outermostDeclaringType.DeclaringType != null)
				{
					outermostDeclaringType = outermostDeclaringType.DeclaringType;
				}
				if (WindowsRuntimeProjections.IsClrImplementationType(outermostDeclaringType))
				{
					WindowsRuntimeProjections.Project(outermostDeclaringType);
					this.WriteTypeReference(value);
					WindowsRuntimeProjections.RemoveProjection(outermostDeclaringType);
					return;
				}
			}
			this.WriteTypeReference(value);
		}

		// Token: 0x06000B9B RID: 2971 RVA: 0x000291AC File Offset: 0x000273AC
		private void WritePrimitiveValue(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException();
			}
			switch (Type.GetTypeCode(value.GetType()))
			{
			case TypeCode.Boolean:
				base.WriteByte(((bool)value > false) ? 1 : 0);
				return;
			case TypeCode.Char:
				base.WriteInt16((short)((char)value));
				return;
			case TypeCode.SByte:
				base.WriteSByte((sbyte)value);
				return;
			case TypeCode.Byte:
				base.WriteByte((byte)value);
				return;
			case TypeCode.Int16:
				base.WriteInt16((short)value);
				return;
			case TypeCode.UInt16:
				base.WriteUInt16((ushort)value);
				return;
			case TypeCode.Int32:
				base.WriteInt32((int)value);
				return;
			case TypeCode.UInt32:
				base.WriteUInt32((uint)value);
				return;
			case TypeCode.Int64:
				base.WriteInt64((long)value);
				return;
			case TypeCode.UInt64:
				base.WriteUInt64((ulong)value);
				return;
			case TypeCode.Single:
				base.WriteSingle((float)value);
				return;
			case TypeCode.Double:
				base.WriteDouble((double)value);
				return;
			default:
				throw new NotSupportedException(value.GetType().FullName);
			}
		}

		// Token: 0x06000B9C RID: 2972 RVA: 0x000292BC File Offset: 0x000274BC
		private void WriteCustomAttributeEnumValue(TypeReference enum_type, object value)
		{
			TypeDefinition type = enum_type.CheckedResolve();
			if (!type.IsEnum)
			{
				throw new ArgumentException();
			}
			this.WriteCustomAttributeValue(type.GetEnumUnderlyingType(), value);
		}

		// Token: 0x06000B9D RID: 2973 RVA: 0x000292EC File Offset: 0x000274EC
		private void WriteCustomAttributeFieldOrPropType(TypeReference type)
		{
			if (type.IsArray)
			{
				ArrayType array = (ArrayType)type;
				this.WriteElementType(ElementType.SzArray);
				this.WriteCustomAttributeFieldOrPropType(array.ElementType);
				return;
			}
			ElementType etype = type.etype;
			if (etype != ElementType.None)
			{
				if (etype == ElementType.GenericInst)
				{
					this.WriteElementType(ElementType.Enum);
					this.WriteTypeReference(type);
					return;
				}
				if (etype == ElementType.Object)
				{
					this.WriteElementType(ElementType.Boxed);
					return;
				}
				this.WriteElementType(etype);
				return;
			}
			else
			{
				if (type.IsTypeOf("System", "Type"))
				{
					this.WriteElementType(ElementType.Type);
					return;
				}
				this.WriteElementType(ElementType.Enum);
				this.WriteTypeReference(type);
				return;
			}
		}

		// Token: 0x06000B9E RID: 2974 RVA: 0x0002937C File Offset: 0x0002757C
		public void WriteCustomAttributeNamedArguments(CustomAttribute attribute)
		{
			int count = SignatureWriter.GetNamedArgumentCount(attribute);
			base.WriteUInt16((ushort)count);
			if (count == 0)
			{
				return;
			}
			this.WriteICustomAttributeNamedArguments(attribute);
		}

		// Token: 0x06000B9F RID: 2975 RVA: 0x000293A4 File Offset: 0x000275A4
		private static int GetNamedArgumentCount(ICustomAttribute attribute)
		{
			int count = 0;
			if (attribute.HasFields)
			{
				count += attribute.Fields.Count;
			}
			if (attribute.HasProperties)
			{
				count += attribute.Properties.Count;
			}
			return count;
		}

		// Token: 0x06000BA0 RID: 2976 RVA: 0x000293E0 File Offset: 0x000275E0
		private void WriteICustomAttributeNamedArguments(ICustomAttribute attribute)
		{
			if (attribute.HasFields)
			{
				this.WriteCustomAttributeNamedArguments(83, attribute.Fields);
			}
			if (attribute.HasProperties)
			{
				this.WriteCustomAttributeNamedArguments(84, attribute.Properties);
			}
		}

		// Token: 0x06000BA1 RID: 2977 RVA: 0x00029410 File Offset: 0x00027610
		private void WriteCustomAttributeNamedArguments(byte kind, Collection<CustomAttributeNamedArgument> named_arguments)
		{
			for (int i = 0; i < named_arguments.Count; i++)
			{
				this.WriteCustomAttributeNamedArgument(kind, named_arguments[i]);
			}
		}

		// Token: 0x06000BA2 RID: 2978 RVA: 0x0002943C File Offset: 0x0002763C
		private void WriteCustomAttributeNamedArgument(byte kind, CustomAttributeNamedArgument named_argument)
		{
			CustomAttributeArgument argument = named_argument.Argument;
			base.WriteByte(kind);
			this.WriteCustomAttributeFieldOrPropType(argument.Type);
			this.WriteUTF8String(named_argument.Name);
			this.WriteCustomAttributeFixedArgument(argument.Type, argument);
		}

		// Token: 0x06000BA3 RID: 2979 RVA: 0x00029480 File Offset: 0x00027680
		private void WriteSecurityAttribute(SecurityAttribute attribute)
		{
			this.WriteTypeReference(attribute.AttributeType);
			int count = SignatureWriter.GetNamedArgumentCount(attribute);
			if (count == 0)
			{
				base.WriteCompressedUInt32(1U);
				base.WriteCompressedUInt32(0U);
				return;
			}
			SignatureWriter buffer = new SignatureWriter(this.metadata);
			buffer.WriteCompressedUInt32((uint)count);
			buffer.WriteICustomAttributeNamedArguments(attribute);
			base.WriteCompressedUInt32((uint)buffer.length);
			base.WriteBytes(buffer);
		}

		// Token: 0x06000BA4 RID: 2980 RVA: 0x000294E0 File Offset: 0x000276E0
		public void WriteSecurityDeclaration(SecurityDeclaration declaration)
		{
			base.WriteByte(46);
			Collection<SecurityAttribute> attributes = declaration.security_attributes;
			if (attributes == null)
			{
				throw new NotSupportedException();
			}
			base.WriteCompressedUInt32((uint)attributes.Count);
			for (int i = 0; i < attributes.Count; i++)
			{
				this.WriteSecurityAttribute(attributes[i]);
			}
		}

		// Token: 0x06000BA5 RID: 2981 RVA: 0x00029530 File Offset: 0x00027730
		public void WriteXmlSecurityDeclaration(SecurityDeclaration declaration)
		{
			string xml = SignatureWriter.GetXmlSecurityDeclaration(declaration);
			if (xml == null)
			{
				throw new NotSupportedException();
			}
			base.WriteBytes(Encoding.Unicode.GetBytes(xml));
		}

		// Token: 0x06000BA6 RID: 2982 RVA: 0x00029560 File Offset: 0x00027760
		private static string GetXmlSecurityDeclaration(SecurityDeclaration declaration)
		{
			if (declaration.security_attributes == null || declaration.security_attributes.Count != 1)
			{
				return null;
			}
			SecurityAttribute attribute = declaration.security_attributes[0];
			if (!attribute.AttributeType.IsTypeOf("System.Security.Permissions", "PermissionSetAttribute"))
			{
				return null;
			}
			if (attribute.properties == null || attribute.properties.Count != 1)
			{
				return null;
			}
			CustomAttributeNamedArgument property = attribute.properties[0];
			if (property.Name != "XML")
			{
				return null;
			}
			return (string)property.Argument.Value;
		}

		// Token: 0x06000BA7 RID: 2983 RVA: 0x000295F9 File Offset: 0x000277F9
		private void WriteTypeReference(TypeReference type)
		{
			this.WriteUTF8String(TypeParser.ToParseable(type, false));
		}

		// Token: 0x06000BA8 RID: 2984 RVA: 0x00029608 File Offset: 0x00027808
		public void WriteMarshalInfo(MarshalInfo marshal_info)
		{
			this.WriteNativeType(marshal_info.native);
			NativeType native = marshal_info.native;
			if (native <= NativeType.SafeArray)
			{
				if (native == NativeType.FixedSysString)
				{
					FixedSysStringMarshalInfo sys_string = (FixedSysStringMarshalInfo)marshal_info;
					if (sys_string.size > -1)
					{
						base.WriteCompressedUInt32((uint)sys_string.size);
					}
					return;
				}
				if (native != NativeType.SafeArray)
				{
					return;
				}
				SafeArrayMarshalInfo array = (SafeArrayMarshalInfo)marshal_info;
				if (array.element_type != VariantType.None)
				{
					this.WriteVariantType(array.element_type);
				}
				return;
			}
			else
			{
				if (native == NativeType.FixedArray)
				{
					FixedArrayMarshalInfo array2 = (FixedArrayMarshalInfo)marshal_info;
					if (array2.size > -1)
					{
						base.WriteCompressedUInt32((uint)array2.size);
					}
					if (array2.element_type != NativeType.None)
					{
						this.WriteNativeType(array2.element_type);
					}
					return;
				}
				if (native == NativeType.Array)
				{
					ArrayMarshalInfo array3 = (ArrayMarshalInfo)marshal_info;
					if (array3.element_type != NativeType.None)
					{
						this.WriteNativeType(array3.element_type);
					}
					if (array3.size_parameter_index > -1)
					{
						base.WriteCompressedUInt32((uint)array3.size_parameter_index);
					}
					if (array3.size > -1)
					{
						base.WriteCompressedUInt32((uint)array3.size);
					}
					if (array3.size_parameter_multiplier > -1)
					{
						base.WriteCompressedUInt32((uint)array3.size_parameter_multiplier);
					}
					return;
				}
				if (native != NativeType.CustomMarshaler)
				{
					return;
				}
				CustomMarshalInfo marshaler = (CustomMarshalInfo)marshal_info;
				this.WriteUTF8String((marshaler.guid != Guid.Empty) ? marshaler.guid.ToString() : string.Empty);
				this.WriteUTF8String(marshaler.unmanaged_type);
				this.WriteTypeReference(marshaler.managed_type);
				this.WriteUTF8String(marshaler.cookie);
				return;
			}
		}

		// Token: 0x06000BA9 RID: 2985 RVA: 0x0002977D File Offset: 0x0002797D
		private void WriteNativeType(NativeType native)
		{
			base.WriteByte((byte)native);
		}

		// Token: 0x06000BAA RID: 2986 RVA: 0x0002977D File Offset: 0x0002797D
		private void WriteVariantType(VariantType variant)
		{
			base.WriteByte((byte)variant);
		}

		// Token: 0x06000BAB RID: 2987 RVA: 0x00029788 File Offset: 0x00027988
		public void WriteSequencePoints(MethodDebugInformation info)
		{
			int start_line = -1;
			int start_column = -1;
			base.WriteCompressedUInt32(info.local_var_token.RID);
			Document previous_document;
			if (!info.TryGetUniqueDocument(out previous_document))
			{
				previous_document = null;
			}
			for (int i = 0; i < info.SequencePoints.Count; i++)
			{
				SequencePoint sequence_point = info.SequencePoints[i];
				Document document = sequence_point.Document;
				if (previous_document != document)
				{
					MetadataToken document_token = this.metadata.GetDocumentToken(document);
					if (previous_document != null)
					{
						base.WriteCompressedUInt32(0U);
					}
					base.WriteCompressedUInt32(document_token.RID);
					previous_document = document;
				}
				if (i > 0)
				{
					base.WriteCompressedUInt32((uint)(sequence_point.Offset - info.SequencePoints[i - 1].Offset));
				}
				else
				{
					base.WriteCompressedUInt32((uint)sequence_point.Offset);
				}
				if (sequence_point.IsHidden)
				{
					base.WriteInt16(0);
				}
				else
				{
					int delta_lines = sequence_point.EndLine - sequence_point.StartLine;
					int delta_columns = sequence_point.EndColumn - sequence_point.StartColumn;
					base.WriteCompressedUInt32((uint)delta_lines);
					if (delta_lines == 0)
					{
						base.WriteCompressedUInt32((uint)delta_columns);
					}
					else
					{
						base.WriteCompressedInt32(delta_columns);
					}
					if (start_line < 0)
					{
						base.WriteCompressedUInt32((uint)sequence_point.StartLine);
						base.WriteCompressedUInt32((uint)sequence_point.StartColumn);
					}
					else
					{
						base.WriteCompressedInt32(sequence_point.StartLine - start_line);
						base.WriteCompressedInt32(sequence_point.StartColumn - start_column);
					}
					start_line = sequence_point.StartLine;
					start_column = sequence_point.StartColumn;
				}
			}
		}

		// Token: 0x04000388 RID: 904
		private readonly MetadataBuilder metadata;
	}
}
