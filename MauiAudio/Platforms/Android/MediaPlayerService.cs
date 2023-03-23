using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Media.Session;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using AndroidNet = Android.Net;

namespace MauiAudio.Platforms.Android;


[Service(Exported = true)]
[IntentFilter(new[] { ActionPlay, ActionPause, ActionStop, ActionTogglePlayback, ActionNext, ActionPrevious })]
public class MediaPlayerService : Service,
   AudioManager.IOnAudioFocusChangeListener,
   MediaPlayer.IOnBufferingUpdateListener,
   MediaPlayer.IOnCompletionListener,
   MediaPlayer.IOnErrorListener,
   MediaPlayer.IOnPreparedListener
{
    //Actions
    public const string ActionPlay = "com.xamarin.action.PLAY";
    public const string ActionPause = "com.xamarin.action.PAUSE";
    public const string ActionStop = "com.xamarin.action.STOP";
    public const string ActionTogglePlayback = "com.xamarin.action.TOGGLEPLAYBACK";
    public const string ActionNext = "com.xamarin.action.NEXT";
    public const string ActionPrevious = "com.xamarin.action.PREVIOUS";

    public MediaPlayer mediaPlayer;
    private AudioManager audioManager;

    private MediaSession mediaSession;
    public MediaController mediaController;

    private WifiManager wifiManager;
    private WifiManager.WifiLock wifiLock;

    public event EventHandler PlayingStarted;
    public event EventHandler PlayingPaused;
    public event EventHandler PlayingEnded;
    public event EventHandler<MediaContent> PreviousMediaAccepted;
    public event EventHandler<MediaContent> CurrentMediaAccepted;
    public event EventHandler<MediaContent> NextMediaAccepted;
    public event EventHandler<TimeSpan> DurationAccepted;
    public event EventHandler<double> BuffCoeffAccepted;


    private void OnPlayingStarted()
    {
        PlayingStarted?.Invoke(this, EventArgs.Empty);
    }
    private void OnPlayingPaused()
    {
        PlayingPaused?.Invoke(this, EventArgs.Empty);
    }
    public async void OnCompletion(MediaPlayer mp)
    {
        PlayingEnded?.Invoke(this, EventArgs.Empty);
        await PlayNext();
    }
    public void OnPreviousMediaAccepted()
    {
        PreviousMediaAccepted?.Invoke(this, MediaPrevious);
    }
    public void OnNextMediaAccepted()
    {
        NextMediaAccepted?.Invoke(this, MediaNext);
    }
    public void OnDurationAccepted(TimeSpan duration)
    {
        DurationAccepted?.Invoke(this, duration);
    }
    public void OnBufferingUpdate(MediaPlayer mp, int percent)
    {
        _buffered = (double)percent / 100;
        BuffCoeffAccepted?.Invoke(this, _buffered);
    }

    public bool LoopMedia { get; set; }

    public MediaContent MediaPrevious;
    public MediaContent MediaCurrent;
    public MediaContent MediaNext;

    public bool isCurrentEpisode = true;

    private readonly Handler PlayingHandler;
    private readonly Java.Lang.Runnable PlayingHandlerRunnable;

    private ComponentName remoteComponentName;

    public PlaybackStateCode MediaPlayerState
    {
        get
        {
            return mediaController.PlaybackState != null
                ? mediaController.PlaybackState.State
                : PlaybackStateCode.None;
        }
    }

    public MediaPlayerService()
    {
        PlayingHandler = new Handler(Looper.MainLooper);

        // Create a runnable, restarting itself if the status still is "playing"
        PlayingHandlerRunnable = new Java.Lang.Runnable(() =>
        {
            if (MediaPlayerState == PlaybackStateCode.Playing)
            {
                PlayingHandler.PostDelayed(PlayingHandlerRunnable, 250);
            }
        });
    }

    /// <summary>
    /// On create simply detect some of our managers
    /// </summary>
    public override void OnCreate()
    {
        base.OnCreate();
        //Find our audio and notificaton managers
        audioManager = (AudioManager)GetSystemService(AudioService);
        wifiManager = (WifiManager)GetSystemService(WifiService);

        remoteComponentName = new ComponentName(PackageName, new RemoteControlBroadcastReceiver().ComponentName);
    }

    /// <summary>
    /// Will register for the remote control client commands in audio manager
    /// </summary>
    private void InitMediaSession()
    {
        try
        {
            if (mediaSession == null)
            {
                Intent nIntent = new(ApplicationContext, typeof(Activity));

                remoteComponentName = new ComponentName(PackageName, new RemoteControlBroadcastReceiver().ComponentName);

                mediaSession = new MediaSession(ApplicationContext, "MauiStreamingAudio"/*, remoteComponentName*/); //TODO
                mediaSession.SetSessionActivity(PendingIntent.GetActivity(ApplicationContext, 0, nIntent, PendingIntentFlags.Mutable));
                mediaController = new MediaController(ApplicationContext, mediaSession.SessionToken);
            }

            mediaSession.Active = true;
            mediaSession.SetCallback(new MediaSessionCallback((MediaPlayerServiceBinder)binder));

            mediaSession.SetFlags(MediaSessionFlags.HandlesMediaButtons | MediaSessionFlags.HandlesTransportControls);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    /// <summary>
    /// Intializes the player.
    /// </summary>
    private void InitializePlayer()
    {
        mediaPlayer = new MediaPlayer();

        mediaPlayer.SetAudioAttributes(
            new AudioAttributes.Builder()
            .SetContentType(AudioContentType.Music)
            .SetUsage(AudioUsageKind.Media)
                .Build());

        mediaPlayer.SetWakeMode(ApplicationContext, WakeLockFlags.Partial);

        mediaPlayer.SetOnBufferingUpdateListener(this);
        mediaPlayer.SetOnCompletionListener(this);
        mediaPlayer.SetOnErrorListener(this);
        mediaPlayer.SetOnPreparedListener(this);
    }

    public bool OnError(MediaPlayer mp, MediaError what, int extra)
    {
        UpdatePlaybackState(PlaybackStateCode.Error);
        return true;
    }

    public void OnPrepared(MediaPlayer mp)
    {
        OnDurationAccepted(TimeSpan.FromMilliseconds(mp.Duration));
        mp.Start();
        UpdatePlaybackState(PlaybackStateCode.Playing);
    }

    public TimeSpan? Position
    {
        get
        {
            if (mediaPlayer == null
                || MediaPlayerState != PlaybackStateCode.Playing
                    && MediaPlayerState != PlaybackStateCode.Paused)
                return null;
            else
                return TimeSpan.FromMilliseconds(mediaPlayer.CurrentPosition);
        }
    }

    public bool IsPlaying
    {
        get
        {
            if (mediaPlayer == null)
                return mediaPlayer.IsPlaying;
            else
                return false;
        }
    }

    public TimeSpan Duration
    {
        get
        {
            if (mediaPlayer == null
                || MediaPlayerState != PlaybackStateCode.Playing
                    && MediaPlayerState != PlaybackStateCode.Paused)
                return TimeSpan.Zero;
            else
                return TimeSpan.FromMilliseconds(mediaPlayer.Duration);
        }
    }

    private double _buffered = 0;
    public double Buffered
    {
        get
        {
            if (mediaPlayer == null)
                return 0;
            else
                return _buffered;
        }
        private set
        {
            _buffered = value;
        }
    }

    private Bitmap cover;

    public object Cover
    {
        get => cover ??= BitmapFactory.DecodeResource(Resources, Resource.Drawable.music); //TODO player_play
        set
        {
            cover = value as Bitmap;
            if (cover != null)
            {
                StartNotification();
                UpdateMediaMetadataCompat();
            }
        }
    }

    private double _volume;
    public double Volume
    {
        get => _volume;
        set
        {
            _volume = value;
            ResetVolume();
        }
    }
    private double _balance;
    public double Balance
    {
        get => _balance;
        set
        {
            _balance = value;
            ResetVolume();
        }
    }
    private bool _muted;
    public bool Muted
    {
        get => _muted;
        set
        {
            _muted = value;
            if (value)
                mediaPlayer.SetVolume(0, 0);
            else
                ResetVolume();
        }
    }
    private void ResetVolume()
    {
        // Using the "constant power pan rule." See: http://www.rs-met.com/documents/tutorials/PanRules.pdf
        var left = Math.Cos((Math.PI * (_balance + 1)) / 4) * _volume;
        var right = Math.Sin((Math.PI * (_balance + 1)) / 4) * _volume;

        mediaPlayer?.SetVolume((float)left, (float)right);
    }

    /// <summary>
    /// Intializes the player.
    /// </summary>
    public async Task Play()
    {
        if (mediaSession == null)
            InitMediaSession();

        if (mediaPlayer == null)
        {
            InitializePlayer();
        }
        else
        {
            if (mediaPlayer.IsPlaying && isCurrentEpisode)
            {
                UpdatePlaybackState(PlaybackStateCode.Playing);
                return;
            }
            if (MediaPlayerState == PlaybackStateCode.Paused)
            {
                //We are simply paused so just start again
                mediaPlayer.Start();
                UpdatePlaybackState(PlaybackStateCode.Playing);
                StartNotification();

                //Update the metadata now that we are playing
                UpdateMediaMetadataCompat();
                return;
            }
        }

        isCurrentEpisode = true;

        //PrepareAndPlayMediaPlayerAsync
        {
            try
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(21))
                {
                    MediaMetadataRetriever metaRetriever = new();

                    AndroidNet.Uri uri;
                    if (MediaCurrent.Stream != null)
                    {
                        var fileStream = File.Create(FileSystem.Current.CacheDirectory + "temp.wav");
                        MediaCurrent.Stream.CopyTo(fileStream);
                        fileStream.Close();
                        uri = AndroidNet.Uri.Parse(FileSystem.Current.CacheDirectory + "temp.wav");
                    }
                    else
                    {
                        uri = AndroidNet.Uri.Parse(MediaCurrent.URL);
                    }
                    await mediaPlayer.SetDataSourceAsync(ApplicationContext, uri);

                    //If Uri Scheme is not set its a local file so there's no metadata to fetch
                    if (!string.IsNullOrWhiteSpace(uri.Scheme))
                        await metaRetriever.SetDataSourceAsync(uri.ToString(), new Dictionary<string, string>());

                    if (OperatingSystem.IsAndroidVersionAtLeast(26))
                    {
                        var focusResult = audioManager.RequestAudioFocus(new AudioFocusRequestClass
                       .Builder(AudioFocus.Gain)
                       .SetOnAudioFocusChangeListener(this)
                       .Build());

                        if (focusResult != AudioFocusRequest.Granted)
                        {
                            // Could not get audio focus
                            Console.WriteLine("Could not get audio focus");
                        }
                    }
                    UpdatePlaybackState(PlaybackStateCode.Buffering);
                    mediaPlayer.PrepareAsync();

                    AquireWifiLock();

                    //Check if there's some metadata
                    if (!string.IsNullOrEmpty(MediaCurrent.Image))
                    {
                        using var webClient = new HttpClient();
                        var imageBytes = await webClient.GetByteArrayAsync(MediaCurrent.Image);
                        if (imageBytes != null && imageBytes.Length > 0)
                        {
                            Cover = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                        }
                    }
                    else if (metaRetriever != null && !string.IsNullOrWhiteSpace(metaRetriever.ExtractMetadata(MetadataKey.Album)))
                    {
                        byte[] imageByteArray = metaRetriever.GetEmbeddedPicture();
                        //if (imageByteArray == null)
                        //    Cover = await BitmapFactory.DecodeResourceAsync(Resources, Resource.Drawable.abc_ab_share_pack_mtrl_alpha); //TODO player_play
                        //else
                        if (imageByteArray != null)
                            Cover = await BitmapFactory.DecodeByteArrayAsync(imageByteArray, 0, imageByteArray.Length);
                    }
                    UpdateMediaMetadataCompat(metaRetriever);
                    StartNotification();
                }
            }
            catch (Exception ex)
            {
                UpdatePlaybackStateStopped();

                // Unable to start playback log error
                Console.WriteLine(ex);
            }
        }
    }

    public async Task SeekTo(TimeSpan position)
    {
        await Task.Run(() => mediaPlayer?.SeekTo((int)position.TotalMilliseconds));
    }

    public async Task PlayNext(bool Manually = false)
    {
        if (mediaPlayer != null)
        {
            if (!Manually && LoopMedia)
            {
                await SeekTo(TimeSpan.Zero);
                await Play();
            }
            else if (MediaNext != null)
            {
                mediaPlayer.Reset();

                MediaCurrent = MediaNext;
                OnNextMediaAccepted();

                UpdatePlaybackState(PlaybackStateCode.SkippingToNext);

                await Play();
            }
        }
        else if (MediaNext != null)
        {
            MediaCurrent = MediaNext;
            await Play();
        }
    }

    private readonly TimeSpan sec3 = TimeSpan.FromSeconds(3);
    public async Task PlayPrevious(bool Manually = false)
    {
        if (mediaPlayer != null)
        {
            // Start current track from beginning if it's the first track or the track has played more than 3sec and you hit "playPrevious".
            if (Position > sec3)
            {
                await SeekTo(TimeSpan.Zero);
                await Play();
            }
            else
            {
                if (!Manually && LoopMedia)
                {
                    await SeekTo(TimeSpan.Zero);
                    await Play();
                }
                else if (MediaPrevious != null)
                {
                    mediaPlayer.Reset();

                    MediaCurrent = MediaPrevious;
                    OnPreviousMediaAccepted();

                    UpdatePlaybackState(PlaybackStateCode.SkippingToPrevious);

                    await Play();
                }
            }
        }
        else if (MediaPrevious != null)
        {
            MediaCurrent = MediaPrevious;
            await Play();
        }
    }

    public async Task Pause()
    {
        await Task.Run(() =>
        {
            if (mediaPlayer == null)
                return;

            if (mediaPlayer.IsPlaying)
                mediaPlayer.Pause();

            UpdatePlaybackState(PlaybackStateCode.Paused);
        });
    }

    public async Task Stop()
    {
        await Task.Run(() =>
        {
            if (mediaPlayer == null)
                return;

            if (mediaPlayer.IsPlaying)
            {
                mediaPlayer.Stop();
            }

            UpdatePlaybackState(PlaybackStateCode.Stopped);
            mediaPlayer.Reset();
            NotificationHelper.StopNotification(ApplicationContext);
            StopForeground(true);
            ReleaseWifiLock();
            UnregisterMediaSessionCompat();
        });
    }

    public void UpdatePlaybackStateStopped()
    {
        UpdatePlaybackState(PlaybackStateCode.Stopped);

        if (mediaPlayer != null)
        {
            mediaPlayer.Reset();
            mediaPlayer.Release();
            mediaPlayer = null;
        }
    }

    private void UpdatePlaybackState(PlaybackStateCode state)
    {
        if (mediaSession == null || mediaPlayer == null)
            return;

        try
        {
            int pos = 0;
            if (Position != null)
            {
                pos = (int)Position.Value.TotalMilliseconds;
            }

            PlaybackState.Builder stateBuilder = new PlaybackState.Builder()
                .SetActions(
                    PlaybackState.ActionPause |
                    PlaybackState.ActionPlay |
                    PlaybackState.ActionPlayPause |
                    PlaybackState.ActionSkipToNext |
                    PlaybackState.ActionSkipToPrevious |
                    PlaybackState.ActionStop
                )
                .SetState(state, pos, 1.0f, SystemClock.ElapsedRealtime());

            mediaSession.SetPlaybackState(stateBuilder.Build());

            if (state == PlaybackStateCode.Playing)
            {
                PlayingHandler.PostDelayed(PlayingHandlerRunnable, 0);
                StartNotification();
            }
            else if (state == PlaybackStateCode.Paused)
            {
                StartNotification();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void StartNotification()
    {
        if (mediaSession == null)
            return;

        NotificationHelper.StartNotification(
            ApplicationContext,
            mediaController.Metadata,
            mediaSession,
            Cover,
            MediaPlayerState == PlaybackStateCode.Playing);
    }

    /// <summary>
    /// Updates the metadata on the lock screen
    /// </summary>
    private void UpdateMediaMetadataCompat(MediaMetadataRetriever metaRetriever = null)
    {
        if (mediaSession == null)
            return;

        MediaMetadata.Builder builder = new();

        if (metaRetriever != null)
        {
            builder
            .PutString(MediaMetadata.MetadataKeyAlbum,metaRetriever.ExtractMetadata(MetadataKey.Album))
            .PutString(MediaMetadata.MetadataKeyArtist, MediaCurrent.Author ?? metaRetriever.ExtractMetadata(MetadataKey.Artist))
            .PutString(MediaMetadata.MetadataKeyTitle, MediaCurrent.Name ?? metaRetriever.ExtractMetadata(MetadataKey.Title));
        }
        else
        {
            builder
                .PutString(MediaMetadata.MetadataKeyAlbum, mediaSession.Controller.Metadata.GetString(MediaMetadata.MetadataKeyAlbum))
                .PutString(MediaMetadata.MetadataKeyArtist, mediaSession.Controller.Metadata.GetString(MediaMetadata.MetadataKeyArtist))
                .PutString(MediaMetadata.MetadataKeyTitle, mediaSession.Controller.Metadata.GetString(MediaMetadata.MetadataKeyTitle));
        }
        builder.PutBitmap(MediaMetadata.MetadataKeyAlbumArt, Cover as Bitmap);

        mediaSession.SetMetadata(builder.Build());
    }

    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        HandleIntent(intent);
        return base.OnStartCommand(intent, flags, startId);
    }

    private void HandleIntent(Intent intent)
    {
        if (intent == null || intent.Action == null)
            return;

        string action = intent.Action;

        if (action.Equals(ActionPlay))
        {
            mediaController.GetTransportControls().Play();
        }
        else if (action.Equals(ActionPause))
        {
            mediaController.GetTransportControls().Pause();
        }
        else if (action.Equals(ActionPrevious))
        {
            mediaController.GetTransportControls().SkipToPrevious();
        }
        else if (action.Equals(ActionNext))
        {
            mediaController.GetTransportControls().SkipToNext();
        }
        else if (action.Equals(ActionStop))
        {
            mediaController.GetTransportControls().Stop();
        }
    }

    /// <summary>
    /// Lock the wifi so we can still stream under lock screen
    /// </summary>
    private void AquireWifiLock()
    {
        wifiLock ??= wifiManager.CreateWifiLock(WifiMode.Full, "xamarin_wifi_lock");
        wifiLock.Acquire();
    }

    /// <summary>
    /// This will release the wifi lock if it is no longer needed
    /// </summary>
    private void ReleaseWifiLock()
    {
        if (wifiLock == null)
            return;

        wifiLock.Release();
        wifiLock = null;
    }

    private void UnregisterMediaSessionCompat()
    {
        try
        {
            if (mediaSession != null)
            {
                mediaSession.Dispose();
                mediaSession = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    IBinder binder;

    public override IBinder OnBind(Intent intent)
    {
        binder = new MediaPlayerServiceBinder(this);
        return binder;
    }

    public override bool OnUnbind(Intent intent)
    {
        NotificationHelper.StopNotification(ApplicationContext);
        return base.OnUnbind(intent);
    }

    /// <summary>
    /// Properly cleanup of your player by releasing resources
    /// </summary>
    public override void OnDestroy()
    {
        base.OnDestroy();
        if (mediaPlayer != null)
        {
            mediaPlayer.Release();
            mediaPlayer = null;

            NotificationHelper.StopNotification(ApplicationContext);
            StopForeground(true);
            ReleaseWifiLock();
            UnregisterMediaSessionCompat();
        }
    }

    public async void OnAudioFocusChange(AudioFocus focusChange)
    {
        switch (focusChange)
        {
            case AudioFocus.Gain:
                if (mediaPlayer == null)
                    InitializePlayer();

                if (!mediaPlayer.IsPlaying)
                {
                    mediaPlayer.Start();
                }

                mediaPlayer.SetVolume(1.0f, 1.0f);
                break;
            case AudioFocus.Loss:
                //We have lost focus stop!
                await Stop();
                break;
            case AudioFocus.LossTransient:
                //We have lost focus for a short time, but likely to resume so pause
                await Pause();
                break;
            case AudioFocus.LossTransientCanDuck:
                //We have lost focus but should till play at a muted 10% volume
                if (mediaPlayer.IsPlaying)
                    mediaPlayer.SetVolume(.1f, .1f);
                break;
        }
    }

    public class MediaSessionCallback : MediaSession.Callback
    {
        private readonly MediaPlayerServiceBinder mediaPlayerService;
        public MediaSessionCallback(MediaPlayerServiceBinder service)
        {
            mediaPlayerService = service;
        }

        public override void OnPause()
        {
            mediaPlayerService.GetMediaPlayerService().OnPlayingPaused();
            base.OnPause();
        }

        public override void OnPlay()
        {
            mediaPlayerService.GetMediaPlayerService().OnPlayingStarted();
            base.OnPlay();
        }

        public override async void OnSkipToNext()
        {
            await mediaPlayerService.GetMediaPlayerService().PlayNext();
            base.OnSkipToNext();
        }

        public override async void OnSkipToPrevious()
        {
            await mediaPlayerService.GetMediaPlayerService().PlayPrevious();
            base.OnSkipToPrevious();
        }

        public override async void OnStop()
        {
            await mediaPlayerService.GetMediaPlayerService().Stop();
            base.OnStop();
        }
    }
}

public class MediaPlayerServiceBinder : Binder
{
    private readonly MediaPlayerService service;

    public MediaPlayerServiceBinder(MediaPlayerService service)
    {
        this.service = service;
    }

    public MediaPlayerService GetMediaPlayerService()
    {
        return service;
    }
}
