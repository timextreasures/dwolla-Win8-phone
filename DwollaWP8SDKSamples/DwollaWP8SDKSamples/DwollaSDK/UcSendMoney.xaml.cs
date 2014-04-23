using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Dwolla.InAppSDK;
using Dwolla.InAppSDK.Models;

namespace DwollaInAppWP8.DwollaSDK
{
    public partial class UcSendMoney
    {
        #region Events

        /// <summary>
        /// Event fires when user clicks the cancel button
        /// </summary>
        public event EventHandler<string> CancelSendMoney;

        /// <summary>
        /// Event fires when user clicks the done button after money has been sent.
        /// </summary>
        public event EventHandler<string> SendMoneyCompleted;

        #endregion

        #region Private Properties

        private SendMoneyHelper _sendMoneyHelper;
        private double _amount;
        private Account _merchantAccount;
        private Account _account;

        #endregion

        #region Page Methods

        public UcSendMoney()
        {
            InitializeComponent();
            Loaded += ucSendMoney_Loaded;
        }

        private void ucSendMoney_Loaded(object sender, RoutedEventArgs e)
        {
            InitSteps();
            _merchantAccount = null;
            _account = null;
            _amount = 0;
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            if (CancelSendMoney != null)
                CancelSendMoney(this, null);
        }

