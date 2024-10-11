using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Unity.Collections;
using UnityEngine.Rendering;

public class PLYMesh
{
    public struct Vertex
    {
        public Vector3 position;
        public Color32 color;
    }

    public static readonly VertexAttributeDescriptor[] vertexDescriptor = 
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
        new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 0)
    };

    public int vertexCount;
    public NativeArray<Vertex> vertices;

    public int indexCount;
    public NativeArray<uint> indices;
}

public class PLYFileReader
{
    const int bufferSize = 65536;

    enum DataProperty
    {
        Invalid,
        R8, G8, B8, A8,
        R16, G16, B16, A16,
        SingleX, SingleY, SingleZ,
        DoubleX, DoubleY, DoubleZ,
        Data8, Data16, Data32, Data64,
        Count
    }

    static readonly int[] dataPropertySize = new int[(int)DataProperty.Count]
    {
        -1,
        1, 1, 1, 1,
        2, 2, 2, 2,
        4, 4, 4,
        8, 8, 8,
        1, 2, 4, 8
    };

    class DataHeader
    {
        public DataProperty[] properties;
        public int vertexCount = -1;
        public int faceCount = -1;
        public bool binary = true;
        public bool isPosFloat = true;
        public bool isColorByte = true;
    }

    private DataHeader dataHeader;

    public PLYMesh Read(string filename)
    {
        if (!File.Exists(filename))
        {
            Debug.LogError("File doesn't exist " + filename);
            return null;
        }

        PLYMesh mesh = new PLYMesh();

        using FileStream fs = File.OpenRead(filename);
        using BufferedStream bs = new BufferedStream(fs, bufferSize);
        using StreamReader reader = new StreamReader(bs, System.Text.Encoding.ASCII);
        if (!this.ReadDataHeader(reader)) return mesh;
        
        mesh.vertexCount = dataHeader.vertexCount;
        if (dataHeader.binary)
        {
            this.ReadBodyBinary(new BinaryReader(bs), mesh);
        }
        else
        {
            this.ReadBodyAscii(reader, mesh);
        }

        return mesh;
    }


