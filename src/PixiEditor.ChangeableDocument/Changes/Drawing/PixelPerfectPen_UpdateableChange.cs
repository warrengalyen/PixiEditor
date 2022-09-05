﻿using ChunkyImageLib.Operations;
using PixiEditor.DrawingApi.Core.ColorsImpl;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.DrawingApi.Core.Surface;
using SkiaSharp;

namespace PixiEditor.ChangeableDocument.Changes.Drawing;

internal class PixelPerfectPen_UpdateableChange : UpdateableChange
{
    private readonly Color color;
    private readonly bool drawOnMask;
    private readonly Guid memberGuid;
    private readonly HashSet<VecI> confirmedPixels = new();
    private HashSet<VecI> pixelsToConfirm = new();
    private HashSet<VecI> pixelsToConfirm2 = new();
    private List<VecI>? incomingPoints = new();
    private CommittedChunkStorage? chunkStorage;

    [GenerateUpdateableChangeActions]
    public PixelPerfectPen_UpdateableChange(Guid memberGuid, VecI pos, Color color, bool drawOnMask)
    {
        this.memberGuid = memberGuid;
        this.color = color;
        this.drawOnMask = drawOnMask;
    }

    [UpdateChangeMethod]
    public void Update(VecI pos)
    {
        incomingPoints!.Add(pos);
    }

    public override OneOf<Success, Error> InitializeAndValidate(Document target)
    {
        if (!DrawingChangeHelper.IsValidForDrawing(target, memberGuid, drawOnMask))
            return new Error();
        var image = DrawingChangeHelper.GetTargetImageOrThrow(target, memberGuid, drawOnMask);
        image.SetBlendMode(BlendMode.SrcOver);
        DrawingChangeHelper.ApplyClipsSymmetriesEtc(target, image, memberGuid, drawOnMask);
        return new Success();
    }

    private bool IsLShape(int lastPixelIndex)
    {
        if (lastPixelIndex < 3)
            return false;
        VecI first = incomingPoints![lastPixelIndex - 2];
        VecI second = incomingPoints[lastPixelIndex - 1];
        VecI third = incomingPoints[lastPixelIndex];
        return first.X != third.X && first.Y != third.Y && (second - first).TaxicabLength == 1 && (second - third).TaxicabLength == 1;
    }

    private void DoDrawingIteration(ChunkyImage image, int pointsCount)
    {
        if (pointsCount == 1)
        {
            image.EnqueueDrawPixel(incomingPoints![0], color, BlendMode.Src);
            confirmedPixels.Add(incomingPoints[0]);
            return;
        }

        if (incomingPoints![^1] == incomingPoints[^2])
        {
            incomingPoints.RemoveAt(incomingPoints.Count - 1);
            return;
        }


        confirmedPixels.UnionWith(pixelsToConfirm2);
        (pixelsToConfirm2, pixelsToConfirm) = (pixelsToConfirm, pixelsToConfirm2);
        pixelsToConfirm.Clear();

        Point[] line = BresenhamLineHelper.GetBresenhamLine(incomingPoints[pointsCount - 2], incomingPoints[pointsCount - 1]);
        foreach (VecI pixel in line)
        {
            pixelsToConfirm.Add(pixel);
        }
        image.EnqueueDrawPixels(line.Select(point => (VecI)point), color, BlendMode.Src);

        if (pointsCount >= 3 && IsLShape(pointsCount - 1) && !confirmedPixels.Contains(incomingPoints[pointsCount - 2]))
        {
            VecI pixelToErase = incomingPoints[pointsCount - 2];
            image.EnqueueDrawPixel(pixelToErase, Colors.Transparent, BlendMode.Src);
            pixelsToConfirm.Remove(pixelToErase);
            pixelsToConfirm2.Remove(pixelToErase);
            incomingPoints.RemoveAt(pointsCount - 2);
        }
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> ApplyTemporarily(Document target)
    {
        ChunkyImage image = DrawingChangeHelper.GetTargetImageOrThrow(target, memberGuid, drawOnMask);

        int changeCount = image.QueueLength;
        DoDrawingIteration(image, incomingPoints!.Count);
        HashSet<VecI> affChunks = image.FindAffectedChunks(changeCount);
        return DrawingChangeHelper.CreateChunkChangeInfo(memberGuid, affChunks, drawOnMask);
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Apply(Document target, bool firstApply, out bool ignoreInUndo)
    {
        if (chunkStorage is not null)
            throw new InvalidOperationException("Trying to save chunks while a saved one already exist");

        ignoreInUndo = false;
        var image = DrawingChangeHelper.GetTargetImageOrThrow(target, memberGuid, drawOnMask);
        if (firstApply)
        {
            incomingPoints = null;
            confirmedPixels.UnionWith(pixelsToConfirm);
            confirmedPixels.UnionWith(pixelsToConfirm2);
        }
        else
        {
            image.SetBlendMode(BlendMode.SrcOver);
            DrawingChangeHelper.ApplyClipsSymmetriesEtc(target, image, memberGuid, drawOnMask);
            image.EnqueueDrawPixels(confirmedPixels, color, BlendMode.Src);
        }

        var affChunks = image.FindAffectedChunks();
        chunkStorage = new CommittedChunkStorage(image, affChunks);
        image.CommitChanges();
        return DrawingChangeHelper.CreateChunkChangeInfo(memberGuid, affChunks, drawOnMask);
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Revert(Document target)
    {
        var chunks = DrawingChangeHelper.ApplyStoredChunksDisposeAndSetToNull(target, memberGuid, drawOnMask, ref chunkStorage);
        return DrawingChangeHelper.CreateChunkChangeInfo(memberGuid, chunks, drawOnMask);
    }
}
