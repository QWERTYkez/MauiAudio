using MauiAudio.Platforms.Android;
using MauiAudio.Platforms.Android.CurrentActivity;

namespace MauiAudio
{
    public class NativeAudioService : NativeAudioServiceBase
    {
        private protected override void Initialize()
        {
            service = Instance.Binder.GetMediaPlayerService();

            service.PlayingStarted += OnPlayingStarted;
            service.PlayingPaused += OnPlayingPaused;
            service.PlayingEnded += OnPlayingEnded;

            service.PreviousMediaAccepted += OnPreviousMediaAccepted;
            service.NextMediaAccepted += OnNextMediaAccepted;

            service.DurationAccepted += OnDurationAccepted;
            service.BuffCoeffAccepted += OnBuffCoeffAccepted;
        }

        private static IAudioActivity Instance => CrossCurrentActivity.Current.Activity as IAudioActivity;
        private MediaPlayerService service;

        public override bool LoopMedia { get => service.LoopMedia; set => service.LoopMedia = value; }
        public override bool IsPlaying => service.IsPlaying;
        public override double BufferedCoeff => service.Buffered;
        public override TimeSpan Duration => service.Duration;
        public override bool TryGetPosition(out TimeSpan position)
        {
            if (service.Position != null)
            {
                position = service.Position.Value;
                return true;
            }
            else
            {
                position = TimeSpan.Zero;
                return false;
            }
        }

        private protected override void PrepareToStartNew(MediaContent media) => Pause();
        public override void Next() => _ = service.PlayNext(true);
        public override void Pause() => _ = service.Pause();
        public override void Play() => _ = service.Play();
        public override void Stop() => _ = service.Stop();
        public override void Previous() => _ = service.PlayPrevious(true);
        public override void SeekTo(TimeSpan position) => _ = service.SeekTo(position);

        private protected override void SetVolume(double value) => service.Volume = value;
        private protected override void SetBalance(double value) => service.Balance = value;
        private protected override void SetMuted(bool value) => service.Muted = value;

        private protected override void SetMediaPrevious(MediaContent value) => service.MediaPrevious = value;
        private protected override void SetMediaCurrent(MediaContent value) => service.MediaCurrent = value;
        private protected override void SetMediaNext(MediaContent value) => service.MediaNext = value;

        public override void Dispose() => _ = service.Stop();
    }
}
