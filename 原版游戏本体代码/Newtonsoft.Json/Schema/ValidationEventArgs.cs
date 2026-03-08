using System;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Schema
{
	/// <summary>
	/// <para>
	/// Returns detailed information related to the <see cref="T:Newtonsoft.Json.Schema.ValidationEventHandler" />.
	/// </para>
	/// <note type="caution">
	/// JSON Schema validation has been moved to its own package. See <see href="https://www.newtonsoft.com/jsonschema">https://www.newtonsoft.com/jsonschema</see> for more details.
	/// </note>
	/// </summary>
	// Token: 0x020000B4 RID: 180
	[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
	public class ValidationEventArgs : EventArgs
	{
		// Token: 0x06000972 RID: 2418 RVA: 0x0002806B File Offset: 0x0002626B
		internal ValidationEventArgs(JsonSchemaException ex)
		{
			ValidationUtils.ArgumentNotNull(ex, "ex");
			this._ex = ex;
		}

		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Schema.JsonSchemaException" /> associated with the validation error.
		/// </summary>
		/// <value>The JsonSchemaException associated with the validation error.</value>
		// Token: 0x170001B7 RID: 439
		// (get) Token: 0x06000973 RID: 2419 RVA: 0x00028085 File Offset: 0x00026285
		public JsonSchemaException Exception
		{
			get
			{
				return this._ex;
			}
		}

		/// <summary>
		/// Gets the path of the JSON location where the validation error occurred.
		/// </summary>
		/// <value>The path of the JSON location where the validation error occurred.</value>
		// Token: 0x170001B8 RID: 440
		// (get) Token: 0x06000974 RID: 2420 RVA: 0x0002808D File Offset: 0x0002628D
		public string Path
		{
			get
			{
				return this._ex.Path;
			}
		}

		/// <summary>
		/// Gets the text description corresponding to the validation error.
		/// </summary>
		/// <value>The text description.</value>
		// Token: 0x170001B9 RID: 441
		// (get) Token: 0x06000975 RID: 2421 RVA: 0x0002809A File Offset: 0x0002629A
		public string Message
		{
			get
			{
				return this._ex.Message;
			}
		}

		// Token: 0x04000371 RID: 881
		private readonly JsonSchemaException _ex;
	}
}
