using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace BlinkingEye
{
    class Program
    {
        private Rectangle primaryScreenBounds;
        private Bitmap previousScreenShot;

        public Program()
        {
            primaryScreenBounds = Screen.PrimaryScreen.Bounds;
        }

        void createPngDiff()
        {
            // Copy screen contents to bitmap
            Bitmap currentScreenShot = new Bitmap(primaryScreenBounds.Width,
                                                  primaryScreenBounds.Height,
                                                  PixelFormat.Format32bppArgb);

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
                                     PixelFormat.Format32bppArgb);



            // Encode bitmap to PNG
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            
        }

        static void Main(string[] args)
        {
            // Get settings
            string address = "0.0.0.0";
            int port = 3130;
            string password;

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
                    break;
            }

            Console.WriteLine("Starting server listening on http://{0}:{1}/{2}/", address, port, password);

            // TODO Proceed with many other things
            Console.WriteLine("Press Enter to terminate this server...");
            Console.ReadLine();
        }
    }
}
