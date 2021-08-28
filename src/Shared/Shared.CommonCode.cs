using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

namespace KK_Plugins
{
    internal class CC
    {
        private static int _language = -1;
        /// <summary>
        /// Safely get the language as configured in setup.xml if it exists.
        /// </summary>
        public static int Language
        {
            get
            {
                if (_language == -1)
                {
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load("UserData/setup.xml");
                        XmlElement dataXml = doc.DocumentElement;

                        foreach (var elementObj in dataXml.GetElementsByTagName("Language"))
                        {
                            if (elementObj != null)
                            {
                                var element = (XmlElement)elementObj;
                                _language = int.Parse(element.InnerText);
                                break;
                            }
                        }
                    }
                    catch
                    {
                        _language = 0;
                    }
                    finally
                    {
                        if (_language == -1)
                            _language = 0;
                    }
                }

                return _language;
            }
        }

        /// <summary>
        /// Open explorer focused on the specified file or directory
        /// </summary>
        internal static void OpenFileInExplorer(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException(nameof(filename));

            try { NativeMethods.OpenFolderAndSelectFile(filename); }
            catch (Exception) { Process.Start("explorer.exe", $"/select, \"{filename}\""); }
        }
        internal static class NativeMethods
        {
            /// <summary>
            /// Open explorer focused on item. Reuses already opened explorer windows unlike Process.Start
            /// </summary>
            public static void OpenFolderAndSelectFile(string filename)
            {
                var pidl = ILCreateFromPathW(filename);
                SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
                ILFree(pidl);
            }

            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            private static extern IntPtr ILCreateFromPathW(string pszPath);

            [DllImport("shell32.dll")]
            private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, int cild, IntPtr apidl, int dwFlags);

            [DllImport("shell32.dll")]
            private static extern void ILFree(IntPtr pidl);
        }

        internal static class Paths
        {
            internal static readonly string FemaleCardPath = Path.Combine(UserData.Path, "chara/female/");
            internal static readonly string MaleCardPath = Path.Combine(UserData.Path, "chara/male/");
            internal static readonly string CoordinateCardPath = Path.Combine(UserData.Path, "coordinate/");
#if KKS
            internal static readonly string DefaultFemaleCardPath = Path.Combine(Localize.Translate.Manager.DefaultData.PATH, "chara/female/");
            internal static readonly string DefaultMaleCardPath = Path.Combine(Localize.Translate.Manager.DefaultData.PATH, "chara/male/");
#endif
        }
    }
}
