using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Doozy.Engine;
using LitJson;
using Doozy.Engine.UI;

/// <summary>
/// Object that communicates with the Firebase plugin in Javascript. This gameobject must be named Firebase.
/// </summary>
public class FirebaseHandler : MonoBehaviour {
    [SerializeField]
    private GlobalSettings _globalSettings;
    [SerializeField]
    private LoginRegisterHandler _loginHandler;

    private FirebaseHandler _instance;
    public List<GlobalVariableState> LastGameStates {
        get; private set;
    }

    public List<Item> LastItems {
        get; private set;
    }
    public LoginResult LastLoginResult {
        get; private set;
    }

    private void Awake() {
        name = "Firebase";
        if ( _instance ) {
            Destroy( _instance.gameObject );
            _instance = null;
        }
        else {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append( "Directivas: " );
#if LOCAL
            builder.Append( "Local" );
#endif
#if FORCE_FIREBASE
            builder.Append( "Force Firebase" );
#endif
#if FORCE_LOCAL_DATA
            builder.Append( "Force Local Data" );
#endif
            Debug.Log( builder.ToString() );
        }

        DontDestroyOnLoad( gameObject );
        FirebaseWrapper.UpdateStartGame();
        _instance = this;
    }

    private void Start() {
        FirebaseWrapper.CheckLoginStatus();
    }

    private void OnDestroy() {
        FirebaseWrapper.UpdateEndGame();
        FirebaseWrapper.SignOut();
    }

    // Se envía a todos los objetos de juego antes de que se cierre la aplicación
    private void OnApplicationQuit() {
        FirebaseWrapper.UpdateEndGame();
    }

    #region Login Functions
    public void TryLogin( string phone ) {
        Debug.Log( $"#Firebase#Attempting login {phone}" );
        FirebaseWrapper.SignIn( phone ); //This could result in a call to SMSSent or SMSError.
        SendMessageDelayed( "LoginBegin" );
    }

    public void TryConfirmCode( string code ) {
        Debug.Log( $"#Firebase#Sending code {code}" );
        FirebaseWrapper.CheckVerificationCode( code );
    }

    /// <summary>
    /// Callback from javascript when the verificationCode was sent.
    /// </summary>
    public void SMSSent() {
        Debug.Log( "#Firebase#SMS was sent, waiting for confirmation" );
        FirebaseWrapper.CheckLoginStatus();
        SendMessageDelayed( "ConfirmationSent" );
    }

    /// <summary>
    /// Callback from javascript when there was an error in the login
    /// </summary>
    public void SMSError( string jsonError ) {
        Debug.Log( "#Firebase#Login failed" );
        SendMessageDelayed( "ConfirmationFailed" );
        _loginHandler.ShowError( LitJson.JsonMapper.ToObject<ErrorData>( jsonError ) );
    }

    /// <summary>
    /// Callback from javascript called when the login was succesfull
    /// </summary>
    public void LoginSuccess( string jsonLoginResult ) {
        Debug.Log( $"#Firebase#Login was a success with result = {jsonLoginResult}" );
        LastLoginResult = JsonMapper.ToObject<LoginResult>( jsonLoginResult );
        var message = LastLoginResult.FormFilled ? "ConfirmationCompleted_FormFilled" : "ConfirmationCompleted";
        SendMessageDelayed( message );
    }

    /// <summary>
    /// Callback from javascript called when there was an error during the login
    /// </summary>
    public void LoginError( string jsonError ) {
        Debug.Log( "#Firebase#There was an error in the login" );
        SendMessageDelayed( "ConfirmationFailed" );
        _loginHandler.ShowError( LitJson.JsonMapper.ToObject<ErrorData>(jsonError) );
    }

    public void SaveLoginData() {
        Debug.Log( "#Firebase#Saving Data" );
        var data = LoginRegisterHandler.currentData;
#if DONT_SAVE
        OnRegisterDataSave();
        return;
#endif
        FirebaseWrapper.SaveLoginData( LastLoginResult.Id, data );
    }

    public void OnRegisterDataSave() {
        SendMessageDelayed( "RegisterDataFinish" );
    }

    public void OnRegisterDataError() {
        SendMessageDelayed( "RegisterDataError" );
    }

