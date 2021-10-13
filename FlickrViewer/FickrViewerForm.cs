// Fig. 23.4: FickrViewerForm.cs
// Invoking a web service asynchronously with class HttpClient
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace FlickrViewer
{
    public partial class FickrViewerForm : Form
    {
        // Use your Flickr API key here--you can get one at:
        // https://www.flickr.com/services/apps/create/apply
        private const string KEY = "d91899727b85da9041c2d2a1d1c171c0";

        // object used to invoke Flickr web service      
        private static HttpClient flickrClient = new HttpClient();

        Task<string> flickrTask = null; // Task<string> that queries Flickr

        public FickrViewerForm()
        {
            InitializeComponent();
        }

        // initiate asynchronous Flickr search query; 
        // display results when query completes
        private async void searchButton_Click(object sender, EventArgs e)
        {
            //if flickrTask already running, prompt user
            if (flickrTask?.Status != TaskStatus.RanToCompletion)
            {
                var result = MessageBox.Show(
                   "Cancel the current Flickr search?",
                   "Are you sure?", MessageBoxButtons.YesNo,
                   MessageBoxIcon.Question);

                //determine whether user wants to cancel prior search
                if (result == DialogResult.No)
                {
                    return;
                }
                else
                {

                    flickrClient.CancelPendingRequests(); // cancel search
                }
            }

            // Flickr's API URL for searches                         
            var flickrURL = "https://api.flickr.com/services/rest/?method=" +
            $"flickr.photos.search&api_key={KEY}&" +
            $"tags={inputTextBox.Text.Replace(" ", ",")}" +
            "&tag_mode=all&per_page=500&privacy_filter=1";

            imagesListBox.DataSource = null; // remove prior data source
            imagesListBox.Items.Clear(); // clear imagesListBox
            pictureBox.Image = null; // clear pictureBox
            imagesListBox.Items.Add("Loading..."); // display Loading...

            // invoke Flickr web service to search Flick with user's tags
            flickrTask = flickrClient.GetStringAsync(flickrURL);

            // await flickrTask then parse results with XDocument and LINQ
            XDocument flickrXML = XDocument.Parse(await flickrTask);

            // gather information on all photos
            var flickrPhotos =
               from photo in flickrXML.Descendants("photo")
               let id = photo.Attribute("id").Value
               let title = photo.Attribute("title").Value
               let secret = photo.Attribute("secret").Value
               let server = photo.Attribute("server").Value
               let farm = photo.Attribute("farm").Value
               select new FlickrResult
               {
                   Title = title,
                   URL = $"https://farm{farm}.staticflickr.com/" +
                     $"{server}/{id}_{secret}.jpg",
                   SAVE = $"{ id }_{ secret }.jpg", // Save Original image file name
                   RESIZE = $"{ id }_{ secret }_resize.jpg" // Save Tumbnail image file name
               };
            imagesListBox.Items.Clear(); // clear imagesListBox

            // set ListBox properties only if results were found
            if (flickrPhotos.Any())
            {
                imagesListBox.DataSource = flickrPhotos.ToList();
                imagesListBox.DisplayMember = "Title";
            }
            else // no matches were found
            {
                imagesListBox.Items.Add("No matches");
            }
        }

        // display selected image
        private async void imagesListBox_SelectedIndexChanged(
           object sender, EventArgs e)
        {
            // Thumbnail size
            int width = 250;

            if (imagesListBox.SelectedItem != null)
            {
                string selectedURL = ((FlickrResult)imagesListBox.SelectedItem).URL;
                byte[] imageBytes = await flickrClient.GetByteArrayAsync(selectedURL);

                // Save original image file name
                string imageFileName = ((FlickrResult)imagesListBox.SelectedItem).SAVE;
                // Save thumbnail image file name
                string resizeFileName = ((FlickrResult)imagesListBox.SelectedItem).RESIZE;

                Parallel.Invoke(
                ()=>
                {
                    // 1st Display the selected image in the pictureBox
                    using (var memoryStream = new MemoryStream(imageBytes))
                    {
                        pictureBox.Image = Image.FromStream(memoryStream);
                    }
                },
                () =>
                {
                    // 2nd Save the selected image locally 
                    // The image is saved under here, please check this path
                    //(FlickrViewer\bin\Debug\net5.0-windows\)
                    SaveOriginalImage(imageBytes, imageFileName);
                },
                () =>
                {
                    // 3rd Create ThumbNail based on width(250)
                    // The image is saved under here, please check this path
                    //(FlickrViewer\bin\Debug\net5.0-windows\)
                    //Thread.Sleep(2000);
                    CreateSaveThumbNail(imageBytes, width, resizeFileName);
                });
            }
        }

        // Original size method
        public void SaveOriginalImage(byte[] imageBytes, string imageFileName)
        {
            using (var inStream = new MemoryStream(imageBytes))
            using (var outStream = new MemoryStream())
            {
                var imageStream = Image.FromStream(inStream);
                imageStream.Save(outStream, ImageFormat.Jpeg);
                File.WriteAllBytes(imageFileName, outStream.ToArray());
                MessageBox.Show("The Image is Saved!!");
            }
        }

        // Resize method
        // Calculate the height based off of the width
        public void CreateSaveThumbNail(byte[] imageBytes, int width, string resizeFileName)
        {
            using (var stream = new MemoryStream(imageBytes))
            {
                var image = Image.FromStream(stream);
                var height = (width * image.Height) / image.Width;
                var thumbnail = image.GetThumbnailImage(width, height, null, IntPtr.Zero);

                using (var thumbnailStream = new MemoryStream())
                {
                    thumbnail.Save(thumbnailStream, ImageFormat.Jpeg);
                    File.WriteAllBytes(resizeFileName, thumbnailStream.ToArray());
                    MessageBox.Show("The Thumbnail Image is Saved!!");
                }
            }
        }

    }
}


