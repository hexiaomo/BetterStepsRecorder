using System.Text.Json.Serialization;

namespace BetterStepsRecorder
{

    public partial class BSRSettings
    {
        // ══════════════════════════════════════════════════════════════════════
        // General Settings
        // ══════════════════════════════════════════════════════════════════════

        public class GeneralSettings
        {
            public MinimizeBehavior MinimizeOnStartRecording { get; set; } = MinimizeBehavior.MinimizeToTaskbar;
            public bool AllowRecordSelf { get; set; } = false;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Indicator Settings
        // ══════════════════════════════════════════════════════════════════════

        public class IndicatorSettings
        {
            /// <summary>截图时是否叠加鼠标指针（默认启用）。</summary>
            public bool Enabled { get; set; } = true;

            public ClickIndicatorStyle Style { get; set; } = ClickIndicatorStyle.Cursor;

            [JsonConverter(typeof(JsonTools.ArgbHexConverter))]
            public int Color { get; set; } = -65281; // Color.Magenta.ToArgb() = #FFFF00FF
        }

        /// <summary>点击位置提示文字框（默认关闭）。</summary>
        public class ClickLabelSettings
        {
            public bool Enabled { get; set; } = false;
            public string DefaultText { get; set; } = "点击此";
            public int FontSize { get; set; } = 16;

            [JsonConverter(typeof(JsonTools.ArgbHexConverter))]
            public int BackColor { get; set; } = Color.FromArgb(230, 255, 255, 255).ToArgb();

            [JsonConverter(typeof(JsonTools.ArgbHexConverter))]
            public int BorderColor { get; set; } = Color.FromArgb(255, 220, 53, 69).ToArgb();

            [JsonConverter(typeof(JsonTools.ArgbHexConverter))]
            public int TextColor { get; set; } = Color.FromArgb(255, 33, 37, 41).ToArgb();

            /// <summary>文字框相对于点击位置的水平偏移。</summary>
            public int OffsetX { get; set; } = 24;

            /// <summary>文字框相对于点击位置的垂直偏移。</summary>
            public int OffsetY { get; set; } = 24;
        }

        /// <summary>跟随鼠标模式下的录取窗口大小。</summary>
        public class FollowMouseSettings
        {
            public int Width { get; set; } = 480;
            public int Height { get; set; } = 360;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Screenshot Settings
        // ══════════════════════════════════════════════════════════════════════

        public class CroppedSettings
        {
            public int Padding { get; set; }
        }

        public class ClickSettings
        {
            public ClickScreenshotMode Mode { get; set; } = ClickScreenshotMode.ActiveWindow;
            public CroppedSettings Cropped { get; set; } = new CroppedSettings { Padding = 200 };
            public FollowMouseSettings FollowMouse { get; set; } = new FollowMouseSettings();
        }

        public class DragFallbackSettings
        {
            public FallbackDragScreenshotMode Mode { get; set; } = FallbackDragScreenshotMode.Cropped;
        }

        public class DragSettings
        {
            public DragScreenshotMode Mode { get; set; } = DragScreenshotMode.ActiveWindow;
            public CroppedSettings Cropped { get; set; } = new CroppedSettings { Padding = 120 };
            public DragFallbackSettings Fallback { get; set; } = new DragFallbackSettings();
        }

        public class ScreenshotSettings
        {
            public ClickSettings Click { get; set; } = new ClickSettings();
            public DragSettings Drag { get; set; } = new DragSettings();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Export Settings
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// HTML / Markdown / RTF / ODT / Obsidian 五种文档导出共有的明细选项。
        /// 统一设置面板通过该接口读写，无需关心具体格式。
        /// </summary>
        public interface IDetailExportSettings
        {
            bool ShowSummary { get; set; }
            bool ShowGeneratedDate { get; set; }
            bool ShowStepTimestamps { get; set; }
            bool ShowAction { get; set; }
            bool ShowApplication { get; set; }
            bool ShowWindow { get; set; }
            bool ShowElement { get; set; }
            bool ShowElementType { get; set; }
            bool ShowMousePosition { get; set; }
            ExportContentPlacement SummaryPlacement { get; set; }
            ExportContentPlacement GeneratedDatePlacement { get; set; }
            bool ShowFooterBranding { get; set; }
            string FooterText { get; set; }
        }

        public enum ExportContentPlacement { Hidden, Header, Footer }

