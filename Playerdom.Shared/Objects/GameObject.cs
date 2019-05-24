﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared.Services;
using Playerdom.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Playerdom.Shared.Models;

namespace Playerdom.Shared.Objects
{
    public abstract class GameObject : IDisposable
    {
        public const float XP_PER_LEVEL = 10;
        public const float HEALTH_PER_LEVEL = 20;

        Texture2D rect;
        Texture2D background;
        Texture2D dialogTexture;
        Texture2D bar;

        SpriteFont font;

        public bool MarkedForDeletion;

        public bool isNew = true;

        public Guid? ObjectTalkingTo
        {
            get; set;
        }

        public Point Position { get; protected set; }

        public Texture2D ActiveTexture
        {
            get; protected set;
        }

        public bool IsSolid { get; protected set; }

        public bool IsHalted { get; protected set; }

        public decimal Money { get; protected set; }

        public Vector2 Size { get; protected set; }

        public string DisplayName { get; protected set; }

        //[JsonIgnore]
        public bool IsTalking { get; protected set; }

        //[JsonIgnore]
        protected string DialogText { get; set; }

        public Rectangle BoundingBox => new Rectangle(Position.X, Position.Y, (int)Size.X, (int)Size.Y);

        public uint MaxHealth => (uint)(HEALTH_PER_LEVEL * Level);

        public uint MaxXP => (uint)(XP_PER_LEVEL * Level);

        public uint XP { get; protected set; }

        public uint Health { get; protected set; }

        public uint Level { get; protected set; }

        public ObjectType Type { get; protected set; }

        public DirectionX FacingDirectionX { get; protected set; }

        public DirectionY FacingDirectionY { get; protected set; }

        public uint Speed { get; protected set; }

        protected bool isInvincible = false;
        protected DateTime wasLastHurt = DateTime.Now;

        public bool CanBeHurt => !isInvincible && DateTime.Compare(DateTime.Now, wasLastHurt.AddSeconds(1)) > 0;

        public virtual void LoadContent(ContentManager content, GraphicsDevice device)
        {
            rect = new Texture2D(device, 1, 1);
            background = new Texture2D(device, 1, 1);
            bar = new Texture2D(device, 1, 1);
            dialogTexture = new Texture2D(device, 1, 1);

            rect.SetData(new[] { Color.Black });
            bar.SetData(new[] { Color.Green });
            background.SetData(new[] { Color.Red });
            dialogTexture.SetData(new[] { new Color(255, 255, 255, 63) });

            font = content.Load<SpriteFont>("font2");
        }

        public async Task DisplayDialogAsync(string text, float seconds = 3.0F)
        {
            DialogText = text;
            IsTalking = true;

            await Task.Delay(TimeSpan.FromSeconds(seconds));
            IsTalking = false;
        }

        public virtual void Update(GameTime time, Map map, KeyboardState ks, Guid objectGuid)
        {
            if (ObjectTalkingTo == null) return;
            if (!map.gameObjects.TryGetValue(ObjectTalkingTo.Value, out GameObject go) || Math.Abs(Distance(go).Length()) > Tile.SizeX * 3)
                ObjectTalkingTo = null;

        }
        //public virtual void Draw(SpriteBatch spriteBatch, GraphicsDevice device, Microsoft.Xna.Framework.Vector2 centerOffset)
        //{
        //}

