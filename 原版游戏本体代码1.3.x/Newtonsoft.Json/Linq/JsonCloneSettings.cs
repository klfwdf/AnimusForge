using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Specifies the settings used when cloning JSON.
	/// </summary>
	// Token: 0x020000C3 RID: 195
	public class JsonCloneSettings
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JsonCloneSettings" /> class.
		/// </summary>
		// Token: 0x06000ABF RID: 2751 RVA: 0x0002B289 File Offset: 0x00029489
		public JsonCloneSettings()
		{
			this.CopyAnnotations = true;
		}

		/// <summary>
		/// Gets or sets a flag that indicates whether to copy annotations when cloning a <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// The default value is <c>true</c>.
		/// </summary>
		/// <value>
		/// A flag that indicates whether to copy annotations when cloning a <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </value>
		// Token: 0x170001EC RID: 492
		// (get) Token: 0x06000AC0 RID: 2752 RVA: 0x0002B298 File Offset: 0x00029498
		// (set) Token: 0x06000AC1 RID: 2753 RVA: 0x0002B2A0 File Offset: 0x000294A0
		public bool CopyAnnotations { get; set; }

		// Token: 0x0400038A RID: 906
		[Nullable(1)]
		internal static readonly JsonCloneSettings SkipCopyAnnotations = new JsonCloneSettings
		{
			CopyAnnotations = false
		};
	}
}
