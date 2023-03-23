namespace MauiAudio;

public class MediaContent
{
    public MediaContent() { }
    public MediaContent(string url)
    {
        URL = url;
    }

    public virtual string Name { get; private protected set; }
    public virtual string Author { get; private protected set; }
    public virtual string URL { get; private protected set; }
    public virtual Stream Stream { get; private protected set; }
    public virtual string Image { get; private protected set; }
    internal int index { get; set; }
}
