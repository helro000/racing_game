using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour
{

    private GameObject Player;
    private controller RR;
    private GameObject cameralookAt, cameraPos;
    private float speed = 0;
    private float defaltFOV = 0, desiredFOV = 0;
    [Range(0, 50)] public float smothTime = 8;

    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        RR = Player.GetComponent<controller>();
        cameralookAt = Player.transform.Find("camera lookAt").gameObject;
        cameraPos = Player.transform.Find("camera constraint").gameObject;

        defaltFOV = Camera.main.fieldOfView;
        desiredFOV = defaltFOV + 15;
    }

    private void LateUpdate()
    {
        if (RelayRace.Instance != null && Player != null)
        {
            var heading = Quaternion.Euler(0, Player.transform.eulerAngles.y, 0);
            var target = Player.transform.position + heading * new Vector3(0, 3.2f, -6.0f);
            transform.position = Vector3.Lerp(transform.position, target, 1 - Mathf.Exp(-7 * Time.deltaTime));
            transform.LookAt(Player.transform.position + heading * new Vector3(0, 1.2f, 6));
            Camera.main.fieldOfView = 65;
            return;
        }
        follow();
        boostFOV();

    }
    private void follow()
    {
        speed = Mathf.Max(5f, RR.KPH / Mathf.Max(1f, smothTime));
        gameObject.transform.position = Vector3.Lerp(transform.position, cameraPos.transform.position, Time.deltaTime * speed);
        gameObject.transform.LookAt(cameralookAt.gameObject.transform.position);
    }
    private void boostFOV()
    {

        if (RR.nitrusFlag)
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, desiredFOV, Time.deltaTime * 5);
        else
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, defaltFOV, Time.deltaTime * 5);

    }

}