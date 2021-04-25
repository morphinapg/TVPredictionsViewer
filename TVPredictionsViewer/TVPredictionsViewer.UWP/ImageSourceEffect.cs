using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TVPredictionsViewer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ResolutionGroupName("MyCompany")]
[assembly: ExportEffect(typeof(TVPredictionsViewer.UWP.ImageSourceEffect), nameof(ImageEffect))]
namespace TVPredictionsViewer.UWP
{
    public class ImageSourceEffect : PlatformEffect
    {
        Timer timer = new Timer(100) { Enabled = false, AutoReset = false };
        int Width, Height;

        protected override void OnAttached()
        {
            try
            {
                var control = Control ?? Container;

                if (control is DependencyObject)
                {
                    var image = control as Windows.UI.Xaml.Controls.Image;
                    var uri = ImageEffect.GetText(Element);
                    var bitmap = new BitmapImage(new Uri(uri));
                    bitmap.ImageOpened += Bitmap_ImageOpened;
                    image.Source = bitmap;
                    image.SizeChanged += Image_SizeChanged;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Cannot set property on attached control. Error: ", ex.Message);
            }
        }

        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (bitmapImage != null)
            {
                timer.Stop();
                timer.Elapsed -= Timer_Elapsed;

                Width = (int)e.NewSize.Width;
                Height = (int)e.NewSize.Height;

                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            }
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Device.InvokeOnMainThreadAsync(async () =>
            {
                await Task.Delay(1000);
                bitmapImage.DecodePixelType = DecodePixelType.Logical;
                bitmapImage.DecodePixelHeight = Height;
                bitmapImage.DecodePixelWidth = Width;
            });
            timer.Stop();
        }

        private BitmapImage bitmapImage;
        private void Bitmap_ImageOpened(object sender, RoutedEventArgs e)
        {
            bitmapImage = sender as BitmapImage;

        }

        protected override void OnDetached()
        {
            var control = Control ?? Container;
            if (control is DependencyObject)
            {
                var image = control as Windows.UI.Xaml.Controls.Image;
                image.Source = null;
            }
                
        }
    }
}
