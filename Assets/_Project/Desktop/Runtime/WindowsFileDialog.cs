using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WorkplaceStudio.Desktop
{
    public static class WindowsFileDialog
    {
        public static string Open(bool project = false)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            var dialog = new OpenFileName
            {
                size = Marshal.SizeOf<OpenFileName>(), owner = GetActiveWindow(),
                filter = project ? "Training project\0project.json\0\0" : "GLB 2.0 model\0*.glb\0\0",
                file = new StringBuilder(32768), maxFile = 32768,
                title = project ? "Open training project" : "Open local GLB model",
                flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008 | 0x02000000
            };
            return GetOpenFileName(ref dialog) ? dialog.file.ToString() : null;
#else
            throw new PlatformNotSupportedException("This release supports Windows.");
#endif
        }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct OpenFileName
        {
            public int size; public IntPtr owner, instance; public string filter, customFilter; public int maxCustomFilter, filterIndex;
            public StringBuilder file; public int maxFile; public StringBuilder fileTitle; public int maxFileTitle;
            public string initialDirectory, title; public int flags; public short fileOffset, fileExtension;
            public string defaultExtension; public IntPtr customData, hook; public string templateName;
            public IntPtr reserved; public int reservedCount, flagsEx;
        }
        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool GetOpenFileName(ref OpenFileName value);
        [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
#endif
    }
}
