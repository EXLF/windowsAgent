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
3. DELETE_FILES - 删除文件（支持单个、批量删除和文件排除）
4. LIST_FILES - 列出指定目录的文件
5. ANALYZE_FILE - 分析文件
6. EXECUTE_BAT - 执行批处理文件
7. LAUNCH_APP - 启动应用程序
8. UNKNOWN - 无法理解的请求

关于应用程序启动的说明：
- 当用户说'打开xxx'、'启动xxx'、'运行xxx'时，应该理解为启动应用程序
- 支持以下几种应用程序名称格式：
  1. 中文名称：如'记事本'、'微信'、'钉钉'
  2. 英文名称：如'notepad'、'wechat'、'chrome'
  3. 常见别名：如'浏览器'代表'chrome'、'文本编辑器'代表'记事本'
- 返回格式示例：
{
    ""operation"": ""LAUNCH_APP"",
    ""params"": {
        ""app"": ""微信""
    },
    ""explanation"": ""启动微信应用程序""
}

关于删除文件的说明：
- 当用户说'删除xxx'时，应该理解为删除所有匹配的文件
- 如果用户说'删除bat'或'删除桌面bat'，应该返回：
{
    ""operation"": ""DELETE_FILES"",
    ""params"": {
        ""pattern"": ""*.bat""
    },
    ""explanation"": ""删除所有批处理文件""
}
- 如果用户说'删除test.bat'这样的具体文件名，也应该返回：
{
    ""operation"": ""DELETE_FILES"",
    ""params"": {
        ""pattern"": ""test.bat""
    },
    ""explanation"": ""删除指定的批处理文件""
}

对于批处理文件的创建，你有两种方式：

1. 使用预定义模板（适用于常见操作）：
   - cleanup: 系统清理模板
   - fileops: 文件操作模板
   - appops: 应用程序操作模板
   - shutdown: 系统关机模板

2. 直接生成批处理内容（适用于自定义操作）：
   在这种情况下，你需要生成符合以下规范的批处理代码：
   - 必须以 @echo off 开头
   - 必须包含操作说明的 echo 命令
   - 必须包含错误处理
   - 必须以 pause 结尾
   - 所有路径必须用引号包裹
   - 危险操作前必须有注释说明
   - 使用环境变量代替硬编码路径

示例响应格式：

1. 使用模板：
{
    ""operation"": ""CREATE_FILE"",
    ""params"": {
        ""type"": ""bat"",
        ""name"": ""cleanup"",
        ""template"": ""cleanup""
    },
    ""explanation"": ""创建系统清理批处理文件""
}

2. 自定义内容：
{
    ""operation"": ""CREATE_FILE"",
    ""params"": {
        ""type"": ""bat"",
        ""name"": ""shutdown_timer"",
        ""content"": ""@echo off\r\necho 正在设置定时关机...\r\n\r\n:: 设置30分钟后关机\r\nset /a seconds=30*60\r\nshutdown /s /t %seconds%\r\n\r\necho 您可以运行 'shutdown /a' 来取消定时关机\r\necho 系统将在30分钟后关机\r\n\r\nif errorlevel 1 (\r\n    echo 设置失败！\r\n    pause\r\n    exit /b 1\r\n)\r\n\r\npause""
    },
    ""explanation"": ""创建定时关机批处理文件""
}

