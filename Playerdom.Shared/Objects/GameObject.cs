using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playerdom.Shared.Services;
using Playerdom.Shared;
using Playerdom.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ceras;
using System.Reflection;

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

        public bool MarkedForDeletion = false;

        public bool isNew = true;


        protected Point _position;
        [JsonConverter(typeof(XnaConverter))]
        public Point Position
        {
            get { return _position; }
            protected set
            {
                _position = value;
                AddDelta<Point>("Position");
            }
        }

        [JsonIgnore]
        public Texture2D ActiveTexture
        {
            get; protected set;
        }

        protected bool _isSolid;
        public bool IsSolid
        {
            get { return _isSolid; }
            protected set
            {
                _isSolid = value;
                AddDelta<bool>("IsSolid");
            }
        }

        protected bool _isHalted;
        public bool IsHalted
        {
            get { return _isHalted; }
            protected set
            {
                _isHalted = value;
                AddDelta<bool>("IsHalted");
            }
        }

        protected Vector2 _size;
        [JsonConverter(typeof(XnaConverter))]
        public Microsoft.Xna.Framework.Vector2 Size
        {
            get { return _size; }
            protected set
            {
                _size = value;
                AddDelta<Vector2>("Size");
            }
        }

        protected string _displayName;
        public string DisplayName
        {
            get { return _displayName; }
            protected set
            {
                _displayName = value;
                AddDelta<string>("DisplayName");
            }
        }

        protected bool _isTalking;
        //[JsonIgnore]
        public bool IsTalking
        {
            get { return _isTalking; }
            protected set
            {
                _isTalking = value;
                AddDelta<bool>("IsTalking");
            }
        }

        protected string _dialogText;
        //[JsonIgnore]
        public string DialogText
        {
            get { return _dialogText; }
            protected set
            {
                _dialogText = value;
                AddDelta<string>("DialogText");
            }
        }

        [JsonIgnore]
        public Rectangle BoundingBox
        {
            get
            {
                return new Rectangle(Position.X, Position.Y, (int)Size.X, (int)Size.Y);
            }
        }

        [JsonIgnore]
        public uint MaxHealth
        {
            get
            {
                return (uint)(HEALTH_PER_LEVEL * Level);
            }
        }

        [JsonIgnore]
        public uint MaxXP
        {
            get
            {
                return (uint)(XP_PER_LEVEL * Level);
            }
        }

        protected uint _xp;
        public uint XP
        {
            get { return _xp; }
            protected set
            {
                _xp = value;
                AddDelta<uint>("XP");
            }
        }

        protected uint _health;
        public uint Health
        {
            get { return _health; }
            protected set
            {
                _health = value;
                AddDelta<uint>("Health");
            }
        }

        protected uint _level;
        public uint Level
        {
            get { return _level; }
            protected set
            {
                _level = value;
                AddDelta<uint>("Level");
            }
        }

        protected ObjectType _type;
        public ObjectType Type
        {
            get { return _type; }
            protected set
            {
                _type = value;
                AddDelta<ObjectType>("Type");
            }
        }

        protected DirectionX _facingDirectionX;
        public DirectionX FacingDirectionX
        {
            get { return _facingDirectionX; }
            protected set
            {
                _facingDirectionX = value;
                AddDelta<DirectionX>("FacingDirectionX");
            }
        }

        protected DirectionY _facingDirectionY;
        public DirectionY FacingDirectionY
        {
            get { return _facingDirectionY; }
            protected set
            {
                _facingDirectionY = value;
                AddDelta<DirectionY>("FacingDirectionY");
            }
        }

        private uint _speed;
        public uint Speed
        {
            get { return _speed; }
            protected set
            {
                _speed = value;
                AddDelta<uint>("Speed");
            }
        }


        protected bool isInvincible = false;
        protected DateTime wasLastHurt = DateTime.Now;

        [JsonIgnore]
        public bool CanBeHurt
        {
            get
            {
                return !isInvincible && DateTime.Compare(DateTime.Now, wasLastHurt.AddSeconds(1)) > 0;
            }
        }


        public virtual void LoadContent(ContentManager content, GraphicsDevice device)
        {
            rect = new Texture2D(device, 1, 1);
            background = new Texture2D(device, 1, 1);
            bar = new Texture2D(device, 1, 1);
            dialogTexture = new Texture2D(device, 1, 1);

            rect.SetData(new[] { Color.Black });
            bar.SetData(new[] { Color.Green });
            background.SetData(new[] { Color.Red });
            dialogTexture.SetData(new[] { new Color(255, 255, 255, 63)});

            font = content.Load<SpriteFont>("font2");
        }

        public async Task DisplayDialogAsync(string text, float seconds = 3.0F)
        {
            DialogText = text;
            IsTalking = true;

            await Task.Delay(TimeSpan.FromSeconds(seconds));
            IsTalking = false;
        }

        public virtual void Update(GameTime time, Map map, KeyboardState ks)
        {

        }
        public virtual void Draw(SpriteBatch spriteBatch, GraphicsDevice device, Microsoft.Xna.Framework.Vector2 centerOffset)
        {
            if (CanBeHurt || DateTime.Now.Ticks % 2 == 1)
                spriteBatch.Draw(ActiveTexture, new Rectangle((int)((device.PresentationParameters.BackBufferWidth / 2) - (Size.X / 2) - centerOffset.X), (int)((device.PresentationParameters.BackBufferHeight / 2) - (Size.Y / 2) - centerOffset.Y), (int)Size.X, (int)Size.Y), Color.White);

            spriteBatch.DrawString(font, DisplayName + " [Lvl " + Level + "]", new Microsoft.Xna.Framework.Vector2((device.PresentationParameters.BackBufferWidth / 2) - (Size.X / 2) - centerOffset.X + (Size.X - font.MeasureString(DisplayName + " [Lvl " + Level + "]").X) / 2,
                (device.PresentationParameters.BackBufferHeight / 2) - (Size.Y / 2) - centerOffset.Y - font.MeasureString(DisplayName +" [Lvl " + Level + "]").Y - 16), Color.White);

            spriteBatch.Draw(rect, new Rectangle((int)((device.PresentationParameters.BackBufferWidth / 2) - (Size.X / 2) - centerOffset.X), (int)((device.PresentationParameters.BackBufferHeight / 2) - (Size.Y / 2) - centerOffset.Y) - 16, (int)Size.X, 20), Color.White);
            spriteBatch.Draw(background, new Rectangle((int)((device.PresentationParameters.BackBufferWidth / 2) - (Size.X / 2) - centerOffset.X) + 2, (int)((device.PresentationParameters.BackBufferHeight / 2) - (Size.Y / 2) - centerOffset.Y) + 2 - 16, (int)Size.X - 4, 16), Color.White);
            if (Health > 0)
                spriteBatch.Draw(bar, new Rectangle((int)((device.PresentationParameters.BackBufferWidth / 2) - (Size.X / 2) - centerOffset.X) + 2, (int)((device.PresentationParameters.BackBufferHeight / 2) - (Size.Y / 2) - centerOffset.Y) + 2 - 16, (int)((Size.X - 4) * Health / MaxHealth), 16), Color.White);

            if (IsTalking)
            {
                spriteBatch.Draw(dialogTexture, new Rectangle((int)((device.PresentationParameters.BackBufferWidth / 2) - (Size.X / 2) - centerOffset.X + (Size.X - font.MeasureString(DialogText).X) / 2) - 6,
                    (int)((device.PresentationParameters.BackBufferHeight / 2) - (Size.Y / 2) - centerOffset.Y - font.MeasureString(DialogText).Y - 12 - 60), (int)font.MeasureString(DialogText).X + 12, (int)font.MeasureString(DialogText).Y + 12), Color.White);

                spriteBatch.DrawString(font, DialogText, new Vector2((device.PresentationParameters.BackBufferWidth / 2) - (Size.X / 2) - centerOffset.X + (Size.X - font.MeasureString(DialogText).X) / 2,
                        (device.PresentationParameters.BackBufferHeight / 2) - (Size.Y / 2) - centerOffset.Y - font.MeasureString(DialogText).Y - 4 - 60), Color.Black);

            }
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


        public virtual void Heal(uint points)
        {
            if (Health + points > MaxHealth) Health = MaxHealth;
            else Health = Health + points;
        }

        public virtual void TakeDamage(uint points, Map m, GameObject causer = null)
        {
            if (CanBeHurt)
            {
                if ((int)Health - (int)points <= 0)
                {
                    Health = 0;
                    Die(m);
                    if(causer != null)
                    {
                        causer.ChangeXP((int)Level * 4);
                    }
                }
                else Health = Health - points;
                wasLastHurt = DateTime.Now;
            }
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
            else if (offset + XP > MaxHealth)
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

        public Microsoft.Xna.Framework.Vector2 Distance(GameObject otherObject)
        {
            return new Microsoft.Xna.Framework.Vector2((this.Position.X + Size.X / 2) - (otherObject.Position.X + otherObject.Size.X / 2), (this.Position.Y + Size.Y / 2) - (otherObject.Position.Y + otherObject.Size.Y / 2));
        }

        public Microsoft.Xna.Framework.Vector2 Distance(Entity entity)
        {
            return new Microsoft.Xna.Framework.Vector2((this.Position.X + Size.X / 2) - (entity.Position.X + entity.Size.X / 2), (this.Position.Y + Size.Y / 2) - (entity.Position.Y + entity.Size.Y / 2));
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
                TakeDamage(5, m, (entity as Bullet).Sender);
                entity.MarkedForDeletion = true;
            }
        }

        public void Dispose()
        {
            if(ActiveTexture != null)
            {
                //ActiveTexture.Dispose();
                rect.Dispose();
                background.Dispose();
                dialogTexture.Dispose();
                bar.Dispose();
            }
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
        }


        private Dictionary<string,byte[]> delta = new Dictionary<string,byte[]>();

        public Dictionary<string, byte[]> GetDelta()
        {
            return delta;
        }

        public void ResetDelta()
        {
            isNew = false;
            delta.Clear();
        }
        public void AddDelta<T>(string propertyName)
        {
            /*
            CerasSerializer serializer = new CerasSerializer(PlayerdomCerasSettings.config);

            //Type propType = this.GetType().GetProperty(propertyName).PropertyType;
            //MethodInfo serializeMethod = typeof(CerasSerializer).GetMethods().Where(x => x.Name == "Serialize" && x.ContainsGenericParameters && x.GetParameters().Count() == 3).First();


            //MethodInfo generic = serializeMethod.MakeGenericMethod(propType);
            //byte[] changes = null;
            //int length = (int)generic.Invoke(serializer, new object[] { (T)this.GetType().GetProperty(propertyName).GetValue(this), changes, 0 });

            byte[] changes = serializer.Serialize<T>((T)this.GetType().GetProperty(propertyName).GetValue(this));

            if (delta.Keys.Contains(propertyName))
                delta[propertyName] = changes;
            else delta.Add(propertyName, changes);
            */
        }

        public void ApplyDelta(Dictionary<string, byte[]> delta, CerasSerializer serializer)
        {
            if(delta.Count > 0)
            {
                MethodInfo deserializeMethod = typeof(CerasSerializer).GetMethod("Deserialize", new Type[] { typeof(byte[]) });
                foreach (KeyValuePair<string, byte[]> pair in delta)
                {
                    MethodInfo generic = deserializeMethod.MakeGenericMethod(this.GetType().GetProperty(pair.Key).PropertyType);
                    this.GetType().GetProperty(pair.Key).SetValue(this, generic.Invoke(this, new object[] { pair.Value }));
                }
            }
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
