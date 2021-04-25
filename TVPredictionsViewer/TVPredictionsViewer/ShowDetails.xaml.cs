using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TV_Ratings_Predictions;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TVPredictionsViewer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ShowDetails : ContentView
    {
        public ContentView BreakdownView;
        //bool Window_Sizing = false;
        public bool isDesktop = false;

        public ShowDetails()
        {
            InitializeComponent();

            BreakdownTimer.Elapsed += BreakdownTimer_Elapsed;
        }

        

        public event EventHandler PanelOpened;

        private async void ShowPage_Clicked(object sender, EventArgs e)
        {
            var p = BindingContext as PredictionContainer;
            p.IsShowPage = true;
            var parent = Parent.Parent.Parent.Parent.Parent as Predictions;
            await parent.Navigation.PushModalAsync(new ShowDetailPage(p));
        }

        private async void IMDB_Clicked(object sender, EventArgs e)
        {
            var p = BindingContext as PredictionContainer;

            var uri = await NetworkDatabase.GetIMDBuri(p.show.Name);

            await Launcher.OpenAsync(uri);
        }

        private async void TMDB_Clicked(object sender, EventArgs e)
        {
            var p = BindingContext as PredictionContainer;
            var uri = NetworkDatabase.GetTMDBuri(p.show.Name);

            await Launcher.OpenAsync(uri);
        }

        private async void PBreakdown_Clicked(object sender, EventArgs e)
        {
            var p = BindingContext as PredictionContainer;

            var name = p.Name;

            var parent = Parent.Parent.Parent as Grid;
            var oldview = BreakdownView;
            if (BreakdownView != null)
            {
                await BreakdownView.FadeTo(0);
                BreakdownView = null;
            }

            BreakdownView = new PredictionBreakdown(p.show, p.network)
            {
                Opacity = 0,
                BackgroundColor = Content.BackgroundColor
            };            

            parent.Children.Add(BreakdownView);

            BreakdownView.Padding = p.IsShowPage ? new Thickness(0, 50, 0, 0) : 0;

            if (isDesktop) Grid.SetColumn(BreakdownView, 1);
            PanelOpened?.Invoke(this, new EventArgs());
            await BreakdownView.FadeTo(1);
            if (oldview != null) parent.Children.Remove(oldview);
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            if (!(BreakdownView is null))
            {
                var parent = Parent.Parent.Parent as Grid;
                parent.Children.Remove(BreakdownView);
                BreakdownView = null;
            }                
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (BreakdownView != null && Grid.GetColumn(BreakdownView) == 1) width *= 2;

            isDesktop = width > (960);

            if (BreakdownView != null && ((Grid.GetColumn(BreakdownView) == 1 && !isDesktop) || (Grid.GetColumn(BreakdownView) == 0 && isDesktop)))
            {
                BreakdownTimer.Stop();
                BreakdownTimer.Start();
            }
        }

        Timer BreakdownTimer = new Timer(100) { AutoReset = false };
        private async void BreakdownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Device.InvokeOnMainThreadAsync(() =>
            {
                if (!isDesktop)
                    Grid.SetColumn(BreakdownView, 0);
                else
                    Grid.SetColumn(BreakdownView, 1);

                PanelOpened?.Invoke(this, new EventArgs());

            });
        }

        private async void RBreakdown_Clicked(object sender, EventArgs e)
        {
            
                

            var p = BindingContext as PredictionContainer;
            var name = p.Name;

            var parent = Parent.Parent.Parent as Grid;
            var oldview = BreakdownView;
            if (BreakdownView != null)
            {
                await BreakdownView.FadeTo(0);;
                BreakdownView = null;
            }

            BreakdownView = new RatingsBreakdown(p.show, p.network)
            {
                Opacity = 0,
                BackgroundColor = Content.BackgroundColor
            };
            parent.Children.Add(BreakdownView);

            BreakdownView.Padding = p.IsShowPage ? new Thickness(0, 50, 0, 0) : 0;

            if (isDesktop) Grid.SetColumn(BreakdownView, 1);
            PanelOpened?.Invoke(this, new EventArgs());
            await BreakdownView.FadeTo(1);
            if (oldview != null) parent.Children.Remove(oldview);
            //}
        }
    }
}