Welcome to the Dwolla In-App WP8 SDK.  The purpose of this SDK is to make it easy for developers to use Dwolla within your Windows 8 Phone applications for in-app purchases.  The SDK works for Windows 8 phone apps.

Getting Started / Overview
=============================
1. Create a new Windows 8 Phone application or open an existing application.
2. Open the NuGet Package Manager (right-click on the Windows Phone project and select "Manage NuGet Packages".
3. Make sure you select "Online" on the left and search online for "Dwolla In-App SDK for Windows Phone 8".
4. You should see "Dwolla In-App SDK for Windows Phone 8".  Select the package and click "Install".  You may be prompted to accept some projects which are referenced.  Select "I Accept".

The NuGet Package will be installed and you will notice a couple of items in the project.
- References now include:
	Dwolla.InAppSDK
	System.Net.Http
	System.Net.Http.Extensions
	System.Net.Http.Primitives
	Microsoft.Phone.Controls.Toolkit

- A new folder "DwollaSDK" has been added to your project.  This folder contains a user control that you can use in your project that performs the in-app purchase functionality.  It can be customized by the developer.


Create a Dwolla application
===========================
To use the SDK you will need to register a free Dwolla application.  To do this go here: www.dwolla.com/applications

Be sure to request the following "scopes" when registering your application.
- Balance
- AccountInfoFull
- Send
- Funding
- Transactions

Once your application is registered and approved you will have an App Key and App Secret.  You will need these in the SDK.


Setting up your Windows Phone project
=========================================
There are few things you need to add to your project to get things going.

1. Set a Redirect URI to a custom URI scheme.  This is used in the OAuth process.
2. Reference to the Dwolla.InAppSDK.SendMoneyHelper class.
3. Implementing the UcSendMoney user control.

###Configuring the custom URI

As part of the OAuth process we need to redirect to the Dwolla site via a WebBrowserTask.  Part of the process is a callback URL.  In order for the browser to get back to the WP app, the custom URL scheme must be registered in the app.

- To register for a URI association, you must edit WMAppManifest.xml using the XML (Text) Editor. In Solution Explorer, expand the Properties folder and right-click the WMAppManifest.xml file, and then click Open With. In the Open With window, select XML(Text) Editor, and then click OK.

- In the Extensions element of the app manifest file, a URI association is specified with a Protocol element (note that the Extensions element must immediately follow the Tokens element). Your Extensions element should look like this:

```
<Extensions>
  <Protocol Name="dwollaappwp8" NavUriFragment="encodedLaunchUri=%s" TaskID="_default" />
</Extensions>
```
- Update App.xaml.cs:  Need to override the default URI-mapper class in the InitializePhoneApplication method.

```
RootFrame.UriMapper = new OAuthResponseUriMapper(redirectUri);
```

###Setup your application variables in App.xaml.cs.  

- Declare constants:

```
    //Enter your App Key & Secret here
    private const string AppKey = "";
    private const string AppSecret = "";
    private const string MerchantId = "111-111-1111";
    private const string RedirectUri = "dwollaappwp8://MainPage.xaml";
```

- Add variables to your application's resources in the InitializePhoneApplication method (before the UriMapper override):

```
    Resources.Add("AppKey", AppKey);
    Resources.Add("AppSecret", AppSecret);
    Resources.Add("MerchantId", MerchantId);
    Resources.Add("RedirectUri", RedirectUri);

    var redirectUri = Resources["RedirectUri"].ToString();
    RootFrame.UriMapper = new OAuthResponseUriMapper(redirectUri);
```


###Add the following using statements in your page/control:

```
using Dwolla.InAppSDK;
using Dwolla.InAppSDK.Models;
```

###Add the following variables in your page/control:

```
private SendMoneyHelper _sendMoneyHelper;
```

###Using the SendMoneyHelper Class  
Once the user is ready to pay via an in-app purchase SDK you need to make sure the user has authenticated via Dwolla.  To do this follow these steps:

- Create a new instance of the SendMoneyHelper class 
- Call the AuthenticateUser method.  
- If the response is successful then initialize the UcSendMoney user control by passing in the instance of the SendMoneyHelper class and the amount the user needs to pay.

When creating the instance of the SendMoneyHelper class you have the option to allow all types of funding sources or to limit funding sources to real-time only.  To limit to real-time only pass in false.

```
        //Gets the resource value based on a key (as defined in App.xaml.cs)
		private string GetResource(string key)
        {
            if (Application.Current.Resources != null && Application.Current.Resources.Contains(key))
            {
                return Application.Current.Resources[key].ToString();
            }
            return string.Empty;
        }

		//Initialize the SendMoneyHelper class
        private void Initialize()
        {
            _sendMoneyHelper = new SendMoneyHelper(GetResource("MerchantId"), true, GetResource("AppKey"), GetResource("AppSecret"), GetResource("RedirectUri"));
            UcSendMoney.SendMoneyCompleted += SendMoneyComplete;
            UcSendMoney.CancelSendMoney += CloseSendMoney;
        }

		//Initializes the SendMoneyHelper class and looks to see if Dwolla OAuth page has been redirected back to the app.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Initialize();

            // Check the arguments from the query string passed to the page.
            IDictionary<string, string> uriParams = NavigationContext.QueryString;

            // "Approve"
            if (uriParams.ContainsKey(OAuthParameters.Code) && e.NavigationMode != NavigationMode.Back)
            {
                //Now have the temp access code. Call to get the auth token
                var accessCode = uriParams[OAuthParameters.Code];
                AuthenticateWithCode(accessCode);
            }
            // "Deny"
            else if (uriParams.ContainsKey(OAuthParameters.Error) && e.NavigationMode != NavigationMode.Back)
            {
            }
        }

		//Starts the OAuth process
        private async void Authenticate()
        {
            if (_sendMoneyHelper != null)
            {
                AuthenticationResponse response = await _sendMoneyHelper.AuthenticateUser();
                if (response != null)
                {
                    //Do something
                }
            }
        }

```

###Events associated with UcSendMoney.  
There are two event associated with UcSendMoney:

- SendMoneyComplete: The user successfully sent money to the merchant via Dwolla.  The user control returns the transaction id, which could then be used to get details about that transaction.

```
	private void SendMoneyComplete(object sender, string e)
	{
		//Here is the transactionId
		string transactionId = e;
		GetTransactionByID(transactionId);
	}
```

- CloseSendMoney:  The user cancels out of the user control.  Typically you will want to hide the user control.

```
	private void CloseSendMoney(object sender, string e)
	{
	}
```

Available Methods via SendMoneyHelper
=====================================

- AuthenticateUser: Authenticates user for the application

- SendMoney: Sends Money to the merchant as specified by the application

- GetAccountFundingSources: Returns a collection of FundingSources of the authenticated user

- GetAccount: Returns the Account of the authenticated user

- GetMerchantAccount: Returns the Account of the merchant

- GetBalance: Get the account balance for the authenticate user

- GetTransactionByID: Get details for a given transaction

- Logout: Forces the user to be logged out

- IsLoggedIn:  Checks to see if the user is logged in

Notes:
- If an error is thrown by the Dwolla API SendMoneyHelper passes that error back to the caller

To learn more about the Dwolla API: https://developers.dwolla.com/dev/docs
