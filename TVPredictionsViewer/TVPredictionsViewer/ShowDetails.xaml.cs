using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public ShowDetails()
        {
            InitializeComponent();
        }

        private async void ShowPage_Clicked(object sender, EventArgs e)
        {
            var p = BindingContext as PredictionContainer;
            p.IsShowPage = true;
            var parent = Parent.Parent.Parent.Parent.Parent as Predictions;
            await parent.Navigation.PushAsync(new ShowDetailPage(p));
        }

        private async void IMDB_Clicked(object sender, EventArgs e)
        {
            var p = BindingContext as PredictionContainer;

            var uri = await NetworkDatabase.GetIMDBuri(p.show.Name);

            await Launcher.OpenAsync(uri);
        }

        private async void TVDB_Clicked(object sender, EventArgs e)
        {
            var p = BindingContext as PredictionContainer;
            var uri = NetworkDatabase.GetTMDBuri(p.show.Name);

            await Launcher.OpenAsync(uri);
        }

        private async void PBreakdown_Clicked(object sender, EventArgs e)
        {
            var p = BindingContext as PredictionContainer;
            var name = p.Name;

            if (p.IsShowPage)
                //Navigation.PushAsync(new PredictionBreakdown(p.show, p.network) { BackgroundColor = Content.BackgroundColor });
                await Navigation.PushAsync(new ViewPage(new PredictionBreakdown(p.show, p.network), name) { BackgroundColor = Content.BackgroundColor });
            else
            {
                var parent = Parent.Parent.Parent as Grid;
                BreakdownView = new PredictionBreakdown(p.show, p.network)
                {
                    Opacity = 0,
                    BackgroundColor = Content.BackgroundColor
                };
                parent.Children.Add(BreakdownView);
                Grid.SetColumn(BreakdownView, 1);
                await BreakdownView.FadeTo(1);
            }
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

        private async void RBreakdown_Clicked(object sender, EventArgs e)
        {
            var p = BindingContext as PredictionContainer;
            var name = p.Name;

            if (p.IsShowPage)
                //Navigation.PushAsync(new PredictionBreakdown(p.show, p.network) { BackgroundColor = Content.BackgroundColor });
                await Navigation.PushAsync(new ViewPage(new RatingsBreakdown(p.show, p.network), name) { BackgroundColor = Content.BackgroundColor });
            else
            {
                var parent = Parent.Parent.Parent as Grid;
                BreakdownView = new RatingsBreakdown(p.show, p.network)
                {
                    Opacity = 0,
                    BackgroundColor = Content.BackgroundColor
                };
                parent.Children.Add(BreakdownView);
                Grid.SetColumn(BreakdownView, 1);
                await BreakdownView.FadeTo(1);
            }
        }
    }
}