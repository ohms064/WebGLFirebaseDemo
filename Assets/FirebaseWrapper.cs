//#define FORCE_FIREBASE
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Doozy.Engine;
using UnityEngine;

public class FirebaseWrapper {
    private static Dictionary<string, bool> _operationCompleted = new Dictionary<string, bool> ();
    public const string OperationGameState = "GameState",
        OperationItems = "Items",
        OperationGlobalSettings = "GlobalSettings";
    #region LOGIN
#if (!UNITY_EDITOR && UNITY_WEBGL && !LOCAL) || FORCE_FIREBASE
    [DllImport ("__Internal")]
    public static extern void SignIn (string phoneNumber);

    [DllImport ("__Internal")]
    public static extern void CheckVerificationCode (string verificationCode);

    [DllImport ("__Internal")]
    public static extern void SignOut ();
#else
    public static void SignIn (string phoneNumber) {
        string message = phoneNumber.Equals ("+525512345678") ? "SMSSent" : "SMSError";
        var firebase = GameObject.Find ("Firebase");
        Debug.Log ($"#Firebase#Sign in result {message} to gameobject {firebase}", firebase);
        if (message.Equals ("SMSError")) {
            Coroutiner.Start (SendMessageDelayed (firebase, message, LitJson.JsonMapper.ToJson (new ErrorData { message = "Invalid phone", code = "" })));
        }
        else {
            Coroutiner.Start (SendMessageDelayed (firebase, message));
        }
    }

    public static void CheckVerificationCode (string verificationCode) {
        string message = verificationCode.Equals ("123456") ? "LoginSuccess" : "LoginError";
        var firebase = GameObject.Find ("Firebase");
        if (verificationCode.Equals ("123456")) {
            Coroutiner.Start (SendMessageDelayed (firebase, message, LitJson.JsonMapper.ToJson (new LoginResult { Id = "1234", FormFilled = LocalDataProvider.Instance.FormFilled })));
        }
        else {
            Coroutiner.Start (SendMessageDelayed (firebase, message, LitJson.JsonMapper.ToJson (new ErrorData { message = "Invalid code", code = "" })));
        }
        Debug.Log ($"#Firebase#Verification result {message} to gameobject {firebase}", firebase);
    }

    public static void SignOut () {
        Debug.Log ("#Firebase#Signing out completed");
    }
#endif
    #endregion
    #region DATA LOAD

#if ((!UNITY_EDITOR && UNITY_WEBGL && !LOCAL) || FORCE_FIREBASE) && !FORCE_LOCAL_DATA

    [DllImport ("__Internal")]
    public static extern void SaveLoginData (string name, int age, int avatarId, float volume, int textSize);

    [DllImport ("__Internal")]
    public static extern void UpdateStartGame ();

    [DllImport ("__Internal")]
    public static extern void UpdateEndGame ();

    [DllImport ("__Internal")]
    public static extern void UpdateMissionAttempts (int missionIndex);

    [DllImport ("__Internal")]
    public static extern void UpdateGameStates (string id, float completedTime, int missionIndex,
        string nodeTechnicalName, string sceneId, int sceneTag);

    [DllImport ("__Internal")]
    public static extern void SaveLoginData (string uid, string name, int age, int avatarId, int textSize, float volume);

    [DllImport ("__Internal")]
    public static extern void GetGameStates (string uid);

    [DllImport ("__Internal")]
    public static extern void UpdateItems (string id, int amount, string name);

    [DllImport ("__Internal")]
    public static extern void GetItems (string uid);

    [DllImport ("__Internal")]
    public static extern void LoadLoginData (string uid);

    [DllImport ("__Internal")]
    public static extern void CheckLoginStatus ();

    [DllImport ("__Internal")]
    public static extern void UpdateFontSize (int size);

    [DllImport ("__Internal")]
    public static extern void UpdateVolume (float volume);
#else

    public static void CheckLoginStatus () {
        if (LocalDataProvider.Instance.alreadyLogin) {
            var firebase = GameObject.Find ("Firebase");
            var result = new LoginResult { Id = "123", FormFilled = LocalDataProvider.Instance.FormFilled };
            Coroutiner.Start (SendMessageDelayed (firebase, "LoginSuccess", LitJson.JsonMapper.ToJson(result)));
        }
    }

    public static void UpdateFontSize (int size) { }

    public static void UpdateVolume (float volume) { }

    public static void UpdateStartGame () {
        Debug.Log ("#Firebase#Sending Start game event to server");
    }

    public static void UpdateEndGame () {
        Debug.Log ("#Firebase#Sending End game event to server");
    }

