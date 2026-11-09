using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CursorStudioApp
{
    internal static class Native
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr LoadCursorFromFile(string lpFileName);

        [DllImport("user32.dll")]
        internal static extern bool DestroyCursor(IntPtr hCursor);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

        [DllImport("user32.dll")]
        internal static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetSystemCursor(IntPtr hcur, uint id);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr CreateCursor(IntPtr hInst, int xHotSpot, int yHotSpot, int nWidth, int nHeight, byte[] pvANDPlane, byte[] pvXORPlane);

        internal const uint OCR_NORMAL = 32512;
        internal const uint OCR_IBEAM = 32513;
        internal const uint OCR_WAIT = 32514;
        internal const uint OCR_CROSS = 32515;
        internal const uint OCR_UP = 32516;
        internal const uint OCR_SIZENWSE = 32642;
        internal const uint OCR_SIZENESW = 32643;
        internal const uint OCR_SIZEWE = 32644;
        internal const uint OCR_SIZENS = 32645;
        internal const uint OCR_SIZEALL = 32646;
        internal const uint OCR_NO = 32648;
        internal const uint OCR_HAND = 32649;
        internal const uint OCR_APPSTARTING = 32650;
        internal const uint OCR_HELP = 32651;

        internal const uint IMAGE_CURSOR = 2;
        internal const uint LR_LOADFROMFILE = 0x00000010;
        internal const uint LR_DEFAULTSIZE = 0x00000040;

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetCursorInfo(ref CursorInfo pci);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetIconInfo(IntPtr hIcon, out IconInfo piconinfo);

        [DllImport("gdi32.dll")]
        internal static extern int GetObject(IntPtr h, int c, out BitmapHeader pv);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(IntPtr o);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr CreateIconIndirect(ref IconInfo piconinfo);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, byte[] lpvBits);

        [DllImport("user32.dll")]
        internal static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential)]
        internal struct CursorInfo
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct IconInfo
        {
            public int fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BitmapHeader
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public short bmPlanes;
            public short bmBitsPixel;
            public IntPtr bmBits;
        }

        internal const uint SPI_SETCURSORS = 0x0057;
        internal const uint SPIF_SENDCHANGE = 0x0002;
    }

    internal class Scheme
    {
        internal string Name;
        internal string[] Paths;
        internal bool IsSystem;
        internal bool IsPack;
        internal string Dir;

        public override string ToString()
        {
            if (IsSystem) return Name + "（系统内置）";
            return Name + "（自定义）";
        }
    }

    internal static class Roles
    {
        internal static readonly string[] Keys = new string[]
        {
            "Arrow", "Help", "AppStarting", "Wait", "Crosshair", "IBeam", "NWPen",
            "No", "SizeNS", "SizeWE", "SizeNWSE", "SizeNESW", "SizeAll", "UpArrow",
            "Hand", "Pin", "Person"
        };

        internal static readonly string[] ExtraKeys = new string[] { "Pan", "Grabbing", "Zoom-in", "Zoom-out" };

        internal static readonly string[] Names = new string[]
        {
            "正常选择", "帮助选择", "后台运行", "忙碌", "精确选择", "文本选择", "手写笔",
            "不可用", "垂直缩放", "水平缩放", "对角缩放 1", "对角缩放 2", "移动", "备用选择",
            "链接选择", "位置选择", "人员选择", "平移", "抓取", "放大", "缩小"
        };

        // 与 Keys 一一对应（0 = Arrow）
        internal static readonly string[][] Keywords = new string[][]
        {
            new string[] { "pointer", "normal", "arrow", "default", "cursor" },
            new string[] { "help" },
            new string[] { "work", "working", "background" },
            new string[] { "busy", "wait", "loading" },
            new string[] { "cross", "precision" },
            new string[] { "text", "beam", "ibeam" },
            new string[] { "handwriting", "pen" },
            new string[] { "unavailable", "forbidden", "nodrop", "notallowed" },
            new string[] { "vert", "size_ns", "ns_" },
            new string[] { "horz", "size_we", "we_" },
            new string[] { "dgn1", "nwse" },
            new string[] { "dgn2", "nesw" },
            new string[] { "move", "sizeall", "allscroll" },
            new string[] { "alternate", "up_" },
            new string[] { "link", "hand" },
            new string[] { "pin", "location" },
            new string[] { "person" },
            new string[] { "pan" },
            new string[] { "grabbing" },
            new string[] { "zoom-in", "zoomin" },
            new string[] { "zoom-out", "zoomout" }
        };
    }

    internal static class CursorWorks
    {
        internal const string CurKey = @"Control Panel\Cursors";
        internal const string SchemesKey = @"Control Panel\Cursors\Schemes";
        internal const string HklmSchemes = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Control Panel\Cursors\Schemes";
        internal const string AccessibilityKey = @"Software\Microsoft\Accessibility";

        internal static List<Scheme> LoadSchemes()
        {
            List<Scheme> list = new List<Scheme>();
            Dictionary<string, Scheme> byName = new Dictionary<string, Scheme>(StringComparer.OrdinalIgnoreCase);

            using (RegistryKey k = Registry.LocalMachine.OpenSubKey(HklmSchemes))
            {
                if (k != null)
                {
                    foreach (string n in k.GetValueNames())
                    {
                        string v = k.GetValue(n) as string;
                        if (string.IsNullOrEmpty(v)) continue;
                        Scheme s = new Scheme();
                        s.Name = n;
                        s.Paths = v.Split(',');
                        s.IsSystem = true;
                        list.Add(s);
                        byName[n] = s;
                    }
                }
            }

            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(SchemesKey))
            {
                if (k != null)
                {
                    foreach (string n in k.GetValueNames())
                    {
                        string v = k.GetValue(n) as string;
                        if (string.IsNullOrEmpty(v)) continue;
                        if (byName.ContainsKey(n)) continue;
                        Scheme s = new Scheme();
                        s.Name = n;
                        s.Paths = v.Split(',');
                        s.IsSystem = false;
                        list.Add(s);
                        byName[n] = s;
                    }
                }
            }

            foreach (Scheme p in LoadPacks())
            {
                Scheme exist = byName.ContainsKey(p.Name) ? byName[p.Name] : null;
                if (exist != null && !exist.IsSystem)
                {
                    exist.Paths = p.Paths;
                    exist.IsPack = true;
                    exist.Dir = p.Dir;
                }
                else if (exist == null)
                {
                    list.Add(p);
                    byName[p.Name] = p;
                }
            }

            list.Sort(delegate(Scheme a, Scheme b)
            {
                if (a.IsSystem != b.IsSystem) return a.IsSystem ? -1 : 1;
                return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
            });
            return list;
        }

        internal static string CustomDir()
        {
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            string dir = Path.Combine(exeDir, "custom");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        internal static string PackName(string folder)
        {
            string name = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (name.ToLowerInvariant().EndsWith("-windows")) name = name.Substring(0, name.Length - 8);
            name = name.Replace('_', ' ').Replace('-', ' ').Trim();
            if (name.Length == 0) name = "Imported Cursors";
            return name;
        }

        internal static string[] MatchPack(string folder, out int matched)
        {
            List<string> pool = new List<string>();
            pool.AddRange(Directory.GetFiles(folder, "*.cur", SearchOption.AllDirectories));
            pool.AddRange(Directory.GetFiles(folder, "*.ani", SearchOption.AllDirectories));

            int total = Roles.Keys.Length + Roles.ExtraKeys.Length;
            string[] result = new string[total];

            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < Roles.Keywords.Length && i < total; i++)
                {
                    if (pass == 0 && i == 0) continue;
                    if (pass == 1 && i != 0) continue;
                    if (result[i] != null) continue;
                    string hit = FindMatch(pool, Roles.Keywords[i]);
                    if (hit != null) { result[i] = hit; pool.Remove(hit); }
                }
            }

            matched = 0;
            for (int i = 0; i < total; i++) if (result[i] != null) matched++;
            return result;
        }

        internal static List<Scheme> LoadPacks()
        {
            List<Scheme> list = new List<Scheme>();
            string root = CustomDir();
            List<string> dirs = new List<string>(Directory.GetDirectories(root));
            dirs.Sort(StringComparer.CurrentCultureIgnoreCase);
            foreach (string d in dirs)
            {
                int matched;
                string[] paths = MatchPack(d, out matched);
                if (matched < 3) continue;
                Scheme s = new Scheme();
                s.Name = PackName(d);
                s.Paths = paths;
                s.IsSystem = false;
                s.IsPack = true;
                s.Dir = d;
                list.Add(s);
            }
            return list;
        }

        internal static void RegisterScheme(Scheme s)
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(SchemesKey))
            {
                if (k == null) return;
                string val = "";
                for (int i = 0; i < s.Paths.Length; i++)
                {
                    if (i > 0) val += ",";
                    val += (s.Paths[i] == null ? "" : s.Paths[i]);
                }
                k.SetValue(s.Name, val, RegistryValueKind.String);
            }
        }

        internal static string CurrentSchemeName()
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(CurKey))
            {
                if (k == null) return null;
                return k.GetValue("") as string;
            }
        }

        internal static int CurrentSize()
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(AccessibilityKey))
            {
                if (k != null)
                {
                    object v = k.GetValue("CursorSize");
                    if (v != null)
                    {
                        try { return Convert.ToInt32(v); }
                        catch { }
                    }
                }
            }
            return 1;
        }

        internal static string GetRolePath(string role)
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(CurKey))
            {
                if (k == null) return null;
                return k.GetValue(role) as string;
            }
        }

        internal static void ApplyScheme(Scheme s)
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(CurKey, true))
            {
                if (k == null) throw new InvalidOperationException("无法打开光标注册表键（权限不足）");

                int n = Math.Min(s.Paths.Length, Roles.Keys.Length);
                for (int i = 0; i < n; i++)
                {
                    string p = s.Paths[i];
                    if (!string.IsNullOrEmpty(p) && File.Exists(p)) k.SetValue(Roles.Keys[i], p, RegistryValueKind.String);
                    else k.DeleteValue(Roles.Keys[i], false);
                }

                for (int i = 0; i < Roles.ExtraKeys.Length; i++)
                {
                    int idx = Roles.Keys.Length + i;
                    if (s.Paths.Length > idx)
                    {
                        string p = s.Paths[idx];
                        if (!string.IsNullOrEmpty(p) && File.Exists(p)) k.SetValue(Roles.ExtraKeys[i], p, RegistryValueKind.String);
                        else k.DeleteValue(Roles.ExtraKeys[i], false);
                    }
                    else
                    {
                        k.DeleteValue(Roles.ExtraKeys[i], false);
                    }
                }

                string cross = k.GetValue("Crosshair") as string;
                if (!string.IsNullOrEmpty(cross)) k.SetValue("precisionhair", cross, RegistryValueKind.String);
                string move = k.GetValue("SizeAll") as string;
                if (!string.IsNullOrEmpty(move)) k.SetValue("Grab", move, RegistryValueKind.String);

                k.SetValue("", s.Name, RegistryValueKind.String);
                k.SetValue("Scheme Source", 2, RegistryValueKind.DWord);
            }
            if (s.IsPack) RegisterScheme(s);
            Broadcast();
        }

        internal static int ApplySize(int level)
        {
            if (level < 1) level = 1;
            if (level > 15) level = 15;
            int px = 32 + (level - 1) * 8;

            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(AccessibilityKey))
            {
                if (k != null) k.SetValue("CursorSize", level, RegistryValueKind.DWord);
            }
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(CurKey, true))
            {
                if (k != null) k.SetValue("CursorBaseSize", px, RegistryValueKind.DWord);
            }

            int applied = ApplySizeToSystem(px);
            return applied;
        }

        private static uint RoleToOcr(int index)
        {
            switch (index)
            {
                case 0: return Native.OCR_NORMAL;
                case 1: return Native.OCR_HELP;
                case 2: return Native.OCR_APPSTARTING;
                case 3: return Native.OCR_WAIT;
                case 4: return Native.OCR_CROSS;
                case 5: return Native.OCR_IBEAM;
                case 7: return Native.OCR_NO;
                case 8: return Native.OCR_SIZENS;
                case 9: return Native.OCR_SIZEWE;
                case 10: return Native.OCR_SIZENWSE;
                case 11: return Native.OCR_SIZENESW;
                case 12: return Native.OCR_SIZEALL;
                case 13: return Native.OCR_UP;
                case 14: return Native.OCR_HAND;
                default: return 0;
            }
        }

        internal static int ApplySizeToSystem(int px)
        {
            int applied = 0;
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(CurKey))
            {
                if (k == null) return 0;
                for (int i = 0; i < Roles.Keys.Length; i++)
                {
                    uint id = RoleToOcr(i);
                    if (id == 0) continue;
                    string p = k.GetValue(Roles.Keys[i]) as string;
                    if (string.IsNullOrEmpty(p) || !File.Exists(p)) continue;
                    IntPtr h2 = BuildHdCursor(p, px);
                    if (h2 == IntPtr.Zero)
                    {
                        IntPtr h = Native.LoadImage(IntPtr.Zero, p, Native.IMAGE_CURSOR, px, px, Native.LR_LOADFROMFILE);
                        if (h == IntPtr.Zero) continue;
                        h2 = Native.CopyIcon(h);
                        Native.DestroyCursor(h);
                    }
                    if (h2 == IntPtr.Zero) continue;
                    if (Native.SetSystemCursor(h2, id)) applied++;
                    else Native.DestroyCursor(h2);
                }
            }
            return applied;
        }

        // ---- 光标文件高保真解码：直接解析 .cur/.ani 内的原始大帧（128×128），
        // 再用高质量重采样绘制/构造，避免系统 API 只给 32×32 造成的模糊 ----

        internal static Bitmap DecodeCur(string path, out int hx, out int hy)
        {
            hx = 0;
            hy = 0;
            try
            {
                byte[] data = File.ReadAllBytes(path);
                if (path.EndsWith(".ani", StringComparison.OrdinalIgnoreCase))
                {
                    byte[] frame = FindAniFrame(data);
                    if (frame == null) return null;
                    return DecodeIconDir(frame, out hx, out hy);
                }
                return DecodeIconDir(data, out hx, out hy);
            }
            catch
            {
                return null;
            }
        }

        private static byte[] FindAniFrame(byte[] data)
        {
            for (int i = 0; i + 12 < data.Length; i++)
            {
                if (data[i] != 'i' || data[i + 1] != 'c' || data[i + 2] != 'o' || data[i + 3] != 'n') continue;
                int size = BitConverter.ToInt32(data, i + 4);
                int start = i + 8;
                if (size < 6 || start + size > data.Length) continue;
                if (data[start] != 0 || data[start + 1] != 0 || data[start + 2] != 2 || data[start + 3] != 0) continue;
                byte[] sub = new byte[size];
                Array.Copy(data, start, sub, 0, size);
                return sub;
            }
            return null;
        }

        private static Bitmap DecodeIconDir(byte[] data, out int hx, out int hy)
        {
            hx = 0;
            hy = 0;
            if (data.Length < 6) return null;
            if (data[0] != 0 || data[1] != 0 || data[2] != 2 || data[3] != 0) return null;
            int count = BitConverter.ToUInt16(data, 4);
            if (count < 1 || count > 64) return null;

            int best = -1;
            int bestScore = -1;
            for (int i = 0; i < count; i++)
            {
                int e = 6 + i * 16;
                if (e + 16 > data.Length) break;
                int w = data[e] == 0 ? 256 : data[e];
                int hh = data[e + 1] == 0 ? 256 : data[e + 1];
                int score = w * hh * 4 + i;
                if (score >= bestScore) { bestScore = score; best = i; }
            }
            if (best < 0) return null;

            int e2 = 6 + best * 16;
            hx = BitConverter.ToUInt16(data, e2 + 4);
            hy = BitConverter.ToUInt16(data, e2 + 6);
            int bytes = (int)BitConverter.ToUInt32(data, e2 + 8);
            int off = (int)BitConverter.ToUInt32(data, e2 + 12);
            if (off <= 0 || bytes <= 0 || off + bytes > data.Length) return null;

            if (data[off] == 0x89 && data[off + 1] == 0x50 && data[off + 2] == 0x4E && data[off + 3] == 0x47)
            {
                using (MemoryStream ms = new MemoryStream(data, off, bytes, false))
                {
                    using (Image img = Image.FromStream(ms))
                    {
                        Bitmap bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.CompositingMode = CompositingMode.SourceCopy;
                            g.DrawImage(img, 0, 0, img.Width, img.Height);
                        }
                        return bmp;
                    }
                }
            }
            return DecodeDib(data, off);
        }

        private static Bitmap DecodeDib(byte[] data, int off)
        {
            if (off + 40 > data.Length) return null;
            int hdrSize = BitConverter.ToInt32(data, off);
            int bw = BitConverter.ToInt32(data, off + 4);
            int rawH = BitConverter.ToInt32(data, off + 8);
            short bpp = BitConverter.ToInt16(data, off + 14);
            int comp = BitConverter.ToInt32(data, off + 16);
            int clrUsed = BitConverter.ToInt32(data, off + 32);
            if (bw <= 0 || bw > 512) return null;
            int h = Math.Abs(rawH) / 2;
            if (h <= 0 || h > 512) return null;
            if (comp != 0) return null;
            if (bpp != 32 && bpp != 24 && bpp != 8) return null;

            int palette = (bpp <= 8) ? (clrUsed == 0 ? (1 << bpp) : clrUsed) : 0;
            int pixOff = off + hdrSize + palette * 4;
            int stride = ((bw * bpp + 31) / 32) * 4;
            int maskStride = ((bw + 31) / 32) * 4;
            int maskOff = pixOff + stride * h;
            if (maskOff + maskStride * h > data.Length) return null;

            Bitmap bmp = new Bitmap(bw, h, PixelFormat.Format32bppArgb);
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bw, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                byte[] dst = new byte[bd.Stride * h];
                for (int y = 0; y < h; y++)
                {
                    int srcRow = pixOff + (h - 1 - y) * stride;
                    int maskRow = maskOff + (h - 1 - y) * maskStride;
                    int dstRow = y * bd.Stride;
                    for (int x = 0; x < bw; x++)
                    {
                        byte b = 0, g = 0, r = 0, a = 255;
                        if (bpp == 32)
                        {
                            b = data[srcRow + x * 4];
                            g = data[srcRow + x * 4 + 1];
                            r = data[srcRow + x * 4 + 2];
                            a = data[srcRow + x * 4 + 3];
                        }
                        else if (bpp == 24)
                        {
                            b = data[srcRow + x * 3];
                            g = data[srcRow + x * 3 + 1];
                            r = data[srcRow + x * 3 + 2];
                        }
                        else
                        {
                            int idx = data[srcRow + x];
                            int po = off + hdrSize + idx * 4;
                            if (po + 3 < data.Length)
                            {
                                b = data[po];
                                g = data[po + 1];
                                r = data[po + 2];
                                a = data[po + 3];
                                if (a == 0) a = 255;
                            }
                        }
                        bool transparent = ((data[maskRow + (x / 8)] >> (7 - (x % 8))) & 1) == 1;
                        if (transparent) a = 0;
                        dst[dstRow + x * 4] = b;
                        dst[dstRow + x * 4 + 1] = g;
                        dst[dstRow + x * 4 + 2] = r;
                        dst[dstRow + x * 4 + 3] = a;
                    }
                }
                Marshal.Copy(dst, 0, bd.Scan0, dst.Length);
            }
            finally
            {
                bmp.UnlockBits(bd);
            }
            return bmp;
        }

        internal static Bitmap ScaleCursor(Bitmap src, int size, bool center)
        {
            if (src == null || size <= 0) return null;
            Bitmap dst = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                float scale = Math.Min((float)size / src.Width, (float)size / src.Height);
                int w = Math.Max(1, (int)Math.Round(src.Width * scale));
                int hh = Math.Max(1, (int)Math.Round(src.Height * scale));
                int x = center ? (size - w) / 2 : 0;
                int y = center ? (size - hh) / 2 : 0;
                g.DrawImage(src, new Rectangle(x, y, w, hh), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel);
            }
            return dst;
        }

        internal static IntPtr BuildHdCursor(string path, int px)
        {
            int hx, hy;
            Bitmap src = DecodeCur(path, out hx, out hy);
            if (src == null) return IntPtr.Zero;
            Bitmap scaled = null;
            IntPtr hIcon = IntPtr.Zero;
            IntPtr hColor = IntPtr.Zero;
            IntPtr hMask = IntPtr.Zero;
            try
            {
                scaled = ScaleCursor(src, px, false);
                if (scaled == null) return IntPtr.Zero;
                int sx = (int)Math.Round(hx * (double)px / src.Width);
                int sy = (int)Math.Round(hy * (double)px / src.Height);
                hColor = scaled.GetHbitmap(Color.FromArgb(0, 0, 0, 0));
                int maskStride = ((px + 31) / 32) * 4;
                byte[] mask = new byte[maskStride * px];
                hMask = Native.CreateBitmap(px, px, 1, 1, mask);
                Native.IconInfo ii = new Native.IconInfo();
                ii.fIcon = 0;
                ii.xHotspot = Math.Max(0, Math.Min(px - 1, sx));
                ii.yHotspot = Math.Max(0, Math.Min(px - 1, sy));
                ii.hbmMask = hMask;
                ii.hbmColor = hColor;
                hIcon = Native.CreateIconIndirect(ref ii);
            }
            catch
            {
                hIcon = IntPtr.Zero;
            }
            finally
            {
                if (hColor != IntPtr.Zero) Native.DeleteObject(hColor);
                if (hMask != IntPtr.Zero) Native.DeleteObject(hMask);
                if (scaled != null) scaled.Dispose();
                src.Dispose();
            }
            return hIcon;
        }

        internal static int GetCursorPixels()
        {
            Native.CursorInfo ci = new Native.CursorInfo();
            ci.cbSize = Marshal.SizeOf(typeof(Native.CursorInfo));
            if (!Native.GetCursorInfo(ref ci) || ci.hCursor == IntPtr.Zero) return 0;
            Native.IconInfo ii;
            if (!Native.GetIconInfo(ci.hCursor, out ii)) return 0;
            IntPtr hb = ii.hbmColor != IntPtr.Zero ? ii.hbmColor : ii.hbmMask;
            if (hb == IntPtr.Zero) return 0;
            Native.BitmapHeader bm;
            int r = Native.GetObject(hb, Marshal.SizeOf(typeof(Native.BitmapHeader)), out bm);
            if (ii.hbmColor != IntPtr.Zero) Native.DeleteObject(ii.hbmColor);
            if (ii.hbmMask != IntPtr.Zero) Native.DeleteObject(ii.hbmMask);
            if (r == 0) return 0;
            return bm.bmWidth;
        }

        internal static void Broadcast()
        {
            Native.SystemParametersInfo(Native.SPI_SETCURSORS, 0, IntPtr.Zero, Native.SPIF_SENDCHANGE);
        }

        internal static void RestartExplorer()
        {
            foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcessesByName("explorer"))
            {
                try { p.Kill(); }
                catch { }
            }
            System.Threading.Thread.Sleep(2000);
            for (int i = 0; i < 6; i++)
            {
                if (System.Diagnostics.Process.GetProcessesByName("explorer").Length > 0) break;
                try { System.Diagnostics.Process.Start("explorer.exe"); }
                catch { }
                System.Threading.Thread.Sleep(1500);
            }
        }

        internal static string ImportPack(string folder, out int matched, out int total)
        {
            total = Roles.Keys.Length + Roles.ExtraKeys.Length;
            string[] result = MatchPack(folder, out matched);
            if (matched == 0) throw new InvalidOperationException("这个文件夹里没有可识别的光标文件（.cur / .ani）");

            string name = PackName(folder);
            string root = CustomDir();
            string full = Path.GetFullPath(folder);
            string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            string[] finalPaths = new string[total];
            if (full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < total; i++) finalPaths[i] = result[i];
                name = PackName(full);
            }
            else
            {
                string dest = Path.Combine(root, name);
                int suffix = 1;
                while (Directory.Exists(dest))
                {
                    dest = Path.Combine(root, name + " (" + suffix + ")");
                    suffix++;
                }
                Directory.CreateDirectory(dest);
                for (int i = 0; i < total; i++)
                {
                    if (result[i] == null) continue;
                    string target = Path.Combine(dest, Path.GetFileName(result[i]));
                    File.Copy(result[i], target, true);
                    finalPaths[i] = target;
                }
                name = Path.GetFileName(dest);
            }

            Scheme s = new Scheme();
            s.Name = name;
            s.Paths = finalPaths;
            RegisterScheme(s);
            return name;
        }

        // 从 zip 安装光标包：解压到临时目录 → 找 .cur/.ani 最多的目录 → 复制进 custom/<名字> → 登记方案
        internal static string InstallZip(string zipPath, out int matched)
        {
            matched = 0;
            string tmp = Path.Combine(Path.GetTempPath(), "cs_pack_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                string tmpFull = Path.GetFullPath(tmp).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                using (System.IO.Compression.ZipArchive za = System.IO.Compression.ZipFile.OpenRead(zipPath))
                {
                    foreach (System.IO.Compression.ZipArchiveEntry e in za.Entries)
                    {
                        if (string.IsNullOrEmpty(e.Name)) continue;                 // 目录条目
                        string target = Path.GetFullPath(Path.Combine(tmp, e.FullName.Replace('/', Path.DirectorySeparatorChar)));
                        if (!target.StartsWith(tmpFull, StringComparison.OrdinalIgnoreCase)) continue;   // 防目录穿越
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        e.ExtractToFile(target, true);
                    }
                }

                string best = FindCursorDir(tmp);
                if (best == null) throw new InvalidOperationException("压缩包里没有 .cur / .ani 文件");

                string name = PackName(best);
                string root = CustomDir();
                string dest = Path.Combine(root, name);
                int n = 2;
                while (Directory.Exists(dest)) { dest = Path.Combine(root, name + " (" + n + ")"); n++; }
                Directory.CreateDirectory(dest);

                foreach (string f in Directory.GetFiles(best, "*.*", SearchOption.AllDirectories))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext != ".cur" && ext != ".ani") continue;
                    File.Copy(f, Path.Combine(dest, Path.GetFileName(f)), true);
                }

                int total;
                return ImportPack(dest, out matched, out total);
            }
            finally
            {
                try { Directory.Delete(tmp, true); } catch { }
            }
        }

        private static string FindCursorDir(string root)
        {
            string best = null;
            int bestCount = 0;
            foreach (string d in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
            {
                int c = Directory.GetFiles(d, "*.cur").Length + Directory.GetFiles(d, "*.ani").Length;
                if (c > bestCount) { bestCount = c; best = d; }
            }
            int rc = Directory.GetFiles(root, "*.cur").Length + Directory.GetFiles(root, "*.ani").Length;
            if (rc > bestCount) { best = root; bestCount = rc; }
            return bestCount > 0 ? best : null;
        }

        private static string FindMatch(List<string> pool, string[] keywords)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (string f in pool)
                {
                    string bare = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                    foreach (string kw in keywords)
                    {
                        if (pass == 0 && bare == kw) return f;
                        if (pass == 1 && bare.Contains(kw)) return f;
                    }
                }
            }
            return null;
        }

        internal static Bitmap LoadCursorBitmap(string path, int size)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            int hx, hy;
            Bitmap decoded = DecodeCur(path, out hx, out hy);
            if (decoded != null)
            {
                try { return ScaleCursor(decoded, size, true); }
                finally { decoded.Dispose(); }
            }
            IntPtr h = Native.LoadCursorFromFile(path);
            if (h == IntPtr.Zero) return null;
            try
            {
                using (Icon ico = Icon.FromHandle(h))
                {
                    using (Bitmap src = ico.ToBitmap())
                    {
                        Bitmap dst = new Bitmap(size, size);
                        using (Graphics g = Graphics.FromImage(dst))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.Clear(Color.Transparent);
                            float scale = Math.Min((float)size / src.Width, (float)size / src.Height);
                            int w = Math.Max(1, (int)(src.Width * scale));
                            int hgt = Math.Max(1, (int)(src.Height * scale));
                            g.DrawImage(src, (size - w) / 2, (size - hgt) / 2, w, hgt);
                        }
                        return dst;
                    }
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                Native.DestroyCursor(h);
            }
        }
    }
}