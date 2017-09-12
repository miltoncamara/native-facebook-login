using Android.OS;
using Android.Runtime;
using FacebookNativeLogin.Droid;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Facebook;
using Xamarin.Facebook.Login;

[assembly: Xamarin.Forms.Dependency(typeof(Authenticate))]
namespace FacebookNativeLogin.Droid
{
    public class Authenticate : IAuthentication
    {
        readonly List<string> _readPermissions = new List<string>
        {
            "public_profile, email, user_birthday", "user_friends", "user_about_me"
        };

        public void Logout()
        {
            LoginManager.Instance.LogOut();
        }

        private async Task UpdateFacebookResult(LoginResult login, bool iscancelled = false, bool iserror = false)
        {
            await Task.Run(() =>
            {
                var token = AccessToken.CurrentAccessToken != null;
                if (token)
                {
                    login.Token = AccessToken.CurrentAccessToken?.Token;
                    login.ExpirationDate = AccessToken.CurrentAccessToken != null ? JavaToCsharpDateTime(AccessToken.CurrentAccessToken?.Expires.Time) : null;
                    login.UserId = AccessToken.CurrentAccessToken?.UserId;
                    login.IsCancelled = iscancelled;
                }
                else
                {
                    login.IsCancelled = iscancelled;
                }
            });
        }

        public DateTime? JavaToCsharpDateTime(long? longTimeMillis)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            if (longTimeMillis != null) return epoch.AddMilliseconds(longTimeMillis.Value);
            return null;
        }

        public async Task<bool> PostToFacebook(string statusUpdate, byte[] media)
        {
            if (AccessToken.CurrentAccessToken == null || string.IsNullOrEmpty(AccessToken.CurrentAccessToken.UserId))
                return false;
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var parameters = new Bundle();
            parameters.PutString("message", statusUpdate);
            parameters.PutByteArray("picture", media);

            var response = new Response()
            {
                HandleSuccess = (respose) => { taskCompletionSource.SetResult(true); }
            };

            var graphRequest = new GraphRequest(AccessToken.CurrentAccessToken,
                media != null ? "/me/photos" : "/me/feed",
                parameters,
                HttpMethod.Post, response);
            graphRequest.ExecuteAsync();
            return await taskCompletionSource.Task;
        }

        const int _requestLimit = 5000;

        public async Task<List<FacebookFriend>> GetFriendsList()
        {
            // UserProfile userProfile;
            if (AccessToken.CurrentAccessToken == null || string.IsNullOrEmpty(AccessToken.CurrentAccessToken.UserId))
                return null;
            var facebookFriend = new List<FacebookFriend>();
            var taskCompletionSource = new TaskCompletionSource<List<FacebookFriend>>();
            var parameters = new Bundle();
            parameters.PutString("fields", "id, name,picture");
            parameters.PutString("limit", _requestLimit.ToString());

            var response = new Response()
            {
                HandleSuccess = (respose) =>
                {
                    var data = respose.JSONObject.GetJSONArray("data");

                    for (int i = 0; i < data.Length(); i++)
                    {
                        var jsonobject = data.GetJSONObject(i);
                        var friendId = jsonobject.GetString("id");

                        var fbFriend = new FacebookFriend()
                        {
                            Id = friendId,
                            Name = jsonobject.GetString("name"),
                            PictureUrl = $"https://graph.facebook.com/{friendId}/picture?type=normal"
                        };

                        facebookFriend.Add(fbFriend);
                    }
                    taskCompletionSource.SetResult(facebookFriend);
                }
            };


            var graphRequest = new GraphRequest(AccessToken.CurrentAccessToken,
                "/" + AccessToken.CurrentAccessToken.UserId + "/friends",
                null,
                HttpMethod.Get, response);
            graphRequest.ExecuteAsync();
            return await taskCompletionSource.Task;
        }

