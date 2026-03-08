using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200003D RID: 61
	public class PropertyOwnerObject
	{
		// Token: 0x06000405 RID: 1029 RVA: 0x0000FF12 File Offset: 0x0000E112
		protected void OnPropertyChanged<T>(T value, [CallerMemberName] string propertyName = null) where T : class
		{
			Action<PropertyOwnerObject, string, object> propertyChanged = this.PropertyChanged;
			if (propertyChanged == null)
			{
				return;
			}
			propertyChanged(this, propertyName, value);
		}

		// Token: 0x06000406 RID: 1030 RVA: 0x0000FF2C File Offset: 0x0000E12C
		protected void OnPropertyChanged(int value, [CallerMemberName] string propertyName = null)
		{
			Action<PropertyOwnerObject, string, int> action = this.intPropertyChanged;
			if (action == null)
			{
				return;
			}
			action(this, propertyName, value);
		}

		// Token: 0x06000407 RID: 1031 RVA: 0x0000FF41 File Offset: 0x0000E141
		protected void OnPropertyChanged(float value, [CallerMemberName] string propertyName = null)
		{
			Action<PropertyOwnerObject, string, float> action = this.floatPropertyChanged;
			if (action == null)
			{
				return;
			}
			action(this, propertyName, value);
		}

		// Token: 0x06000408 RID: 1032 RVA: 0x0000FF56 File Offset: 0x0000E156
		protected void OnPropertyChanged(bool value, [CallerMemberName] string propertyName = null)
		{
			Action<PropertyOwnerObject, string, bool> action = this.boolPropertyChanged;
			if (action == null)
			{
				return;
			}
			action(this, propertyName, value);
		}

		// Token: 0x06000409 RID: 1033 RVA: 0x0000FF6B File Offset: 0x0000E16B
		protected void OnPropertyChanged(Vec2 value, [CallerMemberName] string propertyName = null)
		{
			Action<PropertyOwnerObject, string, Vec2> vec2PropertyChanged = this.Vec2PropertyChanged;
			if (vec2PropertyChanged == null)
			{
				return;
			}
			vec2PropertyChanged(this, propertyName, value);
		}

		// Token: 0x0600040A RID: 1034 RVA: 0x0000FF80 File Offset: 0x0000E180
		protected void OnPropertyChanged(Vector2 value, [CallerMemberName] string propertyName = null)
		{
			Action<PropertyOwnerObject, string, Vector2> vector2PropertyChanged = this.Vector2PropertyChanged;
			if (vector2PropertyChanged == null)
			{
				return;
			}
			vector2PropertyChanged(this, propertyName, value);
		}

		// Token: 0x0600040B RID: 1035 RVA: 0x0000FF95 File Offset: 0x0000E195
		protected void OnPropertyChanged(double value, [CallerMemberName] string propertyName = null)
		{
			Action<PropertyOwnerObject, string, double> action = this.doublePropertyChanged;
			if (action == null)
			{
				return;
			}
			action(this, propertyName, value);
		}

		// Token: 0x0600040C RID: 1036 RVA: 0x0000FFAA File Offset: 0x0000E1AA
		protected void OnPropertyChanged(uint value, [CallerMemberName] string propertyName = null)
		{
			Action<PropertyOwnerObject, string, uint> action = this.uintPropertyChanged;
			if (action == null)
			{
				return;
			}
			action(this, propertyName, value);
		}

		// Token: 0x0600040D RID: 1037 RVA: 0x0000FFBF File Offset: 0x0000E1BF
		protected void OnPropertyChanged(Color value, [CallerMemberName] string propertyName = null)
		{
			Action<PropertyOwnerObject, string, Color> colorPropertyChanged = this.ColorPropertyChanged;
			if (colorPropertyChanged == null)
			{
				return;
			}
			colorPropertyChanged(this, propertyName, value);
		}

		// Token: 0x14000005 RID: 5
		// (add) Token: 0x0600040E RID: 1038 RVA: 0x0000FFD4 File Offset: 0x0000E1D4
		// (remove) Token: 0x0600040F RID: 1039 RVA: 0x0001000C File Offset: 0x0000E20C
		public event Action<PropertyOwnerObject, string, object> PropertyChanged;

		// Token: 0x14000006 RID: 6
		// (add) Token: 0x06000410 RID: 1040 RVA: 0x00010044 File Offset: 0x0000E244
		// (remove) Token: 0x06000411 RID: 1041 RVA: 0x0001007C File Offset: 0x0000E27C
		public event Action<PropertyOwnerObject, string, bool> boolPropertyChanged;

		// Token: 0x14000007 RID: 7
		// (add) Token: 0x06000412 RID: 1042 RVA: 0x000100B4 File Offset: 0x0000E2B4
		// (remove) Token: 0x06000413 RID: 1043 RVA: 0x000100EC File Offset: 0x0000E2EC
		public event Action<PropertyOwnerObject, string, int> intPropertyChanged;

		// Token: 0x14000008 RID: 8
		// (add) Token: 0x06000414 RID: 1044 RVA: 0x00010124 File Offset: 0x0000E324
		// (remove) Token: 0x06000415 RID: 1045 RVA: 0x0001015C File Offset: 0x0000E35C
		public event Action<PropertyOwnerObject, string, float> floatPropertyChanged;

		// Token: 0x14000009 RID: 9
		// (add) Token: 0x06000416 RID: 1046 RVA: 0x00010194 File Offset: 0x0000E394
		// (remove) Token: 0x06000417 RID: 1047 RVA: 0x000101CC File Offset: 0x0000E3CC
		public event Action<PropertyOwnerObject, string, Vec2> Vec2PropertyChanged;

		// Token: 0x1400000A RID: 10
		// (add) Token: 0x06000418 RID: 1048 RVA: 0x00010204 File Offset: 0x0000E404
		// (remove) Token: 0x06000419 RID: 1049 RVA: 0x0001023C File Offset: 0x0000E43C
		public event Action<PropertyOwnerObject, string, Vector2> Vector2PropertyChanged;

		// Token: 0x1400000B RID: 11
		// (add) Token: 0x0600041A RID: 1050 RVA: 0x00010274 File Offset: 0x0000E474
		// (remove) Token: 0x0600041B RID: 1051 RVA: 0x000102AC File Offset: 0x0000E4AC
		public event Action<PropertyOwnerObject, string, double> doublePropertyChanged;

		// Token: 0x1400000C RID: 12
		// (add) Token: 0x0600041C RID: 1052 RVA: 0x000102E4 File Offset: 0x0000E4E4
		// (remove) Token: 0x0600041D RID: 1053 RVA: 0x0001031C File Offset: 0x0000E51C
		public event Action<PropertyOwnerObject, string, uint> uintPropertyChanged;

		// Token: 0x1400000D RID: 13
		// (add) Token: 0x0600041E RID: 1054 RVA: 0x00010354 File Offset: 0x0000E554
		// (remove) Token: 0x0600041F RID: 1055 RVA: 0x0001038C File Offset: 0x0000E58C
		public event Action<PropertyOwnerObject, string, Color> ColorPropertyChanged;
	}
}
