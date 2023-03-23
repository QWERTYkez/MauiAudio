using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MauiAudio;

public interface INativeAudioService : INotifyPropertyChanged, IDisposable
{
    public event EventHandler PlayingStarted;
    public event EventHandler PlayingPaused;
    public event EventHandler PlayingEnded;
    public event EventHandler<MediaContent> PreviousMediaAccepted;
    public event EventHandler<MediaContent> NextMediaAccepted;
    public event EventHandler<TimeSpan> DurationAccepted;
    public event EventHandler<double> BuffCoeffAccepted;

    public ObservableCollection<MediaContent> Playlist { get; }

    public MediaContent MediaPrevious { get; }
    public MediaContent MediaCurrent { get; }
    public MediaContent MediaNext { get; }

    public double Volume { get; set; }
    public double Balance { get; set; }
    public bool Muted { get; set; }

    public abstract bool IsPlaying { get; }
    public abstract double BufferedCoeff { get; }
    public abstract TimeSpan Duration { get; }
    public abstract bool TryGetPosition(out TimeSpan position);

    public bool LaunchPlaylist<T>(List<T> playlist, MediaContent currentMedia, TimeSpan? position = null) where T : MediaContent;
    public bool LaunchPlaylist(List<MediaContent> playlist, MediaContent currentMedia, TimeSpan? position = null);
    public bool LaunchMedia(MediaContent media, TimeSpan? position = null);

    public abstract void Play();
    public abstract void Pause();
    public abstract void Stop();
    public abstract void SeekTo(TimeSpan position);
    public abstract void Next();
    public abstract void Previous();

    public bool Shuffled { get; set; }
    public abstract bool LoopMedia { get; set; }
}
