using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Runtime.InteropServices.Marshalling;
using System.Drawing;
using System.Numerics;

namespace Tiled2Bin
{
    partial class Tiled2Bin
    {
        static void Main(string[] args)
        {
            //args = new string[] { @"Level01\*.tmx", "-blank=0", "-zx0" };

            if (args.Length == 0)
            {
                DisplayHelp();
                return;
            }

            List<string> fileList = new List<string>();
            TileMapOptions options = new TileMapOptions();
            TileMapSliceOptions sliceOptions = new TileMapSliceOptions();
            bool sliceMode = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();

                if (arg.StartsWith("-"))
                {
                    if (arg == "-?" || arg == "-h")
                    {
                        DisplayHelp();
                        return;
                    }

                    if (arg.StartsWith("-blank="))
                    {
                        string[] vals = arg.Split('=');

                        if (!Int32.TryParse(vals[1], out options.BlankTileId))
                        {
                            Console.WriteLine("ERROR: Invalid value " + args[i]);
                            return;
                        }
                    }

                    if (arg == "-zx0")
                    {
                        options.CompressZx0 = true;
                    }

                    else if (arg == "-512")
                    {
                        options.Extended512 = true;
                    }

                    if (arg == "-q")
                    {
                        options.QuickMode = true;
                    }

                    if (arg == "-b")
                    {
                        options.BackwardsMode = true;
                    }

                    if (arg == "-rle")
                    {
                        options.CompressRLE = true;
                    }

                    if (arg == "-noheader")
                    {
                        options.NoHeader = true;
                    }

                    if (arg == "-split")
                    {
                        options.Split = true;
                    }

                    if (arg == "-slice")
                    {
                        sliceMode = true;
                    }

                    if (arg.StartsWith("-tilesize="))
                    {
                        string[] vals = arg.Split('=');

                        if (Int32.TryParse(vals[1], out int tileSize))
                        {
                            sliceOptions.TileWidth = tileSize;
                            sliceOptions.TileHeight = tileSize;
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Invalid value " + args[i]);
                            return;
                        }
                    }

                    if (arg == "-norepeat")
                    {
                        sliceOptions.NoRepeat = true;
                    }

                    if (arg == "-nomirror")
                    {
                        sliceOptions.NoMirror = true;
                    }

                    if (arg == "-norotate")
                    {
                        sliceOptions.NoRotate = true;
                    }

                    if (arg == "-insertblanktile")
                    {
                        sliceOptions.InsertBlankTile = true;
                    }

                    if (arg.StartsWith("-map-ext="))
                    {
                        string[] vals = arg.Split('=');
                        options.MapExtension = vals[1];
                    }

                    if (arg.StartsWith("-zx0-ext="))
                    {
                        string[] vals = arg.Split('=');
                        options.Zx0Extension = vals[1];
                    }

                    if (arg.StartsWith("-rle-ext="))
                    {
                        string[] vals = arg.Split('=');
                        options.RleExtension = vals[1];
                    }
                }
                else
                {
                    fileList.Add(args[i]);
                }
            }

            if (fileList.Count == 0)
            {
                Console.WriteLine("ERROR: No files specified.");
                return;
            }

            if (sliceMode)
            {
                foreach (string file in fileList)
                {
                    if (file.Contains("*"))
                    {
                        string[] fileArray = Directory.GetFiles(@".\", file);
                        foreach (string pngFile in fileArray)
                            ProcessPngFile(pngFile, sliceOptions);
                    }
                    else
                    {
                        ProcessPngFile(file, sliceOptions);
                    }
                }
            }
            else
            {
                if (fileList[0].Contains("*"))
                {
                    string[] fileArray = Directory.GetFiles(@".\", fileList[0]);

                    if (fileArray.Length == 0)
                    {
                        Console.WriteLine("ERROR: No files found.");
                        return;
                    }

                    TMXParser.ParseFiles(fileArray, options);
                }
                else
                {
                    TMXParser.ParseFiles(fileList.ToArray(), options);
                }
            }
        }

        static void ProcessPngFile(string pngFile, TileMapSliceOptions sliceOptions)
        {
            string tmxPath = Path.ChangeExtension(pngFile, ".tmx"); // Output .tmx file
            TileMap.CreateTiledTmx(pngFile, sliceOptions);
            Console.WriteLine($"Processed {pngFile} to {tmxPath}");
        }

        static void DisplayHelp()
        {
            Console.WriteLine("Usage: Tiled2Bin <filename> [-512] [-blank=n] [-zx0] [-rle] [-q] [-b] [-noheader] [-split] [-slice] [-tilesize=x] [-norepeat] [-nomirror] [-norotate] [-insertblanktile] [-map-ext=<ext>] [-zx0-ext=<ext>] [-rle-ext=<ext>]\n");
            Console.WriteLine("Options:");
            Console.WriteLine("-h or -?            Display basic help.");
            Console.WriteLine("<filename>          Name of the file(s) to process.");
            Console.WriteLine("                    Use the implicit name for a single file, or use");
            Console.WriteLine("                    wildcards to batch process. ie. *.png");
            Console.WriteLine("-512                The map uses 512 tiles. Default is 256.");
            Console.WriteLine("-blank=<n>          Set the value of what Tiled uses for empty space. By");
            Console.WriteLine("                    default, Tiled will use -1.");
            Console.WriteLine("-zx0                Enable zx0 compression. Default is false.");
            Console.WriteLine("-rle                Enable rle compression. Default is false.");
            Console.WriteLine("-q                  Quick non-optimal compression (zx0 only). Default is false.");
            Console.WriteLine("-b                  Compress backwards (zx0 only). Default is false.");
            Console.WriteLine("-noheader           Don't output the header. Default is true.");
            Console.WriteLine("-split              Split the tile id data and attribute data into separate blocks. Default is false. (must be used with -512).");
            Console.WriteLine("-slice              Convert .png to .tmx using TileMap slicer.");
            Console.WriteLine("-tilesize=<n>       Set the width and height of each tile to <n>. Default is 8 (used with -slice).");
            Console.WriteLine("-norepeat           No repeating tiles. Default is true (used with -slice).");
            Console.WriteLine("-nomirror           No mirrored tiles. Default is true (used with -slice).");
            Console.WriteLine("-norotate           No rotating tiles. Default is false (used with -slice).");
            Console.WriteLine("-insertblanktile    Insert a blank tile. Default is true (used with -slice).");
            Console.WriteLine("-map-ext=<ext>      Set the file extension for the map output. Default is .bin.");
            Console.WriteLine("-zx0-ext=<ext>      Set the file extension for the zx0 compressed output. Default is .bin.zx0.");
            Console.WriteLine("-rle-ext=<ext>      Set the file extension for the rle compressed output. Default is .bin.rle.");
            Console.WriteLine("\nNote: Tile2Bin does not support layers or base64 encoded tmx files currently.");
        }
    }
}