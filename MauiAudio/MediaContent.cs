using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiAudio
{
    public class MediaContent
    {
        public virtual string Name { get; private set; }
        public virtual string Author { get; private set; }
        public virtual string URL { get; private set; }
        public virtual Stream Stream { get; private set; }
        public virtual string Image { get; private set; }
        internal int index { get; set; }
    }
}
