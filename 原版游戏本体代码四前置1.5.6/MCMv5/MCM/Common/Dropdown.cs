using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MCM.Common;

public sealed class Dropdown<T> : List<T>, IEqualityComparer<Dropdown<T>>, INotifyPropertyChanged, ICloneable
{
	private int _selectedIndex;

	private T? _selectedValue;

	public static Dropdown<T> Empty => new Dropdown<T>(Array.Empty<T>(), 0);

	public int SelectedIndex
	{
		get
		{
			return _selectedIndex;
		}
		set
		{
			SetField(ref _selectedIndex, value, "SelectedIndex");
		}
	}

	public T SelectedValue
	{
		get
		{
			return base[SelectedIndex];
		}
		set
		{
			if (SetField(ref _selectedValue, value, "SelectedValue"))
			{
				int num = IndexOf(value);
				if (num != -1)
				{
					SelectedIndex = num;
				}
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public Dropdown(IEnumerable<T> values, int selectedIndex)
		: base(values)
	{
		SelectedIndex = selectedIndex;
		if (SelectedIndex != 0 && SelectedIndex < base.Count)
		{
		}
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private bool SetField<TVal>(ref TVal field, TVal value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<TVal>.Default.Equals(field, value))
		{
			return false;
		}
		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	public bool Equals(Dropdown<T>? x, Dropdown<T>? y)
	{
		return x?.SelectedIndex == y?.SelectedIndex;
	}

	public int GetHashCode(Dropdown<T> obj)
	{
		return obj.SelectedIndex;
	}

	public override int GetHashCode()
	{
		return GetHashCode(this);
	}

	public override bool Equals(object? obj)
	{
		if (obj is Dropdown<T> y)
		{
			return Equals(this, y);
		}
		return this == obj;
	}

	public object Clone()
	{
		return new Dropdown<T>(this, SelectedIndex);
	}
}
