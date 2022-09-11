﻿using System;
using PixiEditor.DrawingApi.Core.Bridge;
using PixiEditor.DrawingApi.Core.Surface.ImageData;
using SkiaSharp;

namespace PixiEditor.DrawingApi.Core.Surface;

public class Pixmap : NativeObject
{
    internal Pixmap(IntPtr objPtr) : base(objPtr)
    {
    }
    
    public Pixmap(ImageInfo imgInfo, IntPtr dataPtr) : base(dataPtr)
    {
        ObjectPointer = DrawingBackendApi.Current.PixmapImplementation.Construct(dataPtr, imgInfo);
    }

    public int Width { get; set; }
    public int Height { get; set; }

    public override void Dispose()
    {
        DrawingBackendApi.Current.PixmapImplementation.Dispose(ObjectPointer);
    }

    public IntPtr GetPixels()
    {
        return DrawingBackendApi.Current.PixmapImplementation.GetPixels(ObjectPointer);
    }

    public Span<T> GetPixelSpan<T>()
    {
        return DrawingBackendApi.Current.PixmapImplementation.GetPixelSpan<T>(this);
    }
}
