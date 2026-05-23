using System;
using System.Text;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x0200022A RID: 554
	internal sealed class CallSite : IMethodSignature, IMetadataTokenProvider
	{
		// Token: 0x17000253 RID: 595
		// (get) Token: 0x06000BCC RID: 3020 RVA: 0x0002A184 File Offset: 0x00028384
		// (set) Token: 0x06000BCD RID: 3021 RVA: 0x0002A191 File Offset: 0x00028391
		public bool HasThis
		{
			get
			{
				return this.signature.HasThis;
			}
			set
			{
				this.signature.HasThis = value;
			}
		}

		// Token: 0x17000254 RID: 596
		// (get) Token: 0x06000BCE RID: 3022 RVA: 0x0002A19F File Offset: 0x0002839F
		// (set) Token: 0x06000BCF RID: 3023 RVA: 0x0002A1AC File Offset: 0x000283AC
		public bool ExplicitThis
		{
			get
			{
				return this.signature.ExplicitThis;
			}
			set
			{
				this.signature.ExplicitThis = value;
			}
		}

		// Token: 0x17000255 RID: 597
		// (get) Token: 0x06000BD0 RID: 3024 RVA: 0x0002A1BA File Offset: 0x000283BA
		// (set) Token: 0x06000BD1 RID: 3025 RVA: 0x0002A1C7 File Offset: 0x000283C7
		public MethodCallingConvention CallingConvention
		{
			get
			{
				return this.signature.CallingConvention;
			}
			set
			{
				this.signature.CallingConvention = value;
			}
		}

		// Token: 0x17000256 RID: 598
		// (get) Token: 0x06000BD2 RID: 3026 RVA: 0x0002A1D5 File Offset: 0x000283D5
		public bool HasParameters
		{
			get
			{
				return this.signature.HasParameters;
			}
		}

		// Token: 0x17000257 RID: 599
		// (get) Token: 0x06000BD3 RID: 3027 RVA: 0x0002A1E2 File Offset: 0x000283E2
		public Collection<ParameterDefinition> Parameters
		{
			get
			{
				return this.signature.Parameters;
			}
		}

		// Token: 0x17000258 RID: 600
		// (get) Token: 0x06000BD4 RID: 3028 RVA: 0x0002A1EF File Offset: 0x000283EF
		// (set) Token: 0x06000BD5 RID: 3029 RVA: 0x0002A201 File Offset: 0x00028401
		public TypeReference ReturnType
		{
			get
			{
				return this.signature.MethodReturnType.ReturnType;
			}
			set
			{
				this.signature.MethodReturnType.ReturnType = value;
			}
		}

		// Token: 0x17000259 RID: 601
		// (get) Token: 0x06000BD6 RID: 3030 RVA: 0x0002A214 File Offset: 0x00028414
		public MethodReturnType MethodReturnType
		{
			get
			{
				return this.signature.MethodReturnType;
			}
		}

		// Token: 0x1700025A RID: 602
		// (get) Token: 0x06000BD7 RID: 3031 RVA: 0x0002A221 File Offset: 0x00028421
		// (set) Token: 0x06000BD8 RID: 3032 RVA: 0x0001C627 File Offset: 0x0001A827
		public string Name
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

		// Token: 0x1700025B RID: 603
		// (get) Token: 0x06000BD9 RID: 3033 RVA: 0x0002A221 File Offset: 0x00028421
		// (set) Token: 0x06000BDA RID: 3034 RVA: 0x0001C627 File Offset: 0x0001A827
		public string Namespace
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

		// Token: 0x1700025C RID: 604
		// (get) Token: 0x06000BDB RID: 3035 RVA: 0x0002A228 File Offset: 0x00028428
		public ModuleDefinition Module
		{
			get
			{
				return this.ReturnType.Module;
			}
		}

		// Token: 0x1700025D RID: 605
		// (get) Token: 0x06000BDC RID: 3036 RVA: 0x0002A235 File Offset: 0x00028435
		public IMetadataScope Scope
		{
			get
			{
				return this.signature.ReturnType.Scope;
			}
		}

		// Token: 0x1700025E RID: 606
		// (get) Token: 0x06000BDD RID: 3037 RVA: 0x0002A247 File Offset: 0x00028447
		// (set) Token: 0x06000BDE RID: 3038 RVA: 0x0002A254 File Offset: 0x00028454
		public MetadataToken MetadataToken
		{
			get
			{
				return this.signature.token;
			}
			set
			{
				this.signature.token = value;
			}
		}

		// Token: 0x1700025F RID: 607
		// (get) Token: 0x06000BDF RID: 3039 RVA: 0x0002A264 File Offset: 0x00028464
		public string FullName
		{
			get
			{
				StringBuilder signature = new StringBuilder();
				signature.Append(this.ReturnType.FullName);
				this.MethodSignatureFullName(signature);
				return signature.ToString();
			}
		}

		// Token: 0x06000BE0 RID: 3040 RVA: 0x0002A296 File Offset: 0x00028496
		internal CallSite()
		{
			this.signature = new MethodReference();
			this.signature.token = new MetadataToken(TokenType.Signature, 0);
		}

		// Token: 0x06000BE1 RID: 3041 RVA: 0x0002A2BF File Offset: 0x000284BF
		public CallSite(TypeReference returnType)
			: this()
		{
			if (returnType == null)
			{
				throw new ArgumentNullException("returnType");
			}
			this.signature.ReturnType = returnType;
		}

		// Token: 0x06000BE2 RID: 3042 RVA: 0x0002A2E1 File Offset: 0x000284E1
		public override string ToString()
		{
			return this.FullName;
		}

		// Token: 0x0400038F RID: 911
		private readonly MethodReference signature;
	}
}
