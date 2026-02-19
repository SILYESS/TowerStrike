using Firebase.Auth;
using Firebase;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using Google;
using Facebook.Unity;

namespace User
{
    public class AuthManager : MonoBehaviour
    {
        [Header("Firebase Auth Settings")]
        private FirebaseAuth auth;

        [Header("Email Sign Up References")]
        public InputField emailInputSignUP;
        public InputField passwordInputSignUP;
        public InputField confirmPasswordInputSignUP;

        [Header("Email Sign In References")]
        public InputField emailInputSignIn;
        public InputField passwordInputSignIN;

        [Header("Google Sign In References")]
        [SerializeField] private Button googleButton;

        [Header("Facebook Sign In References")]
        [SerializeField] private Button facebookButton;
        private const string AUTH_TYPE_FACEBOOK = "Facebook";

        [Header("UI References")]
        public Text statusText;

        private const string PLAYER_NAME_KEY = "PlayerName";
        private const string GOOGLE_WEB_CLIENT_ID = "780544107064-7g2pq9a09jhpd4634ip1q6erdng5luo0.apps.googleusercontent.com";

        // Auto-login keys
        private const string KEY_EMAIL = "Auth_Email";
        private const string KEY_PASSWORD = "Auth_Password";
        private const string KEY_AUTH_TYPE = "Auth_Type";
        private const string AUTH_TYPE_EMAIL = "Email";
        private const string AUTH_TYPE_GOOGLE = "Google";

#if UNITY_ANDROID || UNITY_IOS
        private readonly GoogleSignInConfiguration googleConfiguration = new()
        {
            WebClientId = GOOGLE_WEB_CLIENT_ID,
            RequestIdToken = true,
            RequestEmail = true,
            UseGameSignIn = false,
            RequestAuthCode = true,
        };
#endif

