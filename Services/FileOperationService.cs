using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using AIAssistant.Utils;

namespace AIAssistant.Services
{
    public class FileOperationService
    {
        private readonly string _defaultPath;

        public FileOperationService()
        {
            // 设置默认路径为桌面
            _defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            // 确保系统支持 GBK 编码
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// 创建批处理文件
        /// </summary>
        public string CreateBatchFile(string fileName, string content, string templateType = null, string[] templateParams = null)
        {
            try
            {
                // 如果指定了模板类型，使用模板生成内容
                if (!string.IsNullOrEmpty(templateType))
                {
                    content = BatchTemplates.GetTemplate(templateType, templateParams ?? Array.Empty<string>());
                }

                // 验证批处理文件内容
                var (isValid, message) = BatchValidator.ValidateBatchContent(content);
                if (!isValid)
                {
                    throw new Exception($"批处理文件验证失败: {message}");
                }

                // 净化内容
                content = BatchValidator.SanitizeBatchContent(content);

                if (!fileName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".bat";
                }

                var fullPath = Path.Combine(_defaultPath, fileName);
                
                // 使用 GBK 编码写入文件
                var encoding = Encoding.GetEncoding("GBK");
                File.WriteAllText(fullPath, content, encoding);
                return fullPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"创建批处理文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建或更新文本文件
        /// </summary>
        public async Task<string> CreateOrUpdateTextFileAsync(string fileName, string content, string directory = null)
        {
            try
            {
                directory ??= _defaultPath;

                if (!fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".txt";
                }

                var fullPath = Path.Combine(directory, fileName);
                await File.WriteAllTextAsync(fullPath, content, Encoding.UTF8);
                return fullPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"创建文本文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取文件内容
        /// </summary>
        public string ReadFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("文件不存在", filePath);
                }

                // 根据文件扩展名选择编码
                var encoding = Path.GetExtension(filePath).ToLower() == ".bat" 
                    ? Encoding.GetEncoding("GBK") 
                    : Encoding.UTF8;

                return File.ReadAllText(filePath, encoding);
            }
            catch (Exception ex)
            {
                throw new Exception($"读取文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        public void DeleteFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("文件路径不能为空");
                }

                // 如果是相对路径，则相对于桌面
                if (!Path.IsPathRooted(filePath))
                {
                    filePath = Path.Combine(_defaultPath, filePath);
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                else
                {
                    throw new FileNotFoundException($"文件不存在: {filePath}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"删除文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取目录下的所有文件
        /// </summary>
        public List<string> GetFiles(string directory = null, string searchPattern = "*.*")
        {
            try
            {
                directory ??= _defaultPath;
                return new List<string>(Directory.GetFiles(directory, searchPattern));
            }
            catch (Exception ex)
            {
                throw new Exception($"获取文件列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取批处理文件列表
        /// </summary>
        public List<string> GetBatchFiles()
        {
            return GetFiles(_defaultPath, "*.bat");
        }

        /// <summary>
        /// 执行批处理文件
        /// </summary>
        public async Task<string> ExecuteBatchFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("批处理文件不存在", filePath);
                }

                // 验证文件内容
                var content = File.ReadAllText(filePath);
                var (isValid, message) = BatchValidator.ValidateBatchContent(content);
                if (!isValid)
                {
                    throw new Exception($"批处理文件验证失败: {message}");
                }

                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        Verb = "runas" // 以管理员权限运行
                    }
                };

                var output = new StringBuilder();
                process.OutputDataReceived += (sender, e) => 
                {
                    if (e.Data != null)
                        output.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        output.AppendLine($"错误: {e.Data}");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                return output.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"执行批处理文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分析文本文件内容
        /// </summary>
        public Dictionary<string, object> AnalyzeFile(string filePath)
        {
            try
            {
                var content = ReadFile(filePath);
                var result = new Dictionary<string, object>
                {
                    { "文件名", Path.GetFileName(filePath) },
                    { "文件大小", new FileInfo(filePath).Length },
                    { "行数", content.Split('\n').Length },
                    { "字符数", content.Length },
                    { "创建时间", File.GetCreationTime(filePath) },
                    { "修改时间", File.GetLastWriteTime(filePath) }
                };

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"分析文件失败: {ex.Message}");
            }
        }
    }
} 