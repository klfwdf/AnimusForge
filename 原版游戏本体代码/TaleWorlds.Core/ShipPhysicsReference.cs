using System;
using System.Collections;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x020000CC RID: 204
	public class ShipPhysicsReference : MBObjectBase
	{
		// Token: 0x170003CA RID: 970
		// (get) Token: 0x06000AF5 RID: 2805 RVA: 0x0002341E File Offset: 0x0002161E
		// (set) Token: 0x06000AF6 RID: 2806 RVA: 0x00023426 File Offset: 0x00021626
		public LinearFrictionTerm LinearDragTerm { get; private set; }

		// Token: 0x170003CB RID: 971
		// (get) Token: 0x06000AF7 RID: 2807 RVA: 0x0002342F File Offset: 0x0002162F
		// (set) Token: 0x06000AF8 RID: 2808 RVA: 0x00023437 File Offset: 0x00021637
		public LinearFrictionTerm LinearDampingTerm { get; private set; }

		// Token: 0x170003CC RID: 972
		// (get) Token: 0x06000AF9 RID: 2809 RVA: 0x00023440 File Offset: 0x00021640
		// (set) Token: 0x06000AFA RID: 2810 RVA: 0x00023448 File Offset: 0x00021648
		public LinearFrictionTerm ConstantLinearDampingTerm { get; private set; }

		// Token: 0x06000AFB RID: 2811 RVA: 0x00023454 File Offset: 0x00021654
		static ShipPhysicsReference()
		{
			ShipPhysicsReference.Default.PostProcessPhysicsReference();
			ShipPhysicsReference.DefaultDebris.PostProcessPhysicsReference();
		}

		// Token: 0x06000AFC RID: 2812 RVA: 0x0002357F File Offset: 0x0002177F
		public ShipPhysicsReference()
		{
		}

		// Token: 0x06000AFD RID: 2813 RVA: 0x00023587 File Offset: 0x00021787
		public ShipPhysicsReference(string stringId)
			: base(stringId)
		{
		}

		// Token: 0x06000AFE RID: 2814 RVA: 0x00023590 File Offset: 0x00021790
		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			bool flag;
			float f = this.DeserializeScalarAttribute<float>(node, "reference_mass", true, out flag);
			bool assert;
			this.LinearDragTerm = this.DeserializeDragTermElement(node, "linear_drag_term", out assert);
			bool assert2;
			this.LinearDampingTerm = this.DeserializeDragTermElement(node, "linear_damping_term", out assert2);
			bool assert3;
			this.ConstantLinearDampingTerm = this.DeserializeDragTermElement(node, "constant_linear_damping_term", out assert3);
			this.LinearDragTerm /= f;
			this.LinearDampingTerm /= f;
			this.ConstantLinearDampingTerm /= f;
			this.PostProcessPhysicsReference();
			this.AssertFieldValidity(assert, "linear_drag_term");
			this.AssertFieldValidity(assert2, "linear_damping_term");
			this.AssertFieldValidity(assert3, "constant_linear_damping_term");
		}

		// Token: 0x06000AFF RID: 2815 RVA: 0x00023654 File Offset: 0x00021854
		private void PostProcessPhysicsReference()
		{
			float defaultWaterDensity = ShipPhysicsReference.GetDefaultWaterDensity();
			this.LinearDragTerm /= defaultWaterDensity;
			this.LinearDampingTerm /= defaultWaterDensity;
			this.ConstantLinearDampingTerm /= defaultWaterDensity;
		}

		// Token: 0x06000B00 RID: 2816 RVA: 0x0002369D File Offset: 0x0002189D
		public static float GetDefaultWaterDensity()
		{
			return 1020f;
		}

		// Token: 0x06000B01 RID: 2817 RVA: 0x000236A4 File Offset: 0x000218A4
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

		// Token: 0x06000B02 RID: 2818 RVA: 0x0002370C File Offset: 0x0002190C
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

		// Token: 0x06000B03 RID: 2819 RVA: 0x000237E8 File Offset: 0x000219E8
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
				foreach (object obj in xmlNode.Attributes)
				{
					XmlAttribute xmlAttribute = (XmlAttribute)obj;
					string a = xmlAttribute.Name.ToLower();
					string value = xmlAttribute.Value;
					if (a == "right")
					{
						right = float.Parse(value);
					}
					else if (a == "left")
					{
						left = float.Parse(value);
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
			return new LinearFrictionTerm(right, left, forward, backward, up, down);
		}

		// Token: 0x06000B04 RID: 2820 RVA: 0x00023938 File Offset: 0x00021B38
		private void AssertFieldValidity(bool assert, string fieldName)
		{
		}

		// Token: 0x0400061B RID: 1563
		public static readonly ShipPhysicsReference Default = new ShipPhysicsReference
		{
			LinearDragTerm = new LinearFrictionTerm(0.89126307f, 0.89126307f, 0.0009766732f, 0.0033027069f, 0.08070293f, 0.80702925f),
			LinearDampingTerm = new LinearFrictionTerm(0.28781262f, 0.28781262f, 0.0026044627f, 0.008807218f, 0.21520779f, 2.152078f),
			ConstantLinearDampingTerm = new LinearFrictionTerm(0.045454543f, 0.045454543f, 0.013636364f, 0.027272727f, 0.045454543f, 0.045454543f)
		};

		// Token: 0x0400061C RID: 1564
		public static readonly ShipPhysicsReference DefaultDebris = new ShipPhysicsReference
		{
			LinearDragTerm = new LinearFrictionTerm(0.89126307f, 0.89126307f, 0.89126307f, 0.89126307f, 0.80702925f, 0.80702925f),
			LinearDampingTerm = new LinearFrictionTerm(0.28781262f, 0.28781262f, 0.28781262f, 0.28781262f, 2.152078f, 2.152078f),
			ConstantLinearDampingTerm = new LinearFrictionTerm(0.045454543f, 0.045454543f, 0.045454543f, 0.045454543f, 0.045454543f, 0.045454543f)
		};
	}
}
