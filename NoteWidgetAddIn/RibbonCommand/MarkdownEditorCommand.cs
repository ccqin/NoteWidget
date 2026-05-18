// Copyright (c) Efrey Kong. All Rights Reserved.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
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
