using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Converters
{
	// Token: 0x020000F5 RID: 245
	[NullableContext(1)]
	internal interface IXmlDocument : IXmlNode
	{
		// Token: 0x06000CCD RID: 3277
		IXmlNode CreateComment([Nullable(2)] string text);

		// Token: 0x06000CCE RID: 3278
		IXmlNode CreateTextNode([Nullable(2)] string text);

		// Token: 0x06000CCF RID: 3279
		IXmlNode CreateCDataSection([Nullable(2)] string data);

		// Token: 0x06000CD0 RID: 3280
		IXmlNode CreateWhitespace([Nullable(2)] string text);

		// Token: 0x06000CD1 RID: 3281
		IXmlNode CreateSignificantWhitespace([Nullable(2)] string text);

		// Token: 0x06000CD2 RID: 3282
		IXmlNode CreateXmlDeclaration(string version, [Nullable(2)] string encoding, [Nullable(2)] string standalone);

		// Token: 0x06000CD3 RID: 3283
		[NullableContext(2)]
		[return: Nullable(1)]
		IXmlNode CreateXmlDocumentType([Nullable(1)] string name, string publicId, string systemId, string internalSubset);

		// Token: 0x06000CD4 RID: 3284
		IXmlNode CreateProcessingInstruction(string target, string data);

		// Token: 0x06000CD5 RID: 3285
		IXmlElement CreateElement(string elementName);

		// Token: 0x06000CD6 RID: 3286
		IXmlElement CreateElement(string qualifiedName, string namespaceUri);

		// Token: 0x06000CD7 RID: 3287
		IXmlNode CreateAttribute(string name, string value);

		// Token: 0x06000CD8 RID: 3288
		IXmlNode CreateAttribute(string qualifiedName, string namespaceUri, string value);

		// Token: 0x1700022F RID: 559
		// (get) Token: 0x06000CD9 RID: 3289
		[Nullable(2)]
		IXmlElement DocumentElement
		{
			[NullableContext(2)]
			get;
		}
	}
}
