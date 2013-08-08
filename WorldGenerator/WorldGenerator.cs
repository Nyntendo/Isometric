using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Isometric.Common;

namespace Isometric.WorldGeneration
{
    public class WorldGenerator
    {
        private struct Vector3
        {
            public Vector3(int x, int y, int z) : this()
            {
                X = x;
                Y = y;
                Z = z;
            }

            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
        }

        public int MapWidth { get; set; }
        public int MapHeight { get; set; }

        public int HeightSeed { get; set; }
        public int MountainSeed { get; set; }

        public float TerrainNoisiness { get; set; }
        public float MountainNoisiness { get; set; }

        public int TerrainMaxHeight { get; set; }
        public int MountainMaxHeight { get; set; }

        public int WaterLevel { get; set; }
        public int BaseLevel { get; set; }

        public int TreeProbability { get; set; }
        public int PlantProbability { get; set; }

        private Random _rand;

        public WorldGenerator()
        {
            _rand = new Random();

            MapWidth = 150;
            MapHeight = 150;

            TerrainNoisiness = 4.0f;
            MountainNoisiness = 10.0f;

            TerrainMaxHeight = 20;
            MountainMaxHeight = 30;

            WaterLevel = 15;
            BaseLevel = 10;

            TreeProbability = 200;
            PlantProbability = 50;
        }

        public void ReSeed()
        {
            HeightSeed = _rand.Next(0, Int32.MaxValue);
            MountainSeed = _rand.Next(0, Int32.MaxValue);
        }

        public List<Tile>[,] GenerateWorld()
        {
            PerlinGenerator heightPerlin = new PerlinGenerator(HeightSeed);
            PerlinGenerator mountainPerlin = new PerlinGenerator(MountainSeed);

            var world = new List<Tile>[MapWidth, MapHeight];

            //generate mountains
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    world[x, y] = new List<Tile>();

                    int height = (int)Math.Round((mountainPerlin.Noise(MountainNoisiness * x / (float)MapWidth, MountainNoisiness * y / (float)MapHeight, 0) + 0.5f) * MountainMaxHeight);

                    for (int i = 0; i <= height; i++)
                    {
                        world[x, y].Add(new Tile() { Type = TileType.mountain, ZPosition = i });
                    }
                }
            }

            // Generate land
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    int height = BaseLevel + (int)Math.Round((heightPerlin.Noise(TerrainNoisiness * x / (float)MapWidth, TerrainNoisiness * y / (float)MapHeight, 0) + 0.5f) * TerrainMaxHeight);

