using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CaveEnemy : BaseEnemy
{
    private NetworkClient _targetPlayer_s;
    private PlayerStateData _closestPlayerState_s;
    private Collider _currentWaterBody_s;
    private CaveRoom _currentRoom_s;
    private CaveRoom _targetRoom_s;
    
    private List<CaveTraverseNode> roomPath_s = new List<CaveTraverseNode>();
    
    public CaveRoom CurrentRoom_S {get => _currentRoom_s; set => _currentRoom_s = value; }
    
    enum ServerStates
    {
        Idle = 0,
        AttackingPlayer = 1,
        Dead = 2
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
        
        BaseState attackingPlayer = new BaseState(null, null, null);
        _stateMachine.AddState((int)ServerStates.AttackingPlayer, attackingPlayer);
        
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
    private float speed = 0.2f;
    private Vector3 startPosition;
    private CaveTraverseNode destinationNode;

    private void ChangeNextDestination()
    {
        if (roomPath_s.Count <= 0) return;
        
        startTime = Time.time;
        currentTime = Time.time;
        startPosition = transform.position;
        destinationNode = roomPath_s[^1];
        
        Debug.Log("changing destination to: " + roomPath_s[^1].Name);
    }
    
    private void Idle_OnEnter()
    {
        CreateRoomPath_S(DungeonManager.Instance.GetRandomCaveRoom(CurrentRoom_S.DungeonNum, CurrentRoom_S));
        ChangeNextDestination();
    }
    
    private void Idle_Update()
    {
        // If we're at the destination room
        if (roomPath_s.Count == 0)
        {
            CreateRoomPath_S(DungeonManager.Instance.GetRandomCaveRoom(CurrentRoom_S.DungeonNum, CurrentRoom_S));
            ChangeNextDestination();
        }
        else
        {
            Vector3 endPosition = roomPath_s[^1].transform.position;
            currentTime += Time.deltaTime;
            float expectedTime = speed * Vector3.Distance(startPosition, destinationNode.transform.position);
            transform.position = Vector3.Lerp(startPosition, destinationNode.transform.position,
                (currentTime - startTime)/ expectedTime);
            transform.LookAt(endPosition);
            
            if (Vector3.Distance(transform.position, destinationNode.transform.position) < 0.1f)
            {
                CurrentRoom_S = roomPath_s[^1].SourceRoom;
                roomPath_s.RemoveAt(roomPath_s.Count - 1);
                ChangeNextDestination();
            }
        }
    }
    
    #endregion
}
