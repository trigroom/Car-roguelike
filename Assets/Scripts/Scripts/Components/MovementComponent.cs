using UnityEngine;

public struct MovementComponent {
    public float CurrentMoveSpeed;
    public float MaxMoveSpeed;
 public float CurrentRotateSpeed;
    public float RotateSpeed;
    public float Acceleration;     
    public float Deceleration;
    public Rigidbody2D MovableObjectRigidbody;
    public Vector2 TargetPosition;
    public Vector2 PreviousPosition; 
    public Vector2 Velocity;
    
    public float CantMoveTime;  
}
