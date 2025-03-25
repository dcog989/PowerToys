// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Input;
using System.Windows.Threading;

using ColorPicker.Helpers;
using ColorPicker.Settings;

using static ColorPicker.NativeMethods;

namespace ColorPicker.Mouse
{
    [Export(typeof(IMouseInfoProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MouseInfoProvider : IMouseInfoProvider
    {
        private readonly double _mousePullInfoIntervalInMs;
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly MouseHook _mouseHook;
        private readonly IUserSettings _userSettings;
        private System.Windows.Point _previousMousePosition = new System.Windows.Point(-1, 1);
        private Color _previousColor = Color.Transparent;
        private bool _colorFormatChanged;

        [ImportingConstructor]
        public MouseInfoProvider(AppStateHandler appStateMonitor, IUserSettings userSettings)
        {
            _mousePullInfoIntervalInMs = 1000.0 / GetMainDisplayRefreshRate();
            _timer.Interval = TimeSpan.FromMilliseconds(_mousePullInfoIntervalInMs);
            _timer.Tick += Timer_Tick;

            if (appStateMonitor != null)
            {
                appStateMonitor.AppShown += AppStateMonitor_AppShown;
                appStateMonitor.AppClosed += AppStateMonitor_AppClosed;
                appStateMonitor.AppHidden += AppStateMonitor_AppClosed;
            }

            _mouseHook = new MouseHook();
            _userSettings = userSettings;
            _userSettings.CopiedColorRepresentation.PropertyChanged += CopiedColorRepresentation_PropertyChanged;
            _previousMousePosition = GetCursorPosition();
            _previousColor = GetPixelColor(_previousMousePosition);
        }

        public event EventHandler<Color> MouseColorChanged;

        public event EventHandler<System.Windows.Point> MousePositionChanged;

        public event EventHandler<Tuple<System.Windows.Point, bool>> OnMouseWheel;

        public event MouseUpEventHandler OnMouseDown;

        public event SecondaryMouseUpEventHandler OnSecondaryMouseUp;

        public System.Windows.Point CurrentPosition
        {
            get
            {
                return _previousMousePosition;
            }
        }

        public Color CurrentColor
        {
            get

