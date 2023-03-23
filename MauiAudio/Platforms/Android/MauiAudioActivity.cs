using Android.Content;
using Android.OS;
using MauiAudio.Platforms.Android.CurrentActivity;

namespace MauiAudio.Platforms.Android
{
    public class MauiAudioActivity : MauiAppCompatActivity, IAudioActivity
    {
        MediaPlayerServiceConnection mediaPlayerServiceConnection;

        public MediaPlayerServiceBinder Binder { get; set; }

        public event EventHandler StatusChanged;
        public event EventHandler CoverReloaded;
        public event EventHandler Playing;
        public event EventHandler Buffering;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            {
                CrossCurrentActivity.Current.Init(this, savedInstanceState);
                NotificationHelper.CreateNotificationChannel(ApplicationContext);
                if (mediaPlayerServiceConnection == null)
                    InitializeMedia();
            }
            OnCreate();
        }
        protected virtual void OnCreate() { }

        private void InitializeMedia()
        {
            mediaPlayerServiceConnection = new MediaPlayerServiceConnection(this);
            var mediaPlayerServiceIntent = new Intent(ApplicationContext, typeof(MediaPlayerService));
            BindService(mediaPlayerServiceIntent, mediaPlayerServiceConnection, Bind.AutoCreate);
        }
    }
}
