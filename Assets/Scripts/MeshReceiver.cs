using System;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

public class MeshReceiver : MonoBehaviour
{
    [DllImport("MeshReceiver")]
    static extern int openConn(string host, ushort port);
    [DllImport("MeshReceiver")]
    static extern void closeConn(int fd);
    [DllImport("MeshReceiver")]
    static extern int readNF(int fd);
    [DllImport("MeshReceiver")]
    static extern int readMeshTexture(int fd, int nF, int tWidth, int tHeight, IntPtr vertexBuf, IntPtr indexBuf, IntPtr textureBuf);
    [DllImport("MeshReceiver")]
    static extern IntPtr allocTextureBuf(int tWidth, int tHeight);

    struct VertexData
    {
        public Vector3 position;
        public Vector2 uv;
    }

    public int TextureWidth = 1920 * 4;
    public int TextureHeight = 1080;
    public string Host = "192.168.3.199";
    public ushort Port = 33669;

    Mesh mesh;
    Texture2D texture;

    // TcpClient client;
    // NetworkStream ns;
    int fd;

    class MeshFrame
    {
        public Mesh.MeshDataArray meshDataArr;
        // public byte[] textureBuf;
        public IntPtr textureBuf;
    };

    volatile int isReady = 0, isStopped = 0;
    volatile MeshFrame readyFrame;
    // byte[] updTextureBuf;
    IntPtr updTextureBuf;

    // void ReadAll(Span<byte> buf)
    // {
    //     while (buf.Length > 0)
    //     {
    //         int r = ns.Read(buf);
    //         if (r == 0)
    //             throw new IOException("End of Stream");
    //         buf = buf.Slice(r);
    //     }
    // }

    void RunRecv(MeshFrame curFrame)
    {
        try {
            // uint[] bufNF = new uint[1];
            while (isStopped == 0)
            {
                // ReadAll(MemoryMarshal.Cast<uint, byte>(bufNF.AsSpan()));
                // int nF = (int)bufNF[0];
                int nF = readNF(fd);
                if (nF < 0)
                    break;

                var data = curFrame.meshDataArr[0];
                data.SetVertexBufferParams(nF * 3,
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2));
                // ReadAll(MemoryMarshal.Cast<VertexData, byte>(curFrame.meshDataArr[0].GetVertexData<VertexData>().AsSpan()));

                data.SetIndexBufferParams(nF * 3, IndexFormat.UInt32);
                // var indexBuf = data.GetIndexData<uint>();
                // for (int i = 0; i < nF * 3; i++)
                //     indexBuf[i] = (uint)i;

                unsafe {
                    var vertexBufPtr = (IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(data.GetVertexData<VertexData>());
                    var indexBufPtr = (IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(data.GetIndexData<uint>());
                    readMeshTexture(fd, nF, TextureWidth, TextureHeight, vertexBufPtr, indexBufPtr, curFrame.textureBuf);
                }

                data.subMeshCount = 1;
                data.SetSubMesh(0, new SubMeshDescriptor(0, nF * 3));

                // ReadAll(curFrame.textureBuf.AsSpan());

                curFrame = Interlocked.Exchange(ref readyFrame, curFrame);
                isReady = 1;
            }
        } finally {
            Marshal.FreeCoTaskMem(curFrame.textureBuf);
            Debug.Log("Stop Receiving");
            isStopped = 1;
        }
    }

    Thread thRecv;

    // Start is called before the first frame update
    public void Start()
    {
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Unlit/Texture"))
        {
            mainTexture = texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGB24, false)
        };
        meshFilter.mesh = mesh = new Mesh();

        // try {
        //     client = new TcpClient(Host, Port);
        //     ns = client.GetStream();
        // } catch {
        //     Application.Quit();
        //     UnityEditor.EditorApplication.isPlaying = false;
        // }

        fd = openConn(Host, Port);
        if (fd < 0) {
            Application.Quit();
            UnityEditor.EditorApplication.isPlaying = false;
        }

        readyFrame = new MeshFrame() {
            meshDataArr = Mesh.AllocateWritableMeshData(1),
            // textureBuf = new byte[TextureWidth * TextureHeight * 3]
            textureBuf = Marshal.AllocCoTaskMem(TextureWidth * TextureHeight * 3)
        };
        // updTextureBuf = new byte[TextureWidth * TextureHeight * 3];
        updTextureBuf = Marshal.AllocCoTaskMem(TextureWidth * TextureHeight * 3);
        var curRecvFrame = new MeshFrame() {
            meshDataArr = Mesh.AllocateWritableMeshData(1),
            // textureBuf = new byte[TextureWidth * TextureHeight * 3]
            textureBuf = Marshal.AllocCoTaskMem(TextureWidth * TextureHeight * 3)
        };
        thRecv = new Thread(() => {
            RunRecv(curRecvFrame);
        });
        thRecv.Start();
    }

    public void OnApplicationQuit()
    {
        isStopped = 1;
        closeConn(fd);
        thRecv.Join();
        Marshal.FreeCoTaskMem(updTextureBuf);
        Marshal.FreeCoTaskMem(readyFrame.textureBuf);
        Debug.Log("Application Quit");
    }


    // Update is called once per frame
    public void Update()
    {
        if (Interlocked.Exchange(ref isReady, 0) != 0)
        {
            var curFrame = new MeshFrame() {
                meshDataArr = Mesh.AllocateWritableMeshData(1),
                textureBuf = updTextureBuf
            };
            curFrame = Interlocked.Exchange(ref readyFrame, curFrame);

            Mesh.ApplyAndDisposeWritableMeshData(curFrame.meshDataArr, mesh);
            mesh.RecalculateBounds();

            // texture.LoadRawTextureData(curFrame.textureBuf);
            texture.LoadRawTextureData(curFrame.textureBuf, TextureWidth * TextureHeight * 3);

            mesh.UploadMeshData(false);
            texture.Apply();

            updTextureBuf = curFrame.textureBuf;
        }
        if (isStopped != 0) {
            Application.Quit();
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
