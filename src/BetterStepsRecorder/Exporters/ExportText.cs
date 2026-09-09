using System;

namespace BetterStepsRecorder.Exporters
{
    /// <summary>
    /// 各文档导出器（HTML/Markdown/RTF/ODT/Obsidian）共用的中文文案与格式化工具，
    /// 保证所有导出格式里的措辞、日期与时长格式完全一致。
    /// </summary>
    internal static class ExportText
    {
        // 摘要信息
        public const string SummaryTitle = "摘要";
        public const string StepsTitle = "步骤";
        public const string StepCount = "步骤数";
        public const string Started = "开始时间";
        public const string Finished = "结束时间";
        public const string Duration = "总耗时";

        // 明细信息
        public const string DetailItem = "明细";
        public const string ValueItem = "值";
        public const string ActionLabel = "操作";
        public const string ApplicationLabel = "应用程序";
        public const string WindowLabel = "窗口";
        public const string ElementLabel = "元素";
        public const string ElementTypeLabel = "元素类型";
        public const string MousePositionLabel = "鼠标位置";

        public const string NoScreenshot = "此步骤没有截图。";
        public const string GeneratedWithPrefix = "由 ";
        public const string GeneratedWithSuffix = " 生成";
        public const string AppLinkText = "Better Steps Recorder";
        public const string AppLinkUrl = "https://github.com/Better-World-Solutions/BetterStepsRecorder-Community";

        /// <summary>"步骤 3"</summary>
        public static string StepN(int step) => $"步骤 {step}";

        /// <summary>"步骤 3 截图"</summary>
        public static string StepScreenshot(int step) => $"步骤 {step} 截图";

        /// <summary>"生成于 2026年9月9日 15:30"</summary>
        public static string GeneratedAt(DateTime dt) => $"生成于 {dt:yyyy年M月d日 HH:mm}";

        /// <summary>中文日期时间（带秒），用于摘要信息。</summary>
        public static string FormatDateTime(DateTime dt) => dt.ToString("yyyy年M月d日 HH:mm:ss");

        /// <summary>事件类型中文化（左键点击 / 拖拽到 等）。</summary>
        public static string ActionOf(RecordEvent evt) => Program.EventTypeToChinese(evt.EventType);

        /// <summary>时长格式：1小时2分3秒 / 2分3秒 / 3秒。</summary>
        public static string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}小时{ts.Minutes}分{ts.Seconds}秒";
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}分{ts.Seconds}秒";
            return $"{ts.Seconds}秒";
        }
    }
}
