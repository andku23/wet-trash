using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CaveEnemy : BaseEnemy
{
    private NetworkObject _targetPlayer_s;
    private PlayerStateData _closestPlayerState_s;
    private Collider _currentWaterBody_s;
    private CaveRoom _currentRoom_s;
    private CaveRoom _targetRoom_s;

    private float TRAVERSE_HEIGHT_OFFSET = 1.2f;
    private float SIGHT_DISTANCE = 5.0f;
    private float SIGHT_RADIUS = 1.5f;

    [SerializeField] protected Transform _raycastStartPoint;
    [SerializeField] protected LayerMask _playerMask;
    
    private List<CaveTraverseNode> roomPath_s = new List<CaveTraverseNode>();
    
    public CaveRoom CurrentRoom_S {get => _currentRoom_s; set => _currentRoom_s = value; }
    
    enum ServerStates
    {
        Idle = 0,
        Following = 1,
        AttackingPlayer = 2,
        Dead = 3
    }

    public override void InitializeServerValues()
    {
        base.InitializeServerValues();
        _currentWaterBody_s = base.GetCurrentWaterBody();

    }
    
    protected override IEnumerator DoDeath()
    {
        _animator.SetBool("IsDead", true);
        _audioSource.PlaySound(PlayerAudioSource.SoundType.EnemyTakeDamage);
        if (IsServer)
        {
            ChangeState_ServerRpc((int)ServerStates.Dead);
            DropLoot_S();
            yield return new WaitForSeconds(1.5f);
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
    
    protected override void OnHealthUpdated(int prev, int next)
    {
        base.OnHealthUpdated(prev, next);
        _audioSource.PlaySound(PlayerAudioSource.SoundType.EnemyTakeDamage);
    }
    
    public override void InitializeStateMachine()
    {
        base.InitializeStateMachine();
        
        BaseState idle = new BaseState(Idle_OnEnter, Idle_Update, null);
        _stateMachine.AddState((int)ServerStates.Idle, idle);
        
        BaseState attackingPlayer = new BaseState(Attacking_OnEnter, Attacking_OnUpdate, null);
        _stateMachine.AddState((int)ServerStates.AttackingPlayer, attackingPlayer);
        
        BaseState followingPlayer = new BaseState(Following_OnEnter, Following_OnUpdate, null);
        _stateMachine.AddState((int)ServerStates.Following, followingPlayer);
        
        BaseState dead = new BaseState(null, null, null);
        _stateMachine.AddState((int)ServerStates.Dead, dead);
    }

    private bool CreateRoomPath_S(CaveRoom destinationRoom)
    {
        if(destinationRoom.DungeonNum != CurrentRoom_S.DungeonNum) return false;
        
        DungeonManager.Instance.ResetAllRoomSearchFlags();
        Debug.Log("Current Room: " + CurrentRoom_S.SpawnID + " Dest Room: " + destinationRoom.SpawnID);
        
        Queue<CaveRoom> queue = new Queue<CaveRoom>();
        queue.Enqueue(CurrentRoom_S);
        bool foundPath = false;
        
        while (queue.Count > 0 && !foundPath)
        {
            CaveRoom currentSearchRoom = queue.Dequeue();
            if (currentSearchRoom == destinationRoom)
            {
                foundPath = true;
            }
                
            for (int i = 0; i < currentSearchRoom.caveRoomConnectPoints.Length; i++)
            {
                if (!currentSearchRoom.caveRoomConnectPoints[i].ConnectedRoom.PF_Explored)
                {
                    currentSearchRoom.caveRoomConnectPoints[i].ConnectedRoom.PF_Explored = true;
                    currentSearchRoom.caveRoomConnectPoints[i].ConnectedRoom.PF_ParentConnectPoint = currentSearchRoom.caveRoomConnectPoints[i].ConnectedPoint;
                    queue.Enqueue(currentSearchRoom.caveRoomConnectPoints[i].ConnectedRoom);
                }
            }
        }
        
        // Trace path back from destination room
        roomPath_s.Clear();
        CaveRoom currentTracebackRoom = destinationRoom;
        roomPath_s.Add(destinationRoom.caveRoomCenterTraverse);
        roomPath_s.Add(destinationRoom.PF_ParentConnectPoint);
        while (currentTracebackRoom.PF_ParentConnectPoint.ConnectedRoom != CurrentRoom_S)
        {
            roomPath_s.Add(currentTracebackRoom.PF_ParentConnectPoint.ConnectedRoom.caveRoomCenterTraverse);
            roomPath_s.Add(currentTracebackRoom.PF_ParentConnectPoint.ConnectedRoom.PF_ParentConnectPoint);
            currentTracebackRoom = currentTracebackRoom.PF_ParentConnectPoint.ConnectedRoom;
        }
        roomPath_s.Add(CurrentRoom_S.caveRoomCenterTraverse);

        string cavePath = "Cave Path: ";
        foreach (CaveRoomConnectPoint connectPoint in roomPath_s)
        {
            cavePath += connectPoint.Name + ", ";
        }
        Debug.Log(cavePath);

        return true;
    }
    
    #region States

    private float startTime;
    private float currentTime;
    private float speed = 1.0f;
    private float expectedTime;
    private Vector3 startPosition;
    private Vector3 destinationPosition;

    private void ChangeNextDestination()
    {
        if (roomPath_s.Count <= 0) return;
        
        startTime = Time.time;
        currentTime = Time.time;
        startPosition = transform.position;
        destinationPosition = roomPath_s[^1].transform.position + new Vector3(0, TRAVERSE_HEIGHT_OFFSET, 0);
        expectedTime = Vector3.Distance(startPosition, destinationPosition) / speed;
        
        Debug.Log("changing destination to: " + roomPath_s[^1].Name);
    }

    private NetworkObject SeePlayerCheck()
    {
        Vector3 start = _raycastStartPoint.position;
        Vector3 end = _raycastStartPoint.position + (transform.forward * SIGHT_DISTANCE);
        if (Physics.SphereCast(start, SIGHT_RADIUS, transform.forward, out var hit, SIGHT_DISTANCE, _playerMask))
        {
            return hit.transform.gameObject.GetComponent<NetworkObject>();
        }
        else
        {
            return null;
        }
    }
    
    private void Idle_OnEnter()
    {
        if (IsServer)
        {
            CreateRoomPath_S(DungeonManager.Instance.GetRandomCaveRoom(CurrentRoom_S.DungeonNum, CurrentRoom_S));
            ChangeNextDestination();
        }
    }
    
    private void Idle_Update()
    {
        if (IsServer)
        {
            NetworkObject player = SeePlayerCheck();
            if (player != null)
            {
                _targetPlayer_s = player;
                _closestPlayerState_s = player.GetComponent<PlayerStateData>();
                ChangeState_ServerRpc((int) ServerStates.Following);
            }
            // If we're at the destination room
            else if (roomPath_s.Count == 0)
            {
                CreateRoomPath_S(DungeonManager.Instance.GetRandomCaveRoom(CurrentRoom_S.DungeonNum, CurrentRoom_S));
                ChangeNextDestination();
            }
            else
            {
                currentTime += Time.deltaTime;
                transform.position = Vector3.Lerp(startPosition, destinationPosition,
                    (currentTime - startTime)/ expectedTime);
                transform.LookAt(destinationPosition);
            
                if (Vector3.Distance(transform.position, destinationPosition) < 0.1f)
                {
                    CurrentRoom_S = roomPath_s[^1].SourceRoom;
                    roomPath_s.RemoveAt(roomPath_s.Count - 1);
                    ChangeNextDestination();
                }
            }
        }
    }
    
    private void Following_OnEnter()
    {
        
    }
    
    private void Following_OnUpdate()
    {
        float distanceToTravel = speed * Time.deltaTime;
        float distanceTotal = Vector3.Distance(transform.position, _targetPlayer_s.transform.position);

        if (distanceTotal < 1.0f)
        {
            ChangeState_ServerRpc((int) ServerStates.AttackingPlayer);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, _targetPlayer_s.transform.position, distanceToTravel/distanceTotal);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(_targetPlayer_s.transform.position - transform.position), 0.3f);

        }
    }

    private void Attacking_OnEnter()
    {
        
    }
    
    private void Attacking_OnUpdate()
    {
        if (IsServer)
        {
            NetworkObject player = SeePlayerCheck();
            if (player == null)
            {
                ChangeState_ServerRpc((int) ServerStates.Idle);
            }
        }
    }
    
    #endregion
}
