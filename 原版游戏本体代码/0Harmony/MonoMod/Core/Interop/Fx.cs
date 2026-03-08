using System;
using System.Runtime.CompilerServices;
using MonoMod.Core.Interop.Attributes;
using MonoMod.Utils;

namespace MonoMod.Core.Interop
{
	// Token: 0x020005AF RID: 1455
	internal static class Fx
	{
		// Token: 0x020005B0 RID: 1456
		public static class V48
		{
			// Token: 0x020005B1 RID: 1457
			public enum MethodClassification
			{
				// Token: 0x040013F6 RID: 5110
				IL,
				// Token: 0x040013F7 RID: 5111
				FCall,
				// Token: 0x040013F8 RID: 5112
				NDirect,
				// Token: 0x040013F9 RID: 5113
				EEImpl,
				// Token: 0x040013FA RID: 5114
				Array,
				// Token: 0x040013FB RID: 5115
				Instantiated,
				// Token: 0x040013FC RID: 5116
				ComInterop,
				// Token: 0x040013FD RID: 5117
				Dynamic
			}

			// Token: 0x020005B2 RID: 1458
			public enum MethodDescClassification : ushort
			{
				// Token: 0x040013FF RID: 5119
				ClassificationMask = 7,
				// Token: 0x04001400 RID: 5120
				HasNonVtableSlot,
				// Token: 0x04001401 RID: 5121
				MethodImpl = 16,
				// Token: 0x04001402 RID: 5122
				Static = 32,
				// Token: 0x04001403 RID: 5123
				Intercepted = 64,
				// Token: 0x04001404 RID: 5124
				RequiresLinktimeCheck = 128,
				// Token: 0x04001405 RID: 5125
				RequiresInheritanceCheck = 256,
				// Token: 0x04001406 RID: 5126
				ParentRequiresInheritanceCheck = 512,
				// Token: 0x04001407 RID: 5127
				Duplicate = 1024,
				// Token: 0x04001408 RID: 5128
				VerifiedState = 2048,
				// Token: 0x04001409 RID: 5129
				Verifiable = 4096,
				// Token: 0x0400140A RID: 5130
				NotInline = 8192,
				// Token: 0x0400140B RID: 5131
				Synchronized = 16384,
				// Token: 0x0400140C RID: 5132
				RequiresFullSlotNumber = 32768
			}

			// Token: 0x020005B3 RID: 1459
			public struct MethodImpl
			{
				// Token: 0x0400140D RID: 5133
				public unsafe uint* pdwSlots;

				// Token: 0x0400140E RID: 5134
				public unsafe Fx.V48.MethodDesc* pImplementedMD;
			}

			// Token: 0x020005B4 RID: 1460
			public struct MethodDesc
			{
				// Token: 0x17000704 RID: 1796
				// (get) Token: 0x06001FB1 RID: 8113 RVA: 0x00066D87 File Offset: 0x00064F87
				public ushort SlotNumber
				{
					get
					{
						if (!this.m_wFlags.Has(Fx.V48.MethodDescClassification.RequiresFullSlotNumber))
						{
							return this.m_wSlotNumber & 1023;
						}
						return this.m_wSlotNumber;
					}
				}

				// Token: 0x17000705 RID: 1797
				// (get) Token: 0x06001FB2 RID: 8114 RVA: 0x00066DAF File Offset: 0x00064FAF
				public Fx.V48.MethodClassification Classification
				{
					get
					{
						return (Fx.V48.MethodClassification)(this.m_wFlags & Fx.V48.MethodDescClassification.ClassificationMask);
					}
				}

				// Token: 0x17000706 RID: 1798
				// (get) Token: 0x06001FB3 RID: 8115 RVA: 0x00066DB9 File Offset: 0x00064FB9
				public unsafe Fx.V48.MethodDescChunk* MethodDescChunk
				{
					get
					{
						return (Fx.V48.MethodDescChunk*)Unsafe.AsPointer<Fx.V48.MethodDesc>(Unsafe.SubtractByteOffset<Fx.V48.MethodDesc>(ref this, (UIntPtr)((IntPtr)sizeof(Fx.V48.MethodDescChunk) + (IntPtr)((UIntPtr)this.m_chunkIndex * Fx.V48.MethodDesc.Alignment))));
					}
				}

				// Token: 0x17000707 RID: 1799
				// (get) Token: 0x06001FB4 RID: 8116 RVA: 0x00066DDB File Offset: 0x00064FDB
				public unsafe Fx.V48.MethodTable* MethodTable
				{
					get
					{
						return this.MethodDescChunk->MethodTable;
					}
				}

				// Token: 0x06001FB5 RID: 8117 RVA: 0x00066DE8 File Offset: 0x00064FE8
				public unsafe void* GetMethodEntryPoint()
				{
					if (this.HasNonVtableSlot)
					{
						UIntPtr size = this.GetBaseSize();
						return *(IntPtr*)((byte*)Unsafe.AsPointer<Fx.V48.MethodDesc>(ref this) + (ulong)size);
					}
					return this.MethodTable->GetSlot((uint)this.SlotNumber);
				}

				// Token: 0x06001FB6 RID: 8118 RVA: 0x00066E21 File Offset: 0x00065021
				public unsafe bool TryAsInstantiated(out Fx.V48.InstantiatedMethodDesc* md)
				{
					if (this.Classification == Fx.V48.MethodClassification.Instantiated)
					{
						md = Unsafe.AsPointer<Fx.V48.MethodDesc>(ref this);
						return true;
					}
					md = default(Fx.V48.InstantiatedMethodDesc*);
					return false;
				}

				// Token: 0x06001FB7 RID: 8119 RVA: 0x00066E40 File Offset: 0x00065040
				[return: NativeInteger]
				public unsafe UIntPtr SizeOf(bool includeNonVtable = true, bool includeMethodImpl = true, bool includeComPlus = true, bool includeNativeCode = true)
				{
					UIntPtr size = this.GetBaseSize() + (UIntPtr)((includeNonVtable && this.m_wFlags.Has(Fx.V48.MethodDescClassification.HasNonVtableSlot)) ? ((IntPtr)sizeof(void*)) : ((IntPtr)0)) + (UIntPtr)((includeMethodImpl && this.m_wFlags.Has(Fx.V48.MethodDescClassification.MethodImpl)) ? ((IntPtr)sizeof(Fx.V48.MethodImpl)) : ((IntPtr)0));
					if (includeNativeCode && this.HasNativeCodeSlot)
					{
						size += (UIntPtr)(((*Unsafe.As<Fx.V48.MethodDesc, UIntPtr>(Unsafe.AddByteOffset<Fx.V48.MethodDesc>(ref this, size)) & (UIntPtr)((IntPtr)1)) != 0) ? (sizeof(void*) + sizeof(void*)) : sizeof(void*));
					}
					if (includeComPlus && this.IsGenericComPlusCall)
					{
						size += (UIntPtr)Fx.V48.ComPlusCallInfoPtr.CurrentSize;
					}
					return size;
				}

				// Token: 0x06001FB8 RID: 8120 RVA: 0x00066EDC File Offset: 0x000650DC
				public unsafe void* GetNativeCode()
				{
					if (this.HasNativeCodeSlot)
					{
						UIntPtr pCode = ((Fx.V48.RelativePointer*)this.GetAddrOfNativeCodeSlot())->Value & ~1;
						if (pCode != 0)
						{
							return pCode;
						}
					}
					if (!this.HasStableEntryPoint || this.HasPrecode)
					{
						return null;
					}
					return this.GetStableEntryPoint();
				}

				// Token: 0x06001FB9 RID: 8121 RVA: 0x00066F1F File Offset: 0x0006511F
				public unsafe void* GetStableEntryPoint()
				{
					return this.GetMethodEntryPoint();
				}

				// Token: 0x17000708 RID: 1800
				// (get) Token: 0x06001FBA RID: 8122 RVA: 0x00066F27 File Offset: 0x00065127
				public bool HasNonVtableSlot
				{
					get
					{
						return this.m_wFlags.Has(Fx.V48.MethodDescClassification.HasNonVtableSlot);
					}
				}

				// Token: 0x17000709 RID: 1801
				// (get) Token: 0x06001FBB RID: 8123 RVA: 0x00066F35 File Offset: 0x00065135
				public bool HasStableEntryPoint
				{
					get
					{
						return this.m_bFlags2.Has(Fx.V48.MethodDesc.Flags2.HasStableEntryPoint);
					}
				}

				// Token: 0x1700070A RID: 1802
				// (get) Token: 0x06001FBC RID: 8124 RVA: 0x00066F43 File Offset: 0x00065143
				public bool HasPrecode
				{
					get
					{
						return this.m_bFlags2.Has(Fx.V48.MethodDesc.Flags2.HasPrecode);
					}
				}

				// Token: 0x1700070B RID: 1803
				// (get) Token: 0x06001FBD RID: 8125 RVA: 0x00066F51 File Offset: 0x00065151
				public bool HasNativeCodeSlot
				{
					get
					{
						return this.m_bFlags2.Has(Fx.V48.MethodDesc.Flags2.HasNativeCodeSlot);
					}
				}

				// Token: 0x1700070C RID: 1804
				// (get) Token: 0x06001FBE RID: 8126 RVA: 0x00066F5F File Offset: 0x0006515F
				public bool IsUnboxingStub
				{
					get
					{
						return this.m_bFlags2.Has(Fx.V48.MethodDesc.Flags2.IsUnboxingStub);
					}
				}

				// Token: 0x1700070D RID: 1805
				// (get) Token: 0x06001FBF RID: 8127 RVA: 0x00066F70 File Offset: 0x00065170
				public unsafe bool HasMethodInstantiation
				{
					get
					{
						Fx.V48.InstantiatedMethodDesc* inst;
						return this.TryAsInstantiated(out inst) && inst->IMD_HasMethodInstantiation;
					}
				}

				// Token: 0x1700070E RID: 1806
				// (get) Token: 0x06001FC0 RID: 8128 RVA: 0x00066F90 File Offset: 0x00065190
				public unsafe bool IsGenericMethodDefinition
				{
					get
					{
						Fx.V48.InstantiatedMethodDesc* inst;
						return this.TryAsInstantiated(out inst) && inst->IMD_IsGenericMethodDefinition;
					}
				}

				// Token: 0x1700070F RID: 1807
				// (get) Token: 0x06001FC1 RID: 8129 RVA: 0x00066FB0 File Offset: 0x000651B0
				public unsafe bool IsInstantiatingStub
				{
					get
					{
						Fx.V48.InstantiatedMethodDesc* inst;
						return !this.IsUnboxingStub && this.TryAsInstantiated(out inst) && inst->IMD_IsWrapperStubWithInstantiations;
					}
				}

