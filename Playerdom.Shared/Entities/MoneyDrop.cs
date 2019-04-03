using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Playerdom.Shared.Entities
{
    public class MoneyDrop : Entity
    {
        public GameObject Dropper
        {
            get; private set;
        } = null;

        public decimal MoneyContained
        {
            get; private set;
        } = 0;



        public MoneyDrop(Point position, Vector2 size, decimal moneyContained, GameObject dropper = null)
        {
            Position = position;
            Size = size;
            MoneyContained = moneyContained;
            Dropper = dropper;
        }

        public override void LoadContent(ContentManager content, GraphicsDevice device)
        {

            ActiveTexture = content.Load<Texture2D>("ruppie");

            base.LoadContent(content, device);
        }


        public override void Update(GameTime time, Map map)
        {

        }
        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice device, Microsoft.Xna.Framework.Vector2 centerOffset, RenderTarget2D target)
        {
            spriteBatch.Draw(ActiveTexture, new Rectangle((int)((target.Width / 2) - (Size.X / 2) - centerOffset.X), (int)((target.Height / 2) - (Size.Y / 2) - centerOffset.Y), (int)Size.X, (int)Size.Y), Color.White);
        }

        public override void UpdateStats(Entity e)
        {
            if (e.GetType() != typeof(MoneyDrop))
                throw new Exception("Type to update must be the same type as the original");

            MoneyContained = ((MoneyDrop)e).MoneyContained;
            base.UpdateStats(e);
        }
    }
}
