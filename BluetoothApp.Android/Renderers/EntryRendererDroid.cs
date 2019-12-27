using Android.Content;
using Android.Graphics.Drawables;
using BluetoothApp.Droid.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Entry), typeof(EntryRendererDroid))]
namespace BluetoothApp.Droid.Renderers
{
    public class EntryRendererDroid : EntryRenderer
    {
        public EntryRendererDroid(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                Control.SetPadding(5, 0, 5, 0);
                var gradientDrawable = new GradientDrawable();
                gradientDrawable.SetCornerRadius(5);
                gradientDrawable.SetStroke(2, Android.Graphics.Color.ParseColor("#323239"));
                Control.SetBackground(gradientDrawable);
            }
        }
    }
}
