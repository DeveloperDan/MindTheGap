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

                string pathSelected = string.Empty;

                using (var dialog = new FolderBrowserDialog())
                {
                    //if (UseHardCodedPathRadioButton.IsChecked == true)
                    //{
                    //    pathSelected = @"D:\Dan\Music\!!!!Mstr\!!!MASTER Instrumentals 6-3-18\!Fav Instrumentals Shuffled (6-3-18)\Jazz Instr 03 - 222 songs Artists Alpha Shuffled - Add to artists";
                    //}
                    //else if (UseHardCodedPath2RadioButton.IsChecked == true)
                    //{
                    //    pathSelected = @"\\dlee13\H\!!!MASTER Instrumentals 6-19-18\!Fav Instrumentals Shuffled (6-19-18)\Jazz Instr 03 - 339 songs Artists Alpha Shuffled - Cont w Bill Evans";
                    //}

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


                var songList = new List<SongFileInfo>();

                foreach (var filepath in files)
                {

                    string filename = string.Empty;
                    int dashPosition;
                    string title;
                    int lengthOfTitle;
                    string artist;

                    SongFileInfo song;

                    try
                    {
                        filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
                        dashPosition = filename.IndexOf(" - ");
                        title = filename.Substring(0, dashPosition);
                        lengthOfTitle = filename.Length - dashPosition;
                        artist = filename.Substring(dashPosition + 3, lengthOfTitle - 3);

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

                var songsByArtistThenTitle = songList.OrderBy(sng => sng.Artist).ThenBy(sng => sng.Title);

                SongsByArtistThenTitleGrid.ItemsSource = songsByArtistThenTitle;

                var songsByTitleThenArtist = songList.OrderBy(sng => sng.Title).ThenBy(sng => sng.Artist);

                SongsByTitleThenArtistGrid.ItemsSource = songsByTitleThenArtist;

                GetGapToNextOccurranceOfThisArtist(songsByTitleThenArtist);

                var songsBySmallestArtistGap = songList.OrderBy(sng => sng.ArtistGapAhead).ThenBy(sng => sng.Title).Where(sng=>sng.ArtistGapAhead > 0);

                SongsBySmallestArtistGapGrid.ItemsSource = songsBySmallestArtistGap;

                ///////

                if (1 == 2) // show ten chars result in lower right grid
                {
                    var songsBySmallestArtistTenCharsGap = songList.OrderBy(sng => sng.ArtistGapTenChars).ThenBy(sng => sng.Title).Where(sng => sng.ArtistGapTenChars > 0);

                    SongsByArtistCountGrid.ItemsSource = songsBySmallestArtistTenCharsGap;
                }
                else // show sorted by artist count in lower right grid
                {
                    var songsByArtistCount = songList.OrderByDescending(sng => sng.ArtistCount).ThenBy(sng=> sng.ArtistGapAhead);

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

        private static void GetGapToNextOccurranceOfThisArtist(IOrderedEnumerable<SongFileInfo> songsByTitleThenArtist_IOrderedEnumerable)
        {
            int artistCount = 0;
            string previousTitle = string.Empty;
            string previousArtist = string.Empty;
            string filename = string.Empty;

            var songListByTitleThenArtist = songsByTitleThenArtist_IOrderedEnumerable.ToList<SongFileInfo>();

            for (int currentIndex = 0; currentIndex < songsByTitleThenArtist_IOrderedEnumerable.Count(); currentIndex++)
            {
                var song = songsByTitleThenArtist_IOrderedEnumerable.ElementAt(currentIndex);
                song.Position = currentIndex+1;

                filename = song.FileName;

                //https://stackoverflow.com/a/38822432/381082
                var nextOccuranceIndex = songListByTitleThenArtist.FindIndex(currentIndex+1, sng => sng.Artist == song.Artist);

                song.NextOccuranceOfArtistIsAt = nextOccuranceIndex+1;

                var artistGap = nextOccuranceIndex - currentIndex;
                song.ArtistGapAhead = artistGap;

                try
                {
                    var nextOccuranceTenCharsIndex = songListByTitleThenArtist.FindIndex(currentIndex + 1, sng => sng.Artist.Substring(0, 10) == song.Artist.Substring(0, 10));
                    song.ArtistGapTenChars = nextOccuranceTenCharsIndex - currentIndex;

                }
                catch (Exception ex)
                {

                    //swallow the error
                }

                song.ArtistCount = (from sng in songListByTitleThenArtist
                                    where sng.Artist == song.Artist
                                    select sng).Count();

                previousTitle = song.Title;
                previousArtist = song.Artist;
            }
        }
    }
}
