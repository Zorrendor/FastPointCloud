using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PLYFileWritter
{
    const int bufferSize = 65536;
    //const int bufferSize = 1024 * 1024;

    const string headerTop = "ply\nformat binary_little_endian 1.0\ncomment author: Point Cloud\ncomment object: another cube\nelement vertex ";
    const string headerBottom = "\nproperty float x\nproperty float y\nproperty float z\nproperty uchar red\nproperty uchar green\nproperty uchar blue\nend_header\n";

    public void Write(PLYMesh pointCloud, string filename)
    {
        const int vertexByteSize = sizeof(float) * 3 + sizeof(byte) * 3;
        ByteConverter offsets = new ByteConverter();
        offsets.Bytes = new byte[vertexByteSize];

        using (FileStream fs = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
        using (BufferedStream bs = new BufferedStream(fs, bufferSize))
        using (BinaryWriter bw = new BinaryWriter(bs))
        {
            bw.Write((headerTop + pointCloud.vertexCount.ToString() + headerBottom).ToCharArray());
            for (int i = 0; i < pointCloud.vertexCount; i++)
            {
                PLYMesh.Vertex ver = pointCloud.vertices[i];
                offsets.Floats[0] = ver.position.x;
                offsets.Floats[1] = ver.position.z;
                offsets.Floats[2] = ver.position.y;
                offsets.Bytes[12] = ver.color.r;
                offsets.Bytes[13] = ver.color.g;
                offsets.Bytes[14] = ver.color.b;
                bw.Write(offsets.Bytes, 0, vertexByteSize);
                //bw.Write(ver.position.x);
                //bw.Write(ver.position.z);
                //bw.Write(ver.position.y);
                //bw.Write(ver.color.r);
                //bw.Write(ver.color.g);
                //bw.Write(ver.color.b);
            }
        }
        Debug.Log("Save to file " + filename);
    }
}