				// Token: 0x17000710 RID: 1808
				// (get) Token: 0x06001FC2 RID: 8130 RVA: 0x00066FD8 File Offset: 0x000651D8
				public unsafe bool IsGenericComPlusCall
				{
					get
					{
						Fx.V48.InstantiatedMethodDesc* inst;
						return this.TryAsInstantiated(out inst) && inst->IMD_HasComPlusCallInfo;
					}
				}

				// Token: 0x17000711 RID: 1809
				// (get) Token: 0x06001FC3 RID: 8131 RVA: 0x00066FF7 File Offset: 0x000651F7
				public bool IsWrapperStub
				{
					get
					{
						return this.IsUnboxingStub || this.IsInstantiatingStub;
					}
				}

				// Token: 0x17000712 RID: 1810
				// (get) Token: 0x06001FC4 RID: 8132 RVA: 0x00067009 File Offset: 0x00065209
				public bool IsTightlyBoundToMethodTable
				{
					get
					{
						if (!this.HasNonVtableSlot)
						{
							return true;
						}
						if (this.HasMethodInstantiation)
						{
							return this.IsGenericMethodDefinition;
						}
						return !this.IsWrapperStub;
					}
				}

				// Token: 0x06001FC5 RID: 8133 RVA: 0x00067030 File Offset: 0x00065230
				public unsafe static Fx.V48.MethodDesc* FindTightlyBoundWrappedMethodDesc(Fx.V48.MethodDesc* pMD)
				{
					Fx.V48.InstantiatedMethodDesc* inst;
					if (pMD->IsUnboxingStub && pMD->TryAsInstantiated(out inst))
					{
						pMD = inst->IMD_GetWrappedMethodDesc();
					}
					if (!pMD->IsTightlyBoundToMethodTable)
					{
						pMD = pMD->GetCanonicalMethodTable()->GetParallelMethodDesc(pMD);
					}
					if (pMD->IsUnboxingStub)
					{
						pMD = Fx.V48.MethodDesc.GetNextIntroducedMethod(pMD);
					}
					return pMD;
				}

				// Token: 0x06001FC6 RID: 8134 RVA: 0x00067080 File Offset: 0x00065280
				public unsafe static Fx.V48.MethodDesc* GetNextIntroducedMethod(Fx.V48.MethodDesc* pMD)
				{
					Fx.V48.MethodDescChunk* pChunk = pMD->MethodDescChunk;
					UIntPtr pNext = pMD + pMD->SizeOf(true, true, true, true) / (UIntPtr)sizeof(Fx.V48.MethodDesc);
					UIntPtr pEnd = pChunk + pChunk->SizeOf / (UIntPtr)sizeof(Fx.V48.MethodDescChunk);
					if (pNext < pEnd)
					{
						return pNext;
					}
					pChunk = pChunk->m_next;
					if (pChunk != null)
					{
						return pChunk->FirstMethodDesc;
					}
					return null;
				}

				// Token: 0x06001FC7 RID: 8135 RVA: 0x000670C5 File Offset: 0x000652C5
				public unsafe Fx.V48.MethodTable* GetCanonicalMethodTable()
				{
					return this.MethodTable->GetCanonicalMethodTable();
				}

				// Token: 0x06001FC8 RID: 8136 RVA: 0x000670D4 File Offset: 0x000652D4
				public unsafe void* GetAddrOfNativeCodeSlot()
				{
					UIntPtr size = this.SizeOf(true, true, false, false);
					return Unsafe.AsPointer<Fx.V48.MethodDesc>(Unsafe.AddByteOffset<Fx.V48.MethodDesc>(ref this, size));
				}

				// Token: 0x06001FC9 RID: 8137 RVA: 0x000670F8 File Offset: 0x000652F8
				[return: NativeInteger]
				public UIntPtr GetBaseSize()
				{
					return Fx.V48.MethodDesc.GetBaseSize(this.Classification);
				}

				// Token: 0x06001FCA RID: 8138 RVA: 0x00067105 File Offset: 0x00065305
				[return: NativeInteger]
				public static UIntPtr GetBaseSize(Fx.V48.MethodClassification classification)
				{
					return Fx.V48.MethodDesc.s_ClassificationSizeTable[(int)classification];
				}

				// Token: 0x0400140F RID: 5135
				[NativeInteger]
				public static readonly UIntPtr Alignment = (UIntPtr)((IntPtr.Size == 8) ? ((IntPtr)8) : ((IntPtr)4));

				// Token: 0x04001410 RID: 5136
				public Fx.V48.MethodDesc.Flags3 m_wFlags3AndTokenRemainder;

				// Token: 0x04001411 RID: 5137
				public byte m_chunkIndex;

				// Token: 0x04001412 RID: 5138
				public Fx.V48.MethodDesc.Flags2 m_bFlags2;

				// Token: 0x04001413 RID: 5139
				public const ushort PackedSlot_SlotMask = 1023;

				// Token: 0x04001414 RID: 5140
				public const ushort PackedSlot_NameHashMask = 64512;

				// Token: 0x04001415 RID: 5141
				public ushort m_wSlotNumber;

				// Token: 0x04001416 RID: 5142
				public Fx.V48.MethodDescClassification m_wFlags;

				// Token: 0x04001417 RID: 5143
				[NativeInteger]
				[Nullable(1)]
				private static readonly UIntPtr[] s_ClassificationSizeTable = new UIntPtr[]
				{
					(UIntPtr)((IntPtr)sizeof(Fx.V48.MethodDesc)),
					(UIntPtr)((IntPtr)Fx.V48.FCallMethodDescPtr.CurrentSize),
					(UIntPtr)((IntPtr)Fx.V48.NDirectMethodDescPtr.CurrentSize),
					(UIntPtr)((IntPtr)Fx.V48.EEImplMethodDescPtr.CurrentSize),
					(UIntPtr)((IntPtr)Fx.V48.ArrayMethodDescPtr.CurrentSize),
					(UIntPtr)((IntPtr)sizeof(Fx.V48.InstantiatedMethodDesc)),
					(UIntPtr)((IntPtr)sizeof(Fx.V48.ComPlusCallMethodDesc)),
					(UIntPtr)((IntPtr)Fx.V48.DynamicMethodDescPtr.CurrentSize)
				};

				// Token: 0x020005B5 RID: 1461
				[Flags]
				public enum Flags3 : ushort
				{
					// Token: 0x04001419 RID: 5145
					TokenRemainderMask = 16383,
					// Token: 0x0400141A RID: 5146
					HasForwardedValuetypeParameter = 16384,
					// Token: 0x0400141B RID: 5147
					ValueTypeParametersWalked = 16384,
					// Token: 0x0400141C RID: 5148
					DoesNotHaveEquivalentValuetypeParameters = 32768
				}

				// Token: 0x020005B6 RID: 1462
				[Flags]
				public enum Flags2 : byte
				{
					// Token: 0x0400141E RID: 5150
					HasStableEntryPoint = 1,
					// Token: 0x0400141F RID: 5151
					HasPrecode = 2,
					// Token: 0x04001420 RID: 5152
					IsUnboxingStub = 4,
					// Token: 0x04001421 RID: 5153
					HasNativeCodeSlot = 8,
					// Token: 0x04001422 RID: 5154
					TransparencyMask = 48,
					// Token: 0x04001423 RID: 5155
					TransparencyUnknown = 0,
					// Token: 0x04001424 RID: 5156
					TransparencyTransparent = 16,
					// Token: 0x04001425 RID: 5157
					TransparencyCritical = 32,
					// Token: 0x04001426 RID: 5158
					TransparencyTreatAsSafe = 48,
					// Token: 0x04001427 RID: 5159
					CASDemandsOnly = 64,
					// Token: 0x04001428 RID: 5160
					HostProtectionLinkChecksOnly = 128
				}
			}

			// Token: 0x020005B7 RID: 1463
			[FatInterface]
			public struct StoredSigMethodDescPtr
			{
				// Token: 0x17000713 RID: 1811
				// (get) Token: 0x06001FCC RID: 8140 RVA: 0x00067186 File Offset: 0x00065386
				[Nullable(1)]
				public static IntPtr[] CurrentVtable
				{
					[NullableContext(1)]
					get;
				} = ((IntPtr.Size == 8) ? Fx.V48.StoredSigMethodDesc_64.FatVtable_ : Fx.V48.StoredSigMethodDesc_32.FatVtable_);

				// Token: 0x17000714 RID: 1812
				// (get) Token: 0x06001FCD RID: 8141 RVA: 0x0006718D File Offset: 0x0006538D
				public static int CurrentSize { get; } = ((IntPtr.Size == 8) ? sizeof(Fx.V48.StoredSigMethodDesc_64) : sizeof(Fx.V48.StoredSigMethodDesc_32));

				// Token: 0x06001FCE RID: 8142 RVA: 0x00067194 File Offset: 0x00065394
				private unsafe void* GetPSig()
				{
					method system.Void*_u0020(System.Void*) = (void*)this.vtbl_[0];
					return calli(System.Void*(System.Void*), this.ptr_, system.Void*_u0020(System.Void*));
				}

				// Token: 0x17000715 RID: 1813
				// (get) Token: 0x06001FCF RID: 8143 RVA: 0x000671BB File Offset: 0x000653BB
				public unsafe void* m_pSig
				{
					[FatInterfaceIgnore]
					get
					{
						return this.GetPSig();
					}
				}

				// Token: 0x06001FD0 RID: 8144 RVA: 0x000671C4 File Offset: 0x000653C4
				private unsafe uint GetCSig()
				{
					method system.UInt32_u0020(System.Void*) = (void*)this.vtbl_[0];
					return calli(System.UInt32(System.Void*), this.ptr_, system.UInt32_u0020(System.Void*));
				}

				// Token: 0x17000716 RID: 1814
				// (get) Token: 0x06001FD1 RID: 8145 RVA: 0x000671EB File Offset: 0x000653EB
				public uint m_cSig
				{
					[FatInterfaceIgnore]
					get
					{
						return this.GetCSig();
					}
				}

				// Token: 0x06001FD2 RID: 8146 RVA: 0x000671F3 File Offset: 0x000653F3
				public unsafe StoredSigMethodDescPtr(void* ptr, [Nullable(1)] IntPtr[] vtbl)
				{
					this.ptr_ = ptr;
					this.vtbl_ = vtbl;
				}

				// Token: 0x0400142B RID: 5163
				private unsafe readonly void* ptr_;

				// Token: 0x0400142C RID: 5164
				[Nullable(1)]
				private readonly IntPtr[] vtbl_;
			}

