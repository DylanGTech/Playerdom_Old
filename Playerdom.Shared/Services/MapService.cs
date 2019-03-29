using Playerdom.Shared.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Playerdom.Shared.Entities;
using System.Collections.Concurrent;

#if WINDOWS_UAP
using Windows.Storage;
using Windows.ApplicationModel;
#elif WINDOWS

#endif

namespace Playerdom.Shared.Services
{
    public struct Tile
    {
        public const uint SIZE_X = 128;
        public const uint SIZE_Y = 128;

        public ushort typeID { get; set; } //Determines default properties
        public byte variantID { get; set; }
    }

    public class Map
    {
        public const uint SIZE_X = 2048;
        public const uint SIZE_Y = 2048;

        public string levelName = "";
        public Tile[,] tiles = new Tile[SIZE_X,SIZE_Y];
        public ConcurrentDictionary<Guid, GameObject> gameObjects = new ConcurrentDictionary<Guid, GameObject>();
        public ConcurrentDictionary<Guid, Entity> gameEntities = new ConcurrentDictionary<Guid, Entity>();

        public int mapOffsetX;
        public int mapOffsetY;


        public List<Guid> objectsMarkedForDeletion = new List<Guid>();
        public List<Guid> entitiesMarkedForDeletion = new List<Guid>();
    }

