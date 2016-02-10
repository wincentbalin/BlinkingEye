using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging; // The only part from WPF is the PNG encoder
using NetFwTypeLib;

namespace BlinkingEye
{
    static class PngOptimizerDll
    {
        public enum POChunkOption : uint
        {
            Remove,
            Keep,
            Force
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct POSettings
        {
            public bool BackupOldPngFiles;
            public bool KeepInterlacing;
            public bool IE6Compatible;
            public bool SkipAnimatedGifs;
            public bool KeepFileDate;

            public POChunkOption BkgdOption;
            public int BkgdColor;      // Forced color

            public POChunkOption TextOption;
            public string TextKeyword; // Forced text keyword
            public string TextData;    // Forced text data

            public POChunkOption PhysOption;
            public uint PhysPpmX;      // Forced Pixels per meter X
            public uint PhysPpmY;      // Forced Pixels per meter Y
        };

        [DllImport(@"PngOptimizerDll.dll", EntryPoint = "PO_OptimizeFile")]
        public static extern bool OptimizeFile([MarshalAs(UnmanagedType.LPWStr)]string filePath);

        [DllImport(@"PngOptimizerDll.dll", EntryPoint = "PO_OptimizeFileMem")]
        public static extern bool OptimizeFileMem(byte [] image, int imageSize,
                                                  [Out] byte[] result, int resultCapacity, out int resultSize);

        [DllImport(@"PngOptimizerDll.dll", EntryPoint = "PO_GetLastErrorString")]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static extern string GetLastErrorString();

        [DllImport(@"PngOptimizerDll.dll", EntryPoint = "PO_GetSettings")]
        public static extern bool GetSettings(out POSettings settings);

        [DllImport(@"PngOptimizerDll.dll", EntryPoint = "PO_SetSettings")]
        public static extern bool SetSettings(POSettings settings);
    }

    static class Win32
    {
        // From http://www.pinvoke.net/default.aspx/user32.mouse_event
        public const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        public const uint MOUSEEVENTF_XDOWN = 0x0080;
        public const uint MOUSEEVENTF_XUP = 0x0100;
        public const uint MOUSEEVENTF_WHEEL = 0x0800;
        public const uint MOUSEEVENTF_HWHEEL = 0x1000;
        public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        public const uint MOUSEEVENTF_XBUTTON1 = 0x00000001;
        public const uint MOUSEEVENTF_XBUTTON2 = 0x00000002;

