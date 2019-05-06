using System;
using System.IO;
using System.Drawing;


namespace PngToPldms
{
    class Program
    {
        private static byte VERSION_MAJOR = 1;
        private static byte VERSION_MINOR = 0;

        static void Main(string[] args)
        {
            string path;
            Color[,] pixels = null;
            Bitmap bmp = null;
            if (args.Length != 1)
            {
                Console.WriteLine("Error: Accepts only one argument");
                return;
            }

            try
            {
                path = Path.GetFullPath(args[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: Invalid path");
                return;
            }

            try
            {
                bmp = new Bitmap(path);
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: Could not open file");
                return;
            }

            using (BinaryWriter bw = new BinaryWriter(new FileStream(@"structure.pldms", FileMode.Create)))
            {
                bw.Write((byte)0xCA);
                bw.Write((byte)0xBD);
                bw.Write((byte)0xAD);

                bw.Write(VERSION_MAJOR);
                bw.Write(VERSION_MINOR);

                bw.Write((ushort)bmp.Width);
                bw.Write((ushort)bmp.Height);

                int color;
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        color = bmp.GetPixel(x, y).ToArgb();

                        if (color == Convert.ToInt32("FF00FF00", 16)) //Ground
                        {
                            bw.Write((ushort)1);
                            bw.Write((byte)0);
                        }
                        else if (color == Convert.ToInt32("FF007700", 16)) //Grass
                        {
                            bw.Write((ushort)1);
                            bw.Write((byte)1);
                        }
                        else if (color == Convert.ToInt32("FFBF7FBF", 16)) //Flowers
                        {
                            bw.Write((ushort)1);
                            bw.Write((byte)2);
                        }
                        else if (color == Convert.ToInt32("FF7F7F7F", 16)) //Stone
                        {
                            bw.Write((ushort)2);
                            bw.Write((byte)0);
                        }
                        else if (color == Convert.ToInt32("FF7FBF7F", 16)) //Mossy Stone
                        {
                            bw.Write((ushort)2);
                            bw.Write((byte)1);
                        }
                        else if (color == Convert.ToInt32("FFFFFF00", 16)) //Sandy Path
                        {
                            bw.Write((ushort)3);
                            bw.Write((byte)0);
                        }
                        else if (color == Convert.ToInt32("FFBFBFDF", 16)) //Gravel Path
                        {
                            bw.Write((ushort)3);
                            bw.Write((byte)1);
                        }
                        else if (color == Convert.ToInt32("FF00FFFF", 16)) //Water
                        {
                            bw.Write((ushort)4);
                            bw.Write((byte)0);
                        }
                        else if (color == Convert.ToInt32("FF00BFBF", 16)) //Wavy Water
                        {
                            bw.Write((ushort)4);
                            bw.Write((byte)1);
                        }
                        else if (color == Convert.ToInt32("FFFF7F7F", 16)) //Bricks
                        {
                            bw.Write((ushort)5);
                            bw.Write((byte)0);
                        }
                        else if (color == Convert.ToInt32("FFBFBF7F", 16)) //Wood Flooring
                        {
                            bw.Write((ushort)6);
                            bw.Write((byte)0);
                        }
                        else //Nothing/Everything else
                        {
                            bw.Write((ushort)0);
                            bw.Write((byte)0);
                        }
                    }
                }
            }

            Console.WriteLine("Successfully created Playerdom Structure - structure.pldm");
            Console.Write("Press any key to continue . . .");
            Console.ReadKey();
        }
    }
}