        public virtual void DrawTag(SpriteBatch spriteBatch, Vector2 centerOffset, RenderTarget2D target)
        {
            spriteBatch.DrawString(font, DisplayName + " [Lvl " + Level + "]", new Vector2((target.Width / 2) - (Size.X / 2) - centerOffset.X + (Size.X - font.MeasureString(DisplayName + " [Lvl " + Level + "]").X) / 2,
                (target.Height / 2) - (Size.Y / 2) - centerOffset.Y - font.MeasureString(DisplayName + " [Lvl " + Level + "]").Y - 16), Color.White);

            spriteBatch.Draw(rect, new Rectangle((int)((target.Width / 2) - (Size.X / 2) - centerOffset.X), (int)((target.Height / 2) - (Size.Y / 2) - centerOffset.Y) - 16, (int)Size.X, 20), Color.White);
            spriteBatch.Draw(background, new Rectangle((int)((target.Width / 2) - (Size.X / 2) - centerOffset.X) + 2, (int)((target.Height / 2) - (Size.Y / 2) - centerOffset.Y) + 2 - 16, (int)Size.X - 4, 16), Color.White);
            if (Health > 0)
                spriteBatch.Draw(bar, new Rectangle((int)((target.Width / 2) - (Size.X / 2) - centerOffset.X) + 2, (int)((target.Height / 2) - (Size.Y / 2) - centerOffset.Y) + 2 - 16, (int)((Size.X - 4) * Health / MaxHealth), 16), Color.White);

        }

        public virtual void DrawSprite(SpriteBatch spriteBatch, Vector2 centerOffset, RenderTarget2D target)
        {
            if (CanBeHurt || DateTime.Now.Ticks % 2 == 1)
                spriteBatch.Draw(ActiveTexture, new Rectangle((int)((target.Width / 2) - (Size.X / 2) - centerOffset.X), (int)((target.Height / 2) - (Size.Y / 2) - centerOffset.Y), (int)Size.X, (int)Size.Y), Color.White);
        }

        public virtual void DrawDialog(SpriteBatch spriteBatch, Vector2 centerOffset, RenderTarget2D target)
        {
            if (!IsTalking) return;
            spriteBatch.Draw(dialogTexture, new Rectangle((int)((target.Width / 2) - (Size.X / 2) - centerOffset.X + (Size.X - font.MeasureString(DialogText).X) / 2) - 6,
                (int)((target.Height / 2) - (Size.Y / 2) - centerOffset.Y - font.MeasureString(DialogText).Y - 12 - 60), (int)font.MeasureString(DialogText).X + 12, (int)font.MeasureString(DialogText).Y + 12), Color.White);

            spriteBatch.DrawString(font, DialogText, new Vector2((target.Width / 2) - (Size.X / 2) - centerOffset.X + (Size.X - font.MeasureString(DialogText).X) / 2,
                (target.Height / 2) - (Size.Y / 2) - centerOffset.Y - font.MeasureString(DialogText).Y - 4 - 60), Color.Black);
        }

        public virtual void Move(int xOffset, int yOffset, Map map)
        {
            if (xOffset == 0) FacingDirectionX = DirectionX.Center;
            else if (xOffset > 0) FacingDirectionX = DirectionX.Right;
            else if (xOffset < 0) FacingDirectionX = DirectionX.Left;

            if (yOffset == 0) FacingDirectionY = DirectionY.Center;
            else if (yOffset > 0) FacingDirectionY = DirectionY.Down;
            else if (yOffset < 0) FacingDirectionY = DirectionY.Up;

            if (!IsHalted)
            {
                CollisionService.MoveWithTileCollision(this, map, new Vector2((float)xOffset, (float)yOffset));
            }
        }

        public virtual void ChangePosition(int xOffset, int yOffset)
        {
            Point newPosition = new Point(Position.X + xOffset, Position.Y + yOffset);

            if (newPosition.X > Map.SizeX * Tile.SizeX) newPosition.X = (int)(Map.SizeX * Tile.SizeX - Size.X);
            else if (newPosition.X < 0) newPosition.X = 0;

            if (newPosition.Y > Map.SizeY * Tile.SizeY) newPosition.Y = (int)(Map.SizeY * Tile.SizeY - Size.Y);
            else if (newPosition.Y < 0) newPosition.Y = 0;

            Position = newPosition;
        }

        public virtual void Heal(uint points)
        {
            if (Health + points > MaxHealth) Health = MaxHealth;
            else Health += points;
        }

