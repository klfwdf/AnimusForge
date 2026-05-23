using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using MCM.UI.Exceptions;

namespace MCM.UI.Functionality.Injectors
{
	// Token: 0x02000027 RID: 39
	public abstract class ResourceInjector
	{
		// Token: 0x06000180 RID: 384 RVA: 0x00006FF4 File Offset: 0x000051F4
		[NullableContext(1)]
		protected static XmlDocument Load(string embedPath)
		{
			XmlDocument result;
			using (Stream stream = typeof(DefaultResourceInjector).Assembly.GetManifestResourceStream(embedPath))
			{
				if (stream == null)
				{
					throw new MCMUIEmbedResourceNotFoundException("Could not find embed resource '" + embedPath + "'!");
				}
				using (XmlReader xmlReader = XmlReader.Create(stream, new XmlReaderSettings
				{
					IgnoreComments = true
				}))
				{
					XmlDocument doc = new XmlDocument();
					doc.Load(xmlReader);
					result = doc;
				}
			}
			return result;
		}

		// Token: 0x06000181 RID: 385
		public abstract void Inject();
	}
}
