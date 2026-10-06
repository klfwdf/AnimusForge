using System.ComponentModel;

namespace TaleWorlds.Library;

public interface IViewModel : INotifyPropertyChanged
{
	event PropertyChangedWithValueEventHandler PropertyChangedWithValue;

	event PropertyChangedWithBoolValueEventHandler PropertyChangedWithBoolValue;

	event PropertyChangedWithIntValueEventHandler PropertyChangedWithIntValue;

	event PropertyChangedWithFloatValueEventHandler PropertyChangedWithFloatValue;

	event PropertyChangedWithUIntValueEventHandler PropertyChangedWithUIntValue;

	event PropertyChangedWithColorValueEventHandler PropertyChangedWithColorValue;

	event PropertyChangedWithDoubleValueEventHandler PropertyChangedWithDoubleValue;

	event PropertyChangedWithVec2ValueEventHandler PropertyChangedWithVec2Value;

	object GetViewModelAtPath(BindingPath path);

	object GetViewModelAtPath(BindingPath path, bool isList);

	object GetPropertyValue(string name);

	object GetPropertyValue(string name, PropertyTypeFeeder propertyTypeFeeder);

	void SetPropertyValue(string name, object value);

	void ExecuteCommand(string commandName, object[] parameters);
}
