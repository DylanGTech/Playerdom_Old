using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Playerdom.Shared.Objects
{
    public class Townsman : GameObject
    {

        public Townsman(Point position, Vector2 size, uint level = 1, uint xp = 0, uint speed = 6, bool isHalted = false, bool isSolid = true, uint health = 0, string displayName = "Townsman", ObjectType type = ObjectType.NeutralNPC, DirectionY facingDirectionY = DirectionY.Center, DirectionX facingDirectionX = DirectionX.Center, bool isTalking = false, string dialogText = "")
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
            IsTalking = isTalking;
        }

        public override void UpdateStats(GameObject o)
        {
            if (o.GetType() != typeof(Townsman))
                throw new Exception("Type to update must be the same type as the original");

            base.UpdateStats(o);
        }
        public override void LoadContent(ContentManager content, GraphicsDevice device)
        {
            ActiveTexture = content.Load<Texture2D>("townsman");

            base.LoadContent(content, device);
        }
        public override void Update(GameTime time, Map map, KeyboardState ks)
        {
            if(IsTalking == false)
            {
                foreach (KeyValuePair<Guid, GameObject> o in map.gameObjects)
                {
                    if (o.Value.GetType() == typeof(Player))
                    {
                        Vector2 distance = Distance(o.Value);
                        if (Math.Abs(distance.X) <= Tile.SIZE_X * 4 || Math.Abs(distance.Y) <= Tile.SIZE_Y * 4)
                        {
                            Task.Run(() => DisplayDialogAsync("Hello " + o.Value.DisplayName + "!", 3));
                        }
                        break;
                    }
                }
            }

            base.Update(time, map, ks);
        }
    }
}
