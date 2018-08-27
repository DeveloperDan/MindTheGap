﻿
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
                SongsByTitleThenArtistGrid.ItemsSource = songsByTitleThenArtist;

                var songsByFileName = songList.OrderBy(sng => sng.FileName);

                //was: CalculateGapsToSameArtistAndTitle(songsByTitleThenArtist);
                CalculateGapsToSameArtistAndTitle(songsByFileName);

                var songsBySmallestArtistGap = songList.OrderBy(sng => sng.ArtistGapAhead).ThenBy(sng => sng.Title).Where(sng => sng.ArtistGapAhead > 0);
                SongsBySmallestArtistGapGrid.ItemsSource = songsBySmallestArtistGap;

                //var songsByTitleGap = songList.OrderBy(sng => sng.TitleGap);
                //GridTwo.ItemsSource = songsByTitleGap;

                var songsByArtistThenPosition = songList.OrderBy(sng => sng.Artist).ThenBy(sng => sng.Position);
                GridTwo.ItemsSource = songsByArtistThenPosition;


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

                var titleGap = nextTitleOccuranceIndex - (currentIndex + 1);
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
                var nextGenreOccuranceIndex = songListByFileName.FindIndex(currentIndex + 1, sng => sng.Genre == song.Genre);

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
            var nextArtistOccuranceIndex = songListByFileName.FindIndex(currentIndex + 1, sng => sng.Artist == song.Artist);

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

                    // Get the handful of songs having a matching title and titleGapAhead
                    var songsFittingTitleGapNeedsMinusFirstSongsSorted = allSongsListSorted.Where(sng => sng.TitleGapAhead >= titleGapRequested &&
                                                                                            sng.Title == selectedSong.Title).ToList().OrderBy(sng2 => sng2.FileName);  //orderby is critical                                                           

                    List<SongFileInfo> combinedRangesOfSongsQualifyingAsTitleInsertLocations = new List<SongFileInfo>();

                    foreach (var songFittingTitleGapNeeds in songsFittingTitleGapNeedsMinusFirstSongsSorted)
                    {
                        var rangeOfSongsQualifyingAsInsertLocations = allSongsListSorted.Where(sng => sng.Position > songFittingTitleGapNeeds.Position + titleGapRequested &&
                                                                                            sng.Position < songFittingTitleGapNeeds.NextOccuranceOfTitleIsAt - titleGapRequested);

                        combinedRangesOfSongsQualifyingAsTitleInsertLocations.AddRange(rangeOfSongsQualifyingAsInsertLocations);
                    }

                    // add a range of songs from the first song to the first of the selected title 
                    // (if gap need is satisfied and first song is not the selected title)
                    if (allSongsListSorted.FirstOrDefault().Title != selectedSong.Title)
                    {
                        var firstInstanceOfTitle = allSongsListSorted.FirstOrDefault(sng => sng.Title == selectedSong.Title);
                        var rangeOfSongsFromFirstFittingTitleGapNeeds = allSongsListSorted.Where(sng => sng.Position < firstInstanceOfTitle.Position - titleGapRequested);
                        combinedRangesOfSongsQualifyingAsTitleInsertLocations.AddRange(rangeOfSongsFromFirstFittingTitleGapNeeds);
                    }

                    var combinedRangesOfSongsQualifyingAsArtistAndTitleInsertLocationsSorted = combinedRangesOfSongsQualifyingAsTitleInsertLocations.OrderBy(sng => sng.FileName).ToList();

                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

                    // Get the handful of songs having a matching artist and artistGapAhead
                    var songsFittingArtistGapNeedsMinusFirstSongsSorted = combinedRangesOfSongsQualifyingAsArtistAndTitleInsertLocationsSorted.Where(sng => sng.ArtistGapAhead >= artistGapRequested &&
                                                                        sng.Artist == selectedSong.Artist).OrderBy(sng2 => sng2.FileName);  //orderby is critical

                    List<SongFileInfo> combinedRangesOfSongsQualifyingAsArtistAndTitleInsertLocations = new List<SongFileInfo>();

                    foreach (var songFittingArtistGapNeeds in songsFittingArtistGapNeedsMinusFirstSongsSorted)
                    {
                        var rangeOfSongsQualifyingAsInsertLocations = combinedRangesOfSongsQualifyingAsArtistAndTitleInsertLocationsSorted.Where(sng => sng.Position > songFittingArtistGapNeeds.Position + artistGapRequested &&
                                                                                            sng.Position < songFittingArtistGapNeeds.NextOccuranceOfArtistIsAt - artistGapRequested);

                        combinedRangesOfSongsQualifyingAsArtistAndTitleInsertLocations.AddRange(rangeOfSongsQualifyingAsInsertLocations);
                    }

                    // add a range of songs from the first song to the first of the selected artist 
                    // (if gap need is satisfied and first song is not the selected artist)
                    if (combinedRangesOfSongsQualifyingAsArtistAndTitleInsertLocationsSorted.FirstOrDefault().Artist != selectedSong.Artist)
                    {
                        var firstInstanceOfArtist = combinedRangesOfSongsQualifyingAsArtistAndTitleInsertLocationsSorted.FirstOrDefault(sng => sng.Artist == selectedSong.Artist);
                        var rangeOfSongsCombinedFromFirstFittingArtistGapNeeds = combinedRangesOfSongsQualifyingAsArtistAndTitleInsertLocationsSorted.Where(sng => sng.Position < firstInstanceOfArtist.Position - artistGapRequested);
                        combinedRangesOfSongsQualifyingAsArtistAndTitleInsertLocations.AddRange(rangeOfSongsCombinedFromFirstFittingArtistGapNeeds);
                    }

                    var combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocationsSorted = combinedRangesOfSongsQualifyingAsArtistAndTitleInsertLocations.OrderBy(sng => sng.FileName).ToList();

                    //++++++++++++++++++++++++++++++++++++++++++++++++


















                    // Get the handful of songs having a matching genre and genreGapAhead
                    var songsFittingGenreGapNeedsMinusFirstSongsSorted = combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocationsSorted.Where(sng => sng.GenreGapAhead >= genreGapRequested &&
                                                                        sng.Genre == selectedSong.Genre).OrderBy(sng2 => sng2.FileName);  //orderby is critical

                    List<SongFileInfo> combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocations = new List<SongFileInfo>();

                    foreach (var songFittingGenreGapNeeds in songsFittingGenreGapNeedsMinusFirstSongsSorted)
                    {
                        //var rangeOfSongsQualifyingAsInsertLocations = combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocationsSorted.Where(sng => sng.Position > songFittingGenreGapNeeds.Position + genreGapRequested &&
                        //                                                                    sng.Position < songFittingGenreGapNeeds.NextOccuranceOfGenreIsAt - genreGapRequested);

                        //IMPORTANT: Genre gap needs differ from artist and title gap needs!!!
                        //For Genre gap we want any range with a gap greater than the requested genre gap
                        // (we do NOT add and subtract the genreGapRequested from either end when searching for the range)
                        // Get songs between (and not including) the current song location and the next occurance of the genre location
                        // but only get this range if the GenreGapAhead is greater than the genreGapRequested
                        var rangeOfSongsQualifyingAsInsertLocations = combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocationsSorted.Where(sng => sng.Position > songFittingGenreGapNeeds.Position && sng.Position < songFittingGenreGapNeeds.NextOccuranceOfGenreIsAt && songFittingGenreGapNeeds.GenreGapAhead >= genreGapRequested);

                        combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocations.AddRange(rangeOfSongsQualifyingAsInsertLocations);
                    }

                    // add a range of songs from the first song to the first of the selected genre 
                    // (if gap need is satisfied and first song is not the selected genre)
                    if (combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocationsSorted.FirstOrDefault().Genre != selectedSong.Genre)
                    {
                        var firstInstanceOfGenre = combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocationsSorted.FirstOrDefault(sng => sng.Genre == selectedSong.Genre);
                        if (firstInstanceOfGenre != null)
                        {
                            var rangeOfSongsCombinedFromFirstFittingGenreGapNeeds = combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocationsSorted.Where(sng => sng.Position < firstInstanceOfGenre.Position - genreGapRequested);
                            combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocations.AddRange(rangeOfSongsCombinedFromFirstFittingGenreGapNeeds);
                        }
                    }

                    combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocationsSorted = combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocations.OrderBy(sng => sng.FileName).ToList();

                    //++++++++++++++++++++++++++++++++++++++++++++++++

                    string msg = string.Empty;

                    foreach (var songFittingArtistTitleAndGenreNeeds in combinedRangesOfSongsQualifyingAsArtistAndTitleAndGenreInsertLocationsSorted)
                    {
                        msg += songFittingArtistTitleAndGenreNeeds.FileName + ", pos: " + songFittingArtistTitleAndGenreNeeds.Position + ", genre gap: " + songFittingArtistTitleAndGenreNeeds.GenreGapAhead + " (other gaps meaningless from this song) " + Environment.NewLine;  //, artistGapAhead: " + songFittingArtistTitleAndGenreNeeds.ArtistGapAhead + ", titleGapAhead: " + songFittingArtistTitleAndGenreNeeds.TitleGapAhead + ", GenreGapAhead: " + songFittingArtistTitleAndGenreNeeds.GenreGapAhead + Environment.NewLine;
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
                        System.Windows.MessageBox.Show("List too long to display all: " + msg.Substring(0, 1999));
                    }
                }

            }
            catch (Exception)
            {
                throw;
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
                                                                sng.Genre == selectedSong.Genre).OrderBy(sng2 => sng2.FileName);  //orderby is critical

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

                            if (thisSongFromAllSong.Artist == selectedSong.Artist)
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
                        System.Windows.MessageBox.Show("List too long to display all: " + msg.Substring(0, 1999));
                    }
                }

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
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
