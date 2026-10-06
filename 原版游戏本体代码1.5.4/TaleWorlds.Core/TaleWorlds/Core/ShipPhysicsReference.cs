using System;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public class ShipPhysicsReference : MBObjectBase
{
	public static readonly ShipPhysicsReference Default;

	public static readonly ShipPhysicsReference DefaultDebris;

	public LinearFrictionTerm LinearDragTerm { get; private set; }

	public LinearFrictionTerm LinearDampingTerm { get; private set; }

	public LinearFrictionTerm ConstantLinearDampingTerm { get; private set; }

	static ShipPhysicsReference()
	{
		Default = new ShipPhysicsReference
		{
			LinearDragTerm = new LinearFrictionTerm(0.89126307f, 0.89126307f, 0.0009766732f, 0.0033027069f, 0.08070293f, 0.80702925f),
			LinearDampingTerm = new LinearFrictionTerm(0.28781262f, 0.28781262f, 0.0026044627f, 0.008807218f, 0.21520779f, 2.152078f),
			ConstantLinearDampingTerm = new LinearFrictionTerm(0.045454543f, 0.045454543f, 0.013636364f, 3f / 110f, 0.045454543f, 0.045454543f)
		};
		DefaultDebris = new ShipPhysicsReference
		{
			LinearDragTerm = new LinearFrictionTerm(0.89126307f, 0.89126307f, 0.89126307f, 0.89126307f, 0.80702925f, 0.80702925f),
			LinearDampingTerm = new LinearFrictionTerm(0.28781262f, 0.28781262f, 0.28781262f, 0.28781262f, 2.152078f, 2.152078f),
			ConstantLinearDampingTerm = new LinearFrictionTerm(0.045454543f, 0.045454543f, 0.045454543f, 0.045454543f, 0.045454543f, 0.045454543f)
		};
		Default.PostProcessPhysicsReference();
		DefaultDebris.PostProcessPhysicsReference();
	}

	public ShipPhysicsReference()
	{
	}

	public ShipPhysicsReference(string stringId)
		: base(stringId)
	{
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		bool isAttributeValid;
		float num = DeserializeScalarAttribute<float>(node, "reference_mass", isRequiredAttribute: true, out isAttributeValid);
		LinearDragTerm = DeserializeDragTermElement(node, "linear_drag_term", out var isElementValid);
		LinearDampingTerm = DeserializeDragTermElement(node, "linear_damping_term", out var isElementValid2);
		ConstantLinearDampingTerm = DeserializeDragTermElement(node, "constant_linear_damping_term", out var isElementValid3);
		LinearDragTerm /= num;
		LinearDampingTerm /= num;
		ConstantLinearDampingTerm /= num;
		PostProcessPhysicsReference();
		AssertFieldValidity(isElementValid, "linear_drag_term");
		AssertFieldValidity(isElementValid2, "linear_damping_term");
		AssertFieldValidity(isElementValid3, "constant_linear_damping_term");
	}

	private void PostProcessPhysicsReference()
	{
		float defaultWaterDensity = GetDefaultWaterDensity();
		LinearDragTerm /= defaultWaterDensity;
		LinearDampingTerm /= defaultWaterDensity;
		ConstantLinearDampingTerm /= defaultWaterDensity;
	}

	public static float GetDefaultWaterDensity()
	{
		return 1020f;
	}

	private T DeserializeScalarAttribute<T>(XmlNode node, string attributeName, bool isRequiredAttribute, out bool isAttributeValid)
	{
		XmlAttribute xmlAttribute = node.Attributes[attributeName];
		isAttributeValid = xmlAttribute != null && !string.IsNullOrEmpty(xmlAttribute.InnerText);
		if (isAttributeValid)
		{
			return (T)Convert.ChangeType(xmlAttribute.Value, typeof(T));
		}
		AssertFieldValidity(!isRequiredAttribute, attributeName);
		return default(T);
	}

	private Vec3 DeserializeVectorElement(XmlNode node, string elementName, bool isRequiredElement, out bool isElementValid)
	{
		Vec3 invalid = Vec3.Invalid;
		XmlNode xmlNode = node.SelectSingleNode(elementName);
		isElementValid = xmlNode != null;
		if (isElementValid)
		{
			foreach (XmlAttribute attribute in xmlNode.Attributes)
			{
				string text = attribute.Name.ToLower();
				float num = float.Parse(attribute.Value);
				switch (text)
				{
				case "x":
					invalid.x = num;
					break;
				case "y":
					invalid.y = num;
					break;
				case "z":
					invalid.z = num;
					break;
				}
			}
		}
		else
		{
			AssertFieldValidity(!isRequiredElement, elementName);
		}
		return invalid;
	}

	private LinearFrictionTerm DeserializeDragTermElement(XmlNode node, string elementName, out bool isElementValid)
	{
		float right = 0f;
		float left = 0f;
		float forward = 0f;
		float backward = 0f;
		float up = 0f;
		float down = 0f;
		XmlNode xmlNode = node.SelectSingleNode(elementName);
		isElementValid = xmlNode != null;
		if (isElementValid)
		{
			foreach (XmlAttribute attribute in xmlNode.Attributes)
			{
				string text = attribute.Name.ToLower();
				string value = attribute.Value;
				switch (text)
				{
				case "right":
					right = float.Parse(value);
					break;
				case "left":
					left = float.Parse(value);
					break;
				case "forward":
					forward = float.Parse(value);
					break;
				case "backward":
					backward = float.Parse(value);
					break;
				case "up":
					up = float.Parse(value);
					break;
				case "down":
					down = float.Parse(value);
					break;
				}
			}
		}
		return new LinearFrictionTerm(right, left, forward, backward, up, down);
	}

	private void AssertFieldValidity(bool assert, string fieldName)
	{
	}
}
