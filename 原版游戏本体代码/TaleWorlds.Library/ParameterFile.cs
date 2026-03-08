using System;
using System.IO;
using System.Xml;

namespace TaleWorlds.Library
{
	// Token: 0x02000077 RID: 119
	public class ParameterFile
	{
		// Token: 0x1700006C RID: 108
		// (get) Token: 0x06000443 RID: 1091 RVA: 0x0000F0D8 File Offset: 0x0000D2D8
		// (set) Token: 0x06000444 RID: 1092 RVA: 0x0000F0E0 File Offset: 0x0000D2E0
		public string Path { get; private set; }

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x06000445 RID: 1093 RVA: 0x0000F0E9 File Offset: 0x0000D2E9
		// (set) Token: 0x06000446 RID: 1094 RVA: 0x0000F0F1 File Offset: 0x0000D2F1
		public DateTime LastCheckedTime { get; private set; }

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x06000447 RID: 1095 RVA: 0x0000F0FA File Offset: 0x0000D2FA
		// (set) Token: 0x06000448 RID: 1096 RVA: 0x0000F102 File Offset: 0x0000D302
		public ParameterContainer ParameterContainer { get; private set; }

		// Token: 0x06000449 RID: 1097 RVA: 0x0000F10B File Offset: 0x0000D30B
		public ParameterFile(string path)
		{
			this.ParameterContainer = new ParameterContainer();
			this.Path = path;
			this.LastCheckedTime = DateTime.MinValue;
		}

		// Token: 0x0600044A RID: 1098 RVA: 0x0000F130 File Offset: 0x0000D330
		public bool CheckIfNeedsToBeRefreshed()
		{
			return File.GetLastWriteTime(this.Path) > this.LastCheckedTime;
		}

		// Token: 0x0600044B RID: 1099 RVA: 0x0000F148 File Offset: 0x0000D348
		public void Refresh()
		{
			this.ParameterContainer.ClearParameters();
			DateTime lastWriteTime = File.GetLastWriteTime(this.Path);
			XmlDocument xmlDocument = new XmlDocument();
			try
			{
				xmlDocument.Load(this.Path);
			}
			catch
			{
				this._failedAttemptsCount++;
				if (this._failedAttemptsCount >= 100)
				{
					Debug.FailedAssert("Could not load parameters file", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\ParameterFile.cs", "Refresh", 47);
				}
				return;
			}
			this._failedAttemptsCount = 0;
			foreach (object obj in xmlDocument.FirstChild.ChildNodes)
			{
				XmlElement xmlElement = (XmlElement)obj;
				string attribute = xmlElement.GetAttribute("name");
				string attribute2 = xmlElement.GetAttribute("value");
				this.ParameterContainer.AddParameter(attribute, attribute2, true);
			}
			this.LastCheckedTime = lastWriteTime;
		}

		// Token: 0x04000151 RID: 337
		private int _failedAttemptsCount;

		// Token: 0x04000152 RID: 338
		private const int MaxFailedAttemptsCount = 100;
	}
}
