using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TVPredictionsViewer;
using Xamarin.Forms;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using TMDbLib.Client;
using Xamarin.Essentials;

namespace TV_Ratings_Predictions
{
    public static class NetworkDatabase
    {
        public static ObservableCollection<MiniNetwork> NetworkList;
        public static int MaxYear;
        public static string Folder = "Not Loaded";
        public static List<Year> YearList = new List<Year>();
        public static string currentText = "";
        public static TMDbClient Client;
        public static bool backup = false, TMDBerror = false;
        public static MainPage mainpage;
        public static bool IsLoaded = false;
        public static bool CurrentlyLoading = false;
        public static bool InBackground = false;

        public static Dictionary<int, string>
            ShowDescriptions = new Dictionary<int, string>(),
            IMDBList = new Dictionary<int, string>(),
            //ShowSlugs = new Dictionary<int, string>(),
            ShowImages = new Dictionary<int, string>();

        public static Dictionary<string, int> ShowIDs = new Dictionary<string, int>();
        //public static DateTime ApiTime;

        static int _currentyear;
        public static int CurrentYear
        {
            get
            {
                return _currentyear;
            }
            set
            {
                if (_currentyear != value && value > -1)
                {
                    _currentyear = value;
                    foreach (MiniNetwork n in NetworkList)
                        n.pendingFilter = true;

                    CurrentYearUpdated?.Invoke(_currentyear, new EventArgs());
                }
            }
        }

        public static bool HomeButton
        {
            get
            {
                if (Application.Current.Properties.ContainsKey("HomeButton"))
                    return (bool)Application.Current.Properties["HomeButton"];
                else
                    return true;
            }
            set
            {
                Application.Current.Properties["HomeButton"] = value;
                HomeButtonChanged(new object(), new EventArgs());
            }
        }

        public static event EventHandler HomeButtonChanged;

        public static bool UseOdds = false;

        public static event EventHandler CurrentYearUpdated;
        public static void ReadSettings(MainPage page)
        {
            var predictions = "https://github.com/morphinapg/TVPredictions/raw/master/Predictions.TVP";                

            Directory.CreateDirectory(Folder);

            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                try
                {
                    var webClient = new WebClient();
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(page.CompletedSettings);
                    string pathToNewFile = Path.Combine(Folder, Path.GetFileName(predictions));
                    webClient.DownloadFileAsync(new Uri(predictions), pathToNewFile);
                    webClient.Dispose();
                }
                catch (Exception ex)
                {
                    ex = new Exception("Error while downloading predictions");
                    var e = new AsyncCompletedEventArgs(ex, false, page);

                    page.CompletedSettings(page, e);
                }
            }
            else
            {
                var ex = new Exception("Not Connected to the Internet! Try again when connected.");
                var e = new AsyncCompletedEventArgs(ex, false, page);

                page.CompletedSettings(page, e);
            }            
        }

        public static void SetMainPage(MainPage page)
        {
            mainpage = page;
        }

