# RimLogging：RimWorld 日志查看与错误报告分享

在游戏里直接看 RimWorld 的日志。把它筛到你真正关心的那条红色报错，然后一键分享成链接。RimLogging 同时也是给模组作者用的结构化日志框架，它把 `Verse.Log` 和 `UnityEngine.Debug.Log` 换成所有模组共用的一条管线。

## 怎么分享你的 RimWorld 日志

打开日志查看器，按分享按钮，RimLogging 会上传一个打包文件并把链接复制到剪贴板。粘到 Discord 或 GitHub issue 里就行。打包内容带上你的模组列表、游戏版本和抓到的日志条目。RimLogging 会裁剪它以适配上传上限，所以长时间的会话也能发出去。

## 原版的日志窗口有什么问题

它是一个扁平列表，会被截断，而且没法隐藏噪音。你正在追的那个模组的红色报错，夹在四千行你根本不需要的内容中间。

RimLogging 把它换成三栏查看器：左侧是频道树，中间是实时的级别开关，还有一套表达式过滤 DSL。详情面板可以停靠在下方、停靠在右侧，或者弹出成独立窗口。

因为它接管了 `Verse.Log`，连那些从没主动接入的模组的输出也能抓到。一条都漏不掉。

## 和 HugsLib 的 publish log 有什么区别

HugsLib 把整个日志文件当成一大块文本传上去，给你一个链接。这能用，然后你就开始往下翻。

RimLogging 让你先缩小范围。挑一个模组的频道，丢掉 `Warn` 以下的全部内容，写一条过滤表达式，看剩下什么。然后把那些发出去。读你报告的人拿到的是日志加模组列表，不用在文件里翻找那唯一一行重点。

## 给玩家

把它作为共享依赖安装。如果你订阅的某个模组需要 RimLogging，装的就是这个。它负责抓取并整理日志输出，让错误报告干净、好分享。

## 给模组作者

- 层级频道（XML def 或临时创建），按前缀解析。
- Serilog 风格的模板消息，支持具名占位符和位置参数。
- 可以给任意条目附加匿名对象形式的结构化上下文。
- 六个级别：`Trace`、`Debug`、`Info`、`Warn`、`Error`、`Fatal`。
- 多目标输出：回写 Verse 日志、滚动文本文件、滚动 NDJSON 文件、内存，另有插件式输出目标 API。
- 无锁 MPSC 队列加后台消费。`Error` 和 `Fatal` 走同步直通。
- 一个 Roslyn 分析器，在编译期标出残留的 `Verse.Log` 调用，并提供快速修复。

从 NuGet 添加到你的项目：

```
dotnet add package RimWorks.RimLogging
```

然后在 `About.xml` 里把这个 Workshop 项目声明为依赖，订阅者就会自动拿到共享的运行时 DLL。

## 链接

- 源码、文档与问题反馈：https://github.com/RimWorks/rimworld-logging-framework

## RimWorks 的其他模组开发工具

- [Pickle](https://steamcommunity.com/sharedfiles/filedetails/?id=3791648678)：对运行中的 RimWorld 会话跑 Gherkin 测试。
- [Quickstarts](https://steamcommunity.com/sharedfiles/filedetails/?id=3793646067)：从 quicktest 菜单直接进入一个配置好的殖民地。

采用 MIT 许可证。
