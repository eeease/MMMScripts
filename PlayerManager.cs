namespace MultiplayerWithBindingsExample
{
	using System.Collections.Generic;
	using InControl;
	using UnityEngine;
    using UnityEngine.EventSystems;
    using System.Collections;

    // This example iterates on the basic multiplayer example by using action sets with
    // bindings to support both joystick and keyboard players. It would be a good idea
    // to understand the basic multiplayer example first before looking a this one.
    //
    public class PlayerManager : MonoBehaviour
	{
        public static PlayerManager PM;
		public GameObject playerPrefab;
        public List<PlayerJoinPanel> playerPanels; // for the join screen.
        public GameObject joinScreenGO; //to turn off when starting game.
        public List<MyEventSystem> kbEventSystems;
        public List<MyEventSystem> controllerEvents;
        public float startDelay, startDelayOG;

		const int maxPlayers = 2;

        [Tooltip("Player spawn points")]
		//public List<Vector3> playerPositions = new List<Vector3>() {
		//	new Vector3( -1, 1, -10 ),
		//	new Vector3( 1, 1, -10 ),
		//	new Vector3( -1, -1, -10 ),
		//	new Vector3( 1, -1, -10 ),
		//};
        public List<Transform> playerPositions;
        public List<Vector3> spawnPoses;

		public List<PlayerShoot> players = new List<PlayerShoot>( maxPlayers );

		PlayerActions keyboardListener;
		PlayerActions joystickListener;

        public Animator titleScreenAnim;
        public GameObject optionsInfo;
        Board refAutoPlayB;
        float rTimeScale = 1;

        private void Awake()
        {
            PM = this;
        }
        void OnEnable()
		{
            spawnPoses[0] = playerPositions[0].position;
            spawnPoses[1] = playerPositions[1].position;
			InputManager.OnDeviceDetached += OnDeviceDetached;
			keyboardListener = PlayerActions.CreateWithKeyboardBindings();
			joystickListener = PlayerActions.CreateWithJoystickBindings();
		}


		void OnDisable()
		{
			InputManager.OnDeviceDetached -= OnDeviceDetached;
			joystickListener.Destroy();
			keyboardListener.Destroy();
		}


		void Update()
		{
			if (JoinButtonWasPressedOnListener( joystickListener ))
			{
				var inputDevice = InputManager.ActiveDevice;

				if (ThereIsNoPlayerUsingJoystick( inputDevice ))
				{
                    if (GameManager.GM.onJoinScreen)
                    {
                        //CreatePlayer( inputDevice );
                        TurnOnPanel(inputDevice);
                        startDelay = startDelayOG;

                    }else if (GameManager.GM.onTitleScreen)
                    {
                        StartCoroutine(TurnOnEventSystem());
                    }
                    else if (GameManager.GM.playingGame && players.Count<maxPlayers)
                    {
                        JoinMidGame(inputDevice);
                    }

                }
            }

			if (JoinButtonWasPressedOnListener( keyboardListener ))
			{
                //var inputDevice = InputManager.ActiveDevice;

                if (ThereIsNoPlayerUsingKeyboard())
				{
                    if (GameManager.GM.onJoinScreen)
                    {
                        //CreatePlayer( inputDevice );
                        TurnOnPanel(null);
                        startDelay = startDelayOG;

                    }
                    else if (GameManager.GM.onTitleScreen)
                    {
                        StartCoroutine(TurnOnEventSystemKB());
                    }
                }
			}

            if (GameManager.GM.quickTest)
            {
                if (players.Count > 0 && !AllPlayersOn())
                {
                    SetAllFPSControllers(true);
                    //joinScreenGO.SetActive(false);

                }
            }

            if (AllPlayersReady() && players.Count>1)
            {
                //startDelay -= Time.deltaTime;
                if (startDelay <= 0)
                {
                    if (players.Count > 1 && !AllPlayersOn())
                    {
                        SetAllFPSControllers(true);
                    }
                    else
                    {
                        startDelay = startDelayOG;
                    }
                }
            }
		}


		bool JoinButtonWasPressedOnListener( PlayerActions actions )
		{
			return actions.Green.WasPressed || actions.Red.WasPressed || actions.Blue.WasPressed || actions.Yellow.WasPressed;
		}


        PlayerShoot FindPlayerUsingJoystick( InputDevice inputDevice )
		{
			var playerCount = players.Count;
			for (var i = 0; i < playerCount; i++)
			{
				var player = players[i];
				if (player.Device == inputDevice)
				{
					return player;
				}
			}

			return null;
		}


		bool ThereIsNoPlayerUsingJoystick( InputDevice inputDevice )
		{
			return FindPlayerUsingJoystick( inputDevice ) == null;
		}


        PlayerShoot FindPlayerUsingKeyboard()
		{
			var playerCount = players.Count;
            for (var i = 0; i < playerCount; i++)
            {
                var player = players[i];
                if (player.Actions == keyboardListener)
                {
                    return player;
                }
            }

            return null;
		}


		bool ThereIsNoPlayerUsingKeyboard()
		{
			return FindPlayerUsingKeyboard() == null;
		}


		void OnDeviceDetached( InputDevice inputDevice )
		{
			var player = FindPlayerUsingJoystick( inputDevice );
			if (player != null)
			{
				RemovePlayer( player );
			}
		}

        //for start menu navigation?
        IEnumerator TurnOnEventSystem()
        {
            controllerEvents[0].gameObject.SetActive(true);
            controllerEvents[0].SetSelectedGameObject(null);
            GameManager.GM.SetGameState(3);
            yield return new WaitForEndOfFrame();
            controllerEvents[0].SetSelectedGameObject(controllerEvents[0].firstSelectedGameObject);
            controllerEvents[0].firstSelectedGameObject.GetComponent<UnityEngine.UI.Button>().OnSelect(null);
            titleScreenAnim.SetTrigger("Zoom");

        }
        IEnumerator TurnOnEventSystemKB()
        {
            kbEventSystems[0].gameObject.SetActive(true);
            kbEventSystems[0].SetSelectedGameObject(null);
            GameManager.GM.SetGameState(3);
            yield return new WaitForEndOfFrame();
            kbEventSystems[0].SetSelectedGameObject(kbEventSystems[0].firstSelectedGameObject);
            kbEventSystems[0].firstSelectedGameObject.GetComponent<UnityEngine.UI.Button>().OnSelect(null);
            titleScreenAnim.SetTrigger("Zoom");

        }


        void TurnOnPanel(InputDevice device)
        {
            if (players.Count < maxPlayers)
            {
                //set the BeginButton clickable
                GameObject.Find("BeginButton").GetComponent<MyButton>().interactable = true;
                //print("turned on begin");
                // Pop a position off the list. We'll add it back if the player is removed.
                var playerPosition = spawnPoses[0];
                spawnPoses.RemoveAt(0);

                var gameObject = (GameObject)Instantiate(playerPrefab, playerPosition, Quaternion.identity);
                //var player = gameObject.GetComponent<Player>();
                var player = gameObject.GetComponentInChildren<PlayerShoot>();
                //player.Device = inputDevice;
                if (device==null)
                {
                    // We could create a new instance, but might as well reuse the one we have
                    // and it lets us easily find the keyboard player.
                    player.Actions = keyboardListener;
                }
                else
                {
                    // Create a new instance and specifically set it to listen to the
                    // given input device (joystick).
                    //var actions = PlayerActions.CreateWithJoystickBindings();
                    player.Device = device;

                    //player.Actions = actions;
                }

                players.Add(player);
                player.playerNum = players.IndexOf(player);
                //this will spawn them and just have them float there.
                gameObject.GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().enabled = false;
                player.GetComponent<Camera>().enabled = false;

                int index = players.IndexOf(player);
                playerPanels[index].myP = player;
                player.pinButton = playerPanels[index].pinButton;

                playerPanels[index].joinImage.SetActive(false);
                playerPanels[index].enemyHatPrev.SetActive(true);
                //Also turn on the options button so that it's not showing when there's no player:
                optionsInfo.SetActive(true);
                if (players.Count > 1)
                {
                    SetCameras();
                }

               
                //this is an attempt to get kb working in addition to controllers.
                //didn't quite work ;\
                foreach(PlayerJoinPanel p in playerPanels)
                {
                    //if you're joining from kb, set all the buttons to listen for the kb es
                    if (player.Actions==keyboardListener)
                    {
                        kbEventSystems[index].gameObject.SetActive(true);

                        player.m_events = kbEventSystems[index];

                        if (p.buttonProviders[0].eventSystem == null)//if an event system hasn't been assigned to these buttons yet.
                        {
                            kbEventSystems[index].SetSelectedGameObject(kbEventSystems[index].playerPanelFirst);

                            p.SetEventSystemsInChildButtons(kbEventSystems[index]);
                            //print("SETTING " + kbEventSystem.name + " into " + p.name);
                            break;
                        }
                    }
                    else
                    {
                        //print("in here with " + device);
                        player.m_events = controllerEvents[index];

                        if (p.buttonProviders[0].eventSystem == null)//if an event system hasn't been assigned to these buttons yet.
                        {
                            controllerEvents[index].gameObject.SetActive(true);

                            controllerEvents[index].SetSelectedGameObject(controllerEvents[index].playerPanelFirst);
                            p.SetEventSystemsInChildButtons(controllerEvents[index]);
                            break;
                        }

                    }
                }
                playerPanels[index].ChangeName();


                //!need to fix this to work with keyboard as well:
                //assign the input device to an incontrol input module:
                foreach (InControlInputModule g in FindObjectsOfType<InControlInputModule>())
                {
                    if (g.inputDevice == null && device!=null)
                    {
                        g.Device = device;
                        //g.inputDevice = device;
                        //print("assigned " + device + " to " + g.name);
                        return;
                    }
                }
               
            }


        }
        void JoinMidGame(InputDevice device)
        {
            if (players.Count < maxPlayers)
            {
                // Pop a position off the list. We'll add it back if the player is removed.
                var playerPosition = spawnPoses[0];
                spawnPoses.RemoveAt(0);

                var gameObject = (GameObject)Instantiate(playerPrefab, playerPosition, Quaternion.identity);
                //var player = gameObject.GetComponent<Player>();
                var player = gameObject.GetComponentInChildren<PlayerShoot>();
                //player.Device = inputDevice;
                if (kbEventSystems[players.IndexOf(player)].gameObject.activeSelf  && device==null)
                {
                    // We could create a new instance, but might as well reuse the one we have
                    // and it lets us easily find the keyboard player.
                    player.Actions = keyboardListener;
                }
                else
                {
                    // Create a new instance and specifically set it to listen to the
                    // given input device (joystick).
                    //var actions = PlayerActions.CreateWithJoystickBindings();
                    player.Device = device;

                    //player.Actions = actions;
                }

                players.Add(player);
                player.playerNum = players.IndexOf(player);
                player.teamNum = player.playerNum; //this happens at start of playershoot.cs, but i want to do it here for FindOpposingPlayers()
                if (player.Actions == keyboardListener)
                {
                    player.m_events = kbEventSystems[players.IndexOf(player)];

                
                }
                else
                {
                    //print("in here with " + device);
                    controllerEvents[players.IndexOf(player)].gameObject.SetActive(true);
                    player.m_events = controllerEvents[players.IndexOf(player)];
                }

                //this will spawn them and just have them float there.
                //gameObject.GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().enabled = false;
                //player.GetComponent<Camera>().enabled = false;

                //int index = players.IndexOf(player);
                //playerPanels[index].myP = player;
                //player.pinButton = playerPanels[index].pinButton;

                //playerPanels[index].joinImage.SetActive(false);
                //playerPanels[index].enemyHatPrev.SetActive(true);
                //Also turn on the options button so that it's not showing when there's no player:
                //optionsInfo.SetActive(true);
                if (players.Count > 1)
                {
                    SetCameras();
                    foreach(PlayerShoot ps in players)
                    {
                        ps.FindOpposingPlayers(); //should help with laneinfo

                    }
                }


                //this is an attempt to get kb working in addition to controllers.
                //didn't quite work ;\
                //foreach(PlayerJoinPanel p in playerPanels)
                //{
                //    //if you're joining from kb, set all the buttons to listen for the kb es
                //    if (player.Actions==keyboardListener)
                //    {
                //        player.m_events = kbEventSystem;

                //        if (p.buttonProviders[0].eventSystem == null)//if an event system hasn't been assigned to these buttons yet.
                //        {
                //            p.SetEventSystemsInChildButtons(kbEventSystem);
                //            //print("SETTING " + kbEventSystem.name + " into " + p.name);
                //            break;
                //        }
                //    }
                //    else
                //    {
                //        print("in here with " + device);
                //        player.m_events = controllerEvents[index];

                //        if (p.buttonProviders[0].eventSystem == null)//if an event system hasn't been assigned to these buttons yet.
                //        {
                //            controllerEvents[index].gameObject.SetActive(true);

                //            controllerEvents[index].SetSelectedGameObject(controllerEvents[index].playerPanelFirst);
                //            p.SetEventSystemsInChildButtons(controllerEvents[index]);
                //            break;
                //        }

                //    }
                //}
                //playerPanels[index].ChangeName();


                //!need to fix this to work with keyboard as well:
                //assign the input device to an incontrol input module:
                foreach (InControlInputModule g in FindObjectsOfType<InControlInputModule>())
                {
                    if (g.inputDevice == null && device!=null)
                    {
                        g.Device = device;
                        //g.inputDevice = device;
                        //print("assigned " + device + " to " + g.name);
                        return;
                    }
                }

                    //not totally necessary for 2p but let's see.
                if (refAutoPlayB != null)
                {
                    refAutoPlayB.autoPlay = true;

                }
                GameManager.GM.everythingPause = false;
                Time.timeScale = rTimeScale;
                UIManager.UIM.ContDiscon(false);

            }


        }

        bool AllPlayersReady()
        {
            bool allAboard = true;
            if (players.Count > 0)
            {
                foreach (PlayerShoot ps in players)
                {
                    if(!ps.isReady)
                    {
                        allAboard = false;
                    }
                }
            }
            else
            {
                allAboard = false;
            }
            return allAboard;
        }

        public void SetAllFPSControllers(bool trueOn)
        {
            if (GameManager.GM.turboMode)
            {
                Time.timeScale = 2f;
            }
            GameManager.GM.SetGameState(2);
            UIManager.UIM.gameInfoCan.gameObject.SetActive(true);
            UIManager.UIM.TurnOnPanels(0);

            //if you're playing gem frenzy, turn on the slots here?
            if(GameManager.GM.gameMode==1)
            StartCoroutine(UIManager.UIM.SpinGFSlot(4f));

            //joinScreenGO.SetActive(false); //handling this from button now with nifty cinematic.

            if (players.Count > 0)
            {
                foreach (PlayerShoot ps in players)
                {
                    ps.GetComponentInParent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().enabled = trueOn;

                    //commented this out 15 july 2020, not sure why this was on:
                    //ps.transform.LookAt(ps.myBoard.transform);
                    ps.GetComponent<Camera>().enabled = trueOn;
                    ps.LoadOptions(ps.myPIN);
                    ps.FindOpposingPlayers();
                }
                //foreach(MyEventSystem i in controllerEvents)
                //{
                //    i.gameObject.SetActive(false);
                //}
            }
            //!!THESE could be issues with a pause button or a quit button that stops time:
            //start the minions:
            if (GameManager.GM.autoMinions && GameManager.GM.gameMode!=1)
            {
                ObjectsManager.instance.InvokeRepeating("SpawnDownAllLanes", GameManager.GM.autoMinionInterval, GameManager.GM.autoMinionInterval);
                //ObjectsManager.instance.SpawnDownAllLanes();
            }
            if(GameManager.GM.eventsOn)
            {
                float erval = GameManager.GM.eventInterval;
                EventsManager.EM.InvokeRepeating("ChooseACard", erval, erval);
            }

            //turn off towers?:
            if (!GameManager.GM.towersOn || GameManager.GM.gameMode==1)
            {
                //!!Need to also turn off tower healths, probably.
                foreach(Tower t in FindObjectsOfType<Tower>())
                {
                    if (!t.gameObject.name.Contains("CORE"))
                    {
                        t.gameObject.SetActive(false);
                    }
                }
            }
            if(players.Count==1)
            GameManager.GM.SetAutoPlayDifficulty();

        }

        bool AllPlayersOn()
        {
            bool allOn = true;
            foreach (PlayerShoot ps in players)
            {
                if(!ps.GetComponent<Camera>().enabled)
                {
                    allOn= false;
                }

            }
            return allOn;
        }


        PlayerShoot CreatePlayer( InputDevice inputDevice )
		{
			if (players.Count < maxPlayers)
			{
				// Pop a position off the list. We'll add it back if the player is removed.
				var playerPosition = spawnPoses[0];
                spawnPoses.RemoveAt( 0 );

				var gameObject = (GameObject) Instantiate( playerPrefab, playerPosition, Quaternion.identity );
                //var player = gameObject.GetComponent<Player>();
                var player = gameObject.GetComponentInChildren<PlayerShoot>();

                //player.Device = inputDevice;
                if (inputDevice == null)
                {
                    // We could create a new instance, but might as well reuse the one we have
                    // and it lets us easily find the keyboard player.
                    player.Actions = keyboardListener;
                }
                else
                {
                    // Create a new instance and specifically set it to listen to the
                    // given input device (joystick).
                    //var actions = PlayerActions.CreateWithJoystickBindings();
                    player.Device = inputDevice;

                    //player.Actions = actions;
                }

                players.Add( player );
                player.playerNum = players.IndexOf(player);


                if (players.Count > 1)
                {
                    SetCameras();
                }

				return player;
			}

			return null;
		}

        void SetCameras()
        {
            //print("Setting cams");
            players[0].GetComponent<Camera>().rect = new Rect(.25f, .5f, 1, 1);
            players[1].GetComponent<Camera>().rect = new Rect(.25f, -.5f, 1, 1);
        }


		void RemovePlayer(PlayerShoot player )
		{
            if (GameManager.GM.onJoinScreen)
            {
                //turn the join image back on:
                playerPanels[players.IndexOf(player)].joinImage.SetActive(true);
                playerPanels[players.IndexOf(player)].myP = null;

            }
            //store the autoplaying board and get it to stop if a controller was disconnected.
            foreach(Board b in FindObjectsOfType<Board>())
            {
                if (b.autoPlay)
                {
                    b.autoPlay = false;
                    refAutoPlayB = b;
                }
            }
            spawnPoses.Insert( 0, player.transform.parent.position );
			players.Remove( player );
			player.Device= null;
            UIManager.UIM.ContDiscon(true);
            GameManager.GM.everythingPause = true; //pause the whole game if a controller was disconnected.
            rTimeScale = Time.timeScale;
            Time.timeScale = 0;


            Destroy( player.transform.parent.gameObject );

        }
        public void VibrateDef(InputDevice what)
        {

            if (Application.platform != RuntimePlatform.OSXPlayer && Application.platform != RuntimePlatform.OSXEditor)
            {
                print("not on mac, so vibrate!");
                StartCoroutine(Vibrate(what, .2f, .2f));
            }               
        }

        public IEnumerator Vibrate(InputDevice what, float inten, float del)
        {
            if (what!=null && Application.platform != RuntimePlatform.OSXPlayer && Application.platform != RuntimePlatform.OSXEditor)
            {
                what.Vibrate(inten);
                yield return new WaitForSeconds(del);
                what.StopVibration();
            }
        }


        //void OnGUI()
        //{
        //	const float h = 22.0f;
        //	var y = 10.0f;

        //	GUI.Label( new Rect( 10, y, 300, y + h ), "Active players: " + players.Count + "/" + maxPlayers );
        //	y += h;

        //	if (players.Count < maxPlayers)
        //	{
        //		GUI.Label( new Rect( 10, y, 300, y + h ), "Press a button or a/s/d/f key to join!" );
        //		y += h;
        //	}
        //}
        public void TitleScreenEventButton()
        {
            StartCoroutine(TurnOnEventSystemKB());

        }
    }

   
}