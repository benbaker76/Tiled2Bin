# Tiled2Bin

## Overview

Tiled2Bin is a command-line utility designed to convert Tiled `.tmx` files into binary files representing tile data. The utility offers options for compression and customization, making it suitable for optimizing map data for various platforms.

## Usage

Tiled2Bin is used via the command line. Below are examples and options for using the tool.

### Example

```bash
Tiled2Bin Level01.tmx
```

This command will output a file named `Level01.bin`, containing the bytes that indicate each tile number in sequence, from the top-left to the bottom-right.

```bash
Tiled2Bin Level01.tmx -512 -zx0
```

This command will output a file named `Level01.bin.zx0`, containing a compressed version of the binary file. The compression uses `zx0`, and a decompressor (in assembly) is included with this archive (see the `z80` folder). This will significantly reduce the size of the map data.

## Arguments

```bash
Usage: Tiled2Bin <filename> [options]
```

### Options

| Option              | Description                                                                                                                                         | Default Value          |
|---------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|------------------------|
| `-h` or `-?`        | Display basic help.                                                                                                                                 | -                      |
| `<filename>`        | The name of the file(s) to process. You can use a specific file name or use wildcards to batch process multiple files (e.g., `*.tmx`).              | -                      |
| `-map-ext=<ext>`    | Sets the file extension for the map output.                                                                                                         | `.bin`                 |
| `-zx0-ext=<ext>`    | Sets the file extension for the `zx0` compressed output.                                                                                            | `.bin.zx0`             |
| `-rle-ext=<ext>`    | Sets the file extension for the `rle` compressed output.                                                                                            | `.bin.rle`             |
| `-512`              | Indicates that the map uses 512 tiles. The default is 256.                                                                                          | `false`                |
| `-blank=<n>`        | Sets the value of what Tiled uses for empty space. If you have empty untiled space, it is advised to set this value.                                | `-1`                   |
| `-zx0`              | Enables `zx0` compression for the resulting binary file.                                                                                            | `false`                |
| `-rle`              | Enables `rle` compression for the resulting binary file.                                                                                            | `false`                |
| `-q`                | Quick non-optimal compression.                                                                                                                      | `false`                |
| `-b`                | Compress backwards.                                                                                                                                 | `false`                |
| `-header`           | Add a map file header.                                                                                                                | `false`                |
| `-split`            | Split the tile ID data and attribute data into separate blocks (must be used with `-512`).                                                          | `false`                |
| `-slice`            | Convert .png to .tmx using TileMap slicer.                                                                                                          | -                      |
| `-tilesize=<n>`     | Set the width and height of each tile to `<n>` (used with `-slice`).                                                                                | `8`                    |
| `-tilesetwidth=<n>` | Set the width of the tileset in tiles (used with `-slice`).                                                                                         | `256`                  |
| `-norepeat`         | No repeating tiles (used with `-slice`).                                                                                                            | `true`                 |
| `-nomirror`         | No mirrored tiles (used with `-slice`).                                                                                                             | `false`                |
| `-norotate`         | No rotating tiles (used with `-slice`).                                                                                                             | `false`                |
| `-insertblanktile`  | Insert a blank tile (used with `-slice`).                                                                                                           | `false`                 |
| `-clearmap=<n>`     | Clear the map data with the specified tile id (used with `-slice`).                                                                                 | -                       |

**Note:** Tiled2Bin does not support layers or base64 encoded tmx files currently.

### Header

Below is a description of the header structure:

| Field           | Description                                                                                                      | Size          |
|-----------------|------------------------------------------------------------------------------------------------------------------|---------------|
| **File Identifier** | The file starts with a 4-byte identifier, `map\0`, represented as an array of characters.                        | 4 bytes       |
| **Version**     | A single byte representing the version of the file format.                                                       | 1 byte        |
| **Number of Layers** | A single byte indicating the number of tile layers in the file.                                                 | 1 byte        |
| **Layer ID**    | A single byte representing the ID of the tile layer.                                                             | 1 byte per layer |
| **Tile Set Name** | A 16-byte ASCII-encoded string representing the tile set name, padded with null characters (`'\0'`) if the name is shorter than 16 characters. | 16 bytes per layer |
| **Layer Attributes** | A single byte representing the attributes of the tile layer.                                                    | 1 byte per layer |
| **Layer Width** | A 2-byte unsigned short representing the width of the tile layer in tiles.                                        | 2 bytes per layer |
| **Layer Height** | A 2-byte unsigned short representing the height of the tile layer in tiles.                                        | 2 bytes per layer |
| **Tile Width**  | A single byte representing the width of each tile in pixels.                                                     | 1 byte per layer |
| **Tile Height** | A single byte representing the height of each tile in pixels.                                                    | 1 byte per layer |
| **Data Length** | A 2-byte unsigned short representing the length of the tile data in bytes.                                       | 2 bytes per layer |
| **Tile Data**   | A variable-length array of bytes representing the actual tile data for the layer.                                | Variable      |

## Additional Notes

- **Tiled2Bin does not support layers or base64 encoded `.tmx` files currently.**
- The filename provided to Tiled2Bin can also use wildcards, such as `*.tmx` allowing for batch processing of all matching files.
- The `z80` folder contains the necessary decompressor (in assembly) for handling `zx0` compressed files.

---

For further assistance, please refer to the help option using `-h` or `-?`.