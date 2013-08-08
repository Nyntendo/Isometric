using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Isometric.Common;

namespace Isometric
{
    public enum Direction { north, east, south, west };

    public class Character
    {
        public Vector3 Position { get; set; }
        public Vector3 NextPosition { get; set; }
        public Direction Direction { get; set; }
        public Vector2 PositionOffset { get; set; }
        public float JumpOffset { get; set; }
        public double MoveTimer { get; set; }
        public double MoveDuration { get; set; }
        public bool Jumping { get; set; }
        public int JumpHeight { get; set; }
        public double JumpTimer { get; set; }
        public double JumpDuration { get; set; }
        public Common.Action ActiveAction { get; set; }
        public Common.Action LastAction { get; set; }
        public TileType? CarryingTile { get; set; }
        public double ActionTimer { get; set; }


        public Character(Vector3 position)
        {
            Position = position;
            NextPosition = position;
            Direction = Direction.east;
            PositionOffset = Vector2.Zero;
            JumpOffset = 0.0f;
            MoveTimer = 0.0f;
            MoveDuration = 200.0f;
            Jumping = false;
            JumpHeight = 2;
            JumpTimer = 0.0f;
            JumpDuration = 500.0f;
            ActiveAction = Common.Action.dig;
            LastAction = Common.Action.no_action;
            CarryingTile = null;
            ActionTimer = 0.0f;
        }

    }
}
