# Markdown 编辑器实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 NoteWidget 添加基于 Monaco Editor 的 Markdown 编辑功能，支持分栏预览、工具栏操作和 OneNote 页面写回。

**Architecture:** 新建 `MarkdownEditorWindow`（WPF 窗口），内含两个 WebView2 控件（Monaco 编辑器 + HTML 预览），通过 WebView2 消息通信实现编辑器和预览同步。工具栏通过 WPF `ToolBar` 实现，调用 Monaco `executeEdits` API 插入文本。保存时将 Markdown 写回 OneNote 页面 XML。

**Tech Stack:** .NET Framework 4.7.2, C# 7.3, WPF, WebView2, Monaco Editor 0.45+, Markdig, MSTest

---

## File Structure

### 新建文件

| 文件 | 职责 |
|------|------|
| `NoteWidgetAddIn/Resources/js/monaco/` | Monaco Editor 本地文件（从 npm 包复制） |
| `NoteWidgetAddIn/Resources/js/editor-host.html` | Monaco 编辑器宿主 HTML 页面，管理编辑器生命周期和 C# 通信 |
| `NoteWidgetAddIn/RibbonCommand/Markdown/MarkdownEditorWindow.xaml` | 编辑器窗口 XAML 布局：工具栏 + 双 WebView2 + 状态栏 |
| `NoteWidgetAddIn/RibbonCommand/Markdown/MarkdownEditorWindow.xaml.cs` | 编辑器窗口代码：初始化 WebView2、Monaco 通信、保存逻辑、模式切换 |
| `NoteWidgetAddIn/RibbonCommand/MarkdownEditorCommand.cs` | 编辑器 Ribbon 命令，打开编辑器窗口并加载当前 OneNote 页面 |
| `NoteWidgetTests/NotePageUpdateTest.cs` | NotePage.UpdateContent 方法的单元测试 |

### 修改文件

| 文件 | 修改内容 |
|------|---------|
| `NoteWidgetAddIn/Model/NotePage.cs` | 添加 `UpdateContent(string markdown)` 方法 |
| `NoteWidgetAddIn/Properties/ribbon.xml` | 从 Home 页签迁移到独立 Markdown 页签，添加编辑按钮 |
| `NoteWidgetAddIn/AddIn.Ribbon.cs` | 添加 `MarkdownEditorCmd` 命令处理方法 |
| `NoteWidgetAddIn/NoteWidgetAddIn.csproj` | 添加 Monaco 资源文件和新 XAML 页面引用 |
| `NoteWidgetTests/DummyImpl/DummyApplication.cs` | 实现 `UpdatePageContent` 方法用于测试 |

---

## Task 1: 下载并准备 Monaco Editor 文件

**Files:**
- Create: `NoteWidgetAddIn/Resources/js/monaco/vs/` (整个目录)

- [ ] **Step 1: 使用 npm 下载 Monaco Editor**

Run:
```bash
cd d:/13.Net/NoteWidget
mkdir -p temp_monaco
cd temp_monaco
npm init -y
npm install monaco-editor@0.45.0
```

Expected: `node_modules/monaco-editor/min/vs/` 目录出现

- [ ] **Step 2: 复制 min/vs 目录到项目资源**

Run:
```bash
cd d:/13.Net/NoteWidget
cp -r temp_monaco/node_modules/monaco-editor/min/vs NoteWidgetAddIn/Resources/js/monaco/vs
```

Expected: `NoteWidgetAddIn/Resources/js/monaco/vs/loader.js` 存在

- [ ] **Step 3: 清理临时文件**

Run:
```bash
cd d:/13.Net/NoteWidget
rm -rf temp_monaco
```

- [ ] **Step 4: 验证关键文件存在**

Run:
```bash
ls NoteWidgetAddIn/Resources/js/monaco/vs/loader.js
ls NoteWidgetAddIn/Resources/js/monaco/vs/editor/editor.main.js
ls NoteWidgetAddIn/Resources/js/monaco/vs/editor/editor.main.css
```

Expected: 三个文件都存在

- [ ] **Step 5: Commit**

```bash
git add NoteWidgetAddIn/Resources/js/monaco/
git commit -m "chore: add Monaco Editor 0.45.0 files"
```

---

## Task 2: 创建 Monaco 宿主 HTML 页面

**Files:**
- Create: `NoteWidgetAddIn/Resources/js/editor-host.html`

- [ ] **Step 1: 创建 editor-host.html**

创建文件 `NoteWidgetAddIn/Resources/js/editor-host.html`，内容如下：

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <style>
        html, body { margin: 0; padding: 0; overflow: hidden; height: 100vh; }
        #container { width: 100%; height: 100%; }
    </style>
