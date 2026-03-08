using System;
using System.Text;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020001F2 RID: 498
	internal sealed class SignatureReader : ByteBuffer
	{
		// Token: 0x1700024C RID: 588
		// (get) Token: 0x06000A7B RID: 2683 RVA: 0x00023916 File Offset: 0x00021B16
		private TypeSystem TypeSystem
		{
			get
			{
				return this.reader.module.TypeSystem;
			}
		}

		// Token: 0x06000A7C RID: 2684 RVA: 0x00023928 File Offset: 0x00021B28
		public SignatureReader(uint blob, MetadataReader reader)
			: base(reader.image.BlobHeap.data)
		{
			this.reader = reader;
			this.position = (int)blob;
			this.sig_length = base.ReadCompressedUInt32();
			this.start = (uint)this.position;
		}

		// Token: 0x06000A7D RID: 2685 RVA: 0x00023966 File Offset: 0x00021B66
		private MetadataToken ReadTypeTokenSignature()
		{
			return CodedIndex.TypeDefOrRef.GetMetadataToken(base.ReadCompressedUInt32());
		}

		// Token: 0x06000A7E RID: 2686 RVA: 0x00023974 File Offset: 0x00021B74
		private GenericParameter GetGenericParameter(GenericParameterType type, uint var)
		{
			IGenericContext context = this.reader.context;
			if (context == null)
			{
				return this.GetUnboundGenericParameter(type, (int)var);
			}
			IGenericParameterProvider provider;
			if (type != GenericParameterType.Type)
			{
				if (type != GenericParameterType.Method)
				{
					throw new NotSupportedException();
				}
				provider = context.Method;
			}
			else
			{
				provider = context.Type;
			}
			if (!context.IsDefinition)
			{
				SignatureReader.CheckGenericContext(provider, (int)var);
			}
			if (var >= (uint)provider.GenericParameters.Count)
			{
				return this.GetUnboundGenericParameter(type, (int)var);
			}
			return provider.GenericParameters[(int)var];
		}

		// Token: 0x06000A7F RID: 2687 RVA: 0x000239EE File Offset: 0x00021BEE
		private GenericParameter GetUnboundGenericParameter(GenericParameterType type, int index)
		{
			return new GenericParameter(index, type, this.reader.module);
		}

		// Token: 0x06000A80 RID: 2688 RVA: 0x00023A04 File Offset: 0x00021C04
		private static void CheckGenericContext(IGenericParameterProvider owner, int index)
		{
			Collection<GenericParameter> owner_parameters = owner.GenericParameters;
			for (int i = owner_parameters.Count; i <= index; i++)
			{
				owner_parameters.Add(new GenericParameter(owner));
			}
		}

		// Token: 0x06000A81 RID: 2689 RVA: 0x00023A38 File Offset: 0x00021C38
		public void ReadGenericInstanceSignature(IGenericParameterProvider provider, IGenericInstance instance, uint arity)
		{
			if (!provider.IsDefinition)
			{
				SignatureReader.CheckGenericContext(provider, (int)(arity - 1U));
			}
			Collection<TypeReference> instance_arguments = instance.GenericArguments;
			int i = 0;
			while ((long)i < (long)((ulong)arity))
			{
				instance_arguments.Add(this.ReadTypeSignature());
				i++;
			}
		}

		// Token: 0x06000A82 RID: 2690 RVA: 0x00023A78 File Offset: 0x00021C78
		private ArrayType ReadArrayTypeSignature()
		{
			ArrayType array = new ArrayType(this.ReadTypeSignature());
			uint rank = base.ReadCompressedUInt32();
			uint[] sizes = new uint[base.ReadCompressedUInt32()];
			for (int i = 0; i < sizes.Length; i++)
			{
				sizes[i] = base.ReadCompressedUInt32();
			}
			int[] low_bounds = new int[base.ReadCompressedUInt32()];
			for (int j = 0; j < low_bounds.Length; j++)
			{
				low_bounds[j] = base.ReadCompressedInt32();
			}
			array.Dimensions.Clear();
			int k = 0;
			while ((long)k < (long)((ulong)rank))
			{
				int? lower = null;
				int? upper = null;
				if (k < low_bounds.Length)
				{
					lower = new int?(low_bounds[k]);
				}
				if (k < sizes.Length)
				{
					int? num = lower;
					int num2 = (int)sizes[k];
					upper = ((num != null) ? new int?(num.GetValueOrDefault() + num2 - 1) : null);
				}
				array.Dimensions.Add(new ArrayDimension(lower, upper));
				k++;
			}
			return array;
		}

		// Token: 0x06000A83 RID: 2691 RVA: 0x00023B75 File Offset: 0x00021D75
		private TypeReference GetTypeDefOrRef(MetadataToken token)
		{
			return this.reader.GetTypeDefOrRef(token);
		}

		// Token: 0x06000A84 RID: 2692 RVA: 0x00023B83 File Offset: 0x00021D83
		public TypeReference ReadTypeSignature()
		{
			return this.ReadTypeSignature((ElementType)base.ReadByte());
		}

		// Token: 0x06000A85 RID: 2693 RVA: 0x00023B91 File Offset: 0x00021D91
		public TypeReference ReadTypeToken()
		{
			return this.GetTypeDefOrRef(this.ReadTypeTokenSignature());
		}

		// Token: 0x06000A86 RID: 2694 RVA: 0x00023BA0 File Offset: 0x00021DA0
		private TypeReference ReadTypeSignature(ElementType etype)
		{
			switch (etype)
			{
			case ElementType.Void:
				return this.TypeSystem.Void;
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.String:
			case (ElementType)23:
			case (ElementType)26:
				break;
			case ElementType.Ptr:
				return new PointerType(this.ReadTypeSignature());
			case ElementType.ByRef:
				return new ByReferenceType(this.ReadTypeSignature());
			case ElementType.ValueType:
			{
				TypeReference typeDefOrRef = this.GetTypeDefOrRef(this.ReadTypeTokenSignature());
				typeDefOrRef.KnownValueType();
				return typeDefOrRef;
			}
			case ElementType.Class:
				return this.GetTypeDefOrRef(this.ReadTypeTokenSignature());
			case ElementType.Var:
				return this.GetGenericParameter(GenericParameterType.Type, base.ReadCompressedUInt32());
			case ElementType.Array:
				return this.ReadArrayTypeSignature();
			case ElementType.GenericInst:
			{
				bool flag = base.ReadByte() == 17;
				TypeReference element_type = this.GetTypeDefOrRef(this.ReadTypeTokenSignature());
				uint arity = base.ReadCompressedUInt32();
				GenericInstanceType generic_instance = new GenericInstanceType(element_type, (int)arity);
				this.ReadGenericInstanceSignature(element_type, generic_instance, arity);
				if (flag)
				{
					generic_instance.KnownValueType();
					element_type.GetElementType().KnownValueType();
				}
				return generic_instance;
			}
			case ElementType.TypedByRef:
				return this.TypeSystem.TypedReference;
			case ElementType.I:
				return this.TypeSystem.IntPtr;
			case ElementType.U:
				return this.TypeSystem.UIntPtr;
			case ElementType.FnPtr:
			{
				FunctionPointerType fptr = new FunctionPointerType();
				this.ReadMethodSignature(fptr);
				return fptr;
			}
			case ElementType.Object:
				return this.TypeSystem.Object;
			case ElementType.SzArray:
				return new ArrayType(this.ReadTypeSignature());
			case ElementType.MVar:
				return this.GetGenericParameter(GenericParameterType.Method, base.ReadCompressedUInt32());
			case ElementType.CModReqD:
				return new RequiredModifierType(this.GetTypeDefOrRef(this.ReadTypeTokenSignature()), this.ReadTypeSignature());
			case ElementType.CModOpt:
				return new OptionalModifierType(this.GetTypeDefOrRef(this.ReadTypeTokenSignature()), this.ReadTypeSignature());
			default:
				if (etype == ElementType.Sentinel)
				{
					return new SentinelType(this.ReadTypeSignature());
				}
				if (etype == ElementType.Pinned)
				{
					return new PinnedType(this.ReadTypeSignature());
				}
				break;
			}
			return this.GetPrimitiveType(etype);
		}

		// Token: 0x06000A87 RID: 2695 RVA: 0x00023D8C File Offset: 0x00021F8C
		public void ReadMethodSignature(IMethodSignature method)
		{
			byte calling_convention = base.ReadByte();
			if ((calling_convention & 32) != 0)
			{
				method.HasThis = true;
				calling_convention = (byte)((int)calling_convention & -33);
			}
			if ((calling_convention & 64) != 0)
			{
				method.ExplicitThis = true;
				calling_convention = (byte)((int)calling_convention & -65);
			}
			method.CallingConvention = (MethodCallingConvention)calling_convention;
			MethodReference generic_context = method as MethodReference;
			if (generic_context != null && !generic_context.DeclaringType.IsArray)
			{
				this.reader.context = generic_context;
			}
			if ((calling_convention & 16) != 0)
			{
				uint arity = base.ReadCompressedUInt32();
				if (generic_context != null && !generic_context.IsDefinition)
				{
					SignatureReader.CheckGenericContext(generic_context, (int)(arity - 1U));
				}
			}
			uint param_count = base.ReadCompressedUInt32();
			method.MethodReturnType.ReturnType = this.ReadTypeSignature();
			if (param_count == 0U)
			{
				return;
			}
			MethodReference method_ref = method as MethodReference;
			Collection<ParameterDefinition> parameters;
			if (method_ref != null)
			{
				parameters = (method_ref.parameters = new ParameterDefinitionCollection(method, (int)param_count));
			}
			else
			{
				parameters = method.Parameters;
			}
			int i = 0;
			while ((long)i < (long)((ulong)param_count))
			{
				parameters.Add(new ParameterDefinition(this.ReadTypeSignature()));
				i++;
			}
		}

		// Token: 0x06000A88 RID: 2696 RVA: 0x00023E7B File Offset: 0x0002207B
		public object ReadConstantSignature(ElementType type)
		{
			return this.ReadPrimitiveValue(type);
		}

		// Token: 0x06000A89 RID: 2697 RVA: 0x00023E84 File Offset: 0x00022084
		public void ReadCustomAttributeConstructorArguments(CustomAttribute attribute, Collection<ParameterDefinition> parameters)
		{
			int count = parameters.Count;
			if (count == 0)
			{
				return;
			}
			attribute.arguments = new Collection<CustomAttributeArgument>(count);
			for (int i = 0; i < count; i++)
			{
				TypeReference parameterType = GenericParameterResolver.ResolveParameterTypeIfNeeded(attribute.Constructor, parameters[i]);
				attribute.arguments.Add(this.ReadCustomAttributeFixedArgument(parameterType));
			}
		}

		// Token: 0x06000A8A RID: 2698 RVA: 0x00023ED9 File Offset: 0x000220D9
		private CustomAttributeArgument ReadCustomAttributeFixedArgument(TypeReference type)
		{
			if (type.IsArray)
			{
				return this.ReadCustomAttributeFixedArrayArgument((ArrayType)type);
			}
			return this.ReadCustomAttributeElement(type);
		}

		// Token: 0x06000A8B RID: 2699 RVA: 0x00023EF8 File Offset: 0x000220F8
		public void ReadCustomAttributeNamedArguments(ushort count, ref Collection<CustomAttributeNamedArgument> fields, ref Collection<CustomAttributeNamedArgument> properties)
		{
			for (int i = 0; i < (int)count; i++)
			{
				if (!this.CanReadMore())
				{
					return;
				}
				this.ReadCustomAttributeNamedArgument(ref fields, ref properties);
			}
		}

		// Token: 0x06000A8C RID: 2700 RVA: 0x00023F24 File Offset: 0x00022124
		private void ReadCustomAttributeNamedArgument(ref Collection<CustomAttributeNamedArgument> fields, ref Collection<CustomAttributeNamedArgument> properties)
		{
			byte kind = base.ReadByte();
			TypeReference type = this.ReadCustomAttributeFieldOrPropType();
			string name = this.ReadUTF8String();
			Collection<CustomAttributeNamedArgument> container;
			if (kind != 83)
			{
				if (kind != 84)
				{
					throw new NotSupportedException();
				}
				container = SignatureReader.GetCustomAttributeNamedArgumentCollection(ref properties);
			}
			else
			{
				container = SignatureReader.GetCustomAttributeNamedArgumentCollection(ref fields);
			}
			container.Add(new CustomAttributeNamedArgument(name, this.ReadCustomAttributeFixedArgument(type)));
		}

		// Token: 0x06000A8D RID: 2701 RVA: 0x00023F80 File Offset: 0x00022180
		private static Collection<CustomAttributeNamedArgument> GetCustomAttributeNamedArgumentCollection(ref Collection<CustomAttributeNamedArgument> collection)
		{
			if (collection != null)
			{
				return collection;
			}
			Collection<CustomAttributeNamedArgument> result;
			collection = (result = new Collection<CustomAttributeNamedArgument>());
			return result;
		}

		// Token: 0x06000A8E RID: 2702 RVA: 0x00023FA0 File Offset: 0x000221A0
		private CustomAttributeArgument ReadCustomAttributeFixedArrayArgument(ArrayType type)
		{
			uint length = base.ReadUInt32();
			if (length == 4294967295U)
			{
				return new CustomAttributeArgument(type, null);
			}
			if (length == 0U)
			{
				return new CustomAttributeArgument(type, Empty<CustomAttributeArgument>.Array);
			}
			CustomAttributeArgument[] arguments = new CustomAttributeArgument[length];
			TypeReference element_type = type.ElementType;
			int i = 0;
			while ((long)i < (long)((ulong)length))
			{
				arguments[i] = this.ReadCustomAttributeElement(element_type);
				i++;
			}
			return new CustomAttributeArgument(type, arguments);
		}

		// Token: 0x06000A8F RID: 2703 RVA: 0x00024000 File Offset: 0x00022200
		private CustomAttributeArgument ReadCustomAttributeElement(TypeReference type)
		{
			if (type.IsArray)
			{
				return this.ReadCustomAttributeFixedArrayArgument((ArrayType)type);
			}
			return new CustomAttributeArgument(type, (type.etype == ElementType.Object) ? this.ReadCustomAttributeElement(this.ReadCustomAttributeFieldOrPropType()) : this.ReadCustomAttributeElementValue(type));
		}

		// Token: 0x06000A90 RID: 2704 RVA: 0x0002404C File Offset: 0x0002224C
		private object ReadCustomAttributeElementValue(TypeReference type)
		{
			ElementType etype = type.etype;
			if (etype == ElementType.GenericInst)
			{
				type = type.GetElementType();
				etype = type.etype;
			}
			if (etype != ElementType.None)
			{
				if (etype == ElementType.String)
				{
					return this.ReadUTF8String();
				}
				return this.ReadPrimitiveValue(etype);
			}
			else
			{
				if (type.IsTypeOf("System", "Type"))
				{
					return this.ReadTypeReference();
				}
				return this.ReadCustomAttributeEnum(type);
			}
		}

		// Token: 0x06000A91 RID: 2705 RVA: 0x000240AC File Offset: 0x000222AC
		private object ReadPrimitiveValue(ElementType type)
		{
			switch (type)
			{
			case ElementType.Boolean:
				return base.ReadByte() == 1;
			case ElementType.Char:
				return (char)base.ReadUInt16();
			case ElementType.I1:
				return (sbyte)base.ReadByte();
			case ElementType.U1:
				return base.ReadByte();
			case ElementType.I2:
				return base.ReadInt16();
			case ElementType.U2:
				return base.ReadUInt16();
			case ElementType.I4:
				return base.ReadInt32();
			case ElementType.U4:
				return base.ReadUInt32();
			case ElementType.I8:
				return base.ReadInt64();
			case ElementType.U8:
				return base.ReadUInt64();
			case ElementType.R4:
				return base.ReadSingle();
			case ElementType.R8:
				return base.ReadDouble();
			default:
				throw new NotImplementedException(type.ToString());
			}
		}

		// Token: 0x06000A92 RID: 2706 RVA: 0x0002419C File Offset: 0x0002239C
		private TypeReference GetPrimitiveType(ElementType etype)
		{
			switch (etype)
			{
			case ElementType.Boolean:
				return this.TypeSystem.Boolean;
			case ElementType.Char:
				return this.TypeSystem.Char;
			case ElementType.I1:
				return this.TypeSystem.SByte;
			case ElementType.U1:
				return this.TypeSystem.Byte;
			case ElementType.I2:
				return this.TypeSystem.Int16;
			case ElementType.U2:
				return this.TypeSystem.UInt16;
			case ElementType.I4:
				return this.TypeSystem.Int32;
			case ElementType.U4:
				return this.TypeSystem.UInt32;
			case ElementType.I8:
				return this.TypeSystem.Int64;
			case ElementType.U8:
				return this.TypeSystem.UInt64;
			case ElementType.R4:
				return this.TypeSystem.Single;
			case ElementType.R8:
				return this.TypeSystem.Double;
			case ElementType.String:
				return this.TypeSystem.String;
			default:
				throw new NotImplementedException(etype.ToString());
			}
		}

		// Token: 0x06000A93 RID: 2707 RVA: 0x00024298 File Offset: 0x00022498
		private TypeReference ReadCustomAttributeFieldOrPropType()
		{
			ElementType etype = (ElementType)base.ReadByte();
			if (etype <= ElementType.Type)
			{
				if (etype == ElementType.SzArray)
				{
					return new ArrayType(this.ReadCustomAttributeFieldOrPropType());
				}
				if (etype == ElementType.Type)
				{
					return this.TypeSystem.LookupType("System", "Type");
				}
			}
			else
			{
				if (etype == ElementType.Boxed)
				{
					return this.TypeSystem.Object;
				}
				if (etype == ElementType.Enum)
				{
					return this.ReadTypeReference();
				}
			}
			return this.GetPrimitiveType(etype);
		}

		// Token: 0x06000A94 RID: 2708 RVA: 0x00024305 File Offset: 0x00022505
		public TypeReference ReadTypeReference()
		{
			return TypeParser.ParseType(this.reader.module, this.ReadUTF8String(), false);
		}

		// Token: 0x06000A95 RID: 2709 RVA: 0x00024320 File Offset: 0x00022520
		private object ReadCustomAttributeEnum(TypeReference enum_type)
		{
			TypeDefinition type = enum_type.CheckedResolve();
			if (!type.IsEnum)
			{
				throw new ArgumentException();
			}
			return this.ReadCustomAttributeElementValue(type.GetEnumUnderlyingType());
		}

		// Token: 0x06000A96 RID: 2710 RVA: 0x00024350 File Offset: 0x00022550
		public SecurityAttribute ReadSecurityAttribute()
		{
			SecurityAttribute attribute = new SecurityAttribute(this.ReadTypeReference());
			base.ReadCompressedUInt32();
			this.ReadCustomAttributeNamedArguments((ushort)base.ReadCompressedUInt32(), ref attribute.fields, ref attribute.properties);
			return attribute;
		}

		// Token: 0x06000A97 RID: 2711 RVA: 0x0002438C File Offset: 0x0002258C
		public MarshalInfo ReadMarshalInfo()
		{
			NativeType native = this.ReadNativeType();
			if (native <= NativeType.SafeArray)
			{
				if (native == NativeType.FixedSysString)
				{
					FixedSysStringMarshalInfo sys_string = new FixedSysStringMarshalInfo();
					if (this.CanReadMore())
					{
						sys_string.size = (int)base.ReadCompressedUInt32();
					}
					return sys_string;
				}
				if (native == NativeType.SafeArray)
				{
					SafeArrayMarshalInfo array = new SafeArrayMarshalInfo();
					if (this.CanReadMore())
					{
						array.element_type = this.ReadVariantType();
					}
					return array;
				}
			}
			else
			{
				if (native == NativeType.FixedArray)
				{
					FixedArrayMarshalInfo array2 = new FixedArrayMarshalInfo();
					if (this.CanReadMore())
					{
						array2.size = (int)base.ReadCompressedUInt32();
					}
					if (this.CanReadMore())
					{
						array2.element_type = this.ReadNativeType();
					}
					return array2;
				}
				if (native == NativeType.Array)
				{
					ArrayMarshalInfo array3 = new ArrayMarshalInfo();
					if (this.CanReadMore())
					{
						array3.element_type = this.ReadNativeType();
					}
					if (this.CanReadMore())
					{
						array3.size_parameter_index = (int)base.ReadCompressedUInt32();
					}
					if (this.CanReadMore())
					{
						array3.size = (int)base.ReadCompressedUInt32();
					}
					if (this.CanReadMore())
					{
						array3.size_parameter_multiplier = (int)base.ReadCompressedUInt32();
					}
					return array3;
				}
				if (native == NativeType.CustomMarshaler)
				{
					CustomMarshalInfo customMarshalInfo = new CustomMarshalInfo();
					string guid_value = this.ReadUTF8String();
					customMarshalInfo.guid = ((!string.IsNullOrEmpty(guid_value)) ? new Guid(guid_value) : Guid.Empty);
					customMarshalInfo.unmanaged_type = this.ReadUTF8String();
					customMarshalInfo.managed_type = this.ReadTypeReference();
					customMarshalInfo.cookie = this.ReadUTF8String();
					return customMarshalInfo;
				}
			}
			return new MarshalInfo(native);
		}

		// Token: 0x06000A98 RID: 2712 RVA: 0x000244E9 File Offset: 0x000226E9
		private NativeType ReadNativeType()
		{
			return (NativeType)base.ReadByte();
		}

		// Token: 0x06000A99 RID: 2713 RVA: 0x000244E9 File Offset: 0x000226E9
		private VariantType ReadVariantType()
		{
			return (VariantType)base.ReadByte();
		}

		// Token: 0x06000A9A RID: 2714 RVA: 0x000244F4 File Offset: 0x000226F4
		private string ReadUTF8String()
		{
			if (this.buffer[this.position] == 255)
			{
				this.position++;
				return null;
			}
			int length = (int)base.ReadCompressedUInt32();
			if (length == 0)
			{
				return string.Empty;
			}
			if (this.position + length > this.buffer.Length)
			{
				return string.Empty;
			}
			string @string = Encoding.UTF8.GetString(this.buffer, this.position, length);
			this.position += length;
			return @string;
		}

		// Token: 0x06000A9B RID: 2715 RVA: 0x00024574 File Offset: 0x00022774
		public string ReadDocumentName()
		{
			char separator = (char)this.buffer[this.position];
			this.position++;
			StringBuilder builder = new StringBuilder();
			int i = 0;
			while (this.CanReadMore())
			{
				if (i > 0 && separator != '\0')
				{
					builder.Append(separator);
				}
				uint part = base.ReadCompressedUInt32();
				if (part != 0U)
				{
					builder.Append(this.reader.ReadUTF8StringBlob(part));
				}
				i++;
			}
			return builder.ToString();
		}

		// Token: 0x06000A9C RID: 2716 RVA: 0x000245E8 File Offset: 0x000227E8
		public Collection<SequencePoint> ReadSequencePoints(Document document)
		{
			base.ReadCompressedUInt32();
			if (document == null)
			{
				document = this.reader.GetDocument(base.ReadCompressedUInt32());
			}
			int offset = 0;
			int start_line = 0;
			int start_column = 0;
			bool first_non_hidden = true;
			Collection<SequencePoint> sequence_points = new Collection<SequencePoint>((int)((ulong)this.sig_length - (ulong)((long)this.position - (long)((ulong)this.start))) / 5);
			int i = 0;
			while (this.CanReadMore())
			{
				int delta_il = (int)base.ReadCompressedUInt32();
				if (i > 0 && delta_il == 0)
				{
					document = this.reader.GetDocument(base.ReadCompressedUInt32());
				}
				else
				{
					offset += delta_il;
					int delta_lines = (int)base.ReadCompressedUInt32();
					int delta_columns = (int)((delta_lines == 0) ? base.ReadCompressedUInt32() : ((uint)base.ReadCompressedInt32()));
					if (delta_lines == 0 && delta_columns == 0)
					{
						sequence_points.Add(new SequencePoint(offset, document)
						{
							StartLine = 16707566,
							EndLine = 16707566,
							StartColumn = 0,
							EndColumn = 0
						});
					}
					else
					{
						if (first_non_hidden)
						{
							start_line = (int)base.ReadCompressedUInt32();
							start_column = (int)base.ReadCompressedUInt32();
						}
						else
						{
							start_line += base.ReadCompressedInt32();
							start_column += base.ReadCompressedInt32();
						}
						sequence_points.Add(new SequencePoint(offset, document)
						{
							StartLine = start_line,
							StartColumn = start_column,
							EndLine = start_line + delta_lines,
							EndColumn = start_column + delta_columns
						});
						first_non_hidden = false;
					}
				}
				i++;
			}
			return sequence_points;
		}

		// Token: 0x06000A9D RID: 2717 RVA: 0x00024733 File Offset: 0x00022933
		public bool CanReadMore()
		{
			return (long)this.position - (long)((ulong)this.start) < (long)((ulong)this.sig_length);
		}

		// Token: 0x04000346 RID: 838
		private readonly MetadataReader reader;

		// Token: 0x04000347 RID: 839
		internal readonly uint start;

		// Token: 0x04000348 RID: 840
		internal readonly uint sig_length;
	}
}
