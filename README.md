# MD5 文件完整性校验工具

基于 .NET 10 NativeAOT 编译的高性能文件完整性校验工具，使用 MD5 哈希算法，配合 [Spectre.Console](https://spectreconsole.net/) 提供精美的终端界面。

## 功能特性

- **基线建立** — 首次运行时递归扫描当前目录下所有文件，生成 `md5.txt` 清单文件。
- **完整性校验** — 后续运行时会逐一对比文件与清单中的哈希值。
- **变更检测** — 清晰报告 **已变更**、**已删除**、**新增** 的文件，同时显示预期和实际哈希值。
- **原生 AOT** — 使用 .NET 10 NativeAOT 编译为原生代码，启动迅速，无需运行时依赖。
- **丰富的终端界面** — 通过 Spectre.Console 呈现 Figlet 标题、加载动画、彩色表格和面板。

## 环境要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## 构建与发布

```bash
# 调试构建
dotnet build

# 发布为单文件原生可执行程序
dotnet publish -c Release -o publish
```

编译后的原生可执行文件位于 `publish\MD5.exe`。

## 使用方法

```bash
# 首次在目录中运行，建立基线
MD5.exe

# 再次运行，校验文件完整性
MD5.exe

# 管道友好模式（无交互提示）
echo. | MD5.exe
```

### 退出码

| 代码 | 含义                |
|------|---------------------|
| 0    | 通过 — 所有文件匹配 |
| 1    | 失败 — 发现差异      |
| 2    | 错误                |

## 清单文件格式 (`md5.txt`)

```
<哈希值>  <相对路径>
<哈希值>  <相对路径>
...
```

示例：

```
d41d8cd98f00b204e9800998ecf8427e  data/config.json
5d41402abc4b2a76b9719d911017c592  src/main.cs
```

## 开源许可

MIT
