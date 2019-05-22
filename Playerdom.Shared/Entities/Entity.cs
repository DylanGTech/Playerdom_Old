using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Playerdom.Shared.Models;
using Playerdom.Shared.Services;
using System;

namespace Playerdom.Shared.Entities
{
    public class Entity : IDisposable
    {
        public bool MarkedForDeletion = false;

        public Point Position
        {
            get; protected set;
        }

        public Texture2D ActiveTexture
        {
            get; protected set;
        }
        public Rectangle BoundingBox => new Rectangle(Position.X, Position.Y, (int)Size.X, (int)Size.Y);

        public bool IsHalted
        {
            get; protected set;
        }

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

        public virtual void Draw(SpriteBatch spriteBatch, GraphicsDevice device, Microsoft.Xna.Framework.Vector2 centerOffset, RenderTarget2D target)
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

            if (newPosition.X > Map.SizeX * Tile.SIZE_X) newPosition.X = (int)(Map.SizeX * Tile.SIZE_X - Size.X);
            else if (newPosition.X < 0) newPosition.X = 0;

            if (newPosition.Y > Map.SizeY * Tile.SIZE_Y) newPosition.Y = (int)(Map.SizeY * Tile.SIZE_Y - Size.Y);
            else if (newPosition.Y < 0) newPosition.Y = 0;

            Position = newPosition;
        }

        public void Dispose()
        {
            ActiveTexture?.Dispose();
        }

        public virtual void UpdateStats(Entity e)
        {
            Position = e.Position;
        }
    }
}
