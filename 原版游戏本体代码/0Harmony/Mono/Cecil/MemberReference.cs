using System;

namespace Mono.Cecil
{
	// Token: 0x02000263 RID: 611
	internal abstract class MemberReference : IMetadataTokenProvider
	{
		// Token: 0x17000330 RID: 816
		// (get) Token: 0x06000DCD RID: 3533 RVA: 0x0002D88C File Offset: 0x0002BA8C
		// (set) Token: 0x06000DCE RID: 3534 RVA: 0x0002D894 File Offset: 0x0002BA94
		public virtual string Name
		{
			get
			{
				return this.name;
			}
			set
			{
				if (this.IsWindowsRuntimeProjection && value != this.name)
				{
					throw new InvalidOperationException();
				}
				this.name = value;
			}
		}

		// Token: 0x17000331 RID: 817
		// (get) Token: 0x06000DCF RID: 3535
		public abstract string FullName { get; }

		// Token: 0x17000332 RID: 818
		// (get) Token: 0x06000DD0 RID: 3536 RVA: 0x0002D8B9 File Offset: 0x0002BAB9
		// (set) Token: 0x06000DD1 RID: 3537 RVA: 0x0002D8C1 File Offset: 0x0002BAC1
		public virtual TypeReference DeclaringType
		{
			get
			{
				return this.declaring_type;
			}
			set
			{
				this.declaring_type = value;
			}
		}

		// Token: 0x17000333 RID: 819
		// (get) Token: 0x06000DD2 RID: 3538 RVA: 0x0002D8CA File Offset: 0x0002BACA
		// (set) Token: 0x06000DD3 RID: 3539 RVA: 0x0002D8D2 File Offset: 0x0002BAD2
		public MetadataToken MetadataToken
		{
			get
			{
				return this.token;
			}
			set
			{
				this.token = value;
			}
		}

		// Token: 0x17000334 RID: 820
		// (get) Token: 0x06000DD4 RID: 3540 RVA: 0x0002D8DB File Offset: 0x0002BADB
		public bool IsWindowsRuntimeProjection
		{
			get
			{
				return this.projection != null;
			}
		}

		// Token: 0x17000335 RID: 821
		// (get) Token: 0x06000DD5 RID: 3541 RVA: 0x0002D8E8 File Offset: 0x0002BAE8
		internal bool HasImage
		{
			get
			{
				ModuleDefinition module = this.Module;
				return module != null && module.HasImage;
			}
		}

		// Token: 0x17000336 RID: 822
		// (get) Token: 0x06000DD6 RID: 3542 RVA: 0x0002D907 File Offset: 0x0002BB07
		public virtual ModuleDefinition Module
		{
			get
			{
				if (this.declaring_type == null)
				{
					return null;
				}
				return this.declaring_type.Module;
			}
		}

		// Token: 0x17000337 RID: 823
		// (get) Token: 0x06000DD7 RID: 3543 RVA: 0x0001B69F File Offset: 0x0001989F
		public virtual bool IsDefinition
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000338 RID: 824
		// (get) Token: 0x06000DD8 RID: 3544 RVA: 0x0002D91E File Offset: 0x0002BB1E
		public virtual bool ContainsGenericParameter
		{
			get
			{
				return this.declaring_type != null && this.declaring_type.ContainsGenericParameter;
			}
		}

		// Token: 0x06000DD9 RID: 3545 RVA: 0x00002B15 File Offset: 0x00000D15
		internal MemberReference()
		{
		}

		// Token: 0x06000DDA RID: 3546 RVA: 0x0002D935 File Offset: 0x0002BB35
		internal MemberReference(string name)
		{
			this.name = name ?? string.Empty;
		}

		// Token: 0x06000DDB RID: 3547 RVA: 0x0002D94D File Offset: 0x0002BB4D
		internal string MemberFullName()
		{
			if (this.declaring_type == null)
			{
				return this.name;
			}
			return this.declaring_type.FullName + "::" + this.name;
		}

		// Token: 0x06000DDC RID: 3548 RVA: 0x0002D979 File Offset: 0x0002BB79
		public IMemberDefinition Resolve()
		{
			return this.ResolveDefinition();
		}

		// Token: 0x06000DDD RID: 3549
		protected abstract IMemberDefinition ResolveDefinition();

		// Token: 0x06000DDE RID: 3550 RVA: 0x0002D981 File Offset: 0x0002BB81
		public override string ToString()
		{
			return this.FullName;
		}

		// Token: 0x04000417 RID: 1047
		private string name;

		// Token: 0x04000418 RID: 1048
		private TypeReference declaring_type;

		// Token: 0x04000419 RID: 1049
		internal MetadataToken token;

		// Token: 0x0400041A RID: 1050
		internal object projection;
	}
}
