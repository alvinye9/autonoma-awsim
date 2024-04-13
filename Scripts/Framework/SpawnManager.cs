/* 
Copyright 2023 Autonoma, Inc.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at:

    http://www.apache.org/licenses/LICENSE-2.0

The software is provided "AS IS", WITHOUT WARRANTY OF ANY KIND, 
express or implied. In no event shall the authors or copyright 
holders be liable for any claim, damages or other liability, 
whether in action of contract, tort or otherwise, arising from, 
out of or in connection with the software or the use of the software.
*/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

public class SpawnManager : MonoBehaviour 
{
    [SerializeField]
    private GameObject vehiclePrefab;
    public Material[] materials;
    public RaceControlMenuController raceControlMenu;
    private TrackParams trackParams;
    public GlobalCameraManager globalCameraManager;
    private AssetBundle myLoadedAssetBundle = null;



    private void Awake()
    {
        globalCameraManager = FindObjectOfType<GlobalCameraManager>();
    }

    private void Start()
    {
        trackParams = GameManager.Instance.Settings.myTrackParams; //default track params are from the map's respective .asset file

        // trackParams.LAT_ORIGIN = 45.618974079378670; //will not change spawn beahvior, only what the gnsssimulator publishes    
        // trackParams.LON_ORIGIN = 9.281181751068655; //MONZA_default
        // trackParams.carRotation = new Vector3(0f, 45f, 0f); //this will change both the gnss sensors and the way the car spawns in the sim (+y is +azimuth +NED_yaw)
        Debug.Log("Track Name: " + trackParams.TrackName);
        Debug.Log("LAT_ORIGIN: " + trackParams.LAT_ORIGIN);
        Debug.Log("LON_ORIGIN: " + trackParams.LON_ORIGIN);
        Debug.Log("HEIGHT_ORIGIN: " + trackParams.HEIGHT_ORIGIN);  
        Debug.Log("carRotation: " + trackParams.carRotation); 

        trackParams.carSpawnPositions.RemoveRange(1, trackParams.carSpawnPositions.Count - 1); //remove all but the first spawn position

        // // Change the spawn position of the first vehicle relative to origin, default is (0.0, -6.0, 0.0) //likely wont need to change this, (+y is UP)
        // ChangeSpawnPosition(new Vector3(-450f, -6f, 0f), 0); //LVMS spawn
        // Debug.Log("Printing Car Spawn Positions After Modifications:");
        // foreach (var position in trackParams.carSpawnPositions)
        // {
        //     Debug.Log(position.ToString());
        // }


        trackParams.populateStartPositions();

        SpawnEnvironment();

        for (int i = 0; i < GameManager.Instance.Settings.myScenarioObj.NumCars; i++)
        {   
            SpawnVehicle(i);
        }
    }

    // Method to change spawn position //Added
    public void ChangeSpawnPosition(Vector3 newPosition, int spawnIndex)
    {
        if (trackParams != null && spawnIndex >= 0 && spawnIndex < trackParams.carSpawnPositions.Count)
        {
            trackParams.carSpawnPositions[spawnIndex] = newPosition;
        }
        else
        {
            Debug.LogWarning("Invalid spawn position index or TrackParams reference is null.");
        }
    }

    private void OnDestroy()
    {
        if (myLoadedAssetBundle != null)
        {
            myLoadedAssetBundle.Unload(true);
        }
    }

    public void SpawnVehicle(int idx)
    {
        //Object, Position, Rotation
        GameObject vehicleInstance = Instantiate(vehiclePrefab, 
            trackParams.carSpawnPositions[GameManager.Instance.Settings.myScenarioObj.Cars[idx].SpawnPositionIdx],
            transform.rotation);
        vehicleInstance.transform.Rotate(trackParams.carRotation);

        raceControlMenu.rosCars.Add(vehicleInstance);

        Material[] mats = vehicleInstance.transform.Find("Models").Find("Body").Find("Chassis").GetComponent<MeshRenderer>().materials;
        mats[0] = materials[(int) (GameManager.Instance.Settings.myScenarioObj.Cars[idx].Color) ];
        vehicleInstance.transform.Find("Models").Find("Body").Find("Chassis").GetComponent<MeshRenderer>().materials = mats;

        GameObject[] vehicleCameras = vehicleInstance.transform.Find("Cameras").GetComponent<CameraList>().cameras;

       
    
        for(int i = 0; i < vehicleCameras.Length; i++) 
        {
            globalCameraManager.allCarCameraList.Add(new CarCameraPair(vehicleCameras[i], vehicleInstance));
        }

        // Handle the enabling/disabling of Publishers based on ControlType
        bool isROS = (GameManager.Instance.Settings.myScenarioObj.Cars[idx].ControlType == ControlType.ROS);
        var vehiclePublishers = vehicleInstance.GetComponentsInChildren<Autonoma.IPublisherBase>();
        foreach (var pub in vehiclePublishers)
        {
            //pub.ToggleActive(isROS);
            pub.ToggleActive(true);
        }

        // Handle the enabling/disabling of Inputs based on ControlType
        Autonoma.VehicleInputSubscriber[] vehicleSubscribers = vehicleInstance.GetComponentsInChildren<Autonoma.VehicleInputSubscriber>();
        KeyboardInputs[] keyboardInputs = vehicleInstance.GetComponentsInChildren<KeyboardInputs>();
        foreach (KeyboardInputs ki in keyboardInputs)
        {
            //ki.gameObject.SetActive(!isROS);
            ki.gameObject.SetActive(true);
        }
        foreach (Autonoma.VehicleInputSubscriber vi in vehicleSubscribers)
        {
            //vi.gameObject.SetActive(isROS);
            vi.gameObject.SetActive(true);
        }
    }

    public void SpawnEnvironment()
    {
        string path = Application.streamingAssetsPath;//"Assets/Autonoma/Environments/";
        string bundleName;
        string trackName = GameManager.Instance.Settings.myTrackParams.TrackName+".prefab";
        bool isBundleLoaded = false;

        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.OSXPlayer:
                bundleName = "osx_racetracks.v2";
                break;

            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.LinuxEditor:
                bundleName = "linux_racetracks.v2";
                break;

            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
                bundleName = "windows_racetracks.v2";
                break;

            default:
                Debug.LogError("Unsupported platform!");
                return;
        }

        foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (bundle.name == bundleName)
            {
                Debug.Log("AssetBundle is already loaded.");
                isBundleLoaded = true;
                break;
            }
        }

        if (!isBundleLoaded)
        {
            myLoadedAssetBundle = AssetBundle.LoadFromFile(Path.Combine(path, bundleName));
            
            if (myLoadedAssetBundle == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                return;
            }

            GameObject track = myLoadedAssetBundle.LoadAsset<GameObject>(trackName); //trackName is read from tracklist

            //The following presets are based on the PAIR vegas2.csv racelines
            if(trackName.Equals("LVMS.prefab"))
            {
                Debug.Log("INSTANTIATING LVMS TRACK");
                // Specify the position, rotation, and scale for the new track instance
                Vector3 position = new Vector3(262f, 11.5f, 368f);  
                Quaternion rotation = Quaternion.Euler(0, 180f, 0);  
                Vector3 scale = new Vector3(0.8f, 1f, 0.8f); 
                GameObject instantiatedTrack = Instantiate(track, position, rotation);
                instantiatedTrack.transform.localScale = scale;
            }
            else
            {
                Debug.Log("INSTANTIATING TRACK: "+ trackName);
                Instantiate(track);
            }
            
        }
    }

}