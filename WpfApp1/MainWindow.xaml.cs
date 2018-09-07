
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

namespace MindTheGap
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
            GetFiles();
        }

        private void GetFiles()
        {
            try
            {

                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                IEnumerable<string> musicFilePathsList = GetListOfMusicFilePaths();

                List<SongFileInfo> songList = PraseFileNamesIntoSongList(musicFilePathsList);

                var songsByTitleThenArtist = songList.OrderBy(sng => sng.Title).ThenBy(sng => sng.Artist);
                SongsByTitleThenArtistGrid.ItemsSource = songsByTitleThenArtist.ToList();


                var songsByFileName = songList.OrderBy(sng => sng.FileName);

                //was: CalculateGapsToSameArtistAndTitle(songsByTitleThenArtist);
                CalculateGapsToSameArtistAndTitle(songsByFileName);

                var songsBySmallestArtistGap = songList.OrderBy(sng => sng.ArtistGapAhead).ThenBy(sng => sng.Title).Where(sng => sng.ArtistGapAhead > 0);
                SongsBySmallestArtistGapGrid.ItemsSource = songsBySmallestArtistGap.ToList();

                //var songsByTitleGap = songList.OrderBy(sng => sng.TitleGap);
                //GridTwo.ItemsSource = songsByTitleGap;

                var songsByArtistThenArtistGapAhead = songList.OrderBy(sng => sng.Artist).ThenBy(sng => sng.ArtistGapAhead);
                GridTwo.ItemsSource = songsByArtistThenArtistGapAhead.ToList();


                ///////

                //if (1 == 2) // show ten chars result in lower right grid
                //{
                //    var songsBySmallestArtistTenCharsGap = songList.OrderBy(sng => sng.ArtistGapTenChars).ThenBy(sng => sng.Title).Where(sng => sng.ArtistGapTenChars > 0);

                //    GridThree.ItemsSource = songsBySmallestArtistTenCharsGap;
                //}
                //else // show sorted by artist count in lower right grid
                //{
                //    var songsByArtistCount = songList.OrderByDescending(sng => sng.ArtistCount).ThenBy(sng => sng.ArtistGapAhead);

                //    GridThree.ItemsSource = songsByArtistCount;
                //}

                var songsByTitleGapZeroAndUp = songList.Where(sng => sng.TitleGapAhead >= 0).OrderBy(sng => sng.TitleGapAhead);
                GridThree.ItemsSource = songsByTitleGapZeroAndUp;



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

                    if (filename.Contains("~~"))
                    {
                        tildePositionPlusOne = filename.IndexOf("~~") + 2;
                    }
                    else
                    {
                        tildePositionPlusOne = filename.IndexOf("~") + 1;
                    }

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

                    try
                    {
                        // https://stackoverflow.com/a/40839588/381082

                        var musicFile = TagLib.File.Create(filepath);
                        var genre = musicFile.Tag.FirstGenre;
                        song.Genre = genre;

                    }
                    catch (Exception)
                    {

                        throw;
                    }

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

            //Properties.Settings.Default.PreviousPathToMusicFiles = pathSelected;

            //Properties.Settings.Default.Properties["PriorPathToMusicFiles"] = pathSelected;

            var files = Directory.EnumerateFiles(pathSelected, "*.*")
                    .Where(fl => fl.ToLower().EndsWith(".mp3") || fl.ToLower().EndsWith(".flac") || fl.ToLower().EndsWith(".m4a") || fl.ToLower().EndsWith(".ape"));
            return files;
        }

        private void CalculateGapsToSameArtistAndTitle(IOrderedEnumerable<SongFileInfo> songsByFileName_IOrderedEnumerable)
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
                song.Position = currentIndex + 1;

                filename = song.FileName;


                // Get Artist gap +++++++++++++++++++++++++++++

                song.NextOccuranceOfArtistIsAt = GetNextOccuranceOfArtistIndex(songListByFileName, currentIndex, song);

                int artistGap = song.NextOccuranceOfArtistIsAt - (currentIndex + 1);

                if (artistGap < 0)
                {
                    // a negative artistGap means we've reached that last occurance of the artist
                    // set the artistGap to a big number (about the distance from the current song to the last song)
                    // set the next occurance of artist to the song list count
                    artistGap = songListByFileName.Count() - (currentIndex + 1);
                    song.NextOccuranceOfArtistIsAt = songListByFileName.Count();
                }

                song.ArtistGapAhead = artistGap;


                //try
                //{
                //    var nextArtistOccuranceTenCharsIndex = songListByFileName.FindIndex(currentIndex + 1, sng => sng.Artist.Substring(0, 10) == song.Artist.Substring(0, 10));
                //    song.ArtistGapTenChars = nextArtistOccuranceTenCharsIndex - currentIndex;

                //}
                //catch (Exception ex)
                //{

                //    //swallow the error
                //}

                song.ArtistCount = (from sng in songListByFileName
                                    where sng.Artist.ToUpper() == song.Artist.ToUpper()
                                    select sng).Count();

                // Get Title gap ++++++++++++++++++++++++++++++++

                //https://stackoverflow.com/a/38822432/381082
                var nextTitleOccuranceIndex = songListByFileName.FindIndex(currentIndex + 1, sng => sng.Title.ToUpper() == song.Title.ToUpper());

                song.NextOccuranceOfTitleIsAt = nextTitleOccuranceIndex + 1;

                var titleGap = nextTitleOccuranceIndex - (currentIndex + 1);

                if (titleGap < 0)
                {
                    // a negative titleGap means we've reached that last occurance of the title
                    // set the titleGap to a big number (about the distance from the current song to the last song)
                    // set the next occurance of title to the song list count
                    titleGap = songListByFileName.Count() - (currentIndex + 1);
                    song.NextOccuranceOfTitleIsAt = songListByFileName.Count();
                }

                song.TitleGapAhead = titleGap;

                //try
                //{
                //    var nextTitleOccuranceTenCharsIndex = songListByFileName.FindIndex(currentIndex + 1, sng => sng.Title.Substring(0, 10) == song.Title.Substring(0, 10));
                //    song.TitleGapTenChars = nextTitleOccuranceTenCharsIndex - currentIndex;

                //}
                //catch (Exception ex)
                //{

                //    //swallow the error
                //}

                song.TitleCount = (from sng in songListByFileName
                                   where sng.Title.ToUpper() == song.Title.ToUpper()
                                   select sng).Count();

                // Work with Taglib to get and set values +++++++++++++++++++++++++

                try
                {
                    // https://stackoverflow.com/a/40839588/381082

                    if (UpdateMusicFilePositionCheckbox.IsChecked == true)
                    {
                        var musicFile = TagLib.File.Create(song.FilePath);

                        if (musicFile.Tag.Track != (uint)song.Position)
                        {
                            musicFile.Tag.Track = (uint)song.Position;
                            musicFile.Save();
                        }

                    }

                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.ToString());

                    //throw;
                }

                // Get Genre gap +++++++++++++++++++++++++++++

                try
                {

                    //https://stackoverflow.com/a/38822432/381082
                    var nextGenreOccuranceIndex = songListByFileName.FindIndex(currentIndex + 1, sng => sng.Genre?.ToUpper() == song.Genre?.ToUpper());



                    song.NextOccuranceOfGenreIsAt = nextGenreOccuranceIndex + 1;

                    var genreGap = nextGenreOccuranceIndex - (currentIndex + 1);
                    song.GenreGapAhead = genreGap;

                }
                catch (Exception ex)
                {

                    System.Windows.MessageBox.Show(ex.ToString());
                }
                previousTitle = song.Title;
                previousArtist = song.Artist;
            }
        }

        private static int GetNextOccuranceOfArtistIndex(List<SongFileInfo> songListByFileName, int currentIndex, SongFileInfo song)
        {
            //https://stackoverflow.com/a/38822432/381082
            var nextArtistOccuranceIndex = songListByFileName.FindIndex(currentIndex + 1, sng => sng.Artist.ToUpper() == song.Artist.ToUpper());

            return nextArtistOccuranceIndex + 1;

        }

        private void RepositionGridFourSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            RepositionSong();

        }

        private void RepositionSong()
        {
            SongFileInfo selectedSong;

            try
            {
                selectedSong = (SongFileInfo)SongsByTitleThenArtistGrid.SelectedItem;

                if (selectedSong == null)
                {
                    System.Windows.MessageBox.Show("No song selected.");
                    return;
                }

                System.Windows.MessageBox.Show(selectedSong.FileName);

                if (true)
                {

                    int artistGapRequested;

                    if (int.TryParse(ArtistGapTextBox.Text, out artistGapRequested))
                    {
                        //parsing successful 
                    }
                    else
                    {
                        //parsing failed. 
                    }


                    int titleGapRequested;

                    if (int.TryParse(TitleGapTextBox.Text, out titleGapRequested))
                    {
                        //parsing successful 
                    }
                    else
                    {
                        //parsing failed. 
                    }

                    int genreGapRequested;

                    if (int.TryParse(GenreGapTextBox.Text, out genreGapRequested))
                    {
                        //parsing successful 
                    }
                    else
                    {
                        //parsing failed. 
                    }

                    List<SongFileInfo> allSongsList = SongsByTitleThenArtistGrid.ItemsSource.Cast<SongFileInfo>().ToList();

                    var allSongsListSorted = allSongsList.OrderBy(sng => sng.FileName);


                    //var songsFittingGapNeeds = allSongsList.Where(sng => sng.GenreGapAhead > genreGapRequested &&
                    //                                            sng.Genre == selectedSong.Genre).OrderBy(sng2 => sng2.FileName);  //orderby is critical

                    //wrong title: var songsFittingGapNeeds = allSongsList.Where(sng => sng.GenreGapAhead >= genreGapRequested &&
                    //     sng.TitleGapAhead >= titleGapRequested &&
                    //   sng.Genre == selectedSong.Genre).OrderBy(sng2 => sng2.FileName);  //orderby is critical

                    //Do Title Gap work...+++++++++++++++++++++++++++++++++++++++++++++++++++++++

                    // Get the handful of songs having a matching title and titleGapAhead

                    IOrderedEnumerable<SongFileInfo> songsFitting_TitleGap_NeedsMinusFirstSongsSorted;

                    if (allSongsListSorted.FirstOrDefault(sng => sng.Title.ToUpper() == selectedSong.Title.ToUpper()) != null)
                    {   // The wanted title exists so title gap needs to be tested. 
                        // Get the handfull of songs with this title that have a title gap greater than the requested title gap
                        // This list is missing all songs leading up to the first instance of the title
                        songsFitting_TitleGap_NeedsMinusFirstSongsSorted = allSongsListSorted.Where(sng => sng.TitleGapAhead >= titleGapRequested &&
                                                                                                sng.Title.ToUpper() == selectedSong.Title.ToUpper()).ToList().OrderBy(sng2 => sng2.FileName);  //orderby is critical                                                           
                    }
                    else
                    {   // The title does not even exist
                        // keep all songs since there's not title gap anywhere
                        songsFitting_TitleGap_NeedsMinusFirstSongsSorted = allSongsListSorted;
                    }


                    // From the handful of songs with the title and gap needs
                    // get the range of songs that are beyond the title gap needs
                    List<SongFileInfo> combinedRangesOfSongsQualifyingAs_Title_InsertLocations = new List<SongFileInfo>();

                    foreach (var songFittingTitleGapNeeds in songsFitting_TitleGap_NeedsMinusFirstSongsSorted)
                    {
                        var rangeOfSongsQualifyingAsInsertLocations = allSongsListSorted.Where(sng => sng.Position > songFittingTitleGapNeeds.Position + titleGapRequested &&
                                                                                            sng.Position < songFittingTitleGapNeeds.NextOccuranceOfTitleIsAt - titleGapRequested);

                        combinedRangesOfSongsQualifyingAs_Title_InsertLocations.AddRange(rangeOfSongsQualifyingAsInsertLocations);
                    }

                    // Now go back and get songs before the first occurance of the selected title
                    // Add a range of songs from the first song to the first of the selected title 
                    // (if gap need is satisfied and first song is not the selected title)
                    if (allSongsListSorted.FirstOrDefault().Title != selectedSong.Title)
                    {
                        var firstInstanceOfTitle = allSongsListSorted.FirstOrDefault(sng => sng.Title.ToUpper() == selectedSong.Title.ToUpper());
                        var rangeOfSongsFromFirstFittingTitleGapNeeds = allSongsListSorted.Where(sng => sng.Position < firstInstanceOfTitle.Position - titleGapRequested);
                        combinedRangesOfSongsQualifyingAs_Title_InsertLocations.AddRange(rangeOfSongsFromFirstFittingTitleGapNeeds);
                    }

                    if (combinedRangesOfSongsQualifyingAs_Title_InsertLocations.Count() == 0)
                    {
                        System.Windows.MessageBox.Show("Title gap not met");
                    }

                    IOrderedEnumerable<SongFileInfo> combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted = combinedRangesOfSongsQualifyingAs_Title_InsertLocations.OrderBy(sng => sng.FileName);

                    //Do Artist Gap work...++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                    // Get the handful of songs having a matching artist and artistGapAhead

                    IOrderedEnumerable<SongFileInfo> songsFitting_ArtistGap_NeedsMinusFirstSongsSorted;
                    List<SongFileInfo> combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocations = new List<SongFileInfo>();

                    //First see if the selected artist exists in the already found title insert locations
                    if (combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.FirstOrDefault(sng => sng.Artist.ToUpper() == selectedSong.Artist.ToUpper()) != null)
                    {
                        //from the TITLE insert locations (variable name is confusing) find the songs from the selected artist
                        // that are far enough away from songs by the same artist (songs meeting artist gap needs)
                        // The found list will not include any songs leading up to the first song by this artist.
                        songsFitting_ArtistGap_NeedsMinusFirstSongsSorted = combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.Where(sng => sng.ArtistGapAhead >= artistGapRequested &&
                                                                            sng.Artist.ToUpper() == selectedSong.Artist.ToUpper()).OrderBy(sng2 => sng2.FileName);  //orderby is critical

                        foreach (var songFittingArtistGapNeeds in songsFitting_ArtistGap_NeedsMinusFirstSongsSorted)
                        {
                            // loop through each song looking for ranges fitting artist gap needs
                            var rangeOfSongsQualifyingAsInsertLocations = combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.Where(sng => sng.Position > songFittingArtistGapNeeds.Position + artistGapRequested &&
                                                                                                sng.Position < songFittingArtistGapNeeds.NextOccuranceOfArtistIsAt - artistGapRequested);

                            // add the range to the combined ranges of songs so far
                            combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocations.AddRange(rangeOfSongsQualifyingAsInsertLocations);
                        }

                    }
                    else
                    {   // The artist does not even exist so use what was found during the title gap search
                        combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocations = combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.ToList();
                    }



                    // Now go back and get songs before the first occurance of the selected artist 
                    // Add a range of songs from the first song to the first of the selected artist 
                    // (if gap need is satisfied and first song is not the selected artist)
                    if (combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.FirstOrDefault().Artist != selectedSong.Artist)
                    {

                        var firstInstanceOfArtist = combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.FirstOrDefault(sng => sng.Artist.ToUpper() == selectedSong.Artist.ToUpper());
                        if (firstInstanceOfArtist != null)
                        {
                            var rangeOfSongsCombinedFromFirstFitting_ArtistGap_Needs = combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.Where(sng => sng.Position < firstInstanceOfArtist.Position - artistGapRequested);
                            combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocations.AddRange(rangeOfSongsCombinedFromFirstFitting_ArtistGap_Needs);
                        }
                    }

                    if (combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocations.Count() == 0)
                    {
                        System.Windows.MessageBox.Show("Title gap not met");
                    }

                    var combinedRangesOfSongsQualifyingAs_ArtistAndTitleAndGenre_InsertLocationsSorted = combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocations.OrderBy(sng => sng.FileName).ToList();

                    //Do Genre Gap work...++++++++++++++++++++++++++++++++++++++++++++++++

                    // Get the handful of songs having a matching genre and genreGapAhead
                    var songsFittingGenreGapNeedsMinusFirstSongsSorted = combinedRangesOfSongsQualifyingAs_ArtistAndTitleAndGenre_InsertLocationsSorted.Where(sng => sng.GenreGapAhead >= genreGapRequested &&
                                                                        sng.Genre.ToUpper() == selectedSong.Genre.ToUpper()).OrderBy(sng2 => sng2.FileName);  //orderby is critical

                    List<SongFileInfo> songsQualifyingAs_ArtistAndTitleAndGenre_InsertLocations = new List<SongFileInfo>();


                    if (songsFittingGenreGapNeedsMinusFirstSongsSorted != null)
                    {
                        foreach (var songFittingGenreGapNeeds in songsFittingGenreGapNeedsMinusFirstSongsSorted)
                        {
                            //var rangeOfSongsQualifyingAsInsertLocations = combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocationsSorted.Where(sng => sng.Position > songFittingGenreGapNeeds.Position + genreGapRequested &&
                            //                                                                    sng.Position < songFittingGenreGapNeeds.NextOccuranceOfGenreIsAt - genreGapRequested);

                            //IMPORTANT: Genre gap needs differ from artist and title gap needs!!!
                            //For Genre gap we want any range with a gap greater than the requested genre gap
                            // (we do NOT add and subtract the genreGapRequested from either end when searching for the range)
                            // Get songs between (and not including) the current song location and the next occurance of the genre location
                            // but only get this range if the GenreGapAhead is greater than the genreGapRequested
                            var rangeOfSongsQualifyingAsInsertLocations = combinedRangesOfSongsQualifyingAs_ArtistAndTitleAndGenre_InsertLocationsSorted.Where(sng => sng.Position > songFittingGenreGapNeeds.Position && sng.Position < songFittingGenreGapNeeds.NextOccuranceOfGenreIsAt && songFittingGenreGapNeeds.GenreGapAhead >= genreGapRequested);


                            if (rangeOfSongsQualifyingAsInsertLocations != null)
                            {
                                // the above insert locations are 1, 2, 3 or more songs in a row with the same genre. Pick one in the middle.
                                int whichSongIndex;
                                if (rangeOfSongsQualifyingAsInsertLocations.Count() == 1)
                                {
                                    whichSongIndex = 1;
                                }
                                else
                                {
                                    whichSongIndex = rangeOfSongsQualifyingAsInsertLocations.Count() / 2;
                                }
                                var songMeetingArtistTitleAndGenreNeeds = rangeOfSongsQualifyingAsInsertLocations.ElementAtOrDefault(whichSongIndex - 1);

                                //was: combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocations.AddRange(rangeOfSongsQualifyingAsInsertLocations);
                                if (songMeetingArtistTitleAndGenreNeeds != null)
                                {
                                    songsQualifyingAs_ArtistAndTitleAndGenre_InsertLocations.Add(songMeetingArtistTitleAndGenreNeeds);
                                }
                            }
                        }
                    }
                    // NOTE: FOR GENRE THIS MAY BE A TINY RANGE: add a range of songs from the first song to the first of the selected genre 
                    // (if gap need is satisfied and first song is not the selected genre)
                    if (combinedRangesOfSongsQualifyingAs_ArtistAndTitleAndGenre_InsertLocationsSorted.FirstOrDefault() != null)
                    {
                        if (combinedRangesOfSongsQualifyingAs_ArtistAndTitleAndGenre_InsertLocationsSorted.FirstOrDefault().Genre != selectedSong.Genre)
                        {
                            var firstInstanceOfGenre = combinedRangesOfSongsQualifyingAs_ArtistAndTitleAndGenre_InsertLocationsSorted.FirstOrDefault(sng => sng.Genre.ToUpper() == selectedSong.Genre.ToUpper());
                            if (firstInstanceOfGenre != null)
                            {
                                var rangeOfSongsCombinedFromFirstFittingGenreGapNeeds = combinedRangesOfSongsQualifyingAs_ArtistAndTitleAndGenre_InsertLocationsSorted.Where(sng => sng.Position < firstInstanceOfGenre.Position - genreGapRequested);
                                songsQualifyingAs_ArtistAndTitleAndGenre_InsertLocations.AddRange(rangeOfSongsCombinedFromFirstFittingGenreGapNeeds);
                            }
                        }
                    }
                    
                    if (songsQualifyingAs_ArtistAndTitleAndGenre_InsertLocations.Count() == 0)
                    {
                        System.Windows.MessageBox.Show("Genre gap not met");
                    }

                    var songsQualifyingAs_ArtistAndTitleAndGenre_InsertLocationsSorted = songsQualifyingAs_ArtistAndTitleAndGenre_InsertLocations.OrderBy(sng => sng.FileName).ToList();

                    //Show message: ++++++++++++++++++++++++++++++++++++++++++++++++


                    GridThree.ItemsSource = songsQualifyingAs_ArtistAndTitleAndGenre_InsertLocations;

                    string msg = string.Empty;

                    foreach (var songFittingArtistTitleAndGenreNeeds in songsQualifyingAs_ArtistAndTitleAndGenre_InsertLocationsSorted)
                    {
                        msg += songFittingArtistTitleAndGenreNeeds.FileName + ", pos: " + songFittingArtistTitleAndGenreNeeds.Position + ", genre gap: " + songFittingArtistTitleAndGenreNeeds.GenreGapAhead + " (other gaps meaningless from this song) " + Environment.NewLine;  //, artistGapAhead: " + songFittingArtistTitleAndGenreNeeds.ArtistGapAhead + ", titleGapAhead: " + songFittingArtistTitleAndGenreNeeds.TitleGapAhead + ", GenreGapAhead: " + songFittingArtistTitleAndGenreNeeds.GenreGapAhead + Environment.NewLine;
                        if (msg.Length > 2001)
                        {
                            break;
                        }
                    }

                    if (msg.Trim() == String.Empty)
                    {
                        msg = "No songs fit gap needs.";
                    }


                    if (msg.Length < 1999)
                    {
                        System.Windows.MessageBox.Show(msg);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("(partial) See grid three for full list: " + Environment.NewLine + msg.Substring(0, 1999));
                    }


                    try
                    {
                        //https://stackoverflow.com/a/30981377/381082
                        var songs = new List<string>();
                        foreach (var song in songsQualifyingAs_ArtistAndTitleAndGenre_InsertLocations)
                        {
                            songs.Add(song.FileName);
                        }

                        var common = new string(songs.Select(str => str.TakeWhile((c, index) => songs.All(s => s[index] == c)))
                                                       .FirstOrDefault().ToArray());

                        if (common.Trim().Length > 0)
                        {
                            //System.Windows.MessageBox.Show("Common titles: " + common);
                        }
                    }
                    catch (Exception)
                    {

                        //throw;
                    }



                }

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
        }


        private void RenameSelectedSongButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: alphabetically insert selected song in the secondSongName position
            // Try These:
            // https://stackoverflow.com/a/15303016/381082
            // see also: http://www.rohland.co.za/index.php/2009/10/31/csharp-html-diff-algorithm/
            // and: https://www.codeproject.com/Articles/13326/An-O-ND-Difference-Algorithm-for-C
            // and: https://stackoverflow.com/questions/3343874/compare-two-strings-and-get-the-difference
            // and: 

            SongFileInfo selectedSong;
            SongFileInfo sortSelectedSongAlphabeticallyAfterThisSong;

            try
            {
                selectedSong = (SongFileInfo)SongsByTitleThenArtistGrid.SelectedItem;

                if (selectedSong == null)
                {
                    System.Windows.MessageBox.Show("No song selected from grid one.");
                    return;
                }

                //System.Windows.MessageBox.Show("Song to rename: " + selectedSong.FileName);


                sortSelectedSongAlphabeticallyAfterThisSong = (SongFileInfo)GridThree.SelectedItem;

                if (sortSelectedSongAlphabeticallyAfterThisSong == null)
                {
                    System.Windows.MessageBox.Show("No song selected from grid three.");
                    return;
                }

                //System.Windows.MessageBox.Show("Alphabetically follow this song: " + sortSelectedSongAlphabeticallyAfterThisSong.FileName);

                List<SongFileInfo> allSongsList = SongsByTitleThenArtistGrid.ItemsSource.Cast<SongFileInfo>().ToList();

                var nextSong = allSongsList.Where(sng => sng.Position == sortSelectedSongAlphabeticallyAfterThisSong.Position + 1).FirstOrDefault();

                //System.Windows.MessageBox.Show("Next song: " + nextSong.FileName);


                string newSongName = GetNewSongFileNameSmushedAlphabeticallyBetweenTwoSongNames(sortSelectedSongAlphabeticallyAfterThisSong.FileName, nextSong.FileName, selectedSong.FileName);

                var ans = System.Windows.MessageBox.Show("New song name: " + newSongName, "Rename song?", MessageBoxButton.YesNoCancel);

                if (ans == MessageBoxResult.Yes)
                {
                    //https://stackoverflow.com/a/20724492/381082

                    //var sourcePath = selectedSong.FilePath // @"C:\folder\oldname.txt";
                    var directory = System.IO.Path.GetDirectoryName(selectedSong.FilePath);
                    var extension = System.IO.Path.GetExtension(selectedSong.FilePath);
                    var destinationPath = System.IO.Path.Combine(directory, newSongName);
                    destinationPath = System.IO.Path.ChangeExtension(destinationPath, extension);
                    File.Move(selectedSong.FilePath, destinationPath);

                   GetFiles();
                }




            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
        }

        private void SongsByTitleThenArtistGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }




        //[Test]
        //[TestCase("parralele", "parallel", "par[ralele]")]
        //[TestCase("personil", "personal", "person[i]l")]
        //[TestCase("disfuncshunal", "dysfunctional", "d[isfuncshu]nal")]
        //[TestCase("ato", "auto", "a[]to")]
        //[TestCase("inactioned", "inaction", "inaction[ed]")]
        //[TestCase("refraction", "fraction", "[re]fraction")]
        //[TestCase("adiction", "ad[]diction", "ad[]iction")]
        public string GetNewSongFileNameSmushedAlphabeticallyBetweenTwoSongNames(string firstSongName, string secondSongName, string songToRename)
        {
            // from: https://stackoverflow.com/a/15303016/381082
            // see also: http://www.rohland.co.za/index.php/2009/10/31/csharp-html-diff-algorithm/
            // and: https://www.codeproject.com/Articles/13326/An-O-ND-Difference-Algorithm-for-C
            // and: https://stackoverflow.com/questions/3343874/compare-two-strings-and-get-the-difference
            // and: 

            string letterFromFirstSongAtThisIndex = string.Empty;
            string letterFromSecondSongAtThisIndex = string.Empty;
            int firstDifferentLetterIndex = -1, lastDifferentLetterIndex = -1;
            string letterAlphabeticallyOneBeyondFirstSongLetterAtThisIndex;
            string beforeOpeningBrace = string.Empty;
            string withinBraces = string.Empty;
            string afterClosingBrace = string.Empty;
            string firstSongNameMinusSingleQuote = firstSongName.Replace("'", "");
            string secondSongNameMinusSingleQuote = secondSongName.Replace("'", "");

            string result = null;
            //int shorterSongNameLength = (firstSongName.Length < secondSongName.Length ? firstSongName.Length : secondSongName.Length);
            int shorterSongNameLength = (firstSongNameMinusSingleQuote.Length < secondSongNameMinusSingleQuote.Length ? firstSongNameMinusSingleQuote.Length : secondSongNameMinusSingleQuote.Length);

            // Get opening bracket postion - [
            for (int indexToLetter = 0; indexToLetter < shorterSongNameLength; indexToLetter++)
            {
                //letterFromFirstSongAtThisIndex = firstSongName[indexToLetter].ToString().ToUpper();
                letterFromFirstSongAtThisIndex = firstSongNameMinusSingleQuote[indexToLetter].ToString().ToUpper();
                letterFromSecondSongAtThisIndex = secondSongNameMinusSingleQuote[indexToLetter].ToString().ToUpper();

                if ((letterFromSecondSongAtThisIndex != letterFromFirstSongAtThisIndex) || (letterFromFirstSongAtThisIndex == "~")) //give up at the tilde
                {   //We've reached a non-matching letter
                    firstDifferentLetterIndex = indexToLetter;
                    // if more than one letter beyond is not important than this is the correct break point
                    //break;

                    // now see if the non-matching letter more than one letter away alphabetically so there is room for another song here alphabetically
                    letterAlphabeticallyOneBeyondFirstSongLetterAtThisIndex = (++letterFromFirstSongAtThisIndex.ToCharArray()[0]).ToString();
                    if (letterFromSecondSongAtThisIndex != letterAlphabeticallyOneBeyondFirstSongLetterAtThisIndex)
                    {
                        break;
                    }
                }
            }

            // Get closing bracket postion - ]
            var a = secondSongNameMinusSingleQuote.Reverse().ToArray();
            //var b = firstSongName.Reverse().ToArray();
            var b = firstSongNameMinusSingleQuote.Reverse().ToArray();
            for (int indexToLetter = 0; indexToLetter < shorterSongNameLength; indexToLetter++)
            {
                if (a[indexToLetter].ToString().ToUpper() != b[indexToLetter].ToString().ToUpper())
                {
                    lastDifferentLetterIndex = indexToLetter;
                    break;
                }
            }

            if (firstDifferentLetterIndex == -1 && lastDifferentLetterIndex == -1)
                //result = firstSongName;
                result = firstSongNameMinusSingleQuote;
            else
            {
                var sb = new StringBuilder();
                if (firstDifferentLetterIndex == -1)
                    firstDifferentLetterIndex = shorterSongNameLength;
                if (lastDifferentLetterIndex == -1)
                    lastDifferentLetterIndex = shorterSongNameLength;
                // If same letter repeats multiple times (ex: addition)
                // and error is on that letter, we have to trim trail.
                if (firstDifferentLetterIndex + lastDifferentLetterIndex > shorterSongNameLength)
                    lastDifferentLetterIndex = shorterSongNameLength - firstDifferentLetterIndex;

                if (firstDifferentLetterIndex > 0)
                {
                    //beforeOpeningBrace = firstSongName.Substring(0, firstDifferentLetterIndex); 
                    beforeOpeningBrace = firstSongNameMinusSingleQuote.Substring(0, firstDifferentLetterIndex);
                    sb.Append(beforeOpeningBrace);
                }

                sb.Append("[");

                //if (lastDifferentLetterIndex > -1 && lastDifferentLetterIndex + firstDifferentLetterIndex < firstSongName.Length)
                if (lastDifferentLetterIndex > -1 && lastDifferentLetterIndex + firstDifferentLetterIndex < firstSongNameMinusSingleQuote.Length)
                {
                    //withinBraces = firstSongName.Substring(firstDifferentLetterIndex, firstSongName.Length - lastDifferentLetterIndex - firstDifferentLetterIndex);
                    withinBraces = firstSongNameMinusSingleQuote.Substring(firstDifferentLetterIndex, firstSongNameMinusSingleQuote.Length - lastDifferentLetterIndex - firstDifferentLetterIndex);
                    sb.Append(withinBraces);
                }

                sb.Append("]");

                if (lastDifferentLetterIndex > 0)
                {
                    //afterClosingBrace = firstSongName.Substring(firstSongName.Length - lastDifferentLetterIndex, lastDifferentLetterIndex);
                    afterClosingBrace = firstSongNameMinusSingleQuote.Substring(firstSongNameMinusSingleQuote.Length - lastDifferentLetterIndex, lastDifferentLetterIndex);
                    sb.Append(afterClosingBrace);
                }
                result = sb.ToString();

            }

            System.Windows.MessageBox.Show(firstSongNameMinusSingleQuote + Environment.NewLine + secondSongNameMinusSingleQuote + Environment.NewLine + result + Environment.NewLine + Environment.NewLine + "Before opening brace: " + beforeOpeningBrace + Environment.NewLine + "Within braces: " + withinBraces + Environment.NewLine + "After closing brace: " + afterClosingBrace);

            string appendThisToSongToRename = string.Empty;

            char firstLetterWithinBraces = withinBraces[0];

            //https://theasciicode.com.ar/extended-ascii-code/registered-trademark-symbol-ascii-code-169.html

            if (firstLetterWithinBraces.ToString() == "-")
            {
                firstLetterWithinBraces = " ".ToCharArray()[0];
            }

            if (firstLetterWithinBraces.ToString() == "~")
            {
                firstLetterWithinBraces = "}".ToCharArray()[0];
            }


            char nextLetterAlphabetically = ++firstLetterWithinBraces;

            char charFromSecondSongName = secondSongName.Substring(beforeOpeningBrace.Length, 1).ToCharArray()[0];


            if (nextLetterAlphabetically == charFromSecondSongName && nextLetterAlphabetically != '~')
            {
                //THIS CONDITION SHOULD NEVER BE TRUE!!!

            }
            else
            {
                appendThisToSongToRename = beforeOpeningBrace + nextLetterAlphabetically + "~ ";
            }

            string newSongName = string.Empty;

            newSongName = appendThisToSongToRename.ToLower() + songToRename;


            return newSongName;

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