			// Token: 0x020005B8 RID: 1464
			[FatInterfaceImpl(typeof(Fx.V48.StoredSigMethodDescPtr))]
			public struct StoredSigMethodDesc_64
			{
				// Token: 0x06001FD4 RID: 8148 RVA: 0x00067239 File Offset: 0x00065439
				private unsafe void* GetPSig()
				{
					return this.m_pSig;
				}

				// Token: 0x06001FD5 RID: 8149 RVA: 0x00067241 File Offset: 0x00065441
				private uint GetCSig()
				{
					return this.m_cSig;
				}

				// Token: 0x17000717 RID: 1815
				// (get) Token: 0x06001FD6 RID: 8150 RVA: 0x00067249 File Offset: 0x00065449
				[Nullable(1)]
				public static IntPtr[] FatVtable_
				{
					[NullableContext(1)]
					get
					{
						IntPtr[] result;
						if ((result = Fx.V48.StoredSigMethodDesc_64.fatVtable_) == null)
						{
							result = (Fx.V48.StoredSigMethodDesc_64.fatVtable_ = new IntPtr[]
							{
								(IntPtr)ldftn(<get_FatVtable_>g__S_GetPSig_0|8_0),
								(IntPtr)ldftn(<get_FatVtable_>g__S_GetCSig_1|8_1)
							});
						}
						return result;
					}
				}

				// Token: 0x06001FD7 RID: 8151 RVA: 0x0006727C File Offset: 0x0006547C
				[CompilerGenerated]
				internal unsafe static void* <get_FatVtable_>g__S_GetPSig_0|8_0(void* ptr__)
				{
					return ((Fx.V48.StoredSigMethodDesc_64*)ptr__)->GetPSig();
				}

				// Token: 0x06001FD8 RID: 8152 RVA: 0x00067284 File Offset: 0x00065484
				[CompilerGenerated]
				internal unsafe static uint <get_FatVtable_>g__S_GetCSig_1|8_1(void* ptr__)
				{
					return ((Fx.V48.StoredSigMethodDesc_64*)ptr__)->GetCSig();
				}

				// Token: 0x0400142D RID: 5165
				public Fx.V48.MethodDesc @base;

				// Token: 0x0400142E RID: 5166
				public unsafe void* m_pSig;

				// Token: 0x0400142F RID: 5167
				public uint m_cSig;

				// Token: 0x04001430 RID: 5168
				public uint m_dwExtendedFlags;

				// Token: 0x04001431 RID: 5169
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x020005B9 RID: 1465
			[FatInterfaceImpl(typeof(Fx.V48.StoredSigMethodDescPtr))]
			public struct StoredSigMethodDesc_32
			{
				// Token: 0x06001FD9 RID: 8153 RVA: 0x0006728C File Offset: 0x0006548C
				private unsafe void* GetPSig()
				{
					return this.m_pSig;
				}

				// Token: 0x06001FDA RID: 8154 RVA: 0x00067294 File Offset: 0x00065494
				private uint GetCSig()
				{
					return this.m_cSig;
				}

				// Token: 0x17000718 RID: 1816
				// (get) Token: 0x06001FDB RID: 8155 RVA: 0x0006729C File Offset: 0x0006549C
				[Nullable(1)]
				public static IntPtr[] FatVtable_
				{
					[NullableContext(1)]
					get
					{
						IntPtr[] result;
						if ((result = Fx.V48.StoredSigMethodDesc_32.fatVtable_) == null)
						{
							result = (Fx.V48.StoredSigMethodDesc_32.fatVtable_ = new IntPtr[]
							{
								(IntPtr)ldftn(<get_FatVtable_>g__S_GetPSig_0|7_0),
								(IntPtr)ldftn(<get_FatVtable_>g__S_GetCSig_1|7_1)
							});
						}
						return result;
					}
				}

				// Token: 0x06001FDC RID: 8156 RVA: 0x000672CF File Offset: 0x000654CF
				[CompilerGenerated]
				internal unsafe static void* <get_FatVtable_>g__S_GetPSig_0|7_0(void* ptr__)
				{
					return ((Fx.V48.StoredSigMethodDesc_32*)ptr__)->GetPSig();
				}

				// Token: 0x06001FDD RID: 8157 RVA: 0x000672D7 File Offset: 0x000654D7
				[CompilerGenerated]
				internal unsafe static uint <get_FatVtable_>g__S_GetCSig_1|7_1(void* ptr__)
				{
					return ((Fx.V48.StoredSigMethodDesc_32*)ptr__)->GetCSig();
				}

				// Token: 0x04001432 RID: 5170
				public Fx.V48.MethodDesc @base;

				// Token: 0x04001433 RID: 5171
				public unsafe void* m_pSig;

				// Token: 0x04001434 RID: 5172
				public uint m_cSig;

				// Token: 0x04001435 RID: 5173
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x020005BA RID: 1466
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterface]
			public struct FCallMethodDescPtr
			{
				// Token: 0x17000719 RID: 1817
				// (get) Token: 0x06001FDE RID: 8158 RVA: 0x000672DF File Offset: 0x000654DF
				public static IntPtr[] CurrentVtable { get; } = ((IntPtr.Size == 8) ? Fx.V48.FCallMethodDesc_64.FatVtable_ : Fx.V48.FCallMethodDesc_32.FatVtable_);

				// Token: 0x1700071A RID: 1818
				// (get) Token: 0x06001FDF RID: 8159 RVA: 0x000672E6 File Offset: 0x000654E6
				public static int CurrentSize { get; } = ((IntPtr.Size == 8) ? sizeof(Fx.V48.FCallMethodDesc_64) : sizeof(Fx.V48.FCallMethodDesc_32));

				// Token: 0x06001FE0 RID: 8160 RVA: 0x000672F0 File Offset: 0x000654F0
				private unsafe uint GetECallID()
				{
					method system.UInt32_u0020(System.Void*) = (void*)this.vtbl_[0];
					return calli(System.UInt32(System.Void*), this.ptr_, system.UInt32_u0020(System.Void*));
				}

				// Token: 0x1700071B RID: 1819
				// (get) Token: 0x06001FE1 RID: 8161 RVA: 0x00067317 File Offset: 0x00065517
				public uint m_dwECallID
				{
					[FatInterfaceIgnore]
					get
					{
						return this.GetECallID();
					}
				}

				// Token: 0x06001FE2 RID: 8162 RVA: 0x0006731F File Offset: 0x0006551F
				[NullableContext(0)]
				public unsafe FCallMethodDescPtr(void* ptr, [Nullable(1)] IntPtr[] vtbl)
				{
					this.ptr_ = ptr;
					this.vtbl_ = vtbl;
				}

				// Token: 0x04001438 RID: 5176
				[Nullable(0)]
				private unsafe readonly void* ptr_;

				// Token: 0x04001439 RID: 5177
				private readonly IntPtr[] vtbl_;
			}

			// Token: 0x020005BB RID: 1467
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterfaceImpl(typeof(Fx.V48.FCallMethodDescPtr))]
			public struct FCallMethodDesc_64
			{
				// Token: 0x06001FE4 RID: 8164 RVA: 0x00067365 File Offset: 0x00065565
				private uint GetECallID()
				{
					return this.m_dwECallID;
				}

				// Token: 0x1700071C RID: 1820
				// (get) Token: 0x06001FE5 RID: 8165 RVA: 0x0006736D File Offset: 0x0006556D
				public static IntPtr[] FatVtable_
				{
					get
					{
						IntPtr[] result;
						if ((result = Fx.V48.FCallMethodDesc_64.fatVtable_) == null)
						{
							result = (Fx.V48.FCallMethodDesc_64.fatVtable_ = new IntPtr[] { (IntPtr)ldftn(<get_FatVtable_>g__S_GetECallID_0|6_0) });
						}
						return result;
					}
				}

				// Token: 0x06001FE6 RID: 8166 RVA: 0x00067392 File Offset: 0x00065592
				[NullableContext(0)]
				[CompilerGenerated]
				internal unsafe static uint <get_FatVtable_>g__S_GetECallID_0|6_0(void* ptr__)
				{
					return ((Fx.V48.FCallMethodDesc_64*)ptr__)->GetECallID();
				}

				// Token: 0x0400143A RID: 5178
				public Fx.V48.MethodDesc @base;

				// Token: 0x0400143B RID: 5179
				public uint m_dwECallID;

				// Token: 0x0400143C RID: 5180
				public uint m_padding;

				// Token: 0x0400143D RID: 5181
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x020005BC RID: 1468
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterfaceImpl(typeof(Fx.V48.FCallMethodDescPtr))]
			public struct FCallMethodDesc_32
			{
				// Token: 0x06001FE7 RID: 8167 RVA: 0x0006739A File Offset: 0x0006559A
				private uint GetECallID()
				{
					return this.m_dwECallID;
				}

				// Token: 0x1700071D RID: 1821
				// (get) Token: 0x06001FE8 RID: 8168 RVA: 0x000673A2 File Offset: 0x000655A2
				public static IntPtr[] FatVtable_
				{
					get
					{
						IntPtr[] result;
						if ((result = Fx.V48.FCallMethodDesc_32.fatVtable_) == null)
						{
							result = (Fx.V48.FCallMethodDesc_32.fatVtable_ = new IntPtr[] { (IntPtr)ldftn(<get_FatVtable_>g__S_GetECallID_0|5_0) });
						}
						return result;
					}
				}

				// Token: 0x06001FE9 RID: 8169 RVA: 0x000673C7 File Offset: 0x000655C7
				[NullableContext(0)]
				[CompilerGenerated]
				internal unsafe static uint <get_FatVtable_>g__S_GetECallID_0|5_0(void* ptr__)
				{
					return ((Fx.V48.FCallMethodDesc_32*)ptr__)->GetECallID();
				}

				// Token: 0x0400143E RID: 5182
				public Fx.V48.MethodDesc @base;

				// Token: 0x0400143F RID: 5183
				public uint m_dwECallID;

				// Token: 0x04001440 RID: 5184
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x020005BD RID: 1469
			public struct DynamicResolver
			{
			}

