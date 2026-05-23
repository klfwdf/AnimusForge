using System;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007DA RID: 2010
	internal enum VexOpCodeHandlerKind : byte
	{
		// Token: 0x04003905 RID: 14597
		Invalid,
		// Token: 0x04003906 RID: 14598
		Invalid2,
		// Token: 0x04003907 RID: 14599
		Dup,
		// Token: 0x04003908 RID: 14600
		Invalid_NoModRM,
		// Token: 0x04003909 RID: 14601
		Bitness_DontReadModRM,
		// Token: 0x0400390A RID: 14602
		HandlerReference,
		// Token: 0x0400390B RID: 14603
		ArrayReference,
		// Token: 0x0400390C RID: 14604
		RM,
		// Token: 0x0400390D RID: 14605
		Group,
		// Token: 0x0400390E RID: 14606
		W,
		// Token: 0x0400390F RID: 14607
		MandatoryPrefix2_1,
		// Token: 0x04003910 RID: 14608
		MandatoryPrefix2_4,
		// Token: 0x04003911 RID: 14609
		MandatoryPrefix2_NoModRM,
		// Token: 0x04003912 RID: 14610
		VectorLength_NoModRM,
		// Token: 0x04003913 RID: 14611
		VectorLength,
		// Token: 0x04003914 RID: 14612
		Ed_V_Ib,
		// Token: 0x04003915 RID: 14613
		Ev_VX,
		// Token: 0x04003916 RID: 14614
		G_VK,
		// Token: 0x04003917 RID: 14615
		Gv_Ev_Gv,
		// Token: 0x04003918 RID: 14616
		Gv_Ev_Ib,
		// Token: 0x04003919 RID: 14617
		Gv_Ev_Id,
		// Token: 0x0400391A RID: 14618
		Gv_GPR_Ib,
		// Token: 0x0400391B RID: 14619
		Gv_Gv_Ev,
		// Token: 0x0400391C RID: 14620
		Gv_RX,
		// Token: 0x0400391D RID: 14621
		Gv_W,
		// Token: 0x0400391E RID: 14622
		GvM_VX_Ib,
		// Token: 0x0400391F RID: 14623
		HRIb,
		// Token: 0x04003920 RID: 14624
		Hv_Ed_Id,
		// Token: 0x04003921 RID: 14625
		Hv_Ev,
		// Token: 0x04003922 RID: 14626
		M,
		// Token: 0x04003923 RID: 14627
		MHV,
		// Token: 0x04003924 RID: 14628
		M_VK,
		// Token: 0x04003925 RID: 14629
		MV,
		// Token: 0x04003926 RID: 14630
		rDI_VX_RX,
		// Token: 0x04003927 RID: 14631
		RdRq,
		// Token: 0x04003928 RID: 14632
		Simple,
		// Token: 0x04003929 RID: 14633
		VHEv,
		// Token: 0x0400392A RID: 14634
		VHEvIb,
		// Token: 0x0400392B RID: 14635
		VHIs4W,
		// Token: 0x0400392C RID: 14636
		VHIs5W,
		// Token: 0x0400392D RID: 14637
		VHM,
		// Token: 0x0400392E RID: 14638
		VHW_2,
		// Token: 0x0400392F RID: 14639
		VHW_3,
		// Token: 0x04003930 RID: 14640
		VHW_4,
		// Token: 0x04003931 RID: 14641
		VHWIb_2,
		// Token: 0x04003932 RID: 14642
		VHWIb_4,
		// Token: 0x04003933 RID: 14643
		VHWIs4,
		// Token: 0x04003934 RID: 14644
		VHWIs5,
		// Token: 0x04003935 RID: 14645
		VK_HK_RK,
		// Token: 0x04003936 RID: 14646
		VK_R,
		// Token: 0x04003937 RID: 14647
		VK_RK,
		// Token: 0x04003938 RID: 14648
		VK_RK_Ib,
		// Token: 0x04003939 RID: 14649
		VK_WK,
		// Token: 0x0400393A RID: 14650
		VM,
		// Token: 0x0400393B RID: 14651
		VW_2,
		// Token: 0x0400393C RID: 14652
		VW_3,
		// Token: 0x0400393D RID: 14653
		VWH,
		// Token: 0x0400393E RID: 14654
		VWIb_2,
		// Token: 0x0400393F RID: 14655
		VWIb_3,
		// Token: 0x04003940 RID: 14656
		VX_Ev,
		// Token: 0x04003941 RID: 14657
		VX_VSIB_HX,
		// Token: 0x04003942 RID: 14658
		WHV,
		// Token: 0x04003943 RID: 14659
		WV,
		// Token: 0x04003944 RID: 14660
		WVIb,
		// Token: 0x04003945 RID: 14661
		VT_SIBMEM,
		// Token: 0x04003946 RID: 14662
		SIBMEM_VT,
		// Token: 0x04003947 RID: 14663
		VT,
		// Token: 0x04003948 RID: 14664
		VT_RT_HT,
		// Token: 0x04003949 RID: 14665
		Group8x64,
		// Token: 0x0400394A RID: 14666
		Bitness,
		// Token: 0x0400394B RID: 14667
		Null,
		// Token: 0x0400394C RID: 14668
		Options_DontReadModRM,
		// Token: 0x0400394D RID: 14669
		Gq_HK_RK,
		// Token: 0x0400394E RID: 14670
		VK_R_Ib,
		// Token: 0x0400394F RID: 14671
		Gv_Ev,
		// Token: 0x04003950 RID: 14672
		Ev,
		// Token: 0x04003951 RID: 14673
		K_Jb,
		// Token: 0x04003952 RID: 14674
		K_Jz,
		// Token: 0x04003953 RID: 14675
		Ev_Gv_Gv
	}
}
