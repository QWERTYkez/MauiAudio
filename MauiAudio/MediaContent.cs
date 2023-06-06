namespace MauiAudio;

public class MediaContent
{
    public MediaContent() { }
    public MediaContent(string url)
    {
        URL = url;
    }

    public virtual string Name { get; protected set; }
    public virtual string Author { get; protected set; }
    public virtual string URL { get; protected set; }
    public virtual Stream Stream { get; protected set; }
    public virtual string Image { get; protected set; }
    public virtual bool Playable { get; protected set; }
    internal int index { get; set; }
}