    public static class MapService
    {
        /*
        public static async Task<bool> SaveMapAsync(Map mapToSave)
        {
#if WINDOWS_UAP
            try
            {
                StorageFolder folder;
                folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("SaveData", CreationCollisionOption.OpenIfExists);
                folder = await folder.CreateFolderAsync(mapToSave.levelName, CreationCollisionOption.OpenIfExists);
                
                StorageFile file;
                file = await folder.CreateFileAsync(mapToSave.levelName + ".bin", CreationCollisionOption.ReplaceExisting);

                using (FileStream fs = new FileStream(file.Path, FileMode.Create))
                {
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        for (int y = 0; y < Map.SIZE_Y; y++)
                        {
                            for (int x = 0; x < Map.SIZE_X; x++)
                            {
                                bw.Write(mapToSave.tiles[x, y].typeID); //Tile Type
                                bw.Write(mapToSave.tiles[x, y].variantID); //Tile variant
                            }
                        }
                    }
                }

                Player p = null;
                List<Enemy> enemies = new List<Enemy>();

                foreach (GameObject o in mapToSave.gameObjects)
                {
                    if (o.GetType() == typeof(Player))
                        p = o as Player;
                    else if (o.GetType() == typeof(Enemy))
                        enemies.Add(o as Enemy);
                }

                file = await folder.CreateFileAsync("player.json", CreationCollisionOption.ReplaceExisting);
                string objectString = "";

                if (p == null)
                    p = new Player(new Point(0, 0), new Vector2(Tile.SIZE_X * 1, Tile.SIZE_Y * 1));
                    objectString = JsonConvert.SerializeObject(p);
                File.WriteAllText(file.Path, objectString);



                file = await folder.CreateFileAsync("enemies.json", CreationCollisionOption.ReplaceExisting);
                objectString = JsonConvert.SerializeObject(enemies);
                File.WriteAllText(file.Path, objectString);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;

#elif WINDOWS
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\SaveData\\" + mapToSave.levelName;
                Directory.CreateDirectory(path);



                using (FileStream fs = File.Create(path + "\\" + mapToSave.levelName + ".bin"))
                {
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        for (int y = 0; y < Map.SIZE_Y; y++)
                        {
                            for (int x = 0; x < Map.SIZE_X; x++)
                            {
                                bw.Write(mapToSave.tiles[x, y].typeID); //Tile Type
                                bw.Write(mapToSave.tiles[x, y].variantID); //Tile variant
                            }
                        }
                    }
                }

                Player p = null;
                List<Enemy> enemies = new List<Enemy>();

                foreach (GameObject o in mapToSave.gameObjects)
                {
                    if (o.GetType() == typeof(Player))
                        p = o as Player;
                    else if (o.GetType() == typeof(Enemy))
                        enemies.Add(o as Enemy);
                }

                File.Create(path + "\\" + "player.json").Dispose();


                string objectString = "";

                if (p == null)
                    p = new Player(new Point(0, 0), new Vector2(Tile.SIZE_X * 1, Tile.SIZE_Y * 1));
                objectString = JsonConvert.SerializeObject(p);
                File.WriteAllText(path + "\\" + "player.json", objectString);



                File.Create(path + "\\" + "enemies.json").Dispose();
                objectString = JsonConvert.SerializeObject(enemies);
                File.WriteAllText(path + "\\" + "enemies.json", objectString);
            }
            catch(Exception e)
            {
                return false;
            }

            return true;
            
#else
            throw new NotImplementedException();
#endif
        }

        public static async Task<Map> LoadMapAsync(string levelName)
        {
#if WINDOWS_UAP
            StorageFolder folder;
            folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("SaveData", CreationCollisionOption.OpenIfExists);
            folder = await folder.GetFolderAsync(levelName);
            StorageFile file;
            try
            {
                file = await folder.CreateFileAsync(levelName + ".bin", CreationCollisionOption.OpenIfExists);

                Map mapToLoad = new Map
                {
                    levelName = levelName,
                    tiles = new Tile[Map.SIZE_X, Map.SIZE_Y]
                };

                try
                {
                    using (FileStream fs = new FileStream(file.Path, FileMode.OpenOrCreate))
                    {
                        using (BinaryReader br = new BinaryReader(fs))
                        {
                            for (int y = 0; y < Map.SIZE_Y; y++)
                            {
                                for (int x = 0; x < Map.SIZE_X; x++)
                                {
                                    mapToLoad.tiles[x, y].typeID = br.ReadUInt16(); //Tile Type
                                    mapToLoad.tiles[x, y].variantID = br.ReadByte(); //Tile Type
                                }
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    await file.DeleteAsync();
                    return null;
                }
                

                if (mapToLoad.gameObjects == null) mapToLoad.gameObjects = new List<GameObject>();

                file = await folder.CreateFileAsync("player.json", CreationCollisionOption.OpenIfExists);

                string objectString;
                try
                {
                    objectString = File.ReadAllText(file.Path);
                    Player p = JsonConvert.DeserializeObject<Player>(objectString, new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
                    mapToLoad.gameObjects.Add(p);
                }
                catch(Exception e)
                {
                    await file.DeleteAsync();


                    file = await folder.CreateFileAsync("error.txt");
                    File.WriteAllText(file.Path, e.ToString());


                    return null;
                }



                file = await folder.CreateFileAsync("enemies.json", CreationCollisionOption.OpenIfExists);

                try
                {
                    objectString = File.ReadAllText(file.Path);

                    List<Enemy> list = JsonConvert.DeserializeObject<List<Enemy>>(objectString, new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
                    foreach(Enemy e in list)
                        mapToLoad.gameObjects.Add(e);
                }
                catch (Exception e)
                {
                    await file.DeleteAsync();
                    return null;
                }

                return mapToLoad;
            }
            catch (Exception e)
            {
                file = await folder.CreateFileAsync("error.txt", CreationCollisionOption.ReplaceExisting);
                File.WriteAllText(file.Path, e.ToString());
                return null;
            }
#elif WINDOWS

            string path = AppDomain.CurrentDomain.BaseDirectory + "\\SaveData\\" + levelName;

            try
            {

                Map mapToLoad = new Map
                {
                    levelName = levelName,
                    tiles = new Tile[Map.SIZE_X, Map.SIZE_Y]
                };

                try
                {
                    using (FileStream fs = new FileStream(path + "\\" + levelName + ".bin", FileMode.Open))
                    {
                        using (BinaryReader br = new BinaryReader(fs))
                        {
                            for (int y = 0; y < Map.SIZE_Y; y++)
                            {
                                for (int x = 0; x < Map.SIZE_X; x++)
                                {
                                    mapToLoad.tiles[x, y].typeID = br.ReadUInt16(); //Tile Type
                                    mapToLoad.tiles[x, y].variantID = br.ReadByte(); //Tile Type
                                }
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    if(File.Exists(path + "\\" + levelName + ".bin"))
                        File.Delete(path + "\\" + levelName + ".bin");
                    return null;
                }

                if (mapToLoad.gameObjects == null) mapToLoad.gameObjects = new List<GameObject>();

                string objectString;
                

                try
                {
                    objectString = File.ReadAllText(path + "\\player.json");

                    Player p = JsonConvert.DeserializeObject<Player>(objectString, new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
                    mapToLoad.gameObjects.Add(p);
                }
                catch (Exception e)
                {
                    if (File.Exists(path + "\\player.json"))
                        File.Delete(path + "\\player.json");

                    File.Create(path + "\\error.txt").Dispose();
                    File.WriteAllText(path + "\\error.txt", e.ToString());


                    return null;
                }




                try
                {
                    objectString = File.ReadAllText(path + "\\enemies.json");

                    List<Enemy> list = JsonConvert.DeserializeObject<List<Enemy>>(objectString, new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
                    foreach (Enemy e in list)
                        mapToLoad.gameObjects.Add(e);
                }
                catch (Exception e)
                {
                    if (File.Exists(path + "\\enemies.json"))
                        File.Delete(path + "\\enemies.json");
                    return null;
                }

                return mapToLoad;
            }
            catch (Exception e)
            {
                File.Create(path + "\\error.txt");
                File.WriteAllText(path + "\\error.txt", e.ToString());
                return null;
            }
#else
            throw new NotImplementedException();
#endif
        }

    */


