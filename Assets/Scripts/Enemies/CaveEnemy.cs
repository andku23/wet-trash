using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CaveEnemy : BaseEnemy
{
    private CaveRoom _currentRoom_s;
    private CaveRoom _targetRoom_s;

    private float TRAVERSE_HEIGHT_OFFSET = 3.2f;
    private float SIGHT_DISTANCE = 10.0f;
    private float SIGHT_RADIUS = 3.5f;

    [SerializeField] protected Transform _raycastStartPoint;
    
    private List<CaveTraverseNode> roomPath_s = new List<CaveTraverseNode>();
    
    public CaveRoom CurrentRoom_S {get => _currentRoom_s; set => _currentRoom_s = value; }
    
    enum ServerStates
    {
        Idle = 0,
        Following = 1,
        AttackingPlayer = 2,
        Dead = 3,
        AgroAlert = 4
    }
    
    protected override IEnumerator DoDeath()
    {
        _animator_c.SetBool("IsDead", true);
        _audioSource_c.PlaySound(PlayerAudioSource.SoundType.EnemyTakeDamage);
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
        _audioSource_c.PlaySound(PlayerAudioSource.SoundType.EnemyTakeDamage);
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
        
        BaseState agro = new BaseState(AgroAlert_OnEnter, AgroAlert_OnUpdate, null);
        _stateMachine.AddState((int)ServerStates.AgroAlert, agro);
    }

    private bool CreateRoomPath_S(CaveRoom destinationRoom)
    {
        if(destinationRoom.DungeonNum != CurrentRoom_S.DungeonNum) return false;
        
        DungeonManager.Instance.ResetAllRoomSearchFlags();
        //Debug.Log("Current Room: " + CurrentRoom_S.SpawnID + " Dest Room: " + destinationRoom.SpawnID);
        
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
        //Debug.Log(cavePath);

        return true;
    }
    
    #region States

    private void ChangeNextDestination()
    {
        if (roomPath_s.Count <= 0) return;
        
        SetNextStaticTargetPosition(roomPath_s[^1].transform.position + new Vector3(0, TRAVERSE_HEIGHT_OFFSET, 0));
        
        //Debug.Log("changing destination to: " + roomPath_s[^1].Name);
    }

    private void Idle_OnEnter()
    {
        if (IsServer)
        {
            CreateRoomPath_S(DungeonManager.Instance.GetRandomCaveRoom(CurrentRoom_S.DungeonNum, CurrentRoom_S));
            ChangeNextDestination();
            speed_s = SWIM_SPEED_IDLE;
        }
    }
    
    private void Idle_Update()
    {
        if (!IsServer) return;
        NetworkObject player = FindPlayersInLOS(_raycastStartPoint.position, SIGHT_RADIUS, SIGHT_DISTANCE);
        if (player != null)
        {
            SetTargetPlayer(player);
            ChangeState_ServerRpc((int) ServerStates.AgroAlert);
        }
        // If we're at the destination room
        else if (roomPath_s.Count == 0)
        {
            CreateRoomPath_S(DungeonManager.Instance.GetRandomCaveRoom(CurrentRoom_S.DungeonNum, CurrentRoom_S));
            ChangeNextDestination();
        }
        else
        {
            SwimToNextStaticTargetPosition();
        
            if (Vector3.Distance(transform.position, targetPosition_s) < 0.1f)
            {
                CurrentRoom_S = roomPath_s[^1].SourceRoom;
                roomPath_s.RemoveAt(roomPath_s.Count - 1);
                ChangeNextDestination();
            }
        }
    }

    private void AgroAlert_OnEnter()
    {
        if (IsServer)
        {
            startTime_s = Time.time;
        }
        _animator_c.SetTrigger("DoAgro");
        
    }
    
    private void AgroAlert_OnUpdate()
    {
        if (IsServer)
        {
            if (Time.time - startTime_s >= AGRO_TIME)
            {
                ChangeState_ServerRpc((int) ServerStates.Following);
            }
        }
    }
    
    private void Following_OnEnter()
    {
        if (IsServer)
        {
            speed_s = SWIM_SPEED_CHASING;
        }
    }
    
    private void Following_OnUpdate()
    {
		if (!IsServer) return;
        float disengageDistance = MAX_FOLLOW_DISTANCE * GameManager.Instance.gameData.MONSTER_DETECTION_RANGE_MULTIPLIER;
        bool isPlayerUnreachable = IsTargetPlayerUnreachable(disengageDistance, out var distanceToPlayer);
        
        if (isPlayerUnreachable)
        {
            ChangeState_ServerRpc((int) ServerStates.Idle);
        }
        else if (distanceToPlayer < MAX_ATTACK_DISTANCE)
        {
            ChangeState_ServerRpc((int) ServerStates.AttackingPlayer);
        }
        else
        {
            SwimAtTargetPlayer();
        }
    }

    private void Attacking_OnEnter()
    {
        lastAttackTime_s = Time.time;
    }
    
    private void Attacking_OnUpdate()
    {
        if (!IsServer) return;
        
        
        bool isPlayerUnreachable = IsTargetPlayerUnreachable(MAX_ATTACK_DISTANCE, out var distanceToPlayer);
        if (isPlayerUnreachable)
        {
            ChangeState_ServerRpc((int) ServerStates.Following);
        }
        else
        {
            DefaultAttackBehaviour();
        }
    }
    
    #endregion
}