        public async Task<LoginResult> Login()
        {
            var login = new LoginResult();

            if (AccessToken.CurrentAccessToken == null ||
                (AccessToken.CurrentAccessToken != null && AccessToken.CurrentAccessToken.IsExpired))
            {
                // LoginManager.Instance.LogOut();

                var taskCompletionSource = new TaskCompletionSource<LoginResult>();
                var activity = Xamarin.Forms.Forms.Context as MainActivity;
                var loginCallback = new FacebookCallback<Xamarin.Facebook.Login.LoginResult>
                {
                    HandleSuccess = async loginResult =>
                    {
                        await UpdateFacebookResult(login);
                        taskCompletionSource.SetResult(login);
                    },
                    HandleCancel = async () =>
                    {
                        System.Diagnostics.Debug.WriteLine("fbcancel");
                        await UpdateFacebookResult(login, true, false);
                        taskCompletionSource.SetResult(login);
                    },
                    HandleError = async loginError =>
                    {
                        System.Diagnostics.Debug.WriteLine("fberror");
                        login.Error = loginError.Message;
                        await UpdateFacebookResult(login, true, true);
                        taskCompletionSource.SetResult(login);
                    }
                };

                if (activity == null) return null;
                LoginManager.Instance.RegisterCallback(activity.FacebookCallbackManager, loginCallback);

                LoginManager.Instance.LogInWithReadPermissions(activity, _readPermissions);

                return await taskCompletionSource.Task;
            }
            await UpdateFacebookResult(login);
            return login;
        }

        public async Task<UserInfo> GetInfo()
        {
            UserInfo userProfile;
            var taskCompletionSource = new TaskCompletionSource<UserInfo>();
            var userResponse = new Response()
            {
                HandleSuccess = (response) =>
                {
                    userProfile = new UserInfo
                    {
                        Name = response.JSONObject.Has("first_name") ? response.JSONObject.GetString("first_name") : "Unknow",
                        Email = response.JSONObject.Has("email") ? response.JSONObject.GetString("email") : "Unknow",
                        Picture = $"http://graph.facebook.com/{response.JSONObject.GetString("id")}/picture?type=large"

                    };

                    taskCompletionSource.SetResult(userProfile);
                }
            };

            var graphRequest = new GraphRequest(AccessToken.CurrentAccessToken, "/me?fields=id,about,birthday,email,first_name,last_name,middle_name,gender", null, HttpMethod.Get, userResponse);
            graphRequest.ExecuteAsync();
            return await taskCompletionSource.Task;
        }

        public class FacebookFriend
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string PictureUrl { get; set; }
        }

        /// <summary>
        /// Class Response.
        /// </summary>
        class Response : Java.Lang.Object, GraphRequest.ICallback
        {
            /// <summary>
            /// Gets or sets the handle success.
            /// </summary>
            /// <value>The handle success.</value>
            public Action<GraphResponse> HandleSuccess { get; set; }

            /// <summary>
            /// Called when [completed].
            /// </summary>
            /// <param name="response"></param>
            public void OnCompleted(GraphResponse response)
            {
                var c = HandleSuccess;
                c.Invoke(response);
            }
        }

        /// <summary>
        /// Class FacebookCallback.
        /// </summary>
        /// <typeparam name="TResult">The type of the t result.</typeparam>
        private class FacebookCallback<TResult> : Java.Lang.Object, IFacebookCallback where TResult : Java.Lang.Object
        {
            /// <summary>
            /// Gets or sets the handle cancel.
            /// </summary>
            /// <value>The handle cancel.</value>
            public Action HandleCancel { get; set; }

            /// <summary>
            /// Gets or sets the handle error.
            /// </summary>
            /// <value>The handle error.</value>
            public Action<FacebookException> HandleError { get; set; }

            /// <summary>
            /// Gets or sets the handle success.
            /// </summary>
            /// <value>The handle success.</value>
            public Action<TResult> HandleSuccess { get; set; }

            /// <summary>
            /// Called when [cancel].
            /// </summary>
            public void OnCancel()
            {
                var c = HandleCancel;
                c?.Invoke();
            }

            /// <summary>
            /// Called when [error].
            /// </summary>
            /// <param name="error">The error.</param>
            public void OnError(FacebookException error)
            {
                var c = HandleError;
                c?.Invoke(error);
            }

            /// <summary>
            /// Called when [success].
            /// </summary>
            /// <param name="result">The result.</param>
            public void OnSuccess(Java.Lang.Object result)
            {
                var c = HandleSuccess;
                c?.Invoke(result.JavaCast<TResult>());
            }
        }
    }
}