namespace TaleWorlds.Core;

public enum GameManagerLoadingSteps
{
	None = -1,
	PreInitializeZerothStep,
	FirstInitializeFirstStep,
	WaitSecondStep,
	SecondInitializeThirdState,
	PostInitializeFourthState,
	FinishLoadingFifthStep,
	LoadingIsOver
}
