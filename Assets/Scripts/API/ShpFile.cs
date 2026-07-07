using UnityEngine; // NOT NECESSARY but for testing was useful
using System;
using System.IO;
using MechCommanderUnity.API;

/// <summary>
/// This is the only file in the API that was modified
/// This originally had errors due to the header length not being considered on Read when mechs were loaded
/// </summary>

public class ShpFile : IDisposable
{
    #region Class Variables

    /// <summary>
    /// File header.
    /// </summary>
    private ShpFileHeader header;
    private ShpFileHeader[] headers;

    /// <summary>
    /// Managed file.
    /// </summary>
    internal FileProxy managedFile = new FileProxy();

    /// <summary>
    /// The List image data for this SHP.
    /// </summary>
    private MCBitmap[] imgRecord;

    private int Numfiles;
    private int XStart = 1000, YStart = 1000, XEnd = -1000, YEnd = -1000;
    private int headerLength = 0;
    #endregion

    #region Class Structures

    internal struct ShpFileHeader
    {
        public short Type;
        public short NumFiles;

        public short Height;
        public short Width;

        public short XCenter;
        public short YCenter;

        public short XStart;
        public short YStart;

        public short XEnd;
        public short YEnd;

        public int FrameCount;
        public long DataPosition;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ShpFile()
    {
    }

    /// <summary>
    /// Load constructor
    /// Some IMG files contain palette information, this will overwrite the specified palette.
    /// </summary>
    /// <param name="filePath">Absolute path to *.IMG file.</param>
    public ShpFile(string filePath)
    {
        imgRecord = new MCBitmap[0];
        Load(filePath, FileUsage.UseMemory, true);
    }

    /// <summary>
    /// Load constructor
    /// Some IMG files contain palette information, this will overwrite the specified palette.
    /// </summary>
    /// <param name="data">Byte data of file</param>
    public ShpFile(byte[] data)
    {
        imgRecord = new MCBitmap[0];
        Load(data, FileUsage.UseMemory, true);
    }

    /// <summary>
    /// Load constructor.
    /// </summary>
    /// <param name="filePath">Absolute path to *.IMG file.</param>
    /// <param name="usage">Specify if file will be accessed from disk, or loaded into RAM.</param>
    /// <param name="readOnly">File will be read-only if true, read-write if false.</param>
    public ShpFile(string filePath, FileUsage usage, bool readOnly)
    {
        imgRecord = new MCBitmap[0];

        Load(filePath, usage, readOnly);
    }

    /// <summary>
    /// Load constructor
    /// Some IMG files contain palette information, this will overwrite the specified palette.
    /// </summary>
    /// <param name="data">Byte data of file</param>
    /// <param name="usage">Specify if file will be accessed from disk, or loaded into RAM.</param>
    /// <param name="readOnly">File will be read-only if true, read-write if false.</param>
    public ShpFile(byte[] data, FileUsage usage, bool readOnly)
    {
        imgRecord = new MCBitmap[0];
        Load(data, usage, readOnly);
    }


    #endregion

    #region Public Properties

    public int ImgLength
    {
        get { return imgRecord.Length; }

    }

    public short HeaderStartX
    {
        get { return header.XStart; }
    }

    public short HeaderStartY
    {
        get { return header.YStart; }
    }


    #endregion

    #region Public Methods

    /// <summary>
    /// Loads an IMG file.
    /// </summary>
    /// <param name="filePath">Absolute path to *.IMG file</param>
    /// <param name="usage">Specify if file will be accessed from disk, or loaded into RAM.</param>
    /// <param name="readOnly">File will be read-only if true, read-write if false.</param>
    /// <returns>True if successful, otherwise false.</returns>
    public bool Load(string filePath, FileUsage usage, bool readOnly)
    {
        // Exit if this file already loaded
        if (managedFile.FilePath == filePath)
            return true;

        // Load file
        if (!managedFile.Load(filePath, usage, readOnly))
            return false;

        // Read file
        if (!Read())
            return false;

        return true;
    }

    /// <summary>
    /// Loads an IMG file.
    /// </summary>
    /// <param name="filePath">Absolute path to *.IMG file</param>
    /// <param name="usage">Specify if file will be accessed from disk, or loaded into RAM.</param>
    /// <param name="readOnly">File will be read-only if true, read-write if false.</param>
    /// <returns>True if successful, otherwise false.</returns>
    public bool Load(byte[] data, FileUsage usage, bool readOnly)
    {
        // managedFile = new FileProxy(data, "");

        /*// Exit if this file already loaded
         if (managedFile.FilePath == filePath)
             return true;*/

        // Load file
        if (!managedFile.Load(data, ""))
            return false;

        // Read file
        if (!Read())
            return false;

        return true;
    }

