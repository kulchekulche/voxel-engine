using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Project;

public static class Ambient
{
    public static float[,,] array;
    public static int texture;
    public static int distance = 32;

    public static void Init(Voxels voxels)
    {
        var size = voxels.size / distance;
        array = new float[size.X, size.Y, size.Z];
        CalcAll(voxels);
        GenTexture(voxels);
    }

    public static void CalcChanged(Voxels voxels, List<Vector3i> changedVoxels, Vector3i corner)
    {
        List<Vector3i> changedBoxes = new List<Vector3i>();
        foreach (var voxel in changedVoxels) if (!changedBoxes.Contains((voxel + corner) / distance)) changedBoxes.Add((voxel + corner) / distance);
        foreach (var box in changedBoxes) CalcBox(box, voxels);
        UpdateTexture(voxels);
    }

    public static void CalcBox(Vector3i box, Voxels voxels)
    {
        float total = distance * distance * distance;
        float filled = 0;

        for (int vx = 0; vx < distance; vx++)
        {
            for (int vy = 0; vy < distance; vy++)
            {
                for (int vz = 0; vz < distance; vz++)
                {
                    var coord = new Vector3i(box.X * distance + vx, box.Y * distance + vy, box.Z * distance + vz);
                    float value = voxels.array[coord.X, coord.Y, coord.Z];
                    if (value != 0) filled++;
                }
            }
        }

        float ao = filled / total;
        array[box.X, box.Y, box.Z] = ao;
    }

    public static void CalcAll(Voxels voxels)
    {
        var size = voxels.size / distance;
        array = new float[size.X, size.Y, size.Z];
        
        Parallel.For(0, size.X, x =>
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int z = 0; z < size.Z; z++)
                {
                    CalcBox(new Vector3i(x, y, z), voxels);
                }
            }
        });
    }

    public static void GenTexture(Voxels voxels)
    {
        var size = voxels.size / distance;

        // rotate data (dont know why this is needed, but whatever, it works)
        float[,,] rotated = new float[size.Z, size.Y, size.X];
        Parallel.For(0, size.X, x =>
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int z = 0; z < size.Z; z++)
                {
                    rotated[z, y, x] = array[x, y, z];
                }
            }
        });

        GL.DeleteTexture(texture);
        texture = GL.GenTexture();
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture3D, texture);
        GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.R32f, size.X, size.Y, size.Z, 0, PixelFormat.Red, PixelType.Float, rotated);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToBorder);
    }

    public static void UpdateTexture(Voxels voxels)
    {
        var size = voxels.size / distance;

        // rotate data (dont know why this is needed, but whatever, it works)
        float[,,] rotated = new float[size.Z, size.Y, size.X];
        Parallel.For(0, size.X, x =>
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int z = 0; z < size.Z; z++)
                {
                    rotated[z, y, x] = array[x, y, z];
                }
            }
        });

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture3D, texture);
        GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.R32f, size.X, size.Y, size.Z, 0, PixelFormat.Red, PixelType.Float, rotated);
    }
}