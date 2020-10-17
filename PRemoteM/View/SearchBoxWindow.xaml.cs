﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PRM.Core.Model;
using PRM.ViewModel;
using Shawn.Utils;
using Shawn.Utils.PageHost;

namespace PRM.View
{
    /// <summary>
    /// SearchBoxWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SearchBoxWindow : Window
    {
        private readonly VmSearchBox _vmSearchBox = null;


        public SearchBoxWindow()
        {
            InitializeComponent();
            ShowInTaskbar = false;


            double oneItemHeight = (double)FindResource("OneItemHeight");
            double oneActionItemHeight = (double)FindResource("OneActionItemHeight");
            double cornerRadius = (double)FindResource("CornerRadius");
            _vmSearchBox = new VmSearchBox(oneItemHeight, oneActionItemHeight, cornerRadius, GridSelections, GridMenuActions);

            DataContext = _vmSearchBox;
            Loaded += (sender, args) =>
            {
                HideMe();
                Deactivated += (sender1, args1) => { Dispatcher.Invoke(HideMe); };
                KeyDown += (sender1, args1) =>
                {
                    if (args1.Key == Key.Escape) HideMe();
                };
            };
            Show();

            SystemConfig.Instance.QuickConnect.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(SystemConfigQuickConnect.HotKeyKey) ||
                    args.PropertyName == nameof(SystemConfigQuickConnect.HotKeyModifiers))
                {
                    SetHotKey();
                }
            };

