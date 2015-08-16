#region Using

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gecko;
using Timer = System.Windows.Forms.Timer;

#endregion

namespace TwitterPhotoDownloader
{

    #region Additional classes

    public class ProgressC
    {
        public int CurrentProgress { get; set; }
        public int MaxProgress { get; set; }
        public int Page { get; set; }
        public int Downloaded { get; set; }
        public ProgressType Type { get; set; }
    }

    public enum ProgressType
    {
        GettingImages,
        DownloadingImages
    }

    #endregion

    public sealed class TwitterDownloader
    {
        public readonly ProgressC Progress;
        public readonly List<string> ErrorsLinks;

        private readonly GeckoWebBrowser _webBrowser;
        private readonly Form _justForm;
        private readonly Timer _loadingTimer;
        private readonly WebClient _webClient;
        private bool _loading;

        public TwitterDownloader()
        {
            try
            {
                Xpcom.Initialize( Application.StartupPath + @"\xulrunner\" );
            }
            catch ( Exception exception )
            {
                throw new Exception( exception.Message );
            }

            this._webBrowser = new GeckoWebBrowser { Dock = DockStyle.Fill };
            this._webClient = new WebClient();
            this.Progress = new ProgressC();
            this._loadingTimer = new Timer { Interval = 6500 };
            this._loadingTimer.Tick += this._loadingTimer_Tick;
            this._loading = false;
            this.ErrorsLinks = new List<string>();

            #region Изврат

            // иначе не работает скролл :(
            this._justForm = new Form();
            this._justForm.Controls.Add( this._webBrowser );

            #endregion
        }

        private void _loadingTimer_Tick( object sender, EventArgs e )
        {
            this._loadingTimer.Stop();
            this._loading = false;
        }

        private async Task WaitForLoadingAsync()
        {
            while ( this._loading )
            {
                await TaskEx.Delay( 500 );
            }
        }

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="fileUrl">URL to file</param>
        /// <param name="savePath">Where to save</param>
        /// <param name="index"></param>
        private async Task DownloadFileAsync( string fileUrl, string savePath, int index )
        {
            fileUrl = fileUrl.Substring( 0, fileUrl.Length - 6 );
            string fileName = savePath + "\\" + index + Path.GetExtension( fileUrl );
            try
            {
                Task task = TaskEx.Run( () => this._webClient.DownloadFileAsync( new Uri( fileUrl ), fileName ) );
                await task;
            }
            catch
            {
                this.ErrorsLinks.Add( fileUrl );
                return;
            }
            this.Progress.Downloaded++;
        }

        /// <summary>
        /// Get photos from account
        /// </summary>
        /// <param name="username">Twitter username</param>
        /// <param name="cancellToken">Cancel token</param>
        /// <returns></returns>
        [STAThread]
        private async Task<List<string>> GetPhotosAsync( string username, CancellationToken cancellToken )
        {
            // loading media page
            var photosUrls = new List<string>();
            this._loading = true;
            this._webBrowser.Navigate( "https://twitter.com/" + username + "/media" );
            this._loadingTimer.Start();
            await this.WaitForLoadingAsync();

            if ( this._webBrowser.Document?.Body == null || this._webBrowser.Window == null )
            {
                return photosUrls;
            }
            int oldHeight = 0;
            int newHeight = 0;
            int page = 0;
            // scrolling down the page until all photos won't be loaded (oldHeight == newHeight)
            do
            {
                try
                {
                    page++;
                    this.Progress.Page = page;
                    oldHeight = this._webBrowser.Document.Body.ScrollHeight;
                    this._webBrowser.Window.ScrollTo( 0, this._webBrowser.Document.Body.ScrollHeight );
                    this._loading = true;
                    this._loadingTimer.Start();
                    await this.WaitForLoadingAsync();

                    newHeight = this._webBrowser.Document.Body.ScrollHeight;
                    Application.DoEvents();
                    if ( cancellToken.IsCancellationRequested )
                    {
                        cancellToken.ThrowIfCancellationRequested();
                    }
                }
                catch
                {
                    continue;
                }
            }
            while ( newHeight > oldHeight );

            // get and click on display content button
            var displayContentElements =
                this._webBrowser.Document.Body.GetElementsByTagName( "button" )
                    .Where(
                        el =>
                            el.GetAttribute( "class" ) != null &&
                            el.GetAttribute( "class" ).Contains( "display-this-media" ) );
            foreach ( GeckoElement displayContentElement in displayContentElements )
            {
                var el = displayContentElement as GeckoHtmlElement;
                el?.Click();
            }

            var elements =
                this._webBrowser.Document.Body.GetElementsByTagName( "img" )
                    .Where(
                        e =>
                            e.GetAttribute( "class" ) == "TwitterPhoto-mediaSource" &&
                            e.GetAttribute( "src" ).Contains( ":large" ) );
            foreach ( GeckoElement element in elements )
            {
                try
                {
                    photosUrls.Add( element.GetAttribute( "src" ) );
                }
                catch
                {
                    continue;
                }
            }
            return photosUrls;
        }

        [STAThread]
        public async Task DownloadPhotosAsync( string username, string savePath, CancellationToken cancellToken )
        {
            if ( savePath[ savePath.Length - 1 ] == '\\' )
            {
                savePath = savePath.Remove( savePath.Length - 1, 1 );
            }
            this.Progress.Downloaded = 0;
            this.Progress.CurrentProgress = 0;
            this.Progress.MaxProgress = 0;
            this.Progress.Type = ProgressType.GettingImages;
            List<string> photosUrls = await this.GetPhotosAsync( username, cancellToken );
            this.Progress.MaxProgress = photosUrls.Count;
            this.Progress.Type = ProgressType.DownloadingImages;
            if ( !Directory.Exists( savePath ) )
            {
                Directory.CreateDirectory( savePath );
            }
            for ( int i = 0; i < photosUrls.Count; i++ )
            {
                if ( cancellToken.IsCancellationRequested )
                {
                    cancellToken.ThrowIfCancellationRequested();
                }
                await this.DownloadFileAsync( photosUrls[ i ], savePath, i + 1 );
                this.Progress.CurrentProgress = i + 1;
                await TaskEx.Delay( 2500, cancellToken );
            }
            photosUrls.Clear();
        }

        #region Static methods

        public static string FixUserName( string url )
        {
            var regex = new Regex( "http(s)?://twitter\\.com/(\\w+)(/media)?" );
            Match match = regex.Match( url );
            return match.Success ? match.Groups[ 2 ].ToString() : url;
        }

        public static bool CheckForInternetConnection()
        {
            try
            {
                using ( var client = new WebClient() )
                {
                    using ( client.OpenRead( "https://twitter.com/" ) )
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    public static class GeckoExtensions
    {
        public static async Task ClearCache( this GeckoWebBrowser webBrowser )
        {
            await TaskEx.Run( () =>
            {
                string profileDir = Xpcom.ProfileDirectory;
                List<string> dirs = Directory.GetDirectories( profileDir ).ToList();
                for ( int i = 0; i < dirs.Count; i++ )
                {
                    string dir = dirs[ i ] + "\\";
                    dirs[ i ] = Path.GetFileName( Path.GetDirectoryName( dir ) );
                }
                Regex regex = new Regex( "^Cache(\\.Trash[0-9]+)?$" );
                string[] dirsForRemove = dirs.Where( d => regex.Match( d ).Success ).ToArray();
                foreach ( string dir in dirsForRemove )
                {
                    Directory.Delete( profileDir + "\\" + dir, true );
                }
            } );
        }
    }
}