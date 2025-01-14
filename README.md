# Windows Agent

一个基于 .NET Core 的 Windows 桌面助手应用程序，集成了 Deepseek API，可以通过自然语言来执行文件操作和应用程序启动等任务。

## 功能特性

1. 文件操作
   - 创建文件（支持单个和批量创建）
   - 读取文件内容
   - 删除文件（支持单个和批量删除）
   - 分析文件
   - 执行批处理文件

2. 应用程序启动
   - 支持启动系统应用和已安装的应用程序

3. 自然语言交互
   - 集成 Deepseek API
   - 支持自然语言命令
   - 智能理解用户意图

## 使用示例

1. 创建文件：
   - "新建txt文件 test.txt"
   - "新建3个txt文件，分别是a、b、c.txt"
   - "新建一个bat文件，内容是echo hello world"

2. 删除文件：
   - "删除test.txt"
   - "删除所有txt文件"
   - "删除除了curl.txt以外的所有txt文件"

3. 启动应用：
   - "打开记事本"
   - "启动计算器"

## 开发环境

- .NET Core 7.0
- Visual Studio Code 或 Visual Studio 2022
- Windows 10/11

## 配置说明

1. Deepseek API 配置：
   - API地址：https://api.deepseek.com
   - 需要设置有效的 API Key

2. 默认设置：
   - 所有文件操作默认在桌面进行
   - 文本文件默认使用 .txt 扩展名
   - 批处理文件默认使用 .bat 扩展名

## 注意事项

1. 文件操作默认在桌面进行，除非明确指定其他位置
2. 删除文件操作会先显示要删除的文件列表，再执行删除
3. 批量操作时要小心，建议先使用查看命令确认 