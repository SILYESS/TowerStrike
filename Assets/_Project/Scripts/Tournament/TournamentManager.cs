using System;
using UnityEngine;

public class TournamentManager : MonoBehaviour
{
    public static TournamentManager Instance;

    // Keys
    private const string KEY_ROUND = "Tournament_CurrentRound";
    private const string KEY_WEEK = "Tournament_LastPlayedWeekDate"; // Store ticks of the Monday of that week
    private const string KEY_ELIMINATED = "Tournament_IsEliminated"; // 0 or 1
    private const string KEY_WON = "Tournament_IsWinner"; // 0 or 1

    [Header("Config")]
    public int finalRewardCoins = 500;

    // State
    public int CurrentRound { get; private set; } // 0=R16, 1=QF, 2=SF, 3=Final, 4=Complete
    public bool IsEliminated { get; private set; }
    public bool IsWinner { get; private set; }

    // Runtime State
    public bool IsTournamentMatch { get; set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        CheckWeeklyReset();
    }

    private void Start()
    {
        // Debug
        Debug.Log($"[Tournament] Round: {CurrentRound}, Eliminated: {IsEliminated}, Winner: {IsWinner}");
    }

    public void CheckWeeklyReset()
    {
        // Calculate the Monday of the current week
        DateTime today = DateTime.Today;
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime thisWeekMonday = today.AddDays(-1 * diff).Date;

        long lastWeekTicks = Convert.ToInt64(PlayerPrefs.GetString(KEY_WEEK, "0"));
        long currentWeekTicks = thisWeekMonday.Ticks;

        if (lastWeekTicks < currentWeekTicks)
        {
            // New Week! Reset everything.
            ResetTournament(currentWeekTicks);
        }
        else
        {
            // Load status
            CurrentRound = PlayerPrefs.GetInt(KEY_ROUND, 0);
            IsEliminated = PlayerPrefs.GetInt(KEY_ELIMINATED, 0) == 1;
            IsWinner = PlayerPrefs.GetInt(KEY_WON, 0) == 1;
        }
    }

    private void ResetTournament(long weekTicks)
    {
        CurrentRound = 0;
        IsEliminated = false;
        IsWinner = false;

        PlayerPrefs.SetString(KEY_WEEK, weekTicks.ToString());
        PlayerPrefs.SetInt(KEY_ROUND, 0);
        PlayerPrefs.SetInt(KEY_ELIMINATED, 0);
        PlayerPrefs.SetInt(KEY_WON, 0);
        PlayerPrefs.Save();

        Debug.Log("[Tournament] Weekly Reset Performed.");
    }

    public bool TryStartMatch()
    {
        CheckWeeklyReset(); // Ensure fresh state

        if (IsEliminated)
        {
            Debug.LogWarning("[Tournament] Player is eliminated for this week.");
            return false;
        }

        if (IsWinner)
        {
            Debug.LogWarning("[Tournament] Player already won this week.");
            return false;
        }

        IsTournamentMatch = true;
        return true;
    }

    public void ReportMatchResult(bool isWin)
    {
        if (!IsTournamentMatch) return; // Ignore normal custom games

        IsTournamentMatch = false; // Reset flag

        if (isWin)
        {
            AdvanceRound();
        }
        else
        {
            EliminatePlayer();
        }
    }

    private void AdvanceRound()
    {
        CurrentRound++;

        // Check if Won the Bracket (Round 4 completed -> Won)
        // 0->1 (R16 to QF), 1->2 (QF to SF), 2->3 (SF to Final), 3->4 (Final to Win)
        if (CurrentRound >= 4)
        {
            WinTournament();
        }
        else
        {
            PlayerPrefs.SetInt(KEY_ROUND, CurrentRound);
            PlayerPrefs.Save();
            Debug.Log($"[Tournament] Advanced to Round {CurrentRound}");
        }
    }

    private void WinTournament()
    {
        IsWinner = true;
        CurrentRound = 4;

        PlayerPrefs.SetInt(KEY_WON, 1);
        PlayerPrefs.SetInt(KEY_ROUND, 4);
        PlayerPrefs.Save();

        // Award
        if (PlayerProfile.Instance != null)
        {
            PlayerProfile.Instance.AddCoins(finalRewardCoins);
        }

        Debug.Log("[Tournament] User WON the Tournament!");
    }

    private void EliminatePlayer()
    {
        IsEliminated = true;
        PlayerPrefs.SetInt(KEY_ELIMINATED, 1);
        PlayerPrefs.Save();

        Debug.Log("[Tournament] User ELIMINATED.");
    }
}
