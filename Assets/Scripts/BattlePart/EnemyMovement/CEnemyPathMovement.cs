/// <summary>
/// 敌人沿烘焙路径移动（<see cref="PathRouteMovementData"/> → <see cref="EnemyPathBakeCache"/>）。
/// </summary>
public struct CEnemyPathMovement : IComponent
{
    public uint spawnFrame;
    public float originX, originY;
    public int routeBakeIndex;
}
