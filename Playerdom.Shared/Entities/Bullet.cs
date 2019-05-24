﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Playerdom.Shared.Objects;
using System;
using Playerdom.Shared.Models;

namespace Playerdom.Shared.Entities
{
    public class Bullet : Entity
    {
        public GameObject Sender
        {
            get;
        }

        public Vector2 Velocity
        {
            get; private set;
        }

        public Bullet(Point position, Vector2 size, Vector2 velocity, GameObject sender = null)
        {
            Size = size;
            Position = position;
            IsHalted = false;
            Velocity = velocity;
            Sender = sender;
        }

        public override void LoadContent(ContentManager content, GraphicsDevice device)
        {
            Texture2D tmp = new Texture2D(device, 1, 1);

            tmp.SetData(new[] { Color.White });

            ActiveTexture = tmp;

            base.LoadContent(content, device);
        }

        public override void Update(GameTime time, Map map)
        {
            Move((int)Velocity.X, (int)Velocity.Y, map);

            if (Position.X <= 0 || Position.Y <= 0 ||
                Position.X + Size.X >= Map.SizeX * Tile.SizeX ||
                Position.Y + Size.Y >= Map.SizeY * Tile.SizeY)
                MarkedForDeletion = true;
        }

        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice device, Vector2 centerOffset, RenderTarget2D target)
        {
            spriteBatch.Draw(ActiveTexture, new Rectangle((int)(target.Width / 2 - (Size.X / 2) - centerOffset.X), (int)(target.Height / 2 - (Size.Y / 2) - centerOffset.Y), (int)Size.X, (int)Size.Y), Color.Red);
        }

        public override void UpdateStats(Entity e)
        {
            if (e.GetType() != typeof(Bullet))
                throw new Exception("Type to update must be the same type as the original");

            Velocity = ((Bullet)e).Velocity;
            base.UpdateStats(e);
        }
    }
}
