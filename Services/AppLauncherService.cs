using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace AIAssistant.Services
{
    public class AppLauncherService
    {
        private readonly Dictionary<string, string> _appPaths;
        private readonly Dictionary<string, string> _controlPanelItems;
        private readonly string[] _commonProgramPaths = new[]
        {
            @"C:\Program Files",
            @"C:\Program Files (x86)",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
        };

        public AppLauncherService()
        {
            _controlPanelItems = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"网络设置", "ncpa.cpl"},
                {"网络连接", "ncpa.cpl"},
                {"网络适配器", "ncpa.cpl"},
                {"网络和共享中心", "control.exe /name Microsoft.NetworkAndSharingCenter"},
                {"系统设置", "control.exe system"},
                {"声音设置", "mmsys.cpl"},
                {"防火墙设置", "firewall.cpl"},
                {"电源选项", "powercfg.cpl"},
                {"区域设置", "intl.cpl"},
                {"鼠标设置", "main.cpl"},
                {"显示设置", "desk.cpl"},
                {"设备管理器", "devmgmt.msc"},
                {"服务", "services.msc"},
                {"任务计划", "taskschd.msc"}
            };

            _appPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // 系统应用
                {"记事本", "notepad.exe"},
                {"计算器", "calc.exe"},
                {"命令提示符", "cmd.exe"},
                {"控制面板", "control.exe"},
                {"任务管理器", "taskmgr.exe"},
                {"资源管理器", "explorer.exe"},
                
                // 常用文件夹
                {"C盘", @"C:\"},
                {"D盘", @"D:\"},
                {"下载", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"},
                {"桌面", Environment.GetFolderPath(Environment.SpecialFolder.Desktop)},
                {"文档", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}
            };

            // 添加常用应用程序
            AddCommonApps();
        }

        private void AddCommonApps()
        {
            // 微信相关
            TryAddApp("微信", "WeChat.exe", @"Tencent\WeChat");
            TryAddApp("企业微信", "WXWork.exe", @"Tencent\WXWork");
            TryAddApp("企业微信", "WXWork.exe", @"WXWork");
            
            // 浏览器
            TryAddApp("Chrome", "chrome.exe", @"Google\Chrome\Application");
            TryAddApp("Edge", "msedge.exe", @"Microsoft\Edge\Application");
            TryAddApp("火狐", "firefox.exe", @"Mozilla Firefox");
            
            // 办公软件
            string[] officeVersions = { "Office16", "Office15", "Office14" };
            foreach (var version in officeVersions)
            {
                TryAddApp("Word", "WINWORD.EXE", $@"Microsoft Office\{version}\");
                TryAddApp("Excel", "EXCEL.EXE", $@"Microsoft Office\{version}\");
                TryAddApp("PowerPoint", "POWERPNT.EXE", $@"Microsoft Office\{version}\");
            }

            // 常用工具
            TryAddApp("EV录屏", "EV.exe", @"EVCapture");
            TryAddApp("EV录屏", "EV.exe", @"Program Files\EVCapture");
            TryAddApp("EV录屏", "EV.exe", @"Program Files (x86)\EVCapture");
            TryAddApp("钉钉", "DingTalk.exe", @"DingTalk");
            TryAddApp("有道云笔记", "YoudaoNote.exe", @"Youdao\YoudaoNote");
            TryAddApp("网易云音乐", "CloudMusic.exe", @"Netease\CloudMusic");
            TryAddApp("QQ", "QQ.exe", @"Tencent\QQ");
            TryAddApp("QQ音乐", "QQMusic.exe", @"Tencent\QQMusic");

            // 从注册表获取已安装应用
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths"))
                {
                    if (key != null)
                    {
                        foreach (var appName in key.GetSubKeyNames())
                        {
                            using (var appKey = key.OpenSubKey(appName))
                            {
                                var path = appKey?.GetValue("")?.ToString();
                                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                                {
                                    var name = Path.GetFileNameWithoutExtension(appName);
                                    _appPaths[name] = path;
                                }
                            }
                        }
                    }
                }

                // 搜索用户的 Start Menu
                var startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                var programsPath = Path.Combine(startMenuPath, "Programs");
                SearchShortcuts(programsPath);

                // 搜索公共 Start Menu
                startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
                programsPath = Path.Combine(startMenuPath, "Programs");
                SearchShortcuts(programsPath);
            }
            catch { /* 忽略注册表和文件系统访问错误 */ }
        }

        private void SearchShortcuts(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            try
            {
                foreach (var file in Directory.GetFiles(folderPath, "*.lnk", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    _appPaths[name] = file;
                }
            }
            catch { /* 忽略访问错误 */ }
        }

        private void TryAddApp(string appName, string exeName, string relativePath)
        {
            foreach (var basePath in _commonProgramPaths)
            {
                if (string.IsNullOrEmpty(basePath)) continue;

                try
                {
                    var fullPath = Path.Combine(basePath, relativePath, exeName);
                    if (File.Exists(fullPath))
                    {
                        _appPaths[appName] = fullPath;
                        return;
                    }

                    // 尝试在子目录中查找
                    if (Directory.Exists(basePath))
                    {
                        var possiblePath = Directory.GetFiles(basePath, exeName, SearchOption.AllDirectories)
                            .FirstOrDefault();
                        if (!string.IsNullOrEmpty(possiblePath))
                        {
                            _appPaths[appName] = possiblePath;
                            return;
                        }
                    }
                }
                catch { /* 忽略访问错误 */ }
            }
        }

        public bool TryLaunchApp(string appName, out string message)
        {
            try
            {
                // 检查是否是控制面板项
                if (_controlPanelItems.TryGetValue(appName, out string controlCommand))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = controlCommand.Split(' ')[0],
                        Arguments = string.Join(" ", controlCommand.Split(' ').Skip(1)),
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                    message = $"已打开 {appName}";
                    return true;
                }

                // 检查是否是驱动器路径（如 C盘、D盘等）
                if (appName.EndsWith("盘", StringComparison.OrdinalIgnoreCase))
                {
                    var driveLetter = appName[0] + ":\\";
                    if (Directory.Exists(driveLetter))
                    {
                        Process.Start("explorer.exe", driveLetter);
                        message = $"已打开 {appName}";
                        return true;
                    }
                }

                // 检查是否是完整路径
                if (Directory.Exists(appName))
                {
                    Process.Start("explorer.exe", appName);
                    message = $"已打开文件夹: {appName}";
                    return true;
                }

                if (_appPaths.TryGetValue(appName, out string path))
                {
                    if (Directory.Exists(path))
                    {
                        Process.Start("explorer.exe", path);
                        message = $"已打开文件夹: {appName}";
                        return true;
                    }
                    else if (File.Exists(path))
                    {
                        if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = path,
                                UseShellExecute = true
                            });
                        }
                        else
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = path,
                                UseShellExecute = true
                            });
                        }
                        message = $"已启动 {appName}";
                        return true;
                    }
                    message = $"找不到应用程序或文件夹: {appName}";
                    return false;
                }

                // 尝试直接启动
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = appName,
                        UseShellExecute = true
                    });
                    message = $"已启动 {appName}";
                    return true;
                }
                catch
                {
                    message = $"无法启动应用程序: {appName}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                message = $"启动失败: {ex.Message}";
                return false;
            }
        }

        private bool IsSystemCommand(string command)
        {
            return command.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                   (command.Contains("\\") == false);
        }

        public void AddAppPath(string appName, string path)
        {
            _appPaths[appName] = path;
        }

        public bool RemoveAppPath(string appName)
        {
            return _appPaths.Remove(appName);
        }

        public IReadOnlyDictionary<string, string> GetAppPaths()
        {
            return _appPaths;
        }
    }
} 