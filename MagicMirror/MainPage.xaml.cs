using System;
using MagicMirror.ViewModels;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.UI.Xaml.Controls;
using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using System.Threading;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using System.IO;
using Windows.Foundation;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using MagicMirror.Models;
using Windows.UI.Xaml;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MagicMirror
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MediaCapture _mediaCapture;
        bool _isPreviewing;
        DisplayRequest _displayRequest;
        private string _APIKey = "<APIKEY>";
        private readonly Guid _personId = Guid.Parse("<PERSONID>");
        private readonly string _groupId = "<GROUPID>";
        private FaceServiceClient _faceClient = null;
        private Timer _timer;
        private bool _loggedOn;

        public MainPage()
        {
            this.InitializeComponent();
            this.ViewModel = new MainPageViewModel();
            _displayRequest = new DisplayRequest();
        }

        public MainPageViewModel ViewModel { get; set; }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _faceClient = new FaceServiceClient(_APIKey);
            await StartPreviewAsync();
            _timer = new Timer(OnTimer, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await ViewModel.InitializeAsync();
            });
        }

        private void OnTimer(object state)
        {
            if (!CheckTime()) { return; }
            CaptureCameraSnapshot();
        }

        private void Page_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key.ToString().Equals("Space"))
            {
                CaptureCameraSnapshot();
            }
        }

        private void CaptureCameraSnapshot()
        {
            var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                animCameraTimer.Stop();
                animCameraTimer.Begin();
            });
            if (_mediaCapture.CameraStreamState != Windows.Media.Devices.CameraStreamState.Streaming) { return; }
            IRandomAccessStream stream = new InMemoryRandomAccessStream();
            try
            {
                IAsyncAction action = _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);
                action.Completed = (result, status) =>
                {
                    if (status == AsyncStatus.Completed)
                    {
                        OnCapturePhotoCompleted(stream, result, status);
                    }
                };
            }
            catch (Exception)
            {
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
            }
        }

        private async void OnCapturePhotoCompleted(IRandomAccessStream stream, IAsyncAction result, AsyncStatus status)
        {
            try
            {
                Stream streamCopy = stream.AsStreamForRead();
                streamCopy.Position = 0;
                Face[] faces = await _faceClient.DetectAsync(streamCopy);
                stream.Dispose();
                stream = null;
                bool userDetected = false;
                foreach (var face in faces)
                {
                    VerifyResult verifyResult = await _faceClient.VerifyAsync(face.FaceId, _groupId, _personId);
                    if (userDetected = verifyResult.IsIdentical)
                    {
                        break;
                    }
                }
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (userDetected && !_loggedOn)
                    {
                        txtGreeting.Text = "Hello!";
                        animGreeting.Begin();
                        _loggedOn = true;
                        imgLogoff.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                        imgLogon.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        itmsCalendarEvents.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    }
                    else if (!userDetected && _loggedOn)
                    {
                        txtGreeting.Text = "Goodbye!";
                        animGreeting.Begin();
                        _loggedOn = false;
                        imgLogoff.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        imgLogon.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                        itmsCalendarEvents.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    }

                });
            }
            catch (FaceAPIException e)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    txtException.Text = e.ErrorMessage;
                    animException.Begin();
                });
            }
            catch (Exception)
            {
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
            }
        }

        private bool CheckTime()
        {
            DateTime now = DateTime.Now;
            return (now.Hour >= 6 && now.Hour < 7) || (now.Hour >= 15 && now.Hour < 21);
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await CleanupCameraAsync();
        }

        private async Task StartPreviewAsync()
        {
            try
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings()
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video
                });
                PreviewControl.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;

                _displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                System.Diagnostics.Debug.WriteLine("The app was denied access to the camera");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("MediaCapture initialization failed. {0}", ex.Message);
            }
        }

        private async Task CleanupCameraAsync()
        {
            if (_mediaCapture != null)
            {
                if (_isPreviewing)
                {
                    await _mediaCapture.StopPreviewAsync();
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PreviewControl.Source = null;
                    if (_displayRequest != null)
                    {
                        _displayRequest.RequestRelease();
                    }
                });

                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }
    }
}