        public class HtmlSettings : IDetailExportSettings
        {
            public bool ShowSummary { get; set; } = true;
            public bool ShowGeneratedDate { get; set; } = true;
            public ExportContentPlacement SummaryPlacement { get; set; } = ExportContentPlacement.Header;
            public ExportContentPlacement GeneratedDatePlacement { get; set; } = ExportContentPlacement.Header;
            public bool ShowFooterBranding { get; set; } = true;
            public string FooterText { get; set; } = "";
            public bool ShowStepTimestamps { get; set; } = false;
            public bool ShowAction { get; set; } = false;
            public bool ShowApplication { get; set; } = false;
            public bool ShowWindow { get; set; } = false;
            public bool ShowElement { get; set; } = false;
            public bool ShowElementType { get; set; } = false;
            public bool ShowMousePosition { get; set; } = false;

            [JsonIgnore]
            public bool IsDetailStripEmpty =>
                !ShowAction && !ShowApplication && !ShowWindow &&
                !ShowElement && !ShowElementType && !ShowMousePosition;
        }

        public class MarkdownSettings : IDetailExportSettings
        {
            public bool ShowSummary { get; set; } = true;
            public bool ShowGeneratedDate { get; set; } = true;
            public ExportContentPlacement SummaryPlacement { get; set; } = ExportContentPlacement.Header;
            public ExportContentPlacement GeneratedDatePlacement { get; set; } = ExportContentPlacement.Header;
            public bool ShowFooterBranding { get; set; } = true;
            public string FooterText { get; set; } = "";
            public bool ShowStepTimestamps { get; set; } = false;
            public bool ShowAction { get; set; } = false;
            public bool ShowApplication { get; set; } = false;
            public bool ShowWindow { get; set; } = false;
            public bool ShowElement { get; set; } = false;
            public bool ShowElementType { get; set; } = false;
            public bool ShowMousePosition { get; set; } = false;

            [JsonIgnore]
            public bool IsDetailTableEmpty =>
                !ShowAction && !ShowApplication && !ShowWindow &&
                !ShowElement && !ShowElementType && !ShowMousePosition;
        }

        public class RtfSettings : IDetailExportSettings
        {
            public bool ShowSummary { get; set; } = true;
            public bool ShowGeneratedDate { get; set; } = true;
            public ExportContentPlacement SummaryPlacement { get; set; } = ExportContentPlacement.Header;
            public ExportContentPlacement GeneratedDatePlacement { get; set; } = ExportContentPlacement.Header;
            public bool ShowFooterBranding { get; set; } = true;
            public string FooterText { get; set; } = "";
            public bool ShowStepTimestamps { get; set; } = false;
            public bool ShowAction { get; set; } = false;
            public bool ShowApplication { get; set; } = false;
            public bool ShowWindow { get; set; } = false;
            public bool ShowElement { get; set; } = false;
            public bool ShowElementType { get; set; } = false;
            public bool ShowMousePosition { get; set; } = false;

            [JsonIgnore]
            public bool IsDetailStripEmpty =>
                !ShowAction && !ShowApplication && !ShowWindow &&
                !ShowElement && !ShowElementType && !ShowMousePosition;
        }

        public class OdtSettings : IDetailExportSettings
        {
            public bool ShowSummary { get; set; } = true;
            public bool ShowGeneratedDate { get; set; } = true;
            public ExportContentPlacement SummaryPlacement { get; set; } = ExportContentPlacement.Header;
            public ExportContentPlacement GeneratedDatePlacement { get; set; } = ExportContentPlacement.Header;
            public bool ShowFooterBranding { get; set; } = true;
            public string FooterText { get; set; } = "";
            public bool ShowStepTimestamps { get; set; } = false;
            public bool ShowAction { get; set; } = false;
            public bool ShowApplication { get; set; } = false;
            public bool ShowWindow { get; set; } = false;
            public bool ShowElement { get; set; } = false;
            public bool ShowElementType { get; set; } = false;
            public bool ShowMousePosition { get; set; } = false;

            [JsonIgnore]
            public bool IsDetailTableEmpty =>
                !ShowAction && !ShowApplication && !ShowWindow &&
                !ShowElement && !ShowElementType && !ShowMousePosition;
        }

