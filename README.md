# Bonspiel Games Emotiv Unity Plugin

This is the plugin used in Curl for integration with the Emotiv Cortex Service, adapted from [the official support](https://github.com/Emotiv/unity-plugin) for simplicity and support for multiple simultaneous headset streams. The contents are formatted as a Unity package and can be added to a unity project via git URL.

## Prerequisites
1. Install Unity. You can get it for free at [unity3d.com](https://unity3d.com/get-unity/download).

2. Make an EmotivID on [the official website](https://emotiv.com)

3. Create a Cortex application on [the account page](https://emotiv.com/my-account) to get an app client id and secret.
**MAKE SURE TO NOTE THE CLIENT SECRET AS IT WILL ONLY BE SHOWN ONCE**

4. [Download and install](https://www.emotiv.com/developer/) the EMOITV Launcher

## How to Use
More detail is available in [the documentation](https://bonspiel-games.gitbook.io/cortex-unity-plugin//)

1. Import the EmotivUnityPlugin namespace
```cs
using EmotivUnityPlugin
```

2. Call Cortex.Start() with your cortex app's client id and secret. Using Awake() and script execution order in your project settings can ensure that this is called before any other monobehavior functions, letting you freely use Cortex functionality anywhere else without worry.
```cs
Cortex.Start(clientId, clientSecret);
```

3. Subscribe to the Cortex.HeadsetQueryResult to receive a list of available headsets
```cs
Cortex.HeadsetQueryResult += (List<Headset> headsets) => Debug.Log($"{headsets.Count} headsets detected!");
```

4. Connect to a headset, starting data streams
```cs
Cortex.StartSession(headsetId);
```

5. Load or create a profile
```cs
Cortex.profiles.QueryProfiles();
Cortex.profiles.LoadProfile(profileName, headsetId);
Cortex.profiles.CreateProfile(profile2);
```

6. Subscribe to a headset's data streams from anywhere
```cs
Cortex.SubscribeMentalCommands(headsetId, (MentalCommand command) => Debug.Log(command));
Cortex.SubscribeDataStream<MentalCommand>(headsetId, OnMentalCommandReceived);
Cortex.SubscribeDeviceInfo(headsetId, OnDeviceInfoReceived)
```

7. Train mental commands for a profile in engine with live feedback from the data stream
```cs
Cortex.training.GetTrainedActions(profileName);
Cortex.training.StartTraining(actionName);
Cortex.training.AcceptTraining(actionName);
```