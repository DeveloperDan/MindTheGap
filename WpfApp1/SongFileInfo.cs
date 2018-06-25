using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class SongFileInfo
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public int ArtistCount { get; set; }
        public int ArtistGapAhead { get; set; }
        public int Position { get; set; }
        public int NextOccuranceOfArtistIsAt { get; set; }

        public int ArtistGapBehind { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int TitleGap { get; set; }

    }
}
