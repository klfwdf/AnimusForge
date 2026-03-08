using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x020000CE RID: 206
	public sealed class SkeletonScale : MBObjectBase
	{
		// Token: 0x170003CD RID: 973
		// (get) Token: 0x06000B06 RID: 2822 RVA: 0x00023977 File Offset: 0x00021B77
		// (set) Token: 0x06000B07 RID: 2823 RVA: 0x0002397F File Offset: 0x00021B7F
		public string SkeletonModel { get; private set; }

		// Token: 0x170003CE RID: 974
		// (get) Token: 0x06000B08 RID: 2824 RVA: 0x00023988 File Offset: 0x00021B88
		// (set) Token: 0x06000B09 RID: 2825 RVA: 0x00023990 File Offset: 0x00021B90
		public Vec3 MountSitBoneScale { get; private set; }

		// Token: 0x170003CF RID: 975
		// (get) Token: 0x06000B0A RID: 2826 RVA: 0x00023999 File Offset: 0x00021B99
		// (set) Token: 0x06000B0B RID: 2827 RVA: 0x000239A1 File Offset: 0x00021BA1
		public float MountRadiusAdder { get; private set; }

		// Token: 0x170003D0 RID: 976
		// (get) Token: 0x06000B0C RID: 2828 RVA: 0x000239AA File Offset: 0x00021BAA
		// (set) Token: 0x06000B0D RID: 2829 RVA: 0x000239B2 File Offset: 0x00021BB2
		public Vec3[] Scales { get; private set; }

		// Token: 0x170003D1 RID: 977
		// (get) Token: 0x06000B0E RID: 2830 RVA: 0x000239BB File Offset: 0x00021BBB
		// (set) Token: 0x06000B0F RID: 2831 RVA: 0x000239C3 File Offset: 0x00021BC3
		public List<string> BoneNames { get; private set; }

		// Token: 0x170003D2 RID: 978
		// (get) Token: 0x06000B10 RID: 2832 RVA: 0x000239CC File Offset: 0x00021BCC
		// (set) Token: 0x06000B11 RID: 2833 RVA: 0x000239D4 File Offset: 0x00021BD4
		public sbyte[] BoneIndices { get; private set; }

		// Token: 0x06000B12 RID: 2834 RVA: 0x000239DD File Offset: 0x00021BDD
		public SkeletonScale()
		{
			this.BoneNames = null;
		}

		// Token: 0x06000B13 RID: 2835 RVA: 0x000239EC File Offset: 0x00021BEC
		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			this.SkeletonModel = node.Attributes["skeleton"].InnerText;
			XmlAttribute xmlAttribute = node.Attributes["mount_sit_bone_scale"];
			Vec3 mountSitBoneScale = new Vec3(1f, 1f, 1f, -1f);
			if (xmlAttribute != null)
			{
				string[] array = xmlAttribute.Value.Split(new char[] { ',' });
				if (array.Length == 3)
				{
					float.TryParse(array[0], out mountSitBoneScale.x);
					float.TryParse(array[1], out mountSitBoneScale.y);
					float.TryParse(array[2], out mountSitBoneScale.z);
				}
			}
			this.MountSitBoneScale = mountSitBoneScale;
			XmlAttribute xmlAttribute2 = node.Attributes["mount_radius_adder"];
			if (xmlAttribute2 != null)
			{
				this.MountRadiusAdder = float.Parse(xmlAttribute2.Value);
			}
			this.BoneNames = new List<string>();
			foreach (object obj in node.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				string name = xmlNode.Name;
				if (name == "BoneScales")
				{
					List<Vec3> list = new List<Vec3>();
					foreach (object obj2 in xmlNode.ChildNodes)
					{
						XmlNode xmlNode2 = (XmlNode)obj2;
						if (xmlNode2.Attributes != null)
						{
							name = xmlNode2.Name;
							if (name == "BoneScale")
							{
								XmlAttribute xmlAttribute3 = xmlNode2.Attributes["scale"];
								Vec3 item = default(Vec3);
								if (xmlAttribute3 != null)
								{
									string[] array2 = xmlAttribute3.Value.Split(new char[] { ',' });
									if (array2.Length == 3)
									{
										float.TryParse(array2[0], out item.x);
										float.TryParse(array2[1], out item.y);
										float.TryParse(array2[2], out item.z);
									}
								}
								this.BoneNames.Add(xmlNode2.Attributes["bone_name"].InnerText);
								list.Add(item);
							}
						}
					}
					this.Scales = list.ToArray();
				}
			}
		}

		// Token: 0x06000B14 RID: 2836 RVA: 0x00023C7C File Offset: 0x00021E7C
		public void SetBoneIndices(sbyte[] boneIndices)
		{
			this.BoneIndices = boneIndices;
			this.BoneNames = null;
		}
	}
}
