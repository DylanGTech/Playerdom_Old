using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Services;
using Playerdom.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Playerdom.Shared.Entities
{

    public class Bullet : Entity
    {
        public GameObject Sender
        {
            get; private set;
        } = null;

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
                Position.X + Size.X >= Map.SIZE_X * Tile.SIZE_X ||
                Position.Y + Size.Y >= Map.SIZE_Y * Tile.SIZE_Y)
                MarkedForDeletion = true;
        }
        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice device, Microsoft.Xna.Framework.Vector2 centerOffset, RenderTarget2D target)
        {
            spriteBatch.Draw(ActiveTexture, new Rectangle((int)((target.Width / 2) - (Size.X / 2) - centerOffset.X), (int)((target.Height / 2) - (Size.Y / 2) - centerOffset.Y), (int)Size.X, (int)Size.Y), Color.Red);
        }

        public override void UpdateStats(Entity e)
        {
            if (e.GetType() != typeof(Bullet))
                throw new Exception("Type to update must be the same type as the original");

            Velocity = (e as Bullet).Velocity;
            base.UpdateStats(e);
        }
    }
}