    private bool ReadDataHeader(StreamReader reader)
    {
        dataHeader = new DataHeader();
        int readCount = 0;

        // Magic number line ("ply")
        string line = reader.ReadLine();
        if (string.IsNullOrEmpty(line) || line != "ply")
        {
            Debug.LogError("PLY is empty or Magic number ('ply') mismatch." + line);
            return false;
        }

        readCount += line.Length + 1;
        line = reader.ReadLine();
        readCount += line.Length + 1;
        dataHeader.binary = line.Contains("binary");    

        List<DataProperty> properties = new List<DataProperty>();

        while ((line = reader.ReadLine()) != null)
        {
            readCount += line.Length + 1;
            if (line.Contains("end_header"))
            {
                break;
            }
                
            string[] col = line.Split();

            if (col[0] == "element")
            {
                if (col[1] == "vertex")
                {
                    dataHeader.vertexCount = Convert.ToInt32(col[2]);
                }
                else if (col[1] == "face")
                {
                    dataHeader.faceCount = Convert.ToInt32(col[2]);
                }
                else
                {
                    continue;
                }
            }

            if (col[0] == "property")
            {
                DataProperty prop = DataProperty.Invalid;

                switch (col[2])
                {
                    case "red":
                        prop = DataProperty.R8;//1
                        break;
                    case "green":
                        prop = DataProperty.G8;//2
                        break;
                    case "blue":
                        prop = DataProperty.B8;//3
                        break;
                    case "alpha":
                        prop = DataProperty.A8;//4
                        break;
                    case "x":
                        prop = DataProperty.SingleX;//9
                        break;
                    case "y":
                        prop = DataProperty.SingleY;//10
                        break;
                    case "z":
                        prop = DataProperty.SingleZ;//11
                        break;

                }

                switch (col[1])
                {
                    case "list":
                        Debug.Log("there is a list " + col[2] + " " + col[3] + " " + col[4]);
                        break;
                    case "char":
                    case "uchar":
                    case "int8":
                    case "uint8":
                        {
                            if (prop == DataProperty.Invalid)
                                prop = DataProperty.Data8;
                            else if (dataPropertySize[(int)prop] != 1)
                                throw new ArgumentException("Invalid property type ('" + line + "').");
                            break;
                        }
                    case "short":
                    case "ushort":
                    case "int16":
                    case "uint16":
                        {
                            switch (prop)
                            {
                                case DataProperty.Invalid:
                                    prop = DataProperty.Data16;
                                    break;
                                case DataProperty.R8:
                                    dataHeader.isColorByte = false;
                                    prop = DataProperty.R16;
                                    break;
                                case DataProperty.G8:
                                    dataHeader.isColorByte = false;
                                    prop = DataProperty.G16;
                                    break;
                                case DataProperty.B8:
                                    dataHeader.isColorByte = false;
                                    prop = DataProperty.B16;
                                    break;
                                case DataProperty.A8:
                                    dataHeader.isColorByte = false;
                                    prop = DataProperty.A16;
                                    break;
                            }
                            if (dataPropertySize[(int)prop] != 2)
                                throw new ArgumentException("Invalid property type ('" + line + "').");
                            break;
                        }
                    case "int":
                    case "uint":
                    case "float":
                    case "int32":
                    case "uint32":
                    case "float32":
                        {
                            if (prop == DataProperty.Invalid)
                                prop = DataProperty.Data32;
                            else if (dataPropertySize[(int)prop] != 4)
                                throw new ArgumentException("Invalid property type ('" + line + "').");
                            break;
                        }
                    case "int64":
                    case "uint64":
                    case "double":
                    case "float64":
                        {
                            switch (prop)
                            {
                                case DataProperty.Invalid:
                                    prop = DataProperty.Data64;
                                    break;
                                case DataProperty.SingleX:
                                    dataHeader.isPosFloat = false;
                                    prop = DataProperty.DoubleX;
                                    break;
                                case DataProperty.SingleY:
                                    dataHeader.isPosFloat = false;
                                    prop = DataProperty.DoubleY;
                                    break;
                                case DataProperty.SingleZ:
                                    dataHeader.isPosFloat = false;
                                    prop = DataProperty.DoubleZ;
                                    break;
                            }
                            if (dataPropertySize[(int)prop] != 8)
                                throw new ArgumentException("Invalid property type ('" + line + "').");
                            break;
                        }

                    default:
                        throw new ArgumentException("Unsupported property type ('" + line + "').");
                }

                properties.Add(prop);
            }
        }
        dataHeader.properties = properties.ToArray();
        reader.BaseStream.Position = readCount;

        return true;
    }

