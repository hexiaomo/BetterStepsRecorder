using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BetterStepsRecorder.Exporters
{
    /// <summary>
    /// 零依赖的极简 PDF 写入器：每页一张 JPEG 图片（DCTDecode）。
    /// 之所以不引入第三方 PDF 库，是为了避免额外的许可证与依赖负担；
    /// 中文标题由调用方先渲染成位图再嵌入，因此不存在字体嵌入问题。
    /// </summary>
    public sealed class MiniPdfWriter
    {
        private readonly List<Page> _pages = new();

        private sealed class Page
        {
            public byte[] Jpeg = Array.Empty<byte>();
            public int PixelWidth;
            public int PixelHeight;
            public float WidthPt;
            public float HeightPt;
        }

        /// <summary>添加一页。尺寸单位为 point（1pt = 1/72 英寸）。</summary>
        public void AddPage(byte[] jpeg, int pixelWidth, int pixelHeight, float widthPt, float heightPt)
        {
            _pages.Add(new Page
            {
                Jpeg = jpeg,
                PixelWidth = pixelWidth,
                PixelHeight = pixelHeight,
                WidthPt = widthPt,
                HeightPt = heightPt
            });
        }

        public int PageCount => _pages.Count;

        public byte[] Build()
        {
            using var ms = new MemoryStream();
            var offsets = new long[2 + _pages.Count * 3 + 1];

            // 文件头（含二进制标记）
            ms.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));
            ms.Write(new byte[] { 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A });

            // obj 1: Catalog
            offsets[1] = ms.Position;
            Write(ms, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            // obj 2: Pages
            var kids = new StringBuilder();
            for (int i = 0; i < _pages.Count; i++)
                kids.Append(i == 0 ? "" : " ").Append(3 + i * 3).Append(" 0 R");

            offsets[2] = ms.Position;
            Write(ms, $"2 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {_pages.Count} >>\nendobj\n");

            for (int i = 0; i < _pages.Count; i++)
            {
                Page p = _pages[i];
                int pageObj = 3 + i * 3;
                int contentObj = pageObj + 1;
                int imageObj = pageObj + 2;

                // Page
                offsets[pageObj] = ms.Position;
                Write(ms,
                    $"{pageObj} 0 obj\n<< /Type /Page /Parent 2 0 R " +
                    $"/MediaBox [0 0 {F(p.WidthPt)} {F(p.HeightPt)}] " +
                    $"/Resources << /XObject << /Im0 {imageObj} 0 R >> >> " +
                    $"/Contents {contentObj} 0 R >>\nendobj\n");

                // Content stream：整页铺满一张图
                string content = $"q {F(p.WidthPt)} 0 0 {F(p.HeightPt)} 0 0 cm /Im0 Do Q\n";
                offsets[contentObj] = ms.Position;
                Write(ms, $"{contentObj} 0 obj\n<< /Length {content.Length} >>\nstream\n");
                Write(ms, content);
                Write(ms, "endstream\nendobj\n");

                // Image XObject
                offsets[imageObj] = ms.Position;
                Write(ms,
                    $"{imageObj} 0 obj\n<< /Type /XObject /Subtype /Image " +
                    $"/Filter /DCTDecode /Width {p.PixelWidth} /Height {p.PixelHeight} " +
                    $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Length {p.Jpeg.Length} >>\nstream\n");
                ms.Write(p.Jpeg, 0, p.Jpeg.Length);
                Write(ms, "\nendstream\nendobj\n");
            }

            // 交叉引用表
            long xrefOffset = ms.Position;
            int size = 2 + _pages.Count * 3 + 1;
            var sb = new StringBuilder();
            sb.Append("xref\n0 ").Append(size).Append('\n');
            sb.Append("0000000000 65535 f \n");
            for (int i = 1; i < size; i++)
                sb.Append(offsets[i].ToString("D10")).Append(" 00000 n \n");
            Write(ms, sb.ToString());

            Write(ms, $"trailer\n<< /Size {size} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");

            return ms.ToArray();
        }

        private static string F(float v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        private static void Write(Stream s, string text) => s.Write(Encoding.ASCII.GetBytes(text));
    }
}
