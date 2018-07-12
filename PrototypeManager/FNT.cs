using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PrototypeManager {
    public class FNT {
        byte[] FNTData, PROData;

        private FntHeader FHeader;
        private ProHeader PHeader;

        private Encoding Encoding = Encoding.GetEncoding(932);

        public bool Force8bpp = false;

        public FNT(byte[] FNT, byte[] PRO) {
            FNTData = FNT;
            PROData = PRO;
        }

        public Glyph[] GetGlyphs() {
            FHeader = new FntHeader();
            PHeader = new ProHeader();
            Tools.ReadStruct(FNTData, ref FHeader);
            Tools.ReadStruct(PROData, ref PHeader);

            uint FHLen = (uint)Tools.GetStructLength(FHeader);

            Glyph[] Glyphs = new Glyph[FHeader.CharCnt];
            for (uint i = 0; i < FHeader.CharCnt; i++) {
                byte[] Buffer = new byte[2];
                Buffer[0] = FNTData[(i * 2) + FHLen];
                Buffer[1] = FNTData[(i * 2) + FHLen + 1];

                if (Buffer[1] == 0x00)
                    Buffer = new byte[] { Buffer[0] };
                else
                    Buffer = Buffer.Reverse().ToArray();


                char Character = Encoding.GetString(Buffer).First();
                Glyphs[i] = new Glyph();
                Glyphs[i].Char = Character;
                Glyphs[i].Changed = false;
            }

            for (uint i = 0; i < FHeader.CharCnt; i++) {
                uint TIndex = FHLen + (FHeader.CharCnt * 2);

                byte[] Pixels = new byte[0];
                if (FHeader.BitType == 0) {
                    TIndex += (uint)((FHeader.Width / 2) * FHeader.Height) * i;
                    byte[] Buffer = new byte[(FHeader.Width * FHeader.Height) / 2];
                    for (uint z = 0; z < Buffer.LongLength; z++)
                        Buffer[z] = FNTData[TIndex + z];


                    Pixels = new byte[Buffer.Length * 2];
                    for (uint z = 0; z < Buffer.LongLength; z++) {
                        Pixels[(z * 2) + 0] = (byte)((Buffer[z] & 0xF0) | ((Buffer[z] & 0xF0) >> 4));
                        Pixels[(z * 2) + 1] = (byte)((Buffer[z] & 0x0F) | ((Buffer[z] & 0x0F) << 4));
                    }
                }
                if (FHeader.BitType == 3) {
                    TIndex += (uint)((FHeader.Width * FHeader.Height) * i);
                    Pixels = new byte[FHeader.Width * FHeader.Height];
                    for (uint z = 0; z < Pixels.Length; z++)
                        Pixels[z] = FNTData[z + TIndex];
                }

                if (FHeader.BitType != 0 && FHeader.BitType != 3)
                    throw new FormatException("Unsupported Pixel Format");//BitType = 1 (2bpp), = 2 (1bpp)

                Bitmap Bitmap = new Bitmap(FHeader.Width, FHeader.Height);
                uint Pos = 0;
                for (ushort y = 0; y < FHeader.Height; y++)
                    for (ushort x = 0; x < FHeader.Width; x++) {
                        byte Val = Pixels[Pos++];
                        Bitmap.SetPixel(x, y, Color.FromArgb(Val, Val, Val, Val));
                    }

                int PIndex = GetProIndex(Glyphs[i].Char);
                if (PIndex >= 0) {
                    ProEntry Entry = PHeader.Entries[PIndex];
                    if (Entry.Size == 0)
                        Entry.Size = (byte)(FHeader.Width - Entry.x);
                    if (Entry.Size + Entry.x > FHeader.Width)
                        Entry.Size = (byte)(FHeader.Width - Entry.x);

                    Bitmap Trimmed = new Bitmap(Entry.Size, FHeader.Height);
                    var Rect = new Rectangle(Entry.x, Entry.y, Entry.Size, FHeader.Height - Entry.y);
                    Trimmed = Bitmap.Clone(Rect, PixelFormat.Format32bppArgb);
                }

                Glyphs[i].Texture = Bitmap;
            }

            return Glyphs;
        }

        public void UpdateGlyphs(Glyph[] Glyphs, out byte[] FNT, out byte[] PRO) {
            uint FHLen = (uint)Tools.GetStructLength(FHeader);
            var OriGlyphs = GetGlyphs();

            FNT = new byte[FNTData.Length];
            FNTData.CopyTo(FNT, 0);
            PRO = new byte[PROData.Length];
            PROData.CopyTo(PRO, 0);
            FntHeader NewHeader = new FntHeader();
            Tools.CopyStruct(FHeader, ref NewHeader);

            if (Force8bpp) {
                NewHeader.BitType = 3;

                uint InfoLen = FHLen + (FHeader.CharCnt * 2);
                FNT = new byte[InfoLen + ((FHeader.Width * FHeader.Height) * FHeader.CharCnt)];
                for (uint i = 0; i < InfoLen; i++) {
                    FNT[i] = FNTData[i];
                }
            }

            Tools.BuildStruct(ref NewHeader).CopyTo(FNT, 0);

            for (uint i = 0; i < Glyphs.Length; i++) {

                if (Glyphs[i].Changed)
                    BitConverter.GetBytes(Tools.Reverse(GetSJIS(Glyphs[i].Char))).CopyTo(FNT, FHLen + (i * 2));



                uint Pos = 0;
                Bitmap Temp = new Bitmap(FHeader.Width, FHeader.Height);

                for (ushort y = 0; y < Glyphs[i].Texture.Height; y++)
                    for (ushort x = 0; x < Glyphs[i].Texture.Width; x++) {
                        Temp.SetPixel(x, y, Glyphs[i].Texture.GetPixel(x, y));
                    }

                Temp = ConvertTo8bppFormat(Temp);

                byte[] Pixels = new byte[FHeader.Width * FHeader.Height];
                for (ushort y = 0; y < FHeader.Height; y++)
                    for (ushort x = 0; x < FHeader.Width; x++) {
                        var Color = Temp.GetPixel(x, y);
                        byte Val = (byte)((Color.R + Color.G + Color.B) / 3);
                        Pixels[Pos++] = Val;
                    }


                if (NewHeader.BitType == 3) {
                    Pixels.CopyTo(FNT, (FHLen + (FHeader.CharCnt * 2) + ((FHeader.Width * FHeader.Height) * i)));
                } else if (NewHeader.BitType == 0) {
                    uint TIndex = (uint)(FHLen + (FHeader.CharCnt * 2) + (((FHeader.Width / 2) * FHeader.Height) * i));

                    byte[] Buffer = new byte[Pixels.Length / 2];
                    for (uint y = 0; y < Buffer.Length; y++) {
                        byte A = Pixels[(y * 2) + 0];
                        byte B = Pixels[(y * 2) + 1];

                        Buffer[y] = (byte)((A & 0xF0) | (B >> 4));
                    }

                    Buffer.CopyTo(FNT, TIndex);
                } else
                    throw new FormatException("Unsupported Pixel Format");

                int PI = GetProIndex(Glyphs[i].Char);
                uint PIndex = (uint)(PI * 3) + 0x10;
                if (PI > 0 && Glyphs[i].Changed)
                    new byte[] { 0x00, 0x00, (byte)Glyphs[i].Texture.Width }.CopyTo(PRO, PIndex);
                else
                    continue;
            }
        }

        public static Bitmap ConvertTo8bppFormat(Bitmap image) {
            Bitmap destImage = new Bitmap(image.Width, image.Height, PixelFormat.Format8bppIndexed);

            var Rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData bitmapData = destImage.LockBits(Rect, ImageLockMode.ReadWrite, destImage.PixelFormat);

            for (int i = 0; i < image.Width; i++) {
                for (int j = 0; j < image.Height; j++) {
                    Color color = image.GetPixel(i, j);
                    byte index = GetSimilarColor(destImage.Palette, color);
                    WriteBitmapData(i, j, index, bitmapData, 8);
                }
            }

            destImage.UnlockBits(bitmapData);

            return destImage;
        }

        private static byte GetSimilarColor(ColorPalette palette, Color color) {
            byte minDiff = byte.MaxValue;
            byte index = 0;

            for (int i = 0; i < palette.Entries.Length - 1; i++) {

                byte currentDiff = GetMaxDiff(color, palette.Entries[i]);

                if (currentDiff < minDiff) {
                    minDiff = currentDiff;
                    index = (byte)i;
                }
            }

            return index;
        }
        private static byte GetMaxDiff(Color a, Color b) {
            byte aDiff = Convert.ToByte(
                Math.Abs((short)(a.A - b.A)));

            byte bDiff = Convert.ToByte(
                Math.Abs((short)(a.B - b.B)));

            byte gDiff = Convert.ToByte(
                Math.Abs((short)(a.G - b.G)));

            byte rDiff = Convert.ToByte(
                Math.Abs((short)(a.R - b.R)));

            return Math.Max(Math.Max(rDiff, Math.Max(bDiff, gDiff)), aDiff);
        }


        private static void WriteBitmapData(int i, int j, byte index, BitmapData data, int pixelSize) {
            double entry = pixelSize / 8;
            IntPtr realByteAddr = new IntPtr(Convert.ToInt32(data.Scan0.ToInt32() + (j * data.Stride) + i * entry));

            byte[] dataToCopy = new byte[] { index };
            Marshal.Copy(dataToCopy, 0, realByteAddr, dataToCopy.Length);
        }

        private ushort GetSJIS(char Character) {
            byte[] Buffer = new byte[2];
            Encoding.GetBytes(Character.ToString()).CopyTo(Buffer, 0);

            return BitConverter.ToUInt16(Buffer, 0);
        }
        private int GetProIndex(char Character) {
            return GetProIndex(GetSJIS(Character));
        }
        private int GetProIndex(int SJIS) {
            int v1; 
            int v2;

            if (SJIS < 0x100) {
                return SJIS;
            }
            v1 = SJIS >> 8;
            if (v1 < 0x81 || v1 > 0x9F) {
                if (v1 < 0xE0 || v1 > 0xFC) {
                    return -1;
                }
                v2 = v1 - 0xC1;
            } else {
                v2 = v1 - 0x81;
            }
            if (SJIS >= 64 && SJIS <= 0x7E) {
                return SJIS + 0xBC * v2 + 192;
            }
            if (SJIS < 0x80 || SJIS > 0xFC) {
                return -1;
            }
            return (byte)SJIS + 0xBC * v2 + 0xBF;
        }
    }


    public struct Glyph {
        public Bitmap Texture;
        public char Char;
        public bool Changed;
    }

#pragma warning disable 649
    internal struct FntHeader {
        public ushort Width;
        public ushort Height;
        public uint CharCnt;
        public byte BitType;
        [FArray(Length = 7)]
        public byte[] Padding;
    }


    internal class ProHeader {
        public uint Count;
        [FArray(Length = 3)]
        public byte[] Padding;

        [RArray(FieldName = "Count"), StructField()]
        public ProEntry[] Entries;
    }

    internal class ProEntry {
        public byte x;
        public byte y;
        public byte Size;
    }

#pragma warning restore 649
}
