﻿/*
 * Copyright (C) 2011  pleoNeX
 *
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>. 
 *
 * By: pleoNeX
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using PluginInterface;
using PluginInterface.Images;

namespace AI_IGO_DS
{
    public class BIN
    {
        IPluginHost pluginHost;
        int id;

        bool hasMap;
        MapBase[] maps;
        ImageBase image;
        PaletteBase palette;

        public BIN(IPluginHost pluginHost, string file, int id) 
        {
            this.pluginHost = pluginHost;
            this.id = id;

            Read(file);
        }

        public Image Get_Image(int index)
        {
            if (hasMap)
                return maps[index].Get_Image(image, palette);
            else
                return image.Get_Image(palette);
        }

        public void Read(string file)
        {
            BinaryReader br = new BinaryReader(File.OpenRead(file));

            // Header 
            uint paletaOffset = br.ReadUInt32() * 4;
            uint tileOffset = br.ReadUInt32() * 4;
            uint mapOffset = br.ReadUInt32() * 4;
            ColorFormat depth;
            br.BaseStream.Position = mapOffset;
            if (br.ReadUInt32() == 0x01)
                depth = ColorFormat.colors256;
            else
                depth = ColorFormat.colors16;

            // Palette
            Color[][] colors;
            if (paletaOffset == 0x00) // No palette
            {
                depth = ColorFormat.colors16;
                colors = new Color[defaultPaletteData.Length / 0x20][];

                for (int i = 0; i < colors.Length; i++)
                {
                    Byte[] data = new Byte[0x20];
                    Array.Copy(defaultPaletteData, i * 0x20, data, 0, 0x20);
                    colors[i] = pluginHost.BGR555ToColor(data);
                }
                goto Tile;
            }

            br.BaseStream.Position = paletaOffset;
            uint pCabeceraSize = br.ReadUInt32() * 4;
            uint pSize = br.ReadUInt32() * 4;
            if (pSize - 0x08 == 0x0200)
                depth = ColorFormat.colors256;
            else if (pSize - 0x08 == 0x20)
                depth = ColorFormat.colors16;

            colors = new Color[depth == ColorFormat.colors16 ? (pSize - 0x08) / 0x20 : 1][];
            uint pal_length = (depth == ColorFormat.colors16) ? 0x20 : pSize - 0x08;
            for (int i = 0; i < colors.Length; i++)
                colors[i] = pluginHost.BGR555ToColor(br.ReadBytes((int)pal_length));
            
            // Tile data
            Tile:
            br.BaseStream.Position = tileOffset;
            uint tCabeceraSize = br.ReadUInt32() * 4;
            uint tSize = br.ReadUInt32() * 4;
            byte[] tiles = br.ReadBytes((int)(tSize - 0x08));
            image = new RawImage(pluginHost, tiles, TileForm.Horizontal, depth, 0x40, tiles.Length / 0x40, false);

            // Map
            if (mapOffset == 0x00)
            {
                hasMap = false;
                goto End;
            }

            hasMap = true;
            br.BaseStream.Position = mapOffset;
            uint mCabeceraSize = br.ReadUInt32() * 4;
            uint[] mSize = new uint[(int)mCabeceraSize / 4];
            for (int i = 0; i < mSize.Length; i++)
                mSize[i] = (br.ReadUInt32() * 4) - mCabeceraSize - 4;

            maps = new MapBase[mSize.Length];
            for (int i = 0; i < maps.Length; i++)
            {
                ushort width = (ushort)(br.ReadUInt16() * 8);
                ushort height = (ushort)(br.ReadUInt16() * 8);

                NTFS[] map;
                if (i != 0)
                    map = new NTFS[((mSize[i] - mSize[i - 1]) - 4) / 2];
                else
                    map = new NTFS[(mSize[i] - 4) / 2];

                for (int j = 0; j < map.Length; j++)
                    map[j] = pluginHost.MapInfo(br.ReadUInt16());

                maps[i] = new RawMap(pluginHost, map, width, height, false);
            }

            End:
            br.Close();

            palette = new RawPalette(pluginHost, colors, false, depth);
        }

        public Size Get_Size(int index)
        {
            if (hasMap)
                return new Size(maps[index].Width, maps[index].Height);
            else
                return new Size(image.Width, image.Height);
        }
        public void Set_Size(Size size, int index)
        {
            maps[index].Width = size.Width;
            maps[index].Height = size.Height;
        }
        public int NumImages
        {
            get
            {
                if (hasMap) return maps.Length;
                else return 1;
            }
        }
        public Color[] Get_Palette
        {
            get { return palette.Palette[0]; }
        }

        public static byte[] defaultPaletteData = {
	0xE0, 0x7F, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x01, 0x00, 0x21, 0x00,
	0x20, 0x00, 0x20, 0x00, 0x21, 0x00, 0x41, 0x00, 0x40, 0x00, 0x60, 0x00,
	0x60, 0x00, 0x41, 0x00, 0x42, 0x00, 0x61, 0x00, 0x80, 0x00, 0x43, 0x04,
	0x45, 0x00, 0x81, 0x00, 0x63, 0x00, 0xA0, 0x00, 0x82, 0x04, 0xA1, 0x00,
	0xC0, 0x00, 0x84, 0x04, 0x47, 0x00, 0xA2, 0x00, 0x65, 0x04, 0xC1, 0x00,
	0x49, 0x00, 0xE0, 0x00, 0xA3, 0x00, 0xC2, 0x04, 0xA5, 0x04, 0xE2, 0x00,
	0x87, 0x04, 0x20, 0x01, 0x89, 0x04, 0xC4, 0x04, 0xE3, 0x04, 0x02, 0x01,
	0x20, 0x01, 0xC6, 0x04, 0x03, 0x09, 0x8B, 0x04, 0xE5, 0x04, 0x40, 0x01,
	0xC8, 0x04, 0x23, 0x05, 0x42, 0x05, 0x04, 0x05, 0x60, 0x01, 0x8D, 0x08,
	0xE7, 0x04, 0x80, 0x01, 0xE9, 0x04, 0x43, 0x05, 0x25, 0x05, 0x80, 0x01,
	0xCC, 0x08, 0x64, 0x05, 0xA0, 0x01, 0x82, 0x05, 0x28, 0x05, 0xC0, 0x01,
	0xC0, 0x01, 0x66, 0x05, 0xEE, 0x08, 0x0C, 0x09, 0xE0, 0x01, 0xD0, 0x08,
	0xC2, 0x05, 0xA5, 0x05, 0x68, 0x09, 0x00, 0x02, 0x4B, 0x09, 0x2E, 0x09,
	0x20, 0x02, 0xA8, 0x05, 0xE5, 0x05, 0x40, 0x02, 0x6D, 0x0D, 0x31, 0x0D,
	0x8B, 0x09, 0x60, 0x02, 0x43, 0x02, 0x6F, 0x0D, 0x80, 0x02, 0xCA, 0x09,
	0x26, 0x06, 0xAD, 0x0D, 0x36, 0x0D, 0x08, 0x06, 0xA0, 0x02, 0xC0, 0x02,
	0x92, 0x0D, 0xCF, 0x0D, 0xE0, 0x02, 0x0D, 0x0A, 0x66, 0x06, 0x94, 0x11,
	0x4B, 0x06, 0x00, 0x03, 0x89, 0x0A, 0x99, 0x11, 0xF3, 0x0D, 0x30, 0x0E,
	0xE7, 0x0A, 0xD7, 0x11, 0x8D, 0x0E, 0xA0, 0x03, 0x91, 0x06, 0xEC, 0x06,
	0xDD, 0x15, 0x39, 0x16, 0x48, 0x0B, 0x75, 0x12, 0x3F, 0x1A, 0xF5, 0x0E,
	0xBA, 0x0E, 0xC9, 0x17, 0x92, 0x13, 0x59, 0x17, 0xFF, 0x1E, 0xF8, 0x1B,
	0x9F, 0x17, 0xFF, 0x23, 0xE0, 0x7F, 0x03, 0x18, 0x03, 0x20, 0x04, 0x24,
	0x04, 0x2C, 0x05, 0x28, 0x04, 0x34, 0x05, 0x30, 0x04, 0x38, 0x05, 0x38,
	0x04, 0x40, 0x04, 0x48, 0x05, 0x4C, 0x8C, 0x31, 0x39, 0x67, 0xFF, 0x7F,
	0xE0, 0x7F, 0x65, 0x24, 0xC8, 0x2C, 0x0A, 0x31, 0x4C, 0x39, 0x8E, 0x41,
	0xCF, 0x45, 0x11, 0x4E, 0x53, 0x52, 0x95, 0x5A, 0xD7, 0x62, 0x19, 0x67,
	0x5A, 0x6F, 0x9C, 0x73, 0xDE, 0x7B, 0xFF, 0x7F, 0x04, 0xE5, 0x04, 0x40,
    0xE0, 0x7F, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x01, 0x00, 0x21, 0x00,
	0x20, 0x00, 0x20, 0x00, 0x21, 0x00, 0x41, 0x00, 0x40, 0x00, 0x60, 0x00,
	0x60, 0x00, 0x41, 0x00, 0x42, 0x00, 0x61, 0x00, 0x80, 0x00, 0x43, 0x04,
	0x45, 0x00, 0x81, 0x00, 0x63, 0x00, 0xA0, 0x00, 0x82, 0x04, 0xA1, 0x00,
	0xC0, 0x00, 0x84, 0x04, 0x47, 0x00, 0xA2, 0x00, 0x65, 0x04, 0xC1, 0x00,
	0x49, 0x00, 0xE0, 0x00, 0xA3, 0x00, 0xC2, 0x04, 0xA5, 0x04, 0xE2, 0x00,
	0x87, 0x04, 0x20, 0x01, 0x89, 0x04, 0xC4, 0x04, 0xE3, 0x04, 0x02, 0x01,
	0x20, 0x01, 0xC6, 0x04, 0x03, 0x09, 0x8B, 0x01
};


    }
}