        private async void Start()
        {
            try
            {
                UpdateStatus("Checking dependencies...");

                // Initialize Firebase and check dependencies
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

                if (dependencyStatus == DependencyStatus.Available)
                {
                    InitializeFirebase();
                    InitializeGoogleSignIn();
                    InitializeFacebookSignIn();

                    if (auth != null && auth.CurrentUser != null)
                    {
                        Debug.LogFormat("[AuthManager] Session persistent login detected: {0}", auth.CurrentUser.UserId);
                        UpdateStatus($"Welcome back, {auth.CurrentUser.DisplayName ?? "Player"}!");
                        HandleSuccessfulLogin(auth.CurrentUser);
                    }
                    else
                    {
                        AttemptAutoLogin();
                    }
                }
                else
                {
                    Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
                    UpdateStatus($"Firebase Error: {dependencyStatus}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthManager] Exception in Start: {e.Message}\n{e.StackTrace}");
                UpdateStatus($"Init Error: {e.Message}");
            }

            if (googleButton != null)
            {
                googleButton.onClick.AddListener(OnGoogleSignInClicked);
                if (facebookButton != null) facebookButton.onClick.AddListener(OnFacebookSignInClicked);
            }
        }

        private async void AttemptAutoLogin()
        {
            if (!PlayerPrefs.HasKey(KEY_AUTH_TYPE)) return;

            string authType = PlayerPrefs.GetString(KEY_AUTH_TYPE);
            UpdateStatus($"Attempting auto-login via {authType}...");

            if (authType == AUTH_TYPE_EMAIL)
            {
                string email = PlayerPrefs.GetString(KEY_EMAIL, "");
                string password = PlayerPrefs.GetString(KEY_PASSWORD, "");

                if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
                {
                    try
                    {
                        AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);
                        if (result.User != null)
                        {
                            HandleSuccessfulLogin(result.User);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Auto-login (Email) failed: {e.Message}");
                    }
                }
            }
            else if (authType == AUTH_TYPE_GOOGLE)
            {
#if UNITY_ANDROID || UNITY_IOS
                try
                {
                    // Silent sign-in
                    GoogleSignInUser googleUser = await GoogleSignIn.DefaultInstance.SignInSilently();
                    if (googleUser != null)
                    {
                        Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
                        FirebaseUser firebaseUser = await auth.SignInWithCredentialAsync(credential);
                        if (firebaseUser != null)
                        {
                            HandleSuccessfulLogin(firebaseUser);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Auto-login (Google) failed: {e.Message}");
                }
#endif
            }
        }

        private void InitializeFirebase()
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            auth = FirebaseAuth.GetAuth(app);
            UpdateStatus("Firebase initialized.");
            Debug.Log("[AuthManager] Firebase initialized with App: " + app.Name);
        }

        private void InitializeGoogleSignIn()
        {
#if UNITY_ANDROID || UNITY_IOS
            GoogleSignIn.Configuration = googleConfiguration;
            GoogleSignIn.Configuration.UseGameSignIn = false;
#endif
        }
        private void InitializeFacebookSignIn()
        {
            if (!FB.IsInitialized)
            {
                FB.Init(() =>
                {
                    if (FB.IsInitialized)
                    {
                        FB.ActivateApp();
                        Debug.Log("[AuthManager] Facebook Initialized");
                        // Check for auto-login specifically if prefs say Facebook
                        CheckFacebookAutoLogin();
                    }
                    else
                    {
                        Debug.LogError("[AuthManager] Failed to initialize Facebook SDK");
                    }
                });
            }
            else
            {
                FB.ActivateApp();
                CheckFacebookAutoLogin();
            }
        }

        #region Email/Password Auth

        public async void SignUpUser()
        {
            string email = emailInputSignUP.text;
            string password = passwordInputSignUP.text;
            string confirmPassword = confirmPasswordInputSignUP.text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                UpdateStatus("Please enter email and password.");
                return;
            }

            if (password != confirmPassword)
            {
                UpdateStatus("Passwords do not match.");
                return;
            }

            if (auth == null)
            {
                UpdateStatus("Firebase not initialized.");
                return;
            }

            try
            {
                UpdateStatus("Signing up...");
                AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
                FirebaseUser newUser = result.User;

                Debug.LogFormat("Firebase user created successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
                UpdateStatus("Sign up successful!");

                // Save credentials
                PlayerPrefs.SetString(KEY_EMAIL, email);
                PlayerPrefs.SetString(KEY_PASSWORD, password);
                PlayerPrefs.SetString(KEY_AUTH_TYPE, AUTH_TYPE_EMAIL);
                PlayerPrefs.Save();

                HandleSuccessfulLogin(newUser);
            }
            catch (Exception ex)
            {
                Debug.LogError("Sign up failed: " + ex);
                UpdateStatus($"Sign up failed: {GetErrorMessage(ex)}");
            }
        }

        public async void SignInUser()
        {
            string email = emailInputSignIn.text;
            string password = passwordInputSignIN.text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                UpdateStatus("Please enter email and password.");
                return;
            }

            if (auth == null)
            {
                UpdateStatus("Firebase not initialized.");
                return;
            }

            try
            {
                UpdateStatus("Signing in...");
                AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);
                FirebaseUser user = result.User;

                Debug.LogFormat("User signed in successfully: {0} ({1})", user.DisplayName, user.UserId);
                UpdateStatus("Sign in successful!");

                // Save credentials
                PlayerPrefs.SetString(KEY_EMAIL, email);
                PlayerPrefs.SetString(KEY_PASSWORD, password);
                PlayerPrefs.SetString(KEY_AUTH_TYPE, AUTH_TYPE_EMAIL);
                PlayerPrefs.Save();

                HandleSuccessfulLogin(user);
            }
            catch (Exception ex)
            {
                Debug.LogError("Sign in failed: " + ex);
                UpdateStatus($"Sign in failed: {GetErrorMessage(ex)}");
            }
        }

        #endregion

        #region Google Auth

