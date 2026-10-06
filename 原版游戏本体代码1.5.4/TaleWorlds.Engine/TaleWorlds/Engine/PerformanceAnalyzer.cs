using System;
using System.Collections.Generic;
using System.Xml;

namespace TaleWorlds.Engine;

public class PerformanceAnalyzer
{
	private class PerformanceObject
	{
		private string name;

		private int frameCount;

		private float totalMainFps;

		private float totalRendererFps;

		private float totalFps;

		private float AverageMainFps
		{
			get
			{
				if (frameCount > 0)
				{
					return totalMainFps / (float)frameCount;
				}
				return 0f;
			}
		}

		private float AverageRendererFps
		{
			get
			{
				if (frameCount > 0)
				{
					return totalRendererFps / (float)frameCount;
				}
				return 0f;
			}
		}

		private float AverageFps
		{
			get
			{
				if (frameCount > 0)
				{
					return totalFps / (float)frameCount;
				}
				return 0f;
			}
		}

		public void AddFps(float fps, float main, float renderer)
		{
			frameCount++;
			totalFps += fps;
			totalMainFps += main;
			totalRendererFps += renderer;
		}

		public void Write(XmlNode node, XmlDocument document)
		{
			XmlAttribute xmlAttribute = document.CreateAttribute("name");
			xmlAttribute.Value = name;
			node.Attributes.Append(xmlAttribute);
			XmlAttribute xmlAttribute2 = document.CreateAttribute("frameCount");
			xmlAttribute2.Value = frameCount.ToString();
			node.Attributes.Append(xmlAttribute2);
			XmlAttribute xmlAttribute3 = document.CreateAttribute("averageFps");
			xmlAttribute3.Value = AverageFps.ToString();
			node.Attributes.Append(xmlAttribute3);
			XmlAttribute xmlAttribute4 = document.CreateAttribute("averageMainFps");
			xmlAttribute4.Value = AverageMainFps.ToString();
			node.Attributes.Append(xmlAttribute4);
			XmlAttribute xmlAttribute5 = document.CreateAttribute("averageRendererFps");
			xmlAttribute5.Value = AverageRendererFps.ToString();
			node.Attributes.Append(xmlAttribute5);
		}

		public PerformanceObject(string objectName)
		{
			name = objectName;
		}
	}

	private List<PerformanceObject> objects = new List<PerformanceObject>();

	private PerformanceObject currentObject;

	public void Start(string name)
	{
		PerformanceObject item = (currentObject = new PerformanceObject(name));
		objects.Add(item);
	}

	public void End()
	{
		currentObject = null;
	}

	public void FinalizeAndWrite(string filePath)
	{
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			XmlNode xmlNode = xmlDocument.CreateElement("objects");
			xmlDocument.AppendChild(xmlNode);
			foreach (PerformanceObject @object in objects)
			{
				XmlNode xmlNode2 = xmlDocument.CreateElement("object");
				@object.Write(xmlNode2, xmlDocument);
				xmlNode.AppendChild(xmlNode2);
			}
			xmlDocument.Save(filePath);
		}
		catch (Exception ex)
		{
			MBDebug.ShowWarning("Exception occurred while trying to write " + filePath + ": " + ex.ToString());
		}
	}

	public void Tick(float dt)
	{
		if (currentObject != null)
		{
			currentObject.AddFps(Utilities.GetFps(), Utilities.GetMainFps(), Utilities.GetRendererFps());
		}
	}
}
