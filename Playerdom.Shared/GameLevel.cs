using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Services;
using Playerdom.Shared.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using Ceras;
using System.Net.Sockets;
using Ceras.Helpers;
using System.Collections.Concurrent;
using Playerdom.Shared.Models;

#if WINDOWS_UAP
using Windows.Storage;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Core;
#elif WINDOWS
using System.Windows;
#endif


namespace Playerdom.Shared
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class GameLevel : Game
    {
        //readonly GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        RenderTarget2D target;


        Texture2D groundTexture;
        Texture2D grassTexture;
        Texture2D flowerTexture;
        Texture2D stoneTexture;
        Texture2D mossyStoneTexture;
        Texture2D sandyPathTexture;
        Texture2D gravelPathTexture;
        Texture2D waterTexture;
        Texture2D wavyWaterTexture;
        Texture2D bricksTexture;
        Texture2D woodFlooringTexture;

        Texture2D uiBackground;
        Texture2D barBackground;
        Texture2D xpBar;
        Texture2D hpBar;

        bool isTyping = false;
        string typedMessage = "";


        public static List<ChatMessage> chatLog = new List<ChatMessage>();
        public static Map level;
        readonly static Assembly asm = Assembly.GetEntryAssembly();
        private readonly GraphicsDeviceManager graphics;
        SpriteFont font;
        SpriteFont font2;

        static TcpClient _tcpClient;
        static NetworkStream _netStream;


        static Guid securityToken;

        static KeyValuePair<Guid, GameObject> focusedObject = new KeyValuePair<Guid, GameObject>(Guid.Empty, null);

        const string WATERMARK = "Playerdom Test - Copyright 2019 Dylan Green";

        const ushort VIEW_DISTANCE = 31;

        public GameLevel()
        {
            Window.AllowUserResizing = true;
            graphics = new GraphicsDeviceManager(this);

#if !WINDOWS_UAP
            graphics.PreferredBackBufferWidth = 1600;
            graphics.PreferredBackBufferHeight = 900;
#endif
            this.IsMouseVisible = true;
            Content.RootDirectory = "Content";


        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            PlayerdomCerasSettings.Initialize();
            string ip = "localhost";
#if WINDOWS_UAP
            try
            {
                StorageFile file;
                file = Task.Run(async () => await ApplicationData.Current.LocalFolder.GetFileAsync("connection.txt")).Result;

                ip = File.ReadAllText(file.Path);
            }
            catch(Exception e)
            {
                StorageFile file;
                file = Task.Run(async () => await ApplicationData.Current.LocalFolder.CreateFileAsync("connection.txt", CreationCollisionOption.OpenIfExists)).Result;

                File.WriteAllText(file.Path, ip);
                ip = "localhost";
            }


            try
            {
                StorageFile file;
                file = Task.Run(async () => await ApplicationData.Current.LocalFolder.GetFileAsync("token.txt")).Result;

                securityToken = new Guid(File.ReadAllText(file.Path));
            }
            catch(Exception e)
            {
                StorageFile file;
                file = Task.Run(async () => await ApplicationData.Current.LocalFolder.CreateFileAsync("token.txt", CreationCollisionOption.OpenIfExists)).Result;

                securityToken = Guid.Empty;
            }



#elif WINDOWS
            try
            {
                ip = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "connection.txt"));
            }
            catch (Exception e) // e is never used??
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "connection.txt"), ip);
                ip = "localhost";
            }


            try
            {
                securityToken = new Guid(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "token.txt")));
            }
            catch (Exception e)
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "token.txt"), "");
                securityToken = Guid.Empty;
            }




