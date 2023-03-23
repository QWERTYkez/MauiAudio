using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace MauiAudio;

internal class NativeAudioService : NativeAudioServiceBase
{
    MediaPlayer MediaPlayer;

    private bool Buffering = false;
    private protected override void Initialize()
    {
        MediaPlayer = new();

        MediaPlayer.CommandManager.PlayReceived += (s, e) => Play();
        MediaPlayer.CommandManager.PauseReceived += (s, e) => Pause();
        MediaPlayer.CommandManager.PreviousReceived += (s, e) => Previous();
        MediaPlayer.CommandManager.NextReceived += (s, e) => Next();

        MediaPlayer.BufferingStarted += (s, e) =>
        {
            Task.Run(async () =>
            {
                Buffering = true;
                while (Buffering)
                {
                    OnBuffCoeffAccepted(BufferedCoeff);
                    await Task.Delay(500);
                }
            });
        };
        MediaPlayer.BufferingEnded += (s, e) =>
        {
            Buffering = false;
            OnBuffCoeffAccepted(1);
        };
        MediaPlayer.MediaOpened += (s, e) => OnDurationAccepted(Duration);
        MediaPlayer.MediaEnded += (s, e) => OnPlayingEnded(null);
    }


    public override bool LoopMedia { get; set; }
    public override bool IsPlaying => MediaPlayer != null && MediaPlayer.CurrentState == MediaPlayerState.Playing;
    public override double BufferedCoeff => MediaPlayer.BufferingProgress / 100;
    public override TimeSpan Duration => MediaPlayer?.NaturalDuration ?? TimeSpan.Zero;
    public override bool TryGetPosition(out TimeSpan position)
    {
        if (MediaPlayer != null)
        {
            position = MediaPlayer.Position;
            return true;
        }
        else
        {
            position = TimeSpan.Zero;
            return false;
        }
    }


    private protected override void SetVolume(double value) => MediaPlayer.Volume = value;
    private protected override void SetBalance(double value) => MediaPlayer.AudioBalance = value;
    private protected override void SetMuted(bool value) => MediaPlayer.IsMuted = value;


    private protected override void PrepareToStartNew(MediaContent media)
    {
        Pause();

        var mediaItem = new MediaPlaybackItem(media.Stream == null ? MediaSource.CreateFromUri(new Uri(media.URL)) : MediaSource.CreateFromStream(media.Stream?.AsRandomAccessStream(), string.Empty));
        var props = mediaItem.GetDisplayProperties();
        props.Type = MediaPlaybackType.Music;
        if (media.Name != null) props.MusicProperties.Title = media.Name;
        if (media.Author != null) props.MusicProperties.Artist = media.Author;
        if (media.Image != null)
            props.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(media.Image));
        mediaItem.ApplyDisplayProperties(props);
        MediaPlayer.Source = mediaItem;
    }

    public override void Next()
    {
        if (MediaNext != null)
        {
            MediaCurrent = MediaNext;
            PlayNew(MediaCurrent);
            OnNextMediaAccepted(MediaCurrent);
        }
    }
    public override void Pause()
    {
        MediaPlayer.Pause();
        OnPlayingPaused(null);
    }
    public override void Play()
    {
        MediaPlayer.Play();
        OnPlayingStarted(null);
    } 
    public override void Stop()
    {
        MediaPlayer.Pause();
        MediaPlayer.Position = TimeSpan.Zero;
        OnPlayingEnded(null);
    }
    public override void Previous()
    {
        if (MediaPrevious != null)
        {
            MediaCurrent = MediaPrevious;
            PlayNew(MediaCurrent);
            OnPreviousMediaAccepted(MediaCurrent);
        }
    }
    public override void SeekTo(TimeSpan position) => MediaPlayer.Position = position;


    private protected override void SetMediaCurrent(MediaContent value) { }
    private protected override void SetMediaNext(MediaContent value) { }
    private protected override void SetMediaPrevious(MediaContent value) { }

    public override void Dispose() => MediaPlayer?.Dispose();
}
