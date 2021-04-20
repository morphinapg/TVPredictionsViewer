using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using TV_Ratings_Predictions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TVPredictionsViewer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShowDetailPage : ContentPage, INotifyPropertyChanged
    {
        
        PredictionContainer show;
        Timer DelayedScroll;
        string _uri;
        public string ShowImageUri
        {
            get { return _uri; }
            set 
            { 
                _uri = value;
                OnPropertyChanged("ShowImageUri");
            }
        }

        //ObservableCollection<ListOfPredictions> Results = new ObservableCollection<ListOfPredictions>();
        public TitleTemplate TitleBar
        {
            get
            {
                return Bar;
            }
        }

        public ShowDetailPage(PredictionContainer p, bool RemoveStackAfterLoad = false)
        {
            var network = p.network;
            var MenuItems = new ObservableCollection<ShowDetailMenuItem>();
            

            if (p.UseNetwork)
                foreach (ToolbarItem t in new Toolbar(this, network, p).ToolBarItems)
                    MenuItems.Add(new ShowDetailMenuItem(t));
            else
                foreach (ToolbarItem t in new Toolbar(this, p).ToolBarItems)
                    MenuItems.Add(new ShowDetailMenuItem(t));

            BindingContext = p;
            show = p;
            InitializeComponent();
            BindableLayout.SetItemsSource(OptionsMenu, MenuItems);

            Bar.Title = p.Name;

            SearchResults.NavigationParent = this;


            ShowImage.Source = null;
            SideColumn.SizeChanged += SideColumn_SizeChanged;
            SidePanel.PanelOpened += SidePanel_PanelOpened;

            LoadImage(p);

            if (RemoveStackAfterLoad)
                Appearing += ShowDetailPage_Appearing;
        }

        

        private void ShowDetailPage_Appearing(object sender, EventArgs e)
        {
            var stack = Navigation.NavigationStack;
            var count = stack.Count;
            for (int i = count - 2; i > 0; i--)
                Navigation.RemovePage(stack[i]);
        }

        public T FindTemplateElementByName<T>(Page page, string name) where T : Element
        {
            if (!(page is IPageController pc))
            {
                return null;
            }

            foreach (var child in pc.InternalChildren)
            {
                var result = child.FindByName<T>(name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void SideColumn_SizeChanged(object sender, EventArgs e)
        {
            var width = SideColumn.Width;

            if (SidePanel.isDesktop && SidePanel.BreakdownView != null) width /= 2;

            if (SideColumn.Width > 5)
                ImageRow.Height = width * 9 / 16;
                
        }

        private void SidePanel_PanelOpened(object sender, EventArgs e)
        {
            SideColumn_SizeChanged(this, new EventArgs());
        }

        async void LoadImage(PredictionContainer p)
        {

            //bool reload = false;

            //if (!NetworkDatabase.ShowIDs.ContainsKey(p.show.Name) && Application.Current.Properties.ContainsKey("SHOWID " + p.show.Name))
            //    reload = true;

            var ID = await NetworkDatabase.GetShowID(p.show.Name, p.network.name);

            if (ID > 0)
            {
                if (Device.RuntimePlatform == Device.UWP)
                {
                    var uri = await NetworkDatabase.GetImageURI(ID);
                    ShowImageUri = uri.AbsoluteUri;

                    ShowImage.BindingContext = this;
                    ShowImage.SetBinding(ImageEffect.TextProperty, new Binding("ShowImageUri"));

                    p.IsLoaded = true;
                    ShowImage.IsVisible = true;
                    //ImageLoading.IsVisible = false;
                }
                else
                {
                    ShowImage.Source = new UriImageSource
                    {
                        Uri = await NetworkDatabase.GetImageURI(ID),
                        CachingEnabled = true,
                        CacheValidity = new TimeSpan(90, 0, 0, 0)
                    };
                    p.Overview = NetworkDatabase.ShowDescriptions[ID];

                    if (ShowImage.Source != null)
                    {
                        p.IsLoaded = true;
                        ShowImage.IsVisible = true;
                        ImageLoading.IsVisible = false;
                    }
                }
            }

            //if (reload)
            //{
            //    ID = await NetworkDatabase.GetShowID(p.show.Name, p.network.name, true);

            //    if (ID > 0)
            //    {
            //        ShowImage.Source = new UriImageSource
            //        {
            //            Uri = await NetworkDatabase.GetImageURI(ID),
            //            CachingEnabled = true,
            //            CacheValidity = new TimeSpan(90, 0, 0, 0)
            //        };

            //        p.Overview = NetworkDatabase.ShowDescriptions[ID];
            //    }
            //}

            var TMDBText = "This product uses the TMDb API but is not endorsed or certified by TMDb.";
            var Formatted = new FormattedString();
            Formatted.Spans.Add(new Span { Text = TMDBText });

            if (NetworkDatabase.TMDBerror)
                Formatted.Spans.Add(new Span()
                {
                    Text = "Error connecting to TMDB! Some show details and/or images may temporarily be unavailable.",
                    TextColor = Color.DarkRed
                });

            TMDBNotice.FormattedText = Formatted;

            ScrollTo();
        }

        void ScrollTo()
        {
            DelayedScroll = new Timer(1000);
            DelayedScroll.Elapsed += DelayedScroll_Elapsed;
            DelayedScroll.AutoReset = false;
            DelayedScroll.Start();
        }

        private async void DelayedScroll_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Device.InvokeOnMainThreadAsync(async () => await DetailScroll.ScrollToAsync(SidePanel, ScrollToPosition.Start, true));
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            show.IsShowPage = false;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (show != null)
                show.IsShowPage = true;
        }

        protected override bool OnBackButtonPressed()
        {
            if (SearchResults.IsFocused)
            {
                SearchResults.Unfocus();
                return true;
            }
            else if (SearchResults.IsVisible)
            {
                SearchResults.MakeInvisible();
                return true;
            }
            else if (SidePanel.BreakdownView != null && SidePanel.BreakdownView.Opacity > 0)
            {
                FadeOut();
                ScrollTo();
                return true;
            }
            else
                return base.OnBackButtonPressed();
                
        }

        async void FadeOut()
        {
            await SidePanel.BreakdownView.FadeTo(0);
            SideColumn.Children.Remove(SidePanel.BreakdownView);
            SidePanel.BreakdownView = null;
            SidePanel_PanelOpened(this, new EventArgs());
        }

        private void Back_Clicked(object sender, EventArgs e)
        {
            _ = OnBackButtonPressed();
        }
    }

    class ShowDetailMenuItem
    {
        ToolbarItem OriginalItem;
        public string Text { get { return OriginalItem.Text; } }
        public ICommand Command { get { return OriginalItem.Command; } }

        public ShowDetailMenuItem(ToolbarItem item) => OriginalItem = item;
    }
}