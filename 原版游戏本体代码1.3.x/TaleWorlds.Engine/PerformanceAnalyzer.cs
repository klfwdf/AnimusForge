using System;
using System.Collections.Generic;
using System.Xml;

namespace TaleWorlds.Engine
{
	// Token: 0x02000075 RID: 117
	public class PerformanceAnalyzer
	{
		// Token: 0x06000A89 RID: 2697 RVA: 0x0000ABDC File Offset: 0x00008DDC
		public void Start(string name)
		{
			PerformanceAnalyzer.PerformanceObject item = new PerformanceAnalyzer.PerformanceObject(name);
			this.currentObject = item;
			this.objects.Add(item);
		}

		// Token: 0x06000A8A RID: 2698 RVA: 0x0000AC03 File Offset: 0x00008E03
		public void End()
		{
			this.currentObject = null;
		}

		// Token: 0x06000A8B RID: 2699 RVA: 0x0000AC0C File Offset: 0x00008E0C
		public void FinalizeAndWrite(string filePath)
		{
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				XmlNode xmlNode = xmlDocument.CreateElement("objects");
				xmlDocument.AppendChild(xmlNode);
				foreach (PerformanceAnalyzer.PerformanceObject performanceObject in this.objects)
				{
					XmlNode xmlNode2 = xmlDocument.CreateElement("object");
					performanceObject.Write(xmlNode2, xmlDocument);
					xmlNode.AppendChild(xmlNode2);
				}
				xmlDocument.Save(filePath);
			}
			catch (Exception ex)
			{
				MBDebug.ShowWarning("Exception occurred while trying to write " + filePath + ": " + ex.ToString());
			}
		}

		// Token: 0x06000A8C RID: 2700 RVA: 0x0000ACC4 File Offset: 0x00008EC4
		public void Tick(float dt)
		{
			if (this.currentObject != null)
			{
				this.currentObject.AddFps(Utilities.GetFps(), Utilities.GetMainFps(), Utilities.GetRendererFps());
			}
		}

		// Token: 0x04000161 RID: 353
		private List<PerformanceAnalyzer.PerformanceObject> objects = new List<PerformanceAnalyzer.PerformanceObject>();

		// Token: 0x04000162 RID: 354
		private PerformanceAnalyzer.PerformanceObject currentObject;

		// Token: 0x020000CD RID: 205
		private class PerformanceObject
		{
			// Token: 0x170000D3 RID: 211
			// (get) Token: 0x06000FEC RID: 4076 RVA: 0x000146AE File Offset: 0x000128AE
			private float AverageMainFps
			{
				get
				{
					if (this.frameCount > 0)
					{
						return this.totalMainFps / (float)this.frameCount;
					}
					return 0f;
				}
			}

			// Token: 0x170000D4 RID: 212
			// (get) Token: 0x06000FED RID: 4077 RVA: 0x000146CD File Offset: 0x000128CD
			private float AverageRendererFps
			{
				get
				{
					if (this.frameCount > 0)
					{
						return this.totalRendererFps / (float)this.frameCount;
					}
					return 0f;
				}
			}

			// Token: 0x170000D5 RID: 213
			// (get) Token: 0x06000FEE RID: 4078 RVA: 0x000146EC File Offset: 0x000128EC
			private float AverageFps
			{
				get
				{
					if (this.frameCount > 0)
					{
						return this.totalFps / (float)this.frameCount;
					}
					return 0f;
				}
			}

			// Token: 0x06000FEF RID: 4079 RVA: 0x0001470B File Offset: 0x0001290B
			public void AddFps(float fps, float main, float renderer)
			{
				this.frameCount++;
				this.totalFps += fps;
				this.totalMainFps += main;
				this.totalRendererFps += renderer;
			}

			// Token: 0x06000FF0 RID: 4080 RVA: 0x00014748 File Offset: 0x00012948
			public void Write(XmlNode node, XmlDocument document)
			{
				XmlAttribute xmlAttribute = document.CreateAttribute("name");
				xmlAttribute.Value = this.name;
				node.Attributes.Append(xmlAttribute);
				XmlAttribute xmlAttribute2 = document.CreateAttribute("frameCount");
				xmlAttribute2.Value = this.frameCount.ToString();
				node.Attributes.Append(xmlAttribute2);
				XmlAttribute xmlAttribute3 = document.CreateAttribute("averageFps");
				xmlAttribute3.Value = this.AverageFps.ToString();
				node.Attributes.Append(xmlAttribute3);
				XmlAttribute xmlAttribute4 = document.CreateAttribute("averageMainFps");
				xmlAttribute4.Value = this.AverageMainFps.ToString();
				node.Attributes.Append(xmlAttribute4);
				XmlAttribute xmlAttribute5 = document.CreateAttribute("averageRendererFps");
				xmlAttribute5.Value = this.AverageRendererFps.ToString();
				node.Attributes.Append(xmlAttribute5);
			}

			// Token: 0x06000FF1 RID: 4081 RVA: 0x00014831 File Offset: 0x00012A31
			public PerformanceObject(string objectName)
			{
				this.name = objectName;
			}

			// Token: 0x04000433 RID: 1075
			private string name;

			// Token: 0x04000434 RID: 1076
			private int frameCount;

			// Token: 0x04000435 RID: 1077
			private float totalMainFps;

			// Token: 0x04000436 RID: 1078
			private float totalRendererFps;

			// Token: 0x04000437 RID: 1079
			private float totalFps;
		}
	}
}
