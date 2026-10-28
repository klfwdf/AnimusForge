using System;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace SandBox.Objects;

public class SettlementMusicData : MBObjectBase
{
	private MBList<InstrumentData> _instruments;

	public string MusicPath { get; private set; }

	public CultureObject Culture { get; private set; }

	public MBReadOnlyList<InstrumentData> Instruments => _instruments;

	public string LocationId { get; private set; }

	public int Tempo { get; private set; }

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		MusicPath = Convert.ToString(node.Attributes["event_id"].Value);
		Culture = Game.Current.ObjectManager.ReadObjectReferenceFromXml<CultureObject>("culture", node);
		LocationId = Convert.ToString(node.Attributes["location"].Value);
		Tempo = Convert.ToInt32(node.Attributes["tempo"].Value);
		_instruments = new MBList<InstrumentData>();
		if (node.HasChildNodes)
		{
			foreach (XmlNode childNode in node.ChildNodes)
			{
				if (!(childNode.Name == "Instruments"))
				{
					continue;
				}
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					if (!(childNode2.Name == "Instrument"))
					{
						continue;
					}
					InstrumentData instrumentData = null;
					if (childNode2.Attributes?["id"] != null)
					{
						string objectName = Convert.ToString(childNode2.Attributes["id"].Value);
						instrumentData = MBObjectManager.Instance.GetObject<InstrumentData>(objectName);
						if (instrumentData != null)
						{
							_instruments.Add(instrumentData);
						}
					}
					else
					{
						Debug.FailedAssert("Couldn't find required attributes of instrument xml node in Track", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Objects\\SettlementMusicData.cs", "Deserialize", 57);
					}
				}
			}
		}
		_instruments.Capacity = _instruments.Count;
	}
}
