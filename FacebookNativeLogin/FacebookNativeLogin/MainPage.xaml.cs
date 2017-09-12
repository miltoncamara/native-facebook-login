using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace FacebookNativeLogin
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            var authService = DependencyService.Get<IAuthentication>(DependencyFetchTarget.NewInstance);
            var resultLogin = await authService.Login();
            var userInfo = await authService.GetInfo();
            lblName.Text = userInfo.Name;
            imgProfile.Source = userInfo.Picture;
        }
    }
}
