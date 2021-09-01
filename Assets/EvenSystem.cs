using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class EvenSystem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{

    private string path;
    public string nameFile;
    private Manager manager;
    
    private void Start() 
    {
        path = gameObject.transform.Find("Path/Text").GetComponent<TextMeshProUGUI>().text;
        nameFile = gameObject.transform.Find("File Name/Text").GetComponent<TextMeshProUGUI>().text;
        manager = GameObject.Find("Manager").GetComponent<Manager>();
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        if(nameFile.Contains(".png")) manager.GetTexture(path, nameFile);
        else if(nameFile.Contains(".txt")) manager.GetText(path, nameFile);
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {

    }

}
