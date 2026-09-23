using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class awakeManager : MonoBehaviour
{

    [Header("Camera")]
    public float lerpTime;
    public GameObject CameraObject;
    public GameObject finalCameraPosition , startCameraPosition;

    [Header("Deafault Canvas")]
    public GameObject DeafaultCanvas;
    public Text DeafaultCanvasCurrency;

    [Header("Map Canvas")]
    public GameObject mapSelectorCanvas;

    [Header("Upgrades Canvas")]
    public Text upgradesCurrency;
    public GameObject upgradesCanvas;

    [Header("Vehicle Select Canvas")]
    public GameObject vehicleSelectCanvas;
    public GameObject buyButton;
    public GameObject startButton;
    public vehicleList listOfVehicles;
    public Text currency;
    public Text carInfo;



    public GameObject toRotate;
    [HideInInspector]public float rotateSpeed = 10f;
    [HideInInspector]public int vehiclePointer = 0;
    private bool finalToStart,startToFinal;

    private void Awake() {
        mapSelectorCanvas.SetActive(false);
        DeafaultCanvas.SetActive(true);
        vehicleSelectCanvas.SetActive(false);

        DeafaultCanvasCurrency.text ="$" + PlayerPrefs.GetInt("currency").ToString();
        upgradesCurrency.text = "$" + PlayerPrefs.GetInt("currency").ToString();


        vehiclePointer = Mathf.Clamp(PlayerPrefs.GetInt("pointer"),0,listOfVehicles.vehicles.Length-1);
        PlayerPrefs.SetInt("pointer",vehiclePointer);
        if(GetComponent<RelayGarage>() == null) gameObject.AddComponent<RelayGarage>();
        GameObject childObject = Instantiate(listOfVehicles.vehicles[vehiclePointer],Vector3.zero,toRotate.transform.rotation) as GameObject;
        childObject.transform.parent = toRotate.transform;
        getCarInfo();

    }

    private void FixedUpdate() {
        toRotate.transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
        cameraTranzition();
    }

    public void rightButton(){
        if(vehiclePointer < listOfVehicles.vehicles.Length-1){
            Destroy(GameObject.FindGameObjectWithTag("Player"));
            vehiclePointer++;
            PlayerPrefs.SetInt("pointer",vehiclePointer);
            GameObject childObject = Instantiate(listOfVehicles.vehicles[vehiclePointer],Vector3.zero,toRotate.transform.rotation) as GameObject;
            childObject.transform.parent = toRotate.transform;
            getCarInfo();
        }
    }

    public void leftButton(){
        if(vehiclePointer > 0){
            Destroy(GameObject.FindGameObjectWithTag("Player"));
            vehiclePointer--;
            PlayerPrefs.SetInt("pointer",vehiclePointer);
            GameObject childObject = Instantiate(listOfVehicles.vehicles[vehiclePointer],Vector3.zero,toRotate.transform.rotation) as GameObject;
            childObject.transform.parent = toRotate.transform;
            getCarInfo();
        }
    }

    public void startGameButton(){
        DeafaultCanvas.SetActive(false);
        vehicleSelectCanvas.SetActive(false);
        mapSelectorCanvas.SetActive(true);

    }

    // All vehicles are available immediately in this version.
    public void BuyButton(){
        getCarInfo();
    }

    public void getCarInfo(){
        var selectedCar = listOfVehicles.vehicles[vehiclePointer].GetComponent<controller>();
        carInfo.text = selectedCar.carName + " - Free";
        startButton.SetActive(true);
        buyButton.SetActive(false);
        currency.text = "$" + PlayerPrefs.GetInt("currency").ToString();
    }
    public void DeafaultCanvasStartButton(){
        mapSelectorCanvas.SetActive(false);
        DeafaultCanvas.SetActive(false);
        vehicleSelectCanvas.SetActive(true);
        upgradesCanvas.SetActive(false);
        startToFinal = true;
        finalToStart = false;
    }

    public void vehicleSelectCanvasStartButton(){
        mapSelectorCanvas.SetActive(false);
        DeafaultCanvas.SetActive(true);
        vehicleSelectCanvas.SetActive(false);
        finalToStart = true;
        startToFinal = false;

    }

    public void upgradesCanvasButton(){
        upgradesCanvas.SetActive(true);
        vehicleSelectCanvas.SetActive(false);

    }

    public void cameraTranzition(){
        if(startToFinal){
            CameraObject.transform.position = Vector3.Lerp(CameraObject.transform.position,finalCameraPosition.transform.position,lerpTime * Time.deltaTime); 
        }
        if(finalToStart){
            CameraObject.transform.position = Vector3.Lerp(CameraObject.transform.position,startCameraPosition.transform.position,lerpTime * Time.deltaTime); 
        }

    }

    public void loadMarioMap(){
        SceneManager.LoadScene("superMarioMap");
    }
    
    public void loadComunityMap(){
        SceneManager.LoadScene("ComunityMap");
    }

    public void loadEasyRoadsMap(){
        SceneManager.LoadScene("EasyRoadsMap");
    }


}


