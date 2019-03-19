using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using System.Configuration;

namespace rotf_client_content
{
    public static class Messenger
    {
        static WebSocket ws;

        private sealed class Destructor
        {
            ~Destructor()
            {
                // One time only destructor.
                Messenger.ws.Close();
            }
        }

        public static event EventHandler<string> OnLog;

        public static void StartMessenger()
        {
            ws = new WebSocket(ConfigurationManager.AppSettings["CentralEventHubAddress"]);            
            ws.OnMessage += Ws_OnMessage;
            ws.OnError += Ws_OnError;
            ws.Connect();
            ws.Send("{\"rooms\":[\"" + ConfigurationManager.AppSettings["RoomID"] + "\"],\"create\":true}");            
        }

        private static void Ws_OnError(object sender, ErrorEventArgs e)
        {
            OnLog?.Invoke(null, e.Message);
        }

        private static void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            OnLog?.Invoke(null, System.Text.UTF8Encoding.Default.GetString(e.RawData));
        }
    }
}
