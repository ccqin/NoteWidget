// Copyright (c) Efrey Kong. All Rights Reserved.
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;

namespace NoteWidgetAddIn.Markdown
{
    public enum TemplateResourceType
    {
        Local,
        Online
    }
    public enum ColorScheme
    {
        /// <summary>
        /// Follow system theme. Default theme.
        /// </summary>
        [Description("跟随系统设置")]
        System = 0,
        [Description("浅色")]
        Light = 1,
        [Description("深色")]
        Dark = 2
    }
    /// <summary>
    /// Source code hight theme
    /// </summary>
    public enum HighlightTheme
    {
        Default,
        Coy,
        Dark,
        Funky,
        Okaidia,
        Solarizedlight,
        Tomorrow,
        Twilight
    }
    public interface ITemplateExtension
    {
        void Setup(HtmlTemplateBuilder builder, TemplateResourceType resourceType);
    }
}
