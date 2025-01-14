using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AIAssistant.Services
{
    public class DeepseekService
    {
        private readonly HttpClient _httpClient;
        private readonly AppLauncherService _appLauncher;
        private readonly FileOperationService _fileOperation;
        private const string API_KEY = "sk-18f2aff8055d423d8402df8a29237648";
        private const string API_URL = "https://api.deepseek.com/v1/chat/completions";

        // 系统提示词，用于指导AI理解和处理用户请求
        private const string SYSTEM_PROMPT = @"你是一个智能助手，专门帮助用户处理文件操作和应用程序启动的请求。
请分析用户的自然语言输入，理解他们的意图，并返回结构化的操作指令。

你需要将用户的请求解析为以下几种操作类型之一：
1. CREATE_FILE - 创建文件（支持单个和批量创建）
2. READ_FILE - 读取文件内容
3. DELETE_FILE - 删除单个文件
4. DELETE_FILES - 批量删除文件（支持文件排除）
5. LIST_FILES - 列出指定目录的文件
6. ANALYZE_FILE - 分析文件
7. EXECUTE_BAT - 执行批处理文件
8. LAUNCH_APP - 启动应用程序
9. UNKNOWN - 无法理解的请求

对于每种操作，你需要提取相关的参数信息。
请以JSON格式返回，包含以下字段：
{
    ""operation"": ""操作类型"",
    ""params"": {
        ""type"": ""文件类型(txt/bat/all)"",
        ""name"": ""文件名或文件名数组"",
        ""content"": ""文件内容"",
        ""location"": ""文件位置（默认为桌面）"",
        ""pattern"": ""文件匹配模式(*.txt, *.bat等)"",
        ""exclude"": ""要排除的具体文件名"",
        ""app"": ""应用程序名称""
    },
    ""explanation"": ""对这个操作的解释""
}

示例：
1. 当用户说'新建3个txt文件，分别是a、b、c.txt'时，应该返回：
{
    ""operation"": ""CREATE_FILE"",
    ""params"": {
        ""type"": ""txt"",
        ""name"": [""a"", ""b"", ""c""]
    },
    ""explanation"": ""在桌面创建三个txt文件：a.txt、b.txt、c.txt""
}

2. 当用户说'新建txt文件 a.txt'时，应该返回：
{
    ""operation"": ""CREATE_FILE"",
    ""params"": {
        ""type"": ""txt"",
        ""name"": ""a""
    },
    ""explanation"": ""在桌面创建文本文件a.txt""
}

3. 当用户说'删除a.txt'时，应该返回：
{
    ""operation"": ""DELETE_FILE"",
    ""params"": {
        ""name"": ""a.txt""
    },
    ""explanation"": ""删除桌面上的a.txt文件""
}

4. 当用户说'删除除了curl.txt以外的所有txt文件'时，应该返回：
{
    ""operation"": ""DELETE_FILES"",
    ""params"": {
        ""pattern"": ""*.txt"",
        ""exclude"": ""curl.txt""
    },
    ""explanation"": ""将删除桌面上除了curl.txt以外的所有txt文件""
}

注意：
1. 所有文件操作默认都在桌面进行，除非用户明确指定其他位置
2. 如果用户要创建多个文件，name参数应该是一个字符串数组
3. 如果用户要创建单个文件，name参数应该是一个字符串
4. 如果用户没有指定文件类型，默认为txt
5. 对于批量删除文件，使用DELETE_FILES操作
6. 对于单个文件删除，使用DELETE_FILE操作";

        public DeepseekService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");
            _appLauncher = new AppLauncherService();
            _fileOperation = new FileOperationService();
        }

        public async Task<string> GetResponseAsync(string userInput, List<Dictionary<string, string>> history = null)
        {
            try
            {
                // 首先让Deepseek理解用户意图
                var intent = await UnderstandUserIntent(userInput);
                
                // 根据理解的意图执行相应的操作
                return await ExecuteOperation(intent);
            }
            catch (Exception ex)
            {
                return $"操作失败: {ex.Message}";
            }
        }

        private async Task<JsonElement> UnderstandUserIntent(string userInput)
        {
            var messages = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    { "role", "system" },
                    { "content", SYSTEM_PROMPT }
                },
                new Dictionary<string, string>
                {
                    { "role", "user" },
                    { "content", userInput }
                }
            };

            var requestData = new
            {
                model = "deepseek-chat",
                messages = messages,
                temperature = 0.1, // 使用较低的temperature以获得更确定的回答
                max_tokens = 1000,
                response_format = new { type = "json_object" }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(API_URL, content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            var aiResponse = responseObj.GetProperty("choices")[0].GetProperty("message").GetProperty("content");
            
            return JsonSerializer.Deserialize<JsonElement>(aiResponse.GetString());
        }

        private async Task<string> ExecuteOperation(JsonElement intent)
        {
            var operation = intent.GetProperty("operation").GetString();
            var parameters = intent.GetProperty("params");
            var explanation = intent.GetProperty("explanation").GetString();

            switch (operation)
            {
                case "CREATE_FILE":
                    try
                    {
                        // 检查必要的参数
                        if (!parameters.TryGetProperty("name", out var nameElement))
                        {
                            return "缺少文件名参数\n";
                        }

                        // 处理文件名（可能是单个文件名或文件名数组）
                        List<string> fileNames = new List<string>();
                        if (nameElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in nameElement.EnumerateArray())
                            {
                                fileNames.Add(item.GetString());
                            }
                        }
                        else
                        {
                            fileNames.Add(nameElement.GetString());
                        }

                        // 获取其他参数，默认位置为桌面
                        string location = parameters.TryGetProperty("location", out var loc) ? loc.GetString() : "桌面";
                        string type = parameters.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : "txt";
                        string content = parameters.TryGetProperty("content", out var contentElement) ? contentElement.GetString() : "";

                        var results = new StringBuilder();
                        foreach (var baseName in fileNames)
                        {
                            string fileName = baseName;
                            
                            // 添加扩展名（如果没有）
                            if (!Path.HasExtension(fileName))
                            {
                                fileName += "." + type;
                            }

                            // 处理位置（现在总是在桌面）
                            fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                            // 创建文件
                            if (type.Equals("bat", StringComparison.OrdinalIgnoreCase))
                            {
                                var batPath = await _fileOperation.CreateBatchFileAsync(fileName, content);
                                results.AppendLine($"已创建批处理文件: {batPath}");
                            }
                            else
                            {
                                var txtPath = await _fileOperation.CreateOrUpdateTextFileAsync(fileName, content);
                                results.AppendLine($"已创建文本文件: {txtPath}");
                            }
                        }

                        return $"{results}\n{explanation}";
                    }
                    catch (Exception ex)
                    {
                        return $"创建文件失败: {ex.Message}\n{explanation}";
                    }

                case "READ_FILE":
                    var readContent = await _fileOperation.ReadFileAsync(parameters.GetProperty("name").GetString());
                    return $"文件内容:\n{readContent}\n{explanation}";

                case "DELETE_FILE":
                    string deleteFileName = parameters.GetProperty("name").GetString();
                    string deleteFileLocation = parameters.TryGetProperty("location", out var delFileLoc) ? delFileLoc.GetString() : "桌面";
                    
                    // 现在总是在桌面
                    string fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), deleteFileName);

                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        return $"已删除文件: {fullPath}\n{explanation}";
                    }
                    else
                    {
                        return $"文件不存在: {fullPath}\n{explanation}";
                    }

                case "DELETE_FILES":
                    string deleteLocation = parameters.TryGetProperty("location", out var delLoc) ? delLoc.GetString() : "桌面";
                    string deletePattern = parameters.TryGetProperty("pattern", out var delPat) ? delPat.GetString() : "*.*";
                    string excludeFile = parameters.TryGetProperty("exclude", out var excPat) ? excPat.GetString() : null;
                    
                    // 现在总是在桌面
                    string deleteDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    // 获取所有匹配的文件
                    var filesToDelete = Directory.GetFiles(deleteDir, deletePattern)
                        .Select(f => new FileInfo(f))
                        .ToList();

                    // 如果有需要排除的文件
                    if (!string.IsNullOrEmpty(excludeFile))
                    {
                        string excludePath = Path.Combine(deleteDir, excludeFile);
                        filesToDelete = filesToDelete
                            .Where(f => !string.Equals(f.FullName, excludePath, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    if (!filesToDelete.Any())
                    {
                        return $"未找到要删除的文件\n{explanation}";
                    }

                    var deletedFiles = new StringBuilder($"在目录 {deleteDir} 下\n准备删除以下文件:\n");
                    foreach (var file in filesToDelete)
                    {
                        deletedFiles.AppendLine(file.Name);
                    }
                    deletedFiles.AppendLine("\n实际删除的文件:");

                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            file.Delete();
                            deletedFiles.AppendLine($"成功删除: {file.Name}");
                        }
                        catch (Exception ex)
                        {
                            deletedFiles.AppendLine($"删除失败 {file.Name}: {ex.Message}");
                        }
                    }
                    return $"{deletedFiles}\n{explanation}";

                case "LIST_FILES":
                    string listLocation = parameters.TryGetProperty("location", out var listLoc) ? listLoc.GetString() : "桌面";
                    string pattern = parameters.TryGetProperty("pattern", out var pat) ? pat.GetString() : "*.*";
                    
                    // 现在总是在桌面
                    string directory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    var files = Directory.GetFiles(directory, pattern)
                        .Select(Path.GetFileName)
                        .ToList();

                    if (!files.Any())
                    {
                        return $"未找到匹配的文件\n{explanation}";
                    }

                    var fileList = new StringBuilder("找到以下文件:\n");
                    foreach (var file in files)
                    {
                        fileList.AppendLine(file);
                    }
                    return $"{fileList}\n{explanation}";

                case "ANALYZE_FILE":
                    var analysis = await _fileOperation.AnalyzeFileAsync(parameters.GetProperty("name").GetString());
                    var sb = new StringBuilder("文件分析结果:\n");
                    foreach (var item in analysis)
                    {
                        sb.AppendLine($"{item.Key}: {item.Value}");
                    }
                    return $"{sb}\n{explanation}";

                case "EXECUTE_BAT":
                    var output = await _fileOperation.ExecuteBatchFileAsync(parameters.GetProperty("name").GetString());
                    return $"执行结果:\n{output}\n{explanation}";

                case "LAUNCH_APP":
                    if (_appLauncher.TryLaunchApp(parameters.GetProperty("app").GetString(), out string message))
                    {
                        return $"{message}\n{explanation}";
                    }
                    return $"启动应用程序失败\n{explanation}";

                case "UNKNOWN":
                default:
                    return $"抱歉，我无法理解您的请求。{explanation}";
            }
        }
    }
} 