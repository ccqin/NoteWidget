// Copyright (c) Efrey Kong. All Rights Reserved.
// Licensed under the Apache License, Version 2.0.

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
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private Func<NoteApplication> _createApp;
        private NotePage _notePage;
        private string _lastSavedContent;
        private bool _editorReady;
        private bool _previewInitialized;

        private enum ViewMode { View, Split, Edit }
        private ViewMode _currentViewMode = ViewMode.Split;

        public MarkdownEditorWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the editor with a NoteApplication factory and the note page to edit.
        /// </summary>
        public void Initialize(Func<NoteApplication> createApp, NotePage notePage)
        {
            _createApp = createApp;
            _notePage = notePage;
            _lastSavedContent = notePage.ContentInnerText;
        }

        #region Window Events

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await InitEditorWebView();
                await InitPreviewWebView();
                SetViewMode(ViewMode.Split);
                UpdateStatus("就绪");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize editor window.");
                MessageBox.Show("初始化编辑器失败: " + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                if (editorWebView != null)
                {
                    editorWebView.Dispose();
                }
                if (previewWebView != null)
                {
                    previewWebView.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error disposing WebView2 controls.");
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+S: Save
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                e.Handled = true;
                Save_Click(sender, e);
                return;
            }

            // Ctrl+1: Heading 1
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.D1)
            {
                e.Handled = true;
                Heading1_Click(sender, e);
                return;
            }

            // Ctrl+2: Heading 2
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.D2)
            {
                e.Handled = true;
                Heading2_Click(sender, e);
                return;
            }

            // Ctrl+3: Heading 3
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.D3)
            {
                e.Handled = true;
                Heading3_Click(sender, e);
                return;
            }

            // Ctrl+B: Bold
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.B)
            {
                e.Handled = true;
                Bold_Click(sender, e);
                return;
            }

            // Ctrl+I: Italic
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.I)
            {
                e.Handled = true;
                Italic_Click(sender, e);
                return;
            }

            // Ctrl+K: Link
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.K)
            {
                e.Handled = true;
                Link_Click(sender, e);
                return;
            }

            // Ctrl+D: Strikethrough
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.D)
            {
                e.Handled = true;
                Strikethrough_Click(sender, e);
                return;
            }

            // Ctrl+Shift+K: Code block
            if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.K)
            {
                e.Handled = true;
                CodeBlock_Click(sender, e);
                return;
            }
        }

        #endregion

        #region WebView2 Initialization

        private async Task InitEditorWebView()
        {
            var tempDir = Path.Combine(Path.GetTempPath(),
                Assembly.GetExecutingAssembly().GetName().Name + "_editor");
            var env = await CoreWebView2Environment.CreateAsync(null, tempDir, null);
            await editorWebView.EnsureCoreWebView2Async(env);

            editorWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                PathHelper.MappedVirtualHostName,
                PathHelper.GetWidgetRootPath(),
                CoreWebView2HostResourceAccessKind.Allow);

            editorWebView.CoreWebView2.WebMessageReceived += EditorWebMessageReceived;

            // Navigate to the Monaco editor host page
            var editorUrl = string.Format("https://{0}/js/editor-host.html",
                PathHelper.MappedVirtualHostName);
            editorWebView.CoreWebView2.Navigate(editorUrl);
        }

        private async Task InitPreviewWebView()
        {
            var tempDir = Path.Combine(Path.GetTempPath(),
                Assembly.GetExecutingAssembly().GetName().Name + "_preview");
            var env = await CoreWebView2Environment.CreateAsync(null, tempDir, null);
            await previewWebView.EnsureCoreWebView2Async(env);

            previewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                PathHelper.MappedVirtualHostName,
                PathHelper.GetWidgetRootPath(),
                CoreWebView2HostResourceAccessKind.Allow);

            _previewInitialized = true;
        }

        #endregion

        #region Editor Communication

        private void EditorWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(message))
                    return;

                var msgType = ExtractJsonValue(message, "type");
                if (msgType == null)
                    return;

                if (msgType == "editorReady")
                {
                    _editorReady = true;
                    // Set initial content in the editor
                    if (_notePage != null)
                    {
                        var escapedContent = EscapeJsonString(_notePage.ContentInnerText);
                        editorWebView.ExecuteScriptAsync(
                            string.Format("setContent(\"{0}\")", escapedContent));
                        // Mark initial state as clean
                        editorWebView.ExecuteScriptAsync("markClean()");
                    }
                    UpdateStatus("编辑器就绪");
                }
                else if (msgType == "contentChanged")
                {
                    var content = ExtractJsonValue(message, "content");
                    if (content != null)
                    {
                        UpdatePreview(content);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error processing web message from editor.");
            }
        }

        private void UpdatePreview(string markdownContent)
        {
            if (!_previewInitialized)
                return;

            try
            {
                var htmlBody = MarkdownHelper.MarkdownToHtml(markdownContent);
                var title = _notePage != null ? _notePage.Title.InnerText : "预览";
                var htmlDoc = HtmlTemplate.LocalResourceTemplate.ToHtml(title, htmlBody);
                previewWebView.CoreWebView2.NavigateToString(htmlDoc);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating preview.");
            }
        }

        #endregion

        #region Toolbar Formatting

        private void Bold_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript("insertText(\"**\", \"**\", \"粗体文字\")");
        }

        private void Italic_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript("insertText(\"*\", \"*\", \"斜体文字\")");
        }

        private void Heading1_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript("replaceLine(\"# \")");
        }

        private void Heading2_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript("replaceLine(\"## \")");
        }

        private void Heading3_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript("replaceLine(\"### \")");
        }

        private void Link_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript("insertText(\"[\", \"](url)\", \"链接文字\")");
        }

        private void CodeBlock_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript("insertText(\"```\\n\", \"\\n```\", \"代码\")");
        }

        private void InlineCode_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript("insertText(\"`\", \"`\", \"代码\")");
        }

        private void OrderedList_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript("replaceLine(\"1. \")");
        }

        private void UnorderedList_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript("replaceLine(\"- \")");
        }

        private void Quote_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript("replaceLine(\"> \")");
        }

        private void HorizontalRule_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript("insertAtCursor(\"\\n---\\n\")");
        }

        private void Table_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript(
                "insertAtCursor(\"\\n| 标题1 | 标题2 | 标题3 |\\n| --- | --- | --- |\\n| 内容 | 内容 | 内容 |\\n\")");
        }

        private void Strikethrough_Click(object sender, RoutedEventArgs e)
        {
            ExecuteEditorScript("insertText(\"~~\", \"~~\", \"删除线文字\")");
        }

        #endregion

        #region View Modes

        private void ViewMode_Click(object sender, RoutedEventArgs e)
        {
            SetViewMode(ViewMode.View);
        }

        private void SplitMode_Click(object sender, RoutedEventArgs e)
        {
            SetViewMode(ViewMode.Split);
        }

        private void EditMode_Click(object sender, RoutedEventArgs e)
        {
            SetViewMode(ViewMode.Edit);
        }

        private void SetViewMode(ViewMode mode)
        {
            _currentViewMode = mode;

            switch (mode)
            {
                case ViewMode.View:
                    EditorColumn.Width = new GridLength(0);
                    SplitterColumn.Width = new GridLength(0);
                    PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
                    editorWebView.Visibility = Visibility.Collapsed;
                    gridSplitter.Visibility = Visibility.Collapsed;
                    previewWebView.Visibility = Visibility.Visible;
                    break;
                case ViewMode.Split:
                    EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                    SplitterColumn.Width = GridLength.Auto;
                    PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
                    editorWebView.Visibility = Visibility.Visible;
                    gridSplitter.Visibility = Visibility.Visible;
                    previewWebView.Visibility = Visibility.Visible;
                    break;
                case ViewMode.Edit:
                    EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                    SplitterColumn.Width = new GridLength(0);
                    PreviewColumn.Width = new GridLength(0);
                    editorWebView.Visibility = Visibility.Visible;
                    gridSplitter.Visibility = Visibility.Collapsed;
                    previewWebView.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        #endregion

        #region Save

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            await SaveContentAsync();
        }

        private async Task SaveContentAsync()
        {
            if (!_editorReady)
            {
                UpdateStatus("编辑器未就绪，无法保存");
                return;
            }

            try
            {
                UpdateStatus("正在保存...");

                // Get content from Monaco editor
                var result = await editorWebView.ExecuteScriptAsync("getContent()");
                var content = ExtractStringResult(result);
                if (content == null)
                {
                    UpdateStatus("获取内容失败");
                    return;
                }

                // Check for conflict: reload page and compare lastModifiedTime
                using (var app = _createApp())
                {
                    var currentPage = app.GetNotePage(_notePage.PageID);
                    if (currentPage != null && currentPage.LastModifiedTime != _notePage.LastModifiedTime)
                    {
                        var choice = MessageBox.Show(
                            "此页面在 OneNote 中已被修改。是否覆盖？\n\n选择\"是\"将覆盖 OneNote 中的内容，选择\"否\"将取消保存。",
                            "冲突检测",
                            MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (choice != MessageBoxResult.Yes)
                        {
                            UpdateStatus("保存已取消");
                            return;
                        }
                        // Refresh our reference to the latest page version
                        _notePage = currentPage;
                    }
                }

                // Update content and save
                _notePage.UpdateContent(content);

                using (var app2 = _createApp())
                {
                    app2.UpdatePage(_notePage);
                }

                _lastSavedContent = content;

                // Mark clean after successful save
                await editorWebView.ExecuteScriptAsync("markClean()");

                UpdateStatus("已保存");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error saving content.");
                MessageBox.Show("保存失败: " + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("保存失败");
            }
        }

        #endregion

        #region JSON Helpers

        /// <summary>
        /// Extract a value from a JSON string by key.
        /// Simple string-based extraction for C# 7.3 (no JSON library required).
        /// Handles: {"type":"editorReady"} or {"type":"contentChanged","content":"..."}
        /// </summary>
        private static string ExtractJsonValue(string json, string key)
        {
            var searchKey = "\"" + key + "\":\"";
            var startIndex = json.IndexOf(searchKey, StringComparison.Ordinal);
            if (startIndex < 0)
            {
                // Try without quotes (for non-string values)
                searchKey = "\"" + key + "\":";
                startIndex = json.IndexOf(searchKey, StringComparison.Ordinal);
                if (startIndex < 0)
                    return null;
                startIndex += searchKey.Length;
                var endIndex = json.IndexOfAny(new[] { ',', '}', ']' }, startIndex);
                if (endIndex < 0)
                    endIndex = json.Length;
                return json.Substring(startIndex, endIndex - startIndex).Trim().Trim('"');
            }

            startIndex += searchKey.Length;
            var escapeIndex = startIndex;
            var resultBuilder = new System.Text.StringBuilder();
            while (escapeIndex < json.Length)
            {
                var ch = json[escapeIndex];
                if (ch == '\\' && escapeIndex + 1 < json.Length)
                {
                    var nextCh = json[escapeIndex + 1];
                    if (nextCh == '"' || nextCh == '\\' || nextCh == '/')
                    {
                        resultBuilder.Append(nextCh);
                        escapeIndex += 2;
                        continue;
                    }
                    if (nextCh == 'n')
                    {
                        resultBuilder.Append('\n');
                        escapeIndex += 2;
                        continue;
                    }
                    if (nextCh == 'r')
                    {
                        resultBuilder.Append('\r');
                        escapeIndex += 2;
                        continue;
                    }
                    if (nextCh == 't')
                    {
                        resultBuilder.Append('\t');
                        escapeIndex += 2;
                        continue;
                    }
                }
                if (ch == '"')
                {
                    break;
                }
                resultBuilder.Append(ch);
                escapeIndex++;
            }
            return resultBuilder.ToString();
        }

        /// <summary>
        /// Extract the string result from WebView2 ExecuteScriptAsync response.
        /// The result is JSON-encoded, e.g. "\"content here\"" for strings.
        /// </summary>
        private static string ExtractStringResult(string jsonResult)
        {
            if (string.IsNullOrEmpty(jsonResult))
                return null;

            // ExecuteScriptAsync returns JSON-encoded string: "null" for null, "\"text\"" for strings
            if (jsonResult == "null")
                return null;

            // Remove surrounding quotes and unescape
            if (jsonResult.Length >= 2 && jsonResult[0] == '"' && jsonResult[jsonResult.Length - 1] == '"')
            {
                return UnescapeJsonString(jsonResult.Substring(1, jsonResult.Length - 2));
            }

            return jsonResult;
        }

        /// <summary>
        /// Unescape a JSON string value (handles \\, \", \/, \n, \r, \t, \uXXXX).
        /// </summary>
        private static string UnescapeJsonString(string value)
        {
            if (value == null)
                return null;

            var sb = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\\' && i + 1 < value.Length)
                {
                    var next = value[i + 1];
                    switch (next)
                    {
                        case '"': sb.Append('"'); i++; break;
                        case '\\': sb.Append('\\'); i++; break;
                        case '/': sb.Append('/'); i++; break;
                        case 'n': sb.Append('\n'); i++; break;
                        case 'r': sb.Append('\r'); i++; break;
                        case 't': sb.Append('\t'); i++; break;
                        case 'b': sb.Append('\b'); i++; break;
                        case 'f': sb.Append('\f'); i++; break;
                        case 'u':
                            if (i + 5 < value.Length)
                            {
                                var hex = value.Substring(i + 2, 4);
                                int codePoint;
                                if (int.TryParse(hex,
                                    System.Globalization.NumberStyles.HexNumber,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out codePoint))
                                {
                                    sb.Append((char)codePoint);
                                }
                                else
                                {
                                    sb.Append(value[i]);
                                }
                                i += 5;
                            }
                            else
                            {
                                sb.Append(value[i]);
                            }
                            break;
                        default:
                            sb.Append(value[i]);
                            break;
                    }
                }
                else
                {
                    sb.Append(value[i]);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Escape a string for embedding in a JSON string literal.
        /// Handles: backslash, double quote, newline, carriage return, tab.
        /// </summary>
        private static string EscapeJsonString(string value)
        {
            if (value == null)
                return string.Empty;

            var sb = new System.Text.StringBuilder(value.Length + 16);
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (ch < ' ')
                        {
                            sb.AppendFormat("\\u{0:X4}", (int)ch);
                        }
                        else
                        {
                            sb.Append(ch);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        #endregion

        #region Utilities

        private void ExecuteEditorScript(string script)
        {
            if (!_editorReady)
                return;

            try
            {
                editorWebView.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error executing editor script: " + script);
            }
        }

        private void UpdateStatus(string text)
        {
            StatusText.Text = text;
        }

        #endregion
    }
}
