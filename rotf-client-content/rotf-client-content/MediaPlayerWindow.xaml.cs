using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace rotf_client_content
{
    /// <summary>
    /// Interaction logic for MediaPlayerWindow.xaml
    /// </summary>
    public partial class MediaPlayerWindow : Window
    {
        public MediaPlayerWindow()
        {
            InitializeComponent();
        }

        public void PlayVideo(string url)
        {
            MeElement.LoadedBehavior = MediaState.Manual;
            MeElement.Source = new Uri(url);
            MeElement.MediaEnded += MeElement_MediaEnded;
            MeElement.Play();
        }

        public event EventHandler OnMediaEnded;

        private void MeElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            this.Close();
            OnMediaEnded?.Invoke(this, new EventArgs());            
        }
    }
}
