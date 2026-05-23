using System;
using System.Runtime.Serialization;

namespace Mono.Cecil
{
	// Token: 0x02000266 RID: 614
	[Serializable]
	internal sealed class ResolutionException : Exception
	{
		// Token: 0x17000339 RID: 825
		// (get) Token: 0x06000DE4 RID: 3556 RVA: 0x0002D989 File Offset: 0x0002BB89
		public MemberReference Member
		{
			get
			{
				return this.member;
			}
		}

		// Token: 0x1700033A RID: 826
		// (get) Token: 0x06000DE5 RID: 3557 RVA: 0x0002D994 File Offset: 0x0002BB94
		public IMetadataScope Scope
		{
			get
			{
				TypeReference type = this.member as TypeReference;
				if (type != null)
				{
					return type.Scope;
				}
				TypeReference declaring_type = this.member.DeclaringType;
				if (declaring_type != null)
				{
					return declaring_type.Scope;
				}
				throw new NotSupportedException();
			}
		}

		// Token: 0x06000DE6 RID: 3558 RVA: 0x0002D9D2 File Offset: 0x0002BBD2
		public ResolutionException(MemberReference member)
			: base("Failed to resolve " + member.FullName)
		{
			if (member == null)
			{
				throw new ArgumentNullException("member");
			}
			this.member = member;
		}

		// Token: 0x06000DE7 RID: 3559 RVA: 0x0002D9FF File Offset: 0x0002BBFF
		public ResolutionException(MemberReference member, Exception innerException)
			: base("Failed to resolve " + member.FullName, innerException)
		{
			if (member == null)
			{
				throw new ArgumentNullException("member");
			}
			this.member = member;
		}

		// Token: 0x06000DE8 RID: 3560 RVA: 0x0002DA2D File Offset: 0x0002BC2D
		private ResolutionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x0400041B RID: 1051
		private readonly MemberReference member;
	}
}
