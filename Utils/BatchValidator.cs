using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace AIAssistant.Utils
{
    public static class BatchValidator
    {
        private static readonly HashSet<string> DangerousCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "format", "fdisk", "diskpart",
            "reg delete", "reg add",
            "rd /s /q c:", "del /f /s /q c:",
            "rmdir /s /q c:", "rd /s /q %systemroot%",
            "del /f /s /q %systemroot%"
        };

        public static (bool isValid, string message) ValidateBatchContent(string content)
        {
            try
            {
                // 检查是否为空
                if (string.IsNullOrWhiteSpace(content))
                    return (false, "批处理文件内容不能为空");

                // 检查危险命令
                foreach (var command in DangerousCommands)
                {
                    if (content.Contains(command, StringComparison.OrdinalIgnoreCase))
                        return (false, $"包含危险命令: {command}");
                }

                // 检查系统目录操作
                var systemPaths = new[] { "%systemroot%", "%windir%", "c:\\windows", "c:/windows" };
                foreach (var path in systemPaths)
                {
                    if (Regex.IsMatch(content, $@"(del|rd|rmdir).*{Regex.Escape(path)}", RegexOptions.IgnoreCase))
                        return (false, $"不允许直接操作系统目录: {path}");
                }

                return (true, "验证通过");
            }
            catch (Exception ex)
            {
                return (false, $"验证过程出错: {ex.Message}");
            }
        }

        public static string SanitizeBatchContent(string content)
        {
            // 统一换行符为Windows格式
            content = content.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
            
            var lines = content.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            var sb = new StringBuilder();

            // 添加文件头
            if (!lines.Any(line => line.Equals("@echo off", StringComparison.OrdinalIgnoreCase)))
            {
                sb.AppendLine("@echo off");
            }

            // 添加内容
            foreach (var line in lines)
            {
                if (!line.Equals("@echo off", StringComparison.OrdinalIgnoreCase))  // 避免重复添加
                {
                    sb.AppendLine(line);
                }
            }

            // 确保有暂停命令
            if (!lines.Any(line => line.Equals("pause", StringComparison.OrdinalIgnoreCase)))
            {
                sb.AppendLine("pause");
            }

            // 确保使用Windows换行符
            return sb.ToString().Replace("\n", "\r\n");
        }
    }
} 