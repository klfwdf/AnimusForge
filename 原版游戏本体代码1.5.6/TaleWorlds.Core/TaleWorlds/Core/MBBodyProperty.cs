using System.Xml;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public class MBBodyProperty : MBObjectBase
{
	private BodyProperties _bodyPropertyMin;

	private BodyProperties _bodyPropertyMax;

	public string HairTags { get; set; } = "";

	public string BeardTags { get; set; } = "";

	public string TattooTags { get; set; } = "";

	public BodyProperties BodyPropertyMin => _bodyPropertyMin;

	public BodyProperties BodyPropertyMax => _bodyPropertyMax;

	public MBBodyProperty(string stringId)
		: base(stringId)
	{
	}

	public MBBodyProperty()
	{
	}

	public static MBBodyProperty CreateFrom(MBBodyProperty bodyProperty)
	{
		MBBodyProperty mBBodyProperty = MBObjectManager.Instance.CreateObject<MBBodyProperty>();
		mBBodyProperty.HairTags = bodyProperty.HairTags;
		mBBodyProperty.BeardTags = bodyProperty.BeardTags;
		mBBodyProperty.TattooTags = bodyProperty.TattooTags;
		mBBodyProperty._bodyPropertyMin = bodyProperty._bodyPropertyMin;
		mBBodyProperty._bodyPropertyMax = bodyProperty._bodyPropertyMax;
		return mBBodyProperty;
	}

	public void Init(BodyProperties bodyPropertyMin, BodyProperties bodyPropertyMax)
	{
		base.Initialize();
		_bodyPropertyMin = bodyPropertyMin;
		_bodyPropertyMax = bodyPropertyMax;
		if (_bodyPropertyMax.Age <= 0f)
		{
			_bodyPropertyMax = _bodyPropertyMin;
		}
		if (_bodyPropertyMin.Age <= 0f)
		{
			_bodyPropertyMin = _bodyPropertyMax;
		}
		AfterInitialized();
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "BodyPropertiesMin")
			{
				BodyProperties.FromXmlNode(childNode, out _bodyPropertyMin);
			}
			else if (childNode.Name == "BodyPropertiesMax")
			{
				BodyProperties.FromXmlNode(childNode, out _bodyPropertyMax);
			}
			else if (childNode.Name == "hair_tags")
			{
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					HairTags = HairTags + childNode2.Attributes?["name"].Value + ",";
				}
			}
			else if (childNode.Name == "beard_tags")
			{
				foreach (XmlNode childNode3 in childNode.ChildNodes)
				{
					BeardTags = BeardTags + childNode3.Attributes?["name"].Value + ",";
				}
			}
			else
			{
				if (!(childNode.Name == "tattoo_tags"))
				{
					continue;
				}
				foreach (XmlNode childNode4 in childNode.ChildNodes)
				{
					TattooTags = TattooTags + childNode4.Attributes?["name"].Value + ",";
				}
			}
		}
		if (_bodyPropertyMax.Age <= 0f)
		{
			_bodyPropertyMax = _bodyPropertyMin;
		}
		if (_bodyPropertyMin.Age <= 0f)
		{
			_bodyPropertyMin = _bodyPropertyMax;
		}
	}
}
