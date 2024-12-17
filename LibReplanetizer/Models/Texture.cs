// Copyright (C) 2018-2021, The Replanetizer Contributors.
// Replanetizer is free software: you can redistribute it
// and/or modify it under the terms of the GNU General Public
// License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// Please see the LICENSE.md file for more details.

using System;
using SixLabors.ImageSharp;
using System.IO;
using System.Runtime.InteropServices;
using static LibReplanetizer.DataFunctions;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;

namespace LibReplanetizer
{
    public enum TextureFormat : byte
    {
        BC1 = 0x86,
        BC2 = 0x87,
        BC3 = 0x88
    }
    public class Texture
    {
        public const int TEXTUREELEMSIZE = 0x24;

        public Image? renderedImage;
        public Image? img;

        /*public short width;
        public short height;
        public short mipMapCount;
        public int vramPointer;
        public byte[] data = new byte[0];

        public short off06;
        public int off08;
        public int off0C;

        public int off10;
        public int off14;
        public int off1C;

        public int off20;*/

        public int vramPointer;
        public byte off04;
        public byte mipMapCount;
        public TextureFormat textureFormat;
        public byte off07;
        public byte off08;
        public byte off09;
        public byte off0A;
        public byte off0B;
        public byte off0C;
        public byte off0D;
        public byte off0E;
        public byte off0F;
        public int gtfFlags;  // has to do with swizzle mode for uncompressed textures. common value found in games using GTF images with DXT compression is 0xAAE4
        public byte off14;
        public byte off15;
        public byte off16;
        public byte off17;
        public short width;
        public short height;
        public short off1C;
        public short off1E;
        public short off20;
        public short off22;

        public byte[] data;

        public int id;


        public Texture(int id, short width, short height, byte[] data, TextureFormat fmt = TextureFormat.BC3)
        {
            this.id = id;
            this.data = data;

            off04 = 0;
            mipMapCount = 1;
            textureFormat = fmt;
            off07 = 0x29;
            off08 = 0x00;
            off09 = 0x01;
            off0A = 0x03;
            off0B = 0x03;
            off0C = 0x80;
            off0D = 0x03;
            off0E = 0x00;
            off0F = 0x00;

            gtfFlags = 0x0000AAE4;  // good default value.
            off14 = 0x02;
            off15 = 0x06;
            off16 = 0x3E;
            off17 = 0x80;
            this.width = width;
            this.height = height;
            off1C = 0x0010;
            off1E = 0x0000;

            off20 = 0x00FF;
            off22 = 0x0000;
        }

        public Texture(byte[] textureBlock, int offset)
        {
            vramPointer = ReadInt(textureBlock, (offset * TEXTUREELEMSIZE) + 0x00);
            off04 = textureBlock[(offset * TEXTUREELEMSIZE) + 0x04];
            mipMapCount = textureBlock[(offset * TEXTUREELEMSIZE) + 0x05];
            textureFormat = (TextureFormat)textureBlock[(offset * TEXTUREELEMSIZE) + 0x06];
            off07 = textureBlock[(offset * TEXTUREELEMSIZE) + 0x07];
            off08 = textureBlock[(offset * TEXTUREELEMSIZE) + 0x08];
            off09 = textureBlock[(offset * TEXTUREELEMSIZE) + 0x09];
            off0A = textureBlock[(offset * TEXTUREELEMSIZE) + 0x0A];
            off0B = textureBlock[(offset * TEXTUREELEMSIZE) + 0x0B];
            off0C = textureBlock[(offset * TEXTUREELEMSIZE) + 0x0C];
            off0D = textureBlock[(offset * TEXTUREELEMSIZE) + 0x0D];
            off0E = textureBlock[(offset * TEXTUREELEMSIZE) + 0x0E];
            off0F = textureBlock[(offset * TEXTUREELEMSIZE) + 0x0F];

            gtfFlags = ReadInt(textureBlock, (offset * TEXTUREELEMSIZE) + 0x10);
            off14 = textureBlock[(offset * TEXTUREELEMSIZE) + 0x14];
            off15 = textureBlock[(offset * TEXTUREELEMSIZE) + 0x15];
            off16 = textureBlock[(offset * TEXTUREELEMSIZE) + 0x16];
            off17 = textureBlock[(offset * TEXTUREELEMSIZE) + 0x17];
            width = ReadShort(textureBlock, (offset * TEXTUREELEMSIZE) + 0x18);
            height = ReadShort(textureBlock, (offset * TEXTUREELEMSIZE) + 0x1A);
            off1C = ReadShort(textureBlock, (offset * TEXTUREELEMSIZE) + 0x1C);
            off1E = ReadShort(textureBlock, (offset * TEXTUREELEMSIZE) + 0x1E);

            off20 = ReadShort(textureBlock, (offset * TEXTUREELEMSIZE) + 0x20);
            off22 = ReadShort(textureBlock, (offset * TEXTUREELEMSIZE) + 0x22);

            id = offset;
        }

