using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Schema
{
	/// <summary>
	/// <para>
	/// Contains the JSON schema extension methods.
	/// </para>
	/// <note type="caution">
	/// JSON Schema validation has been moved to its own package. See <see href="https://www.newtonsoft.com/jsonschema">https://www.newtonsoft.com/jsonschema</see> for more details.
	/// </note>
	/// </summary>
	// Token: 0x020000A6 RID: 166
	[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
	public static class Extensions
	{
		/// <summary>
		/// <para>
		/// Determines whether the <see cref="T:Newtonsoft.Json.Linq.JToken" /> is valid.
		/// </para>
		/// <note type="caution">
		/// JSON Schema validation has been moved to its own package. See <see href="https://www.newtonsoft.com/jsonschema">https://www.newtonsoft.com/jsonschema</see> for more details.
		/// </note>
		/// </summary>
		/// <param name="source">The source <see cref="T:Newtonsoft.Json.Linq.JToken" /> to test.</param>
		/// <param name="schema">The schema to test with.</param>
		/// <returns>
		/// 	<c>true</c> if the specified <see cref="T:Newtonsoft.Json.Linq.JToken" /> is valid; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x06000898 RID: 2200 RVA: 0x00024E9C File Offset: 0x0002309C
		[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
		public static bool IsValid(this JToken source, JsonSchema schema)
		{
			bool valid = true;
			source.Validate(schema, delegate(object sender, ValidationEventArgs args)
			{
				valid = false;
			});
			return valid;
		}

		/// <summary>
		/// <para>
		/// Determines whether the <see cref="T:Newtonsoft.Json.Linq.JToken" /> is valid.
		/// </para>
		/// <note type="caution">
		/// JSON Schema validation has been moved to its own package. See <see href="https://www.newtonsoft.com/jsonschema">https://www.newtonsoft.com/jsonschema</see> for more details.
		/// </note>
		/// </summary>
		/// <param name="source">The source <see cref="T:Newtonsoft.Json.Linq.JToken" /> to test.</param>
		/// <param name="schema">The schema to test with.</param>
		/// <param name="errorMessages">When this method returns, contains any error messages generated while validating. </param>
		/// <returns>
		/// 	<c>true</c> if the specified <see cref="T:Newtonsoft.Json.Linq.JToken" /> is valid; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x06000899 RID: 2201 RVA: 0x00024ED0 File Offset: 0x000230D0
		[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
		public static bool IsValid(this JToken source, JsonSchema schema, out IList<string> errorMessages)
		{
			IList<string> errors = new List<string>();
			source.Validate(schema, delegate(object sender, ValidationEventArgs args)
			{
				errors.Add(args.Message);
			});
			errorMessages = errors;
			return errorMessages.Count == 0;
		}

		/// <summary>
		/// <para>
		/// Validates the specified <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </para>
		/// <note type="caution">
		/// JSON Schema validation has been moved to its own package. See <see href="https://www.newtonsoft.com/jsonschema">https://www.newtonsoft.com/jsonschema</see> for more details.
		/// </note>
		/// </summary>
		/// <param name="source">The source <see cref="T:Newtonsoft.Json.Linq.JToken" /> to test.</param>
		/// <param name="schema">The schema to test with.</param>
		// Token: 0x0600089A RID: 2202 RVA: 0x00024F13 File Offset: 0x00023113
		[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
		public static void Validate(this JToken source, JsonSchema schema)
		{
			source.Validate(schema, null);
		}

		/// <summary>
		/// <para>
		/// Validates the specified <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </para>
		/// <note type="caution">
		/// JSON Schema validation has been moved to its own package. See <see href="https://www.newtonsoft.com/jsonschema">https://www.newtonsoft.com/jsonschema</see> for more details.
		/// </note>
		/// </summary>
		/// <param name="source">The source <see cref="T:Newtonsoft.Json.Linq.JToken" /> to test.</param>
		/// <param name="schema">The schema to test with.</param>
		/// <param name="validationEventHandler">The validation event handler.</param>
		// Token: 0x0600089B RID: 2203 RVA: 0x00024F20 File Offset: 0x00023120
		[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
		public static void Validate(this JToken source, JsonSchema schema, ValidationEventHandler validationEventHandler)
		{
			ValidationUtils.ArgumentNotNull(source, "source");
			ValidationUtils.ArgumentNotNull(schema, "schema");
			using (JsonValidatingReader jsonValidatingReader = new JsonValidatingReader(source.CreateReader()))
			{
				jsonValidatingReader.Schema = schema;
				if (validationEventHandler != null)
				{
					jsonValidatingReader.ValidationEventHandler += validationEventHandler;
				}
				while (jsonValidatingReader.Read())
				{
				}
			}
		}
	}
}
