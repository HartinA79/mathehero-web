using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace MatheHero
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        //protected override void OnCreate(Bundle savedInstanceState)
        //{
        //    base.OnCreate(savedInstanceState);

        //    // Mach die Statusbar transparent
        //    if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
        //    {
        //        Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
        //        Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(
        //            SystemUiFlags.LayoutStable |
        //            SystemUiFlags.LayoutFullscreen
        //        );
        //    }
        //}
    }
}
