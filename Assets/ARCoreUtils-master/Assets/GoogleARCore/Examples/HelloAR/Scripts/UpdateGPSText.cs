using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GoogleARCore;
using GoogleARCore.HelloAR;

public class UpdateGPSText : MonoBehaviour
{
    private ControllerScript control;
    public Text location;

    public Text coordinates;
    private void Start()
    {
        control = FindObjectOfType<ControllerScript>();
    }
    private void Update()
    {
        if (control.FThasBegun == true || control.SChasBegun == true)
        {
            coordinates.enabled = false;
        } else if (control.FThasBegun == false && control.SChasBegun == false);
        {
            coordinates.enabled = true;
            coordinates.text = "Lat:" + GPS.Instance.latitude.ToString() + "   Long:" + GPS.Instance.longitude.ToString();
        }

        if (GPS.Instance.inAssemblyHall == true)
        {
            location.text = "Assembly Hall";
        }
        else if (GPS.Instance.inMemorialStadium == true)
        {
            location.text = "Memorial Stadium";
        }
        else
        {
            location.text = "Unknown";
        }
    }
}
