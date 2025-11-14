using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
  [SerializeField] private Ball _prefabBall;

  [Networked] private TickTimer delay { get; set; }
  [Networked] private Color playerColor { get; set; }
  [Networked] private int Health { get; set; }
  [Networked] public int HitCount { get; set; }

  private NetworkCharacterController _cc;
  private Vector3 _forward;
  private Material _material;

  private void Awake()
  {
    _cc = GetComponent<NetworkCharacterController>();
    _forward = transform.forward;
    _material = GetComponentInChildren<MeshRenderer>().material;
  }

  public override void Spawned()
  {
    if (HasStateAuthority)
    {
      playerColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
      Health = 3;
      HitCount = 0;
    }
  }

  public override void Render()
  {
    _material.color = playerColor;
  }

  [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
  public void RPC_ApplyDamage(int amount, RpcInfo info = default)
  {
    if (!HasStateAuthority) return;

    Health -= amount;
    HitCount += amount;

    if (HitCount >= 3)
      Runner.Shutdown();

    if (Health <= 0)
      Runner.Despawn(Object);
  }

  public override void FixedUpdateNetwork()
  {
    if (GetInput(out NetworkInputData data))
    {
      data.direction.Normalize();
      _cc.Move(5 * data.direction * Runner.DeltaTime);

      if (data.direction.sqrMagnitude > 0)
        _forward = data.direction;

      if (HasStateAuthority && delay.ExpiredOrNotRunning(Runner))
      {
        if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
        {
          delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
          Runner.Spawn(
            _prefabBall,
            transform.position + _forward,
            Quaternion.LookRotation(_forward),
            Object.InputAuthority,
            (runner, o) =>
            {
              o.GetComponent<Ball>().Init();
            }
          );
        }
      }
    }
  }
}