        public class ObsidianSettings : IDetailExportSettings
        {
            public bool ShowSummary { get; set; } = true;
            public bool ShowGeneratedDate { get; set; } = true;
            public ExportContentPlacement SummaryPlacement { get; set; } = ExportContentPlacement.Header;
            public ExportContentPlacement GeneratedDatePlacement { get; set; } = ExportContentPlacement.Header;
            public bool ShowFooterBranding { get; set; } = true;
            public string FooterText { get; set; } = "";
            public bool ShowStepTimestamps { get; set; } = false;
            public bool ShowAction { get; set; } = false;
            public bool ShowApplication { get; set; } = false;
            public bool ShowWindow { get; set; } = false;
            public bool ShowElement { get; set; } = false;
            public bool ShowElementType { get; set; } = false;
            public bool ShowMousePosition { get; set; } = false;

            [JsonIgnore]
            public bool IsDetailTableEmpty =>
                !ShowAction && !ShowApplication && !ShowWindow &&
                !ShowElement && !ShowElementType && !ShowMousePosition;
        }

        /// <summary>导出为纵向拼接的一张长图。</summary>
        public class LongImageSettings
        {
            public bool ShowStepNumber { get; set; } = true;
            public bool ShowStepText { get; set; } = true;
            public int Gap { get; set; } = 16;
            public int CaptionHeight { get; set; } = 36;
            /// <summary>统一缩放到该宽度（0 表示保持各自原尺寸）。</summary>
            public int UniformWidth { get; set; } = 0;
        }

        /// <summary>导出为按步骤编号的多张图片。</summary>
        public class ImageSequenceSettings
        {
            /// <summary>文件名是否带步骤说明（如 003_点击此.png）。</summary>
            public bool IncludeStepTextInFileName { get; set; } = true;
            /// <summary>编号位数，3 表示 001、002……</summary>
            public int NumberDigits { get; set; } = 3;
            public string Separator { get; set; } = "_";
        }

        /// <summary>导出 PDF。</summary>
        public class PdfSettings
        {
            public bool ShowStepNumber { get; set; } = true;
            public bool ShowStepText { get; set; } = true;
            /// <summary>A4 / Letter / FitImage：FitImage 表示页面尺寸跟随图片。</summary>
            public string PageSize { get; set; } = "A4";
            public int JpegQuality { get; set; } = 85;
        }

        public class ExportSettings
        {
            public HtmlSettings Html { get; set; } = new HtmlSettings();
            public MarkdownSettings Markdown { get; set; } = new MarkdownSettings();
            public RtfSettings Rtf { get; set; } = new RtfSettings();
            public OdtSettings Odt { get; set; } = new OdtSettings();
            public ObsidianSettings Obsidian { get; set; } = new ObsidianSettings();
            public LongImageSettings LongImage { get; set; } = new LongImageSettings();
            public ImageSequenceSettings ImageSequence { get; set; } = new ImageSequenceSettings();
            public PdfSettings Pdf { get; set; } = new PdfSettings();
        }
    }





    // ══════════════════════════════════════════════════════════════════════
    // Setting Enums
    // ══════════════════════════════════════════════════════════════════════



    /// <summary>
    /// Indicator style for click visualization.
    /// </summary>
    public enum ClickIndicatorStyle
    {
        Arrow,
        Circle,
        Cursor
    }

    /// <summary>
    /// Screenshot mode for click events.
    /// </summary>
    public enum ClickScreenshotMode
    {
        Cropped,
        ActiveWindow,
        ActiveScreen,
        AllScreens,
        /// <summary>跟随鼠标：以鼠标为中心，按设定的宽×高录取窗口。</summary>
        FollowMouse
    }

    /// <summary>
    /// Screenshot mode for drag events.
    /// </summary>
    public enum DragScreenshotMode
    {
        Cropped,
        ActiveWindow,
        ActiveScreen,
        AllScreens
    }

    /// <summary>
    /// Screenshot mode for drag events.
    /// </summary>
    public enum FallbackDragScreenshotMode
    {
        Cropped,
        ActiveScreen,
        AllScreens
        // ActiveWindow is skipped since it is the primary mode that triggers fallback when it fails
    }

    /// <summary>
    /// Mode of minimize on recording start.
    /// </summary>
    public enum MinimizeBehavior
    {
        DoNotMinimize = 0,
        MinimizeToTaskbar = 1,
        MinimizeToSystemTray = 2
    }


}