			// Token: 0x020005BE RID: 1470
			[Flags]
			public enum DynamicMethodDesc_ExtendedFlags : uint
			{
				// Token: 0x04001442 RID: 5186
				Attrs = 65535U,
				// Token: 0x04001443 RID: 5187
				ILStubAttrs = 23U,
				// Token: 0x04001444 RID: 5188
				MemberAccessMask = 7U,
				// Token: 0x04001445 RID: 5189
				ReverseStub = 8U,
				// Token: 0x04001446 RID: 5190
				Static = 16U,
				// Token: 0x04001447 RID: 5191
				CALLIStub = 32U,
				// Token: 0x04001448 RID: 5192
				DelegateStub = 64U,
				// Token: 0x04001449 RID: 5193
				CopyCtorArgs = 128U,
				// Token: 0x0400144A RID: 5194
				Unbreakable = 256U,
				// Token: 0x0400144B RID: 5195
				DelegateCOMStub = 512U,
				// Token: 0x0400144C RID: 5196
				SignatureNeedsResture = 1024U,
				// Token: 0x0400144D RID: 5197
				StubNeedsCOMStarted = 2048U,
				// Token: 0x0400144E RID: 5198
				MulticastStub = 4096U,
				// Token: 0x0400144F RID: 5199
				UnboxingILStub = 8192U,
				// Token: 0x04001450 RID: 5200
				ILStub = 65536U,
				// Token: 0x04001451 RID: 5201
				LCGMethod = 131072U,
				// Token: 0x04001452 RID: 5202
				StackArgSize = 4294705152U
			}

			// Token: 0x020005BF RID: 1471
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterface]
			public struct DynamicMethodDescPtr
			{
				// Token: 0x1700071E RID: 1822
				// (get) Token: 0x06001FEA RID: 8170 RVA: 0x000673CF File Offset: 0x000655CF
				public static IntPtr[] CurrentVtable { get; } = ((IntPtr.Size == 8) ? Fx.V48.DynamicMethodDesc_64.FatVtable_ : Fx.V48.DynamicMethodDesc_32.FatVtable_);

				// Token: 0x1700071F RID: 1823
				// (get) Token: 0x06001FEB RID: 8171 RVA: 0x000673D6 File Offset: 0x000655D6
				public static int CurrentSize { get; } = ((IntPtr.Size == 8) ? sizeof(Fx.V48.DynamicMethodDesc_64) : sizeof(Fx.V48.DynamicMethodDesc_32));

				// Token: 0x06001FEC RID: 8172 RVA: 0x000673E0 File Offset: 0x000655E0
				private unsafe Fx.V48.DynamicMethodDesc_ExtendedFlags GetFlags()
				{
					method monoMod.Core.Interop.Fx/V48/DynamicMethodDesc_ExtendedFlags_u0020(System.Void*) = (void*)this.vtbl_[0];
					return calli(MonoMod.Core.Interop.Fx/V48/DynamicMethodDesc_ExtendedFlags(System.Void*), this.ptr_, monoMod.Core.Interop.Fx/V48/DynamicMethodDesc_ExtendedFlags_u0020(System.Void*));
				}

				// Token: 0x17000720 RID: 1824
				// (get) Token: 0x06001FED RID: 8173 RVA: 0x00067407 File Offset: 0x00065607
				public Fx.V48.DynamicMethodDesc_ExtendedFlags Flags
				{
					get
					{
						return this.GetFlags();
					}
				}

				// Token: 0x06001FEE RID: 8174 RVA: 0x0006740F File Offset: 0x0006560F
				[NullableContext(0)]
				public unsafe DynamicMethodDescPtr(void* ptr, [Nullable(1)] IntPtr[] vtbl)
				{
					this.ptr_ = ptr;
					this.vtbl_ = vtbl;
				}

				// Token: 0x04001455 RID: 5205
				[Nullable(0)]
				private unsafe readonly void* ptr_;

				// Token: 0x04001456 RID: 5206
				private readonly IntPtr[] vtbl_;
			}

			// Token: 0x020005C0 RID: 1472
			[FatInterfaceImpl(typeof(Fx.V48.DynamicMethodDescPtr))]
			public struct DynamicMethodDesc_64
			{
				// Token: 0x06001FF0 RID: 8176 RVA: 0x00067455 File Offset: 0x00065655
				private Fx.V48.DynamicMethodDesc_ExtendedFlags GetFlags()
				{
					return (Fx.V48.DynamicMethodDesc_ExtendedFlags)this.@base.m_dwExtendedFlags;
				}

				// Token: 0x17000721 RID: 1825
				// (get) Token: 0x06001FF1 RID: 8177 RVA: 0x00067462 File Offset: 0x00065662
				public Fx.V48.DynamicMethodDesc_ExtendedFlags Flags
				{
					get
					{
						return this.GetFlags();
					}
				}

				// Token: 0x17000722 RID: 1826
				// (get) Token: 0x06001FF2 RID: 8178 RVA: 0x0006746A File Offset: 0x0006566A
				[Nullable(1)]
				public static IntPtr[] FatVtable_
				{
					[NullableContext(1)]
					get
					{
						IntPtr[] result;
						if ((result = Fx.V48.DynamicMethodDesc_64.fatVtable_) == null)
						{
							result = (Fx.V48.DynamicMethodDesc_64.fatVtable_ = new IntPtr[] { (IntPtr)ldftn(<get_FatVtable_>g__S_GetFlags_0|8_0) });
						}
						return result;
					}
				}

				// Token: 0x06001FF3 RID: 8179 RVA: 0x00067462 File Offset: 0x00065662
				[CompilerGenerated]
				internal unsafe static Fx.V48.DynamicMethodDesc_ExtendedFlags <get_FatVtable_>g__S_GetFlags_0|8_0(void* ptr__)
				{
					return ((Fx.V48.DynamicMethodDesc_64*)ptr__)->GetFlags();
				}

				// Token: 0x04001457 RID: 5207
				public Fx.V48.StoredSigMethodDesc_64 @base;

				// Token: 0x04001458 RID: 5208
				public unsafe byte* m_pszMethodName;

				// Token: 0x04001459 RID: 5209
				public unsafe Fx.V48.DynamicResolver* m_pResolver;

				// Token: 0x0400145A RID: 5210
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x020005C1 RID: 1473
			[FatInterfaceImpl(typeof(Fx.V48.DynamicMethodDescPtr))]
			public struct DynamicMethodDesc_32
			{
				// Token: 0x06001FF4 RID: 8180 RVA: 0x0006748F File Offset: 0x0006568F
				private Fx.V48.DynamicMethodDesc_ExtendedFlags GetFlags()
				{
					return (Fx.V48.DynamicMethodDesc_ExtendedFlags)this.m_dwExtendedFlags;
				}

				// Token: 0x17000723 RID: 1827
				// (get) Token: 0x06001FF5 RID: 8181 RVA: 0x00067497 File Offset: 0x00065697
				public Fx.V48.DynamicMethodDesc_ExtendedFlags Flags
				{
					get
					{
						return this.GetFlags();
					}
				}

				// Token: 0x17000724 RID: 1828
				// (get) Token: 0x06001FF6 RID: 8182 RVA: 0x0006749F File Offset: 0x0006569F
				[Nullable(1)]
				public static IntPtr[] FatVtable_
				{
					[NullableContext(1)]
					get
					{
						IntPtr[] result;
						if ((result = Fx.V48.DynamicMethodDesc_32.fatVtable_) == null)
						{
							result = (Fx.V48.DynamicMethodDesc_32.fatVtable_ = new IntPtr[] { (IntPtr)ldftn(<get_FatVtable_>g__S_GetFlags_0|9_0) });
						}
						return result;
					}
				}

				// Token: 0x06001FF7 RID: 8183 RVA: 0x00067497 File Offset: 0x00065697
				[CompilerGenerated]
				internal unsafe static Fx.V48.DynamicMethodDesc_ExtendedFlags <get_FatVtable_>g__S_GetFlags_0|9_0(void* ptr__)
				{
					return ((Fx.V48.DynamicMethodDesc_32*)ptr__)->GetFlags();
				}

				// Token: 0x0400145B RID: 5211
				public Fx.V48.StoredSigMethodDesc_32 @base;

				// Token: 0x0400145C RID: 5212
				public unsafe byte* m_pszMethodName;

				// Token: 0x0400145D RID: 5213
				public unsafe Fx.V48.DynamicResolver* m_pResolver;

				// Token: 0x0400145E RID: 5214
				public uint m_dwExtendedFlags;

				// Token: 0x0400145F RID: 5215
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x020005C2 RID: 1474
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterface]
			public struct ArrayMethodDescPtr
			{
				// Token: 0x17000725 RID: 1829
				// (get) Token: 0x06001FF8 RID: 8184 RVA: 0x000674C4 File Offset: 0x000656C4
				public static IntPtr[] CurrentVtable { get; } = ((IntPtr.Size == 8) ? Fx.V48.ArrayMethodDesc_64.FatVtable_ : Fx.V48.ArrayMethodDesc_32.FatVtable_);

				// Token: 0x17000726 RID: 1830
				// (get) Token: 0x06001FF9 RID: 8185 RVA: 0x000674CB File Offset: 0x000656CB
				public static int CurrentSize { get; } = ((IntPtr.Size == 8) ? sizeof(Fx.V48.ArrayMethodDesc_64) : sizeof(Fx.V48.ArrayMethodDesc_32));

				// Token: 0x06001FFA RID: 8186 RVA: 0x000674D2 File Offset: 0x000656D2
				[NullableContext(0)]
				public unsafe ArrayMethodDescPtr(void* ptr, [Nullable(1)] IntPtr[] vtbl)
				{
					this.ptr_ = ptr;
					this.vtbl_ = vtbl;
				}

				// Token: 0x04001462 RID: 5218
				[Nullable(0)]
				private unsafe readonly void* ptr_;

				// Token: 0x04001463 RID: 5219
				private readonly IntPtr[] vtbl_;
			}

			// Token: 0x020005C3 RID: 1475
			public enum ArrayFunc
			{
				// Token: 0x04001465 RID: 5221
				Get,
				// Token: 0x04001466 RID: 5222
				Set,
				// Token: 0x04001467 RID: 5223
				Address,
				// Token: 0x04001468 RID: 5224
				Ctor
			}

			// Token: 0x020005C4 RID: 1476
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterfaceImpl(typeof(Fx.V48.ArrayMethodDescPtr))]
			public struct ArrayMethodDesc_64
			{
				// Token: 0x17000727 RID: 1831
				// (get) Token: 0x06001FFC RID: 8188 RVA: 0x00067518 File Offset: 0x00065718
				public static IntPtr[] FatVtable_
				{
					get
					{
						IntPtr[] result;
						if ((result = Fx.V48.ArrayMethodDesc_64.fatVtable_) == null)
						{
							result = (Fx.V48.ArrayMethodDesc_64.fatVtable_ = new IntPtr[0]);
						}
						return result;
					}
				}

				// Token: 0x04001469 RID: 5225
				public Fx.V48.StoredSigMethodDesc_64 @base;

