using AVFoundation;
using Foundation;

namespace MauiAudio;

internal class NativeAudioService : NativeAudioServiceBase
{
    AVAudioPlayer AVplayer;

    public override bool LoopMedia { get; set; }
    public override bool IsPlaying => AVplayer != null && AVplayer.Playing;
    public override double BufferedCoeff => Double.NaN;
    public override TimeSpan Duration => TimeSpan.FromSeconds(AVplayer?.Duration ?? 0);
    public override bool TryGetPosition(out TimeSpan position)
    {
        if (AVplayer != null)
        {
            position = TimeSpan.FromSeconds(AVplayer.CurrentTime);
            return true;
        }
        else
        {
            position = TimeSpan.Zero;
            return false;
        }
    }

    private protected override void SetVolume(double value)
    {
        if (AVplayer != null)
            AVplayer.Volume = Convert.ToSingle(value);
    }
    private protected override void SetBalance(double value)
    {
        if (AVplayer != null)
            AVplayer.Pan = Convert.ToSingle(value);
    }
    private protected override void SetMuted(bool value)
    {
        if (AVplayer != null)
        {
            if (value)
                AVplayer.Volume = 0;
            else
                AVplayer.Volume = Convert.ToSingle(Volume);
        }
    }

    private protected override void PrepareToStartNew(MediaContent media)
    {
        if (AVplayer != null) Pause();

        if (media.Stream != null)
        {
            // Using Stream
            var data = NSData.FromStream(media.Stream) ?? throw new Exception("Unable to convert audioStream to NSData.");
            AVplayer = AVAudioPlayer.FromData(data)
            ?? throw new Exception("Unable to create AVAudioPlayer from data.");
        }
        else
        {
            // Using URL
            NSUrl fileURL = new NSUrl(media.URL);
            AVplayer = AVAudioPlayer.FromUrl(fileURL);
        }

        {
            SetVolume(Volume);
            SetBalance(Balance);
            SetMuted(Muted);

            AVplayer.FinishedPlaying += OnPlayingEnded;
        }
    }
    public override void Next()
    {
        if (MediaNext != null)
        {
            MediaCurrent = MediaNext;
            PlayNew(MediaCurrent);
        }
    }
    public override void Pause() => AVplayer?.Pause();
    public override void Play() => AVplayer?.Play();
    public override void Stop() => AVplayer?.Stop();
    public override void Previous()
    {
        if (MediaPrevious != null)
        {
            MediaCurrent = MediaPrevious;
            PlayNew(MediaCurrent);
        }
    }
    public override void SeekTo(TimeSpan position) => AVplayer?.PlayAtTime(position.TotalSeconds);

    private protected override void Initialize() { }
    private protected override void SetMediaCurrent(MediaContent value) { }
    private protected override void SetMediaNext(MediaContent value) { }
    private protected override void SetMediaPrevious(MediaContent value) { }

    public override void Dispose() => AVplayer?.Dispose();
}