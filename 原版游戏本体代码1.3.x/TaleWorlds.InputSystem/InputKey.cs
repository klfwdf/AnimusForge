using System;

namespace TaleWorlds.InputSystem
{
	// Token: 0x0200000D RID: 13
	public enum InputKey
	{
		// Token: 0x0400003E RID: 62
		Invalid = -1,
		// Token: 0x0400003F RID: 63
		D1 = 2,
		// Token: 0x04000040 RID: 64
		D2,
		// Token: 0x04000041 RID: 65
		D3,
		// Token: 0x04000042 RID: 66
		D4,
		// Token: 0x04000043 RID: 67
		D5,
		// Token: 0x04000044 RID: 68
		D6,
		// Token: 0x04000045 RID: 69
		D7,
		// Token: 0x04000046 RID: 70
		D8,
		// Token: 0x04000047 RID: 71
		D9,
		// Token: 0x04000048 RID: 72
		D0,
		// Token: 0x04000049 RID: 73
		A = 30,
		// Token: 0x0400004A RID: 74
		B = 48,
		// Token: 0x0400004B RID: 75
		C = 46,
		// Token: 0x0400004C RID: 76
		D = 32,
		// Token: 0x0400004D RID: 77
		E = 18,
		// Token: 0x0400004E RID: 78
		F = 33,
		// Token: 0x0400004F RID: 79
		G,
		// Token: 0x04000050 RID: 80
		H,
		// Token: 0x04000051 RID: 81
		I = 23,
		// Token: 0x04000052 RID: 82
		J = 36,
		// Token: 0x04000053 RID: 83
		K,
		// Token: 0x04000054 RID: 84
		L,
		// Token: 0x04000055 RID: 85
		M = 50,
		// Token: 0x04000056 RID: 86
		N = 49,
		// Token: 0x04000057 RID: 87
		O = 24,
		// Token: 0x04000058 RID: 88
		P,
		// Token: 0x04000059 RID: 89
		Q = 16,
		// Token: 0x0400005A RID: 90
		R = 19,
		// Token: 0x0400005B RID: 91
		S = 31,
		// Token: 0x0400005C RID: 92
		T = 20,
		// Token: 0x0400005D RID: 93
		U = 22,
		// Token: 0x0400005E RID: 94
		V = 47,
		// Token: 0x0400005F RID: 95
		W = 17,
		// Token: 0x04000060 RID: 96
		X = 45,
		// Token: 0x04000061 RID: 97
		Y = 21,
		// Token: 0x04000062 RID: 98
		Z = 44,
		// Token: 0x04000063 RID: 99
		Numpad0 = 82,
		// Token: 0x04000064 RID: 100
		Numpad1 = 79,
		// Token: 0x04000065 RID: 101
		Numpad2,
		// Token: 0x04000066 RID: 102
		Numpad3,
		// Token: 0x04000067 RID: 103
		Numpad4 = 75,
		// Token: 0x04000068 RID: 104
		Numpad5,
		// Token: 0x04000069 RID: 105
		Numpad6,
		// Token: 0x0400006A RID: 106
		Numpad7 = 71,
		// Token: 0x0400006B RID: 107
		Numpad8,
		// Token: 0x0400006C RID: 108
		Numpad9,
		// Token: 0x0400006D RID: 109
		NumLock = 197,
		// Token: 0x0400006E RID: 110
		NumpadSlash = 181,
		// Token: 0x0400006F RID: 111
		NumpadMultiply = 55,
		// Token: 0x04000070 RID: 112
		NumpadMinus = 74,
		// Token: 0x04000071 RID: 113
		NumpadPlus = 78,
		// Token: 0x04000072 RID: 114
		NumpadEnter = 156,
		// Token: 0x04000073 RID: 115
		NumpadPeriod = 83,
		// Token: 0x04000074 RID: 116
		Insert = 210,
		// Token: 0x04000075 RID: 117
		Delete,
		// Token: 0x04000076 RID: 118
		Home = 199,
		// Token: 0x04000077 RID: 119
		End = 207,
		// Token: 0x04000078 RID: 120
		PageUp = 201,
		// Token: 0x04000079 RID: 121
		PageDown = 209,
		// Token: 0x0400007A RID: 122
		Up = 200,
		// Token: 0x0400007B RID: 123
		Down = 208,
		// Token: 0x0400007C RID: 124
		Left = 203,
		// Token: 0x0400007D RID: 125
		Right = 205,
		// Token: 0x0400007E RID: 126
		F1 = 59,
		// Token: 0x0400007F RID: 127
		F2,
		// Token: 0x04000080 RID: 128
		F3,
		// Token: 0x04000081 RID: 129
		F4,
		// Token: 0x04000082 RID: 130
		F5,
		// Token: 0x04000083 RID: 131
		F6,
		// Token: 0x04000084 RID: 132
		F7,
		// Token: 0x04000085 RID: 133
		F8,
		// Token: 0x04000086 RID: 134
		F9,
		// Token: 0x04000087 RID: 135
		F10,
		// Token: 0x04000088 RID: 136
		F11 = 87,
		// Token: 0x04000089 RID: 137
		F12,
		// Token: 0x0400008A RID: 138
		F13 = 100,
		// Token: 0x0400008B RID: 139
		F14,
		// Token: 0x0400008C RID: 140
		F15,
		// Token: 0x0400008D RID: 141
		F16,
		// Token: 0x0400008E RID: 142
		F17,
		// Token: 0x0400008F RID: 143
		F18,
		// Token: 0x04000090 RID: 144
		F19,
		// Token: 0x04000091 RID: 145
		F20,
		// Token: 0x04000092 RID: 146
		F21,
		// Token: 0x04000093 RID: 147
		F22,
		// Token: 0x04000094 RID: 148
		F23,
		// Token: 0x04000095 RID: 149
		F24 = 118,
		// Token: 0x04000096 RID: 150
		Space = 57,
		// Token: 0x04000097 RID: 151
		Escape = 1,
		// Token: 0x04000098 RID: 152
		Enter = 28,
		// Token: 0x04000099 RID: 153
		Tab = 15,
		// Token: 0x0400009A RID: 154
		BackSpace = 14,
		// Token: 0x0400009B RID: 155
		OpenBraces = 26,
		// Token: 0x0400009C RID: 156
		CloseBraces,
		// Token: 0x0400009D RID: 157
		Comma = 51,
		// Token: 0x0400009E RID: 158
		Period,
		// Token: 0x0400009F RID: 159
		Slash,
		// Token: 0x040000A0 RID: 160
		BackSlash = 43,
		// Token: 0x040000A1 RID: 161
		Equals = 13,
		// Token: 0x040000A2 RID: 162
		Minus = 12,
		// Token: 0x040000A3 RID: 163
		SemiColon = 39,
		// Token: 0x040000A4 RID: 164
		Apostrophe,
		// Token: 0x040000A5 RID: 165
		Tilde,
		// Token: 0x040000A6 RID: 166
		CapsLock = 58,
		// Token: 0x040000A7 RID: 167
		Extended = 86,
		// Token: 0x040000A8 RID: 168
		LeftShift = 42,
		// Token: 0x040000A9 RID: 169
		RightShift = 54,
		// Token: 0x040000AA RID: 170
		LeftControl = 29,
		// Token: 0x040000AB RID: 171
		RightControl = 157,
		// Token: 0x040000AC RID: 172
		LeftAlt = 56,
		// Token: 0x040000AD RID: 173
		RightAlt = 184,
		// Token: 0x040000AE RID: 174
		LeftMouseButton = 224,
		// Token: 0x040000AF RID: 175
		RightMouseButton,
		// Token: 0x040000B0 RID: 176
		MiddleMouseButton,
		// Token: 0x040000B1 RID: 177
		X1MouseButton,
		// Token: 0x040000B2 RID: 178
		X2MouseButton,
		// Token: 0x040000B3 RID: 179
		MouseScrollUp,
		// Token: 0x040000B4 RID: 180
		MouseScrollDown,
		// Token: 0x040000B5 RID: 181
		ControllerLStick = 222,
		// Token: 0x040000B6 RID: 182
		ControllerRStick,
		// Token: 0x040000B7 RID: 183
		ControllerLOptionTap = 231,
		// Token: 0x040000B8 RID: 184
		ControllerLStickUp,
		// Token: 0x040000B9 RID: 185
		ControllerLStickDown,
		// Token: 0x040000BA RID: 186
		ControllerLStickLeft,
		// Token: 0x040000BB RID: 187
		ControllerLStickRight,
		// Token: 0x040000BC RID: 188
		ControllerRStickUp,
		// Token: 0x040000BD RID: 189
		ControllerRStickDown,
		// Token: 0x040000BE RID: 190
		ControllerRStickLeft,
		// Token: 0x040000BF RID: 191
		ControllerRStickRight,
		// Token: 0x040000C0 RID: 192
		ControllerLUp,
		// Token: 0x040000C1 RID: 193
		ControllerLDown,
		// Token: 0x040000C2 RID: 194
		ControllerLLeft,
		// Token: 0x040000C3 RID: 195
		ControllerLRight,
		// Token: 0x040000C4 RID: 196
		ControllerRUp,
		// Token: 0x040000C5 RID: 197
		ControllerRDown,
		// Token: 0x040000C6 RID: 198
		ControllerRLeft,
		// Token: 0x040000C7 RID: 199
		ControllerRRight,
		// Token: 0x040000C8 RID: 200
		ControllerLBumper,
		// Token: 0x040000C9 RID: 201
		ControllerRBumper,
		// Token: 0x040000CA RID: 202
		ControllerLOption,
		// Token: 0x040000CB RID: 203
		ControllerROption,
		// Token: 0x040000CC RID: 204
		ControllerLThumb,
		// Token: 0x040000CD RID: 205
		ControllerRThumb,
		// Token: 0x040000CE RID: 206
		ControllerLTrigger,
		// Token: 0x040000CF RID: 207
		ControllerRTrigger
	}
}