    public void RetrieveLoginData() {
        FirebaseWrapper.StartOperation( FirebaseWrapper.OperationGlobalSettings );
#if DONT_SAVE
        OnRetrieveLoginData( LocalDataProvider.Instance.RegistrationJson );
        return;
#endif
        FirebaseWrapper.LoadLoginData( LastLoginResult.Id );
    }

    public void OnRetrieveLoginData( string jsonData ) {
        Debug.Log( $"#Firebase#OnRetrieveLoginData: {jsonData}" );
        var data = JsonMapper.ToObject<RegistrationData>( jsonData );
        _globalSettings.data = data;
        SendMessageDelayed( "RetrieveDataComplete" );
        FirebaseWrapper.CompleteOperation( FirebaseWrapper.OperationGlobalSettings );
    }

    public void OnRetrieveLoginError() {
        SendMessageDelayed( "RetrieveDataError" );
    }
    #endregion

    #region Data Load Save
    public void SaveGameState( GlobalVariableState state ) {
        Debug.Log( $"#Firebase#Saving gameState: {state.CompleteId}." );
        FirebaseWrapper.StartOperation( FirebaseWrapper.OperationGameState );
        FirebaseWrapper.UpdateGameStates( state );
    }

    public void RequestGameStates() {
        FirebaseWrapper.StartOperation( FirebaseWrapper.OperationGameState );
        LastGameStates = new List<GlobalVariableState>();
        FirebaseWrapper.GetGameStates( LastLoginResult.Id );
    }

    public void ReceiveGameStates( string states ) {
        Debug.Log( $"#Firebase#Received gameStates: {states}." );
        try {
            LastGameStates = JsonMapper.ToObject<List<GlobalVariableState>>( states );
        }
        catch {
            ReceiveGameStatesError();
            return;
        }
        FirebaseWrapper.CompleteOperation( FirebaseWrapper.OperationGameState );
    }

    public void ReceiveGameStatesError() {
        Debug.Log( $"#Firebase#Communication error for ReceiveGameStates. Continue with empty array." );
        LastGameStates = new List<GlobalVariableState>();
        FirebaseWrapper.CompleteOperation( FirebaseWrapper.OperationGameState );
    }

    public void SaveItem( Item item ) {
        Debug.Log( $"#Firebase#Saving item: {item.CompleteId}." );
        FirebaseWrapper.StartOperation( FirebaseWrapper.OperationItems );
        FirebaseWrapper.UpdateItems( item );
    }

    public void RequestItems() {
        FirebaseWrapper.StartOperation( FirebaseWrapper.OperationItems );
        LastItems = new List<Item>();
        FirebaseWrapper.GetItems( LastLoginResult.Id );
    }

    public void ReceiveItems( string items ) {
        Debug.Log( $"#Firebase#Received items: {items}" );
        try {
            LastItems = JsonMapper.ToObject<List<Item>>( items );
        }
        catch {
            ReceiveItemsError();
            return;
        }
        FirebaseWrapper.CompleteOperation( FirebaseWrapper.OperationItems );
    }

    public void ReceiveItemsError() {
        Debug.Log( $"#Firebase#Communication error for ReceiveItemsError. Continue with empty array." );
        LastItems = new List<Item>();
        FirebaseWrapper.CompleteOperation( FirebaseWrapper.OperationItems );
    }
    #endregion

    #region Properties Edit
    public void UpdateVolume( float volume ) {
        Debug.Log( $"Saving volume {volume}" );
    }

    public void UpdateFontSize( int size ) {
        Debug.Log( $"Saving fontSize {size}" );
    }

    public void ApplyChanges() {
        if ( GlobalSettingsSetter.isFontDirty ) {
            GlobalSettingsSetter.isFontDirty = false;
            UpdateFontSize( _globalSettings.data.FontSize );
        }
        if ( GlobalSettingsSetter.isVolumeDirty ) {
            GlobalSettingsSetter.isVolumeDirty = false;
            UpdateVolume( (float) _globalSettings.data.Volume );
        }
    }
    #endregion

    private void SendMessageDelayed( string message ) {
        StartCoroutine( DelayedMessage( message ) );
    }

    private IEnumerator DelayedMessage( string message ) {
        yield return new WaitForSeconds( 1.0f );
        Debug.Log( $"#GameEvents#Sending Game event message: {message}" );
        GameEventMessage.SendEvent( message );
    }
}

public class LoginResult {
    public string Id;
    public bool FormFilled;
}