        [DllImport("User32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    }

    static class Event
    {
        private static bool testMode = false;

        public static bool TestMode
        {
            get
            {
                return testMode;
            }
            set
            {
                if (testMode != value)
                    testMode = value;
            }
        }

        private static Point lastPos;

        public static void MouseDown(Dictionary<string, string> p)
        {
            if (!p.ContainsKey("which"))
                return;

            Console.WriteLine("MouseDown");

            int which = Convert.ToInt32(p["which"]);
            uint button = 0;

            switch (which)
            {
                case 0: return; // No button pressed
                case 1: button = Win32.MOUSEEVENTF_LEFTDOWN; break;
                case 2: button = Win32.MOUSEEVENTF_MIDDLEDOWN; break;
                case 3: button = Win32.MOUSEEVENTF_RIGHTDOWN; break;
                default: return; // Unknown button
            };

            Win32.mouse_event(button, 0, 0, 0, UIntPtr.Zero);
        }

        public static void MouseUp(Dictionary<string, string> p)
        {
            if (!p.ContainsKey("which"))
                return;

            Console.WriteLine("MouseUp");

            int which = Convert.ToInt32(p["which"]);
            uint button = 0;

            switch (which)
            {
                case 0: return; // No button released
                case 1: button = Win32.MOUSEEVENTF_LEFTUP; break;
                case 2: button = Win32.MOUSEEVENTF_MIDDLEUP; break;
                case 3: button = Win32.MOUSEEVENTF_RIGHTUP; break;
                default: return; // Unknown button
            };

            Win32.mouse_event(button, 0, 0, 0, UIntPtr.Zero);
        }

        public static void MouseMove(Dictionary<string, string> p)
        {
            if (!p.ContainsKey("x") || !p.ContainsKey("y"))
                return;

            // Some things were taken from https://github.com/T1T4N/NVNC/blob/master/NVNC/Utils/Robot.cs
            string xs = p["x"];
            string ys = p["y"];
            Console.WriteLine("Coords: {0},{1}", xs, ys);
            Console.WriteLine("Coords: {0},{1}", Convert.ToUInt32(xs), Convert.ToUInt32(ys));
            Console.WriteLine("Primary screen bounds: {0}", Screen.PrimaryScreen.Bounds);
            Rectangle primaryScreenBounds = Screen.PrimaryScreen.Bounds;
            int x = Convert.ToInt32(xs) - primaryScreenBounds.Left;
            int y = Convert.ToInt32(ys) - primaryScreenBounds.Top;
            Console.WriteLine("Converted coords: {0},{1}", x, y);

            if (testMode)
            {
                lastPos = new Point(x, y);
            }
            else
            {
                Cursor.Position = new Point(x, y);
            }
        }
    };

    class Program
    {
        private static HttpListener server;

        private static string address = "*";
        private static int port = 3130;
        private static string password = "";

        private static string parentPath;

        private static Rectangle primaryScreenBounds = Screen.PrimaryScreen.Bounds;
        private static Bitmap previousScreenShot = null;
        private static PixelFormat screenShotFormat = PixelFormat.Format24bppRgb;

        private static bool optimizerPresent;

        public Program()
        {
        }

        private static void ContextReceivedCallback(IAsyncResult asyncResult)
        {
            // Get HTTP listener context
            HttpListenerContext context = server.EndGetContext(asyncResult);

            // Start new thread for incoming requests
            server.BeginGetContext(new AsyncCallback(ContextReceivedCallback), null);

            // Process request
            Console.WriteLine("Request for: {0}", context.Request.Url.LocalPath);

            if (context.Request.HttpMethod == "GET") // Output
            {
                string localPath = context.Request.Url.LocalPath;
                byte[] bytes;

                if (localPath.StartsWith(parentPath))
                {
                    string fileName = localPath.Remove(0, parentPath.Length); // Remove password part

                    if (fileName == "")
                        fileName = "index.html";

                    if (fileName == "screen.png")
                        bytes = GetScreen();
                    else if (fileName == "screen-diff.png")
                        bytes = GetScreenDiff();
                    else if (fileName == "screen-size.json")
                        bytes = GetScreenSize();
                    else
                        bytes = GetResource(fileName);

                    if (bytes != null)
                    {
                        context.Response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");
                        context.Response.ContentType = GetContentType(fileName);
                        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                        context.Response.OutputStream.Close();
                    }
                    else
                        WriteError(context, HttpStatusCode.NotFound, "File not found");
                }
                else
                    WriteError(context, HttpStatusCode.NotFound, "File not found");
            }
            else if (context.Request.HttpMethod == "POST") // Input
            {
                Console.WriteLine("Got a POST!");

                // Here mostly from http://stackoverflow.com/questions/5197579/getting-form-data-from-httplistenerrequest
                if (context.Request.HasEntityBody)
                {
                    string postData;
                    using (Stream body = context.Request.InputStream)
                        using (StreamReader reader = new StreamReader(body, context.Request.ContentEncoding))
                            postData = reader.ReadToEnd();

                    // Here mostly from http://stackoverflow.com/questions/19031438/parse-post-parameters-from-httplistener
                    Dictionary<string, string> postParams = new Dictionary<string, string>();
                    foreach (string postParam in postData.Split('&'))
                    {
                        string[] kvPair = postParam.Split('=');
                        postParams.Add(kvPair[0], WebUtility.HtmlDecode(kvPair[1]));
                    }

                    if (postParams.ContainsKey("type")) // We have an event
                    {
                        string type = postParams["type"];

                        if (type == "mousedown")
                        {
                            Event.MouseDown(postParams);
                        }
                        else if (type == "mouseup")
                        {
                            Event.MouseUp(postParams);
                        }
                        else if (type == "mousemove")
                        {
                            Event.MouseMove(postParams);
                        }
                    }
                }

                context.Response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");
                context.Response.ContentType = "application/json";
                context.Response.OutputStream.Close();
            }
        }

        private static byte[] GetResource(string resourceName)
        {
            const string prefix = "BlinkingEye.Data.";
            byte[] bytes = null;

            Assembly assembly = Assembly.GetExecutingAssembly();

            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(prefix + resourceName))
                {
                    int length = (int) stream.Length;
                    bytes = new byte[length];
                    stream.Read(bytes, 0, length);
                }
            }
            catch { } // Ignore all errors

            return bytes;
        }

        private static string GetContentType(string fileName)
        {
            string fn = fileName.ToLower();

            if (fn.EndsWith(".htm") || fn.EndsWith(".html"))
                return "text/html";
            else if (fn.EndsWith(".css"))
                return "text/css";
            else if (fn.EndsWith(".js"))
                return "text/javascript";
            else if (fn.EndsWith(".json"))
                return "application/json";
            else if (fn.EndsWith(".png"))
                return "image/png";
            else
                return "application/octet-stream";
        }

        private static byte[] GetScreen()
        {
            // Copy screen contents to bitmap
            Bitmap currentScreenShot = new Bitmap(primaryScreenBounds.Width,
                                                  primaryScreenBounds.Height,
                                                  screenShotFormat);

            Graphics g = Graphics.FromImage(currentScreenShot);
            g.CopyFromScreen(primaryScreenBounds.X,
                             primaryScreenBounds.Y,
                             0,
                             0,
                             primaryScreenBounds.Size,
                             CopyPixelOperation.SourceCopy);

            // Pack the image into a byte array with PNG format
            byte[] result = null;
            using (MemoryStream stream = new MemoryStream())
            {
                currentScreenShot.Save(stream, ImageFormat.Png);
                result = stream.ToArray();
            }

            // Backup current screenshot
            if (previousScreenShot != null)
                previousScreenShot.Dispose();
            previousScreenShot = currentScreenShot;

            if (optimizerPresent)
                return OptimizePNGByteArray(result);
            else
                return result;
        }

        private static byte[] GetScreenDiff()
        {
            const PixelFormat diffScreenShotFormat = PixelFormat.Format32bppArgb;

            // Copy screen contents to bitmap
            Bitmap currentScreenShot = new Bitmap(primaryScreenBounds.Width,
                                                  primaryScreenBounds.Height,
                                                  screenShotFormat);

            Graphics g = Graphics.FromImage(currentScreenShot);
            g.CopyFromScreen(primaryScreenBounds.X,
                             primaryScreenBounds.Y,
                             0,
                             0,
                             primaryScreenBounds.Size,
                             CopyPixelOperation.SourceCopy);

            // Create difference image
            Bitmap diff = new Bitmap(primaryScreenBounds.Width,
                                     primaryScreenBounds.Height,
                                     diffScreenShotFormat);

            BitmapData cssbd = currentScreenShot.LockBits(primaryScreenBounds, ImageLockMode.ReadOnly, screenShotFormat);
            BitmapData pssbd = previousScreenShot.LockBits(primaryScreenBounds, ImageLockMode.ReadOnly, screenShotFormat);
            BitmapData dssbd = diff.LockBits(primaryScreenBounds, ImageLockMode.ReadWrite, diffScreenShotFormat);

            int cssSize = cssbd.Stride * cssbd.Height;
            int pssSize = pssbd.Stride * pssbd.Height; // should be identical to cssSize
            int dssSize = dssbd.Stride * dssbd.Height; // and this one too

            const int ssbpp = 3; // because of 24 bits per pixel
            const int dssbpp = 4; // because of 32 bits per pixel

            byte[] cssd = new byte[cssSize];
            byte[] pssd = new byte[pssSize];
            byte[] dssd = new byte[dssSize];

            Marshal.Copy(cssbd.Scan0, cssd, 0, cssSize);
            Marshal.Copy(pssbd.Scan0, pssd, 0, pssSize);

            for (int y = 0; y < primaryScreenBounds.Height; y++)
            {
                for (int x = 0; x < primaryScreenBounds.Width; x++)
                {
                    int cssi = y * cssbd.Stride + x * ssbpp;
                    int pssi = y * pssbd.Stride + x * ssbpp;
                    int dssi = y * dssbd.Stride + x * dssbpp;

                    if (cssd[cssi + 0] == pssd[pssi + 0] &&
                        cssd[cssi + 1] == pssd[pssi + 1] &&
                        cssd[cssi + 2] == pssd[pssi + 2]) // Transparent pixels
                    {
                        dssd[dssi + 0] = 0xFF;
                        dssd[dssi + 1] = 0xFF;
                        dssd[dssi + 2] = 0xFF;
                        dssd[dssi + 3] = 0x00;
                    }
                    else // Non-transparent pixels from current screen shot
                    {
                        dssd[dssi + 0] = cssd[cssi + 0];
                        dssd[dssi + 1] = cssd[cssi + 1];
                        dssd[dssi + 2] = cssd[cssi + 2];
                        dssd[dssi + 3] = 0xFF;
                    }
                }
            }

            Marshal.Copy(dssd, 0, dssbd.Scan0, dssd.Length);

            currentScreenShot.UnlockBits(cssbd);
            previousScreenShot.UnlockBits(pssbd);
            diff.UnlockBits(dssbd);

            // Pack the difference image into a byte array with PNG format
            byte[] result = null;
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                diff.Save(stream, ImageFormat.Png);
                result = stream.ToArray();
            }

            // Backup current screenshot
            if (previousScreenShot != null)
                previousScreenShot.Dispose();
            previousScreenShot = currentScreenShot;

            if (optimizerPresent)
                return OptimizePNGByteArray(result);
            else
                return result;
        }

