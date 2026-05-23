using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.Reflection.Emit
{
	// Token: 0x0200065F RID: 1631
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_SignatureHelper))]
	[ComVisible(true)]
	public sealed class SignatureHelper : _SignatureHelper
	{
		// Token: 0x06004CFB RID: 19707 RVA: 0x00117415 File Offset: 0x00115615
		[SecuritySafeCritical]
		public static SignatureHelper GetMethodSigHelper(Module mod, Type returnType, Type[] parameterTypes)
		{
			return SignatureHelper.GetMethodSigHelper(mod, CallingConventions.Standard, returnType, null, null, parameterTypes, null, null);
		}

		// Token: 0x06004CFC RID: 19708 RVA: 0x00117424 File Offset: 0x00115624
		[SecurityCritical]
		internal static SignatureHelper GetMethodSigHelper(Module mod, CallingConventions callingConvention, Type returnType, int cGenericParam)
		{
			return SignatureHelper.GetMethodSigHelper(mod, callingConvention, cGenericParam, returnType, null, null, null, null, null);
		}

		// Token: 0x06004CFD RID: 19709 RVA: 0x0011743F File Offset: 0x0011563F
		[SecuritySafeCritical]
		public static SignatureHelper GetMethodSigHelper(Module mod, CallingConventions callingConvention, Type returnType)
		{
			return SignatureHelper.GetMethodSigHelper(mod, callingConvention, returnType, null, null, null, null, null);
		}

		// Token: 0x06004CFE RID: 19710 RVA: 0x00117450 File Offset: 0x00115650
		internal static SignatureHelper GetMethodSpecSigHelper(Module scope, Type[] inst)
		{
			SignatureHelper signatureHelper = new SignatureHelper(scope, MdSigCallingConvention.GenericInst);
			signatureHelper.AddData(inst.Length);
			foreach (Type clsArgument in inst)
			{
				signatureHelper.AddArgument(clsArgument);
			}
			return signatureHelper;
		}

		// Token: 0x06004CFF RID: 19711 RVA: 0x0011748C File Offset: 0x0011568C
		[SecurityCritical]
		internal static SignatureHelper GetMethodSigHelper(Module scope, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
		{
			return SignatureHelper.GetMethodSigHelper(scope, callingConvention, 0, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
		}

		// Token: 0x06004D00 RID: 19712 RVA: 0x001174AC File Offset: 0x001156AC
		[SecurityCritical]
		internal static SignatureHelper GetMethodSigHelper(Module scope, CallingConventions callingConvention, int cGenericParam, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
		{
			if (returnType == null)
			{
				returnType = typeof(void);
			}
			MdSigCallingConvention mdSigCallingConvention = MdSigCallingConvention.Default;
			if ((callingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
			{
				mdSigCallingConvention = MdSigCallingConvention.Vararg;
			}
			if (cGenericParam > 0)
			{
				mdSigCallingConvention |= MdSigCallingConvention.Generic;
			}
			if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
			{
				mdSigCallingConvention |= MdSigCallingConvention.HasThis;
			}
			SignatureHelper signatureHelper = new SignatureHelper(scope, mdSigCallingConvention, cGenericParam, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers);
			signatureHelper.AddArguments(parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
			return signatureHelper;
		}

		// Token: 0x06004D01 RID: 19713 RVA: 0x0011750C File Offset: 0x0011570C
		[SecuritySafeCritical]
		public static SignatureHelper GetMethodSigHelper(Module mod, CallingConvention unmanagedCallConv, Type returnType)
		{
			if (returnType == null)
			{
				returnType = typeof(void);
			}
			MdSigCallingConvention callingConvention;
			if (unmanagedCallConv == CallingConvention.Cdecl)
			{
				callingConvention = MdSigCallingConvention.C;
			}
			else if (unmanagedCallConv == CallingConvention.StdCall || unmanagedCallConv == CallingConvention.Winapi)
			{
				callingConvention = MdSigCallingConvention.StdCall;
			}
			else if (unmanagedCallConv == CallingConvention.ThisCall)
			{
				callingConvention = MdSigCallingConvention.ThisCall;
			}
			else
			{
				if (unmanagedCallConv != CallingConvention.FastCall)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_UnknownUnmanagedCallConv"), "unmanagedCallConv");
				}
				callingConvention = MdSigCallingConvention.FastCall;
			}
			return new SignatureHelper(mod, callingConvention, returnType, null, null);
		}

		// Token: 0x06004D02 RID: 19714 RVA: 0x00117573 File Offset: 0x00115773
		public static SignatureHelper GetLocalVarSigHelper()
		{
			return SignatureHelper.GetLocalVarSigHelper(null);
		}

		// Token: 0x06004D03 RID: 19715 RVA: 0x0011757B File Offset: 0x0011577B
		public static SignatureHelper GetMethodSigHelper(CallingConventions callingConvention, Type returnType)
		{
			return SignatureHelper.GetMethodSigHelper(null, callingConvention, returnType);
		}

		// Token: 0x06004D04 RID: 19716 RVA: 0x00117585 File Offset: 0x00115785
		public static SignatureHelper GetMethodSigHelper(CallingConvention unmanagedCallingConvention, Type returnType)
		{
			return SignatureHelper.GetMethodSigHelper(null, unmanagedCallingConvention, returnType);
		}

		// Token: 0x06004D05 RID: 19717 RVA: 0x0011758F File Offset: 0x0011578F
		public static SignatureHelper GetLocalVarSigHelper(Module mod)
		{
			return new SignatureHelper(mod, MdSigCallingConvention.LocalSig);
		}

		// Token: 0x06004D06 RID: 19718 RVA: 0x00117598 File Offset: 0x00115798
		public static SignatureHelper GetFieldSigHelper(Module mod)
		{
			return new SignatureHelper(mod, MdSigCallingConvention.Field);
		}

		// Token: 0x06004D07 RID: 19719 RVA: 0x001175A1 File Offset: 0x001157A1
		public static SignatureHelper GetPropertySigHelper(Module mod, Type returnType, Type[] parameterTypes)
		{
			return SignatureHelper.GetPropertySigHelper(mod, returnType, null, null, parameterTypes, null, null);
		}

		// Token: 0x06004D08 RID: 19720 RVA: 0x001175AF File Offset: 0x001157AF
		public static SignatureHelper GetPropertySigHelper(Module mod, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
		{
			return SignatureHelper.GetPropertySigHelper(mod, (CallingConventions)0, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
		}

		// Token: 0x06004D09 RID: 19721 RVA: 0x001175C4 File Offset: 0x001157C4
		[SecuritySafeCritical]
		public static SignatureHelper GetPropertySigHelper(Module mod, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
		{
			if (returnType == null)
			{
				returnType = typeof(void);
			}
			MdSigCallingConvention mdSigCallingConvention = MdSigCallingConvention.Property;
			if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
			{
				mdSigCallingConvention |= MdSigCallingConvention.HasThis;
			}
			SignatureHelper signatureHelper = new SignatureHelper(mod, mdSigCallingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers);
			signatureHelper.AddArguments(parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
			return signatureHelper;
		}

		// Token: 0x06004D0A RID: 19722 RVA: 0x0011760E File Offset: 0x0011580E
		[SecurityCritical]
		internal static SignatureHelper GetTypeSigToken(Module mod, Type type)
		{
			if (mod == null)
			{
				throw new ArgumentNullException("module");
			}
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			return new SignatureHelper(mod, type);
		}

		// Token: 0x06004D0B RID: 19723 RVA: 0x0011763F File Offset: 0x0011583F
		private SignatureHelper(Module mod, MdSigCallingConvention callingConvention)
		{
			this.Init(mod, callingConvention);
		}

		// Token: 0x06004D0C RID: 19724 RVA: 0x0011764F File Offset: 0x0011584F
		[SecurityCritical]
		private SignatureHelper(Module mod, MdSigCallingConvention callingConvention, int cGenericParameters, Type returnType, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
		{
			this.Init(mod, callingConvention, cGenericParameters);
			if (callingConvention == MdSigCallingConvention.Field)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_BadFieldSig"));
			}
			this.AddOneArgTypeHelper(returnType, requiredCustomModifiers, optionalCustomModifiers);
		}

		// Token: 0x06004D0D RID: 19725 RVA: 0x00117680 File Offset: 0x00115880
		[SecurityCritical]
		private SignatureHelper(Module mod, MdSigCallingConvention callingConvention, Type returnType, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
			: this(mod, callingConvention, 0, returnType, requiredCustomModifiers, optionalCustomModifiers)
		{
		}

		// Token: 0x06004D0E RID: 19726 RVA: 0x00117690 File Offset: 0x00115890
		[SecurityCritical]
		private SignatureHelper(Module mod, Type type)
		{
			this.Init(mod);
			this.AddOneArgTypeHelper(type);
		}

		// Token: 0x06004D0F RID: 19727 RVA: 0x001176A8 File Offset: 0x001158A8
		private void Init(Module mod)
		{
			this.m_signature = new byte[32];
			this.m_currSig = 0;
			this.m_module = mod as ModuleBuilder;
			this.m_argCount = 0;
			this.m_sigDone = false;
			this.m_sizeLoc = -1;
			if (this.m_module == null && mod != null)
			{
				throw new ArgumentException(Environment.GetResourceString("NotSupported_MustBeModuleBuilder"));
			}
		}

		// Token: 0x06004D10 RID: 19728 RVA: 0x00117711 File Offset: 0x00115911
		private void Init(Module mod, MdSigCallingConvention callingConvention)
		{
			this.Init(mod, callingConvention, 0);
		}

		// Token: 0x06004D11 RID: 19729 RVA: 0x0011771C File Offset: 0x0011591C
		private void Init(Module mod, MdSigCallingConvention callingConvention, int cGenericParam)
		{
			this.Init(mod);
			this.AddData((int)callingConvention);
			if (callingConvention == MdSigCallingConvention.Field || callingConvention == MdSigCallingConvention.GenericInst)
			{
				this.m_sizeLoc = -1;
				return;
			}
			if (cGenericParam > 0)
			{
				this.AddData(cGenericParam);
			}
			int currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			this.m_sizeLoc = currSig;
		}

		// Token: 0x06004D12 RID: 19730 RVA: 0x0011776A File Offset: 0x0011596A
		[SecurityCritical]
		private void AddOneArgTypeHelper(Type argument, bool pinned)
		{
			if (pinned)
			{
				this.AddElementType(CorElementType.Pinned);
			}
			this.AddOneArgTypeHelper(argument);
		}

		// Token: 0x06004D13 RID: 19731 RVA: 0x00117780 File Offset: 0x00115980
		[SecurityCritical]
		private void AddOneArgTypeHelper(Type clsArgument, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
		{
			if (optionalCustomModifiers != null)
			{
				foreach (Type type in optionalCustomModifiers)
				{
					if (type == null)
					{
						throw new ArgumentNullException("optionalCustomModifiers");
					}
					if (type.HasElementType)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_ArraysInvalid"), "optionalCustomModifiers");
					}
					if (type.ContainsGenericParameters)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_GenericsInvalid"), "optionalCustomModifiers");
					}
					this.AddElementType(CorElementType.CModOpt);
					int token = this.m_module.GetTypeToken(type).Token;
					this.AddToken(token);
				}
			}
			if (requiredCustomModifiers != null)
			{
				foreach (Type type2 in requiredCustomModifiers)
				{
					if (type2 == null)
					{
						throw new ArgumentNullException("requiredCustomModifiers");
					}
					if (type2.HasElementType)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_ArraysInvalid"), "requiredCustomModifiers");
					}
					if (type2.ContainsGenericParameters)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_GenericsInvalid"), "requiredCustomModifiers");
					}
					this.AddElementType(CorElementType.CModReqd);
					int token2 = this.m_module.GetTypeToken(type2).Token;
					this.AddToken(token2);
				}
			}
			this.AddOneArgTypeHelper(clsArgument);
		}

		// Token: 0x06004D14 RID: 19732 RVA: 0x001178BA File Offset: 0x00115ABA
		[SecurityCritical]
		private void AddOneArgTypeHelper(Type clsArgument)
		{
			this.AddOneArgTypeHelperWorker(clsArgument, false);
		}

		// Token: 0x06004D15 RID: 19733 RVA: 0x001178C4 File Offset: 0x00115AC4
		[SecurityCritical]
		private void AddOneArgTypeHelperWorker(Type clsArgument, bool lastWasGenericInst)
		{
			if (clsArgument.IsGenericParameter)
			{
				if (clsArgument.DeclaringMethod != null)
				{
					this.AddElementType(CorElementType.MVar);
				}
				else
				{
					this.AddElementType(CorElementType.Var);
				}
				this.AddData(clsArgument.GenericParameterPosition);
				return;
			}
			if (clsArgument.IsGenericType && (!clsArgument.IsGenericTypeDefinition || !lastWasGenericInst))
			{
				this.AddElementType(CorElementType.GenericInst);
				this.AddOneArgTypeHelperWorker(clsArgument.GetGenericTypeDefinition(), true);
				Type[] genericArguments = clsArgument.GetGenericArguments();
				this.AddData(genericArguments.Length);
				foreach (Type clsArgument2 in genericArguments)
				{
					this.AddOneArgTypeHelper(clsArgument2);
				}
				return;
			}
			if (clsArgument is TypeBuilder)
			{
				TypeBuilder typeBuilder = (TypeBuilder)clsArgument;
				TypeToken typeToken;
				if (typeBuilder.Module.Equals(this.m_module))
				{
					typeToken = typeBuilder.TypeToken;
				}
				else
				{
					typeToken = this.m_module.GetTypeToken(clsArgument);
				}
				if (clsArgument.IsValueType)
				{
					this.InternalAddTypeToken(typeToken, CorElementType.ValueType);
					return;
				}
				this.InternalAddTypeToken(typeToken, CorElementType.Class);
				return;
			}
			else if (clsArgument is EnumBuilder)
			{
				TypeBuilder typeBuilder2 = ((EnumBuilder)clsArgument).m_typeBuilder;
				TypeToken typeToken2;
				if (typeBuilder2.Module.Equals(this.m_module))
				{
					typeToken2 = typeBuilder2.TypeToken;
				}
				else
				{
					typeToken2 = this.m_module.GetTypeToken(clsArgument);
				}
				if (clsArgument.IsValueType)
				{
					this.InternalAddTypeToken(typeToken2, CorElementType.ValueType);
					return;
				}
				this.InternalAddTypeToken(typeToken2, CorElementType.Class);
				return;
			}
			else
			{
				if (clsArgument.IsByRef)
				{
					this.AddElementType(CorElementType.ByRef);
					clsArgument = clsArgument.GetElementType();
					this.AddOneArgTypeHelper(clsArgument);
					return;
				}
				if (clsArgument.IsPointer)
				{
					this.AddElementType(CorElementType.Ptr);
					this.AddOneArgTypeHelper(clsArgument.GetElementType());
					return;
				}
				if (clsArgument.IsArray)
				{
					if (clsArgument.IsSzArray)
					{
						this.AddElementType(CorElementType.SzArray);
						this.AddOneArgTypeHelper(clsArgument.GetElementType());
						return;
					}
					this.AddElementType(CorElementType.Array);
					this.AddOneArgTypeHelper(clsArgument.GetElementType());
					int arrayRank = clsArgument.GetArrayRank();
					this.AddData(arrayRank);
					this.AddData(0);
					this.AddData(arrayRank);
					for (int j = 0; j < arrayRank; j++)
					{
						this.AddData(0);
					}
					return;
				}
				else
				{
					CorElementType corElementType = CorElementType.Max;
					if (clsArgument is RuntimeType)
					{
						corElementType = RuntimeTypeHandle.GetCorElementType((RuntimeType)clsArgument);
						if (corElementType == CorElementType.Class)
						{
							if (clsArgument == typeof(object))
							{
								corElementType = CorElementType.Object;
							}
							else if (clsArgument == typeof(string))
							{
								corElementType = CorElementType.String;
							}
						}
					}
					if (SignatureHelper.IsSimpleType(corElementType))
					{
						this.AddElementType(corElementType);
						return;
					}
					if (this.m_module == null)
					{
						this.InternalAddRuntimeType(clsArgument);
						return;
					}
					if (clsArgument.IsValueType)
					{
						this.InternalAddTypeToken(this.m_module.GetTypeToken(clsArgument), CorElementType.ValueType);
						return;
					}
					this.InternalAddTypeToken(this.m_module.GetTypeToken(clsArgument), CorElementType.Class);
					return;
				}
			}
		}

		// Token: 0x06004D16 RID: 19734 RVA: 0x00117B6C File Offset: 0x00115D6C
		private void AddData(int data)
		{
			if (this.m_currSig + 4 > this.m_signature.Length)
			{
				this.m_signature = this.ExpandArray(this.m_signature);
			}
			if (data <= 127)
			{
				byte[] signature = this.m_signature;
				int currSig = this.m_currSig;
				this.m_currSig = currSig + 1;
				signature[currSig] = (byte)(data & 255);
				return;
			}
			if (data <= 16383)
			{
				byte[] signature2 = this.m_signature;
				int currSig = this.m_currSig;
				this.m_currSig = currSig + 1;
				signature2[currSig] = (byte)((data >> 8) | 128);
				byte[] signature3 = this.m_signature;
				currSig = this.m_currSig;
				this.m_currSig = currSig + 1;
				signature3[currSig] = (byte)(data & 255);
				return;
			}
			if (data <= 536870911)
			{
				byte[] signature4 = this.m_signature;
				int currSig = this.m_currSig;
				this.m_currSig = currSig + 1;
				signature4[currSig] = (byte)((data >> 24) | 192);
				byte[] signature5 = this.m_signature;
				currSig = this.m_currSig;
				this.m_currSig = currSig + 1;
				signature5[currSig] = (byte)((data >> 16) & 255);
				byte[] signature6 = this.m_signature;
				currSig = this.m_currSig;
				this.m_currSig = currSig + 1;
				signature6[currSig] = (byte)((data >> 8) & 255);
				byte[] signature7 = this.m_signature;
				currSig = this.m_currSig;
				this.m_currSig = currSig + 1;
				signature7[currSig] = (byte)(data & 255);
				return;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_LargeInteger"));
		}

		// Token: 0x06004D17 RID: 19735 RVA: 0x00117CB4 File Offset: 0x00115EB4
		private void AddData(uint data)
		{
			if (this.m_currSig + 4 > this.m_signature.Length)
			{
				this.m_signature = this.ExpandArray(this.m_signature);
			}
			byte[] signature = this.m_signature;
			int currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			signature[currSig] = (byte)(data & 255U);
			byte[] signature2 = this.m_signature;
			currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			signature2[currSig] = (byte)((data >> 8) & 255U);
			byte[] signature3 = this.m_signature;
			currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			signature3[currSig] = (byte)((data >> 16) & 255U);
			byte[] signature4 = this.m_signature;
			currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			signature4[currSig] = (byte)((data >> 24) & 255U);
		}

		// Token: 0x06004D18 RID: 19736 RVA: 0x00117D70 File Offset: 0x00115F70
		private void AddData(ulong data)
		{
			if (this.m_currSig + 8 > this.m_signature.Length)
			{
				this.m_signature = this.ExpandArray(this.m_signature);
			}
			byte[] signature = this.m_signature;
			int currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			signature[currSig] = (byte)(data & 255UL);
			byte[] signature2 = this.m_signature;
			currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			signature2[currSig] = (byte)((data >> 8) & 255UL);
			byte[] signature3 = this.m_signature;
			currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			signature3[currSig] = (byte)((data >> 16) & 255UL);
			byte[] signature4 = this.m_signature;
			currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			signature4[currSig] = (byte)((data >> 24) & 255UL);
			byte[] signature5 = this.m_signature;
			currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			signature5[currSig] = (byte)((data >> 32) & 255UL);
			byte[] signature6 = this.m_signature;
			currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			signature6[currSig] = (byte)((data >> 40) & 255UL);
			byte[] signature7 = this.m_signature;
			currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			signature7[currSig] = (byte)((data >> 48) & 255UL);
			byte[] signature8 = this.m_signature;
			currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			signature8[currSig] = (byte)((data >> 56) & 255UL);
		}

		// Token: 0x06004D19 RID: 19737 RVA: 0x00117EC0 File Offset: 0x001160C0
		private void AddElementType(CorElementType cvt)
		{
			if (this.m_currSig + 1 > this.m_signature.Length)
			{
				this.m_signature = this.ExpandArray(this.m_signature);
			}
			byte[] signature = this.m_signature;
			int currSig = this.m_currSig;
			this.m_currSig = currSig + 1;
			signature[currSig] = cvt;
		}

		// Token: 0x06004D1A RID: 19738 RVA: 0x00117F0C File Offset: 0x0011610C
		private void AddToken(int token)
		{
			int num = token & 16777215;
			MetadataTokenType metadataTokenType = (MetadataTokenType)(token & -16777216);
			if (num > 67108863)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_LargeInteger"));
			}
			num <<= 2;
			if (metadataTokenType == MetadataTokenType.TypeRef)
			{
				num |= 1;
			}
			else if (metadataTokenType == MetadataTokenType.TypeSpec)
			{
				num |= 2;
			}
			this.AddData(num);
		}

		// Token: 0x06004D1B RID: 19739 RVA: 0x00117F66 File Offset: 0x00116166
		private void InternalAddTypeToken(TypeToken clsToken, CorElementType CorType)
		{
			this.AddElementType(CorType);
			this.AddToken(clsToken.Token);
		}

		// Token: 0x06004D1C RID: 19740 RVA: 0x00117F7C File Offset: 0x0011617C
		[SecurityCritical]
		private unsafe void InternalAddRuntimeType(Type type)
		{
			this.AddElementType(CorElementType.Internal);
			IntPtr value = type.GetTypeHandleInternal().Value;
			if (this.m_currSig + sizeof(void*) > this.m_signature.Length)
			{
				this.m_signature = this.ExpandArray(this.m_signature);
			}
			byte* ptr = (byte*)(&value);
			for (int i = 0; i < sizeof(void*); i++)
			{
				byte[] signature = this.m_signature;
				int currSig = this.m_currSig;
				this.m_currSig = currSig + 1;
				signature[currSig] = ptr[i];
			}
		}

		// Token: 0x06004D1D RID: 19741 RVA: 0x00117FFD File Offset: 0x001161FD
		private byte[] ExpandArray(byte[] inArray)
		{
			return this.ExpandArray(inArray, inArray.Length * 2);
		}

		// Token: 0x06004D1E RID: 19742 RVA: 0x0011800C File Offset: 0x0011620C
		private byte[] ExpandArray(byte[] inArray, int requiredLength)
		{
			if (requiredLength < inArray.Length)
			{
				requiredLength = inArray.Length * 2;
			}
			byte[] array = new byte[requiredLength];
			Array.Copy(inArray, array, inArray.Length);
			return array;
		}

		// Token: 0x06004D1F RID: 19743 RVA: 0x00118038 File Offset: 0x00116238
		private void IncrementArgCounts()
		{
			if (this.m_sizeLoc == -1)
			{
				return;
			}
			this.m_argCount++;
		}

		// Token: 0x06004D20 RID: 19744 RVA: 0x00118054 File Offset: 0x00116254
		private void SetNumberOfSignatureElements(bool forceCopy)
		{
			int currSig = this.m_currSig;
			if (this.m_sizeLoc == -1)
			{
				return;
			}
			if (this.m_argCount < 128 && !forceCopy)
			{
				this.m_signature[this.m_sizeLoc] = (byte)this.m_argCount;
				return;
			}
			int num;
			if (this.m_argCount < 128)
			{
				num = 1;
			}
			else if (this.m_argCount < 16384)
			{
				num = 2;
			}
			else
			{
				num = 4;
			}
			byte[] array = new byte[this.m_currSig + num - 1];
			array[0] = this.m_signature[0];
			Array.Copy(this.m_signature, this.m_sizeLoc + 1, array, this.m_sizeLoc + num, currSig - (this.m_sizeLoc + 1));
			this.m_signature = array;
			this.m_currSig = this.m_sizeLoc;
			this.AddData(this.m_argCount);
			this.m_currSig = currSig + (num - 1);
		}

		// Token: 0x17000C15 RID: 3093
		// (get) Token: 0x06004D21 RID: 19745 RVA: 0x00118126 File Offset: 0x00116326
		internal int ArgumentCount
		{
			get
			{
				return this.m_argCount;
			}
		}

		// Token: 0x06004D22 RID: 19746 RVA: 0x0011812E File Offset: 0x0011632E
		internal static bool IsSimpleType(CorElementType type)
		{
			return type <= CorElementType.String || (type == CorElementType.TypedByRef || type == CorElementType.I || type == CorElementType.U || type == CorElementType.Object);
		}

		// Token: 0x06004D23 RID: 19747 RVA: 0x0011814E File Offset: 0x0011634E
		internal byte[] InternalGetSignature(out int length)
		{
			if (!this.m_sigDone)
			{
				this.m_sigDone = true;
				this.SetNumberOfSignatureElements(false);
			}
			length = this.m_currSig;
			return this.m_signature;
		}

		// Token: 0x06004D24 RID: 19748 RVA: 0x00118174 File Offset: 0x00116374
		internal byte[] InternalGetSignatureArray()
		{
			int argCount = this.m_argCount;
			int currSig = this.m_currSig;
			int num = currSig;
			if (argCount < 127)
			{
				num++;
			}
			else if (argCount < 16383)
			{
				num += 2;
			}
			else
			{
				num += 4;
			}
			byte[] array = new byte[num];
			int destinationIndex = 0;
			array[destinationIndex++] = this.m_signature[0];
			if (argCount <= 127)
			{
				array[destinationIndex++] = (byte)(argCount & 255);
			}
			else if (argCount <= 16383)
			{
				array[destinationIndex++] = (byte)((argCount >> 8) | 128);
				array[destinationIndex++] = (byte)(argCount & 255);
			}
			else
			{
				if (argCount > 536870911)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_LargeInteger"));
				}
				array[destinationIndex++] = (byte)((argCount >> 24) | 192);
				array[destinationIndex++] = (byte)((argCount >> 16) & 255);
				array[destinationIndex++] = (byte)((argCount >> 8) & 255);
				array[destinationIndex++] = (byte)(argCount & 255);
			}
			Array.Copy(this.m_signature, 2, array, destinationIndex, currSig - 2);
			array[num - 1] = 0;
			return array;
		}

		// Token: 0x06004D25 RID: 19749 RVA: 0x00118291 File Offset: 0x00116491
		public void AddArgument(Type clsArgument)
		{
			this.AddArgument(clsArgument, null, null);
		}

		// Token: 0x06004D26 RID: 19750 RVA: 0x0011829C File Offset: 0x0011649C
		[SecuritySafeCritical]
		public void AddArgument(Type argument, bool pinned)
		{
			if (argument == null)
			{
				throw new ArgumentNullException("argument");
			}
			this.IncrementArgCounts();
			this.AddOneArgTypeHelper(argument, pinned);
		}

		// Token: 0x06004D27 RID: 19751 RVA: 0x001182C0 File Offset: 0x001164C0
		public void AddArguments(Type[] arguments, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
		{
			if (requiredCustomModifiers != null && (arguments == null || requiredCustomModifiers.Length != arguments.Length))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MismatchedArrays", new object[] { "requiredCustomModifiers", "arguments" }));
			}
			if (optionalCustomModifiers != null && (arguments == null || optionalCustomModifiers.Length != arguments.Length))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MismatchedArrays", new object[] { "optionalCustomModifiers", "arguments" }));
			}
			if (arguments != null)
			{
				for (int i = 0; i < arguments.Length; i++)
				{
					this.AddArgument(arguments[i], (requiredCustomModifiers == null) ? null : requiredCustomModifiers[i], (optionalCustomModifiers == null) ? null : optionalCustomModifiers[i]);
				}
			}
		}

		// Token: 0x06004D28 RID: 19752 RVA: 0x00118361 File Offset: 0x00116561
		[SecuritySafeCritical]
		public void AddArgument(Type argument, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
		{
			if (this.m_sigDone)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_SigIsFinalized"));
			}
			if (argument == null)
			{
				throw new ArgumentNullException("argument");
			}
			this.IncrementArgCounts();
			this.AddOneArgTypeHelper(argument, requiredCustomModifiers, optionalCustomModifiers);
		}

		// Token: 0x06004D29 RID: 19753 RVA: 0x0011839E File Offset: 0x0011659E
		public void AddSentinel()
		{
			this.AddElementType(CorElementType.Sentinel);
		}

		// Token: 0x06004D2A RID: 19754 RVA: 0x001183A8 File Offset: 0x001165A8
		public override bool Equals(object obj)
		{
			if (!(obj is SignatureHelper))
			{
				return false;
			}
			SignatureHelper signatureHelper = (SignatureHelper)obj;
			if (!signatureHelper.m_module.Equals(this.m_module) || signatureHelper.m_currSig != this.m_currSig || signatureHelper.m_sizeLoc != this.m_sizeLoc || signatureHelper.m_sigDone != this.m_sigDone)
			{
				return false;
			}
			for (int i = 0; i < this.m_currSig; i++)
			{
				if (this.m_signature[i] != signatureHelper.m_signature[i])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06004D2B RID: 19755 RVA: 0x0011842C File Offset: 0x0011662C
		public override int GetHashCode()
		{
			int num = this.m_module.GetHashCode() + this.m_currSig + this.m_sizeLoc;
			if (this.m_sigDone)
			{
				num++;
			}
			for (int i = 0; i < this.m_currSig; i++)
			{
				num += this.m_signature[i].GetHashCode();
			}
			return num;
		}

		// Token: 0x06004D2C RID: 19756 RVA: 0x00118485 File Offset: 0x00116685
		public byte[] GetSignature()
		{
			return this.GetSignature(false);
		}

		// Token: 0x06004D2D RID: 19757 RVA: 0x00118490 File Offset: 0x00116690
		internal byte[] GetSignature(bool appendEndOfSig)
		{
			if (!this.m_sigDone)
			{
				if (appendEndOfSig)
				{
					this.AddElementType(CorElementType.End);
				}
				this.SetNumberOfSignatureElements(true);
				this.m_sigDone = true;
			}
			if (this.m_signature.Length > this.m_currSig)
			{
				byte[] array = new byte[this.m_currSig];
				Array.Copy(this.m_signature, array, this.m_currSig);
				this.m_signature = array;
			}
			return this.m_signature;
		}

		// Token: 0x06004D2E RID: 19758 RVA: 0x001184F8 File Offset: 0x001166F8
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Length: " + this.m_currSig.ToString() + Environment.NewLine);
			if (this.m_sizeLoc != -1)
			{
				stringBuilder.Append("Arguments: " + this.m_signature[this.m_sizeLoc].ToString() + Environment.NewLine);
			}
			else
			{
				stringBuilder.Append("Field Signature" + Environment.NewLine);
			}
			stringBuilder.Append("Signature: " + Environment.NewLine);
			for (int i = 0; i <= this.m_currSig; i++)
			{
				stringBuilder.Append(this.m_signature[i].ToString() + "  ");
			}
			stringBuilder.Append(Environment.NewLine);
			return stringBuilder.ToString();
		}

		// Token: 0x06004D2F RID: 19759 RVA: 0x001185D4 File Offset: 0x001167D4
		void _SignatureHelper.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004D30 RID: 19760 RVA: 0x001185DB File Offset: 0x001167DB
		void _SignatureHelper.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004D31 RID: 19761 RVA: 0x001185E2 File Offset: 0x001167E2
		void _SignatureHelper.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004D32 RID: 19762 RVA: 0x001185E9 File Offset: 0x001167E9
		void _SignatureHelper.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0400219E RID: 8606
		private const int NO_SIZE_IN_SIG = -1;

		// Token: 0x0400219F RID: 8607
		private byte[] m_signature;

		// Token: 0x040021A0 RID: 8608
		private int m_currSig;

		// Token: 0x040021A1 RID: 8609
		private int m_sizeLoc;

		// Token: 0x040021A2 RID: 8610
		private ModuleBuilder m_module;

		// Token: 0x040021A3 RID: 8611
		private bool m_sigDone;

		// Token: 0x040021A4 RID: 8612
		private int m_argCount;
	}
}
