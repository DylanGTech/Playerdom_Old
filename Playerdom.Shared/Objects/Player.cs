using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Services;
using Playerdom.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Playerdom.Shared.Models;

namespace Playerdom.Shared.Objects
{
    public class Player : GameObject
    {
        private DateTime bulletTimer;
        private DateTime dropTimer;


        public Player(Point position, Vector2 size, uint level = 1, uint xp = 0, uint speed = 8, bool isHalted = false, bool isSolid = true, uint health = 0, string displayName = "Player", ObjectType type = ObjectType.Player, DirectionY facingDirectionY = DirectionY.Center, DirectionX facingDirectionX = DirectionX.Center, bool isTalking = false, string dialogText = "", Guid? objectTalkingTo = null, decimal money = 0)
        {
            Position = position;
            IsSolid = isSolid;
            Size = size;
            Level = level;
            XP = xp;
            Speed = speed;
            if (health == 0) Health = MaxHealth;
            else Health = health; Type = type;
            IsHalted = isHalted;
            DisplayName = displayName;
            FacingDirectionX = facingDirectionX;
            FacingDirectionY = facingDirectionY;
            DialogText = dialogText;
            bulletTimer = DateTime.Now;
            dropTimer = DateTime.Now;
            IsTalking = isTalking;
            ObjectTalkingTo = objectTalkingTo;
            Money = money;
        }

        public override void UpdateStats(GameObject o)
        {
            if (o.GetType() != typeof(Player))
                throw new Exception("Type to update must be the same type as the original");

            bulletTimer = (o as Player).bulletTimer;


            base.UpdateStats(o);
        }

        public override void LoadContent(ContentManager content, GraphicsDevice device)
        {
            ActiveTexture = content.Load<Texture2D>("player");

            base.LoadContent(content, device);
        }
        public override void HandleCollision(GameObject otherObject, Map m)
        {
            if (otherObject.GetType() == typeof(Enemy))
            {
                TakeDamage(1, m);
            }
            base.HandleCollision(otherObject, m);
        }
        public override void HandleCollision(Entity entity, Map m)
        {
            base.HandleCollision(entity, m);
        }

        public override void Update(GameTime time, Map map, KeyboardState ks, Guid objectGuid)
        {
            Vector2 distance = new Vector2();

            if (ks.IsKeyDown(Keys.W))
            {
                distance.Y -= Speed;

            }
            if (ks.IsKeyDown(Keys.S))
            {
                distance.Y += Speed;
            }
            if (ks.IsKeyDown(Keys.A))
            {
                distance.X -= Speed;
            }
            if (ks.IsKeyDown(Keys.D))
            {
                distance.X += Speed;

            }
            if (distance.X != 0 || distance.Y != 0)
            {
                double angle = Math.Atan2(distance.Y, distance.X);

                Move((int)(Speed * Math.Cos(angle)), (int)(Speed * Math.Sin(angle)), map);
            }


            if (IsTalking)
            {
                if (ObjectTalkingTo != null && map.gameObjects.TryGetValue(ObjectTalkingTo.Value, out GameObject ott))
                {
                    Vector2 d = this.Distance(ott);
                    if (Math.Abs(d.X) <= Tile.SIZE_X * 2 || Math.Abs(d.Y) <= Tile.SIZE_Y * 2)
                    {

                    }
                    else
                    {
                        ott.ObjectTalkingTo = null;
                        ObjectTalkingTo = null;
                    }
                }
                else ObjectTalkingTo = null;
            }





            if (ks.IsKeyDown(Keys.F) && DateTime.Now >= bulletTimer)
            {
                if(!(FacingDirectionX == DirectionX.Center && FacingDirectionY == DirectionY.Center))
                {
                    Vector2 tragectory = new Vector2(0);
                    Point position = new Point(Position.X + (int)Size.X / 2 - 16, Position.Y + (int)Size.Y / 2 - 16);
                    switch (FacingDirectionX)
                    {
                        case DirectionX.Left:
                            tragectory.X -= 16;
                            position.X -= (int)Size.X;
                            break;
                        case DirectionX.Right:
                            tragectory.X += 16;
                            position.X += (int)Size.X;
                            break;
                    }
                    switch (FacingDirectionY)
                    {
                        case DirectionY.Up:
                            tragectory.Y -= 16;
                            position.Y -= (int)Size.Y;
                            break;
                        case DirectionY.Down:
                            tragectory.Y += 16;
                            position.Y += (int)Size.Y;
                            break;
                    }


                    map.gameEntities.TryAdd(Guid.NewGuid(), new Bullet(position, new Vector2(32, 32), tragectory, this));

                    bulletTimer = DateTime.Now.AddSeconds(0.85);
                }
            }



            if(ks.IsKeyDown(Keys.T) && !IsTalking)
            {
                bool talkingToObject = false;
                foreach(KeyValuePair<Guid, GameObject> go1 in map.gameObjects)
                {
                    if(go1.Key != objectGuid && go1.Value.IsTalking == false && (go1.Value.ObjectTalkingTo == null || go1.Value.ObjectTalkingTo == objectGuid))
                    {
                        Vector2 d = Distance(go1.Value);
                        if (Math.Abs(d.X) <= Tile.SIZE_X * 2 && Math.Abs(d.Y) <= Tile.SIZE_Y * 2)
                        {
                            talkingToObject = true;
                            go1.Value.StartConversation(new KeyValuePair<Guid, GameObject>(objectGuid, this), go1.Key);
                        }
                    }
                }
                if(!talkingToObject)
                {
                    ObjectTalkingTo = null;
                    Random r = new Random(DateTime.Now.Millisecond);
                    string text = "";
                    switch (r.Next(0, 8))
                    {
                        default:
                        case 0:
                            text = "Dylan would love it if I were to give him ideas.";
                            break;
                        case 1:
                            text = "Booooring!";
                            break;
                        case 2:
                            text = "I ought to level up.";
                            break;
                        case 3:
                            text = "Hmm, What should I do now?";
                            break;
                        case 4:
                            text = "I wish I were in a friendlier dimension.";
                            break;
                        case 5:
                            text = "What's the meaning of life?";
                            break;
                        case 6:
                            text = "Boy was that exciting!";
                            break;
                        case 7:
                            text = "I'm only in it for the money";
                            break;
                        case 8:
                            text = "Don't you dare hack me!";
                            break;
                    }

                    Task.Run(async () => await DisplayDialogAsync(text));
                }
            }

            if(ks.IsKeyDown(Keys.Q) && DateTime.Now > dropTimer)
            {

                if(Money >= 1)
                {
                    map.gameEntities.TryAdd(Guid.NewGuid(), new MoneyDrop(new Point((int)(Position.X + Size.X / 2), (int)(Position.Y + Size.Y / 2)), new Vector2(32, 32), 1, this));
                    Money -= 1;
                }
                else if(Money > 0)
                {
                    map.gameEntities.TryAdd(Guid.NewGuid(), new MoneyDrop(Position, new Vector2(32, 32), Money, this));
                    Money = 0;
                }
                else if(!IsTalking)
                    Task.Run(async () => await DisplayDialogAsync("Oh no! I'm out of money!"));
                dropTimer = DateTime.Now.AddSeconds(0.50);
            }

            base.Update(time, map, ks, objectGuid);
        }

        public override void Die(Map m)
        {
            Health = MaxHealth;
            Position = new Point(0, 0);
            //base.Die();
        }
    }
}
