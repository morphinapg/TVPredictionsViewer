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
        //bool Window_Sizing = false;
        bool isDesktop = false;

        public ShowDetails()
        {
            InitializeComponent();
        }

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

            //if (p.IsShowPage)
            //Navigation.PushAsync(new PredictionBreakdown(p.show, p.network) { BackgroundColor = Content.BackgroundColor });
            //await Navigation.PushAsync(new ViewPage(new PredictionBreakdown(p.show, p.network), name) { BackgroundColor = Content.BackgroundColor });
            //{
            //}
            //else
            //{
            var parent = Parent.Parent.Parent as Grid;

            BreakdownView = new PredictionBreakdown(p.show, p.network)
            {
                Opacity = 0,
                BackgroundColor = Content.BackgroundColor
            };            

            parent.Children.Add(BreakdownView);

            BreakdownView.Padding = p.IsShowPage ? new Thickness(0, 50, 0, 0) : 0;

            if (isDesktop) Grid.SetColumn(BreakdownView, 1);

            await BreakdownView.FadeTo(1);
            //}
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
            var p = BindingContext as PredictionContainer;

            //Window_Sizing = true;

            if (BreakdownView != null && BreakdownView.Opacity > 0 && Grid.GetColumn(BreakdownView) == 1) width *= 2;

            var tmpDesktop = isDesktop;
            isDesktop = width > (960);

            //if (isDesktop != tmpDesktop)
            //{
            //    if (isDesktop && BreakdownView != null && BreakdownView.Opacity > 0)
            //    {
            //        //FirstColumn.Width = new GridLength(1, GridUnitType.Star);
            //        //SecondColumn.Width = new GridLength(1, GridUnitType.Star);
            //        Grid.SetColumn(BreakdownView, 1);
            //    }
            //    else
            //    {
            //        //FirstColumn.Width = new GridLength(1, GridUnitType.Star);
            //        //SecondColumn.Width = new GridLength(0);
            //        if (BreakdownView != null)
            //        {
            //            Grid.SetColumn(BreakdownView, 0);
            //        }
                        
            //    }
            //}
        }

        private async void RBreakdown_Clicked(object sender, EventArgs e)
        {
            var p = BindingContext as PredictionContainer;
            var name = p.Name;

            //if (p.IsShowPage)
            //    //Navigation.PushAsync(new PredictionBreakdown(p.show, p.network) { BackgroundColor = Content.BackgroundColor });
            //    await Navigation.PushAsync(new ViewPage(new RatingsBreakdown(p.show, p.network), name) { BackgroundColor = Content.BackgroundColor });
            //else
            //{
            var parent = Parent.Parent.Parent as Grid;
            BreakdownView = new RatingsBreakdown(p.show, p.network)
            {
                Opacity = 0,
                BackgroundColor = Content.BackgroundColor
            };
            parent.Children.Add(BreakdownView);

            BreakdownView.Padding = p.IsShowPage ? new Thickness(0, 50, 0, 0) : 0;

            await BreakdownView.FadeTo(1);
            //}
        }
    }
}