using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Project
{
    public class VoxelData
    {
        public float[,,] rawData;
        public int voxelTextureHandle;

        public VoxelData(int size)
        {
            rawData = new float[size, size, size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        if (Vector3.Distance(new Vector3(x, y, z), (new Vector3(size, size, size) / 2)) < (size / 4)) rawData[x, y, z] = 1;
                        else rawData[x, y, z] = 0;
                    }
                }
            }
            voxelTextureHandle = CreateVoxelTexture(size, rawData);
        }

        private int CreateVoxelTexture(int size, float[,,] rawData)
        {
            int voxelTextureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture3D, voxelTextureHandle);
            GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.R32f, size, size, size, 0, PixelFormat.Red, PixelType.Float, rawData);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToBorder);
            return voxelTextureHandle;
        }

        public void PlaceVoxelSphere(Vector3i position, int radius, float value)
        {
            GL.BindTexture(TextureTarget.Texture3D, voxelTextureHandle);

            float[,,] newSubVoxelData = new float[radius, radius, radius];

            position = position - Vector3i.One * radius / 2;

            for (int x = 0; x < radius; x++)
            {
                for (int y = 0; y < radius; y++)
                {
                    for (int z = 0; z < radius; z++)
                    {
                        if (IsInBounds(position + new Vector3(x, y, z), rawData))
                        {
                            Vector3i localCoord = new Vector3i(x, y, z);
                            Vector3i worldCoord = position + localCoord;
                            
                            bool isInRadius = Vector3.Distance(localCoord, new Vector3(radius, radius , radius) / 2) < radius / 2;

                            float currentWorldSpaceValue = rawData[worldCoord.X, worldCoord.Y, worldCoord.Z];

                            float newValue = currentWorldSpaceValue;

                            if (currentWorldSpaceValue == 0 && isInRadius) newValue = value;

                            newSubVoxelData[localCoord.Z, localCoord.Y, localCoord.X] = newValue;
                            rawData[worldCoord.X, worldCoord.Y, worldCoord.Z] = newValue;
                        }
                    }
                }
            }

            GL.TexSubImage3D(TextureTarget.Texture3D, 0, position.X, position.Y, position.Z, radius, radius, radius, PixelFormat.Red, PixelType.Float, newSubVoxelData);
        }

        public bool IsInBounds(Vector3 coord, float[,,] data)
        {
            bool xIsInBounds = coord.X >= data.GetLowerBound(0) && coord.X <= data.GetUpperBound(0);
            bool yIsInBounds = coord.Y >= data.GetLowerBound(1) && coord.Y <= data.GetUpperBound(1);
            bool zIsInBounds = coord.Z >= data.GetLowerBound(2) && coord.Z <= data.GetUpperBound(2);
            return xIsInBounds && yIsInBounds && zIsInBounds;
        }

        public Vector3 VoxelTrace(Vector3 eye, Vector3 marchingDirection, int voxelTraceSteps)
        {
            Vector3 rayOrigin = eye;
            Vector3 rayDirection = marchingDirection;
            Vector3 cellDimension = new Vector3(1, 1, 1);
            Vector3 voxelcoord;
            Vector3 deltaT;
            float t_x, t_y, t_z;

            // initializing values
            if (rayDirection.X < 0)
            {
                deltaT.X = -cellDimension.X / rayDirection.X;
                t_x = (MathF.Floor(rayOrigin.X / cellDimension.X) * cellDimension.X- rayOrigin.X) / rayDirection.X;
            }
            else 
            {
                deltaT.X = cellDimension.X / rayDirection.X;
                t_x = ((MathF.Floor(rayOrigin.X / cellDimension.X) + 1) * cellDimension.X - rayOrigin.X) / rayDirection.X;
            }
            if (rayDirection.Y < 0)
            {
                deltaT.Y = -cellDimension.Y / rayDirection.Y;
                t_y = (MathF.Floor(rayOrigin.Y / cellDimension.Y) * cellDimension.Y - rayOrigin.Y) / rayDirection.Y;
            }
            else 
            {
                deltaT.Y = cellDimension.Y / rayDirection.Y;
                t_y = ((MathF.Floor(rayOrigin.Y / cellDimension.Y) + 1) * cellDimension.Y - rayOrigin.Y) / rayDirection.Y;
            }
            if (rayDirection.Z < 0)
            {
                deltaT.Z = -cellDimension.Z / rayDirection.Z;
                t_z = (MathF.Floor(rayOrigin.Z / cellDimension.Z) * cellDimension.Z - rayOrigin.Z) / rayDirection.Z;
            }
            else
            {
                deltaT.Z = cellDimension.Z / rayDirection.Z;
                t_z = ((MathF.Floor(rayOrigin.Z / cellDimension.Z) + 1) * cellDimension.Z - rayOrigin.Z) / rayDirection.Z;
            }

            // initializing some variables
            float t = 0;
            float stepsTraced = 0;
            Vector3 cellIndex = new Vector3(MathF.Floor(rayOrigin.X), MathF.Floor(rayOrigin.Y), MathF.Floor(rayOrigin.Z));

            // tracing the grid
            while (true)
            {
                // if voxel is found
                if (IsInBounds(cellIndex, rawData))
                {
                    if (rawData[((int)cellIndex.X), ((int)cellIndex.Y), ((int)cellIndex.Z)] > 0)
                    {
                        voxelcoord = cellIndex;
                        break;
                    }
                }

                // increment step
                if (t_x < t_y)
                {
                    if (t_x < t_z)
                    {
                        t = t_x;
                        t_x += deltaT.X;
                        if (rayDirection.X < 0) cellIndex.X -= 1;
                        else cellIndex.X += 1;
                    }
                    else
                    {
                        t = t_z;
                        t_z += deltaT.Z;
                        if (rayDirection.Z < 0) cellIndex.Z -= 1;
                        else cellIndex.Z += 1;
                    }
                }
                else
                {
                    if (t_y < t_z)
                    {
                        t = t_y;
                        t_y += deltaT.Y;
                        if (rayDirection.Y < 0) cellIndex.Y -= 1;
                        else cellIndex.Y += 1;
                    }
                    else
                    {
                        t = t_z;
                        t_z += deltaT.Z;
                        if (rayDirection.Z < 0) cellIndex.Z -= 1;
                        else cellIndex.Z += 1;
                    }
                }

                stepsTraced++;

                // if no voxel was hit
                if (stepsTraced > voxelTraceSteps)
                {
                    voxelcoord = new Vector3(0, 0, 0);
                    break;
                }
            }

            return voxelcoord;
        }
    }
}