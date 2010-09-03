#region Copyright (c) 2006 Active Web Solutions Ltd
//
// TileServerDemo shows how to build a Virtual Earth compatible tile server
// (C) Copyright 2006 Active Web Solutions Ltd
//
// This program is free software; you can redistribute it and/or
// modify as long as you acknowledge the copyright holder.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
//
#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

/// <summary>
/// An HttpHandler which implements a Virtual Earth compatible 
/// tile server
/// </summary>
public class VeHttpHandler : IHttpHandler
{
    public const double EARTH_RADIUS = 6378137;
    public const double EARTH_CIRCUM = EARTH_RADIUS * 2.0 * Math.PI;
    public const double EARTH_HALF_CIRC = EARTH_CIRCUM / 2;

    public bool IsReusable
    {
        get { return (true); }
    }

    /// <summary>
    /// Retrieves a quad key from a Virtual Earth tile specifier URL
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public string GetQuadKey(string url)
    {
        Regex regex = new Regex(".*tiles/(.+)[.].*");
        Match match = regex.Match(url);

        return match.Groups[1].ToString().Substring(1);
    }

    /// <summary>
    /// Returns the bounding box for a grid square represented by
    /// the given quad key
    /// </summary>
    /// <param name="quadString"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="zoomLevel"></param>
    /// <returns></returns>
    public Box QuadKeyToBox(string quadString, int x, int y, int zoomLevel)
    {
        char c = quadString[0];

        int tileSize = 2 << (18 - zoomLevel - 1);

        if (c == '0')
        {
            y = y - tileSize;
        }

        else if (c == '1')
        {
            y = y - tileSize;
            x = x + tileSize;
        }

        else if (c == '3')
        {
            x = x + tileSize;
        }

        if (quadString.Length > 1)
        {
            return QuadKeyToBox(quadString.Substring(1), x, y, zoomLevel + 1);
        }
        else
        {
            return new Box(x, y, tileSize, tileSize);
        }
    }

    /// <summary>
    /// Converts radians to degrees
    /// </summary>
    /// <param name="d"></param>
    /// <returns></returns>
    public double RadToDeg(double d)
    {
        return d / Math.PI * 180.0;
    }

    /// <summary>
    /// Converts a grid row to Latitude
    /// </summary>
    /// <param name="y"></param>
    /// <param name="zoom"></param>
    /// <returns></returns>
    public double YToLatitudeAtZoom(int y, int zoom)
    {
        double arc = EARTH_CIRCUM / ((1 << zoom) * 256);
        double metersY = EARTH_HALF_CIRC - (y * arc);
        double a = Math.Exp(metersY * 2 / EARTH_RADIUS);
        double result = RadToDeg(Math.Asin((a - 1) / (a + 1)));
        return result;
    }

    /// <summary>
    /// Converts a grid column to Longitude
    /// </summary>
    /// <param name="x"></param>
    /// <param name="zoom"></param>
    /// <returns></returns>
    public double XToLongitudeAtZoom(int x, int zoom)
    {
        double arc = EARTH_CIRCUM / ((1 << zoom) * 256);
        double metersX = (x * arc) - EARTH_HALF_CIRC;
        double result = RadToDeg(metersX / EARTH_RADIUS);
        return result;
    }

    public void ProcessRequest(HttpContext context)
    {

        // Extract the QuadKey from the URL
        string urlString = context.Request.Url.ToString();
        string quadKey = GetQuadKey(urlString);

        int zoomLevel = quadKey.Length;

        // Use the quadkey to determine a bounding box for the requested tile
        int x = 0;
        int y = 262144;
        Box boundingBox = QuadKeyToBox(quadKey, x, y, 1);

        // Get the lat longs of the corners of the box
        double lon = XToLongitudeAtZoom(boundingBox.x * 256, 18);
        double lat = YToLatitudeAtZoom(boundingBox.y * 256, 18);

        double lon2 = XToLongitudeAtZoom((boundingBox.x + boundingBox.width) * 256, 18);
        double lat2 = YToLatitudeAtZoom((boundingBox.y - boundingBox.height) * 256, 18);

        // Create a new bitmap for the tile that we'll create
        Bitmap tileBitmap = new Bitmap(256, 256);

        Graphics graphics = Graphics.FromImage(tileBitmap);
        
        graphics.FillRectangle(Brushes.White, new Rectangle(0,0,256,256));
        graphics.DrawRectangle(Pens.Black, new Rectangle(0, 0, 256, 256));

        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        Font font = new Font("Arial", 10);
        Brush blackBrush = new SolidBrush(Color.Black);
        Brush redBrush = new SolidBrush(Color.Red);

        // Mark the North East and South West corner points
        string southWest = string.Format("{0} , {1}", lon, lat);
        string northEast = string.Format("{0} , {1}", lon2, lat2);
        graphics.DrawString(southWest, font, blackBrush, 10, 230);
        graphics.DrawString(northEast, font, blackBrush, 246 - graphics.MeasureString(northEast, font).Width, 10);

        // Write the quad key number in the centre of the tile
        graphics.DrawString(quadKey, font, redBrush, (256 - graphics.MeasureString(quadKey, font).Width) / 2, 128);

        graphics.Dispose();

        // Stream the image out in response to the HTTP request
        MemoryStream streamImage = new MemoryStream();
        tileBitmap.Save(streamImage, ImageFormat.Png);
        context.Response.Clear();
        context.Response.ContentType = "image/png";
        context.Response.AddHeader("content-length",
            System.Convert.ToString(streamImage.Length));
        context.Response.BinaryWrite(streamImage.ToArray());

        // Clean up
        tileBitmap.Dispose();
        streamImage.Dispose();
        font.Dispose();
        redBrush.Dispose();
        blackBrush.Dispose();
    }


}
