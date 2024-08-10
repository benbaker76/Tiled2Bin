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

This command will output a file named `Level01.map.zx0`, containing a compressed version of the binary file. The compression uses `zx0`, and a decompressor (in assembly) is included with this archive (see the `z80` folder). This will significantly reduce the size of the map data.

## Arguments

```bash
Usage: Tiled2Bin <filename> [options]
```

### Options

| Option              | Description                                                                                                                                         | Default Value          |
|---------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|------------------------|
| `-h` or `-?`        | Display basic help.                                                                                                                                 | -                      |
| `<filename>`        | The name of the file(s) to process. You can use a specific file name or use wildcards to batch process multiple files (e.g., `*.tmx`).              | -                      |
| `-512`              | Indicates that the map uses 512 tiles. The default is 256.                                                                                          | `false`                |
| `-blank=<n>`        | Sets the value of what Tiled uses for empty space. If you have empty untiled space, it is advised to set this value.                                | `-1`                   |
| `-zx0`              | Enables `zx0` compression for the resulting binary file. The compressed file will have a `.zx0` extension.                                          | `false`                |
| `-q`                | Quick non-optimal compression.                                                                                                                      | `false`                |
| `-b`                | Compress backwards.                                                                                                                                 | `false`                |
| `-noheader`         | Suppresses the output of the header.                                                                                                                | `false`                |
| `-split`            | Split the tile ID data and attribute data into separate blocks (must be used with `-512`).                                                          | `false`                |
| `-slice`            | Convert .png to .tmx using TileMap slicer.                                                                                                          | -                      |
| `-tilesize=<n>`     | Set the width and height of each tile to `<n>` (used with `-slice`).                                                                                | `8`                    |
| `-norepeat`         | Disable tile repetition (used with `-slice`).                                                                                                       | `true`                 |
| `-nomirror`         | Disable tile mirroring (used with `-slice`).                                                                                                        | `true`                 |
| `-norotate`         | Disable tile rotation (used with `-slice`).                                                                                                         | `false`                |
| `-insertblanktile`  | Insert a blank tile (used with `-slice`).                                                                                                           | `true`                 |

**Note:** Tiled2Bin does not support layers or base64 encoded tmx files currently.

## Additional Notes

- **Tiled2Bin does not support layers or base64 encoded `.tmx` files currently.**
- The filename provided to Tiled2Bin can also use wildcards, such as `*.tmx` allowing for batch processing of all matching files.
- The `z80` folder contains the necessary decompressor (in assembly) for handling `zx0` compressed files.

---

For further assistance, please refer to the help option using `-h` or `-?`.