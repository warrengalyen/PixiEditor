﻿using System.Windows.Input;
using ChunkyImageLib.DataHolders;
using PixiEditor.Models.Commands.Attributes.Commands;

namespace PixiEditor.ViewModels.SubViewModels.Tools.Tools;

[Command.Tool(Key = Key.R)]
internal class RectangleToolViewModel : ShapeTool
{
    private string defaultActionDisplay = "Click and move to draw a rectangle. Hold Shift to draw a square.";
    public RectangleToolViewModel()
    {
        ActionDisplay = defaultActionDisplay;
    }

    public override string Tooltip => $"Draws rectangle on canvas ({Shortcut}). Hold Shift to draw a square.";

    public bool Filled { get; set; } = false;

    public override void UpdateActionDisplay(bool ctrlIsDown, bool shiftIsDown, bool altIsDown)
    {
        if (shiftIsDown)
            ActionDisplay = "Click and move to draw a square.";
        else
            ActionDisplay = defaultActionDisplay;
    }
    
    public override void OnLeftMouseButtonDown(VecD pos)
    {
        ViewModelMain.Current?.DocumentManagerSubViewModel.ActiveDocument?.Tools.UseRectangleTool();
    }
}
