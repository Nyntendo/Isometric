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

namespace Isometric
{
    public enum TileType { sand, dirt, grass, water, tree, leafs,
                            plant1, plant2, plant3, stump, plant4, plant5,
                            plant6, plant7, log, bush1, stone1, stone2,
                            mushroom1, mushroom2, mushroom3, bush2, lilypad1, lilypad2};

    public enum Action { no_action, drop, dig };

    public struct Tile
    {
        public TileType Type { get; set; }
        public int ZPosition { get; set; }
    }

    public struct TileProperty
    {
        public bool Passable { get; set; }
        public float Transparency { get; set; }
        public bool Diggable { get; set; }
    }

    public struct ActionProperty
    {
        public double Duration { get; set; }
    }

    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Texture2D tileSprites;
        Texture2D charSprite;
        Texture2D toolbarSprite;
        Texture2D toolsSprite;

        Vector2 toolbarPosition = new Vector2(10, 10);

        Character character;

        bool cutRoof = false;

        Random rand;

        int tileWidth = 35;
        int tileHeight = 37;

        int isoWidth = 18;
        int isoHeight = 18;
        int isoZHeight = 21;

        Dictionary<TileType, TileProperty> tileProperies;

        Dictionary<Action, ActionProperty> actionProperties;

        Vector2 startPoint = new Vector2(400, 100);

        int mapSize = 150;
        List<Tile>[,] map;

        PerlinGenerator perlin;

        float noisiness = 6.0f;

        int multiplier = 20;

        int waterLevel = 4;
        int baseLevel = 0;

        int treeProbability = 50;
        int plantProbability = 50;

        KeyboardState keyboardState, previousKeyboardState;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = 1680;
            graphics.PreferredBackBufferHeight = 1050;
            graphics.IsFullScreen = false;

            rand = new Random();

            perlin = new PerlinGenerator(0);
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

            tileProperies = new Dictionary<TileType, TileProperty>();

            List<TileType> passableTiles = new List<TileType>(new TileType[] {TileType.water, TileType.plant1, TileType.plant2, TileType.plant3, TileType.plant4, TileType.plant5, TileType.plant6, TileType.plant7, TileType.stump, TileType.log, TileType.mushroom1, TileType.mushroom2, TileType.mushroom3, TileType.lilypad1, TileType.lilypad2});
            List<TileType> diggableTiles = new List<TileType>(new TileType[] { TileType.dirt, TileType.sand, TileType.grass });

            for (int i = 0; i < Enum.GetNames(typeof(TileType)).Count(); i++)
            {
                var passable = false;
                var diggable = false;
                var transparency = 1.0f;

                if (passableTiles.Contains((TileType)i))
                    passable = true;

                if (diggableTiles.Contains((TileType)i))
                    diggable = true;

                if ((TileType)i == TileType.water)
                    transparency = 0.2f;

                tileProperies.Add((TileType)i, new TileProperty() { Passable = passable, Transparency = transparency, Diggable = diggable });
            }

            actionProperties = new Dictionary<Action, ActionProperty>();

            for (int i = 0; i < Enum.GetNames(typeof(Action)).Count(); i++)
            {
                actionProperties.Add((Action)i, new ActionProperty() { Duration = 1.0f });
            }

            character = new Character(new Vector3(0, 0, 0));

            GenerateMap();