        public static async Task ReadUpdateAsync()
        {
            if (!NetworkDatabase.CurrentlyLoading)
            {
                NetworkDatabase.CurrentlyLoading = true;

                var update = "https://github.com/morphinapg/TVPredictions/raw/master/Update.txt";

                var FilePath = Path.Combine(Folder, "Update.txt");

                Directory.CreateDirectory(Folder);

                if (File.Exists(FilePath))
                    using (var fs = new FileStream(FilePath, FileMode.Open))
                    {
                        using (var reader = new StreamReader(fs))
                        {
                            currentText = await reader.ReadToEndAsync();
                        }
                    }

                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    try
                    {
                        var webClient = new WebClient();
                        webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(mainpage.CompletedUpdateAsync);
                        webClient.DownloadFileAsync(new Uri(update), FilePath);
                        webClient.Dispose();
                    }
                    catch (Exception ex)
                    {
                        ex = new Exception("Error while downloading predictions");
                        var e = new AsyncCompletedEventArgs(ex, false, mainpage);

                        mainpage.CompletedUpdateAsync(mainpage, e);
                    }

                    await AuthenticateTMDB();
                }
                else
                {
                    var ex = new Exception("Not Connected to the Internet! Try again when connected.");

                    var e = new AsyncCompletedEventArgs(ex, false, mainpage);

                    mainpage.CompletedUpdateAsync(mainpage, e);
                }
            }
        }

        public static async Task AuthenticateTMDB()
        {
            //if (Application.Current.Properties.ContainsKey("TMDB"))
                //Application.Current.Properties.Remove("TMDB");

            if (!Application.Current.Properties.ContainsKey("TMDB"))
            {
                foreach (string ShowName in ShowIDs.Keys)
                {
                    string key = "SHOWID " + ShowName;

                    if (Application.Current.Properties.ContainsKey(key))
                        Application.Current.Properties.Remove(key);
                }

                ShowIDs = new Dictionary<string, int>();
                //ShowSlugs = new Dictionary<int, string>();
                ShowDescriptions = new Dictionary<int, string>();
                ShowImages = new Dictionary<int, string>();
                IMDBList = new Dictionary<int, string>();

                if (File.Exists(Path.Combine(Folder, "backup")))
                    File.Delete(Path.Combine(Folder, "backup"));

                Application.Current.Properties["TMDB"] = "Initialized";
            }

            try
            {
                TMDBerror = false;

                await Task.Run(() => Client = new TMDbClient("cfa9429b40f59417c6b45db8adef0b15"));
                
                //await client.Authentication.AuthenticateAsync("GU08T4COWROF8RQC");
            }
            catch (Exception)
            {
                TMDBerror = true;
            }
            //finally
            //{
            //    ApiTime = DateTime.Now;
            //}   
        }
        
        public static async Task<int> GetShowID(string name, string network, bool ForceDownload = false)
        {
            if (ShowIDs.ContainsKey(name))
                return ShowIDs[name];
            else
            {
                try
                {
                    if (TMDBerror)
                        await AuthenticateTMDB();

                    var key = "SHOWID " + name;

                    if (Application.Current.Properties.ContainsKey(key) && !ForceDownload)
                    {
                        await ReadBackup(0, name);

                        if (ShowIDs.ContainsKey(name))
                        {
                            var id = ShowIDs[name];
                            if (ShowDescriptions.ContainsKey(id) && ShowImages.ContainsKey(id))
                                return id;
                        }
                    }

                    var result = await GetSearchResults(name); 

                    var data = result.Where(x => !String.IsNullOrEmpty(x.SeriesName)).ToList();
                    var networkdata = data.Where(x => x.Networks.Count > 0).ToList();

                    var showResults = new List<TVSearchResult>();
                    

                    if (Application.Current.Properties.ContainsKey(key))
                        showResults = data.Where(x => x.Id == (int)Application.Current.Properties[key]).ToList();

                    if (showResults.Count == 0)
                        showResults = networkdata.Where(x => (x.SeriesName == name || x.SeriesName.Contains(name + " (")) && x.Networks.Contains(network)).ToList();

                    if (showResults.Count == 0)
                        showResults = data.Where(x => x.SeriesName == name || x.SeriesName.Contains(name + " (")).ToList();
                    
                    if (showResults.Count == 0)
                        showResults = networkdata.Where(x => (x.SeriesName == name || x.SeriesName.Contains(name + " (") || x.Aliases.Contains(name)) && x.Networks.Contains(network)).ToList();
                        
                    if (showResults.Count == 0)
                        showResults = data.Where(x => x.SeriesName == name || x.SeriesName.Contains(name + " (") || x.Aliases.Contains(name)).ToList();                        

                    if (showResults.Count == 0)
                        showResults = networkdata.Where(x => (x.SeriesName.Contains(name) || x.Aliases.Contains(name)) && x.Networks.Contains(network)).ToList();                        

                    if (showResults.Count == 0)
                        showResults = data.Where(x => x.SeriesName.Contains(name) || x.Aliases.Contains(name)).ToList();

                    if (showResults.Count == 0)
                        showResults = networkdata.Where(x => x.Networks.Contains(network)).ToList();
                        
                    if (showResults.Count == 0)
                        showResults = data.ToList();

                    //foreach (TVSearchResult s in showResults)
                    //{
                    //    try
                    //    {
                    //        if (s.FirstAired == "")
                    //        {
                    //            var episodes = await client.Series.GetEpisodesAsync(s.Id, 1);
                    //            s.FirstAired = episodes.Data.First().FirstAired;
                    //        }
                    //    }
                    //    catch (Exception)
                    //    {
                    //        //do nothing
                    //    }
                    //}

                    showResults = showResults.OrderByDescending(x => x.Show.FirstAirDate).ToList();

                    int ID = showResults.First().Id;
                    
                    ShowDescriptions[ID] = showResults.First().Show.Overview;
                    //ShowSlugs[ID] = showResults.First().Slug;

                    //TvDbSharper.Dto.TvDbResponse<TvDbSharper.Dto.Image[]> imgs;

                    //try
                    //{
                    //    imgs = await client.Series.GetImagesAsync(ID, new TvDbSharper.Dto.ImagesQuery() { KeyType = TvDbSharper.Dto.KeyType.Series });
                    //}
                    //catch (Exception)
                    //{
                    //    imgs = new TvDbSharper.Dto.TvDbResponse<TvDbSharper.Dto.Image[]>();
                    //}

                    //if (imgs.Data is null)
                    //    ShowImages[ID] = null;
                    //else
                    //    ShowImages[ID] = imgs.Data.First().FileName;

                    ShowImages[ID] = showResults.First().Show.BackdropPath;

                    IMDBList[ID] = "";

                    Application.Current.Properties[key] = ID;
                    ShowIDs[name] = ID;

                    backup = true;
                    return ID;
                }
                catch (Exception)
                {
                    TMDBerror = true;
                    if (!ShowIDs.ContainsKey(name))
                        await ReadBackup(0, name);

                    if (ShowIDs.ContainsKey(name))
                        return ShowIDs[name];

                    return 0;
                }

            }
        }

        //public static async Task<TvDbSharper.Dto.TvDbResponse<TvDbSharper.Dto.SeriesSearchResult[]>> GetSearchResults(string name)
        //{
        //    name = Regex.Replace(name, @"[^\u0000-\u007F]+", " ");
        //    return await client.Search.SearchSeriesByNameAsync(name);
        //}

        public static async Task<List<TVSearchResult>> GetSearchResults(string name)
        {
            name = Regex.Replace(name, @"[^\u0000-\u007F]+", " ");
            var result = await Client.SearchTvShowAsync(name);

            var DetailedResults = new List<TVSearchResult>();
            foreach (TMDbLib.Objects.Search.SearchTv show in result.Results)
            {
                var ShowDetails = await Client.GetTvShowAsync(show.Id, TMDbLib.Objects.TvShows.TvShowMethods.AlternativeTitles);

                DetailedResults.Add(new TVSearchResult(ShowDetails.Name, ShowDetails.Id, ShowDetails.Networks.Select(x => x.Name).ToList(), ShowDetails.AlternativeTitles.Results.Select(x => x.Title).ToList(), ShowDetails));
            }

            return DetailedResults;
        }

        public static async Task<Uri> GetImageURI(int ID)
        {
            if (!ShowImages.ContainsKey(ID))
                await ReadBackup(ID, "");

            try
            {
                return new Uri("https://www.themoviedb.org/t/p/original/" + ShowImages[ID]);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<Uri> GetIMDBuri(string name)
        {
            if (!ShowIDs.ContainsKey(name))
                await ReadBackup(0, name);

            if (!ShowIDs.ContainsKey(name))
            {
                name = name.Replace(" ", "+");
                return new Uri("https://www.imdb.com/find?s=all&q=" + name);
            }
                

            var ID = ShowIDs[name];

            if (string.IsNullOrWhiteSpace(IMDBList[ID]) && Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                try
                {
                    //if (DateTime.Now - ApiTime > TimeSpan.FromHours(24))
                    //    await RefreshTVDB();

                    if (TMDBerror)
                        await AuthenticateTMDB();

                    var result = await Client.GetTvShowExternalIdsAsync(ID);
                    IMDBList[ID] = result.ImdbId;
                }
                catch (Exception)
                {
                    TMDBerror = true;
                    name = name.Replace(" ", "+");
                    return new Uri("https://www.imdb.com/find?s=all&q=" + name);
                }                
            }

            return new Uri(Path.Combine("https://www.imdb.com/title/", IMDBList[ID]));
        }

        public static Uri GetTMDBuri(string name)
        {
            if (!ShowIDs.ContainsKey(name))
            {
                name = name.Replace(" ", "+");
                return new Uri("https://www.themoviedb.org/search?query=" + name);
            }

            var index = ShowIDs[name];

            return new Uri(Path.Combine("https://www.themoviedb.org/tv/", index.ToString()));
        }

        public static async Task SaveBackup()
        {
            await Task.Run(() =>
            {
                var NewBackup = new BackupData
                {
                    ShowIDs = new Dictionary<string, int>(),
                    ShowDescriptions = new Dictionary<int, string>(),
                    IMDBList = new Dictionary<int, string>(),
                    ShowImages = new Dictionary<int, string>()
                };

                ShowIDs.Where(x => ShowDescriptions.ContainsKey(x.Value) && IMDBList.ContainsKey(x.Value) && ShowImages.ContainsKey(x.Value)).ToList().ForEach(x =>
                {
                    NewBackup.ShowIDs[x.Key] = x.Value;
                    NewBackup.ShowDescriptions[x.Value] = ShowDescriptions[x.Value];
                    NewBackup.IMDBList[x.Value] = IMDBList[x.Value];
                    NewBackup.ShowImages[x.Value] = ShowImages[x.Value];
                });

                if (File.Exists(Path.Combine(Folder, "backup")))
                {
                    try
                    {
                        var serializer = new DataContractSerializer(typeof(BackupData));
                        BackupData OldBackup;

                        using (var fs = new FileStream(Path.Combine(Folder, "backup"), FileMode.Open))
                        {
                            OldBackup = serializer.ReadObject(fs) as BackupData;
                        }

                        OldBackup.ShowIDs.Where(x => !NewBackup.ShowIDs.ContainsKey(x.Key) && OldBackup.ShowDescriptions.ContainsKey(x.Value) && OldBackup.IMDBList.ContainsKey(x.Value) && OldBackup.ShowImages.ContainsKey(x.Value)).ToList().ForEach(x =>
                        {
                            NewBackup.ShowIDs[x.Key] = x.Value;
                            NewBackup.ShowDescriptions[x.Value] = OldBackup.ShowDescriptions[x.Value];
                            NewBackup.IMDBList[x.Value] = OldBackup.IMDBList[x.Value];
                            NewBackup.ShowImages[x.Value] = OldBackup.ShowImages[x.Value];
                        });
                    }
                    catch (Exception)
                    {
                        //Error reading backup, cancel
                    }
                }

                try
                {
                    using (var fs = new FileStream(Path.Combine(Folder, "backup"), FileMode.Create))
                    {
                        var serializer = new DataContractSerializer(typeof(BackupData));
                        serializer.WriteObject(fs, NewBackup);
                    }
                }
                catch (Exception)
                {
                    //File is in use, do not back up
                }
            });

            backup = false;
        }

        public static async Task ReadBackup(int ID, string name)
        {
            await Task.Run(() =>
            {
                if (File.Exists(Path.Combine(Folder, "backup")))
                {
                    try
                    {
                        var serializer = new DataContractSerializer(typeof(BackupData));
                        BackupData OldBackup;

                        using (var fs = new FileStream(Path.Combine(Folder, "backup"), FileMode.Open))
                        {
                            OldBackup = serializer.ReadObject(fs) as BackupData;
                        }

                        //Append any missing OldBackup data
                        OldBackup.ShowIDs.Where(x => (x.Key == name || x.Value == ID) && !ShowIDs.ContainsKey(x.Key) && OldBackup.ShowDescriptions.ContainsKey(x.Value) && OldBackup.IMDBList.ContainsKey(x.Value) && OldBackup.ShowImages.ContainsKey(x.Value)).ToList().ForEach(x =>
                        {
                            ShowIDs[x.Key] = x.Value;
                            ShowDescriptions[x.Value] = OldBackup.ShowDescriptions[x.Value];
                            IMDBList[x.Value] = OldBackup.IMDBList[x.Value];
                            ShowImages[x.Value] = OldBackup.ShowImages[x.Value];
                        });
                    }
                    catch (Exception)
                    {
                        //backup is corrupted, do not read
                    }
                }
            });
        }
    }    
}