				// Token: 0x0400146A RID: 5226
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x020005C5 RID: 1477
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterfaceImpl(typeof(Fx.V48.ArrayMethodDescPtr))]
			public struct ArrayMethodDesc_32
			{
				// Token: 0x17000728 RID: 1832
				// (get) Token: 0x06001FFD RID: 8189 RVA: 0x0006752F File Offset: 0x0006572F
				public static IntPtr[] FatVtable_
				{
					get
					{
						IntPtr[] result;
						if ((result = Fx.V48.ArrayMethodDesc_32.fatVtable_) == null)
						{
							result = (Fx.V48.ArrayMethodDesc_32.fatVtable_ = new IntPtr[0]);
						}
						return result;
					}
				}

				// Token: 0x0400146B RID: 5227
				public Fx.V48.StoredSigMethodDesc_32 @base;

				// Token: 0x0400146C RID: 5228
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x020005C6 RID: 1478
			public struct NDirectWriteableData
			{
			}

			// Token: 0x020005C7 RID: 1479
			[Flags]
			public enum NDirectMethodDesc_Flags : ushort
			{
				// Token: 0x0400146E RID: 5230
				EarlyBound = 1,
				// Token: 0x0400146F RID: 5231
				HasSuppressUnmanagedCodeAccess = 2,
				// Token: 0x04001470 RID: 5232
				DefaultDllImportSearchPathIsCached = 4,
				// Token: 0x04001471 RID: 5233
				IsMarshalingRequiredCached = 16,
				// Token: 0x04001472 RID: 5234
				CachedMarshalingRequired = 32,
				// Token: 0x04001473 RID: 5235
				NativeAnsi = 64,
				// Token: 0x04001474 RID: 5236
				LastError = 128,
				// Token: 0x04001475 RID: 5237
				NativeNoMangle = 256,
				// Token: 0x04001476 RID: 5238
				VarArgs = 512,
				// Token: 0x04001477 RID: 5239
				StdCall = 1024,
				// Token: 0x04001478 RID: 5240
				ThisCall = 2048,
				// Token: 0x04001479 RID: 5241
				IsQCall = 4096,
				// Token: 0x0400147A RID: 5242
				DefaultDllImportSearchPathsStatus = 8192,
				// Token: 0x0400147B RID: 5243
				NDirectPopulated = 32768
			}

			// Token: 0x020005C8 RID: 1480
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterface]
			public struct NDirectMethodDescPtr
			{
				// Token: 0x17000729 RID: 1833
				// (get) Token: 0x06001FFE RID: 8190 RVA: 0x00067546 File Offset: 0x00065746
				public static IntPtr[] CurrentVtable { get; } = ((PlatformDetection.Architecture == ArchitectureKind.x86) ? Fx.V48.NDirectMethodDesc_x86.FatVtable_ : Fx.V48.NDirectMethodDesc_other.FatVtable_);

				// Token: 0x1700072A RID: 1834
				// (get) Token: 0x06001FFF RID: 8191 RVA: 0x0006754D File Offset: 0x0006574D
				public static int CurrentSize { get; } = ((PlatformDetection.Architecture == ArchitectureKind.x86) ? sizeof(Fx.V48.NDirectMethodDesc_x86) : sizeof(Fx.V48.NDirectMethodDesc_other));

				// Token: 0x06002000 RID: 8192 RVA: 0x00067554 File Offset: 0x00065754
				[NullableContext(0)]
				public unsafe NDirectMethodDescPtr(void* ptr, [Nullable(1)] IntPtr[] vtbl)
				{
					this.ptr_ = ptr;
					this.vtbl_ = vtbl;
				}

				// Token: 0x0400147E RID: 5246
				[Nullable(0)]
				private unsafe readonly void* ptr_;

				// Token: 0x0400147F RID: 5247
				private readonly IntPtr[] vtbl_;
			}

			// Token: 0x020005C9 RID: 1481
			[FatInterfaceImpl(typeof(Fx.V48.NDirectMethodDescPtr))]
			public struct NDirectMethodDesc_other
			{
				// Token: 0x1700072B RID: 1835
				// (get) Token: 0x06002002 RID: 8194 RVA: 0x0006759A File Offset: 0x0006579A
				[Nullable(1)]
				public static IntPtr[] FatVtable_
				{
					[NullableContext(1)]
					get
					{
						IntPtr[] result;
						if ((result = Fx.V48.NDirectMethodDesc_other.fatVtable_) == null)
						{
							result = (Fx.V48.NDirectMethodDesc_other.fatVtable_ = new IntPtr[0]);
						}
						return result;
					}
				}

				// Token: 0x04001480 RID: 5248
				public Fx.V48.MethodDesc @base;

				// Token: 0x04001481 RID: 5249
				private Fx.V48.NDirectMethodDesc_other.NDirect ndirect;

				// Token: 0x04001482 RID: 5250
				[Nullable(2)]
				private static IntPtr[] fatVtable_;

				// Token: 0x020005CA RID: 1482
				public struct NDirect
				{
					// Token: 0x04001483 RID: 5251
					public unsafe void* m_pNativeNDirectTarget;

					// Token: 0x04001484 RID: 5252
					public unsafe byte* m_pszEntrypointName;

					// Token: 0x04001485 RID: 5253
					[NativeInteger]
					public UIntPtr union_pszLibName_dwECallID;

					// Token: 0x04001486 RID: 5254
					public unsafe Fx.V48.NDirectWriteableData* m_pWriteableData;

					// Token: 0x04001487 RID: 5255
					public unsafe void* m_pImportThunkGlue;

					// Token: 0x04001488 RID: 5256
					public uint m_DefaultDllImportSearchPathsAttributeValue;

					// Token: 0x04001489 RID: 5257
					public Fx.V48.NDirectMethodDesc_Flags m_wFlags;

					// Token: 0x0400148A RID: 5258
					public unsafe Fx.V48.MethodDesc* m_pStubMD;
				}
			}

			// Token: 0x020005CB RID: 1483
			[FatInterfaceImpl(typeof(Fx.V48.NDirectMethodDescPtr))]
			public struct NDirectMethodDesc_x86
			{
				// Token: 0x1700072C RID: 1836
				// (get) Token: 0x06002003 RID: 8195 RVA: 0x000675B1 File Offset: 0x000657B1
				[Nullable(1)]
				public static IntPtr[] FatVtable_
				{
					[NullableContext(1)]
					get
					{
						IntPtr[] result;
						if ((result = Fx.V48.NDirectMethodDesc_x86.fatVtable_) == null)
						{
							result = (Fx.V48.NDirectMethodDesc_x86.fatVtable_ = new IntPtr[0]);
						}
						return result;
					}
				}

				// Token: 0x0400148B RID: 5259
				public Fx.V48.MethodDesc @base;

				// Token: 0x0400148C RID: 5260
				private Fx.V48.NDirectMethodDesc_x86.NDirect ndirect;

				// Token: 0x0400148D RID: 5261
				[Nullable(2)]
				private static IntPtr[] fatVtable_;

				// Token: 0x020005CC RID: 1484
				public struct NDirect
				{
					// Token: 0x0400148E RID: 5262
					public unsafe void* m_pNativeNDirectTarget;

					// Token: 0x0400148F RID: 5263
					public unsafe byte* m_pszEntrypointName;

					// Token: 0x04001490 RID: 5264
					[NativeInteger]
					public UIntPtr union_pszLibName_dwECallID;

					// Token: 0x04001491 RID: 5265
					public unsafe Fx.V48.NDirectWriteableData* m_pWriteableData;

					// Token: 0x04001492 RID: 5266
					public unsafe void* m_pImportThunkGlue;

					// Token: 0x04001493 RID: 5267
					public uint m_DefaultDllImportSearchPathsAttributeValue;

					// Token: 0x04001494 RID: 5268
					public Fx.V48.NDirectMethodDesc_Flags m_wFlags;

					// Token: 0x04001495 RID: 5269
					public ushort m_cbStackArgumentSize;

					// Token: 0x04001496 RID: 5270
					public unsafe Fx.V48.MethodDesc* m_pStubMD;
				}
			}

			// Token: 0x020005CD RID: 1485
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterface]
			public struct EEImplMethodDescPtr
			{
				// Token: 0x1700072D RID: 1837
				// (get) Token: 0x06002004 RID: 8196 RVA: 0x000675C8 File Offset: 0x000657C8
				public static IntPtr[] CurrentVtable { get; } = ((IntPtr.Size == 8) ? Fx.V48.EEImplMethodDesc_64.FatVtable_ : Fx.V48.EEImplMethodDesc_32.FatVtable_);

				// Token: 0x1700072E RID: 1838
				// (get) Token: 0x06002005 RID: 8197 RVA: 0x000675CF File Offset: 0x000657CF
				public static int CurrentSize { get; } = ((IntPtr.Size == 8) ? sizeof(Fx.V48.EEImplMethodDesc_64) : sizeof(Fx.V48.EEImplMethodDesc_32));

				// Token: 0x06002006 RID: 8198 RVA: 0x000675D6 File Offset: 0x000657D6
				[NullableContext(0)]
				public unsafe EEImplMethodDescPtr(void* ptr, [Nullable(1)] IntPtr[] vtbl)
				{
					this.ptr_ = ptr;
					this.vtbl_ = vtbl;
				}

				// Token: 0x04001499 RID: 5273
				[Nullable(0)]
				private unsafe readonly void* ptr_;

				// Token: 0x0400149A RID: 5274
				private readonly IntPtr[] vtbl_;
			}

			// Token: 0x020005CE RID: 1486
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterfaceImpl(typeof(Fx.V48.EEImplMethodDescPtr))]
			public struct EEImplMethodDesc_64
			{
				// Token: 0x1700072F RID: 1839
				// (get) Token: 0x06002008 RID: 8200 RVA: 0x0006761C File Offset: 0x0006581C
				public static IntPtr[] FatVtable_
				{
					get
					{
						IntPtr[] result;
						if ((result = Fx.V48.EEImplMethodDesc_64.fatVtable_) == null)
						{
							result = (Fx.V48.EEImplMethodDesc_64.fatVtable_ = new IntPtr[0]);
						}
						return result;
					}
				}

				// Token: 0x0400149B RID: 5275
				public Fx.V48.StoredSigMethodDesc_64 @base;

				// Token: 0x0400149C RID: 5276
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x020005CF RID: 1487
			[NullableContext(1)]
			[Nullable(0)]
			[FatInterfaceImpl(typeof(Fx.V48.EEImplMethodDescPtr))]
			public struct EEImplMethodDesc_32
			{
				// Token: 0x17000730 RID: 1840
				// (get) Token: 0x06002009 RID: 8201 RVA: 0x00067633 File Offset: 0x00065833
				public static IntPtr[] FatVtable_
				{
					get
					{
						IntPtr[] result;
						if ((result = Fx.V48.EEImplMethodDesc_32.fatVtable_) == null)
						{
							result = (Fx.V48.EEImplMethodDesc_32.fatVtable_ = new IntPtr[0]);
						}
						return result;
					}
				}