        public virtual void TakeDamage(uint points, Map m, GameObject causer = null)
        {
            if (!CanBeHurt) return;
            if ((int)Health - (int)points <= 0)
            {
                Health = 0;
                Die(m);
                if (causer != null && causer != this)
                {
                    causer.ChangeXP((int)Level * 4);
                }
            }
            else Health -= points;
            wasLastHurt = DateTime.Now;
        }

        public virtual void ChangeHealth(int offset)
        {
            if (offset + Health < Health)
            {
                Health = 0;
            }
            else if (offset + Health > MaxHealth)
            {
                Health = MaxHealth;
            }
            else Health = (uint)(Health + offset);
        }

        public virtual void ChangeXP(int offset)
        {
            if (offset + XP < XP)
            {
                XP = 0;
            }
            else if (offset + XP > MaxXP)
            {
                XP = 0;
                Level++;
            }
            else XP = (uint)(XP + offset);
        }

        public virtual void Die(Map m)
        {
            MarkedForDeletion = true;
        }

        public Vector2 Distance(GameObject otherObject)
        {
            return new Vector2((this.Position.X + Size.X / 2) - (otherObject.Position.X + otherObject.Size.X / 2), (this.Position.Y + Size.Y / 2) - (otherObject.Position.Y + otherObject.Size.Y / 2));
        }

        public Vector2 Distance(Entity entity)
        {
            return new Vector2((this.Position.X + Size.X / 2) - (entity.Position.X + entity.Size.X / 2), (this.Position.Y + Size.Y / 2) - (entity.Position.Y + entity.Size.Y / 2));
        }

        public bool CheckCollision(GameObject otherObject)
        {
            return BoundingBox.Intersects(otherObject.BoundingBox);
        }

        public virtual void HandleCollision(GameObject otherObject, Map m)
        {

        }

        public virtual void HandleCollision(Entity entity, Map m)
        {
            if (entity.GetType() == typeof(Bullet))
            {
                TakeDamage(5, m, (entity as Bullet)?.Sender);
                entity.MarkedForDeletion = true;
            }
            else if (entity.GetType() == typeof(MoneyDrop) && (entity as MoneyDrop)?.Dropper != this)
            {
                Money += ((MoneyDrop)entity).MoneyContained;
                entity.MarkedForDeletion = true;
            }
        }

        public void Dispose()
        {
            if (ActiveTexture == null) return;
            //ActiveTexture.Dispose();
            rect.Dispose();
            background.Dispose();
            dialogTexture.Dispose();
            bar.Dispose();
        }

        public virtual void UpdateStats(GameObject o)
        {
            Position = o.Position;
            IsSolid = o.IsSolid;
            Size = o.Size;
            Level = o.Level;
            XP = o.XP;
            Speed = o.Speed;
            Health = o.Health;
            Type = o.Type;
            IsHalted = o.IsHalted;
            DisplayName = o.DisplayName;
            FacingDirectionX = o.FacingDirectionX;
            FacingDirectionY = o.FacingDirectionY;
            IsTalking = o.IsTalking;
            DialogText = o.DialogText;
            ObjectTalkingTo = o.ObjectTalkingTo;
            Money = o.Money;
        }

        public bool TransferMoney(decimal amount, GameObject fromObject = null)
        {
            if (Money + amount < 0) return false;

            if (fromObject == null)
                Money += amount;
            else if (fromObject.Money - amount < 0) return false;
            else
            {
                Money += amount;
                fromObject.Money -= amount;
            }

            return true;
        }

        public virtual void StartConversation(KeyValuePair<Guid, GameObject> otherObject, Guid thisObjectId)
        {

        }

        public virtual void SetDisplayName(string newName)
        {
            DisplayName = newName;
        }
    }

    public enum ObjectType : byte
    {
        Player,
        HelpfulNPC,
        EnemyNPC,
        NeutralNPC
    }

    public enum DirectionY : byte
    {
        Up,
        Center,
        Down
    }
    public enum DirectionX : byte
    {
        Left,
        Center,
        Right
    }
}
