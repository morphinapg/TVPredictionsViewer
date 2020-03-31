using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TVPredictionsViewer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChangeLog : ContentView
    {
        public ChangeLog()
        {
            InitializeComponent();
            var assembly = IntrospectionExtensions.GetTypeInfo(typeof(ChangeLog)).Assembly;
            Stream stream = assembly.GetManifestResourceStream("TVPredictionsViewer.ChangeLog.txt");

            using (var reader = new System.IO.StreamReader(stream))
            {
                Log.Text = reader.ReadToEnd();
            }
        }

        private async void ImageButton_Clicked(object sender, EventArgs e)
        {
            await Launcher.OpenAsync("https://www.Facebook.com/TVPredictions");
        }

        private async void PayPalButton_Clicked(object sender, EventArgs e)
        {
            await Launcher.OpenAsync("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=MSNDRDRMWGLUU&source=url");
        }
    }
}