				// Token: 0x0400149D RID: 5277
				public Fx.V48.StoredSigMethodDesc_32 @base;

				// Token: 0x0400149E RID: 5278
				[Nullable(2)]
				private static IntPtr[] fatVtable_;
			}

			// Token: 0x020005D0 RID: 1488
			[FatInterface]
			public struct ComPlusCallInfoPtr
			{
				// Token: 0x17000731 RID: 1841
				// (get) Token: 0x0600200A RID: 8202 RVA: 0x0006764A File Offset: 0x0006584A
				public static int CurrentSize { get; } = ((PlatformDetection.Architecture == ArchitectureKind.x86) ? sizeof(Fx.V48.ComPlusCallInfo_x86) : sizeof(Fx.V48.ComPlusCallInfo_other));

				// Token: 0x0600200B RID: 8203 RVA: 0x00067651 File Offset: 0x00065851
				public unsafe ComPlusCallInfoPtr(void* ptr, [Nullable(1)] IntPtr[] vtbl)
				{
					this.ptr_ = ptr;
					this.vtbl_ = vtbl;
				}

				// Token: 0x040014A0 RID: 5280
				private unsafe readonly void* ptr_;

				// Token: 0x040014A1 RID: 5281
				[Nullable(1)]
				private readonly IntPtr[] vtbl_;
			}

			// Token: 0x020005D1 RID: 1489
			public struct ComPlusCallInfo_x86
			{
				// Token: 0x040014A2 RID: 5282
				public unsafe void* union_m_pILStub_pEventProviderMD;

				// Token: 0x040014A3 RID: 5283
				public unsafe Fx.V48.MethodTable* m_pInterfaceMT;

				// Token: 0x040014A4 RID: 5284
				public byte m_flags;

				// Token: 0x040014A5 RID: 5285
				public ushort m_cachedComSlot;

				// Token: 0x040014A6 RID: 5286
				public ushort m_cbStackArgumentSize;

				// Token: 0x040014A7 RID: 5287
				public unsafe void* union_m_pRetThunk_pInterceptStub;

				// Token: 0x040014A8 RID: 5288
				private Fx.V48.RelativePointer m_pStubMD;
			}

			// Token: 0x020005D2 RID: 1490
			public struct ComPlusCallInfo_other
			{
				// Token: 0x040014A9 RID: 5289
				public unsafe void* union_m_pILStub_pEventProviderMD;

				// Token: 0x040014AA RID: 5290
				public unsafe Fx.V48.MethodTable* m_pInterfaceMT;

				// Token: 0x040014AB RID: 5291
				public byte m_flags;

				// Token: 0x040014AC RID: 5292
				public ushort m_cachedComSlot;

				// Token: 0x040014AD RID: 5293
				private Fx.V48.RelativePointer m_pStubMD;
			}

			// Token: 0x020005D3 RID: 1491
			public struct ComPlusCallMethodDesc
			{
				// Token: 0x040014AE RID: 5294
				public Fx.V48.MethodDesc @base;

				// Token: 0x040014AF RID: 5295
				public unsafe void* m_pComPlusCallInfo;
			}

			// Token: 0x020005D4 RID: 1492
			public struct InstantiatedMethodDesc
			{
				// Token: 0x17000732 RID: 1842
				// (get) Token: 0x0600200D RID: 8205 RVA: 0x0006767E File Offset: 0x0006587E
				public bool IMD_HasMethodInstantiation
				{
					get
					{
						return this.IMD_IsGenericMethodDefinition || this.m_pPerInstInfo != null;
					}
				}

				// Token: 0x17000733 RID: 1843
				// (get) Token: 0x0600200E RID: 8206 RVA: 0x00067697 File Offset: 0x00065897
				public bool IMD_IsGenericMethodDefinition
				{
					get
					{
						return (this.m_wFlags2 & Fx.V48.InstantiatedMethodDesc.Flags.KindMask) == Fx.V48.InstantiatedMethodDesc.Flags.GenericMethodDefinition;
					}
				}

				// Token: 0x17000734 RID: 1844
				// (get) Token: 0x0600200F RID: 8207 RVA: 0x000676A4 File Offset: 0x000658A4
				public bool IMD_IsWrapperStubWithInstantiations
				{
					get
					{
						return (this.m_wFlags2 & Fx.V48.InstantiatedMethodDesc.Flags.KindMask) == Fx.V48.InstantiatedMethodDesc.Flags.WrapperStubWithInstantiations;
					}
				}

				// Token: 0x17000735 RID: 1845
				// (get) Token: 0x06002010 RID: 8208 RVA: 0x000676B1 File Offset: 0x000658B1
				public bool IMD_HasComPlusCallInfo
				{
					get
					{
						return this.m_wFlags2.Has(Fx.V48.InstantiatedMethodDesc.Flags.HasComPlusCallInfo);
					}
				}

				// Token: 0x06002011 RID: 8209 RVA: 0x000676C0 File Offset: 0x000658C0
				public unsafe Fx.V48.MethodDesc* IMD_GetWrappedMethodDesc()
				{
					Helpers.Assert(this.IMD_IsWrapperStubWithInstantiations, null, "IMD_IsWrapperStubWithInstantiations");
					return (Fx.V48.MethodDesc*)this.union_pDictLayout_pWrappedMethodDesc;
				}

				// Token: 0x040014B0 RID: 5296
				public Fx.V48.MethodDesc @base;

				// Token: 0x040014B1 RID: 5297
				public unsafe void* union_pDictLayout_pWrappedMethodDesc;

				// Token: 0x040014B2 RID: 5298
				public unsafe Fx.V48.Dictionary* m_pPerInstInfo;

				// Token: 0x040014B3 RID: 5299
				public Fx.V48.InstantiatedMethodDesc.Flags m_wFlags2;

				// Token: 0x040014B4 RID: 5300
				public ushort m_wNumGenericArgs;

				// Token: 0x020005D5 RID: 1493
				[Flags]
				public enum Flags : ushort
				{
					// Token: 0x040014B6 RID: 5302
					KindMask = 7,
					// Token: 0x040014B7 RID: 5303
					GenericMethodDefinition = 0,
					// Token: 0x040014B8 RID: 5304
					UnsharedMethodInstantiation = 1,
					// Token: 0x040014B9 RID: 5305
					SharedMethodInstantiation = 2,
					// Token: 0x040014BA RID: 5306
					WrapperStubWithInstantiations = 3,
					// Token: 0x040014BB RID: 5307
					EnCAddedMethod = 7,
					// Token: 0x040014BC RID: 5308
					Unrestored = 8,
					// Token: 0x040014BD RID: 5309
					HasComPlusCallInfo = 16
				}
			}

			// Token: 0x020005D6 RID: 1494
			public struct RelativeFixupPointer
			{
				// Token: 0x17000736 RID: 1846
				// (get) Token: 0x06002012 RID: 8210 RVA: 0x000676DC File Offset: 0x000658DC
				public unsafe void* Value
				{
					get
					{
						IntPtr delta = this.value;
						if (delta == 0)
						{
							return null;
						}
						IntPtr addr = (byte*)Unsafe.AsPointer<Fx.V48.RelativeFixupPointer>(ref this) + delta;
						if ((addr & (IntPtr)1) != 0)
						{
							addr = *(addr - (IntPtr)1);
						}
						return addr;
					}
				}

				// Token: 0x040014BE RID: 5310
				[NativeInteger]
				private readonly IntPtr value;
			}

			// Token: 0x020005D7 RID: 1495
			public struct MethodDescChunk
			{
				// Token: 0x17000737 RID: 1847
				// (get) Token: 0x06002013 RID: 8211 RVA: 0x0006770C File Offset: 0x0006590C
				public unsafe Fx.V48.MethodTable* MethodTable
				{
					get
					{
						return (Fx.V48.MethodTable*)this.m_methodTable.Value;
					}
				}

				// Token: 0x17000738 RID: 1848
				// (get) Token: 0x06002014 RID: 8212 RVA: 0x00067719 File Offset: 0x00065919
				public unsafe Fx.V48.MethodDesc* FirstMethodDesc
				{
					get
					{
						return (Fx.V48.MethodDesc*)((byte*)Unsafe.AsPointer<Fx.V48.MethodDescChunk>(ref this) + sizeof(Fx.V48.MethodDescChunk));
					}
				}

				// Token: 0x17000739 RID: 1849
				// (get) Token: 0x06002015 RID: 8213 RVA: 0x00067728 File Offset: 0x00065928
				public uint Size
				{
					get
					{
						return (uint)(this.m_size + 1);
					}
				}

				// Token: 0x1700073A RID: 1850
				// (get) Token: 0x06002016 RID: 8214 RVA: 0x00067732 File Offset: 0x00065932
				public uint Count
				{
					get
					{
						return (uint)(this.m_count + 1);
					}
				}

				// Token: 0x1700073B RID: 1851
				// (get) Token: 0x06002017 RID: 8215 RVA: 0x0006773C File Offset: 0x0006593C
				[NativeInteger]
				public UIntPtr SizeOf
				{
					[return: NativeInteger]
					get
					{
						return (UIntPtr)((IntPtr)sizeof(Fx.V48.MethodDescChunk) + (IntPtr)((UIntPtr)this.Size * Fx.V48.MethodDesc.Alignment));
					}
				}

				// Token: 0x040014BF RID: 5311
				public Fx.V48.RelativeFixupPointer m_methodTable;

				// Token: 0x040014C0 RID: 5312
				public unsafe Fx.V48.MethodDescChunk* m_next;

				// Token: 0x040014C1 RID: 5313
				public byte m_size;

				// Token: 0x040014C2 RID: 5314
				public byte m_count;

				// Token: 0x040014C3 RID: 5315
				public Fx.V48.MethodDescChunk.Flags m_flagsAndTokenRange;

				// Token: 0x020005D8 RID: 1496
				[Flags]
				public enum Flags : ushort
				{
					// Token: 0x040014C5 RID: 5317
					TokenRangeMask = 1023,
					// Token: 0x040014C6 RID: 5318
					HasCompactEntrypoints = 16384,
					// Token: 0x040014C7 RID: 5319
					IsZapped = 32768
				}
			}

			// Token: 0x020005D9 RID: 1497
			public struct Dictionary
			{
			}

			// Token: 0x020005DA RID: 1498
			public struct Module
			{
			}

			// Token: 0x020005DB RID: 1499
			public struct MethodTableWriteableData
			{
			}

			// Token: 0x020005DC RID: 1500
			public struct RelativePointer
			{
				// Token: 0x06002018 RID: 8216 RVA: 0x00067753 File Offset: 0x00065953
				public RelativePointer([NativeInteger] IntPtr delta)
				{
					this.m_delta = delta;
				}

