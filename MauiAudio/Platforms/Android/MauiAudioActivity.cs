using Android.Content;
using Android.OS;
using MauiAudio.Platforms.Android.CurrentActivity;

namespace MauiAudio.Platforms.Android;

public class MauiAudioActivity : MauiAppCompatActivity, IAudioActivity
{
    MediaPlayerServiceConnection mediaPlayerServiceConnection;

    MediaPlayerServiceBinder IAudioActivity.Binder { get; set; }

    event EventHandler StatusChanged;
    event EventHandler CoverReloaded;
    event EventHandler Playing;
    event EventHandler Buffering;

    event EventHandler IAudioActivity.StatusChanged
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }

    event EventHandler IAudioActivity.CoverReloaded
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }

    event EventHandler IAudioActivity.Playing
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }

    event EventHandler IAudioActivity.Buffering
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }

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
