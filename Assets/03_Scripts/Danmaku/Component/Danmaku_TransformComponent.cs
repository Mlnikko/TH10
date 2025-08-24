using UnityEngine;
public class Danmaku_TransformComponent
{
    public Vector2 Position;
    public Vector2 Rotation;
    public Vector2 Velocity; 
    
    public Danmaku_TransformComponent(Vector2 position, Vector2 rotation, Vector2 startVelocity)
    {
        Position = position;
        Rotation = rotation;
        Velocity = startVelocity;
    }
}
