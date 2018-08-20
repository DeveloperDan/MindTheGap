using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindTheGap
{
    public class SongFileInfo
    {
        public string Title { get; set; }
        public string Artist { get; set; }


        public int ArtistCount { get; set; }
        public int ArtistGapAhead { get; set; }
        public int TitleCount { get; set; }
        public int TitleGapAhead { get; set; }

        public int ArtistGapTenChars { get; set; }
        public int Position { get; set; }
        public int NextOccuranceOfArtistIsAt { get; set; }
        public int NextOccuranceOfArtistTenCharsIsAt { get; set; }
        public int ArtistGapBehind { get; set; }
        

        public int TitleGapTenChars { get; set; }
        public int NextOccuranceOfTitleIsAt { get; set; }
        public int NextOccuranceOfTitleTenCharsIsAt { get; set; }
        public int TitleGapBehind { get; set; }
        

        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int TitleGap { get; set; }

    }
}