        private static byte[] GetScreenSize()
        {
            string contents = "{ \"width\": " + primaryScreenBounds.Width + ", " +
                               "\"height\": " + primaryScreenBounds.Height + " }";
            return Encoding.UTF8.GetBytes(contents);
        }

        private static void WriteError(HttpListenerContext context, HttpStatusCode statusCode, string message)
        {
            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                context.Response.StatusCode = (int) statusCode;
                context.Response.StatusDescription = message;
                context.Response.OutputStream.Write(messageBytes, 0, messageBytes.Length);
                context.Response.Close();
            }
            catch { } // Ignore all errors
        }

        private static void TestPresenceOfPNGOptimizer()
        {
            try
            {
                string lastError = PngOptimizerDll.GetLastErrorString();
                optimizerPresent = true;
            }
            catch (DllNotFoundException)
            {
                optimizerPresent = false;
            }
        }

        private static byte[] OptimizePNGByteArray(byte[] input)
        {
            byte[] result = new byte[input.Length + 400000]; // As seen in the test app for PNG optimizer DLL
            int resultSize = 0;
            bool optimized = PngOptimizerDll.OptimizeFileMem(input, input.Length, result, result.Length, out resultSize);

            if (optimized && resultSize < input.Length)
            {
                byte[] optimizedPNG = new byte[resultSize];
                Array.Copy(result, optimizedPNG, resultSize);    
                return optimizedPNG;
            }
            else
                return input;
        }

