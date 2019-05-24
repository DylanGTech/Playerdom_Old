﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared.Models;
using System;
using System.Collections.Generic;

namespace Playerdom.Shared.Objects
{
    public class Enemy : GameObject
    {
        public Enemy(Point position, Vector2 size, uint level = 1, uint xp = 0, uint speed = 4, bool isHalted = false, bool isSolid = true, uint health = 0, string displayName = "Enemy", ObjectType type = ObjectType.EnemyNPC, DirectionY facingDirectionY = DirectionY.Center, DirectionX facingDirectionX = DirectionX.Center, Guid? objectTalkingTo = null, decimal money = 0)
        {
            Position = position;
            IsSolid = isSolid;
            Size = size;
            Level = level;
            XP = xp;
            Speed = speed;
            if (health == 0) Health = MaxHealth;
            else Health = health;
            Type = type;
            IsHalted = isHalted;
            DisplayName = displayName;
            FacingDirectionX = facingDirectionX;
            FacingDirectionY = facingDirectionY;
            DialogText = "";
            ObjectTalkingTo = objectTalkingTo;
            Money = money;
        }

        public override void UpdateStats(GameObject o)
        {
            if (o.GetType() != typeof(Enemy))
                throw new Exception("Type to update must be the same type as the original");

            base.UpdateStats(o);
        }

        public override void LoadContent(ContentManager content, GraphicsDevice device)
        {
            ActiveTexture = content.Load<Texture2D>("enemy");

            base.LoadContent(content, device);
        }

        public override void Update(GameTime time, Map map, KeyboardState ks, Guid objectGuid)
        {
            //GameObject selectedEnemy;

            foreach(KeyValuePair<Guid, GameObject> o in map.gameObjects)
            {
                if (o.Value.GetType() != typeof(Player)) continue;
                var (x, y) = Distance(o.Value);
                if (Math.Abs(x) <= Tile.SizeX * 16 || Math.Abs(y) <= Tile.SizeY * 16)
                {
                    double angle = Math.Atan2(y, x);

                    Move((int)(-Speed * Math.Cos(angle)), (int)(-Speed * Math.Sin(angle)), map);
                }
                break;
            }

            base.Update(time, map, ks, objectGuid);
        }
    }
}
