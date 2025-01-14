using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AIAssistant.Services
{
    public class FileOperationService
    {
        private readonly string _defaultBatPath;
        private readonly string _defaultScriptPath;

        public FileOperationService()
        {
            // 设置默认路径
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _defaultBatPath = Path.Combine(documentsPath, "AIAssistant", "BatchFiles");
            _defaultScriptPath = Path.Combine(documentsPath, "AIAssistant", "Scripts");

            // 确保目录存在
            Directory.CreateDirectory(_defaultBatPath);
            Directory.CreateDirectory(_defaultScriptPath);
        }

        /// <summary>
        /// 创建批处理文件
        /// </summary>
        public async Task<string> CreateBatchFileAsync(string fileName, string content)
        {
            try
            {
                if (!fileName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".bat";
                }

                var fullPath = Path.Combine(_defaultBatPath, fileName);
                await File.WriteAllTextAsync(fullPath, content, Encoding.Default);
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
                directory ??= _defaultScriptPath;
                Directory.CreateDirectory(directory);

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
        public async Task<string> ReadFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("文件不存在", filePath);
                }

                return await File.ReadAllTextAsync(filePath);
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
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                else
                {
                    throw new FileNotFoundException("文件不存在", filePath);
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
                directory ??= _defaultScriptPath;
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
            return GetFiles(_defaultBatPath, "*.bat");
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

                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
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
        public async Task<Dictionary<string, object>> AnalyzeFileAsync(string filePath)
        {
            try
            {
                var content = await ReadFileAsync(filePath);
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