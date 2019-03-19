using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Speech;
using Microsoft.Exchange.WebServices.Data;
using Amazon.Polly;
using Amazon.Polly.Model;
using System.IO;
using System.Configuration;
using Newtonsoft.Json;

namespace rotf_client_content
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public class DataForUI
        {
            public string RoomName { get; set; }
            public string NextMeetingStartsAt { get; set; }
            public string NextMeetingCountdown { get; set; }
            public string MeetingTitle { get; set; }
            public string OrganizerName { get; set; }

            public class Attendee
            {
                public string Name { get; set; }
                public string NetID { get; set; }
                public bool Arrived { get; set; }
            }

            public List<Attendee> RequiredAttendees { get; set; }
            public List<Attendee> OptionalAttendees { get; set; }
        }

        public void Say(string text)
        {            
            var client = new AmazonPollyClient(Environment.GetEnvironmentVariable("AWS_POLLY_KEY"), Environment.GetEnvironmentVariable("AWS_POLLY_SECRET"), Amazon.RegionEndpoint.USWest1);
            String outputFileName = "speech.mp3";

            var synthesizeSpeechRequest = new SynthesizeSpeechRequest()
            {
                OutputFormat = OutputFormat.Mp3,
                VoiceId = VoiceId.Joanna,
                Text = text
            };

            try
            {
                using (var outputStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write))
                {
                    var synthesizeSpeechResponse = client.SynthesizeSpeech(synthesizeSpeechRequest);
                    byte[] buffer = new byte[2 * 1024];
                    int readBytes;

                    var inputStream = synthesizeSpeechResponse.AudioStream;
                    while ((readBytes = inputStream.Read(buffer, 0, 2 * 1024)) > 0)
                        outputStream.Write(buffer, 0, readBytes);
                }

                Speaker.LoadedBehavior = MediaState.Manual;
                Speaker.Source = new Uri("speech.mp3", UriKind.Relative);
                Speaker.Play();

                
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught: " + e.Message);
            }
        }
                
        public DataForUI Data { get; set; }        

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //start messenger
            System.Threading.Tasks.Task.Run(() => {
                Messenger.OnLog += Messenger_OnLog;
                Messenger.StartMessenger();
            });

            //go get the next meeting info
            System.Threading.Tasks.Task.Run(() => {
                GetNextMeetingInfo();
                Dispatcher.Invoke(() => this.DataContext = Data);
            });

            Data = new DataForUI();
            Data.RequiredAttendees = new List<DataForUI.Attendee>();
            Data.OptionalAttendees = new List<DataForUI.Attendee>();            
            Data.RoomName = ConfigurationManager.AppSettings["RoomID"];
            this.Data = Data;
        }
        
        private void Messenger_OnLog(object sender, string e)
        {
            if (e.StartsWith(ConfigurationManager.AppSettings["RoomName"]))
            {
                string[] lines = e.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                var theEvent = JsonConvert.DeserializeObject<Event>(lines[1]);
                AnnounceArrival(theEvent.user);                
            }
        }

        public void Log(string x)
        {
            //Dispatcher.Invoke(() => MyListBox.Items.Add(x));
        }

        public void AnnounceArrival(string netID)
        {
            var attendee = Data.RequiredAttendees.Find(one => one.NetID == netID);

            if (attendee == null)
            {
                attendee = Data.OptionalAttendees.Find(one => one.NetID == netID);
                if (attendee == null)
                {
                    attendee = new DataForUI.Attendee() { Name = netID, NetID = netID };
                    Data.OptionalAttendees.Add(attendee);
                }
            }

            //see if this is our first one
            bool first = true;
            foreach (var person in Data.RequiredAttendees)
            {
                if (person.Arrived)
                {
                    first = false;
                    break;
                }
            }

            foreach (var person in Data.OptionalAttendees)
            {
                if (person.Arrived)
                {
                    first = false;
                    break;
                }
            }

            if (first)
            {
                LowerProjectorScreen();
                TurnOnTV();
                //wait 2 seconds for stuff to turn on
                System.Threading.Thread.Sleep(2000);
            }

            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText("config.json"));
            //see if we have one for this uer
            var configAction = config.actions.Find(one => one.id == netID);

            if (configAction == null)
                configAction = config.actions.Find(one => one.id == "*");

            if (!string.IsNullOrEmpty(configAction.welcomeMovie))
            {
                //play the movie
                MediaPlayerWindow mp = new MediaPlayerWindow();
                mp.OnMediaEnded += (sender, e) =>
                {
                    //show the theme color for a couple seconds
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        Dispatcher.Invoke(() => LastGradientStop.Color = (Color)ColorConverter.ConvertFromString(configAction.themeColor));
                        System.Threading.Thread.Sleep(2000);
                        Dispatcher.Invoke(() => LastGradientStop.Color = Colors.White);
                    });                    

                    //say the welcome text
                    Say(configAction.welcomeText.Replace("{Name}", attendee.Name));
                };
                mp.Show();
                mp.PlayVideo(configAction.welcomeMovie);
            }
            else
            {
                //show the theme color for a couple seconds
                System.Threading.Tasks.Task.Run(() =>
                {
                    Dispatcher.Invoke(() => LastGradientStop.Color = (Color)ColorConverter.ConvertFromString(configAction.themeColor));
                    System.Threading.Thread.Sleep(2000);
                    Dispatcher.Invoke(() => LastGradientStop.Color = Colors.White);
                });

                //say the welcome text
                Say(configAction.welcomeText.Replace("{Name}", attendee.Name));
            }

            if (attendee.Name == Data.OrganizerName && !string.IsNullOrEmpty(configAction.powerpoint) && attendee.Arrived)
            {
                //second tap - show powerpoint
                System.Diagnostics.Process.Start(configAction.powerpoint);
            }

            attendee.Arrived = true;
        }

        private void GetNextMeetingInfo()
        {
            ExchangeService service = new ExchangeService();
            service.Credentials = new WebCredentials(Environment.GetEnvironmentVariable("EXCHANGE_PROXY_USERNAME"), Environment.GetEnvironmentVariable("EXCHANGE_PROXY_PASSWORD"));
            service.Url = new Uri(ConfigurationManager.AppSettings["ExchangeWebServiceURL"]);
            service.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, ConfigurationManager.AppSettings["ExchangeRoomID"]);

            CalendarFolder calendar = CalendarFolder.Bind(service, WellKnownFolderName.Calendar, new PropertySet());

            CalendarView cView = new CalendarView(DateTime.Today, DateTime.Today.AddDays(1));
            cView.PropertySet = new PropertySet(AppointmentSchema.Subject, AppointmentSchema.Start, AppointmentSchema.End, AppointmentSchema.Organizer);

            FindItemsResults<Appointment> appointments = calendar.FindAppointments(cView);            
            Appointment nextAppointment = null;

            foreach (Appointment a in appointments)
            {
                if ((nextAppointment == null || a.Start < nextAppointment.Start) && a.Start > DateTime.Now.AddMinutes(-30))
                {
                    nextAppointment = a;
                }
            }

            nextAppointment.Load();

            Data.MeetingTitle = nextAppointment.Subject;
            Data.NextMeetingStartsAt = "Next meeting at " + nextAppointment.Start.ToString("h:mm tt");

            var organizer = service.ResolveName(nextAppointment.Organizer.Address)[0];
            Data.OrganizerName = nextAppointment.Organizer.Name;
            
            foreach (var attend in nextAppointment.RequiredAttendees)
            {
                var attendee = service.ResolveName(attend.Address)[0];
                Data.RequiredAttendees.Add(new DataForUI.Attendee()
                {
                    Name = attend.Name,
                    NetID = attendee.Mailbox.Name,
                    Arrived = false
                });
            }

            foreach (var attend in nextAppointment.OptionalAttendees)
            {
                var attendee = service.ResolveName(attend.Address)[0];
                Data.OptionalAttendees.Add(new DataForUI.Attendee()
                {
                    Name = attend.Name,
                    NetID = attendee.Mailbox.Name,
                    Arrived = false
                });
            }
        }

        private void LowerProjectorScreen()
        {
            //lower the projector screen
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                //turn on the projector screen down button
                System.Net.WebClient wc = new System.Net.WebClient();
                wc.DownloadString("http://localhost:8028/10.66.9.16/activate/2");
                //wait 2 seconds
                System.Threading.Thread.Sleep(2000);
                //turn it off
                wc.DownloadString("http://localhost:8028/10.66.9.16/deactivate/2");
            });
        }

        private void RaiseProjectorScreen()
        {
            //raise the projector screen
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                //turn on the projector screen down button
                System.Net.WebClient wc = new System.Net.WebClient();
                wc.DownloadString("http://localhost:8028/10.66.9.16/activate/1");
                //wait 2 seconds
                System.Threading.Thread.Sleep(2000);
                //turn it off
                wc.DownloadString("http://localhost:8028/10.66.9.16/deactivate/1");
            });
        }

        private void BlinkLights()
        {
            //blink the lights
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                System.Net.WebClient wc = new System.Net.WebClient();

                string[] OneLevels = new string[] { "0", "50", "100", "25", "75", "0", "75", "0" };
                string[] TwoLevels = new string[] { "0", "20", "40", "60", "80", "100", "0", "0" };

                for (int i = 0; i < OneLevels.Length; i++)
                {
                    wc.DownloadString("http://localhost:8030/10.66.9.17/1/" + OneLevels[i].ToString());
                    wc.DownloadString("http://localhost:8030/10.66.9.17/2/" + TwoLevels[i].ToString());
                    System.Threading.Thread.Sleep(250);
                }

                wc.DownloadString("http://localhost:8030/10.66.9.17/1/off");
                wc.DownloadString("http://localhost:8030/10.66.9.17/2/off");
            });
        }

        private void TurnOnTV()
        {
            //turn on the TV
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                AvApiRequest req = new AvApiRequest();
                req.displays = new List<Display>();
                req.displays.Add(new Display() { name = "D1", power = "on", input = "PC1" });
                req.audioDevices = new List<AudioDevice>();
                req.audioDevices.Add(new AudioDevice() { name = "D1", power = "on", volume = 50 });
                System.Net.WebClient wc = new System.Net.WebClient();
                wc.Headers.Add(System.Net.HttpRequestHeader.ContentType, "application/json");
                wc.UploadString("http://itb-1106B-cp1.byu.edu:8000/buildings/ITB/rooms/1106B", "PUT",
                   JsonConvert.SerializeObject(req));
            });
        }
        
    }
}
