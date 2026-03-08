using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000310 RID: 784
	internal sealed class ConstantDebugInformation : DebugInformation
	{
		// Token: 0x1700052E RID: 1326
		// (get) Token: 0x06001465 RID: 5221 RVA: 0x00040FB6 File Offset: 0x0003F1B6
		// (set) Token: 0x06001466 RID: 5222 RVA: 0x00040FBE File Offset: 0x0003F1BE
		public string Name
		{
			get
			{
				return this.name;
			}
			set
			{
				this.name = value;
			}
		}

		// Token: 0x1700052F RID: 1327
		// (get) Token: 0x06001467 RID: 5223 RVA: 0x00040FC7 File Offset: 0x0003F1C7
		// (set) Token: 0x06001468 RID: 5224 RVA: 0x00040FCF File Offset: 0x0003F1CF
		public TypeReference ConstantType
		{
			get
			{
				return this.constant_type;
			}
			set
			{
				this.constant_type = value;
			}
		}

		// Token: 0x17000530 RID: 1328
		// (get) Token: 0x06001469 RID: 5225 RVA: 0x00040FD8 File Offset: 0x0003F1D8
		// (set) Token: 0x0600146A RID: 5226 RVA: 0x00040FE0 File Offset: 0x0003F1E0
		public object Value
		{
			get
			{
				return this.value;
			}
			set
			{
				this.value = value;
			}
		}

		// Token: 0x0600146B RID: 5227 RVA: 0x00040FE9 File Offset: 0x0003F1E9
		public ConstantDebugInformation(string name, TypeReference constant_type, object value)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			this.name = name;
			this.constant_type = constant_type;
			this.value = value;
			this.token = new MetadataToken(TokenType.LocalConstant);
		}

		// Token: 0x04000A2A RID: 2602
		private string name;

		// Token: 0x04000A2B RID: 2603
		private TypeReference constant_type;

		// Token: 0x04000A2C RID: 2604
		private object value;
	}
}
