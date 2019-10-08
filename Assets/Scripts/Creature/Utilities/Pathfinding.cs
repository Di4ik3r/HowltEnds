using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
        public static Vector2[] GetPath (Vector2 from, Vector2 to) {
        // bresenham line algorithm
        float w = to.x - from.x;
        float h = to.y - from.y;
        float absW = System.Math.Abs (w);
        float absH = System.Math.Abs (h);

        // Is neighbouring tile
        if (absW <= 1 && absH <= 1) {
            return null;
        }

        float dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
        if (w < 0) {
            dx1 = -1;
            dx2 = -1;
        } else if (w > 0) {
            dx1 = 1;
            dx2 = 1;
        }
        if (h < 0) {
            dy1 = -1;
        } else if (h > 0) {
            dy1 = 1;
        }

        int longest = (int)absW;
        float shortest = absH;
        if (longest <= shortest) {
            longest = (int)absH;
            shortest = absW;
            if (h < 0) {
                dy2 = -1;
            } else if (h > 0) {
                dy2 = 1;
            }
            dx2 = 0;
        }

        float numerator = longest >> 1;
        Vector2[] path = new Vector2[longest];
        for (int i = 1; i <= longest; i++) {
            numerator += shortest;
            if (numerator >= longest) {
                numerator -= longest;
                from.x += dx1;
                from.y += dy1;
            } else {
                from.x += dx2;
                from.y += dy2;
            }

            // If not walkable, path is invalid so return null
            // (unless is target tile, which may be unwalkable e.g water)
            // if (/* i != longest && */ Creature.digitalMap[(int)System.Math.Truncate(from.x), (int)System.Math.Truncate(from.y)] != 0) {
                // return null;
            // }
            path[i - 1] = new Vector2(from.x, from.y);
        }
        return path;
    }
}
