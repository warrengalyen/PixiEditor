﻿using System.Collections.ObjectModel;
using System.Windows.Input;
using PixiEditor.ChangeableDocument.Enums;
using PixiEditor.Models.Commands.Attributes.Commands;
using PixiEditor.Models.Dialogs;
using PixiEditor.Models.Events;
using PixiEditor.ViewModels.SubViewModels.Tools.Tools;
using PixiEditor.Views.UserControls.SymmetryOverlay;

namespace PixiEditor.ViewModels.SubViewModels.Document;
#nullable enable
[Command.Group("PixiEditor.Document", "Image")]
internal class DocumentManagerViewModel : SubViewModel<ViewModelMain>
{
    public ObservableCollection<DocumentViewModel> Documents { get; } = new ObservableCollection<DocumentViewModel>();
    public event EventHandler<DocumentChangedEventArgs>? ActiveDocumentChanged;


    private DocumentViewModel? activeDocument;
    public DocumentViewModel? ActiveDocument
    {
        get => activeDocument;
        // Use WindowSubViewModel.MakeDocumentViewportActive(document);
        private set
        {
            if (activeDocument == value)
                return;
            DocumentViewModel? prevDoc = activeDocument;
            activeDocument = value;
            RaisePropertyChanged(nameof(ActiveDocument));
            ActiveDocumentChanged?.Invoke(this, new(value, prevDoc));
            
            if (ViewModelMain.Current.ToolsSubViewModel.ActiveTool == null)
            {
                ViewModelMain.Current.ToolsSubViewModel.SetActiveTool<PenToolViewModel>();
            }
        }
    }

    public DocumentManagerViewModel(ViewModelMain owner) : base(owner)
    {
        owner.WindowSubViewModel.ActiveViewportChanged += (_, args) => ActiveDocument = args.Document;
    }

    public void MakeActiveDocumentNull() => ActiveDocument = null;

    [Evaluator.CanExecute("PixiEditor.HasDocument")]
    public bool DocumentNotNull() => ActiveDocument != null;

    [Command.Basic("PixiEditor.Document.ClipCanvas", "Clip Canvas", "Clip Canvas", CanExecute = "PixiEditor.HasDocument")]
    public void ClipCanvas()
    {
        if (ActiveDocument is null)
            return;
        
        ActiveDocument?.Operations.ClipCanvas();
    }

    [Command.Basic("PixiEditor.Document.ToggleVerticalSymmetryAxis", "Toggle vertical symmetry axis", "Toggle vertical symmetry axis", CanExecute = "PixiEditor.HasDocument")]
    public void ToggleVerticalSymmetryAxis()
    {
        if (ActiveDocument is null)
            return;
        ActiveDocument.VerticalSymmetryAxisEnabledBindable ^= true;
    }

    [Command.Basic("PixiEditor.Document.ToggleHorizontalSymmetryAxis", "Toggle horizontal symmetry axis", "Toggle horizontal symmetry axis", CanExecute = "PixiEditor.HasDocument")]
    public void ToggleHorizontalSymmetryAxis()
    {
        if (ActiveDocument is null)
            return;
        ActiveDocument.HorizontalSymmetryAxisEnabledBindable ^= true;
    }

    [Command.Internal("PixiEditor.Document.DragSymmetry", CanExecute = "PixiEditor.HasDocument")]
    public void DragSymmetry(SymmetryAxisDragInfo info)
    {
        if (ActiveDocument is null)
            return;
        ActiveDocument.EventInlet.OnSymmetryDragged(info);
    }

    [Command.Internal("PixiEditor.Document.StartDragSymmetry", CanExecute = "PixiEditor.HasDocument")]
    public void StartDragSymmetry(SymmetryAxisDirection dir)
    {
        if (ActiveDocument is null)
            return;
        ActiveDocument.EventInlet.OnSymmetryDragStarted(dir);
        ActiveDocument.Tools.UseSymmetry(dir);
    }

    [Command.Internal("PixiEditor.Document.EndDragSymmetry", CanExecute = "PixiEditor.HasDocument")]
    public void EndDragSymmetry(SymmetryAxisDirection dir)
    {
        if (ActiveDocument is null)
            return;
        ActiveDocument.EventInlet.OnSymmetryDragEnded(dir);
    }

    [Command.Basic("PixiEditor.Document.DeletePixels", "Delete pixels", "Delete selected pixels", CanExecute = "PixiEditor.Selection.IsNotEmpty", Key = Key.Delete, IconPath = "Tools/EraserImage.png")]
    public void DeletePixels()
    {
        Owner.DocumentManagerSubViewModel.ActiveDocument?.Operations.DeleteSelectedPixels();
    }


    [Command.Basic("PixiEditor.Document.ResizeDocument", false, "Resize Document", "Resize Document", CanExecute = "PixiEditor.HasDocument", Key = Key.I, Modifiers = ModifierKeys.Control | ModifierKeys.Shift)]
    [Command.Basic("PixiEditor.Document.ResizeCanvas", true, "Resize Canvas", "Resize Canvas", CanExecute = "PixiEditor.HasDocument", Key = Key.C, Modifiers = ModifierKeys.Control | ModifierKeys.Shift)]
    public void OpenResizePopup(bool canvas)
    {
        DocumentViewModel? doc = Owner.DocumentManagerSubViewModel.ActiveDocument;
        if (doc is null)
            return;

        ResizeDocumentDialog dialog = new ResizeDocumentDialog(
            doc.Width,
            doc.Height,
            canvas);
        if (dialog.ShowDialog())
        {
            if (canvas)
            {
                doc.Operations.ResizeCanvas(new(dialog.Width, dialog.Height), dialog.ResizeAnchor);
            }
            else
            {
                doc.Operations.ResizeImage(new(dialog.Width, dialog.Height), ResamplingMethod.NearestNeighbor);
            }
        }
    }

    [Command.Basic("PixiEditor.Document.CenterContent", "Center Content", "Center Content", CanExecute = "PixiEditor.HasDocument")]
    public void CenterContent()
    {
        if(ActiveDocument is null || ActiveDocument.SelectedStructureMember == null)
            return;
        
        List<Guid> layerGuids = new List<Guid>() { ActiveDocument.SelectedStructureMember.GuidValue };
        layerGuids.AddRange(ActiveDocument.SoftSelectedStructureMembers.Select(x => x.GuidValue));
        ActiveDocument.Operations.CenterContent(layerGuids);
    }
}
