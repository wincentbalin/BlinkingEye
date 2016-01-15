using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace BlinkingEye
{
    class Program
    {
        private static HttpListener server;
        private Rectangle primaryScreenBounds;
        private Bitmap previousScreenShot;

        public Program()
        {
            primaryScreenBounds = Screen.PrimaryScreen.Bounds;
        }

        private static void ContextReceivedCallback(IAsyncResult asyncResult)
        {
            HttpListenerContext context = server.EndGetContext(asyncResult);


        }

        void createPngDiff()
        {
            const PixelFormat screenShotFormat = PixelFormat.Format24bppRgb;
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

            BitmapData cssbd = currentScreenShot.LockBits(primaryScreenBounds, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData pssbd = previousScreenShot.LockBits(primaryScreenBounds, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dssbd = diff.LockBits(primaryScreenBounds, ImageLockMode.ReadWrite, diffScreenShotFormat);

            int cssSize = cssbd.Stride * cssbd.Height;
            int pssSize = pssbd.Stride * pssbd.Height; // should be identical to cssSize
            int dssSize = dssbd.Stride * dssbd.Height; // and this one too

            const int ssbpp = 3; // because of 24 bits per pixel
            const int dssbpp = 4; // because of 32 bits per pixel

            byte[] cssd = new byte[cssSize];
            byte[] pssd = new byte[pssSize];
            byte[] dssd = new byte[dssSize];

            System.Runtime.InteropServices.Marshal.Copy(cssbd.Scan0, cssd, 0, cssSize);
            System.Runtime.InteropServices.Marshal.Copy(pssbd.Scan0, pssd, 0, pssSize);

            for (int y = 0; y < primaryScreenBounds.Height; y++)
            {
                for (int x = 0; x < primaryScreenBounds.Width; x++)
                {
                    int cssi = y * cssbd.Stride + x * ssbpp;
                    int pssi = y * pssbd.Stride + x * ssbpp;
                    int dssi = y * dssbd.Stride + x * dssbpp;

                    if (cssd[cssi + 0] == pssd[pssi + 0] &&
                        cssd[cssi + 1] == pssd[pssi + 1] &&
                        cssd[cssi + 3] == pssd[pssi + 2])
                    {
                        dssd[dssi + 0] = 0xFF;
                        dssd[dssi + 1] = 0xFF;
                        dssd[dssi + 2] = 0xFF;
                        dssd[dssi + 3] = 0x00;
                    }
                    else
                    {
                        dssd[dssi + 0] = pssd[pssi + 0];
                        dssd[dssi + 1] = pssd[pssi + 1];
                        dssd[dssi + 2] = pssd[pssi + 2];
                        dssd[dssi + 3] = 0xFF;
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(dssd, 0, dssbd.Scan0, dssd.Length);

            currentScreenShot.UnlockBits(cssbd);
            previousScreenShot.UnlockBits(pssbd);
            diff.UnlockBits(dssbd);

            // Encode bitmap to PNG
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            
            // Backup current screenshot
            previousScreenShot = currentScreenShot;
        }

        static void Main(string[] args)
        {
            // Get settings
            string address = "0.0.0.0";
            int port = 3130;
            string password = "";

            //Console.WriteLine("We have {0} arguments", args.Length);

            switch (args.Length)
            {
                case 3:
                    address = args[args.Length - 3];
                    port = Convert.ToInt32(args[args.Length - 2]);
                    password = args[args.Length - 1];
                    break;

                case 2:
                    port = Convert.ToInt32(args[args.Length - 2]);
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

            String serverPrefix = String.Format("http://{0}:{1}/{2}/", address, port, password);
            Console.WriteLine("Starting server listening on {0}", serverPrefix);

            server = new HttpListener();
            server.Prefixes.Add(serverPrefix);
            server.Start();

            // TODO Proceed with many other things
            Console.WriteLine("Press Enter to terminate this server...");
            Console.ReadLine();
        }
    }
}
