using System;
using System.ComponentModel;

namespace MCM.Common;

public interface IRef : INotifyPropertyChanged
{
	Type Type { get; }

	object? Value { get; set; }
}
