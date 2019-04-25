using Playerdom.Shared.Entities;
using Playerdom.Shared.Objects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Playerdom.Shared.Models
{
    public class Map
    {
        public const uint SIZE_X = 2048;
        public const uint SIZE_Y = 2048;

        public string levelName = "";
        public Tile[,] tiles = new Tile[SIZE_X, SIZE_Y];
        public ConcurrentDictionary<Guid, GameObject> gameObjects = new ConcurrentDictionary<Guid, GameObject>();
        public ConcurrentDictionary<Guid, Entity> gameEntities = new ConcurrentDictionary<Guid, Entity>();


        public List<Guid> objectsMarkedForDeletion = new List<Guid>();
        public List<Guid> entitiesMarkedForDeletion = new List<Guid>();



        public Map Clone()
        {
            Map m = new Map();

            m.levelName = levelName;
            for (int y = 0; y < Map.SIZE_Y; y++)
            {
                for (int x = 0; x < Map.SIZE_X; x++)
                {
                    m.tiles[x, y].typeID = tiles[x, y].typeID;
                    m.tiles[x, y].variantID = tiles[x, y].variantID;
                }
            }

            m.gameObjects = new ConcurrentDictionary<Guid, GameObject>(gameObjects);
            m.gameEntities = new ConcurrentDictionary<Guid, Entity>(gameEntities);

            m.objectsMarkedForDeletion = new List<Guid>(objectsMarkedForDeletion);
            m.entitiesMarkedForDeletion = new List<Guid>(entitiesMarkedForDeletion);

            return m;

        }
    }

    public struct Tile
    {
        public const uint SIZE_X = 128;
        public const uint SIZE_Y = 128;

        public ushort typeID { get; set; } //Determines default properties
        public byte variantID { get; set; }
    }

}
