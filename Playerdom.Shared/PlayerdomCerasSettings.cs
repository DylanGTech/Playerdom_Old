using Ceras;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

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

            //config.KnownTypes.Add(typeof(DirectionX));
            //config.KnownTypes.Add(typeof(DirectionY));
            //config.KnownTypes.Add(typeof(ObjectType));
            config.KnownTypes.Add(typeof(Vector2));
            config.KnownTypes.Add(typeof(Point));
            config.KnownTypes.Add(typeof(Guid));
            config.KnownTypes.Add(typeof(Keys));



            config.KnownTypes.Add(typeof(bool));
            config.KnownTypes.Add(typeof(uint));
            config.KnownTypes.Add(typeof(string));
            config.KnownTypes.Add(typeof(ObjectType));
            config.KnownTypes.Add(typeof(DirectionX));
            config.KnownTypes.Add(typeof(DirectionY));
            //config.KnownTypes.Add(typeof(Dictionary<Guid, GameObject>));
            //config.KnownTypes.Add(typeof(Dictionary<Guid, Entity>));
            //config.KnownTypes.Add(typeof(Dictionary<string, object>));
            //config.KnownTypes.Add(typeof(Dictionary<Guid, Dictionary<string, object>>));


            config.ConfigType<Player>().ConstructBy(typeof(Player).GetConstructors()[0]);
            config.ConfigType<Enemy>().ConstructBy(typeof(Enemy).GetConstructors()[0]);
            config.ConfigType<Townsman>().ConstructBy(typeof(Townsman).GetConstructors()[0]);
            config.ConfigType<Bullet>().ConstructBy(typeof(Bullet).GetConstructors()[0]);
        }
    }
}
