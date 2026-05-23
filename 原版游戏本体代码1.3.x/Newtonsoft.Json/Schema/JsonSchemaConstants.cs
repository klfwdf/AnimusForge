using System;
using System.Collections.Generic;

namespace Newtonsoft.Json.Schema
{
	// Token: 0x020000A9 RID: 169
	[Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
	internal static class JsonSchemaConstants
	{
		// Token: 0x04000316 RID: 790
		public const string TypePropertyName = "type";

		// Token: 0x04000317 RID: 791
		public const string PropertiesPropertyName = "properties";

		// Token: 0x04000318 RID: 792
		public const string ItemsPropertyName = "items";

		// Token: 0x04000319 RID: 793
		public const string AdditionalItemsPropertyName = "additionalItems";

		// Token: 0x0400031A RID: 794
		public const string RequiredPropertyName = "required";

		// Token: 0x0400031B RID: 795
		public const string PatternPropertiesPropertyName = "patternProperties";

		// Token: 0x0400031C RID: 796
		public const string AdditionalPropertiesPropertyName = "additionalProperties";

		// Token: 0x0400031D RID: 797
		public const string RequiresPropertyName = "requires";

		// Token: 0x0400031E RID: 798
		public const string MinimumPropertyName = "minimum";

		// Token: 0x0400031F RID: 799
		public const string MaximumPropertyName = "maximum";

		// Token: 0x04000320 RID: 800
		public const string ExclusiveMinimumPropertyName = "exclusiveMinimum";

		// Token: 0x04000321 RID: 801
		public const string ExclusiveMaximumPropertyName = "exclusiveMaximum";

		// Token: 0x04000322 RID: 802
		public const string MinimumItemsPropertyName = "minItems";

		// Token: 0x04000323 RID: 803
		public const string MaximumItemsPropertyName = "maxItems";

		// Token: 0x04000324 RID: 804
		public const string PatternPropertyName = "pattern";

		// Token: 0x04000325 RID: 805
		public const string MaximumLengthPropertyName = "maxLength";

		// Token: 0x04000326 RID: 806
		public const string MinimumLengthPropertyName = "minLength";

		// Token: 0x04000327 RID: 807
		public const string EnumPropertyName = "enum";

		// Token: 0x04000328 RID: 808
		public const string ReadOnlyPropertyName = "readonly";

		// Token: 0x04000329 RID: 809
		public const string TitlePropertyName = "title";

		// Token: 0x0400032A RID: 810
		public const string DescriptionPropertyName = "description";

		// Token: 0x0400032B RID: 811
		public const string FormatPropertyName = "format";

		// Token: 0x0400032C RID: 812
		public const string DefaultPropertyName = "default";

		// Token: 0x0400032D RID: 813
		public const string TransientPropertyName = "transient";

		// Token: 0x0400032E RID: 814
		public const string DivisibleByPropertyName = "divisibleBy";

		// Token: 0x0400032F RID: 815
		public const string HiddenPropertyName = "hidden";

		// Token: 0x04000330 RID: 816
		public const string DisallowPropertyName = "disallow";

		// Token: 0x04000331 RID: 817
		public const string ExtendsPropertyName = "extends";

		// Token: 0x04000332 RID: 818
		public const string IdPropertyName = "id";

		// Token: 0x04000333 RID: 819
		public const string UniqueItemsPropertyName = "uniqueItems";

		// Token: 0x04000334 RID: 820
		public const string OptionValuePropertyName = "value";

		// Token: 0x04000335 RID: 821
		public const string OptionLabelPropertyName = "label";

		// Token: 0x04000336 RID: 822
		public static readonly IDictionary<string, JsonSchemaType> JsonSchemaTypeMapping = new Dictionary<string, JsonSchemaType>
		{
			{
				"string",
				JsonSchemaType.String
			},
			{
				"object",
				JsonSchemaType.Object
			},
			{
				"integer",
				JsonSchemaType.Integer
			},
			{
				"number",
				JsonSchemaType.Float
			},
			{
				"null",
				JsonSchemaType.Null
			},
			{
				"boolean",
				JsonSchemaType.Boolean
			},
			{
				"array",
				JsonSchemaType.Array
			},
			{
				"any",
				JsonSchemaType.Any
			}
		};
	}
}
