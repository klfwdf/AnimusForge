using System;

namespace Steamworks
{
	// Token: 0x02000109 RID: 265
	public enum EInputActionOrigin
	{
		// Token: 0x04000438 RID: 1080
		k_EInputActionOrigin_None,
		// Token: 0x04000439 RID: 1081
		k_EInputActionOrigin_SteamController_A,
		// Token: 0x0400043A RID: 1082
		k_EInputActionOrigin_SteamController_B,
		// Token: 0x0400043B RID: 1083
		k_EInputActionOrigin_SteamController_X,
		// Token: 0x0400043C RID: 1084
		k_EInputActionOrigin_SteamController_Y,
		// Token: 0x0400043D RID: 1085
		k_EInputActionOrigin_SteamController_LeftBumper,
		// Token: 0x0400043E RID: 1086
		k_EInputActionOrigin_SteamController_RightBumper,
		// Token: 0x0400043F RID: 1087
		k_EInputActionOrigin_SteamController_LeftGrip,
		// Token: 0x04000440 RID: 1088
		k_EInputActionOrigin_SteamController_RightGrip,
		// Token: 0x04000441 RID: 1089
		k_EInputActionOrigin_SteamController_Start,
		// Token: 0x04000442 RID: 1090
		k_EInputActionOrigin_SteamController_Back,
		// Token: 0x04000443 RID: 1091
		k_EInputActionOrigin_SteamController_LeftPad_Touch,
		// Token: 0x04000444 RID: 1092
		k_EInputActionOrigin_SteamController_LeftPad_Swipe,
		// Token: 0x04000445 RID: 1093
		k_EInputActionOrigin_SteamController_LeftPad_Click,
		// Token: 0x04000446 RID: 1094
		k_EInputActionOrigin_SteamController_LeftPad_DPadNorth,
		// Token: 0x04000447 RID: 1095
		k_EInputActionOrigin_SteamController_LeftPad_DPadSouth,
		// Token: 0x04000448 RID: 1096
		k_EInputActionOrigin_SteamController_LeftPad_DPadWest,
		// Token: 0x04000449 RID: 1097
		k_EInputActionOrigin_SteamController_LeftPad_DPadEast,
		// Token: 0x0400044A RID: 1098
		k_EInputActionOrigin_SteamController_RightPad_Touch,
		// Token: 0x0400044B RID: 1099
		k_EInputActionOrigin_SteamController_RightPad_Swipe,
		// Token: 0x0400044C RID: 1100
		k_EInputActionOrigin_SteamController_RightPad_Click,
		// Token: 0x0400044D RID: 1101
		k_EInputActionOrigin_SteamController_RightPad_DPadNorth,
		// Token: 0x0400044E RID: 1102
		k_EInputActionOrigin_SteamController_RightPad_DPadSouth,
		// Token: 0x0400044F RID: 1103
		k_EInputActionOrigin_SteamController_RightPad_DPadWest,
		// Token: 0x04000450 RID: 1104
		k_EInputActionOrigin_SteamController_RightPad_DPadEast,
		// Token: 0x04000451 RID: 1105
		k_EInputActionOrigin_SteamController_LeftTrigger_Pull,
		// Token: 0x04000452 RID: 1106
		k_EInputActionOrigin_SteamController_LeftTrigger_Click,
		// Token: 0x04000453 RID: 1107
		k_EInputActionOrigin_SteamController_RightTrigger_Pull,
		// Token: 0x04000454 RID: 1108
		k_EInputActionOrigin_SteamController_RightTrigger_Click,
		// Token: 0x04000455 RID: 1109
		k_EInputActionOrigin_SteamController_LeftStick_Move,
		// Token: 0x04000456 RID: 1110
		k_EInputActionOrigin_SteamController_LeftStick_Click,
		// Token: 0x04000457 RID: 1111
		k_EInputActionOrigin_SteamController_LeftStick_DPadNorth,
		// Token: 0x04000458 RID: 1112
		k_EInputActionOrigin_SteamController_LeftStick_DPadSouth,
		// Token: 0x04000459 RID: 1113
		k_EInputActionOrigin_SteamController_LeftStick_DPadWest,
		// Token: 0x0400045A RID: 1114
		k_EInputActionOrigin_SteamController_LeftStick_DPadEast,
		// Token: 0x0400045B RID: 1115
		k_EInputActionOrigin_SteamController_Gyro_Move,
		// Token: 0x0400045C RID: 1116
		k_EInputActionOrigin_SteamController_Gyro_Pitch,
		// Token: 0x0400045D RID: 1117
		k_EInputActionOrigin_SteamController_Gyro_Yaw,
		// Token: 0x0400045E RID: 1118
		k_EInputActionOrigin_SteamController_Gyro_Roll,
		// Token: 0x0400045F RID: 1119
		k_EInputActionOrigin_SteamController_Reserved0,
		// Token: 0x04000460 RID: 1120
		k_EInputActionOrigin_SteamController_Reserved1,
		// Token: 0x04000461 RID: 1121
		k_EInputActionOrigin_SteamController_Reserved2,
		// Token: 0x04000462 RID: 1122
		k_EInputActionOrigin_SteamController_Reserved3,
		// Token: 0x04000463 RID: 1123
		k_EInputActionOrigin_SteamController_Reserved4,
		// Token: 0x04000464 RID: 1124
		k_EInputActionOrigin_SteamController_Reserved5,
		// Token: 0x04000465 RID: 1125
		k_EInputActionOrigin_SteamController_Reserved6,
		// Token: 0x04000466 RID: 1126
		k_EInputActionOrigin_SteamController_Reserved7,
		// Token: 0x04000467 RID: 1127
		k_EInputActionOrigin_SteamController_Reserved8,
		// Token: 0x04000468 RID: 1128
		k_EInputActionOrigin_SteamController_Reserved9,
		// Token: 0x04000469 RID: 1129
		k_EInputActionOrigin_SteamController_Reserved10,
		// Token: 0x0400046A RID: 1130
		k_EInputActionOrigin_PS4_X,
		// Token: 0x0400046B RID: 1131
		k_EInputActionOrigin_PS4_Circle,
		// Token: 0x0400046C RID: 1132
		k_EInputActionOrigin_PS4_Triangle,
		// Token: 0x0400046D RID: 1133
		k_EInputActionOrigin_PS4_Square,
		// Token: 0x0400046E RID: 1134
		k_EInputActionOrigin_PS4_LeftBumper,
		// Token: 0x0400046F RID: 1135
		k_EInputActionOrigin_PS4_RightBumper,
		// Token: 0x04000470 RID: 1136
		k_EInputActionOrigin_PS4_Options,
		// Token: 0x04000471 RID: 1137
		k_EInputActionOrigin_PS4_Share,
		// Token: 0x04000472 RID: 1138
		k_EInputActionOrigin_PS4_LeftPad_Touch,
		// Token: 0x04000473 RID: 1139
		k_EInputActionOrigin_PS4_LeftPad_Swipe,
		// Token: 0x04000474 RID: 1140
		k_EInputActionOrigin_PS4_LeftPad_Click,
		// Token: 0x04000475 RID: 1141
		k_EInputActionOrigin_PS4_LeftPad_DPadNorth,
		// Token: 0x04000476 RID: 1142
		k_EInputActionOrigin_PS4_LeftPad_DPadSouth,
		// Token: 0x04000477 RID: 1143
		k_EInputActionOrigin_PS4_LeftPad_DPadWest,
		// Token: 0x04000478 RID: 1144
		k_EInputActionOrigin_PS4_LeftPad_DPadEast,
		// Token: 0x04000479 RID: 1145
		k_EInputActionOrigin_PS4_RightPad_Touch,
		// Token: 0x0400047A RID: 1146
		k_EInputActionOrigin_PS4_RightPad_Swipe,
		// Token: 0x0400047B RID: 1147
		k_EInputActionOrigin_PS4_RightPad_Click,
		// Token: 0x0400047C RID: 1148
		k_EInputActionOrigin_PS4_RightPad_DPadNorth,
		// Token: 0x0400047D RID: 1149
		k_EInputActionOrigin_PS4_RightPad_DPadSouth,
		// Token: 0x0400047E RID: 1150
		k_EInputActionOrigin_PS4_RightPad_DPadWest,
		// Token: 0x0400047F RID: 1151
		k_EInputActionOrigin_PS4_RightPad_DPadEast,
		// Token: 0x04000480 RID: 1152
		k_EInputActionOrigin_PS4_CenterPad_Touch,
		// Token: 0x04000481 RID: 1153
		k_EInputActionOrigin_PS4_CenterPad_Swipe,
		// Token: 0x04000482 RID: 1154
		k_EInputActionOrigin_PS4_CenterPad_Click,
		// Token: 0x04000483 RID: 1155
		k_EInputActionOrigin_PS4_CenterPad_DPadNorth,
		// Token: 0x04000484 RID: 1156
		k_EInputActionOrigin_PS4_CenterPad_DPadSouth,
		// Token: 0x04000485 RID: 1157
		k_EInputActionOrigin_PS4_CenterPad_DPadWest,
		// Token: 0x04000486 RID: 1158
		k_EInputActionOrigin_PS4_CenterPad_DPadEast,
		// Token: 0x04000487 RID: 1159
		k_EInputActionOrigin_PS4_LeftTrigger_Pull,
		// Token: 0x04000488 RID: 1160
		k_EInputActionOrigin_PS4_LeftTrigger_Click,
		// Token: 0x04000489 RID: 1161
		k_EInputActionOrigin_PS4_RightTrigger_Pull,
		// Token: 0x0400048A RID: 1162
		k_EInputActionOrigin_PS4_RightTrigger_Click,
		// Token: 0x0400048B RID: 1163
		k_EInputActionOrigin_PS4_LeftStick_Move,
		// Token: 0x0400048C RID: 1164
		k_EInputActionOrigin_PS4_LeftStick_Click,
		// Token: 0x0400048D RID: 1165
		k_EInputActionOrigin_PS4_LeftStick_DPadNorth,
		// Token: 0x0400048E RID: 1166
		k_EInputActionOrigin_PS4_LeftStick_DPadSouth,
		// Token: 0x0400048F RID: 1167
		k_EInputActionOrigin_PS4_LeftStick_DPadWest,
		// Token: 0x04000490 RID: 1168
		k_EInputActionOrigin_PS4_LeftStick_DPadEast,
		// Token: 0x04000491 RID: 1169
		k_EInputActionOrigin_PS4_RightStick_Move,
		// Token: 0x04000492 RID: 1170
		k_EInputActionOrigin_PS4_RightStick_Click,
		// Token: 0x04000493 RID: 1171
		k_EInputActionOrigin_PS4_RightStick_DPadNorth,
		// Token: 0x04000494 RID: 1172
		k_EInputActionOrigin_PS4_RightStick_DPadSouth,
		// Token: 0x04000495 RID: 1173
		k_EInputActionOrigin_PS4_RightStick_DPadWest,
		// Token: 0x04000496 RID: 1174
		k_EInputActionOrigin_PS4_RightStick_DPadEast,
		// Token: 0x04000497 RID: 1175
		k_EInputActionOrigin_PS4_DPad_North,
		// Token: 0x04000498 RID: 1176
		k_EInputActionOrigin_PS4_DPad_South,
		// Token: 0x04000499 RID: 1177
		k_EInputActionOrigin_PS4_DPad_West,
		// Token: 0x0400049A RID: 1178
		k_EInputActionOrigin_PS4_DPad_East,
		// Token: 0x0400049B RID: 1179
		k_EInputActionOrigin_PS4_Gyro_Move,
		// Token: 0x0400049C RID: 1180
		k_EInputActionOrigin_PS4_Gyro_Pitch,
		// Token: 0x0400049D RID: 1181
		k_EInputActionOrigin_PS4_Gyro_Yaw,
		// Token: 0x0400049E RID: 1182
		k_EInputActionOrigin_PS4_Gyro_Roll,
		// Token: 0x0400049F RID: 1183
		k_EInputActionOrigin_PS4_DPad_Move,
		// Token: 0x040004A0 RID: 1184
		k_EInputActionOrigin_PS4_Reserved1,
		// Token: 0x040004A1 RID: 1185
		k_EInputActionOrigin_PS4_Reserved2,
		// Token: 0x040004A2 RID: 1186
		k_EInputActionOrigin_PS4_Reserved3,
		// Token: 0x040004A3 RID: 1187
		k_EInputActionOrigin_PS4_Reserved4,
		// Token: 0x040004A4 RID: 1188
		k_EInputActionOrigin_PS4_Reserved5,
		// Token: 0x040004A5 RID: 1189
		k_EInputActionOrigin_PS4_Reserved6,
		// Token: 0x040004A6 RID: 1190
		k_EInputActionOrigin_PS4_Reserved7,
		// Token: 0x040004A7 RID: 1191
		k_EInputActionOrigin_PS4_Reserved8,
		// Token: 0x040004A8 RID: 1192
		k_EInputActionOrigin_PS4_Reserved9,
		// Token: 0x040004A9 RID: 1193
		k_EInputActionOrigin_PS4_Reserved10,
		// Token: 0x040004AA RID: 1194
		k_EInputActionOrigin_XBoxOne_A,
		// Token: 0x040004AB RID: 1195
		k_EInputActionOrigin_XBoxOne_B,
		// Token: 0x040004AC RID: 1196
		k_EInputActionOrigin_XBoxOne_X,
		// Token: 0x040004AD RID: 1197
		k_EInputActionOrigin_XBoxOne_Y,
		// Token: 0x040004AE RID: 1198
		k_EInputActionOrigin_XBoxOne_LeftBumper,
		// Token: 0x040004AF RID: 1199
		k_EInputActionOrigin_XBoxOne_RightBumper,
		// Token: 0x040004B0 RID: 1200
		k_EInputActionOrigin_XBoxOne_Menu,
		// Token: 0x040004B1 RID: 1201
		k_EInputActionOrigin_XBoxOne_View,
		// Token: 0x040004B2 RID: 1202
		k_EInputActionOrigin_XBoxOne_LeftTrigger_Pull,
		// Token: 0x040004B3 RID: 1203
		k_EInputActionOrigin_XBoxOne_LeftTrigger_Click,
		// Token: 0x040004B4 RID: 1204
		k_EInputActionOrigin_XBoxOne_RightTrigger_Pull,
		// Token: 0x040004B5 RID: 1205
		k_EInputActionOrigin_XBoxOne_RightTrigger_Click,
		// Token: 0x040004B6 RID: 1206
		k_EInputActionOrigin_XBoxOne_LeftStick_Move,
		// Token: 0x040004B7 RID: 1207
		k_EInputActionOrigin_XBoxOne_LeftStick_Click,
		// Token: 0x040004B8 RID: 1208
		k_EInputActionOrigin_XBoxOne_LeftStick_DPadNorth,
		// Token: 0x040004B9 RID: 1209
		k_EInputActionOrigin_XBoxOne_LeftStick_DPadSouth,
		// Token: 0x040004BA RID: 1210
		k_EInputActionOrigin_XBoxOne_LeftStick_DPadWest,
		// Token: 0x040004BB RID: 1211
		k_EInputActionOrigin_XBoxOne_LeftStick_DPadEast,
		// Token: 0x040004BC RID: 1212
		k_EInputActionOrigin_XBoxOne_RightStick_Move,
		// Token: 0x040004BD RID: 1213
		k_EInputActionOrigin_XBoxOne_RightStick_Click,
		// Token: 0x040004BE RID: 1214
		k_EInputActionOrigin_XBoxOne_RightStick_DPadNorth,
		// Token: 0x040004BF RID: 1215
		k_EInputActionOrigin_XBoxOne_RightStick_DPadSouth,
		// Token: 0x040004C0 RID: 1216
		k_EInputActionOrigin_XBoxOne_RightStick_DPadWest,
		// Token: 0x040004C1 RID: 1217
		k_EInputActionOrigin_XBoxOne_RightStick_DPadEast,
		// Token: 0x040004C2 RID: 1218
		k_EInputActionOrigin_XBoxOne_DPad_North,
		// Token: 0x040004C3 RID: 1219
		k_EInputActionOrigin_XBoxOne_DPad_South,
		// Token: 0x040004C4 RID: 1220
		k_EInputActionOrigin_XBoxOne_DPad_West,
		// Token: 0x040004C5 RID: 1221
		k_EInputActionOrigin_XBoxOne_DPad_East,
		// Token: 0x040004C6 RID: 1222
		k_EInputActionOrigin_XBoxOne_DPad_Move,
		// Token: 0x040004C7 RID: 1223
		k_EInputActionOrigin_XBoxOne_LeftGrip_Lower,
		// Token: 0x040004C8 RID: 1224
		k_EInputActionOrigin_XBoxOne_LeftGrip_Upper,
		// Token: 0x040004C9 RID: 1225
		k_EInputActionOrigin_XBoxOne_RightGrip_Lower,
		// Token: 0x040004CA RID: 1226
		k_EInputActionOrigin_XBoxOne_RightGrip_Upper,
		// Token: 0x040004CB RID: 1227
		k_EInputActionOrigin_XBoxOne_Share,
		// Token: 0x040004CC RID: 1228
		k_EInputActionOrigin_XBoxOne_Reserved6,
		// Token: 0x040004CD RID: 1229
		k_EInputActionOrigin_XBoxOne_Reserved7,
		// Token: 0x040004CE RID: 1230
		k_EInputActionOrigin_XBoxOne_Reserved8,
		// Token: 0x040004CF RID: 1231
		k_EInputActionOrigin_XBoxOne_Reserved9,
		// Token: 0x040004D0 RID: 1232
		k_EInputActionOrigin_XBoxOne_Reserved10,
		// Token: 0x040004D1 RID: 1233
		k_EInputActionOrigin_XBox360_A,
		// Token: 0x040004D2 RID: 1234
		k_EInputActionOrigin_XBox360_B,
		// Token: 0x040004D3 RID: 1235
		k_EInputActionOrigin_XBox360_X,
		// Token: 0x040004D4 RID: 1236
		k_EInputActionOrigin_XBox360_Y,
		// Token: 0x040004D5 RID: 1237
		k_EInputActionOrigin_XBox360_LeftBumper,
		// Token: 0x040004D6 RID: 1238
		k_EInputActionOrigin_XBox360_RightBumper,
		// Token: 0x040004D7 RID: 1239
		k_EInputActionOrigin_XBox360_Start,
		// Token: 0x040004D8 RID: 1240
		k_EInputActionOrigin_XBox360_Back,
		// Token: 0x040004D9 RID: 1241
		k_EInputActionOrigin_XBox360_LeftTrigger_Pull,
		// Token: 0x040004DA RID: 1242
		k_EInputActionOrigin_XBox360_LeftTrigger_Click,
		// Token: 0x040004DB RID: 1243
		k_EInputActionOrigin_XBox360_RightTrigger_Pull,
		// Token: 0x040004DC RID: 1244
		k_EInputActionOrigin_XBox360_RightTrigger_Click,
		// Token: 0x040004DD RID: 1245
		k_EInputActionOrigin_XBox360_LeftStick_Move,
		// Token: 0x040004DE RID: 1246
		k_EInputActionOrigin_XBox360_LeftStick_Click,
		// Token: 0x040004DF RID: 1247
		k_EInputActionOrigin_XBox360_LeftStick_DPadNorth,
		// Token: 0x040004E0 RID: 1248
		k_EInputActionOrigin_XBox360_LeftStick_DPadSouth,
		// Token: 0x040004E1 RID: 1249
		k_EInputActionOrigin_XBox360_LeftStick_DPadWest,
		// Token: 0x040004E2 RID: 1250
		k_EInputActionOrigin_XBox360_LeftStick_DPadEast,
		// Token: 0x040004E3 RID: 1251
		k_EInputActionOrigin_XBox360_RightStick_Move,
		// Token: 0x040004E4 RID: 1252
		k_EInputActionOrigin_XBox360_RightStick_Click,
		// Token: 0x040004E5 RID: 1253
		k_EInputActionOrigin_XBox360_RightStick_DPadNorth,
		// Token: 0x040004E6 RID: 1254
		k_EInputActionOrigin_XBox360_RightStick_DPadSouth,
		// Token: 0x040004E7 RID: 1255
		k_EInputActionOrigin_XBox360_RightStick_DPadWest,
		// Token: 0x040004E8 RID: 1256
		k_EInputActionOrigin_XBox360_RightStick_DPadEast,
		// Token: 0x040004E9 RID: 1257
		k_EInputActionOrigin_XBox360_DPad_North,
		// Token: 0x040004EA RID: 1258
		k_EInputActionOrigin_XBox360_DPad_South,
		// Token: 0x040004EB RID: 1259
		k_EInputActionOrigin_XBox360_DPad_West,
		// Token: 0x040004EC RID: 1260
		k_EInputActionOrigin_XBox360_DPad_East,
		// Token: 0x040004ED RID: 1261
		k_EInputActionOrigin_XBox360_DPad_Move,
		// Token: 0x040004EE RID: 1262
		k_EInputActionOrigin_XBox360_Reserved1,
		// Token: 0x040004EF RID: 1263
		k_EInputActionOrigin_XBox360_Reserved2,
		// Token: 0x040004F0 RID: 1264
		k_EInputActionOrigin_XBox360_Reserved3,
		// Token: 0x040004F1 RID: 1265
		k_EInputActionOrigin_XBox360_Reserved4,
		// Token: 0x040004F2 RID: 1266
		k_EInputActionOrigin_XBox360_Reserved5,
		// Token: 0x040004F3 RID: 1267
		k_EInputActionOrigin_XBox360_Reserved6,
		// Token: 0x040004F4 RID: 1268
		k_EInputActionOrigin_XBox360_Reserved7,
		// Token: 0x040004F5 RID: 1269
		k_EInputActionOrigin_XBox360_Reserved8,
		// Token: 0x040004F6 RID: 1270
		k_EInputActionOrigin_XBox360_Reserved9,
		// Token: 0x040004F7 RID: 1271
		k_EInputActionOrigin_XBox360_Reserved10,
		// Token: 0x040004F8 RID: 1272
		k_EInputActionOrigin_Switch_A,
		// Token: 0x040004F9 RID: 1273
		k_EInputActionOrigin_Switch_B,
		// Token: 0x040004FA RID: 1274
		k_EInputActionOrigin_Switch_X,
		// Token: 0x040004FB RID: 1275
		k_EInputActionOrigin_Switch_Y,
		// Token: 0x040004FC RID: 1276
		k_EInputActionOrigin_Switch_LeftBumper,
		// Token: 0x040004FD RID: 1277
		k_EInputActionOrigin_Switch_RightBumper,
		// Token: 0x040004FE RID: 1278
		k_EInputActionOrigin_Switch_Plus,
		// Token: 0x040004FF RID: 1279
		k_EInputActionOrigin_Switch_Minus,
		// Token: 0x04000500 RID: 1280
		k_EInputActionOrigin_Switch_Capture,
		// Token: 0x04000501 RID: 1281
		k_EInputActionOrigin_Switch_LeftTrigger_Pull,
		// Token: 0x04000502 RID: 1282
		k_EInputActionOrigin_Switch_LeftTrigger_Click,
		// Token: 0x04000503 RID: 1283
		k_EInputActionOrigin_Switch_RightTrigger_Pull,
		// Token: 0x04000504 RID: 1284
		k_EInputActionOrigin_Switch_RightTrigger_Click,
		// Token: 0x04000505 RID: 1285
		k_EInputActionOrigin_Switch_LeftStick_Move,
		// Token: 0x04000506 RID: 1286
		k_EInputActionOrigin_Switch_LeftStick_Click,
		// Token: 0x04000507 RID: 1287
		k_EInputActionOrigin_Switch_LeftStick_DPadNorth,
		// Token: 0x04000508 RID: 1288
		k_EInputActionOrigin_Switch_LeftStick_DPadSouth,
		// Token: 0x04000509 RID: 1289
		k_EInputActionOrigin_Switch_LeftStick_DPadWest,
		// Token: 0x0400050A RID: 1290
		k_EInputActionOrigin_Switch_LeftStick_DPadEast,
		// Token: 0x0400050B RID: 1291
		k_EInputActionOrigin_Switch_RightStick_Move,
		// Token: 0x0400050C RID: 1292
		k_EInputActionOrigin_Switch_RightStick_Click,
		// Token: 0x0400050D RID: 1293
		k_EInputActionOrigin_Switch_RightStick_DPadNorth,
		// Token: 0x0400050E RID: 1294
		k_EInputActionOrigin_Switch_RightStick_DPadSouth,
		// Token: 0x0400050F RID: 1295
		k_EInputActionOrigin_Switch_RightStick_DPadWest,
		// Token: 0x04000510 RID: 1296
		k_EInputActionOrigin_Switch_RightStick_DPadEast,
		// Token: 0x04000511 RID: 1297
		k_EInputActionOrigin_Switch_DPad_North,
		// Token: 0x04000512 RID: 1298
		k_EInputActionOrigin_Switch_DPad_South,
		// Token: 0x04000513 RID: 1299
		k_EInputActionOrigin_Switch_DPad_West,
		// Token: 0x04000514 RID: 1300
		k_EInputActionOrigin_Switch_DPad_East,
		// Token: 0x04000515 RID: 1301
		k_EInputActionOrigin_Switch_ProGyro_Move,
		// Token: 0x04000516 RID: 1302
		k_EInputActionOrigin_Switch_ProGyro_Pitch,
		// Token: 0x04000517 RID: 1303
		k_EInputActionOrigin_Switch_ProGyro_Yaw,
		// Token: 0x04000518 RID: 1304
		k_EInputActionOrigin_Switch_ProGyro_Roll,
		// Token: 0x04000519 RID: 1305
		k_EInputActionOrigin_Switch_DPad_Move,
		// Token: 0x0400051A RID: 1306
		k_EInputActionOrigin_Switch_Reserved1,
		// Token: 0x0400051B RID: 1307
		k_EInputActionOrigin_Switch_Reserved2,
		// Token: 0x0400051C RID: 1308
		k_EInputActionOrigin_Switch_Reserved3,
		// Token: 0x0400051D RID: 1309
		k_EInputActionOrigin_Switch_Reserved4,
		// Token: 0x0400051E RID: 1310
		k_EInputActionOrigin_Switch_Reserved5,
		// Token: 0x0400051F RID: 1311
		k_EInputActionOrigin_Switch_Reserved6,
		// Token: 0x04000520 RID: 1312
		k_EInputActionOrigin_Switch_Reserved7,
		// Token: 0x04000521 RID: 1313
		k_EInputActionOrigin_Switch_Reserved8,
		// Token: 0x04000522 RID: 1314
		k_EInputActionOrigin_Switch_Reserved9,
		// Token: 0x04000523 RID: 1315
		k_EInputActionOrigin_Switch_Reserved10,
		// Token: 0x04000524 RID: 1316
		k_EInputActionOrigin_Switch_RightGyro_Move,
		// Token: 0x04000525 RID: 1317
		k_EInputActionOrigin_Switch_RightGyro_Pitch,
		// Token: 0x04000526 RID: 1318
		k_EInputActionOrigin_Switch_RightGyro_Yaw,
		// Token: 0x04000527 RID: 1319
		k_EInputActionOrigin_Switch_RightGyro_Roll,
		// Token: 0x04000528 RID: 1320
		k_EInputActionOrigin_Switch_LeftGyro_Move,
		// Token: 0x04000529 RID: 1321
		k_EInputActionOrigin_Switch_LeftGyro_Pitch,
		// Token: 0x0400052A RID: 1322
		k_EInputActionOrigin_Switch_LeftGyro_Yaw,
		// Token: 0x0400052B RID: 1323
		k_EInputActionOrigin_Switch_LeftGyro_Roll,
		// Token: 0x0400052C RID: 1324
		k_EInputActionOrigin_Switch_LeftGrip_Lower,
		// Token: 0x0400052D RID: 1325
		k_EInputActionOrigin_Switch_LeftGrip_Upper,
		// Token: 0x0400052E RID: 1326
		k_EInputActionOrigin_Switch_RightGrip_Lower,
		// Token: 0x0400052F RID: 1327
		k_EInputActionOrigin_Switch_RightGrip_Upper,
		// Token: 0x04000530 RID: 1328
		k_EInputActionOrigin_Switch_Reserved11,
		// Token: 0x04000531 RID: 1329
		k_EInputActionOrigin_Switch_Reserved12,
		// Token: 0x04000532 RID: 1330
		k_EInputActionOrigin_Switch_Reserved13,
		// Token: 0x04000533 RID: 1331
		k_EInputActionOrigin_Switch_Reserved14,
		// Token: 0x04000534 RID: 1332
		k_EInputActionOrigin_Switch_Reserved15,
		// Token: 0x04000535 RID: 1333
		k_EInputActionOrigin_Switch_Reserved16,
		// Token: 0x04000536 RID: 1334
		k_EInputActionOrigin_Switch_Reserved17,
		// Token: 0x04000537 RID: 1335
		k_EInputActionOrigin_Switch_Reserved18,
		// Token: 0x04000538 RID: 1336
		k_EInputActionOrigin_Switch_Reserved19,
		// Token: 0x04000539 RID: 1337
		k_EInputActionOrigin_Switch_Reserved20,
		// Token: 0x0400053A RID: 1338
		k_EInputActionOrigin_PS5_X,
		// Token: 0x0400053B RID: 1339
		k_EInputActionOrigin_PS5_Circle,
		// Token: 0x0400053C RID: 1340
		k_EInputActionOrigin_PS5_Triangle,
		// Token: 0x0400053D RID: 1341
		k_EInputActionOrigin_PS5_Square,
		// Token: 0x0400053E RID: 1342
		k_EInputActionOrigin_PS5_LeftBumper,
		// Token: 0x0400053F RID: 1343
		k_EInputActionOrigin_PS5_RightBumper,
		// Token: 0x04000540 RID: 1344
		k_EInputActionOrigin_PS5_Option,
		// Token: 0x04000541 RID: 1345
		k_EInputActionOrigin_PS5_Create,
		// Token: 0x04000542 RID: 1346
		k_EInputActionOrigin_PS5_Mute,
		// Token: 0x04000543 RID: 1347
		k_EInputActionOrigin_PS5_LeftPad_Touch,
		// Token: 0x04000544 RID: 1348
		k_EInputActionOrigin_PS5_LeftPad_Swipe,
		// Token: 0x04000545 RID: 1349
		k_EInputActionOrigin_PS5_LeftPad_Click,
		// Token: 0x04000546 RID: 1350
		k_EInputActionOrigin_PS5_LeftPad_DPadNorth,
		// Token: 0x04000547 RID: 1351
		k_EInputActionOrigin_PS5_LeftPad_DPadSouth,
		// Token: 0x04000548 RID: 1352
		k_EInputActionOrigin_PS5_LeftPad_DPadWest,
		// Token: 0x04000549 RID: 1353
		k_EInputActionOrigin_PS5_LeftPad_DPadEast,
		// Token: 0x0400054A RID: 1354
		k_EInputActionOrigin_PS5_RightPad_Touch,
		// Token: 0x0400054B RID: 1355
		k_EInputActionOrigin_PS5_RightPad_Swipe,
		// Token: 0x0400054C RID: 1356
		k_EInputActionOrigin_PS5_RightPad_Click,
		// Token: 0x0400054D RID: 1357
		k_EInputActionOrigin_PS5_RightPad_DPadNorth,
		// Token: 0x0400054E RID: 1358
		k_EInputActionOrigin_PS5_RightPad_DPadSouth,
		// Token: 0x0400054F RID: 1359
		k_EInputActionOrigin_PS5_RightPad_DPadWest,
		// Token: 0x04000550 RID: 1360
		k_EInputActionOrigin_PS5_RightPad_DPadEast,
		// Token: 0x04000551 RID: 1361
		k_EInputActionOrigin_PS5_CenterPad_Touch,
		// Token: 0x04000552 RID: 1362
		k_EInputActionOrigin_PS5_CenterPad_Swipe,
		// Token: 0x04000553 RID: 1363
		k_EInputActionOrigin_PS5_CenterPad_Click,
		// Token: 0x04000554 RID: 1364
		k_EInputActionOrigin_PS5_CenterPad_DPadNorth,
		// Token: 0x04000555 RID: 1365
		k_EInputActionOrigin_PS5_CenterPad_DPadSouth,
		// Token: 0x04000556 RID: 1366
		k_EInputActionOrigin_PS5_CenterPad_DPadWest,
		// Token: 0x04000557 RID: 1367
		k_EInputActionOrigin_PS5_CenterPad_DPadEast,
		// Token: 0x04000558 RID: 1368
		k_EInputActionOrigin_PS5_LeftTrigger_Pull,
		// Token: 0x04000559 RID: 1369
		k_EInputActionOrigin_PS5_LeftTrigger_Click,
		// Token: 0x0400055A RID: 1370
		k_EInputActionOrigin_PS5_RightTrigger_Pull,
		// Token: 0x0400055B RID: 1371
		k_EInputActionOrigin_PS5_RightTrigger_Click,
		// Token: 0x0400055C RID: 1372
		k_EInputActionOrigin_PS5_LeftStick_Move,
		// Token: 0x0400055D RID: 1373
		k_EInputActionOrigin_PS5_LeftStick_Click,
		// Token: 0x0400055E RID: 1374
		k_EInputActionOrigin_PS5_LeftStick_DPadNorth,
		// Token: 0x0400055F RID: 1375
		k_EInputActionOrigin_PS5_LeftStick_DPadSouth,
		// Token: 0x04000560 RID: 1376
		k_EInputActionOrigin_PS5_LeftStick_DPadWest,
		// Token: 0x04000561 RID: 1377
		k_EInputActionOrigin_PS5_LeftStick_DPadEast,
		// Token: 0x04000562 RID: 1378
		k_EInputActionOrigin_PS5_RightStick_Move,
		// Token: 0x04000563 RID: 1379
		k_EInputActionOrigin_PS5_RightStick_Click,
		// Token: 0x04000564 RID: 1380
		k_EInputActionOrigin_PS5_RightStick_DPadNorth,
		// Token: 0x04000565 RID: 1381
		k_EInputActionOrigin_PS5_RightStick_DPadSouth,
		// Token: 0x04000566 RID: 1382
		k_EInputActionOrigin_PS5_RightStick_DPadWest,
		// Token: 0x04000567 RID: 1383
		k_EInputActionOrigin_PS5_RightStick_DPadEast,
		// Token: 0x04000568 RID: 1384
		k_EInputActionOrigin_PS5_DPad_North,
		// Token: 0x04000569 RID: 1385
		k_EInputActionOrigin_PS5_DPad_South,
		// Token: 0x0400056A RID: 1386
		k_EInputActionOrigin_PS5_DPad_West,
		// Token: 0x0400056B RID: 1387
		k_EInputActionOrigin_PS5_DPad_East,
		// Token: 0x0400056C RID: 1388
		k_EInputActionOrigin_PS5_Gyro_Move,
		// Token: 0x0400056D RID: 1389
		k_EInputActionOrigin_PS5_Gyro_Pitch,
		// Token: 0x0400056E RID: 1390
		k_EInputActionOrigin_PS5_Gyro_Yaw,
		// Token: 0x0400056F RID: 1391
		k_EInputActionOrigin_PS5_Gyro_Roll,
		// Token: 0x04000570 RID: 1392
		k_EInputActionOrigin_PS5_DPad_Move,
		// Token: 0x04000571 RID: 1393
		k_EInputActionOrigin_PS5_Reserved1,
		// Token: 0x04000572 RID: 1394
		k_EInputActionOrigin_PS5_Reserved2,
		// Token: 0x04000573 RID: 1395
		k_EInputActionOrigin_PS5_Reserved3,
		// Token: 0x04000574 RID: 1396
		k_EInputActionOrigin_PS5_Reserved4,
		// Token: 0x04000575 RID: 1397
		k_EInputActionOrigin_PS5_Reserved5,
		// Token: 0x04000576 RID: 1398
		k_EInputActionOrigin_PS5_Reserved6,
		// Token: 0x04000577 RID: 1399
		k_EInputActionOrigin_PS5_Reserved7,
		// Token: 0x04000578 RID: 1400
		k_EInputActionOrigin_PS5_Reserved8,
		// Token: 0x04000579 RID: 1401
		k_EInputActionOrigin_PS5_Reserved9,
		// Token: 0x0400057A RID: 1402
		k_EInputActionOrigin_PS5_Reserved10,
		// Token: 0x0400057B RID: 1403
		k_EInputActionOrigin_PS5_Reserved11,
		// Token: 0x0400057C RID: 1404
		k_EInputActionOrigin_PS5_Reserved12,
		// Token: 0x0400057D RID: 1405
		k_EInputActionOrigin_PS5_Reserved13,
		// Token: 0x0400057E RID: 1406
		k_EInputActionOrigin_PS5_Reserved14,
		// Token: 0x0400057F RID: 1407
		k_EInputActionOrigin_PS5_Reserved15,
		// Token: 0x04000580 RID: 1408
		k_EInputActionOrigin_PS5_Reserved16,
		// Token: 0x04000581 RID: 1409
		k_EInputActionOrigin_PS5_Reserved17,
		// Token: 0x04000582 RID: 1410
		k_EInputActionOrigin_PS5_Reserved18,
		// Token: 0x04000583 RID: 1411
		k_EInputActionOrigin_PS5_Reserved19,
		// Token: 0x04000584 RID: 1412
		k_EInputActionOrigin_PS5_Reserved20,
		// Token: 0x04000585 RID: 1413
		k_EInputActionOrigin_SteamDeck_A,
		// Token: 0x04000586 RID: 1414
		k_EInputActionOrigin_SteamDeck_B,
		// Token: 0x04000587 RID: 1415
		k_EInputActionOrigin_SteamDeck_X,
		// Token: 0x04000588 RID: 1416
		k_EInputActionOrigin_SteamDeck_Y,
		// Token: 0x04000589 RID: 1417
		k_EInputActionOrigin_SteamDeck_L1,
		// Token: 0x0400058A RID: 1418
		k_EInputActionOrigin_SteamDeck_R1,
		// Token: 0x0400058B RID: 1419
		k_EInputActionOrigin_SteamDeck_Menu,
		// Token: 0x0400058C RID: 1420
		k_EInputActionOrigin_SteamDeck_View,
		// Token: 0x0400058D RID: 1421
		k_EInputActionOrigin_SteamDeck_LeftPad_Touch,
		// Token: 0x0400058E RID: 1422
		k_EInputActionOrigin_SteamDeck_LeftPad_Swipe,
		// Token: 0x0400058F RID: 1423
		k_EInputActionOrigin_SteamDeck_LeftPad_Click,
		// Token: 0x04000590 RID: 1424
		k_EInputActionOrigin_SteamDeck_LeftPad_DPadNorth,
		// Token: 0x04000591 RID: 1425
		k_EInputActionOrigin_SteamDeck_LeftPad_DPadSouth,
		// Token: 0x04000592 RID: 1426
		k_EInputActionOrigin_SteamDeck_LeftPad_DPadWest,
		// Token: 0x04000593 RID: 1427
		k_EInputActionOrigin_SteamDeck_LeftPad_DPadEast,
		// Token: 0x04000594 RID: 1428
		k_EInputActionOrigin_SteamDeck_RightPad_Touch,
		// Token: 0x04000595 RID: 1429
		k_EInputActionOrigin_SteamDeck_RightPad_Swipe,
		// Token: 0x04000596 RID: 1430
		k_EInputActionOrigin_SteamDeck_RightPad_Click,
		// Token: 0x04000597 RID: 1431
		k_EInputActionOrigin_SteamDeck_RightPad_DPadNorth,
		// Token: 0x04000598 RID: 1432
		k_EInputActionOrigin_SteamDeck_RightPad_DPadSouth,
		// Token: 0x04000599 RID: 1433
		k_EInputActionOrigin_SteamDeck_RightPad_DPadWest,
		// Token: 0x0400059A RID: 1434
		k_EInputActionOrigin_SteamDeck_RightPad_DPadEast,
		// Token: 0x0400059B RID: 1435
		k_EInputActionOrigin_SteamDeck_L2_SoftPull,
		// Token: 0x0400059C RID: 1436
		k_EInputActionOrigin_SteamDeck_L2,
		// Token: 0x0400059D RID: 1437
		k_EInputActionOrigin_SteamDeck_R2_SoftPull,
		// Token: 0x0400059E RID: 1438
		k_EInputActionOrigin_SteamDeck_R2,
		// Token: 0x0400059F RID: 1439
		k_EInputActionOrigin_SteamDeck_LeftStick_Move,
		// Token: 0x040005A0 RID: 1440
		k_EInputActionOrigin_SteamDeck_L3,
		// Token: 0x040005A1 RID: 1441
		k_EInputActionOrigin_SteamDeck_LeftStick_DPadNorth,
		// Token: 0x040005A2 RID: 1442
		k_EInputActionOrigin_SteamDeck_LeftStick_DPadSouth,
		// Token: 0x040005A3 RID: 1443
		k_EInputActionOrigin_SteamDeck_LeftStick_DPadWest,
		// Token: 0x040005A4 RID: 1444
		k_EInputActionOrigin_SteamDeck_LeftStick_DPadEast,
		// Token: 0x040005A5 RID: 1445
		k_EInputActionOrigin_SteamDeck_LeftStick_Touch,
		// Token: 0x040005A6 RID: 1446
		k_EInputActionOrigin_SteamDeck_RightStick_Move,
		// Token: 0x040005A7 RID: 1447
		k_EInputActionOrigin_SteamDeck_R3,
		// Token: 0x040005A8 RID: 1448
		k_EInputActionOrigin_SteamDeck_RightStick_DPadNorth,
		// Token: 0x040005A9 RID: 1449
		k_EInputActionOrigin_SteamDeck_RightStick_DPadSouth,
		// Token: 0x040005AA RID: 1450
		k_EInputActionOrigin_SteamDeck_RightStick_DPadWest,
		// Token: 0x040005AB RID: 1451
		k_EInputActionOrigin_SteamDeck_RightStick_DPadEast,
		// Token: 0x040005AC RID: 1452
		k_EInputActionOrigin_SteamDeck_RightStick_Touch,
		// Token: 0x040005AD RID: 1453
		k_EInputActionOrigin_SteamDeck_L4,
		// Token: 0x040005AE RID: 1454
		k_EInputActionOrigin_SteamDeck_R4,
		// Token: 0x040005AF RID: 1455
		k_EInputActionOrigin_SteamDeck_L5,
		// Token: 0x040005B0 RID: 1456
		k_EInputActionOrigin_SteamDeck_R5,
		// Token: 0x040005B1 RID: 1457
		k_EInputActionOrigin_SteamDeck_DPad_Move,
		// Token: 0x040005B2 RID: 1458
		k_EInputActionOrigin_SteamDeck_DPad_North,
		// Token: 0x040005B3 RID: 1459
		k_EInputActionOrigin_SteamDeck_DPad_South,
		// Token: 0x040005B4 RID: 1460
		k_EInputActionOrigin_SteamDeck_DPad_West,
		// Token: 0x040005B5 RID: 1461
		k_EInputActionOrigin_SteamDeck_DPad_East,
		// Token: 0x040005B6 RID: 1462
		k_EInputActionOrigin_SteamDeck_Gyro_Move,
		// Token: 0x040005B7 RID: 1463
		k_EInputActionOrigin_SteamDeck_Gyro_Pitch,
		// Token: 0x040005B8 RID: 1464
		k_EInputActionOrigin_SteamDeck_Gyro_Yaw,
		// Token: 0x040005B9 RID: 1465
		k_EInputActionOrigin_SteamDeck_Gyro_Roll,
		// Token: 0x040005BA RID: 1466
		k_EInputActionOrigin_SteamDeck_Reserved1,
		// Token: 0x040005BB RID: 1467
		k_EInputActionOrigin_SteamDeck_Reserved2,
		// Token: 0x040005BC RID: 1468
		k_EInputActionOrigin_SteamDeck_Reserved3,
		// Token: 0x040005BD RID: 1469
		k_EInputActionOrigin_SteamDeck_Reserved4,
		// Token: 0x040005BE RID: 1470
		k_EInputActionOrigin_SteamDeck_Reserved5,
		// Token: 0x040005BF RID: 1471
		k_EInputActionOrigin_SteamDeck_Reserved6,
		// Token: 0x040005C0 RID: 1472
		k_EInputActionOrigin_SteamDeck_Reserved7,
		// Token: 0x040005C1 RID: 1473
		k_EInputActionOrigin_SteamDeck_Reserved8,
		// Token: 0x040005C2 RID: 1474
		k_EInputActionOrigin_SteamDeck_Reserved9,
		// Token: 0x040005C3 RID: 1475
		k_EInputActionOrigin_SteamDeck_Reserved10,
		// Token: 0x040005C4 RID: 1476
		k_EInputActionOrigin_SteamDeck_Reserved11,
		// Token: 0x040005C5 RID: 1477
		k_EInputActionOrigin_SteamDeck_Reserved12,
		// Token: 0x040005C6 RID: 1478
		k_EInputActionOrigin_SteamDeck_Reserved13,
		// Token: 0x040005C7 RID: 1479
		k_EInputActionOrigin_SteamDeck_Reserved14,
		// Token: 0x040005C8 RID: 1480
		k_EInputActionOrigin_SteamDeck_Reserved15,
		// Token: 0x040005C9 RID: 1481
		k_EInputActionOrigin_SteamDeck_Reserved16,
		// Token: 0x040005CA RID: 1482
		k_EInputActionOrigin_SteamDeck_Reserved17,
		// Token: 0x040005CB RID: 1483
		k_EInputActionOrigin_SteamDeck_Reserved18,
		// Token: 0x040005CC RID: 1484
		k_EInputActionOrigin_SteamDeck_Reserved19,
		// Token: 0x040005CD RID: 1485
		k_EInputActionOrigin_SteamDeck_Reserved20,
		// Token: 0x040005CE RID: 1486
		k_EInputActionOrigin_Count,
		// Token: 0x040005CF RID: 1487
		k_EInputActionOrigin_MaximumPossibleValue = 32767
	}
}
