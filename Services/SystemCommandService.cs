using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AIAssistant.Services
{
    public class SystemCommandService
    {
        // Windows API 导入
        [DllImport("user32.dll")]
        private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        [DllImport("user32.dll")]
        private static extern void LockWorkStation();

        [DllImport("PowrProf.dll")]
        private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("shell32.dll")]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        // 预定义命令类型
        public enum CommandType
        {
            Shutdown,
            Restart,
            Sleep,
            Hibernate,
            Lock,
            LogOff,
            VolumeUp,
            VolumeDown,
            VolumeMute,
            ShowDesktop,
            MinimizeAll,
            RestoreAll,
            EmptyRecycleBin,
            Custom
        }

        // 命令模板字典
        private readonly Dictionary<CommandType, string> _commandTemplates;

        public SystemCommandService()
        {
            _commandTemplates = new Dictionary<CommandType, string>
            {
                { CommandType.Shutdown, "shutdown /s /t {0}" },  // {0} 是延迟时间（秒）
                { CommandType.Restart, "shutdown /r /t {0}" },
                { CommandType.LogOff, "shutdown /l" },
                { CommandType.VolumeUp, "nircmd.exe changesysvolume 10000" },
                { CommandType.VolumeDown, "nircmd.exe changesysvolume -10000" },
                { CommandType.VolumeMute, "nircmd.exe mutesysvolume 2" },
                { CommandType.ShowDesktop, "explorer shell:::{3080F90D-D7AD-11D9-BD98-0000947B0257}" }
            };
        }

        /// <summary>
        /// 执行预定义命令
        /// </summary>
        public async Task<string> ExecuteCommandAsync(CommandType commandType, params string[] parameters)
        {
            try
            {
                switch (commandType)
                {
                    case CommandType.Shutdown:
                    case CommandType.Restart:
                        int delay = parameters.Length > 0 ? int.Parse(parameters[0]) : 0;
                        return await ExecuteProcessAsync(_commandTemplates[commandType], delay.ToString());

                    case CommandType.Sleep:
                        SetSuspendState(false, false, false);
                        return "系统正在进入睡眠模式...";

                    case CommandType.Hibernate:
                        SetSuspendState(true, false, false);
                        return "系统正在进入休眠模式...";

                    case CommandType.Lock:
                        LockWorkStation();
                        return "系统已锁定";

                    case CommandType.LogOff:
                        return await ExecuteProcessAsync(_commandTemplates[commandType]);

                    case CommandType.VolumeUp:
                    case CommandType.VolumeDown:
                    case CommandType.VolumeMute:
                        return await ExecuteProcessAsync(_commandTemplates[commandType]);

                    case CommandType.ShowDesktop:
                        return await ExecuteProcessAsync(_commandTemplates[commandType]);

                    case CommandType.MinimizeAll:
                        var shell = FindWindow("Shell_TrayWnd", null);
                        if (shell != IntPtr.Zero)
                        {
                            ShowWindow(shell, 6);  // SW_MINIMIZE
                            return "已最小化所有窗口";
                        }
                        return "最小化失败";

                    case CommandType.RestoreAll:
                        shell = FindWindow("Shell_TrayWnd", null);
                        if (shell != IntPtr.Zero)
                        {
                            ShowWindow(shell, 9);  // SW_RESTORE
                            return "已还原所有窗口";
                        }
                        return "还原失败";

                    case CommandType.EmptyRecycleBin:
                        SHEmptyRecycleBin(IntPtr.Zero, null, 0);
                        return "已清空回收站";

                    default:
                        throw new ArgumentException("未知的命令类型");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"执行命令失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行自定义命令
        /// </summary>
        public async Task<string> ExecuteCustomCommandAsync(string command)
        {
            try
            {
                return await ExecuteProcessAsync(command);
            }
            catch (Exception ex)
            {
                throw new Exception($"执行自定义命令失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行进程并返回输出
        /// </summary>
        private async Task<string> ExecuteProcessAsync(string command, params string[] args)
        {
            try
            {
                var formattedCommand = string.Format(command, args);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {formattedCommand}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception(error);
                }

                return string.IsNullOrEmpty(output) ? "命令执行成功" : output;
            }
            catch (Exception ex)
            {
                throw new Exception($"执行命令失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取系统信息
        /// </summary>
        public async Task<string> GetSystemInfoAsync()
        {
            return await ExecuteProcessAsync("systeminfo");
        }

        /// <summary>
        /// 获取进程列表
        /// </summary>
        public async Task<string> GetProcessListAsync()
        {
            return await ExecuteProcessAsync("tasklist");
        }

        /// <summary>
        /// 结束进程
        /// </summary>
        public async Task<string> KillProcessAsync(string processName)
        {
            return await ExecuteProcessAsync($"taskkill /F /IM {processName}");
        }

        /// <summary>
        /// 清理系统
        /// </summary>
        public async Task<string> CleanSystemAsync()
        {
            var commands = new[]
            {
                "del /f /s /q %temp%\\*.*",
                "rd /s /q %temp%",
                "md %temp%",
                "del /f /s /q C:\\Windows\\temp\\*.*"
            };

            var result = "";
            foreach (var cmd in commands)
            {
                result += await ExecuteProcessAsync(cmd) + "\n";
            }
            return result;
        }

        /// <summary>
        /// 刷新 DNS 缓存
        /// </summary>
        public async Task<string> FlushDnsAsync()
        {
            return await ExecuteProcessAsync("ipconfig /flushdns");
        }
    }
} 