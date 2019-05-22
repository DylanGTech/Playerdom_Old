using Ceras;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Entities;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared.Services;
using System.Collections.Concurrent;
using Playerdom.Shared.Models;

namespace Playerdom.Shared
{
    public static class PlayerdomCerasSettings
    {
        public static SerializerConfig Config
        {
            get;
        } = new SerializerConfig();

        public static void Initialize()
        {
            
            Config.KnownTypes.Add(typeof(GameObject));
            Config.KnownTypes.Add(typeof(Player));
            Config.KnownTypes.Add(typeof(Enemy));
            Config.KnownTypes.Add(typeof(Townsman));

            Config.KnownTypes.Add(typeof(Entity));
            Config.KnownTypes.Add(typeof(Bullet));
            Config.KnownTypes.Add(typeof(MoneyDrop));

            Config.KnownTypes.Add(typeof(ChatMessage));
            Config.KnownTypes.Add(typeof(List<ChatMessage>));


            Config.KnownTypes.Add(typeof(Vector2));
            Config.KnownTypes.Add(typeof(Point));
            Config.KnownTypes.Add(typeof(Rectangle));
            Config.KnownTypes.Add(typeof(Guid));
            Config.KnownTypes.Add(typeof(Color));
            Config.KnownTypes.Add(typeof(Keys));
            Config.KnownTypes.Add(typeof(Keys[]));

            Config.KnownTypes.Add(typeof(KeyValuePair<string, string>));

            Config.KnownTypes.Add(typeof(bool));
            Config.KnownTypes.Add(typeof(uint));
            Config.KnownTypes.Add(typeof(string));
            Config.KnownTypes.Add(typeof(ObjectType));
            Config.KnownTypes.Add(typeof(DirectionX));
            Config.KnownTypes.Add(typeof(DirectionY));
            Config.KnownTypes.Add(typeof(MapColumn));
            Config.KnownTypes.Add(typeof(MapColumn[]));
            Config.KnownTypes.Add(typeof(KeyValuePair<Guid, GameObject>));
            Config.KnownTypes.Add(typeof(Dictionary<Guid, GameObject>));
            Config.KnownTypes.Add(typeof(Dictionary<Guid, Entity>));
            Config.KnownTypes.Add(typeof(ConcurrentDictionary<Guid, GameObject>));
            Config.KnownTypes.Add(typeof(ConcurrentDictionary<Guid, Entity>));

            Config.KnownTypes.Add(typeof(KeyboardState));
            
            Config.ConfigType<Player>().ConstructBy(typeof(Player).GetConstructors()[0]);
            Config.ConfigType<Enemy>().ConstructBy(typeof(Enemy).GetConstructors()[0]);
            Config.ConfigType<Townsman>().ConstructBy(typeof(Townsman).GetConstructors()[0]);
            Config.ConfigType<Bullet>().ConstructBy(typeof(Bullet).GetConstructors()[0]);
            Config.ConfigType<MoneyDrop>().ConstructBy(typeof(MoneyDrop).GetConstructors()[0]);
            Config.ConfigType<MapColumn>().ConstructBy(typeof(MapColumn).GetConstructors()[0]);

            Config.ConfigType<Point>().ConstructBy(typeof(Point).GetConstructor(new Type[] { typeof(int), typeof(int) }));
            Config.ConfigType<Vector2>().ConstructBy(typeof(Vector2).GetConstructor(new Type[] { typeof(float), typeof(float) }));

            Config.Advanced.PersistTypeCache = true;
        }
    }
}
