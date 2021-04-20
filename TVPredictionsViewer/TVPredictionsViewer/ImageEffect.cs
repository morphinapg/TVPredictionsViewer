using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace TVPredictionsViewer
{
    public static class ImageEffect
    {

        public static readonly BindableProperty TextProperty =
          BindableProperty.CreateAttached("Text", typeof(string), typeof(ImageEffect), string.Empty, propertyChanged: OnTextChanged);

        public static string GetText(BindableObject view)
        {
            return (string)view.GetValue(TextProperty);
        }

        public static void SetText(BindableObject view, string value)
        {
            view.SetValue(TextProperty, value);
        }

        static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (!(bindable is View view))
            {
                return;
            }

            string text = (string)newValue;
            if (!string.IsNullOrEmpty(text))
            {
                view.Effects.Add(new SourceEffect());
            }
            else
            {
                var toRemove = view.Effects.FirstOrDefault(e => e is SourceEffect);
                if (toRemove != null)
                {
                    view.Effects.Remove(toRemove);
                }
            }
        }

    }

    public class SourceEffect : RoutingEffect
    {
        public SourceEffect() : base($"MyCompany.{nameof(ImageEffect)}")
        {
        }
    }
}
