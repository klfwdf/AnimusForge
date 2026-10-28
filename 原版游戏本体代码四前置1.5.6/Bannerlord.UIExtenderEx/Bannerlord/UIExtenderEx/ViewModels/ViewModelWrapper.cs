using System.ComponentModel;
using System.Reflection;
using Bannerlord.UIExtenderEx.Extensions;
using TaleWorlds.Library;

namespace Bannerlord.UIExtenderEx.ViewModels;

internal class ViewModelWrapper : ViewModel
{
	public ViewModel? Object { get; }

	protected ViewModelWrapper(ViewModel @object)
	{
		Object = @object;
		foreach (PropertyInfo viewModelProperty in this.GetViewModelProperties())
		{
			this.AddProperty(viewModelProperty.Name, viewModelProperty);
		}
		foreach (MethodInfo viewModelMethod in this.GetViewModelMethods())
		{
			this.AddMethod(viewModelMethod.Name, viewModelMethod);
		}
		IViewModel viewModel = Object;
		if (viewModel != null)
		{
			viewModel.PropertyChangedWithValue += OnPropertyChangedWithValueEventHandler;
			return;
		}
		INotifyPropertyChanged notifyPropertyChanged = Object;
		if (notifyPropertyChanged != null)
		{
			notifyPropertyChanged.PropertyChanged += OnPropertyChangedEventHandler;
		}
	}

	private void OnPropertyChangedEventHandler(object? sender, PropertyChangedEventArgs args)
	{
		OnPropertyChanged(args.PropertyName);
	}

	private unsafe void OnPropertyChangedWithValueEventHandler(object? sender, PropertyChangedWithValueEventArgs args)
	{
		base.OnPropertyChangedWithValue(((PropertyChangedWithValueEventArgs*)args)->Value, ((PropertyChangedWithValueEventArgs*)args)->PropertyName);
	}

	public override void RefreshValues()
	{
		Object?.RefreshValues();
		base.RefreshValues();
	}

	public override void OnFinalize()
	{
		if (Object != null)
		{
			Object.PropertyChanged -= OnPropertyChangedEventHandler;
			Object.PropertyChangedWithValue -= OnPropertyChangedWithValueEventHandler;
			Object.OnFinalize();
		}
		base.OnFinalize();
	}
}
