using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Util.Store;
using System.Diagnostics;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Http;

namespace MagicMirror.Models
{
    public class GoogleCalendarApi
    {
        static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        static string AppName = "MagicMirror";
        CalendarService service;
        private string _authServerUrl = "https://accounts.google.com/o/oauth2/auth";
        private string _tokenServerUrl = "https://accounts.google.com/o/oauth2/token";
        private string _userId = "<USERID>";
        private string _deviceCode = "<DEVICECODE>";

        public async Task InitializeAsync()
        {
            var initializer = new Google.Apis.Auth.OAuth2.Flows.AuthorizationCodeFlow.Initializer(_authServerUrl, _tokenServerUrl);
            initializer.ClientSecrets = new ClientSecrets()
            {
                ClientId = "<CLIENTID>",
                ClientSecret = "<CLIENTSECRET>"
            };
            initializer.Scopes = Scopes;
            initializer.DataStore = new StorageDataStore();
            var flow = new AuthorizationCodeFlow(initializer);
            // try to load access token response from storage
            TokenResponse tokenResponse = null;
            try
            {
                tokenResponse = await flow.LoadTokenAsync(_userId, CancellationToken.None);
            }
            catch (Exception)
            {
            }
            // if access token response is not found, exchange device code for access token
            if (tokenResponse == null)
            {
                TokenRequest tokenRequest = new DeviceCodeTokenRequest()
                {
                    ClientId = initializer.ClientSecrets.ClientId,
                    ClientSecret = initializer.ClientSecrets.ClientSecret,
                    Code = _deviceCode,
                    Scope = Scopes[0]
                };
                tokenResponse = await flow.FetchTokenAsync(_userId, tokenRequest, CancellationToken.None);
                await flow.DataStore.StoreAsync(_userId, tokenResponse);
            }
            UserCredential credential = new UserCredential(flow, _userId, tokenResponse);
            service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = AppName,
            });
        }

        public async Task<IEnumerable<Event>> GetEvents()
        {
            // Define parameters of request.
            CalendarListResource.ListRequest calendarRequest = service.CalendarList.List();
            var calendars = await calendarRequest.ExecuteAsync();
            List<Event> allEvents = new List<Event>();
            foreach (var calendar in calendars.Items.Where(item => item.Selected ?? false))
            {
                EventsResource.ListRequest request = service.Events.List(calendar.Id);
                request.TimeMin = DateTime.Now.Date;
                request.TimeMax = DateTime.Now.Date.AddDays(1);
                request.ShowDeleted = false;
                request.SingleEvents = true;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
                Events events = await request.ExecuteAsync();
                if(events.Items != null)
                {
                    allEvents.AddRange(events.Items);
                }
            }
            return allEvents; 
        }
    }

    public class DeviceCodeTokenRequest : TokenRequest
    {
        public DeviceCodeTokenRequest()
        {
            GrantType = "http://oauth.net/grant_type/device/1.0";
        }

        /// <summary>Gets or sets the authorization code received from the authorization server.</summary>
        [Google.Apis.Util.RequestParameterAttribute("code")]
        public string Code { get; set; }
    }
}