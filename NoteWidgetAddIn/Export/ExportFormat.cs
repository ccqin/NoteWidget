// Copyright (c) Efrey Kong. All Rights Reserved.
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using Microsoft.Office.Interop.OneNote;
using NoteWidgetAddIn.Model;

#pragma warning disable CS3016
namespace NoteWidgetAddIn
{
    public enum ExportFormat
    {
        [Description("PDF 文件(*.pdf)")]
        [RestrictedNodeType(NodeType.Notebook, NodeType.Section, NodeType.Page)]
        PDF = PublishFormat.pfPDF,
        [Description("XPS 文档(*.xps)")]
        [RestrictedNodeType(NodeType.Notebook, NodeType.Section, NodeType.Page)]
        XPS = PublishFormat.pfXPS,
        [Description("单个文件网页(*.mht)")]
        [RestrictedNodeType(NodeType.Section)]
        MHTML = PublishFormat.pfMHTML,
        [Description("Word 文档(*.docx)")]
        [RestrictedNodeType(NodeType.Section)]
        Word = PublishFormat.pfWord,
        [Description("Markdown 文档(*.md)")]
        Markdown = 100,
        [Description("HTML 文档(*.html)")]
        Html = 101
    }
}