    public MCBitmap GetBitMap(int index)
    {
        if (index < imgRecord.Length)
        {
            return imgRecord[index];
        }
        else
        {
            return new MCBitmap();
        }
    }
    public MCBitmap[] GetBitMaps()
    {
        return imgRecord;
    }

    public short GetHeaderStartX(int index)
    {
        if (index < headers.Length)
        {
            return headers[index].XStart;
        }
        else
        {
            return 0;
        }
    }

    public short GetHeaderStartY(int index)
    {
        if (index < headers.Length)
        {
            return headers[index].YStart;
        }
        else
        {
            return 0;
        }
    }
    #endregion

    #region Private Methods



    #endregion

    #region Readers

    /// <summary>
    /// Read file.
    /// </summary>
    /// <returns>True if succeeded, otherwise false.</returns>
    private bool Read()
    {
        //try
        //{
        // Step through file
        BinaryReader Reader = managedFile.GetReader();

        if (!ReadHeader(Reader))
            return false;

        Numfiles = Reader.ReadInt32();

        var DataOffsets = new int[Numfiles];

        for (int row = 0; row < Numfiles; row++)
        {
            var offset = Reader.ReadInt32();
            Reader.BaseStream.Position += 4;
            DataOffsets[row] = offset + headerLength;
        }
        // DataOffsets[Numfiles] = (int)Reader.BaseStream.Length;

        imgRecord = new MCBitmap[Numfiles];
        headers = new ShpFileHeader[Numfiles];

        //ReadMinHeightWidth(Reader, DataOffsets);

        for (int row = 0; row < Numfiles; row++)
        {
            if (DataOffsets[row] == 0)
            {
                InitEmptyBmp(row);
                continue;
            }

            Reader.BaseStream.Position = DataOffsets[row];
            //Debug.Log("reader position before header " + Reader.BaseStream.Position);
            if (!ReadShpHeader(Reader))
                return false;

            headers[row].XStart = header.XStart;
            headers[row].YStart = header.YStart;

            //var bmp = new MCBitmap();
            MCBitmap bmp;
            if (!ReadShpData(Reader, out bmp))
            {
                InitEmptyBmp(row);
                Debug.Log("Error Reader Header");
                continue;
            }


            bmp.Name = row.ToString();

            imgRecord[row] = bmp;

        }

        //} catch (Exception e)
        //{
        //    UnityEngine.Debug.Log(e.Message);
        //    managedFile.Close();
        //    return false;
        //}

        managedFile.Close();

        return true;
    }

    /// <summary>
    /// Reads file header.
    /// </summary>
    /// <param name="reader">Source reader.</param>
    private bool ReadShpHeader(BinaryReader reader)
    {
        try
        {
            header.Height = reader.ReadInt16();
            header.Width = reader.ReadInt16();


            header.YCenter = reader.ReadInt16();
            header.XCenter = reader.ReadInt16();

            header.XStart = (short)reader.ReadInt32();
            header.YStart = (short)reader.ReadInt32();

            header.XEnd = (short)reader.ReadInt32();
            header.YEnd = (short)reader.ReadInt32();

            //Debug.Log("header w x h " + header.Width + " x " + header.Height);
            //Debug.Log("header xst " + header.XStart + " xen " + header.XEnd + " yst " + header.YStart + " yen " + header.YEnd);
        }
        catch (Exception)
        {

            return false;
        }
        // Read SHP header data


        return true;
    }

    /// <summary>
    /// Reads file header.
    /// </summary>
    /// <param name="reader">Source reader.</param>
    private bool ReadHeader(BinaryReader reader)
    {
        // Start header
        reader.BaseStream.Position = 0;
        headerLength = 0;
        if (reader.ReadInt32() != 808529457)
        {
            reader.BaseStream.Position = 6;
            headerLength = 6;
            if (reader.ReadInt32() != 808529457)
            {
                return false;
            }
        }

        return true;


    }

