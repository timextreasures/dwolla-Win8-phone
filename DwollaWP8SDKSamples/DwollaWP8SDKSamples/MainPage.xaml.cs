using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Navigation;
using Dwolla.InAppSDK;
using Dwolla.InAppSDK.Models;
using Dwolla.InAppSDK.Utils;

namespace DwollaWP8SDKSamples
{
    public partial class MainPage
    {
        #region Private Variables

        private SendMoneyHelper _sendMoneyHelper;

        #endregion

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        #region Page Methods

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ResetViews();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Initialize();

            // Check the arguments from the query string passed to the page.
            IDictionary<string, string> uriParams = NavigationContext.QueryString;

            // "Approve"
            if (uriParams.ContainsKey(OAuthParameters.Code) && e.NavigationMode != NavigationMode.Back)
            {
                //This method was decoding a '+' (%2B) to a space (%20), which caused an issue
                //var accessCode = uriParams[OAuthParameters.Code];
                string url = HttpUtility.UrlDecode(e.Uri.ToString());
                int startPos = url.IndexOf("code=", StringComparison.Ordinal) + 5;
                var accessCode = url.Substring(startPos);

                //Now have the temp access code. Call to get the auth token
                AuthenticateWithCode(accessCode);
            }
            // "Deny"
            else if (uriParams.ContainsKey(OAuthParameters.Error) && e.NavigationMode != NavigationMode.Back)
            {
            }
        }

        private string GetResource(string key)
        {
            if (Application.Current.Resources != null && Application.Current.Resources.Contains(key))
            {
                return Application.Current.Resources[key].ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// Setup the control to show the initial screen
        /// </summary>
        private void Initialize()
        {
            _sendMoneyHelper = new SendMoneyHelper(GetResource("MerchantId"), true, GetResource("AppKey"), GetResource("AppSecret"), GetResource("RedirectUri"));
            UcSendMoney.SendMoneyCompleted += SendMoneyComplete;
            UcSendMoney.CancelSendMoney += CloseSendMoney;
            SetLoginStatus();
        }

        private void ResetViews()
        {
            SpStore.Visibility = Visibility.Visible;
            GridStore.Visibility = Visibility.Visible;
            SpPayNow.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Event handler fired from UcSendMoney if the user cancels
        /// </summary>
        private void CloseSendMoney(object sender, string e)
        {
            ResetViews();
        }

        /// <summary>
        /// Event handler fired from UcSendMoney when user is finished sending money (clicks done)
        /// </summary>
        private void SendMoneyComplete(object sender, string e)
        {
            //Here is the transactionId
            string transactionId = e;
            GetTransactionByID(transactionId);
            ResetViews();
        }

        #endregion

        #region Authentication Methods

        /// <summary>
        /// Used to set the visibility of the logout button
        /// </summary>
        private void SetLoginStatus()
        {
            if (_sendMoneyHelper != null)
            {
                //TxtLoginStatus.Text = _sendMoneyHelper.IsLoggedIn() ? "Logged In" : "Not Logged In";
                BtnLogout.Visibility = _sendMoneyHelper.IsLoggedIn() ? Visibility.Visible : Visibility.Collapsed;
                BtnClearRefreshToken.Visibility = _sendMoneyHelper.IsLoggedIn() ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets transaction details by ID.  Useful after send money is completed.
        /// </summary>
        /// <param name="transactionId">The transaction Id</param>
        private async void GetTransactionByID(string transactionId)
        {
            if (_sendMoneyHelper != null)
            {
                var transaction = await _sendMoneyHelper.GetTransactionByID(transactionId);
                TxtResponse.Text = transaction.Id;
            }
        }

        /// <summary>
        /// Used to show and initialize the UcSendMoney control.
        /// </summary>
        private void SetPayNowStatus()
        {
            if (_sendMoneyHelper != null)
            {
                SpPayNow.Visibility = _sendMoneyHelper.IsLoggedIn() ? Visibility.Visible : Visibility.Collapsed;
                SpStore.Visibility = _sendMoneyHelper.IsLoggedIn() ? Visibility.Collapsed : Visibility.Visible;
                GridStore.Visibility = _sendMoneyHelper.IsLoggedIn() ? Visibility.Collapsed : Visibility.Visible;
                if (_sendMoneyHelper.IsLoggedIn())
                {
                    UcSendMoney.Initialize(_sendMoneyHelper, Convert.ToDouble(TxtAmount.Text.Replace("$", "")));
                }
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Authenticate();
        }

        /// <summary>
        /// Authenticates the user using the SDK
        /// </summary>
        private async void Authenticate()
        {
            if (_sendMoneyHelper != null)
            {
                AuthenticationResponse response = await _sendMoneyHelper.AuthenticateUser();
                if (response != null)
                {
                    CheckForLogin();
                    //TxtResponse.Text = response.Message;
                }
            }
        }

        private async void AuthenticateWithCode(string accessCode)
        {
            if (_sendMoneyHelper == null)
                return;

            await _sendMoneyHelper.HandleApprove(accessCode);

            CheckForLogin();
        }

        private void CheckForLogin()
        {
            if (_sendMoneyHelper.IsLoggedIn())
            {
                ////Get the balance just for fun
                //BalanceResponse bal = await _sendMoneyHelper.GetBalance();
                //if (bal != null)
                //    TxtResponse.Text += "; balance: " + bal.Response;

                //Authenticated so now prompt user for payment
                SetPayNowStatus();
            }

            SetLoginStatus();
        }

        private void Logout_OnClick(object sender, RoutedEventArgs e)
        {
            if (_sendMoneyHelper != null)
            {
                _sendMoneyHelper.Logout();
                SetLoginStatus();
            }
        }

        private void ClearRefreshToken_OnClick(object sender, RoutedEventArgs e)
        {
            if (_sendMoneyHelper != null)
            {
                _sendMoneyHelper.ClearRefreshToken();
            }
        }

        #endregion
    }
}