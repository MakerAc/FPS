using UnityEngine;

/// <summary>
/// ·ÉÁú×´Ì¬»ùÀà
/// </summary>
public abstract class DragonState
{
    protected DragonStateMachine stateMachine;
    protected FlyingDragonController dragonController;

    public DragonState(DragonStateMachine stateMachine, FlyingDragonController dragonController)
    {
        this.stateMachine = stateMachine;
        this.dragonController = dragonController;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void Exit() { }
    public virtual void OnTriggerEnter(Collider other) { }
}