    /// <summary>
    /// Reads image data.
    /// </summary>
    private bool ReadShpData(BinaryReader Reader, out MCBitmap bmp)
    {
        //bmp = new MCBitmap(Mathf.Abs(XEnd - XStart), Mathf.Abs(YEnd - YStart));
        //Debug.Log("header w " + (header.XEnd - header.XStart + 1) + " " + " header h " + (header.YEnd - header.YStart + 1));
        bmp = new MCBitmap(Mathf.Abs(header.XEnd - header.XStart) + 1, Mathf.Abs(header.YEnd - header.YStart) + 1);

        //Debug.Log("bmp w " + bmp.Width + " h " + bmp.Height);///TESTESTESTS BOGS
        //bmp.PivotX = Math.Abs(XStart);
        //bmp.PivotY = Math.Abs(YStart);
        bmp.PivotX = header.XCenter;
        bmp.PivotY = header.YCenter;

        var BmpData = bmp.Data;

        byte instructionByte, colorByte, skipByte;
        int paintIterations, remainder, i, currentLine = 0, finalLine = currentLine + Mathf.Abs(header.YEnd - header.YStart);

        //var linestart = XStart < 0 ? XStart * -1 + header.XStart : XStart + header.XStart;
        var linestart = 0;
        var x = linestart;
        //if (YStart > 0 && header.YStart > 0)
        /*if (header.YStart > 0)
        {
            currentLine = Math.Abs(bmp.PivotY - header.YStart);
        }
        else
        {
            currentLine = Math.Abs(bmp.PivotY + (header.YStart > 0 ? header.YStart * -1 : header.YStart));
        }

        finalLine = currentLine + (header.YEnd - header.YStart);

        if (currentLine < 0) currentLine = 0;
        */

        while (currentLine <= finalLine)
        {
            // currently the raven has this problem
            if (Reader.BaseStream.Position >= Reader.BaseStream.Length)
			{
                Debug.Log("end reached and instruction still being read");
                break;
			}
            instructionByte = Reader.ReadByte();
            remainder = instructionByte % 2;
            paintIterations = (int)(instructionByte / 2f);

            if (paintIterations == 0 && remainder == 1) // a skip over num pixels in next byte
            {
                skipByte = Reader.ReadByte();
                x += skipByte;
            }
            else if (paintIterations == 0)   // end of line
            {
                currentLine++;
                x = linestart;
            }
            else if (remainder == 0) // a run of bytes
            {
                colorByte = Reader.ReadByte();// ShpData[buffindex]; buffindex++; // the color #
                for (i = 0; i < paintIterations; ++i)
                {
                    // currently the vulture has this problem
                    if ((currentLine * bmp.Width) + x >= BmpData.Length)
					{
                        Debug.Log("oob on color print loop " + ((currentLine * bmp.Width) + x) + " loop at " + i);
                        break;
					}
                    BmpData[(currentLine * bmp.Width) + x] = colorByte;
                    x++;
                }
            }
            else // paintIterations!0 and remainder==1 ... read the next paintIteration bytes as color #'s
            {
                // currently the vulture has this problem
                if ((currentLine * bmp.Width) + x >= BmpData.Length)
                {
                    Debug.Log("oob on color print array copy " + (currentLine * bmp.Width) + x);
                    break;
                }
                var ShpData = Reader.ReadBytes(paintIterations);
                Array.Copy(ShpData, 0, BmpData, (currentLine * bmp.Width) + x, paintIterations);
                x += paintIterations;
            }
        }
        bmp.Data = BmpData;

        return true;
    }


    private bool ReadMinHeightWidth(BinaryReader Reader, int[] DataOffsets) // to try and size the bitmap for the smallest sprite?
    {

        for (int i = 0; i < DataOffsets.Length; i++)
        {
            if (DataOffsets[i] == 0)
                continue;
            if (DataOffsets[i] + 8 > Reader.BaseStream.Length)
                continue;

            Reader.BaseStream.Position = DataOffsets[i] + 8;
            var xst = (short)Reader.ReadInt32();
            if (xst < XStart)
                XStart = xst;

            var yst = (short)Reader.ReadInt32();
            if (yst < YStart)
                YStart = yst;

            var xen = (short)Reader.ReadInt32();
            if (xen > XEnd)
                XEnd = xen;

            var yen = (short)Reader.ReadInt32();
            if (yen > YEnd)
                YEnd = yen;

        }

        XStart--;
        YStart--;
        XEnd++;
        YEnd++;
        //UnityEngine.Debug.Log(string.Format("Min Size: {0},{1},{2},{3}", XStart, YStart, XEnd, YEnd));

        return true;
    }

    private void InitEmptyBmp(int row)
    {
        var bmpEmpty = new MCBitmap(XEnd - XStart, YEnd - YStart);

        bmpEmpty.PivotX = XStart < 0 ? XStart * -1 : XStart;
        bmpEmpty.PivotY = YStart < 0 ? YStart * -1 : YStart;

        bmpEmpty.Data.Fill((byte)0);
        bmpEmpty.Name = row.ToString();

        imgRecord[row] = bmpEmpty;
    }

    #endregion

    #region Private Methods
    public void Dispose()
    {
        for (int i = 0; i < imgRecord.Length; i++)
        {
            if (imgRecord[i] != null)
                imgRecord[i].Dispose();
        }
        managedFile.Close();
    }
    #endregion
}
