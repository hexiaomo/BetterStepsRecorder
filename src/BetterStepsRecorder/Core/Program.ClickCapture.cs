using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using FlaUI.Core.AutomationElements;
using static BetterStepsRecorder.WindowHelper;
using Size = BetterStepsRecorder.WindowHelper.Size;

namespace BetterStepsRecorder
{
    internal static partial class Program
    {
        /// <summary>
        /// 按当前设置计算本次点击要截取的屏幕区域（虚拟屏幕坐标）。
        /// 支持：全屏 / 当前显示器 / 活动窗口 / 跟随鼠标（自定义宽高）/ 围绕鼠标裁剪。
        /// </summary>
        public static Rectangle ComputeCaptureRegion(POINT cursor, RECT windowRect, ClickScreenshotMode mode)
        {
            Rectangle vs = SystemInformation.VirtualScreen;

            switch (mode)
            {
                case ClickScreenshotMode.AllScreens:
                    return vs;

                case ClickScreenshotMode.ActiveScreen:
                {
                    Screen screen = Screen.FromPoint(new Point(cursor.X, cursor.Y));
                    return screen.Bounds;
                }

                case ClickScreenshotMode.FollowMouse:
                {
                    var fm = BSRSettings.Current.Screenshot.Click.FollowMouse;
                    int w = Math.Min(Math.Max(fm.Width, 160), vs.Width);
                    int h = Math.Min(Math.Max(fm.Height, 120), vs.Height);

                    int left = cursor.X - w / 2;
                    int top = cursor.Y - h / 2;

                    // 贴边时向内收，保证截取窗口始终完整落在虚拟屏幕内
                    left = Math.Max(vs.Left, Math.Min(left, vs.Right - w));
                    top = Math.Max(vs.Top, Math.Min(top, vs.Bottom - h));

                    return new Rectangle(left, top, w, h);
                }

                case ClickScreenshotMode.Cropped:
                {
                    int pad = BSRSettings.Current.Screenshot.Click.Cropped.Padding;
                    int left = Math.Max(vs.Left, cursor.X - pad);
                    int top = Math.Max(vs.Top, cursor.Y - pad);
                    int right = Math.Min(vs.Right, cursor.X + pad);
                    int bottom = Math.Min(vs.Bottom, cursor.Y + pad);
                    return new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
                }

                default: // ActiveWindow
                {
                    int left = windowRect.Left, top = windowRect.Top;
                    int w = windowRect.Right - windowRect.Left;
                    int h = windowRect.Bottom - windowRect.Top;
                    if (w <= 0 || h <= 0) return vs;
                    return new Rectangle(left, top, w, h);
                }
            }
        }

        /// <summary>
        /// 首次登记某步骤时，补上截图区域与叠加层（鼠标指针 / 提示文字框）的初始值。
        /// 已保存过的工程不会重复初始化。
        /// </summary>
        public static void EnsureCaptureMetadata(RecordEvent evt)
        {
            if (evt.CaptureWidth > 0 && evt.CaptureHeight > 0) return;

            var winRect = new RECT
            {
                Left = evt.WindowCoordinates.Left,
                Top = evt.WindowCoordinates.Top,
                Right = evt.WindowCoordinates.Right,
                Bottom = evt.WindowCoordinates.Bottom
            };

            Rectangle region = ComputeCaptureRegion(evt.MouseCoordinates, winRect,
                                                    BSRSettings.Current.Screenshot.Click.Mode);

            evt.CaptureLeft = region.Left;
            evt.CaptureTop = region.Top;
            evt.CaptureWidth = region.Width;
            evt.CaptureHeight = region.Height;

            InitStepOverlay(evt);
        }

        /// <summary>按当前设置初始化一个步骤的鼠标指针与提示文字框。</summary>
        public static void InitStepOverlay(RecordEvent evt)
        {
            var ind = BSRSettings.Current.Indicator;
            var lbl = BSRSettings.Current.ClickLabel;
            var ov = evt.Overlay ??= new StepOverlay();

            ov.IndicatorEnabled = ind.Enabled;
            ov.IndicatorStyle = ind.Style;
            ov.IndicatorColorArgb = ind.Color;
            ov.IndicatorX = evt.MouseCoordinates.X - evt.CaptureLeft;
            ov.IndicatorY = evt.MouseCoordinates.Y - evt.CaptureTop;

            ov.LabelEnabled = lbl.Enabled;
            ov.LabelText = string.IsNullOrWhiteSpace(lbl.DefaultText) ? "点击此" : lbl.DefaultText;
            ov.LabelFontSize = lbl.FontSize;
            ov.LabelBackArgb = lbl.BackColor;
            ov.LabelBorderArgb = lbl.BorderColor;
            ov.LabelTextArgb = lbl.TextColor;

            System.Drawing.Size box = StepRenderer.MeasureLabelBox(ov);
            ov.LabelWidth = box.Width;
            ov.LabelHeight = box.Height;
            ov.LabelX = ov.IndicatorX + lbl.OffsetX;
            ov.LabelY = ov.IndicatorY + lbl.OffsetY;
        }

