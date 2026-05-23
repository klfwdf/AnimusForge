using System;

namespace Iced.Intel
{
	// Token: 0x02000632 RID: 1586
	internal enum Code
	{
		// Token: 0x0400169A RID: 5786
		INVALID,
		// Token: 0x0400169B RID: 5787
		DeclareByte,
		// Token: 0x0400169C RID: 5788
		DeclareWord,
		// Token: 0x0400169D RID: 5789
		DeclareDword,
		// Token: 0x0400169E RID: 5790
		DeclareQword,
		// Token: 0x0400169F RID: 5791
		Add_rm8_r8,
		// Token: 0x040016A0 RID: 5792
		Add_rm16_r16,
		// Token: 0x040016A1 RID: 5793
		Add_rm32_r32,
		// Token: 0x040016A2 RID: 5794
		Add_rm64_r64,
		// Token: 0x040016A3 RID: 5795
		Add_r8_rm8,
		// Token: 0x040016A4 RID: 5796
		Add_r16_rm16,
		// Token: 0x040016A5 RID: 5797
		Add_r32_rm32,
		// Token: 0x040016A6 RID: 5798
		Add_r64_rm64,
		// Token: 0x040016A7 RID: 5799
		Add_AL_imm8,
		// Token: 0x040016A8 RID: 5800
		Add_AX_imm16,
		// Token: 0x040016A9 RID: 5801
		Add_EAX_imm32,
		// Token: 0x040016AA RID: 5802
		Add_RAX_imm32,
		// Token: 0x040016AB RID: 5803
		Pushw_ES,
		// Token: 0x040016AC RID: 5804
		Pushd_ES,
		// Token: 0x040016AD RID: 5805
		Popw_ES,
		// Token: 0x040016AE RID: 5806
		Popd_ES,
		// Token: 0x040016AF RID: 5807
		Or_rm8_r8,
		// Token: 0x040016B0 RID: 5808
		Or_rm16_r16,
		// Token: 0x040016B1 RID: 5809
		Or_rm32_r32,
		// Token: 0x040016B2 RID: 5810
		Or_rm64_r64,
		// Token: 0x040016B3 RID: 5811
		Or_r8_rm8,
		// Token: 0x040016B4 RID: 5812
		Or_r16_rm16,
		// Token: 0x040016B5 RID: 5813
		Or_r32_rm32,
		// Token: 0x040016B6 RID: 5814
		Or_r64_rm64,
		// Token: 0x040016B7 RID: 5815
		Or_AL_imm8,
		// Token: 0x040016B8 RID: 5816
		Or_AX_imm16,
		// Token: 0x040016B9 RID: 5817
		Or_EAX_imm32,
		// Token: 0x040016BA RID: 5818
		Or_RAX_imm32,
		// Token: 0x040016BB RID: 5819
		Pushw_CS,
		// Token: 0x040016BC RID: 5820
		Pushd_CS,
		// Token: 0x040016BD RID: 5821
		Popw_CS,
		// Token: 0x040016BE RID: 5822
		Adc_rm8_r8,
		// Token: 0x040016BF RID: 5823
		Adc_rm16_r16,
		// Token: 0x040016C0 RID: 5824
		Adc_rm32_r32,
		// Token: 0x040016C1 RID: 5825
		Adc_rm64_r64,
		// Token: 0x040016C2 RID: 5826
		Adc_r8_rm8,
		// Token: 0x040016C3 RID: 5827
		Adc_r16_rm16,
		// Token: 0x040016C4 RID: 5828
		Adc_r32_rm32,
		// Token: 0x040016C5 RID: 5829
		Adc_r64_rm64,
		// Token: 0x040016C6 RID: 5830
		Adc_AL_imm8,
		// Token: 0x040016C7 RID: 5831
		Adc_AX_imm16,
		// Token: 0x040016C8 RID: 5832
		Adc_EAX_imm32,
		// Token: 0x040016C9 RID: 5833
		Adc_RAX_imm32,
		// Token: 0x040016CA RID: 5834
		Pushw_SS,
		// Token: 0x040016CB RID: 5835
		Pushd_SS,
		// Token: 0x040016CC RID: 5836
		Popw_SS,
		// Token: 0x040016CD RID: 5837
		Popd_SS,
		// Token: 0x040016CE RID: 5838
		Sbb_rm8_r8,
		// Token: 0x040016CF RID: 5839
		Sbb_rm16_r16,
		// Token: 0x040016D0 RID: 5840
		Sbb_rm32_r32,
		// Token: 0x040016D1 RID: 5841
		Sbb_rm64_r64,
		// Token: 0x040016D2 RID: 5842
		Sbb_r8_rm8,
		// Token: 0x040016D3 RID: 5843
		Sbb_r16_rm16,
		// Token: 0x040016D4 RID: 5844
		Sbb_r32_rm32,
		// Token: 0x040016D5 RID: 5845
		Sbb_r64_rm64,
		// Token: 0x040016D6 RID: 5846
		Sbb_AL_imm8,
		// Token: 0x040016D7 RID: 5847
		Sbb_AX_imm16,
		// Token: 0x040016D8 RID: 5848
		Sbb_EAX_imm32,
		// Token: 0x040016D9 RID: 5849
		Sbb_RAX_imm32,
		// Token: 0x040016DA RID: 5850
		Pushw_DS,
		// Token: 0x040016DB RID: 5851
		Pushd_DS,
		// Token: 0x040016DC RID: 5852
		Popw_DS,
		// Token: 0x040016DD RID: 5853
		Popd_DS,
		// Token: 0x040016DE RID: 5854
		And_rm8_r8,
		// Token: 0x040016DF RID: 5855
		And_rm16_r16,
		// Token: 0x040016E0 RID: 5856
		And_rm32_r32,
		// Token: 0x040016E1 RID: 5857
		And_rm64_r64,
		// Token: 0x040016E2 RID: 5858
		And_r8_rm8,
		// Token: 0x040016E3 RID: 5859
		And_r16_rm16,
		// Token: 0x040016E4 RID: 5860
		And_r32_rm32,
		// Token: 0x040016E5 RID: 5861
		And_r64_rm64,
		// Token: 0x040016E6 RID: 5862
		And_AL_imm8,
		// Token: 0x040016E7 RID: 5863
		And_AX_imm16,
		// Token: 0x040016E8 RID: 5864
		And_EAX_imm32,
		// Token: 0x040016E9 RID: 5865
		And_RAX_imm32,
		// Token: 0x040016EA RID: 5866
		Daa,
		// Token: 0x040016EB RID: 5867
		Sub_rm8_r8,
		// Token: 0x040016EC RID: 5868
		Sub_rm16_r16,
		// Token: 0x040016ED RID: 5869
		Sub_rm32_r32,
		// Token: 0x040016EE RID: 5870
		Sub_rm64_r64,
		// Token: 0x040016EF RID: 5871
		Sub_r8_rm8,
		// Token: 0x040016F0 RID: 5872
		Sub_r16_rm16,
		// Token: 0x040016F1 RID: 5873
		Sub_r32_rm32,
		// Token: 0x040016F2 RID: 5874
		Sub_r64_rm64,
		// Token: 0x040016F3 RID: 5875
		Sub_AL_imm8,
		// Token: 0x040016F4 RID: 5876
		Sub_AX_imm16,
		// Token: 0x040016F5 RID: 5877
		Sub_EAX_imm32,
		// Token: 0x040016F6 RID: 5878
		Sub_RAX_imm32,
		// Token: 0x040016F7 RID: 5879
		Das,
		// Token: 0x040016F8 RID: 5880
		Xor_rm8_r8,
		// Token: 0x040016F9 RID: 5881
		Xor_rm16_r16,
		// Token: 0x040016FA RID: 5882
		Xor_rm32_r32,
		// Token: 0x040016FB RID: 5883
		Xor_rm64_r64,
		// Token: 0x040016FC RID: 5884
		Xor_r8_rm8,
		// Token: 0x040016FD RID: 5885
		Xor_r16_rm16,
		// Token: 0x040016FE RID: 5886
		Xor_r32_rm32,
		// Token: 0x040016FF RID: 5887
		Xor_r64_rm64,
		// Token: 0x04001700 RID: 5888
		Xor_AL_imm8,
		// Token: 0x04001701 RID: 5889
		Xor_AX_imm16,
		// Token: 0x04001702 RID: 5890
		Xor_EAX_imm32,
		// Token: 0x04001703 RID: 5891
		Xor_RAX_imm32,
		// Token: 0x04001704 RID: 5892
		Aaa,
		// Token: 0x04001705 RID: 5893
		Cmp_rm8_r8,
		// Token: 0x04001706 RID: 5894
		Cmp_rm16_r16,
		// Token: 0x04001707 RID: 5895
		Cmp_rm32_r32,
		// Token: 0x04001708 RID: 5896
		Cmp_rm64_r64,
		// Token: 0x04001709 RID: 5897
		Cmp_r8_rm8,
		// Token: 0x0400170A RID: 5898
		Cmp_r16_rm16,
		// Token: 0x0400170B RID: 5899
		Cmp_r32_rm32,
		// Token: 0x0400170C RID: 5900
		Cmp_r64_rm64,
		// Token: 0x0400170D RID: 5901
		Cmp_AL_imm8,
		// Token: 0x0400170E RID: 5902
		Cmp_AX_imm16,
		// Token: 0x0400170F RID: 5903
		Cmp_EAX_imm32,
		// Token: 0x04001710 RID: 5904
		Cmp_RAX_imm32,
		// Token: 0x04001711 RID: 5905
		Aas,
		// Token: 0x04001712 RID: 5906
		Inc_r16,
		// Token: 0x04001713 RID: 5907
		Inc_r32,
		// Token: 0x04001714 RID: 5908
		Dec_r16,
		// Token: 0x04001715 RID: 5909
		Dec_r32,
		// Token: 0x04001716 RID: 5910
		Push_r16,
		// Token: 0x04001717 RID: 5911
		Push_r32,
		// Token: 0x04001718 RID: 5912
		Push_r64,
		// Token: 0x04001719 RID: 5913
		Pop_r16,
		// Token: 0x0400171A RID: 5914
		Pop_r32,
		// Token: 0x0400171B RID: 5915
		Pop_r64,
		// Token: 0x0400171C RID: 5916
		Pushaw,
		// Token: 0x0400171D RID: 5917
		Pushad,
		// Token: 0x0400171E RID: 5918
		Popaw,
		// Token: 0x0400171F RID: 5919
		Popad,
		// Token: 0x04001720 RID: 5920
		Bound_r16_m1616,
		// Token: 0x04001721 RID: 5921
		Bound_r32_m3232,
		// Token: 0x04001722 RID: 5922
		Arpl_rm16_r16,
		// Token: 0x04001723 RID: 5923
		Arpl_r32m16_r32,
		// Token: 0x04001724 RID: 5924
		Movsxd_r16_rm16,
		// Token: 0x04001725 RID: 5925
		Movsxd_r32_rm32,
		// Token: 0x04001726 RID: 5926
		Movsxd_r64_rm32,
		// Token: 0x04001727 RID: 5927
		Push_imm16,
		// Token: 0x04001728 RID: 5928
		Pushd_imm32,
		// Token: 0x04001729 RID: 5929
		Pushq_imm32,
		// Token: 0x0400172A RID: 5930
		Imul_r16_rm16_imm16,
		// Token: 0x0400172B RID: 5931
		Imul_r32_rm32_imm32,
		// Token: 0x0400172C RID: 5932
		Imul_r64_rm64_imm32,
		// Token: 0x0400172D RID: 5933
		Pushw_imm8,
		// Token: 0x0400172E RID: 5934
		Pushd_imm8,
		// Token: 0x0400172F RID: 5935
		Pushq_imm8,
		// Token: 0x04001730 RID: 5936
		Imul_r16_rm16_imm8,
		// Token: 0x04001731 RID: 5937
		Imul_r32_rm32_imm8,
		// Token: 0x04001732 RID: 5938
		Imul_r64_rm64_imm8,
		// Token: 0x04001733 RID: 5939
		Insb_m8_DX,
		// Token: 0x04001734 RID: 5940
		Insw_m16_DX,
		// Token: 0x04001735 RID: 5941
		Insd_m32_DX,
		// Token: 0x04001736 RID: 5942
		Outsb_DX_m8,
		// Token: 0x04001737 RID: 5943
		Outsw_DX_m16,
		// Token: 0x04001738 RID: 5944
		Outsd_DX_m32,
		// Token: 0x04001739 RID: 5945
		Jo_rel8_16,
		// Token: 0x0400173A RID: 5946
		Jo_rel8_32,
		// Token: 0x0400173B RID: 5947
		Jo_rel8_64,
		// Token: 0x0400173C RID: 5948
		Jno_rel8_16,
		// Token: 0x0400173D RID: 5949
		Jno_rel8_32,
		// Token: 0x0400173E RID: 5950
		Jno_rel8_64,
		// Token: 0x0400173F RID: 5951
		Jb_rel8_16,
		// Token: 0x04001740 RID: 5952
		Jb_rel8_32,
		// Token: 0x04001741 RID: 5953
		Jb_rel8_64,
		// Token: 0x04001742 RID: 5954
		Jae_rel8_16,
		// Token: 0x04001743 RID: 5955
		Jae_rel8_32,
		// Token: 0x04001744 RID: 5956
		Jae_rel8_64,
		// Token: 0x04001745 RID: 5957
		Je_rel8_16,
		// Token: 0x04001746 RID: 5958
		Je_rel8_32,
		// Token: 0x04001747 RID: 5959
		Je_rel8_64,
		// Token: 0x04001748 RID: 5960
		Jne_rel8_16,
		// Token: 0x04001749 RID: 5961
		Jne_rel8_32,
		// Token: 0x0400174A RID: 5962
		Jne_rel8_64,
		// Token: 0x0400174B RID: 5963
		Jbe_rel8_16,
		// Token: 0x0400174C RID: 5964
		Jbe_rel8_32,
		// Token: 0x0400174D RID: 5965
		Jbe_rel8_64,
		// Token: 0x0400174E RID: 5966
		Ja_rel8_16,
		// Token: 0x0400174F RID: 5967
		Ja_rel8_32,
		// Token: 0x04001750 RID: 5968
		Ja_rel8_64,
		// Token: 0x04001751 RID: 5969
		Js_rel8_16,
		// Token: 0x04001752 RID: 5970
		Js_rel8_32,
		// Token: 0x04001753 RID: 5971
		Js_rel8_64,
		// Token: 0x04001754 RID: 5972
		Jns_rel8_16,
		// Token: 0x04001755 RID: 5973
		Jns_rel8_32,
		// Token: 0x04001756 RID: 5974
		Jns_rel8_64,
		// Token: 0x04001757 RID: 5975
		Jp_rel8_16,
		// Token: 0x04001758 RID: 5976
		Jp_rel8_32,
		// Token: 0x04001759 RID: 5977
		Jp_rel8_64,
		// Token: 0x0400175A RID: 5978
		Jnp_rel8_16,
		// Token: 0x0400175B RID: 5979
		Jnp_rel8_32,
		// Token: 0x0400175C RID: 5980
		Jnp_rel8_64,
		// Token: 0x0400175D RID: 5981
		Jl_rel8_16,
		// Token: 0x0400175E RID: 5982
		Jl_rel8_32,
		// Token: 0x0400175F RID: 5983
		Jl_rel8_64,
		// Token: 0x04001760 RID: 5984
		Jge_rel8_16,
		// Token: 0x04001761 RID: 5985
		Jge_rel8_32,
		// Token: 0x04001762 RID: 5986
		Jge_rel8_64,
		// Token: 0x04001763 RID: 5987
		Jle_rel8_16,
		// Token: 0x04001764 RID: 5988
		Jle_rel8_32,
		// Token: 0x04001765 RID: 5989
		Jle_rel8_64,
		// Token: 0x04001766 RID: 5990
		Jg_rel8_16,
		// Token: 0x04001767 RID: 5991
		Jg_rel8_32,
		// Token: 0x04001768 RID: 5992
		Jg_rel8_64,
		// Token: 0x04001769 RID: 5993
		Add_rm8_imm8,
		// Token: 0x0400176A RID: 5994
		Or_rm8_imm8,
		// Token: 0x0400176B RID: 5995
		Adc_rm8_imm8,
		// Token: 0x0400176C RID: 5996
		Sbb_rm8_imm8,
		// Token: 0x0400176D RID: 5997
		And_rm8_imm8,
		// Token: 0x0400176E RID: 5998
		Sub_rm8_imm8,
		// Token: 0x0400176F RID: 5999
		Xor_rm8_imm8,
		// Token: 0x04001770 RID: 6000
		Cmp_rm8_imm8,
		// Token: 0x04001771 RID: 6001
		Add_rm16_imm16,
		// Token: 0x04001772 RID: 6002
		Add_rm32_imm32,
		// Token: 0x04001773 RID: 6003
		Add_rm64_imm32,
		// Token: 0x04001774 RID: 6004
		Or_rm16_imm16,
		// Token: 0x04001775 RID: 6005
		Or_rm32_imm32,
		// Token: 0x04001776 RID: 6006
		Or_rm64_imm32,
		// Token: 0x04001777 RID: 6007
		Adc_rm16_imm16,
		// Token: 0x04001778 RID: 6008
		Adc_rm32_imm32,
		// Token: 0x04001779 RID: 6009
		Adc_rm64_imm32,
		// Token: 0x0400177A RID: 6010
		Sbb_rm16_imm16,
		// Token: 0x0400177B RID: 6011
		Sbb_rm32_imm32,
		// Token: 0x0400177C RID: 6012
		Sbb_rm64_imm32,
		// Token: 0x0400177D RID: 6013
		And_rm16_imm16,
		// Token: 0x0400177E RID: 6014
		And_rm32_imm32,
		// Token: 0x0400177F RID: 6015
		And_rm64_imm32,
		// Token: 0x04001780 RID: 6016
		Sub_rm16_imm16,
		// Token: 0x04001781 RID: 6017
		Sub_rm32_imm32,
		// Token: 0x04001782 RID: 6018
		Sub_rm64_imm32,
		// Token: 0x04001783 RID: 6019
		Xor_rm16_imm16,
		// Token: 0x04001784 RID: 6020
		Xor_rm32_imm32,
		// Token: 0x04001785 RID: 6021
		Xor_rm64_imm32,
		// Token: 0x04001786 RID: 6022
		Cmp_rm16_imm16,
		// Token: 0x04001787 RID: 6023
		Cmp_rm32_imm32,
		// Token: 0x04001788 RID: 6024
		Cmp_rm64_imm32,
		// Token: 0x04001789 RID: 6025
		Add_rm8_imm8_82,
		// Token: 0x0400178A RID: 6026
		Or_rm8_imm8_82,
		// Token: 0x0400178B RID: 6027
		Adc_rm8_imm8_82,
		// Token: 0x0400178C RID: 6028
		Sbb_rm8_imm8_82,
		// Token: 0x0400178D RID: 6029
		And_rm8_imm8_82,
		// Token: 0x0400178E RID: 6030
		Sub_rm8_imm8_82,
		// Token: 0x0400178F RID: 6031
		Xor_rm8_imm8_82,
		// Token: 0x04001790 RID: 6032
		Cmp_rm8_imm8_82,
		// Token: 0x04001791 RID: 6033
		Add_rm16_imm8,
		// Token: 0x04001792 RID: 6034
		Add_rm32_imm8,
		// Token: 0x04001793 RID: 6035
		Add_rm64_imm8,
		// Token: 0x04001794 RID: 6036
		Or_rm16_imm8,
		// Token: 0x04001795 RID: 6037
		Or_rm32_imm8,
		// Token: 0x04001796 RID: 6038
		Or_rm64_imm8,
		// Token: 0x04001797 RID: 6039
		Adc_rm16_imm8,
		// Token: 0x04001798 RID: 6040
		Adc_rm32_imm8,
		// Token: 0x04001799 RID: 6041
		Adc_rm64_imm8,
		// Token: 0x0400179A RID: 6042
		Sbb_rm16_imm8,
		// Token: 0x0400179B RID: 6043
		Sbb_rm32_imm8,
		// Token: 0x0400179C RID: 6044
		Sbb_rm64_imm8,
		// Token: 0x0400179D RID: 6045
		And_rm16_imm8,
		// Token: 0x0400179E RID: 6046
		And_rm32_imm8,
		// Token: 0x0400179F RID: 6047
		And_rm64_imm8,
		// Token: 0x040017A0 RID: 6048
		Sub_rm16_imm8,
		// Token: 0x040017A1 RID: 6049
		Sub_rm32_imm8,
		// Token: 0x040017A2 RID: 6050
		Sub_rm64_imm8,
		// Token: 0x040017A3 RID: 6051
		Xor_rm16_imm8,
		// Token: 0x040017A4 RID: 6052
		Xor_rm32_imm8,
		// Token: 0x040017A5 RID: 6053
		Xor_rm64_imm8,
		// Token: 0x040017A6 RID: 6054
		Cmp_rm16_imm8,
		// Token: 0x040017A7 RID: 6055
		Cmp_rm32_imm8,
		// Token: 0x040017A8 RID: 6056
		Cmp_rm64_imm8,
		// Token: 0x040017A9 RID: 6057
		Test_rm8_r8,
		// Token: 0x040017AA RID: 6058
		Test_rm16_r16,
		// Token: 0x040017AB RID: 6059
		Test_rm32_r32,
		// Token: 0x040017AC RID: 6060
		Test_rm64_r64,
		// Token: 0x040017AD RID: 6061
		Xchg_rm8_r8,
		// Token: 0x040017AE RID: 6062
		Xchg_rm16_r16,
		// Token: 0x040017AF RID: 6063
		Xchg_rm32_r32,
		// Token: 0x040017B0 RID: 6064
		Xchg_rm64_r64,
		// Token: 0x040017B1 RID: 6065
		Mov_rm8_r8,
		// Token: 0x040017B2 RID: 6066
		Mov_rm16_r16,
		// Token: 0x040017B3 RID: 6067
		Mov_rm32_r32,
		// Token: 0x040017B4 RID: 6068
		Mov_rm64_r64,
		// Token: 0x040017B5 RID: 6069
		Mov_r8_rm8,
		// Token: 0x040017B6 RID: 6070
		Mov_r16_rm16,
		// Token: 0x040017B7 RID: 6071
		Mov_r32_rm32,
		// Token: 0x040017B8 RID: 6072
		Mov_r64_rm64,
		// Token: 0x040017B9 RID: 6073
		Mov_rm16_Sreg,
		// Token: 0x040017BA RID: 6074
		Mov_r32m16_Sreg,
		// Token: 0x040017BB RID: 6075
		Mov_r64m16_Sreg,
		// Token: 0x040017BC RID: 6076
		Lea_r16_m,
		// Token: 0x040017BD RID: 6077
		Lea_r32_m,
		// Token: 0x040017BE RID: 6078
		Lea_r64_m,
		// Token: 0x040017BF RID: 6079
		Mov_Sreg_rm16,
		// Token: 0x040017C0 RID: 6080
		Mov_Sreg_r32m16,
		// Token: 0x040017C1 RID: 6081
		Mov_Sreg_r64m16,
		// Token: 0x040017C2 RID: 6082
		Pop_rm16,
		// Token: 0x040017C3 RID: 6083
		Pop_rm32,
		// Token: 0x040017C4 RID: 6084
		Pop_rm64,
		// Token: 0x040017C5 RID: 6085
		Nopw,
		// Token: 0x040017C6 RID: 6086
		Nopd,
		// Token: 0x040017C7 RID: 6087
		Nopq,
		// Token: 0x040017C8 RID: 6088
		Xchg_r16_AX,
		// Token: 0x040017C9 RID: 6089
		Xchg_r32_EAX,
		// Token: 0x040017CA RID: 6090
		Xchg_r64_RAX,
		// Token: 0x040017CB RID: 6091
		Pause,
		// Token: 0x040017CC RID: 6092
		Cbw,
		// Token: 0x040017CD RID: 6093
		Cwde,
		// Token: 0x040017CE RID: 6094
		Cdqe,
		// Token: 0x040017CF RID: 6095
		Cwd,
		// Token: 0x040017D0 RID: 6096
		Cdq,
		// Token: 0x040017D1 RID: 6097
		Cqo,
		// Token: 0x040017D2 RID: 6098
		Call_ptr1616,
		// Token: 0x040017D3 RID: 6099
		Call_ptr1632,
		// Token: 0x040017D4 RID: 6100
		Wait,
		// Token: 0x040017D5 RID: 6101
		Pushfw,
		// Token: 0x040017D6 RID: 6102
		Pushfd,
		// Token: 0x040017D7 RID: 6103
		Pushfq,
		// Token: 0x040017D8 RID: 6104
		Popfw,
		// Token: 0x040017D9 RID: 6105
		Popfd,
		// Token: 0x040017DA RID: 6106
		Popfq,
		// Token: 0x040017DB RID: 6107
		Sahf,
		// Token: 0x040017DC RID: 6108
		Lahf,
		// Token: 0x040017DD RID: 6109
		Mov_AL_moffs8,
		// Token: 0x040017DE RID: 6110
		Mov_AX_moffs16,
		// Token: 0x040017DF RID: 6111
		Mov_EAX_moffs32,
		// Token: 0x040017E0 RID: 6112
		Mov_RAX_moffs64,
		// Token: 0x040017E1 RID: 6113
		Mov_moffs8_AL,
		// Token: 0x040017E2 RID: 6114
		Mov_moffs16_AX,
		// Token: 0x040017E3 RID: 6115
		Mov_moffs32_EAX,
		// Token: 0x040017E4 RID: 6116
		Mov_moffs64_RAX,
		// Token: 0x040017E5 RID: 6117
		Movsb_m8_m8,
		// Token: 0x040017E6 RID: 6118
		Movsw_m16_m16,
		// Token: 0x040017E7 RID: 6119
		Movsd_m32_m32,
		// Token: 0x040017E8 RID: 6120
		Movsq_m64_m64,
		// Token: 0x040017E9 RID: 6121
		Cmpsb_m8_m8,
		// Token: 0x040017EA RID: 6122
		Cmpsw_m16_m16,
		// Token: 0x040017EB RID: 6123
		Cmpsd_m32_m32,
		// Token: 0x040017EC RID: 6124
		Cmpsq_m64_m64,
		// Token: 0x040017ED RID: 6125
		Test_AL_imm8,
		// Token: 0x040017EE RID: 6126
		Test_AX_imm16,
		// Token: 0x040017EF RID: 6127
		Test_EAX_imm32,
		// Token: 0x040017F0 RID: 6128
		Test_RAX_imm32,
		// Token: 0x040017F1 RID: 6129
		Stosb_m8_AL,
		// Token: 0x040017F2 RID: 6130
		Stosw_m16_AX,
		// Token: 0x040017F3 RID: 6131
		Stosd_m32_EAX,
		// Token: 0x040017F4 RID: 6132
		Stosq_m64_RAX,
		// Token: 0x040017F5 RID: 6133
		Lodsb_AL_m8,
		// Token: 0x040017F6 RID: 6134
		Lodsw_AX_m16,
		// Token: 0x040017F7 RID: 6135
		Lodsd_EAX_m32,
		// Token: 0x040017F8 RID: 6136
		Lodsq_RAX_m64,
		// Token: 0x040017F9 RID: 6137
		Scasb_AL_m8,
		// Token: 0x040017FA RID: 6138
		Scasw_AX_m16,
		// Token: 0x040017FB RID: 6139
		Scasd_EAX_m32,
		// Token: 0x040017FC RID: 6140
		Scasq_RAX_m64,
		// Token: 0x040017FD RID: 6141
		Mov_r8_imm8,
		// Token: 0x040017FE RID: 6142
		Mov_r16_imm16,
		// Token: 0x040017FF RID: 6143
		Mov_r32_imm32,
		// Token: 0x04001800 RID: 6144
		Mov_r64_imm64,
		// Token: 0x04001801 RID: 6145
		Rol_rm8_imm8,
		// Token: 0x04001802 RID: 6146
		Ror_rm8_imm8,
		// Token: 0x04001803 RID: 6147
		Rcl_rm8_imm8,
		// Token: 0x04001804 RID: 6148
		Rcr_rm8_imm8,
		// Token: 0x04001805 RID: 6149
		Shl_rm8_imm8,
		// Token: 0x04001806 RID: 6150
		Shr_rm8_imm8,
		// Token: 0x04001807 RID: 6151
		Sal_rm8_imm8,
		// Token: 0x04001808 RID: 6152
		Sar_rm8_imm8,
		// Token: 0x04001809 RID: 6153
		Rol_rm16_imm8,
		// Token: 0x0400180A RID: 6154
		Rol_rm32_imm8,
		// Token: 0x0400180B RID: 6155
		Rol_rm64_imm8,
		// Token: 0x0400180C RID: 6156
		Ror_rm16_imm8,
		// Token: 0x0400180D RID: 6157
		Ror_rm32_imm8,
		// Token: 0x0400180E RID: 6158
		Ror_rm64_imm8,
		// Token: 0x0400180F RID: 6159
		Rcl_rm16_imm8,
		// Token: 0x04001810 RID: 6160
		Rcl_rm32_imm8,
		// Token: 0x04001811 RID: 6161
		Rcl_rm64_imm8,
		// Token: 0x04001812 RID: 6162
		Rcr_rm16_imm8,
		// Token: 0x04001813 RID: 6163
		Rcr_rm32_imm8,
		// Token: 0x04001814 RID: 6164
		Rcr_rm64_imm8,
		// Token: 0x04001815 RID: 6165
		Shl_rm16_imm8,
		// Token: 0x04001816 RID: 6166
		Shl_rm32_imm8,
		// Token: 0x04001817 RID: 6167
		Shl_rm64_imm8,
		// Token: 0x04001818 RID: 6168
		Shr_rm16_imm8,
		// Token: 0x04001819 RID: 6169
		Shr_rm32_imm8,
		// Token: 0x0400181A RID: 6170
		Shr_rm64_imm8,
		// Token: 0x0400181B RID: 6171
		Sal_rm16_imm8,
		// Token: 0x0400181C RID: 6172
		Sal_rm32_imm8,
		// Token: 0x0400181D RID: 6173
		Sal_rm64_imm8,
		// Token: 0x0400181E RID: 6174
		Sar_rm16_imm8,
		// Token: 0x0400181F RID: 6175
		Sar_rm32_imm8,
		// Token: 0x04001820 RID: 6176
		Sar_rm64_imm8,
		// Token: 0x04001821 RID: 6177
		Retnw_imm16,
		// Token: 0x04001822 RID: 6178
		Retnd_imm16,
		// Token: 0x04001823 RID: 6179
		Retnq_imm16,
		// Token: 0x04001824 RID: 6180
		Retnw,
		// Token: 0x04001825 RID: 6181
		Retnd,
		// Token: 0x04001826 RID: 6182
		Retnq,
		// Token: 0x04001827 RID: 6183
		Les_r16_m1616,
		// Token: 0x04001828 RID: 6184
		Les_r32_m1632,
		// Token: 0x04001829 RID: 6185
		Lds_r16_m1616,
		// Token: 0x0400182A RID: 6186
		Lds_r32_m1632,
		// Token: 0x0400182B RID: 6187
		Mov_rm8_imm8,
		// Token: 0x0400182C RID: 6188
		Xabort_imm8,
		// Token: 0x0400182D RID: 6189
		Mov_rm16_imm16,
		// Token: 0x0400182E RID: 6190
		Mov_rm32_imm32,
		// Token: 0x0400182F RID: 6191
		Mov_rm64_imm32,
		// Token: 0x04001830 RID: 6192
		Xbegin_rel16,
		// Token: 0x04001831 RID: 6193
		Xbegin_rel32,
		// Token: 0x04001832 RID: 6194
		Enterw_imm16_imm8,
		// Token: 0x04001833 RID: 6195
		Enterd_imm16_imm8,
		// Token: 0x04001834 RID: 6196
		Enterq_imm16_imm8,
		// Token: 0x04001835 RID: 6197
		Leavew,
		// Token: 0x04001836 RID: 6198
		Leaved,
		// Token: 0x04001837 RID: 6199
		Leaveq,
		// Token: 0x04001838 RID: 6200
		Retfw_imm16,
		// Token: 0x04001839 RID: 6201
		Retfd_imm16,
		// Token: 0x0400183A RID: 6202
		Retfq_imm16,
		// Token: 0x0400183B RID: 6203
		Retfw,
		// Token: 0x0400183C RID: 6204
		Retfd,
		// Token: 0x0400183D RID: 6205
		Retfq,
		// Token: 0x0400183E RID: 6206
		Int3,
		// Token: 0x0400183F RID: 6207
		Int_imm8,
		// Token: 0x04001840 RID: 6208
		Into,
		// Token: 0x04001841 RID: 6209
		Iretw,
		// Token: 0x04001842 RID: 6210
		Iretd,
		// Token: 0x04001843 RID: 6211
		Iretq,
		// Token: 0x04001844 RID: 6212
		Rol_rm8_1,
		// Token: 0x04001845 RID: 6213
		Ror_rm8_1,
		// Token: 0x04001846 RID: 6214
		Rcl_rm8_1,
		// Token: 0x04001847 RID: 6215
		Rcr_rm8_1,
		// Token: 0x04001848 RID: 6216
		Shl_rm8_1,
		// Token: 0x04001849 RID: 6217
		Shr_rm8_1,
		// Token: 0x0400184A RID: 6218
		Sal_rm8_1,
		// Token: 0x0400184B RID: 6219
		Sar_rm8_1,
		// Token: 0x0400184C RID: 6220
		Rol_rm16_1,
		// Token: 0x0400184D RID: 6221
		Rol_rm32_1,
		// Token: 0x0400184E RID: 6222
		Rol_rm64_1,
		// Token: 0x0400184F RID: 6223
		Ror_rm16_1,
		// Token: 0x04001850 RID: 6224
		Ror_rm32_1,
		// Token: 0x04001851 RID: 6225
		Ror_rm64_1,
		// Token: 0x04001852 RID: 6226
		Rcl_rm16_1,
		// Token: 0x04001853 RID: 6227
		Rcl_rm32_1,
		// Token: 0x04001854 RID: 6228
		Rcl_rm64_1,
		// Token: 0x04001855 RID: 6229
		Rcr_rm16_1,
		// Token: 0x04001856 RID: 6230
		Rcr_rm32_1,
		// Token: 0x04001857 RID: 6231
		Rcr_rm64_1,
		// Token: 0x04001858 RID: 6232
		Shl_rm16_1,
		// Token: 0x04001859 RID: 6233
		Shl_rm32_1,
		// Token: 0x0400185A RID: 6234
		Shl_rm64_1,
		// Token: 0x0400185B RID: 6235
		Shr_rm16_1,
		// Token: 0x0400185C RID: 6236
		Shr_rm32_1,
		// Token: 0x0400185D RID: 6237
		Shr_rm64_1,
		// Token: 0x0400185E RID: 6238
		Sal_rm16_1,
		// Token: 0x0400185F RID: 6239
		Sal_rm32_1,
		// Token: 0x04001860 RID: 6240
		Sal_rm64_1,
		// Token: 0x04001861 RID: 6241
		Sar_rm16_1,
		// Token: 0x04001862 RID: 6242
		Sar_rm32_1,
		// Token: 0x04001863 RID: 6243
		Sar_rm64_1,
		// Token: 0x04001864 RID: 6244
		Rol_rm8_CL,
		// Token: 0x04001865 RID: 6245
		Ror_rm8_CL,
		// Token: 0x04001866 RID: 6246
		Rcl_rm8_CL,
		// Token: 0x04001867 RID: 6247
		Rcr_rm8_CL,
		// Token: 0x04001868 RID: 6248
		Shl_rm8_CL,
		// Token: 0x04001869 RID: 6249
		Shr_rm8_CL,
		// Token: 0x0400186A RID: 6250
		Sal_rm8_CL,
		// Token: 0x0400186B RID: 6251
		Sar_rm8_CL,
		// Token: 0x0400186C RID: 6252
		Rol_rm16_CL,
		// Token: 0x0400186D RID: 6253
		Rol_rm32_CL,
		// Token: 0x0400186E RID: 6254
		Rol_rm64_CL,
		// Token: 0x0400186F RID: 6255
		Ror_rm16_CL,
		// Token: 0x04001870 RID: 6256
		Ror_rm32_CL,
		// Token: 0x04001871 RID: 6257
		Ror_rm64_CL,
		// Token: 0x04001872 RID: 6258
		Rcl_rm16_CL,
		// Token: 0x04001873 RID: 6259
		Rcl_rm32_CL,
		// Token: 0x04001874 RID: 6260
		Rcl_rm64_CL,
		// Token: 0x04001875 RID: 6261
		Rcr_rm16_CL,
		// Token: 0x04001876 RID: 6262
		Rcr_rm32_CL,
		// Token: 0x04001877 RID: 6263
		Rcr_rm64_CL,
		// Token: 0x04001878 RID: 6264
		Shl_rm16_CL,
		// Token: 0x04001879 RID: 6265
		Shl_rm32_CL,
		// Token: 0x0400187A RID: 6266
		Shl_rm64_CL,
		// Token: 0x0400187B RID: 6267
		Shr_rm16_CL,
		// Token: 0x0400187C RID: 6268
		Shr_rm32_CL,
		// Token: 0x0400187D RID: 6269
		Shr_rm64_CL,
		// Token: 0x0400187E RID: 6270
		Sal_rm16_CL,
		// Token: 0x0400187F RID: 6271
		Sal_rm32_CL,
		// Token: 0x04001880 RID: 6272
		Sal_rm64_CL,
		// Token: 0x04001881 RID: 6273
		Sar_rm16_CL,
		// Token: 0x04001882 RID: 6274
		Sar_rm32_CL,
		// Token: 0x04001883 RID: 6275
		Sar_rm64_CL,
		// Token: 0x04001884 RID: 6276
		Aam_imm8,
		// Token: 0x04001885 RID: 6277
		Aad_imm8,
		// Token: 0x04001886 RID: 6278
		Salc,
		// Token: 0x04001887 RID: 6279
		Xlat_m8,
		// Token: 0x04001888 RID: 6280
		Fadd_m32fp,
		// Token: 0x04001889 RID: 6281
		Fmul_m32fp,
		// Token: 0x0400188A RID: 6282
		Fcom_m32fp,
		// Token: 0x0400188B RID: 6283
		Fcomp_m32fp,
		// Token: 0x0400188C RID: 6284
		Fsub_m32fp,
		// Token: 0x0400188D RID: 6285
		Fsubr_m32fp,
		// Token: 0x0400188E RID: 6286
		Fdiv_m32fp,
		// Token: 0x0400188F RID: 6287
		Fdivr_m32fp,
		// Token: 0x04001890 RID: 6288
		Fadd_st0_sti,
		// Token: 0x04001891 RID: 6289
		Fmul_st0_sti,
		// Token: 0x04001892 RID: 6290
		Fcom_st0_sti,
		// Token: 0x04001893 RID: 6291
		Fcomp_st0_sti,
		// Token: 0x04001894 RID: 6292
		Fsub_st0_sti,
		// Token: 0x04001895 RID: 6293
		Fsubr_st0_sti,
		// Token: 0x04001896 RID: 6294
		Fdiv_st0_sti,
		// Token: 0x04001897 RID: 6295
		Fdivr_st0_sti,
		// Token: 0x04001898 RID: 6296
		Fld_m32fp,
		// Token: 0x04001899 RID: 6297
		Fst_m32fp,
		// Token: 0x0400189A RID: 6298
		Fstp_m32fp,
		// Token: 0x0400189B RID: 6299
		Fldenv_m14byte,
		// Token: 0x0400189C RID: 6300
		Fldenv_m28byte,
		// Token: 0x0400189D RID: 6301
		Fldcw_m2byte,
		// Token: 0x0400189E RID: 6302
		Fnstenv_m14byte,
		// Token: 0x0400189F RID: 6303
		Fstenv_m14byte,
		// Token: 0x040018A0 RID: 6304
		Fnstenv_m28byte,
		// Token: 0x040018A1 RID: 6305
		Fstenv_m28byte,
		// Token: 0x040018A2 RID: 6306
		Fnstcw_m2byte,
		// Token: 0x040018A3 RID: 6307
		Fstcw_m2byte,
		// Token: 0x040018A4 RID: 6308
		Fld_sti,
		// Token: 0x040018A5 RID: 6309
		Fxch_st0_sti,
		// Token: 0x040018A6 RID: 6310
		Fnop,
		// Token: 0x040018A7 RID: 6311
		Fstpnce_sti,
		// Token: 0x040018A8 RID: 6312
		Fchs,
		// Token: 0x040018A9 RID: 6313
		Fabs,
		// Token: 0x040018AA RID: 6314
		Ftst,
		// Token: 0x040018AB RID: 6315
		Fxam,
		// Token: 0x040018AC RID: 6316
		Fld1,
		// Token: 0x040018AD RID: 6317
		Fldl2t,
		// Token: 0x040018AE RID: 6318
		Fldl2e,
		// Token: 0x040018AF RID: 6319
		Fldpi,
		// Token: 0x040018B0 RID: 6320
		Fldlg2,
		// Token: 0x040018B1 RID: 6321
		Fldln2,
		// Token: 0x040018B2 RID: 6322
		Fldz,
		// Token: 0x040018B3 RID: 6323
		F2xm1,
		// Token: 0x040018B4 RID: 6324
		Fyl2x,
		// Token: 0x040018B5 RID: 6325
		Fptan,
		// Token: 0x040018B6 RID: 6326
		Fpatan,
		// Token: 0x040018B7 RID: 6327
		Fxtract,
		// Token: 0x040018B8 RID: 6328
		Fprem1,
		// Token: 0x040018B9 RID: 6329
		Fdecstp,
		// Token: 0x040018BA RID: 6330
		Fincstp,
		// Token: 0x040018BB RID: 6331
		Fprem,
		// Token: 0x040018BC RID: 6332
		Fyl2xp1,
		// Token: 0x040018BD RID: 6333
		Fsqrt,
		// Token: 0x040018BE RID: 6334
		Fsincos,
		// Token: 0x040018BF RID: 6335
		Frndint,
		// Token: 0x040018C0 RID: 6336
		Fscale,
		// Token: 0x040018C1 RID: 6337
		Fsin,
		// Token: 0x040018C2 RID: 6338
		Fcos,
		// Token: 0x040018C3 RID: 6339
		Fiadd_m32int,
		// Token: 0x040018C4 RID: 6340
		Fimul_m32int,
		// Token: 0x040018C5 RID: 6341
		Ficom_m32int,
		// Token: 0x040018C6 RID: 6342
		Ficomp_m32int,
		// Token: 0x040018C7 RID: 6343
		Fisub_m32int,
		// Token: 0x040018C8 RID: 6344
		Fisubr_m32int,
		// Token: 0x040018C9 RID: 6345
		Fidiv_m32int,
		// Token: 0x040018CA RID: 6346
		Fidivr_m32int,
		// Token: 0x040018CB RID: 6347
		Fcmovb_st0_sti,
		// Token: 0x040018CC RID: 6348
		Fcmove_st0_sti,
		// Token: 0x040018CD RID: 6349
		Fcmovbe_st0_sti,
		// Token: 0x040018CE RID: 6350
		Fcmovu_st0_sti,
		// Token: 0x040018CF RID: 6351
		Fucompp,
		// Token: 0x040018D0 RID: 6352
		Fild_m32int,
		// Token: 0x040018D1 RID: 6353
		Fisttp_m32int,
		// Token: 0x040018D2 RID: 6354
		Fist_m32int,
		// Token: 0x040018D3 RID: 6355
		Fistp_m32int,
		// Token: 0x040018D4 RID: 6356
		Fld_m80fp,
		// Token: 0x040018D5 RID: 6357
		Fstp_m80fp,
		// Token: 0x040018D6 RID: 6358
		Fcmovnb_st0_sti,
		// Token: 0x040018D7 RID: 6359
		Fcmovne_st0_sti,
		// Token: 0x040018D8 RID: 6360
		Fcmovnbe_st0_sti,
		// Token: 0x040018D9 RID: 6361
		Fcmovnu_st0_sti,
		// Token: 0x040018DA RID: 6362
		Fneni,
		// Token: 0x040018DB RID: 6363
		Feni,
		// Token: 0x040018DC RID: 6364
		Fndisi,
		// Token: 0x040018DD RID: 6365
		Fdisi,
		// Token: 0x040018DE RID: 6366
		Fnclex,
		// Token: 0x040018DF RID: 6367
		Fclex,
		// Token: 0x040018E0 RID: 6368
		Fninit,
		// Token: 0x040018E1 RID: 6369
		Finit,
		// Token: 0x040018E2 RID: 6370
		Fnsetpm,
		// Token: 0x040018E3 RID: 6371
		Fsetpm,
		// Token: 0x040018E4 RID: 6372
		Frstpm,
		// Token: 0x040018E5 RID: 6373
		Fucomi_st0_sti,
		// Token: 0x040018E6 RID: 6374
		Fcomi_st0_sti,
		// Token: 0x040018E7 RID: 6375
		Fadd_m64fp,
		// Token: 0x040018E8 RID: 6376
		Fmul_m64fp,
		// Token: 0x040018E9 RID: 6377
		Fcom_m64fp,
		// Token: 0x040018EA RID: 6378
		Fcomp_m64fp,
		// Token: 0x040018EB RID: 6379
		Fsub_m64fp,
		// Token: 0x040018EC RID: 6380
		Fsubr_m64fp,
		// Token: 0x040018ED RID: 6381
		Fdiv_m64fp,
		// Token: 0x040018EE RID: 6382
		Fdivr_m64fp,
		// Token: 0x040018EF RID: 6383
		Fadd_sti_st0,
		// Token: 0x040018F0 RID: 6384
		Fmul_sti_st0,
		// Token: 0x040018F1 RID: 6385
		Fcom_st0_sti_DCD0,
		// Token: 0x040018F2 RID: 6386
		Fcomp_st0_sti_DCD8,
		// Token: 0x040018F3 RID: 6387
		Fsubr_sti_st0,
		// Token: 0x040018F4 RID: 6388
		Fsub_sti_st0,
		// Token: 0x040018F5 RID: 6389
		Fdivr_sti_st0,
		// Token: 0x040018F6 RID: 6390
		Fdiv_sti_st0,
		// Token: 0x040018F7 RID: 6391
		Fld_m64fp,
		// Token: 0x040018F8 RID: 6392
		Fisttp_m64int,
		// Token: 0x040018F9 RID: 6393
		Fst_m64fp,
		// Token: 0x040018FA RID: 6394
		Fstp_m64fp,
		// Token: 0x040018FB RID: 6395
		Frstor_m94byte,
		// Token: 0x040018FC RID: 6396
		Frstor_m108byte,
		// Token: 0x040018FD RID: 6397
		Fnsave_m94byte,
		// Token: 0x040018FE RID: 6398
		Fsave_m94byte,
		// Token: 0x040018FF RID: 6399
		Fnsave_m108byte,
		// Token: 0x04001900 RID: 6400
		Fsave_m108byte,
		// Token: 0x04001901 RID: 6401
		Fnstsw_m2byte,
		// Token: 0x04001902 RID: 6402
		Fstsw_m2byte,
		// Token: 0x04001903 RID: 6403
		Ffree_sti,
		// Token: 0x04001904 RID: 6404
		Fxch_st0_sti_DDC8,
		// Token: 0x04001905 RID: 6405
		Fst_sti,
		// Token: 0x04001906 RID: 6406
		Fstp_sti,
		// Token: 0x04001907 RID: 6407
		Fucom_st0_sti,
		// Token: 0x04001908 RID: 6408
		Fucomp_st0_sti,
		// Token: 0x04001909 RID: 6409
		Fiadd_m16int,
		// Token: 0x0400190A RID: 6410
		Fimul_m16int,
		// Token: 0x0400190B RID: 6411
		Ficom_m16int,
		// Token: 0x0400190C RID: 6412
		Ficomp_m16int,
		// Token: 0x0400190D RID: 6413
		Fisub_m16int,
		// Token: 0x0400190E RID: 6414
		Fisubr_m16int,
		// Token: 0x0400190F RID: 6415
		Fidiv_m16int,
		// Token: 0x04001910 RID: 6416
		Fidivr_m16int,
		// Token: 0x04001911 RID: 6417
		Faddp_sti_st0,
		// Token: 0x04001912 RID: 6418
		Fmulp_sti_st0,
		// Token: 0x04001913 RID: 6419
		Fcomp_st0_sti_DED0,
		// Token: 0x04001914 RID: 6420
		Fcompp,
		// Token: 0x04001915 RID: 6421
		Fsubrp_sti_st0,
		// Token: 0x04001916 RID: 6422
		Fsubp_sti_st0,
		// Token: 0x04001917 RID: 6423
		Fdivrp_sti_st0,
		// Token: 0x04001918 RID: 6424
		Fdivp_sti_st0,
		// Token: 0x04001919 RID: 6425
		Fild_m16int,
		// Token: 0x0400191A RID: 6426
		Fisttp_m16int,
		// Token: 0x0400191B RID: 6427
		Fist_m16int,
		// Token: 0x0400191C RID: 6428
		Fistp_m16int,
		// Token: 0x0400191D RID: 6429
		Fbld_m80bcd,
		// Token: 0x0400191E RID: 6430
		Fild_m64int,
		// Token: 0x0400191F RID: 6431
		Fbstp_m80bcd,
		// Token: 0x04001920 RID: 6432
		Fistp_m64int,
		// Token: 0x04001921 RID: 6433
		Ffreep_sti,
		// Token: 0x04001922 RID: 6434
		Fxch_st0_sti_DFC8,
		// Token: 0x04001923 RID: 6435
		Fstp_sti_DFD0,
		// Token: 0x04001924 RID: 6436
		Fstp_sti_DFD8,
		// Token: 0x04001925 RID: 6437
		Fnstsw_AX,
		// Token: 0x04001926 RID: 6438
		Fstsw_AX,
		// Token: 0x04001927 RID: 6439
		Fstdw_AX,
		// Token: 0x04001928 RID: 6440
		Fstsg_AX,
		// Token: 0x04001929 RID: 6441
		Fucomip_st0_sti,
		// Token: 0x0400192A RID: 6442
		Fcomip_st0_sti,
		// Token: 0x0400192B RID: 6443
		Loopne_rel8_16_CX,
		// Token: 0x0400192C RID: 6444
		Loopne_rel8_32_CX,
		// Token: 0x0400192D RID: 6445
		Loopne_rel8_16_ECX,
		// Token: 0x0400192E RID: 6446
		Loopne_rel8_32_ECX,
		// Token: 0x0400192F RID: 6447
		Loopne_rel8_64_ECX,
		// Token: 0x04001930 RID: 6448
		Loopne_rel8_16_RCX,
		// Token: 0x04001931 RID: 6449
		Loopne_rel8_64_RCX,
		// Token: 0x04001932 RID: 6450
		Loope_rel8_16_CX,
		// Token: 0x04001933 RID: 6451
		Loope_rel8_32_CX,
		// Token: 0x04001934 RID: 6452
		Loope_rel8_16_ECX,
		// Token: 0x04001935 RID: 6453
		Loope_rel8_32_ECX,
		// Token: 0x04001936 RID: 6454
		Loope_rel8_64_ECX,
		// Token: 0x04001937 RID: 6455
		Loope_rel8_16_RCX,
		// Token: 0x04001938 RID: 6456
		Loope_rel8_64_RCX,
		// Token: 0x04001939 RID: 6457
		Loop_rel8_16_CX,
		// Token: 0x0400193A RID: 6458
		Loop_rel8_32_CX,
		// Token: 0x0400193B RID: 6459
		Loop_rel8_16_ECX,
		// Token: 0x0400193C RID: 6460
		Loop_rel8_32_ECX,
		// Token: 0x0400193D RID: 6461
		Loop_rel8_64_ECX,
		// Token: 0x0400193E RID: 6462
		Loop_rel8_16_RCX,
		// Token: 0x0400193F RID: 6463
		Loop_rel8_64_RCX,
		// Token: 0x04001940 RID: 6464
		Jcxz_rel8_16,
		// Token: 0x04001941 RID: 6465
		Jcxz_rel8_32,
		// Token: 0x04001942 RID: 6466
		Jecxz_rel8_16,
		// Token: 0x04001943 RID: 6467
		Jecxz_rel8_32,
		// Token: 0x04001944 RID: 6468
		Jecxz_rel8_64,
		// Token: 0x04001945 RID: 6469
		Jrcxz_rel8_16,
		// Token: 0x04001946 RID: 6470
		Jrcxz_rel8_64,
		// Token: 0x04001947 RID: 6471
		In_AL_imm8,
		// Token: 0x04001948 RID: 6472
		In_AX_imm8,
		// Token: 0x04001949 RID: 6473
		In_EAX_imm8,
		// Token: 0x0400194A RID: 6474
		Out_imm8_AL,
		// Token: 0x0400194B RID: 6475
		Out_imm8_AX,
		// Token: 0x0400194C RID: 6476
		Out_imm8_EAX,
		// Token: 0x0400194D RID: 6477
		Call_rel16,
		// Token: 0x0400194E RID: 6478
		Call_rel32_32,
		// Token: 0x0400194F RID: 6479
		Call_rel32_64,
		// Token: 0x04001950 RID: 6480
		Jmp_rel16,
		// Token: 0x04001951 RID: 6481
		Jmp_rel32_32,
		// Token: 0x04001952 RID: 6482
		Jmp_rel32_64,
		// Token: 0x04001953 RID: 6483
		Jmp_ptr1616,
		// Token: 0x04001954 RID: 6484
		Jmp_ptr1632,
		// Token: 0x04001955 RID: 6485
		Jmp_rel8_16,
		// Token: 0x04001956 RID: 6486
		Jmp_rel8_32,
		// Token: 0x04001957 RID: 6487
		Jmp_rel8_64,
		// Token: 0x04001958 RID: 6488
		In_AL_DX,
		// Token: 0x04001959 RID: 6489
		In_AX_DX,
		// Token: 0x0400195A RID: 6490
		In_EAX_DX,
		// Token: 0x0400195B RID: 6491
		Out_DX_AL,
		// Token: 0x0400195C RID: 6492
		Out_DX_AX,
		// Token: 0x0400195D RID: 6493
		Out_DX_EAX,
		// Token: 0x0400195E RID: 6494
		Int1,
		// Token: 0x0400195F RID: 6495
		Hlt,
		// Token: 0x04001960 RID: 6496
		Cmc,
		// Token: 0x04001961 RID: 6497
		Test_rm8_imm8,
		// Token: 0x04001962 RID: 6498
		Test_rm8_imm8_F6r1,
		// Token: 0x04001963 RID: 6499
		Not_rm8,
		// Token: 0x04001964 RID: 6500
		Neg_rm8,
		// Token: 0x04001965 RID: 6501
		Mul_rm8,
		// Token: 0x04001966 RID: 6502
		Imul_rm8,
		// Token: 0x04001967 RID: 6503
		Div_rm8,
		// Token: 0x04001968 RID: 6504
		Idiv_rm8,
		// Token: 0x04001969 RID: 6505
		Test_rm16_imm16,
		// Token: 0x0400196A RID: 6506
		Test_rm32_imm32,
		// Token: 0x0400196B RID: 6507
		Test_rm64_imm32,
		// Token: 0x0400196C RID: 6508
		Test_rm16_imm16_F7r1,
		// Token: 0x0400196D RID: 6509
		Test_rm32_imm32_F7r1,
		// Token: 0x0400196E RID: 6510
		Test_rm64_imm32_F7r1,
		// Token: 0x0400196F RID: 6511
		Not_rm16,
		// Token: 0x04001970 RID: 6512
		Not_rm32,
		// Token: 0x04001971 RID: 6513
		Not_rm64,
		// Token: 0x04001972 RID: 6514
		Neg_rm16,
		// Token: 0x04001973 RID: 6515
		Neg_rm32,
		// Token: 0x04001974 RID: 6516
		Neg_rm64,
		// Token: 0x04001975 RID: 6517
		Mul_rm16,
		// Token: 0x04001976 RID: 6518
		Mul_rm32,
		// Token: 0x04001977 RID: 6519
		Mul_rm64,
		// Token: 0x04001978 RID: 6520
		Imul_rm16,
		// Token: 0x04001979 RID: 6521
		Imul_rm32,
		// Token: 0x0400197A RID: 6522
		Imul_rm64,
		// Token: 0x0400197B RID: 6523
		Div_rm16,
		// Token: 0x0400197C RID: 6524
		Div_rm32,
		// Token: 0x0400197D RID: 6525
		Div_rm64,
		// Token: 0x0400197E RID: 6526
		Idiv_rm16,
		// Token: 0x0400197F RID: 6527
		Idiv_rm32,
		// Token: 0x04001980 RID: 6528
		Idiv_rm64,
		// Token: 0x04001981 RID: 6529
		Clc,
		// Token: 0x04001982 RID: 6530
		Stc,
		// Token: 0x04001983 RID: 6531
		Cli,
		// Token: 0x04001984 RID: 6532
		Sti,
		// Token: 0x04001985 RID: 6533
		Cld,
		// Token: 0x04001986 RID: 6534
		Std,
		// Token: 0x04001987 RID: 6535
		Inc_rm8,
		// Token: 0x04001988 RID: 6536
		Dec_rm8,
		// Token: 0x04001989 RID: 6537
		Inc_rm16,
		// Token: 0x0400198A RID: 6538
		Inc_rm32,
		// Token: 0x0400198B RID: 6539
		Inc_rm64,
		// Token: 0x0400198C RID: 6540
		Dec_rm16,
		// Token: 0x0400198D RID: 6541
		Dec_rm32,
		// Token: 0x0400198E RID: 6542
		Dec_rm64,
		// Token: 0x0400198F RID: 6543
		Call_rm16,
		// Token: 0x04001990 RID: 6544
		Call_rm32,
		// Token: 0x04001991 RID: 6545
		Call_rm64,
		// Token: 0x04001992 RID: 6546
		Call_m1616,
		// Token: 0x04001993 RID: 6547
		Call_m1632,
		// Token: 0x04001994 RID: 6548
		Call_m1664,
		// Token: 0x04001995 RID: 6549
		Jmp_rm16,
		// Token: 0x04001996 RID: 6550
		Jmp_rm32,
		// Token: 0x04001997 RID: 6551
		Jmp_rm64,
		// Token: 0x04001998 RID: 6552
		Jmp_m1616,
		// Token: 0x04001999 RID: 6553
		Jmp_m1632,
		// Token: 0x0400199A RID: 6554
		Jmp_m1664,
		// Token: 0x0400199B RID: 6555
		Push_rm16,
		// Token: 0x0400199C RID: 6556
		Push_rm32,
		// Token: 0x0400199D RID: 6557
		Push_rm64,
		// Token: 0x0400199E RID: 6558
		Sldt_rm16,
		// Token: 0x0400199F RID: 6559
		Sldt_r32m16,
		// Token: 0x040019A0 RID: 6560
		Sldt_r64m16,
		// Token: 0x040019A1 RID: 6561
		Str_rm16,
		// Token: 0x040019A2 RID: 6562
		Str_r32m16,
		// Token: 0x040019A3 RID: 6563
		Str_r64m16,
		// Token: 0x040019A4 RID: 6564
		Lldt_rm16,
		// Token: 0x040019A5 RID: 6565
		Lldt_r32m16,
		// Token: 0x040019A6 RID: 6566
		Lldt_r64m16,
		// Token: 0x040019A7 RID: 6567
		Ltr_rm16,
		// Token: 0x040019A8 RID: 6568
		Ltr_r32m16,
		// Token: 0x040019A9 RID: 6569
		Ltr_r64m16,
		// Token: 0x040019AA RID: 6570
		Verr_rm16,
		// Token: 0x040019AB RID: 6571
		Verr_r32m16,
		// Token: 0x040019AC RID: 6572
		Verr_r64m16,
		// Token: 0x040019AD RID: 6573
		Verw_rm16,
		// Token: 0x040019AE RID: 6574
		Verw_r32m16,
		// Token: 0x040019AF RID: 6575
		Verw_r64m16,
		// Token: 0x040019B0 RID: 6576
		Jmpe_rm16,
		// Token: 0x040019B1 RID: 6577
		Jmpe_rm32,
		// Token: 0x040019B2 RID: 6578
		Sgdt_m1632_16,
		// Token: 0x040019B3 RID: 6579
		Sgdt_m1632,
		// Token: 0x040019B4 RID: 6580
		Sgdt_m1664,
		// Token: 0x040019B5 RID: 6581
		Sidt_m1632_16,
		// Token: 0x040019B6 RID: 6582
		Sidt_m1632,
		// Token: 0x040019B7 RID: 6583
		Sidt_m1664,
		// Token: 0x040019B8 RID: 6584
		Lgdt_m1632_16,
		// Token: 0x040019B9 RID: 6585
		Lgdt_m1632,
		// Token: 0x040019BA RID: 6586
		Lgdt_m1664,
		// Token: 0x040019BB RID: 6587
		Lidt_m1632_16,
		// Token: 0x040019BC RID: 6588
		Lidt_m1632,
		// Token: 0x040019BD RID: 6589
		Lidt_m1664,
		// Token: 0x040019BE RID: 6590
		Smsw_rm16,
		// Token: 0x040019BF RID: 6591
		Smsw_r32m16,
		// Token: 0x040019C0 RID: 6592
		Smsw_r64m16,
		// Token: 0x040019C1 RID: 6593
		Rstorssp_m64,
		// Token: 0x040019C2 RID: 6594
		Lmsw_rm16,
		// Token: 0x040019C3 RID: 6595
		Lmsw_r32m16,
		// Token: 0x040019C4 RID: 6596
		Lmsw_r64m16,
		// Token: 0x040019C5 RID: 6597
		Invlpg_m,
		// Token: 0x040019C6 RID: 6598
		Enclv,
		// Token: 0x040019C7 RID: 6599
		Vmcall,
		// Token: 0x040019C8 RID: 6600
		Vmlaunch,
		// Token: 0x040019C9 RID: 6601
		Vmresume,
		// Token: 0x040019CA RID: 6602
		Vmxoff,
		// Token: 0x040019CB RID: 6603
		Pconfig,
		// Token: 0x040019CC RID: 6604
		Monitorw,
		// Token: 0x040019CD RID: 6605
		Monitord,
		// Token: 0x040019CE RID: 6606
		Monitorq,
		// Token: 0x040019CF RID: 6607
		Mwait,
		// Token: 0x040019D0 RID: 6608
		Clac,
		// Token: 0x040019D1 RID: 6609
		Stac,
		// Token: 0x040019D2 RID: 6610
		Encls,
		// Token: 0x040019D3 RID: 6611
		Xgetbv,
		// Token: 0x040019D4 RID: 6612
		Xsetbv,
		// Token: 0x040019D5 RID: 6613
		Vmfunc,
		// Token: 0x040019D6 RID: 6614
		Xend,
		// Token: 0x040019D7 RID: 6615
		Xtest,
		// Token: 0x040019D8 RID: 6616
		Enclu,
		// Token: 0x040019D9 RID: 6617
		Vmrunw,
		// Token: 0x040019DA RID: 6618
		Vmrund,
		// Token: 0x040019DB RID: 6619
		Vmrunq,
		// Token: 0x040019DC RID: 6620
		Vmmcall,
		// Token: 0x040019DD RID: 6621
		Vmloadw,
		// Token: 0x040019DE RID: 6622
		Vmloadd,
		// Token: 0x040019DF RID: 6623
		Vmloadq,
		// Token: 0x040019E0 RID: 6624
		Vmsavew,
		// Token: 0x040019E1 RID: 6625
		Vmsaved,
		// Token: 0x040019E2 RID: 6626
		Vmsaveq,
		// Token: 0x040019E3 RID: 6627
		Stgi,
		// Token: 0x040019E4 RID: 6628
		Clgi,
		// Token: 0x040019E5 RID: 6629
		Skinit,
		// Token: 0x040019E6 RID: 6630
		Invlpgaw,
		// Token: 0x040019E7 RID: 6631
		Invlpgad,
		// Token: 0x040019E8 RID: 6632
		Invlpgaq,
		// Token: 0x040019E9 RID: 6633
		Setssbsy,
		// Token: 0x040019EA RID: 6634
		Saveprevssp,
		// Token: 0x040019EB RID: 6635
		Rdpkru,
		// Token: 0x040019EC RID: 6636
		Wrpkru,
		// Token: 0x040019ED RID: 6637
		Swapgs,
		// Token: 0x040019EE RID: 6638
		Rdtscp,
		// Token: 0x040019EF RID: 6639
		Monitorxw,
		// Token: 0x040019F0 RID: 6640
		Monitorxd,
		// Token: 0x040019F1 RID: 6641
		Monitorxq,
		// Token: 0x040019F2 RID: 6642
		Mcommit,
		// Token: 0x040019F3 RID: 6643
		Mwaitx,
		// Token: 0x040019F4 RID: 6644
		Clzerow,
		// Token: 0x040019F5 RID: 6645
		Clzerod,
		// Token: 0x040019F6 RID: 6646
		Clzeroq,
		// Token: 0x040019F7 RID: 6647
		Rdpru,
		// Token: 0x040019F8 RID: 6648
		Lar_r16_rm16,
		// Token: 0x040019F9 RID: 6649
		Lar_r32_r32m16,
		// Token: 0x040019FA RID: 6650
		Lar_r64_r64m16,
		// Token: 0x040019FB RID: 6651
		Lsl_r16_rm16,
		// Token: 0x040019FC RID: 6652
		Lsl_r32_r32m16,
		// Token: 0x040019FD RID: 6653
		Lsl_r64_r64m16,
		// Token: 0x040019FE RID: 6654
		Storeall,
		// Token: 0x040019FF RID: 6655
		Loadall286,
		// Token: 0x04001A00 RID: 6656
		Syscall,
		// Token: 0x04001A01 RID: 6657
		Clts,
		// Token: 0x04001A02 RID: 6658
		Loadall386,
		// Token: 0x04001A03 RID: 6659
		Sysretd,
		// Token: 0x04001A04 RID: 6660
		Sysretq,
		// Token: 0x04001A05 RID: 6661
		Invd,
		// Token: 0x04001A06 RID: 6662
		Wbinvd,
		// Token: 0x04001A07 RID: 6663
		Wbnoinvd,
		// Token: 0x04001A08 RID: 6664
		Cl1invmb,
		// Token: 0x04001A09 RID: 6665
		Ud2,
		// Token: 0x04001A0A RID: 6666
		Reservednop_rm16_r16_0F0D,
		// Token: 0x04001A0B RID: 6667
		Reservednop_rm32_r32_0F0D,
		// Token: 0x04001A0C RID: 6668
		Reservednop_rm64_r64_0F0D,
		// Token: 0x04001A0D RID: 6669
		Prefetch_m8,
		// Token: 0x04001A0E RID: 6670
		Prefetchw_m8,
		// Token: 0x04001A0F RID: 6671
		Prefetchwt1_m8,
		// Token: 0x04001A10 RID: 6672
		Femms,
		// Token: 0x04001A11 RID: 6673
		Umov_rm8_r8,
		// Token: 0x04001A12 RID: 6674
		Umov_rm16_r16,
		// Token: 0x04001A13 RID: 6675
		Umov_rm32_r32,
		// Token: 0x04001A14 RID: 6676
		Umov_r8_rm8,
		// Token: 0x04001A15 RID: 6677
		Umov_r16_rm16,
		// Token: 0x04001A16 RID: 6678
		Umov_r32_rm32,
		// Token: 0x04001A17 RID: 6679
		Movups_xmm_xmmm128,
		// Token: 0x04001A18 RID: 6680
		VEX_Vmovups_xmm_xmmm128,
		// Token: 0x04001A19 RID: 6681
		VEX_Vmovups_ymm_ymmm256,
		// Token: 0x04001A1A RID: 6682
		EVEX_Vmovups_xmm_k1z_xmmm128,
		// Token: 0x04001A1B RID: 6683
		EVEX_Vmovups_ymm_k1z_ymmm256,
		// Token: 0x04001A1C RID: 6684
		EVEX_Vmovups_zmm_k1z_zmmm512,
		// Token: 0x04001A1D RID: 6685
		Movupd_xmm_xmmm128,
		// Token: 0x04001A1E RID: 6686
		VEX_Vmovupd_xmm_xmmm128,
		// Token: 0x04001A1F RID: 6687
		VEX_Vmovupd_ymm_ymmm256,
		// Token: 0x04001A20 RID: 6688
		EVEX_Vmovupd_xmm_k1z_xmmm128,
		// Token: 0x04001A21 RID: 6689
		EVEX_Vmovupd_ymm_k1z_ymmm256,
		// Token: 0x04001A22 RID: 6690
		EVEX_Vmovupd_zmm_k1z_zmmm512,
		// Token: 0x04001A23 RID: 6691
		Movss_xmm_xmmm32,
		// Token: 0x04001A24 RID: 6692
		VEX_Vmovss_xmm_xmm_xmm,
		// Token: 0x04001A25 RID: 6693
		VEX_Vmovss_xmm_m32,
		// Token: 0x04001A26 RID: 6694
		EVEX_Vmovss_xmm_k1z_xmm_xmm,
		// Token: 0x04001A27 RID: 6695
		EVEX_Vmovss_xmm_k1z_m32,
		// Token: 0x04001A28 RID: 6696
		Movsd_xmm_xmmm64,
		// Token: 0x04001A29 RID: 6697
		VEX_Vmovsd_xmm_xmm_xmm,
		// Token: 0x04001A2A RID: 6698
		VEX_Vmovsd_xmm_m64,
		// Token: 0x04001A2B RID: 6699
		EVEX_Vmovsd_xmm_k1z_xmm_xmm,
		// Token: 0x04001A2C RID: 6700
		EVEX_Vmovsd_xmm_k1z_m64,
		// Token: 0x04001A2D RID: 6701
		Movups_xmmm128_xmm,
		// Token: 0x04001A2E RID: 6702
		VEX_Vmovups_xmmm128_xmm,
		// Token: 0x04001A2F RID: 6703
		VEX_Vmovups_ymmm256_ymm,
		// Token: 0x04001A30 RID: 6704
		EVEX_Vmovups_xmmm128_k1z_xmm,
		// Token: 0x04001A31 RID: 6705
		EVEX_Vmovups_ymmm256_k1z_ymm,
		// Token: 0x04001A32 RID: 6706
		EVEX_Vmovups_zmmm512_k1z_zmm,
		// Token: 0x04001A33 RID: 6707
		Movupd_xmmm128_xmm,
		// Token: 0x04001A34 RID: 6708
		VEX_Vmovupd_xmmm128_xmm,
		// Token: 0x04001A35 RID: 6709
		VEX_Vmovupd_ymmm256_ymm,
		// Token: 0x04001A36 RID: 6710
		EVEX_Vmovupd_xmmm128_k1z_xmm,
		// Token: 0x04001A37 RID: 6711
		EVEX_Vmovupd_ymmm256_k1z_ymm,
		// Token: 0x04001A38 RID: 6712
		EVEX_Vmovupd_zmmm512_k1z_zmm,
		// Token: 0x04001A39 RID: 6713
		Movss_xmmm32_xmm,
		// Token: 0x04001A3A RID: 6714
		VEX_Vmovss_xmm_xmm_xmm_0F11,
		// Token: 0x04001A3B RID: 6715
		VEX_Vmovss_m32_xmm,
		// Token: 0x04001A3C RID: 6716
		EVEX_Vmovss_xmm_k1z_xmm_xmm_0F11,
		// Token: 0x04001A3D RID: 6717
		EVEX_Vmovss_m32_k1_xmm,
		// Token: 0x04001A3E RID: 6718
		Movsd_xmmm64_xmm,
		// Token: 0x04001A3F RID: 6719
		VEX_Vmovsd_xmm_xmm_xmm_0F11,
		// Token: 0x04001A40 RID: 6720
		VEX_Vmovsd_m64_xmm,
		// Token: 0x04001A41 RID: 6721
		EVEX_Vmovsd_xmm_k1z_xmm_xmm_0F11,
		// Token: 0x04001A42 RID: 6722
		EVEX_Vmovsd_m64_k1_xmm,
		// Token: 0x04001A43 RID: 6723
		Movhlps_xmm_xmm,
		// Token: 0x04001A44 RID: 6724
		Movlps_xmm_m64,
		// Token: 0x04001A45 RID: 6725
		VEX_Vmovhlps_xmm_xmm_xmm,
		// Token: 0x04001A46 RID: 6726
		VEX_Vmovlps_xmm_xmm_m64,
		// Token: 0x04001A47 RID: 6727
		EVEX_Vmovhlps_xmm_xmm_xmm,
		// Token: 0x04001A48 RID: 6728
		EVEX_Vmovlps_xmm_xmm_m64,
		// Token: 0x04001A49 RID: 6729
		Movlpd_xmm_m64,
		// Token: 0x04001A4A RID: 6730
		VEX_Vmovlpd_xmm_xmm_m64,
		// Token: 0x04001A4B RID: 6731
		EVEX_Vmovlpd_xmm_xmm_m64,
		// Token: 0x04001A4C RID: 6732
		Movsldup_xmm_xmmm128,
		// Token: 0x04001A4D RID: 6733
		VEX_Vmovsldup_xmm_xmmm128,
		// Token: 0x04001A4E RID: 6734
		VEX_Vmovsldup_ymm_ymmm256,
		// Token: 0x04001A4F RID: 6735
		EVEX_Vmovsldup_xmm_k1z_xmmm128,
		// Token: 0x04001A50 RID: 6736
		EVEX_Vmovsldup_ymm_k1z_ymmm256,
		// Token: 0x04001A51 RID: 6737
		EVEX_Vmovsldup_zmm_k1z_zmmm512,
		// Token: 0x04001A52 RID: 6738
		Movddup_xmm_xmmm64,
		// Token: 0x04001A53 RID: 6739
		VEX_Vmovddup_xmm_xmmm64,
		// Token: 0x04001A54 RID: 6740
		VEX_Vmovddup_ymm_ymmm256,
		// Token: 0x04001A55 RID: 6741
		EVEX_Vmovddup_xmm_k1z_xmmm64,
		// Token: 0x04001A56 RID: 6742
		EVEX_Vmovddup_ymm_k1z_ymmm256,
		// Token: 0x04001A57 RID: 6743
		EVEX_Vmovddup_zmm_k1z_zmmm512,
		// Token: 0x04001A58 RID: 6744
		Movlps_m64_xmm,
		// Token: 0x04001A59 RID: 6745
		VEX_Vmovlps_m64_xmm,
		// Token: 0x04001A5A RID: 6746
		EVEX_Vmovlps_m64_xmm,
		// Token: 0x04001A5B RID: 6747
		Movlpd_m64_xmm,
		// Token: 0x04001A5C RID: 6748
		VEX_Vmovlpd_m64_xmm,
		// Token: 0x04001A5D RID: 6749
		EVEX_Vmovlpd_m64_xmm,
		// Token: 0x04001A5E RID: 6750
		Unpcklps_xmm_xmmm128,
		// Token: 0x04001A5F RID: 6751
		VEX_Vunpcklps_xmm_xmm_xmmm128,
		// Token: 0x04001A60 RID: 6752
		VEX_Vunpcklps_ymm_ymm_ymmm256,
		// Token: 0x04001A61 RID: 6753
		EVEX_Vunpcklps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001A62 RID: 6754
		EVEX_Vunpcklps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001A63 RID: 6755
		EVEX_Vunpcklps_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04001A64 RID: 6756
		Unpcklpd_xmm_xmmm128,
		// Token: 0x04001A65 RID: 6757
		VEX_Vunpcklpd_xmm_xmm_xmmm128,
		// Token: 0x04001A66 RID: 6758
		VEX_Vunpcklpd_ymm_ymm_ymmm256,
		// Token: 0x04001A67 RID: 6759
		EVEX_Vunpcklpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001A68 RID: 6760
		EVEX_Vunpcklpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001A69 RID: 6761
		EVEX_Vunpcklpd_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04001A6A RID: 6762
		Unpckhps_xmm_xmmm128,
		// Token: 0x04001A6B RID: 6763
		VEX_Vunpckhps_xmm_xmm_xmmm128,
		// Token: 0x04001A6C RID: 6764
		VEX_Vunpckhps_ymm_ymm_ymmm256,
		// Token: 0x04001A6D RID: 6765
		EVEX_Vunpckhps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001A6E RID: 6766
		EVEX_Vunpckhps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001A6F RID: 6767
		EVEX_Vunpckhps_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04001A70 RID: 6768
		Unpckhpd_xmm_xmmm128,
		// Token: 0x04001A71 RID: 6769
		VEX_Vunpckhpd_xmm_xmm_xmmm128,
		// Token: 0x04001A72 RID: 6770
		VEX_Vunpckhpd_ymm_ymm_ymmm256,
		// Token: 0x04001A73 RID: 6771
		EVEX_Vunpckhpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001A74 RID: 6772
		EVEX_Vunpckhpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001A75 RID: 6773
		EVEX_Vunpckhpd_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04001A76 RID: 6774
		Movlhps_xmm_xmm,
		// Token: 0x04001A77 RID: 6775
		VEX_Vmovlhps_xmm_xmm_xmm,
		// Token: 0x04001A78 RID: 6776
		EVEX_Vmovlhps_xmm_xmm_xmm,
		// Token: 0x04001A79 RID: 6777
		Movhps_xmm_m64,
		// Token: 0x04001A7A RID: 6778
		VEX_Vmovhps_xmm_xmm_m64,
		// Token: 0x04001A7B RID: 6779
		EVEX_Vmovhps_xmm_xmm_m64,
		// Token: 0x04001A7C RID: 6780
		Movhpd_xmm_m64,
		// Token: 0x04001A7D RID: 6781
		VEX_Vmovhpd_xmm_xmm_m64,
		// Token: 0x04001A7E RID: 6782
		EVEX_Vmovhpd_xmm_xmm_m64,
		// Token: 0x04001A7F RID: 6783
		Movshdup_xmm_xmmm128,
		// Token: 0x04001A80 RID: 6784
		VEX_Vmovshdup_xmm_xmmm128,
		// Token: 0x04001A81 RID: 6785
		VEX_Vmovshdup_ymm_ymmm256,
		// Token: 0x04001A82 RID: 6786
		EVEX_Vmovshdup_xmm_k1z_xmmm128,
		// Token: 0x04001A83 RID: 6787
		EVEX_Vmovshdup_ymm_k1z_ymmm256,
		// Token: 0x04001A84 RID: 6788
		EVEX_Vmovshdup_zmm_k1z_zmmm512,
		// Token: 0x04001A85 RID: 6789
		Movhps_m64_xmm,
		// Token: 0x04001A86 RID: 6790
		VEX_Vmovhps_m64_xmm,
		// Token: 0x04001A87 RID: 6791
		EVEX_Vmovhps_m64_xmm,
		// Token: 0x04001A88 RID: 6792
		Movhpd_m64_xmm,
		// Token: 0x04001A89 RID: 6793
		VEX_Vmovhpd_m64_xmm,
		// Token: 0x04001A8A RID: 6794
		EVEX_Vmovhpd_m64_xmm,
		// Token: 0x04001A8B RID: 6795
		Reservednop_rm16_r16_0F18,
		// Token: 0x04001A8C RID: 6796
		Reservednop_rm32_r32_0F18,
		// Token: 0x04001A8D RID: 6797
		Reservednop_rm64_r64_0F18,
		// Token: 0x04001A8E RID: 6798
		Reservednop_rm16_r16_0F19,
		// Token: 0x04001A8F RID: 6799
		Reservednop_rm32_r32_0F19,
		// Token: 0x04001A90 RID: 6800
		Reservednop_rm64_r64_0F19,
		// Token: 0x04001A91 RID: 6801
		Reservednop_rm16_r16_0F1A,
		// Token: 0x04001A92 RID: 6802
		Reservednop_rm32_r32_0F1A,
		// Token: 0x04001A93 RID: 6803
		Reservednop_rm64_r64_0F1A,
		// Token: 0x04001A94 RID: 6804
		Reservednop_rm16_r16_0F1B,
		// Token: 0x04001A95 RID: 6805
		Reservednop_rm32_r32_0F1B,
		// Token: 0x04001A96 RID: 6806
		Reservednop_rm64_r64_0F1B,
		// Token: 0x04001A97 RID: 6807
		Reservednop_rm16_r16_0F1C,
		// Token: 0x04001A98 RID: 6808
		Reservednop_rm32_r32_0F1C,
		// Token: 0x04001A99 RID: 6809
		Reservednop_rm64_r64_0F1C,
		// Token: 0x04001A9A RID: 6810
		Reservednop_rm16_r16_0F1D,
		// Token: 0x04001A9B RID: 6811
		Reservednop_rm32_r32_0F1D,
		// Token: 0x04001A9C RID: 6812
		Reservednop_rm64_r64_0F1D,
		// Token: 0x04001A9D RID: 6813
		Reservednop_rm16_r16_0F1E,
		// Token: 0x04001A9E RID: 6814
		Reservednop_rm32_r32_0F1E,
		// Token: 0x04001A9F RID: 6815
		Reservednop_rm64_r64_0F1E,
		// Token: 0x04001AA0 RID: 6816
		Reservednop_rm16_r16_0F1F,
		// Token: 0x04001AA1 RID: 6817
		Reservednop_rm32_r32_0F1F,
		// Token: 0x04001AA2 RID: 6818
		Reservednop_rm64_r64_0F1F,
		// Token: 0x04001AA3 RID: 6819
		Prefetchnta_m8,
		// Token: 0x04001AA4 RID: 6820
		Prefetcht0_m8,
		// Token: 0x04001AA5 RID: 6821
		Prefetcht1_m8,
		// Token: 0x04001AA6 RID: 6822
		Prefetcht2_m8,
		// Token: 0x04001AA7 RID: 6823
		Bndldx_bnd_mib,
		// Token: 0x04001AA8 RID: 6824
		Bndmov_bnd_bndm64,
		// Token: 0x04001AA9 RID: 6825
		Bndmov_bnd_bndm128,
		// Token: 0x04001AAA RID: 6826
		Bndcl_bnd_rm32,
		// Token: 0x04001AAB RID: 6827
		Bndcl_bnd_rm64,
		// Token: 0x04001AAC RID: 6828
		Bndcu_bnd_rm32,
		// Token: 0x04001AAD RID: 6829
		Bndcu_bnd_rm64,
		// Token: 0x04001AAE RID: 6830
		Bndstx_mib_bnd,
		// Token: 0x04001AAF RID: 6831
		Bndmov_bndm64_bnd,
		// Token: 0x04001AB0 RID: 6832
		Bndmov_bndm128_bnd,
		// Token: 0x04001AB1 RID: 6833
		Bndmk_bnd_m32,
		// Token: 0x04001AB2 RID: 6834
		Bndmk_bnd_m64,
		// Token: 0x04001AB3 RID: 6835
		Bndcn_bnd_rm32,
		// Token: 0x04001AB4 RID: 6836
		Bndcn_bnd_rm64,
		// Token: 0x04001AB5 RID: 6837
		Cldemote_m8,
		// Token: 0x04001AB6 RID: 6838
		Rdsspd_r32,
		// Token: 0x04001AB7 RID: 6839
		Rdsspq_r64,
		// Token: 0x04001AB8 RID: 6840
		Endbr64,
		// Token: 0x04001AB9 RID: 6841
		Endbr32,
		// Token: 0x04001ABA RID: 6842
		Nop_rm16,
		// Token: 0x04001ABB RID: 6843
		Nop_rm32,
		// Token: 0x04001ABC RID: 6844
		Nop_rm64,
		// Token: 0x04001ABD RID: 6845
		Mov_r32_cr,
		// Token: 0x04001ABE RID: 6846
		Mov_r64_cr,
		// Token: 0x04001ABF RID: 6847
		Mov_r32_dr,
		// Token: 0x04001AC0 RID: 6848
		Mov_r64_dr,
		// Token: 0x04001AC1 RID: 6849
		Mov_cr_r32,
		// Token: 0x04001AC2 RID: 6850
		Mov_cr_r64,
		// Token: 0x04001AC3 RID: 6851
		Mov_dr_r32,
		// Token: 0x04001AC4 RID: 6852
		Mov_dr_r64,
		// Token: 0x04001AC5 RID: 6853
		Mov_r32_tr,
		// Token: 0x04001AC6 RID: 6854
		Mov_tr_r32,
		// Token: 0x04001AC7 RID: 6855
		Movaps_xmm_xmmm128,
		// Token: 0x04001AC8 RID: 6856
		VEX_Vmovaps_xmm_xmmm128,
		// Token: 0x04001AC9 RID: 6857
		VEX_Vmovaps_ymm_ymmm256,
		// Token: 0x04001ACA RID: 6858
		EVEX_Vmovaps_xmm_k1z_xmmm128,
		// Token: 0x04001ACB RID: 6859
		EVEX_Vmovaps_ymm_k1z_ymmm256,
		// Token: 0x04001ACC RID: 6860
		EVEX_Vmovaps_zmm_k1z_zmmm512,
		// Token: 0x04001ACD RID: 6861
		Movapd_xmm_xmmm128,
		// Token: 0x04001ACE RID: 6862
		VEX_Vmovapd_xmm_xmmm128,
		// Token: 0x04001ACF RID: 6863
		VEX_Vmovapd_ymm_ymmm256,
		// Token: 0x04001AD0 RID: 6864
		EVEX_Vmovapd_xmm_k1z_xmmm128,
		// Token: 0x04001AD1 RID: 6865
		EVEX_Vmovapd_ymm_k1z_ymmm256,
		// Token: 0x04001AD2 RID: 6866
		EVEX_Vmovapd_zmm_k1z_zmmm512,
		// Token: 0x04001AD3 RID: 6867
		Movaps_xmmm128_xmm,
		// Token: 0x04001AD4 RID: 6868
		VEX_Vmovaps_xmmm128_xmm,
		// Token: 0x04001AD5 RID: 6869
		VEX_Vmovaps_ymmm256_ymm,
		// Token: 0x04001AD6 RID: 6870
		EVEX_Vmovaps_xmmm128_k1z_xmm,
		// Token: 0x04001AD7 RID: 6871
		EVEX_Vmovaps_ymmm256_k1z_ymm,
		// Token: 0x04001AD8 RID: 6872
		EVEX_Vmovaps_zmmm512_k1z_zmm,
		// Token: 0x04001AD9 RID: 6873
		Movapd_xmmm128_xmm,
		// Token: 0x04001ADA RID: 6874
		VEX_Vmovapd_xmmm128_xmm,
		// Token: 0x04001ADB RID: 6875
		VEX_Vmovapd_ymmm256_ymm,
		// Token: 0x04001ADC RID: 6876
		EVEX_Vmovapd_xmmm128_k1z_xmm,
		// Token: 0x04001ADD RID: 6877
		EVEX_Vmovapd_ymmm256_k1z_ymm,
		// Token: 0x04001ADE RID: 6878
		EVEX_Vmovapd_zmmm512_k1z_zmm,
		// Token: 0x04001ADF RID: 6879
		Cvtpi2ps_xmm_mmm64,
		// Token: 0x04001AE0 RID: 6880
		Cvtpi2pd_xmm_mmm64,
		// Token: 0x04001AE1 RID: 6881
		Cvtsi2ss_xmm_rm32,
		// Token: 0x04001AE2 RID: 6882
		Cvtsi2ss_xmm_rm64,
		// Token: 0x04001AE3 RID: 6883
		VEX_Vcvtsi2ss_xmm_xmm_rm32,
		// Token: 0x04001AE4 RID: 6884
		VEX_Vcvtsi2ss_xmm_xmm_rm64,
		// Token: 0x04001AE5 RID: 6885
		EVEX_Vcvtsi2ss_xmm_xmm_rm32_er,
		// Token: 0x04001AE6 RID: 6886
		EVEX_Vcvtsi2ss_xmm_xmm_rm64_er,
		// Token: 0x04001AE7 RID: 6887
		Cvtsi2sd_xmm_rm32,
		// Token: 0x04001AE8 RID: 6888
		Cvtsi2sd_xmm_rm64,
		// Token: 0x04001AE9 RID: 6889
		VEX_Vcvtsi2sd_xmm_xmm_rm32,
		// Token: 0x04001AEA RID: 6890
		VEX_Vcvtsi2sd_xmm_xmm_rm64,
		// Token: 0x04001AEB RID: 6891
		EVEX_Vcvtsi2sd_xmm_xmm_rm32_er,
		// Token: 0x04001AEC RID: 6892
		EVEX_Vcvtsi2sd_xmm_xmm_rm64_er,
		// Token: 0x04001AED RID: 6893
		Movntps_m128_xmm,
		// Token: 0x04001AEE RID: 6894
		VEX_Vmovntps_m128_xmm,
		// Token: 0x04001AEF RID: 6895
		VEX_Vmovntps_m256_ymm,
		// Token: 0x04001AF0 RID: 6896
		EVEX_Vmovntps_m128_xmm,
		// Token: 0x04001AF1 RID: 6897
		EVEX_Vmovntps_m256_ymm,
		// Token: 0x04001AF2 RID: 6898
		EVEX_Vmovntps_m512_zmm,
		// Token: 0x04001AF3 RID: 6899
		Movntpd_m128_xmm,
		// Token: 0x04001AF4 RID: 6900
		VEX_Vmovntpd_m128_xmm,
		// Token: 0x04001AF5 RID: 6901
		VEX_Vmovntpd_m256_ymm,
		// Token: 0x04001AF6 RID: 6902
		EVEX_Vmovntpd_m128_xmm,
		// Token: 0x04001AF7 RID: 6903
		EVEX_Vmovntpd_m256_ymm,
		// Token: 0x04001AF8 RID: 6904
		EVEX_Vmovntpd_m512_zmm,
		// Token: 0x04001AF9 RID: 6905
		Movntss_m32_xmm,
		// Token: 0x04001AFA RID: 6906
		Movntsd_m64_xmm,
		// Token: 0x04001AFB RID: 6907
		Cvttps2pi_mm_xmmm64,
		// Token: 0x04001AFC RID: 6908
		Cvttpd2pi_mm_xmmm128,
		// Token: 0x04001AFD RID: 6909
		Cvttss2si_r32_xmmm32,
		// Token: 0x04001AFE RID: 6910
		Cvttss2si_r64_xmmm32,
		// Token: 0x04001AFF RID: 6911
		VEX_Vcvttss2si_r32_xmmm32,
		// Token: 0x04001B00 RID: 6912
		VEX_Vcvttss2si_r64_xmmm32,
		// Token: 0x04001B01 RID: 6913
		EVEX_Vcvttss2si_r32_xmmm32_sae,
		// Token: 0x04001B02 RID: 6914
		EVEX_Vcvttss2si_r64_xmmm32_sae,
		// Token: 0x04001B03 RID: 6915
		Cvttsd2si_r32_xmmm64,
		// Token: 0x04001B04 RID: 6916
		Cvttsd2si_r64_xmmm64,
		// Token: 0x04001B05 RID: 6917
		VEX_Vcvttsd2si_r32_xmmm64,
		// Token: 0x04001B06 RID: 6918
		VEX_Vcvttsd2si_r64_xmmm64,
		// Token: 0x04001B07 RID: 6919
		EVEX_Vcvttsd2si_r32_xmmm64_sae,
		// Token: 0x04001B08 RID: 6920
		EVEX_Vcvttsd2si_r64_xmmm64_sae,
		// Token: 0x04001B09 RID: 6921
		Cvtps2pi_mm_xmmm64,
		// Token: 0x04001B0A RID: 6922
		Cvtpd2pi_mm_xmmm128,
		// Token: 0x04001B0B RID: 6923
		Cvtss2si_r32_xmmm32,
		// Token: 0x04001B0C RID: 6924
		Cvtss2si_r64_xmmm32,
		// Token: 0x04001B0D RID: 6925
		VEX_Vcvtss2si_r32_xmmm32,
		// Token: 0x04001B0E RID: 6926
		VEX_Vcvtss2si_r64_xmmm32,
		// Token: 0x04001B0F RID: 6927
		EVEX_Vcvtss2si_r32_xmmm32_er,
		// Token: 0x04001B10 RID: 6928
		EVEX_Vcvtss2si_r64_xmmm32_er,
		// Token: 0x04001B11 RID: 6929
		Cvtsd2si_r32_xmmm64,
		// Token: 0x04001B12 RID: 6930
		Cvtsd2si_r64_xmmm64,
		// Token: 0x04001B13 RID: 6931
		VEX_Vcvtsd2si_r32_xmmm64,
		// Token: 0x04001B14 RID: 6932
		VEX_Vcvtsd2si_r64_xmmm64,
		// Token: 0x04001B15 RID: 6933
		EVEX_Vcvtsd2si_r32_xmmm64_er,
		// Token: 0x04001B16 RID: 6934
		EVEX_Vcvtsd2si_r64_xmmm64_er,
		// Token: 0x04001B17 RID: 6935
		Ucomiss_xmm_xmmm32,
		// Token: 0x04001B18 RID: 6936
		VEX_Vucomiss_xmm_xmmm32,
		// Token: 0x04001B19 RID: 6937
		EVEX_Vucomiss_xmm_xmmm32_sae,
		// Token: 0x04001B1A RID: 6938
		Ucomisd_xmm_xmmm64,
		// Token: 0x04001B1B RID: 6939
		VEX_Vucomisd_xmm_xmmm64,
		// Token: 0x04001B1C RID: 6940
		EVEX_Vucomisd_xmm_xmmm64_sae,
		// Token: 0x04001B1D RID: 6941
		Comiss_xmm_xmmm32,
		// Token: 0x04001B1E RID: 6942
		Comisd_xmm_xmmm64,
		// Token: 0x04001B1F RID: 6943
		VEX_Vcomiss_xmm_xmmm32,
		// Token: 0x04001B20 RID: 6944
		VEX_Vcomisd_xmm_xmmm64,
		// Token: 0x04001B21 RID: 6945
		EVEX_Vcomiss_xmm_xmmm32_sae,
		// Token: 0x04001B22 RID: 6946
		EVEX_Vcomisd_xmm_xmmm64_sae,
		// Token: 0x04001B23 RID: 6947
		Wrmsr,
		// Token: 0x04001B24 RID: 6948
		Rdtsc,
		// Token: 0x04001B25 RID: 6949
		Rdmsr,
		// Token: 0x04001B26 RID: 6950
		Rdpmc,
		// Token: 0x04001B27 RID: 6951
		Sysenter,
		// Token: 0x04001B28 RID: 6952
		Sysexitd,
		// Token: 0x04001B29 RID: 6953
		Sysexitq,
		// Token: 0x04001B2A RID: 6954
		Getsecd,
		// Token: 0x04001B2B RID: 6955
		Cmovo_r16_rm16,
		// Token: 0x04001B2C RID: 6956
		Cmovo_r32_rm32,
		// Token: 0x04001B2D RID: 6957
		Cmovo_r64_rm64,
		// Token: 0x04001B2E RID: 6958
		Cmovno_r16_rm16,
		// Token: 0x04001B2F RID: 6959
		Cmovno_r32_rm32,
		// Token: 0x04001B30 RID: 6960
		Cmovno_r64_rm64,
		// Token: 0x04001B31 RID: 6961
		Cmovb_r16_rm16,
		// Token: 0x04001B32 RID: 6962
		Cmovb_r32_rm32,
		// Token: 0x04001B33 RID: 6963
		Cmovb_r64_rm64,
		// Token: 0x04001B34 RID: 6964
		Cmovae_r16_rm16,
		// Token: 0x04001B35 RID: 6965
		Cmovae_r32_rm32,
		// Token: 0x04001B36 RID: 6966
		Cmovae_r64_rm64,
		// Token: 0x04001B37 RID: 6967
		Cmove_r16_rm16,
		// Token: 0x04001B38 RID: 6968
		Cmove_r32_rm32,
		// Token: 0x04001B39 RID: 6969
		Cmove_r64_rm64,
		// Token: 0x04001B3A RID: 6970
		Cmovne_r16_rm16,
		// Token: 0x04001B3B RID: 6971
		Cmovne_r32_rm32,
		// Token: 0x04001B3C RID: 6972
		Cmovne_r64_rm64,
		// Token: 0x04001B3D RID: 6973
		Cmovbe_r16_rm16,
		// Token: 0x04001B3E RID: 6974
		Cmovbe_r32_rm32,
		// Token: 0x04001B3F RID: 6975
		Cmovbe_r64_rm64,
		// Token: 0x04001B40 RID: 6976
		Cmova_r16_rm16,
		// Token: 0x04001B41 RID: 6977
		Cmova_r32_rm32,
		// Token: 0x04001B42 RID: 6978
		Cmova_r64_rm64,
		// Token: 0x04001B43 RID: 6979
		Cmovs_r16_rm16,
		// Token: 0x04001B44 RID: 6980
		Cmovs_r32_rm32,
		// Token: 0x04001B45 RID: 6981
		Cmovs_r64_rm64,
		// Token: 0x04001B46 RID: 6982
		Cmovns_r16_rm16,
		// Token: 0x04001B47 RID: 6983
		Cmovns_r32_rm32,
		// Token: 0x04001B48 RID: 6984
		Cmovns_r64_rm64,
		// Token: 0x04001B49 RID: 6985
		Cmovp_r16_rm16,
		// Token: 0x04001B4A RID: 6986
		Cmovp_r32_rm32,
		// Token: 0x04001B4B RID: 6987
		Cmovp_r64_rm64,
		// Token: 0x04001B4C RID: 6988
		Cmovnp_r16_rm16,
		// Token: 0x04001B4D RID: 6989
		Cmovnp_r32_rm32,
		// Token: 0x04001B4E RID: 6990
		Cmovnp_r64_rm64,
		// Token: 0x04001B4F RID: 6991
		Cmovl_r16_rm16,
		// Token: 0x04001B50 RID: 6992
		Cmovl_r32_rm32,
		// Token: 0x04001B51 RID: 6993
		Cmovl_r64_rm64,
		// Token: 0x04001B52 RID: 6994
		Cmovge_r16_rm16,
		// Token: 0x04001B53 RID: 6995
		Cmovge_r32_rm32,
		// Token: 0x04001B54 RID: 6996
		Cmovge_r64_rm64,
		// Token: 0x04001B55 RID: 6997
		Cmovle_r16_rm16,
		// Token: 0x04001B56 RID: 6998
		Cmovle_r32_rm32,
		// Token: 0x04001B57 RID: 6999
		Cmovle_r64_rm64,
		// Token: 0x04001B58 RID: 7000
		Cmovg_r16_rm16,
		// Token: 0x04001B59 RID: 7001
		Cmovg_r32_rm32,
		// Token: 0x04001B5A RID: 7002
		Cmovg_r64_rm64,
		// Token: 0x04001B5B RID: 7003
		VEX_Kandw_kr_kr_kr,
		// Token: 0x04001B5C RID: 7004
		VEX_Kandq_kr_kr_kr,
		// Token: 0x04001B5D RID: 7005
		VEX_Kandb_kr_kr_kr,
		// Token: 0x04001B5E RID: 7006
		VEX_Kandd_kr_kr_kr,
		// Token: 0x04001B5F RID: 7007
		VEX_Kandnw_kr_kr_kr,
		// Token: 0x04001B60 RID: 7008
		VEX_Kandnq_kr_kr_kr,
		// Token: 0x04001B61 RID: 7009
		VEX_Kandnb_kr_kr_kr,
		// Token: 0x04001B62 RID: 7010
		VEX_Kandnd_kr_kr_kr,
		// Token: 0x04001B63 RID: 7011
		VEX_Knotw_kr_kr,
		// Token: 0x04001B64 RID: 7012
		VEX_Knotq_kr_kr,
		// Token: 0x04001B65 RID: 7013
		VEX_Knotb_kr_kr,
		// Token: 0x04001B66 RID: 7014
		VEX_Knotd_kr_kr,
		// Token: 0x04001B67 RID: 7015
		VEX_Korw_kr_kr_kr,
		// Token: 0x04001B68 RID: 7016
		VEX_Korq_kr_kr_kr,
		// Token: 0x04001B69 RID: 7017
		VEX_Korb_kr_kr_kr,
		// Token: 0x04001B6A RID: 7018
		VEX_Kord_kr_kr_kr,
		// Token: 0x04001B6B RID: 7019
		VEX_Kxnorw_kr_kr_kr,
		// Token: 0x04001B6C RID: 7020
		VEX_Kxnorq_kr_kr_kr,
		// Token: 0x04001B6D RID: 7021
		VEX_Kxnorb_kr_kr_kr,
		// Token: 0x04001B6E RID: 7022
		VEX_Kxnord_kr_kr_kr,
		// Token: 0x04001B6F RID: 7023
		VEX_Kxorw_kr_kr_kr,
		// Token: 0x04001B70 RID: 7024
		VEX_Kxorq_kr_kr_kr,
		// Token: 0x04001B71 RID: 7025
		VEX_Kxorb_kr_kr_kr,
		// Token: 0x04001B72 RID: 7026
		VEX_Kxord_kr_kr_kr,
		// Token: 0x04001B73 RID: 7027
		VEX_Kaddw_kr_kr_kr,
		// Token: 0x04001B74 RID: 7028
		VEX_Kaddq_kr_kr_kr,
		// Token: 0x04001B75 RID: 7029
		VEX_Kaddb_kr_kr_kr,
		// Token: 0x04001B76 RID: 7030
		VEX_Kaddd_kr_kr_kr,
		// Token: 0x04001B77 RID: 7031
		VEX_Kunpckwd_kr_kr_kr,
		// Token: 0x04001B78 RID: 7032
		VEX_Kunpckdq_kr_kr_kr,
		// Token: 0x04001B79 RID: 7033
		VEX_Kunpckbw_kr_kr_kr,
		// Token: 0x04001B7A RID: 7034
		Movmskps_r32_xmm,
		// Token: 0x04001B7B RID: 7035
		Movmskps_r64_xmm,
		// Token: 0x04001B7C RID: 7036
		VEX_Vmovmskps_r32_xmm,
		// Token: 0x04001B7D RID: 7037
		VEX_Vmovmskps_r64_xmm,
		// Token: 0x04001B7E RID: 7038
		VEX_Vmovmskps_r32_ymm,
		// Token: 0x04001B7F RID: 7039
		VEX_Vmovmskps_r64_ymm,
		// Token: 0x04001B80 RID: 7040
		Movmskpd_r32_xmm,
		// Token: 0x04001B81 RID: 7041
		Movmskpd_r64_xmm,
		// Token: 0x04001B82 RID: 7042
		VEX_Vmovmskpd_r32_xmm,
		// Token: 0x04001B83 RID: 7043
		VEX_Vmovmskpd_r64_xmm,
		// Token: 0x04001B84 RID: 7044
		VEX_Vmovmskpd_r32_ymm,
		// Token: 0x04001B85 RID: 7045
		VEX_Vmovmskpd_r64_ymm,
		// Token: 0x04001B86 RID: 7046
		Sqrtps_xmm_xmmm128,
		// Token: 0x04001B87 RID: 7047
		VEX_Vsqrtps_xmm_xmmm128,
		// Token: 0x04001B88 RID: 7048
		VEX_Vsqrtps_ymm_ymmm256,
		// Token: 0x04001B89 RID: 7049
		EVEX_Vsqrtps_xmm_k1z_xmmm128b32,
		// Token: 0x04001B8A RID: 7050
		EVEX_Vsqrtps_ymm_k1z_ymmm256b32,
		// Token: 0x04001B8B RID: 7051
		EVEX_Vsqrtps_zmm_k1z_zmmm512b32_er,
		// Token: 0x04001B8C RID: 7052
		Sqrtpd_xmm_xmmm128,
		// Token: 0x04001B8D RID: 7053
		VEX_Vsqrtpd_xmm_xmmm128,
		// Token: 0x04001B8E RID: 7054
		VEX_Vsqrtpd_ymm_ymmm256,
		// Token: 0x04001B8F RID: 7055
		EVEX_Vsqrtpd_xmm_k1z_xmmm128b64,
		// Token: 0x04001B90 RID: 7056
		EVEX_Vsqrtpd_ymm_k1z_ymmm256b64,
		// Token: 0x04001B91 RID: 7057
		EVEX_Vsqrtpd_zmm_k1z_zmmm512b64_er,
		// Token: 0x04001B92 RID: 7058
		Sqrtss_xmm_xmmm32,
		// Token: 0x04001B93 RID: 7059
		VEX_Vsqrtss_xmm_xmm_xmmm32,
		// Token: 0x04001B94 RID: 7060
		EVEX_Vsqrtss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x04001B95 RID: 7061
		Sqrtsd_xmm_xmmm64,
		// Token: 0x04001B96 RID: 7062
		VEX_Vsqrtsd_xmm_xmm_xmmm64,
		// Token: 0x04001B97 RID: 7063
		EVEX_Vsqrtsd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x04001B98 RID: 7064
		Rsqrtps_xmm_xmmm128,
		// Token: 0x04001B99 RID: 7065
		VEX_Vrsqrtps_xmm_xmmm128,
		// Token: 0x04001B9A RID: 7066
		VEX_Vrsqrtps_ymm_ymmm256,
		// Token: 0x04001B9B RID: 7067
		Rsqrtss_xmm_xmmm32,
		// Token: 0x04001B9C RID: 7068
		VEX_Vrsqrtss_xmm_xmm_xmmm32,
		// Token: 0x04001B9D RID: 7069
		Rcpps_xmm_xmmm128,
		// Token: 0x04001B9E RID: 7070
		VEX_Vrcpps_xmm_xmmm128,
		// Token: 0x04001B9F RID: 7071
		VEX_Vrcpps_ymm_ymmm256,
		// Token: 0x04001BA0 RID: 7072
		Rcpss_xmm_xmmm32,
		// Token: 0x04001BA1 RID: 7073
		VEX_Vrcpss_xmm_xmm_xmmm32,
		// Token: 0x04001BA2 RID: 7074
		Andps_xmm_xmmm128,
		// Token: 0x04001BA3 RID: 7075
		VEX_Vandps_xmm_xmm_xmmm128,
		// Token: 0x04001BA4 RID: 7076
		VEX_Vandps_ymm_ymm_ymmm256,
		// Token: 0x04001BA5 RID: 7077
		EVEX_Vandps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001BA6 RID: 7078
		EVEX_Vandps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001BA7 RID: 7079
		EVEX_Vandps_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04001BA8 RID: 7080
		Andpd_xmm_xmmm128,
		// Token: 0x04001BA9 RID: 7081
		VEX_Vandpd_xmm_xmm_xmmm128,
		// Token: 0x04001BAA RID: 7082
		VEX_Vandpd_ymm_ymm_ymmm256,
		// Token: 0x04001BAB RID: 7083
		EVEX_Vandpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001BAC RID: 7084
		EVEX_Vandpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001BAD RID: 7085
		EVEX_Vandpd_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04001BAE RID: 7086
		Andnps_xmm_xmmm128,
		// Token: 0x04001BAF RID: 7087
		VEX_Vandnps_xmm_xmm_xmmm128,
		// Token: 0x04001BB0 RID: 7088
		VEX_Vandnps_ymm_ymm_ymmm256,
		// Token: 0x04001BB1 RID: 7089
		EVEX_Vandnps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001BB2 RID: 7090
		EVEX_Vandnps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001BB3 RID: 7091
		EVEX_Vandnps_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04001BB4 RID: 7092
		Andnpd_xmm_xmmm128,
		// Token: 0x04001BB5 RID: 7093
		VEX_Vandnpd_xmm_xmm_xmmm128,
		// Token: 0x04001BB6 RID: 7094
		VEX_Vandnpd_ymm_ymm_ymmm256,
		// Token: 0x04001BB7 RID: 7095
		EVEX_Vandnpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001BB8 RID: 7096
		EVEX_Vandnpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001BB9 RID: 7097
		EVEX_Vandnpd_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04001BBA RID: 7098
		Orps_xmm_xmmm128,
		// Token: 0x04001BBB RID: 7099
		VEX_Vorps_xmm_xmm_xmmm128,
		// Token: 0x04001BBC RID: 7100
		VEX_Vorps_ymm_ymm_ymmm256,
		// Token: 0x04001BBD RID: 7101
		EVEX_Vorps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001BBE RID: 7102
		EVEX_Vorps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001BBF RID: 7103
		EVEX_Vorps_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04001BC0 RID: 7104
		Orpd_xmm_xmmm128,
		// Token: 0x04001BC1 RID: 7105
		VEX_Vorpd_xmm_xmm_xmmm128,
		// Token: 0x04001BC2 RID: 7106
		VEX_Vorpd_ymm_ymm_ymmm256,
		// Token: 0x04001BC3 RID: 7107
		EVEX_Vorpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001BC4 RID: 7108
		EVEX_Vorpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001BC5 RID: 7109
		EVEX_Vorpd_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04001BC6 RID: 7110
		Xorps_xmm_xmmm128,
		// Token: 0x04001BC7 RID: 7111
		VEX_Vxorps_xmm_xmm_xmmm128,
		// Token: 0x04001BC8 RID: 7112
		VEX_Vxorps_ymm_ymm_ymmm256,
		// Token: 0x04001BC9 RID: 7113
		EVEX_Vxorps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001BCA RID: 7114
		EVEX_Vxorps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001BCB RID: 7115
		EVEX_Vxorps_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04001BCC RID: 7116
		Xorpd_xmm_xmmm128,
		// Token: 0x04001BCD RID: 7117
		VEX_Vxorpd_xmm_xmm_xmmm128,
		// Token: 0x04001BCE RID: 7118
		VEX_Vxorpd_ymm_ymm_ymmm256,
		// Token: 0x04001BCF RID: 7119
		EVEX_Vxorpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001BD0 RID: 7120
		EVEX_Vxorpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001BD1 RID: 7121
		EVEX_Vxorpd_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04001BD2 RID: 7122
		Addps_xmm_xmmm128,
		// Token: 0x04001BD3 RID: 7123
		VEX_Vaddps_xmm_xmm_xmmm128,
		// Token: 0x04001BD4 RID: 7124
		VEX_Vaddps_ymm_ymm_ymmm256,
		// Token: 0x04001BD5 RID: 7125
		EVEX_Vaddps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001BD6 RID: 7126
		EVEX_Vaddps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001BD7 RID: 7127
		EVEX_Vaddps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x04001BD8 RID: 7128
		Addpd_xmm_xmmm128,
		// Token: 0x04001BD9 RID: 7129
		VEX_Vaddpd_xmm_xmm_xmmm128,
		// Token: 0x04001BDA RID: 7130
		VEX_Vaddpd_ymm_ymm_ymmm256,
		// Token: 0x04001BDB RID: 7131
		EVEX_Vaddpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001BDC RID: 7132
		EVEX_Vaddpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001BDD RID: 7133
		EVEX_Vaddpd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x04001BDE RID: 7134
		Addss_xmm_xmmm32,
		// Token: 0x04001BDF RID: 7135
		VEX_Vaddss_xmm_xmm_xmmm32,
		// Token: 0x04001BE0 RID: 7136
		EVEX_Vaddss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x04001BE1 RID: 7137
		Addsd_xmm_xmmm64,
		// Token: 0x04001BE2 RID: 7138
		VEX_Vaddsd_xmm_xmm_xmmm64,
		// Token: 0x04001BE3 RID: 7139
		EVEX_Vaddsd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x04001BE4 RID: 7140
		Mulps_xmm_xmmm128,
		// Token: 0x04001BE5 RID: 7141
		VEX_Vmulps_xmm_xmm_xmmm128,
		// Token: 0x04001BE6 RID: 7142
		VEX_Vmulps_ymm_ymm_ymmm256,
		// Token: 0x04001BE7 RID: 7143
		EVEX_Vmulps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001BE8 RID: 7144
		EVEX_Vmulps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001BE9 RID: 7145
		EVEX_Vmulps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x04001BEA RID: 7146
		Mulpd_xmm_xmmm128,
		// Token: 0x04001BEB RID: 7147
		VEX_Vmulpd_xmm_xmm_xmmm128,
		// Token: 0x04001BEC RID: 7148
		VEX_Vmulpd_ymm_ymm_ymmm256,
		// Token: 0x04001BED RID: 7149
		EVEX_Vmulpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001BEE RID: 7150
		EVEX_Vmulpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001BEF RID: 7151
		EVEX_Vmulpd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x04001BF0 RID: 7152
		Mulss_xmm_xmmm32,
		// Token: 0x04001BF1 RID: 7153
		VEX_Vmulss_xmm_xmm_xmmm32,
		// Token: 0x04001BF2 RID: 7154
		EVEX_Vmulss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x04001BF3 RID: 7155
		Mulsd_xmm_xmmm64,
		// Token: 0x04001BF4 RID: 7156
		VEX_Vmulsd_xmm_xmm_xmmm64,
		// Token: 0x04001BF5 RID: 7157
		EVEX_Vmulsd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x04001BF6 RID: 7158
		Cvtps2pd_xmm_xmmm64,
		// Token: 0x04001BF7 RID: 7159
		VEX_Vcvtps2pd_xmm_xmmm64,
		// Token: 0x04001BF8 RID: 7160
		VEX_Vcvtps2pd_ymm_xmmm128,
		// Token: 0x04001BF9 RID: 7161
		EVEX_Vcvtps2pd_xmm_k1z_xmmm64b32,
		// Token: 0x04001BFA RID: 7162
		EVEX_Vcvtps2pd_ymm_k1z_xmmm128b32,
		// Token: 0x04001BFB RID: 7163
		EVEX_Vcvtps2pd_zmm_k1z_ymmm256b32_sae,
		// Token: 0x04001BFC RID: 7164
		Cvtpd2ps_xmm_xmmm128,
		// Token: 0x04001BFD RID: 7165
		VEX_Vcvtpd2ps_xmm_xmmm128,
		// Token: 0x04001BFE RID: 7166
		VEX_Vcvtpd2ps_xmm_ymmm256,
		// Token: 0x04001BFF RID: 7167
		EVEX_Vcvtpd2ps_xmm_k1z_xmmm128b64,
		// Token: 0x04001C00 RID: 7168
		EVEX_Vcvtpd2ps_xmm_k1z_ymmm256b64,
		// Token: 0x04001C01 RID: 7169
		EVEX_Vcvtpd2ps_ymm_k1z_zmmm512b64_er,
		// Token: 0x04001C02 RID: 7170
		Cvtss2sd_xmm_xmmm32,
		// Token: 0x04001C03 RID: 7171
		VEX_Vcvtss2sd_xmm_xmm_xmmm32,
		// Token: 0x04001C04 RID: 7172
		EVEX_Vcvtss2sd_xmm_k1z_xmm_xmmm32_sae,
		// Token: 0x04001C05 RID: 7173
		Cvtsd2ss_xmm_xmmm64,
		// Token: 0x04001C06 RID: 7174
		VEX_Vcvtsd2ss_xmm_xmm_xmmm64,
		// Token: 0x04001C07 RID: 7175
		EVEX_Vcvtsd2ss_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x04001C08 RID: 7176
		Cvtdq2ps_xmm_xmmm128,
		// Token: 0x04001C09 RID: 7177
		VEX_Vcvtdq2ps_xmm_xmmm128,
		// Token: 0x04001C0A RID: 7178
		VEX_Vcvtdq2ps_ymm_ymmm256,
		// Token: 0x04001C0B RID: 7179
		EVEX_Vcvtdq2ps_xmm_k1z_xmmm128b32,
		// Token: 0x04001C0C RID: 7180
		EVEX_Vcvtdq2ps_ymm_k1z_ymmm256b32,
		// Token: 0x04001C0D RID: 7181
		EVEX_Vcvtdq2ps_zmm_k1z_zmmm512b32_er,
		// Token: 0x04001C0E RID: 7182
		EVEX_Vcvtqq2ps_xmm_k1z_xmmm128b64,
		// Token: 0x04001C0F RID: 7183
		EVEX_Vcvtqq2ps_xmm_k1z_ymmm256b64,
		// Token: 0x04001C10 RID: 7184
		EVEX_Vcvtqq2ps_ymm_k1z_zmmm512b64_er,
		// Token: 0x04001C11 RID: 7185
		Cvtps2dq_xmm_xmmm128,
		// Token: 0x04001C12 RID: 7186
		VEX_Vcvtps2dq_xmm_xmmm128,
		// Token: 0x04001C13 RID: 7187
		VEX_Vcvtps2dq_ymm_ymmm256,
		// Token: 0x04001C14 RID: 7188
		EVEX_Vcvtps2dq_xmm_k1z_xmmm128b32,
		// Token: 0x04001C15 RID: 7189
		EVEX_Vcvtps2dq_ymm_k1z_ymmm256b32,
		// Token: 0x04001C16 RID: 7190
		EVEX_Vcvtps2dq_zmm_k1z_zmmm512b32_er,
		// Token: 0x04001C17 RID: 7191
		Cvttps2dq_xmm_xmmm128,
		// Token: 0x04001C18 RID: 7192
		VEX_Vcvttps2dq_xmm_xmmm128,
		// Token: 0x04001C19 RID: 7193
		VEX_Vcvttps2dq_ymm_ymmm256,
		// Token: 0x04001C1A RID: 7194
		EVEX_Vcvttps2dq_xmm_k1z_xmmm128b32,
		// Token: 0x04001C1B RID: 7195
		EVEX_Vcvttps2dq_ymm_k1z_ymmm256b32,
		// Token: 0x04001C1C RID: 7196
		EVEX_Vcvttps2dq_zmm_k1z_zmmm512b32_sae,
		// Token: 0x04001C1D RID: 7197
		Subps_xmm_xmmm128,
		// Token: 0x04001C1E RID: 7198
		VEX_Vsubps_xmm_xmm_xmmm128,
		// Token: 0x04001C1F RID: 7199
		VEX_Vsubps_ymm_ymm_ymmm256,
		// Token: 0x04001C20 RID: 7200
		EVEX_Vsubps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001C21 RID: 7201
		EVEX_Vsubps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001C22 RID: 7202
		EVEX_Vsubps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x04001C23 RID: 7203
		Subpd_xmm_xmmm128,
		// Token: 0x04001C24 RID: 7204
		VEX_Vsubpd_xmm_xmm_xmmm128,
		// Token: 0x04001C25 RID: 7205
		VEX_Vsubpd_ymm_ymm_ymmm256,
		// Token: 0x04001C26 RID: 7206
		EVEX_Vsubpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001C27 RID: 7207
		EVEX_Vsubpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001C28 RID: 7208
		EVEX_Vsubpd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x04001C29 RID: 7209
		Subss_xmm_xmmm32,
		// Token: 0x04001C2A RID: 7210
		VEX_Vsubss_xmm_xmm_xmmm32,
		// Token: 0x04001C2B RID: 7211
		EVEX_Vsubss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x04001C2C RID: 7212
		Subsd_xmm_xmmm64,
		// Token: 0x04001C2D RID: 7213
		VEX_Vsubsd_xmm_xmm_xmmm64,
		// Token: 0x04001C2E RID: 7214
		EVEX_Vsubsd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x04001C2F RID: 7215
		Minps_xmm_xmmm128,
		// Token: 0x04001C30 RID: 7216
		VEX_Vminps_xmm_xmm_xmmm128,
		// Token: 0x04001C31 RID: 7217
		VEX_Vminps_ymm_ymm_ymmm256,
		// Token: 0x04001C32 RID: 7218
		EVEX_Vminps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001C33 RID: 7219
		EVEX_Vminps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001C34 RID: 7220
		EVEX_Vminps_zmm_k1z_zmm_zmmm512b32_sae,
		// Token: 0x04001C35 RID: 7221
		Minpd_xmm_xmmm128,
		// Token: 0x04001C36 RID: 7222
		VEX_Vminpd_xmm_xmm_xmmm128,
		// Token: 0x04001C37 RID: 7223
		VEX_Vminpd_ymm_ymm_ymmm256,
		// Token: 0x04001C38 RID: 7224
		EVEX_Vminpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001C39 RID: 7225
		EVEX_Vminpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001C3A RID: 7226
		EVEX_Vminpd_zmm_k1z_zmm_zmmm512b64_sae,
		// Token: 0x04001C3B RID: 7227
		Minss_xmm_xmmm32,
		// Token: 0x04001C3C RID: 7228
		VEX_Vminss_xmm_xmm_xmmm32,
		// Token: 0x04001C3D RID: 7229
		EVEX_Vminss_xmm_k1z_xmm_xmmm32_sae,
		// Token: 0x04001C3E RID: 7230
		Minsd_xmm_xmmm64,
		// Token: 0x04001C3F RID: 7231
		VEX_Vminsd_xmm_xmm_xmmm64,
		// Token: 0x04001C40 RID: 7232
		EVEX_Vminsd_xmm_k1z_xmm_xmmm64_sae,
		// Token: 0x04001C41 RID: 7233
		Divps_xmm_xmmm128,
		// Token: 0x04001C42 RID: 7234
		VEX_Vdivps_xmm_xmm_xmmm128,
		// Token: 0x04001C43 RID: 7235
		VEX_Vdivps_ymm_ymm_ymmm256,
		// Token: 0x04001C44 RID: 7236
		EVEX_Vdivps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001C45 RID: 7237
		EVEX_Vdivps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001C46 RID: 7238
		EVEX_Vdivps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x04001C47 RID: 7239
		Divpd_xmm_xmmm128,
		// Token: 0x04001C48 RID: 7240
		VEX_Vdivpd_xmm_xmm_xmmm128,
		// Token: 0x04001C49 RID: 7241
		VEX_Vdivpd_ymm_ymm_ymmm256,
		// Token: 0x04001C4A RID: 7242
		EVEX_Vdivpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001C4B RID: 7243
		EVEX_Vdivpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001C4C RID: 7244
		EVEX_Vdivpd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x04001C4D RID: 7245
		Divss_xmm_xmmm32,
		// Token: 0x04001C4E RID: 7246
		VEX_Vdivss_xmm_xmm_xmmm32,
		// Token: 0x04001C4F RID: 7247
		EVEX_Vdivss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x04001C50 RID: 7248
		Divsd_xmm_xmmm64,
		// Token: 0x04001C51 RID: 7249
		VEX_Vdivsd_xmm_xmm_xmmm64,
		// Token: 0x04001C52 RID: 7250
		EVEX_Vdivsd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x04001C53 RID: 7251
		Maxps_xmm_xmmm128,
		// Token: 0x04001C54 RID: 7252
		VEX_Vmaxps_xmm_xmm_xmmm128,
		// Token: 0x04001C55 RID: 7253
		VEX_Vmaxps_ymm_ymm_ymmm256,
		// Token: 0x04001C56 RID: 7254
		EVEX_Vmaxps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001C57 RID: 7255
		EVEX_Vmaxps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001C58 RID: 7256
		EVEX_Vmaxps_zmm_k1z_zmm_zmmm512b32_sae,
		// Token: 0x04001C59 RID: 7257
		Maxpd_xmm_xmmm128,
		// Token: 0x04001C5A RID: 7258
		VEX_Vmaxpd_xmm_xmm_xmmm128,
		// Token: 0x04001C5B RID: 7259
		VEX_Vmaxpd_ymm_ymm_ymmm256,
		// Token: 0x04001C5C RID: 7260
		EVEX_Vmaxpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001C5D RID: 7261
		EVEX_Vmaxpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001C5E RID: 7262
		EVEX_Vmaxpd_zmm_k1z_zmm_zmmm512b64_sae,
		// Token: 0x04001C5F RID: 7263
		Maxss_xmm_xmmm32,
		// Token: 0x04001C60 RID: 7264
		VEX_Vmaxss_xmm_xmm_xmmm32,
		// Token: 0x04001C61 RID: 7265
		EVEX_Vmaxss_xmm_k1z_xmm_xmmm32_sae,
		// Token: 0x04001C62 RID: 7266
		Maxsd_xmm_xmmm64,
		// Token: 0x04001C63 RID: 7267
		VEX_Vmaxsd_xmm_xmm_xmmm64,
		// Token: 0x04001C64 RID: 7268
		EVEX_Vmaxsd_xmm_k1z_xmm_xmmm64_sae,
		// Token: 0x04001C65 RID: 7269
		Punpcklbw_mm_mmm32,
		// Token: 0x04001C66 RID: 7270
		Punpcklbw_xmm_xmmm128,
		// Token: 0x04001C67 RID: 7271
		VEX_Vpunpcklbw_xmm_xmm_xmmm128,
		// Token: 0x04001C68 RID: 7272
		VEX_Vpunpcklbw_ymm_ymm_ymmm256,
		// Token: 0x04001C69 RID: 7273
		EVEX_Vpunpcklbw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001C6A RID: 7274
		EVEX_Vpunpcklbw_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001C6B RID: 7275
		EVEX_Vpunpcklbw_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001C6C RID: 7276
		Punpcklwd_mm_mmm32,
		// Token: 0x04001C6D RID: 7277
		Punpcklwd_xmm_xmmm128,
		// Token: 0x04001C6E RID: 7278
		VEX_Vpunpcklwd_xmm_xmm_xmmm128,
		// Token: 0x04001C6F RID: 7279
		VEX_Vpunpcklwd_ymm_ymm_ymmm256,
		// Token: 0x04001C70 RID: 7280
		EVEX_Vpunpcklwd_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001C71 RID: 7281
		EVEX_Vpunpcklwd_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001C72 RID: 7282
		EVEX_Vpunpcklwd_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001C73 RID: 7283
		Punpckldq_mm_mmm32,
		// Token: 0x04001C74 RID: 7284
		Punpckldq_xmm_xmmm128,
		// Token: 0x04001C75 RID: 7285
		VEX_Vpunpckldq_xmm_xmm_xmmm128,
		// Token: 0x04001C76 RID: 7286
		VEX_Vpunpckldq_ymm_ymm_ymmm256,
		// Token: 0x04001C77 RID: 7287
		EVEX_Vpunpckldq_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001C78 RID: 7288
		EVEX_Vpunpckldq_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001C79 RID: 7289
		EVEX_Vpunpckldq_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04001C7A RID: 7290
		Packsswb_mm_mmm64,
		// Token: 0x04001C7B RID: 7291
		Packsswb_xmm_xmmm128,
		// Token: 0x04001C7C RID: 7292
		VEX_Vpacksswb_xmm_xmm_xmmm128,
		// Token: 0x04001C7D RID: 7293
		VEX_Vpacksswb_ymm_ymm_ymmm256,
		// Token: 0x04001C7E RID: 7294
		EVEX_Vpacksswb_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001C7F RID: 7295
		EVEX_Vpacksswb_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001C80 RID: 7296
		EVEX_Vpacksswb_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001C81 RID: 7297
		Pcmpgtb_mm_mmm64,
		// Token: 0x04001C82 RID: 7298
		Pcmpgtb_xmm_xmmm128,
		// Token: 0x04001C83 RID: 7299
		VEX_Vpcmpgtb_xmm_xmm_xmmm128,
		// Token: 0x04001C84 RID: 7300
		VEX_Vpcmpgtb_ymm_ymm_ymmm256,
		// Token: 0x04001C85 RID: 7301
		EVEX_Vpcmpgtb_kr_k1_xmm_xmmm128,
		// Token: 0x04001C86 RID: 7302
		EVEX_Vpcmpgtb_kr_k1_ymm_ymmm256,
		// Token: 0x04001C87 RID: 7303
		EVEX_Vpcmpgtb_kr_k1_zmm_zmmm512,
		// Token: 0x04001C88 RID: 7304
		Pcmpgtw_mm_mmm64,
		// Token: 0x04001C89 RID: 7305
		Pcmpgtw_xmm_xmmm128,
		// Token: 0x04001C8A RID: 7306
		VEX_Vpcmpgtw_xmm_xmm_xmmm128,
		// Token: 0x04001C8B RID: 7307
		VEX_Vpcmpgtw_ymm_ymm_ymmm256,
		// Token: 0x04001C8C RID: 7308
		EVEX_Vpcmpgtw_kr_k1_xmm_xmmm128,
		// Token: 0x04001C8D RID: 7309
		EVEX_Vpcmpgtw_kr_k1_ymm_ymmm256,
		// Token: 0x04001C8E RID: 7310
		EVEX_Vpcmpgtw_kr_k1_zmm_zmmm512,
		// Token: 0x04001C8F RID: 7311
		Pcmpgtd_mm_mmm64,
		// Token: 0x04001C90 RID: 7312
		Pcmpgtd_xmm_xmmm128,
		// Token: 0x04001C91 RID: 7313
		VEX_Vpcmpgtd_xmm_xmm_xmmm128,
		// Token: 0x04001C92 RID: 7314
		VEX_Vpcmpgtd_ymm_ymm_ymmm256,
		// Token: 0x04001C93 RID: 7315
		EVEX_Vpcmpgtd_kr_k1_xmm_xmmm128b32,
		// Token: 0x04001C94 RID: 7316
		EVEX_Vpcmpgtd_kr_k1_ymm_ymmm256b32,
		// Token: 0x04001C95 RID: 7317
		EVEX_Vpcmpgtd_kr_k1_zmm_zmmm512b32,
		// Token: 0x04001C96 RID: 7318
		Packuswb_mm_mmm64,
		// Token: 0x04001C97 RID: 7319
		Packuswb_xmm_xmmm128,
		// Token: 0x04001C98 RID: 7320
		VEX_Vpackuswb_xmm_xmm_xmmm128,
		// Token: 0x04001C99 RID: 7321
		VEX_Vpackuswb_ymm_ymm_ymmm256,
		// Token: 0x04001C9A RID: 7322
		EVEX_Vpackuswb_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001C9B RID: 7323
		EVEX_Vpackuswb_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001C9C RID: 7324
		EVEX_Vpackuswb_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001C9D RID: 7325
		Punpckhbw_mm_mmm64,
		// Token: 0x04001C9E RID: 7326
		Punpckhbw_xmm_xmmm128,
		// Token: 0x04001C9F RID: 7327
		VEX_Vpunpckhbw_xmm_xmm_xmmm128,
		// Token: 0x04001CA0 RID: 7328
		VEX_Vpunpckhbw_ymm_ymm_ymmm256,
		// Token: 0x04001CA1 RID: 7329
		EVEX_Vpunpckhbw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001CA2 RID: 7330
		EVEX_Vpunpckhbw_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001CA3 RID: 7331
		EVEX_Vpunpckhbw_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001CA4 RID: 7332
		Punpckhwd_mm_mmm64,
		// Token: 0x04001CA5 RID: 7333
		Punpckhwd_xmm_xmmm128,
		// Token: 0x04001CA6 RID: 7334
		VEX_Vpunpckhwd_xmm_xmm_xmmm128,
		// Token: 0x04001CA7 RID: 7335
		VEX_Vpunpckhwd_ymm_ymm_ymmm256,
		// Token: 0x04001CA8 RID: 7336
		EVEX_Vpunpckhwd_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001CA9 RID: 7337
		EVEX_Vpunpckhwd_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001CAA RID: 7338
		EVEX_Vpunpckhwd_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001CAB RID: 7339
		Punpckhdq_mm_mmm64,
		// Token: 0x04001CAC RID: 7340
		Punpckhdq_xmm_xmmm128,
		// Token: 0x04001CAD RID: 7341
		VEX_Vpunpckhdq_xmm_xmm_xmmm128,
		// Token: 0x04001CAE RID: 7342
		VEX_Vpunpckhdq_ymm_ymm_ymmm256,
		// Token: 0x04001CAF RID: 7343
		EVEX_Vpunpckhdq_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001CB0 RID: 7344
		EVEX_Vpunpckhdq_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001CB1 RID: 7345
		EVEX_Vpunpckhdq_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04001CB2 RID: 7346
		Packssdw_mm_mmm64,
		// Token: 0x04001CB3 RID: 7347
		Packssdw_xmm_xmmm128,
		// Token: 0x04001CB4 RID: 7348
		VEX_Vpackssdw_xmm_xmm_xmmm128,
		// Token: 0x04001CB5 RID: 7349
		VEX_Vpackssdw_ymm_ymm_ymmm256,
		// Token: 0x04001CB6 RID: 7350
		EVEX_Vpackssdw_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001CB7 RID: 7351
		EVEX_Vpackssdw_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001CB8 RID: 7352
		EVEX_Vpackssdw_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04001CB9 RID: 7353
		Punpcklqdq_xmm_xmmm128,
		// Token: 0x04001CBA RID: 7354
		VEX_Vpunpcklqdq_xmm_xmm_xmmm128,
		// Token: 0x04001CBB RID: 7355
		VEX_Vpunpcklqdq_ymm_ymm_ymmm256,
		// Token: 0x04001CBC RID: 7356
		EVEX_Vpunpcklqdq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001CBD RID: 7357
		EVEX_Vpunpcklqdq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001CBE RID: 7358
		EVEX_Vpunpcklqdq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04001CBF RID: 7359
		Punpckhqdq_xmm_xmmm128,
		// Token: 0x04001CC0 RID: 7360
		VEX_Vpunpckhqdq_xmm_xmm_xmmm128,
		// Token: 0x04001CC1 RID: 7361
		VEX_Vpunpckhqdq_ymm_ymm_ymmm256,
		// Token: 0x04001CC2 RID: 7362
		EVEX_Vpunpckhqdq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001CC3 RID: 7363
		EVEX_Vpunpckhqdq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001CC4 RID: 7364
		EVEX_Vpunpckhqdq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04001CC5 RID: 7365
		Movd_mm_rm32,
		// Token: 0x04001CC6 RID: 7366
		Movq_mm_rm64,
		// Token: 0x04001CC7 RID: 7367
		Movd_xmm_rm32,
		// Token: 0x04001CC8 RID: 7368
		Movq_xmm_rm64,
		// Token: 0x04001CC9 RID: 7369
		VEX_Vmovd_xmm_rm32,
		// Token: 0x04001CCA RID: 7370
		VEX_Vmovq_xmm_rm64,
		// Token: 0x04001CCB RID: 7371
		EVEX_Vmovd_xmm_rm32,
		// Token: 0x04001CCC RID: 7372
		EVEX_Vmovq_xmm_rm64,
		// Token: 0x04001CCD RID: 7373
		Movq_mm_mmm64,
		// Token: 0x04001CCE RID: 7374
		Movdqa_xmm_xmmm128,
		// Token: 0x04001CCF RID: 7375
		VEX_Vmovdqa_xmm_xmmm128,
		// Token: 0x04001CD0 RID: 7376
		VEX_Vmovdqa_ymm_ymmm256,
		// Token: 0x04001CD1 RID: 7377
		EVEX_Vmovdqa32_xmm_k1z_xmmm128,
		// Token: 0x04001CD2 RID: 7378
		EVEX_Vmovdqa32_ymm_k1z_ymmm256,
		// Token: 0x04001CD3 RID: 7379
		EVEX_Vmovdqa32_zmm_k1z_zmmm512,
		// Token: 0x04001CD4 RID: 7380
		EVEX_Vmovdqa64_xmm_k1z_xmmm128,
		// Token: 0x04001CD5 RID: 7381
		EVEX_Vmovdqa64_ymm_k1z_ymmm256,
		// Token: 0x04001CD6 RID: 7382
		EVEX_Vmovdqa64_zmm_k1z_zmmm512,
		// Token: 0x04001CD7 RID: 7383
		Movdqu_xmm_xmmm128,
		// Token: 0x04001CD8 RID: 7384
		VEX_Vmovdqu_xmm_xmmm128,
		// Token: 0x04001CD9 RID: 7385
		VEX_Vmovdqu_ymm_ymmm256,
		// Token: 0x04001CDA RID: 7386
		EVEX_Vmovdqu32_xmm_k1z_xmmm128,
		// Token: 0x04001CDB RID: 7387
		EVEX_Vmovdqu32_ymm_k1z_ymmm256,
		// Token: 0x04001CDC RID: 7388
		EVEX_Vmovdqu32_zmm_k1z_zmmm512,
		// Token: 0x04001CDD RID: 7389
		EVEX_Vmovdqu64_xmm_k1z_xmmm128,
		// Token: 0x04001CDE RID: 7390
		EVEX_Vmovdqu64_ymm_k1z_ymmm256,
		// Token: 0x04001CDF RID: 7391
		EVEX_Vmovdqu64_zmm_k1z_zmmm512,
		// Token: 0x04001CE0 RID: 7392
		EVEX_Vmovdqu8_xmm_k1z_xmmm128,
		// Token: 0x04001CE1 RID: 7393
		EVEX_Vmovdqu8_ymm_k1z_ymmm256,
		// Token: 0x04001CE2 RID: 7394
		EVEX_Vmovdqu8_zmm_k1z_zmmm512,
		// Token: 0x04001CE3 RID: 7395
		EVEX_Vmovdqu16_xmm_k1z_xmmm128,
		// Token: 0x04001CE4 RID: 7396
		EVEX_Vmovdqu16_ymm_k1z_ymmm256,
		// Token: 0x04001CE5 RID: 7397
		EVEX_Vmovdqu16_zmm_k1z_zmmm512,
		// Token: 0x04001CE6 RID: 7398
		Pshufw_mm_mmm64_imm8,
		// Token: 0x04001CE7 RID: 7399
		Pshufd_xmm_xmmm128_imm8,
		// Token: 0x04001CE8 RID: 7400
		VEX_Vpshufd_xmm_xmmm128_imm8,
		// Token: 0x04001CE9 RID: 7401
		VEX_Vpshufd_ymm_ymmm256_imm8,
		// Token: 0x04001CEA RID: 7402
		EVEX_Vpshufd_xmm_k1z_xmmm128b32_imm8,
		// Token: 0x04001CEB RID: 7403
		EVEX_Vpshufd_ymm_k1z_ymmm256b32_imm8,
		// Token: 0x04001CEC RID: 7404
		EVEX_Vpshufd_zmm_k1z_zmmm512b32_imm8,
		// Token: 0x04001CED RID: 7405
		Pshufhw_xmm_xmmm128_imm8,
		// Token: 0x04001CEE RID: 7406
		VEX_Vpshufhw_xmm_xmmm128_imm8,
		// Token: 0x04001CEF RID: 7407
		VEX_Vpshufhw_ymm_ymmm256_imm8,
		// Token: 0x04001CF0 RID: 7408
		EVEX_Vpshufhw_xmm_k1z_xmmm128_imm8,
		// Token: 0x04001CF1 RID: 7409
		EVEX_Vpshufhw_ymm_k1z_ymmm256_imm8,
		// Token: 0x04001CF2 RID: 7410
		EVEX_Vpshufhw_zmm_k1z_zmmm512_imm8,
		// Token: 0x04001CF3 RID: 7411
		Pshuflw_xmm_xmmm128_imm8,
		// Token: 0x04001CF4 RID: 7412
		VEX_Vpshuflw_xmm_xmmm128_imm8,
		// Token: 0x04001CF5 RID: 7413
		VEX_Vpshuflw_ymm_ymmm256_imm8,
		// Token: 0x04001CF6 RID: 7414
		EVEX_Vpshuflw_xmm_k1z_xmmm128_imm8,
		// Token: 0x04001CF7 RID: 7415
		EVEX_Vpshuflw_ymm_k1z_ymmm256_imm8,
		// Token: 0x04001CF8 RID: 7416
		EVEX_Vpshuflw_zmm_k1z_zmmm512_imm8,
		// Token: 0x04001CF9 RID: 7417
		Psrlw_mm_imm8,
		// Token: 0x04001CFA RID: 7418
		Psrlw_xmm_imm8,
		// Token: 0x04001CFB RID: 7419
		VEX_Vpsrlw_xmm_xmm_imm8,
		// Token: 0x04001CFC RID: 7420
		VEX_Vpsrlw_ymm_ymm_imm8,
		// Token: 0x04001CFD RID: 7421
		EVEX_Vpsrlw_xmm_k1z_xmmm128_imm8,
		// Token: 0x04001CFE RID: 7422
		EVEX_Vpsrlw_ymm_k1z_ymmm256_imm8,
		// Token: 0x04001CFF RID: 7423
		EVEX_Vpsrlw_zmm_k1z_zmmm512_imm8,
		// Token: 0x04001D00 RID: 7424
		Psraw_mm_imm8,
		// Token: 0x04001D01 RID: 7425
		Psraw_xmm_imm8,
		// Token: 0x04001D02 RID: 7426
		VEX_Vpsraw_xmm_xmm_imm8,
		// Token: 0x04001D03 RID: 7427
		VEX_Vpsraw_ymm_ymm_imm8,
		// Token: 0x04001D04 RID: 7428
		EVEX_Vpsraw_xmm_k1z_xmmm128_imm8,
		// Token: 0x04001D05 RID: 7429
		EVEX_Vpsraw_ymm_k1z_ymmm256_imm8,
		// Token: 0x04001D06 RID: 7430
		EVEX_Vpsraw_zmm_k1z_zmmm512_imm8,
		// Token: 0x04001D07 RID: 7431
		Psllw_mm_imm8,
		// Token: 0x04001D08 RID: 7432
		Psllw_xmm_imm8,
		// Token: 0x04001D09 RID: 7433
		VEX_Vpsllw_xmm_xmm_imm8,
		// Token: 0x04001D0A RID: 7434
		VEX_Vpsllw_ymm_ymm_imm8,
		// Token: 0x04001D0B RID: 7435
		EVEX_Vpsllw_xmm_k1z_xmmm128_imm8,
		// Token: 0x04001D0C RID: 7436
		EVEX_Vpsllw_ymm_k1z_ymmm256_imm8,
		// Token: 0x04001D0D RID: 7437
		EVEX_Vpsllw_zmm_k1z_zmmm512_imm8,
		// Token: 0x04001D0E RID: 7438
		EVEX_Vprord_xmm_k1z_xmmm128b32_imm8,
		// Token: 0x04001D0F RID: 7439
		EVEX_Vprord_ymm_k1z_ymmm256b32_imm8,
		// Token: 0x04001D10 RID: 7440
		EVEX_Vprord_zmm_k1z_zmmm512b32_imm8,
		// Token: 0x04001D11 RID: 7441
		EVEX_Vprorq_xmm_k1z_xmmm128b64_imm8,
		// Token: 0x04001D12 RID: 7442
		EVEX_Vprorq_ymm_k1z_ymmm256b64_imm8,
		// Token: 0x04001D13 RID: 7443
		EVEX_Vprorq_zmm_k1z_zmmm512b64_imm8,
		// Token: 0x04001D14 RID: 7444
		EVEX_Vprold_xmm_k1z_xmmm128b32_imm8,
		// Token: 0x04001D15 RID: 7445
		EVEX_Vprold_ymm_k1z_ymmm256b32_imm8,
		// Token: 0x04001D16 RID: 7446
		EVEX_Vprold_zmm_k1z_zmmm512b32_imm8,
		// Token: 0x04001D17 RID: 7447
		EVEX_Vprolq_xmm_k1z_xmmm128b64_imm8,
		// Token: 0x04001D18 RID: 7448
		EVEX_Vprolq_ymm_k1z_ymmm256b64_imm8,
		// Token: 0x04001D19 RID: 7449
		EVEX_Vprolq_zmm_k1z_zmmm512b64_imm8,
		// Token: 0x04001D1A RID: 7450
		Psrld_mm_imm8,
		// Token: 0x04001D1B RID: 7451
		Psrld_xmm_imm8,
		// Token: 0x04001D1C RID: 7452
		VEX_Vpsrld_xmm_xmm_imm8,
		// Token: 0x04001D1D RID: 7453
		VEX_Vpsrld_ymm_ymm_imm8,
		// Token: 0x04001D1E RID: 7454
		EVEX_Vpsrld_xmm_k1z_xmmm128b32_imm8,
		// Token: 0x04001D1F RID: 7455
		EVEX_Vpsrld_ymm_k1z_ymmm256b32_imm8,
		// Token: 0x04001D20 RID: 7456
		EVEX_Vpsrld_zmm_k1z_zmmm512b32_imm8,
		// Token: 0x04001D21 RID: 7457
		Psrad_mm_imm8,
		// Token: 0x04001D22 RID: 7458
		Psrad_xmm_imm8,
		// Token: 0x04001D23 RID: 7459
		VEX_Vpsrad_xmm_xmm_imm8,
		// Token: 0x04001D24 RID: 7460
		VEX_Vpsrad_ymm_ymm_imm8,
		// Token: 0x04001D25 RID: 7461
		EVEX_Vpsrad_xmm_k1z_xmmm128b32_imm8,
		// Token: 0x04001D26 RID: 7462
		EVEX_Vpsrad_ymm_k1z_ymmm256b32_imm8,
		// Token: 0x04001D27 RID: 7463
		EVEX_Vpsrad_zmm_k1z_zmmm512b32_imm8,
		// Token: 0x04001D28 RID: 7464
		EVEX_Vpsraq_xmm_k1z_xmmm128b64_imm8,
		// Token: 0x04001D29 RID: 7465
		EVEX_Vpsraq_ymm_k1z_ymmm256b64_imm8,
		// Token: 0x04001D2A RID: 7466
		EVEX_Vpsraq_zmm_k1z_zmmm512b64_imm8,
		// Token: 0x04001D2B RID: 7467
		Pslld_mm_imm8,
		// Token: 0x04001D2C RID: 7468
		Pslld_xmm_imm8,
		// Token: 0x04001D2D RID: 7469
		VEX_Vpslld_xmm_xmm_imm8,
		// Token: 0x04001D2E RID: 7470
		VEX_Vpslld_ymm_ymm_imm8,
		// Token: 0x04001D2F RID: 7471
		EVEX_Vpslld_xmm_k1z_xmmm128b32_imm8,
		// Token: 0x04001D30 RID: 7472
		EVEX_Vpslld_ymm_k1z_ymmm256b32_imm8,
		// Token: 0x04001D31 RID: 7473
		EVEX_Vpslld_zmm_k1z_zmmm512b32_imm8,
		// Token: 0x04001D32 RID: 7474
		Psrlq_mm_imm8,
		// Token: 0x04001D33 RID: 7475
		Psrlq_xmm_imm8,
		// Token: 0x04001D34 RID: 7476
		VEX_Vpsrlq_xmm_xmm_imm8,
		// Token: 0x04001D35 RID: 7477
		VEX_Vpsrlq_ymm_ymm_imm8,
		// Token: 0x04001D36 RID: 7478
		EVEX_Vpsrlq_xmm_k1z_xmmm128b64_imm8,
		// Token: 0x04001D37 RID: 7479
		EVEX_Vpsrlq_ymm_k1z_ymmm256b64_imm8,
		// Token: 0x04001D38 RID: 7480
		EVEX_Vpsrlq_zmm_k1z_zmmm512b64_imm8,
		// Token: 0x04001D39 RID: 7481
		Psrldq_xmm_imm8,
		// Token: 0x04001D3A RID: 7482
		VEX_Vpsrldq_xmm_xmm_imm8,
		// Token: 0x04001D3B RID: 7483
		VEX_Vpsrldq_ymm_ymm_imm8,
		// Token: 0x04001D3C RID: 7484
		EVEX_Vpsrldq_xmm_xmmm128_imm8,
		// Token: 0x04001D3D RID: 7485
		EVEX_Vpsrldq_ymm_ymmm256_imm8,
		// Token: 0x04001D3E RID: 7486
		EVEX_Vpsrldq_zmm_zmmm512_imm8,
		// Token: 0x04001D3F RID: 7487
		Psllq_mm_imm8,
		// Token: 0x04001D40 RID: 7488
		Psllq_xmm_imm8,
		// Token: 0x04001D41 RID: 7489
		VEX_Vpsllq_xmm_xmm_imm8,
		// Token: 0x04001D42 RID: 7490
		VEX_Vpsllq_ymm_ymm_imm8,
		// Token: 0x04001D43 RID: 7491
		EVEX_Vpsllq_xmm_k1z_xmmm128b64_imm8,
		// Token: 0x04001D44 RID: 7492
		EVEX_Vpsllq_ymm_k1z_ymmm256b64_imm8,
		// Token: 0x04001D45 RID: 7493
		EVEX_Vpsllq_zmm_k1z_zmmm512b64_imm8,
		// Token: 0x04001D46 RID: 7494
		Pslldq_xmm_imm8,
		// Token: 0x04001D47 RID: 7495
		VEX_Vpslldq_xmm_xmm_imm8,
		// Token: 0x04001D48 RID: 7496
		VEX_Vpslldq_ymm_ymm_imm8,
		// Token: 0x04001D49 RID: 7497
		EVEX_Vpslldq_xmm_xmmm128_imm8,
		// Token: 0x04001D4A RID: 7498
		EVEX_Vpslldq_ymm_ymmm256_imm8,
		// Token: 0x04001D4B RID: 7499
		EVEX_Vpslldq_zmm_zmmm512_imm8,
		// Token: 0x04001D4C RID: 7500
		Pcmpeqb_mm_mmm64,
		// Token: 0x04001D4D RID: 7501
		Pcmpeqb_xmm_xmmm128,
		// Token: 0x04001D4E RID: 7502
		VEX_Vpcmpeqb_xmm_xmm_xmmm128,
		// Token: 0x04001D4F RID: 7503
		VEX_Vpcmpeqb_ymm_ymm_ymmm256,
		// Token: 0x04001D50 RID: 7504
		EVEX_Vpcmpeqb_kr_k1_xmm_xmmm128,
		// Token: 0x04001D51 RID: 7505
		EVEX_Vpcmpeqb_kr_k1_ymm_ymmm256,
		// Token: 0x04001D52 RID: 7506
		EVEX_Vpcmpeqb_kr_k1_zmm_zmmm512,
		// Token: 0x04001D53 RID: 7507
		Pcmpeqw_mm_mmm64,
		// Token: 0x04001D54 RID: 7508
		Pcmpeqw_xmm_xmmm128,
		// Token: 0x04001D55 RID: 7509
		VEX_Vpcmpeqw_xmm_xmm_xmmm128,
		// Token: 0x04001D56 RID: 7510
		VEX_Vpcmpeqw_ymm_ymm_ymmm256,
		// Token: 0x04001D57 RID: 7511
		EVEX_Vpcmpeqw_kr_k1_xmm_xmmm128,
		// Token: 0x04001D58 RID: 7512
		EVEX_Vpcmpeqw_kr_k1_ymm_ymmm256,
		// Token: 0x04001D59 RID: 7513
		EVEX_Vpcmpeqw_kr_k1_zmm_zmmm512,
		// Token: 0x04001D5A RID: 7514
		Pcmpeqd_mm_mmm64,
		// Token: 0x04001D5B RID: 7515
		Pcmpeqd_xmm_xmmm128,
		// Token: 0x04001D5C RID: 7516
		VEX_Vpcmpeqd_xmm_xmm_xmmm128,
		// Token: 0x04001D5D RID: 7517
		VEX_Vpcmpeqd_ymm_ymm_ymmm256,
		// Token: 0x04001D5E RID: 7518
		EVEX_Vpcmpeqd_kr_k1_xmm_xmmm128b32,
		// Token: 0x04001D5F RID: 7519
		EVEX_Vpcmpeqd_kr_k1_ymm_ymmm256b32,
		// Token: 0x04001D60 RID: 7520
		EVEX_Vpcmpeqd_kr_k1_zmm_zmmm512b32,
		// Token: 0x04001D61 RID: 7521
		Emms,
		// Token: 0x04001D62 RID: 7522
		VEX_Vzeroupper,
		// Token: 0x04001D63 RID: 7523
		VEX_Vzeroall,
		// Token: 0x04001D64 RID: 7524
		Vmread_rm32_r32,
		// Token: 0x04001D65 RID: 7525
		Vmread_rm64_r64,
		// Token: 0x04001D66 RID: 7526
		EVEX_Vcvttps2udq_xmm_k1z_xmmm128b32,
		// Token: 0x04001D67 RID: 7527
		EVEX_Vcvttps2udq_ymm_k1z_ymmm256b32,
		// Token: 0x04001D68 RID: 7528
		EVEX_Vcvttps2udq_zmm_k1z_zmmm512b32_sae,
		// Token: 0x04001D69 RID: 7529
		EVEX_Vcvttpd2udq_xmm_k1z_xmmm128b64,
		// Token: 0x04001D6A RID: 7530
		EVEX_Vcvttpd2udq_xmm_k1z_ymmm256b64,
		// Token: 0x04001D6B RID: 7531
		EVEX_Vcvttpd2udq_ymm_k1z_zmmm512b64_sae,
		// Token: 0x04001D6C RID: 7532
		Extrq_xmm_imm8_imm8,
		// Token: 0x04001D6D RID: 7533
		EVEX_Vcvttps2uqq_xmm_k1z_xmmm64b32,
		// Token: 0x04001D6E RID: 7534
		EVEX_Vcvttps2uqq_ymm_k1z_xmmm128b32,
		// Token: 0x04001D6F RID: 7535
		EVEX_Vcvttps2uqq_zmm_k1z_ymmm256b32_sae,
		// Token: 0x04001D70 RID: 7536
		EVEX_Vcvttpd2uqq_xmm_k1z_xmmm128b64,
		// Token: 0x04001D71 RID: 7537
		EVEX_Vcvttpd2uqq_ymm_k1z_ymmm256b64,
		// Token: 0x04001D72 RID: 7538
		EVEX_Vcvttpd2uqq_zmm_k1z_zmmm512b64_sae,
		// Token: 0x04001D73 RID: 7539
		EVEX_Vcvttss2usi_r32_xmmm32_sae,
		// Token: 0x04001D74 RID: 7540
		EVEX_Vcvttss2usi_r64_xmmm32_sae,
		// Token: 0x04001D75 RID: 7541
		Insertq_xmm_xmm_imm8_imm8,
		// Token: 0x04001D76 RID: 7542
		EVEX_Vcvttsd2usi_r32_xmmm64_sae,
		// Token: 0x04001D77 RID: 7543
		EVEX_Vcvttsd2usi_r64_xmmm64_sae,
		// Token: 0x04001D78 RID: 7544
		Vmwrite_r32_rm32,
		// Token: 0x04001D79 RID: 7545
		Vmwrite_r64_rm64,
		// Token: 0x04001D7A RID: 7546
		EVEX_Vcvtps2udq_xmm_k1z_xmmm128b32,
		// Token: 0x04001D7B RID: 7547
		EVEX_Vcvtps2udq_ymm_k1z_ymmm256b32,
		// Token: 0x04001D7C RID: 7548
		EVEX_Vcvtps2udq_zmm_k1z_zmmm512b32_er,
		// Token: 0x04001D7D RID: 7549
		EVEX_Vcvtpd2udq_xmm_k1z_xmmm128b64,
		// Token: 0x04001D7E RID: 7550
		EVEX_Vcvtpd2udq_xmm_k1z_ymmm256b64,
		// Token: 0x04001D7F RID: 7551
		EVEX_Vcvtpd2udq_ymm_k1z_zmmm512b64_er,
		// Token: 0x04001D80 RID: 7552
		Extrq_xmm_xmm,
		// Token: 0x04001D81 RID: 7553
		EVEX_Vcvtps2uqq_xmm_k1z_xmmm64b32,
		// Token: 0x04001D82 RID: 7554
		EVEX_Vcvtps2uqq_ymm_k1z_xmmm128b32,
		// Token: 0x04001D83 RID: 7555
		EVEX_Vcvtps2uqq_zmm_k1z_ymmm256b32_er,
		// Token: 0x04001D84 RID: 7556
		EVEX_Vcvtpd2uqq_xmm_k1z_xmmm128b64,
		// Token: 0x04001D85 RID: 7557
		EVEX_Vcvtpd2uqq_ymm_k1z_ymmm256b64,
		// Token: 0x04001D86 RID: 7558
		EVEX_Vcvtpd2uqq_zmm_k1z_zmmm512b64_er,
		// Token: 0x04001D87 RID: 7559
		EVEX_Vcvtss2usi_r32_xmmm32_er,
		// Token: 0x04001D88 RID: 7560
		EVEX_Vcvtss2usi_r64_xmmm32_er,
		// Token: 0x04001D89 RID: 7561
		Insertq_xmm_xmm,
		// Token: 0x04001D8A RID: 7562
		EVEX_Vcvtsd2usi_r32_xmmm64_er,
		// Token: 0x04001D8B RID: 7563
		EVEX_Vcvtsd2usi_r64_xmmm64_er,
		// Token: 0x04001D8C RID: 7564
		EVEX_Vcvttps2qq_xmm_k1z_xmmm64b32,
		// Token: 0x04001D8D RID: 7565
		EVEX_Vcvttps2qq_ymm_k1z_xmmm128b32,
		// Token: 0x04001D8E RID: 7566
		EVEX_Vcvttps2qq_zmm_k1z_ymmm256b32_sae,
		// Token: 0x04001D8F RID: 7567
		EVEX_Vcvttpd2qq_xmm_k1z_xmmm128b64,
		// Token: 0x04001D90 RID: 7568
		EVEX_Vcvttpd2qq_ymm_k1z_ymmm256b64,
		// Token: 0x04001D91 RID: 7569
		EVEX_Vcvttpd2qq_zmm_k1z_zmmm512b64_sae,
		// Token: 0x04001D92 RID: 7570
		EVEX_Vcvtudq2pd_xmm_k1z_xmmm64b32,
		// Token: 0x04001D93 RID: 7571
		EVEX_Vcvtudq2pd_ymm_k1z_xmmm128b32,
		// Token: 0x04001D94 RID: 7572
		EVEX_Vcvtudq2pd_zmm_k1z_ymmm256b32_er,
		// Token: 0x04001D95 RID: 7573
		EVEX_Vcvtuqq2pd_xmm_k1z_xmmm128b64,
		// Token: 0x04001D96 RID: 7574
		EVEX_Vcvtuqq2pd_ymm_k1z_ymmm256b64,
		// Token: 0x04001D97 RID: 7575
		EVEX_Vcvtuqq2pd_zmm_k1z_zmmm512b64_er,
		// Token: 0x04001D98 RID: 7576
		EVEX_Vcvtudq2ps_xmm_k1z_xmmm128b32,
		// Token: 0x04001D99 RID: 7577
		EVEX_Vcvtudq2ps_ymm_k1z_ymmm256b32,
		// Token: 0x04001D9A RID: 7578
		EVEX_Vcvtudq2ps_zmm_k1z_zmmm512b32_er,
		// Token: 0x04001D9B RID: 7579
		EVEX_Vcvtuqq2ps_xmm_k1z_xmmm128b64,
		// Token: 0x04001D9C RID: 7580
		EVEX_Vcvtuqq2ps_xmm_k1z_ymmm256b64,
		// Token: 0x04001D9D RID: 7581
		EVEX_Vcvtuqq2ps_ymm_k1z_zmmm512b64_er,
		// Token: 0x04001D9E RID: 7582
		EVEX_Vcvtps2qq_xmm_k1z_xmmm64b32,
		// Token: 0x04001D9F RID: 7583
		EVEX_Vcvtps2qq_ymm_k1z_xmmm128b32,
		// Token: 0x04001DA0 RID: 7584
		EVEX_Vcvtps2qq_zmm_k1z_ymmm256b32_er,
		// Token: 0x04001DA1 RID: 7585
		EVEX_Vcvtpd2qq_xmm_k1z_xmmm128b64,
		// Token: 0x04001DA2 RID: 7586
		EVEX_Vcvtpd2qq_ymm_k1z_ymmm256b64,
		// Token: 0x04001DA3 RID: 7587
		EVEX_Vcvtpd2qq_zmm_k1z_zmmm512b64_er,
		// Token: 0x04001DA4 RID: 7588
		EVEX_Vcvtusi2ss_xmm_xmm_rm32_er,
		// Token: 0x04001DA5 RID: 7589
		EVEX_Vcvtusi2ss_xmm_xmm_rm64_er,
		// Token: 0x04001DA6 RID: 7590
		EVEX_Vcvtusi2sd_xmm_xmm_rm32_er,
		// Token: 0x04001DA7 RID: 7591
		EVEX_Vcvtusi2sd_xmm_xmm_rm64_er,
		// Token: 0x04001DA8 RID: 7592
		Haddpd_xmm_xmmm128,
		// Token: 0x04001DA9 RID: 7593
		VEX_Vhaddpd_xmm_xmm_xmmm128,
		// Token: 0x04001DAA RID: 7594
		VEX_Vhaddpd_ymm_ymm_ymmm256,
		// Token: 0x04001DAB RID: 7595
		Haddps_xmm_xmmm128,
		// Token: 0x04001DAC RID: 7596
		VEX_Vhaddps_xmm_xmm_xmmm128,
		// Token: 0x04001DAD RID: 7597
		VEX_Vhaddps_ymm_ymm_ymmm256,
		// Token: 0x04001DAE RID: 7598
		Hsubpd_xmm_xmmm128,
		// Token: 0x04001DAF RID: 7599
		VEX_Vhsubpd_xmm_xmm_xmmm128,
		// Token: 0x04001DB0 RID: 7600
		VEX_Vhsubpd_ymm_ymm_ymmm256,
		// Token: 0x04001DB1 RID: 7601
		Hsubps_xmm_xmmm128,
		// Token: 0x04001DB2 RID: 7602
		VEX_Vhsubps_xmm_xmm_xmmm128,
		// Token: 0x04001DB3 RID: 7603
		VEX_Vhsubps_ymm_ymm_ymmm256,
		// Token: 0x04001DB4 RID: 7604
		Movd_rm32_mm,
		// Token: 0x04001DB5 RID: 7605
		Movq_rm64_mm,
		// Token: 0x04001DB6 RID: 7606
		Movd_rm32_xmm,
		// Token: 0x04001DB7 RID: 7607
		Movq_rm64_xmm,
		// Token: 0x04001DB8 RID: 7608
		VEX_Vmovd_rm32_xmm,
		// Token: 0x04001DB9 RID: 7609
		VEX_Vmovq_rm64_xmm,
		// Token: 0x04001DBA RID: 7610
		EVEX_Vmovd_rm32_xmm,
		// Token: 0x04001DBB RID: 7611
		EVEX_Vmovq_rm64_xmm,
		// Token: 0x04001DBC RID: 7612
		Movq_xmm_xmmm64,
		// Token: 0x04001DBD RID: 7613
		VEX_Vmovq_xmm_xmmm64,
		// Token: 0x04001DBE RID: 7614
		EVEX_Vmovq_xmm_xmmm64,
		// Token: 0x04001DBF RID: 7615
		Movq_mmm64_mm,
		// Token: 0x04001DC0 RID: 7616
		Movdqa_xmmm128_xmm,
		// Token: 0x04001DC1 RID: 7617
		VEX_Vmovdqa_xmmm128_xmm,
		// Token: 0x04001DC2 RID: 7618
		VEX_Vmovdqa_ymmm256_ymm,
		// Token: 0x04001DC3 RID: 7619
		EVEX_Vmovdqa32_xmmm128_k1z_xmm,
		// Token: 0x04001DC4 RID: 7620
		EVEX_Vmovdqa32_ymmm256_k1z_ymm,
		// Token: 0x04001DC5 RID: 7621
		EVEX_Vmovdqa32_zmmm512_k1z_zmm,
		// Token: 0x04001DC6 RID: 7622
		EVEX_Vmovdqa64_xmmm128_k1z_xmm,
		// Token: 0x04001DC7 RID: 7623
		EVEX_Vmovdqa64_ymmm256_k1z_ymm,
		// Token: 0x04001DC8 RID: 7624
		EVEX_Vmovdqa64_zmmm512_k1z_zmm,
		// Token: 0x04001DC9 RID: 7625
		Movdqu_xmmm128_xmm,
		// Token: 0x04001DCA RID: 7626
		VEX_Vmovdqu_xmmm128_xmm,
		// Token: 0x04001DCB RID: 7627
		VEX_Vmovdqu_ymmm256_ymm,
		// Token: 0x04001DCC RID: 7628
		EVEX_Vmovdqu32_xmmm128_k1z_xmm,
		// Token: 0x04001DCD RID: 7629
		EVEX_Vmovdqu32_ymmm256_k1z_ymm,
		// Token: 0x04001DCE RID: 7630
		EVEX_Vmovdqu32_zmmm512_k1z_zmm,
		// Token: 0x04001DCF RID: 7631
		EVEX_Vmovdqu64_xmmm128_k1z_xmm,
		// Token: 0x04001DD0 RID: 7632
		EVEX_Vmovdqu64_ymmm256_k1z_ymm,
		// Token: 0x04001DD1 RID: 7633
		EVEX_Vmovdqu64_zmmm512_k1z_zmm,
		// Token: 0x04001DD2 RID: 7634
		EVEX_Vmovdqu8_xmmm128_k1z_xmm,
		// Token: 0x04001DD3 RID: 7635
		EVEX_Vmovdqu8_ymmm256_k1z_ymm,
		// Token: 0x04001DD4 RID: 7636
		EVEX_Vmovdqu8_zmmm512_k1z_zmm,
		// Token: 0x04001DD5 RID: 7637
		EVEX_Vmovdqu16_xmmm128_k1z_xmm,
		// Token: 0x04001DD6 RID: 7638
		EVEX_Vmovdqu16_ymmm256_k1z_ymm,
		// Token: 0x04001DD7 RID: 7639
		EVEX_Vmovdqu16_zmmm512_k1z_zmm,
		// Token: 0x04001DD8 RID: 7640
		Jo_rel16,
		// Token: 0x04001DD9 RID: 7641
		Jo_rel32_32,
		// Token: 0x04001DDA RID: 7642
		Jo_rel32_64,
		// Token: 0x04001DDB RID: 7643
		Jno_rel16,
		// Token: 0x04001DDC RID: 7644
		Jno_rel32_32,
		// Token: 0x04001DDD RID: 7645
		Jno_rel32_64,
		// Token: 0x04001DDE RID: 7646
		Jb_rel16,
		// Token: 0x04001DDF RID: 7647
		Jb_rel32_32,
		// Token: 0x04001DE0 RID: 7648
		Jb_rel32_64,
		// Token: 0x04001DE1 RID: 7649
		Jae_rel16,
		// Token: 0x04001DE2 RID: 7650
		Jae_rel32_32,
		// Token: 0x04001DE3 RID: 7651
		Jae_rel32_64,
		// Token: 0x04001DE4 RID: 7652
		Je_rel16,
		// Token: 0x04001DE5 RID: 7653
		Je_rel32_32,
		// Token: 0x04001DE6 RID: 7654
		Je_rel32_64,
		// Token: 0x04001DE7 RID: 7655
		Jne_rel16,
		// Token: 0x04001DE8 RID: 7656
		Jne_rel32_32,
		// Token: 0x04001DE9 RID: 7657
		Jne_rel32_64,
		// Token: 0x04001DEA RID: 7658
		Jbe_rel16,
		// Token: 0x04001DEB RID: 7659
		Jbe_rel32_32,
		// Token: 0x04001DEC RID: 7660
		Jbe_rel32_64,
		// Token: 0x04001DED RID: 7661
		Ja_rel16,
		// Token: 0x04001DEE RID: 7662
		Ja_rel32_32,
		// Token: 0x04001DEF RID: 7663
		Ja_rel32_64,
		// Token: 0x04001DF0 RID: 7664
		Js_rel16,
		// Token: 0x04001DF1 RID: 7665
		Js_rel32_32,
		// Token: 0x04001DF2 RID: 7666
		Js_rel32_64,
		// Token: 0x04001DF3 RID: 7667
		Jns_rel16,
		// Token: 0x04001DF4 RID: 7668
		Jns_rel32_32,
		// Token: 0x04001DF5 RID: 7669
		Jns_rel32_64,
		// Token: 0x04001DF6 RID: 7670
		Jp_rel16,
		// Token: 0x04001DF7 RID: 7671
		Jp_rel32_32,
		// Token: 0x04001DF8 RID: 7672
		Jp_rel32_64,
		// Token: 0x04001DF9 RID: 7673
		Jnp_rel16,
		// Token: 0x04001DFA RID: 7674
		Jnp_rel32_32,
		// Token: 0x04001DFB RID: 7675
		Jnp_rel32_64,
		// Token: 0x04001DFC RID: 7676
		Jl_rel16,
		// Token: 0x04001DFD RID: 7677
		Jl_rel32_32,
		// Token: 0x04001DFE RID: 7678
		Jl_rel32_64,
		// Token: 0x04001DFF RID: 7679
		Jge_rel16,
		// Token: 0x04001E00 RID: 7680
		Jge_rel32_32,
		// Token: 0x04001E01 RID: 7681
		Jge_rel32_64,
		// Token: 0x04001E02 RID: 7682
		Jle_rel16,
		// Token: 0x04001E03 RID: 7683
		Jle_rel32_32,
		// Token: 0x04001E04 RID: 7684
		Jle_rel32_64,
		// Token: 0x04001E05 RID: 7685
		Jg_rel16,
		// Token: 0x04001E06 RID: 7686
		Jg_rel32_32,
		// Token: 0x04001E07 RID: 7687
		Jg_rel32_64,
		// Token: 0x04001E08 RID: 7688
		Seto_rm8,
		// Token: 0x04001E09 RID: 7689
		Setno_rm8,
		// Token: 0x04001E0A RID: 7690
		Setb_rm8,
		// Token: 0x04001E0B RID: 7691
		Setae_rm8,
		// Token: 0x04001E0C RID: 7692
		Sete_rm8,
		// Token: 0x04001E0D RID: 7693
		Setne_rm8,
		// Token: 0x04001E0E RID: 7694
		Setbe_rm8,
		// Token: 0x04001E0F RID: 7695
		Seta_rm8,
		// Token: 0x04001E10 RID: 7696
		Sets_rm8,
		// Token: 0x04001E11 RID: 7697
		Setns_rm8,
		// Token: 0x04001E12 RID: 7698
		Setp_rm8,
		// Token: 0x04001E13 RID: 7699
		Setnp_rm8,
		// Token: 0x04001E14 RID: 7700
		Setl_rm8,
		// Token: 0x04001E15 RID: 7701
		Setge_rm8,
		// Token: 0x04001E16 RID: 7702
		Setle_rm8,
		// Token: 0x04001E17 RID: 7703
		Setg_rm8,
		// Token: 0x04001E18 RID: 7704
		VEX_Kmovw_kr_km16,
		// Token: 0x04001E19 RID: 7705
		VEX_Kmovq_kr_km64,
		// Token: 0x04001E1A RID: 7706
		VEX_Kmovb_kr_km8,
		// Token: 0x04001E1B RID: 7707
		VEX_Kmovd_kr_km32,
		// Token: 0x04001E1C RID: 7708
		VEX_Kmovw_m16_kr,
		// Token: 0x04001E1D RID: 7709
		VEX_Kmovq_m64_kr,
		// Token: 0x04001E1E RID: 7710
		VEX_Kmovb_m8_kr,
		// Token: 0x04001E1F RID: 7711
		VEX_Kmovd_m32_kr,
		// Token: 0x04001E20 RID: 7712
		VEX_Kmovw_kr_r32,
		// Token: 0x04001E21 RID: 7713
		VEX_Kmovb_kr_r32,
		// Token: 0x04001E22 RID: 7714
		VEX_Kmovd_kr_r32,
		// Token: 0x04001E23 RID: 7715
		VEX_Kmovq_kr_r64,
		// Token: 0x04001E24 RID: 7716
		VEX_Kmovw_r32_kr,
		// Token: 0x04001E25 RID: 7717
		VEX_Kmovb_r32_kr,
		// Token: 0x04001E26 RID: 7718
		VEX_Kmovd_r32_kr,
		// Token: 0x04001E27 RID: 7719
		VEX_Kmovq_r64_kr,
		// Token: 0x04001E28 RID: 7720
		VEX_Kortestw_kr_kr,
		// Token: 0x04001E29 RID: 7721
		VEX_Kortestq_kr_kr,
		// Token: 0x04001E2A RID: 7722
		VEX_Kortestb_kr_kr,
		// Token: 0x04001E2B RID: 7723
		VEX_Kortestd_kr_kr,
		// Token: 0x04001E2C RID: 7724
		VEX_Ktestw_kr_kr,
		// Token: 0x04001E2D RID: 7725
		VEX_Ktestq_kr_kr,
		// Token: 0x04001E2E RID: 7726
		VEX_Ktestb_kr_kr,
		// Token: 0x04001E2F RID: 7727
		VEX_Ktestd_kr_kr,
		// Token: 0x04001E30 RID: 7728
		Pushw_FS,
		// Token: 0x04001E31 RID: 7729
		Pushd_FS,
		// Token: 0x04001E32 RID: 7730
		Pushq_FS,
		// Token: 0x04001E33 RID: 7731
		Popw_FS,
		// Token: 0x04001E34 RID: 7732
		Popd_FS,
		// Token: 0x04001E35 RID: 7733
		Popq_FS,
		// Token: 0x04001E36 RID: 7734
		Cpuid,
		// Token: 0x04001E37 RID: 7735
		Bt_rm16_r16,
		// Token: 0x04001E38 RID: 7736
		Bt_rm32_r32,
		// Token: 0x04001E39 RID: 7737
		Bt_rm64_r64,
		// Token: 0x04001E3A RID: 7738
		Shld_rm16_r16_imm8,
		// Token: 0x04001E3B RID: 7739
		Shld_rm32_r32_imm8,
		// Token: 0x04001E3C RID: 7740
		Shld_rm64_r64_imm8,
		// Token: 0x04001E3D RID: 7741
		Shld_rm16_r16_CL,
		// Token: 0x04001E3E RID: 7742
		Shld_rm32_r32_CL,
		// Token: 0x04001E3F RID: 7743
		Shld_rm64_r64_CL,
		// Token: 0x04001E40 RID: 7744
		Montmul_16,
		// Token: 0x04001E41 RID: 7745
		Montmul_32,
		// Token: 0x04001E42 RID: 7746
		Montmul_64,
		// Token: 0x04001E43 RID: 7747
		Xsha1_16,
		// Token: 0x04001E44 RID: 7748
		Xsha1_32,
		// Token: 0x04001E45 RID: 7749
		Xsha1_64,
		// Token: 0x04001E46 RID: 7750
		Xsha256_16,
		// Token: 0x04001E47 RID: 7751
		Xsha256_32,
		// Token: 0x04001E48 RID: 7752
		Xsha256_64,
		// Token: 0x04001E49 RID: 7753
		Xbts_r16_rm16,
		// Token: 0x04001E4A RID: 7754
		Xbts_r32_rm32,
		// Token: 0x04001E4B RID: 7755
		Xstore_16,
		// Token: 0x04001E4C RID: 7756
		Xstore_32,
		// Token: 0x04001E4D RID: 7757
		Xstore_64,
		// Token: 0x04001E4E RID: 7758
		Xcryptecb_16,
		// Token: 0x04001E4F RID: 7759
		Xcryptecb_32,
		// Token: 0x04001E50 RID: 7760
		Xcryptecb_64,
		// Token: 0x04001E51 RID: 7761
		Xcryptcbc_16,
		// Token: 0x04001E52 RID: 7762
		Xcryptcbc_32,
		// Token: 0x04001E53 RID: 7763
		Xcryptcbc_64,
		// Token: 0x04001E54 RID: 7764
		Xcryptctr_16,
		// Token: 0x04001E55 RID: 7765
		Xcryptctr_32,
		// Token: 0x04001E56 RID: 7766
		Xcryptctr_64,
		// Token: 0x04001E57 RID: 7767
		Xcryptcfb_16,
		// Token: 0x04001E58 RID: 7768
		Xcryptcfb_32,
		// Token: 0x04001E59 RID: 7769
		Xcryptcfb_64,
		// Token: 0x04001E5A RID: 7770
		Xcryptofb_16,
		// Token: 0x04001E5B RID: 7771
		Xcryptofb_32,
		// Token: 0x04001E5C RID: 7772
		Xcryptofb_64,
		// Token: 0x04001E5D RID: 7773
		Ibts_rm16_r16,
		// Token: 0x04001E5E RID: 7774
		Ibts_rm32_r32,
		// Token: 0x04001E5F RID: 7775
		Cmpxchg486_rm8_r8,
		// Token: 0x04001E60 RID: 7776
		Cmpxchg486_rm16_r16,
		// Token: 0x04001E61 RID: 7777
		Cmpxchg486_rm32_r32,
		// Token: 0x04001E62 RID: 7778
		Pushw_GS,
		// Token: 0x04001E63 RID: 7779
		Pushd_GS,
		// Token: 0x04001E64 RID: 7780
		Pushq_GS,
		// Token: 0x04001E65 RID: 7781
		Popw_GS,
		// Token: 0x04001E66 RID: 7782
		Popd_GS,
		// Token: 0x04001E67 RID: 7783
		Popq_GS,
		// Token: 0x04001E68 RID: 7784
		Rsm,
		// Token: 0x04001E69 RID: 7785
		Bts_rm16_r16,
		// Token: 0x04001E6A RID: 7786
		Bts_rm32_r32,
		// Token: 0x04001E6B RID: 7787
		Bts_rm64_r64,
		// Token: 0x04001E6C RID: 7788
		Shrd_rm16_r16_imm8,
		// Token: 0x04001E6D RID: 7789
		Shrd_rm32_r32_imm8,
		// Token: 0x04001E6E RID: 7790
		Shrd_rm64_r64_imm8,
		// Token: 0x04001E6F RID: 7791
		Shrd_rm16_r16_CL,
		// Token: 0x04001E70 RID: 7792
		Shrd_rm32_r32_CL,
		// Token: 0x04001E71 RID: 7793
		Shrd_rm64_r64_CL,
		// Token: 0x04001E72 RID: 7794
		Fxsave_m512byte,
		// Token: 0x04001E73 RID: 7795
		Fxsave64_m512byte,
		// Token: 0x04001E74 RID: 7796
		Rdfsbase_r32,
		// Token: 0x04001E75 RID: 7797
		Rdfsbase_r64,
		// Token: 0x04001E76 RID: 7798
		Fxrstor_m512byte,
		// Token: 0x04001E77 RID: 7799
		Fxrstor64_m512byte,
		// Token: 0x04001E78 RID: 7800
		Rdgsbase_r32,
		// Token: 0x04001E79 RID: 7801
		Rdgsbase_r64,
		// Token: 0x04001E7A RID: 7802
		Ldmxcsr_m32,
		// Token: 0x04001E7B RID: 7803
		Wrfsbase_r32,
		// Token: 0x04001E7C RID: 7804
		Wrfsbase_r64,
		// Token: 0x04001E7D RID: 7805
		VEX_Vldmxcsr_m32,
		// Token: 0x04001E7E RID: 7806
		Stmxcsr_m32,
		// Token: 0x04001E7F RID: 7807
		Wrgsbase_r32,
		// Token: 0x04001E80 RID: 7808
		Wrgsbase_r64,
		// Token: 0x04001E81 RID: 7809
		VEX_Vstmxcsr_m32,
		// Token: 0x04001E82 RID: 7810
		Xsave_mem,
		// Token: 0x04001E83 RID: 7811
		Xsave64_mem,
		// Token: 0x04001E84 RID: 7812
		Ptwrite_rm32,
		// Token: 0x04001E85 RID: 7813
		Ptwrite_rm64,
		// Token: 0x04001E86 RID: 7814
		Xrstor_mem,
		// Token: 0x04001E87 RID: 7815
		Xrstor64_mem,
		// Token: 0x04001E88 RID: 7816
		Incsspd_r32,
		// Token: 0x04001E89 RID: 7817
		Incsspq_r64,
		// Token: 0x04001E8A RID: 7818
		Xsaveopt_mem,
		// Token: 0x04001E8B RID: 7819
		Xsaveopt64_mem,
		// Token: 0x04001E8C RID: 7820
		Clwb_m8,
		// Token: 0x04001E8D RID: 7821
		Tpause_r32,
		// Token: 0x04001E8E RID: 7822
		Tpause_r64,
		// Token: 0x04001E8F RID: 7823
		Clrssbsy_m64,
		// Token: 0x04001E90 RID: 7824
		Umonitor_r16,
		// Token: 0x04001E91 RID: 7825
		Umonitor_r32,
		// Token: 0x04001E92 RID: 7826
		Umonitor_r64,
		// Token: 0x04001E93 RID: 7827
		Umwait_r32,
		// Token: 0x04001E94 RID: 7828
		Umwait_r64,
		// Token: 0x04001E95 RID: 7829
		Clflush_m8,
		// Token: 0x04001E96 RID: 7830
		Clflushopt_m8,
		// Token: 0x04001E97 RID: 7831
		Lfence,
		// Token: 0x04001E98 RID: 7832
		Lfence_E9,
		// Token: 0x04001E99 RID: 7833
		Lfence_EA,
		// Token: 0x04001E9A RID: 7834
		Lfence_EB,
		// Token: 0x04001E9B RID: 7835
		Lfence_EC,
		// Token: 0x04001E9C RID: 7836
		Lfence_ED,
		// Token: 0x04001E9D RID: 7837
		Lfence_EE,
		// Token: 0x04001E9E RID: 7838
		Lfence_EF,
		// Token: 0x04001E9F RID: 7839
		Mfence,
		// Token: 0x04001EA0 RID: 7840
		Mfence_F1,
		// Token: 0x04001EA1 RID: 7841
		Mfence_F2,
		// Token: 0x04001EA2 RID: 7842
		Mfence_F3,
		// Token: 0x04001EA3 RID: 7843
		Mfence_F4,
		// Token: 0x04001EA4 RID: 7844
		Mfence_F5,
		// Token: 0x04001EA5 RID: 7845
		Mfence_F6,
		// Token: 0x04001EA6 RID: 7846
		Mfence_F7,
		// Token: 0x04001EA7 RID: 7847
		Sfence,
		// Token: 0x04001EA8 RID: 7848
		Sfence_F9,
		// Token: 0x04001EA9 RID: 7849
		Sfence_FA,
		// Token: 0x04001EAA RID: 7850
		Sfence_FB,
		// Token: 0x04001EAB RID: 7851
		Sfence_FC,
		// Token: 0x04001EAC RID: 7852
		Sfence_FD,
		// Token: 0x04001EAD RID: 7853
		Sfence_FE,
		// Token: 0x04001EAE RID: 7854
		Sfence_FF,
		// Token: 0x04001EAF RID: 7855
		Pcommit,
		// Token: 0x04001EB0 RID: 7856
		Imul_r16_rm16,
		// Token: 0x04001EB1 RID: 7857
		Imul_r32_rm32,
		// Token: 0x04001EB2 RID: 7858
		Imul_r64_rm64,
		// Token: 0x04001EB3 RID: 7859
		Cmpxchg_rm8_r8,
		// Token: 0x04001EB4 RID: 7860
		Cmpxchg_rm16_r16,
		// Token: 0x04001EB5 RID: 7861
		Cmpxchg_rm32_r32,
		// Token: 0x04001EB6 RID: 7862
		Cmpxchg_rm64_r64,
		// Token: 0x04001EB7 RID: 7863
		Lss_r16_m1616,
		// Token: 0x04001EB8 RID: 7864
		Lss_r32_m1632,
		// Token: 0x04001EB9 RID: 7865
		Lss_r64_m1664,
		// Token: 0x04001EBA RID: 7866
		Btr_rm16_r16,
		// Token: 0x04001EBB RID: 7867
		Btr_rm32_r32,
		// Token: 0x04001EBC RID: 7868
		Btr_rm64_r64,
		// Token: 0x04001EBD RID: 7869
		Lfs_r16_m1616,
		// Token: 0x04001EBE RID: 7870
		Lfs_r32_m1632,
		// Token: 0x04001EBF RID: 7871
		Lfs_r64_m1664,
		// Token: 0x04001EC0 RID: 7872
		Lgs_r16_m1616,
		// Token: 0x04001EC1 RID: 7873
		Lgs_r32_m1632,
		// Token: 0x04001EC2 RID: 7874
		Lgs_r64_m1664,
		// Token: 0x04001EC3 RID: 7875
		Movzx_r16_rm8,
		// Token: 0x04001EC4 RID: 7876
		Movzx_r32_rm8,
		// Token: 0x04001EC5 RID: 7877
		Movzx_r64_rm8,
		// Token: 0x04001EC6 RID: 7878
		Movzx_r16_rm16,
		// Token: 0x04001EC7 RID: 7879
		Movzx_r32_rm16,
		// Token: 0x04001EC8 RID: 7880
		Movzx_r64_rm16,
		// Token: 0x04001EC9 RID: 7881
		Jmpe_disp16,
		// Token: 0x04001ECA RID: 7882
		Jmpe_disp32,
		// Token: 0x04001ECB RID: 7883
		Popcnt_r16_rm16,
		// Token: 0x04001ECC RID: 7884
		Popcnt_r32_rm32,
		// Token: 0x04001ECD RID: 7885
		Popcnt_r64_rm64,
		// Token: 0x04001ECE RID: 7886
		Ud1_r16_rm16,
		// Token: 0x04001ECF RID: 7887
		Ud1_r32_rm32,
		// Token: 0x04001ED0 RID: 7888
		Ud1_r64_rm64,
		// Token: 0x04001ED1 RID: 7889
		Bt_rm16_imm8,
		// Token: 0x04001ED2 RID: 7890
		Bt_rm32_imm8,
		// Token: 0x04001ED3 RID: 7891
		Bt_rm64_imm8,
		// Token: 0x04001ED4 RID: 7892
		Bts_rm16_imm8,
		// Token: 0x04001ED5 RID: 7893
		Bts_rm32_imm8,
		// Token: 0x04001ED6 RID: 7894
		Bts_rm64_imm8,
		// Token: 0x04001ED7 RID: 7895
		Btr_rm16_imm8,
		// Token: 0x04001ED8 RID: 7896
		Btr_rm32_imm8,
		// Token: 0x04001ED9 RID: 7897
		Btr_rm64_imm8,
		// Token: 0x04001EDA RID: 7898
		Btc_rm16_imm8,
		// Token: 0x04001EDB RID: 7899
		Btc_rm32_imm8,
		// Token: 0x04001EDC RID: 7900
		Btc_rm64_imm8,
		// Token: 0x04001EDD RID: 7901
		Btc_rm16_r16,
		// Token: 0x04001EDE RID: 7902
		Btc_rm32_r32,
		// Token: 0x04001EDF RID: 7903
		Btc_rm64_r64,
		// Token: 0x04001EE0 RID: 7904
		Bsf_r16_rm16,
		// Token: 0x04001EE1 RID: 7905
		Bsf_r32_rm32,
		// Token: 0x04001EE2 RID: 7906
		Bsf_r64_rm64,
		// Token: 0x04001EE3 RID: 7907
		Tzcnt_r16_rm16,
		// Token: 0x04001EE4 RID: 7908
		Tzcnt_r32_rm32,
		// Token: 0x04001EE5 RID: 7909
		Tzcnt_r64_rm64,
		// Token: 0x04001EE6 RID: 7910
		Bsr_r16_rm16,
		// Token: 0x04001EE7 RID: 7911
		Bsr_r32_rm32,
		// Token: 0x04001EE8 RID: 7912
		Bsr_r64_rm64,
		// Token: 0x04001EE9 RID: 7913
		Lzcnt_r16_rm16,
		// Token: 0x04001EEA RID: 7914
		Lzcnt_r32_rm32,
		// Token: 0x04001EEB RID: 7915
		Lzcnt_r64_rm64,
		// Token: 0x04001EEC RID: 7916
		Movsx_r16_rm8,
		// Token: 0x04001EED RID: 7917
		Movsx_r32_rm8,
		// Token: 0x04001EEE RID: 7918
		Movsx_r64_rm8,
		// Token: 0x04001EEF RID: 7919
		Movsx_r16_rm16,
		// Token: 0x04001EF0 RID: 7920
		Movsx_r32_rm16,
		// Token: 0x04001EF1 RID: 7921
		Movsx_r64_rm16,
		// Token: 0x04001EF2 RID: 7922
		Xadd_rm8_r8,
		// Token: 0x04001EF3 RID: 7923
		Xadd_rm16_r16,
		// Token: 0x04001EF4 RID: 7924
		Xadd_rm32_r32,
		// Token: 0x04001EF5 RID: 7925
		Xadd_rm64_r64,
		// Token: 0x04001EF6 RID: 7926
		Cmpps_xmm_xmmm128_imm8,
		// Token: 0x04001EF7 RID: 7927
		VEX_Vcmpps_xmm_xmm_xmmm128_imm8,
		// Token: 0x04001EF8 RID: 7928
		VEX_Vcmpps_ymm_ymm_ymmm256_imm8,
		// Token: 0x04001EF9 RID: 7929
		EVEX_Vcmpps_kr_k1_xmm_xmmm128b32_imm8,
		// Token: 0x04001EFA RID: 7930
		EVEX_Vcmpps_kr_k1_ymm_ymmm256b32_imm8,
		// Token: 0x04001EFB RID: 7931
		EVEX_Vcmpps_kr_k1_zmm_zmmm512b32_imm8_sae,
		// Token: 0x04001EFC RID: 7932
		Cmppd_xmm_xmmm128_imm8,
		// Token: 0x04001EFD RID: 7933
		VEX_Vcmppd_xmm_xmm_xmmm128_imm8,
		// Token: 0x04001EFE RID: 7934
		VEX_Vcmppd_ymm_ymm_ymmm256_imm8,
		// Token: 0x04001EFF RID: 7935
		EVEX_Vcmppd_kr_k1_xmm_xmmm128b64_imm8,
		// Token: 0x04001F00 RID: 7936
		EVEX_Vcmppd_kr_k1_ymm_ymmm256b64_imm8,
		// Token: 0x04001F01 RID: 7937
		EVEX_Vcmppd_kr_k1_zmm_zmmm512b64_imm8_sae,
		// Token: 0x04001F02 RID: 7938
		Cmpss_xmm_xmmm32_imm8,
		// Token: 0x04001F03 RID: 7939
		VEX_Vcmpss_xmm_xmm_xmmm32_imm8,
		// Token: 0x04001F04 RID: 7940
		EVEX_Vcmpss_kr_k1_xmm_xmmm32_imm8_sae,
		// Token: 0x04001F05 RID: 7941
		Cmpsd_xmm_xmmm64_imm8,
		// Token: 0x04001F06 RID: 7942
		VEX_Vcmpsd_xmm_xmm_xmmm64_imm8,
		// Token: 0x04001F07 RID: 7943
		EVEX_Vcmpsd_kr_k1_xmm_xmmm64_imm8_sae,
		// Token: 0x04001F08 RID: 7944
		Movnti_m32_r32,
		// Token: 0x04001F09 RID: 7945
		Movnti_m64_r64,
		// Token: 0x04001F0A RID: 7946
		Pinsrw_mm_r32m16_imm8,
		// Token: 0x04001F0B RID: 7947
		Pinsrw_mm_r64m16_imm8,
		// Token: 0x04001F0C RID: 7948
		Pinsrw_xmm_r32m16_imm8,
		// Token: 0x04001F0D RID: 7949
		Pinsrw_xmm_r64m16_imm8,
		// Token: 0x04001F0E RID: 7950
		VEX_Vpinsrw_xmm_xmm_r32m16_imm8,
		// Token: 0x04001F0F RID: 7951
		VEX_Vpinsrw_xmm_xmm_r64m16_imm8,
		// Token: 0x04001F10 RID: 7952
		EVEX_Vpinsrw_xmm_xmm_r32m16_imm8,
		// Token: 0x04001F11 RID: 7953
		EVEX_Vpinsrw_xmm_xmm_r64m16_imm8,
		// Token: 0x04001F12 RID: 7954
		Pextrw_r32_mm_imm8,
		// Token: 0x04001F13 RID: 7955
		Pextrw_r64_mm_imm8,
		// Token: 0x04001F14 RID: 7956
		Pextrw_r32_xmm_imm8,
		// Token: 0x04001F15 RID: 7957
		Pextrw_r64_xmm_imm8,
		// Token: 0x04001F16 RID: 7958
		VEX_Vpextrw_r32_xmm_imm8,
		// Token: 0x04001F17 RID: 7959
		VEX_Vpextrw_r64_xmm_imm8,
		// Token: 0x04001F18 RID: 7960
		EVEX_Vpextrw_r32_xmm_imm8,
		// Token: 0x04001F19 RID: 7961
		EVEX_Vpextrw_r64_xmm_imm8,
		// Token: 0x04001F1A RID: 7962
		Shufps_xmm_xmmm128_imm8,
		// Token: 0x04001F1B RID: 7963
		VEX_Vshufps_xmm_xmm_xmmm128_imm8,
		// Token: 0x04001F1C RID: 7964
		VEX_Vshufps_ymm_ymm_ymmm256_imm8,
		// Token: 0x04001F1D RID: 7965
		EVEX_Vshufps_xmm_k1z_xmm_xmmm128b32_imm8,
		// Token: 0x04001F1E RID: 7966
		EVEX_Vshufps_ymm_k1z_ymm_ymmm256b32_imm8,
		// Token: 0x04001F1F RID: 7967
		EVEX_Vshufps_zmm_k1z_zmm_zmmm512b32_imm8,
		// Token: 0x04001F20 RID: 7968
		Shufpd_xmm_xmmm128_imm8,
		// Token: 0x04001F21 RID: 7969
		VEX_Vshufpd_xmm_xmm_xmmm128_imm8,
		// Token: 0x04001F22 RID: 7970
		VEX_Vshufpd_ymm_ymm_ymmm256_imm8,
		// Token: 0x04001F23 RID: 7971
		EVEX_Vshufpd_xmm_k1z_xmm_xmmm128b64_imm8,
		// Token: 0x04001F24 RID: 7972
		EVEX_Vshufpd_ymm_k1z_ymm_ymmm256b64_imm8,
		// Token: 0x04001F25 RID: 7973
		EVEX_Vshufpd_zmm_k1z_zmm_zmmm512b64_imm8,
		// Token: 0x04001F26 RID: 7974
		Cmpxchg8b_m64,
		// Token: 0x04001F27 RID: 7975
		Cmpxchg16b_m128,
		// Token: 0x04001F28 RID: 7976
		Xrstors_mem,
		// Token: 0x04001F29 RID: 7977
		Xrstors64_mem,
		// Token: 0x04001F2A RID: 7978
		Xsavec_mem,
		// Token: 0x04001F2B RID: 7979
		Xsavec64_mem,
		// Token: 0x04001F2C RID: 7980
		Xsaves_mem,
		// Token: 0x04001F2D RID: 7981
		Xsaves64_mem,
		// Token: 0x04001F2E RID: 7982
		Vmptrld_m64,
		// Token: 0x04001F2F RID: 7983
		Vmclear_m64,
		// Token: 0x04001F30 RID: 7984
		Vmxon_m64,
		// Token: 0x04001F31 RID: 7985
		Rdrand_r16,
		// Token: 0x04001F32 RID: 7986
		Rdrand_r32,
		// Token: 0x04001F33 RID: 7987
		Rdrand_r64,
		// Token: 0x04001F34 RID: 7988
		Vmptrst_m64,
		// Token: 0x04001F35 RID: 7989
		Rdseed_r16,
		// Token: 0x04001F36 RID: 7990
		Rdseed_r32,
		// Token: 0x04001F37 RID: 7991
		Rdseed_r64,
		// Token: 0x04001F38 RID: 7992
		Rdpid_r32,
		// Token: 0x04001F39 RID: 7993
		Rdpid_r64,
		// Token: 0x04001F3A RID: 7994
		Bswap_r16,
		// Token: 0x04001F3B RID: 7995
		Bswap_r32,
		// Token: 0x04001F3C RID: 7996
		Bswap_r64,
		// Token: 0x04001F3D RID: 7997
		Addsubpd_xmm_xmmm128,
		// Token: 0x04001F3E RID: 7998
		VEX_Vaddsubpd_xmm_xmm_xmmm128,
		// Token: 0x04001F3F RID: 7999
		VEX_Vaddsubpd_ymm_ymm_ymmm256,
		// Token: 0x04001F40 RID: 8000
		Addsubps_xmm_xmmm128,
		// Token: 0x04001F41 RID: 8001
		VEX_Vaddsubps_xmm_xmm_xmmm128,
		// Token: 0x04001F42 RID: 8002
		VEX_Vaddsubps_ymm_ymm_ymmm256,
		// Token: 0x04001F43 RID: 8003
		Psrlw_mm_mmm64,
		// Token: 0x04001F44 RID: 8004
		Psrlw_xmm_xmmm128,
		// Token: 0x04001F45 RID: 8005
		VEX_Vpsrlw_xmm_xmm_xmmm128,
		// Token: 0x04001F46 RID: 8006
		VEX_Vpsrlw_ymm_ymm_xmmm128,
		// Token: 0x04001F47 RID: 8007
		EVEX_Vpsrlw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001F48 RID: 8008
		EVEX_Vpsrlw_ymm_k1z_ymm_xmmm128,
		// Token: 0x04001F49 RID: 8009
		EVEX_Vpsrlw_zmm_k1z_zmm_xmmm128,
		// Token: 0x04001F4A RID: 8010
		Psrld_mm_mmm64,
		// Token: 0x04001F4B RID: 8011
		Psrld_xmm_xmmm128,
		// Token: 0x04001F4C RID: 8012
		VEX_Vpsrld_xmm_xmm_xmmm128,
		// Token: 0x04001F4D RID: 8013
		VEX_Vpsrld_ymm_ymm_xmmm128,
		// Token: 0x04001F4E RID: 8014
		EVEX_Vpsrld_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001F4F RID: 8015
		EVEX_Vpsrld_ymm_k1z_ymm_xmmm128,
		// Token: 0x04001F50 RID: 8016
		EVEX_Vpsrld_zmm_k1z_zmm_xmmm128,
		// Token: 0x04001F51 RID: 8017
		Psrlq_mm_mmm64,
		// Token: 0x04001F52 RID: 8018
		Psrlq_xmm_xmmm128,
		// Token: 0x04001F53 RID: 8019
		VEX_Vpsrlq_xmm_xmm_xmmm128,
		// Token: 0x04001F54 RID: 8020
		VEX_Vpsrlq_ymm_ymm_xmmm128,
		// Token: 0x04001F55 RID: 8021
		EVEX_Vpsrlq_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001F56 RID: 8022
		EVEX_Vpsrlq_ymm_k1z_ymm_xmmm128,
		// Token: 0x04001F57 RID: 8023
		EVEX_Vpsrlq_zmm_k1z_zmm_xmmm128,
		// Token: 0x04001F58 RID: 8024
		Paddq_mm_mmm64,
		// Token: 0x04001F59 RID: 8025
		Paddq_xmm_xmmm128,
		// Token: 0x04001F5A RID: 8026
		VEX_Vpaddq_xmm_xmm_xmmm128,
		// Token: 0x04001F5B RID: 8027
		VEX_Vpaddq_ymm_ymm_ymmm256,
		// Token: 0x04001F5C RID: 8028
		EVEX_Vpaddq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001F5D RID: 8029
		EVEX_Vpaddq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001F5E RID: 8030
		EVEX_Vpaddq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04001F5F RID: 8031
		Pmullw_mm_mmm64,
		// Token: 0x04001F60 RID: 8032
		Pmullw_xmm_xmmm128,
		// Token: 0x04001F61 RID: 8033
		VEX_Vpmullw_xmm_xmm_xmmm128,
		// Token: 0x04001F62 RID: 8034
		VEX_Vpmullw_ymm_ymm_ymmm256,
		// Token: 0x04001F63 RID: 8035
		EVEX_Vpmullw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001F64 RID: 8036
		EVEX_Vpmullw_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001F65 RID: 8037
		EVEX_Vpmullw_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001F66 RID: 8038
		Movq_xmmm64_xmm,
		// Token: 0x04001F67 RID: 8039
		VEX_Vmovq_xmmm64_xmm,
		// Token: 0x04001F68 RID: 8040
		EVEX_Vmovq_xmmm64_xmm,
		// Token: 0x04001F69 RID: 8041
		Movq2dq_xmm_mm,
		// Token: 0x04001F6A RID: 8042
		Movdq2q_mm_xmm,
		// Token: 0x04001F6B RID: 8043
		Pmovmskb_r32_mm,
		// Token: 0x04001F6C RID: 8044
		Pmovmskb_r64_mm,
		// Token: 0x04001F6D RID: 8045
		Pmovmskb_r32_xmm,
		// Token: 0x04001F6E RID: 8046
		Pmovmskb_r64_xmm,
		// Token: 0x04001F6F RID: 8047
		VEX_Vpmovmskb_r32_xmm,
		// Token: 0x04001F70 RID: 8048
		VEX_Vpmovmskb_r64_xmm,
		// Token: 0x04001F71 RID: 8049
		VEX_Vpmovmskb_r32_ymm,
		// Token: 0x04001F72 RID: 8050
		VEX_Vpmovmskb_r64_ymm,
		// Token: 0x04001F73 RID: 8051
		Psubusb_mm_mmm64,
		// Token: 0x04001F74 RID: 8052
		Psubusb_xmm_xmmm128,
		// Token: 0x04001F75 RID: 8053
		VEX_Vpsubusb_xmm_xmm_xmmm128,
		// Token: 0x04001F76 RID: 8054
		VEX_Vpsubusb_ymm_ymm_ymmm256,
		// Token: 0x04001F77 RID: 8055
		EVEX_Vpsubusb_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001F78 RID: 8056
		EVEX_Vpsubusb_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001F79 RID: 8057
		EVEX_Vpsubusb_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001F7A RID: 8058
		Psubusw_mm_mmm64,
		// Token: 0x04001F7B RID: 8059
		Psubusw_xmm_xmmm128,
		// Token: 0x04001F7C RID: 8060
		VEX_Vpsubusw_xmm_xmm_xmmm128,
		// Token: 0x04001F7D RID: 8061
		VEX_Vpsubusw_ymm_ymm_ymmm256,
		// Token: 0x04001F7E RID: 8062
		EVEX_Vpsubusw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001F7F RID: 8063
		EVEX_Vpsubusw_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001F80 RID: 8064
		EVEX_Vpsubusw_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001F81 RID: 8065
		Pminub_mm_mmm64,
		// Token: 0x04001F82 RID: 8066
		Pminub_xmm_xmmm128,
		// Token: 0x04001F83 RID: 8067
		VEX_Vpminub_xmm_xmm_xmmm128,
		// Token: 0x04001F84 RID: 8068
		VEX_Vpminub_ymm_ymm_ymmm256,
		// Token: 0x04001F85 RID: 8069
		EVEX_Vpminub_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001F86 RID: 8070
		EVEX_Vpminub_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001F87 RID: 8071
		EVEX_Vpminub_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001F88 RID: 8072
		Pand_mm_mmm64,
		// Token: 0x04001F89 RID: 8073
		Pand_xmm_xmmm128,
		// Token: 0x04001F8A RID: 8074
		VEX_Vpand_xmm_xmm_xmmm128,
		// Token: 0x04001F8B RID: 8075
		VEX_Vpand_ymm_ymm_ymmm256,
		// Token: 0x04001F8C RID: 8076
		EVEX_Vpandd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001F8D RID: 8077
		EVEX_Vpandd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001F8E RID: 8078
		EVEX_Vpandd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04001F8F RID: 8079
		EVEX_Vpandq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001F90 RID: 8080
		EVEX_Vpandq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001F91 RID: 8081
		EVEX_Vpandq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04001F92 RID: 8082
		Paddusb_mm_mmm64,
		// Token: 0x04001F93 RID: 8083
		Paddusb_xmm_xmmm128,
		// Token: 0x04001F94 RID: 8084
		VEX_Vpaddusb_xmm_xmm_xmmm128,
		// Token: 0x04001F95 RID: 8085
		VEX_Vpaddusb_ymm_ymm_ymmm256,
		// Token: 0x04001F96 RID: 8086
		EVEX_Vpaddusb_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001F97 RID: 8087
		EVEX_Vpaddusb_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001F98 RID: 8088
		EVEX_Vpaddusb_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001F99 RID: 8089
		Paddusw_mm_mmm64,
		// Token: 0x04001F9A RID: 8090
		Paddusw_xmm_xmmm128,
		// Token: 0x04001F9B RID: 8091
		VEX_Vpaddusw_xmm_xmm_xmmm128,
		// Token: 0x04001F9C RID: 8092
		VEX_Vpaddusw_ymm_ymm_ymmm256,
		// Token: 0x04001F9D RID: 8093
		EVEX_Vpaddusw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001F9E RID: 8094
		EVEX_Vpaddusw_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001F9F RID: 8095
		EVEX_Vpaddusw_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001FA0 RID: 8096
		Pmaxub_mm_mmm64,
		// Token: 0x04001FA1 RID: 8097
		Pmaxub_xmm_xmmm128,
		// Token: 0x04001FA2 RID: 8098
		VEX_Vpmaxub_xmm_xmm_xmmm128,
		// Token: 0x04001FA3 RID: 8099
		VEX_Vpmaxub_ymm_ymm_ymmm256,
		// Token: 0x04001FA4 RID: 8100
		EVEX_Vpmaxub_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001FA5 RID: 8101
		EVEX_Vpmaxub_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001FA6 RID: 8102
		EVEX_Vpmaxub_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001FA7 RID: 8103
		Pandn_mm_mmm64,
		// Token: 0x04001FA8 RID: 8104
		Pandn_xmm_xmmm128,
		// Token: 0x04001FA9 RID: 8105
		VEX_Vpandn_xmm_xmm_xmmm128,
		// Token: 0x04001FAA RID: 8106
		VEX_Vpandn_ymm_ymm_ymmm256,
		// Token: 0x04001FAB RID: 8107
		EVEX_Vpandnd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04001FAC RID: 8108
		EVEX_Vpandnd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04001FAD RID: 8109
		EVEX_Vpandnd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04001FAE RID: 8110
		EVEX_Vpandnq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04001FAF RID: 8111
		EVEX_Vpandnq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04001FB0 RID: 8112
		EVEX_Vpandnq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04001FB1 RID: 8113
		Pavgb_mm_mmm64,
		// Token: 0x04001FB2 RID: 8114
		Pavgb_xmm_xmmm128,
		// Token: 0x04001FB3 RID: 8115
		VEX_Vpavgb_xmm_xmm_xmmm128,
		// Token: 0x04001FB4 RID: 8116
		VEX_Vpavgb_ymm_ymm_ymmm256,
		// Token: 0x04001FB5 RID: 8117
		EVEX_Vpavgb_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001FB6 RID: 8118
		EVEX_Vpavgb_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001FB7 RID: 8119
		EVEX_Vpavgb_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001FB8 RID: 8120
		Psraw_mm_mmm64,
		// Token: 0x04001FB9 RID: 8121
		Psraw_xmm_xmmm128,
		// Token: 0x04001FBA RID: 8122
		VEX_Vpsraw_xmm_xmm_xmmm128,
		// Token: 0x04001FBB RID: 8123
		VEX_Vpsraw_ymm_ymm_xmmm128,
		// Token: 0x04001FBC RID: 8124
		EVEX_Vpsraw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001FBD RID: 8125
		EVEX_Vpsraw_ymm_k1z_ymm_xmmm128,
		// Token: 0x04001FBE RID: 8126
		EVEX_Vpsraw_zmm_k1z_zmm_xmmm128,
		// Token: 0x04001FBF RID: 8127
		Psrad_mm_mmm64,
		// Token: 0x04001FC0 RID: 8128
		Psrad_xmm_xmmm128,
		// Token: 0x04001FC1 RID: 8129
		VEX_Vpsrad_xmm_xmm_xmmm128,
		// Token: 0x04001FC2 RID: 8130
		VEX_Vpsrad_ymm_ymm_xmmm128,
		// Token: 0x04001FC3 RID: 8131
		EVEX_Vpsrad_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001FC4 RID: 8132
		EVEX_Vpsrad_ymm_k1z_ymm_xmmm128,
		// Token: 0x04001FC5 RID: 8133
		EVEX_Vpsrad_zmm_k1z_zmm_xmmm128,
		// Token: 0x04001FC6 RID: 8134
		EVEX_Vpsraq_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001FC7 RID: 8135
		EVEX_Vpsraq_ymm_k1z_ymm_xmmm128,
		// Token: 0x04001FC8 RID: 8136
		EVEX_Vpsraq_zmm_k1z_zmm_xmmm128,
		// Token: 0x04001FC9 RID: 8137
		Pavgw_mm_mmm64,
		// Token: 0x04001FCA RID: 8138
		Pavgw_xmm_xmmm128,
		// Token: 0x04001FCB RID: 8139
		VEX_Vpavgw_xmm_xmm_xmmm128,
		// Token: 0x04001FCC RID: 8140
		VEX_Vpavgw_ymm_ymm_ymmm256,
		// Token: 0x04001FCD RID: 8141
		EVEX_Vpavgw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001FCE RID: 8142
		EVEX_Vpavgw_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001FCF RID: 8143
		EVEX_Vpavgw_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001FD0 RID: 8144
		Pmulhuw_mm_mmm64,
		// Token: 0x04001FD1 RID: 8145
		Pmulhuw_xmm_xmmm128,
		// Token: 0x04001FD2 RID: 8146
		VEX_Vpmulhuw_xmm_xmm_xmmm128,
		// Token: 0x04001FD3 RID: 8147
		VEX_Vpmulhuw_ymm_ymm_ymmm256,
		// Token: 0x04001FD4 RID: 8148
		EVEX_Vpmulhuw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001FD5 RID: 8149
		EVEX_Vpmulhuw_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001FD6 RID: 8150
		EVEX_Vpmulhuw_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001FD7 RID: 8151
		Pmulhw_mm_mmm64,
		// Token: 0x04001FD8 RID: 8152
		Pmulhw_xmm_xmmm128,
		// Token: 0x04001FD9 RID: 8153
		VEX_Vpmulhw_xmm_xmm_xmmm128,
		// Token: 0x04001FDA RID: 8154
		VEX_Vpmulhw_ymm_ymm_ymmm256,
		// Token: 0x04001FDB RID: 8155
		EVEX_Vpmulhw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001FDC RID: 8156
		EVEX_Vpmulhw_ymm_k1z_ymm_ymmm256,
		// Token: 0x04001FDD RID: 8157
		EVEX_Vpmulhw_zmm_k1z_zmm_zmmm512,
		// Token: 0x04001FDE RID: 8158
		Cvttpd2dq_xmm_xmmm128,
		// Token: 0x04001FDF RID: 8159
		VEX_Vcvttpd2dq_xmm_xmmm128,
		// Token: 0x04001FE0 RID: 8160
		VEX_Vcvttpd2dq_xmm_ymmm256,
		// Token: 0x04001FE1 RID: 8161
		EVEX_Vcvttpd2dq_xmm_k1z_xmmm128b64,
		// Token: 0x04001FE2 RID: 8162
		EVEX_Vcvttpd2dq_xmm_k1z_ymmm256b64,
		// Token: 0x04001FE3 RID: 8163
		EVEX_Vcvttpd2dq_ymm_k1z_zmmm512b64_sae,
		// Token: 0x04001FE4 RID: 8164
		Cvtdq2pd_xmm_xmmm64,
		// Token: 0x04001FE5 RID: 8165
		VEX_Vcvtdq2pd_xmm_xmmm64,
		// Token: 0x04001FE6 RID: 8166
		VEX_Vcvtdq2pd_ymm_xmmm128,
		// Token: 0x04001FE7 RID: 8167
		EVEX_Vcvtdq2pd_xmm_k1z_xmmm64b32,
		// Token: 0x04001FE8 RID: 8168
		EVEX_Vcvtdq2pd_ymm_k1z_xmmm128b32,
		// Token: 0x04001FE9 RID: 8169
		EVEX_Vcvtdq2pd_zmm_k1z_ymmm256b32_er,
		// Token: 0x04001FEA RID: 8170
		EVEX_Vcvtqq2pd_xmm_k1z_xmmm128b64,
		// Token: 0x04001FEB RID: 8171
		EVEX_Vcvtqq2pd_ymm_k1z_ymmm256b64,
		// Token: 0x04001FEC RID: 8172
		EVEX_Vcvtqq2pd_zmm_k1z_zmmm512b64_er,
		// Token: 0x04001FED RID: 8173
		Cvtpd2dq_xmm_xmmm128,
		// Token: 0x04001FEE RID: 8174
		VEX_Vcvtpd2dq_xmm_xmmm128,
		// Token: 0x04001FEF RID: 8175
		VEX_Vcvtpd2dq_xmm_ymmm256,
		// Token: 0x04001FF0 RID: 8176
		EVEX_Vcvtpd2dq_xmm_k1z_xmmm128b64,
		// Token: 0x04001FF1 RID: 8177
		EVEX_Vcvtpd2dq_xmm_k1z_ymmm256b64,
		// Token: 0x04001FF2 RID: 8178
		EVEX_Vcvtpd2dq_ymm_k1z_zmmm512b64_er,
		// Token: 0x04001FF3 RID: 8179
		Movntq_m64_mm,
		// Token: 0x04001FF4 RID: 8180
		Movntdq_m128_xmm,
		// Token: 0x04001FF5 RID: 8181
		VEX_Vmovntdq_m128_xmm,
		// Token: 0x04001FF6 RID: 8182
		VEX_Vmovntdq_m256_ymm,
		// Token: 0x04001FF7 RID: 8183
		EVEX_Vmovntdq_m128_xmm,
		// Token: 0x04001FF8 RID: 8184
		EVEX_Vmovntdq_m256_ymm,
		// Token: 0x04001FF9 RID: 8185
		EVEX_Vmovntdq_m512_zmm,
		// Token: 0x04001FFA RID: 8186
		Psubsb_mm_mmm64,
		// Token: 0x04001FFB RID: 8187
		Psubsb_xmm_xmmm128,
		// Token: 0x04001FFC RID: 8188
		VEX_Vpsubsb_xmm_xmm_xmmm128,
		// Token: 0x04001FFD RID: 8189
		VEX_Vpsubsb_ymm_ymm_ymmm256,
		// Token: 0x04001FFE RID: 8190
		EVEX_Vpsubsb_xmm_k1z_xmm_xmmm128,
		// Token: 0x04001FFF RID: 8191
		EVEX_Vpsubsb_ymm_k1z_ymm_ymmm256,
		// Token: 0x04002000 RID: 8192
		EVEX_Vpsubsb_zmm_k1z_zmm_zmmm512,
		// Token: 0x04002001 RID: 8193
		Psubsw_mm_mmm64,
		// Token: 0x04002002 RID: 8194
		Psubsw_xmm_xmmm128,
		// Token: 0x04002003 RID: 8195
		VEX_Vpsubsw_xmm_xmm_xmmm128,
		// Token: 0x04002004 RID: 8196
		VEX_Vpsubsw_ymm_ymm_ymmm256,
		// Token: 0x04002005 RID: 8197
		EVEX_Vpsubsw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04002006 RID: 8198
		EVEX_Vpsubsw_ymm_k1z_ymm_ymmm256,
		// Token: 0x04002007 RID: 8199
		EVEX_Vpsubsw_zmm_k1z_zmm_zmmm512,
		// Token: 0x04002008 RID: 8200
		Pminsw_mm_mmm64,
		// Token: 0x04002009 RID: 8201
		Pminsw_xmm_xmmm128,
		// Token: 0x0400200A RID: 8202
		VEX_Vpminsw_xmm_xmm_xmmm128,
		// Token: 0x0400200B RID: 8203
		VEX_Vpminsw_ymm_ymm_ymmm256,
		// Token: 0x0400200C RID: 8204
		EVEX_Vpminsw_xmm_k1z_xmm_xmmm128,
		// Token: 0x0400200D RID: 8205
		EVEX_Vpminsw_ymm_k1z_ymm_ymmm256,
		// Token: 0x0400200E RID: 8206
		EVEX_Vpminsw_zmm_k1z_zmm_zmmm512,
		// Token: 0x0400200F RID: 8207
		Por_mm_mmm64,
		// Token: 0x04002010 RID: 8208
		Por_xmm_xmmm128,
		// Token: 0x04002011 RID: 8209
		VEX_Vpor_xmm_xmm_xmmm128,
		// Token: 0x04002012 RID: 8210
		VEX_Vpor_ymm_ymm_ymmm256,
		// Token: 0x04002013 RID: 8211
		EVEX_Vpord_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002014 RID: 8212
		EVEX_Vpord_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002015 RID: 8213
		EVEX_Vpord_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002016 RID: 8214
		EVEX_Vporq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002017 RID: 8215
		EVEX_Vporq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002018 RID: 8216
		EVEX_Vporq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002019 RID: 8217
		Paddsb_mm_mmm64,
		// Token: 0x0400201A RID: 8218
		Paddsb_xmm_xmmm128,
		// Token: 0x0400201B RID: 8219
		VEX_Vpaddsb_xmm_xmm_xmmm128,
		// Token: 0x0400201C RID: 8220
		VEX_Vpaddsb_ymm_ymm_ymmm256,
		// Token: 0x0400201D RID: 8221
		EVEX_Vpaddsb_xmm_k1z_xmm_xmmm128,
		// Token: 0x0400201E RID: 8222
		EVEX_Vpaddsb_ymm_k1z_ymm_ymmm256,
		// Token: 0x0400201F RID: 8223
		EVEX_Vpaddsb_zmm_k1z_zmm_zmmm512,
		// Token: 0x04002020 RID: 8224
		Paddsw_mm_mmm64,
		// Token: 0x04002021 RID: 8225
		Paddsw_xmm_xmmm128,
		// Token: 0x04002022 RID: 8226
		VEX_Vpaddsw_xmm_xmm_xmmm128,
		// Token: 0x04002023 RID: 8227
		VEX_Vpaddsw_ymm_ymm_ymmm256,
		// Token: 0x04002024 RID: 8228
		EVEX_Vpaddsw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04002025 RID: 8229
		EVEX_Vpaddsw_ymm_k1z_ymm_ymmm256,
		// Token: 0x04002026 RID: 8230
		EVEX_Vpaddsw_zmm_k1z_zmm_zmmm512,
		// Token: 0x04002027 RID: 8231
		Pmaxsw_mm_mmm64,
		// Token: 0x04002028 RID: 8232
		Pmaxsw_xmm_xmmm128,
		// Token: 0x04002029 RID: 8233
		VEX_Vpmaxsw_xmm_xmm_xmmm128,
		// Token: 0x0400202A RID: 8234
		VEX_Vpmaxsw_ymm_ymm_ymmm256,
		// Token: 0x0400202B RID: 8235
		EVEX_Vpmaxsw_xmm_k1z_xmm_xmmm128,
		// Token: 0x0400202C RID: 8236
		EVEX_Vpmaxsw_ymm_k1z_ymm_ymmm256,
		// Token: 0x0400202D RID: 8237
		EVEX_Vpmaxsw_zmm_k1z_zmm_zmmm512,
		// Token: 0x0400202E RID: 8238
		Pxor_mm_mmm64,
		// Token: 0x0400202F RID: 8239
		Pxor_xmm_xmmm128,
		// Token: 0x04002030 RID: 8240
		VEX_Vpxor_xmm_xmm_xmmm128,
		// Token: 0x04002031 RID: 8241
		VEX_Vpxor_ymm_ymm_ymmm256,
		// Token: 0x04002032 RID: 8242
		EVEX_Vpxord_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002033 RID: 8243
		EVEX_Vpxord_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002034 RID: 8244
		EVEX_Vpxord_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002035 RID: 8245
		EVEX_Vpxorq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002036 RID: 8246
		EVEX_Vpxorq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002037 RID: 8247
		EVEX_Vpxorq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002038 RID: 8248
		Lddqu_xmm_m128,
		// Token: 0x04002039 RID: 8249
		VEX_Vlddqu_xmm_m128,
		// Token: 0x0400203A RID: 8250
		VEX_Vlddqu_ymm_m256,
		// Token: 0x0400203B RID: 8251
		Psllw_mm_mmm64,
		// Token: 0x0400203C RID: 8252
		Psllw_xmm_xmmm128,
		// Token: 0x0400203D RID: 8253
		VEX_Vpsllw_xmm_xmm_xmmm128,
		// Token: 0x0400203E RID: 8254
		VEX_Vpsllw_ymm_ymm_xmmm128,
		// Token: 0x0400203F RID: 8255
		EVEX_Vpsllw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04002040 RID: 8256
		EVEX_Vpsllw_ymm_k1z_ymm_xmmm128,
		// Token: 0x04002041 RID: 8257
		EVEX_Vpsllw_zmm_k1z_zmm_xmmm128,
		// Token: 0x04002042 RID: 8258
		Pslld_mm_mmm64,
		// Token: 0x04002043 RID: 8259
		Pslld_xmm_xmmm128,
		// Token: 0x04002044 RID: 8260
		VEX_Vpslld_xmm_xmm_xmmm128,
		// Token: 0x04002045 RID: 8261
		VEX_Vpslld_ymm_ymm_xmmm128,
		// Token: 0x04002046 RID: 8262
		EVEX_Vpslld_xmm_k1z_xmm_xmmm128,
		// Token: 0x04002047 RID: 8263
		EVEX_Vpslld_ymm_k1z_ymm_xmmm128,
		// Token: 0x04002048 RID: 8264
		EVEX_Vpslld_zmm_k1z_zmm_xmmm128,
		// Token: 0x04002049 RID: 8265
		Psllq_mm_mmm64,
		// Token: 0x0400204A RID: 8266
		Psllq_xmm_xmmm128,
		// Token: 0x0400204B RID: 8267
		VEX_Vpsllq_xmm_xmm_xmmm128,
		// Token: 0x0400204C RID: 8268
		VEX_Vpsllq_ymm_ymm_xmmm128,
		// Token: 0x0400204D RID: 8269
		EVEX_Vpsllq_xmm_k1z_xmm_xmmm128,
		// Token: 0x0400204E RID: 8270
		EVEX_Vpsllq_ymm_k1z_ymm_xmmm128,
		// Token: 0x0400204F RID: 8271
		EVEX_Vpsllq_zmm_k1z_zmm_xmmm128,
		// Token: 0x04002050 RID: 8272
		Pmuludq_mm_mmm64,
		// Token: 0x04002051 RID: 8273
		Pmuludq_xmm_xmmm128,
		// Token: 0x04002052 RID: 8274
		VEX_Vpmuludq_xmm_xmm_xmmm128,
		// Token: 0x04002053 RID: 8275
		VEX_Vpmuludq_ymm_ymm_ymmm256,
		// Token: 0x04002054 RID: 8276
		EVEX_Vpmuludq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002055 RID: 8277
		EVEX_Vpmuludq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002056 RID: 8278
		EVEX_Vpmuludq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002057 RID: 8279
		Pmaddwd_mm_mmm64,
		// Token: 0x04002058 RID: 8280
		Pmaddwd_xmm_xmmm128,
		// Token: 0x04002059 RID: 8281
		VEX_Vpmaddwd_xmm_xmm_xmmm128,
		// Token: 0x0400205A RID: 8282
		VEX_Vpmaddwd_ymm_ymm_ymmm256,
		// Token: 0x0400205B RID: 8283
		EVEX_Vpmaddwd_xmm_k1z_xmm_xmmm128,
		// Token: 0x0400205C RID: 8284
		EVEX_Vpmaddwd_ymm_k1z_ymm_ymmm256,
		// Token: 0x0400205D RID: 8285
		EVEX_Vpmaddwd_zmm_k1z_zmm_zmmm512,
		// Token: 0x0400205E RID: 8286
		Psadbw_mm_mmm64,
		// Token: 0x0400205F RID: 8287
		Psadbw_xmm_xmmm128,
		// Token: 0x04002060 RID: 8288
		VEX_Vpsadbw_xmm_xmm_xmmm128,
		// Token: 0x04002061 RID: 8289
		VEX_Vpsadbw_ymm_ymm_ymmm256,
		// Token: 0x04002062 RID: 8290
		EVEX_Vpsadbw_xmm_xmm_xmmm128,
		// Token: 0x04002063 RID: 8291
		EVEX_Vpsadbw_ymm_ymm_ymmm256,
		// Token: 0x04002064 RID: 8292
		EVEX_Vpsadbw_zmm_zmm_zmmm512,
		// Token: 0x04002065 RID: 8293
		Maskmovq_rDI_mm_mm,
		// Token: 0x04002066 RID: 8294
		Maskmovdqu_rDI_xmm_xmm,
		// Token: 0x04002067 RID: 8295
		VEX_Vmaskmovdqu_rDI_xmm_xmm,
		// Token: 0x04002068 RID: 8296
		Psubb_mm_mmm64,
		// Token: 0x04002069 RID: 8297
		Psubb_xmm_xmmm128,
		// Token: 0x0400206A RID: 8298
		VEX_Vpsubb_xmm_xmm_xmmm128,
		// Token: 0x0400206B RID: 8299
		VEX_Vpsubb_ymm_ymm_ymmm256,
		// Token: 0x0400206C RID: 8300
		EVEX_Vpsubb_xmm_k1z_xmm_xmmm128,
		// Token: 0x0400206D RID: 8301
		EVEX_Vpsubb_ymm_k1z_ymm_ymmm256,
		// Token: 0x0400206E RID: 8302
		EVEX_Vpsubb_zmm_k1z_zmm_zmmm512,
		// Token: 0x0400206F RID: 8303
		Psubw_mm_mmm64,
		// Token: 0x04002070 RID: 8304
		Psubw_xmm_xmmm128,
		// Token: 0x04002071 RID: 8305
		VEX_Vpsubw_xmm_xmm_xmmm128,
		// Token: 0x04002072 RID: 8306
		VEX_Vpsubw_ymm_ymm_ymmm256,
		// Token: 0x04002073 RID: 8307
		EVEX_Vpsubw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04002074 RID: 8308
		EVEX_Vpsubw_ymm_k1z_ymm_ymmm256,
		// Token: 0x04002075 RID: 8309
		EVEX_Vpsubw_zmm_k1z_zmm_zmmm512,
		// Token: 0x04002076 RID: 8310
		Psubd_mm_mmm64,
		// Token: 0x04002077 RID: 8311
		Psubd_xmm_xmmm128,
		// Token: 0x04002078 RID: 8312
		VEX_Vpsubd_xmm_xmm_xmmm128,
		// Token: 0x04002079 RID: 8313
		VEX_Vpsubd_ymm_ymm_ymmm256,
		// Token: 0x0400207A RID: 8314
		EVEX_Vpsubd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x0400207B RID: 8315
		EVEX_Vpsubd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x0400207C RID: 8316
		EVEX_Vpsubd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x0400207D RID: 8317
		Psubq_mm_mmm64,
		// Token: 0x0400207E RID: 8318
		Psubq_xmm_xmmm128,
		// Token: 0x0400207F RID: 8319
		VEX_Vpsubq_xmm_xmm_xmmm128,
		// Token: 0x04002080 RID: 8320
		VEX_Vpsubq_ymm_ymm_ymmm256,
		// Token: 0x04002081 RID: 8321
		EVEX_Vpsubq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002082 RID: 8322
		EVEX_Vpsubq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002083 RID: 8323
		EVEX_Vpsubq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002084 RID: 8324
		Paddb_mm_mmm64,
		// Token: 0x04002085 RID: 8325
		Paddb_xmm_xmmm128,
		// Token: 0x04002086 RID: 8326
		VEX_Vpaddb_xmm_xmm_xmmm128,
		// Token: 0x04002087 RID: 8327
		VEX_Vpaddb_ymm_ymm_ymmm256,
		// Token: 0x04002088 RID: 8328
		EVEX_Vpaddb_xmm_k1z_xmm_xmmm128,
		// Token: 0x04002089 RID: 8329
		EVEX_Vpaddb_ymm_k1z_ymm_ymmm256,
		// Token: 0x0400208A RID: 8330
		EVEX_Vpaddb_zmm_k1z_zmm_zmmm512,
		// Token: 0x0400208B RID: 8331
		Paddw_mm_mmm64,
		// Token: 0x0400208C RID: 8332
		Paddw_xmm_xmmm128,
		// Token: 0x0400208D RID: 8333
		VEX_Vpaddw_xmm_xmm_xmmm128,
		// Token: 0x0400208E RID: 8334
		VEX_Vpaddw_ymm_ymm_ymmm256,
		// Token: 0x0400208F RID: 8335
		EVEX_Vpaddw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04002090 RID: 8336
		EVEX_Vpaddw_ymm_k1z_ymm_ymmm256,
		// Token: 0x04002091 RID: 8337
		EVEX_Vpaddw_zmm_k1z_zmm_zmmm512,
		// Token: 0x04002092 RID: 8338
		Paddd_mm_mmm64,
		// Token: 0x04002093 RID: 8339
		Paddd_xmm_xmmm128,
		// Token: 0x04002094 RID: 8340
		VEX_Vpaddd_xmm_xmm_xmmm128,
		// Token: 0x04002095 RID: 8341
		VEX_Vpaddd_ymm_ymm_ymmm256,
		// Token: 0x04002096 RID: 8342
		EVEX_Vpaddd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002097 RID: 8343
		EVEX_Vpaddd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002098 RID: 8344
		EVEX_Vpaddd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002099 RID: 8345
		Ud0_r16_rm16,
		// Token: 0x0400209A RID: 8346
		Ud0_r32_rm32,
		// Token: 0x0400209B RID: 8347
		Ud0_r64_rm64,
		// Token: 0x0400209C RID: 8348
		Pshufb_mm_mmm64,
		// Token: 0x0400209D RID: 8349
		Pshufb_xmm_xmmm128,
		// Token: 0x0400209E RID: 8350
		VEX_Vpshufb_xmm_xmm_xmmm128,
		// Token: 0x0400209F RID: 8351
		VEX_Vpshufb_ymm_ymm_ymmm256,
		// Token: 0x040020A0 RID: 8352
		EVEX_Vpshufb_xmm_k1z_xmm_xmmm128,
		// Token: 0x040020A1 RID: 8353
		EVEX_Vpshufb_ymm_k1z_ymm_ymmm256,
		// Token: 0x040020A2 RID: 8354
		EVEX_Vpshufb_zmm_k1z_zmm_zmmm512,
		// Token: 0x040020A3 RID: 8355
		Phaddw_mm_mmm64,
		// Token: 0x040020A4 RID: 8356
		Phaddw_xmm_xmmm128,
		// Token: 0x040020A5 RID: 8357
		VEX_Vphaddw_xmm_xmm_xmmm128,
		// Token: 0x040020A6 RID: 8358
		VEX_Vphaddw_ymm_ymm_ymmm256,
		// Token: 0x040020A7 RID: 8359
		Phaddd_mm_mmm64,
		// Token: 0x040020A8 RID: 8360
		Phaddd_xmm_xmmm128,
		// Token: 0x040020A9 RID: 8361
		VEX_Vphaddd_xmm_xmm_xmmm128,
		// Token: 0x040020AA RID: 8362
		VEX_Vphaddd_ymm_ymm_ymmm256,
		// Token: 0x040020AB RID: 8363
		Phaddsw_mm_mmm64,
		// Token: 0x040020AC RID: 8364
		Phaddsw_xmm_xmmm128,
		// Token: 0x040020AD RID: 8365
		VEX_Vphaddsw_xmm_xmm_xmmm128,
		// Token: 0x040020AE RID: 8366
		VEX_Vphaddsw_ymm_ymm_ymmm256,
		// Token: 0x040020AF RID: 8367
		Pmaddubsw_mm_mmm64,
		// Token: 0x040020B0 RID: 8368
		Pmaddubsw_xmm_xmmm128,
		// Token: 0x040020B1 RID: 8369
		VEX_Vpmaddubsw_xmm_xmm_xmmm128,
		// Token: 0x040020B2 RID: 8370
		VEX_Vpmaddubsw_ymm_ymm_ymmm256,
		// Token: 0x040020B3 RID: 8371
		EVEX_Vpmaddubsw_xmm_k1z_xmm_xmmm128,
		// Token: 0x040020B4 RID: 8372
		EVEX_Vpmaddubsw_ymm_k1z_ymm_ymmm256,
		// Token: 0x040020B5 RID: 8373
		EVEX_Vpmaddubsw_zmm_k1z_zmm_zmmm512,
		// Token: 0x040020B6 RID: 8374
		Phsubw_mm_mmm64,
		// Token: 0x040020B7 RID: 8375
		Phsubw_xmm_xmmm128,
		// Token: 0x040020B8 RID: 8376
		VEX_Vphsubw_xmm_xmm_xmmm128,
		// Token: 0x040020B9 RID: 8377
		VEX_Vphsubw_ymm_ymm_ymmm256,
		// Token: 0x040020BA RID: 8378
		Phsubd_mm_mmm64,
		// Token: 0x040020BB RID: 8379
		Phsubd_xmm_xmmm128,
		// Token: 0x040020BC RID: 8380
		VEX_Vphsubd_xmm_xmm_xmmm128,
		// Token: 0x040020BD RID: 8381
		VEX_Vphsubd_ymm_ymm_ymmm256,
		// Token: 0x040020BE RID: 8382
		Phsubsw_mm_mmm64,
		// Token: 0x040020BF RID: 8383
		Phsubsw_xmm_xmmm128,
		// Token: 0x040020C0 RID: 8384
		VEX_Vphsubsw_xmm_xmm_xmmm128,
		// Token: 0x040020C1 RID: 8385
		VEX_Vphsubsw_ymm_ymm_ymmm256,
		// Token: 0x040020C2 RID: 8386
		Psignb_mm_mmm64,
		// Token: 0x040020C3 RID: 8387
		Psignb_xmm_xmmm128,
		// Token: 0x040020C4 RID: 8388
		VEX_Vpsignb_xmm_xmm_xmmm128,
		// Token: 0x040020C5 RID: 8389
		VEX_Vpsignb_ymm_ymm_ymmm256,
		// Token: 0x040020C6 RID: 8390
		Psignw_mm_mmm64,
		// Token: 0x040020C7 RID: 8391
		Psignw_xmm_xmmm128,
		// Token: 0x040020C8 RID: 8392
		VEX_Vpsignw_xmm_xmm_xmmm128,
		// Token: 0x040020C9 RID: 8393
		VEX_Vpsignw_ymm_ymm_ymmm256,
		// Token: 0x040020CA RID: 8394
		Psignd_mm_mmm64,
		// Token: 0x040020CB RID: 8395
		Psignd_xmm_xmmm128,
		// Token: 0x040020CC RID: 8396
		VEX_Vpsignd_xmm_xmm_xmmm128,
		// Token: 0x040020CD RID: 8397
		VEX_Vpsignd_ymm_ymm_ymmm256,
		// Token: 0x040020CE RID: 8398
		Pmulhrsw_mm_mmm64,
		// Token: 0x040020CF RID: 8399
		Pmulhrsw_xmm_xmmm128,
		// Token: 0x040020D0 RID: 8400
		VEX_Vpmulhrsw_xmm_xmm_xmmm128,
		// Token: 0x040020D1 RID: 8401
		VEX_Vpmulhrsw_ymm_ymm_ymmm256,
		// Token: 0x040020D2 RID: 8402
		EVEX_Vpmulhrsw_xmm_k1z_xmm_xmmm128,
		// Token: 0x040020D3 RID: 8403
		EVEX_Vpmulhrsw_ymm_k1z_ymm_ymmm256,
		// Token: 0x040020D4 RID: 8404
		EVEX_Vpmulhrsw_zmm_k1z_zmm_zmmm512,
		// Token: 0x040020D5 RID: 8405
		VEX_Vpermilps_xmm_xmm_xmmm128,
		// Token: 0x040020D6 RID: 8406
		VEX_Vpermilps_ymm_ymm_ymmm256,
		// Token: 0x040020D7 RID: 8407
		EVEX_Vpermilps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040020D8 RID: 8408
		EVEX_Vpermilps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040020D9 RID: 8409
		EVEX_Vpermilps_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x040020DA RID: 8410
		VEX_Vpermilpd_xmm_xmm_xmmm128,
		// Token: 0x040020DB RID: 8411
		VEX_Vpermilpd_ymm_ymm_ymmm256,
		// Token: 0x040020DC RID: 8412
		EVEX_Vpermilpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x040020DD RID: 8413
		EVEX_Vpermilpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x040020DE RID: 8414
		EVEX_Vpermilpd_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x040020DF RID: 8415
		VEX_Vtestps_xmm_xmmm128,
		// Token: 0x040020E0 RID: 8416
		VEX_Vtestps_ymm_ymmm256,
		// Token: 0x040020E1 RID: 8417
		VEX_Vtestpd_xmm_xmmm128,
		// Token: 0x040020E2 RID: 8418
		VEX_Vtestpd_ymm_ymmm256,
		// Token: 0x040020E3 RID: 8419
		Pblendvb_xmm_xmmm128,
		// Token: 0x040020E4 RID: 8420
		EVEX_Vpsrlvw_xmm_k1z_xmm_xmmm128,
		// Token: 0x040020E5 RID: 8421
		EVEX_Vpsrlvw_ymm_k1z_ymm_ymmm256,
		// Token: 0x040020E6 RID: 8422
		EVEX_Vpsrlvw_zmm_k1z_zmm_zmmm512,
		// Token: 0x040020E7 RID: 8423
		EVEX_Vpmovuswb_xmmm64_k1z_xmm,
		// Token: 0x040020E8 RID: 8424
		EVEX_Vpmovuswb_xmmm128_k1z_ymm,
		// Token: 0x040020E9 RID: 8425
		EVEX_Vpmovuswb_ymmm256_k1z_zmm,
		// Token: 0x040020EA RID: 8426
		EVEX_Vpsravw_xmm_k1z_xmm_xmmm128,
		// Token: 0x040020EB RID: 8427
		EVEX_Vpsravw_ymm_k1z_ymm_ymmm256,
		// Token: 0x040020EC RID: 8428
		EVEX_Vpsravw_zmm_k1z_zmm_zmmm512,
		// Token: 0x040020ED RID: 8429
		EVEX_Vpmovusdb_xmmm32_k1z_xmm,
		// Token: 0x040020EE RID: 8430
		EVEX_Vpmovusdb_xmmm64_k1z_ymm,
		// Token: 0x040020EF RID: 8431
		EVEX_Vpmovusdb_xmmm128_k1z_zmm,
		// Token: 0x040020F0 RID: 8432
		EVEX_Vpsllvw_xmm_k1z_xmm_xmmm128,
		// Token: 0x040020F1 RID: 8433
		EVEX_Vpsllvw_ymm_k1z_ymm_ymmm256,
		// Token: 0x040020F2 RID: 8434
		EVEX_Vpsllvw_zmm_k1z_zmm_zmmm512,
		// Token: 0x040020F3 RID: 8435
		EVEX_Vpmovusqb_xmmm16_k1z_xmm,
		// Token: 0x040020F4 RID: 8436
		EVEX_Vpmovusqb_xmmm32_k1z_ymm,
		// Token: 0x040020F5 RID: 8437
		EVEX_Vpmovusqb_xmmm64_k1z_zmm,
		// Token: 0x040020F6 RID: 8438
		VEX_Vcvtph2ps_xmm_xmmm64,
		// Token: 0x040020F7 RID: 8439
		VEX_Vcvtph2ps_ymm_xmmm128,
		// Token: 0x040020F8 RID: 8440
		EVEX_Vcvtph2ps_xmm_k1z_xmmm64,
		// Token: 0x040020F9 RID: 8441
		EVEX_Vcvtph2ps_ymm_k1z_xmmm128,
		// Token: 0x040020FA RID: 8442
		EVEX_Vcvtph2ps_zmm_k1z_ymmm256_sae,
		// Token: 0x040020FB RID: 8443
		EVEX_Vpmovusdw_xmmm64_k1z_xmm,
		// Token: 0x040020FC RID: 8444
		EVEX_Vpmovusdw_xmmm128_k1z_ymm,
		// Token: 0x040020FD RID: 8445
		EVEX_Vpmovusdw_ymmm256_k1z_zmm,
		// Token: 0x040020FE RID: 8446
		Blendvps_xmm_xmmm128,
		// Token: 0x040020FF RID: 8447
		EVEX_Vprorvd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002100 RID: 8448
		EVEX_Vprorvd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002101 RID: 8449
		EVEX_Vprorvd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002102 RID: 8450
		EVEX_Vprorvq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002103 RID: 8451
		EVEX_Vprorvq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002104 RID: 8452
		EVEX_Vprorvq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002105 RID: 8453
		EVEX_Vpmovusqw_xmmm32_k1z_xmm,
		// Token: 0x04002106 RID: 8454
		EVEX_Vpmovusqw_xmmm64_k1z_ymm,
		// Token: 0x04002107 RID: 8455
		EVEX_Vpmovusqw_xmmm128_k1z_zmm,
		// Token: 0x04002108 RID: 8456
		Blendvpd_xmm_xmmm128,
		// Token: 0x04002109 RID: 8457
		EVEX_Vprolvd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x0400210A RID: 8458
		EVEX_Vprolvd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x0400210B RID: 8459
		EVEX_Vprolvd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x0400210C RID: 8460
		EVEX_Vprolvq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x0400210D RID: 8461
		EVEX_Vprolvq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x0400210E RID: 8462
		EVEX_Vprolvq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x0400210F RID: 8463
		EVEX_Vpmovusqd_xmmm64_k1z_xmm,
		// Token: 0x04002110 RID: 8464
		EVEX_Vpmovusqd_xmmm128_k1z_ymm,
		// Token: 0x04002111 RID: 8465
		EVEX_Vpmovusqd_ymmm256_k1z_zmm,
		// Token: 0x04002112 RID: 8466
		VEX_Vpermps_ymm_ymm_ymmm256,
		// Token: 0x04002113 RID: 8467
		EVEX_Vpermps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002114 RID: 8468
		EVEX_Vpermps_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002115 RID: 8469
		EVEX_Vpermpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002116 RID: 8470
		EVEX_Vpermpd_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002117 RID: 8471
		Ptest_xmm_xmmm128,
		// Token: 0x04002118 RID: 8472
		VEX_Vptest_xmm_xmmm128,
		// Token: 0x04002119 RID: 8473
		VEX_Vptest_ymm_ymmm256,
		// Token: 0x0400211A RID: 8474
		VEX_Vbroadcastss_xmm_m32,
		// Token: 0x0400211B RID: 8475
		VEX_Vbroadcastss_ymm_m32,
		// Token: 0x0400211C RID: 8476
		EVEX_Vbroadcastss_xmm_k1z_xmmm32,
		// Token: 0x0400211D RID: 8477
		EVEX_Vbroadcastss_ymm_k1z_xmmm32,
		// Token: 0x0400211E RID: 8478
		EVEX_Vbroadcastss_zmm_k1z_xmmm32,
		// Token: 0x0400211F RID: 8479
		VEX_Vbroadcastsd_ymm_m64,
		// Token: 0x04002120 RID: 8480
		EVEX_Vbroadcastf32x2_ymm_k1z_xmmm64,
		// Token: 0x04002121 RID: 8481
		EVEX_Vbroadcastf32x2_zmm_k1z_xmmm64,
		// Token: 0x04002122 RID: 8482
		EVEX_Vbroadcastsd_ymm_k1z_xmmm64,
		// Token: 0x04002123 RID: 8483
		EVEX_Vbroadcastsd_zmm_k1z_xmmm64,
		// Token: 0x04002124 RID: 8484
		VEX_Vbroadcastf128_ymm_m128,
		// Token: 0x04002125 RID: 8485
		EVEX_Vbroadcastf32x4_ymm_k1z_m128,
		// Token: 0x04002126 RID: 8486
		EVEX_Vbroadcastf32x4_zmm_k1z_m128,
		// Token: 0x04002127 RID: 8487
		EVEX_Vbroadcastf64x2_ymm_k1z_m128,
		// Token: 0x04002128 RID: 8488
		EVEX_Vbroadcastf64x2_zmm_k1z_m128,
		// Token: 0x04002129 RID: 8489
		EVEX_Vbroadcastf32x8_zmm_k1z_m256,
		// Token: 0x0400212A RID: 8490
		EVEX_Vbroadcastf64x4_zmm_k1z_m256,
		// Token: 0x0400212B RID: 8491
		Pabsb_mm_mmm64,
		// Token: 0x0400212C RID: 8492
		Pabsb_xmm_xmmm128,
		// Token: 0x0400212D RID: 8493
		VEX_Vpabsb_xmm_xmmm128,
		// Token: 0x0400212E RID: 8494
		VEX_Vpabsb_ymm_ymmm256,
		// Token: 0x0400212F RID: 8495
		EVEX_Vpabsb_xmm_k1z_xmmm128,
		// Token: 0x04002130 RID: 8496
		EVEX_Vpabsb_ymm_k1z_ymmm256,
		// Token: 0x04002131 RID: 8497
		EVEX_Vpabsb_zmm_k1z_zmmm512,
		// Token: 0x04002132 RID: 8498
		Pabsw_mm_mmm64,
		// Token: 0x04002133 RID: 8499
		Pabsw_xmm_xmmm128,
		// Token: 0x04002134 RID: 8500
		VEX_Vpabsw_xmm_xmmm128,
		// Token: 0x04002135 RID: 8501
		VEX_Vpabsw_ymm_ymmm256,
		// Token: 0x04002136 RID: 8502
		EVEX_Vpabsw_xmm_k1z_xmmm128,
		// Token: 0x04002137 RID: 8503
		EVEX_Vpabsw_ymm_k1z_ymmm256,
		// Token: 0x04002138 RID: 8504
		EVEX_Vpabsw_zmm_k1z_zmmm512,
		// Token: 0x04002139 RID: 8505
		Pabsd_mm_mmm64,
		// Token: 0x0400213A RID: 8506
		Pabsd_xmm_xmmm128,
		// Token: 0x0400213B RID: 8507
		VEX_Vpabsd_xmm_xmmm128,
		// Token: 0x0400213C RID: 8508
		VEX_Vpabsd_ymm_ymmm256,
		// Token: 0x0400213D RID: 8509
		EVEX_Vpabsd_xmm_k1z_xmmm128b32,
		// Token: 0x0400213E RID: 8510
		EVEX_Vpabsd_ymm_k1z_ymmm256b32,
		// Token: 0x0400213F RID: 8511
		EVEX_Vpabsd_zmm_k1z_zmmm512b32,
		// Token: 0x04002140 RID: 8512
		EVEX_Vpabsq_xmm_k1z_xmmm128b64,
		// Token: 0x04002141 RID: 8513
		EVEX_Vpabsq_ymm_k1z_ymmm256b64,
		// Token: 0x04002142 RID: 8514
		EVEX_Vpabsq_zmm_k1z_zmmm512b64,
		// Token: 0x04002143 RID: 8515
		Pmovsxbw_xmm_xmmm64,
		// Token: 0x04002144 RID: 8516
		VEX_Vpmovsxbw_xmm_xmmm64,
		// Token: 0x04002145 RID: 8517
		VEX_Vpmovsxbw_ymm_xmmm128,
		// Token: 0x04002146 RID: 8518
		EVEX_Vpmovsxbw_xmm_k1z_xmmm64,
		// Token: 0x04002147 RID: 8519
		EVEX_Vpmovsxbw_ymm_k1z_xmmm128,
		// Token: 0x04002148 RID: 8520
		EVEX_Vpmovsxbw_zmm_k1z_ymmm256,
		// Token: 0x04002149 RID: 8521
		EVEX_Vpmovswb_xmmm64_k1z_xmm,
		// Token: 0x0400214A RID: 8522
		EVEX_Vpmovswb_xmmm128_k1z_ymm,
		// Token: 0x0400214B RID: 8523
		EVEX_Vpmovswb_ymmm256_k1z_zmm,
		// Token: 0x0400214C RID: 8524
		Pmovsxbd_xmm_xmmm32,
		// Token: 0x0400214D RID: 8525
		VEX_Vpmovsxbd_xmm_xmmm32,
		// Token: 0x0400214E RID: 8526
		VEX_Vpmovsxbd_ymm_xmmm64,
		// Token: 0x0400214F RID: 8527
		EVEX_Vpmovsxbd_xmm_k1z_xmmm32,
		// Token: 0x04002150 RID: 8528
		EVEX_Vpmovsxbd_ymm_k1z_xmmm64,
		// Token: 0x04002151 RID: 8529
		EVEX_Vpmovsxbd_zmm_k1z_xmmm128,
		// Token: 0x04002152 RID: 8530
		EVEX_Vpmovsdb_xmmm32_k1z_xmm,
		// Token: 0x04002153 RID: 8531
		EVEX_Vpmovsdb_xmmm64_k1z_ymm,
		// Token: 0x04002154 RID: 8532
		EVEX_Vpmovsdb_xmmm128_k1z_zmm,
		// Token: 0x04002155 RID: 8533
		Pmovsxbq_xmm_xmmm16,
		// Token: 0x04002156 RID: 8534
		VEX_Vpmovsxbq_xmm_xmmm16,
		// Token: 0x04002157 RID: 8535
		VEX_Vpmovsxbq_ymm_xmmm32,
		// Token: 0x04002158 RID: 8536
		EVEX_Vpmovsxbq_xmm_k1z_xmmm16,
		// Token: 0x04002159 RID: 8537
		EVEX_Vpmovsxbq_ymm_k1z_xmmm32,
		// Token: 0x0400215A RID: 8538
		EVEX_Vpmovsxbq_zmm_k1z_xmmm64,
		// Token: 0x0400215B RID: 8539
		EVEX_Vpmovsqb_xmmm16_k1z_xmm,
		// Token: 0x0400215C RID: 8540
		EVEX_Vpmovsqb_xmmm32_k1z_ymm,
		// Token: 0x0400215D RID: 8541
		EVEX_Vpmovsqb_xmmm64_k1z_zmm,
		// Token: 0x0400215E RID: 8542
		Pmovsxwd_xmm_xmmm64,
		// Token: 0x0400215F RID: 8543
		VEX_Vpmovsxwd_xmm_xmmm64,
		// Token: 0x04002160 RID: 8544
		VEX_Vpmovsxwd_ymm_xmmm128,
		// Token: 0x04002161 RID: 8545
		EVEX_Vpmovsxwd_xmm_k1z_xmmm64,
		// Token: 0x04002162 RID: 8546
		EVEX_Vpmovsxwd_ymm_k1z_xmmm128,
		// Token: 0x04002163 RID: 8547
		EVEX_Vpmovsxwd_zmm_k1z_ymmm256,
		// Token: 0x04002164 RID: 8548
		EVEX_Vpmovsdw_xmmm64_k1z_xmm,
		// Token: 0x04002165 RID: 8549
		EVEX_Vpmovsdw_xmmm128_k1z_ymm,
		// Token: 0x04002166 RID: 8550
		EVEX_Vpmovsdw_ymmm256_k1z_zmm,
		// Token: 0x04002167 RID: 8551
		Pmovsxwq_xmm_xmmm32,
		// Token: 0x04002168 RID: 8552
		VEX_Vpmovsxwq_xmm_xmmm32,
		// Token: 0x04002169 RID: 8553
		VEX_Vpmovsxwq_ymm_xmmm64,
		// Token: 0x0400216A RID: 8554
		EVEX_Vpmovsxwq_xmm_k1z_xmmm32,
		// Token: 0x0400216B RID: 8555
		EVEX_Vpmovsxwq_ymm_k1z_xmmm64,
		// Token: 0x0400216C RID: 8556
		EVEX_Vpmovsxwq_zmm_k1z_xmmm128,
		// Token: 0x0400216D RID: 8557
		EVEX_Vpmovsqw_xmmm32_k1z_xmm,
		// Token: 0x0400216E RID: 8558
		EVEX_Vpmovsqw_xmmm64_k1z_ymm,
		// Token: 0x0400216F RID: 8559
		EVEX_Vpmovsqw_xmmm128_k1z_zmm,
		// Token: 0x04002170 RID: 8560
		Pmovsxdq_xmm_xmmm64,
		// Token: 0x04002171 RID: 8561
		VEX_Vpmovsxdq_xmm_xmmm64,
		// Token: 0x04002172 RID: 8562
		VEX_Vpmovsxdq_ymm_xmmm128,
		// Token: 0x04002173 RID: 8563
		EVEX_Vpmovsxdq_xmm_k1z_xmmm64,
		// Token: 0x04002174 RID: 8564
		EVEX_Vpmovsxdq_ymm_k1z_xmmm128,
		// Token: 0x04002175 RID: 8565
		EVEX_Vpmovsxdq_zmm_k1z_ymmm256,
		// Token: 0x04002176 RID: 8566
		EVEX_Vpmovsqd_xmmm64_k1z_xmm,
		// Token: 0x04002177 RID: 8567
		EVEX_Vpmovsqd_xmmm128_k1z_ymm,
		// Token: 0x04002178 RID: 8568
		EVEX_Vpmovsqd_ymmm256_k1z_zmm,
		// Token: 0x04002179 RID: 8569
		EVEX_Vptestmb_kr_k1_xmm_xmmm128,
		// Token: 0x0400217A RID: 8570
		EVEX_Vptestmb_kr_k1_ymm_ymmm256,
		// Token: 0x0400217B RID: 8571
		EVEX_Vptestmb_kr_k1_zmm_zmmm512,
		// Token: 0x0400217C RID: 8572
		EVEX_Vptestmw_kr_k1_xmm_xmmm128,
		// Token: 0x0400217D RID: 8573
		EVEX_Vptestmw_kr_k1_ymm_ymmm256,
		// Token: 0x0400217E RID: 8574
		EVEX_Vptestmw_kr_k1_zmm_zmmm512,
		// Token: 0x0400217F RID: 8575
		EVEX_Vptestnmb_kr_k1_xmm_xmmm128,
		// Token: 0x04002180 RID: 8576
		EVEX_Vptestnmb_kr_k1_ymm_ymmm256,
		// Token: 0x04002181 RID: 8577
		EVEX_Vptestnmb_kr_k1_zmm_zmmm512,
		// Token: 0x04002182 RID: 8578
		EVEX_Vptestnmw_kr_k1_xmm_xmmm128,
		// Token: 0x04002183 RID: 8579
		EVEX_Vptestnmw_kr_k1_ymm_ymmm256,
		// Token: 0x04002184 RID: 8580
		EVEX_Vptestnmw_kr_k1_zmm_zmmm512,
		// Token: 0x04002185 RID: 8581
		EVEX_Vptestmd_kr_k1_xmm_xmmm128b32,
		// Token: 0x04002186 RID: 8582
		EVEX_Vptestmd_kr_k1_ymm_ymmm256b32,
		// Token: 0x04002187 RID: 8583
		EVEX_Vptestmd_kr_k1_zmm_zmmm512b32,
		// Token: 0x04002188 RID: 8584
		EVEX_Vptestmq_kr_k1_xmm_xmmm128b64,
		// Token: 0x04002189 RID: 8585
		EVEX_Vptestmq_kr_k1_ymm_ymmm256b64,
		// Token: 0x0400218A RID: 8586
		EVEX_Vptestmq_kr_k1_zmm_zmmm512b64,
		// Token: 0x0400218B RID: 8587
		EVEX_Vptestnmd_kr_k1_xmm_xmmm128b32,
		// Token: 0x0400218C RID: 8588
		EVEX_Vptestnmd_kr_k1_ymm_ymmm256b32,
		// Token: 0x0400218D RID: 8589
		EVEX_Vptestnmd_kr_k1_zmm_zmmm512b32,
		// Token: 0x0400218E RID: 8590
		EVEX_Vptestnmq_kr_k1_xmm_xmmm128b64,
		// Token: 0x0400218F RID: 8591
		EVEX_Vptestnmq_kr_k1_ymm_ymmm256b64,
		// Token: 0x04002190 RID: 8592
		EVEX_Vptestnmq_kr_k1_zmm_zmmm512b64,
		// Token: 0x04002191 RID: 8593
		Pmuldq_xmm_xmmm128,
		// Token: 0x04002192 RID: 8594
		VEX_Vpmuldq_xmm_xmm_xmmm128,
		// Token: 0x04002193 RID: 8595
		VEX_Vpmuldq_ymm_ymm_ymmm256,
		// Token: 0x04002194 RID: 8596
		EVEX_Vpmuldq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002195 RID: 8597
		EVEX_Vpmuldq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002196 RID: 8598
		EVEX_Vpmuldq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002197 RID: 8599
		EVEX_Vpmovm2b_xmm_kr,
		// Token: 0x04002198 RID: 8600
		EVEX_Vpmovm2b_ymm_kr,
		// Token: 0x04002199 RID: 8601
		EVEX_Vpmovm2b_zmm_kr,
		// Token: 0x0400219A RID: 8602
		EVEX_Vpmovm2w_xmm_kr,
		// Token: 0x0400219B RID: 8603
		EVEX_Vpmovm2w_ymm_kr,
		// Token: 0x0400219C RID: 8604
		EVEX_Vpmovm2w_zmm_kr,
		// Token: 0x0400219D RID: 8605
		Pcmpeqq_xmm_xmmm128,
		// Token: 0x0400219E RID: 8606
		VEX_Vpcmpeqq_xmm_xmm_xmmm128,
		// Token: 0x0400219F RID: 8607
		VEX_Vpcmpeqq_ymm_ymm_ymmm256,
		// Token: 0x040021A0 RID: 8608
		EVEX_Vpcmpeqq_kr_k1_xmm_xmmm128b64,
		// Token: 0x040021A1 RID: 8609
		EVEX_Vpcmpeqq_kr_k1_ymm_ymmm256b64,
		// Token: 0x040021A2 RID: 8610
		EVEX_Vpcmpeqq_kr_k1_zmm_zmmm512b64,
		// Token: 0x040021A3 RID: 8611
		EVEX_Vpmovb2m_kr_xmm,
		// Token: 0x040021A4 RID: 8612
		EVEX_Vpmovb2m_kr_ymm,
		// Token: 0x040021A5 RID: 8613
		EVEX_Vpmovb2m_kr_zmm,
		// Token: 0x040021A6 RID: 8614
		EVEX_Vpmovw2m_kr_xmm,
		// Token: 0x040021A7 RID: 8615
		EVEX_Vpmovw2m_kr_ymm,
		// Token: 0x040021A8 RID: 8616
		EVEX_Vpmovw2m_kr_zmm,
		// Token: 0x040021A9 RID: 8617
		Movntdqa_xmm_m128,
		// Token: 0x040021AA RID: 8618
		VEX_Vmovntdqa_xmm_m128,
		// Token: 0x040021AB RID: 8619
		VEX_Vmovntdqa_ymm_m256,
		// Token: 0x040021AC RID: 8620
		EVEX_Vmovntdqa_xmm_m128,
		// Token: 0x040021AD RID: 8621
		EVEX_Vmovntdqa_ymm_m256,
		// Token: 0x040021AE RID: 8622
		EVEX_Vmovntdqa_zmm_m512,
		// Token: 0x040021AF RID: 8623
		EVEX_Vpbroadcastmb2q_xmm_kr,
		// Token: 0x040021B0 RID: 8624
		EVEX_Vpbroadcastmb2q_ymm_kr,
		// Token: 0x040021B1 RID: 8625
		EVEX_Vpbroadcastmb2q_zmm_kr,
		// Token: 0x040021B2 RID: 8626
		Packusdw_xmm_xmmm128,
		// Token: 0x040021B3 RID: 8627
		VEX_Vpackusdw_xmm_xmm_xmmm128,
		// Token: 0x040021B4 RID: 8628
		VEX_Vpackusdw_ymm_ymm_ymmm256,
		// Token: 0x040021B5 RID: 8629
		EVEX_Vpackusdw_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040021B6 RID: 8630
		EVEX_Vpackusdw_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040021B7 RID: 8631
		EVEX_Vpackusdw_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x040021B8 RID: 8632
		VEX_Vmaskmovps_xmm_xmm_m128,
		// Token: 0x040021B9 RID: 8633
		VEX_Vmaskmovps_ymm_ymm_m256,
		// Token: 0x040021BA RID: 8634
		EVEX_Vscalefps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040021BB RID: 8635
		EVEX_Vscalefps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040021BC RID: 8636
		EVEX_Vscalefps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x040021BD RID: 8637
		EVEX_Vscalefpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x040021BE RID: 8638
		EVEX_Vscalefpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x040021BF RID: 8639
		EVEX_Vscalefpd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x040021C0 RID: 8640
		VEX_Vmaskmovpd_xmm_xmm_m128,
		// Token: 0x040021C1 RID: 8641
		VEX_Vmaskmovpd_ymm_ymm_m256,
		// Token: 0x040021C2 RID: 8642
		EVEX_Vscalefss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x040021C3 RID: 8643
		EVEX_Vscalefsd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x040021C4 RID: 8644
		VEX_Vmaskmovps_m128_xmm_xmm,
		// Token: 0x040021C5 RID: 8645
		VEX_Vmaskmovps_m256_ymm_ymm,
		// Token: 0x040021C6 RID: 8646
		VEX_Vmaskmovpd_m128_xmm_xmm,
		// Token: 0x040021C7 RID: 8647
		VEX_Vmaskmovpd_m256_ymm_ymm,
		// Token: 0x040021C8 RID: 8648
		Pmovzxbw_xmm_xmmm64,
		// Token: 0x040021C9 RID: 8649
		VEX_Vpmovzxbw_xmm_xmmm64,
		// Token: 0x040021CA RID: 8650
		VEX_Vpmovzxbw_ymm_xmmm128,
		// Token: 0x040021CB RID: 8651
		EVEX_Vpmovzxbw_xmm_k1z_xmmm64,
		// Token: 0x040021CC RID: 8652
		EVEX_Vpmovzxbw_ymm_k1z_xmmm128,
		// Token: 0x040021CD RID: 8653
		EVEX_Vpmovzxbw_zmm_k1z_ymmm256,
		// Token: 0x040021CE RID: 8654
		EVEX_Vpmovwb_xmmm64_k1z_xmm,
		// Token: 0x040021CF RID: 8655
		EVEX_Vpmovwb_xmmm128_k1z_ymm,
		// Token: 0x040021D0 RID: 8656
		EVEX_Vpmovwb_ymmm256_k1z_zmm,
		// Token: 0x040021D1 RID: 8657
		Pmovzxbd_xmm_xmmm32,
		// Token: 0x040021D2 RID: 8658
		VEX_Vpmovzxbd_xmm_xmmm32,
		// Token: 0x040021D3 RID: 8659
		VEX_Vpmovzxbd_ymm_xmmm64,
		// Token: 0x040021D4 RID: 8660
		EVEX_Vpmovzxbd_xmm_k1z_xmmm32,
		// Token: 0x040021D5 RID: 8661
		EVEX_Vpmovzxbd_ymm_k1z_xmmm64,
		// Token: 0x040021D6 RID: 8662
		EVEX_Vpmovzxbd_zmm_k1z_xmmm128,
		// Token: 0x040021D7 RID: 8663
		EVEX_Vpmovdb_xmmm32_k1z_xmm,
		// Token: 0x040021D8 RID: 8664
		EVEX_Vpmovdb_xmmm64_k1z_ymm,
		// Token: 0x040021D9 RID: 8665
		EVEX_Vpmovdb_xmmm128_k1z_zmm,
		// Token: 0x040021DA RID: 8666
		Pmovzxbq_xmm_xmmm16,
		// Token: 0x040021DB RID: 8667
		VEX_Vpmovzxbq_xmm_xmmm16,
		// Token: 0x040021DC RID: 8668
		VEX_Vpmovzxbq_ymm_xmmm32,
		// Token: 0x040021DD RID: 8669
		EVEX_Vpmovzxbq_xmm_k1z_xmmm16,
		// Token: 0x040021DE RID: 8670
		EVEX_Vpmovzxbq_ymm_k1z_xmmm32,
		// Token: 0x040021DF RID: 8671
		EVEX_Vpmovzxbq_zmm_k1z_xmmm64,
		// Token: 0x040021E0 RID: 8672
		EVEX_Vpmovqb_xmmm16_k1z_xmm,
		// Token: 0x040021E1 RID: 8673
		EVEX_Vpmovqb_xmmm32_k1z_ymm,
		// Token: 0x040021E2 RID: 8674
		EVEX_Vpmovqb_xmmm64_k1z_zmm,
		// Token: 0x040021E3 RID: 8675
		Pmovzxwd_xmm_xmmm64,
		// Token: 0x040021E4 RID: 8676
		VEX_Vpmovzxwd_xmm_xmmm64,
		// Token: 0x040021E5 RID: 8677
		VEX_Vpmovzxwd_ymm_xmmm128,
		// Token: 0x040021E6 RID: 8678
		EVEX_Vpmovzxwd_xmm_k1z_xmmm64,
		// Token: 0x040021E7 RID: 8679
		EVEX_Vpmovzxwd_ymm_k1z_xmmm128,
		// Token: 0x040021E8 RID: 8680
		EVEX_Vpmovzxwd_zmm_k1z_ymmm256,
		// Token: 0x040021E9 RID: 8681
		EVEX_Vpmovdw_xmmm64_k1z_xmm,
		// Token: 0x040021EA RID: 8682
		EVEX_Vpmovdw_xmmm128_k1z_ymm,
		// Token: 0x040021EB RID: 8683
		EVEX_Vpmovdw_ymmm256_k1z_zmm,
		// Token: 0x040021EC RID: 8684
		Pmovzxwq_xmm_xmmm32,
		// Token: 0x040021ED RID: 8685
		VEX_Vpmovzxwq_xmm_xmmm32,
		// Token: 0x040021EE RID: 8686
		VEX_Vpmovzxwq_ymm_xmmm64,
		// Token: 0x040021EF RID: 8687
		EVEX_Vpmovzxwq_xmm_k1z_xmmm32,
		// Token: 0x040021F0 RID: 8688
		EVEX_Vpmovzxwq_ymm_k1z_xmmm64,
		// Token: 0x040021F1 RID: 8689
		EVEX_Vpmovzxwq_zmm_k1z_xmmm128,
		// Token: 0x040021F2 RID: 8690
		EVEX_Vpmovqw_xmmm32_k1z_xmm,
		// Token: 0x040021F3 RID: 8691
		EVEX_Vpmovqw_xmmm64_k1z_ymm,
		// Token: 0x040021F4 RID: 8692
		EVEX_Vpmovqw_xmmm128_k1z_zmm,
		// Token: 0x040021F5 RID: 8693
		Pmovzxdq_xmm_xmmm64,
		// Token: 0x040021F6 RID: 8694
		VEX_Vpmovzxdq_xmm_xmmm64,
		// Token: 0x040021F7 RID: 8695
		VEX_Vpmovzxdq_ymm_xmmm128,
		// Token: 0x040021F8 RID: 8696
		EVEX_Vpmovzxdq_xmm_k1z_xmmm64,
		// Token: 0x040021F9 RID: 8697
		EVEX_Vpmovzxdq_ymm_k1z_xmmm128,
		// Token: 0x040021FA RID: 8698
		EVEX_Vpmovzxdq_zmm_k1z_ymmm256,
		// Token: 0x040021FB RID: 8699
		EVEX_Vpmovqd_xmmm64_k1z_xmm,
		// Token: 0x040021FC RID: 8700
		EVEX_Vpmovqd_xmmm128_k1z_ymm,
		// Token: 0x040021FD RID: 8701
		EVEX_Vpmovqd_ymmm256_k1z_zmm,
		// Token: 0x040021FE RID: 8702
		VEX_Vpermd_ymm_ymm_ymmm256,
		// Token: 0x040021FF RID: 8703
		EVEX_Vpermd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002200 RID: 8704
		EVEX_Vpermd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002201 RID: 8705
		EVEX_Vpermq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002202 RID: 8706
		EVEX_Vpermq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002203 RID: 8707
		Pcmpgtq_xmm_xmmm128,
		// Token: 0x04002204 RID: 8708
		VEX_Vpcmpgtq_xmm_xmm_xmmm128,
		// Token: 0x04002205 RID: 8709
		VEX_Vpcmpgtq_ymm_ymm_ymmm256,
		// Token: 0x04002206 RID: 8710
		EVEX_Vpcmpgtq_kr_k1_xmm_xmmm128b64,
		// Token: 0x04002207 RID: 8711
		EVEX_Vpcmpgtq_kr_k1_ymm_ymmm256b64,
		// Token: 0x04002208 RID: 8712
		EVEX_Vpcmpgtq_kr_k1_zmm_zmmm512b64,
		// Token: 0x04002209 RID: 8713
		Pminsb_xmm_xmmm128,
		// Token: 0x0400220A RID: 8714
		VEX_Vpminsb_xmm_xmm_xmmm128,
		// Token: 0x0400220B RID: 8715
		VEX_Vpminsb_ymm_ymm_ymmm256,
		// Token: 0x0400220C RID: 8716
		EVEX_Vpminsb_xmm_k1z_xmm_xmmm128,
		// Token: 0x0400220D RID: 8717
		EVEX_Vpminsb_ymm_k1z_ymm_ymmm256,
		// Token: 0x0400220E RID: 8718
		EVEX_Vpminsb_zmm_k1z_zmm_zmmm512,
		// Token: 0x0400220F RID: 8719
		EVEX_Vpmovm2d_xmm_kr,
		// Token: 0x04002210 RID: 8720
		EVEX_Vpmovm2d_ymm_kr,
		// Token: 0x04002211 RID: 8721
		EVEX_Vpmovm2d_zmm_kr,
		// Token: 0x04002212 RID: 8722
		EVEX_Vpmovm2q_xmm_kr,
		// Token: 0x04002213 RID: 8723
		EVEX_Vpmovm2q_ymm_kr,
		// Token: 0x04002214 RID: 8724
		EVEX_Vpmovm2q_zmm_kr,
		// Token: 0x04002215 RID: 8725
		Pminsd_xmm_xmmm128,
		// Token: 0x04002216 RID: 8726
		VEX_Vpminsd_xmm_xmm_xmmm128,
		// Token: 0x04002217 RID: 8727
		VEX_Vpminsd_ymm_ymm_ymmm256,
		// Token: 0x04002218 RID: 8728
		EVEX_Vpminsd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002219 RID: 8729
		EVEX_Vpminsd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x0400221A RID: 8730
		EVEX_Vpminsd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x0400221B RID: 8731
		EVEX_Vpminsq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x0400221C RID: 8732
		EVEX_Vpminsq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x0400221D RID: 8733
		EVEX_Vpminsq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x0400221E RID: 8734
		EVEX_Vpmovd2m_kr_xmm,
		// Token: 0x0400221F RID: 8735
		EVEX_Vpmovd2m_kr_ymm,
		// Token: 0x04002220 RID: 8736
		EVEX_Vpmovd2m_kr_zmm,
		// Token: 0x04002221 RID: 8737
		EVEX_Vpmovq2m_kr_xmm,
		// Token: 0x04002222 RID: 8738
		EVEX_Vpmovq2m_kr_ymm,
		// Token: 0x04002223 RID: 8739
		EVEX_Vpmovq2m_kr_zmm,
		// Token: 0x04002224 RID: 8740
		Pminuw_xmm_xmmm128,
		// Token: 0x04002225 RID: 8741
		VEX_Vpminuw_xmm_xmm_xmmm128,
		// Token: 0x04002226 RID: 8742
		VEX_Vpminuw_ymm_ymm_ymmm256,
		// Token: 0x04002227 RID: 8743
		EVEX_Vpminuw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04002228 RID: 8744
		EVEX_Vpminuw_ymm_k1z_ymm_ymmm256,
		// Token: 0x04002229 RID: 8745
		EVEX_Vpminuw_zmm_k1z_zmm_zmmm512,
		// Token: 0x0400222A RID: 8746
		EVEX_Vpbroadcastmw2d_xmm_kr,
		// Token: 0x0400222B RID: 8747
		EVEX_Vpbroadcastmw2d_ymm_kr,
		// Token: 0x0400222C RID: 8748
		EVEX_Vpbroadcastmw2d_zmm_kr,
		// Token: 0x0400222D RID: 8749
		Pminud_xmm_xmmm128,
		// Token: 0x0400222E RID: 8750
		VEX_Vpminud_xmm_xmm_xmmm128,
		// Token: 0x0400222F RID: 8751
		VEX_Vpminud_ymm_ymm_ymmm256,
		// Token: 0x04002230 RID: 8752
		EVEX_Vpminud_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002231 RID: 8753
		EVEX_Vpminud_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002232 RID: 8754
		EVEX_Vpminud_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002233 RID: 8755
		EVEX_Vpminuq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002234 RID: 8756
		EVEX_Vpminuq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002235 RID: 8757
		EVEX_Vpminuq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002236 RID: 8758
		Pmaxsb_xmm_xmmm128,
		// Token: 0x04002237 RID: 8759
		VEX_Vpmaxsb_xmm_xmm_xmmm128,
		// Token: 0x04002238 RID: 8760
		VEX_Vpmaxsb_ymm_ymm_ymmm256,
		// Token: 0x04002239 RID: 8761
		EVEX_Vpmaxsb_xmm_k1z_xmm_xmmm128,
		// Token: 0x0400223A RID: 8762
		EVEX_Vpmaxsb_ymm_k1z_ymm_ymmm256,
		// Token: 0x0400223B RID: 8763
		EVEX_Vpmaxsb_zmm_k1z_zmm_zmmm512,
		// Token: 0x0400223C RID: 8764
		Pmaxsd_xmm_xmmm128,
		// Token: 0x0400223D RID: 8765
		VEX_Vpmaxsd_xmm_xmm_xmmm128,
		// Token: 0x0400223E RID: 8766
		VEX_Vpmaxsd_ymm_ymm_ymmm256,
		// Token: 0x0400223F RID: 8767
		EVEX_Vpmaxsd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002240 RID: 8768
		EVEX_Vpmaxsd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002241 RID: 8769
		EVEX_Vpmaxsd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002242 RID: 8770
		EVEX_Vpmaxsq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002243 RID: 8771
		EVEX_Vpmaxsq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002244 RID: 8772
		EVEX_Vpmaxsq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002245 RID: 8773
		Pmaxuw_xmm_xmmm128,
		// Token: 0x04002246 RID: 8774
		VEX_Vpmaxuw_xmm_xmm_xmmm128,
		// Token: 0x04002247 RID: 8775
		VEX_Vpmaxuw_ymm_ymm_ymmm256,
		// Token: 0x04002248 RID: 8776
		EVEX_Vpmaxuw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04002249 RID: 8777
		EVEX_Vpmaxuw_ymm_k1z_ymm_ymmm256,
		// Token: 0x0400224A RID: 8778
		EVEX_Vpmaxuw_zmm_k1z_zmm_zmmm512,
		// Token: 0x0400224B RID: 8779
		Pmaxud_xmm_xmmm128,
		// Token: 0x0400224C RID: 8780
		VEX_Vpmaxud_xmm_xmm_xmmm128,
		// Token: 0x0400224D RID: 8781
		VEX_Vpmaxud_ymm_ymm_ymmm256,
		// Token: 0x0400224E RID: 8782
		EVEX_Vpmaxud_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x0400224F RID: 8783
		EVEX_Vpmaxud_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002250 RID: 8784
		EVEX_Vpmaxud_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002251 RID: 8785
		EVEX_Vpmaxuq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002252 RID: 8786
		EVEX_Vpmaxuq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002253 RID: 8787
		EVEX_Vpmaxuq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002254 RID: 8788
		Pmulld_xmm_xmmm128,
		// Token: 0x04002255 RID: 8789
		VEX_Vpmulld_xmm_xmm_xmmm128,
		// Token: 0x04002256 RID: 8790
		VEX_Vpmulld_ymm_ymm_ymmm256,
		// Token: 0x04002257 RID: 8791
		EVEX_Vpmulld_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002258 RID: 8792
		EVEX_Vpmulld_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002259 RID: 8793
		EVEX_Vpmulld_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x0400225A RID: 8794
		EVEX_Vpmullq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x0400225B RID: 8795
		EVEX_Vpmullq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x0400225C RID: 8796
		EVEX_Vpmullq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x0400225D RID: 8797
		Phminposuw_xmm_xmmm128,
		// Token: 0x0400225E RID: 8798
		VEX_Vphminposuw_xmm_xmmm128,
		// Token: 0x0400225F RID: 8799
		EVEX_Vgetexpps_xmm_k1z_xmmm128b32,
		// Token: 0x04002260 RID: 8800
		EVEX_Vgetexpps_ymm_k1z_ymmm256b32,
		// Token: 0x04002261 RID: 8801
		EVEX_Vgetexpps_zmm_k1z_zmmm512b32_sae,
		// Token: 0x04002262 RID: 8802
		EVEX_Vgetexppd_xmm_k1z_xmmm128b64,
		// Token: 0x04002263 RID: 8803
		EVEX_Vgetexppd_ymm_k1z_ymmm256b64,
		// Token: 0x04002264 RID: 8804
		EVEX_Vgetexppd_zmm_k1z_zmmm512b64_sae,
		// Token: 0x04002265 RID: 8805
		EVEX_Vgetexpss_xmm_k1z_xmm_xmmm32_sae,
		// Token: 0x04002266 RID: 8806
		EVEX_Vgetexpsd_xmm_k1z_xmm_xmmm64_sae,
		// Token: 0x04002267 RID: 8807
		EVEX_Vplzcntd_xmm_k1z_xmmm128b32,
		// Token: 0x04002268 RID: 8808
		EVEX_Vplzcntd_ymm_k1z_ymmm256b32,
		// Token: 0x04002269 RID: 8809
		EVEX_Vplzcntd_zmm_k1z_zmmm512b32,
		// Token: 0x0400226A RID: 8810
		EVEX_Vplzcntq_xmm_k1z_xmmm128b64,
		// Token: 0x0400226B RID: 8811
		EVEX_Vplzcntq_ymm_k1z_ymmm256b64,
		// Token: 0x0400226C RID: 8812
		EVEX_Vplzcntq_zmm_k1z_zmmm512b64,
		// Token: 0x0400226D RID: 8813
		VEX_Vpsrlvd_xmm_xmm_xmmm128,
		// Token: 0x0400226E RID: 8814
		VEX_Vpsrlvd_ymm_ymm_ymmm256,
		// Token: 0x0400226F RID: 8815
		VEX_Vpsrlvq_xmm_xmm_xmmm128,
		// Token: 0x04002270 RID: 8816
		VEX_Vpsrlvq_ymm_ymm_ymmm256,
		// Token: 0x04002271 RID: 8817
		EVEX_Vpsrlvd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002272 RID: 8818
		EVEX_Vpsrlvd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002273 RID: 8819
		EVEX_Vpsrlvd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002274 RID: 8820
		EVEX_Vpsrlvq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002275 RID: 8821
		EVEX_Vpsrlvq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002276 RID: 8822
		EVEX_Vpsrlvq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002277 RID: 8823
		VEX_Vpsravd_xmm_xmm_xmmm128,
		// Token: 0x04002278 RID: 8824
		VEX_Vpsravd_ymm_ymm_ymmm256,
		// Token: 0x04002279 RID: 8825
		EVEX_Vpsravd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x0400227A RID: 8826
		EVEX_Vpsravd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x0400227B RID: 8827
		EVEX_Vpsravd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x0400227C RID: 8828
		EVEX_Vpsravq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x0400227D RID: 8829
		EVEX_Vpsravq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x0400227E RID: 8830
		EVEX_Vpsravq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x0400227F RID: 8831
		VEX_Vpsllvd_xmm_xmm_xmmm128,
		// Token: 0x04002280 RID: 8832
		VEX_Vpsllvd_ymm_ymm_ymmm256,
		// Token: 0x04002281 RID: 8833
		VEX_Vpsllvq_xmm_xmm_xmmm128,
		// Token: 0x04002282 RID: 8834
		VEX_Vpsllvq_ymm_ymm_ymmm256,
		// Token: 0x04002283 RID: 8835
		EVEX_Vpsllvd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002284 RID: 8836
		EVEX_Vpsllvd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002285 RID: 8837
		EVEX_Vpsllvd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002286 RID: 8838
		EVEX_Vpsllvq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002287 RID: 8839
		EVEX_Vpsllvq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002288 RID: 8840
		EVEX_Vpsllvq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002289 RID: 8841
		EVEX_Vrcp14ps_xmm_k1z_xmmm128b32,
		// Token: 0x0400228A RID: 8842
		EVEX_Vrcp14ps_ymm_k1z_ymmm256b32,
		// Token: 0x0400228B RID: 8843
		EVEX_Vrcp14ps_zmm_k1z_zmmm512b32,
		// Token: 0x0400228C RID: 8844
		EVEX_Vrcp14pd_xmm_k1z_xmmm128b64,
		// Token: 0x0400228D RID: 8845
		EVEX_Vrcp14pd_ymm_k1z_ymmm256b64,
		// Token: 0x0400228E RID: 8846
		EVEX_Vrcp14pd_zmm_k1z_zmmm512b64,
		// Token: 0x0400228F RID: 8847
		EVEX_Vrcp14ss_xmm_k1z_xmm_xmmm32,
		// Token: 0x04002290 RID: 8848
		EVEX_Vrcp14sd_xmm_k1z_xmm_xmmm64,
		// Token: 0x04002291 RID: 8849
		EVEX_Vrsqrt14ps_xmm_k1z_xmmm128b32,
		// Token: 0x04002292 RID: 8850
		EVEX_Vrsqrt14ps_ymm_k1z_ymmm256b32,
		// Token: 0x04002293 RID: 8851
		EVEX_Vrsqrt14ps_zmm_k1z_zmmm512b32,
		// Token: 0x04002294 RID: 8852
		EVEX_Vrsqrt14pd_xmm_k1z_xmmm128b64,
		// Token: 0x04002295 RID: 8853
		EVEX_Vrsqrt14pd_ymm_k1z_ymmm256b64,
		// Token: 0x04002296 RID: 8854
		EVEX_Vrsqrt14pd_zmm_k1z_zmmm512b64,
		// Token: 0x04002297 RID: 8855
		EVEX_Vrsqrt14ss_xmm_k1z_xmm_xmmm32,
		// Token: 0x04002298 RID: 8856
		EVEX_Vrsqrt14sd_xmm_k1z_xmm_xmmm64,
		// Token: 0x04002299 RID: 8857
		EVEX_Vpdpbusd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x0400229A RID: 8858
		EVEX_Vpdpbusd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x0400229B RID: 8859
		EVEX_Vpdpbusd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x0400229C RID: 8860
		EVEX_Vpdpbusds_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x0400229D RID: 8861
		EVEX_Vpdpbusds_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x0400229E RID: 8862
		EVEX_Vpdpbusds_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x0400229F RID: 8863
		EVEX_Vpdpwssd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040022A0 RID: 8864
		EVEX_Vpdpwssd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040022A1 RID: 8865
		EVEX_Vpdpwssd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x040022A2 RID: 8866
		EVEX_Vdpbf16ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040022A3 RID: 8867
		EVEX_Vdpbf16ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040022A4 RID: 8868
		EVEX_Vdpbf16ps_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x040022A5 RID: 8869
		EVEX_Vp4dpwssd_zmm_k1z_zmmp3_m128,
		// Token: 0x040022A6 RID: 8870
		EVEX_Vpdpwssds_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040022A7 RID: 8871
		EVEX_Vpdpwssds_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040022A8 RID: 8872
		EVEX_Vpdpwssds_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x040022A9 RID: 8873
		EVEX_Vp4dpwssds_zmm_k1z_zmmp3_m128,
		// Token: 0x040022AA RID: 8874
		EVEX_Vpopcntb_xmm_k1z_xmmm128,
		// Token: 0x040022AB RID: 8875
		EVEX_Vpopcntb_ymm_k1z_ymmm256,
		// Token: 0x040022AC RID: 8876
		EVEX_Vpopcntb_zmm_k1z_zmmm512,
		// Token: 0x040022AD RID: 8877
		EVEX_Vpopcntw_xmm_k1z_xmmm128,
		// Token: 0x040022AE RID: 8878
		EVEX_Vpopcntw_ymm_k1z_ymmm256,
		// Token: 0x040022AF RID: 8879
		EVEX_Vpopcntw_zmm_k1z_zmmm512,
		// Token: 0x040022B0 RID: 8880
		EVEX_Vpopcntd_xmm_k1z_xmmm128b32,
		// Token: 0x040022B1 RID: 8881
		EVEX_Vpopcntd_ymm_k1z_ymmm256b32,
		// Token: 0x040022B2 RID: 8882
		EVEX_Vpopcntd_zmm_k1z_zmmm512b32,
		// Token: 0x040022B3 RID: 8883
		EVEX_Vpopcntq_xmm_k1z_xmmm128b64,
		// Token: 0x040022B4 RID: 8884
		EVEX_Vpopcntq_ymm_k1z_ymmm256b64,
		// Token: 0x040022B5 RID: 8885
		EVEX_Vpopcntq_zmm_k1z_zmmm512b64,
		// Token: 0x040022B6 RID: 8886
		VEX_Vpbroadcastd_xmm_xmmm32,
		// Token: 0x040022B7 RID: 8887
		VEX_Vpbroadcastd_ymm_xmmm32,
		// Token: 0x040022B8 RID: 8888
		EVEX_Vpbroadcastd_xmm_k1z_xmmm32,
		// Token: 0x040022B9 RID: 8889
		EVEX_Vpbroadcastd_ymm_k1z_xmmm32,
		// Token: 0x040022BA RID: 8890
		EVEX_Vpbroadcastd_zmm_k1z_xmmm32,
		// Token: 0x040022BB RID: 8891
		VEX_Vpbroadcastq_xmm_xmmm64,
		// Token: 0x040022BC RID: 8892
		VEX_Vpbroadcastq_ymm_xmmm64,
		// Token: 0x040022BD RID: 8893
		EVEX_Vbroadcasti32x2_xmm_k1z_xmmm64,
		// Token: 0x040022BE RID: 8894
		EVEX_Vbroadcasti32x2_ymm_k1z_xmmm64,
		// Token: 0x040022BF RID: 8895
		EVEX_Vbroadcasti32x2_zmm_k1z_xmmm64,
		// Token: 0x040022C0 RID: 8896
		EVEX_Vpbroadcastq_xmm_k1z_xmmm64,
		// Token: 0x040022C1 RID: 8897
		EVEX_Vpbroadcastq_ymm_k1z_xmmm64,
		// Token: 0x040022C2 RID: 8898
		EVEX_Vpbroadcastq_zmm_k1z_xmmm64,
		// Token: 0x040022C3 RID: 8899
		VEX_Vbroadcasti128_ymm_m128,
		// Token: 0x040022C4 RID: 8900
		EVEX_Vbroadcasti32x4_ymm_k1z_m128,
		// Token: 0x040022C5 RID: 8901
		EVEX_Vbroadcasti32x4_zmm_k1z_m128,
		// Token: 0x040022C6 RID: 8902
		EVEX_Vbroadcasti64x2_ymm_k1z_m128,
		// Token: 0x040022C7 RID: 8903
		EVEX_Vbroadcasti64x2_zmm_k1z_m128,
		// Token: 0x040022C8 RID: 8904
		EVEX_Vbroadcasti32x8_zmm_k1z_m256,
		// Token: 0x040022C9 RID: 8905
		EVEX_Vbroadcasti64x4_zmm_k1z_m256,
		// Token: 0x040022CA RID: 8906
		EVEX_Vpexpandb_xmm_k1z_xmmm128,
		// Token: 0x040022CB RID: 8907
		EVEX_Vpexpandb_ymm_k1z_ymmm256,
		// Token: 0x040022CC RID: 8908
		EVEX_Vpexpandb_zmm_k1z_zmmm512,
		// Token: 0x040022CD RID: 8909
		EVEX_Vpexpandw_xmm_k1z_xmmm128,
		// Token: 0x040022CE RID: 8910
		EVEX_Vpexpandw_ymm_k1z_ymmm256,
		// Token: 0x040022CF RID: 8911
		EVEX_Vpexpandw_zmm_k1z_zmmm512,
		// Token: 0x040022D0 RID: 8912
		EVEX_Vpcompressb_xmmm128_k1z_xmm,
		// Token: 0x040022D1 RID: 8913
		EVEX_Vpcompressb_ymmm256_k1z_ymm,
		// Token: 0x040022D2 RID: 8914
		EVEX_Vpcompressb_zmmm512_k1z_zmm,
		// Token: 0x040022D3 RID: 8915
		EVEX_Vpcompressw_xmmm128_k1z_xmm,
		// Token: 0x040022D4 RID: 8916
		EVEX_Vpcompressw_ymmm256_k1z_ymm,
		// Token: 0x040022D5 RID: 8917
		EVEX_Vpcompressw_zmmm512_k1z_zmm,
		// Token: 0x040022D6 RID: 8918
		EVEX_Vpblendmd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040022D7 RID: 8919
		EVEX_Vpblendmd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040022D8 RID: 8920
		EVEX_Vpblendmd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x040022D9 RID: 8921
		EVEX_Vpblendmq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x040022DA RID: 8922
		EVEX_Vpblendmq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x040022DB RID: 8923
		EVEX_Vpblendmq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x040022DC RID: 8924
		EVEX_Vblendmps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040022DD RID: 8925
		EVEX_Vblendmps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040022DE RID: 8926
		EVEX_Vblendmps_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x040022DF RID: 8927
		EVEX_Vblendmpd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x040022E0 RID: 8928
		EVEX_Vblendmpd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x040022E1 RID: 8929
		EVEX_Vblendmpd_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x040022E2 RID: 8930
		EVEX_Vpblendmb_xmm_k1z_xmm_xmmm128,
		// Token: 0x040022E3 RID: 8931
		EVEX_Vpblendmb_ymm_k1z_ymm_ymmm256,
		// Token: 0x040022E4 RID: 8932
		EVEX_Vpblendmb_zmm_k1z_zmm_zmmm512,
		// Token: 0x040022E5 RID: 8933
		EVEX_Vpblendmw_xmm_k1z_xmm_xmmm128,
		// Token: 0x040022E6 RID: 8934
		EVEX_Vpblendmw_ymm_k1z_ymm_ymmm256,
		// Token: 0x040022E7 RID: 8935
		EVEX_Vpblendmw_zmm_k1z_zmm_zmmm512,
		// Token: 0x040022E8 RID: 8936
		EVEX_Vp2intersectd_kp1_xmm_xmmm128b32,
		// Token: 0x040022E9 RID: 8937
		EVEX_Vp2intersectd_kp1_ymm_ymmm256b32,
		// Token: 0x040022EA RID: 8938
		EVEX_Vp2intersectd_kp1_zmm_zmmm512b32,
		// Token: 0x040022EB RID: 8939
		EVEX_Vp2intersectq_kp1_xmm_xmmm128b64,
		// Token: 0x040022EC RID: 8940
		EVEX_Vp2intersectq_kp1_ymm_ymmm256b64,
		// Token: 0x040022ED RID: 8941
		EVEX_Vp2intersectq_kp1_zmm_zmmm512b64,
		// Token: 0x040022EE RID: 8942
		EVEX_Vpshldvw_xmm_k1z_xmm_xmmm128,
		// Token: 0x040022EF RID: 8943
		EVEX_Vpshldvw_ymm_k1z_ymm_ymmm256,
		// Token: 0x040022F0 RID: 8944
		EVEX_Vpshldvw_zmm_k1z_zmm_zmmm512,
		// Token: 0x040022F1 RID: 8945
		EVEX_Vpshldvd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040022F2 RID: 8946
		EVEX_Vpshldvd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040022F3 RID: 8947
		EVEX_Vpshldvd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x040022F4 RID: 8948
		EVEX_Vpshldvq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x040022F5 RID: 8949
		EVEX_Vpshldvq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x040022F6 RID: 8950
		EVEX_Vpshldvq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x040022F7 RID: 8951
		EVEX_Vpshrdvw_xmm_k1z_xmm_xmmm128,
		// Token: 0x040022F8 RID: 8952
		EVEX_Vpshrdvw_ymm_k1z_ymm_ymmm256,
		// Token: 0x040022F9 RID: 8953
		EVEX_Vpshrdvw_zmm_k1z_zmm_zmmm512,
		// Token: 0x040022FA RID: 8954
		EVEX_Vcvtneps2bf16_xmm_k1z_xmmm128b32,
		// Token: 0x040022FB RID: 8955
		EVEX_Vcvtneps2bf16_xmm_k1z_ymmm256b32,
		// Token: 0x040022FC RID: 8956
		EVEX_Vcvtneps2bf16_ymm_k1z_zmmm512b32,
		// Token: 0x040022FD RID: 8957
		EVEX_Vcvtne2ps2bf16_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040022FE RID: 8958
		EVEX_Vcvtne2ps2bf16_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040022FF RID: 8959
		EVEX_Vcvtne2ps2bf16_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002300 RID: 8960
		EVEX_Vpshrdvd_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002301 RID: 8961
		EVEX_Vpshrdvd_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002302 RID: 8962
		EVEX_Vpshrdvd_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002303 RID: 8963
		EVEX_Vpshrdvq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002304 RID: 8964
		EVEX_Vpshrdvq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002305 RID: 8965
		EVEX_Vpshrdvq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002306 RID: 8966
		EVEX_Vpermi2b_xmm_k1z_xmm_xmmm128,
		// Token: 0x04002307 RID: 8967
		EVEX_Vpermi2b_ymm_k1z_ymm_ymmm256,
		// Token: 0x04002308 RID: 8968
		EVEX_Vpermi2b_zmm_k1z_zmm_zmmm512,
		// Token: 0x04002309 RID: 8969
		EVEX_Vpermi2w_xmm_k1z_xmm_xmmm128,
		// Token: 0x0400230A RID: 8970
		EVEX_Vpermi2w_ymm_k1z_ymm_ymmm256,
		// Token: 0x0400230B RID: 8971
		EVEX_Vpermi2w_zmm_k1z_zmm_zmmm512,
		// Token: 0x0400230C RID: 8972
		EVEX_Vpermi2d_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x0400230D RID: 8973
		EVEX_Vpermi2d_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x0400230E RID: 8974
		EVEX_Vpermi2d_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x0400230F RID: 8975
		EVEX_Vpermi2q_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002310 RID: 8976
		EVEX_Vpermi2q_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002311 RID: 8977
		EVEX_Vpermi2q_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002312 RID: 8978
		EVEX_Vpermi2ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002313 RID: 8979
		EVEX_Vpermi2ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002314 RID: 8980
		EVEX_Vpermi2ps_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002315 RID: 8981
		EVEX_Vpermi2pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002316 RID: 8982
		EVEX_Vpermi2pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002317 RID: 8983
		EVEX_Vpermi2pd_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002318 RID: 8984
		VEX_Vpbroadcastb_xmm_xmmm8,
		// Token: 0x04002319 RID: 8985
		VEX_Vpbroadcastb_ymm_xmmm8,
		// Token: 0x0400231A RID: 8986
		EVEX_Vpbroadcastb_xmm_k1z_xmmm8,
		// Token: 0x0400231B RID: 8987
		EVEX_Vpbroadcastb_ymm_k1z_xmmm8,
		// Token: 0x0400231C RID: 8988
		EVEX_Vpbroadcastb_zmm_k1z_xmmm8,
		// Token: 0x0400231D RID: 8989
		VEX_Vpbroadcastw_xmm_xmmm16,
		// Token: 0x0400231E RID: 8990
		VEX_Vpbroadcastw_ymm_xmmm16,
		// Token: 0x0400231F RID: 8991
		EVEX_Vpbroadcastw_xmm_k1z_xmmm16,
		// Token: 0x04002320 RID: 8992
		EVEX_Vpbroadcastw_ymm_k1z_xmmm16,
		// Token: 0x04002321 RID: 8993
		EVEX_Vpbroadcastw_zmm_k1z_xmmm16,
		// Token: 0x04002322 RID: 8994
		EVEX_Vpbroadcastb_xmm_k1z_r32,
		// Token: 0x04002323 RID: 8995
		EVEX_Vpbroadcastb_ymm_k1z_r32,
		// Token: 0x04002324 RID: 8996
		EVEX_Vpbroadcastb_zmm_k1z_r32,
		// Token: 0x04002325 RID: 8997
		EVEX_Vpbroadcastw_xmm_k1z_r32,
		// Token: 0x04002326 RID: 8998
		EVEX_Vpbroadcastw_ymm_k1z_r32,
		// Token: 0x04002327 RID: 8999
		EVEX_Vpbroadcastw_zmm_k1z_r32,
		// Token: 0x04002328 RID: 9000
		EVEX_Vpbroadcastd_xmm_k1z_r32,
		// Token: 0x04002329 RID: 9001
		EVEX_Vpbroadcastd_ymm_k1z_r32,
		// Token: 0x0400232A RID: 9002
		EVEX_Vpbroadcastd_zmm_k1z_r32,
		// Token: 0x0400232B RID: 9003
		EVEX_Vpbroadcastq_xmm_k1z_r64,
		// Token: 0x0400232C RID: 9004
		EVEX_Vpbroadcastq_ymm_k1z_r64,
		// Token: 0x0400232D RID: 9005
		EVEX_Vpbroadcastq_zmm_k1z_r64,
		// Token: 0x0400232E RID: 9006
		EVEX_Vpermt2b_xmm_k1z_xmm_xmmm128,
		// Token: 0x0400232F RID: 9007
		EVEX_Vpermt2b_ymm_k1z_ymm_ymmm256,
		// Token: 0x04002330 RID: 9008
		EVEX_Vpermt2b_zmm_k1z_zmm_zmmm512,
		// Token: 0x04002331 RID: 9009
		EVEX_Vpermt2w_xmm_k1z_xmm_xmmm128,
		// Token: 0x04002332 RID: 9010
		EVEX_Vpermt2w_ymm_k1z_ymm_ymmm256,
		// Token: 0x04002333 RID: 9011
		EVEX_Vpermt2w_zmm_k1z_zmm_zmmm512,
		// Token: 0x04002334 RID: 9012
		EVEX_Vpermt2d_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002335 RID: 9013
		EVEX_Vpermt2d_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002336 RID: 9014
		EVEX_Vpermt2d_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x04002337 RID: 9015
		EVEX_Vpermt2q_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002338 RID: 9016
		EVEX_Vpermt2q_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002339 RID: 9017
		EVEX_Vpermt2q_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x0400233A RID: 9018
		EVEX_Vpermt2ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x0400233B RID: 9019
		EVEX_Vpermt2ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x0400233C RID: 9020
		EVEX_Vpermt2ps_zmm_k1z_zmm_zmmm512b32,
		// Token: 0x0400233D RID: 9021
		EVEX_Vpermt2pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x0400233E RID: 9022
		EVEX_Vpermt2pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x0400233F RID: 9023
		EVEX_Vpermt2pd_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002340 RID: 9024
		Invept_r32_m128,
		// Token: 0x04002341 RID: 9025
		Invept_r64_m128,
		// Token: 0x04002342 RID: 9026
		Invvpid_r32_m128,
		// Token: 0x04002343 RID: 9027
		Invvpid_r64_m128,
		// Token: 0x04002344 RID: 9028
		Invpcid_r32_m128,
		// Token: 0x04002345 RID: 9029
		Invpcid_r64_m128,
		// Token: 0x04002346 RID: 9030
		EVEX_Vpmultishiftqb_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002347 RID: 9031
		EVEX_Vpmultishiftqb_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002348 RID: 9032
		EVEX_Vpmultishiftqb_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002349 RID: 9033
		EVEX_Vexpandps_xmm_k1z_xmmm128,
		// Token: 0x0400234A RID: 9034
		EVEX_Vexpandps_ymm_k1z_ymmm256,
		// Token: 0x0400234B RID: 9035
		EVEX_Vexpandps_zmm_k1z_zmmm512,
		// Token: 0x0400234C RID: 9036
		EVEX_Vexpandpd_xmm_k1z_xmmm128,
		// Token: 0x0400234D RID: 9037
		EVEX_Vexpandpd_ymm_k1z_ymmm256,
		// Token: 0x0400234E RID: 9038
		EVEX_Vexpandpd_zmm_k1z_zmmm512,
		// Token: 0x0400234F RID: 9039
		EVEX_Vpexpandd_xmm_k1z_xmmm128,
		// Token: 0x04002350 RID: 9040
		EVEX_Vpexpandd_ymm_k1z_ymmm256,
		// Token: 0x04002351 RID: 9041
		EVEX_Vpexpandd_zmm_k1z_zmmm512,
		// Token: 0x04002352 RID: 9042
		EVEX_Vpexpandq_xmm_k1z_xmmm128,
		// Token: 0x04002353 RID: 9043
		EVEX_Vpexpandq_ymm_k1z_ymmm256,
		// Token: 0x04002354 RID: 9044
		EVEX_Vpexpandq_zmm_k1z_zmmm512,
		// Token: 0x04002355 RID: 9045
		EVEX_Vcompressps_xmmm128_k1z_xmm,
		// Token: 0x04002356 RID: 9046
		EVEX_Vcompressps_ymmm256_k1z_ymm,
		// Token: 0x04002357 RID: 9047
		EVEX_Vcompressps_zmmm512_k1z_zmm,
		// Token: 0x04002358 RID: 9048
		EVEX_Vcompresspd_xmmm128_k1z_xmm,
		// Token: 0x04002359 RID: 9049
		EVEX_Vcompresspd_ymmm256_k1z_ymm,
		// Token: 0x0400235A RID: 9050
		EVEX_Vcompresspd_zmmm512_k1z_zmm,
		// Token: 0x0400235B RID: 9051
		EVEX_Vpcompressd_xmmm128_k1z_xmm,
		// Token: 0x0400235C RID: 9052
		EVEX_Vpcompressd_ymmm256_k1z_ymm,
		// Token: 0x0400235D RID: 9053
		EVEX_Vpcompressd_zmmm512_k1z_zmm,
		// Token: 0x0400235E RID: 9054
		EVEX_Vpcompressq_xmmm128_k1z_xmm,
		// Token: 0x0400235F RID: 9055
		EVEX_Vpcompressq_ymmm256_k1z_ymm,
		// Token: 0x04002360 RID: 9056
		EVEX_Vpcompressq_zmmm512_k1z_zmm,
		// Token: 0x04002361 RID: 9057
		VEX_Vpmaskmovd_xmm_xmm_m128,
		// Token: 0x04002362 RID: 9058
		VEX_Vpmaskmovd_ymm_ymm_m256,
		// Token: 0x04002363 RID: 9059
		VEX_Vpmaskmovq_xmm_xmm_m128,
		// Token: 0x04002364 RID: 9060
		VEX_Vpmaskmovq_ymm_ymm_m256,
		// Token: 0x04002365 RID: 9061
		EVEX_Vpermb_xmm_k1z_xmm_xmmm128,
		// Token: 0x04002366 RID: 9062
		EVEX_Vpermb_ymm_k1z_ymm_ymmm256,
		// Token: 0x04002367 RID: 9063
		EVEX_Vpermb_zmm_k1z_zmm_zmmm512,
		// Token: 0x04002368 RID: 9064
		EVEX_Vpermw_xmm_k1z_xmm_xmmm128,
		// Token: 0x04002369 RID: 9065
		EVEX_Vpermw_ymm_k1z_ymm_ymmm256,
		// Token: 0x0400236A RID: 9066
		EVEX_Vpermw_zmm_k1z_zmm_zmmm512,
		// Token: 0x0400236B RID: 9067
		VEX_Vpmaskmovd_m128_xmm_xmm,
		// Token: 0x0400236C RID: 9068
		VEX_Vpmaskmovd_m256_ymm_ymm,
		// Token: 0x0400236D RID: 9069
		VEX_Vpmaskmovq_m128_xmm_xmm,
		// Token: 0x0400236E RID: 9070
		VEX_Vpmaskmovq_m256_ymm_ymm,
		// Token: 0x0400236F RID: 9071
		EVEX_Vpshufbitqmb_kr_k1_xmm_xmmm128,
		// Token: 0x04002370 RID: 9072
		EVEX_Vpshufbitqmb_kr_k1_ymm_ymmm256,
		// Token: 0x04002371 RID: 9073
		EVEX_Vpshufbitqmb_kr_k1_zmm_zmmm512,
		// Token: 0x04002372 RID: 9074
		VEX_Vpgatherdd_xmm_vm32x_xmm,
		// Token: 0x04002373 RID: 9075
		VEX_Vpgatherdd_ymm_vm32y_ymm,
		// Token: 0x04002374 RID: 9076
		VEX_Vpgatherdq_xmm_vm32x_xmm,
		// Token: 0x04002375 RID: 9077
		VEX_Vpgatherdq_ymm_vm32x_ymm,
		// Token: 0x04002376 RID: 9078
		EVEX_Vpgatherdd_xmm_k1_vm32x,
		// Token: 0x04002377 RID: 9079
		EVEX_Vpgatherdd_ymm_k1_vm32y,
		// Token: 0x04002378 RID: 9080
		EVEX_Vpgatherdd_zmm_k1_vm32z,
		// Token: 0x04002379 RID: 9081
		EVEX_Vpgatherdq_xmm_k1_vm32x,
		// Token: 0x0400237A RID: 9082
		EVEX_Vpgatherdq_ymm_k1_vm32x,
		// Token: 0x0400237B RID: 9083
		EVEX_Vpgatherdq_zmm_k1_vm32y,
		// Token: 0x0400237C RID: 9084
		VEX_Vpgatherqd_xmm_vm64x_xmm,
		// Token: 0x0400237D RID: 9085
		VEX_Vpgatherqd_xmm_vm64y_xmm,
		// Token: 0x0400237E RID: 9086
		VEX_Vpgatherqq_xmm_vm64x_xmm,
		// Token: 0x0400237F RID: 9087
		VEX_Vpgatherqq_ymm_vm64y_ymm,
		// Token: 0x04002380 RID: 9088
		EVEX_Vpgatherqd_xmm_k1_vm64x,
		// Token: 0x04002381 RID: 9089
		EVEX_Vpgatherqd_xmm_k1_vm64y,
		// Token: 0x04002382 RID: 9090
		EVEX_Vpgatherqd_ymm_k1_vm64z,
		// Token: 0x04002383 RID: 9091
		EVEX_Vpgatherqq_xmm_k1_vm64x,
		// Token: 0x04002384 RID: 9092
		EVEX_Vpgatherqq_ymm_k1_vm64y,
		// Token: 0x04002385 RID: 9093
		EVEX_Vpgatherqq_zmm_k1_vm64z,
		// Token: 0x04002386 RID: 9094
		VEX_Vgatherdps_xmm_vm32x_xmm,
		// Token: 0x04002387 RID: 9095
		VEX_Vgatherdps_ymm_vm32y_ymm,
		// Token: 0x04002388 RID: 9096
		VEX_Vgatherdpd_xmm_vm32x_xmm,
		// Token: 0x04002389 RID: 9097
		VEX_Vgatherdpd_ymm_vm32x_ymm,
		// Token: 0x0400238A RID: 9098
		EVEX_Vgatherdps_xmm_k1_vm32x,
		// Token: 0x0400238B RID: 9099
		EVEX_Vgatherdps_ymm_k1_vm32y,
		// Token: 0x0400238C RID: 9100
		EVEX_Vgatherdps_zmm_k1_vm32z,
		// Token: 0x0400238D RID: 9101
		EVEX_Vgatherdpd_xmm_k1_vm32x,
		// Token: 0x0400238E RID: 9102
		EVEX_Vgatherdpd_ymm_k1_vm32x,
		// Token: 0x0400238F RID: 9103
		EVEX_Vgatherdpd_zmm_k1_vm32y,
		// Token: 0x04002390 RID: 9104
		VEX_Vgatherqps_xmm_vm64x_xmm,
		// Token: 0x04002391 RID: 9105
		VEX_Vgatherqps_xmm_vm64y_xmm,
		// Token: 0x04002392 RID: 9106
		VEX_Vgatherqpd_xmm_vm64x_xmm,
		// Token: 0x04002393 RID: 9107
		VEX_Vgatherqpd_ymm_vm64y_ymm,
		// Token: 0x04002394 RID: 9108
		EVEX_Vgatherqps_xmm_k1_vm64x,
		// Token: 0x04002395 RID: 9109
		EVEX_Vgatherqps_xmm_k1_vm64y,
		// Token: 0x04002396 RID: 9110
		EVEX_Vgatherqps_ymm_k1_vm64z,
		// Token: 0x04002397 RID: 9111
		EVEX_Vgatherqpd_xmm_k1_vm64x,
		// Token: 0x04002398 RID: 9112
		EVEX_Vgatherqpd_ymm_k1_vm64y,
		// Token: 0x04002399 RID: 9113
		EVEX_Vgatherqpd_zmm_k1_vm64z,
		// Token: 0x0400239A RID: 9114
		VEX_Vfmaddsub132ps_xmm_xmm_xmmm128,
		// Token: 0x0400239B RID: 9115
		VEX_Vfmaddsub132ps_ymm_ymm_ymmm256,
		// Token: 0x0400239C RID: 9116
		VEX_Vfmaddsub132pd_xmm_xmm_xmmm128,
		// Token: 0x0400239D RID: 9117
		VEX_Vfmaddsub132pd_ymm_ymm_ymmm256,
		// Token: 0x0400239E RID: 9118
		EVEX_Vfmaddsub132ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x0400239F RID: 9119
		EVEX_Vfmaddsub132ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040023A0 RID: 9120
		EVEX_Vfmaddsub132ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x040023A1 RID: 9121
		EVEX_Vfmaddsub132pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x040023A2 RID: 9122
		EVEX_Vfmaddsub132pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x040023A3 RID: 9123
		EVEX_Vfmaddsub132pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x040023A4 RID: 9124
		VEX_Vfmsubadd132ps_xmm_xmm_xmmm128,
		// Token: 0x040023A5 RID: 9125
		VEX_Vfmsubadd132ps_ymm_ymm_ymmm256,
		// Token: 0x040023A6 RID: 9126
		VEX_Vfmsubadd132pd_xmm_xmm_xmmm128,
		// Token: 0x040023A7 RID: 9127
		VEX_Vfmsubadd132pd_ymm_ymm_ymmm256,
		// Token: 0x040023A8 RID: 9128
		EVEX_Vfmsubadd132ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040023A9 RID: 9129
		EVEX_Vfmsubadd132ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040023AA RID: 9130
		EVEX_Vfmsubadd132ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x040023AB RID: 9131
		EVEX_Vfmsubadd132pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x040023AC RID: 9132
		EVEX_Vfmsubadd132pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x040023AD RID: 9133
		EVEX_Vfmsubadd132pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x040023AE RID: 9134
		VEX_Vfmadd132ps_xmm_xmm_xmmm128,
		// Token: 0x040023AF RID: 9135
		VEX_Vfmadd132ps_ymm_ymm_ymmm256,
		// Token: 0x040023B0 RID: 9136
		VEX_Vfmadd132pd_xmm_xmm_xmmm128,
		// Token: 0x040023B1 RID: 9137
		VEX_Vfmadd132pd_ymm_ymm_ymmm256,
		// Token: 0x040023B2 RID: 9138
		EVEX_Vfmadd132ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040023B3 RID: 9139
		EVEX_Vfmadd132ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040023B4 RID: 9140
		EVEX_Vfmadd132ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x040023B5 RID: 9141
		EVEX_Vfmadd132pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x040023B6 RID: 9142
		EVEX_Vfmadd132pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x040023B7 RID: 9143
		EVEX_Vfmadd132pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x040023B8 RID: 9144
		VEX_Vfmadd132ss_xmm_xmm_xmmm32,
		// Token: 0x040023B9 RID: 9145
		VEX_Vfmadd132sd_xmm_xmm_xmmm64,
		// Token: 0x040023BA RID: 9146
		EVEX_Vfmadd132ss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x040023BB RID: 9147
		EVEX_Vfmadd132sd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x040023BC RID: 9148
		VEX_Vfmsub132ps_xmm_xmm_xmmm128,
		// Token: 0x040023BD RID: 9149
		VEX_Vfmsub132ps_ymm_ymm_ymmm256,
		// Token: 0x040023BE RID: 9150
		VEX_Vfmsub132pd_xmm_xmm_xmmm128,
		// Token: 0x040023BF RID: 9151
		VEX_Vfmsub132pd_ymm_ymm_ymmm256,
		// Token: 0x040023C0 RID: 9152
		EVEX_Vfmsub132ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040023C1 RID: 9153
		EVEX_Vfmsub132ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040023C2 RID: 9154
		EVEX_Vfmsub132ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x040023C3 RID: 9155
		EVEX_Vfmsub132pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x040023C4 RID: 9156
		EVEX_Vfmsub132pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x040023C5 RID: 9157
		EVEX_Vfmsub132pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x040023C6 RID: 9158
		EVEX_V4fmaddps_zmm_k1z_zmmp3_m128,
		// Token: 0x040023C7 RID: 9159
		VEX_Vfmsub132ss_xmm_xmm_xmmm32,
		// Token: 0x040023C8 RID: 9160
		VEX_Vfmsub132sd_xmm_xmm_xmmm64,
		// Token: 0x040023C9 RID: 9161
		EVEX_Vfmsub132ss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x040023CA RID: 9162
		EVEX_Vfmsub132sd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x040023CB RID: 9163
		EVEX_V4fmaddss_xmm_k1z_xmmp3_m128,
		// Token: 0x040023CC RID: 9164
		VEX_Vfnmadd132ps_xmm_xmm_xmmm128,
		// Token: 0x040023CD RID: 9165
		VEX_Vfnmadd132ps_ymm_ymm_ymmm256,
		// Token: 0x040023CE RID: 9166
		VEX_Vfnmadd132pd_xmm_xmm_xmmm128,
		// Token: 0x040023CF RID: 9167
		VEX_Vfnmadd132pd_ymm_ymm_ymmm256,
		// Token: 0x040023D0 RID: 9168
		EVEX_Vfnmadd132ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040023D1 RID: 9169
		EVEX_Vfnmadd132ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040023D2 RID: 9170
		EVEX_Vfnmadd132ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x040023D3 RID: 9171
		EVEX_Vfnmadd132pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x040023D4 RID: 9172
		EVEX_Vfnmadd132pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x040023D5 RID: 9173
		EVEX_Vfnmadd132pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x040023D6 RID: 9174
		VEX_Vfnmadd132ss_xmm_xmm_xmmm32,
		// Token: 0x040023D7 RID: 9175
		VEX_Vfnmadd132sd_xmm_xmm_xmmm64,
		// Token: 0x040023D8 RID: 9176
		EVEX_Vfnmadd132ss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x040023D9 RID: 9177
		EVEX_Vfnmadd132sd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x040023DA RID: 9178
		VEX_Vfnmsub132ps_xmm_xmm_xmmm128,
		// Token: 0x040023DB RID: 9179
		VEX_Vfnmsub132ps_ymm_ymm_ymmm256,
		// Token: 0x040023DC RID: 9180
		VEX_Vfnmsub132pd_xmm_xmm_xmmm128,
		// Token: 0x040023DD RID: 9181
		VEX_Vfnmsub132pd_ymm_ymm_ymmm256,
		// Token: 0x040023DE RID: 9182
		EVEX_Vfnmsub132ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040023DF RID: 9183
		EVEX_Vfnmsub132ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040023E0 RID: 9184
		EVEX_Vfnmsub132ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x040023E1 RID: 9185
		EVEX_Vfnmsub132pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x040023E2 RID: 9186
		EVEX_Vfnmsub132pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x040023E3 RID: 9187
		EVEX_Vfnmsub132pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x040023E4 RID: 9188
		VEX_Vfnmsub132ss_xmm_xmm_xmmm32,
		// Token: 0x040023E5 RID: 9189
		VEX_Vfnmsub132sd_xmm_xmm_xmmm64,
		// Token: 0x040023E6 RID: 9190
		EVEX_Vfnmsub132ss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x040023E7 RID: 9191
		EVEX_Vfnmsub132sd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x040023E8 RID: 9192
		EVEX_Vpscatterdd_vm32x_k1_xmm,
		// Token: 0x040023E9 RID: 9193
		EVEX_Vpscatterdd_vm32y_k1_ymm,
		// Token: 0x040023EA RID: 9194
		EVEX_Vpscatterdd_vm32z_k1_zmm,
		// Token: 0x040023EB RID: 9195
		EVEX_Vpscatterdq_vm32x_k1_xmm,
		// Token: 0x040023EC RID: 9196
		EVEX_Vpscatterdq_vm32x_k1_ymm,
		// Token: 0x040023ED RID: 9197
		EVEX_Vpscatterdq_vm32y_k1_zmm,
		// Token: 0x040023EE RID: 9198
		EVEX_Vpscatterqd_vm64x_k1_xmm,
		// Token: 0x040023EF RID: 9199
		EVEX_Vpscatterqd_vm64y_k1_xmm,
		// Token: 0x040023F0 RID: 9200
		EVEX_Vpscatterqd_vm64z_k1_ymm,
		// Token: 0x040023F1 RID: 9201
		EVEX_Vpscatterqq_vm64x_k1_xmm,
		// Token: 0x040023F2 RID: 9202
		EVEX_Vpscatterqq_vm64y_k1_ymm,
		// Token: 0x040023F3 RID: 9203
		EVEX_Vpscatterqq_vm64z_k1_zmm,
		// Token: 0x040023F4 RID: 9204
		EVEX_Vscatterdps_vm32x_k1_xmm,
		// Token: 0x040023F5 RID: 9205
		EVEX_Vscatterdps_vm32y_k1_ymm,
		// Token: 0x040023F6 RID: 9206
		EVEX_Vscatterdps_vm32z_k1_zmm,
		// Token: 0x040023F7 RID: 9207
		EVEX_Vscatterdpd_vm32x_k1_xmm,
		// Token: 0x040023F8 RID: 9208
		EVEX_Vscatterdpd_vm32x_k1_ymm,
		// Token: 0x040023F9 RID: 9209
		EVEX_Vscatterdpd_vm32y_k1_zmm,
		// Token: 0x040023FA RID: 9210
		EVEX_Vscatterqps_vm64x_k1_xmm,
		// Token: 0x040023FB RID: 9211
		EVEX_Vscatterqps_vm64y_k1_xmm,
		// Token: 0x040023FC RID: 9212
		EVEX_Vscatterqps_vm64z_k1_ymm,
		// Token: 0x040023FD RID: 9213
		EVEX_Vscatterqpd_vm64x_k1_xmm,
		// Token: 0x040023FE RID: 9214
		EVEX_Vscatterqpd_vm64y_k1_ymm,
		// Token: 0x040023FF RID: 9215
		EVEX_Vscatterqpd_vm64z_k1_zmm,
		// Token: 0x04002400 RID: 9216
		VEX_Vfmaddsub213ps_xmm_xmm_xmmm128,
		// Token: 0x04002401 RID: 9217
		VEX_Vfmaddsub213ps_ymm_ymm_ymmm256,
		// Token: 0x04002402 RID: 9218
		VEX_Vfmaddsub213pd_xmm_xmm_xmmm128,
		// Token: 0x04002403 RID: 9219
		VEX_Vfmaddsub213pd_ymm_ymm_ymmm256,
		// Token: 0x04002404 RID: 9220
		EVEX_Vfmaddsub213ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002405 RID: 9221
		EVEX_Vfmaddsub213ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002406 RID: 9222
		EVEX_Vfmaddsub213ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x04002407 RID: 9223
		EVEX_Vfmaddsub213pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002408 RID: 9224
		EVEX_Vfmaddsub213pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002409 RID: 9225
		EVEX_Vfmaddsub213pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x0400240A RID: 9226
		VEX_Vfmsubadd213ps_xmm_xmm_xmmm128,
		// Token: 0x0400240B RID: 9227
		VEX_Vfmsubadd213ps_ymm_ymm_ymmm256,
		// Token: 0x0400240C RID: 9228
		VEX_Vfmsubadd213pd_xmm_xmm_xmmm128,
		// Token: 0x0400240D RID: 9229
		VEX_Vfmsubadd213pd_ymm_ymm_ymmm256,
		// Token: 0x0400240E RID: 9230
		EVEX_Vfmsubadd213ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x0400240F RID: 9231
		EVEX_Vfmsubadd213ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002410 RID: 9232
		EVEX_Vfmsubadd213ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x04002411 RID: 9233
		EVEX_Vfmsubadd213pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002412 RID: 9234
		EVEX_Vfmsubadd213pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002413 RID: 9235
		EVEX_Vfmsubadd213pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x04002414 RID: 9236
		VEX_Vfmadd213ps_xmm_xmm_xmmm128,
		// Token: 0x04002415 RID: 9237
		VEX_Vfmadd213ps_ymm_ymm_ymmm256,
		// Token: 0x04002416 RID: 9238
		VEX_Vfmadd213pd_xmm_xmm_xmmm128,
		// Token: 0x04002417 RID: 9239
		VEX_Vfmadd213pd_ymm_ymm_ymmm256,
		// Token: 0x04002418 RID: 9240
		EVEX_Vfmadd213ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002419 RID: 9241
		EVEX_Vfmadd213ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x0400241A RID: 9242
		EVEX_Vfmadd213ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x0400241B RID: 9243
		EVEX_Vfmadd213pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x0400241C RID: 9244
		EVEX_Vfmadd213pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x0400241D RID: 9245
		EVEX_Vfmadd213pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x0400241E RID: 9246
		VEX_Vfmadd213ss_xmm_xmm_xmmm32,
		// Token: 0x0400241F RID: 9247
		VEX_Vfmadd213sd_xmm_xmm_xmmm64,
		// Token: 0x04002420 RID: 9248
		EVEX_Vfmadd213ss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x04002421 RID: 9249
		EVEX_Vfmadd213sd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x04002422 RID: 9250
		VEX_Vfmsub213ps_xmm_xmm_xmmm128,
		// Token: 0x04002423 RID: 9251
		VEX_Vfmsub213ps_ymm_ymm_ymmm256,
		// Token: 0x04002424 RID: 9252
		VEX_Vfmsub213pd_xmm_xmm_xmmm128,
		// Token: 0x04002425 RID: 9253
		VEX_Vfmsub213pd_ymm_ymm_ymmm256,
		// Token: 0x04002426 RID: 9254
		EVEX_Vfmsub213ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002427 RID: 9255
		EVEX_Vfmsub213ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002428 RID: 9256
		EVEX_Vfmsub213ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x04002429 RID: 9257
		EVEX_Vfmsub213pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x0400242A RID: 9258
		EVEX_Vfmsub213pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x0400242B RID: 9259
		EVEX_Vfmsub213pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x0400242C RID: 9260
		EVEX_V4fnmaddps_zmm_k1z_zmmp3_m128,
		// Token: 0x0400242D RID: 9261
		VEX_Vfmsub213ss_xmm_xmm_xmmm32,
		// Token: 0x0400242E RID: 9262
		VEX_Vfmsub213sd_xmm_xmm_xmmm64,
		// Token: 0x0400242F RID: 9263
		EVEX_Vfmsub213ss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x04002430 RID: 9264
		EVEX_Vfmsub213sd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x04002431 RID: 9265
		EVEX_V4fnmaddss_xmm_k1z_xmmp3_m128,
		// Token: 0x04002432 RID: 9266
		VEX_Vfnmadd213ps_xmm_xmm_xmmm128,
		// Token: 0x04002433 RID: 9267
		VEX_Vfnmadd213ps_ymm_ymm_ymmm256,
		// Token: 0x04002434 RID: 9268
		VEX_Vfnmadd213pd_xmm_xmm_xmmm128,
		// Token: 0x04002435 RID: 9269
		VEX_Vfnmadd213pd_ymm_ymm_ymmm256,
		// Token: 0x04002436 RID: 9270
		EVEX_Vfnmadd213ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002437 RID: 9271
		EVEX_Vfnmadd213ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002438 RID: 9272
		EVEX_Vfnmadd213ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x04002439 RID: 9273
		EVEX_Vfnmadd213pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x0400243A RID: 9274
		EVEX_Vfnmadd213pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x0400243B RID: 9275
		EVEX_Vfnmadd213pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x0400243C RID: 9276
		VEX_Vfnmadd213ss_xmm_xmm_xmmm32,
		// Token: 0x0400243D RID: 9277
		VEX_Vfnmadd213sd_xmm_xmm_xmmm64,
		// Token: 0x0400243E RID: 9278
		EVEX_Vfnmadd213ss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x0400243F RID: 9279
		EVEX_Vfnmadd213sd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x04002440 RID: 9280
		VEX_Vfnmsub213ps_xmm_xmm_xmmm128,
		// Token: 0x04002441 RID: 9281
		VEX_Vfnmsub213ps_ymm_ymm_ymmm256,
		// Token: 0x04002442 RID: 9282
		VEX_Vfnmsub213pd_xmm_xmm_xmmm128,
		// Token: 0x04002443 RID: 9283
		VEX_Vfnmsub213pd_ymm_ymm_ymmm256,
		// Token: 0x04002444 RID: 9284
		EVEX_Vfnmsub213ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002445 RID: 9285
		EVEX_Vfnmsub213ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002446 RID: 9286
		EVEX_Vfnmsub213ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x04002447 RID: 9287
		EVEX_Vfnmsub213pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002448 RID: 9288
		EVEX_Vfnmsub213pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002449 RID: 9289
		EVEX_Vfnmsub213pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x0400244A RID: 9290
		VEX_Vfnmsub213ss_xmm_xmm_xmmm32,
		// Token: 0x0400244B RID: 9291
		VEX_Vfnmsub213sd_xmm_xmm_xmmm64,
		// Token: 0x0400244C RID: 9292
		EVEX_Vfnmsub213ss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x0400244D RID: 9293
		EVEX_Vfnmsub213sd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x0400244E RID: 9294
		EVEX_Vpmadd52luq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x0400244F RID: 9295
		EVEX_Vpmadd52luq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002450 RID: 9296
		EVEX_Vpmadd52luq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002451 RID: 9297
		EVEX_Vpmadd52huq_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002452 RID: 9298
		EVEX_Vpmadd52huq_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002453 RID: 9299
		EVEX_Vpmadd52huq_zmm_k1z_zmm_zmmm512b64,
		// Token: 0x04002454 RID: 9300
		VEX_Vfmaddsub231ps_xmm_xmm_xmmm128,
		// Token: 0x04002455 RID: 9301
		VEX_Vfmaddsub231ps_ymm_ymm_ymmm256,
		// Token: 0x04002456 RID: 9302
		VEX_Vfmaddsub231pd_xmm_xmm_xmmm128,
		// Token: 0x04002457 RID: 9303
		VEX_Vfmaddsub231pd_ymm_ymm_ymmm256,
		// Token: 0x04002458 RID: 9304
		EVEX_Vfmaddsub231ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002459 RID: 9305
		EVEX_Vfmaddsub231ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x0400245A RID: 9306
		EVEX_Vfmaddsub231ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x0400245B RID: 9307
		EVEX_Vfmaddsub231pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x0400245C RID: 9308
		EVEX_Vfmaddsub231pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x0400245D RID: 9309
		EVEX_Vfmaddsub231pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x0400245E RID: 9310
		VEX_Vfmsubadd231ps_xmm_xmm_xmmm128,
		// Token: 0x0400245F RID: 9311
		VEX_Vfmsubadd231ps_ymm_ymm_ymmm256,
		// Token: 0x04002460 RID: 9312
		VEX_Vfmsubadd231pd_xmm_xmm_xmmm128,
		// Token: 0x04002461 RID: 9313
		VEX_Vfmsubadd231pd_ymm_ymm_ymmm256,
		// Token: 0x04002462 RID: 9314
		EVEX_Vfmsubadd231ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002463 RID: 9315
		EVEX_Vfmsubadd231ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002464 RID: 9316
		EVEX_Vfmsubadd231ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x04002465 RID: 9317
		EVEX_Vfmsubadd231pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002466 RID: 9318
		EVEX_Vfmsubadd231pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002467 RID: 9319
		EVEX_Vfmsubadd231pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x04002468 RID: 9320
		VEX_Vfmadd231ps_xmm_xmm_xmmm128,
		// Token: 0x04002469 RID: 9321
		VEX_Vfmadd231ps_ymm_ymm_ymmm256,
		// Token: 0x0400246A RID: 9322
		VEX_Vfmadd231pd_xmm_xmm_xmmm128,
		// Token: 0x0400246B RID: 9323
		VEX_Vfmadd231pd_ymm_ymm_ymmm256,
		// Token: 0x0400246C RID: 9324
		EVEX_Vfmadd231ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x0400246D RID: 9325
		EVEX_Vfmadd231ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x0400246E RID: 9326
		EVEX_Vfmadd231ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x0400246F RID: 9327
		EVEX_Vfmadd231pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x04002470 RID: 9328
		EVEX_Vfmadd231pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x04002471 RID: 9329
		EVEX_Vfmadd231pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x04002472 RID: 9330
		VEX_Vfmadd231ss_xmm_xmm_xmmm32,
		// Token: 0x04002473 RID: 9331
		VEX_Vfmadd231sd_xmm_xmm_xmmm64,
		// Token: 0x04002474 RID: 9332
		EVEX_Vfmadd231ss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x04002475 RID: 9333
		EVEX_Vfmadd231sd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x04002476 RID: 9334
		VEX_Vfmsub231ps_xmm_xmm_xmmm128,
		// Token: 0x04002477 RID: 9335
		VEX_Vfmsub231ps_ymm_ymm_ymmm256,
		// Token: 0x04002478 RID: 9336
		VEX_Vfmsub231pd_xmm_xmm_xmmm128,
		// Token: 0x04002479 RID: 9337
		VEX_Vfmsub231pd_ymm_ymm_ymmm256,
		// Token: 0x0400247A RID: 9338
		EVEX_Vfmsub231ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x0400247B RID: 9339
		EVEX_Vfmsub231ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x0400247C RID: 9340
		EVEX_Vfmsub231ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x0400247D RID: 9341
		EVEX_Vfmsub231pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x0400247E RID: 9342
		EVEX_Vfmsub231pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x0400247F RID: 9343
		EVEX_Vfmsub231pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x04002480 RID: 9344
		VEX_Vfmsub231ss_xmm_xmm_xmmm32,
		// Token: 0x04002481 RID: 9345
		VEX_Vfmsub231sd_xmm_xmm_xmmm64,
		// Token: 0x04002482 RID: 9346
		EVEX_Vfmsub231ss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x04002483 RID: 9347
		EVEX_Vfmsub231sd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x04002484 RID: 9348
		VEX_Vfnmadd231ps_xmm_xmm_xmmm128,
		// Token: 0x04002485 RID: 9349
		VEX_Vfnmadd231ps_ymm_ymm_ymmm256,
		// Token: 0x04002486 RID: 9350
		VEX_Vfnmadd231pd_xmm_xmm_xmmm128,
		// Token: 0x04002487 RID: 9351
		VEX_Vfnmadd231pd_ymm_ymm_ymmm256,
		// Token: 0x04002488 RID: 9352
		EVEX_Vfnmadd231ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002489 RID: 9353
		EVEX_Vfnmadd231ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x0400248A RID: 9354
		EVEX_Vfnmadd231ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x0400248B RID: 9355
		EVEX_Vfnmadd231pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x0400248C RID: 9356
		EVEX_Vfnmadd231pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x0400248D RID: 9357
		EVEX_Vfnmadd231pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x0400248E RID: 9358
		VEX_Vfnmadd231ss_xmm_xmm_xmmm32,
		// Token: 0x0400248F RID: 9359
		VEX_Vfnmadd231sd_xmm_xmm_xmmm64,
		// Token: 0x04002490 RID: 9360
		EVEX_Vfnmadd231ss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x04002491 RID: 9361
		EVEX_Vfnmadd231sd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x04002492 RID: 9362
		VEX_Vfnmsub231ps_xmm_xmm_xmmm128,
		// Token: 0x04002493 RID: 9363
		VEX_Vfnmsub231ps_ymm_ymm_ymmm256,
		// Token: 0x04002494 RID: 9364
		VEX_Vfnmsub231pd_xmm_xmm_xmmm128,
		// Token: 0x04002495 RID: 9365
		VEX_Vfnmsub231pd_ymm_ymm_ymmm256,
		// Token: 0x04002496 RID: 9366
		EVEX_Vfnmsub231ps_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x04002497 RID: 9367
		EVEX_Vfnmsub231ps_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x04002498 RID: 9368
		EVEX_Vfnmsub231ps_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x04002499 RID: 9369
		EVEX_Vfnmsub231pd_xmm_k1z_xmm_xmmm128b64,
		// Token: 0x0400249A RID: 9370
		EVEX_Vfnmsub231pd_ymm_k1z_ymm_ymmm256b64,
		// Token: 0x0400249B RID: 9371
		EVEX_Vfnmsub231pd_zmm_k1z_zmm_zmmm512b64_er,
		// Token: 0x0400249C RID: 9372
		VEX_Vfnmsub231ss_xmm_xmm_xmmm32,
		// Token: 0x0400249D RID: 9373
		VEX_Vfnmsub231sd_xmm_xmm_xmmm64,
		// Token: 0x0400249E RID: 9374
		EVEX_Vfnmsub231ss_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x0400249F RID: 9375
		EVEX_Vfnmsub231sd_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x040024A0 RID: 9376
		EVEX_Vpconflictd_xmm_k1z_xmmm128b32,
		// Token: 0x040024A1 RID: 9377
		EVEX_Vpconflictd_ymm_k1z_ymmm256b32,
		// Token: 0x040024A2 RID: 9378
		EVEX_Vpconflictd_zmm_k1z_zmmm512b32,
		// Token: 0x040024A3 RID: 9379
		EVEX_Vpconflictq_xmm_k1z_xmmm128b64,
		// Token: 0x040024A4 RID: 9380
		EVEX_Vpconflictq_ymm_k1z_ymmm256b64,
		// Token: 0x040024A5 RID: 9381
		EVEX_Vpconflictq_zmm_k1z_zmmm512b64,
		// Token: 0x040024A6 RID: 9382
		EVEX_Vgatherpf0dps_vm32z_k1,
		// Token: 0x040024A7 RID: 9383
		EVEX_Vgatherpf0dpd_vm32y_k1,
		// Token: 0x040024A8 RID: 9384
		EVEX_Vgatherpf1dps_vm32z_k1,
		// Token: 0x040024A9 RID: 9385
		EVEX_Vgatherpf1dpd_vm32y_k1,
		// Token: 0x040024AA RID: 9386
		EVEX_Vscatterpf0dps_vm32z_k1,
		// Token: 0x040024AB RID: 9387
		EVEX_Vscatterpf0dpd_vm32y_k1,
		// Token: 0x040024AC RID: 9388
		EVEX_Vscatterpf1dps_vm32z_k1,
		// Token: 0x040024AD RID: 9389
		EVEX_Vscatterpf1dpd_vm32y_k1,
		// Token: 0x040024AE RID: 9390
		EVEX_Vgatherpf0qps_vm64z_k1,
		// Token: 0x040024AF RID: 9391
		EVEX_Vgatherpf0qpd_vm64z_k1,
		// Token: 0x040024B0 RID: 9392
		EVEX_Vgatherpf1qps_vm64z_k1,
		// Token: 0x040024B1 RID: 9393
		EVEX_Vgatherpf1qpd_vm64z_k1,
		// Token: 0x040024B2 RID: 9394
		EVEX_Vscatterpf0qps_vm64z_k1,
		// Token: 0x040024B3 RID: 9395
		EVEX_Vscatterpf0qpd_vm64z_k1,
		// Token: 0x040024B4 RID: 9396
		EVEX_Vscatterpf1qps_vm64z_k1,
		// Token: 0x040024B5 RID: 9397
		EVEX_Vscatterpf1qpd_vm64z_k1,
		// Token: 0x040024B6 RID: 9398
		Sha1nexte_xmm_xmmm128,
		// Token: 0x040024B7 RID: 9399
		EVEX_Vexp2ps_zmm_k1z_zmmm512b32_sae,
		// Token: 0x040024B8 RID: 9400
		EVEX_Vexp2pd_zmm_k1z_zmmm512b64_sae,
		// Token: 0x040024B9 RID: 9401
		Sha1msg1_xmm_xmmm128,
		// Token: 0x040024BA RID: 9402
		Sha1msg2_xmm_xmmm128,
		// Token: 0x040024BB RID: 9403
		EVEX_Vrcp28ps_zmm_k1z_zmmm512b32_sae,
		// Token: 0x040024BC RID: 9404
		EVEX_Vrcp28pd_zmm_k1z_zmmm512b64_sae,
		// Token: 0x040024BD RID: 9405
		Sha256rnds2_xmm_xmmm128,
		// Token: 0x040024BE RID: 9406
		EVEX_Vrcp28ss_xmm_k1z_xmm_xmmm32_sae,
		// Token: 0x040024BF RID: 9407
		EVEX_Vrcp28sd_xmm_k1z_xmm_xmmm64_sae,
		// Token: 0x040024C0 RID: 9408
		Sha256msg1_xmm_xmmm128,
		// Token: 0x040024C1 RID: 9409
		EVEX_Vrsqrt28ps_zmm_k1z_zmmm512b32_sae,
		// Token: 0x040024C2 RID: 9410
		EVEX_Vrsqrt28pd_zmm_k1z_zmmm512b64_sae,
		// Token: 0x040024C3 RID: 9411
		Sha256msg2_xmm_xmmm128,
		// Token: 0x040024C4 RID: 9412
		EVEX_Vrsqrt28ss_xmm_k1z_xmm_xmmm32_sae,
		// Token: 0x040024C5 RID: 9413
		EVEX_Vrsqrt28sd_xmm_k1z_xmm_xmmm64_sae,
		// Token: 0x040024C6 RID: 9414
		Gf2p8mulb_xmm_xmmm128,
		// Token: 0x040024C7 RID: 9415
		VEX_Vgf2p8mulb_xmm_xmm_xmmm128,
		// Token: 0x040024C8 RID: 9416
		VEX_Vgf2p8mulb_ymm_ymm_ymmm256,
		// Token: 0x040024C9 RID: 9417
		EVEX_Vgf2p8mulb_xmm_k1z_xmm_xmmm128,
		// Token: 0x040024CA RID: 9418
		EVEX_Vgf2p8mulb_ymm_k1z_ymm_ymmm256,
		// Token: 0x040024CB RID: 9419
		EVEX_Vgf2p8mulb_zmm_k1z_zmm_zmmm512,
		// Token: 0x040024CC RID: 9420
		Aesimc_xmm_xmmm128,
		// Token: 0x040024CD RID: 9421
		VEX_Vaesimc_xmm_xmmm128,
		// Token: 0x040024CE RID: 9422
		Aesenc_xmm_xmmm128,
		// Token: 0x040024CF RID: 9423
		VEX_Vaesenc_xmm_xmm_xmmm128,
		// Token: 0x040024D0 RID: 9424
		VEX_Vaesenc_ymm_ymm_ymmm256,
		// Token: 0x040024D1 RID: 9425
		EVEX_Vaesenc_xmm_xmm_xmmm128,
		// Token: 0x040024D2 RID: 9426
		EVEX_Vaesenc_ymm_ymm_ymmm256,
		// Token: 0x040024D3 RID: 9427
		EVEX_Vaesenc_zmm_zmm_zmmm512,
		// Token: 0x040024D4 RID: 9428
		Aesenclast_xmm_xmmm128,
		// Token: 0x040024D5 RID: 9429
		VEX_Vaesenclast_xmm_xmm_xmmm128,
		// Token: 0x040024D6 RID: 9430
		VEX_Vaesenclast_ymm_ymm_ymmm256,
		// Token: 0x040024D7 RID: 9431
		EVEX_Vaesenclast_xmm_xmm_xmmm128,
		// Token: 0x040024D8 RID: 9432
		EVEX_Vaesenclast_ymm_ymm_ymmm256,
		// Token: 0x040024D9 RID: 9433
		EVEX_Vaesenclast_zmm_zmm_zmmm512,
		// Token: 0x040024DA RID: 9434
		Aesdec_xmm_xmmm128,
		// Token: 0x040024DB RID: 9435
		VEX_Vaesdec_xmm_xmm_xmmm128,
		// Token: 0x040024DC RID: 9436
		VEX_Vaesdec_ymm_ymm_ymmm256,
		// Token: 0x040024DD RID: 9437
		EVEX_Vaesdec_xmm_xmm_xmmm128,
		// Token: 0x040024DE RID: 9438
		EVEX_Vaesdec_ymm_ymm_ymmm256,
		// Token: 0x040024DF RID: 9439
		EVEX_Vaesdec_zmm_zmm_zmmm512,
		// Token: 0x040024E0 RID: 9440
		Aesdeclast_xmm_xmmm128,
		// Token: 0x040024E1 RID: 9441
		VEX_Vaesdeclast_xmm_xmm_xmmm128,
		// Token: 0x040024E2 RID: 9442
		VEX_Vaesdeclast_ymm_ymm_ymmm256,
		// Token: 0x040024E3 RID: 9443
		EVEX_Vaesdeclast_xmm_xmm_xmmm128,
		// Token: 0x040024E4 RID: 9444
		EVEX_Vaesdeclast_ymm_ymm_ymmm256,
		// Token: 0x040024E5 RID: 9445
		EVEX_Vaesdeclast_zmm_zmm_zmmm512,
		// Token: 0x040024E6 RID: 9446
		Movbe_r16_m16,
		// Token: 0x040024E7 RID: 9447
		Movbe_r32_m32,
		// Token: 0x040024E8 RID: 9448
		Movbe_r64_m64,
		// Token: 0x040024E9 RID: 9449
		Crc32_r32_rm8,
		// Token: 0x040024EA RID: 9450
		Crc32_r64_rm8,
		// Token: 0x040024EB RID: 9451
		Movbe_m16_r16,
		// Token: 0x040024EC RID: 9452
		Movbe_m32_r32,
		// Token: 0x040024ED RID: 9453
		Movbe_m64_r64,
		// Token: 0x040024EE RID: 9454
		Crc32_r32_rm16,
		// Token: 0x040024EF RID: 9455
		Crc32_r32_rm32,
		// Token: 0x040024F0 RID: 9456
		Crc32_r64_rm64,
		// Token: 0x040024F1 RID: 9457
		VEX_Andn_r32_r32_rm32,
		// Token: 0x040024F2 RID: 9458
		VEX_Andn_r64_r64_rm64,
		// Token: 0x040024F3 RID: 9459
		VEX_Blsr_r32_rm32,
		// Token: 0x040024F4 RID: 9460
		VEX_Blsr_r64_rm64,
		// Token: 0x040024F5 RID: 9461
		VEX_Blsmsk_r32_rm32,
		// Token: 0x040024F6 RID: 9462
		VEX_Blsmsk_r64_rm64,
		// Token: 0x040024F7 RID: 9463
		VEX_Blsi_r32_rm32,
		// Token: 0x040024F8 RID: 9464
		VEX_Blsi_r64_rm64,
		// Token: 0x040024F9 RID: 9465
		VEX_Bzhi_r32_rm32_r32,
		// Token: 0x040024FA RID: 9466
		VEX_Bzhi_r64_rm64_r64,
		// Token: 0x040024FB RID: 9467
		Wrussd_m32_r32,
		// Token: 0x040024FC RID: 9468
		Wrussq_m64_r64,
		// Token: 0x040024FD RID: 9469
		VEX_Pext_r32_r32_rm32,
		// Token: 0x040024FE RID: 9470
		VEX_Pext_r64_r64_rm64,
		// Token: 0x040024FF RID: 9471
		VEX_Pdep_r32_r32_rm32,
		// Token: 0x04002500 RID: 9472
		VEX_Pdep_r64_r64_rm64,
		// Token: 0x04002501 RID: 9473
		Wrssd_m32_r32,
		// Token: 0x04002502 RID: 9474
		Wrssq_m64_r64,
		// Token: 0x04002503 RID: 9475
		Adcx_r32_rm32,
		// Token: 0x04002504 RID: 9476
		Adcx_r64_rm64,
		// Token: 0x04002505 RID: 9477
		Adox_r32_rm32,
		// Token: 0x04002506 RID: 9478
		Adox_r64_rm64,
		// Token: 0x04002507 RID: 9479
		VEX_Mulx_r32_r32_rm32,
		// Token: 0x04002508 RID: 9480
		VEX_Mulx_r64_r64_rm64,
		// Token: 0x04002509 RID: 9481
		VEX_Bextr_r32_rm32_r32,
		// Token: 0x0400250A RID: 9482
		VEX_Bextr_r64_rm64_r64,
		// Token: 0x0400250B RID: 9483
		VEX_Shlx_r32_rm32_r32,
		// Token: 0x0400250C RID: 9484
		VEX_Shlx_r64_rm64_r64,
		// Token: 0x0400250D RID: 9485
		VEX_Sarx_r32_rm32_r32,
		// Token: 0x0400250E RID: 9486
		VEX_Sarx_r64_rm64_r64,
		// Token: 0x0400250F RID: 9487
		VEX_Shrx_r32_rm32_r32,
		// Token: 0x04002510 RID: 9488
		VEX_Shrx_r64_rm64_r64,
		// Token: 0x04002511 RID: 9489
		Movdir64b_r16_m512,
		// Token: 0x04002512 RID: 9490
		Movdir64b_r32_m512,
		// Token: 0x04002513 RID: 9491
		Movdir64b_r64_m512,
		// Token: 0x04002514 RID: 9492
		Enqcmds_r16_m512,
		// Token: 0x04002515 RID: 9493
		Enqcmds_r32_m512,
		// Token: 0x04002516 RID: 9494
		Enqcmds_r64_m512,
		// Token: 0x04002517 RID: 9495
		Enqcmd_r16_m512,
		// Token: 0x04002518 RID: 9496
		Enqcmd_r32_m512,
		// Token: 0x04002519 RID: 9497
		Enqcmd_r64_m512,
		// Token: 0x0400251A RID: 9498
		Movdiri_m32_r32,
		// Token: 0x0400251B RID: 9499
		Movdiri_m64_r64,
		// Token: 0x0400251C RID: 9500
		VEX_Vpermq_ymm_ymmm256_imm8,
		// Token: 0x0400251D RID: 9501
		EVEX_Vpermq_ymm_k1z_ymmm256b64_imm8,
		// Token: 0x0400251E RID: 9502
		EVEX_Vpermq_zmm_k1z_zmmm512b64_imm8,
		// Token: 0x0400251F RID: 9503
		VEX_Vpermpd_ymm_ymmm256_imm8,
		// Token: 0x04002520 RID: 9504
		EVEX_Vpermpd_ymm_k1z_ymmm256b64_imm8,
		// Token: 0x04002521 RID: 9505
		EVEX_Vpermpd_zmm_k1z_zmmm512b64_imm8,
		// Token: 0x04002522 RID: 9506
		VEX_Vpblendd_xmm_xmm_xmmm128_imm8,
		// Token: 0x04002523 RID: 9507
		VEX_Vpblendd_ymm_ymm_ymmm256_imm8,
		// Token: 0x04002524 RID: 9508
		EVEX_Valignd_xmm_k1z_xmm_xmmm128b32_imm8,
		// Token: 0x04002525 RID: 9509
		EVEX_Valignd_ymm_k1z_ymm_ymmm256b32_imm8,
		// Token: 0x04002526 RID: 9510
		EVEX_Valignd_zmm_k1z_zmm_zmmm512b32_imm8,
		// Token: 0x04002527 RID: 9511
		EVEX_Valignq_xmm_k1z_xmm_xmmm128b64_imm8,
		// Token: 0x04002528 RID: 9512
		EVEX_Valignq_ymm_k1z_ymm_ymmm256b64_imm8,
		// Token: 0x04002529 RID: 9513
		EVEX_Valignq_zmm_k1z_zmm_zmmm512b64_imm8,
		// Token: 0x0400252A RID: 9514
		VEX_Vpermilps_xmm_xmmm128_imm8,
		// Token: 0x0400252B RID: 9515
		VEX_Vpermilps_ymm_ymmm256_imm8,
		// Token: 0x0400252C RID: 9516
		EVEX_Vpermilps_xmm_k1z_xmmm128b32_imm8,
		// Token: 0x0400252D RID: 9517
		EVEX_Vpermilps_ymm_k1z_ymmm256b32_imm8,
		// Token: 0x0400252E RID: 9518
		EVEX_Vpermilps_zmm_k1z_zmmm512b32_imm8,
		// Token: 0x0400252F RID: 9519
		VEX_Vpermilpd_xmm_xmmm128_imm8,
		// Token: 0x04002530 RID: 9520
		VEX_Vpermilpd_ymm_ymmm256_imm8,
		// Token: 0x04002531 RID: 9521
		EVEX_Vpermilpd_xmm_k1z_xmmm128b64_imm8,
		// Token: 0x04002532 RID: 9522
		EVEX_Vpermilpd_ymm_k1z_ymmm256b64_imm8,
		// Token: 0x04002533 RID: 9523
		EVEX_Vpermilpd_zmm_k1z_zmmm512b64_imm8,
		// Token: 0x04002534 RID: 9524
		VEX_Vperm2f128_ymm_ymm_ymmm256_imm8,
		// Token: 0x04002535 RID: 9525
		Roundps_xmm_xmmm128_imm8,
		// Token: 0x04002536 RID: 9526
		VEX_Vroundps_xmm_xmmm128_imm8,
		// Token: 0x04002537 RID: 9527
		VEX_Vroundps_ymm_ymmm256_imm8,
		// Token: 0x04002538 RID: 9528
		EVEX_Vrndscaleps_xmm_k1z_xmmm128b32_imm8,
		// Token: 0x04002539 RID: 9529
		EVEX_Vrndscaleps_ymm_k1z_ymmm256b32_imm8,
		// Token: 0x0400253A RID: 9530
		EVEX_Vrndscaleps_zmm_k1z_zmmm512b32_imm8_sae,
		// Token: 0x0400253B RID: 9531
		Roundpd_xmm_xmmm128_imm8,
		// Token: 0x0400253C RID: 9532
		VEX_Vroundpd_xmm_xmmm128_imm8,
		// Token: 0x0400253D RID: 9533
		VEX_Vroundpd_ymm_ymmm256_imm8,
		// Token: 0x0400253E RID: 9534
		EVEX_Vrndscalepd_xmm_k1z_xmmm128b64_imm8,
		// Token: 0x0400253F RID: 9535
		EVEX_Vrndscalepd_ymm_k1z_ymmm256b64_imm8,
		// Token: 0x04002540 RID: 9536
		EVEX_Vrndscalepd_zmm_k1z_zmmm512b64_imm8_sae,
		// Token: 0x04002541 RID: 9537
		Roundss_xmm_xmmm32_imm8,
		// Token: 0x04002542 RID: 9538
		VEX_Vroundss_xmm_xmm_xmmm32_imm8,
		// Token: 0x04002543 RID: 9539
		EVEX_Vrndscaless_xmm_k1z_xmm_xmmm32_imm8_sae,
		// Token: 0x04002544 RID: 9540
		Roundsd_xmm_xmmm64_imm8,
		// Token: 0x04002545 RID: 9541
		VEX_Vroundsd_xmm_xmm_xmmm64_imm8,
		// Token: 0x04002546 RID: 9542
		EVEX_Vrndscalesd_xmm_k1z_xmm_xmmm64_imm8_sae,
		// Token: 0x04002547 RID: 9543
		Blendps_xmm_xmmm128_imm8,
		// Token: 0x04002548 RID: 9544
		VEX_Vblendps_xmm_xmm_xmmm128_imm8,
		// Token: 0x04002549 RID: 9545
		VEX_Vblendps_ymm_ymm_ymmm256_imm8,
		// Token: 0x0400254A RID: 9546
		Blendpd_xmm_xmmm128_imm8,
		// Token: 0x0400254B RID: 9547
		VEX_Vblendpd_xmm_xmm_xmmm128_imm8,
		// Token: 0x0400254C RID: 9548
		VEX_Vblendpd_ymm_ymm_ymmm256_imm8,
		// Token: 0x0400254D RID: 9549
		Pblendw_xmm_xmmm128_imm8,
		// Token: 0x0400254E RID: 9550
		VEX_Vpblendw_xmm_xmm_xmmm128_imm8,
		// Token: 0x0400254F RID: 9551
		VEX_Vpblendw_ymm_ymm_ymmm256_imm8,
		// Token: 0x04002550 RID: 9552
		Palignr_mm_mmm64_imm8,
		// Token: 0x04002551 RID: 9553
		Palignr_xmm_xmmm128_imm8,
		// Token: 0x04002552 RID: 9554
		VEX_Vpalignr_xmm_xmm_xmmm128_imm8,
		// Token: 0x04002553 RID: 9555
		VEX_Vpalignr_ymm_ymm_ymmm256_imm8,
		// Token: 0x04002554 RID: 9556
		EVEX_Vpalignr_xmm_k1z_xmm_xmmm128_imm8,
		// Token: 0x04002555 RID: 9557
		EVEX_Vpalignr_ymm_k1z_ymm_ymmm256_imm8,
		// Token: 0x04002556 RID: 9558
		EVEX_Vpalignr_zmm_k1z_zmm_zmmm512_imm8,
		// Token: 0x04002557 RID: 9559
		Pextrb_r32m8_xmm_imm8,
		// Token: 0x04002558 RID: 9560
		Pextrb_r64m8_xmm_imm8,
		// Token: 0x04002559 RID: 9561
		VEX_Vpextrb_r32m8_xmm_imm8,
		// Token: 0x0400255A RID: 9562
		VEX_Vpextrb_r64m8_xmm_imm8,
		// Token: 0x0400255B RID: 9563
		EVEX_Vpextrb_r32m8_xmm_imm8,
		// Token: 0x0400255C RID: 9564
		EVEX_Vpextrb_r64m8_xmm_imm8,
		// Token: 0x0400255D RID: 9565
		Pextrw_r32m16_xmm_imm8,
		// Token: 0x0400255E RID: 9566
		Pextrw_r64m16_xmm_imm8,
		// Token: 0x0400255F RID: 9567
		VEX_Vpextrw_r32m16_xmm_imm8,
		// Token: 0x04002560 RID: 9568
		VEX_Vpextrw_r64m16_xmm_imm8,
		// Token: 0x04002561 RID: 9569
		EVEX_Vpextrw_r32m16_xmm_imm8,
		// Token: 0x04002562 RID: 9570
		EVEX_Vpextrw_r64m16_xmm_imm8,
		// Token: 0x04002563 RID: 9571
		Pextrd_rm32_xmm_imm8,
		// Token: 0x04002564 RID: 9572
		Pextrq_rm64_xmm_imm8,
		// Token: 0x04002565 RID: 9573
		VEX_Vpextrd_rm32_xmm_imm8,
		// Token: 0x04002566 RID: 9574
		VEX_Vpextrq_rm64_xmm_imm8,
		// Token: 0x04002567 RID: 9575
		EVEX_Vpextrd_rm32_xmm_imm8,
		// Token: 0x04002568 RID: 9576
		EVEX_Vpextrq_rm64_xmm_imm8,
		// Token: 0x04002569 RID: 9577
		Extractps_rm32_xmm_imm8,
		// Token: 0x0400256A RID: 9578
		Extractps_r64m32_xmm_imm8,
		// Token: 0x0400256B RID: 9579
		VEX_Vextractps_rm32_xmm_imm8,
		// Token: 0x0400256C RID: 9580
		VEX_Vextractps_r64m32_xmm_imm8,
		// Token: 0x0400256D RID: 9581
		EVEX_Vextractps_rm32_xmm_imm8,
		// Token: 0x0400256E RID: 9582
		EVEX_Vextractps_r64m32_xmm_imm8,
		// Token: 0x0400256F RID: 9583
		VEX_Vinsertf128_ymm_ymm_xmmm128_imm8,
		// Token: 0x04002570 RID: 9584
		EVEX_Vinsertf32x4_ymm_k1z_ymm_xmmm128_imm8,
		// Token: 0x04002571 RID: 9585
		EVEX_Vinsertf32x4_zmm_k1z_zmm_xmmm128_imm8,
		// Token: 0x04002572 RID: 9586
		EVEX_Vinsertf64x2_ymm_k1z_ymm_xmmm128_imm8,
		// Token: 0x04002573 RID: 9587
		EVEX_Vinsertf64x2_zmm_k1z_zmm_xmmm128_imm8,
		// Token: 0x04002574 RID: 9588
		VEX_Vextractf128_xmmm128_ymm_imm8,
		// Token: 0x04002575 RID: 9589
		EVEX_Vextractf32x4_xmmm128_k1z_ymm_imm8,
		// Token: 0x04002576 RID: 9590
		EVEX_Vextractf32x4_xmmm128_k1z_zmm_imm8,
		// Token: 0x04002577 RID: 9591
		EVEX_Vextractf64x2_xmmm128_k1z_ymm_imm8,
		// Token: 0x04002578 RID: 9592
		EVEX_Vextractf64x2_xmmm128_k1z_zmm_imm8,
		// Token: 0x04002579 RID: 9593
		EVEX_Vinsertf32x8_zmm_k1z_zmm_ymmm256_imm8,
		// Token: 0x0400257A RID: 9594
		EVEX_Vinsertf64x4_zmm_k1z_zmm_ymmm256_imm8,
		// Token: 0x0400257B RID: 9595
		EVEX_Vextractf32x8_ymmm256_k1z_zmm_imm8,
		// Token: 0x0400257C RID: 9596
		EVEX_Vextractf64x4_ymmm256_k1z_zmm_imm8,
		// Token: 0x0400257D RID: 9597
		VEX_Vcvtps2ph_xmmm64_xmm_imm8,
		// Token: 0x0400257E RID: 9598
		VEX_Vcvtps2ph_xmmm128_ymm_imm8,
		// Token: 0x0400257F RID: 9599
		EVEX_Vcvtps2ph_xmmm64_k1z_xmm_imm8,
		// Token: 0x04002580 RID: 9600
		EVEX_Vcvtps2ph_xmmm128_k1z_ymm_imm8,
		// Token: 0x04002581 RID: 9601
		EVEX_Vcvtps2ph_ymmm256_k1z_zmm_imm8_sae,
		// Token: 0x04002582 RID: 9602
		EVEX_Vpcmpud_kr_k1_xmm_xmmm128b32_imm8,
		// Token: 0x04002583 RID: 9603
		EVEX_Vpcmpud_kr_k1_ymm_ymmm256b32_imm8,
		// Token: 0x04002584 RID: 9604
		EVEX_Vpcmpud_kr_k1_zmm_zmmm512b32_imm8,
		// Token: 0x04002585 RID: 9605
		EVEX_Vpcmpuq_kr_k1_xmm_xmmm128b64_imm8,
		// Token: 0x04002586 RID: 9606
		EVEX_Vpcmpuq_kr_k1_ymm_ymmm256b64_imm8,
		// Token: 0x04002587 RID: 9607
		EVEX_Vpcmpuq_kr_k1_zmm_zmmm512b64_imm8,
		// Token: 0x04002588 RID: 9608
		EVEX_Vpcmpd_kr_k1_xmm_xmmm128b32_imm8,
		// Token: 0x04002589 RID: 9609
		EVEX_Vpcmpd_kr_k1_ymm_ymmm256b32_imm8,
		// Token: 0x0400258A RID: 9610
		EVEX_Vpcmpd_kr_k1_zmm_zmmm512b32_imm8,
		// Token: 0x0400258B RID: 9611
		EVEX_Vpcmpq_kr_k1_xmm_xmmm128b64_imm8,
		// Token: 0x0400258C RID: 9612
		EVEX_Vpcmpq_kr_k1_ymm_ymmm256b64_imm8,
		// Token: 0x0400258D RID: 9613
		EVEX_Vpcmpq_kr_k1_zmm_zmmm512b64_imm8,
		// Token: 0x0400258E RID: 9614
		Pinsrb_xmm_r32m8_imm8,
		// Token: 0x0400258F RID: 9615
		Pinsrb_xmm_r64m8_imm8,
		// Token: 0x04002590 RID: 9616
		VEX_Vpinsrb_xmm_xmm_r32m8_imm8,
		// Token: 0x04002591 RID: 9617
		VEX_Vpinsrb_xmm_xmm_r64m8_imm8,
		// Token: 0x04002592 RID: 9618
		EVEX_Vpinsrb_xmm_xmm_r32m8_imm8,
		// Token: 0x04002593 RID: 9619
		EVEX_Vpinsrb_xmm_xmm_r64m8_imm8,
		// Token: 0x04002594 RID: 9620
		Insertps_xmm_xmmm32_imm8,
		// Token: 0x04002595 RID: 9621
		VEX_Vinsertps_xmm_xmm_xmmm32_imm8,
		// Token: 0x04002596 RID: 9622
		EVEX_Vinsertps_xmm_xmm_xmmm32_imm8,
		// Token: 0x04002597 RID: 9623
		Pinsrd_xmm_rm32_imm8,
		// Token: 0x04002598 RID: 9624
		Pinsrq_xmm_rm64_imm8,
		// Token: 0x04002599 RID: 9625
		VEX_Vpinsrd_xmm_xmm_rm32_imm8,
		// Token: 0x0400259A RID: 9626
		VEX_Vpinsrq_xmm_xmm_rm64_imm8,
		// Token: 0x0400259B RID: 9627
		EVEX_Vpinsrd_xmm_xmm_rm32_imm8,
		// Token: 0x0400259C RID: 9628
		EVEX_Vpinsrq_xmm_xmm_rm64_imm8,
		// Token: 0x0400259D RID: 9629
		EVEX_Vshuff32x4_ymm_k1z_ymm_ymmm256b32_imm8,
		// Token: 0x0400259E RID: 9630
		EVEX_Vshuff32x4_zmm_k1z_zmm_zmmm512b32_imm8,
		// Token: 0x0400259F RID: 9631
		EVEX_Vshuff64x2_ymm_k1z_ymm_ymmm256b64_imm8,
		// Token: 0x040025A0 RID: 9632
		EVEX_Vshuff64x2_zmm_k1z_zmm_zmmm512b64_imm8,
		// Token: 0x040025A1 RID: 9633
		EVEX_Vpternlogd_xmm_k1z_xmm_xmmm128b32_imm8,
		// Token: 0x040025A2 RID: 9634
		EVEX_Vpternlogd_ymm_k1z_ymm_ymmm256b32_imm8,
		// Token: 0x040025A3 RID: 9635
		EVEX_Vpternlogd_zmm_k1z_zmm_zmmm512b32_imm8,
		// Token: 0x040025A4 RID: 9636
		EVEX_Vpternlogq_xmm_k1z_xmm_xmmm128b64_imm8,
		// Token: 0x040025A5 RID: 9637
		EVEX_Vpternlogq_ymm_k1z_ymm_ymmm256b64_imm8,
		// Token: 0x040025A6 RID: 9638
		EVEX_Vpternlogq_zmm_k1z_zmm_zmmm512b64_imm8,
		// Token: 0x040025A7 RID: 9639
		EVEX_Vgetmantps_xmm_k1z_xmmm128b32_imm8,
		// Token: 0x040025A8 RID: 9640
		EVEX_Vgetmantps_ymm_k1z_ymmm256b32_imm8,
		// Token: 0x040025A9 RID: 9641
		EVEX_Vgetmantps_zmm_k1z_zmmm512b32_imm8_sae,
		// Token: 0x040025AA RID: 9642
		EVEX_Vgetmantpd_xmm_k1z_xmmm128b64_imm8,
		// Token: 0x040025AB RID: 9643
		EVEX_Vgetmantpd_ymm_k1z_ymmm256b64_imm8,
		// Token: 0x040025AC RID: 9644
		EVEX_Vgetmantpd_zmm_k1z_zmmm512b64_imm8_sae,
		// Token: 0x040025AD RID: 9645
		EVEX_Vgetmantss_xmm_k1z_xmm_xmmm32_imm8_sae,
		// Token: 0x040025AE RID: 9646
		EVEX_Vgetmantsd_xmm_k1z_xmm_xmmm64_imm8_sae,
		// Token: 0x040025AF RID: 9647
		VEX_Kshiftrb_kr_kr_imm8,
		// Token: 0x040025B0 RID: 9648
		VEX_Kshiftrw_kr_kr_imm8,
		// Token: 0x040025B1 RID: 9649
		VEX_Kshiftrd_kr_kr_imm8,
		// Token: 0x040025B2 RID: 9650
		VEX_Kshiftrq_kr_kr_imm8,
		// Token: 0x040025B3 RID: 9651
		VEX_Kshiftlb_kr_kr_imm8,
		// Token: 0x040025B4 RID: 9652
		VEX_Kshiftlw_kr_kr_imm8,
		// Token: 0x040025B5 RID: 9653
		VEX_Kshiftld_kr_kr_imm8,
		// Token: 0x040025B6 RID: 9654
		VEX_Kshiftlq_kr_kr_imm8,
		// Token: 0x040025B7 RID: 9655
		VEX_Vinserti128_ymm_ymm_xmmm128_imm8,
		// Token: 0x040025B8 RID: 9656
		EVEX_Vinserti32x4_ymm_k1z_ymm_xmmm128_imm8,
		// Token: 0x040025B9 RID: 9657
		EVEX_Vinserti32x4_zmm_k1z_zmm_xmmm128_imm8,
		// Token: 0x040025BA RID: 9658
		EVEX_Vinserti64x2_ymm_k1z_ymm_xmmm128_imm8,
		// Token: 0x040025BB RID: 9659
		EVEX_Vinserti64x2_zmm_k1z_zmm_xmmm128_imm8,
		// Token: 0x040025BC RID: 9660
		VEX_Vextracti128_xmmm128_ymm_imm8,
		// Token: 0x040025BD RID: 9661
		EVEX_Vextracti32x4_xmmm128_k1z_ymm_imm8,
		// Token: 0x040025BE RID: 9662
		EVEX_Vextracti32x4_xmmm128_k1z_zmm_imm8,
		// Token: 0x040025BF RID: 9663
		EVEX_Vextracti64x2_xmmm128_k1z_ymm_imm8,
		// Token: 0x040025C0 RID: 9664
		EVEX_Vextracti64x2_xmmm128_k1z_zmm_imm8,
		// Token: 0x040025C1 RID: 9665
		EVEX_Vinserti32x8_zmm_k1z_zmm_ymmm256_imm8,
		// Token: 0x040025C2 RID: 9666
		EVEX_Vinserti64x4_zmm_k1z_zmm_ymmm256_imm8,
		// Token: 0x040025C3 RID: 9667
		EVEX_Vextracti32x8_ymmm256_k1z_zmm_imm8,
		// Token: 0x040025C4 RID: 9668
		EVEX_Vextracti64x4_ymmm256_k1z_zmm_imm8,
		// Token: 0x040025C5 RID: 9669
		EVEX_Vpcmpub_kr_k1_xmm_xmmm128_imm8,
		// Token: 0x040025C6 RID: 9670
		EVEX_Vpcmpub_kr_k1_ymm_ymmm256_imm8,
		// Token: 0x040025C7 RID: 9671
		EVEX_Vpcmpub_kr_k1_zmm_zmmm512_imm8,
		// Token: 0x040025C8 RID: 9672
		EVEX_Vpcmpuw_kr_k1_xmm_xmmm128_imm8,
		// Token: 0x040025C9 RID: 9673
		EVEX_Vpcmpuw_kr_k1_ymm_ymmm256_imm8,
		// Token: 0x040025CA RID: 9674
		EVEX_Vpcmpuw_kr_k1_zmm_zmmm512_imm8,
		// Token: 0x040025CB RID: 9675
		EVEX_Vpcmpb_kr_k1_xmm_xmmm128_imm8,
		// Token: 0x040025CC RID: 9676
		EVEX_Vpcmpb_kr_k1_ymm_ymmm256_imm8,
		// Token: 0x040025CD RID: 9677
		EVEX_Vpcmpb_kr_k1_zmm_zmmm512_imm8,
		// Token: 0x040025CE RID: 9678
		EVEX_Vpcmpw_kr_k1_xmm_xmmm128_imm8,
		// Token: 0x040025CF RID: 9679
		EVEX_Vpcmpw_kr_k1_ymm_ymmm256_imm8,
		// Token: 0x040025D0 RID: 9680
		EVEX_Vpcmpw_kr_k1_zmm_zmmm512_imm8,
		// Token: 0x040025D1 RID: 9681
		Dpps_xmm_xmmm128_imm8,
		// Token: 0x040025D2 RID: 9682
		VEX_Vdpps_xmm_xmm_xmmm128_imm8,
		// Token: 0x040025D3 RID: 9683
		VEX_Vdpps_ymm_ymm_ymmm256_imm8,
		// Token: 0x040025D4 RID: 9684
		Dppd_xmm_xmmm128_imm8,
		// Token: 0x040025D5 RID: 9685
		VEX_Vdppd_xmm_xmm_xmmm128_imm8,
		// Token: 0x040025D6 RID: 9686
		Mpsadbw_xmm_xmmm128_imm8,
		// Token: 0x040025D7 RID: 9687
		VEX_Vmpsadbw_xmm_xmm_xmmm128_imm8,
		// Token: 0x040025D8 RID: 9688
		VEX_Vmpsadbw_ymm_ymm_ymmm256_imm8,
		// Token: 0x040025D9 RID: 9689
		EVEX_Vdbpsadbw_xmm_k1z_xmm_xmmm128_imm8,
		// Token: 0x040025DA RID: 9690
		EVEX_Vdbpsadbw_ymm_k1z_ymm_ymmm256_imm8,
		// Token: 0x040025DB RID: 9691
		EVEX_Vdbpsadbw_zmm_k1z_zmm_zmmm512_imm8,
		// Token: 0x040025DC RID: 9692
		EVEX_Vshufi32x4_ymm_k1z_ymm_ymmm256b32_imm8,
		// Token: 0x040025DD RID: 9693
		EVEX_Vshufi32x4_zmm_k1z_zmm_zmmm512b32_imm8,
		// Token: 0x040025DE RID: 9694
		EVEX_Vshufi64x2_ymm_k1z_ymm_ymmm256b64_imm8,
		// Token: 0x040025DF RID: 9695
		EVEX_Vshufi64x2_zmm_k1z_zmm_zmmm512b64_imm8,
		// Token: 0x040025E0 RID: 9696
		Pclmulqdq_xmm_xmmm128_imm8,
		// Token: 0x040025E1 RID: 9697
		VEX_Vpclmulqdq_xmm_xmm_xmmm128_imm8,
		// Token: 0x040025E2 RID: 9698
		VEX_Vpclmulqdq_ymm_ymm_ymmm256_imm8,
		// Token: 0x040025E3 RID: 9699
		EVEX_Vpclmulqdq_xmm_xmm_xmmm128_imm8,
		// Token: 0x040025E4 RID: 9700
		EVEX_Vpclmulqdq_ymm_ymm_ymmm256_imm8,
		// Token: 0x040025E5 RID: 9701
		EVEX_Vpclmulqdq_zmm_zmm_zmmm512_imm8,
		// Token: 0x040025E6 RID: 9702
		VEX_Vperm2i128_ymm_ymm_ymmm256_imm8,
		// Token: 0x040025E7 RID: 9703
		VEX_Vpermil2ps_xmm_xmm_xmmm128_xmm_imm4,
		// Token: 0x040025E8 RID: 9704
		VEX_Vpermil2ps_ymm_ymm_ymmm256_ymm_imm4,
		// Token: 0x040025E9 RID: 9705
		VEX_Vpermil2ps_xmm_xmm_xmm_xmmm128_imm4,
		// Token: 0x040025EA RID: 9706
		VEX_Vpermil2ps_ymm_ymm_ymm_ymmm256_imm4,
		// Token: 0x040025EB RID: 9707
		VEX_Vpermil2pd_xmm_xmm_xmmm128_xmm_imm4,
		// Token: 0x040025EC RID: 9708
		VEX_Vpermil2pd_ymm_ymm_ymmm256_ymm_imm4,
		// Token: 0x040025ED RID: 9709
		VEX_Vpermil2pd_xmm_xmm_xmm_xmmm128_imm4,
		// Token: 0x040025EE RID: 9710
		VEX_Vpermil2pd_ymm_ymm_ymm_ymmm256_imm4,
		// Token: 0x040025EF RID: 9711
		VEX_Vblendvps_xmm_xmm_xmmm128_xmm,
		// Token: 0x040025F0 RID: 9712
		VEX_Vblendvps_ymm_ymm_ymmm256_ymm,
		// Token: 0x040025F1 RID: 9713
		VEX_Vblendvpd_xmm_xmm_xmmm128_xmm,
		// Token: 0x040025F2 RID: 9714
		VEX_Vblendvpd_ymm_ymm_ymmm256_ymm,
		// Token: 0x040025F3 RID: 9715
		VEX_Vpblendvb_xmm_xmm_xmmm128_xmm,
		// Token: 0x040025F4 RID: 9716
		VEX_Vpblendvb_ymm_ymm_ymmm256_ymm,
		// Token: 0x040025F5 RID: 9717
		EVEX_Vrangeps_xmm_k1z_xmm_xmmm128b32_imm8,
		// Token: 0x040025F6 RID: 9718
		EVEX_Vrangeps_ymm_k1z_ymm_ymmm256b32_imm8,
		// Token: 0x040025F7 RID: 9719
		EVEX_Vrangeps_zmm_k1z_zmm_zmmm512b32_imm8_sae,
		// Token: 0x040025F8 RID: 9720
		EVEX_Vrangepd_xmm_k1z_xmm_xmmm128b64_imm8,
		// Token: 0x040025F9 RID: 9721
		EVEX_Vrangepd_ymm_k1z_ymm_ymmm256b64_imm8,
		// Token: 0x040025FA RID: 9722
		EVEX_Vrangepd_zmm_k1z_zmm_zmmm512b64_imm8_sae,
		// Token: 0x040025FB RID: 9723
		EVEX_Vrangess_xmm_k1z_xmm_xmmm32_imm8_sae,
		// Token: 0x040025FC RID: 9724
		EVEX_Vrangesd_xmm_k1z_xmm_xmmm64_imm8_sae,
		// Token: 0x040025FD RID: 9725
		EVEX_Vfixupimmps_xmm_k1z_xmm_xmmm128b32_imm8,
		// Token: 0x040025FE RID: 9726
		EVEX_Vfixupimmps_ymm_k1z_ymm_ymmm256b32_imm8,
		// Token: 0x040025FF RID: 9727
		EVEX_Vfixupimmps_zmm_k1z_zmm_zmmm512b32_imm8_sae,
		// Token: 0x04002600 RID: 9728
		EVEX_Vfixupimmpd_xmm_k1z_xmm_xmmm128b64_imm8,
		// Token: 0x04002601 RID: 9729
		EVEX_Vfixupimmpd_ymm_k1z_ymm_ymmm256b64_imm8,
		// Token: 0x04002602 RID: 9730
		EVEX_Vfixupimmpd_zmm_k1z_zmm_zmmm512b64_imm8_sae,
		// Token: 0x04002603 RID: 9731
		EVEX_Vfixupimmss_xmm_k1z_xmm_xmmm32_imm8_sae,
		// Token: 0x04002604 RID: 9732
		EVEX_Vfixupimmsd_xmm_k1z_xmm_xmmm64_imm8_sae,
		// Token: 0x04002605 RID: 9733
		EVEX_Vreduceps_xmm_k1z_xmmm128b32_imm8,
		// Token: 0x04002606 RID: 9734
		EVEX_Vreduceps_ymm_k1z_ymmm256b32_imm8,
		// Token: 0x04002607 RID: 9735
		EVEX_Vreduceps_zmm_k1z_zmmm512b32_imm8_sae,
		// Token: 0x04002608 RID: 9736
		EVEX_Vreducepd_xmm_k1z_xmmm128b64_imm8,
		// Token: 0x04002609 RID: 9737
		EVEX_Vreducepd_ymm_k1z_ymmm256b64_imm8,
		// Token: 0x0400260A RID: 9738
		EVEX_Vreducepd_zmm_k1z_zmmm512b64_imm8_sae,
		// Token: 0x0400260B RID: 9739
		EVEX_Vreducess_xmm_k1z_xmm_xmmm32_imm8_sae,
		// Token: 0x0400260C RID: 9740
		EVEX_Vreducesd_xmm_k1z_xmm_xmmm64_imm8_sae,
		// Token: 0x0400260D RID: 9741
		VEX_Vfmaddsubps_xmm_xmm_xmmm128_xmm,
		// Token: 0x0400260E RID: 9742
		VEX_Vfmaddsubps_ymm_ymm_ymmm256_ymm,
		// Token: 0x0400260F RID: 9743
		VEX_Vfmaddsubps_xmm_xmm_xmm_xmmm128,
		// Token: 0x04002610 RID: 9744
		VEX_Vfmaddsubps_ymm_ymm_ymm_ymmm256,
		// Token: 0x04002611 RID: 9745
		VEX_Vfmaddsubpd_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002612 RID: 9746
		VEX_Vfmaddsubpd_ymm_ymm_ymmm256_ymm,
		// Token: 0x04002613 RID: 9747
		VEX_Vfmaddsubpd_xmm_xmm_xmm_xmmm128,
		// Token: 0x04002614 RID: 9748
		VEX_Vfmaddsubpd_ymm_ymm_ymm_ymmm256,
		// Token: 0x04002615 RID: 9749
		VEX_Vfmsubaddps_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002616 RID: 9750
		VEX_Vfmsubaddps_ymm_ymm_ymmm256_ymm,
		// Token: 0x04002617 RID: 9751
		VEX_Vfmsubaddps_xmm_xmm_xmm_xmmm128,
		// Token: 0x04002618 RID: 9752
		VEX_Vfmsubaddps_ymm_ymm_ymm_ymmm256,
		// Token: 0x04002619 RID: 9753
		VEX_Vfmsubaddpd_xmm_xmm_xmmm128_xmm,
		// Token: 0x0400261A RID: 9754
		VEX_Vfmsubaddpd_ymm_ymm_ymmm256_ymm,
		// Token: 0x0400261B RID: 9755
		VEX_Vfmsubaddpd_xmm_xmm_xmm_xmmm128,
		// Token: 0x0400261C RID: 9756
		VEX_Vfmsubaddpd_ymm_ymm_ymm_ymmm256,
		// Token: 0x0400261D RID: 9757
		Pcmpestrm_xmm_xmmm128_imm8,
		// Token: 0x0400261E RID: 9758
		Pcmpestrm64_xmm_xmmm128_imm8,
		// Token: 0x0400261F RID: 9759
		VEX_Vpcmpestrm_xmm_xmmm128_imm8,
		// Token: 0x04002620 RID: 9760
		VEX_Vpcmpestrm64_xmm_xmmm128_imm8,
		// Token: 0x04002621 RID: 9761
		Pcmpestri_xmm_xmmm128_imm8,
		// Token: 0x04002622 RID: 9762
		Pcmpestri64_xmm_xmmm128_imm8,
		// Token: 0x04002623 RID: 9763
		VEX_Vpcmpestri_xmm_xmmm128_imm8,
		// Token: 0x04002624 RID: 9764
		VEX_Vpcmpestri64_xmm_xmmm128_imm8,
		// Token: 0x04002625 RID: 9765
		Pcmpistrm_xmm_xmmm128_imm8,
		// Token: 0x04002626 RID: 9766
		VEX_Vpcmpistrm_xmm_xmmm128_imm8,
		// Token: 0x04002627 RID: 9767
		Pcmpistri_xmm_xmmm128_imm8,
		// Token: 0x04002628 RID: 9768
		VEX_Vpcmpistri_xmm_xmmm128_imm8,
		// Token: 0x04002629 RID: 9769
		EVEX_Vfpclassps_kr_k1_xmmm128b32_imm8,
		// Token: 0x0400262A RID: 9770
		EVEX_Vfpclassps_kr_k1_ymmm256b32_imm8,
		// Token: 0x0400262B RID: 9771
		EVEX_Vfpclassps_kr_k1_zmmm512b32_imm8,
		// Token: 0x0400262C RID: 9772
		EVEX_Vfpclasspd_kr_k1_xmmm128b64_imm8,
		// Token: 0x0400262D RID: 9773
		EVEX_Vfpclasspd_kr_k1_ymmm256b64_imm8,
		// Token: 0x0400262E RID: 9774
		EVEX_Vfpclasspd_kr_k1_zmmm512b64_imm8,
		// Token: 0x0400262F RID: 9775
		EVEX_Vfpclassss_kr_k1_xmmm32_imm8,
		// Token: 0x04002630 RID: 9776
		EVEX_Vfpclasssd_kr_k1_xmmm64_imm8,
		// Token: 0x04002631 RID: 9777
		VEX_Vfmaddps_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002632 RID: 9778
		VEX_Vfmaddps_ymm_ymm_ymmm256_ymm,
		// Token: 0x04002633 RID: 9779
		VEX_Vfmaddps_xmm_xmm_xmm_xmmm128,
		// Token: 0x04002634 RID: 9780
		VEX_Vfmaddps_ymm_ymm_ymm_ymmm256,
		// Token: 0x04002635 RID: 9781
		VEX_Vfmaddpd_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002636 RID: 9782
		VEX_Vfmaddpd_ymm_ymm_ymmm256_ymm,
		// Token: 0x04002637 RID: 9783
		VEX_Vfmaddpd_xmm_xmm_xmm_xmmm128,
		// Token: 0x04002638 RID: 9784
		VEX_Vfmaddpd_ymm_ymm_ymm_ymmm256,
		// Token: 0x04002639 RID: 9785
		VEX_Vfmaddss_xmm_xmm_xmmm32_xmm,
		// Token: 0x0400263A RID: 9786
		VEX_Vfmaddss_xmm_xmm_xmm_xmmm32,
		// Token: 0x0400263B RID: 9787
		VEX_Vfmaddsd_xmm_xmm_xmmm64_xmm,
		// Token: 0x0400263C RID: 9788
		VEX_Vfmaddsd_xmm_xmm_xmm_xmmm64,
		// Token: 0x0400263D RID: 9789
		VEX_Vfmsubps_xmm_xmm_xmmm128_xmm,
		// Token: 0x0400263E RID: 9790
		VEX_Vfmsubps_ymm_ymm_ymmm256_ymm,
		// Token: 0x0400263F RID: 9791
		VEX_Vfmsubps_xmm_xmm_xmm_xmmm128,
		// Token: 0x04002640 RID: 9792
		VEX_Vfmsubps_ymm_ymm_ymm_ymmm256,
		// Token: 0x04002641 RID: 9793
		VEX_Vfmsubpd_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002642 RID: 9794
		VEX_Vfmsubpd_ymm_ymm_ymmm256_ymm,
		// Token: 0x04002643 RID: 9795
		VEX_Vfmsubpd_xmm_xmm_xmm_xmmm128,
		// Token: 0x04002644 RID: 9796
		VEX_Vfmsubpd_ymm_ymm_ymm_ymmm256,
		// Token: 0x04002645 RID: 9797
		VEX_Vfmsubss_xmm_xmm_xmmm32_xmm,
		// Token: 0x04002646 RID: 9798
		VEX_Vfmsubss_xmm_xmm_xmm_xmmm32,
		// Token: 0x04002647 RID: 9799
		VEX_Vfmsubsd_xmm_xmm_xmmm64_xmm,
		// Token: 0x04002648 RID: 9800
		VEX_Vfmsubsd_xmm_xmm_xmm_xmmm64,
		// Token: 0x04002649 RID: 9801
		EVEX_Vpshldw_xmm_k1z_xmm_xmmm128_imm8,
		// Token: 0x0400264A RID: 9802
		EVEX_Vpshldw_ymm_k1z_ymm_ymmm256_imm8,
		// Token: 0x0400264B RID: 9803
		EVEX_Vpshldw_zmm_k1z_zmm_zmmm512_imm8,
		// Token: 0x0400264C RID: 9804
		EVEX_Vpshldd_xmm_k1z_xmm_xmmm128b32_imm8,
		// Token: 0x0400264D RID: 9805
		EVEX_Vpshldd_ymm_k1z_ymm_ymmm256b32_imm8,
		// Token: 0x0400264E RID: 9806
		EVEX_Vpshldd_zmm_k1z_zmm_zmmm512b32_imm8,
		// Token: 0x0400264F RID: 9807
		EVEX_Vpshldq_xmm_k1z_xmm_xmmm128b64_imm8,
		// Token: 0x04002650 RID: 9808
		EVEX_Vpshldq_ymm_k1z_ymm_ymmm256b64_imm8,
		// Token: 0x04002651 RID: 9809
		EVEX_Vpshldq_zmm_k1z_zmm_zmmm512b64_imm8,
		// Token: 0x04002652 RID: 9810
		EVEX_Vpshrdw_xmm_k1z_xmm_xmmm128_imm8,
		// Token: 0x04002653 RID: 9811
		EVEX_Vpshrdw_ymm_k1z_ymm_ymmm256_imm8,
		// Token: 0x04002654 RID: 9812
		EVEX_Vpshrdw_zmm_k1z_zmm_zmmm512_imm8,
		// Token: 0x04002655 RID: 9813
		EVEX_Vpshrdd_xmm_k1z_xmm_xmmm128b32_imm8,
		// Token: 0x04002656 RID: 9814
		EVEX_Vpshrdd_ymm_k1z_ymm_ymmm256b32_imm8,
		// Token: 0x04002657 RID: 9815
		EVEX_Vpshrdd_zmm_k1z_zmm_zmmm512b32_imm8,
		// Token: 0x04002658 RID: 9816
		EVEX_Vpshrdq_xmm_k1z_xmm_xmmm128b64_imm8,
		// Token: 0x04002659 RID: 9817
		EVEX_Vpshrdq_ymm_k1z_ymm_ymmm256b64_imm8,
		// Token: 0x0400265A RID: 9818
		EVEX_Vpshrdq_zmm_k1z_zmm_zmmm512b64_imm8,
		// Token: 0x0400265B RID: 9819
		VEX_Vfnmaddps_xmm_xmm_xmmm128_xmm,
		// Token: 0x0400265C RID: 9820
		VEX_Vfnmaddps_ymm_ymm_ymmm256_ymm,
		// Token: 0x0400265D RID: 9821
		VEX_Vfnmaddps_xmm_xmm_xmm_xmmm128,
		// Token: 0x0400265E RID: 9822
		VEX_Vfnmaddps_ymm_ymm_ymm_ymmm256,
		// Token: 0x0400265F RID: 9823
		VEX_Vfnmaddpd_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002660 RID: 9824
		VEX_Vfnmaddpd_ymm_ymm_ymmm256_ymm,
		// Token: 0x04002661 RID: 9825
		VEX_Vfnmaddpd_xmm_xmm_xmm_xmmm128,
		// Token: 0x04002662 RID: 9826
		VEX_Vfnmaddpd_ymm_ymm_ymm_ymmm256,
		// Token: 0x04002663 RID: 9827
		VEX_Vfnmaddss_xmm_xmm_xmmm32_xmm,
		// Token: 0x04002664 RID: 9828
		VEX_Vfnmaddss_xmm_xmm_xmm_xmmm32,
		// Token: 0x04002665 RID: 9829
		VEX_Vfnmaddsd_xmm_xmm_xmmm64_xmm,
		// Token: 0x04002666 RID: 9830
		VEX_Vfnmaddsd_xmm_xmm_xmm_xmmm64,
		// Token: 0x04002667 RID: 9831
		VEX_Vfnmsubps_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002668 RID: 9832
		VEX_Vfnmsubps_ymm_ymm_ymmm256_ymm,
		// Token: 0x04002669 RID: 9833
		VEX_Vfnmsubps_xmm_xmm_xmm_xmmm128,
		// Token: 0x0400266A RID: 9834
		VEX_Vfnmsubps_ymm_ymm_ymm_ymmm256,
		// Token: 0x0400266B RID: 9835
		VEX_Vfnmsubpd_xmm_xmm_xmmm128_xmm,
		// Token: 0x0400266C RID: 9836
		VEX_Vfnmsubpd_ymm_ymm_ymmm256_ymm,
		// Token: 0x0400266D RID: 9837
		VEX_Vfnmsubpd_xmm_xmm_xmm_xmmm128,
		// Token: 0x0400266E RID: 9838
		VEX_Vfnmsubpd_ymm_ymm_ymm_ymmm256,
		// Token: 0x0400266F RID: 9839
		VEX_Vfnmsubss_xmm_xmm_xmmm32_xmm,
		// Token: 0x04002670 RID: 9840
		VEX_Vfnmsubss_xmm_xmm_xmm_xmmm32,
		// Token: 0x04002671 RID: 9841
		VEX_Vfnmsubsd_xmm_xmm_xmmm64_xmm,
		// Token: 0x04002672 RID: 9842
		VEX_Vfnmsubsd_xmm_xmm_xmm_xmmm64,
		// Token: 0x04002673 RID: 9843
		Sha1rnds4_xmm_xmmm128_imm8,
		// Token: 0x04002674 RID: 9844
		Gf2p8affineqb_xmm_xmmm128_imm8,
		// Token: 0x04002675 RID: 9845
		VEX_Vgf2p8affineqb_xmm_xmm_xmmm128_imm8,
		// Token: 0x04002676 RID: 9846
		VEX_Vgf2p8affineqb_ymm_ymm_ymmm256_imm8,
		// Token: 0x04002677 RID: 9847
		EVEX_Vgf2p8affineqb_xmm_k1z_xmm_xmmm128b64_imm8,
		// Token: 0x04002678 RID: 9848
		EVEX_Vgf2p8affineqb_ymm_k1z_ymm_ymmm256b64_imm8,
		// Token: 0x04002679 RID: 9849
		EVEX_Vgf2p8affineqb_zmm_k1z_zmm_zmmm512b64_imm8,
		// Token: 0x0400267A RID: 9850
		Gf2p8affineinvqb_xmm_xmmm128_imm8,
		// Token: 0x0400267B RID: 9851
		VEX_Vgf2p8affineinvqb_xmm_xmm_xmmm128_imm8,
		// Token: 0x0400267C RID: 9852
		VEX_Vgf2p8affineinvqb_ymm_ymm_ymmm256_imm8,
		// Token: 0x0400267D RID: 9853
		EVEX_Vgf2p8affineinvqb_xmm_k1z_xmm_xmmm128b64_imm8,
		// Token: 0x0400267E RID: 9854
		EVEX_Vgf2p8affineinvqb_ymm_k1z_ymm_ymmm256b64_imm8,
		// Token: 0x0400267F RID: 9855
		EVEX_Vgf2p8affineinvqb_zmm_k1z_zmm_zmmm512b64_imm8,
		// Token: 0x04002680 RID: 9856
		Aeskeygenassist_xmm_xmmm128_imm8,
		// Token: 0x04002681 RID: 9857
		VEX_Vaeskeygenassist_xmm_xmmm128_imm8,
		// Token: 0x04002682 RID: 9858
		VEX_Rorx_r32_rm32_imm8,
		// Token: 0x04002683 RID: 9859
		VEX_Rorx_r64_rm64_imm8,
		// Token: 0x04002684 RID: 9860
		XOP_Vpmacssww_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002685 RID: 9861
		XOP_Vpmacsswd_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002686 RID: 9862
		XOP_Vpmacssdql_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002687 RID: 9863
		XOP_Vpmacssdd_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002688 RID: 9864
		XOP_Vpmacssdqh_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002689 RID: 9865
		XOP_Vpmacsww_xmm_xmm_xmmm128_xmm,
		// Token: 0x0400268A RID: 9866
		XOP_Vpmacswd_xmm_xmm_xmmm128_xmm,
		// Token: 0x0400268B RID: 9867
		XOP_Vpmacsdql_xmm_xmm_xmmm128_xmm,
		// Token: 0x0400268C RID: 9868
		XOP_Vpmacsdd_xmm_xmm_xmmm128_xmm,
		// Token: 0x0400268D RID: 9869
		XOP_Vpmacsdqh_xmm_xmm_xmmm128_xmm,
		// Token: 0x0400268E RID: 9870
		XOP_Vpcmov_xmm_xmm_xmmm128_xmm,
		// Token: 0x0400268F RID: 9871
		XOP_Vpcmov_ymm_ymm_ymmm256_ymm,
		// Token: 0x04002690 RID: 9872
		XOP_Vpcmov_xmm_xmm_xmm_xmmm128,
		// Token: 0x04002691 RID: 9873
		XOP_Vpcmov_ymm_ymm_ymm_ymmm256,
		// Token: 0x04002692 RID: 9874
		XOP_Vpperm_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002693 RID: 9875
		XOP_Vpperm_xmm_xmm_xmm_xmmm128,
		// Token: 0x04002694 RID: 9876
		XOP_Vpmadcsswd_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002695 RID: 9877
		XOP_Vpmadcswd_xmm_xmm_xmmm128_xmm,
		// Token: 0x04002696 RID: 9878
		XOP_Vprotb_xmm_xmmm128_imm8,
		// Token: 0x04002697 RID: 9879
		XOP_Vprotw_xmm_xmmm128_imm8,
		// Token: 0x04002698 RID: 9880
		XOP_Vprotd_xmm_xmmm128_imm8,
		// Token: 0x04002699 RID: 9881
		XOP_Vprotq_xmm_xmmm128_imm8,
		// Token: 0x0400269A RID: 9882
		XOP_Vpcomb_xmm_xmm_xmmm128_imm8,
		// Token: 0x0400269B RID: 9883
		XOP_Vpcomw_xmm_xmm_xmmm128_imm8,
		// Token: 0x0400269C RID: 9884
		XOP_Vpcomd_xmm_xmm_xmmm128_imm8,
		// Token: 0x0400269D RID: 9885
		XOP_Vpcomq_xmm_xmm_xmmm128_imm8,
		// Token: 0x0400269E RID: 9886
		XOP_Vpcomub_xmm_xmm_xmmm128_imm8,
		// Token: 0x0400269F RID: 9887
		XOP_Vpcomuw_xmm_xmm_xmmm128_imm8,
		// Token: 0x040026A0 RID: 9888
		XOP_Vpcomud_xmm_xmm_xmmm128_imm8,
		// Token: 0x040026A1 RID: 9889
		XOP_Vpcomuq_xmm_xmm_xmmm128_imm8,
		// Token: 0x040026A2 RID: 9890
		XOP_Blcfill_r32_rm32,
		// Token: 0x040026A3 RID: 9891
		XOP_Blcfill_r64_rm64,
		// Token: 0x040026A4 RID: 9892
		XOP_Blsfill_r32_rm32,
		// Token: 0x040026A5 RID: 9893
		XOP_Blsfill_r64_rm64,
		// Token: 0x040026A6 RID: 9894
		XOP_Blcs_r32_rm32,
		// Token: 0x040026A7 RID: 9895
		XOP_Blcs_r64_rm64,
		// Token: 0x040026A8 RID: 9896
		XOP_Tzmsk_r32_rm32,
		// Token: 0x040026A9 RID: 9897
		XOP_Tzmsk_r64_rm64,
		// Token: 0x040026AA RID: 9898
		XOP_Blcic_r32_rm32,
		// Token: 0x040026AB RID: 9899
		XOP_Blcic_r64_rm64,
		// Token: 0x040026AC RID: 9900
		XOP_Blsic_r32_rm32,
		// Token: 0x040026AD RID: 9901
		XOP_Blsic_r64_rm64,
		// Token: 0x040026AE RID: 9902
		XOP_T1mskc_r32_rm32,
		// Token: 0x040026AF RID: 9903
		XOP_T1mskc_r64_rm64,
		// Token: 0x040026B0 RID: 9904
		XOP_Blcmsk_r32_rm32,
		// Token: 0x040026B1 RID: 9905
		XOP_Blcmsk_r64_rm64,
		// Token: 0x040026B2 RID: 9906
		XOP_Blci_r32_rm32,
		// Token: 0x040026B3 RID: 9907
		XOP_Blci_r64_rm64,
		// Token: 0x040026B4 RID: 9908
		XOP_Llwpcb_r32,
		// Token: 0x040026B5 RID: 9909
		XOP_Llwpcb_r64,
		// Token: 0x040026B6 RID: 9910
		XOP_Slwpcb_r32,
		// Token: 0x040026B7 RID: 9911
		XOP_Slwpcb_r64,
		// Token: 0x040026B8 RID: 9912
		XOP_Vfrczps_xmm_xmmm128,
		// Token: 0x040026B9 RID: 9913
		XOP_Vfrczps_ymm_ymmm256,
		// Token: 0x040026BA RID: 9914
		XOP_Vfrczpd_xmm_xmmm128,
		// Token: 0x040026BB RID: 9915
		XOP_Vfrczpd_ymm_ymmm256,
		// Token: 0x040026BC RID: 9916
		XOP_Vfrczss_xmm_xmmm32,
		// Token: 0x040026BD RID: 9917
		XOP_Vfrczsd_xmm_xmmm64,
		// Token: 0x040026BE RID: 9918
		XOP_Vprotb_xmm_xmmm128_xmm,
		// Token: 0x040026BF RID: 9919
		XOP_Vprotb_xmm_xmm_xmmm128,
		// Token: 0x040026C0 RID: 9920
		XOP_Vprotw_xmm_xmmm128_xmm,
		// Token: 0x040026C1 RID: 9921
		XOP_Vprotw_xmm_xmm_xmmm128,
		// Token: 0x040026C2 RID: 9922
		XOP_Vprotd_xmm_xmmm128_xmm,
		// Token: 0x040026C3 RID: 9923
		XOP_Vprotd_xmm_xmm_xmmm128,
		// Token: 0x040026C4 RID: 9924
		XOP_Vprotq_xmm_xmmm128_xmm,
		// Token: 0x040026C5 RID: 9925
		XOP_Vprotq_xmm_xmm_xmmm128,
		// Token: 0x040026C6 RID: 9926
		XOP_Vpshlb_xmm_xmmm128_xmm,
		// Token: 0x040026C7 RID: 9927
		XOP_Vpshlb_xmm_xmm_xmmm128,
		// Token: 0x040026C8 RID: 9928
		XOP_Vpshlw_xmm_xmmm128_xmm,
		// Token: 0x040026C9 RID: 9929
		XOP_Vpshlw_xmm_xmm_xmmm128,
		// Token: 0x040026CA RID: 9930
		XOP_Vpshld_xmm_xmmm128_xmm,
		// Token: 0x040026CB RID: 9931
		XOP_Vpshld_xmm_xmm_xmmm128,
		// Token: 0x040026CC RID: 9932
		XOP_Vpshlq_xmm_xmmm128_xmm,
		// Token: 0x040026CD RID: 9933
		XOP_Vpshlq_xmm_xmm_xmmm128,
		// Token: 0x040026CE RID: 9934
		XOP_Vpshab_xmm_xmmm128_xmm,
		// Token: 0x040026CF RID: 9935
		XOP_Vpshab_xmm_xmm_xmmm128,
		// Token: 0x040026D0 RID: 9936
		XOP_Vpshaw_xmm_xmmm128_xmm,
		// Token: 0x040026D1 RID: 9937
		XOP_Vpshaw_xmm_xmm_xmmm128,
		// Token: 0x040026D2 RID: 9938
		XOP_Vpshad_xmm_xmmm128_xmm,
		// Token: 0x040026D3 RID: 9939
		XOP_Vpshad_xmm_xmm_xmmm128,
		// Token: 0x040026D4 RID: 9940
		XOP_Vpshaq_xmm_xmmm128_xmm,
		// Token: 0x040026D5 RID: 9941
		XOP_Vpshaq_xmm_xmm_xmmm128,
		// Token: 0x040026D6 RID: 9942
		XOP_Vphaddbw_xmm_xmmm128,
		// Token: 0x040026D7 RID: 9943
		XOP_Vphaddbd_xmm_xmmm128,
		// Token: 0x040026D8 RID: 9944
		XOP_Vphaddbq_xmm_xmmm128,
		// Token: 0x040026D9 RID: 9945
		XOP_Vphaddwd_xmm_xmmm128,
		// Token: 0x040026DA RID: 9946
		XOP_Vphaddwq_xmm_xmmm128,
		// Token: 0x040026DB RID: 9947
		XOP_Vphadddq_xmm_xmmm128,
		// Token: 0x040026DC RID: 9948
		XOP_Vphaddubw_xmm_xmmm128,
		// Token: 0x040026DD RID: 9949
		XOP_Vphaddubd_xmm_xmmm128,
		// Token: 0x040026DE RID: 9950
		XOP_Vphaddubq_xmm_xmmm128,
		// Token: 0x040026DF RID: 9951
		XOP_Vphadduwd_xmm_xmmm128,
		// Token: 0x040026E0 RID: 9952
		XOP_Vphadduwq_xmm_xmmm128,
		// Token: 0x040026E1 RID: 9953
		XOP_Vphaddudq_xmm_xmmm128,
		// Token: 0x040026E2 RID: 9954
		XOP_Vphsubbw_xmm_xmmm128,
		// Token: 0x040026E3 RID: 9955
		XOP_Vphsubwd_xmm_xmmm128,
		// Token: 0x040026E4 RID: 9956
		XOP_Vphsubdq_xmm_xmmm128,
		// Token: 0x040026E5 RID: 9957
		XOP_Bextr_r32_rm32_imm32,
		// Token: 0x040026E6 RID: 9958
		XOP_Bextr_r64_rm64_imm32,
		// Token: 0x040026E7 RID: 9959
		XOP_Lwpins_r32_rm32_imm32,
		// Token: 0x040026E8 RID: 9960
		XOP_Lwpins_r64_rm32_imm32,
		// Token: 0x040026E9 RID: 9961
		XOP_Lwpval_r32_rm32_imm32,
		// Token: 0x040026EA RID: 9962
		XOP_Lwpval_r64_rm32_imm32,
		// Token: 0x040026EB RID: 9963
		D3NOW_Pi2fw_mm_mmm64,
		// Token: 0x040026EC RID: 9964
		D3NOW_Pi2fd_mm_mmm64,
		// Token: 0x040026ED RID: 9965
		D3NOW_Pf2iw_mm_mmm64,
		// Token: 0x040026EE RID: 9966
		D3NOW_Pf2id_mm_mmm64,
		// Token: 0x040026EF RID: 9967
		D3NOW_Pfrcpv_mm_mmm64,
		// Token: 0x040026F0 RID: 9968
		D3NOW_Pfrsqrtv_mm_mmm64,
		// Token: 0x040026F1 RID: 9969
		D3NOW_Pfnacc_mm_mmm64,
		// Token: 0x040026F2 RID: 9970
		D3NOW_Pfpnacc_mm_mmm64,
		// Token: 0x040026F3 RID: 9971
		D3NOW_Pfcmpge_mm_mmm64,
		// Token: 0x040026F4 RID: 9972
		D3NOW_Pfmin_mm_mmm64,
		// Token: 0x040026F5 RID: 9973
		D3NOW_Pfrcp_mm_mmm64,
		// Token: 0x040026F6 RID: 9974
		D3NOW_Pfrsqrt_mm_mmm64,
		// Token: 0x040026F7 RID: 9975
		D3NOW_Pfsub_mm_mmm64,
		// Token: 0x040026F8 RID: 9976
		D3NOW_Pfadd_mm_mmm64,
		// Token: 0x040026F9 RID: 9977
		D3NOW_Pfcmpgt_mm_mmm64,
		// Token: 0x040026FA RID: 9978
		D3NOW_Pfmax_mm_mmm64,
		// Token: 0x040026FB RID: 9979
		D3NOW_Pfrcpit1_mm_mmm64,
		// Token: 0x040026FC RID: 9980
		D3NOW_Pfrsqit1_mm_mmm64,
		// Token: 0x040026FD RID: 9981
		D3NOW_Pfsubr_mm_mmm64,
		// Token: 0x040026FE RID: 9982
		D3NOW_Pfacc_mm_mmm64,
		// Token: 0x040026FF RID: 9983
		D3NOW_Pfcmpeq_mm_mmm64,
		// Token: 0x04002700 RID: 9984
		D3NOW_Pfmul_mm_mmm64,
		// Token: 0x04002701 RID: 9985
		D3NOW_Pfrcpit2_mm_mmm64,
		// Token: 0x04002702 RID: 9986
		D3NOW_Pmulhrw_mm_mmm64,
		// Token: 0x04002703 RID: 9987
		D3NOW_Pswapd_mm_mmm64,
		// Token: 0x04002704 RID: 9988
		D3NOW_Pavgusb_mm_mmm64,
		// Token: 0x04002705 RID: 9989
		Rmpadjust,
		// Token: 0x04002706 RID: 9990
		Rmpupdate,
		// Token: 0x04002707 RID: 9991
		Psmash,
		// Token: 0x04002708 RID: 9992
		Pvalidatew,
		// Token: 0x04002709 RID: 9993
		Pvalidated,
		// Token: 0x0400270A RID: 9994
		Pvalidateq,
		// Token: 0x0400270B RID: 9995
		Serialize,
		// Token: 0x0400270C RID: 9996
		Xsusldtrk,
		// Token: 0x0400270D RID: 9997
		Xresldtrk,
		// Token: 0x0400270E RID: 9998
		Invlpgbw,
		// Token: 0x0400270F RID: 9999
		Invlpgbd,
		// Token: 0x04002710 RID: 10000
		Invlpgbq,
		// Token: 0x04002711 RID: 10001
		Tlbsync,
		// Token: 0x04002712 RID: 10002
		Prefetchreserved3_m8,
		// Token: 0x04002713 RID: 10003
		Prefetchreserved4_m8,
		// Token: 0x04002714 RID: 10004
		Prefetchreserved5_m8,
		// Token: 0x04002715 RID: 10005
		Prefetchreserved6_m8,
		// Token: 0x04002716 RID: 10006
		Prefetchreserved7_m8,
		// Token: 0x04002717 RID: 10007
		Ud0,
		// Token: 0x04002718 RID: 10008
		Vmgexit,
		// Token: 0x04002719 RID: 10009
		Getsecq,
		// Token: 0x0400271A RID: 10010
		VEX_Ldtilecfg_m512,
		// Token: 0x0400271B RID: 10011
		VEX_Tilerelease,
		// Token: 0x0400271C RID: 10012
		VEX_Sttilecfg_m512,
		// Token: 0x0400271D RID: 10013
		VEX_Tilezero_tmm,
		// Token: 0x0400271E RID: 10014
		VEX_Tileloaddt1_tmm_sibmem,
		// Token: 0x0400271F RID: 10015
		VEX_Tilestored_sibmem_tmm,
		// Token: 0x04002720 RID: 10016
		VEX_Tileloadd_tmm_sibmem,
		// Token: 0x04002721 RID: 10017
		VEX_Tdpbf16ps_tmm_tmm_tmm,
		// Token: 0x04002722 RID: 10018
		VEX_Tdpbuud_tmm_tmm_tmm,
		// Token: 0x04002723 RID: 10019
		VEX_Tdpbusd_tmm_tmm_tmm,
		// Token: 0x04002724 RID: 10020
		VEX_Tdpbsud_tmm_tmm_tmm,
		// Token: 0x04002725 RID: 10021
		VEX_Tdpbssd_tmm_tmm_tmm,
		// Token: 0x04002726 RID: 10022
		Fnstdw_AX,
		// Token: 0x04002727 RID: 10023
		Fnstsg_AX,
		// Token: 0x04002728 RID: 10024
		Rdshr_rm32,
		// Token: 0x04002729 RID: 10025
		Wrshr_rm32,
		// Token: 0x0400272A RID: 10026
		Smint,
		// Token: 0x0400272B RID: 10027
		Dmint,
		// Token: 0x0400272C RID: 10028
		Rdm,
		// Token: 0x0400272D RID: 10029
		Svdc_m80_Sreg,
		// Token: 0x0400272E RID: 10030
		Rsdc_Sreg_m80,
		// Token: 0x0400272F RID: 10031
		Svldt_m80,
		// Token: 0x04002730 RID: 10032
		Rsldt_m80,
		// Token: 0x04002731 RID: 10033
		Svts_m80,
		// Token: 0x04002732 RID: 10034
		Rsts_m80,
		// Token: 0x04002733 RID: 10035
		Smint_0F7E,
		// Token: 0x04002734 RID: 10036
		Bb0_reset,
		// Token: 0x04002735 RID: 10037
		Bb1_reset,
		// Token: 0x04002736 RID: 10038
		Cpu_write,
		// Token: 0x04002737 RID: 10039
		Cpu_read,
		// Token: 0x04002738 RID: 10040
		Altinst,
		// Token: 0x04002739 RID: 10041
		Paveb_mm_mmm64,
		// Token: 0x0400273A RID: 10042
		Paddsiw_mm_mmm64,
		// Token: 0x0400273B RID: 10043
		Pmagw_mm_mmm64,
		// Token: 0x0400273C RID: 10044
		Pdistib_mm_m64,
		// Token: 0x0400273D RID: 10045
		Psubsiw_mm_mmm64,
		// Token: 0x0400273E RID: 10046
		Pmvzb_mm_m64,
		// Token: 0x0400273F RID: 10047
		Pmulhrw_mm_mmm64,
		// Token: 0x04002740 RID: 10048
		Pmvnzb_mm_m64,
		// Token: 0x04002741 RID: 10049
		Pmvlzb_mm_m64,
		// Token: 0x04002742 RID: 10050
		Pmvgezb_mm_m64,
		// Token: 0x04002743 RID: 10051
		Pmulhriw_mm_mmm64,
		// Token: 0x04002744 RID: 10052
		Pmachriw_mm_m64,
		// Token: 0x04002745 RID: 10053
		Cyrix_D9D7,
		// Token: 0x04002746 RID: 10054
		Cyrix_D9E2,
		// Token: 0x04002747 RID: 10055
		Ftstp,
		// Token: 0x04002748 RID: 10056
		Cyrix_D9E7,
		// Token: 0x04002749 RID: 10057
		Frint2,
		// Token: 0x0400274A RID: 10058
		Frichop,
		// Token: 0x0400274B RID: 10059
		Cyrix_DED8,
		// Token: 0x0400274C RID: 10060
		Cyrix_DEDA,
		// Token: 0x0400274D RID: 10061
		Cyrix_DEDC,
		// Token: 0x0400274E RID: 10062
		Cyrix_DEDD,
		// Token: 0x0400274F RID: 10063
		Cyrix_DEDE,
		// Token: 0x04002750 RID: 10064
		Frinear,
		// Token: 0x04002751 RID: 10065
		Tdcall,
		// Token: 0x04002752 RID: 10066
		Seamret,
		// Token: 0x04002753 RID: 10067
		Seamops,
		// Token: 0x04002754 RID: 10068
		Seamcall,
		// Token: 0x04002755 RID: 10069
		Aesencwide128kl_m384,
		// Token: 0x04002756 RID: 10070
		Aesdecwide128kl_m384,
		// Token: 0x04002757 RID: 10071
		Aesencwide256kl_m512,
		// Token: 0x04002758 RID: 10072
		Aesdecwide256kl_m512,
		// Token: 0x04002759 RID: 10073
		Loadiwkey_xmm_xmm,
		// Token: 0x0400275A RID: 10074
		Aesenc128kl_xmm_m384,
		// Token: 0x0400275B RID: 10075
		Aesdec128kl_xmm_m384,
		// Token: 0x0400275C RID: 10076
		Aesenc256kl_xmm_m512,
		// Token: 0x0400275D RID: 10077
		Aesdec256kl_xmm_m512,
		// Token: 0x0400275E RID: 10078
		Encodekey128_r32_r32,
		// Token: 0x0400275F RID: 10079
		Encodekey256_r32_r32,
		// Token: 0x04002760 RID: 10080
		VEX_Vbroadcastss_xmm_xmm,
		// Token: 0x04002761 RID: 10081
		VEX_Vbroadcastss_ymm_xmm,
		// Token: 0x04002762 RID: 10082
		VEX_Vbroadcastsd_ymm_xmm,
		// Token: 0x04002763 RID: 10083
		Vmgexit_F2,
		// Token: 0x04002764 RID: 10084
		Uiret,
		// Token: 0x04002765 RID: 10085
		Testui,
		// Token: 0x04002766 RID: 10086
		Clui,
		// Token: 0x04002767 RID: 10087
		Stui,
		// Token: 0x04002768 RID: 10088
		Senduipi_r64,
		// Token: 0x04002769 RID: 10089
		Hreset_imm8,
		// Token: 0x0400276A RID: 10090
		VEX_Vpdpbusd_xmm_xmm_xmmm128,
		// Token: 0x0400276B RID: 10091
		VEX_Vpdpbusd_ymm_ymm_ymmm256,
		// Token: 0x0400276C RID: 10092
		VEX_Vpdpbusds_xmm_xmm_xmmm128,
		// Token: 0x0400276D RID: 10093
		VEX_Vpdpbusds_ymm_ymm_ymmm256,
		// Token: 0x0400276E RID: 10094
		VEX_Vpdpwssd_xmm_xmm_xmmm128,
		// Token: 0x0400276F RID: 10095
		VEX_Vpdpwssd_ymm_ymm_ymmm256,
		// Token: 0x04002770 RID: 10096
		VEX_Vpdpwssds_xmm_xmm_xmmm128,
		// Token: 0x04002771 RID: 10097
		VEX_Vpdpwssds_ymm_ymm_ymmm256,
		// Token: 0x04002772 RID: 10098
		Ccs_hash_16,
		// Token: 0x04002773 RID: 10099
		Ccs_hash_32,
		// Token: 0x04002774 RID: 10100
		Ccs_hash_64,
		// Token: 0x04002775 RID: 10101
		Ccs_encrypt_16,
		// Token: 0x04002776 RID: 10102
		Ccs_encrypt_32,
		// Token: 0x04002777 RID: 10103
		Ccs_encrypt_64,
		// Token: 0x04002778 RID: 10104
		Lkgs_rm16,
		// Token: 0x04002779 RID: 10105
		Lkgs_r32m16,
		// Token: 0x0400277A RID: 10106
		Lkgs_r64m16,
		// Token: 0x0400277B RID: 10107
		Eretu,
		// Token: 0x0400277C RID: 10108
		Erets,
		// Token: 0x0400277D RID: 10109
		EVEX_Vaddph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x0400277E RID: 10110
		EVEX_Vaddph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x0400277F RID: 10111
		EVEX_Vaddph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x04002780 RID: 10112
		EVEX_Vaddsh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x04002781 RID: 10113
		EVEX_Vcmpph_kr_k1_xmm_xmmm128b16_imm8,
		// Token: 0x04002782 RID: 10114
		EVEX_Vcmpph_kr_k1_ymm_ymmm256b16_imm8,
		// Token: 0x04002783 RID: 10115
		EVEX_Vcmpph_kr_k1_zmm_zmmm512b16_imm8_sae,
		// Token: 0x04002784 RID: 10116
		EVEX_Vcmpsh_kr_k1_xmm_xmmm16_imm8_sae,
		// Token: 0x04002785 RID: 10117
		EVEX_Vcomish_xmm_xmmm16_sae,
		// Token: 0x04002786 RID: 10118
		EVEX_Vcvtdq2ph_xmm_k1z_xmmm128b32,
		// Token: 0x04002787 RID: 10119
		EVEX_Vcvtdq2ph_xmm_k1z_ymmm256b32,
		// Token: 0x04002788 RID: 10120
		EVEX_Vcvtdq2ph_ymm_k1z_zmmm512b32_er,
		// Token: 0x04002789 RID: 10121
		EVEX_Vcvtpd2ph_xmm_k1z_xmmm128b64,
		// Token: 0x0400278A RID: 10122
		EVEX_Vcvtpd2ph_xmm_k1z_ymmm256b64,
		// Token: 0x0400278B RID: 10123
		EVEX_Vcvtpd2ph_xmm_k1z_zmmm512b64_er,
		// Token: 0x0400278C RID: 10124
		EVEX_Vcvtph2dq_xmm_k1z_xmmm64b16,
		// Token: 0x0400278D RID: 10125
		EVEX_Vcvtph2dq_ymm_k1z_xmmm128b16,
		// Token: 0x0400278E RID: 10126
		EVEX_Vcvtph2dq_zmm_k1z_ymmm256b16_er,
		// Token: 0x0400278F RID: 10127
		EVEX_Vcvtph2pd_xmm_k1z_xmmm32b16,
		// Token: 0x04002790 RID: 10128
		EVEX_Vcvtph2pd_ymm_k1z_xmmm64b16,
		// Token: 0x04002791 RID: 10129
		EVEX_Vcvtph2pd_zmm_k1z_xmmm128b16_sae,
		// Token: 0x04002792 RID: 10130
		EVEX_Vcvtph2psx_xmm_k1z_xmmm64b16,
		// Token: 0x04002793 RID: 10131
		EVEX_Vcvtph2psx_ymm_k1z_xmmm128b16,
		// Token: 0x04002794 RID: 10132
		EVEX_Vcvtph2psx_zmm_k1z_ymmm256b16_sae,
		// Token: 0x04002795 RID: 10133
		EVEX_Vcvtph2qq_xmm_k1z_xmmm32b16,
		// Token: 0x04002796 RID: 10134
		EVEX_Vcvtph2qq_ymm_k1z_xmmm64b16,
		// Token: 0x04002797 RID: 10135
		EVEX_Vcvtph2qq_zmm_k1z_xmmm128b16_er,
		// Token: 0x04002798 RID: 10136
		EVEX_Vcvtph2udq_xmm_k1z_xmmm64b16,
		// Token: 0x04002799 RID: 10137
		EVEX_Vcvtph2udq_ymm_k1z_xmmm128b16,
		// Token: 0x0400279A RID: 10138
		EVEX_Vcvtph2udq_zmm_k1z_ymmm256b16_er,
		// Token: 0x0400279B RID: 10139
		EVEX_Vcvtph2uqq_xmm_k1z_xmmm32b16,
		// Token: 0x0400279C RID: 10140
		EVEX_Vcvtph2uqq_ymm_k1z_xmmm64b16,
		// Token: 0x0400279D RID: 10141
		EVEX_Vcvtph2uqq_zmm_k1z_xmmm128b16_er,
		// Token: 0x0400279E RID: 10142
		EVEX_Vcvtph2uw_xmm_k1z_xmmm128b16,
		// Token: 0x0400279F RID: 10143
		EVEX_Vcvtph2uw_ymm_k1z_ymmm256b16,
		// Token: 0x040027A0 RID: 10144
		EVEX_Vcvtph2uw_zmm_k1z_zmmm512b16_er,
		// Token: 0x040027A1 RID: 10145
		EVEX_Vcvtph2w_xmm_k1z_xmmm128b16,
		// Token: 0x040027A2 RID: 10146
		EVEX_Vcvtph2w_ymm_k1z_ymmm256b16,
		// Token: 0x040027A3 RID: 10147
		EVEX_Vcvtph2w_zmm_k1z_zmmm512b16_er,
		// Token: 0x040027A4 RID: 10148
		EVEX_Vcvtps2phx_xmm_k1z_xmmm128b32,
		// Token: 0x040027A5 RID: 10149
		EVEX_Vcvtps2phx_xmm_k1z_ymmm256b32,
		// Token: 0x040027A6 RID: 10150
		EVEX_Vcvtps2phx_ymm_k1z_zmmm512b32_er,
		// Token: 0x040027A7 RID: 10151
		EVEX_Vcvtqq2ph_xmm_k1z_xmmm128b64,
		// Token: 0x040027A8 RID: 10152
		EVEX_Vcvtqq2ph_xmm_k1z_ymmm256b64,
		// Token: 0x040027A9 RID: 10153
		EVEX_Vcvtqq2ph_xmm_k1z_zmmm512b64_er,
		// Token: 0x040027AA RID: 10154
		EVEX_Vcvtsd2sh_xmm_k1z_xmm_xmmm64_er,
		// Token: 0x040027AB RID: 10155
		EVEX_Vcvtsh2sd_xmm_k1z_xmm_xmmm16_sae,
		// Token: 0x040027AC RID: 10156
		EVEX_Vcvtsh2si_r32_xmmm16_er,
		// Token: 0x040027AD RID: 10157
		EVEX_Vcvtsh2si_r64_xmmm16_er,
		// Token: 0x040027AE RID: 10158
		EVEX_Vcvtsh2ss_xmm_k1z_xmm_xmmm16_sae,
		// Token: 0x040027AF RID: 10159
		EVEX_Vcvtsh2usi_r32_xmmm16_er,
		// Token: 0x040027B0 RID: 10160
		EVEX_Vcvtsh2usi_r64_xmmm16_er,
		// Token: 0x040027B1 RID: 10161
		EVEX_Vcvtsi2sh_xmm_xmm_rm32_er,
		// Token: 0x040027B2 RID: 10162
		EVEX_Vcvtsi2sh_xmm_xmm_rm64_er,
		// Token: 0x040027B3 RID: 10163
		EVEX_Vcvtss2sh_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x040027B4 RID: 10164
		EVEX_Vcvttph2dq_xmm_k1z_xmmm64b16,
		// Token: 0x040027B5 RID: 10165
		EVEX_Vcvttph2dq_ymm_k1z_xmmm128b16,
		// Token: 0x040027B6 RID: 10166
		EVEX_Vcvttph2dq_zmm_k1z_ymmm256b16_sae,
		// Token: 0x040027B7 RID: 10167
		EVEX_Vcvttph2qq_xmm_k1z_xmmm32b16,
		// Token: 0x040027B8 RID: 10168
		EVEX_Vcvttph2qq_ymm_k1z_xmmm64b16,
		// Token: 0x040027B9 RID: 10169
		EVEX_Vcvttph2qq_zmm_k1z_xmmm128b16_sae,
		// Token: 0x040027BA RID: 10170
		EVEX_Vcvttph2udq_xmm_k1z_xmmm64b16,
		// Token: 0x040027BB RID: 10171
		EVEX_Vcvttph2udq_ymm_k1z_xmmm128b16,
		// Token: 0x040027BC RID: 10172
		EVEX_Vcvttph2udq_zmm_k1z_ymmm256b16_sae,
		// Token: 0x040027BD RID: 10173
		EVEX_Vcvttph2uqq_xmm_k1z_xmmm32b16,
		// Token: 0x040027BE RID: 10174
		EVEX_Vcvttph2uqq_ymm_k1z_xmmm64b16,
		// Token: 0x040027BF RID: 10175
		EVEX_Vcvttph2uqq_zmm_k1z_xmmm128b16_sae,
		// Token: 0x040027C0 RID: 10176
		EVEX_Vcvttph2uw_xmm_k1z_xmmm128b16,
		// Token: 0x040027C1 RID: 10177
		EVEX_Vcvttph2uw_ymm_k1z_ymmm256b16,
		// Token: 0x040027C2 RID: 10178
		EVEX_Vcvttph2uw_zmm_k1z_zmmm512b16_sae,
		// Token: 0x040027C3 RID: 10179
		EVEX_Vcvttph2w_xmm_k1z_xmmm128b16,
		// Token: 0x040027C4 RID: 10180
		EVEX_Vcvttph2w_ymm_k1z_ymmm256b16,
		// Token: 0x040027C5 RID: 10181
		EVEX_Vcvttph2w_zmm_k1z_zmmm512b16_sae,
		// Token: 0x040027C6 RID: 10182
		EVEX_Vcvttsh2si_r32_xmmm16_sae,
		// Token: 0x040027C7 RID: 10183
		EVEX_Vcvttsh2si_r64_xmmm16_sae,
		// Token: 0x040027C8 RID: 10184
		EVEX_Vcvttsh2usi_r32_xmmm16_sae,
		// Token: 0x040027C9 RID: 10185
		EVEX_Vcvttsh2usi_r64_xmmm16_sae,
		// Token: 0x040027CA RID: 10186
		EVEX_Vcvtudq2ph_xmm_k1z_xmmm128b32,
		// Token: 0x040027CB RID: 10187
		EVEX_Vcvtudq2ph_xmm_k1z_ymmm256b32,
		// Token: 0x040027CC RID: 10188
		EVEX_Vcvtudq2ph_ymm_k1z_zmmm512b32_er,
		// Token: 0x040027CD RID: 10189
		EVEX_Vcvtuqq2ph_xmm_k1z_xmmm128b64,
		// Token: 0x040027CE RID: 10190
		EVEX_Vcvtuqq2ph_xmm_k1z_ymmm256b64,
		// Token: 0x040027CF RID: 10191
		EVEX_Vcvtuqq2ph_xmm_k1z_zmmm512b64_er,
		// Token: 0x040027D0 RID: 10192
		EVEX_Vcvtusi2sh_xmm_xmm_rm32_er,
		// Token: 0x040027D1 RID: 10193
		EVEX_Vcvtusi2sh_xmm_xmm_rm64_er,
		// Token: 0x040027D2 RID: 10194
		EVEX_Vcvtuw2ph_xmm_k1z_xmmm128b16,
		// Token: 0x040027D3 RID: 10195
		EVEX_Vcvtuw2ph_ymm_k1z_ymmm256b16,
		// Token: 0x040027D4 RID: 10196
		EVEX_Vcvtuw2ph_zmm_k1z_zmmm512b16_er,
		// Token: 0x040027D5 RID: 10197
		EVEX_Vcvtw2ph_xmm_k1z_xmmm128b16,
		// Token: 0x040027D6 RID: 10198
		EVEX_Vcvtw2ph_ymm_k1z_ymmm256b16,
		// Token: 0x040027D7 RID: 10199
		EVEX_Vcvtw2ph_zmm_k1z_zmmm512b16_er,
		// Token: 0x040027D8 RID: 10200
		EVEX_Vdivph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x040027D9 RID: 10201
		EVEX_Vdivph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x040027DA RID: 10202
		EVEX_Vdivph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x040027DB RID: 10203
		EVEX_Vdivsh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x040027DC RID: 10204
		EVEX_Vfcmaddcph_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040027DD RID: 10205
		EVEX_Vfcmaddcph_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040027DE RID: 10206
		EVEX_Vfcmaddcph_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x040027DF RID: 10207
		EVEX_Vfmaddcph_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040027E0 RID: 10208
		EVEX_Vfmaddcph_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040027E1 RID: 10209
		EVEX_Vfmaddcph_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x040027E2 RID: 10210
		EVEX_Vfcmaddcsh_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x040027E3 RID: 10211
		EVEX_Vfmaddcsh_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x040027E4 RID: 10212
		EVEX_Vfcmulcph_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040027E5 RID: 10213
		EVEX_Vfcmulcph_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040027E6 RID: 10214
		EVEX_Vfcmulcph_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x040027E7 RID: 10215
		EVEX_Vfmulcph_xmm_k1z_xmm_xmmm128b32,
		// Token: 0x040027E8 RID: 10216
		EVEX_Vfmulcph_ymm_k1z_ymm_ymmm256b32,
		// Token: 0x040027E9 RID: 10217
		EVEX_Vfmulcph_zmm_k1z_zmm_zmmm512b32_er,
		// Token: 0x040027EA RID: 10218
		EVEX_Vfcmulcsh_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x040027EB RID: 10219
		EVEX_Vfmulcsh_xmm_k1z_xmm_xmmm32_er,
		// Token: 0x040027EC RID: 10220
		EVEX_Vfmaddsub132ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x040027ED RID: 10221
		EVEX_Vfmaddsub132ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x040027EE RID: 10222
		EVEX_Vfmaddsub132ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x040027EF RID: 10223
		EVEX_Vfmaddsub213ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x040027F0 RID: 10224
		EVEX_Vfmaddsub213ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x040027F1 RID: 10225
		EVEX_Vfmaddsub213ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x040027F2 RID: 10226
		EVEX_Vfmaddsub231ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x040027F3 RID: 10227
		EVEX_Vfmaddsub231ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x040027F4 RID: 10228
		EVEX_Vfmaddsub231ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x040027F5 RID: 10229
		EVEX_Vfmsubadd132ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x040027F6 RID: 10230
		EVEX_Vfmsubadd132ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x040027F7 RID: 10231
		EVEX_Vfmsubadd132ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x040027F8 RID: 10232
		EVEX_Vfmsubadd213ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x040027F9 RID: 10233
		EVEX_Vfmsubadd213ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x040027FA RID: 10234
		EVEX_Vfmsubadd213ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x040027FB RID: 10235
		EVEX_Vfmsubadd231ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x040027FC RID: 10236
		EVEX_Vfmsubadd231ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x040027FD RID: 10237
		EVEX_Vfmsubadd231ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x040027FE RID: 10238
		EVEX_Vfmadd132ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x040027FF RID: 10239
		EVEX_Vfmadd132ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x04002800 RID: 10240
		EVEX_Vfmadd132ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x04002801 RID: 10241
		EVEX_Vfmadd213ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x04002802 RID: 10242
		EVEX_Vfmadd213ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x04002803 RID: 10243
		EVEX_Vfmadd213ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x04002804 RID: 10244
		EVEX_Vfmadd231ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x04002805 RID: 10245
		EVEX_Vfmadd231ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x04002806 RID: 10246
		EVEX_Vfmadd231ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x04002807 RID: 10247
		EVEX_Vfnmadd132ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x04002808 RID: 10248
		EVEX_Vfnmadd132ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x04002809 RID: 10249
		EVEX_Vfnmadd132ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x0400280A RID: 10250
		EVEX_Vfnmadd213ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x0400280B RID: 10251
		EVEX_Vfnmadd213ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x0400280C RID: 10252
		EVEX_Vfnmadd213ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x0400280D RID: 10253
		EVEX_Vfnmadd231ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x0400280E RID: 10254
		EVEX_Vfnmadd231ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x0400280F RID: 10255
		EVEX_Vfnmadd231ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x04002810 RID: 10256
		EVEX_Vfmadd132sh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x04002811 RID: 10257
		EVEX_Vfmadd213sh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x04002812 RID: 10258
		EVEX_Vfmadd231sh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x04002813 RID: 10259
		EVEX_Vfnmadd132sh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x04002814 RID: 10260
		EVEX_Vfnmadd213sh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x04002815 RID: 10261
		EVEX_Vfnmadd231sh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x04002816 RID: 10262
		EVEX_Vfmsub132ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x04002817 RID: 10263
		EVEX_Vfmsub132ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x04002818 RID: 10264
		EVEX_Vfmsub132ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x04002819 RID: 10265
		EVEX_Vfmsub213ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x0400281A RID: 10266
		EVEX_Vfmsub213ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x0400281B RID: 10267
		EVEX_Vfmsub213ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x0400281C RID: 10268
		EVEX_Vfmsub231ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x0400281D RID: 10269
		EVEX_Vfmsub231ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x0400281E RID: 10270
		EVEX_Vfmsub231ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x0400281F RID: 10271
		EVEX_Vfnmsub132ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x04002820 RID: 10272
		EVEX_Vfnmsub132ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x04002821 RID: 10273
		EVEX_Vfnmsub132ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x04002822 RID: 10274
		EVEX_Vfnmsub213ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x04002823 RID: 10275
		EVEX_Vfnmsub213ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x04002824 RID: 10276
		EVEX_Vfnmsub213ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x04002825 RID: 10277
		EVEX_Vfnmsub231ph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x04002826 RID: 10278
		EVEX_Vfnmsub231ph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x04002827 RID: 10279
		EVEX_Vfnmsub231ph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x04002828 RID: 10280
		EVEX_Vfmsub132sh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x04002829 RID: 10281
		EVEX_Vfmsub213sh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x0400282A RID: 10282
		EVEX_Vfmsub231sh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x0400282B RID: 10283
		EVEX_Vfnmsub132sh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x0400282C RID: 10284
		EVEX_Vfnmsub213sh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x0400282D RID: 10285
		EVEX_Vfnmsub231sh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x0400282E RID: 10286
		EVEX_Vfpclassph_kr_k1_xmmm128b16_imm8,
		// Token: 0x0400282F RID: 10287
		EVEX_Vfpclassph_kr_k1_ymmm256b16_imm8,
		// Token: 0x04002830 RID: 10288
		EVEX_Vfpclassph_kr_k1_zmmm512b16_imm8,
		// Token: 0x04002831 RID: 10289
		EVEX_Vfpclasssh_kr_k1_xmmm16_imm8,
		// Token: 0x04002832 RID: 10290
		EVEX_Vgetexpph_xmm_k1z_xmmm128b16,
		// Token: 0x04002833 RID: 10291
		EVEX_Vgetexpph_ymm_k1z_ymmm256b16,
		// Token: 0x04002834 RID: 10292
		EVEX_Vgetexpph_zmm_k1z_zmmm512b16_sae,
		// Token: 0x04002835 RID: 10293
		EVEX_Vgetexpsh_xmm_k1z_xmm_xmmm16_sae,
		// Token: 0x04002836 RID: 10294
		EVEX_Vgetmantph_xmm_k1z_xmmm128b16_imm8,
		// Token: 0x04002837 RID: 10295
		EVEX_Vgetmantph_ymm_k1z_ymmm256b16_imm8,
		// Token: 0x04002838 RID: 10296
		EVEX_Vgetmantph_zmm_k1z_zmmm512b16_imm8_sae,
		// Token: 0x04002839 RID: 10297
		EVEX_Vgetmantsh_xmm_k1z_xmm_xmmm16_imm8_sae,
		// Token: 0x0400283A RID: 10298
		EVEX_Vmaxph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x0400283B RID: 10299
		EVEX_Vmaxph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x0400283C RID: 10300
		EVEX_Vmaxph_zmm_k1z_zmm_zmmm512b16_sae,
		// Token: 0x0400283D RID: 10301
		EVEX_Vmaxsh_xmm_k1z_xmm_xmmm16_sae,
		// Token: 0x0400283E RID: 10302
		EVEX_Vminph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x0400283F RID: 10303
		EVEX_Vminph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x04002840 RID: 10304
		EVEX_Vminph_zmm_k1z_zmm_zmmm512b16_sae,
		// Token: 0x04002841 RID: 10305
		EVEX_Vminsh_xmm_k1z_xmm_xmmm16_sae,
		// Token: 0x04002842 RID: 10306
		EVEX_Vmovsh_xmm_k1z_m16,
		// Token: 0x04002843 RID: 10307
		EVEX_Vmovsh_m16_k1_xmm,
		// Token: 0x04002844 RID: 10308
		EVEX_Vmovsh_xmm_k1z_xmm_xmm,
		// Token: 0x04002845 RID: 10309
		EVEX_Vmovsh_xmm_k1z_xmm_xmm_MAP5_11,
		// Token: 0x04002846 RID: 10310
		EVEX_Vmovw_xmm_r32m16,
		// Token: 0x04002847 RID: 10311
		EVEX_Vmovw_xmm_r64m16,
		// Token: 0x04002848 RID: 10312
		EVEX_Vmovw_r32m16_xmm,
		// Token: 0x04002849 RID: 10313
		EVEX_Vmovw_r64m16_xmm,
		// Token: 0x0400284A RID: 10314
		EVEX_Vmulph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x0400284B RID: 10315
		EVEX_Vmulph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x0400284C RID: 10316
		EVEX_Vmulph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x0400284D RID: 10317
		EVEX_Vmulsh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x0400284E RID: 10318
		EVEX_Vrcpph_xmm_k1z_xmmm128b16,
		// Token: 0x0400284F RID: 10319
		EVEX_Vrcpph_ymm_k1z_ymmm256b16,
		// Token: 0x04002850 RID: 10320
		EVEX_Vrcpph_zmm_k1z_zmmm512b16,
		// Token: 0x04002851 RID: 10321
		EVEX_Vrcpsh_xmm_k1z_xmm_xmmm16,
		// Token: 0x04002852 RID: 10322
		EVEX_Vreduceph_xmm_k1z_xmmm128b16_imm8,
		// Token: 0x04002853 RID: 10323
		EVEX_Vreduceph_ymm_k1z_ymmm256b16_imm8,
		// Token: 0x04002854 RID: 10324
		EVEX_Vreduceph_zmm_k1z_zmmm512b16_imm8_sae,
		// Token: 0x04002855 RID: 10325
		EVEX_Vreducesh_xmm_k1z_xmm_xmmm16_imm8_sae,
		// Token: 0x04002856 RID: 10326
		EVEX_Vrndscaleph_xmm_k1z_xmmm128b16_imm8,
		// Token: 0x04002857 RID: 10327
		EVEX_Vrndscaleph_ymm_k1z_ymmm256b16_imm8,
		// Token: 0x04002858 RID: 10328
		EVEX_Vrndscaleph_zmm_k1z_zmmm512b16_imm8_sae,
		// Token: 0x04002859 RID: 10329
		EVEX_Vrndscalesh_xmm_k1z_xmm_xmmm16_imm8_sae,
		// Token: 0x0400285A RID: 10330
		EVEX_Vrsqrtph_xmm_k1z_xmmm128b16,
		// Token: 0x0400285B RID: 10331
		EVEX_Vrsqrtph_ymm_k1z_ymmm256b16,
		// Token: 0x0400285C RID: 10332
		EVEX_Vrsqrtph_zmm_k1z_zmmm512b16,
		// Token: 0x0400285D RID: 10333
		EVEX_Vrsqrtsh_xmm_k1z_xmm_xmmm16,
		// Token: 0x0400285E RID: 10334
		EVEX_Vscalefph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x0400285F RID: 10335
		EVEX_Vscalefph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x04002860 RID: 10336
		EVEX_Vscalefph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x04002861 RID: 10337
		EVEX_Vscalefsh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x04002862 RID: 10338
		EVEX_Vsqrtph_xmm_k1z_xmmm128b16,
		// Token: 0x04002863 RID: 10339
		EVEX_Vsqrtph_ymm_k1z_ymmm256b16,
		// Token: 0x04002864 RID: 10340
		EVEX_Vsqrtph_zmm_k1z_zmmm512b16_er,
		// Token: 0x04002865 RID: 10341
		EVEX_Vsqrtsh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x04002866 RID: 10342
		EVEX_Vsubph_xmm_k1z_xmm_xmmm128b16,
		// Token: 0x04002867 RID: 10343
		EVEX_Vsubph_ymm_k1z_ymm_ymmm256b16,
		// Token: 0x04002868 RID: 10344
		EVEX_Vsubph_zmm_k1z_zmm_zmmm512b16_er,
		// Token: 0x04002869 RID: 10345
		EVEX_Vsubsh_xmm_k1z_xmm_xmmm16_er,
		// Token: 0x0400286A RID: 10346
		EVEX_Vucomish_xmm_xmmm16_sae,
		// Token: 0x0400286B RID: 10347
		Rdudbg,
		// Token: 0x0400286C RID: 10348
		Wrudbg,
		// Token: 0x0400286D RID: 10349
		VEX_KNC_Jkzd_kr_rel8_64,
		// Token: 0x0400286E RID: 10350
		VEX_KNC_Jknzd_kr_rel8_64,
		// Token: 0x0400286F RID: 10351
		VEX_KNC_Vprefetchnta_m8,
		// Token: 0x04002870 RID: 10352
		VEX_KNC_Vprefetch0_m8,
		// Token: 0x04002871 RID: 10353
		VEX_KNC_Vprefetch1_m8,
		// Token: 0x04002872 RID: 10354
		VEX_KNC_Vprefetch2_m8,
		// Token: 0x04002873 RID: 10355
		VEX_KNC_Vprefetchenta_m8,
		// Token: 0x04002874 RID: 10356
		VEX_KNC_Vprefetche0_m8,
		// Token: 0x04002875 RID: 10357
		VEX_KNC_Vprefetche1_m8,
		// Token: 0x04002876 RID: 10358
		VEX_KNC_Vprefetche2_m8,
		// Token: 0x04002877 RID: 10359
		VEX_KNC_Kand_kr_kr,
		// Token: 0x04002878 RID: 10360
		VEX_KNC_Kandn_kr_kr,
		// Token: 0x04002879 RID: 10361
		VEX_KNC_Kandnr_kr_kr,
		// Token: 0x0400287A RID: 10362
		VEX_KNC_Knot_kr_kr,
		// Token: 0x0400287B RID: 10363
		VEX_KNC_Kor_kr_kr,
		// Token: 0x0400287C RID: 10364
		VEX_KNC_Kxnor_kr_kr,
		// Token: 0x0400287D RID: 10365
		VEX_KNC_Kxor_kr_kr,
		// Token: 0x0400287E RID: 10366
		VEX_KNC_Kmerge2l1h_kr_kr,
		// Token: 0x0400287F RID: 10367
		VEX_KNC_Kmerge2l1l_kr_kr,
		// Token: 0x04002880 RID: 10368
		VEX_KNC_Jkzd_kr_rel32_64,
		// Token: 0x04002881 RID: 10369
		VEX_KNC_Jknzd_kr_rel32_64,
		// Token: 0x04002882 RID: 10370
		VEX_KNC_Kmov_kr_kr,
		// Token: 0x04002883 RID: 10371
		VEX_KNC_Kmov_kr_r32,
		// Token: 0x04002884 RID: 10372
		VEX_KNC_Kmov_r32_kr,
		// Token: 0x04002885 RID: 10373
		VEX_KNC_Kconcath_r64_kr_kr,
		// Token: 0x04002886 RID: 10374
		VEX_KNC_Kconcatl_r64_kr_kr,
		// Token: 0x04002887 RID: 10375
		VEX_KNC_Kortest_kr_kr,
		// Token: 0x04002888 RID: 10376
		VEX_KNC_Delay_r32,
		// Token: 0x04002889 RID: 10377
		VEX_KNC_Delay_r64,
		// Token: 0x0400288A RID: 10378
		VEX_KNC_Spflt_r32,
		// Token: 0x0400288B RID: 10379
		VEX_KNC_Spflt_r64,
		// Token: 0x0400288C RID: 10380
		VEX_KNC_Clevict1_m8,
		// Token: 0x0400288D RID: 10381
		VEX_KNC_Clevict0_m8,
		// Token: 0x0400288E RID: 10382
		VEX_KNC_Popcnt_r32_r32,
		// Token: 0x0400288F RID: 10383
		VEX_KNC_Popcnt_r64_r64,
		// Token: 0x04002890 RID: 10384
		VEX_KNC_Tzcnt_r32_r32,
		// Token: 0x04002891 RID: 10385
		VEX_KNC_Tzcnt_r64_r64,
		// Token: 0x04002892 RID: 10386
		VEX_KNC_Tzcnti_r32_r32,
		// Token: 0x04002893 RID: 10387
		VEX_KNC_Tzcnti_r64_r64,
		// Token: 0x04002894 RID: 10388
		VEX_KNC_Lzcnt_r32_r32,
		// Token: 0x04002895 RID: 10389
		VEX_KNC_Lzcnt_r64_r64,
		// Token: 0x04002896 RID: 10390
		VEX_KNC_Undoc_r32_rm32_128_F3_0F38_W0_F0,
		// Token: 0x04002897 RID: 10391
		VEX_KNC_Undoc_r64_rm64_128_F3_0F38_W1_F0,
		// Token: 0x04002898 RID: 10392
		VEX_KNC_Undoc_r32_rm32_128_F2_0F38_W0_F0,
		// Token: 0x04002899 RID: 10393
		VEX_KNC_Undoc_r64_rm64_128_F2_0F38_W1_F0,
		// Token: 0x0400289A RID: 10394
		VEX_KNC_Undoc_r32_rm32_128_F2_0F38_W0_F1,
		// Token: 0x0400289B RID: 10395
		VEX_KNC_Undoc_r64_rm64_128_F2_0F38_W1_F1,
		// Token: 0x0400289C RID: 10396
		VEX_KNC_Kextract_kr_r64_imm8,
		// Token: 0x0400289D RID: 10397
		MVEX_Vprefetchnta_m,
		// Token: 0x0400289E RID: 10398
		MVEX_Vprefetch0_m,
		// Token: 0x0400289F RID: 10399
		MVEX_Vprefetch1_m,
		// Token: 0x040028A0 RID: 10400
		MVEX_Vprefetch2_m,
		// Token: 0x040028A1 RID: 10401
		MVEX_Vprefetchenta_m,
		// Token: 0x040028A2 RID: 10402
		MVEX_Vprefetche0_m,
		// Token: 0x040028A3 RID: 10403
		MVEX_Vprefetche1_m,
		// Token: 0x040028A4 RID: 10404
		MVEX_Vprefetche2_m,
		// Token: 0x040028A5 RID: 10405
		MVEX_Vmovaps_zmm_k1_zmmmt,
		// Token: 0x040028A6 RID: 10406
		MVEX_Vmovapd_zmm_k1_zmmmt,
		// Token: 0x040028A7 RID: 10407
		MVEX_Vmovaps_mt_k1_zmm,
		// Token: 0x040028A8 RID: 10408
		MVEX_Vmovapd_mt_k1_zmm,
		// Token: 0x040028A9 RID: 10409
		MVEX_Vmovnrapd_m_k1_zmm,
		// Token: 0x040028AA RID: 10410
		MVEX_Vmovnrngoapd_m_k1_zmm,
		// Token: 0x040028AB RID: 10411
		MVEX_Vmovnraps_m_k1_zmm,
		// Token: 0x040028AC RID: 10412
		MVEX_Vmovnrngoaps_m_k1_zmm,
		// Token: 0x040028AD RID: 10413
		MVEX_Vaddps_zmm_k1_zmm_zmmmt,
		// Token: 0x040028AE RID: 10414
		MVEX_Vaddpd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028AF RID: 10415
		MVEX_Vmulps_zmm_k1_zmm_zmmmt,
		// Token: 0x040028B0 RID: 10416
		MVEX_Vmulpd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028B1 RID: 10417
		MVEX_Vcvtps2pd_zmm_k1_zmmmt,
		// Token: 0x040028B2 RID: 10418
		MVEX_Vcvtpd2ps_zmm_k1_zmmmt,
		// Token: 0x040028B3 RID: 10419
		MVEX_Vsubps_zmm_k1_zmm_zmmmt,
		// Token: 0x040028B4 RID: 10420
		MVEX_Vsubpd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028B5 RID: 10421
		MVEX_Vpcmpgtd_kr_k1_zmm_zmmmt,
		// Token: 0x040028B6 RID: 10422
		MVEX_Vmovdqa32_zmm_k1_zmmmt,
		// Token: 0x040028B7 RID: 10423
		MVEX_Vmovdqa64_zmm_k1_zmmmt,
		// Token: 0x040028B8 RID: 10424
		MVEX_Vpshufd_zmm_k1_zmmmt_imm8,
		// Token: 0x040028B9 RID: 10425
		MVEX_Vpsrld_zmm_k1_zmmmt_imm8,
		// Token: 0x040028BA RID: 10426
		MVEX_Vpsrad_zmm_k1_zmmmt_imm8,
		// Token: 0x040028BB RID: 10427
		MVEX_Vpslld_zmm_k1_zmmmt_imm8,
		// Token: 0x040028BC RID: 10428
		MVEX_Vpcmpeqd_kr_k1_zmm_zmmmt,
		// Token: 0x040028BD RID: 10429
		MVEX_Vcvtudq2pd_zmm_k1_zmmmt,
		// Token: 0x040028BE RID: 10430
		MVEX_Vmovdqa32_mt_k1_zmm,
		// Token: 0x040028BF RID: 10431
		MVEX_Vmovdqa64_mt_k1_zmm,
		// Token: 0x040028C0 RID: 10432
		MVEX_Clevict1_m,
		// Token: 0x040028C1 RID: 10433
		MVEX_Clevict0_m,
		// Token: 0x040028C2 RID: 10434
		MVEX_Vcmpps_kr_k1_zmm_zmmmt_imm8,
		// Token: 0x040028C3 RID: 10435
		MVEX_Vcmppd_kr_k1_zmm_zmmmt_imm8,
		// Token: 0x040028C4 RID: 10436
		MVEX_Vpandd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028C5 RID: 10437
		MVEX_Vpandq_zmm_k1_zmm_zmmmt,
		// Token: 0x040028C6 RID: 10438
		MVEX_Vpandnd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028C7 RID: 10439
		MVEX_Vpandnq_zmm_k1_zmm_zmmmt,
		// Token: 0x040028C8 RID: 10440
		MVEX_Vcvtdq2pd_zmm_k1_zmmmt,
		// Token: 0x040028C9 RID: 10441
		MVEX_Vpord_zmm_k1_zmm_zmmmt,
		// Token: 0x040028CA RID: 10442
		MVEX_Vporq_zmm_k1_zmm_zmmmt,
		// Token: 0x040028CB RID: 10443
		MVEX_Vpxord_zmm_k1_zmm_zmmmt,
		// Token: 0x040028CC RID: 10444
		MVEX_Vpxorq_zmm_k1_zmm_zmmmt,
		// Token: 0x040028CD RID: 10445
		MVEX_Vpsubd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028CE RID: 10446
		MVEX_Vpaddd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028CF RID: 10447
		MVEX_Vbroadcastss_zmm_k1_mt,
		// Token: 0x040028D0 RID: 10448
		MVEX_Vbroadcastsd_zmm_k1_mt,
		// Token: 0x040028D1 RID: 10449
		MVEX_Vbroadcastf32x4_zmm_k1_mt,
		// Token: 0x040028D2 RID: 10450
		MVEX_Vbroadcastf64x4_zmm_k1_mt,
		// Token: 0x040028D3 RID: 10451
		MVEX_Vptestmd_kr_k1_zmm_zmmmt,
		// Token: 0x040028D4 RID: 10452
		MVEX_Vpermd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028D5 RID: 10453
		MVEX_Vpminsd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028D6 RID: 10454
		MVEX_Vpminud_zmm_k1_zmm_zmmmt,
		// Token: 0x040028D7 RID: 10455
		MVEX_Vpmaxsd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028D8 RID: 10456
		MVEX_Vpmaxud_zmm_k1_zmm_zmmmt,
		// Token: 0x040028D9 RID: 10457
		MVEX_Vpmulld_zmm_k1_zmm_zmmmt,
		// Token: 0x040028DA RID: 10458
		MVEX_Vgetexpps_zmm_k1_zmmmt,
		// Token: 0x040028DB RID: 10459
		MVEX_Vgetexppd_zmm_k1_zmmmt,
		// Token: 0x040028DC RID: 10460
		MVEX_Vpsrlvd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028DD RID: 10461
		MVEX_Vpsravd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028DE RID: 10462
		MVEX_Vpsllvd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028DF RID: 10463
		MVEX_Undoc_zmm_k1_zmmmt_512_66_0F38_W0_48,
		// Token: 0x040028E0 RID: 10464
		MVEX_Undoc_zmm_k1_zmmmt_512_66_0F38_W0_49,
		// Token: 0x040028E1 RID: 10465
		MVEX_Undoc_zmm_k1_zmmmt_512_66_0F38_W0_4A,
		// Token: 0x040028E2 RID: 10466
		MVEX_Undoc_zmm_k1_zmmmt_512_66_0F38_W0_4B,
		// Token: 0x040028E3 RID: 10467
		MVEX_Vaddnps_zmm_k1_zmm_zmmmt,
		// Token: 0x040028E4 RID: 10468
		MVEX_Vaddnpd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028E5 RID: 10469
		MVEX_Vgmaxabsps_zmm_k1_zmm_zmmmt,
		// Token: 0x040028E6 RID: 10470
		MVEX_Vgminps_zmm_k1_zmm_zmmmt,
		// Token: 0x040028E7 RID: 10471
		MVEX_Vgminpd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028E8 RID: 10472
		MVEX_Vgmaxps_zmm_k1_zmm_zmmmt,
		// Token: 0x040028E9 RID: 10473
		MVEX_Vgmaxpd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028EA RID: 10474
		MVEX_Undoc_zmm_k1_zmm_zmmmt_512_66_0F38_W0_54,
		// Token: 0x040028EB RID: 10475
		MVEX_Vfixupnanps_zmm_k1_zmm_zmmmt,
		// Token: 0x040028EC RID: 10476
		MVEX_Vfixupnanpd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028ED RID: 10477
		MVEX_Undoc_zmm_k1_zmm_zmmmt_512_66_0F38_W0_56,
		// Token: 0x040028EE RID: 10478
		MVEX_Undoc_zmm_k1_zmm_zmmmt_512_66_0F38_W0_57,
		// Token: 0x040028EF RID: 10479
		MVEX_Vpbroadcastd_zmm_k1_mt,
		// Token: 0x040028F0 RID: 10480
		MVEX_Vpbroadcastq_zmm_k1_mt,
		// Token: 0x040028F1 RID: 10481
		MVEX_Vbroadcasti32x4_zmm_k1_mt,
		// Token: 0x040028F2 RID: 10482
		MVEX_Vbroadcasti64x4_zmm_k1_mt,
		// Token: 0x040028F3 RID: 10483
		MVEX_Vpadcd_zmm_k1_kr_zmmmt,
		// Token: 0x040028F4 RID: 10484
		MVEX_Vpaddsetcd_zmm_k1_kr_zmmmt,
		// Token: 0x040028F5 RID: 10485
		MVEX_Vpsbbd_zmm_k1_kr_zmmmt,
		// Token: 0x040028F6 RID: 10486
		MVEX_Vpsubsetbd_zmm_k1_kr_zmmmt,
		// Token: 0x040028F7 RID: 10487
		MVEX_Vpblendmd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028F8 RID: 10488
		MVEX_Vpblendmq_zmm_k1_zmm_zmmmt,
		// Token: 0x040028F9 RID: 10489
		MVEX_Vblendmps_zmm_k1_zmm_zmmmt,
		// Token: 0x040028FA RID: 10490
		MVEX_Vblendmpd_zmm_k1_zmm_zmmmt,
		// Token: 0x040028FB RID: 10491
		MVEX_Undoc_zmm_k1_zmm_zmmmt_512_66_0F38_W0_67,
		// Token: 0x040028FC RID: 10492
		MVEX_Undoc_zmm_k1_zmmmt_512_66_0F38_W0_68,
		// Token: 0x040028FD RID: 10493
		MVEX_Undoc_zmm_k1_zmmmt_512_66_0F38_W0_69,
		// Token: 0x040028FE RID: 10494
		MVEX_Undoc_zmm_k1_zmmmt_512_66_0F38_W0_6A,
		// Token: 0x040028FF RID: 10495
		MVEX_Undoc_zmm_k1_zmmmt_512_66_0F38_W0_6B,
		// Token: 0x04002900 RID: 10496
		MVEX_Vpsubrd_zmm_k1_zmm_zmmmt,
		// Token: 0x04002901 RID: 10497
		MVEX_Vsubrps_zmm_k1_zmm_zmmmt,
		// Token: 0x04002902 RID: 10498
		MVEX_Vsubrpd_zmm_k1_zmm_zmmmt,
		// Token: 0x04002903 RID: 10499
		MVEX_Vpsbbrd_zmm_k1_kr_zmmmt,
		// Token: 0x04002904 RID: 10500
		MVEX_Vpsubrsetbd_zmm_k1_kr_zmmmt,
		// Token: 0x04002905 RID: 10501
		MVEX_Undoc_zmm_k1_zmm_zmmmt_512_66_0F38_W0_70,
		// Token: 0x04002906 RID: 10502
		MVEX_Undoc_zmm_k1_zmm_zmmmt_512_66_0F38_W0_71,
		// Token: 0x04002907 RID: 10503
		MVEX_Undoc_zmm_k1_zmm_zmmmt_512_66_0F38_W0_72,
		// Token: 0x04002908 RID: 10504
		MVEX_Undoc_zmm_k1_zmm_zmmmt_512_66_0F38_W0_73,
		// Token: 0x04002909 RID: 10505
		MVEX_Vpcmpltd_kr_k1_zmm_zmmmt,
		// Token: 0x0400290A RID: 10506
		MVEX_Vscaleps_zmm_k1_zmm_zmmmt,
		// Token: 0x0400290B RID: 10507
		MVEX_Vpmulhud_zmm_k1_zmm_zmmmt,
		// Token: 0x0400290C RID: 10508
		MVEX_Vpmulhd_zmm_k1_zmm_zmmmt,
		// Token: 0x0400290D RID: 10509
		MVEX_Vpgatherdd_zmm_k1_mvt,
		// Token: 0x0400290E RID: 10510
		MVEX_Vpgatherdq_zmm_k1_mvt,
		// Token: 0x0400290F RID: 10511
		MVEX_Vgatherdps_zmm_k1_mvt,
		// Token: 0x04002910 RID: 10512
		MVEX_Vgatherdpd_zmm_k1_mvt,
		// Token: 0x04002911 RID: 10513
		MVEX_Undoc_zmm_k1_zmm_zmmmt_512_66_0F38_W0_94,
		// Token: 0x04002912 RID: 10514
		MVEX_Undoc_zmm_k1_zmm_zmmmt_512_66_0F38_W1_94,
		// Token: 0x04002913 RID: 10515
		MVEX_Vfmadd132ps_zmm_k1_zmm_zmmmt,
		// Token: 0x04002914 RID: 10516
		MVEX_Vfmadd132pd_zmm_k1_zmm_zmmmt,
		// Token: 0x04002915 RID: 10517
		MVEX_Vfmsub132ps_zmm_k1_zmm_zmmmt,
		// Token: 0x04002916 RID: 10518
		MVEX_Vfmsub132pd_zmm_k1_zmm_zmmmt,
		// Token: 0x04002917 RID: 10519
		MVEX_Vfnmadd132ps_zmm_k1_zmm_zmmmt,
		// Token: 0x04002918 RID: 10520
		MVEX_Vfnmadd132pd_zmm_k1_zmm_zmmmt,
		// Token: 0x04002919 RID: 10521
		MVEX_Vfnmsub132ps_zmm_k1_zmm_zmmmt,
		// Token: 0x0400291A RID: 10522
		MVEX_Vfnmsub132pd_zmm_k1_zmm_zmmmt,
		// Token: 0x0400291B RID: 10523
		MVEX_Vpscatterdd_mvt_k1_zmm,
		// Token: 0x0400291C RID: 10524
		MVEX_Vpscatterdq_mvt_k1_zmm,
		// Token: 0x0400291D RID: 10525
		MVEX_Vscatterdps_mvt_k1_zmm,
		// Token: 0x0400291E RID: 10526
		MVEX_Vscatterdpd_mvt_k1_zmm,
		// Token: 0x0400291F RID: 10527
		MVEX_Vfmadd233ps_zmm_k1_zmm_zmmmt,
		// Token: 0x04002920 RID: 10528
		MVEX_Vfmadd213ps_zmm_k1_zmm_zmmmt,
		// Token: 0x04002921 RID: 10529
		MVEX_Vfmadd213pd_zmm_k1_zmm_zmmmt,
		// Token: 0x04002922 RID: 10530
		MVEX_Vfmsub213ps_zmm_k1_zmm_zmmmt,
		// Token: 0x04002923 RID: 10531
		MVEX_Vfmsub213pd_zmm_k1_zmm_zmmmt,
		// Token: 0x04002924 RID: 10532
		MVEX_Vfnmadd213ps_zmm_k1_zmm_zmmmt,
		// Token: 0x04002925 RID: 10533
		MVEX_Vfnmadd213pd_zmm_k1_zmm_zmmmt,
		// Token: 0x04002926 RID: 10534
		MVEX_Vfnmsub213ps_zmm_k1_zmm_zmmmt,
		// Token: 0x04002927 RID: 10535
		MVEX_Vfnmsub213pd_zmm_k1_zmm_zmmmt,
		// Token: 0x04002928 RID: 10536
		MVEX_Undoc_zmm_k1_mvt_512_66_0F38_W0_B0,
		// Token: 0x04002929 RID: 10537
		MVEX_Undoc_zmm_k1_mvt_512_66_0F38_W0_B2,
		// Token: 0x0400292A RID: 10538
		MVEX_Vpmadd233d_zmm_k1_zmm_zmmmt,
		// Token: 0x0400292B RID: 10539
		MVEX_Vpmadd231d_zmm_k1_zmm_zmmmt,
		// Token: 0x0400292C RID: 10540
		MVEX_Vfmadd231ps_zmm_k1_zmm_zmmmt,
		// Token: 0x0400292D RID: 10541
		MVEX_Vfmadd231pd_zmm_k1_zmm_zmmmt,
		// Token: 0x0400292E RID: 10542
		MVEX_Vfmsub231ps_zmm_k1_zmm_zmmmt,
		// Token: 0x0400292F RID: 10543
		MVEX_Vfmsub231pd_zmm_k1_zmm_zmmmt,
		// Token: 0x04002930 RID: 10544
		MVEX_Vfnmadd231ps_zmm_k1_zmm_zmmmt,
		// Token: 0x04002931 RID: 10545
		MVEX_Vfnmadd231pd_zmm_k1_zmm_zmmmt,
		// Token: 0x04002932 RID: 10546
		MVEX_Vfnmsub231ps_zmm_k1_zmm_zmmmt,
		// Token: 0x04002933 RID: 10547
		MVEX_Vfnmsub231pd_zmm_k1_zmm_zmmmt,
		// Token: 0x04002934 RID: 10548
		MVEX_Undoc_zmm_k1_mvt_512_66_0F38_W0_C0,
		// Token: 0x04002935 RID: 10549
		MVEX_Vgatherpf0hintdps_mvt_k1,
		// Token: 0x04002936 RID: 10550
		MVEX_Vgatherpf0hintdpd_mvt_k1,
		// Token: 0x04002937 RID: 10551
		MVEX_Vgatherpf0dps_mvt_k1,
		// Token: 0x04002938 RID: 10552
		MVEX_Vgatherpf1dps_mvt_k1,
		// Token: 0x04002939 RID: 10553
		MVEX_Vscatterpf0hintdps_mvt_k1,
		// Token: 0x0400293A RID: 10554
		MVEX_Vscatterpf0hintdpd_mvt_k1,
		// Token: 0x0400293B RID: 10555
		MVEX_Vscatterpf0dps_mvt_k1,
		// Token: 0x0400293C RID: 10556
		MVEX_Vscatterpf1dps_mvt_k1,
		// Token: 0x0400293D RID: 10557
		MVEX_Vexp223ps_zmm_k1_zmmmt,
		// Token: 0x0400293E RID: 10558
		MVEX_Vlog2ps_zmm_k1_zmmmt,
		// Token: 0x0400293F RID: 10559
		MVEX_Vrcp23ps_zmm_k1_zmmmt,
		// Token: 0x04002940 RID: 10560
		MVEX_Vrsqrt23ps_zmm_k1_zmmmt,
		// Token: 0x04002941 RID: 10561
		MVEX_Vaddsetsps_zmm_k1_zmm_zmmmt,
		// Token: 0x04002942 RID: 10562
		MVEX_Vpaddsetsd_zmm_k1_zmm_zmmmt,
		// Token: 0x04002943 RID: 10563
		MVEX_Undoc_zmm_k1_zmm_zmmmt_512_66_0F38_W0_CE,
		// Token: 0x04002944 RID: 10564
		MVEX_Undoc_zmm_k1_zmm_zmmmt_512_66_0F38_W1_CE,
		// Token: 0x04002945 RID: 10565
		MVEX_Undoc_zmm_k1_zmm_zmmmt_512_66_0F38_W0_CF,
		// Token: 0x04002946 RID: 10566
		MVEX_Vloadunpackld_zmm_k1_mt,
		// Token: 0x04002947 RID: 10567
		MVEX_Vloadunpacklq_zmm_k1_mt,
		// Token: 0x04002948 RID: 10568
		MVEX_Vpackstoreld_mt_k1_zmm,
		// Token: 0x04002949 RID: 10569
		MVEX_Vpackstorelq_mt_k1_zmm,
		// Token: 0x0400294A RID: 10570
		MVEX_Vloadunpacklps_zmm_k1_mt,
		// Token: 0x0400294B RID: 10571
		MVEX_Vloadunpacklpd_zmm_k1_mt,
		// Token: 0x0400294C RID: 10572
		MVEX_Vpackstorelps_mt_k1_zmm,
		// Token: 0x0400294D RID: 10573
		MVEX_Vpackstorelpd_mt_k1_zmm,
		// Token: 0x0400294E RID: 10574
		MVEX_Undoc_zmm_k1_zmmmt_512_0F38_W0_D2,
		// Token: 0x0400294F RID: 10575
		MVEX_Undoc_zmm_k1_zmmmt_512_66_0F38_W0_D2,
		// Token: 0x04002950 RID: 10576
		MVEX_Undoc_zmm_k1_zmmmt_512_0F38_W0_D3,
		// Token: 0x04002951 RID: 10577
		MVEX_Vloadunpackhd_zmm_k1_mt,
		// Token: 0x04002952 RID: 10578
		MVEX_Vloadunpackhq_zmm_k1_mt,
		// Token: 0x04002953 RID: 10579
		MVEX_Vpackstorehd_mt_k1_zmm,
		// Token: 0x04002954 RID: 10580
		MVEX_Vpackstorehq_mt_k1_zmm,
		// Token: 0x04002955 RID: 10581
		MVEX_Vloadunpackhps_zmm_k1_mt,
		// Token: 0x04002956 RID: 10582
		MVEX_Vloadunpackhpd_zmm_k1_mt,
		// Token: 0x04002957 RID: 10583
		MVEX_Vpackstorehps_mt_k1_zmm,
		// Token: 0x04002958 RID: 10584
		MVEX_Vpackstorehpd_mt_k1_zmm,
		// Token: 0x04002959 RID: 10585
		MVEX_Undoc_zmm_k1_zmmmt_512_0F38_W0_D6,
		// Token: 0x0400295A RID: 10586
		MVEX_Undoc_zmm_k1_zmmmt_512_66_0F38_W0_D6,
		// Token: 0x0400295B RID: 10587
		MVEX_Undoc_zmm_k1_zmmmt_512_0F38_W0_D7,
		// Token: 0x0400295C RID: 10588
		MVEX_Valignd_zmm_k1_zmm_zmmmt_imm8,
		// Token: 0x0400295D RID: 10589
		MVEX_Vpermf32x4_zmm_k1_zmmmt_imm8,
		// Token: 0x0400295E RID: 10590
		MVEX_Vpcmpud_kr_k1_zmm_zmmmt_imm8,
		// Token: 0x0400295F RID: 10591
		MVEX_Vpcmpd_kr_k1_zmm_zmmmt_imm8,
		// Token: 0x04002960 RID: 10592
		MVEX_Vgetmantps_zmm_k1_zmmmt_imm8,
		// Token: 0x04002961 RID: 10593
		MVEX_Vgetmantpd_zmm_k1_zmmmt_imm8,
		// Token: 0x04002962 RID: 10594
		MVEX_Vrndfxpntps_zmm_k1_zmmmt_imm8,
		// Token: 0x04002963 RID: 10595
		MVEX_Vrndfxpntpd_zmm_k1_zmmmt_imm8,
		// Token: 0x04002964 RID: 10596
		MVEX_Vcvtfxpntudq2ps_zmm_k1_zmmmt_imm8,
		// Token: 0x04002965 RID: 10597
		MVEX_Vcvtfxpntps2udq_zmm_k1_zmmmt_imm8,
		// Token: 0x04002966 RID: 10598
		MVEX_Vcvtfxpntpd2udq_zmm_k1_zmmmt_imm8,
		// Token: 0x04002967 RID: 10599
		MVEX_Vcvtfxpntdq2ps_zmm_k1_zmmmt_imm8,
		// Token: 0x04002968 RID: 10600
		MVEX_Vcvtfxpntps2dq_zmm_k1_zmmmt_imm8,
		// Token: 0x04002969 RID: 10601
		MVEX_Undoc_zmm_k1_zmmmt_imm8_512_66_0F3A_W0_D0,
		// Token: 0x0400296A RID: 10602
		MVEX_Undoc_zmm_k1_zmmmt_imm8_512_66_0F3A_W0_D1,
		// Token: 0x0400296B RID: 10603
		MVEX_Vcvtfxpntpd2dq_zmm_k1_zmmmt_imm8,
		// Token: 0x0400296C RID: 10604
		Via_undoc_F30FA6F0_16,
		// Token: 0x0400296D RID: 10605
		Via_undoc_F30FA6F0_32,
		// Token: 0x0400296E RID: 10606
		Via_undoc_F30FA6F0_64,
		// Token: 0x0400296F RID: 10607
		Via_undoc_F30FA6F8_16,
		// Token: 0x04002970 RID: 10608
		Via_undoc_F30FA6F8_32,
		// Token: 0x04002971 RID: 10609
		Via_undoc_F30FA6F8_64,
		// Token: 0x04002972 RID: 10610
		Xsha512_16,
		// Token: 0x04002973 RID: 10611
		Xsha512_32,
		// Token: 0x04002974 RID: 10612
		Xsha512_64,
		// Token: 0x04002975 RID: 10613
		Xstore_alt_16,
		// Token: 0x04002976 RID: 10614
		Xstore_alt_32,
		// Token: 0x04002977 RID: 10615
		Xstore_alt_64,
		// Token: 0x04002978 RID: 10616
		Xsha512_alt_16,
		// Token: 0x04002979 RID: 10617
		Xsha512_alt_32,
		// Token: 0x0400297A RID: 10618
		Xsha512_alt_64,
		// Token: 0x0400297B RID: 10619
		Zero_bytes,
		// Token: 0x0400297C RID: 10620
		Wrmsrns,
		// Token: 0x0400297D RID: 10621
		Wrmsrlist,
		// Token: 0x0400297E RID: 10622
		Rdmsrlist,
		// Token: 0x0400297F RID: 10623
		Rmpquery,
		// Token: 0x04002980 RID: 10624
		Prefetchit1_m8,
		// Token: 0x04002981 RID: 10625
		Prefetchit0_m8,
		// Token: 0x04002982 RID: 10626
		Aadd_m32_r32,
		// Token: 0x04002983 RID: 10627
		Aadd_m64_r64,
		// Token: 0x04002984 RID: 10628
		Aand_m32_r32,
		// Token: 0x04002985 RID: 10629
		Aand_m64_r64,
		// Token: 0x04002986 RID: 10630
		Axor_m32_r32,
		// Token: 0x04002987 RID: 10631
		Axor_m64_r64,
		// Token: 0x04002988 RID: 10632
		Aor_m32_r32,
		// Token: 0x04002989 RID: 10633
		Aor_m64_r64,
		// Token: 0x0400298A RID: 10634
		VEX_Vpdpbuud_xmm_xmm_xmmm128,
		// Token: 0x0400298B RID: 10635
		VEX_Vpdpbuud_ymm_ymm_ymmm256,
		// Token: 0x0400298C RID: 10636
		VEX_Vpdpbsud_xmm_xmm_xmmm128,
		// Token: 0x0400298D RID: 10637
		VEX_Vpdpbsud_ymm_ymm_ymmm256,
		// Token: 0x0400298E RID: 10638
		VEX_Vpdpbssd_xmm_xmm_xmmm128,
		// Token: 0x0400298F RID: 10639
		VEX_Vpdpbssd_ymm_ymm_ymmm256,
		// Token: 0x04002990 RID: 10640
		VEX_Vpdpbuuds_xmm_xmm_xmmm128,
		// Token: 0x04002991 RID: 10641
		VEX_Vpdpbuuds_ymm_ymm_ymmm256,
		// Token: 0x04002992 RID: 10642
		VEX_Vpdpbsuds_xmm_xmm_xmmm128,
		// Token: 0x04002993 RID: 10643
		VEX_Vpdpbsuds_ymm_ymm_ymmm256,
		// Token: 0x04002994 RID: 10644
		VEX_Vpdpbssds_xmm_xmm_xmmm128,
		// Token: 0x04002995 RID: 10645
		VEX_Vpdpbssds_ymm_ymm_ymmm256,
		// Token: 0x04002996 RID: 10646
		VEX_Tdpfp16ps_tmm_tmm_tmm,
		// Token: 0x04002997 RID: 10647
		VEX_Vcvtneps2bf16_xmm_xmmm128,
		// Token: 0x04002998 RID: 10648
		VEX_Vcvtneps2bf16_xmm_ymmm256,
		// Token: 0x04002999 RID: 10649
		VEX_Vcvtneoph2ps_xmm_m128,
		// Token: 0x0400299A RID: 10650
		VEX_Vcvtneoph2ps_ymm_m256,
		// Token: 0x0400299B RID: 10651
		VEX_Vcvtneeph2ps_xmm_m128,
		// Token: 0x0400299C RID: 10652
		VEX_Vcvtneeph2ps_ymm_m256,
		// Token: 0x0400299D RID: 10653
		VEX_Vcvtneebf162ps_xmm_m128,
		// Token: 0x0400299E RID: 10654
		VEX_Vcvtneebf162ps_ymm_m256,
		// Token: 0x0400299F RID: 10655
		VEX_Vcvtneobf162ps_xmm_m128,
		// Token: 0x040029A0 RID: 10656
		VEX_Vcvtneobf162ps_ymm_m256,
		// Token: 0x040029A1 RID: 10657
		VEX_Vbcstnesh2ps_xmm_m16,
		// Token: 0x040029A2 RID: 10658
		VEX_Vbcstnesh2ps_ymm_m16,
		// Token: 0x040029A3 RID: 10659
		VEX_Vbcstnebf162ps_xmm_m16,
		// Token: 0x040029A4 RID: 10660
		VEX_Vbcstnebf162ps_ymm_m16,
		// Token: 0x040029A5 RID: 10661
		VEX_Vpmadd52luq_xmm_xmm_xmmm128,
		// Token: 0x040029A6 RID: 10662
		VEX_Vpmadd52luq_ymm_ymm_ymmm256,
		// Token: 0x040029A7 RID: 10663
		VEX_Vpmadd52huq_xmm_xmm_xmmm128,
		// Token: 0x040029A8 RID: 10664
		VEX_Vpmadd52huq_ymm_ymm_ymmm256,
		// Token: 0x040029A9 RID: 10665
		VEX_Cmpoxadd_m32_r32_r32,
		// Token: 0x040029AA RID: 10666
		VEX_Cmpoxadd_m64_r64_r64,
		// Token: 0x040029AB RID: 10667
		VEX_Cmpnoxadd_m32_r32_r32,
		// Token: 0x040029AC RID: 10668
		VEX_Cmpnoxadd_m64_r64_r64,
		// Token: 0x040029AD RID: 10669
		VEX_Cmpbxadd_m32_r32_r32,
		// Token: 0x040029AE RID: 10670
		VEX_Cmpbxadd_m64_r64_r64,
		// Token: 0x040029AF RID: 10671
		VEX_Cmpnbxadd_m32_r32_r32,
		// Token: 0x040029B0 RID: 10672
		VEX_Cmpnbxadd_m64_r64_r64,
		// Token: 0x040029B1 RID: 10673
		VEX_Cmpzxadd_m32_r32_r32,
		// Token: 0x040029B2 RID: 10674
		VEX_Cmpzxadd_m64_r64_r64,
		// Token: 0x040029B3 RID: 10675
		VEX_Cmpnzxadd_m32_r32_r32,
		// Token: 0x040029B4 RID: 10676
		VEX_Cmpnzxadd_m64_r64_r64,
		// Token: 0x040029B5 RID: 10677
		VEX_Cmpbexadd_m32_r32_r32,
		// Token: 0x040029B6 RID: 10678
		VEX_Cmpbexadd_m64_r64_r64,
		// Token: 0x040029B7 RID: 10679
		VEX_Cmpnbexadd_m32_r32_r32,
		// Token: 0x040029B8 RID: 10680
		VEX_Cmpnbexadd_m64_r64_r64,
		// Token: 0x040029B9 RID: 10681
		VEX_Cmpsxadd_m32_r32_r32,
		// Token: 0x040029BA RID: 10682
		VEX_Cmpsxadd_m64_r64_r64,
		// Token: 0x040029BB RID: 10683
		VEX_Cmpnsxadd_m32_r32_r32,
		// Token: 0x040029BC RID: 10684
		VEX_Cmpnsxadd_m64_r64_r64,
		// Token: 0x040029BD RID: 10685
		VEX_Cmppxadd_m32_r32_r32,
		// Token: 0x040029BE RID: 10686
		VEX_Cmppxadd_m64_r64_r64,
		// Token: 0x040029BF RID: 10687
		VEX_Cmpnpxadd_m32_r32_r32,
		// Token: 0x040029C0 RID: 10688
		VEX_Cmpnpxadd_m64_r64_r64,
		// Token: 0x040029C1 RID: 10689
		VEX_Cmplxadd_m32_r32_r32,
		// Token: 0x040029C2 RID: 10690
		VEX_Cmplxadd_m64_r64_r64,
		// Token: 0x040029C3 RID: 10691
		VEX_Cmpnlxadd_m32_r32_r32,
		// Token: 0x040029C4 RID: 10692
		VEX_Cmpnlxadd_m64_r64_r64,
		// Token: 0x040029C5 RID: 10693
		VEX_Cmplexadd_m32_r32_r32,
		// Token: 0x040029C6 RID: 10694
		VEX_Cmplexadd_m64_r64_r64,
		// Token: 0x040029C7 RID: 10695
		VEX_Cmpnlexadd_m32_r32_r32,
		// Token: 0x040029C8 RID: 10696
		VEX_Cmpnlexadd_m64_r64_r64,
		// Token: 0x040029C9 RID: 10697
		VEX_Tcmmrlfp16ps_tmm_tmm_tmm,
		// Token: 0x040029CA RID: 10698
		VEX_Tcmmimfp16ps_tmm_tmm_tmm,
		// Token: 0x040029CB RID: 10699
		Pbndkb,
		// Token: 0x040029CC RID: 10700
		VEX_Vsha512rnds2_ymm_ymm_xmm,
		// Token: 0x040029CD RID: 10701
		VEX_Vsha512msg1_ymm_xmm,
		// Token: 0x040029CE RID: 10702
		VEX_Vsha512msg2_ymm_ymm,
		// Token: 0x040029CF RID: 10703
		VEX_Vpdpwuud_xmm_xmm_xmmm128,
		// Token: 0x040029D0 RID: 10704
		VEX_Vpdpwuud_ymm_ymm_ymmm256,
		// Token: 0x040029D1 RID: 10705
		VEX_Vpdpwusd_xmm_xmm_xmmm128,
		// Token: 0x040029D2 RID: 10706
		VEX_Vpdpwusd_ymm_ymm_ymmm256,
		// Token: 0x040029D3 RID: 10707
		VEX_Vpdpwsud_xmm_xmm_xmmm128,
		// Token: 0x040029D4 RID: 10708
		VEX_Vpdpwsud_ymm_ymm_ymmm256,
		// Token: 0x040029D5 RID: 10709
		VEX_Vpdpwuuds_xmm_xmm_xmmm128,
		// Token: 0x040029D6 RID: 10710
		VEX_Vpdpwuuds_ymm_ymm_ymmm256,
		// Token: 0x040029D7 RID: 10711
		VEX_Vpdpwusds_xmm_xmm_xmmm128,
		// Token: 0x040029D8 RID: 10712
		VEX_Vpdpwusds_ymm_ymm_ymmm256,
		// Token: 0x040029D9 RID: 10713
		VEX_Vpdpwsuds_xmm_xmm_xmmm128,
		// Token: 0x040029DA RID: 10714
		VEX_Vpdpwsuds_ymm_ymm_ymmm256,
		// Token: 0x040029DB RID: 10715
		VEX_Vsm3msg1_xmm_xmm_xmmm128,
		// Token: 0x040029DC RID: 10716
		VEX_Vsm3msg2_xmm_xmm_xmmm128,
		// Token: 0x040029DD RID: 10717
		VEX_Vsm4key4_xmm_xmm_xmmm128,
		// Token: 0x040029DE RID: 10718
		VEX_Vsm4key4_ymm_ymm_ymmm256,
		// Token: 0x040029DF RID: 10719
		VEX_Vsm4rnds4_xmm_xmm_xmmm128,
		// Token: 0x040029E0 RID: 10720
		VEX_Vsm4rnds4_ymm_ymm_ymmm256,
		// Token: 0x040029E1 RID: 10721
		VEX_Vsm3rnds2_xmm_xmm_xmmm128_imm8
	}
}