注意：
1. 所有文件操作默认都在桌面进行
2. 批处理文件必须经过安全验证
3. 优先使用预定义模板，但如果用户需求特殊，则生成自定义内容
4. 确保所有路径都使用环境变量
5. 查询文件列表时，必须指定正确的文件匹配模式";

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
                temperature = 0.1,
                max_tokens = 1000,
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await _httpClient.PostAsync(API_URL, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API请求失败: {response.StatusCode}, {errorContent}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                var aiResponse = responseObj.GetProperty("choices")[0].GetProperty("message").GetProperty("content");
                
                try
                {
                    return JsonSerializer.Deserialize<JsonElement>(aiResponse.GetString());
                }
                catch
                {
                    var defaultResponse = new
                    {
                        operation = "UNKNOWN",
                        @params = new { },
                        explanation = aiResponse.GetString()
                    };
                    return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(defaultResponse));
                }
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    operation = "UNKNOWN",
                    @params = new { },
                    explanation = $"API调用失败: {ex.Message}"
                };
                return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(errorResponse));
            }
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

                        // 获取其他参数
                        string type = parameters.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : "txt";
                        string content = "";
                        string templateType = parameters.TryGetProperty("template", out var templateElement) ? templateElement.GetString() : null;
                        string[] templateParams = null;

                        // 如果有模板参数
                        if (parameters.TryGetProperty("template_params", out var templateParamsElement))
                        {
                            templateParams = templateParamsElement.EnumerateArray()
                                .Select(x => x.GetString())
                                .ToArray();
                        }

                        // 如果有直接内容
                        if (parameters.TryGetProperty("content", out var contentElement))
                        {
                            content = contentElement.GetString();
                        }

                        // 处理文件名
                        string fileName = nameElement.GetString();
                        if (!Path.HasExtension(fileName))
                        {
                            fileName += "." + type;
                        }

                        // 创建文件
                        if (type.Equals("bat", StringComparison.OrdinalIgnoreCase))
                        {
                            var batPath = _fileOperation.CreateBatchFile(
                                fileName,
                                content,
                                templateType,
                                templateParams
                            );
                            return $"已创建批处理文件: {batPath}\n{explanation}";
                        }
                        else
                        {
                            var txtPath = await _fileOperation.CreateOrUpdateTextFileAsync(fileName, content);
                            return $"已创建文本文件: {txtPath}\n{explanation}";
                        }
                    }
                    catch (Exception ex)
                    {
                        return $"创建文件失败: {ex.Message}\n{explanation}";
                    }

                case "READ_FILE":
                    var readContent = _fileOperation.ReadFile(parameters.GetProperty("name").GetString());
                    return $"文件内容:\n{readContent}\n{explanation}";

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

                    // 如果用户没有指定具体模式，但提到了特定类型
                    if (pattern == "*.*" && explanation.ToLower().Contains("bat"))
                    {
                        pattern = "*.bat";
                    }
                    else if (pattern == "*.*" && explanation.ToLower().Contains("txt"))
                    {
                        pattern = "*.txt";
                    }

                    var files = Directory.GetFiles(directory, pattern)
                        .Select(Path.GetFileName)
                        .ToList();

                    if (!files.Any())
                    {
                        return $"未找到匹配的文件（搜索模式：{pattern}）\n{explanation}";
                    }

                    var fileList = new StringBuilder($"找到以下文件（搜索模式：{pattern}）:\n");
                    foreach (var file in files)
                    {
                        fileList.AppendLine(file);
                    }
                    return $"{fileList}\n{explanation}";

                case "ANALYZE_FILE":
                    var analysis = _fileOperation.AnalyzeFile(parameters.GetProperty("name").GetString());
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
                    var appName = parameters.GetProperty("app").GetString().Trim();
                    // 标准化应用程序名称
                    appName = NormalizeAppName(appName);
                    if (_appLauncher.TryLaunchApp(appName, out string message))
                    {
                        return $"{message}\n{explanation}";
                    }
                    return $"启动应用程序失败: {message}\n{explanation}";

                case "UNKNOWN":
                default:
                    return $"抱歉，我无法理解您的请求。{explanation}";
            }
        }

        private string NormalizeAppName(string appName)
        {
            // 移除常见的动词前缀
            string[] prefixes = { "打开", "启动", "运行" };
            foreach (var prefix in prefixes)
            {
                if (appName.StartsWith(prefix))
                {
                    appName = appName.Substring(prefix.Length).Trim();
                    break;
                }
            }
            
            // 处理特殊的应用程序名称映射
            var appNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "浏览器", "chrome" },
                { "谷歌", "chrome" },
                { "文本编辑器", "记事本" },
                { "资源管理器", "explorer" },
                { "命令行", "cmd" }
            };

            if (appNameMap.TryGetValue(appName, out string mappedName))
            {
                appName = mappedName;
            }

            return appName;
        }
    }
} 