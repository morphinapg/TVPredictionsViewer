using Windows.Storage;
using TV_Ratings_Predictions;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI;
using Xamarin.Forms;

namespace TVPredictionsViewer.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();
            NetworkDatabase.Folder = ApplicationData.Current.LocalFolder.Path;
            FFImageLoading.Forms.Platform.CachedImageRenderer.Init();           


            LoadApplication(new TVPredictionsViewer.App());

            var TitleColor = (Xamarin.Forms.Color)Application.Current.Resources["TitleColor"];
            var TitleText = (Xamarin.Forms.Color)Application.Current.Resources["TitleText"];

            var BarColor = new Windows.UI.Color() { A = 255, R = (byte)(TitleColor.R*255), G = (byte)(TitleColor.G*255), B = (byte)(TitleColor.B*255) };
            var BarText = new Windows.UI.Color() { A = 255, R = (byte)(TitleText.R*255), G = (byte)(TitleText.G*255), B = (byte)(TitleText.B*255) };

            if (TitleText == Xamarin.Forms.Color.White)
                BarText = Colors.Snow;

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = false;

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.BackgroundColor = BarColor;
            titleBar.ForegroundColor = BarText;
            titleBar.InactiveBackgroundColor = BarColor;
            titleBar.InactiveForegroundColor = BarText;
            titleBar.ButtonBackgroundColor = BarColor;
            titleBar.ButtonForegroundColor = BarText;
            titleBar.ButtonInactiveBackgroundColor = BarColor;
            titleBar.ButtonInactiveForegroundColor = BarText;


        }
    }
}
