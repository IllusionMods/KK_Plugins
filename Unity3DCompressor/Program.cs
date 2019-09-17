using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Unity3DCompressor
{
    internal class Program
    {
        private static bool CABRandomization = false;
        private static readonly RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

        private static void Main(string[] args)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// Drag and drop this .txt file on to SB3UtilityScript.exe");
            sb.AppendLine("// Get SB3Utility from https://github.com/enimaroah/SB3Utility/releases");
            sb.AppendLine();
            sb.AppendLine("LoadPlugin(PluginDirectory+\"UnityPlugin.dll\")");
            sb.AppendLine();

            bool processedFile = false;

            foreach (string path in args)
            {
                if (File.Exists(path) && FileIsAssetBundle(path))
                {
                    sb.Append(ProcessFile(path));
                    processedFile = true;
                }
                else if (Directory.Exists(path))
                {
                    var allfiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories).Where(FileIsAssetBundle);
                    foreach(var x in allfiles)
                    {
                        sb.Append(ProcessFile(x));
                        processedFile = true;
                    }
                }
            }

            if (processedFile)
            {
                Console.WriteLine("Press any key to generate output.txt");
                Console.ReadKey();

                File.WriteAllText(@"output.txt", sb.ToString());
            }
            else
            {
                Console.WriteLine("Drag and drop a .unity3d file or a directory containing .unity3d files.");
                Console.WriteLine("Press any key to close.");
                Console.ReadKey();
            }
        }

        private static string ProcessFile(string path)
        {
            StringBuilder sb = new StringBuilder();

            var rnbuf = new byte[16];
            rng.GetBytes(rnbuf);
            string CAB = "CAB-" + string.Concat(rnbuf.Select((x) => ((int)x).ToString("X2")).ToArray()).ToLower();

            sb.AppendLine($"Log(\"Compressing: {path}\")");
            sb.AppendLine($"unityParser4 = OpenUnity3d(path=\"{path}\")");
            sb.AppendLine("unityEditor4 = Unity3dEditor(parser=unityParser4)");
            sb.AppendLine("unityEditor4.GetAssetNames(filter=True)");
            if (CABRandomization)
                sb.AppendLine($"unityEditor4.RenameCabinet(cabinetIndex=0, name=\"{CAB}\")");
            sb.AppendLine("unityEditor4.SaveUnity3d(keepBackup=False, backupExtension=\".unit-y3d\", background=False, clearMainAsset=True, pathIDsMode=-1, compressionLevel=2, compressionBufferSize=262144)");
            sb.AppendLine();
            return sb.ToString();
        }

        private static bool FileIsAssetBundle(string path)
        {
            byte[] buffer = new byte[7];
            using(FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fs.Read(buffer, 0, buffer.Length);
                fs.Close();
            }
            return Encoding.UTF8.GetString(buffer, 0, buffer.Length) == "UnityFS";
        }
    }
}
