mergeInto(LibraryManager.library, {
  CheckLoginStatus: function() {
    console.log("Arrived here");
    if (currentUser) {
      currentUser
        .getIdTokenResult()
        .then(function(token) {
          const claims = token.claims;
          console.log("claims received: " + String(claims));
          if (claims.invited) {
            const data = {
              Id: currentUser.uid,
              FormFilled: claims.formFilled
            };
            unityInstance.SendMessage(
              "Firebase",
              "LoginSuccess",
              JSON.stringify(data)
            );
          } else {
            firebase.auth().signOut();
            console.log("user not valid");
            unityInstance.SendMessage("Firebase", "LoginError");
          }
        })
        .catch(function() {
          unityInstance.SendMessage("Firebase", "LoginError");
        });
    }
  },

  SignIn: function(phoneNumber) {
    const appVerifier = window.recaptchaVerifier;
    const phone = Pointer_stringify(phoneNumber);

    firebase
      .auth()
      .setPersistence(firebase.auth.Auth.Persistence.SESSION)
      .then(function() {
        console.log("Trying to sign in " + phone);
        firebase
          .auth()
          .signInWithPhoneNumber(phone, appVerifier)
          .then(function(confirmationResult) {
            // SMS sent. Prompt user to type the code from the message, then sign the
            // user in with confirmationResult.confirm(code).
            window.confirmationResult = confirmationResult;
            console.log("SMSSent");
            unityInstance.SendMessage("Firebase", "SMSSent");
          })
          .catch(function(error) {
            // Error; SMS not sent
            grecaptcha.reset(window.recaptchaWidgetId);
            console.log(error.code + " SMSError " + error);
            const smsError = {
              message: error.message,
              code: error.code
            };
            unityInstance.SendMessage("Firebase", "SMSError", JSON.stringify(smsError));
          });
      })
      .catch(function(error) {
        console.log(error.code + " Persistence error " + error);
        const persistenceError = {
          message: error.message,
          code: error.code
        };
        unityInstance.SendMessage(
          "Firebase",
          "SMSError",
          JSON.stringify(persistenceError)
        );
      });
  },
  //Syntax error
  CheckVerificationCode: function(verificationCode) {
    window.confirmationResult
      .confirm(Pointer_stringify(verificationCode))
      .then(function(result) {
        // User signed in successfully.
        currentUser = result.user;
        currentUser
          .getIdTokenResult()
          .then(function(token) {
            const claims = token.claims;
            console.log("claims received: " + String(claims));
            if (claims.invited) {
              const data = {
                Id: currentUser.uid,
                FormFilled: claims.formFilled
              };
              unityInstance.SendMessage(
                "Firebase",
                "LoginSuccess",
                JSON.stringify(data)
              );
            } else {
              firebase.auth().signOut();
              console.log("user not valid");
              const error = {
                message: "User not registered",
                code: "internal/user_not_valid"
              };
              unityInstance.SendMessage(
                "Firebase",
                "LoginError",
                JSON.stringify(error)
              );
            }
          })
          .catch(function(error) {
            const returnError = {
              message: error.message,
              code: error.code
            };
            unityInstance.SendMessage(
              "Firebase",
              "LoginError",
              JSON.stringify(returnError)
            );
          });

        // ...
      })
      .catch(function(error) {
        // User couldn't sign in (bad verification code?)
        // ...
        console.log( error.code + " Login error: " + error);
        const tokenError = {
          message: error.message,
          code: error.code
        };
        unityInstance.SendMessage(
          "Firebase",
          "LoginError",
          JSON.stringify(tokenError)
        );
      });
  },

  SignOut: function() {
    if (currentUser) {
      firebase.auth().signOut();
    }
  },

  //Syntax error
  SaveLoginData: function(uid, name, age, avatarId, textSize, volume) {
    console.log("Called succesfully");
    const id = Pointer_stringify(uid);
    console.log("stringyfy ok");
    const data = {
      Age: age,
      AvatarID: avatarId,
      Volume: 1,
      FontSize: textSize
    };
    console.log("Setting promise");

    firebase
      .firestore()
      .doc("users/" + id)
      .set(data, { merge: true })
      .then(function() {
        console.log("Registry data saved!");
        unityInstance.SendMessage("Firebase", "OnRegisterDataSave");
        updateFormFilled();
      })
      .catch(function(error) {
        console.log("Registry data NOT saved! Cause:" + error);
        unityInstance.SendMessage("Firebase", "OnRegisterDataError");
      });
  },

  LoadLoginData: function(uid) {
    const id = Pointer_stringify(uid);
    firebase
      .firestore()
      .doc("users/" + id)
      .get()
      .then(function(doc) {
        if (doc.exists) {
          const data = doc.data();
          console.log("Document data:" + data);
          unityInstance.SendMessage(
            "Firebase",
            "OnRetrieveLoginData",
            JSON.stringify(data)
          );
        } else {
          // doc.data() will be undefined in this case
          console.log("No such document!");
          unityInstance.SendMessage("Firebase", "OnRetrieveLoginError");
        }
      })
      .catch(function(error) {
        console.log("LoadLogin error. Cause: " + error);
        unityInstance.SendMessage("Firebase", "OnRetrieveLoginError");
      });
  }
});