				// Token: 0x1700073C RID: 1852
				// (get) Token: 0x06002019 RID: 8217 RVA: 0x0006775C File Offset: 0x0006595C
				public unsafe void* Value
				{
					get
					{
						IntPtr delta = this.m_delta;
						if (delta != 0)
						{
							return Unsafe.AsPointer<Fx.V48.RelativePointer>(Unsafe.AddByteOffset<Fx.V48.RelativePointer>(ref this, delta));
						}
						return null;
					}
				}

				// Token: 0x040014C8 RID: 5320
				[NativeInteger]
				private IntPtr m_delta;
			}

			// Token: 0x020005DD RID: 1501
			public struct MethodTable
			{
				// Token: 0x1700073D RID: 1853
				// (get) Token: 0x0600201A RID: 8218 RVA: 0x00067782 File Offset: 0x00065982
				public Fx.V48.MethodTable.WFlagsHigh FlagsHigh
				{
					get
					{
						return (Fx.V48.MethodTable.WFlagsHigh)(this.m_dwFlags & 4294901760U);
					}
				}

				// Token: 0x1700073E RID: 1854
				// (get) Token: 0x0600201B RID: 8219 RVA: 0x00067790 File Offset: 0x00065990
				public Fx.V48.MethodTable.WFlagsLow FlagsLow
				{
					get
					{
						if (!this.FlagsHigh.Has((Fx.V48.MethodTable.WFlagsHigh)2147483648U))
						{
							return (Fx.V48.MethodTable.WFlagsLow)(this.m_dwFlags & 65535U);
						}
						return Fx.V48.MethodTable.WFlagsLow.StaticsMask_NonDynamic;
					}
				}

				// Token: 0x1700073F RID: 1855
				// (get) Token: 0x0600201C RID: 8220 RVA: 0x000677B2 File Offset: 0x000659B2
				public int ComponentSize
				{
					get
					{
						if (!this.FlagsHigh.Has((Fx.V48.MethodTable.WFlagsHigh)2147483648U))
						{
							return 0;
						}
						return (int)(this.m_dwFlags & 65535U);
					}
				}

				// Token: 0x0600201D RID: 8221 RVA: 0x000677D4 File Offset: 0x000659D4
				public unsafe Fx.V48.MethodTable* GetCanonicalMethodTable()
				{
					UIntPtr addr = this.union_pEEClass_pCanonMT;
					if ((addr & (UIntPtr)((IntPtr)2)) == 0)
					{
						return addr;
					}
					if ((addr & (UIntPtr)((IntPtr)1)) != 0)
					{
						return *(addr - (UIntPtr)((IntPtr)3));
					}
					return addr - 2;
				}

				// Token: 0x0600201E RID: 8222 RVA: 0x00067800 File Offset: 0x00065A00
				public unsafe Fx.V48.MethodDesc* GetParallelMethodDesc(Fx.V48.MethodDesc* pDefMD)
				{
					return this.GetMethodDescForSlot((uint)pDefMD->SlotNumber);
				}

				// Token: 0x17000740 RID: 1856
				// (get) Token: 0x0600201F RID: 8223 RVA: 0x0006780E File Offset: 0x00065A0E
				public bool IsInterface
				{
					get
					{
						return (this.m_dwFlags & 983040U) == 786432U;
					}
				}

				// Token: 0x06002020 RID: 8224 RVA: 0x00067823 File Offset: 0x00065A23
				public unsafe Fx.V48.MethodDesc* GetMethodDescForSlot(uint slotNumber)
				{
					if (this.IsInterface)
					{
						this.GetNumVirtuals();
					}
					throw new NotImplementedException();
				}

				// Token: 0x06002021 RID: 8225 RVA: 0x0006783C File Offset: 0x00065A3C
				public unsafe void* GetRestoredSlot(uint slotNumber)
				{
					Fx.V48.MethodTable* pMT = (Fx.V48.MethodTable*)Unsafe.AsPointer<Fx.V48.MethodTable>(ref this);
					void* slot;
					for (;;)
					{
						pMT = pMT->GetCanonicalMethodTable();
						slot = pMT->GetSlot(slotNumber);
						if (slot != null)
						{
							break;
						}
						pMT = pMT->ParentMethodTable;
					}
					return slot;
				}

				// Token: 0x17000741 RID: 1857
				// (get) Token: 0x06002022 RID: 8226 RVA: 0x0006786E File Offset: 0x00065A6E
				public bool HasIndirectParent
				{
					get
					{
						return this.FlagsHigh.Has(Fx.V48.MethodTable.WFlagsHigh.HasIndirectParent);
					}
				}

				// Token: 0x17000742 RID: 1858
				// (get) Token: 0x06002023 RID: 8227 RVA: 0x00067880 File Offset: 0x00065A80
				public unsafe Fx.V48.MethodTable* ParentMethodTable
				{
					get
					{
						void* ptr = this.m_pParentMethodTable;
						if (this.HasIndirectParent)
						{
							return (Fx.V48.MethodTable*)((Fx.V48.MethodTable*)ptr)->m_pParentMethodTable;
						}
						return (Fx.V48.MethodTable*)ptr;
					}
				}

				// Token: 0x06002024 RID: 8228 RVA: 0x000678A4 File Offset: 0x00065AA4
				public unsafe void* GetSlot(uint slotNumber)
				{
					return *this.GetSlotPtrRaw(slotNumber);
				}

				// Token: 0x06002025 RID: 8229 RVA: 0x000678B0 File Offset: 0x00065AB0
				[return: NativeInteger]
				public unsafe IntPtr GetSlotPtrRaw(uint slotNum)
				{
					if (slotNum < (uint)this.GetNumVirtuals())
					{
						uint index = Fx.V48.MethodTable.GetIndexOfVtableIndirection(slotNum);
						void** @base = *(IntPtr*)(this.GetVtableIndirections() + (ulong)index * (ulong)((long)sizeof(void*)) / (ulong)sizeof(void*));
						return @base + (ulong)Fx.V48.MethodTable.GetIndexAfterVtableIndirection(slotNum) * (ulong)((long)sizeof(void*)) / (ulong)sizeof(void*);
					}
					if (this.HasSingleNonVirtualSlot)
					{
						return this.GetNonVirtualSlotsPtr();
					}
					return this.GetNonVirtualSlotsArray() + (ulong)(slotNum - (uint)this.GetNumVirtuals()) * (ulong)((long)sizeof(void*)) / (ulong)sizeof(void*);
				}

				// Token: 0x06002026 RID: 8230 RVA: 0x0006791C File Offset: 0x00065B1C
				public ushort GetNumVirtuals()
				{
					return this.m_wNumVirtuals;
				}

				// Token: 0x06002027 RID: 8231 RVA: 0x00066C73 File Offset: 0x00064E73
				public static uint GetIndexOfVtableIndirection(uint slotNum)
				{
					return slotNum >> 3;
				}

				// Token: 0x06002028 RID: 8232 RVA: 0x00067924 File Offset: 0x00065B24
				public unsafe void** GetVtableIndirections()
				{
					return (void**)((byte*)Unsafe.AsPointer<Fx.V48.MethodTable>(ref this) + sizeof(Fx.V48.MethodTable));
				}

				// Token: 0x06002029 RID: 8233 RVA: 0x00066C87 File Offset: 0x00064E87
				public static uint GetIndexAfterVtableIndirection(uint slotNum)
				{
					return slotNum & 7U;
				}

				// Token: 0x17000743 RID: 1859
				// (get) Token: 0x0600202A RID: 8234 RVA: 0x00067933 File Offset: 0x00065B33
				public bool HasSingleNonVirtualSlot
				{
					get
					{
						return this.m_wFlags2.Has(Fx.V48.MethodTable.Flags2.HasSingleNonVirtualSlot);
					}
				}

				// Token: 0x0600202B RID: 8235 RVA: 0x00067948 File Offset: 0x00065B48
				[NullableContext(1)]
				[MultipurposeSlotOffsetTable(3, typeof(Fx.V48.MethodTable.MultipurposeSlotHelpers))]
				private static byte[] GetNonVirtualSlotsOffsets()
				{
					return new byte[]
					{
						Fx.V48.MethodTable.MultipurposeSlotHelpers.OffsetOfMp1(),
						Fx.V48.MethodTable.MultipurposeSlotHelpers.OffsetOfMp2(),
						Fx.V48.MethodTable.MultipurposeSlotHelpers.OffsetOfMp1(),
						Fx.V48.MethodTable.MultipurposeSlotHelpers.RegularOffset(2),
						Fx.V48.MethodTable.MultipurposeSlotHelpers.OffsetOfMp2(),
						Fx.V48.MethodTable.MultipurposeSlotHelpers.RegularOffset(2),
						Fx.V48.MethodTable.MultipurposeSlotHelpers.RegularOffset(2),
						Fx.V48.MethodTable.MultipurposeSlotHelpers.RegularOffset(3)
					};
				}

				// Token: 0x0600202C RID: 8236 RVA: 0x0006799F File Offset: 0x00065B9F
				[return: NativeInteger]
				public IntPtr GetNonVirtualSlotsPtr()
				{
					return this.GetMultipurposeSlotPtr(Fx.V48.MethodTable.Flags2.HasNonVirtualSlots, Fx.V48.MethodTable.c_NonVirtualSlotsOffsets);
				}

				// Token: 0x0600202D RID: 8237 RVA: 0x000679B0 File Offset: 0x00065BB0
				[NullableContext(1)]
				[return: NativeInteger]
				public unsafe IntPtr GetMultipurposeSlotPtr(Fx.V48.MethodTable.Flags2 flag, byte[] offsets)
				{
					IntPtr offset = (IntPtr)((UIntPtr)offsets[(int)(this.m_wFlags2 & (flag - 1))]);
					if (offset >= (IntPtr)sizeof(Fx.V48.MethodTable))
					{
						offset += (IntPtr)((UIntPtr)this.GetNumVTableIndirections() * (UIntPtr)((IntPtr)sizeof(void**)));
					}
					return (byte*)Unsafe.AsPointer<Fx.V48.MethodTable>(ref this) + offset;
				}

				// Token: 0x0600202E RID: 8238 RVA: 0x000679EF File Offset: 0x00065BEF
				public unsafe void** GetNonVirtualSlotsArray()
				{
					return (void**)this.GetNonVirtualSlotsPtr().Value;
				}

				// Token: 0x0600202F RID: 8239 RVA: 0x000679FC File Offset: 0x00065BFC
				public uint GetNumVTableIndirections()
				{
					return Fx.V48.MethodTable.GetNumVtableIndirections((uint)this.GetNumVirtuals());
				}

