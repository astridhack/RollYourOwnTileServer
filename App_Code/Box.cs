using System;
using System.Collections.Generic;
using System.Text;


/// <summary>
/// A box describing the bounds of a virtual earth map tile
/// </summary>
public class Box
{
    public int x;
    public int y;
    public int width;
    public int height;

    public Box(int x, int y, int width, int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }
}

