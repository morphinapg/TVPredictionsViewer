using Microsoft.Azure.NotificationHubs;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xamarin.Forms;

namespace TVPredictionsViewer.Notifications
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        async Task<NotificationOutcome> InternalSendNotificationAsync(string message, string installationId = "")
        {
            var hub = NotificationHubClient.CreateClientFromConnectionString(Constants.FullAccessConnectionString, Constants.NotificationHubName, true);

            //var regs = await hub.GetAllRegistrationsAsync(0);

            var templateParams = new Dictionary<string, string>
            {
                ["messageParam"] = message
            };

            NotificationOutcome result = (string.IsNullOrWhiteSpace(installationId)) ? 
                await hub.SendTemplateNotificationAsync(templateParams) : 
                await hub.SendTemplateNotificationAsync(templateParams, "$InstallationId:{" + installationId + "}");

            return result;
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            Send.Content = "Sending...";
            Send.IsEnabled = false;

            var result = SpecifyID.IsChecked == true ? await InternalSendNotificationAsync(Message.Text, ID.Text) : await InternalSendNotificationAsync(Message.Text);

            //while ((int) result.State < 3)
            //    System.Threading.Thread.Sleep(250);

            MessageBox.Show("Successfully sent notifications to " + result.Success + " devices.");

            Send.Content = "Send Notification";
            Send.IsEnabled = true;
        }
    }
}