        public static Map CreateMap(string mapName)
        {
            Map m = new Map();
            m.levelName = mapName;

            Random r = new Random(DateTime.Now.Millisecond);

            for (int y = 0; y < Map.SIZE_Y; y++)
            {
                for (int x = 0; x < Map.SIZE_X; x++)
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


                        if (number > 40) m.tiles[x, y].variantID = 1;
                        else m.tiles[x, y].variantID = 0;
                    }
                }
            }

            for (int i = 0; i < 32; i++)
                GenerateRandomRiver(m, r);

            int numPaths = r.Next(24, 32);

            List<Point> endPoints = new List<Point>();

            for (int path = 0; path < numPaths; path++)
            {
                Point p0 = new Point(r.Next(0, (int)Map.SIZE_X - 1), r.Next(0, (int)Map.SIZE_Y - 1));
                Point p1 = new Point(r.Next(0, (int)Map.SIZE_X - 1), r.Next(0, (int)Map.SIZE_Y - 1));

                endPoints.Add(p0);
                endPoints.Add(p1);



                int thickness = 4;
                double wy = 0;
                double wx = 0;

                if (p1.X != p0.X && (p1.Y - p0.Y) / (p1.X - p0.X) < 1)
                {
                    wy = (thickness - 1) * Math.Sqrt(Math.Pow((p1.X - p0.X), 2) + Math.Pow((p1.Y - p0.Y), 2)) / (2 * Math.Abs(p1.X - p0.X));

                    for (int i = 0; i < wy; i++)
                    {
                        GenerateLine(m, new Point(p0.X, p0.Y - i), new Point(p1.X, p1.Y - i), 3, 0);
                        GenerateLine(m, new Point(p0.X, p0.Y + i), new Point(p1.X, p1.Y + i), 3, 0);
                    }
                }
                else if (p1.Y != p0.Y && (p1.X - p0.X) / (p1.Y - p0.Y) < 1)
                {
                    wx = (thickness - 1) * Math.Sqrt(Math.Pow((p1.X - p0.X), 2) + Math.Pow((p1.Y - p0.Y), 2)) / (2 * Math.Abs(p1.Y - p0.Y));

                    for (int i = 0; i < wx; i++)
                    {
                        GenerateLine(m, new Point(p0.X - i, p0.Y), new Point(p1.X - i, p1.Y), 3, 0);
                        GenerateLine(m, new Point(p0.X + i, p0.Y), new Point(p1.X + i, p1.Y), 3, 0);
                    }
                }
            }

