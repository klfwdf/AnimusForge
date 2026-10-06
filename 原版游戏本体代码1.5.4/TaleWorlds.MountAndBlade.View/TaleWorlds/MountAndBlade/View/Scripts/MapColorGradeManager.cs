using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.View.Scripts;

public class MapColorGradeManager : ScriptComponentBehavior
{
	private class ColorGradeBlendRecord
	{
		public string color1;

		public string color2;

		public float alpha;

		public ColorGradeBlendRecord()
		{
			color1 = "";
			color2 = "";
			alpha = 0f;
		}

		public ColorGradeBlendRecord(ColorGradeBlendRecord other)
		{
			color1 = other.color1;
			color2 = other.color2;
			alpha = other.alpha;
		}
	}

	public bool ColorGradeEnabled;

	public bool AtmosphereSimulationEnabled;

	public float TimeOfDay;

	public float SeasonTimeFactor;

	private string colorGradeGridName = "worldmap_colorgrade_grid";

	private const int colorGradeGridSize = 262144;

	private byte[] colorGradeGrid = new byte[262144];

	private Dictionary<byte, string> colorGradeGridMapping = new Dictionary<byte, string>();

	private ColorGradeBlendRecord primaryTransitionRecord;

	private ColorGradeBlendRecord secondaryTransitionRecord;

	private byte lastColorGrade;

	private Vec2 terrainSize = new Vec2(1f, 1f);

	private string defaultColorGradeTextureName = "worldmap_colorgrade_stratosphere";

	private const float transitionSpeedFactor = 1f;

	private float lastSceneTimeOfDay;

	private void Init()
	{
		if (base.Scene.ContainsTerrain)
		{
			base.Scene.GetTerrainData(out var nodeDimension, out var nodeSize, out var _, out var _);
			terrainSize.x = (float)nodeDimension.X * nodeSize;
			terrainSize.y = (float)nodeDimension.Y * nodeSize;
		}
		colorGradeGridMapping.Add(1, defaultColorGradeTextureName);
		colorGradeGridMapping.Add(2, "worldmap_colorgrade_night");
		ReadColorGradesXml();
		MBMapScene.GetColorGradeGridData(base.Scene, colorGradeGrid, colorGradeGridName);
	}

	protected override void OnInit()
	{
		base.OnInit();
		Init();
	}

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		Init();
		TimeOfDay = base.Scene.TimeOfDay;
		lastSceneTimeOfDay = TimeOfDay;
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	protected override void OnTick(float dt)
	{
		TimeOfDay = base.Scene.TimeOfDay;
		SeasonTimeFactor = MBMapScene.GetSeasonTimeFactor(base.Scene);
		ApplyAtmosphere(forceLoadTextures: false);
		ApplyColorGrade(dt);
	}

	protected override void OnEditorTick(float dt)
	{
		if (base.Scene.TimeOfDay != lastSceneTimeOfDay)
		{
			TimeOfDay = base.Scene.TimeOfDay;
			lastSceneTimeOfDay = TimeOfDay;
		}
		if (base.Scene.ContainsTerrain)
		{
			base.Scene.GetTerrainData(out var nodeDimension, out var nodeSize, out var _, out var _);
			terrainSize.x = (float)nodeDimension.X * nodeSize;
			terrainSize.y = (float)nodeDimension.Y * nodeSize;
		}
		else
		{
			terrainSize.x = 1f;
			terrainSize.y = 1f;
		}
		if (AtmosphereSimulationEnabled)
		{
			TimeOfDay += dt;
			if (TimeOfDay >= 24f)
			{
				TimeOfDay -= 24f;
			}
			ApplyAtmosphere(forceLoadTextures: false);
		}
		if (ColorGradeEnabled)
		{
			ApplyColorGrade(dt);
		}
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		switch (variableName)
		{
		case "ColorGradeEnabled":
			if (!ColorGradeEnabled)
			{
				base.Scene.SetColorGradeBlend("", "", -1f);
				lastColorGrade = 0;
			}
			break;
		case "TimeOfDay":
			ApplyAtmosphere(forceLoadTextures: false);
			break;
		case "SeasonTimeFactor":
			ApplyAtmosphere(forceLoadTextures: false);
			break;
		}
	}

