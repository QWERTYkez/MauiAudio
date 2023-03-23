using Android.App;

namespace MauiAudio.Platforms.Android.CurrentActivity;

internal class ActivityEventArgs : EventArgs
{
    internal ActivityEventArgs(Activity activity, ActivityEvent ev)
    {
        Event = ev;
        Activity = activity;
    }

    public ActivityEvent Event { get; }
    public Activity Activity { get; }
}