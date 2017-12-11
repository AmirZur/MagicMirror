using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MagicMirror.ViewModels
{
    public class CurrentWeatherViewModel : INotifyPropertyChanged
    {
        //{
        //  "coord": {
        //    "lon": -122.09,
        //    "lat": 37.39
        //  },
        //  "sys": {
        //    "type": 3,
        //    "id": 168940,
        //    "message": 0.0297,
        //    "country": "US",
        //    "sunrise": 1427723751,
        //    "sunset": 1427768967
        //  },
        //  "weather": [
        //    {
        //      "id": 800,
        //      "main": "Clear",
        //      "description": "Sky is Clear",
        //      "icon": "01n"
        //    }
        //  ],
        //  "base": "stations",
        //  "main": {
        //    "temp": 285.68,
        //    "humidity": 74,
        //    "pressure": 1016.8,
        //    "temp_min": 284.82,
        //    "temp_max": 286.48
        //  },
        //  "wind": {
        //    "speed": 0.96,
        //    "deg": 285.001
        //  },
        //  "clouds": { "all": 0 },
        //  "dt": 1427700245,
        //  "id": 0,
        //  "name": "Mountain View",
        //  "cod": 200
        //}    
        public CurrentWeatherViewModel(string json)
        {
            var jsonObj = JObject.Parse(json);
            City = ((string)jsonObj["name"]).ToUpper();
            Description = (string)jsonObj["weather"][0]["description"];
            double kelvin = double.Parse((string)jsonObj["main"]["temp"]);
            Temprature = string.Format("{0:F0}\u00B0", ((kelvin - 273) / 5 * 9) + 32);
        }

        private string _city;
        public string City
        {
            get { return _city; }
            set
            {
                _city = value;
                OnProperyChanged();
            }
        }

        private string _temprature;
        public string Temprature
        {
            get { return _temprature; }
            set
            {
                _temprature = value;
                OnProperyChanged();
            }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                OnProperyChanged();
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        private void OnProperyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
