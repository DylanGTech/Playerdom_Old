﻿using System;
using Microsoft.Xna.Framework;
using Playerdom.Shared.Objects;
using Playerdom.Shared.Entities;
using System.Collections.Generic;
using Playerdom.Shared.Models;

namespace Playerdom.Shared.Services
{
    public static class CollisionService
    {
        public static void MoveWithTileCollision(GameObject gameObject, Map map, Vector2 velocity)
        {
            int top = (int)(gameObject.Position.Y / Tile.SizeY) - 1;
            int bottom = top + (int)(gameObject.Size.Y / Tile.SizeY) + 1;
            int left = (int)(gameObject.Position.X / Tile.SizeX) - 1;
            int right = left + (int)(gameObject.Size.X / Tile.SizeX) + 1;

            if (top < 0) top = 0;
            if (left < 0) left = 0;
            if (bottom > Map.SizeY - 2) bottom = (int)(Map.SizeY - 3);
            if (right > Map.SizeX - 2) right = (int)(Map.SizeX - 3);


            if (gameObject.IsSolid)
            {
                foreach (KeyValuePair<Guid, GameObject> other in map.gameObjects)
                {
                    if (other.Value == gameObject || !other.Value.IsSolid) continue;
                    Vector2 newDepth = GetIntersectionDepth(gameObject.BoundingBox, other.Value.BoundingBox);

                    if (newDepth == Vector2.Zero || gameObject.IsHalted) continue;
                    if (newDepth.X != 0 && velocity.X != 0 && Math.Abs(gameObject.Position.X - other.Value.Position.X) >= Math.Abs(gameObject.Position.Y - other.Value.Position.Y))
                        velocity.X += newDepth.X;

                    if (newDepth.Y != 0 && velocity.Y != 0 && Math.Abs(gameObject.Position.X - other.Value.Position.X) <= Math.Abs(gameObject.Position.Y - other.Value.Position.Y))
                        velocity.Y += newDepth.Y;
                }
            }

            for (int y = top; y <= bottom + 1; y++)
            {
                for (int x = left; x <= right + 1; x++)
                {
                    if (map.tiles[x, y].typeID != 2 &&
                        !(map.tiles[x, y].typeID == 4 | map.tiles[x, y].typeID == 5)) continue;
                    Rectangle newBounds = gameObject.BoundingBox;

                    newBounds.Offset(velocity.X, velocity.Y);
                    Vector2 depth = GetIntersectionDepth(newBounds, new Rectangle(x * (int)Tile.SizeX, y * (int)Tile.SizeY, (int)Tile.SizeX, (int)Tile.SizeY));

                    if (depth == Vector2.Zero || gameObject.IsHalted) continue;
                    if (depth.X != 0 && velocity.X != 0 && Math.Abs(gameObject.Position.X - x * (int)Tile.SizeX) > Math.Abs(gameObject.Position.Y - y * (int)Tile.SizeY))
                        velocity.X += depth.X;

                    if (depth.Y != 0 && velocity.Y != 0 && Math.Abs(gameObject.Position.X - x * (int)Tile.SizeX) < Math.Abs(gameObject.Position.Y - y * (int)Tile.SizeY))
                        velocity.Y += depth.Y;
                }
            }

            gameObject.ChangePosition((int)velocity.X, (int)velocity.Y);
        }

        public static void MoveWithTileCollision(Entity gameEntity, Map map, Vector2 velocity)
        {
            int top = (int)(gameEntity.Position.Y / Tile.SizeY) - 1;
            int bottom = top + (int)(gameEntity.Size.Y / Tile.SizeY) + 1;
            int left = (int)(gameEntity.Position.X / Tile.SizeX) - 1;
            int right = left + (int)(gameEntity.Size.X / Tile.SizeX) + 1;

            if (top < 0) top = 0;
            if (left < 0) left = 0;
            if (bottom > Map.SizeY - 1) bottom = (int)(Map.SizeY - 2);
            if (right > Map.SizeX - 1) right = (int)(Map.SizeX - 2);

            for (int y = top; y <= bottom + 1; y++)
            {
                for (int x = left; x <= right + 1; x++)
                {
                    if (map.tiles[x, y].typeID != 2 && map.tiles[x, y].typeID != 4 &&
                        map.tiles[x, y].typeID != 5) continue;
                    Rectangle newBounds = gameEntity.BoundingBox;

                    newBounds.Offset(velocity.X, velocity.Y);
                    Vector2 depth = GetIntersectionDepth(newBounds, new Rectangle(x * (int)Tile.SizeX, y * (int)Tile.SizeY, (int)Tile.SizeX, (int)Tile.SizeY));

                    if (gameEntity.GetType() == typeof(Bullet) && depth.X != 0 && depth.Y != 0)
                    {
                        if (map.tiles[x, y].typeID == 2 || map.tiles[x, y].typeID == 5) gameEntity.MarkedForDeletion = true;
                    }
                    else if (depth != Vector2.Zero && !gameEntity.IsHalted)
                    {
                        if (depth.X != 0 && velocity.X != 0 && Math.Abs(gameEntity.Position.X - x * (int)Tile.SizeX) > Math.Abs(gameEntity.Position.Y - y * (int)Tile.SizeY))
                            velocity.X += depth.X;
                        if (depth.Y != 0 && velocity.Y != 0 && Math.Abs(gameEntity.Position.X - x * (int)Tile.SizeX) < Math.Abs(gameEntity.Position.Y - y * (int)Tile.SizeY))
                            velocity.Y += depth.Y;
                    }
                }
            }

            gameEntity.ChangePosition((int)velocity.X, (int)velocity.Y);
        }

        public static Vector2 GetIntersectionDepth(this Rectangle rectA, Rectangle rectB)
        {
            // Calculate half sizes.
            float halfWidthA = rectA.Width / 2.0f;
            float halfHeightA = rectA.Height / 2.0f;
            float halfWidthB = rectB.Width / 2.0f;
            float halfHeightB = rectB.Height / 2.0f;

            // Calculate centers.
            var (x, y) = new Vector2(rectA.Left + halfWidthA, rectA.Top + halfHeightA);
            var (f, f1) = new Vector2(rectB.Left + halfWidthB, rectB.Top + halfHeightB);

            // Calculate current and minimum-non-intersecting distances between centers.
            float distanceX = x - f;
            float distanceY = y - f1;
            float minDistanceX = halfWidthA + halfWidthB;
            float minDistanceY = halfHeightA + halfHeightB;

            // If we are not intersecting at all, return (0, 0).
            if (Math.Abs(distanceX) >= minDistanceX || Math.Abs(distanceY) >= minDistanceY)
                return Vector2.Zero;

            // Calculate and return intersection depths.
            float depthX = distanceX > 0 ? minDistanceX - distanceX : -minDistanceX - distanceX;
            float depthY = distanceY > 0 ? minDistanceY - distanceY : -minDistanceY - distanceY;
            return new Vector2(depthX, depthY);
        }
    }
}
