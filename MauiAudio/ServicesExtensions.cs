namespace MauiAudio;

public static class Extensions
{
    public static MauiAppBuilder UseMauiAudio(this MauiAppBuilder builder)
    {
#if WINDOWS||ANDROID||MACCATALYST||IOS
        builder.Services.AddSingleton<MauiAudio.INativeAudioService, MauiAudio.NativeAudioService>();
#endif
        return builder;
    }

    public static INativeAudioService GetMauiAudioService(this VisualElement element) =>
        element.Handler.MauiContext.Services.GetService<INativeAudioService>();
}
