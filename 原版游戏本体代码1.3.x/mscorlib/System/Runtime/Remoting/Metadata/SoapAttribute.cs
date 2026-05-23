using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Metadata
{
	// Token: 0x020007DB RID: 2011
	[ComVisible(true)]
	public class SoapAttribute : Attribute
	{
		// Token: 0x06005712 RID: 22290 RVA: 0x00134D8B File Offset: 0x00132F8B
		internal void SetReflectInfo(object info)
		{
			this.ReflectInfo = info;
		}

		// Token: 0x17000E5D RID: 3677
		// (get) Token: 0x06005713 RID: 22291 RVA: 0x00134D94 File Offset: 0x00132F94
		// (set) Token: 0x06005714 RID: 22292 RVA: 0x00134D9C File Offset: 0x00132F9C
		public virtual string XmlNamespace
		{
			get
			{
				return this.ProtXmlNamespace;
			}
			set
			{
				this.ProtXmlNamespace = value;
			}
		}

		// Token: 0x17000E5E RID: 3678
		// (get) Token: 0x06005715 RID: 22293 RVA: 0x00134DA5 File Offset: 0x00132FA5
		// (set) Token: 0x06005716 RID: 22294 RVA: 0x00134DAD File Offset: 0x00132FAD
		public virtual bool UseAttribute
		{
			get
			{
				return this._bUseAttribute;
			}
			set
			{
				this._bUseAttribute = value;
			}
		}

		// Token: 0x17000E5F RID: 3679
		// (get) Token: 0x06005717 RID: 22295 RVA: 0x00134DB6 File Offset: 0x00132FB6
		// (set) Token: 0x06005718 RID: 22296 RVA: 0x00134DBE File Offset: 0x00132FBE
		public virtual bool Embedded
		{
			get
			{
				return this._bEmbedded;
			}
			set
			{
				this._bEmbedded = value;
			}
		}

		// Token: 0x040027E4 RID: 10212
		protected string ProtXmlNamespace;

		// Token: 0x040027E5 RID: 10213
		private bool _bUseAttribute;

		// Token: 0x040027E6 RID: 10214
		private bool _bEmbedded;

		// Token: 0x040027E7 RID: 10215
		protected object ReflectInfo;
	}
}
