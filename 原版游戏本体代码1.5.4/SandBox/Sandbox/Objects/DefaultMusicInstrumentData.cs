using TaleWorlds.Core;

namespace SandBox.Objects;

public class DefaultMusicInstrumentData
{
	public DefaultMusicInstrumentData()
	{
		InstrumentData instrumentData = Game.Current.ObjectManager.RegisterPresumedObject(new InstrumentData("cheerful"));
		instrumentData.InitializeInstrumentData("act_musician_idle_sitting_cheerful", "act_musician_idle_stand_cheerful", isDataWithoutInstrument: true);
		instrumentData.AfterInitialized();
		InstrumentData instrumentData2 = Game.Current.ObjectManager.RegisterPresumedObject(new InstrumentData("active"));
		instrumentData2.InitializeInstrumentData("act_musician_idle_sitting_active", "act_musician_idle_stand_active", isDataWithoutInstrument: true);
		instrumentData2.AfterInitialized();
		InstrumentData instrumentData3 = Game.Current.ObjectManager.RegisterPresumedObject(new InstrumentData("calm"));
		instrumentData3.InitializeInstrumentData("act_musician_idle_sitting_calm", "act_musician_idle_stand_calm", isDataWithoutInstrument: true);
		instrumentData3.AfterInitialized();
	}
}
