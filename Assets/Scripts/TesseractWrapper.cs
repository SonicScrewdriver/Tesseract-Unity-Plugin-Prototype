using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class TesseractWrapper
{
    // Set the parameters 
    IntPtr _tessHandle;
    private string _errorMsg;
    private Texture2D _highlightedTexture;
    private const float MinimumConfidence = 60;

    // Set the correct DLL file to load, depending on platform
#if UNITY_EDITOR
    private const string TesseractDllName = "tesseract";
    private const string LeptonicaDllName = "tesseract";
#elif UNITY_ANDROID
    private const string TesseractDllName = "libtesseract.so";
    private const string LeptonicaDllName = "liblept.so";
#else
    private const string TesseractDllName = "tesseract";
    private const string LeptonicaDllName = "tesseract";
#endif

    // Start importing methods from the specified Dll file

    // Version of Tesseract
    [DllImport(TesseractDllName)]
    private static extern IntPtr TessVersion();

    // Create Base API 
    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPICreate();

    // This is Init3 so that it uses the 3rd implementation of the method
    // Theres no overloading when exposing an implementation from a DLL
    [DllImport(TesseractDllName)]
    private static extern int TessBaseAPIInit3(IntPtr handle, string dataPath, string language);

    // Delete Base API in case of error / shutdown
    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPIDelete(IntPtr handle);

    // End API in case of error / shutdown
    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPIEnd(IntPtr handle);

    // Set the image that needs to be recognized
    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPISetImage(IntPtr handle, IntPtr
             imagedata, int width, int height,
             int bytes_per_pixel, int bytes_per_line);

    // Set the image that needs to be recognized ELECTRIC BUGALOO
    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPISetImage2(IntPtr handle,
                 IntPtr pix);

    // Recognize the image that was set 
    [DllImport(TesseractDllName)]
    private static extern int TessBaseAPIRecognize(IntPtr handle, IntPtr
                 monitor);

    // Get the recognized text in utf-8 format
    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPIGetUTF8Text(IntPtr handle);

    // Delete the string pointer produced by the above method
    [DllImport(TesseractDllName)]
    private static extern void TessDeleteText(IntPtr text);

    // Clear the APIs
    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPIClear(IntPtr handle);

    // Get the words Tesseract identified, so we can locate them
    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPIGetWords(IntPtr handle, IntPtr pixa);

    // Get the Tesseract Word Confidence levels
    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPIAllWordConfidences(IntPtr handle);


    public TesseractWrapper()
    {
        _tessHandle = IntPtr.Zero;
    }

    // Return the current version to ensure Tesseract is set up correctly 
    public string Version()
    {
        IntPtr strPtr = TessVersion();
        string tessVersion = Marshal.PtrToStringAnsi(strPtr);
        return tessVersion;
    }

    // Return any error messages
    public string GetErrorMessage()
    {
        return _errorMsg;
    }

    // Initialise Tesseract with the specified parameters. 
    // Return false if failed.
    public bool Init(string lang, string dataPath)
    {
        if (!_tessHandle.Equals(IntPtr.Zero))
            Close();

        try
        {
            _tessHandle = TessBaseAPICreate();
            // If the init failed (null returned)
            if (_tessHandle.Equals(IntPtr.Zero))
            {
                _errorMsg = "TessAPICreate failed";
                return false;
            }
            // If the datapath returns null
            if (string.IsNullOrWhiteSpace(dataPath))
            {
                _errorMsg = "Invalid DataPath";
                return false;
            }

            int init = TessBaseAPIInit3(_tessHandle, dataPath,
                    lang);
            if (init != 0)
            {
                // Run close method and display the error message from Tesseract
                Close();
                _errorMsg = "TessAPIInit failed. Output: " + init;
                return false;
            }
        }
        // Catch any exceptions and return the error message
        catch (Exception ex)
        {
            _errorMsg = ex + " -- " + ex.Message;
            return false;
        }

        return true;
    }

    // If Tesseract failed to init, delete our previous attempt.
    public void Close()
    {
        if (_tessHandle.Equals(IntPtr.Zero))
            return;
        TessBaseAPIEnd(_tessHandle);
        TessBaseAPIDelete(_tessHandle);
        _tessHandle = IntPtr.Zero;
    }

    // Recognize the text by feeding it a Texture2D , return a string
    public string Recognize(Texture2D texture)
    {
        // Guard in case Tesseract isn't initialized
        if (_tessHandle.Equals(IntPtr.Zero))
            return null;

        // Set a property to store the texture
        _highlightedTexture = texture;

        // Determine the bytes data
        // First get the texture's dimensions
        int width = _highlightedTexture.width;
        int height = _highlightedTexture.height;

        //Get the color scape
        // The image texture must have read/write enabled otherwise this function will fail
        Color32[] colors = _highlightedTexture.GetPixels32();

        // Count the total pixels
        int count = width * height;

        //Determine the number of bytes by multiplying the total pixels by the bytes per pixel
        // Four bytes as we're using RGBA (one byte for each element in the byte array
        int bytesPerPixel = 4;
        byte[] dataBytes = new byte[count * bytesPerPixel];

        // Byte Pointer
        int bytePtr = 0;

        // Set up the Byte Stream/Array from the PixelArray
        // For each pixel of Y (height) while it's greater than 0
        for (int y = height - 1; y >= 0; y--)
        {
            // At each Y value, For each pixel of x (width) while X is smaller than the total width
            // Scanning top down like a security guard using a metal detector
            for (int x = 0; x < width; x++)
            {
                // ColorIndex equals current height times total width plus the current width
                int colorIdx = y * width + x;

                // Comprehension of where the data is being placed:
                // dataBytes[0] = First Pixel (Top left), Red Channel 
                // dataBytes[1] = First Pixel (Top left), Green Channel 
                // dataBytes[2] = Second Pixel (Top Left, one pixel right), Red Channel
                dataBytes[bytePtr++] = colors[colorIdx].r;
                dataBytes[bytePtr++] = colors[colorIdx].g;
                dataBytes[bytePtr++] = colors[colorIdx].b;
                dataBytes[bytePtr++] = colors[colorIdx].a;
            }
        }

        // Method Research:
        // IntPtr is pointer for an integer who's size is platform-specific (32 or 64 bit) 
        // The Marshal class provides a collection of methods for allocating and manipulating unmanaged memory

        // AllocHGlobal allocates memory from the unmanaged memory of the process, equal to the size of the image by the number of bytes per pixel
        IntPtr imagePtr = Marshal.AllocHGlobal(count * bytesPerPixel);

        //Copies data from the datastream we set up earlier to our new unmanaged memory pointer
        // We're using overload 16: Copy(Byte[], Int32, IntPtr, Int32): Copies data from a one-dimensional, managed 8-bit unsigned integer array to an unmanaged memory pointer.
        Marshal.Copy(dataBytes, 0, imagePtr, count * bytesPerPixel);

        //  Passing a pointer to the memory of the byte array as a parameter of SetImage
        TessBaseAPISetImage(_tessHandle, imagePtr, width, height,
                        bytesPerPixel, width * bytesPerPixel);

        // See if Tesseract has recognized the image
        if (TessBaseAPIRecognize(_tessHandle, IntPtr.Zero) != 0)
        {
            // If Tesseract has recognized the image, free the memory used for the image data
            Marshal.FreeHGlobal(imagePtr);
            return null;
        }

        // Determine the confidence level for each word
        IntPtr confidencesPointer = TessBaseAPIAllWordConfidences(_tessHandle);
        int i = 0;

        // Create a list of the confidence at each byte
        // Note the AllWordConfidences returns a pointer to the 1st element of an Integer32 array ending with -1, 
        // so you need to loop through until you get -1
        List<int> confidence = new List<int>();

        while (true)
        {
            int tempConfidence = Marshal.ReadInt32(confidencesPointer, i * 4);

            if (tempConfidence == -1) break;

            i++;
            confidence.Add(tempConfidence);
        }

        // -------- Determine Boxes for Highlights 
        // Get the byte size of the pointer
        int pointerSize = Marshal.SizeOf(typeof(IntPtr));

        // Get the words from Tesseract
        IntPtr intPtr = TessBaseAPIGetWords(_tessHandle, IntPtr.Zero);

        // Put the pointer data into the Boxa structure
        Boxa boxa = Marshal.PtrToStructure<Boxa>(intPtr);

        // Determine the boxes 
        Box[] boxes = new Box[boxa.n];

        // For Each of the boxes, set it to the read value of the box at the given offset and make it a box structure
        for (int index = 0; index < boxes.Length; index++)
        {
            // If the confidence of the word meets the minimum index
            if (confidence[index] >= MinimumConfidence)
            {
                IntPtr boxPtr = Marshal.ReadIntPtr(boxa.box,
                                           index * pointerSize);

                boxes[index] = Marshal.PtrToStructure<Box>(boxPtr);
                Box box = boxes[index];

                // draw lines around the box / word
                DrawLines(texture,
                     new Rect(box.x, texture.height - box.y - box.h, box.w, box.h),
                     Color.magenta);
            }
        }
        // --------- End Highlight Section

        // Create a new pointer for the string data and assign it to the result of the method to return UTF8 data.  
        IntPtr str_ptr = TessBaseAPIGetUTF8Text(_tessHandle);

        // Free the memory used for the image data
        Marshal.FreeHGlobal(imagePtr);

        // If the string returns null/empty, return null 
        if (str_ptr.Equals(IntPtr.Zero))
            return null;
        //IF we're in windows, convert to an ansi string 
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string recognizedText = Marshal.PtrToStringAnsi(str_ptr);
        // Else, convert auto 
#else
    string recognizedText = Marshal.PtrToStringAuto(str_ptr);
#endif
        // Clear Tesseract
        TessBaseAPIClear(_tessHandle);

        // Clear the text from Tesseract
        TessDeleteText(str_ptr);

        // Filter out the words from the text that are too low in confidence
        string[] words = recognizedText.Split(new[] { ' ', '\n' },
              StringSplitOptions.RemoveEmptyEntries);
        StringBuilder result = new StringBuilder();

        for (i = 0; i < boxes.Length; i++)
        {
            Debug.Log(words[i] + " -> " + confidence[i]);
            if (confidence[i] >= MinimumConfidence)
            {
                result.Append(words[i]);
                result.Append(" ");
            }
        }

        // Return the filtered words
        return result.ToString();
    }
    private void DrawLines(Texture2D texture, Rect boundingRect, Color
               color, int thickness = 3)
    {
        int x1 = (int)boundingRect.x;
        int x2 = (int)(boundingRect.x + boundingRect.width);
        int y1 = (int)boundingRect.y;
        int y2 = (int)(boundingRect.y + boundingRect.height);

        for (int x = x1; x <= x2; x++)
        {
            for (int i = 0; i < thickness; i++)
            {
                texture.SetPixel(x, y1 + i, color);
                texture.SetPixel(x, y2 - i, color);
            }
        }

        for (int y = y1; y <= y2; y++)
        {
            for (int i = 0; i < thickness; i++)
            {
                texture.SetPixel(x1 + i, y, color);
                texture.SetPixel(x2 - i, y, color);
            }
        }

        texture.Apply();
    }
  

    public Texture2D GetHighlightedTexture()
    {
        return _highlightedTexture;
    }
}