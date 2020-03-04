using System;
using System.Runtime.InteropServices;

// Set the Structure Layout to Sequential 
[StructLayout(LayoutKind.Sequential)]

// Create the Boxa Structure, using 32bit int and a pointer to the box
public struct Boxa
{
    public Int32 n;
    public Int32 nalloc;
    public Int32 refcount;
    public IntPtr box;
}