        private void OnGoogleSignInClicked()
        {
            Debug.Log("[AuthManager] Google Sign-In Clicked");
            UpdateStatus("Starting Google Sign-In...");
            SignInWithGoogle();
        }

        private async void SignInWithGoogle()
        {
            try
            {
#if UNITY_EDITOR
                Debug.LogWarning("Google Sign-In not supported in Editor. Simulating...");
                string mockName = "Editor Player";

                // Simulate successful login
                UpdateStatusFormat("Simulated Login: {0}", mockName);

                // Save mock auth type
                PlayerPrefs.SetString(KEY_AUTH_TYPE, AUTH_TYPE_GOOGLE);
                PlayerPrefs.Save();

                PlayerPrefs.SetString(PLAYER_NAME_KEY, mockName);
                if (PlayerProfile.Instance != null)
                {
                    PlayerProfile.Instance.ChangePlayerName(mockName);
                }

                // Navigate to main
                HandleCustomLoginSuccess();
#elif UNITY_ANDROID || UNITY_IOS
                var googleUser = await GoogleSignIn.DefaultInstance.SignIn();

                if (googleUser == null)
                {
                    UpdateStatus("Google Sign-In Cancelled/Failed (No User)");
                    return;
                }

                UpdateStatus("Google Auth successful. Authenticating Firebase...");

                Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
                FirebaseUser firebaseUser = await auth.SignInWithCredentialAsync(credential);

                Debug.LogFormat("Firebase User Signed In via Google: {0} ({1})", firebaseUser.DisplayName, firebaseUser.UserId);
                UpdateStatus("Google Firebase Login Successful!");

                // Save auth type for Google
                PlayerPrefs.SetString(KEY_AUTH_TYPE, AUTH_TYPE_GOOGLE);
                PlayerPrefs.Save();

                HandleSuccessfulLogin(firebaseUser);
#else
                UpdateStatus("Google Sign-In not supported on this platform.");
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"Google Sign-In Exception: {e.Message}");
                UpdateStatus($"Google Login Error: {e.Message}");
            }
        }

        #endregion

        #region Facebook Auth

        private async void CheckFacebookAutoLogin()
        {
            // Only run if user selected Facebook as their last login method
            if (PlayerPrefs.GetString(KEY_AUTH_TYPE) == AUTH_TYPE_FACEBOOK)
            {
                if (FB.IsLoggedIn && AccessToken.CurrentAccessToken != null)
                {
                    UpdateStatus("Restoring Facebook session...");
                    try
                    {
                        Credential credential = FacebookAuthProvider.GetCredential(AccessToken.CurrentAccessToken.TokenString);
                        FirebaseUser firebaseUser = await auth.SignInWithCredentialAsync(credential);
                        HandleSuccessfulLogin(firebaseUser);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Facebook Auto-login failed: " + e.Message);
                        UpdateStatus("Session expired. Please login.");
                    }
                }
            }
        }
        private void OnFacebookSignInClicked()
        {
            Debug.Log("[AuthManager] Facebook Sign-In Clicked");
            UpdateStatus("Starting Facebook Sign-In...");
            SignInWithFacebook();
        }

