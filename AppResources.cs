using System.Drawing;
using System.IO;
using System.Reflection;

namespace WTF
{
    public static class AppResources
    {
        private static Icon _applicationIcon;
        private static Bitmap _applicationImage;

        public static Icon ApplicationIcon
        {
            get
            {
                if (_applicationIcon == null)
                {
                    _applicationIcon = LoadIcon("WTF.Ressources.wtf.ico");
                }

                return _applicationIcon;
            }
        }

        public static Bitmap ApplicationImage
        {
            get
            {
                if (_applicationImage == null)
                {
                    _applicationImage = LoadBitmap("WTF.Ressources.wtf.png");
                }

                return _applicationImage;
            }
        }

        private static Icon LoadIcon(string resourceName)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                return SystemIcons.Application;
            }

            using (stream)
            {
                return new Icon(stream);
            }
        }

        private static Bitmap LoadBitmap(string resourceName)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                return SystemIcons.Application.ToBitmap();
            }

            using (stream)
            using (Image image = Image.FromStream(stream))
            {
                return new Bitmap(image);
            }
        }
    }
}