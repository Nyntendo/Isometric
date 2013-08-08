using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Isometric.Common
{
    public enum TileType
    {
        sand, dirt, grass, water, tree, leafs, mountain,
        plant1, plant2, plant3, stump, plant4, plant5,
        plant6, plant7, log, bush1, stone1, stone2,
        mushroom1, mushroom2, mushroom3, bush2, lilypad1, lilypad2, cactus, desertplant1, desertplant2
    }

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

    public static class Configuration
    {
        private static Dictionary<TileType, TileProperty> _tileProperties;
        private static Dictionary<Action, ActionProperty> _actionProperties;

        public static Dictionary<TileType, TileProperty> TileProperties
        {
            get
            {
                if (_tileProperties == null)
                {
                    _tileProperties = new Dictionary<TileType, TileProperty>();

                    //TODO: These things should be loaded from some XML
                    List<TileType> passableTiles = new List<TileType>(new TileType[] { TileType.water, TileType.plant1, TileType.plant2, TileType.plant3, TileType.plant4, TileType.plant5, TileType.plant6, TileType.plant7, TileType.stump, TileType.log, TileType.mushroom1, TileType.mushroom2, TileType.mushroom3, TileType.lilypad1, TileType.lilypad2, TileType.desertplant1, TileType.desertplant2 });
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

                        _tileProperties.Add((TileType)i, new TileProperty() { Passable = passable, Transparency = transparency, Diggable = diggable, IsBlock = isBlock });
                    }
                }

                return _tileProperties;
            }
        }

        public static Dictionary<Action, ActionProperty> ActionProperties
        {
            get
            {
                if (_actionProperties == null)
                {
                    _actionProperties = new Dictionary<Action, ActionProperty>();

                    //TODO: These things should be loaded from some XML
                    for (int i = 0; i < Enum.GetNames(typeof(Action)).Count(); i++)
                    {
                        _actionProperties.Add((Action)i, new ActionProperty() { Duration = 1.0f });
                    }
                }

                return _actionProperties;
            }
        }
    }

    public static class Tools
    {
        public static bool IsWithinMap(int x, int y, int mapWidth, int mapHeight)
        {
            return x >= 0 && y >= 0 && x < mapWidth && y < mapHeight;
        }
    }
}
