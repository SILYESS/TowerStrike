using Fusion;
using UnityEngine;
using System.Linq;

namespace Network
{
    public class NetworkPlayerProfile : NetworkBehaviour
    {
        [Networked]
        [Capacity(32)]
        public string NetworkedName { get; set; }

        [Networked]
        [Capacity(32)]
        public string NetworkedAvatarId { get; set; }

        private string _lastName;
        private string _lastAvatarId;

        public override void Spawned()
        {
            Debug.Log($"[NetworkPlayerProfile] Spawned. HasInputAuthority: {Object.HasInputAuthority}");
            if (Object.HasInputAuthority)
            {
                if (PlayerProfile.Instance != null)
                {
                    string name = PlayerProfile.Instance.GetPlayerName();
                    string avatarId = PlayerProfile.Instance.GetCurrentAvatarId();

                    // Call RPC to tell the server our details
                    RPC_SetProfileData(name, avatarId);

                    Debug.Log($"[NetworkPlayerProfile] Sending Local Data: {name} / {avatarId}");
                }
                else
                {
                    Debug.LogError("[NetworkPlayerProfile] PlayerProfile.Instance is NULL!");
                }
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetProfileData(string name, string avatarId)
        {
            Debug.Log($"[NetworkPlayerProfile] Server received data from {Object.InputAuthority}: {name}");
            NetworkedName = name;
            NetworkedAvatarId = avatarId;
        }

        public override void Render()
        {
            // Regularly check for changes in the networked data to update the UI
            // This replaces the OnChanged callback which was causing compile errors
            if (!Object.HasInputAuthority)
            {
                if (_lastName != NetworkedName || _lastAvatarId != NetworkedAvatarId)
                {
                    Debug.Log($"[NetworkPlayerProfile] Remote Data Changed: {NetworkedName} / {NetworkedAvatarId} (Old: {_lastName})");
                    UpdateUI();
                    _lastName = NetworkedName;
                    _lastAvatarId = NetworkedAvatarId;
                }
            }
        }

        private void UpdateUI()
        {
            if (!Object.HasInputAuthority)
            {
                var panel = FindObjectOfType<SearchingOpponentPanel>(true);
                if (panel != null)
                {
                    Sprite avatarSprite = null;
                    if (PlayerProfile.Instance != null)
                    {
                        var avatarData = PlayerProfile.Instance.avatarDatabase.FirstOrDefault(a => a.id == NetworkedAvatarId);
                        if (avatarData != null) avatarSprite = avatarData.icon;
                    }

                    Debug.Log($"[NetworkPlayerProfile] Updating UI with: {NetworkedName}");
                    panel.SetOpponentData(NetworkedName, avatarSprite);
                }
                else
                {
                    Debug.LogWarning("[NetworkPlayerProfile] SearchingOpponentPanel not found!");
                }
            }
        }
    }
}