        private async void SignInWithFacebook()
        {
            try
            {
#if UNITY_EDITOR
                // Editor simulation logic (similar to your Google logic)
                Debug.LogWarning("Facebook Sign-In not fully supported in Editor. Simulating...");
                string mockName = "FB Editor Player";
                UpdateStatusFormat("Simulated FB Login: {0}", mockName);

                PlayerPrefs.SetString(KEY_AUTH_TYPE, AUTH_TYPE_FACEBOOK);
                PlayerPrefs.Save();
                PlayerPrefs.SetString(PLAYER_NAME_KEY, mockName);

                if (PlayerProfile.Instance != null) PlayerProfile.Instance.ChangePlayerName(mockName);
                HandleCustomLoginSuccess();

#elif UNITY_ANDROID || UNITY_IOS
        // Call Facebook Login
        // Define permissions (public_profile is mandatory, email is recommended)
        var perms = new List<string>() { "public_profile", "email" };
        
        // We use a TaskCompletionSource to await the Facebook callback
        var tcs = new TaskCompletionSource<ILoginResult>();
        
        FB.LogInWithReadPermissions(perms, (result) => 
        {
            tcs.SetResult(result);
        });

        ILoginResult loginResult = await tcs.Task;

        if (FB.IsLoggedIn && loginResult.AccessToken != null)
        {
            UpdateStatus("Facebook Auth successful. Connecting to Firebase...");
            
            // Get the token
            string accessToken = loginResult.AccessToken.TokenString;
            
            // Create Firebase Credential
            Credential credential = FacebookAuthProvider.GetCredential(accessToken);
            
            // Sign in to Firebase
            FirebaseUser firebaseUser = await auth.SignInWithCredentialAsync(credential);
            
            Debug.LogFormat("Firebase User Signed In via Facebook: {0} ({1})", firebaseUser.DisplayName, firebaseUser.UserId);
            UpdateStatus("Facebook Login Successful!");

            // Save Auth Type for auto-login
            PlayerPrefs.SetString(KEY_AUTH_TYPE, AUTH_TYPE_FACEBOOK);
            PlayerPrefs.Save();

            HandleSuccessfulLogin(firebaseUser);
        }
        else
        {
            if (loginResult.Error != null)
            {
                Debug.LogError("Facebook Login Error: " + loginResult.Error);
                UpdateStatus("FB Error: " + loginResult.Error.Message);
            }
            else
            {
                UpdateStatus("Facebook Login Cancelled.");
            }
        }
#else
        UpdateStatus("Facebook Sign-In not supported on this platform.");
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"Facebook Sign-In Exception: {e.Message}");
                UpdateStatus($"FB Login Error: {e.Message}");
            }
        }
        #endregion
        public void SignOutUser()
        {
            // Clear credentials
            PlayerPrefs.DeleteKey(KEY_EMAIL);
            PlayerPrefs.DeleteKey(KEY_PASSWORD);
            PlayerPrefs.DeleteKey(KEY_AUTH_TYPE);
            PlayerPrefs.Save();

            if (auth != null)
            {
                auth.SignOut();
#if UNITY_ANDROID || UNITY_IOS
                GoogleSignIn.DefaultInstance.SignOut();
                if (FB.IsLoggedIn) 
                {
                    FB.LogOut();
                }
#endif
                UpdateStatus("Signed out.");
            }
        }

        private async void HandleSuccessfulLogin(FirebaseUser user)
        {
            string displayName = string.IsNullOrEmpty(user.DisplayName) ? "Player" : user.DisplayName;
            PlayerPrefs.SetString(PLAYER_NAME_KEY, displayName);

            if (PlayerProfile.Instance != null)
            {
                // Assuming PlayerProfile logic handles name change immediately
                PlayerProfile.Instance.ChangePlayerName(displayName);
            }

            if (FirebaseManager.Instance != null)
            {
                await FirebaseManager.Instance.LoadPlayerData();
            }
            HandleCustomLoginSuccess();

        }

        // Separate method for navigation/post-login common logic
        private void HandleCustomLoginSuccess()
        {
            UpdateStatus("Loading game...");
            SceneManager.LoadScene("Main");
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log("[AuthManager] " + message);
        }

        private void UpdateStatusFormat(string format, params object[] args)
        {
            UpdateStatus(string.Format(format, args));
        }

        private string GetErrorMessage(Exception exception)
        {
            FirebaseException firebaseEx = exception as FirebaseException;
            if (firebaseEx != null)
            {
                return firebaseEx.Message;
            }

            if (exception.InnerException is FirebaseException innerFirebaseEx)
            {
                return innerFirebaseEx.Message;
            }

            return exception.Message;
        }
    }
}