using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using IO.RestClient;
using System.Threading.Tasks;
using IO.SimpleHttpServer;
using Windows.UI.Core;

namespace MES_Simulator
{
    public sealed partial class MainPage : Page
    {

        RestClient RestAPIClient = new RestClient();
        HttpServer HttpRestServerForIoTServer = new HttpServer();
        private const string HttpServerPort = "8011";
        private const string HttpServerUserName = "MES";
        private const string HttpServerPassword = "P@ssword";

        int MessageID = 0;

        public MainPage()
        {
            this.InitializeComponent();

            RestAPIClient.Username = "MES";
            RestAPIClient.Password = "P@ssword";
            RestAPIClient.uriString = "http://10.29.2.152:8010/";
            RestAPIClient.RestClientInitialize();

            StartRestServerForIoTServer();
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            ProcessStart StartObj = new ProcessStart();

            MessageID += 1;
            StartObj.MessageID = MessageID.ToString();
            StartObj.ProductID = ProductId.Text;
            StartObj.TimestampUTC = DateTime.UtcNow;

            var result = await RestAPIClient.RestClientPOST("/iotserver/start", StartObj);
            if (result.Success == true)
            {
                MessageList.Items.Add(result.ResultContent);
            }
            else
            {
                MessageList.Items.Add(result.ErrorMessage);
            }
        }

        private async void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ProcessCancel CancelObj = new ProcessCancel();

            MessageID += 1;
            CancelObj.MessageID = MessageID.ToString();
            CancelObj.TimestampUTC = DateTime.UtcNow;

            var result = await RestAPIClient.RestClientPOST("/iotserver/cancel", CancelObj);
            if (result.Success == true)
            {
                MessageList.Items.Add(result.ResultContent);
            }
            else
            {
                MessageList.Items.Add(result.ErrorMessage);
            }
        }

        private async void GetStatus_Click(object sender, RoutedEventArgs e)
        {
            GetStatusRequest Request = new GetStatusRequest();

            MessageID += 1;
            Request.MessageID = MessageID.ToString();
            Request.TimestampUTC = DateTime.UtcNow;

            var result = await RestAPIClient.RestClientPOST("/iotserver/getstatus", Request);
            if (result.Success == true)
            {
                MessageList.Items.Add(result.ResultContent);
            }
            else
            {
                MessageList.Items.Add(result.ErrorMessage);
            }
        }

        private void ClearEvent_Click(object sender, RoutedEventArgs e)
        {
            MessageList.Items.Clear();
        }

        #region ## Rest Server for MES communication ##
        private async Task<bool> StartRestServerForIoTServer()
        {
            var result = await HttpRestServerForIoTServer.StartServer(HttpServerPort);
            HttpRestServerForIoTServer.ClientRequestEvent += HttpRestServerForIoTServer_ClientRequestEvent;
            return result.Success;
        }
        private async void HttpRestServerForIoTServer_ClientRequestEvent(object sender, ClientRequestEventArgs e)
        {
            IO.SimpleHttpServer.Result res = new IO.SimpleHttpServer.Result();

            try
            {
                if (e.Authorization == AuthorizationType.Basic && e.UserName == HttpServerUserName && e.Password == HttpServerPassword)
                {
                    if (e.RequestMethod == RequestMethodType.GET)
                    {
                        //....
                    }
                    else if (e.RequestMethod == RequestMethodType.POST)
                    {
                        if (e.uriString.ToLower() == "/mes/productreport")
                        {
                            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => MessageList.Items.Add(e.HttpContent));

                            res = await HttpRestServerForIoTServer.ServerResponse(HTTPStatusCodes.OK, e.OStream, null);
                        }
                        
                    }
                    else
                    {
                        res = await HttpRestServerForIoTServer.ServerResponse(HTTPStatusCodes.Not_Found, e.OStream, null);
                    }
                }
                else
                {
                    res = await HttpRestServerForIoTServer.ServerResponse(HTTPStatusCodes.Unauthorized, e.OStream, null);
                }
            }
            catch (Exception ex)
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => MessageList.Items.Add("HttpRestServerForIoTServer error: " + ex.Message));
            }
        }
        #endregion

        #region RestAPI objects
        private class ProcessStart
        {
            public string MessageID { get; set; }
            public string ProductID { get; set; }
            public DateTime TimestampUTC { get; set; }
        }

        private class ProcessCancel
        {
            public string MessageID { get; set; }
            public DateTime TimestampUTC { get; set; }
        }

        private class Answer
        {
            public string MessageID { get; set; }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }

        private class GetStatusRequest
        {
            public string MessageID { get; set; }
            public DateTime TimestampUTC { get; set; }
        }

        private class GetStatusAnswer
        {
            public string StationID { get; set; }
            public string MessageID { get; set; }
            public bool Success { get; set; }
            public DateTime TimestampUTC { get; set; }
            public bool Run { get; set; }
            public bool Error { get; set; }
            public bool Completed { get; set; }
            public int ProcessStatus { get; set; }
        }

        #endregion

    }
}
