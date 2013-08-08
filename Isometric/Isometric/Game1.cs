using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Isometric.Common;
using Isometric.WorldGeneration;

namespace Isometric
{
    public enum ViewMode { full, dimmed, cut_roof, cut_front }

    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private WorldGenerator _worldGenerator;

        private int _mapWidth;
        private int _mapHeight;

        Texture2D tileSprites;
        Texture2D charSprite;
        Texture2D toolbarSprite;
        Texture2D toolsSprite;

        Vector2 toolbarPosition = new Vector2(10, 10);

        ViewMode currentViewMode = ViewMode.full;

        Character character;

        Random rand;

        int tilesPerRow = 7;

        int tileWidth = 35;
        int tileHeight = 37;

        int isoWidth = 17;
        int isoHeight = 17;
        int isoZHeight = 19;

        Vector2 startPoint = new Vector2(400, 100);

        List<Tile>[,] map;

        KeyboardState keyboardState, previousKeyboardState;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = 1680;
            graphics.PreferredBackBufferHeight = 1050;
            graphics.IsFullScreen = false;

            rand = new Random();

            _worldGenerator = new WorldGenerator();
            _mapWidth = _worldGenerator.MapWidth;
            _mapHeight = _worldGenerator.MapHeight;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            tileSprites = Content.Load<Texture2D>("tiles");
            charSprite = Content.Load<Texture2D>("char");
            toolbarSprite = Content.Load<Texture2D>("toolbar");
            toolsSprite = Content.Load<Texture2D>("tools");

            character = new Character(new Vector3(0, 0, 0));

            GenerateMap();

