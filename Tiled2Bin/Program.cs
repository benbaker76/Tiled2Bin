﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Runtime.InteropServices.Marshalling;
using System.Numerics;
using Baker76.TileMap;
using Baker76.Core.IO;
using Baker76.Imaging;

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
            TileMapOptions tileMapOptions = new TileMapOptions();
            TileMapSliceOptions tileMapSliceOptions = new TileMapSliceOptions();
            bool sliceMode = false;
            string mapExtension = ".bin"; // .map
            string zx0Extension = ".bin.zx0";
            string rleExtension = ".bin.rle";

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

                        if (!Int32.TryParse(vals[1], out tileMapOptions.BlankTileId))
                        {
                            Console.WriteLine("ERROR: Invalid value " + args[i]);
                            return;
                        }
                    }

                    if (arg == "-zx0")
                    {
                        tileMapOptions.CompressZx0 = true;
                    }

                    else if (arg == "-512")
                    {
                        tileMapOptions.Extended512 = true;
                    }

                    if (arg == "-rle")
                    {
                        tileMapOptions.CompressRLE = true;
                    }

                    if (arg == "-q")
                    {
                        tileMapOptions.QuickMode = true;
                    }

                    if (arg == "-b")
                    {
                        tileMapOptions.BackwardsMode = true;
                    }

                    if (arg.StartsWith("-map-ext="))
                    {
                        string[] vals = arg.Split('=');
                        mapExtension = vals[1];
                    }

                    if (arg.StartsWith("-zx0-ext="))
                    {
                        string[] vals = arg.Split('=');
                        zx0Extension = vals[1];
                    }

                    if (arg.StartsWith("-rle-ext="))
                    {
                        string[] vals = arg.Split('=');
                        rleExtension = vals[1];
                    }

                    if (arg == "-header")
                    {
                        tileMapOptions.Header = true;
                    }

                    if (arg == "-split")
                    {
                        tileMapOptions.Split = true;
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
                            tileMapSliceOptions.TileWidth = tileSize;
                            tileMapSliceOptions.TileHeight = tileSize;
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Invalid value " + args[i]);
                            return;
                        }
                    }

                    if (arg.StartsWith("-tilesetwidth="))
                    {
                        string[] vals = arg.Split('=');

                        if (Int32.TryParse(vals[1], out int tileSetWidth))
                        {
                            tileMapSliceOptions.TileSetWidth = tileSetWidth;
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Invalid value " + args[i]);
                            return;
                        }
                    }

                    if (arg == "-norepeat")
                    {
                        tileMapSliceOptions.NoRepeat = true;
                    }

                    if (arg == "-nomirror")
                    {
                        tileMapSliceOptions.NoMirror = true;
                    }

                    if (arg == "-norotate")
                    {
                        tileMapSliceOptions.NoRotate = true;
                    }

                    if (arg == "-insertblanktile")
                    {
                        tileMapSliceOptions.InsertBlankTile = true;
                    }

                    if (arg.StartsWith("-clearmap="))
                    {
                        string[] vals = arg.Split('=');

                        if (Int32.TryParse(vals[1], out int clearMap))
                        {
                            tileMapSliceOptions.ClearMap = clearMap;
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Invalid value " + args[i]);
                            return;
                        }
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
                            TileMap.CreateTiledTmx(pngFile, tileMapSliceOptions);
                    }
                    else
                    {
                        TileMap.CreateTiledTmx(file, tileMapSliceOptions);
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

                    fileList.Clear();
                    fileList.AddRange(fileArray);
                }

                List<IFileSource> fileSources = new List<IFileSource>();

                foreach (var file in fileList)
                    fileSources.Add(new DiskFileSource(file));

                foreach (var file in fileSources)
                {
                    string fileName = file.Name;
                    string binaryFilename = GetBinaryFileName(fileName, mapExtension, rleExtension, zx0Extension, tileMapOptions);

                    using (Stream stream = File.Create(binaryFilename))
                        TileMap.ParseTmx(stream, file, tileMapOptions).Wait();
                }
            }
        }

        public static string GetBinaryFileName(string fileName, string mapExtension, string rleExtension, string zx0Extension, TileMapOptions options)
        {
            string binaryFilename = Path.ChangeExtension(fileName, mapExtension);

            if (options.CompressRLE)
                binaryFilename = Path.ChangeExtension(fileName, rleExtension);

            if (options.CompressZx0)
                binaryFilename = Path.ChangeExtension(fileName, zx0Extension);

            return binaryFilename;
        }

        static void DisplayHelp()
        {
            Console.WriteLine("Usage: Tiled2Bin <filename> [-512] [-blank=n] [-zx0] [-rle] [-q] [-b] [-noheader] [-split] [-slice] [-tilesize=x] [-norepeat] [-nomirror] [-norotate] [-insertblanktile] [-map-ext=<ext>] [-zx0-ext=<ext>] [-rle-ext=<ext>]\n");
            Console.WriteLine("Options:");
            Console.WriteLine("-h or -?            Display basic help.");
            Console.WriteLine("<filename>          Name of the file(s) to process.");
            Console.WriteLine("                    Use the implicit name for a single file, or use");
            Console.WriteLine("                    wildcards to batch process. ie. *.png");
            Console.WriteLine("-map-ext=<ext>      Set the file extension for the map output. Default is .bin.");
            Console.WriteLine("-zx0-ext=<ext>      Set the file extension for the zx0 compressed output. Default is .bin.zx0.");
            Console.WriteLine("-rle-ext=<ext>      Set the file extension for the rle compressed output. Default is .bin.rle.");
            Console.WriteLine("-512                The map uses 512 tiles. Default is 256.");
            Console.WriteLine("-blank=<n>          Set the value of what Tiled uses for empty space. By");
            Console.WriteLine("                    default, Tiled will use -1.");
            Console.WriteLine("-zx0                Enable zx0 compression. Default is false.");
            Console.WriteLine("-rle                Enable rle compression. Default is false.");
            Console.WriteLine("-q                  Quick non-optimal compression (zx0 only). Default is false.");
            Console.WriteLine("-b                  Compress backwards (zx0 only). Default is false.");
            Console.WriteLine("-header             Add a map file header. Default is false.");
            Console.WriteLine("-split              Split the tile id data and attribute data into separate blocks. Default is false. (must be used with -512).");
            Console.WriteLine("-slice              Convert .png to .tmx using TileMap slicer.");
            Console.WriteLine("-tilesize=<n>       Set the width and height of each tile to <n>. Default is 8 (used with -slice).");
            Console.WriteLine("-tilesetwidth=<n>   Set the width of the tileset in tiles. Default is 256 (used with -slice).");
            Console.WriteLine("-norepeat           No repeating tiles. Default is true (used with -slice).");
            Console.WriteLine("-nomirror           No mirrored tiles. Default is false (used with -slice).");
            Console.WriteLine("-norotate           No rotating tiles. Default is false (used with -slice).");
            Console.WriteLine("-insertblanktile    Insert a blank tile. Default is false (used with -slice).");
            Console.WriteLine("-clearmap=<n>       Clear the map data with the specified tile id (used with -slice).");
            Console.WriteLine("\nNote: Tile2Bin does not support layers or base64 encoded tmx files currently.");
        }
    }
}