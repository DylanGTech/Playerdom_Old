using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playerdom.Shared.GUIs
{
    public class ButtonObject
    {
        public Vector2 Offset { get; private set; }
        public Vector2 Size { get; private set; }

        GuiAnchorPoint anchor;

        protected Action action;
        protected string text;
        protected SpriteFont font;
        protected GraphicsDevice hostDevice;
        protected Color borderColor;
        protected Color backgroundColor;
        protected Color textColor;
        protected int borderThickness;
        
        public Texture2D Background
        {
            get; protected set;
        } = null;

        public Texture2D Border
        {
            get; protected set;
        } = null;


        public ButtonObject(Vector2 offset, Vector2 size, string text, Action action,
            GuiAnchorPoint anchor, GraphicsDevice graphicsDevice, Color backgroundColor, Color borderColor, Color textColor, int borderThickness)
        {
            Offset = offset;
            Size = size;
            this.action = action;
            this.anchor = anchor;
            this.text = text;
            this.backgroundColor = backgroundColor;
            this.borderColor = borderColor;
            this.textColor = textColor;
            this.borderThickness = borderThickness;
            hostDevice = graphicsDevice;
        }


        public void LoadContent(ContentManager content, GraphicsDevice device)
        {
            Background = new Texture2D(device, 1, 1);
            Border = new Texture2D(device, 1, 1);
            font = content.Load<SpriteFont>("font2");

            Background.SetData(new[] { backgroundColor });
            Border.SetData(new[] { borderColor });
        }

        public void Update(GameTime time)
        {
            MouseState ms = Mouse.GetState();

            
            if(ms.LeftButton == ButtonState.Pressed)
            {
                if (ms.Position.X > Position.X
                    && ms.Position.X < Position.X + Size.X
                    && ms.Position.Y > Position.Y
                    && ms.Position.Y < Position.Y + Size.Y)
                {
                    action();
                }
            }
        }

        public Rectangle BoundingBox
        {
            get
            {
                return new Rectangle(new Point((int)Position.X, (int)Position.Y), new Point((int)Size.X, (int)Size.Y));
            }
        }



        public void Draw(SpriteBatch spriteBatch, GraphicsDevice device)
        {
            spriteBatch.Draw(Border, BoundingBox, Color.White);
            spriteBatch.Draw(Background, new Rectangle(BoundingBox.X + borderThickness, BoundingBox.Y + borderThickness, BoundingBox.Size.X - borderThickness * 2, BoundingBox.Size.Y - borderThickness * 2), Color.White);

            Vector2 textPosition = new Vector2(Position.X - font.MeasureString(text).X / 2 + BoundingBox.Width / 2,
                Position.Y - font.MeasureString(text).Y / 2 + BoundingBox.Height / 2);



            spriteBatch.DrawString(font, text, textPosition, Color.Black);
        }



        Vector2 Position
        {
            get
            {
                Vector2 position;
                switch (anchor)
                {
                    default:
                        position.Y = hostDevice.PresentationParameters.BackBufferHeight / 2 + Offset.Y - Size.Y / 2;
                        break;

                    case GuiAnchorPoint.Top:
                    case GuiAnchorPoint.TopLeft:
                    case GuiAnchorPoint.TopRight:
                        position.Y = 0 + Offset.Y;
                        break;

                    case GuiAnchorPoint.Bottom:
                    case GuiAnchorPoint.BottomLeft:
                    case GuiAnchorPoint.BottomRight:
                        position.Y = hostDevice.PresentationParameters.BackBufferHeight + Offset.Y - Size.Y;
                        break;
                }

                switch (anchor)
                {
                    default:
                        position.X = hostDevice.PresentationParameters.BackBufferWidth / 2 + Offset.X - Size.X / 2;
                        break;

                    case GuiAnchorPoint.Right:
                    case GuiAnchorPoint.TopRight:
                    case GuiAnchorPoint.BottomRight:
                        position.X = hostDevice.PresentationParameters.BackBufferWidth + Offset.X - Size.X;
                        break;

                    case GuiAnchorPoint.Left:
                    case GuiAnchorPoint.TopLeft:
                    case GuiAnchorPoint.BottomLeft:
                        position.X = 0 + Offset.X;
                        break;
                }


                return position;
            }
        }
    }

    public enum GuiAnchorPoint
    {
        Center,
        Top,
        Bottom,
        Left,
        Right,
        TopRight,
        TopLeft,
        BottomRight,
        BottomLeft
    }
}
