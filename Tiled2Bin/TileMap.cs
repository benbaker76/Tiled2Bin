using Baker76.Compression;
using Baker76.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Tiled2Bin.TMXParser;

namespace Tiled2Bin
{
    public class TileMapSliceOptions
    {
        public int TileWidth = 8;
        public int TileHeight = 8;
        public int TileSetWidth = 256;
        public bool NoRepeat = false;
        public bool NoMirror = false;
        public bool NoRotate = false;
        public bool InsertBlankTile = false;
        public int PaletteSlot = 0;
        public int ColorCount = 256;
    }

    public class TiledNode
    {
        public int Id;
        public TiledAttributes Attributes;
        
        public TiledNode(int id, TiledAttributes attributes)
        {
            Id = id;
            Attributes = attributes;
        }

        public long ToLong()
        {
            return (long)((long)Attributes << 28) | ((long)Id + 1);
        }
    }

    public enum TileLayerAttributes : byte
    {
        None = 0,
        Extended512 = (1 << 1),
        CompressedZx0 = (1 << 2),
        QuickMode = (1 << 3),
        BackwardsMode = (1 << 4),
        CompressedRLE = (1 << 5),
        Split = (1 << 6),
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TileHeader
    {
        public uint Header;
        public byte Version;
        public byte NumLayers;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct TileLayer
    {
        public byte Id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string TileSet;
        public TileLayerAttributes Attributes;
        public short Width;
        public short Height;
        public byte TileWidth;
        public byte TileHeight;
        public ushort DataLength;
    }

    public class TileMap
    {
        public string Name;
        public short MapWidth;
        public short MapHeight;
        public byte TileWidth;
        public byte TileHeight;
        public ushort[] TileData;

        public TileMap(string name, short mapWidth, short mapHeight, byte tileWidth, byte tileHeight, ushort[] tileData)
        {
            Name = name;
            MapWidth = mapWidth;
            MapHeight = mapHeight;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            TileData = tileData;
        }

        public static async Task<List<TileMap>> ReadBin(string fileName, Palette palette)
        {
            int dataOffset = 0;
            List<TileMap> tileMaps = new List<TileMap>();
            byte[] buffer = File.ReadAllBytes(fileName);

            TileHeader tileHeader = Utility.ToObject<TileHeader>(buffer, 0);
            dataOffset += Marshal.SizeOf<TileHeader>();

            if (tileHeader.Header != 0x70616D)
            {
                Console.WriteLine("Invalid Tile Map Header!");
                return null;
            }

            for (int i = 0; i < tileHeader.NumLayers; i++)
            {
                TileLayer tileLayer = Utility.ToObject<TileLayer>(buffer, dataOffset);

                dataOffset += Marshal.SizeOf<TileLayer>();

                byte[] srcData = new byte[tileLayer.DataLength];
                byte[] dstData = new byte[tileLayer.Width * tileLayer.Height * 2];

                Array.Copy(buffer, dataOffset, srcData, 0, (int)tileLayer.DataLength);

                Zx0.Decompress(srcData, ref dstData);

                ushort[] tileData = new ushort[dstData.Length / 2];

                for (int j = 0; j < tileData.Length; j++)
                    tileData[j] = (ushort)((dstData[j * 2 + 1] << 8) | dstData[j * 2]);

                string suffix = tileLayer.Id > 1 ? "_" + tileLayer.Id.ToString() : "";
                string name = Path.GetFileNameWithoutExtension(fileName) + suffix;

                TileMap tileMap = new TileMap(name, tileLayer.Width, tileLayer.Height, tileLayer.TileWidth, tileLayer.TileHeight, tileData);

                tileMaps.Add(tileMap);

                dataOffset += tileLayer.DataLength;
            }

            return tileMaps;
        }

        public static void WriteBin(string fileName, List<TileLayer> tileLayers, List<byte[]> byteList, int version)
        {
            try
            {
                using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                {
                    using (BinaryWriter writer = new BinaryWriter(fileStream))
                    {
                        writer.Write(new char[] { 'm', 'a', 'p', '\0' });
                        writer.Write((byte)version);
                        writer.Write((byte)tileLayers.Count);

                        for (int i = 0; i < tileLayers.Count; i++)
                        {
                            TileLayer tileLayer = tileLayers[i];
                            writer.Write((byte)tileLayer.Id);
                            writer.Write(Encoding.ASCII.GetBytes(tileLayer.TileSet.PadRight(16, '\0')));
                            writer.Write((byte)tileLayer.Attributes);
                            writer.Write((ushort)tileLayer.Width);
                            writer.Write((ushort)tileLayer.Height);
                            writer.Write((byte)tileLayer.TileWidth);
                            writer.Write((byte)tileLayer.TileHeight);
                            writer.Write((ushort)byteList[i].Length);
                            writer.Write(byteList[i]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static void CreateTiledTmx(string srcImagePath, TileMapSliceOptions options)
        {
            string srcImagePathWithoutExtension = Path.GetFileNameWithoutExtension(srcImagePath);
            string dstTmxPath = srcImagePathWithoutExtension + ".tmx";
            string dstImagePath = srcImagePathWithoutExtension + "Tiles.png";

            Image srcImage = PngReader.Read(srcImagePath);

            if (srcImage == null)
            {
                Console.WriteLine("ERROR: Could not read the PNG file.");
                return;
            }

            if (srcImage.Width % options.TileWidth != 0 || srcImage.Height % options.TileHeight != 0)
            {
                Console.WriteLine("ERROR: The image dimensions are not multiples of the tile size.");
                return;
            }

            int tileCols = srcImage.Width / options.TileWidth;
            int tileRows = srcImage.Height / options.TileHeight;

            List<TiledNode> tiledNodes = new List<TiledNode>();
            List<Image> uniqueTiles = new List<Image>();

            Dictionary<UInt64, TiledNode> tileDictionary = new Dictionary<UInt64, TiledNode>();

            int tileCount = 0;
            int bitsPerPixel = srcImage.BitsPerPixel <= 8 ? 8 : 32;

            if (options.InsertBlankTile)
            {
                Image blankTile = new Image(options.TileWidth, options.TileHeight, bitsPerPixel, srcImage.Palette);
                uniqueTiles.Add(blankTile);

                tileDictionary[blankTile.GetHash()] = new TiledNode(tileCount++, TiledAttributes.None);
            }

            for (int y = 0; y < tileRows; y++)
            {
                for (int x = 0; x < tileCols; x++)
                {
                    Rectangle srcRect = new Rectangle(x * options.TileWidth, y * options.TileHeight, options.TileWidth, options.TileHeight);
                    Image tileImage = new Image(options.TileWidth, options.TileHeight, bitsPerPixel, srcImage.Palette);

                    //grab the tile
                    tileImage.DrawImage(srcImage, new Rectangle(0, 0, tileImage.Width, tileImage.Height), srcRect);

                    //get the hash code of the image
                    UInt64 hash = tileImage.GetHash();

                    if (!tileDictionary.ContainsKey(hash))
                    {
                        int id = tileCount++;

                        if (options.NoRepeat)
                        {
                            tileDictionary.Add(hash, new TiledNode(id, TiledAttributes.None));
                        }

                        if (options.NoMirror)
                        {
                            UInt64 noMirrorHash = tileImage.GetHash(ImageAttributes.MirrorX);

                            if (!tileDictionary.ContainsKey(noMirrorHash))
                                tileDictionary.Add(noMirrorHash, new TiledNode(id, TiledAttributes.Horizontal));

                            noMirrorHash = tileImage.GetHash(ImageAttributes.MirrorY);

                            if (!tileDictionary.ContainsKey(noMirrorHash))
                                tileDictionary.Add(noMirrorHash, new TiledNode(id, TiledAttributes.Vertical));

                            noMirrorHash = tileImage.GetHash(ImageAttributes.MirrorX_Y);

                            if (!tileDictionary.ContainsKey(noMirrorHash))
                                tileDictionary.Add(noMirrorHash, new TiledNode(id, TiledAttributes.Horizontal_Vertical));
                        }

                        if (options.NoRotate)
                        {
                            UInt64 noRotateHash = tileImage.GetHash(ImageAttributes.Rotate | ImageAttributes.MirrorX_Y);

                            if (!tileDictionary.ContainsKey(noRotateHash))
                                tileDictionary.Add(noRotateHash, new TiledNode(id, TiledAttributes.AntiDiagonal | TiledAttributes.Vertical));

                            noRotateHash = tileImage.GetHash(ImageAttributes.Rotate | ImageAttributes.MirrorX);

                            if (!tileDictionary.ContainsKey(noRotateHash))
                                tileDictionary.Add(noRotateHash, new TiledNode(id, TiledAttributes.AntiDiagonal | TiledAttributes.Horizontal_Vertical));

                            noRotateHash = tileImage.GetHash(ImageAttributes.Rotate | ImageAttributes.MirrorY);

                            if (!tileDictionary.ContainsKey(noRotateHash))
                                tileDictionary.Add(noRotateHash, new TiledNode(id, TiledAttributes.AntiDiagonal));

                            noRotateHash = tileImage.GetHash(ImageAttributes.Rotate);

                            if (!tileDictionary.ContainsKey(noRotateHash))
                                tileDictionary.Add(noRotateHash, new TiledNode(id, TiledAttributes.AntiDiagonal | TiledAttributes.Horizontal));
                        }

                        uniqueTiles.Add(tileImage);
                    }

                    tiledNodes.Add(tileDictionary[hash]);
                }
            }

            int tileSetWidth = options.TileSetWidth;

            if (tileSetWidth == 0)
                tileSetWidth = Baker76.Imaging.Utility.CalculateTextureSize(options.TileWidth, options.TileHeight, uniqueTiles.Count);

            int destRows = (uniqueTiles.Count + (tileSetWidth / options.TileWidth) - 1) / (tileSetWidth / options.TileWidth);
            int destCols = (tileSetWidth / options.TileWidth);
            Image pngDst = new Image(tileSetWidth, destRows * options.TileHeight, bitsPerPixel, srcImage.Palette);

            int tileIndex = 0;

            for (int destRow = 0; destRow < destRows; destRow++)
            {
                for (int destCol = 0; destCol < destCols; destCol++)
                {
                    if (tileIndex >= uniqueTiles.Count)
                        break;

                    Image tile = uniqueTiles[tileIndex];
                    int destX = destCol * options.TileWidth;
                    int destY = destRow * options.TileHeight;
                    Rectangle destRect = new Rectangle(destX, destY, tile.Width, tile.Height);
                    pngDst.DrawImage(tile, destRect, new Rectangle(0, 0, tile.Width, tile.Height));
                    tileIndex++;
                }
            }

            PngWriter.Write(dstImagePath, pngDst);

            using (StreamWriter writer = new StreamWriter(dstTmxPath))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                writer.WriteLine("<map version=\"1.10\" tiledversion=\"1.10.2\" orientation=\"orthogonal\" renderorder=\"right-down\" width=\"" + tileCols + "\" height=\"" + tileRows + "\" tilewidth=\"" + options.TileWidth + "\" tileheight=\"" + options.TileHeight + "\" infinite=\"0\" nextlayerid=\"2\" nextobjectid=\"1\">");
                writer.WriteLine(" <tileset firstgid=\"1\" name=\"tiles\" tilewidth=\"" + options.TileWidth + "\" tileheight=\"" + options.TileHeight + "\" tilecount=\"" + uniqueTiles.Count + "\" columns=\"" + destCols + "\">");
                writer.WriteLine("  <image source=\"" + Path.GetFileName(dstImagePath) + "\" width=\"" + pngDst.Width + "\" height=\"" + pngDst.Height + "\"/>");
                writer.WriteLine(" </tileset>");
                writer.WriteLine(" <layer id=\"1\" name=\"Tile Layer 1\" width=\"" + tileCols + "\" height=\"" + tileRows + "\">");
                writer.WriteLine("  <data encoding=\"csv\">");

                for (int i = 0; i < tiledNodes.Count; i++)
                {
                    writer.Write(tiledNodes[i].ToLong().ToString());

                    if (i < tiledNodes.Count - 1)
                        writer.Write(",");
                    else
                        writer.WriteLine();

                    if (i % srcImage.Width == srcImage.Width - 1)
                        writer.WriteLine();
                }

                writer.WriteLine("  </data>");
                writer.WriteLine(" </layer>");
                writer.WriteLine("</map>");
            }
        }

        public static TileAttributes TiledToTileAttributes(TiledAttributes tiledAttributes)
        {
            TileAttributes tileAttributes = TileAttributes.None;
            
            if (tiledAttributes.HasFlag(TiledAttributes.AntiDiagonal))
            {
                if (tiledAttributes.HasFlag(TiledAttributes.Horizontal_Vertical))
                    tileAttributes = TileAttributes.Rotate | TileAttributes.MirrorY;
                else if (tiledAttributes.HasFlag(TiledAttributes.Horizontal))
                    tileAttributes = TileAttributes.Rotate;
                else if (tiledAttributes.HasFlag(TiledAttributes.Vertical))
                    tileAttributes = TileAttributes.Rotate | TileAttributes.MirrorX_Y;
                else
                    tileAttributes = TileAttributes.Rotate | TileAttributes.MirrorX;
            }
            else
            {
                if (tiledAttributes.HasFlag(TiledAttributes.Horizontal_Vertical))
                    tileAttributes = TileAttributes.MirrorX_Y;
                else if (tiledAttributes.HasFlag(TiledAttributes.Horizontal))
                    tileAttributes = TileAttributes.MirrorX;
                else if (tiledAttributes.HasFlag(TiledAttributes.Vertical))
                    tileAttributes = TileAttributes.MirrorY;
            }

            return tileAttributes;
        }
    }
}
