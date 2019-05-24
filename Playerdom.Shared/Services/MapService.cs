﻿using Playerdom.Shared.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Playerdom.Shared.Models;

#if WINDOWS_UAP
using Windows.Storage;
using Windows.ApplicationModel;
#elif WINDOWS

#endif

namespace Playerdom.Shared.Services
{
    public static class MapService
    {
        public static Map CreateMap(string mapName)
        {
            Map m = new Map {levelName = mapName};

            Random r = new Random(DateTime.Now.Millisecond);

            m.spawnTileLocation = new Point(r.Next(0, (int)Map.SizeX), r.Next(0, (int)Map.SizeY));

            for (int y = 0; y < Map.SizeY; y++)
            {
                for (int x = 0; x < Map.SizeX; x++)
                {
                    int number = r.Next(0, 48);

                    if (number == 47)
                    {
                        m.tiles[x, y].typeID = 2;
                        m.tiles[x, y].variantID = 0;
                    }
                    else
                    {
                        m.tiles[x, y].typeID = 1;
                        m.tiles[x, y].variantID = number > 40 ? (byte) 1 : (byte) 0;
                    }
                }
            }

            for (int i = 0; i < 32; i++)
                GenerateRandomRiver(m, r);

            int numPaths = r.Next(24, 32);

            List<Point> endPoints = new List<Point>();

            for (int path = 0; path < numPaths; path++)
            {
                Point p0 = new Point(r.Next(0, (int)Map.SizeX - 1), r.Next(0, (int)Map.SizeY - 1));
                Point p1 = new Point(r.Next(0, (int)Map.SizeX - 1), r.Next(0, (int)Map.SizeY - 1));

                endPoints.Add(p0);
                endPoints.Add(p1);

                const int thickness = 4;

                if (p1.X != p0.X && (p1.Y - p0.Y) / (p1.X - p0.X) < 1)
                {
                    var wy = (thickness - 1) * Math.Sqrt(Math.Pow((p1.X - p0.X), 2) + Math.Pow((p1.Y - p0.Y), 2)) / (2 * Math.Abs(p1.X - p0.X));

                    for (int i = 0; i < wy; i++)
                    {
                        GenerateLine(m, new Point(p0.X, p0.Y - i), new Point(p1.X, p1.Y - i), 3, 0);
                        GenerateLine(m, new Point(p0.X, p0.Y + i), new Point(p1.X, p1.Y + i), 3, 0);
                    }
                }
                else if (p1.Y != p0.Y && (p1.X - p0.X) / (p1.Y - p0.Y) < 1)
                {
                    var wx = (thickness - 1) * Math.Sqrt(Math.Pow((p1.X - p0.X), 2) + Math.Pow((p1.Y - p0.Y), 2)) / (2 * Math.Abs(p1.Y - p0.Y));

                    for (int i = 0; i < wx; i++)
                    {
                        GenerateLine(m, new Point(p0.X - i, p0.Y), new Point(p1.X - i, p1.Y), 3, 0);
                        GenerateLine(m, new Point(p0.X + i, p0.Y), new Point(p1.X + i, p1.Y), 3, 0);
                    }
                }
            }

            for (int i = 0; i < endPoints.Count; i++)
            {
                if (i % 2 != 1) continue;
                int pathOffsetX = r.Next(-5, 5);
                int pathOffsetY = r.Next(-5, 5);

                GenerateRandomHouse(m, new Point(endPoints[i].X + pathOffsetX, endPoints[i].X + pathOffsetY), r);
            }

            AddStructure(m, new Point(16, 16), "structure.pldms");


            const int numRandomEnemies = 96;

            for (int i = 0; i < numRandomEnemies; i++)
            {
                m.gameObjects.TryAdd(Guid.NewGuid(), new Enemy(new Point(r.Next(0, (int)Map.SizeX - 1) * (int)Tile.SizeX, r.Next(0, (int)Map.SizeY - 1) * (int)Tile.SizeY), new Vector2(Tile.SizeX, Tile.SizeY)));
            }

            m.gameObjects.TryAdd(Guid.NewGuid(), new Townsman(new Point(12 * (int)Tile.SizeX, 12 * (int)Tile.SizeY), new Vector2(Tile.SizeX, Tile.SizeY), money: (decimal)5.0));

            Point endToStartAt = endPoints[r.Next(0, endPoints.Count - 1)];
            endToStartAt.X *= (int)Tile.SizeX;
            endToStartAt.Y *= (int)Tile.SizeY;

            return m;
        }


