using System;
using System.Threading;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x0200026C RID: 620
	internal sealed class MethodDefinition : MethodReference, IMemberDefinition, ICustomAttributeProvider, IMetadataTokenProvider, ISecurityDeclarationProvider, ICustomDebugInformationProvider
	{
		// Token: 0x1700033C RID: 828
		// (get) Token: 0x06000E28 RID: 3624 RVA: 0x0002E8CD File Offset: 0x0002CACD
		// (set) Token: 0x06000E29 RID: 3625 RVA: 0x0002E8D5 File Offset: 0x0002CAD5
		public override string Name
		{
			get
			{
				return base.Name;
			}
			set
			{
				if (base.IsWindowsRuntimeProjection && value != base.Name)
				{
					throw new InvalidOperationException();
				}
				base.Name = value;
			}
		}

		// Token: 0x1700033D RID: 829
		// (get) Token: 0x06000E2A RID: 3626 RVA: 0x0002E8FA File Offset: 0x0002CAFA
		// (set) Token: 0x06000E2B RID: 3627 RVA: 0x0002E902 File Offset: 0x0002CB02
		public MethodAttributes Attributes
		{
			get
			{
				return (MethodAttributes)this.attributes;
			}
			set
			{
				if (base.IsWindowsRuntimeProjection && value != (MethodAttributes)this.attributes)
				{
					throw new InvalidOperationException();
				}
				this.attributes = (ushort)value;
			}
		}

		// Token: 0x1700033E RID: 830
		// (get) Token: 0x06000E2C RID: 3628 RVA: 0x0002E922 File Offset: 0x0002CB22
		// (set) Token: 0x06000E2D RID: 3629 RVA: 0x0002E92A File Offset: 0x0002CB2A
		public MethodImplAttributes ImplAttributes
		{
			get
			{
				return (MethodImplAttributes)this.impl_attributes;
			}
			set
			{
				if (base.IsWindowsRuntimeProjection && value != (MethodImplAttributes)this.impl_attributes)
				{
					throw new InvalidOperationException();
				}
				this.impl_attributes = (ushort)value;
			}
		}

		// Token: 0x1700033F RID: 831
		// (get) Token: 0x06000E2E RID: 3630 RVA: 0x0002E94A File Offset: 0x0002CB4A
		// (set) Token: 0x06000E2F RID: 3631 RVA: 0x0002E988 File Offset: 0x0002CB88
		public MethodSemanticsAttributes SemanticsAttributes
		{
			get
			{
				if (this.sem_attrs_ready)
				{
					return this.sem_attrs;
				}
				if (base.HasImage)
				{
					this.ReadSemantics();
					return this.sem_attrs;
				}
				this.sem_attrs = MethodSemanticsAttributes.None;
				this.sem_attrs_ready = true;
				return this.sem_attrs;
			}
			set
			{
				this.sem_attrs = value;
			}
		}

		// Token: 0x17000340 RID: 832
		// (get) Token: 0x06000E30 RID: 3632 RVA: 0x0002E991 File Offset: 0x0002CB91
		// (set) Token: 0x06000E31 RID: 3633 RVA: 0x0002B182 File Offset: 0x00029382
		internal MethodDefinitionProjection WindowsRuntimeProjection
		{
			get
			{
				return (MethodDefinitionProjection)this.projection;
			}
			set
			{
				this.projection = value;
			}
		}

		// Token: 0x06000E32 RID: 3634 RVA: 0x0002E9A0 File Offset: 0x0002CBA0
		internal void ReadSemantics()
		{
			if (this.sem_attrs_ready)
			{
				return;
			}
			ModuleDefinition module = this.Module;
			if (module == null)
			{
				return;
			}
			if (!module.HasImage)
			{
				return;
			}
			object syncRoot = module.SyncRoot;
			lock (syncRoot)
			{
				if (!this.sem_attrs_ready)
				{
					module.Read<MethodDefinition>(this, delegate(MethodDefinition method, MetadataReader reader)
					{
						reader.ReadAllSemantics(method);
					});
				}
			}
		}

		// Token: 0x17000341 RID: 833
		// (get) Token: 0x06000E33 RID: 3635 RVA: 0x0002EA2C File Offset: 0x0002CC2C
		public bool HasSecurityDeclarations
		{
			get
			{
				if (this.security_declarations != null)
				{
					return this.security_declarations.Count > 0;
				}
				return this.GetHasSecurityDeclarations(this.Module);
			}
		}

		// Token: 0x17000342 RID: 834
		// (get) Token: 0x06000E34 RID: 3636 RVA: 0x0002EA51 File Offset: 0x0002CC51
		public Collection<SecurityDeclaration> SecurityDeclarations
		{
			get
			{
				return this.security_declarations ?? this.GetSecurityDeclarations(ref this.security_declarations, this.Module);
			}
		}

		// Token: 0x17000343 RID: 835
		// (get) Token: 0x06000E35 RID: 3637 RVA: 0x0002EA6F File Offset: 0x0002CC6F
		public bool HasCustomAttributes
		{
			get
			{
				if (this.custom_attributes != null)
				{
					return this.custom_attributes.Count > 0;
				}
				return this.GetHasCustomAttributes(this.Module);
			}
		}

		// Token: 0x17000344 RID: 836
		// (get) Token: 0x06000E36 RID: 3638 RVA: 0x0002EA94 File Offset: 0x0002CC94
		public Collection<CustomAttribute> CustomAttributes
		{
			get
			{
				return this.custom_attributes ?? this.GetCustomAttributes(ref this.custom_attributes, this.Module);
			}
		}

		// Token: 0x17000345 RID: 837
		// (get) Token: 0x06000E37 RID: 3639 RVA: 0x0002EAB2 File Offset: 0x0002CCB2
		public int RVA
		{
			get
			{
				return (int)this.rva;
			}
		}

		// Token: 0x17000346 RID: 838
		// (get) Token: 0x06000E38 RID: 3640 RVA: 0x0002EABC File Offset: 0x0002CCBC
		public bool HasBody
		{
			get
			{
				return (this.attributes & 1024) == 0 && (this.attributes & 8192) == 0 && (this.impl_attributes & 4096) == 0 && (this.impl_attributes & 1) == 0 && (this.impl_attributes & 4) == 0 && (this.impl_attributes & 3) == 0;
			}
		}

		// Token: 0x17000347 RID: 839
		// (get) Token: 0x06000E39 RID: 3641 RVA: 0x0002EB14 File Offset: 0x0002CD14
		// (set) Token: 0x06000E3A RID: 3642 RVA: 0x0002EB94 File Offset: 0x0002CD94
		public MethodBody Body
		{
			get
			{
				MethodBody local = this.body;
				if (local != null)
				{
					return local;
				}
				if (!this.HasBody)
				{
					return null;
				}
				if (base.HasImage && this.rva != 0U)
				{
					return this.Module.Read<MethodDefinition, MethodBody>(ref this.body, this, (MethodDefinition method, MetadataReader reader) => reader.ReadMethodBody(method));
				}
				Interlocked.CompareExchange<MethodBody>(ref this.body, new MethodBody(this), null);
				return this.body;
			}
			set
			{
				ModuleDefinition module = this.Module;
				if (module == null)
				{
					this.body = value;
					return;
				}
				object syncRoot = module.SyncRoot;
				lock (syncRoot)
				{
					this.body = value;
					if (value == null)
					{
						this.debug_info = null;
					}
				}
			}
		}

		// Token: 0x17000348 RID: 840
		// (get) Token: 0x06000E3B RID: 3643 RVA: 0x0002EBF4 File Offset: 0x0002CDF4
		// (set) Token: 0x06000E3C RID: 3644 RVA: 0x0002EC22 File Offset: 0x0002CE22
		public MethodDebugInformation DebugInformation
		{
			get
			{
				Mixin.Read(this.Body);
				if (this.debug_info == null)
				{
					Interlocked.CompareExchange<MethodDebugInformation>(ref this.debug_info, new MethodDebugInformation(this), null);
				}
				return this.debug_info;
			}
			set
			{
				this.debug_info = value;
			}
		}

		// Token: 0x17000349 RID: 841
		// (get) Token: 0x06000E3D RID: 3645 RVA: 0x0002EC2B File Offset: 0x0002CE2B
		public bool HasPInvokeInfo
		{
			get
			{
				return this.pinvoke != null || this.IsPInvokeImpl;
			}
		}

		// Token: 0x1700034A RID: 842
		// (get) Token: 0x06000E3E RID: 3646 RVA: 0x0002EC40 File Offset: 0x0002CE40
		// (set) Token: 0x06000E3F RID: 3647 RVA: 0x0002EC9F File Offset: 0x0002CE9F
		public PInvokeInfo PInvokeInfo
		{
			get
			{
				if (this.pinvoke != null)
				{
					return this.pinvoke;
				}
				if (base.HasImage && this.IsPInvokeImpl)
				{
					return this.Module.Read<MethodDefinition, PInvokeInfo>(ref this.pinvoke, this, (MethodDefinition method, MetadataReader reader) => reader.ReadPInvokeInfo(method));
				}
				return null;
			}
			set
			{
				this.IsPInvokeImpl = true;
				this.pinvoke = value;
			}
		}

		// Token: 0x1700034B RID: 843
		// (get) Token: 0x06000E40 RID: 3648 RVA: 0x0002ECB0 File Offset: 0x0002CEB0
		public bool HasOverrides
		{
			get
			{
				if (this.overrides != null)
				{
					return this.overrides.Count > 0;
				}
				if (base.HasImage)
				{
					return this.Module.Read<MethodDefinition, bool>(this, (MethodDefinition method, MetadataReader reader) => reader.HasOverrides(method));
				}
				return false;
			}
		}

		// Token: 0x1700034C RID: 844
		// (get) Token: 0x06000E41 RID: 3649 RVA: 0x0002ED0C File Offset: 0x0002CF0C
		public Collection<MethodReference> Overrides
		{
			get
			{
				if (this.overrides != null)
				{
					return this.overrides;
				}
				if (base.HasImage)
				{
					return this.Module.Read<MethodDefinition, Collection<MethodReference>>(ref this.overrides, this, (MethodDefinition method, MetadataReader reader) => reader.ReadOverrides(method));
				}
				Interlocked.CompareExchange<Collection<MethodReference>>(ref this.overrides, new Collection<MethodReference>(), null);
				return this.overrides;
			}
		}

		// Token: 0x1700034D RID: 845
		// (get) Token: 0x06000E42 RID: 3650 RVA: 0x0002ED7A File Offset: 0x0002CF7A
		public override bool HasGenericParameters
		{
			get
			{
				if (this.generic_parameters != null)
				{
					return this.generic_parameters.Count > 0;
				}
				return this.GetHasGenericParameters(this.Module);
			}
		}

		// Token: 0x1700034E RID: 846
		// (get) Token: 0x06000E43 RID: 3651 RVA: 0x0002ED9F File Offset: 0x0002CF9F
		public override Collection<GenericParameter> GenericParameters
		{
			get
			{
				return this.generic_parameters ?? this.GetGenericParameters(ref this.generic_parameters, this.Module);
			}
		}

		// Token: 0x1700034F RID: 847
		// (get) Token: 0x06000E44 RID: 3652 RVA: 0x0002EDBD File Offset: 0x0002CFBD
		public bool HasCustomDebugInformations
		{
			get
			{
				Mixin.Read(this.Body);
				return !this.custom_infos.IsNullOrEmpty<CustomDebugInformation>();
			}
		}

		// Token: 0x17000350 RID: 848
		// (get) Token: 0x06000E45 RID: 3653 RVA: 0x0002EDD8 File Offset: 0x0002CFD8
		public Collection<CustomDebugInformation> CustomDebugInformations
		{
			get
			{
				Mixin.Read(this.Body);
				if (this.custom_infos == null)
				{
					Interlocked.CompareExchange<Collection<CustomDebugInformation>>(ref this.custom_infos, new Collection<CustomDebugInformation>(), null);
				}
				return this.custom_infos;
			}
		}

		// Token: 0x17000351 RID: 849
		// (get) Token: 0x06000E46 RID: 3654 RVA: 0x0002EE05 File Offset: 0x0002D005
		// (set) Token: 0x06000E47 RID: 3655 RVA: 0x0002EE14 File Offset: 0x0002D014
		public bool IsCompilerControlled
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 0U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 0U, value);
			}
		}

		// Token: 0x17000352 RID: 850
		// (get) Token: 0x06000E48 RID: 3656 RVA: 0x0002EE2A File Offset: 0x0002D02A
		// (set) Token: 0x06000E49 RID: 3657 RVA: 0x0002EE39 File Offset: 0x0002D039
		public bool IsPrivate
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 1U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 1U, value);
			}
		}

		// Token: 0x17000353 RID: 851
		// (get) Token: 0x06000E4A RID: 3658 RVA: 0x0002EE4F File Offset: 0x0002D04F
		// (set) Token: 0x06000E4B RID: 3659 RVA: 0x0002EE5E File Offset: 0x0002D05E
		public bool IsFamilyAndAssembly
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 2U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 2U, value);
			}
		}

		// Token: 0x17000354 RID: 852
		// (get) Token: 0x06000E4C RID: 3660 RVA: 0x0002EE74 File Offset: 0x0002D074
		// (set) Token: 0x06000E4D RID: 3661 RVA: 0x0002EE83 File Offset: 0x0002D083
		public bool IsAssembly
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 3U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 3U, value);
			}
		}

		// Token: 0x17000355 RID: 853
		// (get) Token: 0x06000E4E RID: 3662 RVA: 0x0002EE99 File Offset: 0x0002D099
		// (set) Token: 0x06000E4F RID: 3663 RVA: 0x0002EEA8 File Offset: 0x0002D0A8
		public bool IsFamily
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 4U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 4U, value);
			}
		}

		// Token: 0x17000356 RID: 854
		// (get) Token: 0x06000E50 RID: 3664 RVA: 0x0002EEBE File Offset: 0x0002D0BE
		// (set) Token: 0x06000E51 RID: 3665 RVA: 0x0002EECD File Offset: 0x0002D0CD
		public bool IsFamilyOrAssembly
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 5U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 5U, value);
			}
		}

		// Token: 0x17000357 RID: 855
		// (get) Token: 0x06000E52 RID: 3666 RVA: 0x0002EEE3 File Offset: 0x0002D0E3
		// (set) Token: 0x06000E53 RID: 3667 RVA: 0x0002EEF2 File Offset: 0x0002D0F2
		public bool IsPublic
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7, 6U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7, 6U, value);
			}
		}

		// Token: 0x17000358 RID: 856
		// (get) Token: 0x06000E54 RID: 3668 RVA: 0x0002EF08 File Offset: 0x0002D108
		// (set) Token: 0x06000E55 RID: 3669 RVA: 0x0002EF17 File Offset: 0x0002D117
		public bool IsStatic
		{
			get
			{
				return this.attributes.GetAttributes(16);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(16, value);
			}
		}

		// Token: 0x17000359 RID: 857
		// (get) Token: 0x06000E56 RID: 3670 RVA: 0x0002EF2D File Offset: 0x0002D12D
		// (set) Token: 0x06000E57 RID: 3671 RVA: 0x0002EF3C File Offset: 0x0002D13C
		public bool IsFinal
		{
			get
			{
				return this.attributes.GetAttributes(32);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(32, value);
			}
		}

		// Token: 0x1700035A RID: 858
		// (get) Token: 0x06000E58 RID: 3672 RVA: 0x0002EF52 File Offset: 0x0002D152
		// (set) Token: 0x06000E59 RID: 3673 RVA: 0x0002EF61 File Offset: 0x0002D161
		public bool IsVirtual
		{
			get
			{
				return this.attributes.GetAttributes(64);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(64, value);
			}
		}

		// Token: 0x1700035B RID: 859
		// (get) Token: 0x06000E5A RID: 3674 RVA: 0x0002EF77 File Offset: 0x0002D177
		// (set) Token: 0x06000E5B RID: 3675 RVA: 0x0002EF89 File Offset: 0x0002D189
		public bool IsHideBySig
		{
			get
			{
				return this.attributes.GetAttributes(128);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(128, value);
			}
		}

		// Token: 0x1700035C RID: 860
		// (get) Token: 0x06000E5C RID: 3676 RVA: 0x0002EFA2 File Offset: 0x0002D1A2
		// (set) Token: 0x06000E5D RID: 3677 RVA: 0x0002EFB5 File Offset: 0x0002D1B5
		public bool IsReuseSlot
		{
			get
			{
				return this.attributes.GetMaskedAttributes(256, 0U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(256, 0U, value);
			}
		}

		// Token: 0x1700035D RID: 861
		// (get) Token: 0x06000E5E RID: 3678 RVA: 0x0002EFCF File Offset: 0x0002D1CF
		// (set) Token: 0x06000E5F RID: 3679 RVA: 0x0002EFE6 File Offset: 0x0002D1E6
		public bool IsNewSlot
		{
			get
			{
				return this.attributes.GetMaskedAttributes(256, 256U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(256, 256U, value);
			}
		}

		// Token: 0x1700035E RID: 862
		// (get) Token: 0x06000E60 RID: 3680 RVA: 0x0002F004 File Offset: 0x0002D204
		// (set) Token: 0x06000E61 RID: 3681 RVA: 0x0002F016 File Offset: 0x0002D216
		public bool IsCheckAccessOnOverride
		{
			get
			{
				return this.attributes.GetAttributes(512);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(512, value);
			}
		}

		// Token: 0x1700035F RID: 863
		// (get) Token: 0x06000E62 RID: 3682 RVA: 0x0002F02F File Offset: 0x0002D22F
		// (set) Token: 0x06000E63 RID: 3683 RVA: 0x0002F041 File Offset: 0x0002D241
		public bool IsAbstract
		{
			get
			{
				return this.attributes.GetAttributes(1024);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(1024, value);
			}
		}

		// Token: 0x17000360 RID: 864
		// (get) Token: 0x06000E64 RID: 3684 RVA: 0x0002F05A File Offset: 0x0002D25A
		// (set) Token: 0x06000E65 RID: 3685 RVA: 0x0002F06C File Offset: 0x0002D26C
		public bool IsSpecialName
		{
			get
			{
				return this.attributes.GetAttributes(2048);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(2048, value);
			}
		}

		// Token: 0x17000361 RID: 865
		// (get) Token: 0x06000E66 RID: 3686 RVA: 0x0002F085 File Offset: 0x0002D285
		// (set) Token: 0x06000E67 RID: 3687 RVA: 0x0002F097 File Offset: 0x0002D297
		public bool IsPInvokeImpl
		{
			get
			{
				return this.attributes.GetAttributes(8192);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(8192, value);
			}
		}

		// Token: 0x17000362 RID: 866
		// (get) Token: 0x06000E68 RID: 3688 RVA: 0x0002F0B0 File Offset: 0x0002D2B0
		// (set) Token: 0x06000E69 RID: 3689 RVA: 0x0002F0BE File Offset: 0x0002D2BE
		public bool IsUnmanagedExport
		{
			get
			{
				return this.attributes.GetAttributes(8);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(8, value);
			}
		}

		// Token: 0x17000363 RID: 867
		// (get) Token: 0x06000E6A RID: 3690 RVA: 0x0002F0D3 File Offset: 0x0002D2D3
		// (set) Token: 0x06000E6B RID: 3691 RVA: 0x0002F0E5 File Offset: 0x0002D2E5
		public bool IsRuntimeSpecialName
		{
			get
			{
				return this.attributes.GetAttributes(4096);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(4096, value);
			}
		}

		// Token: 0x17000364 RID: 868
		// (get) Token: 0x06000E6C RID: 3692 RVA: 0x0002F0FE File Offset: 0x0002D2FE
		// (set) Token: 0x06000E6D RID: 3693 RVA: 0x0002F110 File Offset: 0x0002D310
		public bool HasSecurity
		{
			get
			{
				return this.attributes.GetAttributes(16384);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(16384, value);
			}
		}

		// Token: 0x17000365 RID: 869
		// (get) Token: 0x06000E6E RID: 3694 RVA: 0x0002F129 File Offset: 0x0002D329
		// (set) Token: 0x06000E6F RID: 3695 RVA: 0x0002F138 File Offset: 0x0002D338
		public bool IsIL
		{
			get
			{
				return this.impl_attributes.GetMaskedAttributes(3, 0U);
			}
			set
			{
				this.impl_attributes = this.impl_attributes.SetMaskedAttributes(3, 0U, value);
			}
		}

		// Token: 0x17000366 RID: 870
		// (get) Token: 0x06000E70 RID: 3696 RVA: 0x0002F14E File Offset: 0x0002D34E
		// (set) Token: 0x06000E71 RID: 3697 RVA: 0x0002F15D File Offset: 0x0002D35D
		public bool IsNative
		{
			get
			{
				return this.impl_attributes.GetMaskedAttributes(3, 1U);
			}
			set
			{
				this.impl_attributes = this.impl_attributes.SetMaskedAttributes(3, 1U, value);
			}
		}

		// Token: 0x17000367 RID: 871
		// (get) Token: 0x06000E72 RID: 3698 RVA: 0x0002F173 File Offset: 0x0002D373
		// (set) Token: 0x06000E73 RID: 3699 RVA: 0x0002F182 File Offset: 0x0002D382
		public bool IsRuntime
		{
			get
			{
				return this.impl_attributes.GetMaskedAttributes(3, 3U);
			}
			set
			{
				this.impl_attributes = this.impl_attributes.SetMaskedAttributes(3, 3U, value);
			}
		}

		// Token: 0x17000368 RID: 872
		// (get) Token: 0x06000E74 RID: 3700 RVA: 0x0002F198 File Offset: 0x0002D398
		// (set) Token: 0x06000E75 RID: 3701 RVA: 0x0002F1A7 File Offset: 0x0002D3A7
		public bool IsUnmanaged
		{
			get
			{
				return this.impl_attributes.GetMaskedAttributes(4, 4U);
			}
			set
			{
				this.impl_attributes = this.impl_attributes.SetMaskedAttributes(4, 4U, value);
			}
		}

		// Token: 0x17000369 RID: 873
		// (get) Token: 0x06000E76 RID: 3702 RVA: 0x0002F1BD File Offset: 0x0002D3BD
		// (set) Token: 0x06000E77 RID: 3703 RVA: 0x0002F1CC File Offset: 0x0002D3CC
		public bool IsManaged
		{
			get
			{
				return this.impl_attributes.GetMaskedAttributes(4, 0U);
			}
			set
			{
				this.impl_attributes = this.impl_attributes.SetMaskedAttributes(4, 0U, value);
			}
		}

		// Token: 0x1700036A RID: 874
		// (get) Token: 0x06000E78 RID: 3704 RVA: 0x0002F1E2 File Offset: 0x0002D3E2
		// (set) Token: 0x06000E79 RID: 3705 RVA: 0x0002F1F1 File Offset: 0x0002D3F1
		public bool IsForwardRef
		{
			get
			{
				return this.impl_attributes.GetAttributes(16);
			}
			set
			{
				this.impl_attributes = this.impl_attributes.SetAttributes(16, value);
			}
		}

		// Token: 0x1700036B RID: 875
		// (get) Token: 0x06000E7A RID: 3706 RVA: 0x0002F207 File Offset: 0x0002D407
		// (set) Token: 0x06000E7B RID: 3707 RVA: 0x0002F219 File Offset: 0x0002D419
		public bool IsPreserveSig
		{
			get
			{
				return this.impl_attributes.GetAttributes(128);
			}
			set
			{
				this.impl_attributes = this.impl_attributes.SetAttributes(128, value);
			}
		}

		// Token: 0x1700036C RID: 876
		// (get) Token: 0x06000E7C RID: 3708 RVA: 0x0002F232 File Offset: 0x0002D432
		// (set) Token: 0x06000E7D RID: 3709 RVA: 0x0002F244 File Offset: 0x0002D444
		public bool IsInternalCall
		{
			get
			{
				return this.impl_attributes.GetAttributes(4096);
			}
			set
			{
				this.impl_attributes = this.impl_attributes.SetAttributes(4096, value);
			}
		}

		// Token: 0x1700036D RID: 877
		// (get) Token: 0x06000E7E RID: 3710 RVA: 0x0002F25D File Offset: 0x0002D45D
		// (set) Token: 0x06000E7F RID: 3711 RVA: 0x0002F26C File Offset: 0x0002D46C
		public bool IsSynchronized
		{
			get
			{
				return this.impl_attributes.GetAttributes(32);
			}
			set
			{
				this.impl_attributes = this.impl_attributes.SetAttributes(32, value);
			}
		}

		// Token: 0x1700036E RID: 878
		// (get) Token: 0x06000E80 RID: 3712 RVA: 0x0002F282 File Offset: 0x0002D482
		// (set) Token: 0x06000E81 RID: 3713 RVA: 0x0002F290 File Offset: 0x0002D490
		public bool NoInlining
		{
			get
			{
				return this.impl_attributes.GetAttributes(8);
			}
			set
			{
				this.impl_attributes = this.impl_attributes.SetAttributes(8, value);
			}
		}

		// Token: 0x1700036F RID: 879
		// (get) Token: 0x06000E82 RID: 3714 RVA: 0x0002F2A5 File Offset: 0x0002D4A5
		// (set) Token: 0x06000E83 RID: 3715 RVA: 0x0002F2B4 File Offset: 0x0002D4B4
		public bool NoOptimization
		{
			get
			{
				return this.impl_attributes.GetAttributes(64);
			}
			set
			{
				this.impl_attributes = this.impl_attributes.SetAttributes(64, value);
			}
		}

		// Token: 0x17000370 RID: 880
		// (get) Token: 0x06000E84 RID: 3716 RVA: 0x0002F2CA File Offset: 0x0002D4CA
		// (set) Token: 0x06000E85 RID: 3717 RVA: 0x0002F2DC File Offset: 0x0002D4DC
		public bool AggressiveInlining
		{
			get
			{
				return this.impl_attributes.GetAttributes(256);
			}
			set
			{
				this.impl_attributes = this.impl_attributes.SetAttributes(256, value);
			}
		}

		// Token: 0x17000371 RID: 881
		// (get) Token: 0x06000E86 RID: 3718 RVA: 0x0002F2F5 File Offset: 0x0002D4F5
		// (set) Token: 0x06000E87 RID: 3719 RVA: 0x0002F307 File Offset: 0x0002D507
		public bool AggressiveOptimization
		{
			get
			{
				return this.impl_attributes.GetAttributes(512);
			}
			set
			{
				this.impl_attributes = this.impl_attributes.SetAttributes(512, value);
			}
		}

		// Token: 0x17000372 RID: 882
		// (get) Token: 0x06000E88 RID: 3720 RVA: 0x0002F320 File Offset: 0x0002D520
		// (set) Token: 0x06000E89 RID: 3721 RVA: 0x0002F329 File Offset: 0x0002D529
		public bool IsSetter
		{
			get
			{
				return this.GetSemantics(MethodSemanticsAttributes.Setter);
			}
			set
			{
				this.SetSemantics(MethodSemanticsAttributes.Setter, value);
			}
		}

		// Token: 0x17000373 RID: 883
		// (get) Token: 0x06000E8A RID: 3722 RVA: 0x0002F333 File Offset: 0x0002D533
		// (set) Token: 0x06000E8B RID: 3723 RVA: 0x0002F33C File Offset: 0x0002D53C
		public bool IsGetter
		{
			get
			{
				return this.GetSemantics(MethodSemanticsAttributes.Getter);
			}
			set
			{
				this.SetSemantics(MethodSemanticsAttributes.Getter, value);
			}
		}

		// Token: 0x17000374 RID: 884
		// (get) Token: 0x06000E8C RID: 3724 RVA: 0x0002F346 File Offset: 0x0002D546
		// (set) Token: 0x06000E8D RID: 3725 RVA: 0x0002F34F File Offset: 0x0002D54F
		public bool IsOther
		{
			get
			{
				return this.GetSemantics(MethodSemanticsAttributes.Other);
			}
			set
			{
				this.SetSemantics(MethodSemanticsAttributes.Other, value);
			}
		}

		// Token: 0x17000375 RID: 885
		// (get) Token: 0x06000E8E RID: 3726 RVA: 0x0002F359 File Offset: 0x0002D559
		// (set) Token: 0x06000E8F RID: 3727 RVA: 0x0002F362 File Offset: 0x0002D562
		public bool IsAddOn
		{
			get
			{
				return this.GetSemantics(MethodSemanticsAttributes.AddOn);
			}
			set
			{
				this.SetSemantics(MethodSemanticsAttributes.AddOn, value);
			}
		}

		// Token: 0x17000376 RID: 886
		// (get) Token: 0x06000E90 RID: 3728 RVA: 0x0002F36C File Offset: 0x0002D56C
		// (set) Token: 0x06000E91 RID: 3729 RVA: 0x0002F376 File Offset: 0x0002D576
		public bool IsRemoveOn
		{
			get
			{
				return this.GetSemantics(MethodSemanticsAttributes.RemoveOn);
			}
			set
			{
				this.SetSemantics(MethodSemanticsAttributes.RemoveOn, value);
			}
		}

		// Token: 0x17000377 RID: 887
		// (get) Token: 0x06000E92 RID: 3730 RVA: 0x0002F381 File Offset: 0x0002D581
		// (set) Token: 0x06000E93 RID: 3731 RVA: 0x0002F38B File Offset: 0x0002D58B
		public bool IsFire
		{
			get
			{
				return this.GetSemantics(MethodSemanticsAttributes.Fire);
			}
			set
			{
				this.SetSemantics(MethodSemanticsAttributes.Fire, value);
			}
		}

		// Token: 0x17000378 RID: 888
		// (get) Token: 0x06000E94 RID: 3732 RVA: 0x0002A9E4 File Offset: 0x00028BE4
		// (set) Token: 0x06000E95 RID: 3733 RVA: 0x0002A9F1 File Offset: 0x00028BF1
		public new TypeDefinition DeclaringType
		{
			get
			{
				return (TypeDefinition)base.DeclaringType;
			}
			set
			{
				base.DeclaringType = value;
			}
		}

		// Token: 0x17000379 RID: 889
		// (get) Token: 0x06000E96 RID: 3734 RVA: 0x0002F396 File Offset: 0x0002D596
		public bool IsConstructor
		{
			get
			{
				return this.IsRuntimeSpecialName && this.IsSpecialName && (this.Name == ".cctor" || this.Name == ".ctor");
			}
		}

		// Token: 0x1700037A RID: 890
		// (get) Token: 0x06000E97 RID: 3735 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsDefinition
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06000E98 RID: 3736 RVA: 0x0002F3CE File Offset: 0x0002D5CE
		internal MethodDefinition()
		{
			this.token = new MetadataToken(TokenType.Method);
		}

		// Token: 0x06000E99 RID: 3737 RVA: 0x0002F3E6 File Offset: 0x0002D5E6
		public MethodDefinition(string name, MethodAttributes attributes, TypeReference returnType)
			: base(name, returnType)
		{
			this.attributes = (ushort)attributes;
			this.HasThis = !this.IsStatic;
			this.token = new MetadataToken(TokenType.Method);
		}

		// Token: 0x06000E9A RID: 3738 RVA: 0x0001B6A2 File Offset: 0x000198A2
		public override MethodDefinition Resolve()
		{
			return this;
		}

		// Token: 0x0400045E RID: 1118
		private ushort attributes;

		// Token: 0x0400045F RID: 1119
		private ushort impl_attributes;

		// Token: 0x04000460 RID: 1120
		internal volatile bool sem_attrs_ready;

		// Token: 0x04000461 RID: 1121
		internal MethodSemanticsAttributes sem_attrs;

		// Token: 0x04000462 RID: 1122
		private Collection<CustomAttribute> custom_attributes;

		// Token: 0x04000463 RID: 1123
		private Collection<SecurityDeclaration> security_declarations;

		// Token: 0x04000464 RID: 1124
		internal uint rva;

		// Token: 0x04000465 RID: 1125
		internal PInvokeInfo pinvoke;

		// Token: 0x04000466 RID: 1126
		private Collection<MethodReference> overrides;

		// Token: 0x04000467 RID: 1127
		internal MethodBody body;

		// Token: 0x04000468 RID: 1128
		internal MethodDebugInformation debug_info;

		// Token: 0x04000469 RID: 1129
		internal Collection<CustomDebugInformation> custom_infos;
	}
}
