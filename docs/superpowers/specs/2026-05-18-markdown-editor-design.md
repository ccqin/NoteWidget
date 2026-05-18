# NoteWidget Markdown 编辑器设计文档

**日期**: 2026-05-18
**状态**: 待审核

## 概述

为 NoteWidget（OneNote Markdown 增强插件）添加 Markdown 编辑功能。当前插件仅支持查看 Markdown 内容，本设计将添加一个完整的编辑器，支持 Monaco Editor（VS Code 引擎）、实时预览、工具栏操作和 OneNote 页面写回。

## 需求总结

- **编辑模式**: 独立编辑器窗口，支持查看/编辑/分栏三种模式切换
- **编辑体验**: 语法高亮、行号、括号匹配、自动缩进、多光标（Monaco Editor）
- **工具栏**: 加粗、斜体、标题、链接、代码块、列表、引用、表格等快捷操作
- **保存机制**: Ctrl+S 手动保存，写回当前 OneNote 页面
- **Ribbon 集成**: 独立 "Markdown" 页签，位于 OneNote "视图"页签之后

## 技术选型

**Monaco Editor** — VS Code 的编辑器引擎，嵌入 WebView2 控件中。

选择理由：
- 完整的 VS Code 编辑体验
- 原生 Markdown 语法高亮和智能提示
- 项目已深度使用 WebView2，集成自然
- 微软官方维护，成熟稳定

## 架构设计

### 窗口架构

新增 `MarkdownEditorWindow`（WPF Window），包含：

```
┌──────────────────────────────────────────────────────────┐
│ [工具栏: B I H1 H2 | 链接 代码 列表 引用 表格 | 查看|分栏|编辑]│
├─────────────────────────┬────────────────────────────────┤
│                         │                                │
│   Monaco Editor         │   WebView2 预览                │
│   (WebView2 控件)       │   (现有渲染逻辑)               │
│                         │                                │
├─────────────────────────┴────────────────────────────────┤
│ 状态栏: 已保存 / 未保存                                   │
└──────────────────────────────────────────────────────────┘
```

三种视图模式：
1. **查看模式** — 仅显示预览面板
2. **分栏模式** — 左编辑器 + 右预览，可拖拽分隔条调整比例
3. **编辑模式** — 仅显示编辑器

WPF 布局使用 `Grid` + `GridSplitter`，通过 `Visibility.Collapsed` 切换面板。

### 数据流

```
OneNote 页面 (XML)
  ↓ 读取
提取纯文本 (NotePage.ContentInnerText)
  ↓ 加载
Monaco Editor (Markdown 源码)
  ↕ 编辑
用户编辑 → 防抖 500ms → Markdig → HTML → 预览 WebView2
  ↓ Ctrl+S
从 Monaco 获取文本 → 重建 OneNote XML → UpdatePage 写回
```

### Monaco Editor 集成

**加载方式**：本地打包 `Resources/js/monaco/`，通过 WebView2 虚拟主机映射加载。

**Monaco 配置**：
- 语言: `markdown`
- 启用: 行号、括号匹配、自动缩进、minimap、自动换行
- 主题: `vs`（亮色）/ `vs-dark`（暗色），跟随系统主题

**C# ↔ Monaco 通信**：
- C# → Monaco: `ExecuteScriptAsync()` 调用 JS 函数
- Monaco → C#: `PostWebMessage()` 发送消息

**实时预览同步**：
- Monaco `onDidChangeModelContent` 事件，防抖 500ms
- 使用现有 `MarkdownHelper.MarkdownToHtml` + `HtmlTemplate` 渲染

### 工具栏

| 按钮 | 功能 | Markdown 语法 | 快捷键 |
|------|------|---------------|--------|
| B | 粗体 | `**text**` | Ctrl+B |
| I | 斜体 | `*text*` | Ctrl+I |
| H1/H2/H3 | 标题 | `# / ## / ###` | Ctrl+1/2/3 |
| 删除线 | 删除线 | `~~text~~` | Ctrl+D |
| 链接 | 链接 | `[text](url)` | Ctrl+K |
| 代码块 | 代码块 | ` ``` ` | Ctrl+Shift+K |
| 行内代码 | 行内代码 | `` `code` `` | Ctrl+` |
| 有序列表 | 有序列表 | `1. item` | — |
| 无序列表 | 无序列表 | `- item` | — |
| 引用 | 引用 | `> text` | — |
| 分割线 | 分割线 | `---` | — |
| 表格 | 表格 | Markdown 表格模板 | — |

实现方式: WPF `ToolBar` + `Button`。点击时通过 Monaco `executeEdits` API 在光标位置插入文本。

### OneNote 写回机制

**策略: 简单替换法**

1. 保存时从 Monaco 获取完整 Markdown 文本
2. 获取当前 NotePage 的 XML 结构
3. 清空页面 `<one:OEChildren>` 下的所有内容
4. 按 Markdown 行拆分，每行创建 `<one:OE><one:T><![CDATA[行内容]]></one:T></one:OE>`
5. 调用 `NoteApplication.UpdatePage(page)` 写回

**冲突处理**: 保存前检查 `lastModifiedTime`，如果页面在编辑期间被外部修改，提示用户确认覆盖。

**注意**: 此方法会丢失 OneNote 原生富文本格式（字体、颜色等）。对于 Markdown 用户可接受。

### Ribbon 集成

从 Home 页签迁移到独立的 Markdown 页签：

```xml
<tab id="tabNoteWidgetMarkdown" label="Markdown" insertAfterQ="TabView">
  <group id="groupNoteWidgetMarkdown" label="工具">
    <button label="预览" onAction="PreviewMarkdownCmd" />
    <button label="编辑" onAction="MarkdownEditorCmd" />
    <button label="速查表" onAction="MarkdownCheatsheetCmd" />
    <button label="设置" onAction="WidgetAdvancedSettingsCmd" />
  </group>
</tab>
```

## 新增文件

| 文件 | 说明 |
|------|------|
| `RibbonCommand/Markdown/MarkdownEditorWindow.xaml(.cs)` | 编辑器主窗口 |
| `RibbonCommand/MarkdownEditorCommand.cs` | 编辑器 Ribbon 命令 |
| `Resources/js/monaco/` | Monaco Editor 本地文件（minified） |
| `Resources/js/editor-host.html` | Monaco 宿主 HTML 页面 |

## 修改文件

| 文件 | 修改内容 |
|------|---------|
| `Properties/ribbon.xml` | 从 Home 迁移到独立 Markdown 页签，添加编辑按钮 |
| `AddIn.Ribbon.cs` | 添加 `MarkdownEditorCmd` 命令方法 |
| `Model/NotePage.cs` | 添加 `UpdateContent(string markdown)` 方法 |

## 不修改

- `WebBrowserWindow` — 保持原有预览功能不变
- `PreviewMarkdownCommand` — 保持不变
- `NoteApplication.cs` — 已有 `UpdatePage()` 可用

## 限制与风险

1. **Monaco 体积**: minified 版约 2-3MB，初始化需 1-2 秒
2. **双 WebView2 实例**: 编辑器+预览各一个 WebView2，内存占用约 100-150MB
3. **写回格式丢失**: 替换法会丢失 OneNote 原生富文本格式
4. **.NET Framework 限制**: 项目使用 .NET Framework 4.7.2，部分新库不兼容
