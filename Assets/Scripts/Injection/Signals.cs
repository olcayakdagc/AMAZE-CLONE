using GridSystem;
using UnityEngine;

namespace Injection
{
   public class CreateSprites
   {
      public bool IsZ;
   }
   public class OnLevelUnload
   {
     
   }
   public class OnLevelLoad
   {
      public Vector2Int StartPos;
      public float GridWidth;
      public float GridHeight;
      public float CellSize;
      public float BallSpeed;
      public float RollSpeedMultiplier;
      public float CameraYOffset;
      public int AvailableCount;
   }
   
   public class OnNodePainted
   {
      public int X;
      public int Y;
   }

   public class OnLevelMaterialsLoaded
   {
      public Material Ground;
      public Material Wall;
      public Material Paint;
   }

   public class OnCameraExitComplete
   {
   }
   
   public class OnWin
   {
     
   }
   
   public class OnLost
   {
     
   }
   
   public class Restart
   {
     
   }
   
   public class NextLevel
   {
     
   }
   
   public class OnGameplay
   {
    
   }
   public class OnSwipe
   {
      public Direction Direction;
   }
}
