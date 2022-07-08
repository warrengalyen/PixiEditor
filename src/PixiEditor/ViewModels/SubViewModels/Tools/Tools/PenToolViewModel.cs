﻿using System.Windows.Input;
using ChunkyImageLib.DataHolders;
using PixiEditor.Models.Commands.Attributes.Commands;
using PixiEditor.ViewModels.SubViewModels.Tools.ToolSettings.Toolbars;

namespace PixiEditor.ViewModels.SubViewModels.Tools.Tools
{
    [Command.Tool(Key = Key.B)]
    internal class PenToolViewModel : ShapeTool
    {
        public PenToolViewModel()
        {
            Cursor = Cursors.Pen;
            ActionDisplay = "Click and move to draw.";
            Toolbar = new PenToolbar();
        }

        public override string Tooltip => $"Standard brush. ({Shortcut})";

        public override void OnLeftMouseButtonDown(VecD pos)
        {
            ViewModelMain.Current?.DocumentManagerSubViewModel.ActiveDocument?.UsePenTool();
        }
    }
}
