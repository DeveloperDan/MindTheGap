using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void GetFilesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                IEnumerable<string> musicFilePathsList = GetListOfMusicFilePaths();

                List<SongFileInfo> songList = PraseFileNamesIntoSongList(musicFilePathsList);

                var songsByArtistThenTitle = songList.OrderBy(sng => sng.Artist).ThenBy(sng => sng.Title);
                SongsByArtistThenTitleGrid.ItemsSource = songsByArtistThenTitle;

                var songsByTitleThenArtist = songList.OrderBy(sng => sng.Title).ThenBy(sng => sng.Artist);
                SongsByTitleThenArtistGrid.ItemsSource = songsByTitleThenArtist;

                var songsByFileName = songList.OrderBy(sng => sng.FileName);

                //was: CalculateGapsToSameArtistAndTitle(songsByTitleThenArtist);
                CalculateGapsToSameArtistAndTitle(songsByFileName);

                var songsBySmallestArtistGap = songList.OrderBy(sng => sng.ArtistGapAhead).ThenBy(sng => sng.Title).Where(sng => sng.ArtistGapAhead > 0);
                SongsBySmallestArtistGapGrid.ItemsSource = songsBySmallestArtistGap;

                ///////

                if (1 == 2) // show ten chars result in lower right grid
                {
                    var songsBySmallestArtistTenCharsGap = songList.OrderBy(sng => sng.ArtistGapTenChars).ThenBy(sng => sng.Title).Where(sng => sng.ArtistGapTenChars > 0);

                    SongsByArtistCountGrid.ItemsSource = songsBySmallestArtistTenCharsGap;
                }
                else // show sorted by artist count in lower right grid
                {
                    var songsByArtistCount = songList.OrderByDescending(sng => sng.ArtistCount).ThenBy(sng => sng.ArtistGapAhead);

                    SongsByArtistCountGrid.ItemsSource = songsByArtistCount;
                }
            }
            catch (Exception ex)
            {

                System.Windows.MessageBox.Show(ex.ToString());
            }
            finally
            {
                Mouse.OverrideCursor = null;
                this.Title = "Updated: " + DateTime.Now.ToLongTimeString();
            }
        }

        private static List<SongFileInfo> PraseFileNamesIntoSongList(IEnumerable<string> musicFilePathsList)
        {
            var songList = new List<SongFileInfo>();

            foreach (var filepath in musicFilePathsList)
            {

                string filename = string.Empty;
                int dashPosition;
                int tildePositionPlusOne;
                int parenPositionMinusOne;
                string title;
                int lengthOfTitle;
                string artist;

                SongFileInfo song;

                try
                {
                    filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
                    dashPosition = filename.IndexOf(" - ");
                    tildePositionPlusOne = filename.IndexOf("~")+1;
                    parenPositionMinusOne = filename.IndexOf("(") - 1;
                    if (parenPositionMinusOne > -1)
                    {

                        title = filename.Substring(tildePositionPlusOne, parenPositionMinusOne - tildePositionPlusOne).Trim();
                    }
                    else
                    {
                        title = filename.Substring(tildePositionPlusOne, dashPosition - tildePositionPlusOne).Trim();
                    }
                    lengthOfTitle = filename.Length - dashPosition;
                    artist = filename.Substring(dashPosition + 3, lengthOfTitle - 3).Trim();

                    song = new SongFileInfo();
                    song.FilePath = filepath;
                    song.FileName = filename;
                    song.Title = title;
                    song.Artist = artist;

                    songList.Add(song);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("File name: " + filename + "     Error:  " + ex.ToString());
                }
            }

            return songList;
        }

        private IEnumerable<string> GetListOfMusicFilePaths()
        {
            string pathSelected = string.Empty;

            using (var dialog = new FolderBrowserDialog())
            {

                if (UseLastPickerPathRadioButton.IsChecked == true)
                    pathSelected = LastPickerPathTextBox.Text;
                else
                {
                    DialogResult result = dialog.ShowDialog();

                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        pathSelected = dialog.SelectedPath;
                        LastPickerPathTextBox.Text = pathSelected;
                    }
                }
            }

            var files = Directory.EnumerateFiles(pathSelected, "*.*")
                    .Where(fl => fl.ToLower().EndsWith(".mp3") || fl.ToLower().EndsWith(".flac"));
            return files;
        }

        private static void CalculateGapsToSameArtistAndTitle(IOrderedEnumerable<SongFileInfo> songsByFileName_IOrderedEnumerable)
        {
            //routine was called: GetGapToNextOccurranceOfThisArtist
            //int artistCount = 0;
            string previousTitle = string.Empty;
            string previousArtist = string.Empty;
            string filename = string.Empty;

            var songListByFileName = songsByFileName_IOrderedEnumerable.ToList<SongFileInfo>();

            for (int currentIndex = 0; currentIndex < songsByFileName_IOrderedEnumerable.Count(); currentIndex++)
            {
                var song = songsByFileName_IOrderedEnumerable.ElementAt(currentIndex);
                song.Position = currentIndex+1;

                filename = song.FileName;


                // Get Artist gap +++++++++++++++++++++++++++++

                //https://stackoverflow.com/a/38822432/381082
                var nextArtistOccuranceIndex = songListByFileName.FindIndex(currentIndex+1, sng => sng.Artist == song.Artist);

                song.NextOccuranceOfArtistIsAt = nextArtistOccuranceIndex+1;

                var artistGap = nextArtistOccuranceIndex - currentIndex;
                song.ArtistGapAhead = artistGap;

                try
                {
                    var nextArtistOccuranceTenCharsIndex = songListByFileName.FindIndex(currentIndex + 1, sng => sng.Artist.Substring(0, 10) == song.Artist.Substring(0, 10));
                    song.ArtistGapTenChars = nextArtistOccuranceTenCharsIndex - currentIndex;

                }
                catch (Exception ex)
                {

                    //swallow the error
                }

                song.ArtistCount = (from sng in songListByFileName
                                    where sng.Artist == song.Artist
                                    select sng).Count();


                // Get Title gap ++++++++++++++++++++++++++++++++

                //https://stackoverflow.com/a/38822432/381082
                var nextTitleOccuranceIndex = songListByFileName.FindIndex(currentIndex + 1, sng => sng.Title == song.Title);

                song.NextOccuranceOfTitleIsAt = nextTitleOccuranceIndex + 1;

                var titleGap = nextTitleOccuranceIndex - currentIndex;
                song.TitleGapAhead = titleGap;

                try
                {
                    var nextTitleOccuranceTenCharsIndex = songListByFileName.FindIndex(currentIndex + 1, sng => sng.Title.Substring(0, 10) == song.Title.Substring(0, 10));
                    song.TitleGapTenChars = nextTitleOccuranceTenCharsIndex - currentIndex;

                }
                catch (Exception ex)
                {

                    //swallow the error
                }

                song.TitleCount = (from sng in songListByFileName
                                    where sng.Title == song.Title
                                    select sng).Count();



                //+++++++++++++++++++++++++


                previousTitle = song.Title;
                previousArtist = song.Artist;
            }
        }




