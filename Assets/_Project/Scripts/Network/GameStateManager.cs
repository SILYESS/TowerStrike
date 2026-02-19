using Fusion;
using System.Linq;
using Towers;
using UnityEngine;
using UnityEngine.UI;

namespace Network
{
    public class GameStateManager : NetworkBehaviour
    {
        public static GameStateManager Instance;

        [Networked] public bool GameEnded { get; set; }
        [Networked] public PlayerRef Winner { get; set; }

        private float checkTimer = 0f;

        public override void Spawned()
        {
            Instance = this;
            GameEnded = false;
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;
            if (GameEnded) return;

            checkTimer += Runner.DeltaTime;
            if (checkTimer < 1f) return;
            checkTimer = 0;

            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            int blueCount = 0;
            int redCount = 0;
            bool anyTowers = false;

            if (LevelManager.Instance == null) return;
            
            // Use Registry
            foreach (var t in LevelManager.Instance.allTowers)
            {
                if (t == null) continue;
                if (t.Object == null || !t.Object.IsValid) continue;

                if (t.type == TowerType.PlayerTower) blueCount++;
                else if (t.type == TowerType.RedTower) redCount++;
                anyTowers = true;
            }

            if (!anyTowers) return;
            if (Runner.Tick < 60) return;

            // Debug.Log($"WinCheck: Blue={blueCount} Red={redCount}");

            if (blueCount == 0 && redCount > 0)
            {
                Debug.Log("Blue Team Defeated! Client Wins.");
                if (MultiplayerManager.Instance != null && MultiplayerManager.Instance.ClientPlayer != PlayerRef.None)
                {
                    EndGame(MultiplayerManager.Instance.ClientPlayer);
                }
                else
                {
                    Debug.LogError("Client won but ClientPlayer ref is missing!");
                    // Fallback to active player check if needed, but Manager should have it
                    foreach (var p in Runner.ActivePlayers)
                    {
                        if (p != Runner.LocalPlayer) { EndGame(p); return; }
                    }
                }
            }
            else if (redCount == 0 && blueCount > 0)
            {
                Debug.Log("Red Team Defeated! Host Wins.");
                if (MultiplayerManager.Instance != null)
                {
                    EndGame(MultiplayerManager.Instance.HostPlayer);
                }
                else
                {
                    EndGame(Object.StateAuthority);
                }
            }
        }

        private void EndGame(PlayerRef winner)
        {
            GameEnded = true;
            if (MultiplayerManager.Instance != null) MultiplayerManager.Instance.gameInProgress = false;
            Winner = winner;
            RPC_ShowEndScreen(winner);
        }

        public void ForceWin(PlayerRef winner)
        {
            if (GameEnded) return;
            Debug.Log($"ForceWin triggered for: {winner}");
            EndGame(winner);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ShowEndScreen(PlayerRef winner)
        {
            StartCoroutine(ShowEndRoutine(winner));
        }

        private System.Collections.IEnumerator ShowEndRoutine(PlayerRef winner)
        {
            // Wait a bit or show immediately?
            yield return new WaitForSeconds(0.5f);
            if (MultiplayerGameHUD.Instance != null)
            {
                bool isWinner = (winner == Runner.LocalPlayer);
                MultiplayerGameHUD.Instance.ShowGameOver(isWinner);
                if (TournamentManager.Instance != null && TournamentManager.Instance.IsTournamentMatch)
                {
                    TournamentManager.Instance.ReportMatchResult(isWinner);
                }
            }
            else
            {
                string msg = (winner == Runner.LocalPlayer) ? "YOU WIN!" : "YOU LOSE!";
                Debug.Log($"GAME OVER: {msg} (HUD not found)");

                if (TournamentManager.Instance != null && TournamentManager.Instance.IsTournamentMatch)
                {
                    bool isWin = (winner == Runner.LocalPlayer);
                    TournamentManager.Instance.ReportMatchResult(isWin);
                }
            }
        }
    }
}