            keyboardState = previousKeyboardState = Keyboard.GetState();
        }

        private void GenerateMap()
        {
            map = _worldGenerator.GenerateWorld();

            if(character != null)
                character.Position = new Vector3(character.Position.X, character.Position.Y, map[(int)character.Position.X, (int)character.Position.Y].Where(tile => tile.Type != TileType.water && tile.Type != TileType.leafs).OrderBy(tile => tile.ZPosition).Last().ZPosition + 1);
        }
        

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void Update(GameTime gameTime)
        {
            keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Escape) && previousKeyboardState.IsKeyUp(Keys.Escape))
            {
                this.Exit();
            }

            #region mapcontrol

            if (keyboardState.IsKeyDown(Keys.F5) && previousKeyboardState.IsKeyUp(Keys.F5))
            {
                _worldGenerator.ReSeed();
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.Add) && previousKeyboardState.IsKeyUp(Keys.Add))
            {
                _worldGenerator.TerrainNoisiness += 1.0f;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.Subtract) && previousKeyboardState.IsKeyUp(Keys.Subtract))
            {
                _worldGenerator.TerrainNoisiness -= 1.0f;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.W) && previousKeyboardState.IsKeyUp(Keys.W))
            {
                _worldGenerator.BaseLevel += 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.S) && previousKeyboardState.IsKeyUp(Keys.S))
            {
                _worldGenerator.BaseLevel -= 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.Q) && previousKeyboardState.IsKeyUp(Keys.Q))
            {
                _worldGenerator.WaterLevel += 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.A) && previousKeyboardState.IsKeyUp(Keys.A))
            {
                _worldGenerator.WaterLevel -= 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.E) && previousKeyboardState.IsKeyUp(Keys.E))
            {
                _worldGenerator.TerrainMaxHeight += 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.D) && previousKeyboardState.IsKeyUp(Keys.D))
            {
                _worldGenerator.TerrainMaxHeight -= 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.R) && previousKeyboardState.IsKeyUp(Keys.R))
            {
                _worldGenerator.TreeProbability -= 5;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.F) && previousKeyboardState.IsKeyUp(Keys.F))
            {
                _worldGenerator.TreeProbability += 5;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.T) && previousKeyboardState.IsKeyUp(Keys.T))
            {
                _worldGenerator.MountainMaxHeight += 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.G) && previousKeyboardState.IsKeyUp(Keys.G))
            {
                _worldGenerator.MountainMaxHeight -= 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.Y) && previousKeyboardState.IsKeyUp(Keys.Y))
            {
                _worldGenerator.MountainNoisiness += 1.0f;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.H) && previousKeyboardState.IsKeyUp(Keys.H))
            {
                _worldGenerator.MountainNoisiness -= 1.0f;
                GenerateMap();
            }

            #endregion mapcontrol

            #region scroll

            if (keyboardState.IsKeyDown(Keys.NumPad8))
            {
                startPoint += new Vector2(0, 10);
            }

            if (keyboardState.IsKeyDown(Keys.NumPad2))
            {
                startPoint -= new Vector2(0, 10);
            }

            if (keyboardState.IsKeyDown(Keys.NumPad4))
            {
                startPoint += new Vector2(10, 0);
            }

            if (keyboardState.IsKeyDown(Keys.NumPad6))
            {
                startPoint -= new Vector2(10, 0);
            }

            if (keyboardState.IsKeyDown(Keys.Tab) && previousKeyboardState.IsKeyUp(Keys.Tab))
            {
                var newViewMode = (int)currentViewMode + 1;

                if (newViewMode > Enum.GetNames(typeof(ViewMode)).Count())
                    newViewMode = 0;

                currentViewMode = (ViewMode)newViewMode;
            }

            #endregion scroll

            #region movement

            //TODO: Break out the logic here
            if (keyboardState.IsKeyDown(Keys.Up) && character.MoveTimer == 0 && character.ActionTimer == 0)
            {
                character.Direction = Direction.north;
                if (Tools.IsWithinMap((int)character.Position.X, (int)character.Position.Y - 1, _mapWidth, _mapHeight) && keyboardState.IsKeyUp(Keys.LeftShift))
                {
                    var tileCollection = map[(int)character.Position.X, (int)character.Position.Y - 1].Where(tile => tile.ZPosition <= character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1) && Configuration.TileProperties[tile.Type].Passable == false).OrderBy(tile => tile.ZPosition);

                    if (tileCollection.Count() > 0 && tileCollection.Last().ZPosition != character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1))
                    {
                        character.NextPosition = new Vector3((int)character.Position.X, (int)character.Position.Y - 1, tileCollection.Last().ZPosition + 1);
                        character.MoveTimer = character.MoveDuration;
                    }
                }
            }

            if (keyboardState.IsKeyDown(Keys.Down) && character.MoveTimer == 0 && character.ActionTimer == 0)
            {
                character.Direction = Direction.south;
                if (Tools.IsWithinMap((int)character.Position.X, (int)character.Position.Y + 1, _mapWidth, _mapHeight) && keyboardState.IsKeyUp(Keys.LeftShift))
                {
                    var tileCollection = map[(int)character.Position.X, (int)character.Position.Y + 1].Where(tile => tile.ZPosition <= character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1) && Configuration.TileProperties[tile.Type].Passable == false).OrderBy(tile => tile.ZPosition);

                    if (tileCollection.Count() > 0 && tileCollection.Last().ZPosition != character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1))
                    {
                        character.NextPosition = new Vector3((int)character.Position.X, (int)character.Position.Y + 1, tileCollection.Last().ZPosition + 1);
                        character.MoveTimer = character.MoveDuration;
                    }
                }
            }

            if (keyboardState.IsKeyDown(Keys.Left) && character.MoveTimer == 0 && character.ActionTimer == 0)
            {
                character.Direction = Direction.west;
                if (Tools.IsWithinMap((int)character.Position.X - 1, (int)character.Position.Y, _mapWidth, _mapHeight) && keyboardState.IsKeyUp(Keys.LeftShift))
                {
                    var tileCollection = map[(int)character.Position.X - 1, (int)character.Position.Y].Where(tile => tile.ZPosition <= character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1) && Configuration.TileProperties[tile.Type].Passable == false).OrderBy(tile => tile.ZPosition);

                    if (tileCollection.Count() > 0 && tileCollection.Last().ZPosition != character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1))
                    {
                        character.NextPosition = new Vector3((int)character.Position.X - 1, (int)character.Position.Y, tileCollection.Last().ZPosition + 1);
                        character.MoveTimer = character.MoveDuration;
                    }
                }
            }

            if (keyboardState.IsKeyDown(Keys.Right) && character.MoveTimer == 0 && character.ActionTimer == 0)
            {
                character.Direction = Direction.east;
                if (Tools.IsWithinMap((int)character.Position.X + 1, (int)character.Position.Y, _mapWidth, _mapHeight) && keyboardState.IsKeyUp(Keys.LeftShift))
                {
                    var tileCollection = map[(int)character.Position.X + 1, (int)character.Position.Y].Where(tile => tile.ZPosition <= character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1) && Configuration.TileProperties[tile.Type].Passable == false).OrderBy(tile => tile.ZPosition);

                    if (tileCollection.Count() > 0 && tileCollection.Last().ZPosition != character.Position.Z + ((character.Jumping)?character.JumpHeight:1))
                    {
                        character.NextPosition = new Vector3((int)character.Position.X + 1, (int)character.Position.Y, tileCollection.Last().ZPosition + 1);
                        character.MoveTimer = character.MoveDuration;
                    }
                }
            }

            if (keyboardState.IsKeyDown(Keys.Space) && previousKeyboardState.IsKeyUp(Keys.Space) && character.JumpTimer == 0 && character.ActionTimer == 0)
            {
                character.JumpTimer = character.JumpDuration;
            }

            if (keyboardState.IsKeyDown(Keys.LeftControl) && previousKeyboardState.IsKeyUp(Keys.LeftControl) && character.ActionTimer == 0)
            {
                character.ActionTimer = Configuration.ActionProperties[character.ActiveAction].Duration;
            }

            #endregion movement

            if (character.MoveTimer > 0)
            {
                character.MoveTimer -= gameTime.ElapsedGameTime.TotalMilliseconds;

                if (character.MoveTimer > 0)
                {
                    if (character.Direction == Direction.north)
                    {
                        character.PositionOffset = new Vector2(0, -isoHeight) * (float)(1 - (character.MoveTimer / character.MoveDuration));
                    }
                    if (character.Direction == Direction.south)
                    {
                        character.PositionOffset = new Vector2(0, isoHeight) * (float)(1 - (character.MoveTimer / character.MoveDuration));
                    }
                    if (character.Direction == Direction.west)
                    {
                        character.PositionOffset = new Vector2(-isoWidth, 0) * (float)(1 - (character.MoveTimer / character.MoveDuration));
                    }
                    if (character.Direction == Direction.east)
                    {
                        character.PositionOffset = new Vector2(isoWidth, 0) * (float)(1 - (character.MoveTimer / character.MoveDuration));
                    }
                }
                else
                {
                    character.PositionOffset = Vector2.Zero;

                    character.Position = character.NextPosition;

                    character.MoveTimer = 0;
                }
            }

            if (character.JumpTimer > 0)
            {
                character.JumpTimer -= gameTime.ElapsedGameTime.TotalMilliseconds;

                if (character.JumpTimer > 0)
                {
                    if (character.JumpTimer < character.JumpDuration / 2)
                    {
                        character.JumpOffset = isoZHeight * character.JumpHeight * (float)(character.JumpTimer / (character.JumpDuration / 2));
                    }
                    else
                    {
                        character.JumpOffset = isoZHeight * character.JumpHeight * (float)(1 - ((character.JumpTimer - (character.JumpDuration / 2)) / (character.JumpDuration / 2)));
                    }

                    if (character.NextPosition.Z > character.Position.Z)
                    {
                        character.JumpOffset -= (character.NextPosition.Z - character.Position.Z) * isoZHeight;

                        if (character.JumpOffset < 0)
                        {
                            character.JumpOffset = 0;
                            character.JumpTimer = 1;
                        }
                    }

                    if (character.JumpTimer > character.JumpDuration / 4 && character.JumpTimer < character.JumpDuration * 3 / 4)
                    {
                        character.Jumping = true;
                    }
                    else
                    {
                        character.Jumping = false;
                    }
                }
                else
                {
                    character.JumpTimer = 0;
                    character.JumpOffset = 0;
                    character.Jumping = false;
                }
            }

            if (character.ActionTimer > 0)
            {
                character.ActionTimer -= gameTime.ElapsedGameTime.TotalMilliseconds;

                if (character.ActionTimer > 0)
                {

                }
                else
                {
                    PerformAction();

                    if (character.CarryingTile != null)
                    {
                        character.ActiveAction = Common.Action.drop;
                    }
                    else
                    {
                        character.ActiveAction = character.LastAction;
                    }
                    character.ActionTimer = 0;
                }
            }
            
            previousKeyboardState = keyboardState;

            base.Update(gameTime);
        }

        private void PerformAction()
        {
            switch (character.ActiveAction)
            {
                case Common.Action.drop:
                    {
                        if (character.CarryingTile.HasValue)
                        {
                            int dropX = (int)character.Position.X + (character.Direction == Direction.east ? 1 : 0) - (character.Direction == Direction.west ? 1 : 0);
                            int dropY = (int)character.Position.Y + (character.Direction == Direction.south ? 1 : 0) - (character.Direction == Direction.north ? 1 : 0);

                            var tileCollection = map[dropX, dropY].Where(tile => tile.ZPosition <= character.Position.Z + ((character.Jumping) ? character.JumpHeight - 1 : 0)).OrderBy(tile => tile.ZPosition);

                            if (tileCollection.Count() > 0 && tileCollection.Last().ZPosition != character.Position.Z + ((character.Jumping) ? character.JumpHeight - 1 : 0))
                            {
                                int dropZ = tileCollection.Last().ZPosition + 1;
                                map[dropX, dropY].Add(new Tile() { Type = character.CarryingTile.Value, ZPosition = dropZ, Visible = true });
                                character.CarryingTile = null;
                            }
                        }
                    }
                    break;
                case Common.Action.dig:
                    {
                        character.LastAction = Common.Action.dig;

                        int digX = (int)character.Position.X + (character.Direction == Direction.east ? 1 : 0) - (character.Direction == Direction.west ? 1 : 0);
                        int digY = (int)character.Position.Y + (character.Direction == Direction.south ? 1 : 0) - (character.Direction == Direction.north ? 1 : 0);

                        var tileCollection = map[digX, digY].Where(tile => tile.ZPosition == character.Position.Z + ((character.Jumping) ? character.JumpHeight - 1 : 0));

                        if (tileCollection.Count() == 0)
                            tileCollection = map[digX, digY].Where(tile => tile.ZPosition == character.Position.Z - 1);

                        if (tileCollection.Count() > 0)
                        {
                            var tile = tileCollection.First();

                            if (Configuration.TileProperties[tile.Type].Diggable)
                            {
                                var digZ = tile.ZPosition;
                                map[digX, digY].Remove(tile);
                                ShowHiddenNeighbors(digX, digY, digZ);
                                character.CarryingTile = tile.Type;
                            }
                        }
                    }
                    break;
                case Common.Action.no_action:
                default:

                    break;
            }
        }

        //Move to server
        private void ShowHiddenNeighbors(int x, int y, int z)
        {
            IEnumerable<Tile> neighborCollection;

            if (x > 0)
            {
                neighborCollection = map[x - 1, y].Where(tile => tile.ZPosition == z);
                if (neighborCollection.Any())
                    neighborCollection.First().Visible = true;
            }

            if (x < _mapWidth - 1)
            {
                neighborCollection = map[x + 1, y].Where(tile => tile.ZPosition == z);
                if (neighborCollection.Any())
                    neighborCollection.First().Visible = true;
            }

            if (y > 0)
            {
                neighborCollection = map[x, y - 1].Where(tile => tile.ZPosition == z);
                if (neighborCollection.Any())
                    neighborCollection.First().Visible = true;
            }

            if (y < _mapHeight - 1)
            {
                neighborCollection = map[x, y + 1].Where(tile => tile.ZPosition == z);
                if (neighborCollection.Any())
                    neighborCollection.First().Visible = true;
            }

            neighborCollection = map[x, y].Where(tile => tile.ZPosition == z + 1);
            if (neighborCollection.Any())
                neighborCollection.First().Visible = true;

            neighborCollection = map[x, y].Where(tile => tile.ZPosition == z - 1);
            if (neighborCollection.Any())
                neighborCollection.First().Visible = true;
        }

        

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            for (int x = 0; x < _mapWidth; x++)
            {
                for (int y = 0; y < _mapHeight; y++)
                {
                    foreach(Tile tile in map[x,y].Where(tile => tile.Visible).OrderBy(tile => tile.ZPosition)) {

                        int spriteXOffset = ((int)tile.Type % tilesPerRow) * tileWidth;
                        int spriteYOffset = (int)tile.Type / tilesPerRow * tileHeight;

                        var cartX = x * isoWidth;
                        var cartY = y * isoHeight;

                        float isoX = cartX - cartY;
                        float isoY = (cartX + cartY ) / 2;
                        isoY -= isoZHeight * tile.ZPosition;

                        Vector2 position = new Vector2(isoX, isoY);

                        var transparency = Configuration.TileProperties[tile.Type].Transparency;

                        if (currentViewMode == ViewMode.cut_front && x > character.Position.X && y > character.Position.Y)
                            transparency = 0.0f;

                        if ((currentViewMode == ViewMode.cut_roof || currentViewMode == ViewMode.dimmed) && tile.ZPosition > character.Position.Z)
                            transparency = 0.0f;

                        if (currentViewMode == ViewMode.dimmed && tile.ZPosition > character.Position.Z && tile.ZPosition < character.Position.Z + 2)
                            transparency = 0.6f;

                        if (currentViewMode == ViewMode.dimmed && tile.ZPosition > character.Position.Z + 1 && tile.ZPosition < character.Position.Z + 3)
                            transparency = 0.3f;

                        spriteBatch.Draw(tileSprites, startPoint + position, new Rectangle(spriteXOffset, spriteYOffset, tileWidth, tileHeight), Color.White * transparency);

                        if ((character.Position.X == x && character.Position.Y == y && character.Position.Z == tile.ZPosition + 1) && (character.NextPosition.X <= character.Position.X || character.NextPosition.Y <= character.Position.Y) ||
                            (character.NextPosition.X == x && character.NextPosition.Y == y && character.NextPosition.Z == tile.ZPosition + 1) && (character.NextPosition.X > character.Position.X || character.NextPosition.Y > character.Position.Y))
                        {
                            var charCartX = character.Position.X * isoWidth + character.PositionOffset.X;
                            var charCartY = character.Position.Y * isoHeight + character.PositionOffset.Y;

                            float charIsoX = charCartX - charCartY;
                            float charIsoY = (charCartX + charCartY) / 2;

                            charIsoY -= ((character.NextPosition.Z > character.Position.Z)?character.NextPosition.Z:character.Position.Z) * isoZHeight + character.JumpOffset;

                            // Magic numbers for setting char sprite in center of tile
                            Vector2 charPosition = new Vector2(charIsoX + 4, charIsoY - 3);

                            var charSpriteXOffset = 0;

                            if (character.MoveTimer > 0)
                            {
                                charSpriteXOffset += (character.MoveTimer >= character.MoveDuration / 2) ? 24 : 48;
                            }

                            spriteBatch.Draw(charSprite, startPoint + charPosition, new Rectangle(charSpriteXOffset, (character.Direction == Direction.north || character.Direction == Direction.west) ? 35 : 0, 24, 35), Color.White, 0.0f, Vector2.Zero, 1.0f, (character.Direction == Direction.south || character.Direction == Direction.west) ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 1.0f);
                        }
                    }
                }
            }

            spriteBatch.Draw(toolbarSprite, toolbarPosition, Color.White);

            if (character.ActiveAction == Common.Action.drop && character.CarryingTile != null)
            {
                int spriteXOffset = ((int)character.CarryingTile % 6) * tileWidth;
                int spriteYOffset = (int)character.CarryingTile / 6 * tileHeight;

                spriteBatch.Draw(tileSprites, toolbarPosition + new Vector2(10,9), new Rectangle(spriteXOffset, spriteYOffset, tileWidth, tileHeight), Color.White);
            }
            else if (character.ActiveAction == Common.Action.dig)
            {
                spriteBatch.Draw(toolsSprite, toolbarPosition + new Vector2(16, 15), new Rectangle(0, 0, 24, 24), Color.White);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
