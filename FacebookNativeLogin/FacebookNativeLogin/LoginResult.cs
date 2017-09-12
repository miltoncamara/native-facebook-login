using System;

namespace FacebookNativeLogin
{
    public class LoginResult
    {
        public string Token { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string UserId { get; set; }
        public bool IsCancelled { get; set; }
        public string Error { get; set; }

    }
}
