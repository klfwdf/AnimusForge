using System;

namespace Iced.Intel
{
	// Token: 0x0200065B RID: 1627
	internal enum MemorySize
	{
		// Token: 0x04002B02 RID: 11010
		Unknown,
		// Token: 0x04002B03 RID: 11011
		UInt8,
		// Token: 0x04002B04 RID: 11012
		UInt16,
		// Token: 0x04002B05 RID: 11013
		UInt32,
		// Token: 0x04002B06 RID: 11014
		UInt52,
		// Token: 0x04002B07 RID: 11015
		UInt64,
		// Token: 0x04002B08 RID: 11016
		UInt128,
		// Token: 0x04002B09 RID: 11017
		UInt256,
		// Token: 0x04002B0A RID: 11018
		UInt512,
		// Token: 0x04002B0B RID: 11019
		Int8,
		// Token: 0x04002B0C RID: 11020
		Int16,
		// Token: 0x04002B0D RID: 11021
		Int32,
		// Token: 0x04002B0E RID: 11022
		Int64,
		// Token: 0x04002B0F RID: 11023
		Int128,
		// Token: 0x04002B10 RID: 11024
		Int256,
		// Token: 0x04002B11 RID: 11025
		Int512,
		// Token: 0x04002B12 RID: 11026
		SegPtr16,
		// Token: 0x04002B13 RID: 11027
		SegPtr32,
		// Token: 0x04002B14 RID: 11028
		SegPtr64,
		// Token: 0x04002B15 RID: 11029
		WordOffset,
		// Token: 0x04002B16 RID: 11030
		DwordOffset,
		// Token: 0x04002B17 RID: 11031
		QwordOffset,
		// Token: 0x04002B18 RID: 11032
		Bound16_WordWord,
		// Token: 0x04002B19 RID: 11033
		Bound32_DwordDword,
		// Token: 0x04002B1A RID: 11034
		Bnd32,
		// Token: 0x04002B1B RID: 11035
		Bnd64,
		// Token: 0x04002B1C RID: 11036
		Fword6,
		// Token: 0x04002B1D RID: 11037
		Fword10,
		// Token: 0x04002B1E RID: 11038
		Float16,
		// Token: 0x04002B1F RID: 11039
		Float32,
		// Token: 0x04002B20 RID: 11040
		Float64,
		// Token: 0x04002B21 RID: 11041
		Float80,
		// Token: 0x04002B22 RID: 11042
		Float128,
		// Token: 0x04002B23 RID: 11043
		BFloat16,
		// Token: 0x04002B24 RID: 11044
		FpuEnv14,
		// Token: 0x04002B25 RID: 11045
		FpuEnv28,
		// Token: 0x04002B26 RID: 11046
		FpuState94,
		// Token: 0x04002B27 RID: 11047
		FpuState108,
		// Token: 0x04002B28 RID: 11048
		Fxsave_512Byte,
		// Token: 0x04002B29 RID: 11049
		Fxsave64_512Byte,
		// Token: 0x04002B2A RID: 11050
		Xsave,
		// Token: 0x04002B2B RID: 11051
		Xsave64,
		// Token: 0x04002B2C RID: 11052
		Bcd,
		// Token: 0x04002B2D RID: 11053
		Tilecfg,
		// Token: 0x04002B2E RID: 11054
		Tile,
		// Token: 0x04002B2F RID: 11055
		SegmentDescSelector,
		// Token: 0x04002B30 RID: 11056
		KLHandleAes128,
		// Token: 0x04002B31 RID: 11057
		KLHandleAes256,
		// Token: 0x04002B32 RID: 11058
		Packed16_UInt8,
		// Token: 0x04002B33 RID: 11059
		Packed16_Int8,
		// Token: 0x04002B34 RID: 11060
		Packed32_UInt8,
		// Token: 0x04002B35 RID: 11061
		Packed32_Int8,
		// Token: 0x04002B36 RID: 11062
		Packed32_UInt16,
		// Token: 0x04002B37 RID: 11063
		Packed32_Int16,
		// Token: 0x04002B38 RID: 11064
		Packed32_Float16,
		// Token: 0x04002B39 RID: 11065
		Packed32_BFloat16,
		// Token: 0x04002B3A RID: 11066
		Packed64_UInt8,
		// Token: 0x04002B3B RID: 11067
		Packed64_Int8,
		// Token: 0x04002B3C RID: 11068
		Packed64_UInt16,
		// Token: 0x04002B3D RID: 11069
		Packed64_Int16,
		// Token: 0x04002B3E RID: 11070
		Packed64_UInt32,
		// Token: 0x04002B3F RID: 11071
		Packed64_Int32,
		// Token: 0x04002B40 RID: 11072
		Packed64_Float16,
		// Token: 0x04002B41 RID: 11073
		Packed64_Float32,
		// Token: 0x04002B42 RID: 11074
		Packed128_UInt8,
		// Token: 0x04002B43 RID: 11075
		Packed128_Int8,
		// Token: 0x04002B44 RID: 11076
		Packed128_UInt16,
		// Token: 0x04002B45 RID: 11077
		Packed128_Int16,
		// Token: 0x04002B46 RID: 11078
		Packed128_UInt32,
		// Token: 0x04002B47 RID: 11079
		Packed128_Int32,
		// Token: 0x04002B48 RID: 11080
		Packed128_UInt52,
		// Token: 0x04002B49 RID: 11081
		Packed128_UInt64,
		// Token: 0x04002B4A RID: 11082
		Packed128_Int64,
		// Token: 0x04002B4B RID: 11083
		Packed128_Float16,
		// Token: 0x04002B4C RID: 11084
		Packed128_Float32,
		// Token: 0x04002B4D RID: 11085
		Packed128_Float64,
		// Token: 0x04002B4E RID: 11086
		Packed128_BFloat16,
		// Token: 0x04002B4F RID: 11087
		Packed128_2xFloat16,
		// Token: 0x04002B50 RID: 11088
		Packed128_2xBFloat16,
		// Token: 0x04002B51 RID: 11089
		Packed256_UInt8,
		// Token: 0x04002B52 RID: 11090
		Packed256_Int8,
		// Token: 0x04002B53 RID: 11091
		Packed256_UInt16,
		// Token: 0x04002B54 RID: 11092
		Packed256_Int16,
		// Token: 0x04002B55 RID: 11093
		Packed256_UInt32,
		// Token: 0x04002B56 RID: 11094
		Packed256_Int32,
		// Token: 0x04002B57 RID: 11095
		Packed256_UInt52,
		// Token: 0x04002B58 RID: 11096
		Packed256_UInt64,
		// Token: 0x04002B59 RID: 11097
		Packed256_Int64,
		// Token: 0x04002B5A RID: 11098
		Packed256_UInt128,
		// Token: 0x04002B5B RID: 11099
		Packed256_Int128,
		// Token: 0x04002B5C RID: 11100
		Packed256_Float16,
		// Token: 0x04002B5D RID: 11101
		Packed256_Float32,
		// Token: 0x04002B5E RID: 11102
		Packed256_Float64,
		// Token: 0x04002B5F RID: 11103
		Packed256_Float128,
		// Token: 0x04002B60 RID: 11104
		Packed256_BFloat16,
		// Token: 0x04002B61 RID: 11105
		Packed256_2xFloat16,
		// Token: 0x04002B62 RID: 11106
		Packed256_2xBFloat16,
		// Token: 0x04002B63 RID: 11107
		Packed512_UInt8,
		// Token: 0x04002B64 RID: 11108
		Packed512_Int8,
		// Token: 0x04002B65 RID: 11109
		Packed512_UInt16,
		// Token: 0x04002B66 RID: 11110
		Packed512_Int16,
		// Token: 0x04002B67 RID: 11111
		Packed512_UInt32,
		// Token: 0x04002B68 RID: 11112
		Packed512_Int32,
		// Token: 0x04002B69 RID: 11113
		Packed512_UInt52,
		// Token: 0x04002B6A RID: 11114
		Packed512_UInt64,
		// Token: 0x04002B6B RID: 11115
		Packed512_Int64,
		// Token: 0x04002B6C RID: 11116
		Packed512_UInt128,
		// Token: 0x04002B6D RID: 11117
		Packed512_Float16,
		// Token: 0x04002B6E RID: 11118
		Packed512_Float32,
		// Token: 0x04002B6F RID: 11119
		Packed512_Float64,
		// Token: 0x04002B70 RID: 11120
		Packed512_2xFloat16,
		// Token: 0x04002B71 RID: 11121
		Packed512_2xBFloat16,
		// Token: 0x04002B72 RID: 11122
		Broadcast32_Float16,
		// Token: 0x04002B73 RID: 11123
		Broadcast64_UInt32,
		// Token: 0x04002B74 RID: 11124
		Broadcast64_Int32,
		// Token: 0x04002B75 RID: 11125
		Broadcast64_Float16,
		// Token: 0x04002B76 RID: 11126
		Broadcast64_Float32,
		// Token: 0x04002B77 RID: 11127
		Broadcast128_Int16,
		// Token: 0x04002B78 RID: 11128
		Broadcast128_UInt16,
		// Token: 0x04002B79 RID: 11129
		Broadcast128_UInt32,
		// Token: 0x04002B7A RID: 11130
		Broadcast128_Int32,
		// Token: 0x04002B7B RID: 11131
		Broadcast128_UInt52,
		// Token: 0x04002B7C RID: 11132
		Broadcast128_UInt64,
		// Token: 0x04002B7D RID: 11133
		Broadcast128_Int64,
		// Token: 0x04002B7E RID: 11134
		Broadcast128_Float16,
		// Token: 0x04002B7F RID: 11135
		Broadcast128_Float32,
		// Token: 0x04002B80 RID: 11136
		Broadcast128_Float64,
		// Token: 0x04002B81 RID: 11137
		Broadcast128_2xInt16,
		// Token: 0x04002B82 RID: 11138
		Broadcast128_2xInt32,
		// Token: 0x04002B83 RID: 11139
		Broadcast128_2xUInt32,
		// Token: 0x04002B84 RID: 11140
		Broadcast128_2xFloat16,
		// Token: 0x04002B85 RID: 11141
		Broadcast128_2xBFloat16,
		// Token: 0x04002B86 RID: 11142
		Broadcast256_Int16,
		// Token: 0x04002B87 RID: 11143
		Broadcast256_UInt16,
		// Token: 0x04002B88 RID: 11144
		Broadcast256_UInt32,
		// Token: 0x04002B89 RID: 11145
		Broadcast256_Int32,
		// Token: 0x04002B8A RID: 11146
		Broadcast256_UInt52,
		// Token: 0x04002B8B RID: 11147
		Broadcast256_UInt64,
		// Token: 0x04002B8C RID: 11148
		Broadcast256_Int64,
		// Token: 0x04002B8D RID: 11149
		Broadcast256_Float16,
		// Token: 0x04002B8E RID: 11150
		Broadcast256_Float32,
		// Token: 0x04002B8F RID: 11151
		Broadcast256_Float64,
		// Token: 0x04002B90 RID: 11152
		Broadcast256_2xInt16,
		// Token: 0x04002B91 RID: 11153
		Broadcast256_2xInt32,
		// Token: 0x04002B92 RID: 11154
		Broadcast256_2xUInt32,
		// Token: 0x04002B93 RID: 11155
		Broadcast256_2xFloat16,
		// Token: 0x04002B94 RID: 11156
		Broadcast256_2xBFloat16,
		// Token: 0x04002B95 RID: 11157
		Broadcast512_Int16,
		// Token: 0x04002B96 RID: 11158
		Broadcast512_UInt16,
		// Token: 0x04002B97 RID: 11159
		Broadcast512_UInt32,
		// Token: 0x04002B98 RID: 11160
		Broadcast512_Int32,
		// Token: 0x04002B99 RID: 11161
		Broadcast512_UInt52,
		// Token: 0x04002B9A RID: 11162
		Broadcast512_UInt64,
		// Token: 0x04002B9B RID: 11163
		Broadcast512_Int64,
		// Token: 0x04002B9C RID: 11164
		Broadcast512_Float16,
		// Token: 0x04002B9D RID: 11165
		Broadcast512_Float32,
		// Token: 0x04002B9E RID: 11166
		Broadcast512_Float64,
		// Token: 0x04002B9F RID: 11167
		Broadcast512_2xFloat16,
		// Token: 0x04002BA0 RID: 11168
		Broadcast512_2xInt16,
		// Token: 0x04002BA1 RID: 11169
		Broadcast512_2xUInt32,
		// Token: 0x04002BA2 RID: 11170
		Broadcast512_2xInt32,
		// Token: 0x04002BA3 RID: 11171
		Broadcast512_2xBFloat16
	}
}
