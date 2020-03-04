using System;
using System.Runtime.InteropServices;

// Set the Structure Layout to Sequential 
[StructLayout(LayoutKind.Sequential)]

// Create the Box Structure, using 32bit int
public struct Box
{
    public Int32 x;
    public Int32 y;
    public Int32 w;
    public Int32 h;
    public Int32 refcount;
}