        private async void ButtonNext_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                switch (button.Tag.ToString().ToLower())
                {
                    case "next":
                        if (ValidateForm())
                            ShowReview();
                        break;
                    case "edit":
                        InitSteps();
                        break;
                    case "submit":
                        await SendMoney();
                        break;
                    case "done":
                        if (SendMoneyCompleted != null)
                            SendMoneyCompleted(this, TransactionId);
                        break;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the Send Money user control to be ready to process the Dwolla payment
        /// </summary>
        /// <param name="sendMoneyHelper">The current instance of the SendMoneyHelper class</param>
        /// <param name="amount">The amount to charge</param>
        public void Initialize(SendMoneyHelper sendMoneyHelper, double amount)
        {
            _sendMoneyHelper = sendMoneyHelper;
            _amount = amount;
            TxtAmount.Text = amount.ToString("C2");

            InitSteps();
            LoadMerchantInfo();
            LoadFundsSource();
            LoadBalance();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the Grids to show step 1.
        /// </summary>
        private void InitSteps()
        {
            TxtTitle.Text = "Send Money";
            GrdSendMoneyInputs.Visibility = Visibility.Visible;
            GrdReview.Visibility = Visibility.Collapsed;
            GrdConfirm.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Gets the account information for the Merchant
        /// </summary>
        private async void LoadMerchantInfo()
        {
            try
            {
                //Load the Merchant Information
                var merchantAccount = await _sendMoneyHelper.GetMerchantAccount();
                if (merchantAccount != null)
                {
                    _merchantAccount = merchantAccount;
                    TxtMerchantName.Text = merchantAccount.Name;
                    TxtMerchantId.Text = merchantAccount.Id;
                    var merchantUri = new Uri(merchantAccount.Avatar);
                    ImgMerchant.Source = new BitmapImage(merchantUri);
                }

                var account = await _sendMoneyHelper.GetAccount();
                if (account != null)
                {
                    _account = account;
                }

            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Gets the funding sources for the authenticated user
        /// </summary>
        private async void LoadFundsSource()
        {
            try
            {
                //Load the funding sources dropdown
                CboFundsSource.ItemsSource = null;
                var fundsSources = await _sendMoneyHelper.GetAccountFundingSources();
                if (fundsSources != null)
                {
                    var funds = fundsSources.ToList();
                    CboFundsSource.ItemsSource = funds;
                    CboFundsSource.SelectedItem = funds.FirstOrDefault();
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Gets the dwolla balance for the authenticated user
        /// </summary>
        private async void LoadBalance()
        {
            try
            {
                var balanceResponse = await _sendMoneyHelper.GetBalance();
                if (balanceResponse != null)
                {
                    double balance;
                    Double.TryParse(balanceResponse.Response, out balance);
                    TxtAvailableDwollaBalance.Text = "Available Dwolla balance: " + balance.ToString("C2");
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Show the review form with the information for the transaction
        /// </summary>
        private void ShowReview()
        {
            try
            {
                BtnSendMoney.IsEnabled = true;

                TxtTitle.Text = "Review";
                TxtReviewAmount.Text = "send " + _amount.ToString("C2");

                string destinationName = "";

                //Image for the Merchant
                if (_merchantAccount != null)
                {
                    var merchantUri = new Uri(_merchantAccount.Avatar);
                    ImgDestination.Source = new BitmapImage(merchantUri);
                    destinationName = _merchantAccount.Name;
                }

                //Image for the Authenticated User
                if (_account != null)
                {
                    var userUri = new Uri(_account.Avatar);
                    ImgSender.Source = new BitmapImage(userUri);
                }

                TxtReviewSend.Text = "You are about to send money to " + destinationName + " via " +
                     GetSelectedFundSource() + ".";

                TxtReviewSendTo.Text = destinationName;
                TxtReviewSource.Text = GetSelectedFundSource();
                TxtReviewAmount2.Text = _amount.ToString("C2");

                GrdSendMoneyInputs.Visibility = Visibility.Collapsed;
                GrdReview.Visibility = Visibility.Visible;
                GrdConfirm.Visibility = Visibility.Collapsed;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Gets the name of the selected funding source
        /// </summary>
        /// <returns>Name of the selected funding source</returns>
        private string GetSelectedFundSource()
        {
            if (CboFundsSource.SelectedItem != null)
            {
                var fundSource = CboFundsSource.SelectedItem as FundingSource;
                if (fundSource != null)
                {
                    return fundSource.Name;
                }
            }
            return "";
        }

        /// <summary>
        /// Call to the Send money API
        /// </summary>
        private async Task SendMoney()
        {
            //Call the send money
            int pin;
            if (Int32.TryParse(TxtPin.Password, out pin))
            {
                //Disable button so we don't click it twice.
                BtnSendMoney.IsEnabled = false;

                string fundsSource = "";
                var fundSelected = CboFundsSource.SelectedItem as FundingSource;
                if (fundSelected != null)
                {
                    fundsSource = fundSelected.Id;
                }
                var transaction = await _sendMoneyHelper.SendMoney(_amount, pin, fundsSource);
                if (transaction.Success)
                {
                    TransactionId = transaction.TransactionId;
                    //Show the confirmation screen
                    LoadConfirm();
                }
                else
                {
                    //Error - Show the initial screen
                    var result = MessageBox.Show("Error Sending Money: " + transaction.Message);
                    if (result == MessageBoxResult.OK)
                    {
                        InitSteps();
                    }
                }
            }
        }

        /// <summary>
        /// Show the confirmation screen
        /// </summary>
        private void LoadConfirm()
        {
            try
            {
                string destinationName = "";

                //Image for the Merchant
                if (_merchantAccount != null)
                {
                    var merchantUri = new Uri(_merchantAccount.Avatar);
                    ImgDestinationConfirm.Source = new BitmapImage(merchantUri);
                    destinationName = _merchantAccount.Name;
                }

                //Image for the Authenticated User
                if (_account != null)
                {
                    var userUri = new Uri(_account.Avatar);
                    ImgSenderConfirm.Source = new BitmapImage(userUri);
                }

                TxtConfirmSend.Text = "Success! You sent money to " + destinationName + ".";

                TxtTitle.Text = "Confirmed";
                GrdSendMoneyInputs.Visibility = Visibility.Collapsed;
                GrdReview.Visibility = Visibility.Collapsed;
                GrdConfirm.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Make sure all of the form fields have been entered and are valid.
        /// </summary>
        /// <returns>True if valid; else false</returns>
        private bool ValidateForm()
        {
            bool isValid = true;
            try
            {
                int pin;
                if (string.IsNullOrEmpty(TxtPin.Password.Trim()))
                {
                    isValid = false;
                    ShowInvalidPINMessage("PIN is required");
                }
                else if (TxtPin.Password.Trim().Length != 4)
                {
                    isValid = false;
                    ShowInvalidPINMessage("PIN must be 4 digits");
                }
                else if (!Int32.TryParse(TxtPin.Password.Trim(), out pin))
                {
                    isValid = false;
                    ShowInvalidPINMessage("Invalid PIN format");
                }
            }
            catch (Exception)
            {
                isValid = false;
            }
            return isValid;
        }

        /// <summary>
        /// Helper function which shows a message dialog box for the given message.
        /// </summary>
        /// <param name="message">The error message to display</param>
        private void ShowInvalidPINMessage(string message)
        {
            var result = MessageBox.Show(message);
            if (result == MessageBoxResult.OK)
            {
                TxtPin.Password = "";
                TxtPin.Focus();
            }
        }

        #endregion

        #region Public Properties

        public string TransactionId { get; private set; }

        #endregion

    }
}
