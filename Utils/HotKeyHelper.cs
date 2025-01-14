using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;

namespace AIAssistant.Utils
{
    public static class HotKeyHelper
    {
        // Windows API 常量
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_WIN = 0x0008;

        // Windows API 函数
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 热键ID
        public const int HOTKEY_ID_SHOW = 9000;
        public const int HOTKEY_ID_HIDE = 9001;

        public static bool RegisterGlobalHotKey(IntPtr handle)
        {
            try
            {
                // 先注销已有的热键
                UnregisterGlobalHotKey(handle);

                // 注册 Alt + Q 显示窗口
                bool showKeyRegistered = RegisterHotKey(handle, HOTKEY_ID_SHOW, MOD_ALT, 0x51); // 0x51 是 Q 键的虚拟键码
                if (!showKeyRegistered)
                {
                    MessageBox.Show("注册 Alt + Q 热键失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // 注册 Esc 隐藏窗口
                bool hideKeyRegistered = RegisterHotKey(handle, HOTKEY_ID_HIDE, 0, 0x1B); // 0x1B 是 Esc 键的虚拟键码
                if (!hideKeyRegistered)
                {
                    MessageBox.Show("注册 Esc 热键失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    UnregisterHotKey(handle, HOTKEY_ID_SHOW); // 清理已注册的热键
                    return false;
                }

                MessageBox.Show("热键注册成功！\nAlt + Q: 显示窗口\nEsc: 隐藏窗口", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"注册热键时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static void UnregisterGlobalHotKey(IntPtr handle)
        {
            try
            {
                UnregisterHotKey(handle, HOTKEY_ID_SHOW);
                UnregisterHotKey(handle, HOTKEY_ID_HIDE);
            }
            catch
            {
                // 忽略注销时的错误
            }
        }
    }
} 