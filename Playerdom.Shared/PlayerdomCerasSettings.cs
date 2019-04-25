using Ceras;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared.Services;
using System.Collections.Concurrent;
using Playerdom.Shared.Models;

namespace Playerdom.Shared
{
    public static class PlayerdomCerasSettings
    {
        public static SerializerConfig config
        {
            get; private set;
        } = new SerializerConfig();

        public static void Initialize()
        {
            
            config.KnownTypes.Add(typeof(GameObject));
            config.KnownTypes.Add(typeof(Player));
            config.KnownTypes.Add(typeof(Enemy));
            config.KnownTypes.Add(typeof(Townsman));

            config.KnownTypes.Add(typeof(Entity));
            config.KnownTypes.Add(typeof(Bullet));
            config.KnownTypes.Add(typeof(MoneyDrop));

            config.KnownTypes.Add(typeof(ChatMessage));
            config.KnownTypes.Add(typeof(List<ChatMessage>));


            config.KnownTypes.Add(typeof(Vector2));
            config.KnownTypes.Add(typeof(Point));
            config.KnownTypes.Add(typeof(Rectangle));
            config.KnownTypes.Add(typeof(Guid));
            config.KnownTypes.Add(typeof(Color));
            config.KnownTypes.Add(typeof(Keys));
            config.KnownTypes.Add(typeof(Keys[]));

            config.KnownTypes.Add(typeof(KeyValuePair<string, string>));

            config.KnownTypes.Add(typeof(bool));
            config.KnownTypes.Add(typeof(uint));
            config.KnownTypes.Add(typeof(string));
            config.KnownTypes.Add(typeof(ObjectType));
            config.KnownTypes.Add(typeof(DirectionX));
            config.KnownTypes.Add(typeof(DirectionY));
            config.KnownTypes.Add(typeof(MapColumn));
            config.KnownTypes.Add(typeof(MapColumn[]));
            config.KnownTypes.Add(typeof(KeyValuePair<Guid, GameObject>));
            config.KnownTypes.Add(typeof(Dictionary<Guid, GameObject>));
            config.KnownTypes.Add(typeof(Dictionary<Guid, Entity>));
            config.KnownTypes.Add(typeof(ConcurrentDictionary<Guid, GameObject>));
            config.KnownTypes.Add(typeof(ConcurrentDictionary<Guid, Entity>));

            config.KnownTypes.Add(typeof(KeyboardState));
            
            config.ConfigType<Player>().ConstructBy(typeof(Player).GetConstructors()[0]);
            config.ConfigType<Enemy>().ConstructBy(typeof(Enemy).GetConstructors()[0]);
            config.ConfigType<Townsman>().ConstructBy(typeof(Townsman).GetConstructors()[0]);
            config.ConfigType<Bullet>().ConstructBy(typeof(Bullet).GetConstructors()[0]);
            config.ConfigType<MoneyDrop>().ConstructBy(typeof(MoneyDrop).GetConstructors()[0]);
            config.ConfigType<MapColumn>().ConstructBy(typeof(MapColumn).GetConstructors()[0]);

            config.ConfigType<Point>().ConstructBy(typeof(Point).GetConstructor(new Type[] { typeof(int), typeof(int) }));
            config.ConfigType<Vector2>().ConstructBy(typeof(Vector2).GetConstructor(new Type[] { typeof(float), typeof(float) }));

            config.Advanced.PersistTypeCache = true;
        }
    }
}