            _vmSearchBox.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(VmSearchBox.SelectedIndex))
                {
                    ListBoxSelections.ScrollIntoView(ListBoxSelections.SelectedItem);
                }
            };
        }

        private readonly object _closeLocker = new object();
        private bool _isHidden = false;
        private void HideMe()
        {
            if (_isHidden == false)
                lock (_closeLocker)
                {
                    if (_isHidden == false)
                    {
                        this.Visibility = Visibility.Hidden;
                        _isHidden = true;
                        this.Hide();
                        GridMenuActions.Margin = new Thickness(0, -GridMenuActions.ActualHeight, 0, 0);
                        ListBoxActions.ScrollIntoView(ListBoxActions.SelectedItem);
                        _vmSearchBox.HideActionsList();
                    }
                }
        }




        public void ShowMe()
        {
            SimpleLogHelper.Debug("Call shortcut to invoke quick window.");
            _vmSearchBox.DispNameFilter = "";
            if (SystemConfig.Instance.QuickConnect.Enable)
                if (_isHidden == true)
                    lock (_closeLocker)
                    {
                        if (_isHidden == true)
                        {
                            var p = ScreenInfoEx.GetMouseSystemPosition();
                            var screenEx = ScreenInfoEx.GetCurrentScreenBySystemPosition(p);
                            this.Top = screenEx.VirtualWorkingAreaCenter.Y - this.Height / 2;
                            this.Left = screenEx.VirtualWorkingAreaCenter.X - this.Width / 2;
                            this.Show();
                            this.Visibility = Visibility.Visible;
                            this.Activate();
                            this.Topmost = true;  // important
                            this.Topmost = false; // important
                            this.Focus();         // important
                            TbKeyWord.Focus();
                            _isHidden = false;
                        }
                    }
        }










        private void WindowHeader_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }




        private readonly object _keyDownLocker = new object();
        private void TbKeyWord_OnKeyDown(object sender, KeyEventArgs e)
        {
            lock (_keyDownLocker)
            {
                var key = e.Key;

                if (key == Key.Escape)
                {
                    HideMe();
                    return;
                }
                else if (GridMenuActions.Visibility == Visibility.Visible)
                {
                    switch (key)
                    {
                        case Key.Enter:
                            HideMe();
                            if (_vmSearchBox.Actions.Count > 0
                                && _vmSearchBox.SelectedActionIndex >= 0
                                && _vmSearchBox.SelectedActionIndex < _vmSearchBox.Actions.Count)
                            {
                                _vmSearchBox.Actions[_vmSearchBox.SelectedActionIndex]?.Run();
                            }
                            break;
                        case Key.Down:
                            if (_vmSearchBox.SelectedActionIndex < _vmSearchBox.Actions.Count - 1)
                            {
                                ++_vmSearchBox.SelectedActionIndex;
                                ListBoxActions.ScrollIntoView(ListBoxActions.SelectedItem);
                            }
                            break;
                        case Key.Up:
                            if (_vmSearchBox.SelectedActionIndex > 0)
                            {
                                --_vmSearchBox.SelectedActionIndex;
                                ListBoxActions.ScrollIntoView(ListBoxActions.SelectedItem);
                            }
                            break;
                        case Key.PageUp:
                            if (_vmSearchBox.SelectedActionIndex > 0)
                            {
                                _vmSearchBox.SelectedActionIndex =
                                    _vmSearchBox.SelectedActionIndex - 5 < 0 ? 0 : _vmSearchBox.SelectedActionIndex - 5;
                                ListBoxActions.ScrollIntoView(ListBoxActions.SelectedItem);
                            }
                            break;
                        case Key.PageDown:
                            if (_vmSearchBox.SelectedActionIndex < _vmSearchBox.Actions.Count - 1)
                            {
                                _vmSearchBox.SelectedActionIndex =
                                    _vmSearchBox.SelectedActionIndex + 5 > _vmSearchBox.Actions.Count - 1
                                        ? _vmSearchBox.Actions.Count - 1
                                        : _vmSearchBox.SelectedActionIndex + 5;
                                ListBoxActions.ScrollIntoView(ListBoxActions.SelectedItem);
                            }
                            break;
                        case Key.Left:
                            _vmSearchBox.HideActionsList();
                            break;
                    }
                    e.Handled = true;
                }
                else
                {
                    switch (key)
                    {
                        case Key.Right:
                            if (sender is TextBox tb)
                            {
                                if (tb.CaretIndex != tb.Text.Length)
                                {
                                    return;
                                }
                            }

                            if (_vmSearchBox.SelectedIndex >= 0 &&
                                _vmSearchBox.SelectedIndex < GlobalData.Instance.VmItemList.Count)
                            {
                                _vmSearchBox.ShowActionsList();
                            }
                            e.Handled = true;
                            break;
                        case Key.Enter:
                            HideMe();
                            if (_vmSearchBox.SelectedIndex >= 0 && _vmSearchBox.SelectedIndex < GlobalData.Instance.VmItemList.Count)
                            {
                                var s = GlobalData.Instance.VmItemList[_vmSearchBox.SelectedIndex];
                                GlobalEventHelper.OnServerConnect?.Invoke(s.Server.Id);
                            }
                            break;
                        case Key.Down:
                            if (_vmSearchBox.SelectedIndex < GlobalData.Instance.VmItemList.Count - 1)
                            {
                                var index = _vmSearchBox.SelectedIndex;
                                for (int i = _vmSearchBox.SelectedIndex + 1; i < GlobalData.Instance.VmItemList.Count; i++)
                                {
                                    if (GlobalData.Instance.VmItemList[i].ObjectVisibility == Visibility)
                                    {
                                        index = i;
                                        break;
                                    }
                                }
                                _vmSearchBox.SelectedIndex = index;
                            }
                            break;
                        case Key.Up:
                            if (_vmSearchBox.SelectedIndex > 0)
                            {
                                var index = _vmSearchBox.SelectedIndex;
                                for (int i = _vmSearchBox.SelectedIndex - 1; i >= 0; i--)
                                {
                                    if (GlobalData.Instance.VmItemList[i].ObjectVisibility == Visibility)
                                    {
                                        index = i;
                                        break;
                                    }
                                }
                                _vmSearchBox.SelectedIndex = index;
                            }
                            break;
                        case Key.PageUp:
                            if (_vmSearchBox.SelectedIndex > 0)
                            {
                                var index = _vmSearchBox.SelectedIndex;
                                int count = 0;
                                for (int i = _vmSearchBox.SelectedIndex - 1; i >= 0; i--)
                                {
                                    if (GlobalData.Instance.VmItemList[i].ObjectVisibility == Visibility)
                                    {
                                        ++count;
                                        index = i;
                                        if (count == 5)
                                            break;
                                    }
                                }
                                _vmSearchBox.SelectedIndex = index;
                            }
                            break;
                        case Key.PageDown:
                            if (_vmSearchBox.SelectedIndex < GlobalData.Instance.VmItemList.Count - 1)
                            {
                                var index = _vmSearchBox.SelectedIndex;
                                int count = 0;
                                for (int i = _vmSearchBox.SelectedIndex + 1; i < GlobalData.Instance.VmItemList.Count; i++)
                                {
                                    if (GlobalData.Instance.VmItemList[i].ObjectVisibility == Visibility)
                                    {
                                        ++count;
                                        index = i;
                                        if (count == 5)
                                            break;
                                    }
                                }
                                _vmSearchBox.SelectedIndex = index;
                            }
                            break;
                    }
                }
            }
        }


        /// <summary>
        /// use it after Show() has been called
        /// </summary>
        public void SetHotKey()
        {
            GlobalHotkeyHooker.Instance.Unregist(this);
            var r = GlobalHotkeyHooker.Instance.Regist(this, SystemConfig.Instance.QuickConnect.HotKeyModifiers, SystemConfig.Instance.QuickConnect.HotKeyKey, this.ShowMe);
            var title = SystemConfig.Instance.Language.GetText("messagebox_title_warning");
            switch (r.Item1)
            {
                case GlobalHotkeyHooker.RetCode.Success:
                    break;
                case GlobalHotkeyHooker.RetCode.ERROR_HOTKEY_NOT_REGISTERED:
                    {
                        var msg = $"{SystemConfig.Instance.Language.GetText("info_hotkey_registered_fail")}: {r.Item2}";
                        SimpleLogHelper.Warning(msg);
                        MessageBox.Show(msg, title);
                        break;
                    }
                case GlobalHotkeyHooker.RetCode.ERROR_HOTKEY_ALREADY_REGISTERED:
                    {
                        var msg = $"{SystemConfig.Instance.Language.GetText("info_hotkey_already_registered")}: {r.Item2}";
                        SimpleLogHelper.Warning(msg);
                        MessageBox.Show(msg, title);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