				// Token: 0x06002030 RID: 8240 RVA: 0x00066D5C File Offset: 0x00064F5C
				public static uint GetNumVtableIndirections(uint numVirtuals)
				{
					return numVirtuals + 7U >> 3;
				}

				// Token: 0x040014C9 RID: 5321
				private uint m_dwFlags;

				// Token: 0x040014CA RID: 5322
				public uint m_BaseSize;

				// Token: 0x040014CB RID: 5323
				public Fx.V48.MethodTable.Flags2 m_wFlags2;

				// Token: 0x040014CC RID: 5324
				public ushort m_wToken;

				// Token: 0x040014CD RID: 5325
				public ushort m_wNumVirtuals;

				// Token: 0x040014CE RID: 5326
				public ushort m_wNumInterfaces;

				// Token: 0x040014CF RID: 5327
				private unsafe void* m_pParentMethodTable;

				// Token: 0x040014D0 RID: 5328
				public unsafe Fx.V48.Module* m_pLoaderModule;

				// Token: 0x040014D1 RID: 5329
				public unsafe Fx.V48.MethodTableWriteableData* m_pWriteableData;

				// Token: 0x040014D2 RID: 5330
				public unsafe void* union_pEEClass_pCanonMT;

				// Token: 0x040014D3 RID: 5331
				public unsafe void* union_pPerInstInfo_ElementTypeHnd_pMultipurposeSlot1;

				// Token: 0x040014D4 RID: 5332
				public unsafe void* union_p_InterfaceMap_pMultipurposeSlot2;

				// Token: 0x040014D5 RID: 5333
				public const int VTABLE_SLOTS_PER_CHUNK = 8;

				// Token: 0x040014D6 RID: 5334
				public const int VTABLE_SLOTS_PER_CHUNK_LOG2 = 3;

				// Token: 0x040014D7 RID: 5335
				[Nullable(1)]
				private static readonly byte[] c_NonVirtualSlotsOffsets = Fx.V48.MethodTable.GetNonVirtualSlotsOffsets();

				// Token: 0x020005DE RID: 1502
				[Flags]
				public enum WFlagsLow : uint
				{
					// Token: 0x040014D9 RID: 5337
					UNUSED_ComponentSize_1 = 1U,
					// Token: 0x040014DA RID: 5338
					StaticsMask = 6U,
					// Token: 0x040014DB RID: 5339
					StaticsMask_NonDynamic = 0U,
					// Token: 0x040014DC RID: 5340
					StaticsMask_Dynamic = 2U,
					// Token: 0x040014DD RID: 5341
					StaticsMask_Generics = 4U,
					// Token: 0x040014DE RID: 5342
					StaticsMask_CrossModuleGenerics = 6U,
					// Token: 0x040014DF RID: 5343
					StaticsMask_IfGenericsThenCrossModule = 2U,
					// Token: 0x040014E0 RID: 5344
					NotInPZM = 8U,
					// Token: 0x040014E1 RID: 5345
					GenericsMask = 48U,
					// Token: 0x040014E2 RID: 5346
					GenericsMask_NonGeneric = 0U,
					// Token: 0x040014E3 RID: 5347
					GenericsMask_GenericInst = 16U,
					// Token: 0x040014E4 RID: 5348
					GenericsMask_SharedInst = 32U,
					// Token: 0x040014E5 RID: 5349
					GenericsMask_TypicalInst = 48U,
					// Token: 0x040014E6 RID: 5350
					ContextStatic = 64U,
					// Token: 0x040014E7 RID: 5351
					HasRemotingVtsInfo = 128U,
					// Token: 0x040014E8 RID: 5352
					HasVariance = 256U,
					// Token: 0x040014E9 RID: 5353
					HasDefaultCtor = 512U,
					// Token: 0x040014EA RID: 5354
					HasPreciseInitCctors = 1024U,
					// Token: 0x040014EB RID: 5355
					IsHFA_IsRegStructPassed = 2048U,
					// Token: 0x040014EC RID: 5356
					IsByRefLike = 4096U,
					// Token: 0x040014ED RID: 5357
					UNUSED_ComponentSize_5 = 8192U,
					// Token: 0x040014EE RID: 5358
					UNUSED_ComponentSize_6 = 16384U,
					// Token: 0x040014EF RID: 5359
					UNUSED_ComponentSize_7 = 32768U,
					// Token: 0x040014F0 RID: 5360
					StringArrayValues = 0U
				}

				// Token: 0x020005DF RID: 1503
				[Flags]
				public enum WFlagsHigh : uint
				{
					// Token: 0x040014F2 RID: 5362
					Category_Mask = 983040U,
					// Token: 0x040014F3 RID: 5363
					Category_Class = 0U,
					// Token: 0x040014F4 RID: 5364
					Category_Unused_1 = 65536U,
					// Token: 0x040014F5 RID: 5365
					Category_MarshalByRef_Mask = 917504U,
					// Token: 0x040014F6 RID: 5366
					Category_MarshalByRef = 131072U,
					// Token: 0x040014F7 RID: 5367
					Category_Contextful = 196608U,
					// Token: 0x040014F8 RID: 5368
					Category_ValueType = 262144U,
					// Token: 0x040014F9 RID: 5369
					Category_ValueType_Mask = 786432U,
					// Token: 0x040014FA RID: 5370
					Category_Nullable = 327680U,
					// Token: 0x040014FB RID: 5371
					Category_PrimitiveValueType = 393216U,
					// Token: 0x040014FC RID: 5372
					Category_TruePrimitive = 458752U,
					// Token: 0x040014FD RID: 5373
					Category_Array = 524288U,
					// Token: 0x040014FE RID: 5374
					Category_Array_Mask = 786432U,
					// Token: 0x040014FF RID: 5375
					Category_IfArrayThenSzArray = 131072U,
					// Token: 0x04001500 RID: 5376
					Category_Interface = 786432U,
					// Token: 0x04001501 RID: 5377
					Category_Unused_2 = 851968U,
					// Token: 0x04001502 RID: 5378
					Category_TransparentProxy = 917504U,
					// Token: 0x04001503 RID: 5379
					Category_AsyncPin = 983040U,
					// Token: 0x04001504 RID: 5380
					Category_ElementTypeMask = 917504U,
					// Token: 0x04001505 RID: 5381
					HasFinalizer = 1048576U,
					// Token: 0x04001506 RID: 5382
					IfNotInterfaceThenMarshalable = 2097152U,
					// Token: 0x04001507 RID: 5383
					IfInterfaceThenHasGuidInfo = 2097152U,
					// Token: 0x04001508 RID: 5384
					ICastable = 4194304U,
					// Token: 0x04001509 RID: 5385
					HasIndirectParent = 8388608U,
					// Token: 0x0400150A RID: 5386
					ContainsPointers = 16777216U,
					// Token: 0x0400150B RID: 5387
					HasTypeEquivalence = 33554432U,
					// Token: 0x0400150C RID: 5388
					HasRCWPerTypeData = 67108864U,
					// Token: 0x0400150D RID: 5389
					HasCriticalFinalizer = 134217728U,
					// Token: 0x0400150E RID: 5390
					Collectible = 268435456U,
					// Token: 0x0400150F RID: 5391
					ContainsGenericVariables = 536870912U,
					// Token: 0x04001510 RID: 5392
					ComObject = 1073741824U,
					// Token: 0x04001511 RID: 5393
					HasComponentSize = 2147483648U,
					// Token: 0x04001512 RID: 5394
					NonTrivialInterfaceCast = 1078460416U
				}

				// Token: 0x020005E0 RID: 1504
				[Flags]
				public enum Flags2 : ushort
				{
					// Token: 0x04001514 RID: 5396
					MultipurposeSlotsMask = 31,
					// Token: 0x04001515 RID: 5397
					HasPerInstInfo = 1,
					// Token: 0x04001516 RID: 5398
					HasInterfaceMap = 2,
					// Token: 0x04001517 RID: 5399
					HasDispatchMapSlot = 4,
					// Token: 0x04001518 RID: 5400
					HasNonVirtualSlots = 8,
					// Token: 0x04001519 RID: 5401
					HasModuleOverride = 16,
					// Token: 0x0400151A RID: 5402
					IsZapped = 32,
					// Token: 0x0400151B RID: 5403
					IsPreRestored = 64,
					// Token: 0x0400151C RID: 5404
					HasModuleDependencies = 128,
					// Token: 0x0400151D RID: 5405
					NoSecurityProperties = 256,
					// Token: 0x0400151E RID: 5406
					RequiresDispatchTokenFat = 512,
					// Token: 0x0400151F RID: 5407
					HasCctor = 1024,
					// Token: 0x04001520 RID: 5408
					HasCCWTemplate = 2048,
					// Token: 0x04001521 RID: 5409
					RequiresAlign8 = 4096,
					// Token: 0x04001522 RID: 5410
					HasBoxedRegularStatics = 8192,
					// Token: 0x04001523 RID: 5411
					HasSingleNonVirtualSlot = 16384,
					// Token: 0x04001524 RID: 5412
					DependsOnEquivalentOrForwardedStructs = 32768
				}

				// Token: 0x020005E1 RID: 1505
				public enum UnionLowBits
				{
					// Token: 0x04001526 RID: 5414
					EEClass,
					// Token: 0x04001527 RID: 5415
					Invalid,
					// Token: 0x04001528 RID: 5416
					MethodTable,
					// Token: 0x04001529 RID: 5417
					Indirection
				}

				// Token: 0x020005E2 RID: 1506
				private static class MultipurposeSlotHelpers
				{
					// Token: 0x06002032 RID: 8242 RVA: 0x00067A18 File Offset: 0x00065C18
					public unsafe static byte OffsetOfMp1()
					{
						Fx.V48.MethodTable t = default(Fx.V48.MethodTable);
						return (byte)((long)((byte*)(&t.union_pPerInstInfo_ElementTypeHnd_pMultipurposeSlot1) - (byte*)(&t)));
					}

					// Token: 0x06002033 RID: 8243 RVA: 0x00067A40 File Offset: 0x00065C40
					public unsafe static byte OffsetOfMp2()
					{
						Fx.V48.MethodTable t = default(Fx.V48.MethodTable);
						return (byte)((long)((byte*)(&t.union_p_InterfaceMap_pMultipurposeSlot2) - (byte*)(&t)));
					}

					// Token: 0x06002034 RID: 8244 RVA: 0x00067A65 File Offset: 0x00065C65
					public static byte RegularOffset(int index)
					{
						return (byte)(sizeof(Fx.V48.MethodTable) + index * IntPtr.Size - 2 * IntPtr.Size);
					}
				}
			}
		}
	}
}