	private void ReadColorGradesXml()
	{
		List<string> usedPaths;
		XmlDocument mergedXmlForNative = MBObjectManager.GetMergedXmlForNative("soln_worldmap_color_grades", out usedPaths);
		if (mergedXmlForNative == null)
		{
			return;
		}
		XmlNode xmlNode = mergedXmlForNative.SelectSingleNode("worldmap_color_grades");
		if (xmlNode == null)
		{
			return;
		}
		XmlNode xmlNode2 = xmlNode.SelectSingleNode("color_grade_grid");
		if (xmlNode2 != null && xmlNode2.Attributes["name"] != null)
		{
			colorGradeGridName = xmlNode2.Attributes["name"].Value;
		}
		XmlNode xmlNode3 = xmlNode.SelectSingleNode("color_grade_default");
		if (xmlNode3 != null && xmlNode3.Attributes["name"] != null)
		{
			defaultColorGradeTextureName = xmlNode3.Attributes["name"].Value;
			colorGradeGridMapping[1] = defaultColorGradeTextureName;
		}
		XmlNode xmlNode4 = xmlNode.SelectSingleNode("color_grade_night");
		if (xmlNode4 != null && xmlNode4.Attributes["name"] != null)
		{
			colorGradeGridMapping[2] = xmlNode4.Attributes["name"].Value;
		}
		XmlNodeList xmlNodeList = xmlNode.SelectNodes("color_grade");
		if (xmlNodeList == null)
		{
			return;
		}
		foreach (XmlNode item in xmlNodeList)
		{
			if (item.Attributes["name"] != null && item.Attributes["value"] != null && byte.TryParse(item.Attributes["value"].Value, out var result))
			{
				colorGradeGridMapping[result] = item.Attributes["name"].Value;
			}
		}
	}

	public void ApplyAtmosphere(bool forceLoadTextures)
	{
		TimeOfDay = MBMath.ClampFloat(TimeOfDay, 0f, 23.99f);
		SeasonTimeFactor = MBMath.ClampFloat(SeasonTimeFactor, 0f, 1f);
		MBMapScene.SetFrameForAtmosphere(base.Scene, TimeOfDay * 10f, base.Scene.LastFinalRenderCameraFrame.origin.z, forceLoadTextures);
		float valueFrom = 0.55f;
		float valueTo = -0.1f;
		float seasonTimeFactor = SeasonTimeFactor;
		Vec3 dynamic_params = new Vec3(0f, 0.65f);
		dynamic_params.x = MBMath.Lerp(valueFrom, valueTo, seasonTimeFactor);
		MBMapScene.SetTerrainDynamicParams(base.Scene, dynamic_params);
	}

	public void ApplyColorGrade(float dt)
	{
		Vec3 origin = base.Scene.LastFinalRenderCameraFrame.origin;
		float num = 1f;
		int value = MathF.Floor(origin.x / terrainSize.X * 512f);
		int value2 = MathF.Floor(origin.y / terrainSize.Y * 512f);
		value = MBMath.ClampIndex(value, 0, 512);
		value2 = MBMath.ClampIndex(value2, 0, 512);
		byte b = colorGradeGrid[value2 * 512 + value];
		if (origin.z > 400f)
		{
			b = 1;
		}
		if (TimeOfDay > 22f || TimeOfDay < 2f)
		{
			b = 2;
		}
		if (MBMapScene.GetApplyRainColorGrade() && origin.z < 50f)
		{
			b = 160;
			num = 0.2f;
		}
		if (lastColorGrade != b)
		{
			string value3 = "";
			string value4 = "";
			if (!colorGradeGridMapping.TryGetValue(lastColorGrade, out value3))
			{
				value3 = defaultColorGradeTextureName;
			}
			if (!colorGradeGridMapping.TryGetValue(b, out value4))
			{
				value4 = defaultColorGradeTextureName;
			}
			if (primaryTransitionRecord == null)
			{
				primaryTransitionRecord = new ColorGradeBlendRecord
				{
					color1 = value3,
					color2 = value4,
					alpha = 0f
				};
			}
			else
			{
				secondaryTransitionRecord = new ColorGradeBlendRecord
				{
					color1 = primaryTransitionRecord.color2,
					color2 = value4,
					alpha = 0f
				};
			}
			lastColorGrade = b;
		}
		if (primaryTransitionRecord == null)
		{
			return;
		}
		if (primaryTransitionRecord.alpha < 1f)
		{
			primaryTransitionRecord.alpha = MathF.Min(primaryTransitionRecord.alpha + dt * (1f / num), 1f);
			base.Scene.SetColorGradeBlend(primaryTransitionRecord.color1, primaryTransitionRecord.color2, primaryTransitionRecord.alpha);
			return;
		}
		primaryTransitionRecord = null;
		if (secondaryTransitionRecord != null)
		{
			primaryTransitionRecord = new ColorGradeBlendRecord(secondaryTransitionRecord);
			secondaryTransitionRecord = null;
		}
	}
}
