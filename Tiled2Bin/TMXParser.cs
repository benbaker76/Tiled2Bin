using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Xml;
using Tiled2Bin;
using Baker76.Compression;

namespace Tiled2Bin
{
    public class TileMapOptions
    {
        public int BlankTileId = 0;
        public bool Extended512 = false;
        public bool CompressZx0 = false;
        public bool QuickMode = false;
        public bool BackwardsMode = false;
        public bool CompressRLE = false;
        public bool NoHeader = false;
        public bool Split = false;
    }

    [Flags]
    public enum TileAttributes
    {
        None = 0,
        Rotate = (1 << 1),
        MirrorY = (1 << 2),
        MirrorX = (1 << 3),
        MirrorX_Y = MirrorX | MirrorY,
    };

    [Flags]
    public enum TiledAttributes
    {
        None = 0,
        AntiDiagonal = (1 << 1),
        Vertical = (1 << 2),
        Horizontal = (1 << 3),
        Horizontal_Vertical = Horizontal | Vertical,
    };

    internal class TMXParser
    {
        public static void ParseFiles(string[] fileArray, TileMapOptions options)
        {
            List<TileLayer> tileLayers = new List<TileLayer>();
            List<byte[]> byteList = new List<byte[]>();

            foreach (string fileName in fileArray)
            {
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("ERROR: File Not Found");
                    return;
                }

                string binaryFilename = Path.GetFileNameWithoutExtension(fileName) + ".map";

                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(fileName);

                XmlNodeList tilesetNode = xmlDocument.GetElementsByTagName("tileset");

                int firstGid = int.Parse(tilesetNode[0].Attributes["firstgid"].Value);
                string tileSet = "";

                if (tilesetNode[0].Attributes["source"] != null)
                {
                    tileSet = tilesetNode[0].Attributes["source"].Value;
                }
                else
                {
                    XmlNode imageNode = tilesetNode[0].ChildNodes[0];

                    tileSet = imageNode.Attributes["source"].Value;
                }

                XmlNodeList mapNode = xmlDocument.GetElementsByTagName("map");

                int tileWidth = int.Parse(mapNode[0].Attributes["tilewidth"].Value);
                int tileHeight = int.Parse(mapNode[0].Attributes["tileheight"].Value);

                XmlNodeList layerNode = xmlDocument.GetElementsByTagName("layer");

                string layerID = layerNode[0].Attributes["id"].Value;
                int width = int.Parse(layerNode[0].Attributes["width"].Value);
                int height = int.Parse(layerNode[0].Attributes["height"].Value);
                bool over256 = false;
                bool over512 = false;

                XmlNode dataNode = layerNode[0].ChildNodes[0];

                string encoding = dataNode.Attributes["encoding"].Value.ToLower();
                string dataText = dataNode.InnerText;

                long[] tileData = Array.ConvertAll(dataText.Split(','), long.Parse);

                List<byte> tileBytes = new List<byte>();
                List<byte> attribBytes = new List<byte>();

                for (int i = 0; i < tileData.Length; i++)
                {
                    bool isBlankTile = tileData[i] == -1;
                    int tileId = (int)tileData[i] & 0x1FFFFFFF;
                    TiledAttributes tiledAttributes = (TiledAttributes)(tileData[i] >> 28);

                    TileAttributes tileAttributes = TileAttributes.None;

                    if (options.Extended512)
                    {
                        tileAttributes = TileMap.TiledToTileAttributes(tiledAttributes);

                        if (tileId - firstGid > 512)
                            over512 = true;

                        if (isBlankTile)
                            tileId = options.BlankTileId;

                        tileId -= firstGid;
                        byte attributesByte = (byte)((tileId >> 8) & 1 | (int)tileAttributes);

                        if (options.Split)
                        {
                            tileBytes.Add((byte)tileId);
                            attribBytes.Add(attributesByte);
                        }
                        else
                        {
                            tileBytes.Add((byte)tileId);
                            tileBytes.Add(attributesByte);
                        }
                    }
                    else
                    {
                        if (tileId - firstGid > 255)
                            over256 = true;

                        if (isBlankTile)
                            tileId = options.BlankTileId;

                        tileId -= firstGid;
                        tileId = (isBlankTile ? options.BlankTileId : tileId & 0xFF);

                        tileBytes.Add((byte)tileId);
                    }
                }

                if (options.Split)
                    tileBytes.AddRange(attribBytes);

                if (options.CompressRLE)
                {
                    int oldLength = tileBytes.Count;
                    int bytes = (options.Extended512 && !options.Split ? 2 : 1);
                    tileBytes = new List<byte>(Rle.Write(tileBytes.ToArray(), tileBytes.Count / bytes, bytes));

                    Console.WriteLine($"RLE: {oldLength} -> {tileBytes.Count}");
                }

                if (options.CompressZx0)
                {
                    int size = 0;
                    int oldLength = tileBytes.Count;
                    tileBytes = new List<byte>(Zx0.Compress(tileBytes.ToArray(), options.QuickMode, options.BackwardsMode, out size));

                    Console.WriteLine($"ZX0: {oldLength} -> {tileBytes.Count}");
                }

                TileLayerAttributes layerAttributes = TileLayerAttributes.None;

                if (options.CompressZx0)
                    layerAttributes |= TileLayerAttributes.CompressedZx0;

                if (options.Extended512)
                    layerAttributes |= TileLayerAttributes.Extended512;

                if (options.QuickMode)
                    layerAttributes |= TileLayerAttributes.QuickMode;

                if (options.BackwardsMode)
                    layerAttributes |= TileLayerAttributes.BackwardsMode;

                if (options.CompressRLE)
                    layerAttributes |= TileLayerAttributes.CompressedRLE;

                if (options.Split)
                    layerAttributes |= TileLayerAttributes.Split;

                TileLayer tileLayer = new TileLayer
                {
                    Id = byte.Parse(layerID),
                    TileSet = Path.GetFileNameWithoutExtension(tileSet),
                    Attributes = layerAttributes,
                    Width = (short)width,
                    Height = (short)height,
                    TileWidth = (byte)tileWidth,
                    TileHeight = (byte)tileHeight,
                    DataLength = (ushort)tileBytes.Count,
                };

                byteList.Add(tileBytes.ToArray());

                tileLayers.Add(tileLayer);

                if (over256)
                    Console.WriteLine($"WARNING: {Path.GetFileName(fileName)} - Tile values greater than 255 found.");

                if (over512)
                    Console.WriteLine($"WARNING: {Path.GetFileName(fileName)} - Tile values greater than 511 found.");
            }

            if (options.NoHeader)
            {
                for (int i = 0; i < fileArray.Length; i++)
                {
                    string binaryFilename = Path.GetFileNameWithoutExtension(fileArray[i]) + ".map";

                    File.WriteAllBytes(binaryFilename, byteList[i]);
                }
            }
            else
            {
                string binaryFilename = Path.GetFileNameWithoutExtension(fileArray[0]) + ".map";

                TileMap.WriteBin(binaryFilename, tileLayers, byteList, 0);
            }
        }
    }
}