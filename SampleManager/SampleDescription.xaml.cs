using GoogleAnalytics;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace SampleManager
{
    public sealed partial class SampleDescription : UserControl
    {
        public event EventHandler CloseClicked;

        private string _cacheFileName = "description.md";

        public SampleDescription(string mdLocal, string mdRemote = null)
        {
            this.RequiresPointer = RequiresPointer.WhenFocused;
            this.InitializeComponent();

            if (!Sample.IsXbox())
            {
                CloseButton.Visibility = Visibility.Visible;
            }
            else
            {

            }

            var nop = Init(mdLocal, mdRemote);
        }

        private async Task Init(string mdFilename, string mdRemote)
        {
            string md = null;
            try
            {
                if (mdRemote != null)
                {
                    var client = new HttpClient();
                    
                    md = await client.GetStringAsync(new Uri(mdRemote));

                    if (string.IsNullOrWhiteSpace(md))
                    {
                        md = null;
                    }
                    else
                    {
                        await StorageFileHelper.WriteTextToLocalFileAsync(md, _cacheFileName);
                    }
                }
            }
            catch (Exception){}

            // get from cache if not available online
            if (md == null)
            {
                try
                {
                    md = await StorageFileHelper.ReadTextFromLocalFileAsync(_cacheFileName);
                }
                catch (Exception) {}
            }

            // get packaged copy if not available in cache
            if (md == null)
            {
                md = await StorageFileHelper.ReadTextFromPackagedFileAsync(mdFilename);
            }

            MTB.Text = md;

            ProgressRinger.IsEnabled = false;
            ProgressRinger.Visibility = Visibility.Collapsed;
            Content.Fade(1, 200).Start();
        }

        private void Close_Clicked(object sender, RoutedEventArgs e)
        {
            CloseClicked?.Invoke(this, null);
        }

        public void FocusWebView()
        {
            MTB.Focus(FocusState.Keyboard);
        }

        private async void MTB_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {
            var link = e.Link;
            if (link.StartsWith("#"))
            {
                link = link.Remove(0, 1);
            }
            await Launcher.LaunchUriAsync(new Uri(link, UriKind.Absolute));
        }
    }
}
