using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;

// The Data Container
[Serializable]
public class PlayerData
{
    public int currentLevel = 0;
    public int totalCoins = 0;
}

// The Manager
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    // This is the data you access anywhere: FirebaseManager.Instance.Data
    public PlayerData Data = new PlayerData();

    private FirebaseAuth auth;
    private DatabaseReference dbReference;

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
    }

    // Call this after Login is successful
    public async Task LoadPlayerData()
    {
        // Ensure Firebase is ready
        await FirebaseApp.CheckAndFixDependenciesAsync();

        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        if (auth.CurrentUser == null)
        {
            Debug.LogWarning("User not logged in, cannot load data.");
            return;
        }

        string userId = auth.CurrentUser.UserId;

        try
        {
            DataSnapshot snapshot = await dbReference.Child("users").Child(userId).GetValueAsync();

            if (snapshot.Exists)
            {
                string json = snapshot.GetRawJsonValue();
                Data = JsonUtility.FromJson<PlayerData>(json);
                Debug.Log($"[FirebaseManager] Loaded: Level {Data.currentLevel}, Coins {Data.totalCoins}");
            }
            else
            {
                // New user, create defaults
                Data = new PlayerData();
                await SavePlayerData();
                Debug.Log("[FirebaseManager] New profile created.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseManager] Load Error: {e.Message}");
        }
    }

    // Call this when winning a level or gaining coins
    public async Task SavePlayerData()
    {
        if (auth == null || auth.CurrentUser == null) return;

        string userId = auth.CurrentUser.UserId;
        string json = JsonUtility.ToJson(Data);

        try
        {
            await dbReference.Child("users").Child(userId).SetRawJsonValueAsync(json);
            Debug.Log("[FirebaseManager] Game Saved.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseManager] Save Error: {e.Message}");
        }
    }

    // Helper for LevelManager to add coins
    public void AddCoins(int amount)
    {
        Data.totalCoins += amount;
        SavePlayerData(); // Fire and forget save
    }

    // Helper for LevelManager to update level
    public void SetLevel(int levelIndex)
    {
        Data.currentLevel = levelIndex;
        SavePlayerData(); // Fire and forget save
    }
}