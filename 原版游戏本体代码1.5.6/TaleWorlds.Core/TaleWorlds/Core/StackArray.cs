using System.Collections.Specialized;

namespace TaleWorlds.Core;

public class StackArray
{
	public struct StackArray3Float
	{
		private float _element0;

		private float _element1;

		private float _element2;

		public const int Length = 3;

		public float this[int index]
		{
			get
			{
				return index switch
				{
					0 => _element0, 
					1 => _element1, 
					2 => _element2, 
					_ => 0f, 
				};
			}
			set
			{
				switch (index)
				{
				case 0:
					_element0 = value;
					break;
				case 1:
					_element1 = value;
					break;
				case 2:
					_element2 = value;
					break;
				}
			}
		}
	}

	public struct StackArray5Float
	{
		private float _element0;

		private float _element1;

		private float _element2;

		private float _element3;

		private float _element4;

		public const int Length = 5;

		public float this[int index]
		{
			get
			{
				return index switch
				{
					0 => _element0, 
					1 => _element1, 
					2 => _element2, 
					3 => _element3, 
					4 => _element4, 
					_ => 0f, 
				};
			}
			set
			{
				switch (index)
				{
				case 0:
					_element0 = value;
					break;
				case 1:
					_element1 = value;
					break;
				case 2:
					_element2 = value;
					break;
				case 3:
					_element3 = value;
					break;
				case 4:
					_element4 = value;
					break;
				}
			}
		}
	}

	public struct StackArray3Int
	{
		private int _element0;

		private int _element1;

		private int _element2;

		public const int Length = 3;

		public int this[int index]
		{
			get
			{
				return index switch
				{
					0 => _element0, 
					1 => _element1, 
					2 => _element2, 
					_ => 0, 
				};
			}
			set
			{
				switch (index)
				{
				case 0:
					_element0 = value;
					break;
				case 1:
					_element1 = value;
					break;
				case 2:
					_element2 = value;
					break;
				}
			}
		}
	}

	public struct StackArray4Int
	{
		private int _element0;

		private int _element1;

		private int _element2;

		private int _element3;

		public const int Length = 4;

		public int this[int index]
		{
			get
			{
				return index switch
				{
					0 => _element0, 
					1 => _element1, 
					2 => _element2, 
					3 => _element3, 
					_ => 0, 
				};
			}
			set
			{
				switch (index)
				{
				case 0:
					_element0 = value;
					break;
				case 1:
					_element1 = value;
					break;
				case 2:
					_element2 = value;
					break;
				case 3:
					_element3 = value;
					break;
				}
			}
		}
	}

	public struct StackArray2Bool
	{
		private byte _element;

		public const int Length = 2;

		public bool this[int index]
		{
			get
			{
				return (_element & (1 << index)) != 0;
			}
			set
			{
				_element = (byte)(value ? (_element | (1 << index)) : (_element & ~(1 << index)));
			}
		}
	}

	public struct StackArray8Int
	{
		private int _element0;

		private int _element1;

		private int _element2;

		private int _element3;

		private int _element4;

		private int _element5;

		private int _element6;

		private int _element7;

		public const int Length = 8;

		public int this[int index]
		{
			get
			{
				return index switch
				{
					0 => _element0, 
					1 => _element1, 
					2 => _element2, 
					3 => _element3, 
					4 => _element4, 
					5 => _element5, 
					6 => _element6, 
					7 => _element7, 
					_ => 0, 
				};
			}
			set
			{
				switch (index)
				{
				case 0:
					_element0 = value;
					break;
				case 1:
					_element1 = value;
					break;
				case 2:
					_element2 = value;
					break;
				case 3:
					_element3 = value;
					break;
				case 4:
					_element4 = value;
					break;
				case 5:
					_element5 = value;
					break;
				case 6:
					_element6 = value;
					break;
				case 7:
					_element7 = value;
					break;
				}
			}
		}
	}

	public struct StackArray10FloatFloatTuple
	{
		private (float, float) _element0;

		private (float, float) _element1;

		private (float, float) _element2;

		private (float, float) _element3;

		private (float, float) _element4;

		private (float, float) _element5;

		private (float, float) _element6;

		private (float, float) _element7;

		private (float, float) _element8;

		private (float, float) _element9;

		public const int Length = 10;

		public (float, float) this[int index]
		{
			get
			{
				return index switch
				{
					0 => _element0, 
					1 => _element1, 
					2 => _element2, 
					3 => _element3, 
					4 => _element4, 
					5 => _element5, 
					6 => _element6, 
					7 => _element7, 
					8 => _element8, 
					9 => _element9, 
					_ => (0f, 0f), 
				};
			}
			set
			{
				switch (index)
				{
				case 0:
					_element0 = value;
					break;
				case 1:
					_element1 = value;
					break;
				case 2:
					_element2 = value;
					break;
				case 3:
					_element3 = value;
					break;
				case 4:
					_element4 = value;
					break;
				case 5:
					_element5 = value;
					break;
				case 6:
					_element6 = value;
					break;
				case 7:
					_element7 = value;
					break;
				case 8:
					_element8 = value;
					break;
				case 9:
					_element9 = value;
					break;
				}
			}
		}
	}

	public struct StackArray3Bool
	{
		private byte _element;

		public const int Length = 3;

		public bool this[int index]
		{
			get
			{
				return (_element & (1 << index)) != 0;
			}
			set
			{
				_element = (byte)(value ? (_element | (1 << index)) : (_element & ~(1 << index)));
			}
		}
	}

	public struct StackArray4Bool
	{
		private byte _element;

		public const int Length = 4;

		public bool this[int index]
		{
			get
			{
				return (_element & (1 << index)) != 0;
			}
			set
			{
				_element = (byte)(value ? (_element | (1 << index)) : (_element & ~(1 << index)));
			}
		}
	}

	public struct StackArray5Bool
	{
		private byte _element;

		public const int Length = 5;

		public bool this[int index]
		{
			get
			{
				return (_element & (1 << index)) != 0;
			}
			set
			{
				_element = (byte)(value ? (_element | (1 << index)) : (_element & ~(1 << index)));
			}
		}
	}

	public struct StackArray6Bool
	{
		private byte _element;

		public const int Length = 6;

		public bool this[int index]
		{
			get
			{
				return (_element & (1 << index)) != 0;
			}
			set
			{
				_element = (byte)(value ? (_element | (1 << index)) : (_element & ~(1 << index)));
			}
		}
	}

	public struct StackArray7Bool
	{
		private byte _element;

		public const int Length = 7;

		public bool this[int index]
		{
			get
			{
				return (_element & (1 << index)) != 0;
			}
			set
			{
				_element = (byte)(value ? (_element | (1 << index)) : (_element & ~(1 << index)));
			}
		}
	}

	public struct StackArray8Bool
	{
		private byte _element;

		public const int Length = 8;

		public bool this[int index]
		{
			get
			{
				return (_element & (1 << index)) != 0;
			}
			set
			{
				_element = (byte)(value ? (_element | (1 << index)) : (_element & ~(1 << index)));
			}
		}
	}

	public struct StackArray32Bool
	{
		private BitVector32 _element;

		public const int Length = 32;

		public bool this[int index]
		{
			get
			{
				return _element[1 << index];
			}
			set
			{
				_element[1 << index] = value;
			}
		}

		public StackArray32Bool(int init)
		{
			_element = new BitVector32(init);
		}
	}
}
