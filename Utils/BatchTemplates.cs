using System;
using System.Text;

namespace AIAssistant.Utils
{
    public static class BatchTemplates
    {
        public static string GetTemplate(string templateType, params string[] parameters)
        {
            return templateType.ToLower() switch
            {
                "cleanup" => GetCleanupTemplate(),
                "fileops" => GetFileOperationsTemplate(parameters),
                "appops" => GetAppOperationsTemplate(parameters),
                "shutdown" => GetShutdownTemplate(parameters),
                _ => throw new ArgumentException("未知的模板类型")
            };
        }

        private static string GetCleanupTemplate()
        {
            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("echo 开始系统清理...");
            sb.AppendLine("");
            sb.AppendLine(":: 设置变量");
            sb.AppendLine("set TEMP_DIR=%temp%");
            sb.AppendLine("set WIN_TEMP=%windir%\\Temp");
            sb.AppendLine("");
            sb.AppendLine(":: 清理用户临时文件");
            sb.AppendLine("echo 正在清理用户临时文件...");
            sb.AppendLine("del /f /s /q \"%TEMP_DIR%\\*.*\"");
            sb.AppendLine("rd /s /q \"%TEMP_DIR%\"");
            sb.AppendLine("md \"%TEMP_DIR%\"");
            sb.AppendLine("");
            sb.AppendLine(":: 清理系统临时文件");
            sb.AppendLine("echo 正在清理系统临时文件...");
            sb.AppendLine("del /f /s /q \"%WIN_TEMP%\\*.*\"");
            sb.AppendLine("rd /s /q \"%WIN_TEMP%\"");
            sb.AppendLine("md \"%WIN_TEMP%\"");
            sb.AppendLine("");
            sb.AppendLine("echo 清理完成！");
            sb.AppendLine("pause");
            return sb.ToString();
        }

        private static string GetFileOperationsTemplate(string[] parameters)
        {
            if (parameters.Length < 2)
                throw new ArgumentException("文件操作需要指定操作类型和文件路径");

            var operation = parameters[0].ToLower();
            var path = parameters[1];

            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("echo 开始文件操作...");
            sb.AppendLine("");

            switch (operation)
            {
                case "copy":
                    if (parameters.Length < 3)
                        throw new ArgumentException("复制操作需要源路径和目标路径");
                    sb.AppendLine($"echo 正在复制文件从 {path} 到 {parameters[2]}");
                    sb.AppendLine($"xcopy \"{path}\" \"{parameters[2]}\" /E /H /C /I /Y");
                    break;
                case "delete":
                    sb.AppendLine($"echo 正在删除文件 {path}");
                    sb.AppendLine($"del /f /q \"{path}\"");
                    break;
                case "move":
                    if (parameters.Length < 3)
                        throw new ArgumentException("移动操作需要源路径和目标路径");
                    sb.AppendLine($"echo 正在移动文件从 {path} 到 {parameters[2]}");
                    sb.AppendLine($"move \"{path}\" \"{parameters[2]}\"");
                    break;
                default:
                    throw new ArgumentException("未知的文件操作类型");
            }

            sb.AppendLine("");
            sb.AppendLine("if %errorlevel% neq 0 (");
            sb.AppendLine("    echo 操作失败！");
            sb.AppendLine("    pause");
            sb.AppendLine("    exit /b 1");
            sb.AppendLine(")");
            sb.AppendLine("");
            sb.AppendLine("echo 操作完成！");
            sb.AppendLine("pause");
            return sb.ToString();
        }

        private static string GetAppOperationsTemplate(string[] parameters)
        {
            if (parameters.Length < 1)
                throw new ArgumentException("应用操作需要指定应用路径");

            var appPath = parameters[0];

            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("echo 开始应用操作...");
            sb.AppendLine("");
            sb.AppendLine($"echo 正在启动应用 {appPath}");
            sb.AppendLine($"start \"\" \"{appPath}\"");
            sb.AppendLine("");
            sb.AppendLine("if %errorlevel% neq 0 (");
            sb.AppendLine("    echo 启动失败！");
            sb.AppendLine("    pause");
            sb.AppendLine("    exit /b 1");
            sb.AppendLine(")");
            sb.AppendLine("");
            sb.AppendLine("echo 启动完成！");
            sb.AppendLine("pause");
            return sb.ToString();
        }

        private static string GetShutdownTemplate(string[] parameters)
        {
            // 默认延迟时间为60秒
            int delaySeconds = 60;
            if (parameters != null && parameters.Length > 0 && int.TryParse(parameters[0], out int seconds))
            {
                delaySeconds = seconds;
            }

            var sb = new StringBuilder();
            // 使用Windows换行符
            sb.AppendLine("@echo off");
            sb.AppendLine("echo 正在设置系统定时关机...");
            sb.AppendLine("");
            sb.AppendLine($":: 设置 {delaySeconds} 秒后关机");
            sb.AppendLine($"set /a seconds={delaySeconds}");
            sb.AppendLine("shutdown /s /t %seconds%");
            sb.AppendLine("");
            sb.AppendLine("echo 您可以运行 'shutdown /a' 来取消定时关机");
            sb.AppendLine($"echo 系统将在 {delaySeconds} 秒后关机");
            sb.AppendLine("");
            sb.AppendLine("if errorlevel 1 (");
            sb.AppendLine("    echo 设置失败！");
            sb.AppendLine("    pause");
            sb.AppendLine("    exit /b 1");
            sb.AppendLine(")");
            sb.AppendLine("");
            sb.AppendLine("pause");

            // 确保使用Windows换行符
            return sb.ToString().Replace("\n", "\r\n");
        }
    }
} 