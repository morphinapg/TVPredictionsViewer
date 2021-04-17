using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TV_Ratings_Predictions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TVPredictionsViewer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TitleTemplate : ContentView
    {
        public Label TLabel
        {
            get
            {
                return TitleLabel;
            }
            set
            {
                TitleLabel = value;
            }
        }

        public SearchBar TBar
        {
            get
            {
                return Search;
            }
            set
            {
                Search = value;
            }
        }

        public Button TButton
        {
            get
            {
                return SearchButton;
            }
            set
            {
                SearchButton = value;
            }
        }

        public string Title
        {
            get
            {
                return TitleLabel.Text;
            }
            set
            {
                TitleLabel.Text = value;
            }
        }

        private bool _transparentButtons = false;
        public bool TransparentButtons
        {
            get { return _transparentButtons; }
            set 
            { 
                _transparentButtons = value;
                SearchButton.BackgroundColor = Color.Transparent;
                HomeButton.BackgroundColor = Color.Transparent;
            }
        }

        Timer timer = new Timer(10);
        double OriginalSize, MaxSize;
        double LastWidth, LastHeight;

        bool home = true;
        public bool HomeButtonDisplayed
        {
            get
            {
                return home;
            }
            set
            {
                home = value;
                NetworkDatabase_HomeButtonChanged(this, new EventArgs());
            }
        }

        public TitleTemplate()
        {           
            InitializeComponent();

            HomeButton.IsVisible = NetworkDatabase.HomeButton && HomeButtonDisplayed;
            NetworkDatabase.HomeButtonChanged += NetworkDatabase_HomeButtonChanged;
            timer.Elapsed += Timer_Elapsed;
            OriginalSize = TitleLabel.FontSize;
            MaxSize = OriginalSize;
        }

        private void NetworkDatabase_HomeButtonChanged(object sender, EventArgs e)
        {
            HomeButton.IsVisible = NetworkDatabase.HomeButton && HomeButtonDisplayed;
        }

        private async void HomeButton_Clicked(object sender, EventArgs e)
        {
            if (Parent is Page)
                await (Parent as Page).Navigation.PopToRootAsync();
            else
            {
                await (Parent.Parent.Parent.Parent as Page).Navigation.PopModalAsync();
                await (NetworkDatabase.mainpage.Detail as NavigationPage).Navigation.PopToRootAsync();
            }
                
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (TitleColumn.Width > 0 && TitleLabel.Width > 0)
                if (TitleColumn.Width <= TitleLabel.Width)
                {
                    Device.BeginInvokeOnMainThread(() => TitleLabel.FontSize -= 0.1);
                    MaxSize = TitleLabel.FontSize -0.1;
                }
                else if (TitleLabel.FontSize < MaxSize)
                    Device.BeginInvokeOnMainThread(() => TitleLabel.FontSize += 0.1);
                else
                {
                    MaxSize = OriginalSize;
                    timer.Stop();
                }
                
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (width > 0 && (width != LastWidth || height != LastHeight) && TitleLabel.IsVisible)
            {
                LastWidth = width;
                LastHeight = height;
                timer.Stop();
                timer.Start();
            }
        }
    }
}