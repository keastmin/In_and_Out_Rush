using System.Collections.Generic;

namespace KIM.Dev
{
    public interface IWorldObstacleConsumer
    {
        void InitializeWorldObstacles(IReadOnlyList<WorldObstacle> worldObstacles);
    }
}
