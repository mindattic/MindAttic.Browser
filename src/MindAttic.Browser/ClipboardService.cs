using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MindAttic.Browser;

/// <summary>Puts captures on the clipboard so they can be pasted straight into a chat, document or editor.</summary>
internal static class ClipboardService
{
    /// <summary>The image itself (for paste into apps) plus the saved file (for paste into folders and uploaders).</summary>
    public static bool CopyImage(BitmapSource image, string file)
    {
        var data = new DataObject();
        data.SetImage(image);
        data.SetFileDropList(new StringCollection { file });
        return TrySet(data);
    }

    /// <summary>Video can't live on the clipboard as data, so it goes on as a file: paste into a folder, chat or uploader.</summary>
    public static bool CopyFile(string file)
    {
        var data = new DataObject();
        data.SetFileDropList(new StringCollection { file });
        data.SetText(file);
        return TrySet(data);
    }

    /// <summary>Another app can hold the clipboard open for a moment; retry briefly instead of failing.</summary>
    private static bool TrySet(DataObject data)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                Clipboard.SetDataObject(data, copy: true);
                return true;
            }
            catch (COMException)
            {
                Thread.Sleep(50);
            }
        }
        return false;
    }
}