            keyboardState = previousKeyboardState = Keyboard.GetState();
        }

        private void GenerateMap()
        {
            map = new List<Tile>[mapSize, mapSize];

            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    map[x, y] = new List<Tile>();

                    int height = baseLevel + (int)Math.Round(Math.Abs(perlin.Noise(noisiness * x / (float)mapSize, noisiness * y / (float)mapSize, 0) * multiplier));

                    for (int i = 0; i <= height; i++)
                    {
                        map[x, y].Add(new Tile(){Type = (i < waterLevel)?TileType.sand:TileType.dirt, ZPosition = i});
                    }
                }
            }

            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    if (map[x, y].Count > waterLevel)
                    {
                        map[x, y].Add(new Tile() { Type = TileType.grass, ZPosition = map[x, y].Last().ZPosition + 1});
                    }
                    else
                    {
                        while (map[x, y].Count < waterLevel)
                        {
                            map[x, y].Add(new Tile() { Type = TileType.water, ZPosition = map[x, y].Last().ZPosition + 1});
                        }
                    }
                }
            }

            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    if (map[x, y].Last().Type == TileType.grass && rand.Next(0, treeProbability) == 0)
                    {
                        GenerateTree(x, y, map[x, y].Last().ZPosition);
                    }
                }

            }

            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    var topTile = map[x, y].Where(tile => tile.Type != TileType.leafs).Last();

                    if (topTile.Type == TileType.grass && rand.Next(0, plantProbability) == 0)
                    {
                        var tileType = (TileType)rand.Next(6, 22);
                        map[x, y].Add(new Tile() { Type = tileType, ZPosition = topTile.ZPosition + 1 });
                    }

                    if (topTile.Type == TileType.water && rand.Next(0, plantProbability) == 0)
                    {
                        var tileType = (TileType)rand.Next(22, 24);
                        map[x, y].Add(new Tile() { Type = tileType, ZPosition = topTile.ZPosition + 1 });
                    }
                }
            }

            if(character != null)
                character.Position = new Vector3(character.Position.X, character.Position.Y, map[(int)character.Position.X, (int)character.Position.Y].Where(tile => tile.Type != TileType.water).OrderBy(tile => tile.ZPosition).Last().ZPosition + 1);
        }

        private void GenerateTree(int x, int y, int z)
        {
            Vector3[] crownCoords = { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(0,-1,0), new Vector3(1,0,0), new Vector3(-1,0,0),
                                      new Vector3(1,1,1), new Vector3(1,0,1), new Vector3(1,-1,1), new Vector3(0,1,1), new Vector3(0,0,1), new Vector3(0,-1,1), new Vector3(-1,1,1), new Vector3(-1,0,1), new Vector3(-1,-1,1), new Vector3(0,2,1), new Vector3(0,-2,1), new Vector3(2,0,1), new Vector3(-2,0,1),
                                      new Vector3(0,0,2), new Vector3(0,1,2), new Vector3(0,-1,2), new Vector3(1,0,2), new Vector3(-1,0,2),
                                      new Vector3(0,0,3)};

            var treeHeight = rand.Next(3, 9);

            //trunk

            for (int i = 0; i < treeHeight; i++)
            {
                map[x, y].Add(new Tile() { Type = TileType.tree, ZPosition = z + i + 1 });
            }

            //crown

            foreach (Vector3 coord in crownCoords)
            {
                var cx = x + (int)coord.X;
                var cy = y + (int)coord.Y;
                var cz = z + treeHeight + (int)coord.Z + 1;
                
                if (IsWithinMap(cx,cy))
                    map[cx, cy].Add(new Tile() { Type = TileType.leafs, ZPosition = cz });
            }
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
                perlin = new PerlinGenerator(gameTime.TotalGameTime.Milliseconds);
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.Add) && previousKeyboardState.IsKeyUp(Keys.Add))
            {
                noisiness += 1.0f;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.Subtract) && previousKeyboardState.IsKeyUp(Keys.Subtract))
            {
                noisiness -= 1.0f;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.W) && previousKeyboardState.IsKeyUp(Keys.W))
            {
                baseLevel += 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.S) && previousKeyboardState.IsKeyUp(Keys.S))
            {
                baseLevel -= 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.Q) && previousKeyboardState.IsKeyUp(Keys.Q))
            {
                waterLevel += 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.A) && previousKeyboardState.IsKeyUp(Keys.A))
            {
                waterLevel -= 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.E) && previousKeyboardState.IsKeyUp(Keys.E))
            {
                multiplier += 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.D) && previousKeyboardState.IsKeyUp(Keys.D))
            {
                multiplier -= 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.R) && previousKeyboardState.IsKeyUp(Keys.R))
            {
                treeProbability -= 5;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.F) && previousKeyboardState.IsKeyUp(Keys.F))
            {
                treeProbability += 5;
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
                cutRoof = !cutRoof;
            }

            #endregion scroll

            #region movement
            if (keyboardState.IsKeyDown(Keys.Up) && character.MoveTimer == 0)
            {
                character.Direction = Direction.north;
                if (IsWithinMap((int)character.Position.X, (int)character.Position.Y - 1) && keyboardState.IsKeyUp(Keys.LeftShift))
                {
                    var tileCollection = map[(int)character.Position.X, (int)character.Position.Y - 1].Where(tile => tile.ZPosition <= character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1) && tileProperies[tile.Type].Passable == false).OrderBy(tile => tile.ZPosition);

                    if (tileCollection.Count() > 0 && tileCollection.Last().ZPosition != character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1))
                    {
                        character.NextPosition = new Vector3((int)character.Position.X, (int)character.Position.Y - 1, tileCollection.Last().ZPosition + 1);
                        character.MoveTimer = character.MoveDuration;
                    }
                }
            }

            if (keyboardState.IsKeyDown(Keys.Down) && character.MoveTimer == 0)
            {
                character.Direction = Direction.south;
                if (IsWithinMap((int)character.Position.X, (int)character.Position.Y + 1) && keyboardState.IsKeyUp(Keys.LeftShift))
                {
                    var tileCollection = map[(int)character.Position.X, (int)character.Position.Y + 1].Where(tile => tile.ZPosition <= character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1) && tileProperies[tile.Type].Passable == false).OrderBy(tile => tile.ZPosition);

                    if (tileCollection.Count() > 0 && tileCollection.Last().ZPosition != character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1))
                    {
                        character.NextPosition = new Vector3((int)character.Position.X, (int)character.Position.Y + 1, tileCollection.Last().ZPosition + 1);
                        character.MoveTimer = character.MoveDuration;
                    }
                }
            }

            if (keyboardState.IsKeyDown(Keys.Left) && character.MoveTimer == 0)
            {
                character.Direction = Direction.west;
                if (IsWithinMap((int)character.Position.X - 1, (int)character.Position.Y) && keyboardState.IsKeyUp(Keys.LeftShift))
                {
                    var tileCollection = map[(int)character.Position.X - 1, (int)character.Position.Y].Where(tile => tile.ZPosition <= character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1) && tileProperies[tile.Type].Passable == false).OrderBy(tile => tile.ZPosition);

                    if (tileCollection.Count() > 0 && tileCollection.Last().ZPosition != character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1))
                    {
                        character.NextPosition = new Vector3((int)character.Position.X - 1, (int)character.Position.Y, tileCollection.Last().ZPosition + 1);
                        character.MoveTimer = character.MoveDuration;
                    }
                }
            }

            if (keyboardState.IsKeyDown(Keys.Right) && character.MoveTimer == 0)
            {
                character.Direction = Direction.east;
                if (IsWithinMap((int)character.Position.X + 1, (int)character.Position.Y) && keyboardState.IsKeyUp(Keys.LeftShift))
                {
                    var tileCollection = map[(int)character.Position.X + 1, (int)character.Position.Y].Where(tile => tile.ZPosition <= character.Position.Z + ((character.Jumping) ? character.JumpHeight : 1) && tileProperies[tile.Type].Passable == false).OrderBy(tile => tile.ZPosition);

                    if (tileCollection.Count() > 0 && tileCollection.Last().ZPosition != character.Position.Z + ((character.Jumping)?character.JumpHeight:1))
                    {
                        character.NextPosition = new Vector3((int)character.Position.X + 1, (int)character.Position.Y, tileCollection.Last().ZPosition + 1);
                        character.MoveTimer = character.MoveDuration;
                    }
                }
            }

            if (keyboardState.IsKeyDown(Keys.Space) && previousKeyboardState.IsKeyUp(Keys.Space) && character.JumpTimer == 0)
            {
                character.JumpTimer = character.JumpDuration;
            }

            if (keyboardState.IsKeyDown(Keys.LeftControl) && previousKeyboardState.IsKeyUp(Keys.LeftControl) && character.ActionTimer == 0)
            {
                character.ActionTimer = actionProperties[character.ActiveAction].Duration;
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
                        character.ActiveAction = Action.drop;
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
                case Action.drop:
                    {
                        if (character.CarryingTile.HasValue)
                        {
                            int dropX = (int)character.Position.X + (character.Direction == Direction.east ? 1 : 0) - (character.Direction == Direction.west ? 1 : 0);
                            int dropY = (int)character.Position.Y + (character.Direction == Direction.south ? 1 : 0) - (character.Direction == Direction.north ? 1 : 0);

                            var tileCollection = map[dropX, dropY].Where(tile => tile.ZPosition <= character.Position.Z + ((character.Jumping) ? character.JumpHeight - 1 : 0)).OrderBy(tile => tile.ZPosition);

                            if (tileCollection.Count() > 0 && tileCollection.Last().ZPosition != character.Position.Z + ((character.Jumping) ? character.JumpHeight - 1 : 0))
                            {
                                int dropZ = tileCollection.Last().ZPosition + 1;
                                map[dropX, dropY].Add(new Tile() { Type = character.CarryingTile.Value, ZPosition = dropZ });
                                character.CarryingTile = null;
                            }
                        }
                    }
                    break;
                case Action.dig:
                    {
                        int digX = (int)character.Position.X + (character.Direction == Direction.east ? 1 : 0) - (character.Direction == Direction.west ? 1 : 0);
                        int digY = (int)character.Position.Y + (character.Direction == Direction.south ? 1 : 0) - (character.Direction == Direction.north ? 1 : 0);

                        var tileCollection = map[digX, digY].Where(tile => tile.ZPosition == character.Position.Z + ((character.Jumping) ? character.JumpHeight - 1 : 0));

                        if (tileCollection.Count() == 0)
                            tileCollection = map[digX, digY].Where(tile => tile.ZPosition == character.Position.Z - 1);

                        if (tileCollection.Count() > 0)
                        {
                            var tile = tileCollection.First();

                            if (tileProperies[tile.Type].Diggable)
                            {
                                map[digX, digY].Remove(tile);
                                character.CarryingTile = tile.Type;
                                character.LastAction = Action.dig;
                            }
                        }
                    }
                    break;
                case Action.no_action:
                default:

                    break;
            }
        }

        private bool IsWithinMap(int x, int y)
        {
            return x >= 0 && y >= 0 && x < mapSize && y < mapSize;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    foreach(Tile tile in map[x,y]) {

                        int spriteXOffset = ((int)tile.Type % 6) * tileWidth;
                        int spriteYOffset = (int)tile.Type / 6 * tileHeight;

                        var cartX = x * isoWidth;
                        var cartY = y * isoHeight;

                        float isoX = cartX - cartY;
                        float isoY = (cartX + cartY ) / 2;
                        isoY -= isoZHeight * tile.ZPosition;

                        Vector2 position = new Vector2(isoX, isoY);

                        var transparency = tileProperies[tile.Type].Transparency;

                        if (cutRoof && tile.ZPosition > character.Position.Z)
                            transparency = 0.0f;

                        if (cutRoof && tile.ZPosition > character.Position.Z && tile.ZPosition < character.Position.Z + 2)
                            transparency = 0.6f;

                        if (cutRoof && tile.ZPosition > character.Position.Z + 1 && tile.ZPosition < character.Position.Z + 3)
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

            if (character.ActiveAction == Action.drop && character.CarryingTile != null)
            {
                int spriteXOffset = ((int)character.CarryingTile % 6) * tileWidth;
                int spriteYOffset = (int)character.CarryingTile / 6 * tileHeight;

                spriteBatch.Draw(tileSprites, toolbarPosition + new Vector2(10,9), new Rectangle(spriteXOffset, spriteYOffset, tileWidth, tileHeight), Color.White);
            }
            else if (character.ActiveAction == Action.dig)
            {
                spriteBatch.Draw(toolsSprite, toolbarPosition + new Vector2(16, 15), new Rectangle(0, 0, 24, 24), Color.White);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