                    for (int i = 0; i <= height; i++)
                    {
                        if( !world[x,y].Any(tile => tile.ZPosition == i))
                            world[x, y].Add(new Tile() { Type = (i <= WaterLevel) ? TileType.sand : (i == height ? TileType.grass : TileType.dirt), ZPosition = i });
                    }
                }
            }

            // Generate water
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    while (world[x, y].OrderBy(tileHeight => tileHeight.ZPosition).Last().ZPosition < WaterLevel)
                    {
                        world[x, y].Add(new Tile() { Type = TileType.water, ZPosition = world[x, y].Last().ZPosition + 1 });
                    }
                }
            }

            // Generate trees
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    if (world[x, y].Last().Type == TileType.grass && _rand.Next(0, TreeProbability) == 0)
                    {
                        GenerateTree(x, y, world[x, y].Last().ZPosition, ref world);
                    }
                }

            }

            // Generate plants
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    var topTile = world[x, y].Where(tile => tile.Type != TileType.leafs).Last();

                    if (topTile.Type == TileType.grass && _rand.Next(0, PlantProbability) == 0)
                    {
                        var tileType = (TileType)_rand.Next((int)TileType.plant1, (int)TileType.bush2 + 1);
                        world[x, y].Add(new Tile() { Type = tileType, ZPosition = topTile.ZPosition + 1 });
                    }

                    if (topTile.Type == TileType.water && _rand.Next(0, PlantProbability) == 0)
                    {
                        var tileType = (TileType)_rand.Next((int)TileType.lilypad1, (int)TileType.lilypad2 + 1);
                        world[x, y].Add(new Tile() { Type = tileType, ZPosition = topTile.ZPosition + 1 });
                    }

                    if (topTile.Type == TileType.sand && _rand.Next(0, PlantProbability) == 0)
                    {
                        var tileType = (TileType)_rand.Next((int)TileType.cactus, (int)TileType.desertplant2 + 1);
                        world[x, y].Add(new Tile() { Type = tileType, ZPosition = topTile.ZPosition + 1 });
                    }
                }
            }


            // Hide hidden tiles
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    foreach (Tile tile in world[x, y])
                    {
                        if (IsHidden(x, y, tile.ZPosition, ref world) && Configuration.TileProperties[tile.Type].Transparency == 1.0f)
                            tile.Visible = false;
                        else
                            tile.Visible = true;
                    }
                }
            }

            return world;
        }

        private void GenerateTree(int x, int y, int z, ref List<Tile>[,] world)
        {
            Vector3[] crownCoords = { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(0,-1,0), new Vector3(1,0,0), new Vector3(-1,0,0),
                                      new Vector3(1,1,1), new Vector3(1,0,1), new Vector3(1,-1,1), new Vector3(0,1,1), new Vector3(0,0,1), new Vector3(0,-1,1), new Vector3(-1,1,1), new Vector3(-1,0,1), new Vector3(-1,-1,1), new Vector3(0,2,1), new Vector3(0,-2,1), new Vector3(2,0,1), new Vector3(-2,0,1),
                                      new Vector3(0,0,2), new Vector3(0,1,2), new Vector3(0,-1,2), new Vector3(1,0,2), new Vector3(-1,0,2),
                                      new Vector3(0,0,3)};

            var treeHeight = _rand.Next(3, 9);

            //trunk

            for (int i = 0; i < treeHeight; i++)
            {
                world[x, y].Add(new Tile() { Type = TileType.tree, ZPosition = z + i + 1 });
            }

            //crown

            foreach (Vector3 coord in crownCoords)
            {
                var cx = x + coord.X;
                var cy = y + coord.Y;
                var cz = z + treeHeight + coord.Z + 1;

                if (Tools.IsWithinMap(cx, cy, MapWidth, MapHeight))
                    world[cx, cy].Add(new Tile() { Type = TileType.leafs, ZPosition = cz });
            }
        }

        private bool IsHidden(int x, int y, int z, ref List<Tile>[,] world)
        {
            var isHidden = true;

            if (x < MapWidth - 1)
                isHidden = isHidden && world[x + 1, y].Any(tile => tile.ZPosition == z && Configuration.TileProperties[tile.Type].Transparency == 1.0f && Configuration.TileProperties[tile.Type].IsBlock);

            if (x > 0)
                isHidden = isHidden && world[x - 1, y].Any(tile => tile.ZPosition == z && Configuration.TileProperties[tile.Type].Transparency == 1.0f && Configuration.TileProperties[tile.Type].IsBlock);

            if (y < MapHeight - 1)
                isHidden = isHidden && world[x, y + 1].Any(tile => tile.ZPosition == z && Configuration.TileProperties[tile.Type].Transparency == 1.0f && Configuration.TileProperties[tile.Type].IsBlock);

            if (y > 0)
                isHidden = isHidden && world[x, y - 1].Any(tile => tile.ZPosition == z && Configuration.TileProperties[tile.Type].Transparency == 1.0f && Configuration.TileProperties[tile.Type].IsBlock);

            isHidden = isHidden && world[x, y].Any(tile => tile.ZPosition == z + 1 && Configuration.TileProperties[tile.Type].Transparency == 1.0f && Configuration.TileProperties[tile.Type].IsBlock);

            return isHidden;
        }
    }
}