#endif
            _tcpClient = new TcpClient();
            _tcpClient.Connect(ip, 25565);
            _netStream = _tcpClient.GetStream();


            level = new Map { tiles = new Tile[Map.SizeX, Map.SizeY] };
            Thread newThread = new Thread(ReceiveOutputAsync) { Name = "HandlingThread" };
            newThread.Start();

            Stopwatch connectionWatch = new Stopwatch();
            byte attempts = 0;

            connectionWatch.Start();
            while (focusedObject.Value == null)
            {
                if (attempts >= 55)
                {
                    throw new Exception("Connection Timed Out");
                }

                if (connectionWatch.ElapsedMilliseconds < 1000) continue;
                attempts++;
                connectionWatch.Restart();
            }
            connectionWatch.Stop();



            CerasSerializer keyboardSerializer = null;
            Task.Run(() =>
            {
                if (keyboardSerializer == null)
                    keyboardSerializer = new CerasSerializer(PlayerdomCerasSettings.Config);
                Window.TextInput += (sender, e) =>
                {
                    if (!isTyping) return;
                    KeyboardState ks = Keyboard.GetState();
                    if (ks.IsKeyDown(Keys.Escape))
                    {
                        typedMessage = "";
                        isTyping = false;
                        return;
                    }

                    if (ks.IsKeyDown(Keys.Enter))
                    {
                        isTyping = false;
                        keyboardSerializer.WriteToStream(_netStream, new KeyValuePair<string, string>("ChatMessage", typedMessage));
                        typedMessage = "";
                        return;
                    }

                    if (ks.IsKeyDown(Keys.Back) && typedMessage.Length > 0)
                    {
                        typedMessage = typedMessage.Substring(0, typedMessage.Length - 1);
                        return;
                    }


                    if (isTyping)
                    {
                        if (font2.Characters.Contains(e.Character))
                        {
                            char key = e.Character;

                            if (ks.IsKeyUp(Keys.LeftShift) && ks.IsKeyUp(Keys.RightShift))
                            {
                                if (char.IsLetter(key))
                                    key = char.ToLower(key);
                            }
                            else if (char.IsDigit(key))
                                switch (key)
                                {
                                    default:
                                        break;
                                    case '0':
                                        key = ')';
                                        break;
                                }
                        }
                    };

                    while (true)
                    {
                        // KeyboardState ks = Keyboard.GetState();

                        if (!isTyping)
                        {
                            keyboardSerializer.WriteToStream(_netStream, ks);

                            if (ks.IsKeyDown(Keys.OemSemicolon))
                            {
                                isTyping = true;
                            }
                        }

                        Thread.Sleep(15);
                    }

                };

                base.Initialize();
            });
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            try
            {
                // Create a new SpriteBatch, which can be used to draw textures.
                spriteBatch = new SpriteBatch(GraphicsDevice);
                groundTexture = Content.Load<Texture2D>("ground");
                grassTexture = Content.Load<Texture2D>("grass");
                flowerTexture = Content.Load<Texture2D>("flowers");
                stoneTexture = Content.Load<Texture2D>("stone");
                mossyStoneTexture = Content.Load<Texture2D>("mossy-stone");
                sandyPathTexture = Content.Load<Texture2D>("sandy-path");
                gravelPathTexture = Content.Load<Texture2D>("gravel-path");
                waterTexture = Content.Load<Texture2D>("water");
                wavyWaterTexture = Content.Load<Texture2D>("wavy-water");
                bricksTexture = Content.Load<Texture2D>("bricks");
                woodFlooringTexture = Content.Load<Texture2D>("wood-flooring");

                target = new RenderTarget2D(GraphicsDevice, 3840, 2160, false, SurfaceFormat.Color, DepthFormat.None);

                uiBackground = new Texture2D(GraphicsDevice, 1, 1);
                xpBar = new Texture2D(GraphicsDevice, 1, 1);
                hpBar = new Texture2D(GraphicsDevice, 1, 1);
                barBackground = new Texture2D(GraphicsDevice, 1, 1);

                uiBackground.SetData(new Color[] { Color.Gray });
                xpBar.SetData(new Color[] { Color.Yellow });
                hpBar.SetData(new Color[] { Color.Green });
                barBackground.SetData(new Color[] { Color.Black });

                font = Content.Load<SpriteFont>("font1");
                font2 = Content.Load<SpriteFont>("font2");
            }
            catch (Exception e)
            {
                LogException(e);

#if WINDOWS_UAP
                CoreApplication.Exit();
#elif WINDOWS
                Exit();
#endif
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        /*
        protected override void UnloadContent()
        {
            try
            {
                //Task.Run(async () => await MapService.SaveMapAsync(level)).Wait();
            }
            catch(Exception e)
            {

            }
        }
        */

        protected bool isHoldingTab = false;
        protected DateTime lastUpdate = DateTime.Now;
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            try
            {
                base.Update(gameTime);
            }
            catch (Exception e)
            {

                LogException(e);
#if WINDOWS_UAP
                CoreApplication.Exit();
#elif WINDOWS
                Exit();
#endif
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {

            GraphicsDevice.SetRenderTarget(target);
            GraphicsDevice.Clear(new Color(31, 31, 31));

            spriteBatch.Begin();
            DrawMap();

            foreach (KeyValuePair<Guid, Entity> e in level.gameEntities)
            {
                if (e.Value.ActiveTexture == null) e.Value.LoadContent(Content, GraphicsDevice);
                Vector2 distance = focusedObject.Value.Distance(e.Value);
                e.Value.Draw(spriteBatch, GraphicsDevice, distance, target);
            }

            foreach (KeyValuePair<Guid, GameObject> o in level.gameObjects)
            {

                if (o.Value == null || o.Key == Guid.Empty)
                    continue;

                if (o.Value.ActiveTexture == null) o.Value.LoadContent(Content, GraphicsDevice);
                if (object.ReferenceEquals(o.Value, focusedObject.Value))
                {
                    o.Value.DrawSprite(spriteBatch, new Vector2(0, 0), target);
                }
                else
                {
                    Vector2 d = focusedObject.Value.Distance(o.Value);

                    if (Math.Abs((int)d.Length()) < 24 * Tile.SizeX)
                        o.Value.DrawSprite(spriteBatch, d, target);
                }

            }
            foreach (KeyValuePair<Guid, GameObject> o in level.gameObjects)
            {

                if (o.Value == null || o.Key == Guid.Empty)
                    continue;

                if (o.Value.ActiveTexture == null) o.Value.LoadContent(Content, GraphicsDevice);
                if (object.ReferenceEquals(o.Value, focusedObject.Value))
                {
                    o.Value.DrawTag(spriteBatch, new Vector2(0, 0), target);
                }
                else
                {
                    Vector2 d = focusedObject.Value.Distance(o.Value);

                    if (Math.Abs((int)d.Length()) < 24 * Tile.SizeX)
                        o.Value.DrawTag(spriteBatch, d, target);
                }

            }

            foreach (KeyValuePair<Guid, GameObject> o in level.gameObjects)
            {

                if (o.Value == null || o.Key == Guid.Empty)
                    continue;

                if (o.Value.ActiveTexture == null) o.Value.LoadContent(Content, GraphicsDevice);
                if (object.ReferenceEquals(o.Value, focusedObject.Value))
                {
                    o.Value.DrawDialog(spriteBatch, new Vector2(0, 0), target);
                }
                else
                {
                    Vector2 d = focusedObject.Value.Distance(o.Value);

                    if (Math.Abs((int)d.Length()) < 24 * Tile.SizeX)
                        o.Value.DrawDialog(spriteBatch, d, target);
                }

            }

#if DEBUG
            spriteBatch.DrawString(font, "X: " + focusedObject.Value.Position.X, new Vector2(0, 0), Color.Red);
            spriteBatch.DrawString(font, "Y: " + focusedObject.Value.Position.Y, new Vector2(384, 0), Color.Red);
#endif
            spriteBatch.DrawString(font2, WATERMARK, new Vector2(0, target.Height - 48), Color.White);


            DrawStats();

            DrawChat();

            base.Draw(gameTime);

            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            spriteBatch.Begin();



            double preferredAspect = target.Width / (double)target.Height;
            double outputAspect = Window.ClientBounds.Width / (double)Window.ClientBounds.Height;


            Rectangle destination;
            if (outputAspect <= preferredAspect)
            {
                // output is taller than it is wider, bars on top/bottom
                int presentHeight = (int)((Window.ClientBounds.Width / preferredAspect) + 0.5f);
                int barHeight = (Window.ClientBounds.Height - presentHeight) / 2;
                destination = new Rectangle(0, barHeight, Window.ClientBounds.Width, presentHeight);
            }
            else
            {
                // output is wider than it is tall, bars left/right
                int presentWidth = (int)((Window.ClientBounds.Height * preferredAspect) + 0.5f);
                int barWidth = (Window.ClientBounds.Width - presentWidth) / 2;
                destination = new Rectangle(barWidth, 0, presentWidth, Window.ClientBounds.Height);
            }

            spriteBatch.Draw(target, destination, Color.White);
            spriteBatch.End();
        }


        protected void DrawChat()
        {
            for (int i = 0; i < chatLog.Count; i++)
            {
                try
                {
                    Rectangle r = new Rectangle(72, 72 + i * (int)font2.MeasureString(chatLog[i].message).Y, (int)font2.MeasureString(chatLog[i].message).X, (int)font2.MeasureString(chatLog[i].message).Y);
                    spriteBatch.Draw(uiBackground, r, Color.Black);
                    spriteBatch.DrawString(font2, chatLog[i].message, new Vector2(r.X, r.Y), chatLog[i].textColor);

                }
                catch (Exception e) // exception is never used?
                {
                    Rectangle r = new Rectangle(72, 72 + i * (int)font2.MeasureString("[Error Displaying Text]").Y, (int)font2.MeasureString("[Error Displaying Text]").X, (int)font2.MeasureString("[Error Displaying Text]").Y);
                    spriteBatch.Draw(uiBackground, r, Color.Black);
                    spriteBatch.DrawString(font2, "[Error Displaying Text]", new Vector2(r.X, r.Y), chatLog[i].textColor);
                }
            }

            try
            {
                Rectangle rect = new Rectangle(72, 80 + chatLog.Count * (int)font2.MeasureString(">: " + typedMessage).Y, (int)font2.MeasureString(">: " + typedMessage).X, (int)font2.MeasureString(">: " + typedMessage).Y);
                spriteBatch.Draw(uiBackground, rect, Color.Black);
                spriteBatch.DrawString(font2, ">: " + typedMessage, new Vector2(rect.X, rect.Y), Color.LightGray);
            }
            catch (Exception e) // Exception is never used?
            {
                Rectangle rect = new Rectangle(72, 80 + chatLog.Count * (int)font2.MeasureString(">: [Error Displaying Text]").Y, (int)font2.MeasureString(">: [Error Displaying Text]").X, (int)font2.MeasureString(">: [Error Displaying Text]").Y);
                spriteBatch.Draw(uiBackground, rect, Color.Black);
                spriteBatch.DrawString(font2, ">: [Error Displaying Text]", new Vector2(rect.X, rect.Y), Color.LightGray);
            }
        }

        protected void DrawStats()
        {
            spriteBatch.Draw(barBackground, new Rectangle(64 - 8, target.Height - 64 - 192 - 8, 512 + 16, 192 + 16), Color.White);
            spriteBatch.Draw(uiBackground, new Rectangle(64, target.Height - 64 - 192, 512, 192), Color.White);

            spriteBatch.DrawString(font2, string.Format("{0} - Lvl {1} - {2:N2} Ruppies", focusedObject.Value.DisplayName, focusedObject.Value.Level, focusedObject.Value.Money), new Vector2(64 + 16, target.Height - 64 - 192 + 16), Color.White);

            spriteBatch.DrawString(font2, string.Format("Health: {0} / {1}", focusedObject.Value.Health, focusedObject.Value.MaxHealth), new Vector2(64 + 16, target.Height - 64 - 192 + 48), Color.White);
            spriteBatch.Draw(barBackground, new Rectangle(64 + 16, target.Height - 64 - 192 + 80, 512 - 32, 16), Color.White);
            spriteBatch.Draw(hpBar, new Rectangle(64 + 16, target.Height - 64 - 192 + 80, (int)((512 - 32) * ((double)focusedObject.Value.Health / (double)focusedObject.Value.MaxHealth)), 16), Color.White);


            spriteBatch.DrawString(font2, string.Format("Expereince: {0} / {1}", focusedObject.Value.XP, focusedObject.Value.MaxXP), new Vector2(64 + 16, target.Height - 64 - 192 + 108), Color.White);
            spriteBatch.Draw(barBackground, new Rectangle(64 + 16, target.Height - 64 - 192 + 144, 512 - 32, 16), Color.White);
            spriteBatch.Draw(xpBar, new Rectangle(64 + 16, target.Height - 64 - 192 + 144, (int)((512 - 32) * ((double)focusedObject.Value.XP / (double)focusedObject.Value.MaxXP)), 16), Color.White);
        }

        protected void DrawMap()
        {

            ushort YTilePosition = (ushort)(focusedObject.Value.Position.Y / Tile.SizeY);
            ushort XTilePosition = (ushort)(focusedObject.Value.Position.X / Tile.SizeX);

            int ymin = YTilePosition - VIEW_DISTANCE;
            int ymax = YTilePosition + VIEW_DISTANCE;
            int xmin = XTilePosition - VIEW_DISTANCE;
            int xmax = XTilePosition + VIEW_DISTANCE;

            if (ymin < 0) ymin = 0;
            if (ymax > Map.SizeY) ymax = (int)Map.SizeY;
            if (xmin < 0) xmin = 0;
            if (xmax > Map.SizeX) xmax = (int)Map.SizeX;


            for (int y = ymin; y < ymax; y++)
            {
                for (int x = xmin; x < xmax; x++)
                {
                    int positionX = (int)(x * Tile.SizeX - focusedObject.Value.Position.X + target.Width / 2 - focusedObject.Value.Size.X / 2);
                    int positionY = (int)(y * Tile.SizeY - focusedObject.Value.Position.Y + target.Height / 2 - focusedObject.Value.Size.Y / 2);

                    if (level.tiles[x, y].typeID == 1)
                    {
                        if (level.tiles[x, y].variantID == 1) spriteBatch.Draw(grassTexture, new Rectangle(positionX, positionY, (int)Tile.SizeX, (int)Tile.SizeY), Color.White);
                        else if (level.tiles[x, y].variantID == 2) spriteBatch.Draw(flowerTexture, new Rectangle(positionX, positionY, (int)Tile.SizeX, (int)Tile.SizeY), Color.White);
                        else spriteBatch.Draw(groundTexture, new Rectangle(positionX, positionY, (int)Tile.SizeX, (int)Tile.SizeY), Color.White);
                    }
                    else if (level.tiles[x, y].typeID == 2)
                    {
                        if (level.tiles[x, y].variantID == 1) spriteBatch.Draw(mossyStoneTexture, new Rectangle(positionX, positionY, (int)Tile.SizeX, (int)Tile.SizeY), Color.White);
                        else spriteBatch.Draw(stoneTexture, new Rectangle(positionX, positionY, (int)Tile.SizeX, (int)Tile.SizeY), Color.White);
                    }
                    else if (level.tiles[x, y].typeID == 3)
                    {
                        if (level.tiles[x, y].variantID == 1) spriteBatch.Draw(gravelPathTexture, new Rectangle(positionX, positionY, (int)Tile.SizeX, (int)Tile.SizeY), Color.White);
                        else spriteBatch.Draw(sandyPathTexture, new Rectangle(positionX, positionY, (int)Tile.SizeX, (int)Tile.SizeY), Color.White);
                    }
                    else if (level.tiles[x, y].typeID == 4)
                    {
                        if (level.tiles[x, y].variantID == 1) spriteBatch.Draw(wavyWaterTexture, new Rectangle(positionX, positionY, (int)Tile.SizeX, (int)Tile.SizeY), Color.White);
                        else spriteBatch.Draw(waterTexture, new Rectangle(positionX, positionY, (int)Tile.SizeX, (int)Tile.SizeY), Color.White);

                    }
                    else if (level.tiles[x, y].typeID == 5)
                    {
                        spriteBatch.Draw(bricksTexture, new Rectangle(positionX, positionY, (int)Tile.SizeX, (int)Tile.SizeY), Color.White);
                    }
                    else if (level.tiles[x, y].typeID == 6)
                    {
                        spriteBatch.Draw(woodFlooringTexture, new Rectangle(positionX, positionY, (int)Tile.SizeX, (int)Tile.SizeY), Color.White);
                    }
                }
            }
        }

        private void LogException(Exception e)
        {


#if WINDOWS_UAP

                StorageFile file;
                file = Task.Run(async () => await ApplicationData.Current.LocalFolder.CreateFileAsync(e.GetType().ToString() + ".txt", CreationCollisionOption.OpenIfExists)).Result;

                File.WriteAllText(file.Path, e.StackTrace);


#elif WINDOWS
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, e.GetType().ToString() + ".txt"), e.StackTrace);
#endif
        }

        static async void ReceiveOutputAsync()
        {
            CerasSerializer _receiveCeras = new CerasSerializer(PlayerdomCerasSettings.Config);
            CerasSerializer _sendCeras = new CerasSerializer(PlayerdomCerasSettings.Config);


            _sendCeras.WriteToStream(_netStream, securityToken);


            while (true)
            {
                object obj = await _receiveCeras.ReadFromStream(_netStream);

                Debug.WriteLine("Received {0} packet", obj.GetType());


                if (obj is MapColumn[])
                {
                    MapColumn[] colArray = (MapColumn[])obj;

                    for (int i = 0; i < 32; i++)
                    {

                        for (int j = 0; j < Map.SizeY; j++)
                        {
                            level.tiles[colArray[i].ColumnNumber, j].typeID = colArray[i].TypesColumn[j];
                            level.tiles[colArray[i].ColumnNumber, j].variantID = colArray[i].VariantsColumn[j];
                        }
                    }


                    if (colArray[31].ColumnNumber == Map.SizeX - 1)
                    {
                        //lock(_sendCeras)
                        _sendCeras.WriteToStream(_netStream, "MapAffirmation");
                    }
                }
                //TODO: Make this work with ANY game object type
                else if (obj is KeyValuePair<Guid, GameObject> || obj is KeyValuePair<Guid, Player>)
                {


                    if (level.gameObjects.TryGetValue(((KeyValuePair<Guid, GameObject>)obj).Key, out GameObject ogo))
                    {
                        //level.gameObjects[((KeyValuePair<Guid, GameObject>)obj).Key] = ((KeyValuePair<Guid, GameObject>)obj).Value;
                        //focusedObject = (KeyValuePair<Guid, GameObject>)obj;
                        //ogo.Dispose();
                    }
                    else
                    {
                        focusedObject = (KeyValuePair<Guid, GameObject>)obj;
                        level.gameObjects.TryAdd(focusedObject.Key, focusedObject.Value);
                    }
                }
                else if (obj is ConcurrentDictionary<Guid, GameObject> initialObjects)
                {
                    Dictionary<Guid, GameObject> copyO = new Dictionary<Guid, GameObject>();
                    foreach (KeyValuePair<Guid, GameObject> kvp in level.gameObjects)
                    {
                        copyO.Add(kvp.Key, kvp.Value);
                    }
                    foreach (KeyValuePair<Guid, GameObject> o in copyO)
                    {
                        if (initialObjects.TryGetValue(o.Key, out GameObject gobj))
                        {
                            level.gameObjects[o.Key].UpdateStats(gobj);
                        }
                        else
                        {
                            level.gameObjects[o.Key].Dispose();
                            level.gameObjects.TryRemove(o.Key, out GameObject _object);
                        }
                    }


                    foreach (KeyValuePair<Guid, GameObject> o in initialObjects)
                    {
                        if (!level.gameObjects.TryGetValue(o.Key, out GameObject gobj))
                        {
                            level.gameObjects.TryAdd(o.Key, o.Value);
                        }
                    }
                }
                else if (obj is ConcurrentDictionary<Guid, Entity> newEntities)
                {
                    Dictionary<Guid, Entity> copyE = new Dictionary<Guid, Entity>();
                    foreach (KeyValuePair<Guid, Entity> kvp in level.gameEntities)
                    {
                        copyE.Add(kvp.Key, (Entity)kvp.Value);
                    }
                    foreach (KeyValuePair<Guid, Entity> e in copyE)
                    {
                        if (newEntities.TryGetValue(e.Key, out Entity ent))
                        {
                            level.gameEntities[e.Key].UpdateStats(ent);
                        }
                        else
                        {
                            level.gameEntities[e.Key].Dispose();
                            level.gameEntities.TryRemove(e.Key, out Entity _entity);
                        }
                    }

                    foreach (KeyValuePair<Guid, Entity> e in newEntities)
                    {
                        if (!level.gameEntities.TryGetValue(e.Key, out Entity ent))
                        {
                            level.gameEntities.TryAdd(e.Key, e.Value);
                        }
                    }
                }
                else if (obj is List<ChatMessage> messages)
                {
                    chatLog = messages;
                }
                else if (obj is Guid newToken)
                {
                    securityToken = newToken;

#if WINDOWS_UAP
                    StorageFile file;
                    file = Task.Run(async () => await ApplicationData.Current.LocalFolder.CreateFileAsync("token.txt", CreationCollisionOption.OpenIfExists)).Result;

                    File.WriteAllText(file.Path, newToken.ToString());
#elif WINDOWS
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "token.txt"), newToken.ToString());
#endif
                }
                else throw new Exception("Object " + obj.GetType() + " not supported");
            }

        }
    }
}
