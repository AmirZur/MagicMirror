using Google.Apis.Calendar.v3.Data;
using MagicMirror.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace MagicMirror.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        #region private members
        private DispatcherTimer _timer;
        private DateTime _now;
        private WeatherApi _weatherApi;
        private DispatcherTimer _weatherTimer;
        private CurrentWeatherViewModel _currentWeather;
        private DateTime _lastWeatherUpdate;
        private GoogleCalendarApi _calendarApi;
        private DispatcherTimer _calendarTimer;
        #endregion

        #region ctor
        public MainPageViewModel()
        {
            Now = DateTime.Now;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += _timer_Tick;
            _timer.Start();
            _weatherTimer = new DispatcherTimer();
            _weatherTimer.Interval = TimeSpan.FromHours(1);
            _weatherTimer.Tick += _weatherTimer_Tick;
            _weatherApi = new WeatherApi();
            _calendarApi = new GoogleCalendarApi();
            _calendarTimer = new DispatcherTimer();
            _calendarTimer.Interval = TimeSpan.FromHours(1);
            _calendarTimer.Tick += _calendarTimer_Tick;
            _calendarEvents = new ObservableCollection<CalendarEventViewModel>();
        }

        
        #endregion

        #region public properties
        public DateTime Now
        {
            get { return _now; }
            set
            {
                if (_now != value)
                {
                    _now = value;
                    OnProperyChanged();
                    OnProperyChanged("Date");
                    OnProperyChanged("Time");
                }
            }
        }

        public string Time
        {
            get { return Now.ToString("h:mm"); }
        }

        public string Date
        {
            get { return Now.ToString("dddd, MMMM dd"); }
        }

        public CurrentWeatherViewModel CurrentWeather
        {
            get { return _currentWeather; }
            set
            {
                _currentWeather = value;
                OnProperyChanged();
            }
        }

        public DateTime LastWeatherUpdate
        {
            get { return _lastWeatherUpdate; }
            set
            {
                _lastWeatherUpdate = value;
                OnProperyChanged();
                OnProperyChanged("LastWeatherUpdateDisplay");
            }
        }

        public string LastWeatherUpdateDisplay
        {
            get { return "Last Updated: " + LastWeatherUpdate.ToString("M/d h:mm"); }
        }

        private ObservableCollection<CalendarEventViewModel> _calendarEvents;
        public ObservableCollection<CalendarEventViewModel> CalendarEvents
        {
            get { return _calendarEvents; }
            set
            {
                _calendarEvents = value;
                OnProperyChanged();
            }
        }

        #endregion

        #region public methods
        public async Task InitializeAsync()
        {
            await GetCurrentWeatherAsync();
            _weatherTimer.Start();
            await _calendarApi.InitializeAsync();
            await GetCalendarEventsAsync();
            _calendarTimer.Start();
        }

        #endregion
        #region private methods
        private void _timer_Tick(object sender, object e)
        {
            Now = DateTime.Now;
        }

        private async void _weatherTimer_Tick(object sender, object e)
        {
            await GetCurrentWeatherAsync();
        }

        private async Task GetCurrentWeatherAsync()
        {
            try
            {
                string currentWeather = await _weatherApi.GetCurrentWeatherByCityAsync("<CITY>", "<STATE>");
                CurrentWeather = new CurrentWeatherViewModel(currentWeather);
                LastWeatherUpdate = DateTime.Now;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Weather API failed. Error: " + e.Message);
            }
        }

        private async void _calendarTimer_Tick(object sender, object e)
        {
            await GetCalendarEventsAsync();
        }

        private async Task GetCalendarEventsAsync()
        {
            IEnumerable<Event> events = await _calendarApi.GetEvents();
            _calendarEvents.Clear();
            if (!events.Any<Event>())
            {
                _calendarEvents.Add(new CalendarEventViewModel()
                {
                    Start = "No events today",
                    End = null,
                    Title = null
                });
            }
            else
            {
                foreach (Event e in events)
                {
                    _calendarEvents.Add(new CalendarEventViewModel()
                    {
                        Start = e.Start.DateTime.HasValue ? e.Start.DateTime.Value.ToString("M/dd h:mm tt").ToLower() : "All day",
                        End = e.End.DateTime.HasValue ? e.End.DateTime.Value.ToString("M/dd h:mm tt").ToLower() : string.Empty,
                        Title = e.Summary
                    });
                }
            }
        }

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        private void OnProperyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