        public byte[] Serialize(int vramOffset)
        {
            byte[] outBytes = new byte[0x24];

            WriteInt(outBytes, 0x00, vramOffset);
            outBytes[0x04] = off04;
            outBytes[0x05] = mipMapCount;
            outBytes[0x06] = (byte)textureFormat;
            outBytes[0x07] = off07;
            outBytes[0x08] = off08;
            outBytes[0x09] = off09;
            outBytes[0x0A] = off0A;
            outBytes[0x0B] = off0B;
            outBytes[0x0C] = off0C;
            outBytes[0x0D] = off0D;
            outBytes[0x0E] = off0E;
            outBytes[0x0F] = off0F;

            WriteInt(outBytes, 0x10, gtfFlags);
            outBytes[0x14] = off14;
            outBytes[0x15] = off15;
            outBytes[0x16] = off16;
            outBytes[0x17] = off17;
            WriteShort(outBytes, 0x18, width);
            WriteShort(outBytes, 0x1A, height);
            WriteShort(outBytes, 0x1C, off1C);
            WriteShort(outBytes, 0x1E, off1E);

            WriteShort(outBytes, 0x20, off20);
            WriteShort(outBytes, 0x22, off22);

            return outBytes;
        }

        public Image? GetTextureImage(bool includeTransparency)
        {
            if (img != null) return img;

            switch (textureFormat)
            {
                case TextureFormat.BC1:
                    {

                        byte[]? imgData = DecompressDxt1(data, width, height);

                        if (imgData != null)
                        {
                            if (!includeTransparency)
                            {
                                for (int i = 0; i < width * height; i++)
                                {
                                    imgData[i * 4 + 3] = 255;
                                }
                            }

                            img = Image.LoadPixelData<Bgra32>(imgData, width, height);
                        }

                        return img;
                    }
                case TextureFormat.BC3:
                    {

                        byte[]? imgData = DecompressDxt5(data, width, height);

                        if (imgData != null)
                        {
                            if (!includeTransparency)
                            {
                                for (int i = 0; i < width * height; i++)
                                {
                                    imgData[i * 4 + 3] = 255;
                                }
                            }

                            img = Image.LoadPixelData<Bgra32>(imgData, width, height);
                        }

                        return img;
                    }
                default:
                    throw new NotImplementedException("Unsupported texture format");
            }
        }


        public static byte[]? DecompressDxt5(byte[] imageData, int width, int height)
        {
            if (imageData != null)
            {
                using (MemoryStream imageStream = new MemoryStream(imageData))
                    return DecompressDxt5(imageStream, width, height);
            }
            return null;
        }

        public static byte[]? DecompressDxt1(byte[] imageData, int width, int height)
        {
            if (imageData != null)
            {
                using (MemoryStream imageStream = new MemoryStream(imageData))
                    return DecompressDxt1(imageStream, width, height);
            }
            return null;
        }

        internal static byte[] DecompressDxt5(Stream imageStream, int width, int height)
        {
            byte[] imageData = new byte[width * height * 4];

            using (BinaryReader imageReader = new BinaryReader(imageStream))
            {
                int blockCountX = (width + 3) / 4;
                int blockCountY = (height + 3) / 4;

                for (int y = 0; y < blockCountY; y++)
                {
                    for (int x = 0; x < blockCountX; x++)
                    {
                        DecompressDxt5Block(imageReader, x, y, blockCountX, width, height, imageData);
                    }
                }
            }

            return imageData;
        }

        internal static byte[] DecompressDxt1(Stream imageStream, int width, int height)
        {
            byte[] imageData = new byte[width * height];

            using (BinaryReader imageReader = new BinaryReader(imageStream))
            {
                int blockCountX = (width + 3) / 4;
                int blockCountY = (height + 3) / 4;

                for (int y = 0; y < blockCountY; y++)
                {
                    for (int x = 0; x < blockCountX; x++)
                    {
                        DecompressDxt1Block(imageReader, x, y, blockCountX, width, height, imageData);
                    }
                }
            }

            return imageData;
        }

