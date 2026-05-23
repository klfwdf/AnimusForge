using System;

namespace HarmonyLib
{
	/// <summary>Specifies the type of method</summary>
	// Token: 0x0200005E RID: 94
	public enum MethodType
	{
		/// <summary>This is a normal method</summary>
		// Token: 0x04000141 RID: 321
		Normal,
		/// <summary>This is a getter</summary>
		// Token: 0x04000142 RID: 322
		Getter,
		/// <summary>This is a setter</summary>
		// Token: 0x04000143 RID: 323
		Setter,
		/// <summary>This is a constructor</summary>
		// Token: 0x04000144 RID: 324
		Constructor,
		/// <summary>This is a static constructor</summary>
		// Token: 0x04000145 RID: 325
		StaticConstructor,
		/// <summary>This targets the MoveNext method of the enumerator result, that actually contains the method's implementation</summary>
		// Token: 0x04000146 RID: 326
		Enumerator,
		/// <summary>This targets the MoveNext method of the async state machine, that actually contains the method's implementation</summary>
		// Token: 0x04000147 RID: 327
		Async,
		/// <summary>Finalize</summary>
		// Token: 0x04000148 RID: 328
		Finalizer,
		/// <summary>This is a add event method</summary>
		// Token: 0x04000149 RID: 329
		EventAdd,
		/// <summary>This is a remove event method</summary>
		// Token: 0x0400014A RID: 330
		EventRemove,
		/// <summary>This is a op_Implicit</summary>
		// Token: 0x0400014B RID: 331
		OperatorImplicit,
		/// <summary>This is a op_Explicit</summary>
		// Token: 0x0400014C RID: 332
		OperatorExplicit,
		/// <summary>This is a op_UnaryPlus</summary>
		// Token: 0x0400014D RID: 333
		OperatorUnaryPlus,
		/// <summary>This is a op_UnaryNegation</summary>
		// Token: 0x0400014E RID: 334
		OperatorUnaryNegation,
		/// <summary>This is a op_LogicalNot</summary>
		// Token: 0x0400014F RID: 335
		OperatorLogicalNot,
		/// <summary>This is a op_OnesComplement</summary>
		// Token: 0x04000150 RID: 336
		OperatorOnesComplement,
		/// <summary>This is a op_Increment</summary>
		// Token: 0x04000151 RID: 337
		OperatorIncrement,
		/// <summary>This is a op_Decrement</summary>
		// Token: 0x04000152 RID: 338
		OperatorDecrement,
		/// <summary>This is a op_True</summary>
		// Token: 0x04000153 RID: 339
		OperatorTrue,
		/// <summary>This is a op_False</summary>
		// Token: 0x04000154 RID: 340
		OperatorFalse,
		/// <summary>This is a op_Addition</summary>
		// Token: 0x04000155 RID: 341
		OperatorAddition,
		/// <summary>This is a op_Subtraction</summary>
		// Token: 0x04000156 RID: 342
		OperatorSubtraction,
		/// <summary>This is a op_Multiply</summary>
		// Token: 0x04000157 RID: 343
		OperatorMultiply,
		/// <summary>This is a op_Division</summary>
		// Token: 0x04000158 RID: 344
		OperatorDivision,
		/// <summary>This is a op_Modulus</summary>
		// Token: 0x04000159 RID: 345
		OperatorModulus,
		/// <summary>This is a op_BitwiseAnd</summary>
		// Token: 0x0400015A RID: 346
		OperatorBitwiseAnd,
		/// <summary>This is a op_BitwiseOr</summary>
		// Token: 0x0400015B RID: 347
		OperatorBitwiseOr,
		/// <summary>This is a op_ExclusiveOr</summary>
		// Token: 0x0400015C RID: 348
		OperatorExclusiveOr,
		/// <summary>This is a op_LeftShift</summary>
		// Token: 0x0400015D RID: 349
		OperatorLeftShift,
		/// <summary>This is a op_RightShift</summary>
		// Token: 0x0400015E RID: 350
		OperatorRightShift,
		/// <summary>This is a op_Equality</summary>
		// Token: 0x0400015F RID: 351
		OperatorEquality,
		/// <summary>This is a op_Inequality</summary>
		// Token: 0x04000160 RID: 352
		OperatorInequality,
		/// <summary>This is a op_GreaterThan</summary>
		// Token: 0x04000161 RID: 353
		OperatorGreaterThan,
		/// <summary>This is a op_LessThan</summary>
		// Token: 0x04000162 RID: 354
		OperatorLessThan,
		/// <summary>This is a op_GreaterThanOrEqual</summary>
		// Token: 0x04000163 RID: 355
		OperatorGreaterThanOrEqual,
		/// <summary>This is a op_LessThanOrEqual</summary>
		// Token: 0x04000164 RID: 356
		OperatorLessThanOrEqual,
		/// <summary>This is a op_Comma</summary>
		// Token: 0x04000165 RID: 357
		OperatorComma
	}
}
