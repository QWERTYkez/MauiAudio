namespace MauiAudio.Platforms.Android
{
    public interface IAudioActivity
    {
        public MediaPlayerServiceBinder Binder { get; set; }

        public event EventHandler StatusChanged;
        public event EventHandler CoverReloaded;
        public event EventHandler Playing;
        public event EventHandler Buffering;
    }
}
