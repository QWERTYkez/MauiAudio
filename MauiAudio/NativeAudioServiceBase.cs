using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiAudio;

internal abstract class NativeAudioServiceBase : INativeAudioService
{
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string prop = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public NativeAudioServiceBase() => Initialize();
    private protected abstract void Initialize();

    public event EventHandler PlayingStarted;
    public event EventHandler PlayingPaused;
    public event EventHandler PlayingEnded;
    public event EventHandler<MediaContent> PreviousMediaAccepted;
    public event EventHandler<MediaContent> NextMediaAccepted;
    public event EventHandler<TimeSpan> DurationAccepted;
    public event EventHandler<double> BuffCoeffAccepted;

    private protected void OnPlayingStarted(object sender, EventArgs e) => OnPlayingStarted(e);
    private protected void OnPlayingPaused(object sender, EventArgs e) => OnPlayingPaused(e);
    private protected void OnPlayingEnded(object sender, EventArgs e) => OnPlayingEnded(e);
    private protected void OnPreviousMediaAccepted(object sender, MediaContent e) => OnPreviousMediaAccepted(e);
    private protected void OnNextMediaAccepted(object sender, MediaContent e) => OnNextMediaAccepted(e);
    private protected void OnDurationAccepted(object sender, TimeSpan e) => OnDurationAccepted(e);
    private protected void OnBuffCoeffAccepted(object sender, double e) => OnBuffCoeffAccepted(e);

    private protected void OnPlayingStarted(EventArgs e)
    {
        PlayingStarted?.Invoke(this, e);
        OnPropertyChanged(nameof(IsPlaying));
    }
    private protected void OnPlayingPaused(EventArgs e)
    {
        PlayingPaused?.Invoke(this, e);
        OnPropertyChanged(nameof(IsPlaying));
    }
    private protected void OnPlayingEnded(EventArgs e)
    {
        PlayingEnded?.Invoke(this, e);
        OnPropertyChanged(nameof(IsPlaying));
    }
    private protected void OnPreviousMediaAccepted(MediaContent e)
    {
        PreviousMediaAccepted?.Invoke(this, e);
        UpdateQueue(e);
    }
    private protected void OnNextMediaAccepted(MediaContent e)
    {
        NextMediaAccepted?.Invoke(this, e);
        UpdateQueue(e);

    }
    private protected void OnDurationAccepted(TimeSpan e)
    {
        DurationAccepted?.Invoke(this, e);
        OnPropertyChanged(nameof(Duration));
    }
    private protected void OnBuffCoeffAccepted(double e)
    {
        BuffCoeffAccepted?.Invoke(this, e);
        OnPropertyChanged(nameof(BufferedCoeff));
    }

    private ObservableCollection<MediaContent> _playlist;
    public ObservableCollection<MediaContent> Playlist
    {
        get => _playlist;
        private protected set
        {
            _playlist = value;
            OnPropertyChanged();
        }
    }

    private MediaContent _mediaPrevious;
    public MediaContent MediaPrevious
    {
        get => _mediaPrevious;
        private protected set
        {
            SetMediaPrevious(_mediaPrevious = value);
            OnPropertyChanged();
        }
    }
    private protected abstract void SetMediaPrevious(MediaContent value);

    private MediaContent _mediaCurrent;
    public MediaContent MediaCurrent
    {
        get => _mediaCurrent;
        private protected set
        {
            SetMediaCurrent(_mediaCurrent = value);
            OnPropertyChanged();
        }
    }
    private protected abstract void SetMediaCurrent(MediaContent value);

    private MediaContent _mediaNext;
    public MediaContent MediaNext
    {
        get => _mediaNext;
        private protected set
        {
            SetMediaNext(_mediaNext = value);
            OnPropertyChanged();
        }
    }
    private protected abstract void SetMediaNext(MediaContent value);

    private double _volume = 1;
    public double Volume
    {
        get => _volume;
        set
        {
            SetVolume(_volume = Math.Clamp(value, 0, 1));
            OnPropertyChanged();
        }
    }
    private protected abstract void SetVolume(double value);

    private double _balance = 0;
    public double Balance
    {
        get => _balance;
        set
        {
            SetBalance(_balance = Math.Clamp(value, -1, 1));
            OnPropertyChanged();
        }
    }
    private protected abstract void SetBalance(double value);

    private bool _muted = false;
    public bool Muted
    {
        get => _muted;
        set
        {
            SetMuted(_muted = value);
            OnPropertyChanged();
        }
    }
    private protected abstract void SetMuted(bool value);

    public abstract bool IsPlaying { get; }
    public abstract double BufferedCoeff { get; }
    public abstract TimeSpan Duration { get; }
    public abstract bool TryGetPosition(out TimeSpan position);

    public bool LaunchPlaylist<T>(List<T> playlist, MediaContent currentMedia, TimeSpan? position = null) where T : MediaContent
    {
        return LaunchPlaylist(playlist.Select(t => (MediaContent)t).ToList(), currentMedia, position);
    }
    public bool LaunchPlaylist(List<MediaContent> playlist, MediaContent currentMedia, TimeSpan? position = null)
    {
        if (playlist == null || playlist.Count == 0 || !playlist.Contains(currentMedia))
            return false;
        Playlist = new ObservableCollection<MediaContent>(playlist);

        for (int i = 0; i < Playlist.Count;)
            Playlist[i].index = ++i;

        Shuffle(_shuffled);

        PlayNew(currentMedia, position);
        return true;
    }
    public bool LaunchMedia(MediaContent media, TimeSpan? position = null)
    {
        Playlist = null;

        PlayNew(media, position);
        return true;
    }

    private protected void PlayNew(MediaContent media, TimeSpan? position = null)
    {
        MediaCurrent = media;

        if (Playlist != null)
            UpdateQueue();

        PrepareToStartNew(media);
        Play();
        if (position?.TotalSeconds > 0)
            SeekTo(position.Value);
    }
    private protected abstract void PrepareToStartNew(MediaContent media);
    public abstract void Play();
    public abstract void Pause();
    public abstract void Stop();
    public abstract void SeekTo(TimeSpan position);
    public abstract void Next();
    public abstract void Previous();

    private bool _shuffled = false;
    public bool Shuffled
    {
        get => _shuffled;
        set
        {
            Shuffle(_shuffled = value);
            OnPropertyChanged();
        }
    }
    private protected void Shuffle(bool shuffle)
    {
        if (Playlist == null) return;
        if (Playlist.Count < 2) return;
        if (shuffle)
        {
            var random = new Random();
            int count = Playlist.Count;
            while (count > 1)
            {
                --count;
                int index = random.Next(count + 1);
                (Playlist[count], Playlist[index]) = (Playlist[index], Playlist[count]);
            }
        }
        else Playlist.QuickSort();
        UpdateQueue();
    }

    public abstract bool LoopMedia { get; set; }

    private void UpdateQueue(MediaContent newCurrent = null)
    {
        if (newCurrent != null)
            MediaCurrent = newCurrent;

        if (Playlist == null || Playlist.Count < 2)
        {
            MediaPrevious = null;
            MediaNext = null;
            return;
        }

        int num = Playlist.IndexOf(MediaCurrent);
        int index1 = num - 1;
        if (index1 < 0)
            index1 = 0;
        int index2 = num + 1;
        if (index2 == Playlist.Count)
            index2 = Playlist.Count - 1;
        MediaPrevious = Playlist[index1];
        MediaNext = Playlist[index2];
    }

    public abstract void Dispose();
}