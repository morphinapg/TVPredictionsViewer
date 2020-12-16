using System;
using System.Collections.Generic;
using System.Text;

namespace TVPredictionsViewer
{
    public static class Constants
    {
        public const string ListenConnectionString = "Endpoint=sb://tvpredictionsviewer.servicebus.windows.net/;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=ogZQCPKaLi3nwfoWjeBsmhwN7JIZ/30r+Fklqg7RxcM=";
        public const string FullAccessConnectionString = "Endpoint=sb://tvpredictionsviewer.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=adhYJPv+vtK+J67zdTnEoVLzdWOQnfrm4icAdmLQ2Mo=";
        public const string NotificationHubName = "TVPredictionsViewer";
        public static string NotificationChannelName { get; set; } = "TVPredictions";

        public static string DebugTag { get; set; } = "XamarinNotify";
        public static string[] SubscriptionTags { get; set; } = { "default" };

        //public static string[] SubscriptionTags(string token)
        //{
        //    return new string[] { "$InstallationId:{" + token + "}" };
        //}
        public static string FCMTemplateBody { get; set; } = "{\"data\":{\"message\":\"$(messageParam)\"}}";
        public static string APNTemplateBody { get; set; } = "{\"aps\":{\"alert\":\"$(messageParam)\"}}";
        public static string WNSTemplateBody { get; set; } =
            "<toast>\r\n" + 
            "<visual><binding template=\"ToastText01\">\r\n" +
            "<text id=\"1\">$(messageParam)</text>\r\n" + 
            "</binding>\r\n" + 
            "</visual>\r\n" +
            "</toast>";
    }
}