    private void ReadBodyBinary(BinaryReader br, PLYMesh mesh)
    {
        int colorOffset = 0;
        int vertexByteSize = 0;
        foreach (DataProperty dataProperty in dataHeader.properties)
        {
            if (dataProperty == DataProperty.R8 || dataProperty == DataProperty.R16)
            {
                colorOffset = vertexByteSize;
            }
            vertexByteSize += dataPropertySize[(int)dataProperty];
        }
        vertexByteSize = 15;

        ByteConverter offsets = new ByteConverter();
        
        Vector3 pos = new Vector3();
        Color32 col = new Color32();

        PLYMesh.Vertex vertex = new PLYMesh.Vertex();
        
        mesh.vertices = new NativeArray<PLYMesh.Vertex>(dataHeader.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        if (dataHeader.faceCount > 0)
        {
            mesh.indices = new NativeArray<uint>(dataHeader.faceCount * 3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        }
        for (int i = 0; i < dataHeader.vertexCount; i++)
        {
            offsets.Bytes = br.ReadBytes(vertexByteSize);
            if (dataHeader.isPosFloat)
            {
                pos.x = offsets.Floats[0];
                pos.z = offsets.Floats[1];
                pos.y = offsets.Floats[2];
            }
            else
            {
                pos.x = (float)offsets.Doubles[0];
                pos.z = (float)offsets.Doubles[1];
                pos.y = (float)offsets.Doubles[2];
            }

            if (dataHeader.isColorByte)
            {
                col.r = offsets.Bytes[colorOffset];
                col.g = offsets.Bytes[colorOffset + 1];
                col.b = offsets.Bytes[colorOffset + 2];
            }
            else
            {
                colorOffset = colorOffset / 2;
                col.r = (byte)(offsets.UShorts[colorOffset] >> 8);
                col.g = (byte)(offsets.UShorts[colorOffset + 1] >> 8);
                col.b = (byte)(offsets.UShorts[colorOffset + 2] >> 8);
            }

            vertex.position = pos;
            vertex.color = col;
            mesh.vertices[i] = vertex;
        }

        for (int i = 0; i < dataHeader.faceCount; i++)
        {
            byte faceSize = br.ReadByte();
            uint i1 = br.ReadUInt32();
            uint i2 = br.ReadUInt32();
            uint i3 = br.ReadUInt32();

            mesh.indices[i * 3] = i1;
            mesh.indices[i * 3 + 1] = i2;
            mesh.indices[i * 3 + 2] = i3;
        }
    }

    private void ReadBodyAscii(StreamReader reader, PLYMesh mesh)
    {
        Vector3 pos = new Vector3();
        Color32 col = new Color32();

        PLYMesh.Vertex vertex = new PLYMesh.Vertex();

        var invariantCulture = System.Globalization.CultureInfo.InvariantCulture;

        mesh.vertices = new NativeArray<PLYMesh.Vertex>(dataHeader.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        if (dataHeader.faceCount > 0)
        {
            mesh.indices = new NativeArray<uint>(dataHeader.faceCount * 3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        }

        for (int i = 0, max = dataHeader.vertexCount; i < max; i++)
        {
            string line = reader.ReadLine();
            string[] strings = line.Split(' ');
            if (strings.Length != 6)
            {
                Debug.LogError("Wrong vertex line " + line + "  " + " line number " + i + "  Length = " + strings.Length);

                foreach (var str in strings)
                {
                    Debug.Log(str);
                }
                return;
            }

            try
            {
                pos.x = float.Parse(strings[0], invariantCulture);
                pos.y = float.Parse(strings[1], invariantCulture);
                pos.z = float.Parse(strings[2], invariantCulture);
                col.r = byte.Parse(strings[3], invariantCulture);
                col.g = byte.Parse(strings[4], invariantCulture);
                col.b = byte.Parse(strings[5], invariantCulture);

                vertex.position = pos;
                vertex.color = col;
                mesh.vertices[i] = vertex;
            }
            catch (Exception e)
            {
                Debug.LogError("[ERROR] Line not well formated : " + line + " " + e.Message + "  number = " + i);
            }
        }
        for (int i = 0, max = dataHeader.faceCount; i < max; i++)
        {
            mesh.indices[i * 3] = 0;
            mesh.indices[i * 3 + 1] = 0;
            mesh.indices[i * 3 + 2] = 0;

            string line = reader.ReadLine();
            string[] strings = line.Split(' ');
            if (strings.Length != 4)
            {
                Debug.LogError("Wrong indices line " + line + "  " + " line number " + i + "  length = " + strings.Length);
                continue;
            }

            if (strings[0] != "3")
            {
                Debug.LogError("Wrong vertex line " + line + "  " + " line number " + i);
                return;
            }

            try
            {
                uint i1 = uint.Parse(strings[1], invariantCulture);
                uint i2 = uint.Parse(strings[2], invariantCulture);
                uint i3 = uint.Parse(strings[3], invariantCulture);

                mesh.indices[i * 3] = i1;
                mesh.indices[i * 3 + 1] = i2;
                mesh.indices[i * 3 + 2] = i3;
            }
            catch (Exception e)
            {
                Debug.LogError("[ERROR] Line not well formated : " + line + " " + e.Message);
            }
        }
    }
}


//ply
//format ascii 1.0
//comment author: Greg Turk
//comment object: another cube
//element vertex 8
//property float x
//property float y
//property float z
//property uchar red                   { start of vertex color }
//property uchar green
//property uchar blue
//element face 7
//property list uchar int vertex_index  { number of vertices for each face }
//element edge 5                        { five edges in object }
//property int vertex1 { index to first vertex of edge }
//property int vertex2 { index to second vertex }
//property uchar red                    { start of edge color }
//property uchar green
//property uchar blue
//end_header
//0 0 0 255 0 0                         { start of vertex list }
//0 0 1 255 0 0
//0 1 1 255 0 0
//0 1 0 255 0 0
//1 0 0 0 0 255
//1 0 1 0 0 255
//1 1 1 0 0 255
//1 1 0 0 0 255