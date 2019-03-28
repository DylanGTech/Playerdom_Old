using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Playerdom.Shared.Entities
{
    public class Entity : IDisposable
    {
        public bool MarkedForDeletion = false;



        [JsonConverter(typeof(XnaConverter))]
        public Point Position
        {
            get; protected set;
        }

        [JsonIgnore]
        public Texture2D ActiveTexture
        {
            get; protected set;
        }
        [JsonIgnore]
        public Rectangle BoundingBox
        {
            get
            {
                return new Rectangle(Position.X, Position.Y, (int)Size.X, (int)Size.Y);
            }
        }



        public bool IsHalted
        {
            get; protected set;
        }

        [JsonConverter(typeof(XnaConverter))]
        public Microsoft.Xna.Framework.Vector2 Size
        {
            get; protected set;
        }

        public virtual void LoadContent(ContentManager content, GraphicsDevice device)
        {

        }


        public virtual void Update(GameTime time, Map map)
        {

        }
        public virtual void Draw(SpriteBatch spriteBatch, GraphicsDevice device, Microsoft.Xna.Framework.Vector2 centerOffset)
        {

        }

        public virtual void Move(int xOffset, int yOffset, Map map)
        {
            if (!IsHalted)
            {
                CollisionService.MoveWithTileCollision(this, map, new Microsoft.Xna.Framework.Vector2((float)xOffset, (float)yOffset));
            }
        }
        public virtual void ChangePosition(int xOffset, int yOffset)
        {
            Point newPosition = new Point(Position.X + xOffset, Position.Y + yOffset);

            if (newPosition.X > Map.SIZE_X * Tile.SIZE_X) newPosition.X = (int)(Map.SIZE_X * Tile.SIZE_X - Size.X);
            else if (newPosition.X < 0) newPosition.X = 0;

            if (newPosition.Y > Map.SIZE_Y * Tile.SIZE_Y) newPosition.Y = (int)(Map.SIZE_Y * Tile.SIZE_Y - Size.Y);
            else if (newPosition.Y < 0) newPosition.Y = 0;

            Position = newPosition;
        }

        public void Dispose()
        {
            if(ActiveTexture != null)
                ActiveTexture.Dispose();
        }

        public virtual void UpdateStats(Entity e)
        {
            Position = e.Position;
        }
    }
}