</head>
<body>
    <div id="container"></div>
    <script src="monaco/vs/loader.js"></script>
    <script>
        var editor = null;
        var debounceTimer = null;

        require.config({
            paths: { 'vs': 'monaco/vs' }
        });

        require(['vs/editor/editor.main'], function () {
            editor = monaco.editor.create(document.getElementById('container'), {
                value: '',
                language: 'markdown',
                theme: 'vs',
                automaticLayout: true,
                minimap: { enabled: true },
                wordWrap: 'on',
                lineNumbers: 'on',
                autoIndent: 'full',
                bracketPairColorization: { enabled: true },
                scrollBeyondLastLine: false,
                fontSize: 14,
                renderWhitespace: 'selection',
                tabSize: 4
            });

            editor.onDidChangeModelContent(function () {
                clearTimeout(debounceTimer);
                debounceTimer = setTimeout(function () {
                    window.chrome.webview.postMessage(
                        JSON.stringify({ type: 'contentChanged', content: editor.getValue() })
                    );
                }, 500);
            });

            // Notify C# that editor is ready
            window.chrome.webview.postMessage(JSON.stringify({ type: 'editorReady' }));
        });

        // C# callable APIs
        function setContent(content) {
            if (editor) {
                editor.setValue(content);
            }
        }

        function getContent() {
            return editor ? editor.getValue() : '';
        }

        function insertText(prefix, suffix, placeholder) {
            if (!editor) return;
            var selection = editor.getSelection();
            var selectedText = editor.getModel().getValueInRange(selection);
            var text = selectedText || placeholder;
            var newText = prefix + text + (suffix || '');
            editor.executeEdits('toolbar', [{
                range: selection,
                text: newText,
                forceMoveMarkers: true
            }]);
            editor.focus();
        }

        function replaceLine(prefix) {
            if (!editor) return;
            var position = editor.getPosition();
            var model = editor.getModel();
            var lineContent = model.getLineContent(position.lineNumber);
            var newContent = prefix + lineContent.replace(/^#+\s*/, '').replace(/^>\s*/, '').replace(/^[-*]\s*/, '').replace(/^\d+\.\s*/, '');
            editor.executeEdits('toolbar', [{
                range: new monaco.Range(position.lineNumber, 1, position.lineNumber, lineContent.length + 1),
                text: newContent,
                forceMoveMarkers: true
            }]);
            editor.focus();
        }

        function insertAtCursor(text) {
            if (!editor) return;
            var position = editor.getPosition();
            editor.executeEdits('toolbar', [{
                range: new monaco.Range(position.lineNumber, position.column, position.lineNumber, position.column),
                text: text,
                forceMoveMarkers: true
            }]);
            editor.focus();
        }

        function setEditorTheme(theme) {
            monaco.editor.setTheme(theme);
        }

        function markClean() {
            if (editor) {
                var currentVersion = editor.getModel().getAlternativeVersionId();
                editor._cleanVersionId = currentVersion;
            }
        }

        function isDirty() {
            if (!editor) return false;
            return editor.getModel().getAlternativeVersionId() !== editor._cleanVersionId;
        }
    </script>
</body>
</html>
```

- [ ] **Step 2: 验证 HTML 文件无语法错误**

在浏览器中打开文件检查（可选，手动验证）。

- [ ] **Step 3: Commit**

```bash
git add NoteWidgetAddIn/Resources/js/editor-host.html
git commit -m "feat: add Monaco Editor host HTML page"
```

---

## Task 3: 添加 NotePage.UpdateContent 方法与单元测试

**Files:**
- Modify: `NoteWidgetAddIn/Model/NotePage.cs`
- Modify: `NoteWidgetTests/DummyImpl/DummyApplication.cs`
- Create: `NoteWidgetTests/NotePageUpdateTest.cs`

- [ ] **Step 1: 在 NotePage.cs 中添加 UpdateContent 方法**

在文件 `NoteWidgetAddIn/Model/NotePage.cs` 的 `SetMarkdownFlag()` 方法（第226-229行）之后，添加以下方法：

```csharp
public void UpdateContent(string markdown)
{
    var outline = Root.Descendants(Namespace + "Outline").FirstOrDefault();
    if (outline == null) return;

    var oeChildren = outline.Element(Namespace + "OEChildren");
    if (oeChildren == null) return;

    // Remove existing content
    oeChildren.Elements().Remove();

    // Split markdown into lines and create OE elements
    var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    foreach (var line in lines)
    {
        var oe = new XElement(Namespace + "OE",
            new XElement(Namespace + "T", new XCData(line)));
        oeChildren.Add(oe);
    }
}
```

同时，在文件顶部 `using` 区域确保有：

```csharp
using System;
using System.Linq;
```

（这些 using 已存在于文件中，无需额外添加。）

- [ ] **Step 2: 更新 DummyApplication 实现 UpdatePageContent**

在文件 `NoteWidgetTests/DummyImpl/DummyApplication.cs` 中，替换第71-74行的 `UpdatePageContent` 方法（当前抛出 `NotImplementedException`）：

```csharp
private string _lastUpdatedPageXml;

public void UpdatePageContent(string bstrPageChangesXmlIn, DateTime dateExpectedLastModified, XMLSchema xsSchema = XMLSchema.xs2013, bool force = false)
{
    _lastUpdatedPageXml = bstrPageChangesXmlIn;
}

public string GetLastUpdatedPageXml()
{
    return _lastUpdatedPageXml;
}
```

- [ ] **Step 3: 创建测试文件 NotePageUpdateTest.cs**

创建文件 `NoteWidgetTests/NotePageUpdateTest.cs`：

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NoteWidgetAddIn.Model;
using System.Linq;
using System.Xml.Linq;

namespace NoteWidgetAddIn
{
    [TestClass]
    public class NotePageUpdateTest
    {
        private static readonly XNamespace OneNs = "http://schemas.microsoft.com/office/onenote/2013/onenote";

        [TestMethod]
        public void UpdateContent_SingleLine_ReplacesPageContent()
        {
            var page = CreateTestPage("Old content");
            page.UpdateContent("New content");

            var texts = GetOeTexts(page);
            Assert.AreEqual(1, texts.Count);
            Assert.AreEqual("New content", texts[0]);
        }

        [TestMethod]
        public void UpdateContent_MultipleLines_CreatesMultipleOE()
        {
            var page = CreateTestPage("Old");
            page.UpdateContent("Line 1\nLine 2\nLine 3");

            var texts = GetOeTexts(page);
            Assert.AreEqual(3, texts.Count);
            Assert.AreEqual("Line 1", texts[0]);
            Assert.AreEqual("Line 2", texts[1]);
            Assert.AreEqual("Line 3", texts[2]);
        }

        [TestMethod]
        public void UpdateContent_WindowsLineEndings_CreatesMultipleOE()
        {
            var page = CreateTestPage("Old");
            page.UpdateContent("Line 1\r\nLine 2");

            var texts = GetOeTexts(page);
            Assert.AreEqual(2, texts.Count);
            Assert.AreEqual("Line 1", texts[0]);
            Assert.AreEqual("Line 2", texts[1]);
        }

        [TestMethod]
        public void UpdateContent_MarkdownSyntax_PreservedInText()
        {
            var page = CreateTestPage("Plain text");
            page.UpdateContent("# Heading\n**bold** and *italic*\n- list item");

            var texts = GetOeTexts(page);
            Assert.AreEqual("# Heading", texts[0]);
            Assert.AreEqual("**bold** and *italic*", texts[1]);
            Assert.AreEqual("- list item", texts[2]);
        }

        [TestMethod]
        public void UpdateContent_EmptyString_ClearsAllContent()
        {
            var page = CreateTestPage("Existing content");
            page.UpdateContent("");

            var texts = GetOeTexts(page);
            Assert.AreEqual(1, texts.Count);
            Assert.AreEqual("", texts[0]);
        }

        private NotePage CreateTestPage(string content)
        {
            var xml = $@"<?xml version=""1.0""?>
<one:Page xmlns:one=""http://schemas.microsoft.com/office/onenote/2013/onenote""
          ID=""{{test-page-id}}"" lastModifiedTime=""2026-01-01T00:00:00Z"">
  <one:Title>
    <one:OE>
      <one:T><![CDATA[Test Page]]></one:T>
    </one:OE>
  </one:Title>
  <one:Outline>
    <one:OEChildren>
      <one:OE>
        <one:T><![CDATA[{content}]]></one:T>
      </one:OE>
    </one:OEChildren>
  </one:Outline>
</one:Page>";
            return new NotePage(XElement.Parse(xml));
        }

        private System.Collections.Generic.List<string> GetOeTexts(NotePage page)
        {
            return page.Root
                .Descendants(OneNs + "Outline")
                .Elements(OneNs + "OEChildren")
                .Elements(OneNs + "OE")
                .Elements(OneNs + "T")
                .Select(t => t.Value)
                .ToList();
        }
    }
}
```

- [ ] **Step 4: 运行测试验证通过**

Run:
```bash
cd d:/13.Net/NoteWidget
dotnet test NoteWidgetTests/NoteWidgetTests.csproj --filter "FullyQualifiedName~NotePageUpdateTest" --no-build
```

如果上方命令不适用于 .NET Framework 项目，在 Visual Studio 中使用 Test Explorer 运行 `NotePageUpdateTest` 类的所有测试。

Expected: 5 个测试全部 PASS

- [ ] **Step 5: Commit**

```bash
git add NoteWidgetAddIn/Model/NotePage.cs NoteWidgetTests/DummyImpl/DummyApplication.cs NoteWidgetTests/NotePageUpdateTest.cs
git commit -m "feat: add NotePage.UpdateContent for markdown write-back with tests"
```

---

## Task 4: 创建 MarkdownEditorWindow XAML

**Files:**
- Create: `NoteWidgetAddIn/RibbonCommand/Markdown/MarkdownEditorWindow.xaml`

- [ ] **Step 1: 创建 XAML 文件**

创建文件 `NoteWidgetAddIn/RibbonCommand/Markdown/MarkdownEditorWindow.xaml`：

```xml
<Window x:Class="NoteWidgetAddIn.RibbonCommand.Markdown.MarkdownEditorWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:Wpf="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"
        Title="Markdown 编辑器" ShowInTaskbar="True"
        Width="1200" Height="800"
        Icon="/NoteWidgetAddIn;component/Properties/markdown.png"
        Loaded="Window_Loaded" Closed="Window_Closed"
        KeyDown="Window_KeyDown">
    <DockPanel>
        <!-- Toolbar -->
        <ToolBarTray DockPanel.Dock="Top">
            <ToolBar Band="0" BandIndex="0" Name="FormattingToolbar">
                <Button Content="B" FontWeight="Bold" FontSize="13" Padding="6,2"
                        ToolTip="粗体 (Ctrl+B)" Click="Bold_Click" />
                <Button Content="I" FontStyle="Italic" FontSize="13" Padding="6,2"
                        ToolTip="斜体 (Ctrl+I)" Click="Italic_Click" />
                <Separator />
                <Button Content="H1" FontWeight="Bold" Padding="6,2"
                        ToolTip="标题1 (Ctrl+1)" Click="Heading1_Click" />
                <Button Content="H2" FontWeight="Bold" Padding="6,2"
                        ToolTip="标题2 (Ctrl+2)" Click="Heading2_Click" />
                <Button Content="H3" FontWeight="Bold" Padding="6,2"
                        ToolTip="标题3 (Ctrl+3)" Click="Heading3_Click" />
                <Separator />
                <Button Content="Link" Padding="6,2"
                        ToolTip="链接 (Ctrl+K)" Click="Link_Click" />
                <Button Content="Code" Padding="6,2"
                        ToolTip="代码块 (Ctrl+Shift+K)" Click="CodeBlock_Click" />
                <Button Content="`Code`" Padding="6,2"
                        ToolTip="行内代码" Click="InlineCode_Click" />
                <Separator />
                <Button Content="OL" Padding="6,2"
                        ToolTip="有序列表" Click="OrderedList_Click" />
                <Button Content="UL" Padding="6,2"
                        ToolTip="无序列表" Click="UnorderedList_Click" />
                <Button Content="Quote" Padding="6,2"
                        ToolTip="引用" Click="Quote_Click" />
                <Separator />
                <Button Content="---" Padding="6,2"
                        ToolTip="分割线" Click="HorizontalRule_Click" />
                <Button Content="Table" Padding="6,2"
                        ToolTip="表格" Click="Table_Click" />
                <Separator />
                <Button Content="S" Padding="6,2"
                        ToolTip="删除线 (Ctrl+D)" Click="Strikethrough_Click" />
            </ToolBar>
            <ToolBar Band="1" BandIndex="0" Name="ViewToolbar">
                <Button Content="查看" Padding="8,2" Name="ViewModeBtn"
                        ToolTip="仅查看预览" Click="ViewMode_Click" />
                <Button Content="分栏" Padding="8,2" Name="SplitModeBtn"
                        ToolTip="编辑器 + 预览" Click="SplitMode_Click" />
                <Button Content="编辑" Padding="8,2" Name="EditModeBtn"
                        ToolTip="仅编辑器" Click="EditMode_Click" />
                <Separator />
                <Button Content="保存" Padding="8,2" FontWeight="Bold"
                        ToolTip="保存到 OneNote (Ctrl+S)" Click="Save_Click" />
            </ToolBar>
        </ToolBarTray>

        <!-- Status bar -->
        <StatusBar DockPanel.Dock="Bottom">
            <StatusBarItem>
                <TextBlock Name="StatusText" Text="就绪" />
            </StatusBarItem>
        </StatusBar>

        <!-- Main content area -->
        <Grid Name="MainGrid">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Name="EditorColumn" Width="*" MinWidth="200" />
                <ColumnDefinition Name="SplitterColumn" Width="Auto" />
                <ColumnDefinition Name="PreviewColumn" Width="*" MinWidth="200" />
            </Grid.ColumnDefinitions>

            <Wpf:WebView2 Name="editorWebView" Grid.Column="0" />
            <GridSplitter Name="gridSplitter" Grid.Column="1" Width="5"
                          HorizontalAlignment="Center" VerticalAlignment="Stretch"
                          Background="#CCCCCC" />
            <Wpf:WebView2 Name="previewWebView" Grid.Column="2" />
        </Grid>
    </DockPanel>
</Window>
```

- [ ] **Step 2: Commit**

```bash
git add NoteWidgetAddIn/RibbonCommand/Markdown/MarkdownEditorWindow.xaml
git commit -m "feat: add MarkdownEditorWindow XAML layout"
```

---

## Task 5: 创建 MarkdownEditorWindow 代码逻辑

**Files:**
- Create: `NoteWidgetAddIn/RibbonCommand/Markdown/MarkdownEditorWindow.xaml.cs`

这是最大的任务，包含 WebView2 初始化、Monaco 通信、工具栏操作、模式切换和保存逻辑。

- [ ] **Step 1: 创建代码文件**

创建文件 `NoteWidgetAddIn/RibbonCommand/Markdown/MarkdownEditorWindow.xaml.cs`：

```csharp
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using NoteWidgetAddIn.Markdown;
using NoteWidgetAddIn.Model;
using NoteWidgetAddIn.Utils;
using NLog;

namespace NoteWidgetAddIn.RibbonCommand.Markdown
{
    public partial class MarkdownEditorWindow : Window
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private bool _editorReady = false;
        private bool _isDirty = false;
        private string _currentPageId;
        private string _currentPageLastModifiedTime;
        private Func<NoteApplication> _createApp;

        private enum ViewMode { View, Split, Edit }
        private ViewMode _currentMode = ViewMode.Split;

        public MarkdownEditorWindow()
        {
            InitializeComponent();
        }

        public void Initialize(Func<NoteApplication> createApp, NotePage notePage)
        {
            _createApp = createApp;
            _currentPageId = notePage.PageID;
            _currentPageLastModifiedTime = notePage.LastModifiedTime;
            Title = $"Markdown 编辑器 - {notePage.Title.InnerText}";
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await InitEditorWebView();
            await InitPreviewWebView();
            LoadCurrentPageContent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            editorWebView?.Dispose();
            previewWebView?.Dispose();
        }

        #region WebView2 Initialization

        private async Task InitEditorWebView()
        {
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name + "_Editor");
                var env = await CoreWebView2Environment.CreateAsync(null, tempDir, null);
                await editorWebView.EnsureCoreWebView2Async(env);

                editorWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    PathHelper.MappedVirtualHostName,
                    PathHelper.GetWidgetRootPath(),
                    CoreWebView2HostResourceAccessKind.Allow);

                editorWebView.CoreWebView2.WebMessageReceived += Editor_WebMessageReceived;

                var editorUrl = $"http://{PathHelper.MappedVirtualHostName}/resources/js/editor-host.html";
                editorWebView.CoreWebView2.Navigate(editorUrl);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize editor WebView2");
            }
        }

        private async Task InitPreviewWebView()
        {
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name + "_Preview2");
                var env = await CoreWebView2Environment.CreateAsync(null, tempDir, null);
                await previewWebView.EnsureCoreWebView2Async(env);

                previewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    PathHelper.MappedVirtualHostName,
                    PathHelper.GetWidgetRootPath(),
                    CoreWebView2HostResourceAccessKind.Allow);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize preview WebView2");
            }
        }

        #endregion

        #region Monaco Communication

        private void Editor_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var json = e.TryGetWebMessageAsString();
            if (string.IsNullOrEmpty(json)) return;

            // Parse simple JSON: {"type":"editorReady"} or {"type":"contentChanged","content":"..."}
            var msgType = ExtractJsonValue(json, "type");

            if (msgType == "editorReady")
            {
                _editorReady = true;
                LoadCurrentPageContent();
            }
            else if (msgType == "contentChanged")
            {
                var content = ExtractJsonValue(json, "content");
                _isDirty = true;
                UpdateStatusText("未保存");
                UpdatePreview(content);
            }
        }

        private string ExtractJsonValue(string json, string key)
        {
            var pattern = "\"" + key + "\":\"";
            var startIdx = json.IndexOf(pattern, StringComparison.Ordinal);
            if (startIdx < 0)
            {
                pattern = "\"" + key + "\": \"";
                startIdx = json.IndexOf(pattern, StringComparison.Ordinal);
            }
            if (startIdx < 0) return null;

            startIdx += pattern.Length;
            var endIdx = startIdx;
            while (endIdx < json.Length)
            {
                if (json[endIdx] == '"' && json[endIdx - 1] != '\\')
                    break;
                endIdx++;
            }
            return json.Substring(startIdx, endIdx - startIdx).Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        private async void LoadCurrentPageContent()
        {
            if (!_editorReady) return;

            try
            {
                using (var app = _createApp())
                {
                    var page = app.GetNotePage(_currentPageId);
                    if (page != null)
                    {
                        var content = page.ContentInnerText;
                        await ExecuteEditorScript($"setContent({EscapeForJs(content)})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load page content");
            }
        }

        private async void UpdatePreview(string markdownContent)
        {
            try
            {
                var htmlBody = MarkdownHelper.MarkdownToHtml(markdownContent);
                var title = Title.Replace("Markdown 编辑器 - ", "");
                var html = HtmlTemplate.LocalResourceTemplate.ToHtml(title, htmlBody);
                previewWebView.CoreWebView2.NavigateToString(html);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to update preview");
            }
        }

        #endregion

        #region Toolbar Actions

        private async void Bold_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("insertText('**', '**', '粗体文本')");
        }

        private async void Italic_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("insertText('*', '*', '斜体文本')");
        }

        private async void Heading1_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("replaceLine('# ')");
        }

        private async void Heading2_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("replaceLine('## ')");
        }

        private async void Heading3_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("replaceLine('### ')");
        }

        private async void Link_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("insertText('[', '](url)', '链接文本')");
        }

        private async void CodeBlock_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("insertText('\\n```\\n', '\\n```\\n', '代码')");
        }

        private async void InlineCode_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("insertText('`', '`', '代码')");
        }

        private async void OrderedList_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("replaceLine('1. ')");
        }

        private async void UnorderedList_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("replaceLine('- ')");
        }

        private async void Quote_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("replaceLine('> ')");
        }

        private async void HorizontalRule_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("insertAtCursor('\\n---\\n')");
        }

        private async void Table_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("insertAtCursor('\\n| 列1 | 列2 | 列3 |\\n| --- | --- | --- |\\n| 内容 | 内容 | 内容 |\\n')");
        }

        private async void Strikethrough_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteEditorScript("insertText('~~', '~~', '删除线文本')");
        }

        #endregion

        #region View Mode Switching

        private void ViewMode_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = ViewMode.View;
            EditorColumn.Width = new GridLength(0);
            SplitterColumn.Width = new GridLength(0);
            PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
            editorWebView.Visibility = Visibility.Collapsed;
            gridSplitter.Visibility = Visibility.Collapsed;
            FormattingToolbar.IsEnabled = false;
            UpdateStatusText(_isDirty ? "未保存 - 查看模式" : "已保存 - 查看模式");
        }

        private void SplitMode_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = ViewMode.Split;
            EditorColumn.Width = new GridLength(1, GridUnitType.Star);
            SplitterColumn.Width = new GridLength(5);
            PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
            editorWebView.Visibility = Visibility.Visible;
            gridSplitter.Visibility = Visibility.Visible;
            FormattingToolbar.IsEnabled = true;
            UpdateStatusText(_isDirty ? "未保存 - 分栏模式" : "已保存 - 分栏模式");
        }

        private void EditMode_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = ViewMode.Edit;
            EditorColumn.Width = new GridLength(1, GridUnitType.Star);
            SplitterColumn.Width = new GridLength(0);
            PreviewColumn.Width = new GridLength(0);
            editorWebView.Visibility = Visibility.Visible;
            gridSplitter.Visibility = Visibility.Collapsed;
            previewWebView.Visibility = Visibility.Collapsed;
            FormattingToolbar.IsEnabled = true;
            UpdateStatusText(_isDirty ? "未保存 - 编辑模式" : "已保存 - 编辑模式");
        }

        #endregion

        #region Save

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            await SaveToNotePage();
        }

        private async Task SaveToNotePage()
        {
            if (!_editorReady) return;

            try
            {
                var content = await ExecuteEditorScript("getContent()");
                if (content == null) return;

                // Remove surrounding quotes from JSON result
                content = content.Trim('"').Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\\"", "\"").Replace("\\\\", "\\");

                using (var app = _createApp())
                {
                    var page = app.GetNotePage(_currentPageId);

                    // Conflict check
                    if (page != null && page.LastModifiedTime != _currentPageLastModifiedTime)
                    {
                        var result = MessageBox.Show(
                            "OneNote 页面在编辑期间已被修改。是否覆盖？",
                            "冲突检测",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                        if (result != MessageBoxResult.Yes) return;
                    }

                    if (page != null)
                    {
                        page.UpdateContent(content);
                        app.UpdatePage(page);
                        _currentPageLastModifiedTime = page.LastModifiedTime;
                        _isDirty = false;
                        await ExecuteEditorScript("markClean()");
                        UpdateStatusText("已保存");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save to OneNote");
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Keyboard Shortcuts

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.S:
                        e.Handled = true;
                        await SaveToNotePage();
                        break;
                    case Key.D1:
                        e.Handled = true;
                        await ExecuteEditorScript("replaceLine('# ')");
                        break;
                    case Key.D2:
                        e.Handled = true;
                        await ExecuteEditorScript("replaceLine('## ')");
                        break;
                    case Key.D3:
                        e.Handled = true;
                        await ExecuteEditorScript("replaceLine('### ')");
                        break;
                }
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (e.Key == Key.K)
                {
                    e.Handled = true;
                    await ExecuteEditorScript("insertText('\\n```\\n', '\\n```\\n', '代码')");
                }
            }
        }

        #endregion

        #region Helpers

        private async Task<string> ExecuteEditorScript(string script)
        {
            if (editorWebView?.CoreWebView2 == null) return null;
            try
            {
                return await editorWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to execute script: {script}");
                return null;
            }
        }

        private string EscapeForJs(string text)
        {
            if (text == null) return "''";
            var escaped = text
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r\n", "\\n")
                .Replace("\r", "\\n")
                .Replace("\n", "\\n")
                .Replace("\"", "\\\"");
            return $"'{escaped}'";
        }

        private void UpdateStatusText(string text)
        {
            StatusText.Text = text;
        }

        #endregion
    }
}
```

- [ ] **Step 2: 验证编译通过**

在 Visual Studio 中编译项目，确保无编译错误。

注意：此时尚未添加 csproj 引用和 Ribbon 命令，编译可能需要完成 Task 6 和 Task 7 后才能完全通过。

- [ ] **Step 3: Commit**

```bash
git add NoteWidgetAddIn/RibbonCommand/Markdown/MarkdownEditorWindow.xaml.cs
git commit -m "feat: add MarkdownEditorWindow code-behind with Monaco integration"
```

---

## Task 6: 创建 MarkdownEditorCommand

**Files:**
- Create: `NoteWidgetAddIn/RibbonCommand/MarkdownEditorCommand.cs`

- [ ] **Step 1: 创建命令文件**

创建文件 `NoteWidgetAddIn/RibbonCommand/MarkdownEditorCommand.cs`：

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NoteWidgetAddIn.Model;
using NoteWidgetAddIn.RibbonCommand.Markdown;

namespace NoteWidgetAddIn.RibbonCommand
{
    internal class MarkdownEditorCommand : Command
    {
        private static Dictionary<int, MarkdownEditorWindow> _editorWindows = new Dictionary<int, MarkdownEditorWindow>();

        public override async Task ExecuteAsync(params object[] args)
        {
            await OpenEditorForCurrentPage();
        }

        private async Task OpenEditorForCurrentPage()
        {
            if (!TryGetCurrentNotePage(out var notePage)) return;

            await WpfAddInApplication.Current.BeginInvoke(() =>
            {
                var window = new MarkdownEditorWindow();
                window.Initialize(() => Context.CreateApplication(), notePage);

                var helper = new System.Windows.Interop.WindowInteropHelper(window);
                helper.Owner = OwnerWin32Window.Handle;

                window.Closed += (s, e) =>
                {
                    var key = window.GetHashCode();
                    if (_editorWindows.ContainsKey(key))
                    {
                        _editorWindows.Remove(key);
                    }
                };

                _editorWindows.Add(window.GetHashCode(), window);
                window.Show();
            });
        }

        private bool TryGetCurrentNotePage(out NotePage currentNotePage)
        {
            try
            {
                using (var app = Context.CreateApplication())
                {
                    currentNotePage = app.GetCurrentNotePage();
                }
                return currentNotePage != null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            currentNotePage = null;
            return false;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add NoteWidgetAddIn/RibbonCommand/MarkdownEditorCommand.cs
git commit -m "feat: add MarkdownEditorCommand for ribbon integration"
```

---

## Task 7: 更新 Ribbon XML 和 AddIn.Ribbon.cs

**Files:**
- Modify: `NoteWidgetAddIn/Properties/ribbon.xml`
- Modify: `NoteWidgetAddIn/AddIn.Ribbon.cs`

- [ ] **Step 1: 更新 ribbon.xml**

将文件 `NoteWidgetAddIn/Properties/ribbon.xml` 的第5-25行（`<tab idMso="TabHome">` 及其内容）替换为：

```xml
			<tab id="tabNoteWidgetMarkdown" label="Markdown" insertAfterQ="TabView">
				<group id="groupNoteWidgetMarkdown" label="工具">
					<button id="viewMarkdownAsHtmlButton" size="large"
							label="预览"
							screentip="将 Markdown 内容预览为 HTML 文档"
							onAction="PreviewMarkdownCmd"
							image="markdown.png"
				            />
					<button id="markdownEditorButton" size="large"
							label="编辑"
							screentip="编辑当前页面的 Markdown 内容"
							onAction="MarkdownEditorCmd"
							image="markdown.png"
				            />
		            <button id="markdownCheatsheetButton" size="large"
		                    label="速查表"
		                    screentip="查看 Markdown 语法速查表"
		                    onAction="MarkdownCheatsheetCmd"
		                    image="markdownflag.png"
		                    />
		            <button id="widgetAdvancedSettings" size="large"
		                    label="设置"
		                    screentip="高级设置"
		                    onAction="WidgetAdvancedSettingsCmd"
		                    image="settings.png"
		                    />
				</group>
			</tab>
```

完整的 ribbon.xml 应变为：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<customUI xmlns="http://schemas.microsoft.com/office/2009/07/customui" loadImage="GetImage">
	<ribbon>
		<tabs>
			<tab id="tabNoteWidgetMarkdown" label="Markdown" insertAfterQ="TabView">
				<group id="groupNoteWidgetMarkdown" label="工具">
					<button id="viewMarkdownAsHtmlButton" size="large"
							label="预览"
							screentip="将 Markdown 内容预览为 HTML 文档"
							onAction="PreviewMarkdownCmd"
							image="markdown.png"
				            />
					<button id="markdownEditorButton" size="large"
							label="编辑"
							screentip="编辑当前页面的 Markdown 内容"
							onAction="MarkdownEditorCmd"
							image="markdown.png"
				            />
		            <button id="markdownCheatsheetButton" size="large"
		                    label="速查表"
		                    screentip="查看 Markdown 语法速查表"
		                    onAction="MarkdownCheatsheetCmd"
		                    image="markdownflag.png"
		                    />
		            <button id="widgetAdvancedSettings" size="large"
		                    label="设置"
		                    screentip="高级设置"
		                    onAction="WidgetAdvancedSettingsCmd"
		                    image="settings.png"
		                    />
				</group>
			</tab>
		</tabs>
		<contextMenus>
			<contextMenu idMso="ContextMenuNotebook">
				<menuSeparator id="ctxNoteWidgetNotebookSeparator" />
				<menu id="ctxExportToPathNotebook" label="导出笔记本" imageMso="FileSaveAsContextualItem">
					<button id="ctxExportToPathNotebookButton"
						label ="分层文件..."
						screentip="将每页导出为独立文件，保留层级结构"
						onAction="ExportPathCmd"
						tag="Notebook" />
					<button id="ctxExportToFileNotebookButton"
						label ="单个文件..."
						screentip="将所有页面导出为一个文件"
						onAction="ExportFileCmd"
						tag="Notebook" />
				</menu>
			</contextMenu>
	        <contextMenu idMso="ContextMenuSectionGroup">
				<menuSeparator id="ctxNoteWidgetSectionGrouptSeparator" />
				<menu id="ctxExportToPathSectionGroup" label="导出分区组" imageMso="FileSaveAsContextualItem">
					<button id="ctxExportToPathSectionGroupButton"
						label ="分层文件..."
						screentip="将每页导出为独立文件，保留层级结构"
						onAction="ExportPathCmd"
						tag="SectionGroup" />
					<button id="ctxExportToFileSectionGroupButton"
						label ="单个文件..."
						screentip="将所有页面导出为一个文件"
						onAction="ExportFileCmd"
						tag="SectionGroup" />
				</menu>
			</contextMenu>
			<contextMenu idMso="ContextMenuSection">
				<menuSeparator id="ctxNoteWidgetSectionSeparator" />
				<menu id="ctxExportToPathSection" label="导出分区" imageMso="FileSaveAsContextualItem">
					<button id="ctxExportToPathSectionButton"
						label ="分层文件..."
						screentip="将每页导出为独立文件，保留层级结构"
						onAction="ExportPathCmd"
						tag="Section" />
					<button id="ctxExportToFileSectionButton"
						label ="单个文件..."
						screentip="将所有页面导出为一个文件"
						onAction="ExportFileCmd"
						tag="Section" />
				</menu>
			</contextMenu>

			<contextMenu idMso="ContextMenuPage">
	            <menuSeparator id="ctxNoteWidgetPageSeparator" />
				<button id="ctxExportToFilePageButton"
					label ="导出页面..."
					imageMso="FileSaveAsContextualItem"
					screentip="将页面导出为文件"
					onAction="ExportFileCmd"
					tag="Page" />
			</contextMenu>
		</contextMenus>
	</customUI>
```

- [ ] **Step 2: 在 AddIn.Ribbon.cs 中添加编辑器命令**

在文件 `NoteWidgetAddIn/AddIn.Ribbon.cs` 的第64行（`PreviewMarkdownCmd` 方法）之后，添加：

```csharp
        /// <summary>
        /// Edit current page's markdown content in editor window.
        /// </summary>
        public async Task MarkdownEditorCmd(IRibbonControl control) => await _commandFactory.Run<MarkdownEditorCommand>();
```

- [ ] **Step 3: Commit**

```bash
git add NoteWidgetAddIn/Properties/ribbon.xml NoteWidgetAddIn/AddIn.Ribbon.cs
git commit -m "feat: move ribbon to independent Markdown tab and add editor button"
```

---

## Task 8: 更新 csproj 并验证编译

**Files:**
- Modify: `NoteWidgetAddIn/NoteWidgetAddIn.csproj`

- [ ] **Step 1: 在 csproj 中添加 Monaco 资源文件引用**

在文件 `NoteWidgetAddIn/NoteWidgetAddIn.csproj` 中，找到第198行（`<Content Include="Resources\js\prism.js" />`），在其后添加：

```xml
	    <Content Include="Resources\js\editor-host.html" />
	    <Content Include="Resources\js\monaco\**\*">
	      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
	    </Content>
```

- [ ] **Step 2: 在 csproj 中添加 MarkdownEditorWindow XAML 页面引用**

在同一文件中，找到第215-218行（`WebBrowserWindow.xaml` 的 `<Page>` 项），在其后添加：

```xml
	    <Page Include="RibbonCommand\Markdown\MarkdownEditorWindow.xaml">
	      <SubType>Designer</SubType>
	      <Generator>MSBuild:Compile</Generator>
	    </Page>
```

- [ ] **Step 3: 在 Visual Studio 中编译整个解决方案**

在 Visual Studio 中：Build → Build Solution

Expected: 编译成功，0 个错误

- [ ] **Step 4: 运行所有测试**

在 Visual Studio Test Explorer 中运行全部测试。

Expected: 所有测试通过（包括 Task 3 新增的 NotePageUpdateTest）

- [ ] **Step 5: Commit**

```bash
git add NoteWidgetAddIn/NoteWidgetAddIn.csproj
git commit -m "feat: add Monaco resources and editor window to csproj"
```

---

## Task 9: 集成测试与手动验证

此任务需在安装了 OneNote 的环境中手动测试。

- [ ] **Step 1: 部署插件到 OneNote**

使用 `deploy.bat` 或手动将编译产物复制到 OneNote 插件目录。

- [ ] **Step 2: 验证 Ribbon 页签**

1. 打开 OneNote
2. 确认 "Markdown" 页签出现在 "视图" 页签之后
3. 确认 "预览"、"编辑"、"速查表"、"设置" 按钮都在该页签中

- [ ] **Step 3: 验证编辑器窗口基本功能**

1. 在 OneNote 中创建一个包含 Markdown 内容的页面
2. 点击 "编辑" 按钮
3. 确认编辑器窗口打开，显示分栏模式（左编辑器 + 右预览）
4. 确认 Monaco 编辑器加载成功，显示 Markdown 语法高亮
5. 确认预览面板正确渲染 Markdown

- [ ] **Step 4: 验证工具栏操作**

1. 选中文字，点击 "B"（粗体），确认 `**text**` 被正确插入
2. 测试 "I"（斜体）、"H1/H2/H3"（标题）、"Link"（链接）
3. 确认每次操作后预览实时更新

- [ ] **Step 5: 验证保存功能**

1. 在编辑器中修改内容
2. 确认状态栏显示 "未保存"
3. 按 Ctrl+S 或点击 "保存" 按钮
4. 确认状态栏变为 "已保存"
5. 切换到 OneNote 页面，确认内容已更新

- [ ] **Step 6: 验证模式切换**

1. 点击 "查看"，确认只显示预览
2. 点击 "分栏"，确认恢复编辑器 + 预览
3. 点击 "编辑"，确认只显示编辑器

- [ ] **Step 7: 修复发现的问题并提交**

```bash
git add -A
git commit -m "fix: resolve integration issues from manual testing"
```