            for (int i = 0; i < endPoints.Count; i++)
            {
                if (i % 2 == 1)
                {
                    int pathOffsetX = r.Next(-5, 5);
                    int pathOffsetY = r.Next(-5, 5);

                    GenerateRandomHouse(m, new Point(endPoints[i].X + pathOffsetX, endPoints[i].X + pathOffsetY), r);
                }
            }

            AddStructure(m, new Point(16, 16), "structure.pldms");


            int numRandomEnemies = 96;

            for (int i = 0; i < numRandomEnemies; i++)
            {
                m.gameObjects.TryAdd(Guid.NewGuid(), new Enemy(new Point(r.Next(0, (int)Map.SIZE_X - 1) * (int)Tile.SIZE_X, r.Next(0, (int)Map.SIZE_Y - 1) * (int)Tile.SIZE_Y), new Vector2(Tile.SIZE_X, Tile.SIZE_Y)));
            }

            m.gameObjects.TryAdd(Guid.NewGuid(), new Townsman(new Point(12 * (int)Tile.SIZE_X, 12 * (int)Tile.SIZE_Y), new Vector2(Tile.SIZE_X, Tile.SIZE_Y), money: (decimal)5.0));

            Point endToStartAt = endPoints[r.Next(0, endPoints.Count - 1)];
            endToStartAt.X *= (int)Tile.SIZE_X;
            endToStartAt.Y *= (int)Tile.SIZE_Y;

