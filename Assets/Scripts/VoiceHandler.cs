using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using HoloToolkit.Unity.SpatialMapping;

public class VoiceHandler : MonoBehaviour {

    private VoiceToPlace m_current = null;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnVoice()
    {
        Debug.Log("Place");
        if( m_current == null )
        {
            RaycastHit hit;
            int layerMask = ~( 1 << SpatialMappingManager.Instance.PhysicsLayer );
            if( Physics.Raycast( Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask ) )
            {
                m_current = hit.collider.gameObject.GetComponent<VoiceToPlace>();
                if( m_current != null )
                {
                    m_current.OnVoice();
                }
            }
        }
        else
        {
            m_current.OnVoice();
            m_current = null;
        }
    }
}
