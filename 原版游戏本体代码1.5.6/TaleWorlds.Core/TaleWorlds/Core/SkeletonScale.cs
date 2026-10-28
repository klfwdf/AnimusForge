using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public sealed class SkeletonScale : MBObjectBase
{
	public string SkeletonModel { get; private set; }

	public Vec3 MountSitBoneScale { get; private set; }

	public float MountRadiusAdder { get; private set; }

	public Vec3[] Scales { get; private set; }

	public List<string> BoneNames { get; private set; }

	public sbyte[] BoneIndices { get; private set; }

	public SkeletonScale()
	{
		BoneNames = null;
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		SkeletonModel = node.Attributes["skeleton"].InnerText;
		XmlAttribute xmlAttribute = node.Attributes["mount_sit_bone_scale"];
		Vec3 mountSitBoneScale = new Vec3(1f, 1f, 1f);
		if (xmlAttribute != null)
		{
			string[] array = xmlAttribute.Value.Split(new char[1] { ',' });
			if (array.Length == 3)
			{
				float.TryParse(array[0], out mountSitBoneScale.x);
				float.TryParse(array[1], out mountSitBoneScale.y);
				float.TryParse(array[2], out mountSitBoneScale.z);
			}
		}
		MountSitBoneScale = mountSitBoneScale;
		XmlAttribute xmlAttribute2 = node.Attributes["mount_radius_adder"];
		if (xmlAttribute2 != null)
		{
			MountRadiusAdder = float.Parse(xmlAttribute2.Value);
		}
		BoneNames = new List<string>();
		foreach (XmlNode childNode in node.ChildNodes)
		{
			string name = childNode.Name;
			if (!(name == "BoneScales"))
			{
				continue;
			}
			List<Vec3> list = new List<Vec3>();
			foreach (XmlNode childNode2 in childNode.ChildNodes)
			{
				if (childNode2.Attributes == null)
				{
					continue;
				}
				name = childNode2.Name;
				if (!(name == "BoneScale"))
				{
					continue;
				}
				XmlAttribute xmlAttribute3 = childNode2.Attributes["scale"];
				Vec3 item = default(Vec3);
				if (xmlAttribute3 != null)
				{
					string[] array2 = xmlAttribute3.Value.Split(new char[1] { ',' });
					if (array2.Length == 3)
					{
						float.TryParse(array2[0], out item.x);
						float.TryParse(array2[1], out item.y);
						float.TryParse(array2[2], out item.z);
					}
				}
				BoneNames.Add(childNode2.Attributes["bone_name"].InnerText);
				list.Add(item);
			}
			Scales = list.ToArray();
		}
	}

	public void SetBoneIndices(sbyte[] boneIndices)
	{
		BoneIndices = boneIndices;
		BoneNames = null;
	}
}