        private static void DecompressDxt5Block(BinaryReader imageReader, int x, int y, int blockCountX, int width, int height, byte[] imageData)
        {
            byte alpha0 = imageReader.ReadByte();
            byte alpha1 = imageReader.ReadByte();

            ulong alphaMask = (ulong) imageReader.ReadByte();
            alphaMask += (ulong) imageReader.ReadByte() << 8;
            alphaMask += (ulong) imageReader.ReadByte() << 16;
            alphaMask += (ulong) imageReader.ReadByte() << 24;
            alphaMask += (ulong) imageReader.ReadByte() << 32;
            alphaMask += (ulong) imageReader.ReadByte() << 40;

            ushort c0 = imageReader.ReadUInt16();
            ushort c1 = imageReader.ReadUInt16();

            byte r0, g0, b0;
            byte r1, g1, b1;
            ConvertRgb565ToRgb888(c0, out b0, out g0, out r0);
            ConvertRgb565ToRgb888(c1, out b1, out g1, out r1);

            uint lookupTable = imageReader.ReadUInt32();

            for (int blockY = 0; blockY < 4; blockY++)
            {
                for (int blockX = 0; blockX < 4; blockX++)
                {
                    byte r = 0, g = 0, b = 0, a = 255;
                    uint index = (lookupTable >> 2 * (4 * blockY + blockX)) & 0x03;

                    uint alphaIndex = (uint) ((alphaMask >> 3 * (4 * blockY + blockX)) & 0x07);
                    if (alphaIndex == 0)
                    {
                        a = alpha0;
                    }
                    else if (alphaIndex == 1)
                    {
                        a = alpha1;
                    }
                    else if (alpha0 > alpha1)
                    {
                        a = (byte) (((8 - alphaIndex) * alpha0 + (alphaIndex - 1) * alpha1) / 7);
                    }
                    else if (alphaIndex == 6)
                    {
                        a = 0;
                    }
                    else if (alphaIndex == 7)
                    {
                        a = 0xff;
                    }
                    else
                    {
                        a = (byte) (((6 - alphaIndex) * alpha0 + (alphaIndex - 1) * alpha1) / 5);
                    }

                    switch (index)
                    {
                        case 0:
                            r = r0;
                            g = g0;
                            b = b0;
                            break;
                        case 1:
                            r = r1;
                            g = g1;
                            b = b1;
                            break;
                        case 2:
                            r = (byte) ((2 * r0 + r1) / 3);
                            g = (byte) ((2 * g0 + g1) / 3);
                            b = (byte) ((2 * b0 + b1) / 3);
                            break;
                        case 3:
                            r = (byte) ((r0 + 2 * r1) / 3);
                            g = (byte) ((g0 + 2 * g1) / 3);
                            b = (byte) ((b0 + 2 * b1) / 3);
                            break;
                    }

                    int px = (x << 2) + blockX;
                    int py = (y << 2) + blockY;
                    if ((px < width) && (py < height))
                    {
                        int offset = ((py * width) + px) << 2;
                        imageData[offset] = r;
                        imageData[offset + 1] = g;
                        imageData[offset + 2] = b;
                        imageData[offset + 3] = a;
                    }
                }
            }
        }
        private static void DecompressDxt1Block(BinaryReader imageReader, int x, int y, int blockCountX, int width, int height, byte[] imageData)
        {
            ushort c0 = imageReader.ReadUInt16();
            ushort c1 = imageReader.ReadUInt16();

            byte r0, g0, b0;
            byte r1, g1, b1;
            ConvertRgb565ToRgb888(c0, out b0, out g0, out r0);
            ConvertRgb565ToRgb888(c1, out b1, out g1, out r1);

            uint lookupTable = imageReader.ReadUInt32();

            for (int blockY = 0; blockY < 4; blockY++)
            {
                for (int blockX = 0; blockX < 4; blockX++)
                {
                    byte r = 0, g = 0, b = 0, a = 255;
                    uint index = (lookupTable >> 2 * (4 * blockY + blockX)) & 0x03;

                    switch (index)
                    {
                        case 0:
                            r = r0;
                            g = g0;
                            b = b0;
                            break;
                        case 1:
                            r = r1;
                            g = g1;
                            b = b1;
                            break;
                        case 2:
                            r = (byte) ((2 * r0 + r1) / 3);
                            g = (byte) ((2 * g0 + g1) / 3);
                            b = (byte) ((2 * b0 + b1) / 3);
                            break;
                        case 3:
                            r = (byte) ((r0 + 2 * r1) / 3);
                            g = (byte) ((g0 + 2 * g1) / 3);
                            b = (byte) ((b0 + 2 * b1) / 3);
                            break;
                    }

                    int px = (x << 2) + blockX;
                    int py = (y << 2) + blockY;
                    if ((px < width) && (py < height))
                    {
                        int offset = ((py * width) + px) << 2;
                        imageData[offset] = r;
                        imageData[offset + 1] = g;
                        imageData[offset + 2] = b;
                        imageData[offset + 3] = a;
                    }
                }
            }
        }
        private static void ConvertRgb565ToRgb888(ushort color, out byte r, out byte g, out byte b)
        {
            int temp;

            temp = (color >> 11) * 255 + 16;
            r = (byte) ((temp / 32 + temp) / 32);
            temp = ((color & 0x07E0) >> 5) * 255 + 32;
            g = (byte) ((temp / 64 + temp) / 64);
            temp = (color & 0x001F) * 255 + 16;
            b = (byte) ((temp / 32 + temp) / 32);
        }
    }
}