//private static void GetGapToNextOccurranceOfThisTitle(IOrderedEnumerable<SongFileInfo> songsByTitleThenArtist_IOrderedEnumerable)
//        {
//            //int titleCount = 0;
//            string previousTitle = string.Empty;
//            string previousArtist = string.Empty;
//            string filename = string.Empty;

//            var songListByTitleThenArtist = songsByTitleThenArtist_IOrderedEnumerable.ToList<SongFileInfo>();

//            for (int currentIndex = 0; currentIndex < songsByTitleThenArtist_IOrderedEnumerable.Count(); currentIndex++)
//            {
//                var song = songsByTitleThenArtist_IOrderedEnumerable.ElementAt(currentIndex);
//                song.Position = currentIndex+1;

//                filename = song.FileName;

//                //https://stackoverflow.com/a/38822432/381082
//                var nextOccuranceIndex = songListByTitleThenArtist.FindIndex(currentIndex+1, sng => sng.Artist == song.Artist);

//                song.NextOccuranceOfArtistIsAt = nextOccuranceIndex+1;

//                var artistGap = nextOccuranceIndex - currentIndex;
//                song.ArtistGapAhead = artistGap;

//                try
//                {
//                    var nextOccuranceTenCharsIndex = songListByTitleThenArtist.FindIndex(currentIndex + 1, sng => sng.Artist.Substring(0, 10) == song.Artist.Substring(0, 10));
//                    song.ArtistGapTenChars = nextOccuranceTenCharsIndex - currentIndex;

//                }
//                catch (Exception ex)
//                {

//                    //swallow the error
//                }

//                song.TitleCount = (from sng in songListByTitleThenArtist
//                                    where sng.Title == song.Title
//                                    select sng).Count();

//                previousTitle = song.Title;
//                previousArtist = song.Artist;
//            }
//        }


    }
}
