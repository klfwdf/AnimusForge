using System;
using System.IO;
using System.Xml;
using TaleWorlds.Library;

namespace Helpers
{
	// Token: 0x02000020 RID: 32
	public static class MiscHelper
	{
		// Token: 0x0600010E RID: 270 RVA: 0x0000D4FC File Offset: 0x0000B6FC
		public static XmlDocument LoadXmlFile(string path)
		{
			Debug.Print("opening " + path, 0, Debug.DebugColor.White, 17592186044416UL);
			XmlDocument xmlDocument = new XmlDocument();
			StreamReader streamReader = new StreamReader(path);
			string xml = streamReader.ReadToEnd();
			xmlDocument.LoadXml(xml);
			streamReader.Close();
			return xmlDocument;
		}

		// Token: 0x0600010F RID: 271 RVA: 0x0000D548 File Offset: 0x0000B748
		public static string GenerateCampaignId(int length)
		{
			Random random = new Random((int)(DateTime.Now.Ticks & 65535L));
			char[] array = new char[length];
			for (int i = 0; i < length; i++)
			{
				array[i] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"[random.Next("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".Length)];
			}
			string text = new string(array);
			Debug.Print("Campaign id: " + text, 1, Debug.DebugColor.Green, 17592186044416UL);
			return text;
		}
	}
}
