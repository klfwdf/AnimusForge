using System;
using System.Xml;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public sealed class BladeData : MBObjectBase
{
	public readonly CraftingPiece.PieceTypes PieceType;

	public DamageTypes ThrustDamageType { get; private set; }

	public float ThrustDamageFactor { get; private set; }

	public DamageTypes SwingDamageType { get; private set; }

	public float SwingDamageFactor { get; private set; }

	public float BladeLength { get; private set; }

	public float BladeWidth { get; private set; }

	public short StackAmount { get; private set; }

	public string PhysicsMaterial { get; private set; }

	public string BodyName { get; private set; }

	public string HolsterMeshName { get; private set; }

	public string HolsterBodyName { get; private set; }

	public float HolsterMeshLength { get; private set; }

	public BladeData(CraftingPiece.PieceTypes pieceType, float bladeLength)
	{
		PieceType = pieceType;
		BladeLength = bladeLength;
		ThrustDamageType = DamageTypes.Invalid;
		SwingDamageType = DamageTypes.Invalid;
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode childNode)
	{
		Initialize();
		XmlAttribute xmlAttribute = childNode.Attributes["stack_amount"];
		XmlAttribute xmlAttribute2 = childNode.Attributes["blade_length"];
		XmlAttribute xmlAttribute3 = childNode.Attributes["blade_width"];
		XmlAttribute xmlAttribute4 = childNode.Attributes["physics_material"];
		XmlAttribute xmlAttribute5 = childNode.Attributes["body_name"];
		XmlAttribute xmlAttribute6 = childNode.Attributes["holster_mesh"];
		XmlAttribute xmlAttribute7 = childNode.Attributes["holster_body_name"];
		XmlAttribute xmlAttribute8 = childNode.Attributes["holster_mesh_length"];
		StackAmount = (short)((xmlAttribute == null) ? 1 : short.Parse(xmlAttribute.Value));
		BladeLength = ((xmlAttribute2 != null) ? (0.01f * float.Parse(xmlAttribute2.Value)) : BladeLength);
		BladeWidth = ((xmlAttribute3 != null) ? (0.01f * float.Parse(xmlAttribute3.Value)) : (0.15f + BladeLength * 0.3f));
		PhysicsMaterial = xmlAttribute4?.InnerText;
		BodyName = xmlAttribute5?.InnerText;
		HolsterMeshName = xmlAttribute6?.InnerText;
		HolsterBodyName = xmlAttribute7?.InnerText;
		HolsterMeshLength = 0.01f * ((xmlAttribute8 != null) ? float.Parse(xmlAttribute8.Value) : 0f);
		foreach (XmlNode childNode2 in childNode.ChildNodes)
		{
			string name = childNode2.Name;
			if (!(name == "Thrust"))
			{
				if (name == "Swing")
				{
					XmlAttribute xmlAttribute9 = childNode2.Attributes["damage_type"];
					XmlAttribute xmlAttribute10 = childNode2.Attributes["damage_factor"];
					SwingDamageType = (DamageTypes)Enum.Parse(typeof(DamageTypes), xmlAttribute9.Value, ignoreCase: true);
					SwingDamageFactor = float.Parse(xmlAttribute10.Value);
				}
			}
			else
			{
				XmlAttribute xmlAttribute11 = childNode2.Attributes["damage_type"];
				XmlAttribute xmlAttribute12 = childNode2.Attributes["damage_factor"];
				ThrustDamageType = (DamageTypes)Enum.Parse(typeof(DamageTypes), xmlAttribute11.Value, ignoreCase: true);
				ThrustDamageFactor = float.Parse(xmlAttribute12.Value);
			}
		}
	}
}
