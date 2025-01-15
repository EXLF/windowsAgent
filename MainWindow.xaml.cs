using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using AIAssistant.Utils;
using AIAssistant.Services;

namespace AIAssistant
{
    public partial class MainWindow : Window
    {
        private NotifyIcon _notifyIcon;
        private IntPtr _windowHandle;
        private readonly DeepseekService _deepseekService;
        private List<Dictionary<string, string>> _chatHistory;
        private bool _isProcessing = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeNotifyIcon();
            
            // 初始化时隐藏窗口
            this.Hide();

            // 初始化服务和历史记录
            _deepseekService = new DeepseekService();
            _chatHistory = new List<Dictionary<string, string>>();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // 获取窗口句柄
            _windowHandle = new WindowInteropHelper(this).Handle;
            
            // 添加消息处理
            HwndSource source = HwndSource.FromHwnd(_windowHandle);
            source.AddHook(WndProc);
            
            // 注册热键
            HotKeyHelper.RegisterGlobalHotKey(_windowHandle);
            
            // 处理关闭按钮
            this.Closing += MainWindow_Closing;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();
                switch (hotkeyId)
                {
                    case HotKeyHelper.HOTKEY_ID_SHOW:
                        ShowAndActivate();
                        handled = true;
                        break;
                    case HotKeyHelper.HOTKEY_ID_HIDE:
                        this.Hide();
                        handled = true;
                        break;
                }
            }
            return IntPtr.Zero;
        }

        private void ShowAndActivate()
        {
            this.Show();
            this.Activate();
            // 将窗口移动到屏幕中央
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 点击关闭按钮时隐藏窗口而不是退出程序
            e.Cancel = true;
            this.Hide();
        }

        protected override void OnClosed(EventArgs e)
        {
            // 注销热键
            HotKeyHelper.UnregisterGlobalHotKey(_windowHandle);
            // 清理托盘图标
            _notifyIcon.Dispose();
            base.OnClosed(e);
        }

        private void InitializeNotifyIcon()
        {
            string iconUrl = "https://img.icons8.com/?size=100&id=37410&format=png";
            _notifyIcon = new NotifyIcon
            {
                Icon = IconHelper.CreateIconFromPng(iconUrl),
                Visible = true,
                Text = "AI Assistant (Alt + Q: 显示, Esc: 隐藏)"
            };

            // 添加双击事件处理
            _notifyIcon.DoubleClick += (s, e) => ShowAndActivate();

            // 创建右键菜单
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("显示 (Alt + Q)", null, ShowWindow);
            contextMenu.Items.Add("退出", null, ExitApplication);
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void ShowWindow(object sender, EventArgs e)
        {
            ShowAndActivate();
        }

        private void ExitApplication(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !_isProcessing)
            {
                await SendMessage();
            }
        }

        private async System.Threading.Tasks.Task SendMessage()
        {
            if (_isProcessing || string.IsNullOrWhiteSpace(InputTextBox.Text))
                return;

            _isProcessing = true;
            string userInput = InputTextBox.Text;
            InputTextBox.Text = string.Empty;

            // 添加用户消息到界面
            AddMessageToChat("用户", userInput, false);

            try
            {
                // 调用 API
                var response = await _deepseekService.GetResponseAsync(userInput, _chatHistory);

                // 添加对话历史
                _chatHistory.Add(new Dictionary<string, string> { { "role", "user" }, { "content", userInput } });
                _chatHistory.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", response } });

                // 添加助手回复到界面
                AddMessageToChat("AI", response, true);
            }
            catch (Exception ex)
            {
                AddMessageToChat("系统", $"错误: {ex.Message}", true, true);
            }
            finally
            {
                _isProcessing = false;
                InputTextBox.Focus();
            }
        }

        private void AddMessageToChat(string sender, string message, bool isReceived, bool isError = false)
        {
            var messageContainer = new Border
            {
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(10),
                MaxWidth = 500,
                Background = isError ? new SolidColorBrush(Colors.LightPink) :
                           isReceived ? new SolidColorBrush(Colors.White) : 
                                      new SolidColorBrush(Colors.LightBlue),
                HorizontalAlignment = isReceived ? System.Windows.HorizontalAlignment.Left : 
                                                 System.Windows.HorizontalAlignment.Right
            };

            var stackPanel = new StackPanel();

            // 发送者名称
            var senderText = new TextBlock
            {
                Text = sender,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            stackPanel.Children.Add(senderText);

            // 消息内容
            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(messageText);

            messageContainer.Child = stackPanel;
            ChatContent.Children.Add(messageContainer);

            // 滚动到底部
            ChatScrollViewer.ScrollToBottom();
        }
    }
} 