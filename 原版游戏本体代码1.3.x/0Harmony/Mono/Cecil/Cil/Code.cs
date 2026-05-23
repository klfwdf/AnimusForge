using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002E5 RID: 741
	internal enum Code
	{
		// Token: 0x04000799 RID: 1945
		Nop,
		// Token: 0x0400079A RID: 1946
		Break,
		// Token: 0x0400079B RID: 1947
		Ldarg_0,
		// Token: 0x0400079C RID: 1948
		Ldarg_1,
		// Token: 0x0400079D RID: 1949
		Ldarg_2,
		// Token: 0x0400079E RID: 1950
		Ldarg_3,
		// Token: 0x0400079F RID: 1951
		Ldloc_0,
		// Token: 0x040007A0 RID: 1952
		Ldloc_1,
		// Token: 0x040007A1 RID: 1953
		Ldloc_2,
		// Token: 0x040007A2 RID: 1954
		Ldloc_3,
		// Token: 0x040007A3 RID: 1955
		Stloc_0,
		// Token: 0x040007A4 RID: 1956
		Stloc_1,
		// Token: 0x040007A5 RID: 1957
		Stloc_2,
		// Token: 0x040007A6 RID: 1958
		Stloc_3,
		// Token: 0x040007A7 RID: 1959
		Ldarg_S,
		// Token: 0x040007A8 RID: 1960
		Ldarga_S,
		// Token: 0x040007A9 RID: 1961
		Starg_S,
		// Token: 0x040007AA RID: 1962
		Ldloc_S,
		// Token: 0x040007AB RID: 1963
		Ldloca_S,
		// Token: 0x040007AC RID: 1964
		Stloc_S,
		// Token: 0x040007AD RID: 1965
		Ldnull,
		// Token: 0x040007AE RID: 1966
		Ldc_I4_M1,
		// Token: 0x040007AF RID: 1967
		Ldc_I4_0,
		// Token: 0x040007B0 RID: 1968
		Ldc_I4_1,
		// Token: 0x040007B1 RID: 1969
		Ldc_I4_2,
		// Token: 0x040007B2 RID: 1970
		Ldc_I4_3,
		// Token: 0x040007B3 RID: 1971
		Ldc_I4_4,
		// Token: 0x040007B4 RID: 1972
		Ldc_I4_5,
		// Token: 0x040007B5 RID: 1973
		Ldc_I4_6,
		// Token: 0x040007B6 RID: 1974
		Ldc_I4_7,
		// Token: 0x040007B7 RID: 1975
		Ldc_I4_8,
		// Token: 0x040007B8 RID: 1976
		Ldc_I4_S,
		// Token: 0x040007B9 RID: 1977
		Ldc_I4,
		// Token: 0x040007BA RID: 1978
		Ldc_I8,
		// Token: 0x040007BB RID: 1979
		Ldc_R4,
		// Token: 0x040007BC RID: 1980
		Ldc_R8,
		// Token: 0x040007BD RID: 1981
		Dup,
		// Token: 0x040007BE RID: 1982
		Pop,
		// Token: 0x040007BF RID: 1983
		Jmp,
		// Token: 0x040007C0 RID: 1984
		Call,
		// Token: 0x040007C1 RID: 1985
		Calli,
		// Token: 0x040007C2 RID: 1986
		Ret,
		// Token: 0x040007C3 RID: 1987
		Br_S,
		// Token: 0x040007C4 RID: 1988
		Brfalse_S,
		// Token: 0x040007C5 RID: 1989
		Brtrue_S,
		// Token: 0x040007C6 RID: 1990
		Beq_S,
		// Token: 0x040007C7 RID: 1991
		Bge_S,
		// Token: 0x040007C8 RID: 1992
		Bgt_S,
		// Token: 0x040007C9 RID: 1993
		Ble_S,
		// Token: 0x040007CA RID: 1994
		Blt_S,
		// Token: 0x040007CB RID: 1995
		Bne_Un_S,
		// Token: 0x040007CC RID: 1996
		Bge_Un_S,
		// Token: 0x040007CD RID: 1997
		Bgt_Un_S,
		// Token: 0x040007CE RID: 1998
		Ble_Un_S,
		// Token: 0x040007CF RID: 1999
		Blt_Un_S,
		// Token: 0x040007D0 RID: 2000
		Br,
		// Token: 0x040007D1 RID: 2001
		Brfalse,
		// Token: 0x040007D2 RID: 2002
		Brtrue,
		// Token: 0x040007D3 RID: 2003
		Beq,
		// Token: 0x040007D4 RID: 2004
		Bge,
		// Token: 0x040007D5 RID: 2005
		Bgt,
		// Token: 0x040007D6 RID: 2006
		Ble,
		// Token: 0x040007D7 RID: 2007
		Blt,
		// Token: 0x040007D8 RID: 2008
		Bne_Un,
		// Token: 0x040007D9 RID: 2009
		Bge_Un,
		// Token: 0x040007DA RID: 2010
		Bgt_Un,
		// Token: 0x040007DB RID: 2011
		Ble_Un,
		// Token: 0x040007DC RID: 2012
		Blt_Un,
		// Token: 0x040007DD RID: 2013
		Switch,
		// Token: 0x040007DE RID: 2014
		Ldind_I1,
		// Token: 0x040007DF RID: 2015
		Ldind_U1,
		// Token: 0x040007E0 RID: 2016
		Ldind_I2,
		// Token: 0x040007E1 RID: 2017
		Ldind_U2,
		// Token: 0x040007E2 RID: 2018
		Ldind_I4,
		// Token: 0x040007E3 RID: 2019
		Ldind_U4,
		// Token: 0x040007E4 RID: 2020
		Ldind_I8,
		// Token: 0x040007E5 RID: 2021
		Ldind_I,
		// Token: 0x040007E6 RID: 2022
		Ldind_R4,
		// Token: 0x040007E7 RID: 2023
		Ldind_R8,
		// Token: 0x040007E8 RID: 2024
		Ldind_Ref,
		// Token: 0x040007E9 RID: 2025
		Stind_Ref,
		// Token: 0x040007EA RID: 2026
		Stind_I1,
		// Token: 0x040007EB RID: 2027
		Stind_I2,
		// Token: 0x040007EC RID: 2028
		Stind_I4,
		// Token: 0x040007ED RID: 2029
		Stind_I8,
		// Token: 0x040007EE RID: 2030
		Stind_R4,
		// Token: 0x040007EF RID: 2031
		Stind_R8,
		// Token: 0x040007F0 RID: 2032
		Add,
		// Token: 0x040007F1 RID: 2033
		Sub,
		// Token: 0x040007F2 RID: 2034
		Mul,
		// Token: 0x040007F3 RID: 2035
		Div,
		// Token: 0x040007F4 RID: 2036
		Div_Un,
		// Token: 0x040007F5 RID: 2037
		Rem,
		// Token: 0x040007F6 RID: 2038
		Rem_Un,
		// Token: 0x040007F7 RID: 2039
		And,
		// Token: 0x040007F8 RID: 2040
		Or,
		// Token: 0x040007F9 RID: 2041
		Xor,
		// Token: 0x040007FA RID: 2042
		Shl,
		// Token: 0x040007FB RID: 2043
		Shr,
		// Token: 0x040007FC RID: 2044
		Shr_Un,
		// Token: 0x040007FD RID: 2045
		Neg,
		// Token: 0x040007FE RID: 2046
		Not,
		// Token: 0x040007FF RID: 2047
		Conv_I1,
		// Token: 0x04000800 RID: 2048
		Conv_I2,
		// Token: 0x04000801 RID: 2049
		Conv_I4,
		// Token: 0x04000802 RID: 2050
		Conv_I8,
		// Token: 0x04000803 RID: 2051
		Conv_R4,
		// Token: 0x04000804 RID: 2052
		Conv_R8,
		// Token: 0x04000805 RID: 2053
		Conv_U4,
		// Token: 0x04000806 RID: 2054
		Conv_U8,
		// Token: 0x04000807 RID: 2055
		Callvirt,
		// Token: 0x04000808 RID: 2056
		Cpobj,
		// Token: 0x04000809 RID: 2057
		Ldobj,
		// Token: 0x0400080A RID: 2058
		Ldstr,
		// Token: 0x0400080B RID: 2059
		Newobj,
		// Token: 0x0400080C RID: 2060
		Castclass,
		// Token: 0x0400080D RID: 2061
		Isinst,
		// Token: 0x0400080E RID: 2062
		Conv_R_Un,
		// Token: 0x0400080F RID: 2063
		Unbox,
		// Token: 0x04000810 RID: 2064
		Throw,
		// Token: 0x04000811 RID: 2065
		Ldfld,
		// Token: 0x04000812 RID: 2066
		Ldflda,
		// Token: 0x04000813 RID: 2067
		Stfld,
		// Token: 0x04000814 RID: 2068
		Ldsfld,
		// Token: 0x04000815 RID: 2069
		Ldsflda,
		// Token: 0x04000816 RID: 2070
		Stsfld,
		// Token: 0x04000817 RID: 2071
		Stobj,
		// Token: 0x04000818 RID: 2072
		Conv_Ovf_I1_Un,
		// Token: 0x04000819 RID: 2073
		Conv_Ovf_I2_Un,
		// Token: 0x0400081A RID: 2074
		Conv_Ovf_I4_Un,
		// Token: 0x0400081B RID: 2075
		Conv_Ovf_I8_Un,
		// Token: 0x0400081C RID: 2076
		Conv_Ovf_U1_Un,
		// Token: 0x0400081D RID: 2077
		Conv_Ovf_U2_Un,
		// Token: 0x0400081E RID: 2078
		Conv_Ovf_U4_Un,
		// Token: 0x0400081F RID: 2079
		Conv_Ovf_U8_Un,
		// Token: 0x04000820 RID: 2080
		Conv_Ovf_I_Un,
		// Token: 0x04000821 RID: 2081
		Conv_Ovf_U_Un,
		// Token: 0x04000822 RID: 2082
		Box,
		// Token: 0x04000823 RID: 2083
		Newarr,
		// Token: 0x04000824 RID: 2084
		Ldlen,
		// Token: 0x04000825 RID: 2085
		Ldelema,
		// Token: 0x04000826 RID: 2086
		Ldelem_I1,
		// Token: 0x04000827 RID: 2087
		Ldelem_U1,
		// Token: 0x04000828 RID: 2088
		Ldelem_I2,
		// Token: 0x04000829 RID: 2089
		Ldelem_U2,
		// Token: 0x0400082A RID: 2090
		Ldelem_I4,
		// Token: 0x0400082B RID: 2091
		Ldelem_U4,
		// Token: 0x0400082C RID: 2092
		Ldelem_I8,
		// Token: 0x0400082D RID: 2093
		Ldelem_I,
		// Token: 0x0400082E RID: 2094
		Ldelem_R4,
		// Token: 0x0400082F RID: 2095
		Ldelem_R8,
		// Token: 0x04000830 RID: 2096
		Ldelem_Ref,
		// Token: 0x04000831 RID: 2097
		Stelem_I,
		// Token: 0x04000832 RID: 2098
		Stelem_I1,
		// Token: 0x04000833 RID: 2099
		Stelem_I2,
		// Token: 0x04000834 RID: 2100
		Stelem_I4,
		// Token: 0x04000835 RID: 2101
		Stelem_I8,
		// Token: 0x04000836 RID: 2102
		Stelem_R4,
		// Token: 0x04000837 RID: 2103
		Stelem_R8,
		// Token: 0x04000838 RID: 2104
		Stelem_Ref,
		// Token: 0x04000839 RID: 2105
		Ldelem_Any,
		// Token: 0x0400083A RID: 2106
		Stelem_Any,
		// Token: 0x0400083B RID: 2107
		Unbox_Any,
		// Token: 0x0400083C RID: 2108
		Conv_Ovf_I1,
		// Token: 0x0400083D RID: 2109
		Conv_Ovf_U1,
		// Token: 0x0400083E RID: 2110
		Conv_Ovf_I2,
		// Token: 0x0400083F RID: 2111
		Conv_Ovf_U2,
		// Token: 0x04000840 RID: 2112
		Conv_Ovf_I4,
		// Token: 0x04000841 RID: 2113
		Conv_Ovf_U4,
		// Token: 0x04000842 RID: 2114
		Conv_Ovf_I8,
		// Token: 0x04000843 RID: 2115
		Conv_Ovf_U8,
		// Token: 0x04000844 RID: 2116
		Refanyval,
		// Token: 0x04000845 RID: 2117
		Ckfinite,
		// Token: 0x04000846 RID: 2118
		Mkrefany,
		// Token: 0x04000847 RID: 2119
		Ldtoken,
		// Token: 0x04000848 RID: 2120
		Conv_U2,
		// Token: 0x04000849 RID: 2121
		Conv_U1,
		// Token: 0x0400084A RID: 2122
		Conv_I,
		// Token: 0x0400084B RID: 2123
		Conv_Ovf_I,
		// Token: 0x0400084C RID: 2124
		Conv_Ovf_U,
		// Token: 0x0400084D RID: 2125
		Add_Ovf,
		// Token: 0x0400084E RID: 2126
		Add_Ovf_Un,
		// Token: 0x0400084F RID: 2127
		Mul_Ovf,
		// Token: 0x04000850 RID: 2128
		Mul_Ovf_Un,
		// Token: 0x04000851 RID: 2129
		Sub_Ovf,
		// Token: 0x04000852 RID: 2130
		Sub_Ovf_Un,
		// Token: 0x04000853 RID: 2131
		Endfinally,
		// Token: 0x04000854 RID: 2132
		Leave,
		// Token: 0x04000855 RID: 2133
		Leave_S,
		// Token: 0x04000856 RID: 2134
		Stind_I,
		// Token: 0x04000857 RID: 2135
		Conv_U,
		// Token: 0x04000858 RID: 2136
		Arglist,
		// Token: 0x04000859 RID: 2137
		Ceq,
		// Token: 0x0400085A RID: 2138
		Cgt,
		// Token: 0x0400085B RID: 2139
		Cgt_Un,
		// Token: 0x0400085C RID: 2140
		Clt,
		// Token: 0x0400085D RID: 2141
		Clt_Un,
		// Token: 0x0400085E RID: 2142
		Ldftn,
		// Token: 0x0400085F RID: 2143
		Ldvirtftn,
		// Token: 0x04000860 RID: 2144
		Ldarg,
		// Token: 0x04000861 RID: 2145
		Ldarga,
		// Token: 0x04000862 RID: 2146
		Starg,
		// Token: 0x04000863 RID: 2147
		Ldloc,
		// Token: 0x04000864 RID: 2148
		Ldloca,
		// Token: 0x04000865 RID: 2149
		Stloc,
		// Token: 0x04000866 RID: 2150
		Localloc,
		// Token: 0x04000867 RID: 2151
		Endfilter,
		// Token: 0x04000868 RID: 2152
		Unaligned,
		// Token: 0x04000869 RID: 2153
		Volatile,
		// Token: 0x0400086A RID: 2154
		Tail,
		// Token: 0x0400086B RID: 2155
		Initobj,
		// Token: 0x0400086C RID: 2156
		Constrained,
		// Token: 0x0400086D RID: 2157
		Cpblk,
		// Token: 0x0400086E RID: 2158
		Initblk,
		// Token: 0x0400086F RID: 2159
		No,
		// Token: 0x04000870 RID: 2160
		Rethrow,
		// Token: 0x04000871 RID: 2161
		Sizeof,
		// Token: 0x04000872 RID: 2162
		Refanytype,
		// Token: 0x04000873 RID: 2163
		Readonly
	}
}
