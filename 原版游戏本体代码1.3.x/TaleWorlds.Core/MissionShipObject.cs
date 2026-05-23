using System;
using System.Collections;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x020000B9 RID: 185
	public class MissionShipObject : MBObjectBase
	{
		// Token: 0x1700032D RID: 813
		// (get) Token: 0x06000990 RID: 2448 RVA: 0x0001F582 File Offset: 0x0001D782
		// (set) Token: 0x06000991 RID: 2449 RVA: 0x0001F58A File Offset: 0x0001D78A
		public string Prefab { get; private set; }

		// Token: 0x1700032E RID: 814
		// (get) Token: 0x06000992 RID: 2450 RVA: 0x0001F593 File Offset: 0x0001D793
		// (set) Token: 0x06000993 RID: 2451 RVA: 0x0001F59B File Offset: 0x0001D79B
		public Vec2 DeploymentArea { get; private set; }

		// Token: 0x1700032F RID: 815
		// (get) Token: 0x06000994 RID: 2452 RVA: 0x0001F5A4 File Offset: 0x0001D7A4
		// (set) Token: 0x06000995 RID: 2453 RVA: 0x0001F5AC File Offset: 0x0001D7AC
		public float Mass { get; private set; }

		// Token: 0x17000330 RID: 816
		// (get) Token: 0x06000996 RID: 2454 RVA: 0x0001F5B5 File Offset: 0x0001D7B5
		// (set) Token: 0x06000997 RID: 2455 RVA: 0x0001F5BD File Offset: 0x0001D7BD
		public float FloatingForceMultiplier { get; private set; }

		// Token: 0x17000331 RID: 817
		// (get) Token: 0x06000998 RID: 2456 RVA: 0x0001F5C6 File Offset: 0x0001D7C6
		// (set) Token: 0x06000999 RID: 2457 RVA: 0x0001F5CE File Offset: 0x0001D7CE
		public float MaximumSubmergedVolumeRatio { get; private set; }

		// Token: 0x17000332 RID: 818
		// (get) Token: 0x0600099A RID: 2458 RVA: 0x0001F5D7 File Offset: 0x0001D7D7
		// (set) Token: 0x0600099B RID: 2459 RVA: 0x0001F5DF File Offset: 0x0001D7DF
		public Vec3 RudderStockPosition { get; private set; }

		// Token: 0x17000333 RID: 819
		// (get) Token: 0x0600099C RID: 2460 RVA: 0x0001F5E8 File Offset: 0x0001D7E8
		// (set) Token: 0x0600099D RID: 2461 RVA: 0x0001F5F0 File Offset: 0x0001D7F0
		public float MaxLateralDragShift { get; private set; }

		// Token: 0x17000334 RID: 820
		// (get) Token: 0x0600099E RID: 2462 RVA: 0x0001F5F9 File Offset: 0x0001D7F9
		// (set) Token: 0x0600099F RID: 2463 RVA: 0x0001F601 File Offset: 0x0001D801
		public float LateralDragShiftCriticalAngle { get; private set; }

		// Token: 0x17000335 RID: 821
		// (get) Token: 0x060009A0 RID: 2464 RVA: 0x0001F60A File Offset: 0x0001D80A
		// (set) Token: 0x060009A1 RID: 2465 RVA: 0x0001F612 File Offset: 0x0001D812
		public ShipPhysicsReference PhysicsReference { get; private set; }

		// Token: 0x17000336 RID: 822
		// (get) Token: 0x060009A2 RID: 2466 RVA: 0x0001F61B File Offset: 0x0001D81B
		// (set) Token: 0x060009A3 RID: 2467 RVA: 0x0001F623 File Offset: 0x0001D823
		public Vec3 MomentOfInertiaMultiplier { get; private set; }

		// Token: 0x17000337 RID: 823
		// (get) Token: 0x060009A4 RID: 2468 RVA: 0x0001F62C File Offset: 0x0001D82C
		// (set) Token: 0x060009A5 RID: 2469 RVA: 0x0001F634 File Offset: 0x0001D834
		public LinearFrictionTerm LinearFrictionMultiplier { get; private set; }

		// Token: 0x17000338 RID: 824
		// (get) Token: 0x060009A6 RID: 2470 RVA: 0x0001F63D File Offset: 0x0001D83D
		// (set) Token: 0x060009A7 RID: 2471 RVA: 0x0001F645 File Offset: 0x0001D845
		public Vec3 AngularFrictionMultiplier { get; private set; }

		// Token: 0x17000339 RID: 825
		// (get) Token: 0x060009A8 RID: 2472 RVA: 0x0001F64E File Offset: 0x0001D84E
		// (set) Token: 0x060009A9 RID: 2473 RVA: 0x0001F656 File Offset: 0x0001D856
		public float TorqueMultiplierOfLateralBuoyantForces { get; private set; }

		// Token: 0x1700033A RID: 826
		// (get) Token: 0x060009AA RID: 2474 RVA: 0x0001F65F File Offset: 0x0001D85F
		// (set) Token: 0x060009AB RID: 2475 RVA: 0x0001F667 File Offset: 0x0001D867
		public Vec3 TorqueMultiplierOfVerticalBuoyantForces { get; private set; }

		// Token: 0x1700033B RID: 827
		// (get) Token: 0x060009AC RID: 2476 RVA: 0x0001F670 File Offset: 0x0001D870
		// (set) Token: 0x060009AD RID: 2477 RVA: 0x0001F678 File Offset: 0x0001D878
		public float OarsmenForceMultiplier { get; private set; }

		// Token: 0x1700033C RID: 828
		// (get) Token: 0x060009AE RID: 2478 RVA: 0x0001F681 File Offset: 0x0001D881
		// (set) Token: 0x060009AF RID: 2479 RVA: 0x0001F689 File Offset: 0x0001D889
		public float OarsTipSpeed { get; private set; }

		// Token: 0x1700033D RID: 829
		// (get) Token: 0x060009B0 RID: 2480 RVA: 0x0001F692 File Offset: 0x0001D892
		// (set) Token: 0x060009B1 RID: 2481 RVA: 0x0001F69A File Offset: 0x0001D89A
		public float OarFrictionMultiplier { get; private set; }

		// Token: 0x1700033E RID: 830
		// (get) Token: 0x060009B2 RID: 2482 RVA: 0x0001F6A3 File Offset: 0x0001D8A3
		public MBReadOnlyList<ShipSail> Sails
		{
			get
			{
				return this._sails;
			}
		}

		// Token: 0x1700033F RID: 831
		// (get) Token: 0x060009B3 RID: 2483 RVA: 0x0001F6AB File Offset: 0x0001D8AB
		// (set) Token: 0x060009B4 RID: 2484 RVA: 0x0001F6B3 File Offset: 0x0001D8B3
		public int OarCount { get; private set; }

		// Token: 0x17000340 RID: 832
		// (get) Token: 0x060009B5 RID: 2485 RVA: 0x0001F6BC File Offset: 0x0001D8BC
		// (set) Token: 0x060009B6 RID: 2486 RVA: 0x0001F6C4 File Offset: 0x0001D8C4
		public float RudderBladeLength { get; private set; }

		// Token: 0x17000341 RID: 833
		// (get) Token: 0x060009B7 RID: 2487 RVA: 0x0001F6CD File Offset: 0x0001D8CD
		// (set) Token: 0x060009B8 RID: 2488 RVA: 0x0001F6D5 File Offset: 0x0001D8D5
		public float RudderBladeHeight { get; private set; }

		// Token: 0x17000342 RID: 834
		// (get) Token: 0x060009B9 RID: 2489 RVA: 0x0001F6DE File Offset: 0x0001D8DE
		// (set) Token: 0x060009BA RID: 2490 RVA: 0x0001F6E6 File Offset: 0x0001D8E6
		public float RudderDeflectionCoef { get; private set; }

		// Token: 0x17000343 RID: 835
		// (get) Token: 0x060009BB RID: 2491 RVA: 0x0001F6EF File Offset: 0x0001D8EF
		// (set) Token: 0x060009BC RID: 2492 RVA: 0x0001F6F7 File Offset: 0x0001D8F7
		public float RudderRotationMax { get; private set; }

		// Token: 0x17000344 RID: 836
		// (get) Token: 0x060009BD RID: 2493 RVA: 0x0001F700 File Offset: 0x0001D900
		// (set) Token: 0x060009BE RID: 2494 RVA: 0x0001F708 File Offset: 0x0001D908
		public float RudderRotationRate { get; private set; }

		// Token: 0x17000345 RID: 837
		// (get) Token: 0x060009BF RID: 2495 RVA: 0x0001F711 File Offset: 0x0001D911
		// (set) Token: 0x060009C0 RID: 2496 RVA: 0x0001F719 File Offset: 0x0001D919
		public float RudderForceMax { get; private set; }

		// Token: 0x17000346 RID: 838
		// (get) Token: 0x060009C1 RID: 2497 RVA: 0x0001F722 File Offset: 0x0001D922
		// (set) Token: 0x060009C2 RID: 2498 RVA: 0x0001F72A File Offset: 0x0001D92A
		public float MaxLinearSpeed { get; private set; }

		// Token: 0x17000347 RID: 839
		// (get) Token: 0x060009C3 RID: 2499 RVA: 0x0001F733 File Offset: 0x0001D933
		// (set) Token: 0x060009C4 RID: 2500 RVA: 0x0001F73B File Offset: 0x0001D93B
		public float MaxLinearAccel { get; private set; }

		// Token: 0x17000348 RID: 840
		// (get) Token: 0x060009C5 RID: 2501 RVA: 0x0001F744 File Offset: 0x0001D944
		// (set) Token: 0x060009C6 RID: 2502 RVA: 0x0001F74C File Offset: 0x0001D94C
		public float MaxAngularSpeed { get; private set; }

		// Token: 0x17000349 RID: 841
		// (get) Token: 0x060009C7 RID: 2503 RVA: 0x0001F755 File Offset: 0x0001D955
		// (set) Token: 0x060009C8 RID: 2504 RVA: 0x0001F75D File Offset: 0x0001D95D
		public float MaxAngularAccel { get; private set; }

		// Token: 0x1700034A RID: 842
		// (get) Token: 0x060009C9 RID: 2505 RVA: 0x0001F766 File Offset: 0x0001D966
		// (set) Token: 0x060009CA RID: 2506 RVA: 0x0001F76E File Offset: 0x0001D96E
		public float PartialHitPointsRatio { get; private set; }

		// Token: 0x1700034B RID: 843
		// (get) Token: 0x060009CB RID: 2507 RVA: 0x0001F777 File Offset: 0x0001D977
		public bool HasSails
		{
			get
			{
				return this._sails.Count > 0;
			}
		}

		// Token: 0x1700034C RID: 844
		// (get) Token: 0x060009CC RID: 2508 RVA: 0x0001F788 File Offset: 0x0001D988
		public bool HasValidRudderStockPosition
		{
			get
			{
				return this.RudderStockPosition.IsValid;
			}
		}

		// Token: 0x1700034D RID: 845
		// (get) Token: 0x060009CD RID: 2509 RVA: 0x0001F7A3 File Offset: 0x0001D9A3
		// (set) Token: 0x060009CE RID: 2510 RVA: 0x0001F7AB File Offset: 0x0001D9AB
		public string ShipPhysicsReferenceId { get; private set; }

		// Token: 0x1700034E RID: 846
		// (get) Token: 0x060009CF RID: 2511 RVA: 0x0001F7B4 File Offset: 0x0001D9B4
		// (set) Token: 0x060009D0 RID: 2512 RVA: 0x0001F7BC File Offset: 0x0001D9BC
		public float BowAngleLimitFromCenterline { get; private set; }

		// Token: 0x060009D1 RID: 2513 RVA: 0x0001F7C5 File Offset: 0x0001D9C5
		public MissionShipObject()
		{
		}

		// Token: 0x060009D2 RID: 2514 RVA: 0x0001F7D8 File Offset: 0x0001D9D8
		public MissionShipObject(string stringId)
			: base(stringId)
		{
		}

		// Token: 0x060009D3 RID: 2515 RVA: 0x0001F7EC File Offset: 0x0001D9EC
		public void SetPhysicsReference(ShipPhysicsReference physicsReference)
		{
			this.PhysicsReference = physicsReference;
		}

		// Token: 0x060009D4 RID: 2516 RVA: 0x0001F7F8 File Offset: 0x0001D9F8
		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			bool flag;
			this.Prefab = this.DeserializeScalarAttribute<string>(node, "prefab", true, out flag);
			bool flag2;
			this.ShipPhysicsReferenceId = this.DeserializeScalarAttribute<string>(node, "ship_physics_reference_id", true, out flag2);
			this.Mass = this.DeserializeScalarAttribute<float>(node, "mass", true, out flag);
			this.FloatingForceMultiplier = this.DeserializeFloatAttribute(node, "floating_force_multiplier", false, out flag, 0.01f, 5f, 1f, "");
			this.MaximumSubmergedVolumeRatio = this.DeserializeFloatAttribute(node, "maximum_submerged_volume_ratio", false, out flag, 0.01f, 5f, 0.7f, "");
			this.OarsTipSpeed = this.DeserializeFloatAttribute(node, "oars_tip_speed", true, out flag, float.MinValue, float.MaxValue, 1f, "");
			this.OarsmenForceMultiplier = this.DeserializeFloatAttribute(node, "oarsmen_force_multiplier", false, out flag, float.MinValue, float.MaxValue, 1f, "");
			this.OarFrictionMultiplier = this.DeserializeFloatAttribute(node, "oar_friction_multiplier", false, out flag, float.MinValue, float.MaxValue, 1f, "");
			this.OarCount = this.DeserializeScalarAttribute<int>(node, "oar_count", true, out flag);
			this.RudderBladeLength = this.DeserializeScalarAttribute<float>(node, "rudder_blade_length", true, out flag);
			this.RudderBladeHeight = this.DeserializeScalarAttribute<float>(node, "rudder_blade_height", true, out flag);
			this.RudderDeflectionCoef = this.DeserializeScalarAttribute<float>(node, "rudder_deflection_coef", true, out flag);
			this.RudderRotationMax = this.DeserializeScalarAttribute<float>(node, "rudder_rotation_max", true, out flag) * 0.017453292f;
			this.RudderRotationRate = this.DeserializeScalarAttribute<float>(node, "rudder_rotation_rate", true, out flag) * 0.017453292f;
			this.RudderForceMax = this.DeserializeScalarAttribute<float>(node, "rudder_force_max", true, out flag);
			this.MaxLinearSpeed = this.DeserializeFloatAttribute(node, "max_linear_speed", true, out flag, 1f, 100f, 0f, "m/s");
			this.MaxLinearAccel = this.DeserializeFloatAttribute(node, "max_linear_acceleration", true, out flag, 0.1f, 50f, 0f, "m/s^2");
			this.MaxAngularSpeed = this.DeserializeFloatAttribute(node, "max_angular_speed", true, out flag, 1f, 180f, 0f, "deg") * 0.017453292f;
			this.MaxAngularAccel = this.DeserializeFloatAttribute(node, "max_angular_acceleration", true, out flag, 1f, 180f, 0f, "deg/s") * 0.017453292f;
			this.BowAngleLimitFromCenterline = this.DeserializeFloatAttribute(node, "bow_angle_limit_from_centerline", true, out flag, 0f, 90f, 0f, "");
			bool flag3;
			this.DeploymentArea = this.Deserialize2DDimensionElement(node, "deployment_area", true, out flag3);
			this._sails = this.DeserializeSailsElement(node, out flag3);
			bool flag4;
			this.MomentOfInertiaMultiplier = this.DeserializeVectorElement(node, "moment_of_inertia_multiplier", false, out flag4);
			if (!flag4)
			{
				this.MomentOfInertiaMultiplier = new Vec3(1f, 1f, 1f, -1f);
			}
			bool flag5;
			this.LinearFrictionMultiplier = this.DeserializeLinearDragTermElement(node, "linear_friction_multiplier", out flag5);
			if (!flag5)
			{
				this.LinearFrictionMultiplier = LinearFrictionTerm.One;
			}
			else if (!this.LinearFrictionMultiplier.IsValid)
			{
				Debug.FailedAssert("LinearFrictionMultiplier.IsValid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "Deserialize", 183);
				this.LinearFrictionMultiplier = LinearFrictionTerm.One;
			}
			bool flag6;
			this.AngularFrictionMultiplier = this.DeserializeAngularDragTermElement(node, "angular_friction_multiplier", out flag6);
			if (!flag6)
			{
				this.AngularFrictionMultiplier = Vec3.One;
			}
			else if (this.AngularFrictionMultiplier.x <= 0f || this.AngularFrictionMultiplier.y <= 0f || this.AngularFrictionMultiplier.z <= 0f)
			{
				Debug.FailedAssert("(AngularFrictionMultiplier.x > 0) && (AngularFrictionMultiplier.y > 0) && (AngularFrictionMultiplier.z > 0)", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "Deserialize", 197);
				this.AngularFrictionMultiplier = Vec3.One;
			}
			this.MaxLateralDragShift = this.DeserializeFloatAttribute(node, "lateral_drag_shift_max", false, out flag, 0f, 100f, 0f, "m");
			this.LateralDragShiftCriticalAngle = this.DeserializeFloatAttribute(node, "lateral_drag_shift_critical_angle", false, out flag, 0f, 90f, 0f, "deg") * 0.017453292f;
			Vec3 rudderStockPosition = this.DeserializeVectorElement(node, "rudder_stock_position", false, out flag3);
			if (flag3)
			{
				this.RudderStockPosition = rudderStockPosition;
			}
			else
			{
				this.RudderStockPosition = Vec3.Invalid;
			}
			this.MaximumSubmergedVolumeRatio = this.DeserializeFloatAttribute(node, "maximum_submerged_volume_ratio", false, out flag, float.MinValue, float.MaxValue, 0.7f, "");
			this.PartialHitPointsRatio = this.DeserializeFloatAttribute(node, "partial_hit_points_ratio", true, out flag, float.MinValue, float.MaxValue, 0f, "");
			this.TorqueMultiplierOfLateralBuoyantForces = this.DeserializeFloatAttribute(node, "torque_multiplier_of_lateral_buoyant_forces", false, out flag, float.MinValue, float.MaxValue, 0.5f, "");
			bool flag7;
			this.TorqueMultiplierOfVerticalBuoyantForces = this.DeserializeVectorElement(node, "torque_multiplier_of_vertical_buoyant_forces", false, out flag7);
			if (!flag7)
			{
				this.TorqueMultiplierOfVerticalBuoyantForces = new Vec3(1f, 1f, 1f, -1f);
			}
			if (objectManager != null)
			{
				ShipPhysicsReference @object = objectManager.GetObject<ShipPhysicsReference>(this.ShipPhysicsReferenceId);
				this.PhysicsReference = @object;
				return;
			}
			this.PhysicsReference = ShipPhysicsReference.Default;
		}

		// Token: 0x060009D5 RID: 2517 RVA: 0x0001FD20 File Offset: 0x0001DF20
		private T DeserializeScalarAttribute<T>(XmlNode node, string attributeName, bool isRequiredAttribute, out bool isAttributeValid)
		{
			XmlAttribute xmlAttribute = node.Attributes[attributeName];
			isAttributeValid = xmlAttribute != null && !string.IsNullOrEmpty(xmlAttribute.InnerText);
			if (isAttributeValid)
			{
				return (T)((object)Convert.ChangeType(xmlAttribute.Value, typeof(T)));
			}
			this.AssertFieldValidity(!isRequiredAttribute, attributeName);
			return default(T);
		}

		// Token: 0x060009D6 RID: 2518 RVA: 0x0001FD88 File Offset: 0x0001DF88
		private float DeserializeFloatAttribute(XmlNode node, string attributeName, bool isRequiredAttribute, out bool isAttributeValid, float minValue = -3.4028235E+38f, float maxValue = 3.4028235E+38f, float defaultValue = 0f, string unitString = "")
		{
			float num = this.DeserializeScalarAttribute<float>(node, attributeName, isRequiredAttribute, out isAttributeValid);
			if (isAttributeValid)
			{
				if (num < minValue)
				{
					Debug.FailedAssert(string.Concat(new object[] { "ShipObject(", base.StringId, "): ", attributeName, " field is less than the required minimum value of ", minValue, " ", unitString, "." }), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "DeserializeFloatAttribute", 267);
					num = minValue;
				}
				if (num > maxValue)
				{
					Debug.FailedAssert(string.Concat(new object[] { "ShipObject(", base.StringId, "): ", attributeName, " field is greater than the required maximum value of ", maxValue, " ", unitString, "." }), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "DeserializeFloatAttribute", 273);
					num = maxValue;
				}
			}
			else
			{
				num = defaultValue;
			}
			return num;
		}

		// Token: 0x060009D7 RID: 2519 RVA: 0x0001FE88 File Offset: 0x0001E088
		private Vec2 Deserialize2DDimensionElement(XmlNode node, string elementName, bool isRequiredElement, out bool isElementValid)
		{
			Vec2 zero = Vec2.Zero;
			XmlNode xmlNode = node.SelectSingleNode(elementName);
			isElementValid = xmlNode != null;
			if (isElementValid)
			{
				using (IEnumerator enumerator = xmlNode.Attributes.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						object obj = enumerator.Current;
						XmlAttribute xmlAttribute = (XmlAttribute)obj;
						string a = xmlAttribute.Name.ToLower();
						float num = float.Parse(xmlAttribute.Value);
						if (a == "width")
						{
							zero.x = num;
						}
						else if (a == "length" || a == "height")
						{
							zero.y = num;
						}
					}
					return zero;
				}
			}
			this.AssertFieldValidity(!isRequiredElement, elementName);
			return zero;
		}

		// Token: 0x060009D8 RID: 2520 RVA: 0x0001FF58 File Offset: 0x0001E158
		private Vec3 Deserialize3DDimensionElement(XmlNode node, string elementName, bool isRequiredElement, out bool isElementValid)
		{
			Vec3 zero = Vec3.Zero;
			XmlNode xmlNode = node.SelectSingleNode(elementName);
			isElementValid = xmlNode != null;
			if (isElementValid)
			{
				using (IEnumerator enumerator = xmlNode.Attributes.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						object obj = enumerator.Current;
						XmlAttribute xmlAttribute = (XmlAttribute)obj;
						string a = xmlAttribute.Name.ToLower();
						float num = float.Parse(xmlAttribute.Value);
						if (a == "width")
						{
							zero.x = num;
						}
						else if (a == "length")
						{
							zero.y = num;
						}
						else if (a == "height")
						{
							zero.z = num;
						}
					}
					return zero;
				}
			}
			this.AssertFieldValidity(!isRequiredElement, elementName);
			return zero;
		}

		// Token: 0x060009D9 RID: 2521 RVA: 0x00020034 File Offset: 0x0001E234
		private Vec3 DeserializeVectorElement(XmlNode node, string elementName, bool isRequiredElement, out bool isElementValid)
		{
			Vec3 invalid = Vec3.Invalid;
			XmlNode xmlNode = node.SelectSingleNode(elementName);
			isElementValid = xmlNode != null;
			if (isElementValid)
			{
				using (IEnumerator enumerator = xmlNode.Attributes.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						object obj = enumerator.Current;
						XmlAttribute xmlAttribute = (XmlAttribute)obj;
						string a = xmlAttribute.Name.ToLower();
						float num = float.Parse(xmlAttribute.Value);
						if (a == "x")
						{
							invalid.x = num;
						}
						else if (a == "y")
						{
							invalid.y = num;
						}
						else if (a == "z")
						{
							invalid.z = num;
						}
					}
					return invalid;
				}
			}
			this.AssertFieldValidity(!isRequiredElement, elementName);
			return invalid;
		}

		// Token: 0x060009DA RID: 2522 RVA: 0x00020110 File Offset: 0x0001E310
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
					Debug.FailedAssert(string.Concat(new object[] { "ShipObject(", base.StringId, "): ", elementName, " element must have exactly ", 5, " attributes for directional drag multipliers" }), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "DeserializeLinearDragTermElement", 416);
					isElementValid = false;
				}
				else
				{
					foreach (object obj in attributes)
					{
						XmlAttribute xmlAttribute = (XmlAttribute)obj;
						string a = xmlAttribute.Name.ToLower();
						string value = xmlAttribute.Value;
						if (a == "side")
						{
							num = float.Parse(value);
							left = num;
						}
						else if (a == "forward")
						{
							forward = float.Parse(value);
						}
						else if (a == "backward")
						{
							backward = float.Parse(value);
						}
						else if (a == "up")
						{
							up = float.Parse(value);
						}
						else if (a == "down")
						{
							down = float.Parse(value);
						}
					}
				}
			}
			LinearFrictionTerm result = new LinearFrictionTerm(num, left, forward, backward, up, down);
			isElementValid = isElementValid && result.IsValid;
			return result;
		}

		// Token: 0x060009DB RID: 2523 RVA: 0x000202C8 File Offset: 0x0001E4C8
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
					Debug.FailedAssert(string.Concat(new object[] { "ShipObject(", base.StringId, "): ", elementName, " element must have exactly ", 3, " attributes for angular drag friction multipliers" }), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "DeserializeAngularDragTermElement", 482);
					isElementValid = false;
				}
				else
				{
					foreach (object obj in attributes)
					{
						XmlAttribute xmlAttribute = (XmlAttribute)obj;
						string a = xmlAttribute.Name.ToLower();
						string value = xmlAttribute.Value;
						if (a == "pitch")
						{
							one.x = float.Parse(value);
							flag = true;
						}
						else if (a == "roll")
						{
							one.y = float.Parse(value);
							flag2 = true;
						}
						else if (a == "yaw")
						{
							one.z = float.Parse(value);
							flag3 = true;
						}
					}
				}
			}
			isElementValid = isElementValid && flag && flag2 && flag3;
			return one;
		}

		// Token: 0x060009DC RID: 2524 RVA: 0x00020430 File Offset: 0x0001E630
		private MBList<ShipSail> DeserializeSailsElement(XmlNode node, out bool isElementValid)
		{
			MBList<ShipSail> mblist = new MBList<ShipSail>();
			XmlNode xmlNode = node.SelectSingleNode("sails");
			isElementValid = xmlNode != null && xmlNode.ChildNodes.Count > 0;
			if (isElementValid)
			{
				for (int i = 0; i < xmlNode.ChildNodes.Count; i++)
				{
					XmlNode node2 = xmlNode.ChildNodes[i];
					bool flag;
					int index = this.DeserializeScalarAttribute<int>(node2, "index", true, out flag);
					SailType type = this.DeserializeSailTypeAttribute(node2, "type", true, out flag);
					float forceMultiplier = this.DeserializeFloatAttribute(node2, "force_multiplier", false, out flag, float.MinValue, float.MaxValue, 1f, "");
					float leftRotationLimit = this.DeserializeFloatAttribute(node2, "left_rotation_limit", true, out flag, 0f, float.MaxValue, 0f, "deg") * 0.017453292f;
					float rightRotationLimit = this.DeserializeFloatAttribute(node2, "right_rotation_limit", true, out flag, 0f, float.MaxValue, 0f, "deg") * 0.017453292f;
					float rotationRate = this.DeserializeFloatAttribute(node2, "rotation_rate", true, out flag, 0f, float.MaxValue, 0f, "deg/s") * 0.017453292f;
					ShipSail item = new ShipSail(this, index, type, forceMultiplier, leftRotationLimit, rightRotationLimit, rotationRate);
					mblist.Add(item);
				}
			}
			return mblist;
		}

		// Token: 0x060009DD RID: 2525 RVA: 0x0002057C File Offset: 0x0001E77C
		private SailType DeserializeSailTypeAttribute(XmlNode node, string attributeName, bool isRequiredAttribute, out bool isAttributeValid)
		{
			XmlAttribute xmlAttribute = node.Attributes[attributeName];
			SailType result = SailType.Square;
			isAttributeValid = xmlAttribute != null && !string.IsNullOrEmpty(xmlAttribute.InnerText) && Enum.TryParse<SailType>(xmlAttribute.Value, true, out result);
			if (!isAttributeValid)
			{
				this.AssertFieldValidity(!isRequiredAttribute, attributeName);
			}
			return result;
		}

		// Token: 0x060009DE RID: 2526 RVA: 0x000205CD File Offset: 0x0001E7CD
		private void AssertFieldValidity(bool assert, string fieldName)
		{
		}

		// Token: 0x04000571 RID: 1393
		private MBList<ShipSail> _sails = new MBList<ShipSail>();
	}
}
