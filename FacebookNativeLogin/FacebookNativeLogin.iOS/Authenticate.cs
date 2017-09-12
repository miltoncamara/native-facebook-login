using Facebook.CoreKit;
using Facebook.LoginKit;
using FacebookNativeLogin.iOS;
using Foundation;
using System.Collections.Generic;
using System.Threading.Tasks;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(Authenticate))]
namespace FacebookNativeLogin.iOS
{
    public class Authenticate : IAuthentication
    {
        readonly List<string> _readPermissions = new List<string>
        {
            "public_profile", "email", "user_birthday", "user_friends", "user_about_me"
        };

        public async Task<UserInfo> GetInfo()
        {
            UserInfo userProfile;
            var taskCompletionSource = new TaskCompletionSource<UserInfo>();
            var graphRequest = new GraphRequest("/me?fields=id,about,birthday,email,first_name,last_name,middle_name,gender", null, AccessToken.CurrentAccessToken.TokenString, null, "GET");
            var requestConnection = new GraphRequestConnection();

            requestConnection.AddRequest(graphRequest, (connection, profileReturn, errorReturn) =>
            {
                if (errorReturn == null)
                {
                    var profile = profileReturn;
                    var userInfo = profile as NSDictionary;

                    userProfile = new UserInfo
                    {
                        Name = userInfo["first_name"]?.ToString(),
                        Lastname = userInfo["last_name"]?.ToString(),
                        Email = userInfo["email"]?.ToString(),
                        Description = userInfo["about"]?.ToString(),
                        Picture = $"http://graph.facebook.com/{userInfo["id"].ToString()}/picture?type=large"
                    };

                    taskCompletionSource.SetResult(userProfile);
                }
            });

            requestConnection.Start();
            return await taskCompletionSource.Task;
        }

        public async Task<LoginResult> Login()
        {
            var login = new LoginResult();

            if (AccessToken.CurrentAccessToken == null)
            {
                var window = UIApplication.SharedApplication.KeyWindow;
                var vc = window.RootViewController;

                while (vc.PresentedViewController != null)
                {
                    vc = vc.PresentedViewController;
                }

                var taskCompletionSource = new TaskCompletionSource<LoginResult>();
                var loginCallback = new LoginManagerRequestTokenHandler(async (r, e) =>
                {
                    if (e == null && !r.IsCancelled)
                    {
                        System.Diagnostics.Debug.WriteLine("fberror");
                        await UpdateFacebookResult(login);
                        taskCompletionSource.SetResult(login);
                    }
                });

                var loginManager = new LoginManager();
                var loginResult = await loginManager.LogInWithReadPermissionsAsync(_readPermissions.ToArray(), vc);
            }

            await UpdateFacebookResult(login);
            return login;
        }

        public void Logout()
        {
            var loginManager = new LoginManager();
            loginManager.LogOut();
        }

        public System.Threading.Tasks.Task RegisterNotification()
        {
            return Task<string>.FromResult("Register for notification");
        }

        private async System.Threading.Tasks.Task UpdateFacebookResult(LoginResult login, bool iscancelled = false, bool iserror = false)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                var token = AccessToken.CurrentAccessToken != null;
                if (token)
                {
                    login.Token = AccessToken.CurrentAccessToken?.TokenString;
                    //login.ExpirationDate = AccessToken.CurrentAccessToken != null ? JavaToCsharpDateTime(AccessToken.CurrentAccessToken?.ExpirationDate) : null;
                    login.UserId = AccessToken.CurrentAccessToken.UserID;
                    login.IsCancelled = iscancelled;
                }
                else
                {
                    login.IsCancelled = iscancelled;
                }
            });
        }
    }
}