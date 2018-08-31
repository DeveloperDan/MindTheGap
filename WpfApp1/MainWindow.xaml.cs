
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
                    tildePositionPlusOne = filename.IndexOf("~") + 1;
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
                catch (Exception)
                {

                    throw;
                }

                // Get Genre gap +++++++++++++++++++++++++++++

                //https://stackoverflow.com/a/38822432/381082
                var nextGenreOccuranceIndex = songListByFileName.FindIndex(currentIndex + 1, sng => sng.Genre.ToUpper() == song.Genre.ToUpper());

                song.NextOccuranceOfGenreIsAt = nextGenreOccuranceIndex + 1;

                var genreGap = nextGenreOccuranceIndex - (currentIndex + 1);
                song.GenreGapAhead = genreGap;

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

                    IOrderedEnumerable<SongFileInfo> songsFitting_Title_GapNeedsMinusFirstSongsSorted;

                    if (allSongsListSorted.FirstOrDefault(sng => sng.Title.ToUpper() == selectedSong.Title.ToUpper()) != null)
                    {   // The wanted title exists so title gap needs to be tested. 
                        // Get the handfull of songs with this title that have a title gap greater that the requested title gap
                        // This list is missing all songs leading up to the first instance of the title
                        songsFitting_Title_GapNeedsMinusFirstSongsSorted = allSongsListSorted.Where(sng => sng.TitleGapAhead >= titleGapRequested &&
                                                                                                sng.Title.ToUpper() == selectedSong.Title.ToUpper()).ToList().OrderBy(sng2 => sng2.FileName);  //orderby is critical                                                           
                    }
                    else
                    {   // The title does not even exist
                        // keep all songs since there's not title gap anywhere
                        songsFitting_Title_GapNeedsMinusFirstSongsSorted = allSongsListSorted;
                    }


                    // From the handful of songs with the title and gap needs
                    // get the range of songs that are beyond the title gap needs
                    List<SongFileInfo> combinedRangesOfSongsQualifyingAs_Title_InsertLocations = new List<SongFileInfo>();

                    foreach (var songFittingTitleGapNeeds in songsFitting_Title_GapNeedsMinusFirstSongsSorted)
                    {
                        var rangeOfSongsQualifyingAsInsertLocations = allSongsListSorted.Where(sng => sng.Position > songFittingTitleGapNeeds.Position + titleGapRequested &&
                                                                                            sng.Position < songFittingTitleGapNeeds.NextOccuranceOfTitleIsAt - titleGapRequested);

                        combinedRangesOfSongsQualifyingAs_Title_InsertLocations.AddRange(rangeOfSongsQualifyingAsInsertLocations);
                    }

                    // add a range of songs from the first song to the first of the selected title 
                    // (if gap need is satisfied and first song is not the selected title)
                    if (allSongsListSorted.FirstOrDefault().Title != selectedSong.Title)
                    {
                        var firstInstanceOfTitle = allSongsListSorted.FirstOrDefault(sng => sng.Title.ToUpper() == selectedSong.Title.ToUpper());
                        var rangeOfSongsFromFirstFittingTitleGapNeeds = allSongsListSorted.Where(sng => sng.Position < firstInstanceOfTitle.Position - titleGapRequested);
                        combinedRangesOfSongsQualifyingAs_Title_InsertLocations.AddRange(rangeOfSongsFromFirstFittingTitleGapNeeds);
                    }

                    IOrderedEnumerable<SongFileInfo> combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted = combinedRangesOfSongsQualifyingAs_Title_InsertLocations.OrderBy(sng => sng.FileName);

                    //Do Artist Gap work...++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                    // Get the handful of songs having a matching artist and artistGapAhead

                    IOrderedEnumerable<SongFileInfo> songsFitting_Artist_GapNeedsMinusFirstSongsSorted;
                    List<SongFileInfo> combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocations = new List<SongFileInfo>();

                    if (combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.FirstOrDefault(sng => sng.Artist.ToUpper() == selectedSong.Artist.ToUpper()) != null)
                    {
                        songsFitting_Artist_GapNeedsMinusFirstSongsSorted = combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.Where(sng => sng.ArtistGapAhead >= artistGapRequested &&
                                                                            sng.Artist.ToUpper() == selectedSong.Artist.ToUpper()).OrderBy(sng2 => sng2.FileName);  //orderby is critical

                        foreach (var songFittingArtistGapNeeds in songsFitting_Artist_GapNeedsMinusFirstSongsSorted)
                        {
                            var rangeOfSongsQualifyingAsInsertLocations = combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.Where(sng => sng.Position > songFittingArtistGapNeeds.Position + artistGapRequested &&
                                                                                                sng.Position < songFittingArtistGapNeeds.NextOccuranceOfArtistIsAt - artistGapRequested);

                            combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocations.AddRange(rangeOfSongsQualifyingAsInsertLocations);
                        }

                    }
                    else
                    {   // The artist does not even exist so use what was found during the title gap search
                        combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocations = combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.ToList();
                    }



                    // add a range of songs from the first song to the first of the selected artist 
                    // (if gap need is satisfied and first song is not the selected artist)
                    if (combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.FirstOrDefault().Artist != selectedSong.Artist)
                    {

                        var firstInstanceOfArtist = combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.FirstOrDefault(sng => sng.Artist.ToUpper() == selectedSong.Artist.ToUpper());
                        if (firstInstanceOfArtist != null)
                        {
                            var rangeOfSongsCombinedFromFirstFitting_Artist_GapNeeds = combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocationsSorted.Where(sng => sng.Position < firstInstanceOfArtist.Position - artistGapRequested);
                            combinedRangesOfSongsQualifyingAs_ArtistAndTitle_InsertLocations.AddRange(rangeOfSongsCombinedFromFirstFitting_Artist_GapNeeds);
                        }
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
                                int whichSongIndex = rangeOfSongsQualifyingAsInsertLocations.Count() / 2;
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
                    //was: combinedRangesOfSongsQualifyingAs_ArtistAndTitleAndGenre_InsertLocationsSorted = songsQualifyingAs_ArtistAndTitleAndGenre_InsertLocations.OrderBy(sng => sng.FileName).ToList();

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
                            System.Windows.MessageBox.Show("Common titles: " + common);
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

        private void RepositionSongOLD()
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


                    //var songsFittingGapNeeds = allSongsList.Where(sng => sng.GenreGapAhead > genreGapRequested &&
                    //                                            sng.Genre == selectedSong.Genre).OrderBy(sng2 => sng2.FileName);  //orderby is critical

                    var songsFittingGapNeeds = allSongsList.Where(sng => sng.GenreGapAhead >= genreGapRequested &&
                                                                sng.TitleGapAhead >= titleGapRequested &&
                                                                sng.Genre.ToUpper() == selectedSong.Genre.ToUpper()).OrderBy(sng2 => sng2.FileName);  //orderby is critical

                    string msg = string.Empty;
                    int gapToNextOccuranceOfArtist = 0;
                    int positionOfPreviousOccuranceOfArtist = 0;
                    int gapFromPreviousOccuranceOfArtist = 0;


                    foreach (var songFittingGapNeeds in songsFittingGapNeeds)
                    {

                        var allSongsStartingAtGapNeedsPostion = allSongsList.Where(sng => sng.Position > songFittingGapNeeds.Position)
                                                                                .OrderBy(sng2 => sng2.FileName);    //orderby is critical

                        //tests below...

                        //var allSongsStartingAtGapNeedsPostion = allSongsList.Where(sng => sng.Position > songFittingGapNeeds.Position - artistGapRequested)
                        //                                                        .OrderBy(sng2 => sng2.FileName);    //orderby is critical

                        //var allSongsStartingAtGapNeedsPostion = allSongsList.Where(sng => sng.Position > positionOfPreviousOccuranceOfArtist)
                        //                                                        .OrderBy(sng2 => sng2.FileName);    //orderby is critical

                        foreach (var thisSongFromAllSong in allSongsStartingAtGapNeedsPostion)
                        {

                            if (thisSongFromAllSong.Artist.ToUpper() == selectedSong.Artist.ToUpper())
                            {

                                gapToNextOccuranceOfArtist = thisSongFromAllSong.Position - songFittingGapNeeds.Position;
                                gapFromPreviousOccuranceOfArtist = songFittingGapNeeds.Position - positionOfPreviousOccuranceOfArtist;

                                if (gapToNextOccuranceOfArtist >= artistGapRequested)
                                {
                                    if (gapFromPreviousOccuranceOfArtist >= artistGapRequested)
                                    {
                                        msg += songFittingGapNeeds.FileName + ", gapFromArtist: " + gapFromPreviousOccuranceOfArtist + ", gapToArtist: " + gapToNextOccuranceOfArtist + ", nextOccuranceOfArtistPosition: " + thisSongFromAllSong.Position + ", GenreGapAhead: " + songFittingGapNeeds.GenreGapAhead + Environment.NewLine;
                                    }
                                }

                                positionOfPreviousOccuranceOfArtist = thisSongFromAllSong.Position;

                                break;
                            }
                        }

                    }

                    if (msg.Length < 1999)
                    {
                        System.Windows.MessageBox.Show(msg);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("(partial) See grid three for full list: " + Environment.NewLine + msg.Substring(0, 1999));
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
            // TODO: alphabetically insert selected song in the correct position
            // Try These:
            // https://stackoverflow.com/a/15303016/381082
            // see also: http://www.rohland.co.za/index.php/2009/10/31/csharp-html-diff-algorithm/
            // and: https://www.codeproject.com/Articles/13326/An-O-ND-Difference-Algorithm-for-C
            // and: https://stackoverflow.com/questions/3343874/compare-two-strings-and-get-the-difference
            // and: 




            SongFileInfo selectedSong;
            SongFileInfo sortAlphabeticallyAfterThisSong;

            try
            {
                selectedSong = (SongFileInfo)SongsByTitleThenArtistGrid.SelectedItem;

                if (selectedSong == null)
                {
                    System.Windows.MessageBox.Show("No song selected from grid one.");
                    return;
                }

                System.Windows.MessageBox.Show("Song to rename: " + selectedSong.FileName);


                sortAlphabeticallyAfterThisSong = (SongFileInfo)GridThree.SelectedItem;

                if (sortAlphabeticallyAfterThisSong == null)
                {
                    System.Windows.MessageBox.Show("No song selected from grid three.");
                    return;
                }

                System.Windows.MessageBox.Show("Alphabetically follow this song: " + sortAlphabeticallyAfterThisSong.FileName);




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
        public void CompareStringsTest(string attempted, string correct, string expectedResult)
        {
            // from: https://stackoverflow.com/a/15303016/381082
            // see also: http://www.rohland.co.za/index.php/2009/10/31/csharp-html-diff-algorithm/
            // and: https://www.codeproject.com/Articles/13326/An-O-ND-Difference-Algorithm-for-C
            // and: https://stackoverflow.com/questions/3343874/compare-two-strings-and-get-the-difference
            // and: 

            int first = -1, last = -1;

            string result = null;
            int shorterLength = (attempted.Length < correct.Length ? attempted.Length : correct.Length);

            // First - [
            for (int i = 0; i < shorterLength; i++)
            {
                if (correct[i] != attempted[i])
                {
                    first = i;
                    break;
                }
            }

            // Last - ]
            var a = correct.Reverse().ToArray();
            var b = attempted.Reverse().ToArray();
            for (int i = 0; i < shorterLength; i++)
            {
                if (a[i] != b[i])
                {
                    last = i;
                    break;
                }
            }

            if (first == -1 && last == -1)
                result = attempted;
            else
            {
                var sb = new StringBuilder();
                if (first == -1)
                    first = shorterLength;
                if (last == -1)
                    last = shorterLength;
                // If same letter repeats multiple times (ex: addition)
                // and error is on that letter, we have to trim trail.
                if (first + last > shorterLength)
                    last = shorterLength - first;

                if (first > 0)
                    sb.Append(attempted.Substring(0, first));

                sb.Append("[");

                if (last > -1 && last + first < attempted.Length)
                    sb.Append(attempted.Substring(first, attempted.Length - last - first));

                sb.Append("]");

                if (last > 0)
                    sb.Append(attempted.Substring(attempted.Length - last, last));

                result = sb.ToString();
            }
            //Assert.AreEqual(expectedResult, result);
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
