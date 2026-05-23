using System;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006A8 RID: 1704
	internal enum LegacyOpCodeHandlerKind : byte
	{
		// Token: 0x04003596 RID: 13718
		Bitness,
		// Token: 0x04003597 RID: 13719
		Bitness_DontReadModRM,
		// Token: 0x04003598 RID: 13720
		Invalid,
		// Token: 0x04003599 RID: 13721
		Invalid_NoModRM,
		// Token: 0x0400359A RID: 13722
		Invalid2,
		// Token: 0x0400359B RID: 13723
		Dup,
		// Token: 0x0400359C RID: 13724
		Null,
		// Token: 0x0400359D RID: 13725
		HandlerReference,
		// Token: 0x0400359E RID: 13726
		ArrayReference,
		// Token: 0x0400359F RID: 13727
		RM,
		// Token: 0x040035A0 RID: 13728
		Options3,
		// Token: 0x040035A1 RID: 13729
		Options5,
		// Token: 0x040035A2 RID: 13730
		Options_DontReadModRM,
		// Token: 0x040035A3 RID: 13731
		AnotherTable,
		// Token: 0x040035A4 RID: 13732
		Group,
		// Token: 0x040035A5 RID: 13733
		Group8x64,
		// Token: 0x040035A6 RID: 13734
		Group8x8,
		// Token: 0x040035A7 RID: 13735
		MandatoryPrefix,
		// Token: 0x040035A8 RID: 13736
		MandatoryPrefix4,
		// Token: 0x040035A9 RID: 13737
		Ev_REXW_1a,
		// Token: 0x040035AA RID: 13738
		MandatoryPrefix_NoModRM,
		// Token: 0x040035AB RID: 13739
		MandatoryPrefix3,
		// Token: 0x040035AC RID: 13740
		D3NOW,
		// Token: 0x040035AD RID: 13741
		EVEX,
		// Token: 0x040035AE RID: 13742
		VEX2,
		// Token: 0x040035AF RID: 13743
		VEX3,
		// Token: 0x040035B0 RID: 13744
		XOP,
		// Token: 0x040035B1 RID: 13745
		AL_DX,
		// Token: 0x040035B2 RID: 13746
		Ap,
		// Token: 0x040035B3 RID: 13747
		B_BM,
		// Token: 0x040035B4 RID: 13748
		B_Ev,
		// Token: 0x040035B5 RID: 13749
		B_MIB,
		// Token: 0x040035B6 RID: 13750
		BM_B,
		// Token: 0x040035B7 RID: 13751
		BranchIw,
		// Token: 0x040035B8 RID: 13752
		BranchSimple,
		// Token: 0x040035B9 RID: 13753
		C_R_3a,
		// Token: 0x040035BA RID: 13754
		C_R_3b,
		// Token: 0x040035BB RID: 13755
		DX_AL,
		// Token: 0x040035BC RID: 13756
		DX_eAX,
		// Token: 0x040035BD RID: 13757
		eAX_DX,
		// Token: 0x040035BE RID: 13758
		Eb_1,
		// Token: 0x040035BF RID: 13759
		Eb_2,
		// Token: 0x040035C0 RID: 13760
		Eb_CL,
		// Token: 0x040035C1 RID: 13761
		Eb_Gb_1,
		// Token: 0x040035C2 RID: 13762
		Eb_Gb_2,
		// Token: 0x040035C3 RID: 13763
		Eb_Ib_1,
		// Token: 0x040035C4 RID: 13764
		Eb_Ib_2,
		// Token: 0x040035C5 RID: 13765
		Eb1,
		// Token: 0x040035C6 RID: 13766
		Ed_V_Ib,
		// Token: 0x040035C7 RID: 13767
		Ep,
		// Token: 0x040035C8 RID: 13768
		Ev_3a,
		// Token: 0x040035C9 RID: 13769
		Ev_3b,
		// Token: 0x040035CA RID: 13770
		Ev_4,
		// Token: 0x040035CB RID: 13771
		Ev_CL,
		// Token: 0x040035CC RID: 13772
		Ev_Gv_32_64,
		// Token: 0x040035CD RID: 13773
		Ev_Gv_3a,
		// Token: 0x040035CE RID: 13774
		Ev_Gv_3b,
		// Token: 0x040035CF RID: 13775
		Ev_Gv_4,
		// Token: 0x040035D0 RID: 13776
		Ev_Gv_CL,
		// Token: 0x040035D1 RID: 13777
		Ev_Gv_Ib,
		// Token: 0x040035D2 RID: 13778
		Ev_Gv_REX,
		// Token: 0x040035D3 RID: 13779
		Ev_Ib_3,
		// Token: 0x040035D4 RID: 13780
		Ev_Ib_4,
		// Token: 0x040035D5 RID: 13781
		Ev_Ib2_3,
		// Token: 0x040035D6 RID: 13782
		Ev_Ib2_4,
		// Token: 0x040035D7 RID: 13783
		Ev_Iz_3,
		// Token: 0x040035D8 RID: 13784
		Ev_Iz_4,
		// Token: 0x040035D9 RID: 13785
		Ev_P,
		// Token: 0x040035DA RID: 13786
		Ev_REXW,
		// Token: 0x040035DB RID: 13787
		Ev_Sw,
		// Token: 0x040035DC RID: 13788
		Ev_VX,
		// Token: 0x040035DD RID: 13789
		Ev1,
		// Token: 0x040035DE RID: 13790
		Evj,
		// Token: 0x040035DF RID: 13791
		Evw,
		// Token: 0x040035E0 RID: 13792
		Ew,
		// Token: 0x040035E1 RID: 13793
		Gb_Eb,
		// Token: 0x040035E2 RID: 13794
		Gdq_Ev,
		// Token: 0x040035E3 RID: 13795
		Gv_Eb,
		// Token: 0x040035E4 RID: 13796
		Gv_Eb_REX,
		// Token: 0x040035E5 RID: 13797
		Gv_Ev_32_64,
		// Token: 0x040035E6 RID: 13798
		Gv_Ev_3a,
		// Token: 0x040035E7 RID: 13799
		Gv_Ev_3b,
		// Token: 0x040035E8 RID: 13800
		Gv_Ev_Ib,
		// Token: 0x040035E9 RID: 13801
		Gv_Ev_Ib_REX,
		// Token: 0x040035EA RID: 13802
		Gv_Ev_Iz,
		// Token: 0x040035EB RID: 13803
		Gv_Ev_REX,
		// Token: 0x040035EC RID: 13804
		Gv_Ev2,
		// Token: 0x040035ED RID: 13805
		Gv_Ev3,
		// Token: 0x040035EE RID: 13806
		Gv_Ew,
		// Token: 0x040035EF RID: 13807
		Gv_M,
		// Token: 0x040035F0 RID: 13808
		Gv_M_as,
		// Token: 0x040035F1 RID: 13809
		Gv_Ma,
		// Token: 0x040035F2 RID: 13810
		Gv_Mp_2,
		// Token: 0x040035F3 RID: 13811
		Gv_Mp_3,
		// Token: 0x040035F4 RID: 13812
		Gv_Mv,
		// Token: 0x040035F5 RID: 13813
		Gv_N,
		// Token: 0x040035F6 RID: 13814
		Gv_N_Ib_REX,
		// Token: 0x040035F7 RID: 13815
		Gv_RX,
		// Token: 0x040035F8 RID: 13816
		Gv_W,
		// Token: 0x040035F9 RID: 13817
		GvM_VX_Ib,
		// Token: 0x040035FA RID: 13818
		Ib,
		// Token: 0x040035FB RID: 13819
		Ib3,
		// Token: 0x040035FC RID: 13820
		IbReg,
		// Token: 0x040035FD RID: 13821
		IbReg2,
		// Token: 0x040035FE RID: 13822
		Iw_Ib,
		// Token: 0x040035FF RID: 13823
		Jb,
		// Token: 0x04003600 RID: 13824
		Jb2,
		// Token: 0x04003601 RID: 13825
		Jdisp,
		// Token: 0x04003602 RID: 13826
		Jx,
		// Token: 0x04003603 RID: 13827
		Jz,
		// Token: 0x04003604 RID: 13828
		M_1,
		// Token: 0x04003605 RID: 13829
		M_2,
		// Token: 0x04003606 RID: 13830
		M_REXW_2,
		// Token: 0x04003607 RID: 13831
		M_REXW_4,
		// Token: 0x04003608 RID: 13832
		MemBx,
		// Token: 0x04003609 RID: 13833
		Mf_1,
		// Token: 0x0400360A RID: 13834
		Mf_2a,
		// Token: 0x0400360B RID: 13835
		Mf_2b,
		// Token: 0x0400360C RID: 13836
		MIB_B,
		// Token: 0x0400360D RID: 13837
		MP,
		// Token: 0x0400360E RID: 13838
		Ms,
		// Token: 0x0400360F RID: 13839
		MV,
		// Token: 0x04003610 RID: 13840
		Mv_Gv,
		// Token: 0x04003611 RID: 13841
		Mv_Gv_REXW,
		// Token: 0x04003612 RID: 13842
		NIb,
		// Token: 0x04003613 RID: 13843
		Ob_Reg,
		// Token: 0x04003614 RID: 13844
		Ov_Reg,
		// Token: 0x04003615 RID: 13845
		P_Ev,
		// Token: 0x04003616 RID: 13846
		P_Ev_Ib,
		// Token: 0x04003617 RID: 13847
		P_Q,
		// Token: 0x04003618 RID: 13848
		P_Q_Ib,
		// Token: 0x04003619 RID: 13849
		P_R,
		// Token: 0x0400361A RID: 13850
		P_W,
		// Token: 0x0400361B RID: 13851
		PushEv,
		// Token: 0x0400361C RID: 13852
		PushIb2,
		// Token: 0x0400361D RID: 13853
		PushIz,
		// Token: 0x0400361E RID: 13854
		PushOpSizeReg_4a,
		// Token: 0x0400361F RID: 13855
		PushOpSizeReg_4b,
		// Token: 0x04003620 RID: 13856
		PushSimple2,
		// Token: 0x04003621 RID: 13857
		PushSimpleReg,
		// Token: 0x04003622 RID: 13858
		Q_P,
		// Token: 0x04003623 RID: 13859
		R_C_3a,
		// Token: 0x04003624 RID: 13860
		R_C_3b,
		// Token: 0x04003625 RID: 13861
		rDI_P_N,
		// Token: 0x04003626 RID: 13862
		rDI_VX_RX,
		// Token: 0x04003627 RID: 13863
		Reg,
		// Token: 0x04003628 RID: 13864
		Reg_Ib2,
		// Token: 0x04003629 RID: 13865
		Reg_Iz,
		// Token: 0x0400362A RID: 13866
		Reg_Ob,
		// Token: 0x0400362B RID: 13867
		Reg_Ov,
		// Token: 0x0400362C RID: 13868
		Reg_Xb,
		// Token: 0x0400362D RID: 13869
		Reg_Xv,
		// Token: 0x0400362E RID: 13870
		Reg_Xv2,
		// Token: 0x0400362F RID: 13871
		Reg_Yb,
		// Token: 0x04003630 RID: 13872
		Reg_Yv,
		// Token: 0x04003631 RID: 13873
		RegIb,
		// Token: 0x04003632 RID: 13874
		RegIb3,
		// Token: 0x04003633 RID: 13875
		RegIz2,
		// Token: 0x04003634 RID: 13876
		Reservednop,
		// Token: 0x04003635 RID: 13877
		RIb,
		// Token: 0x04003636 RID: 13878
		RIbIb,
		// Token: 0x04003637 RID: 13879
		Rv,
		// Token: 0x04003638 RID: 13880
		Rv_32_64,
		// Token: 0x04003639 RID: 13881
		RvMw_Gw,
		// Token: 0x0400363A RID: 13882
		Simple,
		// Token: 0x0400363B RID: 13883
		Simple_ModRM,
		// Token: 0x0400363C RID: 13884
		Simple2_3a,
		// Token: 0x0400363D RID: 13885
		Simple2_3b,
		// Token: 0x0400363E RID: 13886
		Simple2Iw,
		// Token: 0x0400363F RID: 13887
		Simple3,
		// Token: 0x04003640 RID: 13888
		Simple4,
		// Token: 0x04003641 RID: 13889
		Simple5,
		// Token: 0x04003642 RID: 13890
		Simple5_ModRM_as,
		// Token: 0x04003643 RID: 13891
		SimpleReg,
		// Token: 0x04003644 RID: 13892
		ST_STi,
		// Token: 0x04003645 RID: 13893
		STi,
		// Token: 0x04003646 RID: 13894
		STi_ST,
		// Token: 0x04003647 RID: 13895
		Sw_Ev,
		// Token: 0x04003648 RID: 13896
		V_Ev,
		// Token: 0x04003649 RID: 13897
		VM,
		// Token: 0x0400364A RID: 13898
		VN,
		// Token: 0x0400364B RID: 13899
		VQ,
		// Token: 0x0400364C RID: 13900
		VRIbIb,
		// Token: 0x0400364D RID: 13901
		VW_2,
		// Token: 0x0400364E RID: 13902
		VW_3,
		// Token: 0x0400364F RID: 13903
		VWIb_2,
		// Token: 0x04003650 RID: 13904
		VWIb_3,
		// Token: 0x04003651 RID: 13905
		VX_E_Ib,
		// Token: 0x04003652 RID: 13906
		VX_Ev,
		// Token: 0x04003653 RID: 13907
		Wbinvd,
		// Token: 0x04003654 RID: 13908
		WV,
		// Token: 0x04003655 RID: 13909
		Xb_Yb,
		// Token: 0x04003656 RID: 13910
		Xchg_Reg_rAX,
		// Token: 0x04003657 RID: 13911
		Xv_Yv,
		// Token: 0x04003658 RID: 13912
		Yb_Reg,
		// Token: 0x04003659 RID: 13913
		Yb_Xb,
		// Token: 0x0400365A RID: 13914
		Yv_Reg,
		// Token: 0x0400365B RID: 13915
		Yv_Reg2,
		// Token: 0x0400365C RID: 13916
		Yv_Xv,
		// Token: 0x0400365D RID: 13917
		Simple4b,
		// Token: 0x0400365E RID: 13918
		Options1632_1,
		// Token: 0x0400365F RID: 13919
		Options1632_2,
		// Token: 0x04003660 RID: 13920
		M_Sw,
		// Token: 0x04003661 RID: 13921
		Sw_M,
		// Token: 0x04003662 RID: 13922
		Rq,
		// Token: 0x04003663 RID: 13923
		Gd_Rd,
		// Token: 0x04003664 RID: 13924
		PrefixEsCsSsDs,
		// Token: 0x04003665 RID: 13925
		PrefixFsGs,
		// Token: 0x04003666 RID: 13926
		Prefix66,
		// Token: 0x04003667 RID: 13927
		Prefix67,
		// Token: 0x04003668 RID: 13928
		PrefixF0,
		// Token: 0x04003669 RID: 13929
		PrefixF2,
		// Token: 0x0400366A RID: 13930
		PrefixF3,
		// Token: 0x0400366B RID: 13931
		PrefixREX,
		// Token: 0x0400366C RID: 13932
		Simple5_a32
	}
}