        private static void GenerateRandomHouse(Map m, Point position, Random r)
        {
            uint sizeX = (uint)r.Next(12, 16);
            uint sizeY = (uint)r.Next(12, 16);

            if (position.X + sizeX > Map.SizeX - 1) position.X = (int)Map.SizeX - (int)sizeX - 1;
            if (position.Y + sizeY > Map.SizeY - 1) position.Y = (int)Map.SizeY - (int)sizeY - 1;

            if (position.X < 0) position.X = 0;
            if (position.Y < 0) position.Y = 0;

            int numDoors = r.Next(1, 3);
            int numRemovedBlocks = 0;

            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    if (m.tiles[position.X + x, position.Y + y].typeID == 4)
                        return;
                    if (m.tiles[position.X + x, position.Y + y].typeID == 3
                        || m.tiles[position.X + x, position.Y + y].typeID == 2)
                    {
                        numRemovedBlocks++;
                    }
                }
            }

            if (numRemovedBlocks > sizeX * sizeY * 0.6)

                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        if (x == 0 || y == 0 || x == sizeX - 1 || y == sizeY - 1)
                        {
                            if (m.tiles[position.X + x, position.Y + y].typeID == 2 ||
                                m.tiles[position.X + x, position.Y + y].typeID == 3)
                            {
                                m.tiles[position.X + x, position.Y + y].typeID = 3;
                                m.tiles[position.X + x, position.Y + y].variantID = 0;
                            }
                            else
                            {
                                m.tiles[position.X + x, position.Y + y].typeID = 2;
                                m.tiles[position.X + x, position.Y + y].variantID = 0;
                            }
                        }
                        else
                        {
                            m.tiles[position.X + x, position.Y + y].typeID = 3;
                            m.tiles[position.X + x, position.Y + y].variantID = 0;
                        }
                    }
                }


            int cornerOffset = sizeX < sizeY ? r.Next(1, (int)sizeX - 2) : r.Next(1, (int)sizeY - 2);

            for (int i = 0; i < numDoors; i++)
            {
                switch (r.Next(0, 4))
                {
                    //Top
                    case 0:
                        m.tiles[position.X + cornerOffset, position.Y].typeID = 3;
                        m.tiles[position.X + cornerOffset, position.Y].variantID = 0;
                        m.tiles[position.X + cornerOffset + 1, position.Y].typeID = 3;
                        m.tiles[position.X + cornerOffset + 1, position.Y].variantID = 0;
                        break;

                    //Bottom
                    case 1:
                        m.tiles[position.X + cornerOffset, position.Y + sizeY - 1].typeID = 3;
                        m.tiles[position.X + cornerOffset, position.Y + sizeY - 1].variantID = 0;
                        m.tiles[position.X + cornerOffset + 1, position.Y + sizeY - 1].typeID = 3;
                        m.tiles[position.X + cornerOffset + 1, position.Y + sizeY - 1].variantID = 0;
                        break;

                    //Left
                    case 2:
                        m.tiles[position.X, position.Y + cornerOffset].typeID = 3;
                        m.tiles[position.X, position.Y + cornerOffset].variantID = 0;
                        m.tiles[position.X, position.Y + cornerOffset + 1].typeID = 3;
                        m.tiles[position.X, position.Y + cornerOffset + 1].variantID = 0;
                        break;

                    //Right
                    case 3:
                        m.tiles[position.X + sizeX - 1, position.Y + cornerOffset].typeID = 3;
                        m.tiles[position.X + sizeX - 1, position.Y + cornerOffset].variantID = 0;
                        m.tiles[position.X + sizeX - 1, position.Y + cornerOffset].typeID = 3;
                        m.tiles[position.X + sizeX - 1, position.Y + cornerOffset].variantID = 0;
                        break;
                }
            }


            int numEnemies = r.Next(0, 7);

            for (int i = 0; i < numEnemies; i++)
                m.gameObjects.TryAdd(Guid.NewGuid(), new Enemy(new Point((int)((position.X + r.Next(1, (int)sizeX - 1)) * Tile.SizeX), (int)((position.Y + r.Next(1, (int)sizeY - 1)) * Tile.SizeY)), new Vector2(Tile.SizeX, Tile.SizeY)));

        }

        private static void GenerateRandomRiver(Map m, Random r)
        {
            Point originalp0;
            Point originalp1;
            Point originalp2;

            const int mapMarginX = (int)(Map.SizeX / 4);
            const int mapMarginY = (int)(Map.SizeY / 4);
            const int thickness = 4;


            switch (r.Next(0, 3))
            {
                default:
                    originalp0 = new Point(0, 0);
                    break;
                case 0:
                    originalp0 = new Point(0 - mapMarginX, r.Next(0 - mapMarginY, (int)Map.SizeY + mapMarginY - 1));
                    break;
                case 1:
                    originalp0 = new Point(r.Next(0 - mapMarginX, (int)Map.SizeX + mapMarginX - 1), 0 - mapMarginY);
                    break;
                case 2:
                    originalp0 = new Point((int)Map.SizeX + mapMarginX - 1, r.Next(0 - mapMarginY, (int)Map.SizeY + mapMarginY - 1));
                    break;
                case 3:
                    originalp0 = new Point(r.Next(0 - mapMarginX, (int)Map.SizeX + mapMarginX - 1), (int)Map.SizeY + mapMarginY - 1);
                    break;
            }

            originalp1 = new Point(r.Next(0 - mapMarginX, (int)Map.SizeX + mapMarginX - 1), r.Next(0 - mapMarginY, (int)Map.SizeY + mapMarginY - 1));


            switch (r.Next(0, 3))
            {
                default:
                    originalp2 = new Point(0, 0);
                    break;
                case 0:
                    originalp2 = new Point(0 - mapMarginX, r.Next(0 - mapMarginY, (int)Map.SizeY + mapMarginY - 1));
                    break;
                case 1:
                    originalp2 = new Point(r.Next(0 - mapMarginX, (int)Map.SizeX + mapMarginX - 1), 0 - mapMarginY);
                    break;
                case 2:
                    originalp2 = new Point((int)Map.SizeX + mapMarginX - 1, r.Next(0 - mapMarginY, (int)Map.SizeY + mapMarginY - 1));
                    break;
                case 3:
                    originalp2 = new Point(r.Next(0 - mapMarginX, (int)Map.SizeX + mapMarginX - 1), (int)Map.SizeY + mapMarginY - 1);
                    break;
            }

            for (int i = -thickness / 2; i < thickness / 2; i++)
            {
                Point p0 = originalp0;
                Point p1 = originalp1;
                Point p2 = originalp2;

                p0.Y += i;
                p1.Y += i;
                p2.Y += i;

                int sx = p2.X - p1.X, sy = p2.Y - p1.Y;
                long xx = p0.X - p1.X, yy = p0.Y - p1.Y;
                double cur = xx * sy - yy * sx;

                if (!(xx * sx <= 0 && yy * sy <= 0)) return;

                if (sx * (long)sx + sy * (long)sy > xx * xx + yy * yy)
                {
                    p2.X = p0.X; p0.X = sx + p1.X; p2.Y = p0.Y; p0.Y = sy + p1.Y; cur = -cur;
                }

                if (cur != 0)
                {
                    xx += sx; xx *= sx = p0.X < p2.X ? 1 : -1;
                    yy += sy; yy *= sy = p0.Y < p2.Y ? 1 : -1;
                    var xy = 2 * xx * yy; xx *= xx; yy *= yy;

                    if (cur * sx * sy < 0)
                    {
                        xx = -xx; yy = -yy; xy = -xy; cur = -cur;
                    }
                    var dx = 4.0 * sy * cur * (p1.X - p0.X) + xx - xy;
                    var dy = 4.0 * sx * cur * (p0.Y - p1.Y) + yy - xy;
                    xx += xx; yy += yy; var err = dx + dy + xy;

                    do
                    {
                        if (p0.X >= 0 && p0.Y >= 0 && p0.X < Map.SizeX && p0.Y < Map.SizeY)
                        {
                            m.tiles[p0.X, p0.Y].typeID = 4;
                            m.tiles[p0.X, p0.Y].variantID = r.Next(0, 6) == 5 ? (byte)1 : (byte)0; ;
                        }

                        if (p0.X == p2.X && p0.Y == p2.Y) break;
                        p1.Y = 2 * err < dx ? 1 : 0;
                        if (2 * err > dy) { p0.X += sx; dx -= xy; err += dy += yy; }

                        if (p1.Y != 1) continue;
                        p0.Y += sy; dy -= xy; err += dx += xx;
                    } while (dy < dx);
                }
                else
                {
                    GenerateLine(m, p0, p2, 4, 0);
                }
            }

            for (int i = -thickness / 2; i < thickness / 2; i++)
            {
                Point p0 = originalp0;
                Point p1 = originalp1;
                Point p2 = originalp2;

                p0.X += i;
                p1.X += i;
                p2.X += i;


                int sx = p2.X - p1.X, sy = p2.Y - p1.Y;
                long xx = p0.X - p1.X, yy = p0.Y - p1.Y;
                double cur = xx * sy - yy * sx;

                if (!(xx * sx <= 0 && yy * sy <= 0)) return;

                if (sx * (long)sx + sy * (long)sy > xx * xx + yy * yy)
                {
                    p2.X = p0.X; p0.X = sx + p1.X; p2.Y = p0.Y; p0.Y = sy + p1.Y; cur = -cur;
                }

                if (cur != 0)
                {
                    xx += sx; xx *= sx = p0.X < p2.X ? 1 : -1;
                    yy += sy; yy *= sy = p0.Y < p2.Y ? 1 : -1;
                    var xy = 2 * xx * yy; xx *= xx; yy *= yy;

                    if (cur * sx * sy < 0)
                    {
                        xx = -xx; yy = -yy; xy = -xy; cur = -cur;
                    }
                    var dx = 4.0 * sy * cur * (p1.X - p0.X) + xx - xy;
                    var dy = 4.0 * sx * cur * (p0.Y - p1.Y) + yy - xy;
                    xx += xx; yy += yy; var err = dx + dy + xy;

                    do
                    {
                        if (p0.X >= 0 && p0.Y >= 0 && p0.X < Map.SizeX && p0.Y < Map.SizeY)
                        {
                            m.tiles[p0.X, p0.Y].typeID = 4;
                            m.tiles[p0.X, p0.Y].variantID = r.Next(0, 6) == 5 ? (byte)1 : (byte)0;
                        }

                        if (p0.X == p2.X && p0.Y == p2.Y) break;
                        p1.Y = 2 * err < dx ? 1 : 0;
                        if (2 * err > dy) { p0.X += sx; dx -= xy; err += dy += yy; }

                        if (p1.Y != 1) continue;
                        p0.Y += sy; dy -= xy; err += dx += xx;
                    } while (dy < dx);
                }
                else
                {
                    GenerateLine(m, p0, p2, 4, 0);
                }
            }


        }

        private static void AddStructure(Map m, Point position, string structureFileName)
        {
            try
            {
                BinaryReader br;
#if WINDOWS_UAP
                var folder = Task.Run(async () => await Package.Current.InstalledLocation.GetFolderAsync("Content")).Result;
                folder = Task.Run(async () => await folder.GetFolderAsync("Structures")).Result;
                StorageFile file = Task.Run(async () => await folder.GetFileAsync(structureFileName)).Result;
                br = new BinaryReader(Task.Run(async () => await file.OpenReadAsync()).Result.AsStreamForRead());
                //#elif WINDOWS
#else
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\Content\\Structures\\" + structureFileName;
                br = new BinaryReader(new FileStream(path, FileMode.Open));
#endif


                uint versionMajor;
                uint versionMinor;
                if (br.ReadByte() == 0xCA && br.ReadByte() == 0xBD && br.ReadByte() == 0xAD)
                {
                    versionMajor = br.ReadByte();
                    versionMinor = br.ReadByte();
                }
                else return;

                uint width;
                uint height;
                if (versionMajor == 1 && versionMinor == 0)
                {
                    width = br.ReadUInt16();
                    height = br.ReadUInt16();
                }
                else return;

                var tiles = new Tile[width, height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        tiles[x, y] = new Tile { typeID = br.ReadUInt16(), variantID = br.ReadByte() };
                    }
                }

                if (position.X + width <= Map.SizeX - 1 && position.Y + height <= Map.SizeY - 1)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            if (tiles[x, y].typeID != 0)
                            {
                                m.tiles[position.X + x, position.Y + y] = tiles[x, y];
                            }
                        }
                    }
                }
                br.Dispose();
            }
            catch (Exception e) // Unused exception??
            {
                return;
            }
        }

        public static Point GenerateSpawnPoint(Map m)
        {
            Random r = new Random();

            Point spawnPoint = new Point(m.spawnTileLocation.X * (int)Tile.SizeX + r.Next((int)Tile.SizeX * -5, (int)Tile.SizeX * 5), m.spawnTileLocation.Y * (int)Tile.SizeY + r.Next((int)Tile.SizeY * -5, (int)Tile.SizeY * 5));

            if (spawnPoint.X < 0) spawnPoint.X = 0;
            else if (spawnPoint.X > (Map.SizeX - 1) * Tile.SizeX) spawnPoint.X = (int)((Map.SizeX - 1) * Tile.SizeX);


            if (spawnPoint.Y < 0) spawnPoint.Y = 0;
            else if (spawnPoint.Y > (Map.SizeY - 1) * Tile.SizeY) spawnPoint.Y = (int)((Map.SizeY - 1) * Tile.SizeY);

            return spawnPoint;
        }

        private static void GenerateLine(Map m, Point p0, Point p1, ushort newTypeID, byte newvariantID)
        {
            int xinc, yinc;
            int e;
            var dx = Math.Abs(p1.X - p0.X);
            var dy = Math.Abs(p1.Y - p0.Y);

            if (p0.X < p1.X)
                xinc = 1;
            else
                xinc = -1;


            if (p0.Y < p1.Y)
                yinc = 1;
            else
                yinc = -1;

            var x = p0.X;
            var y = p0.Y;

            if (x >= 0 && y >= 0 && x < Map.SizeX && y < Map.SizeY)
            {
                m.tiles[x, y].typeID = newTypeID;
                m.tiles[x, y].variantID = newvariantID;
            }

            if (dx >= dy)
            {
                e = (2 * dy) - dx;

                while (x != p1.X)
                {
                    if (e < 0)
                    {
                        e += (2 * dy);
                    }
                    else
                    {
                        e += (2 * (dy - dx));
                        y += yinc;
                    }
                    x += xinc;
                    if (x < 0 || y < 0 || x >= Map.SizeX || y >= Map.SizeY) continue;
                    m.tiles[x, y].typeID = newTypeID;
                    m.tiles[x, y].variantID = newvariantID;
                }
            }
            else
            {
                e = (2 * dx) - dy;

                while (y != p1.Y)
                {
                    if (e < 0)
                    {
                        e += (2 * dx);
                    }
                    else
                    {
                        e += (2 * (dx - dy));
                        x += xinc;
                    }
                    y += yinc;
                    if (x < 0 || y < 0 || x >= Map.SizeX || y >= Map.SizeY) continue;
                    m.tiles[x, y].typeID = newTypeID;
                    m.tiles[x, y].variantID = newvariantID;
                }
            }
        }
    }
    public class MapColumn
    {
        public MapColumn(uint columnNumber, ushort[] typesColumn, byte[] variantsColumn)
        {
            ColumnNumber = columnNumber;
            TypesColumn = typesColumn;
            VariantsColumn = variantsColumn;
        }

        public uint ColumnNumber { get; set; }
        public ushort[] TypesColumn { get; set; }
        public byte[] VariantsColumn { get; set; }
    }
}
