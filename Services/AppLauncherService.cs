using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace AIAssistant.Services
{
    public class AppLauncherService
    {
        private readonly Dictionary<string, string> _appPaths;
        private readonly Dictionary<string, string> _controlPanelItems;
        private readonly Dictionary<string, HashSet<string>> _appAliases;
        private readonly string[] _commonProgramPaths;
        private readonly string[] _registrySearchPaths;

        public AppLauncherService()
        {
            _appPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _appAliases = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            _controlPanelItems = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            // 初始化注册表搜索路径
            _registrySearchPaths = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Classes\Applications",
                @"SOFTWARE\RegisteredApplications",
                @"SOFTWARE\Clients"
            };
            
            // 初始化搜索路径
            _commonProgramPaths = new[]
            {
                @"C:\Program Files",
                @"C:\Program Files (x86)",
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Roaming"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop"),
                @"D:\Program Files",
                @"D:\Program Files (x86)",
                @"E:\Program Files",
                @"E:\Program Files (x86)"
            };
            
            // 初始化控制面板项
            InitializeControlPanelItems();
            
            // 初始化系统应用和文件夹
            InitializeSystemApps();
            
            // 初始化应用程序别名
            InitializeAppAliases();
            
            // 搜索并添加应用程序
            SearchAndAddApps();
        }

        private void InitializeControlPanelItems()
        {
            // 添加控制面板项
            _controlPanelItems.Add("网络设置", "ncpa.cpl");
            _controlPanelItems.Add("网络连接", "ncpa.cpl");
            _controlPanelItems.Add("网络适配器", "ncpa.cpl");
            _controlPanelItems.Add("网络和共享中心", "control.exe /name Microsoft.NetworkAndSharingCenter");
            _controlPanelItems.Add("系统设置", "control.exe system");
            _controlPanelItems.Add("声音设置", "mmsys.cpl");
            _controlPanelItems.Add("防火墙设置", "firewall.cpl");
            _controlPanelItems.Add("电源选项", "powercfg.cpl");
            _controlPanelItems.Add("区域设置", "intl.cpl");
            _controlPanelItems.Add("鼠标设置", "main.cpl");
            _controlPanelItems.Add("显示设置", "desk.cpl");
            _controlPanelItems.Add("设备管理器", "devmgmt.msc");
            _controlPanelItems.Add("服务", "services.msc");
            _controlPanelItems.Add("任务计划", "taskschd.msc");
        }

        private void InitializeSystemApps()
        {
            var systemApps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"记事本", "notepad.exe"},
                {"计算器", "calc.exe"},
                {"命令提示符", "cmd.exe"},
                {"控制面板", "control.exe"},
                {"任务管理器", "taskmgr.exe"},
                {"资源管理器", "explorer.exe"},
                
                {"C盘", @"C:\"},
                {"D盘", @"D:\"},
                {"下载", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"},
                {"桌面", Environment.GetFolderPath(Environment.SpecialFolder.Desktop)},
                {"文档", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}
            };

            foreach (var app in systemApps)
            {
                _appPaths[app.Key] = app.Value;
            }
        }

        private void InitializeAppAliases()
        {
            // 添加常见应用程序的别名
            AddAppAlias("记事本", new[] { "notepad", "文本编辑器" });
            AddAppAlias("计算器", new[] { "calc", "calculator" });
            AddAppAlias("画图", new[] { "mspaint", "paint" });
            AddAppAlias("命令提示符", new[] { "cmd", "命令行", "终端" });
            AddAppAlias("控制面板", new[] { "control", "控制台" });
            AddAppAlias("任务管理器", new[] { "taskmgr", "进程管理器" });
            AddAppAlias("资源管理器", new[] { "explorer", "文件管理器" });
            
            // 游戏平台
            AddAppAlias("Steam", new[] { "steam.exe", "steam客户端", "steam平台" });
            AddAppAlias("Epic Games", new[] { "EpicGamesLauncher.exe", "epic", "epic游戏" });
            AddAppAlias("WeGame", new[] { "wegame.exe", "腾讯游戏平台" });
            
            // 网盘软件
            AddAppAlias("阿里云盘", new[] { "aDrive.exe", "adrive", "阿里网盘" });
            AddAppAlias("百度网盘", new[] { "baidunetdisk.exe", "百度云" });
            AddAppAlias("OneDrive", new[] { "onedrive.exe", "微软网盘" });
            
            // 浏览器
            AddAppAlias("Chrome", new[] { "chrome.exe", "谷歌浏览器", "谷歌", "浏览器" });
            AddAppAlias("Edge", new[] { "msedge.exe", "微软浏览器", "edge浏览器" });
            AddAppAlias("Firefox", new[] { "firefox.exe", "火狐", "火狐浏览器" });
            
            // 办公软件
            AddAppAlias("Word", new[] { "winword.exe", "microsoft word", "word文档" });
            AddAppAlias("Excel", new[] { "excel.exe", "microsoft excel", "表格" });
            AddAppAlias("PowerPoint", new[] { "powerpnt.exe", "microsoft powerpoint", "ppt" });
            
            // 聊天软件
            AddAppAlias("微信", new[] { "wechat.exe", "weixin.exe", "wechat" });
            AddAppAlias("QQ", new[] { "qq.exe", "腾讯QQ", "tim.exe" });
            AddAppAlias("企业微信", new[] { "wxwork.exe", "企微", "workweixin.exe" });
            
            // 常用工具
            AddAppAlias("钉钉", new[] { "dingtalk.exe", "阿里钉钉" });
            AddAppAlias("网易云音乐", new[] { "cloudmusic.exe", "网易云" });
            AddAppAlias("有道云笔记", new[] { "youdaonote.exe", "有道笔记" });
            AddAppAlias("Snipaste", new[] { "snipaste.exe", "截图工具" });
            AddAppAlias("VSCode", new[] { "code.exe", "visual studio code" });
            
            // 添加哔哩哔哩的别名
            AddAppAlias("哔哩哔哩", new[] { "bilibili", "哔哩", "b站", "bilibili客户端", "bilibiliClient" });
        }

        private void AddAppAlias(string mainName, string[] aliases)
        {
            if (!_appAliases.ContainsKey(mainName))
            {
                _appAliases[mainName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            
            foreach (var alias in aliases)
            {
                _appAliases[mainName].Add(alias);
            }
        }

        private void SearchAndAddApps()
        {
            try
            {
                // 搜索注册表
                SearchRegistry();
                
                // 搜索文件系统
                SearchFileSystem();
                
                // 搜索开始菜单
                SearchStartMenu();
                
                // 添加预定义的应用程序
                AddPredefinedApps();
            }
            catch (Exception ex)
            {
                // 记录错误但继续执行
                Console.WriteLine($"搜索应用程序时出错: {ex.Message}");
            }
        }

        private void SearchRegistry()
        {
            foreach (var registryPath in _registrySearchPaths)
            {
                try
                {
                    // 检查 HKLM
                    using (var key = Registry.LocalMachine.OpenSubKey(registryPath))
                    {
                        if (key != null)
                        {
                            ProcessRegistryKey(key);
                        }
                    }

                    // 检查 HKCU
                    using (var key = Registry.CurrentUser.OpenSubKey(registryPath))
                    {
                        if (key != null)
                        {
                            ProcessRegistryKey(key);
                        }
                    }

                    // 检查 Steam 特定路径
                    using (var steamKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Valve\Steam"))
                    {
                        if (steamKey != null)
                        {
                            var installPath = steamKey.GetValue("InstallPath")?.ToString();
                            if (!string.IsNullOrEmpty(installPath))
                            {
                                var steamExe = Path.Combine(installPath, "steam.exe");
                                if (File.Exists(steamExe))
                                {
                                    _appPaths["Steam"] = steamExe;
                                }
                            }
                        }
                    }

                    // 检查阿里云盘特定路径
                    var adriveLocations = new[]
                    {
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "aDrive", "aDrive.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "aDrive", "aDrive.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "aDrive", "aDrive.exe")
                    };

                    foreach (var location in adriveLocations)
                    {
                        if (File.Exists(location))
                        {
                            _appPaths["阿里云盘"] = location;
                            break;
                        }
                    }
                }
                catch { /* 忽略单个注册表路径的错误 */ }
            }
        }

        private void ProcessRegistryKey(RegistryKey key)
        {
            foreach (var subKeyName in key.GetSubKeyNames())
            {
                try
                {
                    using (var subKey = key.OpenSubKey(subKeyName))
                    {
                        // 尝试获取默认值
                        var defaultValue = subKey?.GetValue("")?.ToString();
                        if (!string.IsNullOrEmpty(defaultValue) && File.Exists(defaultValue))
                        {
                            var name = Path.GetFileNameWithoutExtension(subKeyName);
                            _appPaths[name] = defaultValue;
                            continue;
                        }

                        // 尝试获取安装路径
                        var installLocation = subKey?.GetValue("InstallLocation")?.ToString();
                        var displayName = subKey?.GetValue("DisplayName")?.ToString();
                        if (!string.IsNullOrEmpty(installLocation) && !string.IsNullOrEmpty(displayName))
                        {
                            var exeFiles = Directory.GetFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly);
                            if (exeFiles.Length > 0)
                            {
                                _appPaths[displayName] = exeFiles[0];
                            }
                        }
                    }
                }
                catch { /* 忽略单个子键的错误 */ }
            }
        }

        private void SearchFileSystem()
        {
            foreach (var basePath in _commonProgramPaths)
            {
                if (!Directory.Exists(basePath)) continue;

                try
                {
                    // 搜索所有可执行文件
                    var exeFiles = Directory.GetFiles(basePath, "*.exe", SearchOption.AllDirectories);
                    foreach (var exePath in exeFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(exePath);
                        _appPaths[fileName] = exePath;

                        // 为一些特殊的应用程序添加额外的映射
                        switch (fileName.ToLower())
                        {
                            case "steam":
                                _appPaths["Steam"] = exePath;
                                break;
                            case "adrive":
                                _appPaths["阿里云盘"] = exePath;
                                _appPaths["adrive"] = exePath;
                                break;
                        }
                    }
                }
                catch { /* 忽略访问错误 */ }
            }
        }

        private void SearchStartMenu()
        {
            var startMenuPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "Start Menu")
            };

            foreach (var startMenuPath in startMenuPaths)
            {
                if (Directory.Exists(startMenuPath))
                {
                    SearchShortcuts(startMenuPath);
                }
            }
        }

        private void SearchShortcuts(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            try
            {
                foreach (var file in Directory.GetFiles(folderPath, "*.lnk", SearchOption.AllDirectories))
                {
                    try
                    {
                        var targetPath = ResolveShortcutTarget(file);
                        if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
                        {
                            var name = Path.GetFileNameWithoutExtension(file);
                            _appPaths[name] = file;

                            // 添加不带后缀的版本
                            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                _appPaths[name.Substring(0, name.Length - 4)] = file;
                            }

                            // 为特定应用添加额外的映射
                            var lowerName = name.ToLower();
                            if (lowerName.Contains("bilibili") || lowerName.Contains("哔哩哔哩"))
                            {
                                _appPaths["bilibili"] = file;
                                _appPaths["哔哩哔哩"] = file;
                                _appPaths["b站"] = file;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"处理快捷方式出错 {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"搜索快捷方式出错: {ex.Message}");
            }
        }

        private void AddPredefinedApps()
        {
            // ... existing predefined apps ...
        }

        public bool TryLaunchApp(string appName, out string message)
        {
            try
            {
                // 记录开始搜索
                Console.WriteLine($"开始搜索应用程序: {appName}");

                // 1. 直接匹配
                if (_appPaths.TryGetValue(appName, out string path))
                {
                    Console.WriteLine($"直接匹配成功: {path}");
                    return LaunchAppWithPath(path, appName, out message);
                }

                // 2. 检查控制面板项
                if (_controlPanelItems.TryGetValue(appName, out string controlCommand))
                {
                    Console.WriteLine($"控制面板项匹配成功: {controlCommand}");
                    return LaunchControlPanelItem(controlCommand, appName, out message);
                }

                // 3. 检查别名
                var matchedApp = FindAppByAlias(appName);
                if (matchedApp != null)
                {
                    Console.WriteLine($"别名匹配成功: {matchedApp}");
                    if (_appPaths.TryGetValue(matchedApp, out string aliasPath))
                    {
                        return LaunchAppWithPath(aliasPath, matchedApp, out message);
                    }
                }

                // 4. 实时搜索
                Console.WriteLine("开始实时搜索...");
                var searchResult = SearchAppInRealTime(appName);
                if (!string.IsNullOrEmpty(searchResult))
                {
                    Console.WriteLine($"实时搜索成功: {searchResult}");
                    _appPaths[appName] = searchResult; // 缓存结果
                    return LaunchAppWithPath(searchResult, appName, out message);
                }

                // 5. 模糊匹配
                var fuzzyMatch = FindAppByFuzzyMatch(appName);
                if (fuzzyMatch != null)
                {
                    Console.WriteLine($"模糊匹配成功: {fuzzyMatch}");
                    if (_appPaths.TryGetValue(fuzzyMatch, out string fuzzyPath))
                    {
                        return LaunchAppWithPath(fuzzyPath, fuzzyMatch, out message);
                    }
                }

                // 6. 尝试作为完整路径启动
                if (File.Exists(appName))
                {
                    Console.WriteLine($"作为完整路径启动: {appName}");
                    return LaunchAppWithPath(appName, appName, out message);
                }

                // 记录所有已知的应用程序路径
                Console.WriteLine("\n当前已知的应用程序路径:");
                foreach (var kvp in _appPaths)
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }

                message = $"找不到应用程序: {appName}";
                return false;
            }
            catch (Exception ex)
            {
                message = $"启动失败: {ex.Message}\n{ex.StackTrace}";
                return false;
            }
        }

        private string ResolveShortcutTarget(string shortcutPath)
        {
            try
            {
                // 读取快捷方式文件的内容
                var shortcutContent = File.ReadAllText(shortcutPath);
                
                // 尝试从内容中提取目标路径
                var possiblePaths = shortcutContent.Split(new[] { '\0', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                // 如果上面的方法找不到，尝试在常见位置查找
                var fileName = Path.GetFileNameWithoutExtension(shortcutPath);
                if (fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = fileName.Substring(0, fileName.Length - 4);
                }

                foreach (var basePath in _commonProgramPaths)
                {
                    if (!Directory.Exists(basePath)) continue;

                    var files = Directory.GetFiles(basePath, $"{fileName}.exe", SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        return files[0];
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析快捷方式出错: {ex.Message}");
                return null;
            }
        }

        private bool LaunchAppWithPath(string path, string appName, out string message)
        {
            try
            {
                // 处理快捷方式
                if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    var targetPath = ResolveShortcutTarget(path);
                    if (!string.IsNullOrEmpty(targetPath))
                    {
                        path = targetPath;
                        Console.WriteLine($"快捷方式目标路径: {path}");
                    }
                }

                if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", path);
                    message = $"已打开文件夹: {appName}";
                    return true;
                }
                
                if (File.Exists(path))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(path)
                    };
                    Process.Start(startInfo);
                    message = $"已启动: {appName}";
                    return true;
                }

                message = $"路径无效: {path}";
                return false;
            }
            catch (Exception ex)
            {
                message = $"启动失败: {ex.Message}";
                return false;
            }
        }

        private bool LaunchControlPanelItem(string command, string itemName, out string message)
        {
            try
            {
                var parts = command.Split(' ');
                var startInfo = new ProcessStartInfo
                {
                    FileName = parts[0],
                    Arguments = string.Join(" ", parts.Skip(1)),
                    UseShellExecute = true
                };
                Process.Start(startInfo);
                message = $"已打开: {itemName}";
                return true;
            }
            catch (Exception ex)
            {
                message = $"启动失败: {ex.Message}";
                return false;
            }
        }

        private string FindAppByAlias(string input)
        {
            foreach (var kvp in _appAliases)
            {
                if (kvp.Value.Contains(input))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        private string FindAppByFuzzyMatch(string input)
        {
            // 简单的模糊匹配：检查应用名称是否包含输入字符串
            var matches = _appPaths.Keys
                .Where(k => k.Contains(input, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 1)
            {
                return matches[0];
            }

            // 如果有多个匹配，返回最短的那个（假设最短的名称最可能是正确的）
            return matches.OrderBy(m => m.Length).FirstOrDefault();
        }

        private string SearchAppInRealTime(string appName)
        {
            try
            {
                Console.WriteLine($"开始实时搜索应用程序: {appName}");

                // 1. 在所有驱动器中搜索
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady) continue;
                    Console.WriteLine($"搜索驱动器: {drive.Name}");

                    // 常见的应用程序安装目录
                    var searchPaths = new[]
                    {
                        Path.Combine(drive.Name, "Program Files"),
                        Path.Combine(drive.Name, "Program Files (x86)"),
                        Path.Combine(drive.Name, "Users", Environment.UserName, "AppData", "Local"),
                        Path.Combine(drive.Name, "Users", Environment.UserName, "AppData", "LocalLow"),
                        Path.Combine(drive.Name, "Users", Environment.UserName, "AppData", "Roaming"),
                        Path.Combine(drive.Name, "Users", Environment.UserName, "AppData", "Local", "Programs"),
                        Path.Combine(drive.Name, "Users", Environment.UserName, "AppData", "Local", "aDrive"),
                        Path.Combine(drive.Name, "Users", Environment.UserName, "AppData", "Local", "bilibili")
                    };

                    foreach (var searchPath in searchPaths)
                    {
                        if (!Directory.Exists(searchPath))
                        {
                            Console.WriteLine($"目录不存在: {searchPath}");
                            continue;
                        }

                        Console.WriteLine($"搜索目录: {searchPath}");

                        // 搜索可执行文件
                        var searchPatterns = new[]
                        {
                            $"{appName}.exe",
                            $"*{appName}*.exe",
                            $"{appName.ToLower()}.exe",
                            $"*{appName.ToLower()}*.exe",
                            // 特殊应用的匹配模式
                            "aDrive.exe",
                            "bilibiliClient.exe",
                            "bilibili.exe",
                            "steam.exe"
                        };

                        foreach (var pattern in searchPatterns)
                        {
                            Console.WriteLine($"使用匹配模式: {pattern}");
                            try
                            {
                                var files = Directory.GetFiles(searchPath, pattern, SearchOption.AllDirectories);
                                if (files.Length > 0)
                                {
                                    var result = files.OrderBy(f => Path.GetFileName(f).Length).First();
                                    Console.WriteLine($"找到文件: {result}");
                                    return result;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"搜索出错: {ex.Message}");
                            }
                        }
                    }
                }

                Console.WriteLine("在文件系统中未找到匹配的应用程序");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"实时搜索出错: {ex.Message}");
                return null;
            }
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