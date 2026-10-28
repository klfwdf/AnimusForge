namespace TaleWorlds.MountAndBlade;

public interface IFaceGeneratorHandler
{
	void ChangeToBodyCamera();

	void ChangeToEyeCamera();

	void ChangeToNoseCamera();

	void ChangeToMouthCamera();

	void ChangeToFaceCamera();

	void ChangeToHairCamera();

	void RefreshCharacterEntity();

	void MakeVoice();

	void MakeVoiceDelayed();

	void SetFacialAnimation(string faceAnimation, bool loop);

	void Done();

	void Cancel();

	void UndressCharacterEntity();

	void DressCharacterEntity();

	void DefaultFace();
}
