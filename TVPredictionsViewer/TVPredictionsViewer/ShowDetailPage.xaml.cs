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
        Timer DelayedScroll = new Timer(1000), CheckVisible = new Timer(100);
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

            if (Device.RuntimePlatform == Device.UWP)
                Back.IsVisible = NetworkDatabase.mainpage.home.Navigation.NavigationStack.Count == 1;

            BindableLayout.SetItemsSource(OptionsMenuHidden, MenuItems);
            OptionsMenu.ItemsSource = MenuItems;

            OptionsMenuHidden.SizeChanged += OptionsMenuHidden_SizeChanged;

            Bar.Title = p.Name;

            SearchResults.NavigationParent = this;


            ShowImage.Source = null;
            SideColumn.SizeChanged += SideColumn_SizeChanged;
            SidePanel.PanelOpened += SidePanel_PanelOpened;

            LoadImage(p);

            if (RemoveStackAfterLoad)
                Appearing += ShowDetailPage_Appearing;
        }

        private async void CheckVisible_Elapsed(object sender, ElapsedEventArgs e)
        {
            var Visibility = DetailScroll.ScrollY + DetailScroll.Height;
            if (Visibility < ShowImage.Height && !ScrollDown.IsVisible)
                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    ScrollDown.Opacity = 0;
                    ScrollDown.IsVisible = true;
                    await ScrollDown.FadeTo(1);
                });  
            else if (Visibility > ShowImage.Height && ScrollDown.IsVisible)
                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    await ScrollDown.FadeTo(0);
                    ScrollDown.IsVisible = false;
                });
        }

        private void OptionsMenuHidden_SizeChanged(object sender, EventArgs e)
        {
            var w = OptionsMenuHidden.Width;
            var h = OptionsMenuHidden.Height;

            if (w > 0 && h > 0)
            {
                OptionsMenu.WidthRequest = w;
                OptionsMenu.HeightRequest = h;
                OptionsMenuHidden.IsVisible = false;

                OptionsMenu.IsVisible = true;

                OptionsMenuHidden.SizeChanged -= OptionsMenuHidden_SizeChanged;
            }
            
        }

        private void ShowDetailPage_Appearing(object sender, EventArgs e)
        {
            var stack = NetworkDatabase.mainpage.Detail.Navigation.NavigationStack;
            var count = stack.Count;
            for (int i = count - 2; i > 0; i--)
                if (stack[i] is NavigationPage) Navigation.RemovePage(stack[i]);
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

            if (SidePanel.isDesktop && SidePanel.BreakdownView != null && Grid.GetColumn(SidePanel.BreakdownView) == 1) width /= 2;

            if (SideColumn.Width > 5)
                ImageRow.Height = width * 9 / 16;

            ScrollTo();
                
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

            DelayedScroll.Elapsed += DelayedScroll_Elapsed;
            DelayedScroll.AutoReset = false;
            ScrollTo();

            CheckVisible.Elapsed += CheckVisible_Elapsed;
        }

        void ScrollTo()
        {
            DelayedScroll.Stop();            
            DelayedScroll.Start();
        }

        private async void DelayedScroll_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Device.InvokeOnMainThreadAsync(async () => await DetailScroll.ScrollToAsync(SidePanel, ScrollToPosition.Start, true));

            CheckVisible.Start();
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

        private async void Options_Clicked(object sender, EventArgs e)
        {
            OptionsScreen.Opacity = 0;
            OptionsScreen.IsVisible = true;
            await OptionsScreen.FadeTo(1);
        }

        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            //await OptionsScreen.FadeTo(0);
            OptionsScreen.IsVisible = false;
        }

        private async void DownButton_Clicked(object sender, EventArgs e)
        {
            await DetailScroll.ScrollToAsync(SidePanel, ScrollToPosition.Start, true);
        }

        //private void Hamburger_Clicked(object sender, EventArgs e)
        //{
        //    var FlyoutTimer = new Timer(15);
        //    var StartTime = DateTime.Now;
        //    var Opening = FlyoutOffset == 0;
        //    FlyoutTimer.Start();

        //    FlyoutTimer.Elapsed += async (se, ee) =>
        //     {
        //         var TimePassed = (DateTime.Now - StartTime).TotalSeconds;
        //         FlyoutOffset = Opening ? (TimePassed / 0.5) : Math.Max(1 - TimePassed / 0.5, 0);
        //         FlyOutPosition.Constant = -FlyoutOffset * this.Width;
        //         await Device.InvokeOnMainThreadAsync(() => OnPropertyChanged("FlyoutPosition"));

        //         if (TimePassed >= 0.5)
        //             FlyoutTimer.Stop();
        //     };

        //}
    }

    class ShowDetailMenuItem
    {
        ToolbarItem OriginalItem;
        public string Text { get { return OriginalItem.Text; } }
        public ICommand Command { get { return OriginalItem.Command; } }

        public ShowDetailMenuItem(ToolbarItem item) => OriginalItem = item;
    }
}