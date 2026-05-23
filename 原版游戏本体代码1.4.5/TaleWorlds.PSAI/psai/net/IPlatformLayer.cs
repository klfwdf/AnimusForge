using System.IO;

namespace psai.net;

internal interface IPlatformLayer
{
	void Initialize();

	void Release();

	Stream GetStreamOnPsaiSoundtrackFile(string filename);

	string ConvertFilePathForPlatform(string filepath);
}