    public static void UpdateMissionAttempts (int missionIndex) {
        Debug.Log ($"Updating mission index {missionIndex} attempt");
    }

    public static void UpdateGameStates (string id, float completedTime, int missionIndex,
        string nodeTechnicalName, string sceneId, int sceneTag) {
        LocalDataProvider.Instance.localManager.AddState (new GlobalVariableState {
            Id = id,
                CompletedTime = completedTime,
                MissionIndex = missionIndex,
                NodeTechnicalName = nodeTechnicalName,
                SceneId = sceneId,
                SceneTag = (SceneTransitionDestination.DestinationTag) sceneTag
        });
        CompleteOperation (OperationGameState);
        Debug.Log ($"#Firebase# Updated state with id {id}");
    }
    public static void UpdateItems (string id, int amount, string name) {
        LocalDataProvider.Instance.localManager.AddItem (new Item { Id = id });
        CompleteOperation (OperationItems);
        Debug.Log ($"#Firebase# Updated items with id {id}");
    }

    public static void SaveLoginData (string uid, string name, int age, int avatarId, int textSize, float volume) {
        var firebase = GameObject.Find ("Firebase");
        LocalDataProvider.Instance.registrationData = new RegistrationData {
            AvatarID = avatarId,
            Name = name,
            Age = age,
            Volume = volume,
            FontSize = textSize
        };
        Coroutiner.Start (SendMessageDelayed (firebase, "OnRegisterDataSave"));
    }

    public static void LoadLoginData (string uid) {
        var firebase = GameObject.Find ("Firebase");
        Coroutiner.Start (SendMessageDelayed (firebase, "OnRetrieveLoginData", LocalDataProvider.Instance.RegistrationJson));
    }

    public static void GetGameStates (string uid) {
        Debug.Log ($"#Firebase# Getting states from {LocalDataProvider.Instance.localManager.name} {uid}");
        string json = LocalDataProvider.Instance.localManager.GetStatesJson ();
        var firebase = GameObject.Find ("Firebase");
        Debug.Log ($"#Firebase# Received game states");
        Coroutiner.Start (SendMessageDelayed (firebase, "ReceiveGameStates", json));
    }

    public static void GetItems (string uid) {
        Debug.Log ($"#Firebase# Getting items from {LocalDataProvider.Instance.localManager.name} {uid}");
        string json = LocalDataProvider.Instance.localManager.GetItemsJson ();
        var firebase = GameObject.Find ("Firebase");
        Debug.Log ($"#Firebase# Received items");
        Coroutiner.Start (SendMessageDelayed (firebase, "ReceiveItems", json));
    }

    private static IEnumerator SendMessageDelayed (GameObject gameObject, string message) {
        yield return new WaitForSeconds (LocalDataProvider.Instance.Time);
        gameObject.SendMessage (message, SendMessageOptions.RequireReceiver);
    }

    private static IEnumerator SendMessageDelayed (GameObject gameObject, string message, object args) {
        yield return new WaitForSeconds (LocalDataProvider.Instance.Time);
        gameObject.SendMessage (message, args, SendMessageOptions.RequireReceiver);
    }
#endif

    public static void UpdateGameStates (GlobalVariableState state) {
        Debug.Log ($"#Firebase#Updating game state: {state.CompleteId}");
        FirebaseWrapper.UpdateGameStates (state.Id,
            (float) state.CompletedTime,
            state.MissionIndex,
            state.NodeTechnicalName,
            state.SceneId,
            (int) state.SceneTag
        );
    }

    public static void SaveLoginData (string id, RegistrationData data) {
        Debug.Log ($"#Firebase#Saving login data: Name:{data.Name}, Age:{data.Age}, Font:{data.FontSize}, Avatar:{data.AvatarID}");
        Debug.Log ($"Font size of type: {data.FontSize.GetType()}");
        FirebaseWrapper.SaveLoginData (id, data.Name, data.Age, data.AvatarID, (int) data.FontSize, (float) data.Volume);
    }

    public static void StartOperation (string key) {
        if (!_operationCompleted.ContainsKey (key)) {
            _operationCompleted.Add (key, false);
        }
        else {
            _operationCompleted[key] = false;
        }

    }

    public static System.Func<bool> CheckOperationStatus (string key) {
        return () => { return _operationCompleted[key]; };
    }
    public static void CompleteOperation (string key) {
        _operationCompleted[key] = true;
    }

    public static void UpdateItems (Item item) {
        Debug.Log ($"#Firebase#Updating item: {item.CompleteId}.");
        UpdateItems (item.Id, 0, item.Id);
    }
    #endregion
}