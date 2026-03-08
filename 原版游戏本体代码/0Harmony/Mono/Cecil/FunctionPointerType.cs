using System;
using System.Text;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x0200023C RID: 572
	internal sealed class FunctionPointerType : TypeSpecification, IMethodSignature, IMetadataTokenProvider
	{
		// Token: 0x170002C7 RID: 711
		// (get) Token: 0x06000CBB RID: 3259 RVA: 0x0002B70D File Offset: 0x0002990D
		// (set) Token: 0x06000CBC RID: 3260 RVA: 0x0002B71A File Offset: 0x0002991A
		public bool HasThis
		{
			get
			{
				return this.function.HasThis;
			}
			set
			{
				this.function.HasThis = value;
			}
		}

		// Token: 0x170002C8 RID: 712
		// (get) Token: 0x06000CBD RID: 3261 RVA: 0x0002B728 File Offset: 0x00029928
		// (set) Token: 0x06000CBE RID: 3262 RVA: 0x0002B735 File Offset: 0x00029935
		public bool ExplicitThis
		{
			get
			{
				return this.function.ExplicitThis;
			}
			set
			{
				this.function.ExplicitThis = value;
			}
		}

		// Token: 0x170002C9 RID: 713
		// (get) Token: 0x06000CBF RID: 3263 RVA: 0x0002B743 File Offset: 0x00029943
		// (set) Token: 0x06000CC0 RID: 3264 RVA: 0x0002B750 File Offset: 0x00029950
		public MethodCallingConvention CallingConvention
		{
			get
			{
				return this.function.CallingConvention;
			}
			set
			{
				this.function.CallingConvention = value;
			}
		}

		// Token: 0x170002CA RID: 714
		// (get) Token: 0x06000CC1 RID: 3265 RVA: 0x0002B75E File Offset: 0x0002995E
		public bool HasParameters
		{
			get
			{
				return this.function.HasParameters;
			}
		}

		// Token: 0x170002CB RID: 715
		// (get) Token: 0x06000CC2 RID: 3266 RVA: 0x0002B76B File Offset: 0x0002996B
		public Collection<ParameterDefinition> Parameters
		{
			get
			{
				return this.function.Parameters;
			}
		}

		// Token: 0x170002CC RID: 716
		// (get) Token: 0x06000CC3 RID: 3267 RVA: 0x0002B778 File Offset: 0x00029978
		// (set) Token: 0x06000CC4 RID: 3268 RVA: 0x0002B78A File Offset: 0x0002998A
		public TypeReference ReturnType
		{
			get
			{
				return this.function.MethodReturnType.ReturnType;
			}
			set
			{
				this.function.MethodReturnType.ReturnType = value;
			}
		}

		// Token: 0x170002CD RID: 717
		// (get) Token: 0x06000CC5 RID: 3269 RVA: 0x0002B79D File Offset: 0x0002999D
		public MethodReturnType MethodReturnType
		{
			get
			{
				return this.function.MethodReturnType;
			}
		}

		// Token: 0x170002CE RID: 718
		// (get) Token: 0x06000CC6 RID: 3270 RVA: 0x0002B7AA File Offset: 0x000299AA
		// (set) Token: 0x06000CC7 RID: 3271 RVA: 0x0001C627 File Offset: 0x0001A827
		public override string Name
		{
			get
			{
				return this.function.Name;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x170002CF RID: 719
		// (get) Token: 0x06000CC8 RID: 3272 RVA: 0x0002A221 File Offset: 0x00028421
		// (set) Token: 0x06000CC9 RID: 3273 RVA: 0x0001C627 File Offset: 0x0001A827
		public override string Namespace
		{
			get
			{
				return string.Empty;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x170002D0 RID: 720
		// (get) Token: 0x06000CCA RID: 3274 RVA: 0x0002B7B7 File Offset: 0x000299B7
		public override ModuleDefinition Module
		{
			get
			{
				return this.ReturnType.Module;
			}
		}

		// Token: 0x170002D1 RID: 721
		// (get) Token: 0x06000CCB RID: 3275 RVA: 0x0002B7C4 File Offset: 0x000299C4
		// (set) Token: 0x06000CCC RID: 3276 RVA: 0x0001C627 File Offset: 0x0001A827
		public override IMetadataScope Scope
		{
			get
			{
				return this.function.ReturnType.Scope;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x170002D2 RID: 722
		// (get) Token: 0x06000CCD RID: 3277 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsFunctionPointer
		{
			get
			{
				return true;
			}
		}

		// Token: 0x170002D3 RID: 723
		// (get) Token: 0x06000CCE RID: 3278 RVA: 0x0002B7D6 File Offset: 0x000299D6
		public override bool ContainsGenericParameter
		{
			get
			{
				return this.function.ContainsGenericParameter;
			}
		}

		// Token: 0x170002D4 RID: 724
		// (get) Token: 0x06000CCF RID: 3279 RVA: 0x0002B7E4 File Offset: 0x000299E4
		public override string FullName
		{
			get
			{
				StringBuilder signature = new StringBuilder();
				signature.Append(this.function.Name);
				signature.Append(" ");
				signature.Append(this.function.ReturnType.FullName);
				signature.Append(" *");
				this.MethodSignatureFullName(signature);
				return signature.ToString();
			}
		}

		// Token: 0x06000CD0 RID: 3280 RVA: 0x0002B845 File Offset: 0x00029A45
		public FunctionPointerType()
			: base(null)
		{
			this.function = new MethodReference();
			this.function.Name = "method";
			this.etype = Mono.Cecil.Metadata.ElementType.FnPtr;
		}

		// Token: 0x06000CD1 RID: 3281 RVA: 0x0002B871 File Offset: 0x00029A71
		public override TypeDefinition Resolve()
		{
			return null;
		}

		// Token: 0x06000CD2 RID: 3282 RVA: 0x0001B6A2 File Offset: 0x000198A2
		public override TypeReference GetElementType()
		{
			return this;
		}

		// Token: 0x040003D9 RID: 985
		private readonly MethodReference function;
	}
}
