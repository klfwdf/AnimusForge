namespace psai.net;

internal interface IAudioPlaybackLayerChannel
{
	PsaiResult LoadSegment(Segment segment);

	PsaiResult ReleaseSegment();

	PsaiResult ScheduleSegmentPlayback(Segment segment, int delayMilliseconds);

	PsaiResult StopChannel();

	PsaiResult SetVolume(float volume);

	PsaiResult SetPaused(bool paused);

	void Release();
}
