using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Xamarin.Facebook;
using Android.Content;

namespace FacebookNativeLogin.Droid
{
    [Activity(Label = "FacebookNativeLogin", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public static MainActivity CurrentActivity { get; private set; }
        static ICallbackManager _callbackManager;

        internal Xamarin.Facebook.ICallbackManager FacebookCallbackManager
        {
            get { return _callbackManager; }
        }

        protected override void OnCreate(Bundle bundle)
        {
            _callbackManager = CallbackManagerFactory.Create();

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            FacebookSdk.ApplicationId = "FACEBOOK_ID";
            FacebookSdk.ApplicationName = "FACEBOOK_APP_NAME";
            FacebookSdk.SdkInitialize(this);
            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
        }
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (_callbackManager != null)
            {
                _callbackManager.OnActivityResult(requestCode, (int)resultCode, data);
            }
        }
    }
}