        /// <summary>把事件类型翻译成中文步骤描述。</summary>
        public static string DescribeClick(string? appName, string clickType, string? elementType, string? elementName)
        {
            string cn = EventTypeToChinese(clickType);

            if (!string.IsNullOrWhiteSpace(elementName))
                return $"在 {appName} 中{cn}「{elementName}」";
            if (!string.IsNullOrWhiteSpace(elementType))
                return $"在 {appName} 中{cn} {elementType}";
            return $"在 {appName} 中{cn}";
        }

        /// <summary>把事件类型（Left Click / Drag 等）翻译成中文，导出时显示用。</summary>
        public static string EventTypeToChinese(string? eventType) => eventType switch
        {
            "Left Click" => "左键点击",
            "Right Click" => "右键点击",
            "Middle Click" => "中键点击",
            "Drag" => "拖拽到",
            _ => eventType ?? string.Empty
        };

        /// <summary>
        /// 统一处理一次鼠标点击：截图（原始图，不叠加任何标记）→ 建立步骤 → 登记。
        /// 供中键点击使用，左键/右键沿用原有流程但共用同一套区域计算与叠加层初始化。
        /// </summary>
        private static void CaptureClickEvent(POINT cursorPos, string clickType)
        {
            IntPtr hwnd = WindowFromPoint(cursorPos);
            if (hwnd == IntPtr.Zero) return;

            string? windowTitle = GetTopLevelWindowTitle(hwnd);
            string? applicationName = GetApplicationName(hwnd);
            if (applicationName == _ownProcessName && !BSRSettings.Current.General.AllowRecordSelf) return;

            GetWindowRect(hwnd, out RECT uiRect);
            RECT winRect = GetTopLevelWindowRect(hwnd);

            Rectangle region = ComputeCaptureRegion(cursorPos, winRect, BSRSettings.Current.Screenshot.Click.Mode);

            // 截图必须在钩子线程同步完成，否则会后于菜单弹出等界面变化
            Bitmap? bitmap = null;
            try
            {
                bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
                using (Graphics gfx = Graphics.FromImage(bitmap))
                    gfx.CopyFromScreen(region.Left, region.Top, 0, 0,
                        new System.Drawing.Size(region.Width, region.Height), CopyPixelOperation.SourceCopy);
            }
            catch
            {
                bitmap?.Dispose();
                bitmap = null;
            }

            var snapshot = (cursorPos, windowTitle, applicationName, clickType, uiRect, winRect, region, bitmap);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                var (cp, wt, appName, ct, uiR, winR, reg, bmp) = snapshot;

                AutomationElement? element = GetElementFromPoint(new Point(cp.X, cp.Y));
                string? elementName = null, elementType = null;
                if (element != null)
                {
                    try { elementName = element.Properties.Name.IsSupported ? element.Name : null; } catch { }
                    try { elementType = element.Properties.ControlType.IsSupported ? element.ControlType.ToString() : null; } catch { }
                }

                RecordEvent recordEvent;
                lock (_recordEventsLock)
                {
                    recordEvent = new RecordEvent
                    {
                        WindowTitle = wt,
                        ApplicationName = appName,
                        WindowCoordinates = new RECT { Left = winR.Left, Top = winR.Top, Bottom = winR.Bottom, Right = winR.Right },
                        WindowSize = new Size { Width = winR.Right - winR.Left, Height = winR.Bottom - winR.Top },
                        UICoordinates = new RECT { Left = uiR.Left, Top = uiR.Top, Bottom = uiR.Bottom, Right = uiR.Right },
                        UISize = new Size { Width = uiR.Right - uiR.Left, Height = uiR.Bottom - uiR.Top },
                        ElementName = elementName,
                        ElementType = elementType,
                        MouseCoordinates = new POINT { X = cp.X, Y = cp.Y },
                        EventType = ct,
                        _StepText = DescribeClick(appName, ct, elementType, elementName),
                        Step = _recordEvents.Count + 1,
                        CaptureLeft = reg.Left,
                        CaptureTop = reg.Top,
                        CaptureWidth = reg.Width,
                        CaptureHeight = reg.Height
                    };
                    _recordEvents.Add(recordEvent);
                }

                InitStepOverlay(recordEvent);

                if (bmp != null)
                {
                    using (bmp)
                    {
                        using var ms = new MemoryStream();
                        bmp.Save(ms, ImageFormat.Png);
                        byte[] png = ms.ToArray();

                        string? spoolPath = SpoolScreenshot(png, recordEvent.ID);
                        if (spoolPath != null)
                            recordEvent.ScreenshotSpoolPath = spoolPath;
                        else
                            recordEvent.Screenshotb64 = Convert.ToBase64String(png);
                    }
                }

                GC.Collect(2, GCCollectionMode.Optimized, blocking: false);

                _form1Instance?.BeginInvoke((Action)(() =>
                {
                    _form1Instance.AddRecordEventToListBox(recordEvent);
                    _form1Instance.activityTimer.Stop();
                    _form1Instance.activityTimer.Start();
                }));
            });
        }
    }
}
