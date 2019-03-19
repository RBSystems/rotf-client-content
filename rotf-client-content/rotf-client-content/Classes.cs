using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rotf_client_content
{
    public class Event
    {
        public class TargetDevice
        {
            public string buildingID { get; set; }
            public string roomID { get; set; }
            public string deviceID { get; set; }
        }

        public class AffectedRoom
        {
            public string buildingID { get; set; }
            public string roomID { get; set; }
        }

        [JsonProperty(PropertyName = "generating-system")]
        public string generatingSystem { get; set; }

        public string timestamp { get; set; }

        [JsonProperty(PropertyName = "event-tags")]
        public string[] eventTags { get; set; }

        [JsonProperty(PropertyName = "target-device")]
        public TargetDevice targetDevice { get; set; }

        [JsonProperty(PropertyName = "affected-room")]
        public AffectedRoom affectedRoom { get; set; }
        public string key { get; set; }
        public string value { get; set; }
        public string user { get; set; }
    }

    public class Display
    {
        public string name { get; set; }
        public string power { get; set; }
        public string input { get; set; }
        public bool blanked { get; set; }
    }

    public class AudioDevice
    {
        public string name { get; set; }
        public int volume { get; set; }
        public string power { get; set; }
        public string input { get; set; }
        public bool? muted { get; set; }
    }

    public class AvApiRequest
    {
        public List<Display> displays { get; set; }
        public List<AudioDevice> audioDevices { get; set; }
    }

    public class ConfigAction
    {
        public string id { get; set; }
        public string welcomeText { get; set; }
        public string welcomeMovie { get; set; }
        public string themeColor { get; set; }
        public string powerpoint { get; set; }
    }

    public class Config
    {
        public List<ConfigAction> actions { get; set; }
    }
}
