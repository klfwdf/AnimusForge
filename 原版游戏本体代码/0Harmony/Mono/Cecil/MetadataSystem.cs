using System;
using System.Collections.Generic;
using System.Threading;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000269 RID: 617
	internal sealed class MetadataSystem
	{
		// Token: 0x06000DFE RID: 3582 RVA: 0x0002E160 File Offset: 0x0002C360
		private static void InitializePrimitives()
		{
			Dictionary<string, Row<ElementType, bool>> types = new Dictionary<string, Row<ElementType, bool>>(18, StringComparer.Ordinal)
			{
				{
					"Void",
					new Row<ElementType, bool>(ElementType.Void, false)
				},
				{
					"Boolean",
					new Row<ElementType, bool>(ElementType.Boolean, true)
				},
				{
					"Char",
					new Row<ElementType, bool>(ElementType.Char, true)
				},
				{
					"SByte",
					new Row<ElementType, bool>(ElementType.I1, true)
				},
				{
					"Byte",
					new Row<ElementType, bool>(ElementType.U1, true)
				},
				{
					"Int16",
					new Row<ElementType, bool>(ElementType.I2, true)
				},
				{
					"UInt16",
					new Row<ElementType, bool>(ElementType.U2, true)
				},
				{
					"Int32",
					new Row<ElementType, bool>(ElementType.I4, true)
				},
				{
					"UInt32",
					new Row<ElementType, bool>(ElementType.U4, true)
				},
				{
					"Int64",
					new Row<ElementType, bool>(ElementType.I8, true)
				},
				{
					"UInt64",
					new Row<ElementType, bool>(ElementType.U8, true)
				},
				{
					"Single",
					new Row<ElementType, bool>(ElementType.R4, true)
				},
				{
					"Double",
					new Row<ElementType, bool>(ElementType.R8, true)
				},
				{
					"String",
					new Row<ElementType, bool>(ElementType.String, false)
				},
				{
					"TypedReference",
					new Row<ElementType, bool>(ElementType.TypedByRef, false)
				},
				{
					"IntPtr",
					new Row<ElementType, bool>(ElementType.I, true)
				},
				{
					"UIntPtr",
					new Row<ElementType, bool>(ElementType.U, true)
				},
				{
					"Object",
					new Row<ElementType, bool>(ElementType.Object, false)
				}
			};
			Interlocked.CompareExchange<Dictionary<string, Row<ElementType, bool>>>(ref MetadataSystem.primitive_value_types, types, null);
		}

		// Token: 0x06000DFF RID: 3583 RVA: 0x0002E2D8 File Offset: 0x0002C4D8
		public static void TryProcessPrimitiveTypeReference(TypeReference type)
		{
			if (type.Namespace != "System")
			{
				return;
			}
			IMetadataScope scope = type.scope;
			if (scope == null || scope.MetadataScopeType != MetadataScopeType.AssemblyNameReference)
			{
				return;
			}
			Row<ElementType, bool> primitive_data;
			if (!MetadataSystem.TryGetPrimitiveData(type, out primitive_data))
			{
				return;
			}
			type.etype = primitive_data.Col1;
			type.IsValueType = primitive_data.Col2;
		}

		// Token: 0x06000E00 RID: 3584 RVA: 0x0002E330 File Offset: 0x0002C530
		public static bool TryGetPrimitiveElementType(TypeDefinition type, out ElementType etype)
		{
			etype = ElementType.None;
			if (type.Namespace != "System")
			{
				return false;
			}
			Row<ElementType, bool> primitive_data;
			if (MetadataSystem.TryGetPrimitiveData(type, out primitive_data))
			{
				etype = primitive_data.Col1;
				return true;
			}
			return false;
		}

		// Token: 0x06000E01 RID: 3585 RVA: 0x0002E369 File Offset: 0x0002C569
		private static bool TryGetPrimitiveData(TypeReference type, out Row<ElementType, bool> primitive_data)
		{
			if (MetadataSystem.primitive_value_types == null)
			{
				MetadataSystem.InitializePrimitives();
			}
			return MetadataSystem.primitive_value_types.TryGetValue(type.Name, out primitive_data);
		}

		// Token: 0x06000E02 RID: 3586 RVA: 0x0002E388 File Offset: 0x0002C588
		public void Clear()
		{
			if (this.NestedTypes != null)
			{
				this.NestedTypes = new Dictionary<uint, Collection<uint>>(0);
			}
			if (this.ReverseNestedTypes != null)
			{
				this.ReverseNestedTypes = new Dictionary<uint, uint>(0);
			}
			if (this.Interfaces != null)
			{
				this.Interfaces = new Dictionary<uint, Collection<Row<uint, MetadataToken>>>(0);
			}
			if (this.ClassLayouts != null)
			{
				this.ClassLayouts = new Dictionary<uint, Row<ushort, uint>>(0);
			}
			if (this.FieldLayouts != null)
			{
				this.FieldLayouts = new Dictionary<uint, uint>(0);
			}
			if (this.FieldRVAs != null)
			{
				this.FieldRVAs = new Dictionary<uint, uint>(0);
			}
			if (this.FieldMarshals != null)
			{
				this.FieldMarshals = new Dictionary<MetadataToken, uint>(0);
			}
			if (this.Constants != null)
			{
				this.Constants = new Dictionary<MetadataToken, Row<ElementType, uint>>(0);
			}
			if (this.Overrides != null)
			{
				this.Overrides = new Dictionary<uint, Collection<MetadataToken>>(0);
			}
			if (this.CustomAttributes != null)
			{
				this.CustomAttributes = new Dictionary<MetadataToken, Range[]>(0);
			}
			if (this.SecurityDeclarations != null)
			{
				this.SecurityDeclarations = new Dictionary<MetadataToken, Range[]>(0);
			}
			if (this.Events != null)
			{
				this.Events = new Dictionary<uint, Range>(0);
			}
			if (this.Properties != null)
			{
				this.Properties = new Dictionary<uint, Range>(0);
			}
			if (this.Semantics != null)
			{
				this.Semantics = new Dictionary<uint, Row<MethodSemanticsAttributes, MetadataToken>>(0);
			}
			if (this.PInvokes != null)
			{
				this.PInvokes = new Dictionary<uint, Row<PInvokeAttributes, uint, uint>>(0);
			}
			if (this.GenericParameters != null)
			{
				this.GenericParameters = new Dictionary<MetadataToken, Range[]>(0);
			}
			if (this.GenericConstraints != null)
			{
				this.GenericConstraints = new Dictionary<uint, Collection<Row<uint, MetadataToken>>>(0);
			}
			this.Documents = Empty<Document>.Array;
			this.ImportScopes = Empty<ImportDebugInformation>.Array;
			if (this.LocalScopes != null)
			{
				this.LocalScopes = new Dictionary<uint, Collection<Row<uint, Range, Range, uint, uint, uint>>>(0);
			}
			if (this.StateMachineMethods != null)
			{
				this.StateMachineMethods = new Dictionary<uint, uint>(0);
			}
		}

		// Token: 0x06000E03 RID: 3587 RVA: 0x0002E527 File Offset: 0x0002C727
		public AssemblyNameReference GetAssemblyNameReference(uint rid)
		{
			if (rid < 1U || (ulong)rid > (ulong)((long)this.AssemblyReferences.Length))
			{
				return null;
			}
			return this.AssemblyReferences[(int)(rid - 1U)];
		}

		// Token: 0x06000E04 RID: 3588 RVA: 0x0002E546 File Offset: 0x0002C746
		public TypeDefinition GetTypeDefinition(uint rid)
		{
			if (rid < 1U || (ulong)rid > (ulong)((long)this.Types.Length))
			{
				return null;
			}
			return this.Types[(int)(rid - 1U)];
		}

		// Token: 0x06000E05 RID: 3589 RVA: 0x0002E565 File Offset: 0x0002C765
		public void AddTypeDefinition(TypeDefinition type)
		{
			this.Types[(int)(type.token.RID - 1U)] = type;
		}

		// Token: 0x06000E06 RID: 3590 RVA: 0x0002E57C File Offset: 0x0002C77C
		public TypeReference GetTypeReference(uint rid)
		{
			if (rid < 1U || (ulong)rid > (ulong)((long)this.TypeReferences.Length))
			{
				return null;
			}
			return this.TypeReferences[(int)(rid - 1U)];
		}

		// Token: 0x06000E07 RID: 3591 RVA: 0x0002E59B File Offset: 0x0002C79B
		public void AddTypeReference(TypeReference type)
		{
			this.TypeReferences[(int)(type.token.RID - 1U)] = type;
		}

		// Token: 0x06000E08 RID: 3592 RVA: 0x0002E5B2 File Offset: 0x0002C7B2
		public FieldDefinition GetFieldDefinition(uint rid)
		{
			if (rid < 1U || (ulong)rid > (ulong)((long)this.Fields.Length))
			{
				return null;
			}
			return this.Fields[(int)(rid - 1U)];
		}

		// Token: 0x06000E09 RID: 3593 RVA: 0x0002E5D1 File Offset: 0x0002C7D1
		public void AddFieldDefinition(FieldDefinition field)
		{
			this.Fields[(int)(field.token.RID - 1U)] = field;
		}

		// Token: 0x06000E0A RID: 3594 RVA: 0x0002E5E8 File Offset: 0x0002C7E8
		public MethodDefinition GetMethodDefinition(uint rid)
		{
			if (rid < 1U || (ulong)rid > (ulong)((long)this.Methods.Length))
			{
				return null;
			}
			return this.Methods[(int)(rid - 1U)];
		}

		// Token: 0x06000E0B RID: 3595 RVA: 0x0002E607 File Offset: 0x0002C807
		public void AddMethodDefinition(MethodDefinition method)
		{
			this.Methods[(int)(method.token.RID - 1U)] = method;
		}

		// Token: 0x06000E0C RID: 3596 RVA: 0x0002E61E File Offset: 0x0002C81E
		public MemberReference GetMemberReference(uint rid)
		{
			if (rid < 1U || (ulong)rid > (ulong)((long)this.MemberReferences.Length))
			{
				return null;
			}
			return this.MemberReferences[(int)(rid - 1U)];
		}

		// Token: 0x06000E0D RID: 3597 RVA: 0x0002E63D File Offset: 0x0002C83D
		public void AddMemberReference(MemberReference member)
		{
			this.MemberReferences[(int)(member.token.RID - 1U)] = member;
		}

		// Token: 0x06000E0E RID: 3598 RVA: 0x0002E654 File Offset: 0x0002C854
		public bool TryGetNestedTypeMapping(TypeDefinition type, out Collection<uint> mapping)
		{
			return this.NestedTypes.TryGetValue(type.token.RID, out mapping);
		}

		// Token: 0x06000E0F RID: 3599 RVA: 0x0002E66D File Offset: 0x0002C86D
		public void SetNestedTypeMapping(uint type_rid, Collection<uint> mapping)
		{
			this.NestedTypes[type_rid] = mapping;
		}

		// Token: 0x06000E10 RID: 3600 RVA: 0x0002E67C File Offset: 0x0002C87C
		public bool TryGetReverseNestedTypeMapping(TypeDefinition type, out uint declaring)
		{
			return this.ReverseNestedTypes.TryGetValue(type.token.RID, out declaring);
		}

		// Token: 0x06000E11 RID: 3601 RVA: 0x0002E695 File Offset: 0x0002C895
		public void SetReverseNestedTypeMapping(uint nested, uint declaring)
		{
			this.ReverseNestedTypes[nested] = declaring;
		}

		// Token: 0x06000E12 RID: 3602 RVA: 0x0002E6A4 File Offset: 0x0002C8A4
		public bool TryGetInterfaceMapping(TypeDefinition type, out Collection<Row<uint, MetadataToken>> mapping)
		{
			return this.Interfaces.TryGetValue(type.token.RID, out mapping);
		}

		// Token: 0x06000E13 RID: 3603 RVA: 0x0002E6BD File Offset: 0x0002C8BD
		public void SetInterfaceMapping(uint type_rid, Collection<Row<uint, MetadataToken>> mapping)
		{
			this.Interfaces[type_rid] = mapping;
		}

		// Token: 0x06000E14 RID: 3604 RVA: 0x0002E6CC File Offset: 0x0002C8CC
		public void AddPropertiesRange(uint type_rid, Range range)
		{
			this.Properties.Add(type_rid, range);
		}

		// Token: 0x06000E15 RID: 3605 RVA: 0x0002E6DB File Offset: 0x0002C8DB
		public bool TryGetPropertiesRange(TypeDefinition type, out Range range)
		{
			return this.Properties.TryGetValue(type.token.RID, out range);
		}

		// Token: 0x06000E16 RID: 3606 RVA: 0x0002E6F4 File Offset: 0x0002C8F4
		public void AddEventsRange(uint type_rid, Range range)
		{
			this.Events.Add(type_rid, range);
		}

		// Token: 0x06000E17 RID: 3607 RVA: 0x0002E703 File Offset: 0x0002C903
		public bool TryGetEventsRange(TypeDefinition type, out Range range)
		{
			return this.Events.TryGetValue(type.token.RID, out range);
		}

		// Token: 0x06000E18 RID: 3608 RVA: 0x0002E71C File Offset: 0x0002C91C
		public bool TryGetGenericParameterRanges(IGenericParameterProvider owner, out Range[] ranges)
		{
			return this.GenericParameters.TryGetValue(owner.MetadataToken, out ranges);
		}

		// Token: 0x06000E19 RID: 3609 RVA: 0x0002E730 File Offset: 0x0002C930
		public bool TryGetCustomAttributeRanges(ICustomAttributeProvider owner, out Range[] ranges)
		{
			return this.CustomAttributes.TryGetValue(owner.MetadataToken, out ranges);
		}

		// Token: 0x06000E1A RID: 3610 RVA: 0x0002E744 File Offset: 0x0002C944
		public bool TryGetSecurityDeclarationRanges(ISecurityDeclarationProvider owner, out Range[] ranges)
		{
			return this.SecurityDeclarations.TryGetValue(owner.MetadataToken, out ranges);
		}

		// Token: 0x06000E1B RID: 3611 RVA: 0x0002E758 File Offset: 0x0002C958
		public bool TryGetGenericConstraintMapping(GenericParameter generic_parameter, out Collection<Row<uint, MetadataToken>> mapping)
		{
			return this.GenericConstraints.TryGetValue(generic_parameter.token.RID, out mapping);
		}

		// Token: 0x06000E1C RID: 3612 RVA: 0x0002E771 File Offset: 0x0002C971
		public void SetGenericConstraintMapping(uint gp_rid, Collection<Row<uint, MetadataToken>> mapping)
		{
			this.GenericConstraints[gp_rid] = mapping;
		}

		// Token: 0x06000E1D RID: 3613 RVA: 0x0002E780 File Offset: 0x0002C980
		public bool TryGetOverrideMapping(MethodDefinition method, out Collection<MetadataToken> mapping)
		{
			return this.Overrides.TryGetValue(method.token.RID, out mapping);
		}

		// Token: 0x06000E1E RID: 3614 RVA: 0x0002E799 File Offset: 0x0002C999
		public void SetOverrideMapping(uint rid, Collection<MetadataToken> mapping)
		{
			this.Overrides[rid] = mapping;
		}

		// Token: 0x06000E1F RID: 3615 RVA: 0x0002E7A8 File Offset: 0x0002C9A8
		public Document GetDocument(uint rid)
		{
			if (rid < 1U || (ulong)rid > (ulong)((long)this.Documents.Length))
			{
				return null;
			}
			return this.Documents[(int)(rid - 1U)];
		}

		// Token: 0x06000E20 RID: 3616 RVA: 0x0002E7C8 File Offset: 0x0002C9C8
		public bool TryGetLocalScopes(MethodDefinition method, out Collection<Row<uint, Range, Range, uint, uint, uint>> scopes)
		{
			return this.LocalScopes.TryGetValue(method.MetadataToken.RID, out scopes);
		}

		// Token: 0x06000E21 RID: 3617 RVA: 0x0002E7EF File Offset: 0x0002C9EF
		public void SetLocalScopes(uint method_rid, Collection<Row<uint, Range, Range, uint, uint, uint>> records)
		{
			this.LocalScopes[method_rid] = records;
		}

		// Token: 0x06000E22 RID: 3618 RVA: 0x0002E7FE File Offset: 0x0002C9FE
		public ImportDebugInformation GetImportScope(uint rid)
		{
			if (rid < 1U || (ulong)rid > (ulong)((long)this.ImportScopes.Length))
			{
				return null;
			}
			return this.ImportScopes[(int)(rid - 1U)];
		}

		// Token: 0x06000E23 RID: 3619 RVA: 0x0002E820 File Offset: 0x0002CA20
		public bool TryGetStateMachineKickOffMethod(MethodDefinition method, out uint rid)
		{
			return this.StateMachineMethods.TryGetValue(method.MetadataToken.RID, out rid);
		}

		// Token: 0x06000E24 RID: 3620 RVA: 0x0002E847 File Offset: 0x0002CA47
		public TypeDefinition GetFieldDeclaringType(uint field_rid)
		{
			return MetadataSystem.BinaryRangeSearch(this.Types, field_rid, true);
		}

		// Token: 0x06000E25 RID: 3621 RVA: 0x0002E856 File Offset: 0x0002CA56
		public TypeDefinition GetMethodDeclaringType(uint method_rid)
		{
			return MetadataSystem.BinaryRangeSearch(this.Types, method_rid, false);
		}

		// Token: 0x06000E26 RID: 3622 RVA: 0x0002E868 File Offset: 0x0002CA68
		private static TypeDefinition BinaryRangeSearch(TypeDefinition[] types, uint rid, bool field)
		{
			int min = 0;
			int max = types.Length - 1;
			while (min <= max)
			{
				int mid = min + (max - min) / 2;
				TypeDefinition type = types[mid];
				Range range = (field ? type.fields_range : type.methods_range);
				if (rid < range.Start)
				{
					max = mid - 1;
				}
				else
				{
					if (rid < range.Start + range.Length)
					{
						return type;
					}
					min = mid + 1;
				}
			}
			return null;
		}

		// Token: 0x0400041F RID: 1055
		internal AssemblyNameReference[] AssemblyReferences;

		// Token: 0x04000420 RID: 1056
		internal ModuleReference[] ModuleReferences;

		// Token: 0x04000421 RID: 1057
		internal TypeDefinition[] Types;

		// Token: 0x04000422 RID: 1058
		internal TypeReference[] TypeReferences;

		// Token: 0x04000423 RID: 1059
		internal FieldDefinition[] Fields;

		// Token: 0x04000424 RID: 1060
		internal MethodDefinition[] Methods;

		// Token: 0x04000425 RID: 1061
		internal MemberReference[] MemberReferences;

		// Token: 0x04000426 RID: 1062
		internal Dictionary<uint, Collection<uint>> NestedTypes;

		// Token: 0x04000427 RID: 1063
		internal Dictionary<uint, uint> ReverseNestedTypes;

		// Token: 0x04000428 RID: 1064
		internal Dictionary<uint, Collection<Row<uint, MetadataToken>>> Interfaces;

		// Token: 0x04000429 RID: 1065
		internal Dictionary<uint, Row<ushort, uint>> ClassLayouts;

		// Token: 0x0400042A RID: 1066
		internal Dictionary<uint, uint> FieldLayouts;

		// Token: 0x0400042B RID: 1067
		internal Dictionary<uint, uint> FieldRVAs;

		// Token: 0x0400042C RID: 1068
		internal Dictionary<MetadataToken, uint> FieldMarshals;

		// Token: 0x0400042D RID: 1069
		internal Dictionary<MetadataToken, Row<ElementType, uint>> Constants;

		// Token: 0x0400042E RID: 1070
		internal Dictionary<uint, Collection<MetadataToken>> Overrides;

		// Token: 0x0400042F RID: 1071
		internal Dictionary<MetadataToken, Range[]> CustomAttributes;

		// Token: 0x04000430 RID: 1072
		internal Dictionary<MetadataToken, Range[]> SecurityDeclarations;

		// Token: 0x04000431 RID: 1073
		internal Dictionary<uint, Range> Events;

		// Token: 0x04000432 RID: 1074
		internal Dictionary<uint, Range> Properties;

		// Token: 0x04000433 RID: 1075
		internal Dictionary<uint, Row<MethodSemanticsAttributes, MetadataToken>> Semantics;

		// Token: 0x04000434 RID: 1076
		internal Dictionary<uint, Row<PInvokeAttributes, uint, uint>> PInvokes;

		// Token: 0x04000435 RID: 1077
		internal Dictionary<MetadataToken, Range[]> GenericParameters;

		// Token: 0x04000436 RID: 1078
		internal Dictionary<uint, Collection<Row<uint, MetadataToken>>> GenericConstraints;

		// Token: 0x04000437 RID: 1079
		internal Document[] Documents;

		// Token: 0x04000438 RID: 1080
		internal Dictionary<uint, Collection<Row<uint, Range, Range, uint, uint, uint>>> LocalScopes;

		// Token: 0x04000439 RID: 1081
		internal ImportDebugInformation[] ImportScopes;

		// Token: 0x0400043A RID: 1082
		internal Dictionary<uint, uint> StateMachineMethods;

		// Token: 0x0400043B RID: 1083
		internal Dictionary<MetadataToken, Row<Guid, uint, uint>[]> CustomDebugInformations;

		// Token: 0x0400043C RID: 1084
		private static Dictionary<string, Row<ElementType, bool>> primitive_value_types;
	}
}
