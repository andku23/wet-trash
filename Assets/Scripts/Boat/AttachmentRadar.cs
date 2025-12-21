using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class AttachmentRadar : NetworkBehaviour, IInteractable, IAttachment
{
    [SerializeField] private WorldspaceInstruction _worldspaceInstruction;
    [SerializeField] private TextMeshProUGUI _infoText;
    [SerializeField] private GameObject sonarEffect;
    
    
    private List<RaycastHit> pingedItems = new List<RaycastHit>();
    private Coroutine pingCoroutine;
    private float scanRadius = 20f;
    private float revealSpeed = 8f;
    private float minScanDepth = 20;
    
    public void Interact(ulong networkPlayerID, InteractionButtonType buttonType)
    {
        DoPing_ServerRpc(NetworkManager.LocalClientId);
    }
    
    public bool EnableInteractable(IHoldable heldObject)
    {
        _worldspaceInstruction.SetVisible(true);
        return true;
    }
    
    public void DisableInteractable()
    {
        _worldspaceInstruction.SetVisible(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DoPing_ServerRpc(ulong networkPlayerID)
    {
        DoPing_ClientRpc(networkPlayerID);
    }
    
    [ClientRpc]
    private void DoPing_ClientRpc(ulong networkPlayerID)
    {
        Vector3 start = transform.position;
        Vector3 end = start - transform.up * 1000f;
        _infoText.text = "Scanning...";
        GameManager.Instance.AudioSource.PlaySound(PlayerAudioSource.SoundType.RadarPing);
        
        foreach (RaycastHit pinged in pingedItems)
        {
            if(pinged.transform != null)
                pinged.transform.GetComponent<ColliderReference>().reference.GetComponent<ItemInstance>().SetHighlight(false);
        }
        pingedItems.Clear();
        
        RaycastHit[] hits = Physics.CapsuleCastAll(start, end, scanRadius, Vector3.down, Mathf.Infinity);
        
        foreach (RaycastHit hit in hits)
        {
            ColliderReference colliderRef = hit.transform.GetComponent<ColliderReference>();
            if (colliderRef != null)
            {
                var itemInstance = colliderRef.reference.GetComponent<ItemInstance>();
                if(itemInstance != null)
                    pingedItems.Add(hit);
            }
        }
        
        pingedItems.Sort((a,b) =>  a.distance.CompareTo(b.distance));
        
        if(pingCoroutine != null)
            StopCoroutine(pingCoroutine);

        pingCoroutine = StartCoroutine(Co_AnimatePing());
    }

    private float currentPingRevealDistance;
    private IEnumerator Co_AnimatePing()
    {
        currentPingRevealDistance = 0;
        int currentRevealedIndex = 0;
        sonarEffect.SetActive(true);
        sonarEffect.transform.localScale = new Vector3(scanRadius * 2f, 0.01f, scanRadius * 2f);
        while (currentPingRevealDistance < minScanDepth)
        {
            //Debug.Log(currentPingRevealDistance + " " + currentRevealedIndex + " " + pingedItems.Count);
            currentPingRevealDistance += revealSpeed * Time.deltaTime;
            sonarEffect.transform.position = new Vector3(transform.position.x, -currentPingRevealDistance, transform.position.z);
                
            if (currentRevealedIndex < pingedItems.Count && currentPingRevealDistance >= pingedItems[currentRevealedIndex].distance)
            {
                _infoText.text = "Found " + (currentRevealedIndex + 1) + " so far...";
                pingedItems[currentRevealedIndex].transform.GetComponent<ColliderReference>().reference.GetComponent<ItemInstance>().SetHighlight(true);
                Debug.Log(pingedItems[currentRevealedIndex].transform.position);
                currentRevealedIndex++;
            }
            yield return null;
        }
        pingCoroutine = null;
        sonarEffect.SetActive(false);
        _infoText.text = "Finished!\nFound " + (currentRevealedIndex + 1) + " so far...";
    }
}
