﻿//-----------------------------------------------------------------------
// <copyright file="HelloARController.cs" company="Google">
//
// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace GoogleARCore.HelloAR
{
    using System.Collections.Generic;
    using System.Collections;
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.UI;
    using UnityEngine.SceneManagement;

    public class ControllerScript : MonoBehaviour
    {
        public Text coordinates;
        public Text TipText;
        public Text Instruction;
        public Text highscoreText;
        public Text beginHighscoreText;
        public Text scoreText;
        public Text gameMode;
        public Text banter;
        public Text playAgain;
        public Text description;

        private Text Timer;

        public bool ballinhand;
        private bool readytospawn = true;

        public bool canTouch = true;
        public bool isGoalPlaced = false;
        public bool isPlaying = false;
        public bool isPaused = false;
        public bool gameOver = false;
        public bool SChasBegun = false;
        public bool FThasBegun = false;
        public bool searchingForSurfaces = true;
        public bool flickControls;

        public AudioSource buzzer;

        public float StarttimeLeft;
        private float timeLeft;
        public float countdown;


        public int highscore;

        private GameObject[] basketballs;
        public GameObject[] goals;
        public GameObject startMenu;
        public GameObject welcomeMenu;
        public GameObject gameOverMenu;
        public GameObject gui;
        public GameObject pauseMenu;
        public GameObject snackBar;
        private GameObject backboardLight;
        public GameObject HSPanel;
        public GameObject newHSPanel;


        public Text countdownText;

        [HideInInspector] public GameObject scoreCanvas;
        private Animator scoreAnimator;

        private BBallScoreHandler BBallscoreHandler;
        
        /// The first-person camera being used to render the passthrough camera image (i.e. AR background).
        public Camera FirstPersonCamera;
        
        /// A prefab for tracking and visualizing detected planes.
        public GameObject TrackedPlanePrefab;
        
        /// A model to place when a raycast from a user touch hits a plane.
        public GameObject AndyAndroidPrefab;
        public GameObject BasketballPrefab;
        public GameObject BasketballGoalPrefab;
        
        /// A gameobject parenting UI for displaying the "searching for planes" snackbar.
        public GameObject TipUI;
        public GameObject radar;
        public GameObject menuBBall;

        //A list to hold new planes ARCore began tracking in the current frame. This object is used across
        //the application to avoid per-frame allocations.
        private List<TrackedPlane> m_NewPlanes = new List<TrackedPlane>();
        
        // A list to hold all planes ARCore is tracking in the current frame. This object is used across
        // the application to avoid per-frame allocations.
        private List<TrackedPlane> m_AllPlanes = new List<TrackedPlane>();
        
        //True if the app is in the process of quitting due to an ARCore connection error, otherwise false.
        private bool m_IsQuitting = false;
        
        //True if the detected planes should be visualized with transparent meshes.
        public bool VisualizePlanes = false;

        public void Start()
        {
            highscore = PlayerPrefs.GetInt("highscore", 0);
            menuBBall.SetActive(false);
            highscore = 0;
            PlayerPrefs.SetInt("highscore", highscore);
            PlayerPrefs.Save();
        }

        public void Update()
        {
            basketballs = GameObject.FindGameObjectsWithTag("basketball");
            goals = GameObject.FindGameObjectsWithTag("goal");
            if (SChasBegun == false && FThasBegun == false)
            {
                if (searchingForSurfaces == true)
                {
                    menuBBall.SetActive(false);
                    radar.SetActive(true);
                    Instruction.enabled = true;
                    Instruction.text = "Please scan the ground with your camera.";
                }
            }
            // If the player has pressed the "Shoot some hoops" button...
            else if (SChasBegun == true || FThasBegun == true)
            {
                //If the player still needs to find a surface...
                if (searchingForSurfaces == true)
                {
                    Instruction.enabled = true;
                    Instruction.text = "Please scan the ground with your camera.";
                }

                else if (searchingForSurfaces == false && goals.Length <= 0)
                {
                    Instruction.enabled = true;
                    Instruction.text = "Please scan the ground.";
                }
                //If the player lost the loactaion of the basketball goal.
                else if (isPaused == true)
                {
                    Instruction.enabled = true;
                    Instruction.text = "Please scan the area again to find the basketball goal.";
                }
                else
                {
                    Instruction.enabled = false;
                }
            }

            if (isPlaying == true && isGoalPlaced == true)
            {
                if (SChasBegun == true)
                {
                    timeLeft -= Time.deltaTime;
                }
                if (basketballs.Length <= 10 && readytospawn == true)
                {
                    var basketballObject = Instantiate(BasketballPrefab, Camera.main.transform.position - new Vector3(0, .3f, 0) + Camera.main.transform.forward * .5f, Camera.main.transform.rotation);
                    basketballObject.transform.parent = Camera.main.transform;
                    ballinhand = true;
                    readytospawn = false;
                    Debug.Log("Ball Spawned");
                    StartCoroutine(spawntimer());
                }

                if (Mathf.Round(timeLeft) >= 10)
                {
                    Timer.text = Mathf.Round(timeLeft).ToString();
                }
                else if (Mathf.Round(timeLeft) <= 9 && (Mathf.Round(timeLeft) >= 1))
                {
                    Timer.text = "0" + Mathf.Round(timeLeft).ToString();
                }

                if (SChasBegun == true && timeLeft <= 0 && gameOver == false)
                {
                    IEnumerator coroutine = GameOver();
                    StartCoroutine(coroutine);
                }
            }
            if (goals.Length <= 0)
            {
                isGoalPlaced = false;
                TipUI.SetActive(true);
                if (isPlaying == false)
                {
                    startMenu.SetActive(false);
                }
            }
            else if (goals.Length == 1)
            {
                isGoalPlaced = true;
                TipUI.SetActive(false);
            }
            else
            {
                isGoalPlaced = false;
                welcomeMenu.SetActive(true);
            }
            // ARCore stuff that I probably shouldn't mess with. //////////////////////////////////
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            _QuitOnConnectionErrors();
            // Check that motion tracking is tracking.

            if (Frame.TrackingState != TrackingState.Tracking)
            {
                const int lostTrackingSleepTimeout = 15;
                Screen.sleepTimeout = lostTrackingSleepTimeout;
                return;
            }

            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Iterate over planes found in this frame and instantiate corresponding GameObjects to visualize them.
            if (VisualizePlanes)
            {
                Frame.GetPlanes(m_NewPlanes, TrackableQueryFilter.New);
                for (int i = 0; i < m_NewPlanes.Count; i++)
                {
                    // Instantiate a plane visualization prefab and set it to track the new plane. The transform is set to
                    // the origin with an identity rotation since the mesh for our prefab is updated in Unity World
                    // coordinates.
                    GameObject planeObject = Instantiate(TrackedPlanePrefab, Vector3.zero, Quaternion.identity,
                        transform);
                    planeObject.GetComponent<TrackedPlaneVisualizer>().Initialize(m_NewPlanes[i]);
                }
            }
            //////////////////////////////////////////////////////////////////////////////////////

            // Disable the snackbar UI when no planes are valid.
            if (GPS.Instance.inAssemblyHall == true || GPS.Instance.inMemorialStadium == true)
            {
                Frame.GetPlanes(m_AllPlanes);
                searchingForSurfaces = true;
                for (int i = 0; i < m_AllPlanes.Count; i++)
                {
                    if (m_AllPlanes[i].TrackingState == TrackingState.Tracking)
                    {
                        searchingForSurfaces = false;
                        snackBar.SetActive(false);
                        radar.SetActive(false);
                        Instruction.enabled = false;
                        if (SChasBegun == true && goals.Length <= 0 || FThasBegun == true && goals.Length <= 0)
                        {
                            snackBar.SetActive(true);
                            TipText.text = "Waiting for goal to be placed...";
                            Instruction.enabled = true;
                            Instruction.text = "Tap the grid to place a goal.";
                        }
                        menuBBall.SetActive(true);
                        break;
                    }
                    else
                    {
                        searchingForSurfaces = true;
                        snackBar.SetActive(true);
                        TipText.text = "Searching for surfaces...";
                        radar.SetActive(true);
                        break;
                    }
                }
            }
            //If player is in Assembly Hall...
            if (GPS.Instance.inAssemblyHall == true || GPS.Instance.inMemorialStadium == true)
            {
                //Keeps the basketBall count no greater than 6
                if (basketballs.Length <= 5)
                {
                    // If the player has not touched the screen, we are done with this update.
                    Touch touch;
                    if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
                    {
                        return;
                    }

                    // Raycast against the location the player touched to search for planes.
                    TrackableHit hit;
                    TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinBounds | TrackableHitFlags.PlaneWithinPolygon;

                    //If the player touches a surface...
                    if (Session.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
                    {

                        // If the player has pressed "Shoot some Hoops button, but still needs to place a goal...
                        if (isGoalPlaced == false && SChasBegun == true)
                        {
                            //Spawn a basketball goal.
                            var basketballGoalObject = Instantiate(BasketballGoalPrefab, hit.Pose.position, hit.Pose.rotation);
                            startMenu.SetActive(true);

                            // Create an anchor.
                            var anchor = hit.Trackable.CreateAnchor(hit.Pose);

                            // BasketballGoal should be flush with the plane.
                            basketballGoalObject.transform.rotation = Quaternion.Euler(0.0f,
                            basketballGoalObject.transform.rotation.eulerAngles.y - 180, basketballGoalObject.transform.rotation.z);

                            // Make basketballGoal model a child of the anchor.
                            basketballGoalObject.transform.parent = anchor.transform;
                            Debug.Log("Goal has been placed.");

                            //Reference scoreHandler script.
                            BBallscoreHandler = FindObjectOfType<BBallScoreHandler>();

                            //Reference Timer
                            Timer = basketballGoalObject.GetComponentInChildren<Text>();
                            scoreCanvas = GameObject.FindGameObjectWithTag("score");
                            scoreAnimator = scoreCanvas.GetComponent<Animator>();
                            backboardLight = GameObject.FindGameObjectWithTag("backboardlight");
                        }
                        else if (isGoalPlaced == false && FThasBegun == true)
                        {
                            //Spawn a basketball goal.
                            var basketballGoalObject = Instantiate(BasketballGoalPrefab, hit.Pose.position, hit.Pose.rotation);
                            startMenu.SetActive(true);

                            // Create an anchor.
                            var anchor = hit.Trackable.CreateAnchor(hit.Pose);

                            // BasketballGoal should be flush with the plane.
                            basketballGoalObject.transform.rotation = Quaternion.Euler(0.0f,
                            basketballGoalObject.transform.rotation.eulerAngles.y - 180, basketballGoalObject.transform.rotation.z);

                            // Make basketballGoal model a child of the anchor.
                            basketballGoalObject.transform.parent = anchor.transform;
                            Debug.Log("Goal has been placed.");

                            //Reference scoreHandler script.
                            BBallscoreHandler = FindObjectOfType<BBallScoreHandler>();

                            //Reference Timer
                            Timer = basketballGoalObject.GetComponentInChildren<Text>();
                            scoreCanvas = GameObject.FindGameObjectWithTag("score");
                            scoreAnimator = scoreCanvas.GetComponent<Animator>();
                            backboardLight = GameObject.FindGameObjectWithTag("backboardlight");
                        }
                    }
                }
            }
            else
            {
                radar.SetActive(true);
            }
        }

        IEnumerator countdownTimer()
        {
            countdown = 3;
            BBallscoreHandler.score = 0;
            gameOver = false;
            startMenu.SetActive(false);
            gameOverMenu.SetActive(false);
            backboardLight.SetActive(false);
            pauseMenu.SetActive(false);
            gui.SetActive(true);
            scoreAnimator.SetTrigger("beginPlay");
            countdownText.enabled = true;
            while(countdown > 0) {
                countdown -= Time.deltaTime;
                countdownText.text = Mathf.Round(countdown).ToString();
                yield return null;
            }
            countdownText.enabled = false;
            isPlaying = true;
            Timer.enabled = true;
            timeLeft = StarttimeLeft;
            readytospawn = true;
        }
        IEnumerator spawntimer ()
        {
            while (ballinhand == true)
            {
                yield return null;
            }
            yield return new WaitForSeconds(1);
            readytospawn = true;
        }

        public void ShotClock()
        {
            SChasBegun = true;
            HSPanel.SetActive(true);
            description.text = "Beat your higschore in 60 seconds.";
            beginHighscoreText.text = highscore.ToString();
            gameMode.text = "Shot Clock";
        }

        public void FreeThrow()
        {
            FThasBegun = true;
            HSPanel.SetActive(false);
            description.text = "No time limit. Practice makes perfect.";
            gameMode.text = "Free Throw";
        }

        public void PlayGame()
        {
            if (SChasBegun == true)
            {
                StartCoroutine(countdownTimer());
            }
            else if (FThasBegun == true) {
                countdownText.enabled = false;
                isPlaying = true;
                BBallscoreHandler.score = 0;
                gameOver = false;
                startMenu.SetActive(false);
                gameOverMenu.SetActive(false);
                backboardLight.SetActive(false);
                pauseMenu.SetActive(false);
                gui.SetActive(true);
                scoreAnimator.SetTrigger("beginPlay");
                Timer.enabled = true;
                Timer.text = "00";
                readytospawn = true;
            }
        }

        public void Continue()
        {
            isPaused = false;
            pauseMenu.SetActive(false);
            gui.SetActive(true);
            Time.timeScale = 1;
        }

        public void PauseGame()
        {
            isPaused = true;
            gui.SetActive(false);
            pauseMenu.SetActive(true);
            
            Time.timeScale = 0;
            if (SChasBegun == true)
            {
                highscoreText.text = "Highscore: " + highscore;
            }
            else if (FThasBegun == true)
            {
                highscoreText.text = null;
            }
        }

        IEnumerator GameOver()
        {
            gameOver = true;
            isPlaying = false;
            timeLeft = 0;
            Timer.text = "00";
            backboardLight.SetActive(true);
            Destroy(GameObject.FindGameObjectWithTag("inHands"));
            buzzer.Play();
            yield return new WaitForSeconds(1);
            gui.SetActive(false);
            pauseMenu.SetActive(false);
            gameOverMenu.SetActive(true);
            if (BBallscoreHandler.score < highscore)
            {
                playAgain.text = "Not quite... Try Again!";
                newHSPanel.SetActive(false);
            }
            else if (BBallscoreHandler.score == highscore)
            {
                playAgain.text = "So close! You'll get it next time!";
                newHSPanel.SetActive(false);
            }
            else if (BBallscoreHandler.score >= highscore)
            {
                playAgain.text = "Congratulations! Keep going!";
                newHSPanel.SetActive(true);
            }
            StoreHighscore();
            scoreText.text = BBallscoreHandler.score.ToString();
            highscoreText.text = highscore.ToString();
            if (BBallscoreHandler.score < 10)
            {
                banter.text = "That was hard to watch!";
            }
            else if (BBallscoreHandler.score >= 10 && BBallscoreHandler.score < 20)
            {
                banter.text = "Come on! You can do better than that!";
            }
            else if (BBallscoreHandler.score >= 20 && BBallscoreHandler.score < 30)
            {
                banter.text = "Alright, you're gettin' there!";
            }
            else if (BBallscoreHandler.score >= 30 && BBallscoreHandler.score < 40)
            {
                banter.text = "That wasn't too shabby, hotshot! ";
            }
            else if (BBallscoreHandler.score >= 40 && BBallscoreHandler.score < 50)
            {
                banter.text = "I'll proudly call you a Hoosier!";
            }
            else if (BBallscoreHandler.score >= 50 && BBallscoreHandler.score < 60)
            {
                banter.text = "Wow! You lookin' to join the team?";
            }
            else if (BBallscoreHandler.score >= 60 && BBallscoreHandler.score < 70)
            {
                banter.text = "Look out! This player's on fire! ";
            }
            else if (BBallscoreHandler.score >= 70 && BBallscoreHandler.score < 80)
            {
                banter.text = "With skills like that, you'll go pro in no time!";
            }
            else if (BBallscoreHandler.score >= 80 && BBallscoreHandler.score < 90)
            {
                banter.text = "Spectacular! Outstanding! Some third thing!";
            }
            else if (BBallscoreHandler.score >= 90 && BBallscoreHandler.score < 100)
            {
                banter.text = "Yogi, is that you?";
            }
            else if (BBallscoreHandler.score >= 90 && BBallscoreHandler.score < 100)
            {
                banter.text = "That's it! I've seen it all! Time to retire!";
            }
            foreach (GameObject ball in basketballs)
            {
                Destroy(ball);
            }
        }
        
        public void MainMenu()
        {
            BBallscoreHandler.score = 0;
            FThasBegun = false;
            SChasBegun = false;
            isPlaying = false;
            isPaused = false;
            gameOver = false;
            gameOverMenu.SetActive(false);
            pauseMenu.SetActive(false);
            backboardLight.SetActive(false);
            Timer.enabled = false;
            welcomeMenu.SetActive(true);
            Time.timeScale = 1;
            foreach (GameObject goal in goals)
            {
                Destroy(goal);
            }
            foreach (GameObject ball in basketballs)
            {
                Destroy(ball);
            }
            Destroy(GameObject.FindGameObjectWithTag("inHands"));
        }

        void StoreHighscore()
        {
            Debug.Log("Score: " + BBallscoreHandler.score);
            highscore = PlayerPrefs.GetInt("highscore", 0);
            Debug.Log("Highscore: " + highscore);
            if (BBallscoreHandler.score > highscore) {
                PlayerPrefs.SetInt("highscore", BBallscoreHandler.score);
                PlayerPrefs.Save();
            }
            highscore = PlayerPrefs.GetInt("highscore", 0);
            Debug.Log("new highscore: " + highscore);
        }

        // ARCore Stuff I probably shouldn't mess with. ///////////////////////////////////////////////////////////

        /// Quit the application if there was a connection error for the ARCore session.
        private void _QuitOnConnectionErrors()
        {
            if (m_IsQuitting)
            {
                return;
            }

            // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
            if (Session.ConnectionState == SessionConnectionState.UserRejectedNeededPermission)
            {
                _ShowAndroidToastMessage("Camera permission is needed to run this application.");
                m_IsQuitting = true;
                Invoke("DoQuit", 0.5f);
            }
            else if (Session.ConnectionState == SessionConnectionState.ConnectToServiceFailed)
            {
                _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                m_IsQuitting = true;
                Invoke("DoQuit", 0.5f);
            }
        }
        
        /// Actually quit the application.
        private void DoQuit()
        {
            Application.Quit();
        }

        public void Reset()
        {
            DontDestroyOnLoad(Camera.main);
            SceneManager.LoadScene("AR_GPS_2.1");
        }

        /// Show an Android toast message.
        /// <param name="message">Message string to show in the toast.</param>
        private void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                        message, 0);
                    toastObject.Call("show");
                }));
            }
        }
        //////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
