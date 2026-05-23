using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI;

public class PropertyOwnerObject
{
	public event Action<PropertyOwnerObject, string, object> PropertyChanged;

	public event Action<PropertyOwnerObject, string, bool> boolPropertyChanged;

	public event Action<PropertyOwnerObject, string, int> intPropertyChanged;

	public event Action<PropertyOwnerObject, string, float> floatPropertyChanged;

	public event Action<PropertyOwnerObject, string, Vec2> Vec2PropertyChanged;

	public event Action<PropertyOwnerObject, string, Vector2> Vector2PropertyChanged;

	public event Action<PropertyOwnerObject, string, double> doublePropertyChanged;

	public event Action<PropertyOwnerObject, string, uint> uintPropertyChanged;

	public event Action<PropertyOwnerObject, string, Color> ColorPropertyChanged;

	protected void OnPropertyChanged<T>(T value, [CallerMemberName] string propertyName = null) where T : class
	{
		this.PropertyChanged?.Invoke(this, propertyName, value);
	}

	protected void OnPropertyChanged(int value, [CallerMemberName] string propertyName = null)
	{
		this.intPropertyChanged?.Invoke(this, propertyName, value);
	}

	protected void OnPropertyChanged(float value, [CallerMemberName] string propertyName = null)
	{
		this.floatPropertyChanged?.Invoke(this, propertyName, value);
	}

	protected void OnPropertyChanged(bool value, [CallerMemberName] string propertyName = null)
	{
		this.boolPropertyChanged?.Invoke(this, propertyName, value);
	}

	protected void OnPropertyChanged(Vec2 value, [CallerMemberName] string propertyName = null)
	{
		this.Vec2PropertyChanged?.Invoke(this, propertyName, value);
	}

	protected void OnPropertyChanged(Vector2 value, [CallerMemberName] string propertyName = null)
	{
		this.Vector2PropertyChanged?.Invoke(this, propertyName, value);
	}

	protected void OnPropertyChanged(double value, [CallerMemberName] string propertyName = null)
	{
		this.doublePropertyChanged?.Invoke(this, propertyName, value);
	}

	protected void OnPropertyChanged(uint value, [CallerMemberName] string propertyName = null)
	{
		this.uintPropertyChanged?.Invoke(this, propertyName, value);
	}

	protected void OnPropertyChanged(Color value, [CallerMemberName] string propertyName = null)
	{
		this.ColorPropertyChanged?.Invoke(this, propertyName, value);
	}
}
