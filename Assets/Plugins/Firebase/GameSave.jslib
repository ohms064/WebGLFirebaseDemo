mergeInto(LibraryManager.library, {
  UpdateStartGame: function() {
    console.log("Update: " + startGame);
    updateStats({
      property: startGame
    })
      .then(function(result) {
        console.log("Update game states result: " + result);
      })
      .catch(function(error) {
        console.log("Update Game states error: " + error);
      });
  },

  UpdateEndGame: function() {
    console.log("Update: " + endGame);
    updateStats({
      property: endGame
    })
      .then(function(result) {
        console.log("Update game states result: " + result);
      })
      .catch(function(error) {
        console.log("Update Game states error: " + error);
      });
  },

  UpdateMissionAttempts: function(missionIndex) {
    console.log("Update: " + missionAttempts);
    updateStats({
      property: missionAttempts,
      value: missionIndex
    })
      .then(function(result) {
        console.log("Update mission index result: " + result);
      })
      .catch(function(error) {
        console.log("Update mission index error: " + error);
      });
  },

  UpdateGameStates: function(
    id,
    completedTime,
    missionIndex,
    nodeTechnicalName,
    sceneId,
    sceneTag
  ) {
    console.log("Update: " + gameStates);
    updateStats({
      property: gameStates,
      value: {
        Id: Pointer_stringify(id),
        CompletedTime: completedTime,
        MissionIndex: missionIndex,
        NodeTechnicalName: Pointer_stringify(nodeTechnicalName),
        SceneId: Pointer_stringify(sceneId),
        SceneTag: sceneTag
      }
    })
      .then(function(result) {
        console.log("Update game states result: " + result);
      })
      .catch(function(error) {
        console.log("Update Game states error: " + error);
      });
  },

  UpdateItems: function(id, amount, name) {
    console.log("Update: " + items);
    updateStats({
      property: items,
      value: {
        Id: Pointer_stringify(id),
        Amount: amount,
        Name: name
      }
    })
      .then(function(result) {
        console.log("Update item result: " + result);
      })
      .catch(function(error) {
        console.log("Update item error: " + error);
      });
  },

  GetGameStates: function(uid) {
    const id = Pointer_stringify(uid);
    firebase
      .firestore()
      .doc("users/" + id + "/stats/game")
      .get()
      .then(function(doc) {
        if (doc.exists) {
          const data = doc.data();
          unityInstance.SendMessage(
            "Firebase",
            "ReceiveGameStates",
            JSON.stringify(data.GameStates)
          );
        } else {
          // doc.data() will be undefined in this case
          unityInstance.SendMessage("Firebase", "ReceiveGameStatesError");
          console.log("No such game state document!");
        }
      })
      .catch(function(error) {
        console.log("Error getting gamestate document: " + error);
        unityInstance.SendMessage("Firebase", "ReceiveGameStatesError");
      });
  },

  GetItems: function(uid) {
    const id = Pointer_stringify(uid);
    firebase
      .firestore()
      .doc("users/" + id + "/stats/game")
      .get()
      .then(function(doc) {
        if (doc.exists) {
          const data = doc.data();
          unityInstance.SendMessage(
            "Firebase",
            "ReceiveItems",
            JSON.stringify(data.Items)
          );
        } else {
          // doc.data() will be undefined in this case
          unityInstance.SendMessage("Firebase", "ReceiveItemsError");
          console.log("No such item document!");
        }
      })
      .catch(function(error) {
        console.log("Error getting item document: " + error);
        unityInstance.SendMessage("Firebase", "ReceiveItemsError");
      });
  },

  UpdateVolume: function(volume) {
    firestore()
      .doc("users/" + id)
      .update({ Volume: volume });
  },

  UpdateFontSize: function(fontSize) {
    firestore()
      .doc("users/" + id)
      .update({ FontSize: fontSize });
  }
});
