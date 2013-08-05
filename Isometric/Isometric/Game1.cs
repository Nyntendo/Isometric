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
    public enum ViewMode { full, dimmed, cut_roof, cut_front }

    public enum TileType { sand, dirt, grass, water, tree, leafs, mountain,
                            plant1, plant2, plant3, stump, plant4, plant5,
                            plant6, plant7, log, bush1, stone1, stone2,
                            mushroom1, mushroom2, mushroom3, bush2, lilypad1, lilypad2, cactus, desertplant1, desertplant2}

    public enum Action { no_action, drop, dig }

    public class Tile
    {
        public TileType Type { get; set; }
        public int ZPosition { get; set; }
        public bool Visible { get; set; }
    }

    public struct TileProperty
    {
        public bool IsBlock { get; set; }
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

        ViewMode currentViewMode = ViewMode.full;

        Character character;

        bool cutRoof = false;

        Random rand;

        int tilesPerRow = 7;

        int tileWidth = 35;
        int tileHeight = 37;

        int isoWidth = 17;
        int isoHeight = 17;
        int isoZHeight = 19;

        Dictionary<TileType, TileProperty> tileProperies;

        Dictionary<Action, ActionProperty> actionProperties;

        Vector2 startPoint = new Vector2(400, 100);

        int mapSize = 150;
        List<Tile>[,] map;

        PerlinGenerator heightPerlin;
        PerlinGenerator mountainPerlin;

        float terrainNoisiness = 4.0f;
        float mountainNoisiness = 10.0f;

        int terrainMaxHeight = 20;
        int mountainMaxHeight = 30;

        int waterLevel = 15;
        int baseLevel = 10;

        int treeProbability = 200;
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

            heightPerlin = new PerlinGenerator(rand.Next(0,Int32.MaxValue));
            mountainPerlin = new PerlinGenerator(rand.Next(0, Int32.MaxValue));
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

            List<TileType> passableTiles = new List<TileType>(new TileType[] {TileType.water, TileType.plant1, TileType.plant2, TileType.plant3, TileType.plant4, TileType.plant5, TileType.plant6, TileType.plant7, TileType.stump, TileType.log, TileType.mushroom1, TileType.mushroom2, TileType.mushroom3, TileType.lilypad1, TileType.lilypad2, TileType.desertplant1, TileType.desertplant2});
            List<TileType> diggableTiles = new List<TileType>(new TileType[] { TileType.dirt, TileType.sand, TileType.grass });
            List<TileType> blockTiles = new List<TileType>(new TileType[] { TileType.sand, TileType.dirt, TileType.grass, TileType.water, TileType.mountain });

            for (int i = 0; i < Enum.GetNames(typeof(TileType)).Count(); i++)
            {
                var passable = false;
                var diggable = false;
                var transparency = 1.0f;
                var isBlock = false;

                if (passableTiles.Contains((TileType)i))
                    passable = true;

                if (diggableTiles.Contains((TileType)i))
                    diggable = true;

                if ((TileType)i == TileType.water)
                    transparency = 0.2f;

                if (blockTiles.Contains((TileType)i))
                    isBlock = true;

                tileProperies.Add((TileType)i, new TileProperty() { Passable = passable, Transparency = transparency, Diggable = diggable, IsBlock = isBlock });
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

            //generate mountains
            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    map[x, y] = new List<Tile>();

                    int height = (int)Math.Round((mountainPerlin.Noise(mountainNoisiness * x / (float)mapSize, mountainNoisiness * y / (float)mapSize, 0) + 0.5f) * mountainMaxHeight);

                    for (int i = 0; i <= height; i++)
                    {
                        map[x, y].Add(new Tile() { Type = TileType.mountain, ZPosition = i });
                    }
                }
            }

            // Generate land
            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    int height = baseLevel + (int)Math.Round((heightPerlin.Noise(terrainNoisiness * x / (float)mapSize, terrainNoisiness * y / (float)mapSize, 0) + 0.5f) * terrainMaxHeight);

                    for (int i = 0; i <= height; i++)
                    {
                        if( !map[x,y].Any(tile => tile.ZPosition == i))
                            map[x, y].Add(new Tile() { Type = (i <= waterLevel) ? TileType.sand : (i == height ? TileType.grass : TileType.dirt), ZPosition = i });
                    }
                }
            }

            // Generate water
            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    while (map[x, y].OrderBy(tileHeight => tileHeight.ZPosition).Last().ZPosition < waterLevel)
                    {
                        map[x, y].Add(new Tile() { Type = TileType.water, ZPosition = map[x, y].Last().ZPosition + 1 });
                    }
                }
            }

            // Generate trees
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

            // Generate plants
            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    var topTile = map[x, y].Where(tile => tile.Type != TileType.leafs).Last();

                    if (topTile.Type == TileType.grass && rand.Next(0, plantProbability) == 0)
                    {
                        var tileType = (TileType)rand.Next((int)TileType.plant1, (int)TileType.bush2 + 1);
                        map[x, y].Add(new Tile() { Type = tileType, ZPosition = topTile.ZPosition + 1 });
                    }

                    if (topTile.Type == TileType.water && rand.Next(0, plantProbability) == 0)
                    {
                        var tileType = (TileType)rand.Next((int)TileType.lilypad1, (int)TileType.lilypad2 + 1);
                        map[x, y].Add(new Tile() { Type = tileType, ZPosition = topTile.ZPosition + 1 });
                    }

                    if (topTile.Type == TileType.sand && rand.Next(0, plantProbability) == 0)
                    {
                        var tileType = (TileType)rand.Next((int)TileType.cactus, (int)TileType.desertplant2 + 1);
                        map[x, y].Add(new Tile() { Type = tileType, ZPosition = topTile.ZPosition + 1 });
                    }
                }
            }


            // Hide hidden tiles
            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    foreach (Tile tile in map[x, y])
                    {
                        if (IsHidden(x, y, tile.ZPosition) && tileProperies[tile.Type].Transparency == 1.0f)
                            tile.Visible = false;
                        else
                            tile.Visible = true;
                    }
                }
            }

            if(character != null)
                character.Position = new Vector3(character.Position.X, character.Position.Y, map[(int)character.Position.X, (int)character.Position.Y].Where(tile => tile.Type != TileType.water).OrderBy(tile => tile.ZPosition).Last().ZPosition + 1);
        }

        private bool IsHidden(int x, int y, int z)
        {
            var isHidden = true;
            
            if (x < mapSize - 1)
                isHidden = isHidden && map[x + 1, y].Any(tile => tile.ZPosition == z && tileProperies[tile.Type].Transparency == 1.0f && tileProperies[tile.Type].IsBlock);

            if (x > 0)
                isHidden = isHidden && map[x - 1, y].Any(tile => tile.ZPosition == z && tileProperies[tile.Type].Transparency == 1.0f && tileProperies[tile.Type].IsBlock);

            if (y < mapSize - 1)
                isHidden = isHidden && map[x, y + 1].Any(tile => tile.ZPosition == z && tileProperies[tile.Type].Transparency == 1.0f && tileProperies[tile.Type].IsBlock);

            if (y > 0)
                isHidden = isHidden && map[x, y - 1].Any(tile => tile.ZPosition == z && tileProperies[tile.Type].Transparency == 1.0f && tileProperies[tile.Type].IsBlock);

            isHidden = isHidden && map[x, y].Any(tile => tile.ZPosition == z + 1 && tileProperies[tile.Type].Transparency == 1.0f && tileProperies[tile.Type].IsBlock);

            return isHidden;
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
                heightPerlin = new PerlinGenerator(rand.Next(0, Int32.MaxValue));
                mountainPerlin = new PerlinGenerator(rand.Next(0, Int32.MaxValue));
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.Add) && previousKeyboardState.IsKeyUp(Keys.Add))
            {
                terrainNoisiness += 1.0f;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.Subtract) && previousKeyboardState.IsKeyUp(Keys.Subtract))
            {
                terrainNoisiness -= 1.0f;
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
                terrainMaxHeight += 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.D) && previousKeyboardState.IsKeyUp(Keys.D))
            {
                terrainMaxHeight -= 1;
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

            if (keyboardState.IsKeyDown(Keys.T) && previousKeyboardState.IsKeyUp(Keys.T))
            {
                mountainMaxHeight += 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.G) && previousKeyboardState.IsKeyUp(Keys.G))
            {
                mountainMaxHeight -= 1;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.Y) && previousKeyboardState.IsKeyUp(Keys.Y))
            {
                mountainNoisiness += 1.0f;
                GenerateMap();
            }

            if (keyboardState.IsKeyDown(Keys.H) && previousKeyboardState.IsKeyUp(Keys.H))
            {
                mountainNoisiness -= 1.0f;
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
            if (keyboardState.IsKeyDown(Keys.Up) && character.MoveTimer == 0 && character.ActionTimer == 0)
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

            if (keyboardState.IsKeyDown(Keys.Down) && character.MoveTimer == 0 && character.ActionTimer == 0)
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

            if (keyboardState.IsKeyDown(Keys.Left) && character.MoveTimer == 0 && character.ActionTimer == 0)
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

            if (keyboardState.IsKeyDown(Keys.Right) && character.MoveTimer == 0 && character.ActionTimer == 0)
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

            if (keyboardState.IsKeyDown(Keys.Space) && previousKeyboardState.IsKeyUp(Keys.Space) && character.JumpTimer == 0 && character.ActionTimer == 0)
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
                                map[dropX, dropY].Add(new Tile() { Type = character.CarryingTile.Value, ZPosition = dropZ, Visible = true });
                                character.CarryingTile = null;
                            }
                        }
                    }
                    break;
                case Action.dig:
                    {
                        character.LastAction = Action.dig;

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
                                var digZ = tile.ZPosition;
                                map[digX, digY].Remove(tile);
                                ShowHiddenNeighbors(digX, digY, digZ);
                                character.CarryingTile = tile.Type;
                            }
                        }
                    }
                    break;
                case Action.no_action:
                default:

                    break;
            }
        }

        private void ShowHiddenNeighbors(int x, int y, int z)
        {
            IEnumerable<Tile> neighborCollection;

            if (x > 0)
            {
                neighborCollection = map[x - 1, y].Where(tile => tile.ZPosition == z);
                if (neighborCollection.Any())
                    neighborCollection.First().Visible = true;
            }

            if (x < mapSize - 1)
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

            if (y < mapSize - 1)
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
                    foreach(Tile tile in map[x,y].Where(tile => tile.Visible).OrderBy(tile => tile.ZPosition)) {

                        int spriteXOffset = ((int)tile.Type % tilesPerRow) * tileWidth;
                        int spriteYOffset = (int)tile.Type / tilesPerRow * tileHeight;

                        var cartX = x * isoWidth;
                        var cartY = y * isoHeight;

                        float isoX = cartX - cartY;
                        float isoY = (cartX + cartY ) / 2;
                        isoY -= isoZHeight * tile.ZPosition;

                        Vector2 position = new Vector2(isoX, isoY);

                        var transparency = tileProperies[tile.Type].Transparency;

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
