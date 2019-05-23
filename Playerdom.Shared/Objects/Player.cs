using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Playerdom.Shared.Models;

namespace Playerdom.Shared.Objects
{
    public class Player : GameObject
    {
        private DateTime _bulletTimer;
        private DateTime _dropTimer;
        private readonly Random _rnd;

        public Player(Point position, Vector2 size, uint level = 1, uint xp = 0, uint speed = 8, bool isHalted = false, bool isSolid = true, uint health = 0, string displayName = "Player", ObjectType type = ObjectType.Player, DirectionY facingDirectionY = DirectionY.Center, DirectionX facingDirectionX = DirectionX.Center, bool isTalking = false, string dialogText = "", Guid? objectTalkingTo = null, decimal money = 0)
        {
            _rnd = new Random(DateTime.Now.Millisecond);
            Position = position;
            IsSolid = isSolid;
            Size = size;
            Level = level;
            XP = xp;
            Speed = speed;
            Health = health == 0 ? MaxHealth : health; Type = type;
            IsHalted = isHalted;
            DisplayName = displayName;
            FacingDirectionX = facingDirectionX;
            FacingDirectionY = facingDirectionY;
            DialogText = dialogText;
            _bulletTimer = DateTime.Now;
            _dropTimer = DateTime.Now;
            IsTalking = isTalking;
            ObjectTalkingTo = objectTalkingTo;
            Money = money;
        }

        public override void UpdateStats(GameObject o)
        {
            if (o.GetType() != typeof(Player))
                throw new Exception("Type to update must be the same type as the original");

            _bulletTimer = ((Player) o)._bulletTimer;

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

        public override void Update(GameTime time, Map map, KeyboardState ks, Guid objectGuid)
        {
            var (f, f1) = new Vector2();

            if (ks.IsKeyDown(Keys.W))
            {
                f1 -= Speed;

            }
            if (ks.IsKeyDown(Keys.S))
            {
                f1 += Speed;
            }
            if (ks.IsKeyDown(Keys.A))
            {
                f -= Speed;
            }
            if (ks.IsKeyDown(Keys.D))
            {
                f += Speed;

            }
            if (f != 0 || f1 != 0)
            {
                double angle = Math.Atan2(f1, f);

                Move((int)(Speed * Math.Cos(angle)), (int)(Speed * Math.Sin(angle)), map);
            }

            if (IsTalking)
            {
                if (ObjectTalkingTo != null && map.gameObjects.TryGetValue(ObjectTalkingTo.Value, out GameObject ott))
                {
                    var (x, y) = Distance(ott);
                    if (!(Math.Abs(x) <= Tile.SIZE_X * 2) && !(Math.Abs(y) <= Tile.SIZE_Y * 2))
                    {
                        ott.ObjectTalkingTo = null;
                        ObjectTalkingTo = null;
                    }
                }
                else ObjectTalkingTo = null;
            }

            if (ks.IsKeyDown(Keys.F) && DateTime.Now >= _bulletTimer)
            {
                if(!(FacingDirectionX == DirectionX.Center && FacingDirectionY == DirectionY.Center))
                {
                    Vector2 trajectory = new Vector2(0);
                    Point position = new Point(Position.X + (int)Size.X / 2 - 16, Position.Y + (int)Size.Y / 2 - 16);
                    switch (FacingDirectionX)
                    {
                        case DirectionX.Left:
                            trajectory.X -= 16;
                            position.X -= (int)Size.X;
                            break;
                        case DirectionX.Right:
                            trajectory.X += 16;
                            position.X += (int)Size.X;
                            break;
                    }
                    switch (FacingDirectionY)
                    {
                        case DirectionY.Up:
                            trajectory.Y -= 16;
                            position.Y -= (int)Size.Y;
                            break;
                        case DirectionY.Down:
                            trajectory.Y += 16;
                            position.Y += (int)Size.Y;
                            break;
                    }
                    
                    map.gameEntities.TryAdd(Guid.NewGuid(), new Bullet(position, new Vector2(32, 32), trajectory, this));
                    _bulletTimer = DateTime.Now.AddSeconds(0.85);
                }
            }
            
            if(ks.IsKeyDown(Keys.T) && !IsTalking)
            {
                bool talkingToObject = false;
                foreach(KeyValuePair<Guid, GameObject> go1 in map.gameObjects)
                {
                    if (go1.Key == objectGuid || go1.Value.IsTalking ||
                        go1.Value.ObjectTalkingTo != null && go1.Value.ObjectTalkingTo != objectGuid) continue;
                    var (x, y) = Distance(go1.Value);
                    if (!(Math.Abs(x) <= Tile.SIZE_X * 2) || !(Math.Abs(y) <= Tile.SIZE_Y * 2)) continue;
                    talkingToObject = true;
                    go1.Value.StartConversation(new KeyValuePair<Guid, GameObject>(objectGuid, this), go1.Key);
                }
                if(!talkingToObject)
                {
                    ObjectTalkingTo = null;
                    string text;
                    switch (_rnd.Next(0, 8))
                    {
                        // Put strings into resource files, that way you can make it multi-lingual
                        default:
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

            if(ks.IsKeyDown(Keys.Q) && DateTime.Now > _dropTimer)
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
                _dropTimer = DateTime.Now.AddSeconds(0.50);
            }

            base.Update(time, map, ks, objectGuid);
        }

        public override void Die(Map m)
        {
            Health = MaxHealth;
            Position = MapService.GenerateSpawnPoint(m);
            //base.Die();
        }
    }
}