        private static void TestForTestMode()
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testmode.txt")))
                Event.TestMode = true;
        }

        private static void AddFirewallRule()
        {
            // From http://blogs.msdn.com/b/securitytools/archive/2009/08/21/automating-windows-firewall-settings-with-c.aspx
            // and from http://stackoverflow.com/questions/8889587/automating-windows-firewall-with
            Type NetOpenPortType = Type.GetTypeFromCLSID(new Guid("{0CA545C6-37AD-4A6C-BF92-9F7610067EF5}"));
            INetFwOpenPort port = (INetFwOpenPort)Activator.CreateInstance(NetOpenPortType);
            port.Name = "BlinkingEye";
            port.Port = Program.port;

            Type NetFwMgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr", false);
            INetFwMgr mgr = (INetFwMgr)Activator.CreateInstance(NetFwMgrType);

            INetFwOpenPorts ports = (INetFwOpenPorts)mgr.LocalPolicy.CurrentProfile.GloballyOpenPorts;
            ports.Add(port);
        }


        static void Main(string[] args)
        {
            //Console.WriteLine("We have {0} arguments", args.Length);

            /*
            Console.WriteLine("We have these resource names:");
            var assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            foreach (string rn in resourceNames)
                Console.WriteLine("{0}", rn);
            Console.WriteLine();
            */

            // Get settings
            switch (args.Length)
            {
                case 3:
                    address = args[args.Length - 3];
                    port = Convert.ToInt16(args[args.Length - 2]);
                    password = args[args.Length - 1];
                    break;

                case 2:
                    port = Convert.ToInt16(args[args.Length - 2]);
                    password = args[args.Length - 1];
                    break;

                case 1:
                    password = args[args.Length - 1];
                    break;

                default:
                    Console.WriteLine("Usage:\tBlinkingEye [address [port [password]]]");
                    Console.WriteLine("\taddress\tIP address to listen on");
                    Console.WriteLine("\tport\tIP port to listen at");
                    Console.WriteLine("\tpassword\tPassword for this connexion");
                    Environment.Exit(1);
                    break;
            }

            TestPresenceOfPNGOptimizer();
            TestForTestMode();

            parentPath = "/" + password + "/";
            string serverPrefix = String.Format("http://{0}:{1}/{2}/", address, port, password);
            Console.WriteLine("Starting server listening on {0}", serverPrefix);

            AddFirewallRule();

            server = new HttpListener();
            server.Prefixes.Add(serverPrefix);
            server.Start();
            server.BeginGetContext(new AsyncCallback(ContextReceivedCallback), null);

            Console.WriteLine("Press Enter to terminate this server...");
            Console.ReadLine();

            // We could remove the previously added firewall rule here, but for now we will not.
        }
    }
}
