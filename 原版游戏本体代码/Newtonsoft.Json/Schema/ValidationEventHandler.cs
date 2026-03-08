using System;

namespace Newtonsoft.Json.Schema
{
	/// <summary>
	/// <para>
	/// Represents the callback method that will handle JSON schema validation events and the <see cref="T:Newtonsoft.Json.Schema.ValidationEventArgs" />.
	/// </para>
	/// <note type="caution">
	/// JSON Schema validation has been moved to its own package. See <see href="https://www.newtonsoft.com/jsonschema">https://www.newtonsoft.com/jsonschema</see> for more details.
	/// </note>
	/// </summary>
	// Token: 0x020000B5 RID: 181
	// (Invoke) Token: 0x06000977 RID: 2423
	[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
	public delegate void ValidationEventHandler(object sender, ValidationEventArgs e);
}
