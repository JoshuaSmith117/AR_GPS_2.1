using GoogleARCore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPS : MonoBehaviour
{
    public static GPS Instance { get; set; }

    public float latitude;
    public float longitude;
    public bool inAssemblyHall;
    public bool inMemorialStadium;

    private void Start()
    {
            AndroidPermissionsManager.RequestPermission("android.permission.ACCESS_FINE_LOCATION");
            Instance = this;
            StartCoroutine(StartLocationService());
    }

    private IEnumerator StartLocationService()
    {
        if (AndroidPermissionsManager.IsPermissionGranted("android.permission.ACCESS_FINE_LOCATION") == true)
        {
            if (!Input.location.isEnabledByUser)
            {
                latitude = -2;
                Debug.Log("User has not enabled GPS");
                yield break;
            }

            Input.location.Start();
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            if (maxWait <= 0)
            {
                latitude = -1;
                Debug.Log("Timed Out");
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.Log("Unable to determine device location");
                yield break;
            }
        }
    }
    public void Update() {
        latitude = Input.location.lastData.latitude;
        longitude = Input.location.lastData.longitude;

        inAssemblyHall = false;

        if (latitude >= 39.180313f && latitude <= 39.181455f && longitude >= -86.523000f && longitude <= -86.521439f)
        {
            inAssemblyHall = true;
        }
        else if (latitude >= 39.179150f && latitude <= 39.182659f && longitude >= -86.527511f && longitude <= -86.523799f)
        {
            inMemorialStadium = true;
        }
    }

}
