using System;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Hourado
{
    public partial class FTakePhoto : Form
    {

        // -------------------- Initilizations --------------------

        public static Capture capture;

        Rectangle rect;

        private Image<Bgr, byte> frame;
        public Image cropedImage;

        public bool operationCanceled, photoTaken;



        // -------------------- FTakePhoto --------------------

        public FTakePhoto()
        {
            InitializeComponent();
        }

        private void FTakePhoto_Load(object sender, EventArgs e)
        {
            try
            {
                rect = new Rectangle((pcbTakePhoto.Width / 2) + 35, (pcbTakePhoto.Height - 20) / 2, pcbTakePhoto.Width - 60, pcbTakePhoto.Height - 20);

                capture.ImageGrabbed += Camera_ImageGrabbed;
                capture.Start();
            }
            catch
            {
                MessageBox.Show("تنظیمات صفحه عکس برداری ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void FTakePhoto_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                capture.Stop();
                capture.Dispose();

                if (!photoTaken)
                    operationCanceled = true;

                Close();
            }
            catch (Exception)
            {
                MessageBox.Show("بستن صفحه عکس برداری ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -------------------- Main Codes --------------------

        private void Camera_ImageGrabbed(object sender, EventArgs e)
        {
            try
            {
                frame = capture.RetrieveBgrFrame();
                frame.Draw(rect, new Bgr(Color.LightGreen), 2);

                pcbTakePhoto.Image = frame.ToBitmap();
            }
            catch (Exception)
            {

            }
        }

        private void btnTry_Click(object sender, EventArgs e)
        {
            try
            {
                btnTakePhoto.Show();
                photoTaken = false;

                capture.Start();
            }
            catch
            {
                MessageBox.Show("عکس برداری مجدد ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnTakePhoto_Click(object sender, EventArgs e)
        {
            try
            {
                capture.Pause();

                System.Threading.Thread.Sleep(100);

                frame.Draw(rect, new Bgr(Color.Transparent), 2);
                pcbTakePhoto.Image = frame.ToBitmap();

                cropedImage = frame.Bitmap.Clone(rect, frame.ToBitmap().PixelFormat);

                btnTakePhoto.Hide();
            }
            catch
            {
                MessageBox.Show("عکس برداری ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                btnTakePhoto_Click(sender, e);
                photoTaken = true;

                Close();
            }
            catch (Exception)
            {
                MessageBox.Show("عکس برداری و ذخیره ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }
        }



        // -------------------- Functions --------------------


        public static int ConfigCapture()
        {
            try
            {
                capture = new Capture(1);
                capture.FlipHorizontal = false;

                return 1;
            }
            catch (Exception)
            {
                MessageBox.Show("تنظیمات دوربین ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return -1;
        }

        private Image ResizeImage(Image image, Size size, bool preserveAspectRatio = true)
        {
            try
            {
                int newWidth, newHeight;

                if (preserveAspectRatio)
                {
                    int originalWidth = image.Width;
                    int originalHeight = image.Height;
                    float percentWidth = (float)size.Width / (float)originalWidth;
                    float percentHeight = (float)size.Height / (float)originalHeight;
                    float percent = percentHeight < percentWidth ? percentHeight : percentWidth;
                    newWidth = (int)(originalWidth * percent);
                    newHeight = (int)(originalHeight * percent);
                }
                else
                {
                    newWidth = size.Width;
                    newHeight = size.Height;
                }

                Image newImage = new Bitmap(newWidth, newHeight);

                using (Graphics graphicsHandle = Graphics.FromImage(newImage))
                {
                    graphicsHandle.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphicsHandle.DrawImage(image, 0, 0, newWidth, newHeight);
                }

                return newImage;
            }
            catch (Exception)
            {
                MessageBox.Show("تغییر سایز عکس ناموفق", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign);
            }

            return null;
        }

    }
}
