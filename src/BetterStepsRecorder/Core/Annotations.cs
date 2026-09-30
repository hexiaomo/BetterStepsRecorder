using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;

namespace BetterStepsRecorder
{
    /// <summary>
    /// 截图上叠加的标注类型。全部以"对象"形式保存（非破坏式），
    /// 因此录制结束后仍可拖动、改文案、删除。
    /// </summary>
    public enum AnnotationKind
    {
        Mosaic,     // 马赛克
        Highlight,  // 半透明高亮
        Arrow,      // 箭头
        Text,       // 纯文字
        TextBox,    // 带边框文字框
        Rectangle,  // 矩形框
        Ellipse     // 椭圆框
    }

    /// <summary>
    /// 单个标注对象，坐标均为【截图内像素坐标】。
    /// </summary>
    public class Annotation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public AnnotationKind Kind { get; set; }

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        // 箭头两端点（截图内像素坐标）
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }

        public string Text { get; set; } = string.Empty;

        public int ColorArgb { get; set; } = Color.FromArgb(255, 255, 0, 0).ToArgb();
        public int FontSize { get; set; } = 16;

        /// <summary>文字框是否绘制边框与底色。</summary>
        public bool Bordered { get; set; } = true;

        /// <summary>马赛克块大小（像素），越大越糊。</summary>
        public int MosaicBlock { get; set; } = 10;

        [JsonIgnore]
        public Rectangle Bounds => new Rectangle(X, Y, Width, Height);

        [JsonIgnore]
        public Point StartPoint => new Point(StartX, StartY);

        [JsonIgnore]
        public Point EndPoint => new Point(EndX, EndY);

        [JsonIgnore]
        public Color Color => Color.FromArgb(ColorArgb);

        public Annotation Clone() => (Annotation)MemberwiseClone();
    }

    /// <summary>
    /// 一个步骤（一张截图）上所有可编辑的叠加内容。
    /// 底图始终保持"原始截图"，所有显示与导出都通过 StepRenderer 实时合成。
    /// </summary>
    public class StepOverlay
    {
        // ── 鼠标指针 ────────────────────────────────────────────────
        public bool IndicatorEnabled { get; set; } = true;
        public ClickIndicatorStyle IndicatorStyle { get; set; } = ClickIndicatorStyle.Cursor;
        public int IndicatorColorArgb { get; set; } = Color.FromArgb(255, 255, 0, 255).ToArgb();
        public int IndicatorSize { get; set; } = 100;
        public string IndicatorCustomImagePath { get; set; } = string.Empty;
        public int IndicatorX { get; set; }
        public int IndicatorY { get; set; }

        // ── 点击提示文字框 ──────────────────────────────────────────
        public bool LabelEnabled { get; set; } = false;
        public string LabelText { get; set; } = "点击此";
        public int LabelX { get; set; }
        public int LabelY { get; set; }
        public int LabelWidth { get; set; }
        public int LabelHeight { get; set; }
        public int LabelFontSize { get; set; } = 16;
        public int LabelBackArgb { get; set; } = Color.FromArgb(230, 255, 255, 255).ToArgb();
        public int LabelBorderArgb { get; set; } = Color.FromArgb(255, 220, 53, 69).ToArgb();
        public int LabelTextArgb { get; set; } = Color.FromArgb(255, 33, 37, 41).ToArgb();

        // ── 用户标注 ────────────────────────────────────────────────
        public List<Annotation> Annotations { get; set; } = new List<Annotation>();

        // ── 裁剪（null 表示未裁剪）──────────────────────────────────
        public int? CropX { get; set; }
        public int? CropY { get; set; }
        public int? CropWidth { get; set; }
        public int? CropHeight { get; set; }

        [JsonIgnore]
        public bool HasCrop => CropWidth.HasValue && CropHeight.HasValue && CropWidth > 0 && CropHeight > 0;

        [JsonIgnore]
        public Rectangle CropRect => new Rectangle(CropX ?? 0, CropY ?? 0, CropWidth ?? 0, CropHeight ?? 0);

        /// <summary>把裁剪区域应用为"偏移"，使后续标注坐标保持相对裁剪后图像。</summary>
        public void ApplyCrop(Rectangle crop)
        {
            int dx = crop.X, dy = crop.Y;
            CropX = crop.X; CropY = crop.Y; CropWidth = crop.Width; CropHeight = crop.Height;

            IndicatorX -= dx; IndicatorY -= dy;
            LabelX -= dx; LabelY -= dy;

            foreach (var a in Annotations)
            {
                a.X -= dx; a.Y -= dy;
                a.StartX -= dx; a.StartY -= dy;
                a.EndX -= dx; a.EndY -= dy;
            }
        }
    }
}
