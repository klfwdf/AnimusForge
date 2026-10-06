using System;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public class MissionShipObject : MBObjectBase
{
	private MBList<ShipSail> _sails = new MBList<ShipSail>();

	public string Prefab { get; private set; }

	public Vec2 DeploymentArea { get; private set; }

	public float Mass { get; private set; }

	public float FloatingForceMultiplier { get; private set; }

	public float MaximumSubmergedVolumeRatio { get; private set; }

	public Vec3 RudderStockPosition { get; private set; }

	public float MaxLateralDragShift { get; private set; }

	public float LateralDragShiftCriticalAngle { get; private set; }

	public ShipPhysicsReference PhysicsReference { get; private set; }

	public Vec3 MomentOfInertiaMultiplier { get; private set; }

	public LinearFrictionTerm LinearFrictionMultiplier { get; private set; }

	public Vec3 AngularFrictionMultiplier { get; private set; }

	public float TorqueMultiplierOfLateralBuoyantForces { get; private set; }

	public Vec3 TorqueMultiplierOfVerticalBuoyantForces { get; private set; }

	public float OarsmenForceMultiplier { get; private set; }

	public float OarsTipSpeed { get; private set; }

	public float OarFrictionMultiplier { get; private set; }

	public MBReadOnlyList<ShipSail> Sails => _sails;

	public int OarCount { get; private set; }

	public float RudderBladeLength { get; private set; }

	public float RudderBladeHeight { get; private set; }

	public float RudderDeflectionCoef { get; private set; }

	public float RudderRotationMax { get; private set; }

	public float RudderRotationRate { get; private set; }

	public float RudderForceMax { get; private set; }

	public float MaxLinearSpeed { get; private set; }

	public float MaxLinearAccel { get; private set; }

	public float MaxAngularSpeed { get; private set; }

	public float MaxAngularAccel { get; private set; }

	public float PartialHitPointsRatio { get; private set; }

	public bool HasSails => _sails.Count > 0;

	public bool HasValidRudderStockPosition => RudderStockPosition.IsValid;

	public string ShipPhysicsReferenceId { get; private set; }

	public float BowAngleLimitFromCenterline { get; private set; }

	public float LandingDepth { get; private set; }

	public MissionShipObject()
	{
	}

	public MissionShipObject(string stringId)
		: base(stringId)
	{
	}

	public void SetPhysicsReference(ShipPhysicsReference physicsReference)
	{
		PhysicsReference = physicsReference;
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		MomentOfInertiaMultiplier = new Vec3(1f, 1f, 1f);
		LinearFrictionMultiplier = LinearFrictionTerm.One;
		AngularFrictionMultiplier = Vec3.One;
		TorqueMultiplierOfVerticalBuoyantForces = new Vec3(1f, 1f, 1f);
		FloatingForceMultiplier = 1f;
		MaximumSubmergedVolumeRatio = 0.7f;
		OarsmenForceMultiplier = 1f;
		OarFrictionMultiplier = 1f;
		MaxLateralDragShift = 0f;
		LateralDragShiftCriticalAngle = 0f;
		MaximumSubmergedVolumeRatio = 0.7f;
		TorqueMultiplierOfLateralBuoyantForces = 0.5f;
		PhysicsReference = ShipPhysicsReference.Default;
		RudderStockPosition = Vec3.Invalid;
		DeserializeAux(objectManager, node);
	}

	private XmlNode GetSiblingShipNodeWithId(XmlNode node, string id)
	{
		foreach (XmlNode item in node.ParentNode.SelectNodes("MissionShip"))
		{
			XmlAttribute xmlAttribute = item.Attributes["id"];
			if (xmlAttribute != null && !string.IsNullOrEmpty(xmlAttribute.InnerText) && xmlAttribute.Value == id)
			{
				return item;
			}
		}
		return null;
	}

	private void DeserializeAux(MBObjectManager objectManager, XmlNode node)
	{
		bool flag = true;
		bool isAttributeValid;
		string text = DeserializeScalarAttribute<string>(node, "base_mission_ship", isRequiredAttribute: false, out isAttributeValid);
		if (isAttributeValid && text != base.StringId)
		{
			XmlNode siblingShipNodeWithId = GetSiblingShipNodeWithId(node, text);
			if (siblingShipNodeWithId != null)
			{
				DeserializeAux(objectManager, siblingShipNodeWithId);
				flag = false;
			}
		}
		bool isAttributeValid2;
		string text2 = DeserializeScalarAttribute<string>(node, "prefab", flag, out isAttributeValid2);
		if (isAttributeValid2)
		{
			if (!flag)
			{
				_ = text2 == Prefab;
			}
			Prefab = text2;
		}
		bool isAttributeValid3;
		string text3 = DeserializeScalarAttribute<string>(node, "ship_physics_reference_id", flag, out isAttributeValid3);
		if (isAttributeValid3)
		{
			if (!flag)
			{
				_ = text3 == ShipPhysicsReferenceId;
			}
			ShipPhysicsReferenceId = text3;
			if (objectManager != null)
			{
				ShipPhysicsReference physicsReference = objectManager.GetObject<ShipPhysicsReference>(ShipPhysicsReferenceId);
				PhysicsReference = physicsReference;
			}
		}
		bool isAttributeValid4;
		float num = DeserializeScalarAttribute<float>(node, "mass", flag, out isAttributeValid4);
		if (isAttributeValid4)
		{
			if (!flag)
			{
				num.ApproximatelyEqualsTo(Mass);
			}
			Mass = num;
		}
		bool isAttributeValid5;
		float num2 = DeserializeFloatAttribute(node, "floating_force_multiplier", isRequiredAttribute: false, out isAttributeValid5, 0.01f, 5f);
		if (isAttributeValid5)
		{
			if (!flag)
			{
				num2.ApproximatelyEqualsTo(FloatingForceMultiplier);
			}
			FloatingForceMultiplier = num2;
		}
		bool isAttributeValid6;
		float num3 = DeserializeFloatAttribute(node, "maximum_submerged_volume_ratio", isRequiredAttribute: false, out isAttributeValid6, 0.01f, 5f);
		if (isAttributeValid6)
		{
			if (!flag)
			{
				num3.ApproximatelyEqualsTo(MaximumSubmergedVolumeRatio);
			}
			MaximumSubmergedVolumeRatio = num3;
		}
		bool isAttributeValid7;
		float num4 = DeserializeFloatAttribute(node, "oars_tip_speed", flag, out isAttributeValid7);
		if (isAttributeValid7)
		{
			if (!flag)
			{
				num4.ApproximatelyEqualsTo(OarsTipSpeed);
			}
			OarsTipSpeed = num4;
		}
		bool isAttributeValid8;
		float num5 = DeserializeFloatAttribute(node, "oarsmen_force_multiplier", isRequiredAttribute: false, out isAttributeValid8);
		if (isAttributeValid8)
		{
			if (!flag)
			{
				num5.ApproximatelyEqualsTo(OarsmenForceMultiplier);
			}
			OarsmenForceMultiplier = num5;
		}
		bool isAttributeValid9;
		float num6 = DeserializeFloatAttribute(node, "oar_friction_multiplier", isRequiredAttribute: false, out isAttributeValid9);
		if (isAttributeValid9)
		{
			if (!flag)
			{
				num6.ApproximatelyEqualsTo(OarFrictionMultiplier);
			}
			OarFrictionMultiplier = num6;
		}
		bool isAttributeValid10;
		int oarCount = DeserializeScalarAttribute<int>(node, "oar_count", flag, out isAttributeValid10);
		if (isAttributeValid10)
		{
			if (!flag)
			{
				_ = OarCount;
			}
			OarCount = oarCount;
		}
		bool isAttributeValid11;
		float num7 = DeserializeScalarAttribute<float>(node, "rudder_blade_length", flag, out isAttributeValid11);
		if (isAttributeValid11)
		{
			if (!flag)
			{
				num7.ApproximatelyEqualsTo(RudderBladeLength);
			}
			RudderBladeLength = num7;
		}
		bool isAttributeValid12;
		float num8 = DeserializeScalarAttribute<float>(node, "rudder_blade_height", flag, out isAttributeValid12);
		if (isAttributeValid12)
		{
			if (!flag)
			{
				num8.ApproximatelyEqualsTo(RudderBladeHeight);
			}
			RudderBladeHeight = num8;
		}
		bool isAttributeValid13;
		float num9 = DeserializeScalarAttribute<float>(node, "rudder_deflection_coef", flag, out isAttributeValid13);
		if (isAttributeValid13)
		{
			if (!flag)
			{
				num9.ApproximatelyEqualsTo(RudderDeflectionCoef);
			}
			RudderDeflectionCoef = num9;
		}
		bool isAttributeValid14;
		float num10 = DeserializeScalarAttribute<float>(node, "rudder_rotation_max", flag, out isAttributeValid14) * (System.MathF.PI / 180f);
		if (isAttributeValid14)
		{
			if (!flag)
			{
				num10.ApproximatelyEqualsTo(RudderRotationMax);
			}
			RudderRotationMax = num10;
		}
		bool isAttributeValid15;
		float num11 = DeserializeScalarAttribute<float>(node, "rudder_rotation_rate", flag, out isAttributeValid15) * (System.MathF.PI / 180f);
		if (isAttributeValid15)
		{
			if (!flag)
			{
				num11.ApproximatelyEqualsTo(RudderRotationRate);
			}
			RudderRotationRate = num11;
		}
		bool isAttributeValid16;
		float num12 = DeserializeScalarAttribute<float>(node, "rudder_force_max", flag, out isAttributeValid16);
		if (isAttributeValid16)
		{
			if (!flag)
			{
				num12.ApproximatelyEqualsTo(RudderForceMax);
			}
			RudderForceMax = num12;
		}
		bool isAttributeValid17;
		float num13 = DeserializeFloatAttribute(node, "max_linear_speed", flag, out isAttributeValid17, 1f, 100f, "m/s");
		if (isAttributeValid17)
		{
			if (!flag)
			{
				num13.ApproximatelyEqualsTo(MaxLinearSpeed);
			}
			MaxLinearSpeed = num13;
		}
		bool isAttributeValid18;
		float num14 = DeserializeFloatAttribute(node, "max_linear_acceleration", flag, out isAttributeValid18, 0.1f, 50f, "m/s^2");
		if (isAttributeValid18)
		{
			if (!flag)
			{
				num14.ApproximatelyEqualsTo(MaxLinearAccel);
			}
			MaxLinearAccel = num14;
		}
		bool isAttributeValid19;
		float num15 = DeserializeFloatAttribute(node, "max_angular_speed", flag, out isAttributeValid19, 1f, 180f, "deg") * (System.MathF.PI / 180f);
		if (isAttributeValid19)
		{
			if (!flag)
			{
				num15.ApproximatelyEqualsTo(MaxAngularSpeed);
			}
			MaxAngularSpeed = num15;
		}
		bool isAttributeValid20;
		float num16 = DeserializeFloatAttribute(node, "max_angular_acceleration", flag, out isAttributeValid20, 1f, 180f, "deg/s") * (System.MathF.PI / 180f);
		if (isAttributeValid20)
		{
			if (!flag)
			{
				num16.ApproximatelyEqualsTo(MaxAngularAccel);
			}
			MaxAngularAccel = num16;
		}
		bool isAttributeValid21;
		float num17 = DeserializeFloatAttribute(node, "bow_angle_limit_from_centerline", flag, out isAttributeValid21, 0f, 90f);
		if (isAttributeValid21)
		{
			if (!flag)
			{
				num17.ApproximatelyEqualsTo(BowAngleLimitFromCenterline);
			}
			BowAngleLimitFromCenterline = num17;
		}
		bool isAttributeValid22;
		float num18 = DeserializeFloatAttribute(node, "landing_depth", flag, out isAttributeValid22, 0f, 90f);
		if (isAttributeValid22)
		{
			if (!flag)
			{
				num18.ApproximatelyEqualsTo(LandingDepth);
			}
			LandingDepth = num18;
		}
		bool isElementValid;
		Vec2 deploymentArea = Deserialize2DDimensionElement(node, "deployment_area", flag, out isElementValid);
		if (isElementValid)
		{
			if (!flag)
			{
				deploymentArea.NearlyEquals(DeploymentArea);
			}
			DeploymentArea = deploymentArea;
		}
		bool isElementValid2;
		MBList<ShipSail> mBList = DeserializeSailsElement(node, flag, out isElementValid2);
		if (isElementValid2)
		{
			if (!flag && mBList.Count == _sails.Count)
			{
				bool flag2 = true;
				for (int i = 0; i < _sails.Count; i++)
				{
					if (mBList[i].NearlyEquals(_sails[i]))
					{
						flag2 = false;
					}
				}
			}
			_sails = mBList;
		}
		bool isElementValid3;
		Vec3 momentOfInertiaMultiplier = DeserializeVectorElement(node, "moment_of_inertia_multiplier", isRequiredElement: false, out isElementValid3);
		if (isElementValid3)
		{
			if (!flag)
			{
				momentOfInertiaMultiplier.NearlyEquals(MomentOfInertiaMultiplier);
			}
			MomentOfInertiaMultiplier = momentOfInertiaMultiplier;
		}
		bool isElementValid4;
		LinearFrictionTerm linearFrictionMultiplier = DeserializeLinearDragTermElement(node, "linear_friction_multiplier", out isElementValid4);
		if (isElementValid4)
		{
			if (!flag)
			{
				linearFrictionMultiplier.NearlyEquals(LinearFrictionMultiplier);
			}
			LinearFrictionMultiplier = linearFrictionMultiplier;
		}
		bool isElementValid5;
		Vec3 angularFrictionMultiplier = DeserializeAngularDragTermElement(node, "angular_friction_multiplier", out isElementValid5);
		if (isElementValid5)
		{
			if (!flag)
			{
				angularFrictionMultiplier.NearlyEquals(AngularFrictionMultiplier);
			}
			AngularFrictionMultiplier = angularFrictionMultiplier;
		}
		bool isAttributeValid23;
		float num19 = DeserializeFloatAttribute(node, "lateral_drag_shift_max", isRequiredAttribute: false, out isAttributeValid23, 0f, 100f, "m");
		if (isAttributeValid23)
		{
			if (!flag)
			{
				num19.ApproximatelyEqualsTo(MaxLateralDragShift);
			}
			MaxLateralDragShift = num19;
		}
		bool isAttributeValid24;
		float num20 = DeserializeFloatAttribute(node, "lateral_drag_shift_critical_angle", isRequiredAttribute: false, out isAttributeValid24, 0f, 90f, "deg") * (System.MathF.PI / 180f);
		if (isAttributeValid24)
		{
			if (!flag)
			{
				num20.ApproximatelyEqualsTo(LateralDragShiftCriticalAngle);
			}
			LateralDragShiftCriticalAngle = num20;
		}
		bool isElementValid6;
		Vec3 rudderStockPosition = DeserializeVectorElement(node, "rudder_stock_position", isRequiredElement: false, out isElementValid6);
		if (isElementValid6)
		{
			if (!flag)
			{
				rudderStockPosition.NearlyEquals(RudderStockPosition);
			}
			RudderStockPosition = rudderStockPosition;
		}
		bool isAttributeValid25;
		float num21 = DeserializeFloatAttribute(node, "maximum_submerged_volume_ratio", isRequiredAttribute: false, out isAttributeValid25);
		if (isAttributeValid25)
		{
			if (!flag)
			{
				num21.ApproximatelyEqualsTo(MaximumSubmergedVolumeRatio);
			}
			MaximumSubmergedVolumeRatio = num21;
		}
		bool isAttributeValid26;
		float num22 = DeserializeFloatAttribute(node, "partial_hit_points_ratio", flag, out isAttributeValid26);
		if (isAttributeValid26)
		{
			if (!flag)
			{
				num22.ApproximatelyEqualsTo(PartialHitPointsRatio);
			}
			PartialHitPointsRatio = num22;
		}
		bool isAttributeValid27;
		float num23 = DeserializeFloatAttribute(node, "torque_multiplier_of_lateral_buoyant_forces", isRequiredAttribute: false, out isAttributeValid27);
		if (isAttributeValid27)
		{
			if (!flag)
			{
				num23.ApproximatelyEqualsTo(TorqueMultiplierOfLateralBuoyantForces);
			}
			TorqueMultiplierOfLateralBuoyantForces = num23;
		}
		bool isElementValid7;
		Vec3 torqueMultiplierOfVerticalBuoyantForces = DeserializeVectorElement(node, "torque_multiplier_of_vertical_buoyant_forces", isRequiredElement: false, out isElementValid7);
		if (isElementValid7)
		{
			if (!flag)
			{
				torqueMultiplierOfVerticalBuoyantForces.NearlyEquals(TorqueMultiplierOfVerticalBuoyantForces);
			}
			TorqueMultiplierOfVerticalBuoyantForces = torqueMultiplierOfVerticalBuoyantForces;
		}
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

	private float DeserializeFloatAttribute(XmlNode node, string attributeName, bool isRequiredAttribute, out bool isAttributeValid, float minValue = float.MinValue, float maxValue = float.MaxValue, string unitString = "")
	{
		float num = DeserializeScalarAttribute<float>(node, attributeName, isRequiredAttribute, out isAttributeValid);
		if (isAttributeValid)
		{
			if (num < minValue)
			{
				Debug.FailedAssert("ShipObject(" + base.StringId + "): " + attributeName + " field is less than the required minimum value of " + minValue + " " + unitString + ".", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "DeserializeFloatAttribute", 692);
				num = minValue;
			}
			if (num > maxValue)
			{
				Debug.FailedAssert("ShipObject(" + base.StringId + "): " + attributeName + " field is greater than the required maximum value of " + maxValue + " " + unitString + ".", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "DeserializeFloatAttribute", 698);
				num = maxValue;
			}
		}
		return num;
	}

	private Vec2 Deserialize2DDimensionElement(XmlNode node, string elementName, bool isRequiredElement, out bool isElementValid)
	{
		Vec2 zero = Vec2.Zero;
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
				case "width":
					zero.x = num;
					break;
				case "length":
				case "height":
					zero.y = num;
					break;
				}
			}
		}
		else
		{
			AssertFieldValidity(!isRequiredElement, elementName);
		}
		return zero;
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

	private LinearFrictionTerm DeserializeLinearDragTermElement(XmlNode node, string elementName, out bool isElementValid)
	{
		float num = 0f;
		float left = 0f;
		float forward = 0f;
		float backward = 0f;
		float up = 0f;
		float down = 0f;
		XmlNode xmlNode = node.SelectSingleNode(elementName);
		isElementValid = xmlNode != null;
		if (isElementValid)
		{
			XmlAttributeCollection attributes = xmlNode.Attributes;
			if (attributes.Count != 5)
			{
				Debug.FailedAssert("ShipObject(" + base.StringId + "): " + elementName + " element must have exactly " + 5 + " attributes for directional drag multipliers", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "DeserializeLinearDragTermElement", 798);
				isElementValid = false;
			}
			else
			{
				foreach (XmlAttribute item in attributes)
				{
					string text = item.Name.ToLower();
					string value = item.Value;
					switch (text)
					{
					case "side":
						num = float.Parse(value);
						left = num;
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
		}
		LinearFrictionTerm result = new LinearFrictionTerm(num, left, forward, backward, up, down);
		isElementValid = isElementValid && result.IsValid;
		return result;
	}

	private Vec3 DeserializeAngularDragTermElement(XmlNode node, string elementName, out bool isElementValid)
	{
		Vec3 one = Vec3.One;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		XmlNode xmlNode = node.SelectSingleNode(elementName);
		isElementValid = xmlNode != null;
		if (isElementValid)
		{
			XmlAttributeCollection attributes = xmlNode.Attributes;
			if (attributes.Count != 3)
			{
				Debug.FailedAssert("ShipObject(" + base.StringId + "): " + elementName + " element must have exactly " + 3 + " attributes for angular drag friction multipliers", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "DeserializeAngularDragTermElement", 864);
				isElementValid = false;
			}
			else
			{
				foreach (XmlAttribute item in attributes)
				{
					string text = item.Name.ToLower();
					string value = item.Value;
					switch (text)
					{
					case "pitch":
						one.x = float.Parse(value);
						flag = true;
						break;
					case "roll":
						one.y = float.Parse(value);
						flag2 = true;
						break;
					case "yaw":
						one.z = float.Parse(value);
						flag3 = true;
						break;
					}
				}
			}
		}
		isElementValid = isElementValid && flag && flag2 && flag3;
		return one;
	}

	private MBList<ShipSail> DeserializeSailsElement(XmlNode node, bool hasAttributeRequirement, out bool isElementValid)
	{
		MBList<ShipSail> mBList = new MBList<ShipSail>();
		XmlNode xmlNode = node.SelectSingleNode("sails");
		isElementValid = xmlNode != null && xmlNode.ChildNodes.Count > 0;
		if (isElementValid)
		{
			for (int i = 0; i < xmlNode.ChildNodes.Count; i++)
			{
				XmlNode node2 = xmlNode.ChildNodes[i];
				bool isAttributeValid;
				SailType type = DeserializeSailTypeAttribute(node2, "type", isRequiredAttribute: true, out isAttributeValid);
				float forceMultiplier = DeserializeFloatAttribute(node2, "force_multiplier", isRequiredAttribute: true, out isAttributeValid);
				float leftRotationLimit = DeserializeFloatAttribute(node2, "left_rotation_limit", isRequiredAttribute: true, out isAttributeValid, 0f, float.MaxValue, "deg") * (System.MathF.PI / 180f);
				float rightRotationLimit = DeserializeFloatAttribute(node2, "right_rotation_limit", isRequiredAttribute: true, out isAttributeValid, 0f, float.MaxValue, "deg") * (System.MathF.PI / 180f);
				float rotationRate = DeserializeFloatAttribute(node2, "rotation_rate", isRequiredAttribute: true, out isAttributeValid, 0f, float.MaxValue, "deg/s") * (System.MathF.PI / 180f);
				ShipSail item = new ShipSail(type, forceMultiplier, leftRotationLimit, rightRotationLimit, rotationRate);
				mBList.Add(item);
			}
		}
		return mBList;
	}

	private SailType DeserializeSailTypeAttribute(XmlNode node, string attributeName, bool isRequiredAttribute, out bool isAttributeValid)
	{
		XmlAttribute xmlAttribute = node.Attributes[attributeName];
		SailType result = SailType.Square;
		isAttributeValid = xmlAttribute != null && !string.IsNullOrEmpty(xmlAttribute.InnerText) && Enum.TryParse<SailType>(xmlAttribute.Value, ignoreCase: true, out result);
		if (!isAttributeValid)
		{
			AssertFieldValidity(!isRequiredAttribute, attributeName);
		}
		return result;
	}

	private void AssertFieldValidity(bool assert, string fieldName)
	{
	}
}