            return m;
        }


        private static void GenerateRandomHouse(Map m, Point position, Random r)
        {
            uint sizeX = (uint)r.Next(12, 16);
            uint sizeY = (uint)r.Next(12, 16);

            if (position.X + sizeX > Map.SIZE_X - 1) position.X = (int)Map.SIZE_X - (int)sizeX - 1;
            if (position.Y + sizeY > Map.SIZE_Y - 1) position.Y = (int)Map.SIZE_Y - (int)sizeY - 1;

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
                    else if (m.tiles[position.X + x, position.Y + y].typeID == 3
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
                m.gameObjects.TryAdd(Guid.NewGuid(), new Enemy(new Point((int)((position.X + r.Next(1, (int)sizeX - 1)) * Tile.SIZE_X), (int)((position.Y + r.Next(1, (int)sizeY - 1)) * Tile.SIZE_Y)), new Vector2(Tile.SIZE_X, Tile.SIZE_Y)));

        }

        private static void GenerateRandomRiver(Map m, Random r)
        {
            Point originalp0;
            Point originalp1;
            Point originalp2;

            int mapMarginX = (int)(Map.SIZE_X / 4);
            int mapMarginY = (int)(Map.SIZE_Y / 4);
            int thickness = 4;


            switch (r.Next(0, 3))
            {
                default:
                    originalp0 = new Point(0, 0);
                    break;
                case 0:
                    originalp0 = new Point(0 - mapMarginX, r.Next(0 - mapMarginY, (int)Map.SIZE_Y + mapMarginY - 1));
                    break;
                case 1:
                    originalp0 = new Point(r.Next(0 - mapMarginX, (int)Map.SIZE_X + mapMarginX - 1), 0 - mapMarginY);
                    break;
                case 2:
                    originalp0 = new Point((int)Map.SIZE_X + mapMarginX - 1, r.Next(0 - mapMarginY, (int)Map.SIZE_Y + mapMarginY - 1));
                    break;
                case 3:
                    originalp0 = new Point(r.Next(0 - mapMarginX, (int)Map.SIZE_X + mapMarginX - 1), (int)Map.SIZE_Y + mapMarginY - 1);
                    break;
            }

            originalp1 = new Point(r.Next(0 - mapMarginX, (int)Map.SIZE_X + mapMarginX - 1), r.Next(0 - mapMarginY, (int)Map.SIZE_Y + mapMarginY - 1));


            switch (r.Next(0, 3))
            {
                default:
                    originalp2 = new Point(0, 0);
                    break;
                case 0:
                    originalp2 = new Point(0 - mapMarginX, r.Next(0 - mapMarginY, (int)Map.SIZE_Y + mapMarginY - 1));
                    break;
                case 1:
                    originalp2 = new Point(r.Next(0 - mapMarginX, (int)Map.SIZE_X + mapMarginX - 1), 0 - mapMarginY);
                    break;
                case 2:
                    originalp2 = new Point((int)Map.SIZE_X + mapMarginX - 1, r.Next(0 - mapMarginY, (int)Map.SIZE_Y + mapMarginY - 1));
                    break;
                case 3:
                    originalp2 = new Point(r.Next(0 - mapMarginX, (int)Map.SIZE_X + mapMarginX - 1), (int)Map.SIZE_Y + mapMarginY - 1);
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
                long xx = p0.X - p1.X, yy = p0.Y - p1.Y, xy;
                double dx, dy, err, cur = xx * sy - yy * sx;

                if (!(xx * sx <= 0 && yy * sy <= 0)) return;

                if (sx * (long)sx + sy * (long)sy > xx * xx + yy * yy)
                {
                    p2.X = p0.X; p0.X = sx + p1.X; p2.Y = p0.Y; p0.Y = sy + p1.Y; cur = -cur;
                }

                if (cur != 0)
                {
                    xx += sx; xx *= sx = p0.X < p2.X ? 1 : -1;
                    yy += sy; yy *= sy = p0.Y < p2.Y ? 1 : -1;
                    xy = 2 * xx * yy; xx *= xx; yy *= yy;

                    if (cur * sx * sy < 0)
                    {
                        xx = -xx; yy = -yy; xy = -xy; cur = -cur;
                    }
                    dx = 4.0 * sy * cur * (p1.X - p0.X) + xx - xy;
                    dy = 4.0 * sx * cur * (p0.Y - p1.Y) + yy - xy;
                    xx += xx; yy += yy; err = dx + dy + xy;

                    do
                    {
                        if (p0.X >= 0 && p0.Y >= 0 && p0.X < Map.SIZE_X && p0.Y < Map.SIZE_Y)
                        {
                            m.tiles[p0.X, p0.Y].typeID = 4;
                            m.tiles[p0.X, p0.Y].variantID = r.Next(0, 6) == 5 ? (byte)1 : (byte)0; ;
                        }

                        if (p0.X == p2.X && p0.Y == p2.Y) break;
                        p1.Y = 2 * err < dx ? 1 : 0;
                        if (2 * err > dy) { p0.X += sx; dx -= xy; err += dy += yy; }
                        if (p1.Y == 1) { p0.Y += sy; dy -= xy; err += dx += xx; }
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
                long xx = p0.X - p1.X, yy = p0.Y - p1.Y, xy;
                double dx, dy, err, cur = xx * sy - yy * sx;

                if (!(xx * sx <= 0 && yy * sy <= 0)) return;

                if (sx * (long)sx + sy * (long)sy > xx * xx + yy * yy)
                {
                    p2.X = p0.X; p0.X = sx + p1.X; p2.Y = p0.Y; p0.Y = sy + p1.Y; cur = -cur;
                }

                if (cur != 0)
                {
                    xx += sx; xx *= sx = p0.X < p2.X ? 1 : -1;
                    yy += sy; yy *= sy = p0.Y < p2.Y ? 1 : -1;
                    xy = 2 * xx * yy; xx *= xx; yy *= yy;

                    if (cur * sx * sy < 0)
                    {
                        xx = -xx; yy = -yy; xy = -xy; cur = -cur;
                    }
                    dx = 4.0 * sy * cur * (p1.X - p0.X) + xx - xy;
                    dy = 4.0 * sx * cur * (p0.Y - p1.Y) + yy - xy;
                    xx += xx; yy += yy; err = dx + dy + xy;

                    do
                    {
                        if (p0.X >= 0 && p0.Y >= 0 && p0.X < Map.SIZE_X && p0.Y < Map.SIZE_Y)
                        {
                            m.tiles[p0.X, p0.Y].typeID = 4;
                            m.tiles[p0.X, p0.Y].variantID = r.Next(0, 6) == 5 ? (byte)1 : (byte)0;
                        }

                        if (p0.X == p2.X && p0.Y == p2.Y) break;
                        p1.Y = 2 * err < dx ? 1 : 0;
                        if (2 * err > dy) { p0.X += sx; dx -= xy; err += dy += yy; }
                        if (p1.Y == 1) { p0.Y += sy; dy -= xy; err += dx += xx; }
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
            uint width;
            uint height;
            uint versionMajor;
            uint versionMinor;
            Tile[,] tiles;


            try
            {
                BinaryReader br = null;
#if WINDOWS_UAP
                StorageFolder folder;
                folder = Task.Run(async () => await Package.Current.InstalledLocation.GetFolderAsync("Content")).Result;
                folder = Task.Run(async () => await folder.GetFolderAsync("Structures")).Result;
                StorageFile file = Task.Run(async () => await folder.GetFileAsync(structureFileName)).Result;
                br = new BinaryReader(Task.Run(async () => await file.OpenReadAsync()).Result.AsStreamForRead());
                //#elif WINDOWS
#else
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\Content\\Structures\\" + structureFileName;
                br = new BinaryReader(new FileStream(path, FileMode.Open));
#endif


                if (br.ReadByte() == 0xCA && br.ReadByte() == 0xBD && br.ReadByte() == 0xAD)
                {
                    versionMajor = br.ReadByte();
                    versionMinor = br.ReadByte();
                }
                else return;

                if (versionMajor == 1 && versionMinor == 0)
                {
                    width = br.ReadUInt16();
                    height = br.ReadUInt16();
                }
                else return;

                tiles = new Tile[width, height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        tiles[x, y] = new Tile { typeID = br.ReadUInt16(), variantID = br.ReadByte() };
                    }
                }

                if (position.X + width <= Map.SIZE_X - 1 && position.Y + height <= Map.SIZE_Y - 1)
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
            catch (Exception e)
            {
                return;
            }
        }



        private static void GenerateLine(Map m, Point p0, Point p1, ushort newTypeID, byte newvariantID)
        {

            int xinc, yinc, x, y;
            int dx, dy, e;
            dx = Math.Abs(p1.X - p0.X);
            dy = Math.Abs(p1.Y - p0.Y);

            if (p0.X < p1.X)
                xinc = 1;
            else
                xinc = -1;


            if (p0.Y < p1.Y)
                yinc = 1;
            else
                yinc = -1;

            x = p0.X;
            y = p0.Y;


            if (x >= 0 && y >= 0 && x < Map.SIZE_X && y < Map.SIZE_Y)
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
                    if (x >= 0 && y >= 0 && x < Map.SIZE_X && y < Map.SIZE_Y)
                    {
                        m.tiles[x, y].typeID = newTypeID;
                        m.tiles[x, y].variantID = newvariantID;
                    }
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
                    if (x >= 0 && y >= 0 && x < Map.SIZE_X && y < Map.SIZE_Y)
                    {
                        m.tiles[x, y].typeID = newTypeID;
                        m.tiles[x, y].variantID = newvariantID;
                    }
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
